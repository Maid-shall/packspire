# -*- coding: utf-8 -*-
"""Process AI rite art into transparent Unity Resources PNGs."""
from __future__ import annotations

import secrets
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

SRC = Path(r"C:\Users\p9-ti\.cursor\projects\c-maid-apps-Pick-Spire\assets")
DST = Path(r"C:\maid apps\Pick Spire\unity\PackspireUnity\Assets\Resources\Art\Rite")
META_TEMPLATE = DST / "orb-fire-v1.png.meta"

JOBS = [
    ("orb-fire-v2-ai.png", "orb-fire-v2.png", "orb", 256),
    ("orb-water-v2-ai.png", "orb-water-v2.png", "orb", 256),
    ("orb-wind-v2-ai.png", "orb-wind-v2.png", "orb", 256),
    ("orb-earth-v2-ai.png", "orb-earth-v2.png", "orb", 256),
    ("rite-accent-merchant-ai.png", "rite-accent-merchant-v1.png", "accent", 1024),
    ("rite-accent-arcane-ai.png", "rite-accent-arcane-v1.png", "accent", 1024),
    ("rite-accent-coffin-ai.png", "rite-accent-coffin-v1.png", "accent", 1024),
    ("rite-accent-living-ai.png", "rite-accent-living-v1.png", "accent", 1024),
    ("rite-inner-spin-ai.png", "rite-inner-spin-v1.png", "inner", 1024),
]


def luminance(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def saturation(rgb: np.ndarray) -> np.ndarray:
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    return np.where(mx > 1e-6, (mx - mn) / mx, 0.0)


def edge_distance_norm(h: int, w: int) -> np.ndarray:
    ys, xs = np.mgrid[0:h, 0:w]
    dist_l = xs
    dist_r = w - 1 - xs
    dist_t = ys
    dist_b = h - 1 - ys
    dist = np.minimum(np.minimum(dist_l, dist_r), np.minimum(dist_t, dist_b)).astype(np.float32)
    # normalize: 0 at edge, 1 at center-ish for edge band of ~8% of min dim
    band = max(8.0, 0.08 * min(h, w))
    return np.clip(dist / band, 0.0, 1.0)


def key_background(rgba: np.ndarray, key_black_corners: bool = False) -> np.ndarray:
    out = rgba.copy()
    rgb = out[..., :3].astype(np.float32) / 255.0
    a = out[..., 3].astype(np.float32) / 255.0
    h, w = a.shape
    lum = luminance(rgb)
    sat = saturation(rgb)
    edge = edge_distance_norm(h, w)  # 0 at edge

    # Near-white / cream / light gray
    all_high = (rgb[..., 0] > 220 / 255.0) & (rgb[..., 1] > 220 / 255.0) & (rgb[..., 2] > 220 / 255.0)
    bright_desat = (lum > 0.88) & (sat < 0.18)
    # High luminance near edges (cream/gray fringe)
    near_edge_bright = (edge < 0.55) & (lum > 0.82) & (sat < 0.25)
    near_edge_very_bright = (edge < 0.85) & (lum > 0.92) & (sat < 0.22)

    bg = all_high | bright_desat | near_edge_bright | near_edge_very_bright

    if key_black_corners:
        # Pure/near-black in corner regions
        corner_r = int(0.18 * min(h, w))
        ys, xs = np.mgrid[0:h, 0:w]
        in_corner = (
            ((xs < corner_r) & (ys < corner_r))
            | ((xs >= w - corner_r) & (ys < corner_r))
            | ((xs < corner_r) & (ys >= h - corner_r))
            | ((xs >= w - corner_r) & (ys >= h - corner_r))
        )
        blackish = (lum < 0.08) & (sat < 0.35)
        bg = bg | (in_corner & blackish)

    # Soft alpha: fully transparent on strong bg, partial on softer matches
    strength = np.zeros_like(a)
    strength = np.where(all_high | bright_desat, 1.0, strength)
    strength = np.where(near_edge_very_bright & ~bg, np.maximum(strength, 0.85), strength)
    # For bg mask, use soft falloff based on how "background-like"
    bg_score = np.clip((lum - 0.75) / 0.20, 0.0, 1.0) * np.clip(1.0 - sat / 0.30, 0.0, 1.0)
    bg_score = np.where(edge < 0.7, np.maximum(bg_score, bg_score * (1.0 - edge)), bg_score)
    strength = np.where(bg, np.maximum(strength, np.clip(bg_score, 0.55, 1.0)), strength)
    strength = np.where(bg & all_high, 1.0, strength)

    a = a * (1.0 - strength)
    a = np.where(bg & (lum > 0.90) & (sat < 0.15), 0.0, a)

    out[..., 3] = (np.clip(a, 0, 1) * 255.0).astype(np.uint8)
    return out


def soften_fringe(rgba: np.ndarray, radius: int = 2) -> np.ndarray:
    """Soften hard alpha edges / color fringe near transparency."""
    img = Image.fromarray(rgba, "RGBA")
    alpha = img.split()[3]
    # Slight blur on alpha then recombine with unmatted-ish RGB
    blurred = alpha.filter(ImageFilter.GaussianBlur(radius=radius))
    # Keep fully opaque core, only soften transition
    a0 = np.array(alpha, dtype=np.float32)
    a1 = np.array(blurred, dtype=np.float32)
    # Prefer lower alpha near edge (erode fringe slightly) then soften
    a_soft = np.minimum(a0, a1)
    # Extra: where RGB is very light and alpha mid, reduce alpha more
    arr = np.array(img, dtype=np.float32)
    rgb = arr[..., :3] / 255.0
    lum = luminance(rgb)
    sat = saturation(rgb)
    fringe = (a_soft > 8) & (a_soft < 240) & (lum > 0.78) & (sat < 0.28)
    a_soft = np.where(fringe, a_soft * 0.35, a_soft)
    # Light despill: pull light fringe toward neighbor darkness... simple: darken bright fringe
    for c in range(3):
        arr[..., c] = np.where(fringe, arr[..., c] * 0.85, arr[..., c])
    arr[..., 3] = a_soft
    return np.clip(arr, 0, 255).astype(np.uint8)


def punch_soft_hole(rgba: np.ndarray, radius_frac: float) -> np.ndarray:
    """Soft transparent center hole. radius = size * radius_frac (user: size * 0.32 / 0.28)."""
    out = rgba.copy().astype(np.float32)
    h, w = out.shape[:2]
    size = min(h, w)
    radius = size * radius_frac
    cy, cx = (h - 1) / 2.0, (w - 1) / 2.0
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    dist = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2)
    # Soft edge ~4% of size
    feather = max(4.0, size * 0.04)
    # Fully clear inside radius - feather/2, ramp to opaque outside radius + feather/2
    inner = radius - feather * 0.5
    outer = radius + feather * 0.5
    hole = np.ones((h, w), dtype=np.float32)
    hole = np.where(dist <= inner, 0.0, hole)
    mid = (dist > inner) & (dist < outer)
    hole = np.where(mid, (dist - inner) / max(outer - inner, 1e-6), hole)
    out[..., 3] *= hole
    return np.clip(out, 0, 255).astype(np.uint8)


