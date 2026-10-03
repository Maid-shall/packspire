"""Compose product map art FROM locked node UVs/links (nodes are source of truth)."""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "unity/PackspireUnity/Assets/Resources/Art/Map"
DATA = ROOT / "unity/PackspireUnity/Assets/Resources/Data"
ASSETS = Path(r"C:\Users\p9-ti\.cursor\projects\c-maid-apps-Pick-Spire\assets")
PARTS_DIR = ROOT / "tools" / "_map_parts"
W, H = 2048, 1152
RNG = random.Random(17)

# Landmark paste: part file stem -> (target_u, target_v, width_px)
PARTS = [
    ("part-gate", 0.500, 0.787, 320),
    ("part-forge", 0.280, 0.430, 280),
    ("part-barracks", 0.569, 0.465, 300),
    ("part-tower", 0.735, 0.422, 200),
    ("part-shrine", 0.345, 0.548, 180),
    ("part-camp", 0.426, 0.383, 200),
]


def load_cells():
    return json.loads((DATA / "old-spire-cells.json").read_text(encoding="utf-8"))["cells"]


def fbm(u, v, octaves=5):
    n = np.zeros_like(u, dtype=np.float32)
    amp = 1.0
    freq = 1.0
    total = 0.0
    for i in range(octaves):
        x = u * freq
        y = v * freq
        x0 = np.floor(x).astype(np.int64)
        y0 = np.floor(y).astype(np.int64)
        fx = (x - x0).astype(np.float32)
        fy = (y - y0).astype(np.float32)

        def scramble(ix, iy):
            s = (ix * 374761393 + iy * 668265263 + i * 1274126177) % 2147483647
            s = (s * (s * s * 15731 + 789221) + 1376312589) % 2147483647
            return s.astype(np.float32) / 2147483647.0

        v00 = scramble(x0, y0)
        v10 = scramble(x0 + 1, y0)
        v01 = scramble(x0, y0 + 1)
        v11 = scramble(x0 + 1, y0 + 1)
        ux = fx * fx * (3 - 2 * fx)
        uy = fy * fy * (3 - 2 * fy)
        a = v00 * (1 - ux) + v10 * ux
        b = v01 * (1 - ux) + v11 * ux
        n += (a * (1 - uy) + b * uy) * amp
        total += amp
        amp *= 0.5
        freq *= 2.0
    return n / total


def make_terrain(cells) -> Image.Image:
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u, v = xx / W, yy / H
    n = fbm(u * 4.2, v * 3.4)
    n2 = fbm(u * 11.0 + 2.2, v * 9.0 - 1.1)
    ground = np.stack(
        [
            78 + 28 * n + 10 * n2,
            102 + 34 * n + 12 * n2,
            58 + 18 * n + 8 * n2,
        ],
        axis=-1,
    )
    # rocky patches
    rock = n2 > 0.62
    ground[rock] = np.array([92, 88, 82]) + (18 * n[..., None][rock])
    # water / cliff
    sea = (u * 0.9 + v * 0.45) < 0.30
    ground[sea] = np.clip(np.array([42, 68, 92]) + (16 * n[..., None][sea]), 0, 255)
    cliff = ((u * 0.9 + v * 0.45) >= 0.30) & ((u * 0.9 + v * 0.45) < 0.36)
    ground[cliff] = np.array([70, 62, 52]) + (10 * n[..., None][cliff])
    # yard brightening near outer nodes
    pad = np.zeros((H, W), dtype=np.float32)
    for c in cells:
        if c["locked"]:
            continue
        cx, cy = c["u"] * W, c["v"] * H
        d = ((xx - cx) / 85) ** 2 + ((yy - cy) / 62) ** 2
        pad = np.maximum(pad, np.clip(1.1 - d, 0, 1))
    yard = np.array([118, 128, 78], dtype=np.float32)
    ground = ground * (1 - pad[..., None] * 0.35) + yard * (pad[..., None] * 0.35)
    ground = np.clip(ground, 0, 255)
    img = Image.fromarray(ground.astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.7))
    # Atmosphere wash ONLY outside the playable pad — never import another plate's roads.
    wash_path = ASSETS / "old-spire-terrain-only-v1.png"
    if wash_path.exists():
        wash = Image.open(wash_path).convert("RGB").resize((W, H), Image.Resampling.LANCZOS)
        a = np.array(img).astype(np.float32)
        b = np.array(wash).astype(np.float32)
        # keep our generated ground where outer nodes live; wash only far fog/edges
        amount = (1.0 - pad) * 0.5
        amount = np.where(sea, 0.0, amount)
        mix = a * (1 - amount[..., None]) + b * amount[..., None]
        img = Image.fromarray(np.clip(mix, 0, 255).astype(np.uint8), "RGB")
    return img


