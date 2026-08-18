"""Generate opaque PopDark portrait cutouts from hub scene art."""

from __future__ import annotations

import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "unity/PackspireUnity/Assets/Resources/Art/Portraits/PopDark/hero-courier-hub-v1.png"
OUT = ROOT / "unity/PackspireUnity/Assets/Resources/Art/Portraits/PopDark/hero-courier-cutout-v1.png"


def build_mask(bgr: np.ndarray) -> tuple[np.ndarray, tuple[int, int, int, int]]:
    h, w = bgr.shape[:2]
    # Wide crop so outstretched arms and feet stay inside the source region.
    x0, y0 = int(w * 0.11), int(h * 0.05)
    x1, y1 = int(w * 0.89), int(h * 0.998)
    crop = bgr[y0:y1, x0:x1]
    ch, cw = crop.shape[:2]

    rect = (int(cw * 0.02), int(ch * 0.01), int(cw * 0.96), int(ch * 0.98))
    mask = np.zeros((ch, cw), np.uint8)
    bgd = np.zeros((1, 65), np.float64)
    fgd = np.zeros((1, 65), np.float64)
    cv2.grabCut(crop, mask, rect, bgd, fgd, 7, cv2.GC_INIT_WITH_RECT)

    fg = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)

    hsv = cv2.cvtColor(crop, cv2.COLOR_BGR2HSV)
    sat = hsv[:, :, 1]
    val = hsv[:, :, 2]
    xs = np.arange(cw, dtype=np.float32)[None, :]
    ys = np.arange(ch, dtype=np.float32)[:, None]
    center_x = np.clip(1.0 - np.abs(xs - cw * 0.5) / (cw * 0.40), 0.0, 1.0)
    center_y = np.clip(1.0 - np.abs(ys - ch * 0.56) / (ch * 0.56), 0.0, 1.0)
    bg_like = (sat < 40) & (val < 112)
    fg[bg_like & (center_x < 0.50) & (center_y < 0.42)] = 0

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, kernel, iterations=2)
    fg = cv2.morphologyEx(fg, cv2.MORPH_OPEN, kernel, iterations=1)
    fg = cv2.dilate(fg, kernel, iterations=1)

    alpha = np.where(fg > 0, 255, 0).astype(np.uint8)
    n, labels, stats, _ = cv2.connectedComponentsWithStats(alpha // 255, connectivity=8)
    if n > 1:
        largest = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
        alpha = np.where(labels == largest, 255, 0).astype(np.uint8)

    return alpha, (x0, y0, x1, y1)


def main() -> int:
    if not SRC.exists():
        print("missing source:", SRC, file=sys.stderr)
        return 1

    bgr = cv2.imread(str(SRC), cv2.IMREAD_COLOR)
    if bgr is None:
        print("failed to read:", SRC, file=sys.stderr)
        return 1

    alpha, (x0, y0, x1, y1) = build_mask(bgr)
    crop_bgr = bgr[y0:y1, x0:x1]
    rgba = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGBA)
    rgba[:, :, 3] = alpha

    ys, xs = np.where(alpha > 0)
    if len(xs) == 0 or len(ys) == 0:
        print("empty mask", file=sys.stderr)
        return 1

    pad = 28
    minx = max(0, int(xs.min()) - pad)
    maxx = min(alpha.shape[1] - 1, int(xs.max()) + pad)
    miny = max(0, int(ys.min()) - pad)
    maxy = min(alpha.shape[0] - 1, int(ys.max()) + pad)
    rgba = rgba[miny : maxy + 1, minx : maxx + 1]

    out = Image.fromarray(rgba, mode="RGBA")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT)
    print("saved", OUT, "size", out.size)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