def write_meta(dst_png: Path, template_text: str) -> None:
    guid = secrets.token_hex(16)
    # Replace only the guid line
    lines = template_text.splitlines(keepends=True)
    new_lines = []
    for line in lines:
        if line.startswith("guid:"):
            new_lines.append(f"guid: {guid}\n")
        else:
            new_lines.append(line)
    meta_path = Path(str(dst_png) + ".meta")
    meta_path.write_text("".join(new_lines), encoding="utf-8")
    # Verify key flags
    text = meta_path.read_text(encoding="utf-8")
    assert "alphaIsTransparency: 1" in text
    assert "nPOTScale: 0" in text
    assert "enableMipMap: 0" in text
    return guid


def process_one(src_name: str, dst_name: str, kind: str, target: int, template: str) -> dict:
    src = SRC / src_name
    dst = DST / dst_name
    img = Image.open(src).convert("RGBA")
    arr = np.array(img)
    arr = key_background(arr, key_black_corners=(kind == "orb"))
    arr = soften_fringe(arr, radius=2 if kind == "orb" else 3)
    if kind == "accent":
        arr = punch_soft_hole(arr, 0.32)
    elif kind == "inner":
        arr = punch_soft_hole(arr, 0.28)

    out = Image.fromarray(arr, "RGBA")
    if out.size != (target, target):
        # Resize if larger OR if not already target (user: resize if larger / if needed)
        if max(out.size) > target or out.size != (target, target):
            out = out.resize((target, target), Image.Resampling.LANCZOS)

    DST.mkdir(parents=True, exist_ok=True)
    out.save(dst, "PNG")
    guid = write_meta(dst, template)

    a = np.array(out)[..., 3]
    return {
        "file": str(dst),
        "size": out.size,
        "bytes": dst.stat().st_size,
        "alpha_min": int(a.min()),
        "alpha_max": int(a.max()),
        "guid": guid,
    }


def main() -> None:
    template = META_TEMPLATE.read_text(encoding="utf-8")
    results = []
    for job in JOBS:
        r = process_one(*job, template=template)
        results.append(r)
        print(
            f"{Path(r['file']).name}: {r['size'][0]}x{r['size'][1]}  "
            f"{r['bytes']} bytes  alpha[{r['alpha_min']},{r['alpha_max']}]  guid={r['guid']}"
        )
    print("\nDone:", len(results), "files")


if __name__ == "__main__":
    main()