def key_chroma(im: Image.Image) -> Image.Image:
    """Remove green/flat backdrop; keep building pixels."""
    rgba = im.convert("RGBA")
    a = np.array(rgba).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # green screen + near-solid backdrop
    green = (g > 90) & (g > r + 18) & (g > b + 12)
    # also knock out very flat mid greens
    flat = (np.abs(r - g) < 12) & (np.abs(g - b) < 18) & (g > 100) & (g < 190) & (r < 160)
    kill = green | flat
    alpha = np.where(kill, 0, 255).astype(np.uint8)
    # erode speckles then feather
    mask = Image.fromarray(alpha, "L")
    mask = mask.filter(ImageFilter.MaxFilter(3))
    mask = mask.filter(ImageFilter.MinFilter(3))
    mask = mask.filter(ImageFilter.GaussianBlur(1.2))
    a[..., 3] = np.array(mask)
    # dim residual fringe greens
    fringe = (g > r + 10) & (g > b + 8) & (a[..., 3] > 0)
    a[..., 3] = np.where(fringe, np.minimum(a[..., 3], 40), a[..., 3])
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def ensure_parts():
    PARTS_DIR.mkdir(parents=True, exist_ok=True)
    for stem, *_ in PARTS:
        src = ASSETS / f"{stem}.png"
        dst = PARTS_DIR / f"{stem}.png"
        if src.exists():
            Image.open(src).save(dst)
        elif not dst.exists():
            raise FileNotFoundError(f"missing part {stem}")


def _curve(a, b, steps=40):
    ax, ay = a[0] * W, a[1] * H
    bx, by_ = b[0] * W, b[1] * H
    mx = (ax + bx) * 0.5 + (ay - by_) * 0.06
    my = (ay + by_) * 0.5 + (bx - ax) * 0.04
    pts = []
    for i in range(steps + 1):
        t = i / steps
        x = (1 - t) * (1 - t) * ax + 2 * (1 - t) * t * mx + t * t * bx
        y = (1 - t) * (1 - t) * ay + 2 * (1 - t) * t * my + t * t * by_
        pts.append((x, y))
    return pts


def paint_roads(base: Image.Image, cells) -> Image.Image:
    """Stamp illustrated dirt texture exactly along node links."""
    by = {c["id"]: c for c in cells}
    dirt_path = ROOT / "tools" / "_dirt_sample.png"
    if dirt_path.exists():
        dirt = Image.open(dirt_path).convert("RGB").resize((W, H), Image.Resampling.BICUBIC)
    else:
        dirt = Image.new("RGB", (W, H), (150, 115, 70))
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    for c in cells:
        if c["locked"]:
            continue
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by[lid]
            if o.get("locked"):
                continue
            pts = _curve((c["u"], c["v"]), (o["u"], o["v"]))
            md.line(pts, fill=255, width=44)
            # soft shoulder
            md.line(pts, fill=200, width=56)
        # widen hubs
        cx, cy = c["u"] * W, c["v"] * H
        r = 22 if (c.get("landmark") or c["type"] != "empty") else 14
        md.ellipse((cx - r, cy - r * 0.7, cx + r, cy + r * 0.7), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(3.5))
    # darken edges of road
    edge = mask.filter(ImageFilter.FIND_EDGES).point(lambda p: min(255, p * 3))
    edge = edge.filter(ImageFilter.GaussianBlur(2))
    base_rgba = base.convert("RGBA")
    dirt_rgba = dirt.convert("RGBA")
    da = np.array(dirt_rgba).astype(np.float32)
    ba = np.array(base_rgba).astype(np.float32)
    m = (np.array(mask).astype(np.float32) / 255.0)[..., None]
    e = (np.array(edge).astype(np.float32) / 255.0)[..., None]
    # slightly darken dirt, burn edges
    da[..., :3] *= 0.92
    blended = ba * (1 - m * 0.92) + da * (m * 0.92)
    blended[..., :3] *= 1 - e * m * 0.25
    blended[..., 3] = 255
    out = Image.fromarray(np.clip(blended, 0, 255).astype(np.uint8), "RGBA")
    # light center highlight
    hi = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hi)
    for c in cells:
        if c["locked"]:
            continue
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by[lid]
            if o.get("locked"):
                continue
            hd.line(_curve((c["u"], c["v"]), (o["u"], o["v"])), fill=(230, 200, 150, 35), width=8)
    return Image.alpha_composite(out, hi)


