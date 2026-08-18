"""Node-first plate: free routes are truth; illustration is painted FROM those roads."""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "unity/PackspireUnity/Assets/Resources/Art/Map"
DATA = ROOT / "unity/PackspireUnity/Assets/Resources/Data"
ART.mkdir(parents=True, exist_ok=True)
DATA.mkdir(parents=True, exist_ok=True)
W, H = 2048, 1152
RNG = random.Random(11)

DISTRICT_META = [
    ("bailey_outer", "外郭", False),
    ("bailey_market", "城下", True),
    ("bailey_inner", "内郭", True),
    ("spire_base", "主塔裾", True),
    ("undercroft", "地下水路口", True),
]

# Hand-authored route spines — illustration will be built around these.
OUTER_SPINES = [
    [(0.50, 0.80), (0.49, 0.72), (0.48, 0.64), (0.47, 0.56), (0.48, 0.50), (0.50, 0.44)],
    [(0.48, 0.56), (0.42, 0.54), (0.36, 0.50), (0.30, 0.46), (0.27, 0.42)],
    [(0.48, 0.56), (0.52, 0.52), (0.56, 0.48), (0.58, 0.44)],
    [(0.50, 0.50), (0.58, 0.48), (0.64, 0.46), (0.70, 0.44), (0.74, 0.42)],
    [(0.49, 0.72), (0.42, 0.68), (0.36, 0.62), (0.34, 0.56), (0.38, 0.52), (0.48, 0.56)],
    [(0.50, 0.44), (0.46, 0.40), (0.42, 0.38)],
]

LOCKED_SPINES = {
    "bailey_market": [
        [(0.58, 0.36), (0.62, 0.32), (0.66, 0.28), (0.70, 0.26)],
        [(0.62, 0.32), (0.58, 0.28), (0.56, 0.24)],
        [(0.66, 0.28), (0.72, 0.30), (0.74, 0.26)],
    ],
    "bailey_inner": [
        [(0.66, 0.24), (0.70, 0.20), (0.74, 0.16), (0.76, 0.14)],
        [(0.70, 0.20), (0.74, 0.22), (0.78, 0.18)],
        [(0.72, 0.16), (0.68, 0.14), (0.66, 0.12)],
    ],
    "spire_base": [
        [(0.78, 0.16), (0.82, 0.12), (0.86, 0.10), (0.88, 0.08)],
        [(0.82, 0.12), (0.84, 0.16), (0.88, 0.14)],
        [(0.86, 0.10), (0.84, 0.06), (0.80, 0.06)],
    ],
    "undercroft": [
        [(0.20, 0.34), (0.16, 0.28), (0.14, 0.22), (0.18, 0.18)],
        [(0.16, 0.28), (0.22, 0.26), (0.26, 0.22)],
        [(0.14, 0.22), (0.12, 0.16), (0.16, 0.14)],
    ],
}

LANDMARK_TARGETS = [
    ("遠征入口", "entrance", True, "", 0.50, 0.80),
    ("鍛造所の扉", "building_door", True, "forge_interior", 0.27, 0.42),
    ("兵舎の扉", "building_door", True, "barracks_interior", 0.58, 0.44),
    ("監視塔下", "battle", True, "", 0.74, 0.42),
    ("小さな祠", "event", True, "", 0.34, 0.56),
    ("中庭の影", "battle", False, "", 0.48, 0.56),
    ("野営跡", "rest", False, "", 0.42, 0.38),
    ("朽ちた石橋", "empty", True, "", 0.48, 0.64),
]


def clamp01(x: float) -> float:
    return max(0.01, min(0.99, x))


def dist2(a, b):
    return (a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2


def polyline_length(pts):
    total = 0.0
    for i in range(1, len(pts)):
        total += math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1])
    return total


def sample_polyline(pts, t: float):
    if len(pts) == 1:
        return pts[0]
    target = t * polyline_length(pts)
    acc = 0.0
    for i in range(1, len(pts)):
        seg = math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1])
        if acc + seg >= target or i == len(pts) - 1:
            u = 0 if seg < 1e-9 else (target - acc) / seg
            u = max(0.0, min(1.0, u))
            return (
                pts[i - 1][0] + (pts[i][0] - pts[i - 1][0]) * u,
                pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * u,
            )
        acc += seg
    return pts[-1]


