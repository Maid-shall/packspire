# -*- coding: utf-8 -*-
"""Process crest + standard accent AI art into Unity Resources."""
from __future__ import annotations

import secrets
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

SRC = Path(r"C:\Users\p9-ti\.cursor\projects\c-maid-apps-Pick-Spire\assets")
DST = Path(r"C:\maid apps\Pick Spire\unity\PackspireUnity\Assets\Resources\Art\Rite")
META_TEMPLATE = DST / "orb-fire-v1.png.meta"

JOBS = [
    ("rite-crest-stable-ai.png", "rite-crest-stable-v1.png", "crest", 512),
    ("rite-crest-volatile-ai.png", "rite-crest-volatile-v1.png", "crest", 512),
    ("rite-shape-standard-ai.png", "rite-accent-standard-v1.png", "accent", 1024),
]


def luminance(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def saturation(rgb: np.ndarray) -> np.ndarray:
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    return np.where(mx > 1e-6, (mx - mn) / np.maximum(mx, 1e-6), 0.0)


def edge_distance_norm(h: int, w: int) -> np.ndarray:
    ys, xs = np.mgrid[0:h, 0:w]
    dist = np.minimum(np.minimum(xs, w - 1 - xs), np.minimum(ys, h - 1 - ys)).astype(np.float32)
    band = max(8.0, 0.08 * min(h, w))
    return np.clip(dist / band, 0.0, 1.0)


def key_background(rgba: np.ndarray) -> np.ndarray:
    out = rgba.copy()
    rgb = out[..., :3].astype(np.float32) / 255.0
    a = out[..., 3].astype(np.float32) / 255.0
    h, w = a.shape
    lum = luminance(rgb)
    sat = saturation(rgb)
    edge = edge_distance_norm(h, w)

    all_high = (rgb[..., 0] > 220 / 255.0) & (rgb[..., 1] > 220 / 255.0) & (rgb[..., 2] > 220 / 255.0)
    bright_desat = (lum > 0.88) & (sat < 0.18)
    near_edge_bright = (edge < 0.55) & (lum > 0.82) & (sat < 0.25)
    near_edge_very_bright = (edge < 0.85) & (lum > 0.92) & (sat < 0.22)
    bg = all_high | bright_desat | near_edge_bright | near_edge_very_bright

    strength = np.zeros_like(a)
    strength = np.where(all_high | bright_desat, 1.0, strength)
    strength = np.where(near_edge_very_bright & ~bg, np.maximum(strength, 0.85), strength)
    bg_score = np.clip((lum - 0.75) / 0.20, 0.0, 1.0) * np.clip(1.0 - sat / 0.30, 0.0, 1.0)
    bg_score = np.where(edge < 0.7, np.maximum(bg_score, bg_score * (1.0 - edge)), bg_score)
    strength = np.where(bg, np.maximum(strength, np.clip(bg_score, 0.55, 1.0)), strength)
    strength = np.where(bg & all_high, 1.0, strength)

    a = a * (1.0 - strength)
    a = np.where(bg & (lum > 0.90) & (sat < 0.15), 0.0, a)
    out[..., 3] = (np.clip(a, 0, 1) * 255.0).astype(np.uint8)
    return out


def flood_key_background(rgba: np.ndarray) -> np.ndarray:
    """Key near-white bg via flood fill from borders — protects light emblem cores."""
    out = rgba.copy()
    rgb = out[..., :3].astype(np.float32) / 255.0
    a = out[..., 3].astype(np.float32) / 255.0
    h, w = a.shape
    lum = luminance(rgb)
    sat = saturation(rgb)
    edge = edge_distance_norm(h, w)

    cand = (
        ((rgb[..., 0] > 220 / 255.0) & (rgb[..., 1] > 220 / 255.0) & (rgb[..., 2] > 220 / 255.0))
        | ((lum > 0.88) & (sat < 0.18))
        | ((lum > 0.85) & (sat < 0.22) & (edge < 0.9))
        | ((lum > 0.82) & (sat < 0.25) & (edge < 0.55))
    )

    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if cand[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if cand[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((y, x))

    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and cand[ny, nx]:
                visited[ny, nx] = True
                q.append((ny, nx))

    bg = visited
    strength = np.zeros_like(a)
    bg_score = np.clip((lum - 0.75) / 0.20, 0.0, 1.0) * np.clip(1.0 - sat / 0.30, 0.0, 1.0)
    strength = np.where(bg, np.maximum(strength, np.clip(bg_score, 0.55, 1.0)), strength)
    all_high = (rgb[..., 0] > 220 / 255.0) & (rgb[..., 1] > 220 / 255.0) & (rgb[..., 2] > 220 / 255.0)
    strength = np.where(bg & all_high, 1.0, strength)
    strength = np.where(bg & (lum > 0.90) & (sat < 0.15), 1.0, strength)

    a = a * (1.0 - strength)
    a = np.where(bg & (lum > 0.90) & (sat < 0.15), 0.0, a)
    out[..., 3] = (np.clip(a, 0, 1) * 255.0).astype(np.uint8)
    return out


def soften_fringe(rgba: np.ndarray, radius: int = 3) -> np.ndarray:
    img = Image.fromarray(rgba, "RGBA")
    alpha = img.split()[3]
    blurred = alpha.filter(ImageFilter.GaussianBlur(radius=radius))
    a0 = np.array(alpha, dtype=np.float32)
    a1 = np.array(blurred, dtype=np.float32)
    a_soft = np.minimum(a0, a1)
    arr = np.array(img, dtype=np.float32)
    rgb = arr[..., :3] / 255.0
    lum = luminance(rgb)
    sat = saturation(rgb)
    fringe = (a_soft > 8) & (a_soft < 240) & (lum > 0.78) & (sat < 0.28)
    a_soft = np.where(fringe, a_soft * 0.35, a_soft)
    for c in range(3):
        arr[..., c] = np.where(fringe, arr[..., c] * 0.85, arr[..., c])
    arr[..., 3] = a_soft
    return np.clip(arr, 0, 255).astype(np.uint8)


def punch_soft_hole(rgba: np.ndarray, radius_frac: float) -> np.ndarray:
    out = rgba.copy().astype(np.float32)
    h, w = out.shape[:2]
    size = min(h, w)
    radius = size * radius_frac
    cy, cx = (h - 1) / 2.0, (w - 1) / 2.0
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    dist = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2)
    feather = max(4.0, size * 0.04)
    inner = radius - feather * 0.5
    outer = radius + feather * 0.5
    hole = np.ones((h, w), dtype=np.float32)
    hole = np.where(dist <= inner, 0.0, hole)
    mid = (dist > inner) & (dist < outer)
    hole = np.where(mid, (dist - inner) / max(outer - inner, 1e-6), hole)
    out[..., 3] *= hole
    return np.clip(out, 0, 255).astype(np.uint8)


def write_meta(dst_png: Path, template_text: str) -> str:
    guid = secrets.token_hex(16)
    lines = template_text.splitlines(keepends=True)
    new_lines = []
    for line in lines:
        if line.startswith("guid:"):
            new_lines.append(f"guid: {guid}\n")
        else:
            new_lines.append(line)
    meta_path = Path(str(dst_png) + ".meta")
    meta_path.write_text("".join(new_lines), encoding="utf-8")
    text = meta_path.read_text(encoding="utf-8")
    assert "alphaIsTransparency: 1" in text
    assert "nPOTScale: 0" in text
    assert "enableMipMap: 0" in text
    return guid


def center_alpha_stats(a: np.ndarray, frac: float = 0.12) -> dict:
    h, w = a.shape
    cy, cx = h // 2, w // 2
    r = max(2, int(min(h, w) * frac))
    patch = a[cy - r : cy + r + 1, cx - r : cx + r + 1]
    return {
        "center_alpha_min": int(patch.min()),
        "center_alpha_max": int(patch.max()),
        "center_alpha_mean": float(patch.mean()),
    }


def process_one(src_name: str, dst_name: str, kind: str, target: int, template: str) -> dict:
    src = SRC / src_name
    dst = DST / dst_name
    img = Image.open(src).convert("RGBA")
    arr = np.array(img)
    if kind == "crest":
        arr = flood_key_background(arr)
    else:
        arr = key_background(arr)
    arr = soften_fringe(arr, radius=3)
    if kind == "accent":
        arr = punch_soft_hole(arr, 0.34)

    out = Image.fromarray(arr, "RGBA")
    if out.size != (target, target):
        out = out.resize((target, target), Image.Resampling.LANCZOS)

    DST.mkdir(parents=True, exist_ok=True)
    out.save(dst, "PNG")
    guid = write_meta(dst, template)

    a = np.array(out)[..., 3]
    stats = center_alpha_stats(a)
    return {
        "file": str(dst),
        "name": dst.name,
        "size": out.size,
        "bytes": dst.stat().st_size,
        "alpha_min": int(a.min()),
        "alpha_max": int(a.max()),
        "guid": guid,
        "kind": kind,
        **stats,
    }


def main() -> None:
    template = META_TEMPLATE.read_text(encoding="utf-8")
    for job in JOBS:
        r = process_one(*job, template=template)
        print(
            f"{r['name']}: {r['size'][0]}x{r['size'][1]}  "
            f"{r['bytes']} bytes  alpha[{r['alpha_min']},{r['alpha_max']}]  "
            f"center_alpha mean={r['center_alpha_mean']:.1f} "
            f"[{r['center_alpha_min']},{r['center_alpha_max']}]  "
            f"kind={r['kind']}  guid={r['guid']}"
        )
        if r["kind"] == "crest":
            ok = r["center_alpha_mean"] > 180
            print(f"  -> crest center opaque check: {'PASS' if ok else 'FAIL'} (mean={r['center_alpha_mean']:.1f})")
        else:
            ok = r["center_alpha_mean"] < 20
            print(f"  -> accent center transparent check: {'PASS' if ok else 'FAIL'} (mean={r['center_alpha_mean']:.1f})")
    print("\nDone.")


if __name__ == "__main__":
    main()