def paint_wall(base: Image.Image, cells) -> Image.Image:
    outer = [(c["u"] * W, c["v"] * H) for c in cells if not c["locked"]]
    if len(outer) < 5:
        return base
    cx = sum(p[0] for p in outer) / len(outer)
    cy = sum(p[1] for p in outer) / len(outer)
    hull = sorted(outer, key=lambda p: math.atan2(p[1] - cy, p[0] - cx))
    ring = []
    for x, y in hull[:: max(1, len(hull) // 24)]:
        dx, dy = x - cx, y - cy
        n = math.hypot(dx, dy) or 1
        ring.append((x + dx / n * 70, y + dy / n * 52))
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    draw.line(ring + [ring[0]], fill=(40, 38, 44, 240), width=34)
    draw.line(ring + [ring[0]], fill=(96, 94, 104, 235), width=20)
    draw.line(ring + [ring[0]], fill=(150, 148, 158, 140), width=5)
    step = max(1, len(ring) // 7)
    for i in range(0, len(ring), step):
        x, y = ring[i]
        draw.rectangle((x - 16, y - 40, x + 16, y + 8), fill=(108, 106, 116, 245), outline=(50, 48, 56, 255))
        draw.rectangle((x - 18, y - 48, x + 18, y - 36), fill=(118, 116, 126, 245))
    return Image.alpha_composite(base.convert("RGBA"), overlay)


def paste_parts(base: Image.Image) -> Image.Image:
    out = base.convert("RGBA")
    for stem, tu, tv, width in PARTS:
        path = PARTS_DIR / f"{stem}.png"
        part = key_chroma(Image.open(path))
        # trim transparent
        bbox = part.getbbox()
        if bbox:
            part = part.crop(bbox)
        scale = width / max(1, part.width)
        nh = max(8, int(part.height * scale))
        part = part.resize((width, nh), Image.Resampling.LANCZOS)
        x = int(tu * W - width / 2)
        y = int(tv * H - nh * 0.78)
        shadow = Image.new("RGBA", (width, nh), (0, 0, 0, 0))
        sd = ImageDraw.Draw(shadow)
        sd.ellipse((width * 0.18, nh * 0.74, width * 0.82, nh * 0.96), fill=(15, 10, 6, 110))
        shadow = shadow.filter(ImageFilter.GaussianBlur(8))
        out.alpha_composite(shadow, (max(0, x), max(0, y)))
        out.alpha_composite(part, (max(0, x), max(0, y)))
    return out


def fog_locked(base: Image.Image, cells) -> Image.Image:
    arr = np.array(base.convert("RGBA")).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    fog = np.zeros((H, W), dtype=np.float32)
    for c in cells:
        if not c["locked"]:
            continue
        cx, cy = c["u"] * W, c["v"] * H
        d = ((xx - cx) / 95) ** 2 + ((yy - cy) / 66) ** 2
        fog = np.maximum(fog, np.clip(1.0 - d * 0.5, 0, 1) * 0.8)
    # also rim fog
    uu, vv = xx / W, yy / H
    edge = np.minimum(np.minimum(uu, 1 - uu), np.minimum(vv, 1 - vv))
    rim = np.clip((0.08 - edge) / 0.08, 0, 1)
    fog = np.maximum(fog, rim * 0.55)
    fog = fog[..., None]
    tint = np.array([32, 30, 38, 255], dtype=np.float32)
    arr = arr * (1 - fog * 0.6) + tint * (fog * 0.6)
    arr[..., 3] = 255
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")


def node_pips(base: Image.Image, cells) -> Image.Image:
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    for c in cells:
        if c["locked"]:
            continue
        cx, cy = c["u"] * W, c["v"] * H
        special = c.get("landmark") or c["type"] != "empty"
        r = 6 if special else 3
        draw.ellipse((cx - r - 2, cy - r + 2, cx + r + 2, cy + r + 4), fill=(20, 12, 8, 110))
        col = {
            "entrance": (150, 190, 230, 235),
            "building_door": (200, 145, 230, 235),
            "battle": (225, 105, 80, 235),
            "event": (170, 145, 220, 235),
            "rest": (125, 195, 140, 235),
        }.get(c["type"], (235, 210, 160, 215))
        draw.ellipse((cx - r, cy - r + 1, cx + r, cy + r + 1), fill=col, outline=(50, 35, 20, 230), width=2)
    return Image.alpha_composite(base.convert("RGBA"), overlay)


def main():
    ensure_parts()
    cells = load_cells()
    plate = make_terrain(cells)
    plate = paint_roads(plate, cells)
    plate = paint_wall(plate, cells)
    plate = paste_parts(plate)
    plate = fog_locked(plate, cells)
    plate = node_pips(plate, cells)
    plate = ImageEnhance.Contrast(plate.convert("RGB")).enhance(1.06)
    plate = ImageEnhance.Color(plate).enhance(0.96)
    out = ART / "old-spire-iso-v1.png"
    plate.save(out, "PNG", optimize=True)

    dbg = plate.convert("RGBA")
    d = ImageDraw.Draw(dbg)
    by = {c["id"]: c for c in cells}
    for c in cells:
        if c["locked"]:
            continue
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by[lid]
            if o.get("locked"):
                continue
            d.line([(c["u"] * W, c["v"] * H), (o["u"] * W, o["v"] * H)], fill=(0, 255, 90, 100), width=2)
    dbg.convert("RGB").save(ROOT / "tools/_composed_uv_check.png")
    print(f"wrote {out}")
    print("parts glued at landmark UVs; roads follow links")


if __name__ == "__main__":
    main()