def jitter(u, v, amount=0.006):
    return clamp01(u + RNG.uniform(-amount, amount)), clamp01(v + RNG.uniform(-amount, amount))


def place_on_spines(spines, count, min_sep=0.026):
    weights = [max(0.001, polyline_length(s)) for s in spines]
    total_w = sum(weights)
    pts = []
    attempts = 0
    while len(pts) < count and attempts < count * 50:
        attempts += 1
        r = RNG.random() * total_w
        acc = 0.0
        spine = spines[0]
        for s, w in zip(spines, weights):
            acc += w
            if r <= acc:
                spine = s
                break
        u, v = jitter(*sample_polyline(spine, RNG.random()), 0.008)
        if any(dist2((u, v), p) < min_sep * min_sep for p in pts):
            continue
        pts.append((u, v))
    spine = max(spines, key=polyline_length)
    t = 0.0
    while len(pts) < count:
        pts.append(jitter(*sample_polyline(spine, t), 0.01))
        t = min(1.0, t + 1.0 / count)
    return pts[:count]


def link_nearby(points, max_deg=3, reach=0.055):
    n = len(points)
    links = [set() for _ in range(n)]
    for i in range(n):
        order = sorted(range(n), key=lambda j: dist2(points[i], points[j]) if j != i else 9)
        for j in order:
            if j == i:
                continue
            if math.sqrt(dist2(points[i], points[j])) > reach:
                break
            if len(links[i]) >= max_deg:
                break
            links[i].add(j)
            links[j].add(i)
    for i in range(1, n):
        if links[i]:
            continue
        j = min(range(i), key=lambda k: dist2(points[i], points[k]))
        links[i].add(j)
        links[j].add(i)
    order = sorted(range(n), key=lambda i: (points[i][1], points[i][0]))
    for a, b in zip(order, order[1:]):
        if math.sqrt(dist2(points[a], points[b])) < reach * 1.3:
            links[a].add(b)
            links[b].add(a)
    return [sorted(s) for s in links]


def chain_links_along_spines(points, spines, links, snap=0.042):
    for spine in spines:
        steps = max(4, int(polyline_length(spine) / 0.028))
        samples = [sample_polyline(spine, i / steps) for i in range(steps + 1)]
        idxs, used = [], set()
        for s in samples:
            best, bd = None, 9.0
            for i, p in enumerate(points):
                if i in used:
                    continue
                d = math.sqrt(dist2(s, p))
                if d < bd:
                    best, bd = i, d
            if best is not None and bd <= snap:
                idxs.append(best)
                used.add(best)
        for a, b in zip(idxs, idxs[1:]):
            if a != b:
                links[a].add(b)
                links[b].add(a)
    return [sorted(s) for s in links]


def apply_landmarks(cells):
    used = set()
    for name, ctype, landmark, interior, tu, tv in LANDMARK_TARGETS:
        best, bd = None, 9.0
        for c in cells:
            if c["id"] in used or c["locked"]:
                continue
            d = dist2((c["u"], c["v"]), (tu, tv))
            if d < bd:
                best, bd = c, d
        if best is None:
            continue
        used.add(best["id"])
        best["name"] = name
        best["type"] = ctype
        best["landmark"] = landmark
        best["interiorMapId"] = interior or ""


def build_district_free(did, dname, spines, count, locked, id_base, reach=0.055):
    pts = place_on_spines(spines, count, min_sep=0.022 if locked else 0.025)
    raw = [set(x) for x in link_nearby(pts, max_deg=3 if not locked else 2, reach=reach)]
    links = chain_links_along_spines(pts, spines, raw, snap=0.04)
    cells = []
    for i, (u, v) in enumerate(pts):
        cells.append(
            {
                "id": id_base + i,
                "districtId": did,
                "name": f"{dname}{i}",
                "type": "empty",
                "gx": i % 8,
                "gy": i // 8,
                "u": round(u, 4),
                "v": round(v, 4),
                "tileU": 0.016,
                "tileV": 0.012,
                "landmark": False,
                "locked": locked,
                "interiorMapId": "",
                "links": [id_base + j for j in links[i]],
            }
        )
    return cells


