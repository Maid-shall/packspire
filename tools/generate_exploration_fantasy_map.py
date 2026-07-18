"""Composite fantasy base art with node-aligned roads for Old Spire exploration."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1] / "unity" / "PackspireUnity" / "Assets" / "Resources" / "Art" / "Map"
ROOT.mkdir(parents=True, exist_ok=True)
TOOLS = Path(__file__).resolve().parent
W, H = 2048, 1152

# Keep in sync with ExplorationMapCatalog.OldSpireWhite
NODES = {
    0: (0.12, 0.55),
    1: (0.28, 0.48),
    2: (0.45, 0.42),
    3: (0.45, 0.68),
    4: (0.62, 0.35),
    5: (0.68, 0.58),
    6: (0.78, 0.45),
    7: (0.88, 0.30),
    8: (0.55, 0.72),
    9: (0.35, 0.28),
    10: (0.88, 0.52),  # courtyard ambush / side path
}

EDGES = [
    (0, 1),
    (1, 2),
    (1, 9),
    (1, 3),
    (2, 4),
    (2, 5),
    (3, 8),
    (4, 6),
    (5, 6),
    (6, 7),
    (6, 10),
]

BUILDINGS = [
    (0.54, 0.16, 0.74, 0.42),  # forge
    (0.60, 0.48, 0.80, 0.72),  # barracks
    (0.48, 0.62, 0.64, 0.84),  # shrine
    (0.28, 0.12, 0.44, 0.34),  # watchtower
]


def px(n: int) -> tuple[float, float]:
    x, y = NODES[n]
    return x * W, y * H


def load_base() -> Image.Image:
    candidates = [
        TOOLS / "_old-spire-fantasy-base.png",
        Path(r"C:\Users\p9-ti\.cursor\projects\c-maid-apps-Pick-Spire\assets\old-spire-fantasy-base.png"),
        ROOT / "old-spire-fantasy-base.png",
    ]
    for path in candidates:
        if path.is_file():
            img = Image.open(path).convert("RGB")
            return ImageOps.fit(img, (W, H), method=Image.Resampling.LANCZOS)
    # Fallback parchment if base missing
    img = Image.new("RGB", (W, H), (186, 168, 138))
    draw = ImageDraw.Draw(img)
    for y in range(0, H, 24):
        shade = 170 + (y // 24) % 4 * 6
        draw.line([(0, y), (W, y)], fill=(shade, shade - 14, shade - 34), width=1)
    return img


def draw_road(draw: ImageDraw.ImageDraw, a: int, b: int) -> None:
    p0, p1 = px(a), px(b)
    draw.line([p0, p1], fill=(48, 36, 24, 90), width=52)
    draw.line([p0, p1], fill=(142, 118, 82, 210), width=34)
    draw.line([p0, p1], fill=(188, 158, 110, 230), width=16)
    draw.line([p0, p1], fill=(214, 188, 138, 180), width=5)


def main() -> None:
    base = load_base()
    base = ImageEnhance.Color(base).enhance(0.92)
    base = ImageEnhance.Contrast(base).enhance(1.08)
    base = base.filter(ImageFilter.SMOOTH_MORE)

    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    # Soft vignette / parchment wash
    wash = Image.new("RGBA", (W, H), (210, 188, 148, 48))
    base = Image.alpha_composite(base.convert("RGBA"), wash)

    # Cliff hatch (no labels)
    cliff = (0.36 * W, 0.58 * H, 0.54 * W, 0.88 * H)
    draw.rectangle(cliff, fill=(90, 72, 52, 70), outline=(60, 46, 32, 160), width=4)
    for i in range(16):
        x = cliff[0] + 14 + i * 22
        draw.line([(x, cliff[1] + 10), (x - 30, cliff[3] - 10)], fill=(70, 56, 40, 110), width=3)

    # Building silhouettes aligned to doors
    for x0, y0, x1, y1 in BUILDINGS:
        box = [x0 * W, y0 * H, x1 * W, y1 * H]
        draw.rectangle(box, fill=(62, 48, 36, 120), outline=(36, 26, 18, 200), width=5)
        mx = (box[0] + box[2]) * 0.5
        draw.polygon(
            [(box[0] - 6, box[1] + 18), (mx, box[1] - 28), (box[2] + 6, box[1] + 18)],
            fill=(78, 52, 34, 170),
        )

    for a, b in EDGES:
        draw_road(draw, a, b)

    # Bridge plank at node 2
    bx, by = px(2)
    draw.rectangle((bx - 64, by - 22, bx + 64, by + 22), fill=(92, 70, 46, 190), outline=(40, 28, 18, 220), width=3)

    # Subtle node pads (no text)
    for nid in NODES:
        x, y = px(nid)
        draw.ellipse((x - 22, y - 22, x + 22, y + 22), fill=(236, 214, 170, 70), outline=(90, 68, 42, 140), width=3)

    # Frame
    draw.rectangle((14, 14, W - 14, H - 14), outline=(42, 30, 18, 220), width=10)
    draw.rectangle((30, 30, W - 30, H - 30), outline=(180, 148, 96, 120), width=3)

    composed = Image.alpha_composite(base.convert("RGBA"), overlay).convert("RGB")
    composed = composed.filter(ImageFilter.SMOOTH)

    out = ROOT / "old-spire-fantasy-v1.png"
    composed.save(out, "PNG")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
