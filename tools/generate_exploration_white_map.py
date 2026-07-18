"""Generate a clearer schematic white map for Old Spire exploration prototype."""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1] / "unity" / "PackspireUnity" / "Assets" / "Resources" / "Art" / "Map"
ROOT.mkdir(parents=True, exist_ok=True)

W, H = 2048, 1152

NODES = {
    0: (0.12, 0.55, "入口"),
    1: (0.28, 0.48, "分岐"),
    2: (0.45, 0.42, "石橋"),
    3: (0.45, 0.68, "崖"),
    4: (0.62, 0.35, "鍛造所"),
    5: (0.68, 0.58, "兵舎"),
    6: (0.78, 0.45, "中庭"),
    7: (0.88, 0.30, "行き止まり"),
    8: (0.55, 0.72, "祠"),
    9: (0.35, 0.28, "監視塔"),
    10: (0.88, 0.52, "中庭影"),
}

EDGES = [
    (0, 1), (1, 2), (1, 9), (1, 3), (2, 4), (2, 5), (3, 8), (4, 6), (5, 6), (6, 7), (6, 10),
]


def px(n: int) -> tuple[float, float]:
    x, y, _ = NODES[n]
    return x * W, y * H


def draw_road(draw: ImageDraw.ImageDraw, a: int, b: int) -> None:
    p0, p1 = px(a), px(b)
    draw.line([p0, p1], fill=(196, 178, 140), width=42)
    draw.line([p0, p1], fill=(112, 96, 72), width=16)
    draw.line([p0, p1], fill=(168, 148, 110), width=6)


def main() -> None:
    img = Image.new("RGB", (W, H), (214, 204, 182))
    draw = ImageDraw.Draw(img)

    # parchment noise bands
    for y in range(0, H, 28):
        shade = 208 + (y // 28) % 3 * 4
        draw.line([(0, y), (W, y)], fill=(shade, shade - 8, shade - 24), width=1)

    # terrain washes
    draw.ellipse((80, 120, 720, 980), fill=(198, 188, 162))
    draw.ellipse((980, 80, 1900, 700), fill=(190, 178, 152))
    draw.ellipse((1100, 620, 1980, 1080), fill=(186, 172, 148))

    # cliff hatch zone
    cliff = (0.36 * W, 0.58 * H, 0.54 * W, 0.88 * H)
    draw.rectangle(cliff, fill=(170, 158, 136), outline=(96, 84, 66), width=4)
    for i in range(14):
        x = cliff[0] + 18 + i * 24
        draw.line([(x, cliff[1] + 12), (x - 26, cliff[3] - 12)], fill=(130, 118, 96), width=3)

    # building footprints with roofs
    buildings = [
        (0.54, 0.16, 0.74, 0.42, "鍛造所"),
        (0.60, 0.48, 0.80, 0.72, "兵舎"),
        (0.48, 0.62, 0.64, 0.84, "祠"),
        (0.28, 0.12, 0.44, 0.34, "監視塔"),
    ]
    for x0, y0, x1, y1, _label in buildings:
        box = [x0 * W, y0 * H, x1 * W, y1 * H]
        draw.rectangle(box, fill=(156, 140, 118), outline=(72, 60, 46), width=6)
        inset = [box[0] + 14, box[1] + 14, box[2] - 14, box[3] - 14]
        draw.rectangle(inset, outline=(210, 190, 150), width=3)
        # simple roof ridge
        mx = (box[0] + box[2]) * 0.5
        draw.line([(box[0], box[1] + 24), (mx, box[1] - 18), (box[2], box[1] + 24)], fill=(92, 74, 54), width=5)

    for a, b in EDGES:
        draw_road(draw, a, b)

    # bridge board
    bx, by = px(2)
    draw.rectangle((bx - 70, by - 26, bx + 70, by + 26), fill=(120, 100, 74), outline=(60, 48, 34), width=4)
    for i in range(-2, 3):
        draw.line([(bx - 58, by + i * 8), (bx + 58, by + i * 8)], fill=(168, 146, 108), width=3)

    # node pads
    for nid, (nx, ny, label) in NODES.items():
        x, y = nx * W, ny * H
        draw.ellipse((x - 28, y - 28, x + 28, y + 28), fill=(236, 224, 196), outline=(90, 74, 52), width=4)
        draw.ellipse((x - 10, y - 10, x + 10, y + 10), fill=(120, 96, 64))

    # frame
    draw.rectangle((18, 18, W - 18, H - 18), outline=(64, 52, 38), width=10)
    draw.rectangle((36, 36, W - 36, H - 36), outline=(168, 148, 110), width=3)

    # title plate
    draw.rectangle((56, 52, 520, 128), fill=(48, 40, 30), outline=(210, 180, 120), width=3)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/meiryo.ttc", 36)
        small = ImageFont.truetype("C:/Windows/Fonts/meiryo.ttc", 22)
    except OSError:
        font = ImageFont.load_default()
        small = font
    draw.text((76, 66), "古塔外郭・白地図", fill=(245, 228, 186), font=font)
    draw.text((76, 104), "道と建物輪郭は背景焼き込み / 地点はゲーム側", fill=(190, 170, 130), font=small)

    img = img.filter(ImageFilter.SMOOTH_MORE)
    out = ROOT / "old-spire-white-v1.png"
    img.save(out, "PNG")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