def build_cells():
    cells = build_district_free("bailey_outer", "外郭", OUTER_SPINES, 36, False, 0, reach=0.058)
    apply_landmarks(cells)
    base = 36
    counts = {"bailey_market": 38, "bailey_inner": 38, "spire_base": 40, "undercroft": 36}
    for did, dname, locked in DISTRICT_META[1:]:
        chunk = build_district_free(did, dname, LOCKED_SPINES[did], counts[did], locked, base, reach=0.048)
        cells.extend(chunk)
        base += counts[did]
    return cells


def fbm(x, y, octaves=5):
    v = 0.0
    a = 0.5
    f = 1.0
    for _ in range(octaves):
        v += a * (np.sin(x * f * 1.7 + y * f * 0.9) * np.cos(x * f * 0.6 - y * f * 1.3))
        a *= 0.5
        f *= 2.05
    return v


def quad_curve_uv(a, b, steps=16):
    mx, my = (a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5
    dx, dy = b[0] - a[0], b[1] - a[1]
    ln = math.hypot(dx, dy) or 1.0
    bend = 0.01 * (1 if hash((round(a[0], 3), round(b[1], 3))) % 2 == 0 else -1)
    ctrl = (mx + (-dy) / ln * bend, my + dx / ln * bend)
    pts = []
    for i in range(steps + 1):
        t = i / steps
        u = (1 - t) * (1 - t) * a[0] + 2 * (1 - t) * t * ctrl[0] + t * t * b[0]
        v = (1 - t) * (1 - t) * a[1] + 2 * (1 - t) * t * ctrl[1] + t * t * b[1]
        pts.append((u * W, v * H))
    return pts


def iso_building(draw, cx, cy, w, h, wall, roof, tall=1.0):
    """Simple isometric-ish building footprint painted at node."""
    hw, hh = w * 0.5, h * 0.35
    base = [
        (cx, cy + hh),
        (cx + hw, cy + hh * 0.15),
        (cx + hw * 0.15, cy - hh * tall),
        (cx - hw * 0.85, cy - hh * 0.35),
    ]
    roof_poly = [
        (cx + hw * 0.15, cy - hh * tall),
        (cx + hw, cy + hh * 0.15 - hh * 0.9),
        (cx - hw * 0.2, cy - hh * tall - hh * 0.55),
        (cx - hw * 0.85, cy - hh * 0.35 - hh * 0.7),
    ]
    draw.polygon(base, fill=wall)
    draw.polygon(roof_poly, fill=roof)
    # door
    draw.rectangle((cx - 6, cy - 4, cx + 6, cy + hh * 0.55), fill=(40, 28, 20, 230))


def paint_plate(cells) -> Image.Image:
    """Paint parchment map whose roads ARE the node links (no external plate)."""
    by_id = {c["id"]: c for c in cells}
    outer = [c for c in cells if not c["locked"]]
    locked = [c for c in cells if c["locked"]]

    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u, v = xx / W, yy / H
    n = fbm(u * 5.5, v * 4.2)
    n2 = fbm(u * 14.0 + 3.1, v * 11.0 - 1.7)
    parchment = np.stack(
        [
            168 + 22 * n + 10 * n2,
            142 + 18 * n + 8 * n2,
            98 + 14 * n + 6 * n2,
        ],
        axis=-1,
    )
    sea = (u + v * 0.35) < 0.26
    parchment[sea] = np.clip(np.array([70, 95, 118]) + (12 * n[..., None][sea]), 0, 255)
    cliff = ((u + v * 0.35) >= 0.26) & ((u + v * 0.35) < 0.32)
    parchment[cliff] = np.array([92, 78, 58]) + (8 * n[..., None][cliff])

    pad = np.zeros((H, W), dtype=np.float32)
    for c in outer:
        cx, cy = c["u"] * W, c["v"] * H
        d = ((xx - cx) / 70) ** 2 + ((yy - cy) / 52) ** 2
        pad = np.maximum(pad, np.clip(1.15 - d, 0, 1))
    for c in outer:
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by_id[lid]
            for i in range(13):
                t = i / 12
                cx = (c["u"] * (1 - t) + o["u"] * t) * W
                cy = (c["v"] * (1 - t) + o["v"] * t) * H
                d = ((xx - cx) / 38) ** 2 + ((yy - cy) / 30) ** 2
                pad = np.maximum(pad, np.clip(1.0 - d, 0, 1) * 0.85)
    yard = np.array([145, 128, 92])
    ground = parchment * (1 - pad[..., None] * 0.55) + yard * (pad[..., None] * 0.55)
    ground = np.clip(ground + n2[..., None] * 5, 0, 255)

    fog = np.zeros((H, W), dtype=np.float32)
    for c in locked:
        cx, cy = c["u"] * W, c["v"] * H
        d = ((xx - cx) / 78) ** 2 + ((yy - cy) / 54) ** 2
        fog = np.maximum(fog, np.clip(1.0 - d * 0.65, 0, 1) * 0.7)
    ground = ground * (1 - fog[..., None] * 0.45) + np.array([40, 34, 42]) * (fog[..., None] * 0.45)

    img = Image.fromarray(ground.astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.6))
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    for c in outer:
        cx, cy = c["u"] * W, c["v"] * H
        for r, a in ((48, 28), (72, 18)):
            draw.ellipse((cx - r, cy - r * 0.7, cx + r, cy + r * 0.7), outline=(90, 70, 45, a), width=1)

    if len(outer) >= 3:
        hull = sorted(
            [(c["u"] * W, c["v"] * H) for c in outer],
            key=lambda p: math.atan2(p[1] - H * 0.55, p[0] - W * 0.47),
        )
        draw.line(hull + [hull[0]], fill=(70, 60, 48, 140), width=22)
        draw.line(hull + [hull[0]], fill=(120, 105, 80, 170), width=10)

    for c in locked:
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by_id[lid]
            if o["locked"]:
                draw.line(quad_curve_uv((c["u"], c["v"]), (o["u"], o["v"])), fill=(55, 45, 40, 60), width=8)

    for c in outer:
        for lid in c["links"]:
            if lid <= c["id"]:
                continue
            o = by_id[lid]
            pix = quad_curve_uv((c["u"], c["v"]), (o["u"], o["v"]))
            draw.line(pix, fill=(55, 38, 22, 255), width=26)
            draw.line(pix, fill=(140, 105, 62, 255), width=18)
            draw.line(pix, fill=(190, 150, 95, 230), width=10)
            draw.line(pix, fill=(230, 195, 130, 110), width=3)

    keep = next((c for c in locked if c["districtId"] == "bailey_inner"), None)
    if keep:
        iso_building(draw, keep["u"] * W, keep["v"] * H, 150, 95, (55, 52, 62, 200), (40, 38, 48, 210), tall=1.5)

    for c in outer:
        cx, cy = c["u"] * W, c["v"] * H
        t = c["type"]
        if t == "building_door":
            forge = "鍛造" in c["name"]
            if forge:
                iso_building(draw, cx, cy - 10, 108, 76, (125, 82, 54, 250), (95, 50, 34, 250), 1.15)
                draw.rectangle((cx + 20, cy - 78, cx + 34, cy - 18), fill=(68, 52, 40, 245))
                for i, (sx, sy) in enumerate(((24, -96), (30, -118), (20, -136))):
                    draw.ellipse(
                        (cx + sx - 12 - i, cy + sy - 10, cx + sx + 16 + i, cy + sy + 12),
                        fill=(25, 25, 25, 65 - i * 12),
                    )
                draw.ellipse((cx - 9, cy - 20, cx + 11, cy - 2), fill=(255, 145, 55, 230))
            else:
                iso_building(draw, cx, cy - 8, 118, 80, (72, 80, 112, 250), (50, 54, 74, 250), 1.3)
                draw.rectangle((cx - 18, cy - 44, cx - 8, cy - 30), fill=(190, 210, 235, 210))
                draw.rectangle((cx + 8, cy - 44, cx + 18, cy - 30), fill=(190, 210, 235, 210))
        elif t == "entrance":
            for sx in (-34, 34):
                draw.rectangle((cx + sx - 14, cy - 60, cx + sx + 14, cy + 10), fill=(112, 106, 96, 250))
                draw.ellipse((cx + sx - 16, cy - 78, cx + sx + 16, cy - 52), fill=(104, 98, 88, 250))
            draw.rectangle((cx - 24, cy - 8, cx + 24, cy + 14), fill=(72, 52, 34, 245))
        elif t == "battle":
            iso_building(draw, cx, cy - 4, 54, 48, (95, 78, 60, 230), (70, 50, 40, 230), 0.9)
            draw.ellipse((cx - 7, cy - 38, cx + 7, cy - 24), fill=(170, 55, 45, 235))
        elif t == "event":
            draw.polygon([(cx, cy - 46), (cx + 14, cy - 6), (cx, cy + 6), (cx - 14, cy - 6)], fill=(175, 148, 98, 245))
            draw.rectangle((cx - 5, cy - 6, cx + 5, cy + 12), fill=(95, 72, 48, 235))
        elif t == "rest":
            draw.ellipse((cx - 24, cy - 4, cx + 24, cy + 16), fill=(78, 108, 62, 220))
            draw.polygon([(cx - 20, cy - 2), (cx, cy - 32), (cx + 20, cy - 2)], fill=(55, 88, 45, 220))

    for c in outer:
        cx, cy = c["u"] * W, c["v"] * H
        special = c["landmark"] or c["type"] != "empty"
        r = 7 if special else 4
        draw.ellipse((cx - r - 3, cy - r + 1, cx + r + 3, cy + r + 4), fill=(30, 20, 12, 120))
        col = {
            "entrance": (140, 185, 230, 245),
            "building_door": (195, 135, 230, 245),
            "battle": (225, 100, 75, 245),
            "event": (165, 135, 220, 245),
            "rest": (120, 195, 135, 245),
        }.get(c["type"], (245, 215, 155, 240))
        draw.ellipse((cx - r, cy - r + 1, cx + r, cy + r + 1), fill=col, outline=(60, 42, 24, 230), width=2)

    draw.rectangle((10, 10, W - 10, H - 10), outline=(40, 28, 16, 255), width=12)
    draw.rectangle((22, 22, W - 22, H - 22), outline=(160, 120, 60, 190), width=2)
    out = Image.alpha_composite(img.convert("RGBA"), overlay)
    out = ImageEnhance.Contrast(out).enhance(1.1)
    out = ImageEnhance.Color(out).enhance(0.92)
    return out.filter(ImageFilter.SMOOTH).convert("RGB")


def main():
    cells = build_cells()
    assert 180 <= len(cells) <= 200
    payload = {
        "id": "old_spire_iso",
        "name": "古塔パックスパイア",
        "artResource": "Art/Map/old-spire-iso-v1",
        "mapWidth": 20.48,
        "mapHeight": 11.52,
        "districts": [{"id": d, "name": n, "locked": L} for d, n, L in DISTRICT_META],
        "cells": cells,
    }
    (DATA / "old-spire-cells.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    # Prefer node-locked illustrated compose (parts + textured roads on exact links).
    compose = ROOT / "tools" / "compose_map_from_nodes.py"
    parts_ready = (ROOT / "tools" / "_map_parts").exists() and any(
        (ROOT / "tools" / "_map_parts").glob("part-*.png")
    )
    if compose.exists() and parts_ready:
        import runpy

        runpy.run_path(str(compose), run_name="__main__")
        art_path = ART / "old-spire-iso-v1.png"
    else:
        plate = paint_plate(cells)
        art_path = ART / "old-spire-iso-v1.png"
        plate.save(art_path, "PNG", optimize=True)

    ent = next(c for c in cells if c["type"] == "entrance")
    print(f"wrote {art_path} cells={len(cells)}")
    print(f"entrance id={ent['id']} uv=({ent['u']},{ent['v']}) art=from-nodes")


if __name__ == "__main__":
    main()
