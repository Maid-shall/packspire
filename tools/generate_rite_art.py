"""Generate product-quality packing-rite art: magic circle, cell plate, element orbs."""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1] / "unity" / "PackspireUnity" / "Assets" / "Resources" / "Art" / "Rite"
ROOT.mkdir(parents=True, exist_ok=True)


def save(img: Image.Image, name: str) -> None:
    path = ROOT / name
    img.save(path, "PNG")
    print(f"wrote {path}")


def soft_disk(size: int, radius: float, softness: float = 2.5) -> np.ndarray:
    cy = cx = (size - 1) / 2.0
    y, x = np.ogrid[:size, :size]
    d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2)
    return np.clip((radius - d) / max(softness, 0.01) + 1.0, 0.0, 1.0)


def draw_ring(draw: ImageDraw.ImageDraw, box, width: int, fill, steps: int = 2) -> None:
    for i in range(steps):
        inset = i * 0.5
        b = (box[0] + inset, box[1] + inset, box[2] - inset, box[3] - inset)
        draw.ellipse(b, outline=fill, width=max(1, width - i))


def make_magic_circle(size: int = 1024) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size // 2
    gold = (255, 214, 140, 230)
    gold_dim = (210, 160, 90, 160)
    cream = (255, 240, 200, 200)
    violet = (160, 120, 220, 90)

    # soft outer glow plate
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((40, 40, size - 40, size - 40), fill=(40, 20, 60, 70))
    glow = glow.filter(ImageFilter.GaussianBlur(28))
    img = Image.alpha_composite(img, glow)
    draw = ImageDraw.Draw(img)

    # concentric ornate rings
    rings = [
        (18, 5, gold),
        (48, 2, gold_dim),
        (78, 3, cream),
        (118, 2, gold_dim),
        (160, 4, gold),
        (210, 2, cream),
        (250, 3, gold_dim),
    ]
    for inset, width, color in rings:
        box = (inset, inset, size - inset, size - inset)
        draw_ring(draw, box, width, color, steps=2)

    # outer tick marks / runes
    for i in range(48):
        ang = math.radians(i * (360 / 48))
        long = i % 4 == 0
        r0 = size * 0.47
        r1 = size * (0.435 if long else 0.45)
        x0, y0 = c + math.cos(ang) * r0, c + math.sin(ang) * r0
        x1, y1 = c + math.cos(ang) * r1, c + math.sin(ang) * r1
        draw.line((x0, y0, x1, y1), fill=gold if long else gold_dim, width=3 if long else 2)

    # mid diamond / star points
    for i in range(8):
        ang = math.radians(i * 45 - 90)
        r = size * 0.33
        x = c + math.cos(ang) * r
        y = c + math.sin(ang) * r
        s = 10 if i % 2 == 0 else 7
        draw.polygon(
            [(x, y - s), (x + s * 0.7, y), (x, y + s), (x - s * 0.7, y)],
            fill=cream if i % 2 == 0 else gold_dim,
        )

    # inner hexagon
    hex_r = size * 0.22
    hex_pts = [
        (c + math.cos(math.radians(a)) * hex_r, c + math.sin(math.radians(a)) * hex_r)
        for a in range(0, 360, 60)
    ]
    draw.polygon(hex_pts, outline=gold)
    draw.line(hex_pts + [hex_pts[0]], fill=gold, width=2)

    # crossed arcs
    for inset in (280, 300):
        box = (inset, inset, size - inset, size - inset)
        draw.arc(box, 20, 160, fill=violet, width=2)
        draw.arc(box, 200, 340, fill=violet, width=2)

    # cut open center so grid reads clearly
    clear = Image.new("L", (size, size), 0)
    cd = ImageDraw.Draw(clear)
    hole = size * 0.18
    cd.ellipse((c - hole, c - hole, c + hole, c + hole), fill=255)
    clear = clear.filter(ImageFilter.GaussianBlur(10))
    arr = np.array(img)
    alpha = arr[:, :, 3].astype(np.float32)
    mask = np.array(clear, dtype=np.float32) / 255.0
    arr[:, :, 3] = np.clip(alpha * (1.0 - mask * 0.92), 0, 255).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def make_spin_ring(size: int = 1024) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size // 2
    for i in range(72):
        ang = math.radians(i * 5)
        long = i % 6 == 0
        r0 = size * 0.455
        r1 = size * (0.41 if long else 0.438)
        x0, y0 = c + math.cos(ang) * r0, c + math.sin(ang) * r0
        x1, y1 = c + math.cos(ang) * r1, c + math.sin(ang) * r1
        col = (255, 230, 170, 220) if long else (220, 180, 110, 140)
        draw.line((x0, y0, x1, y1), fill=col, width=3 if long else 2)
    for i in range(12):
        ang = math.radians(i * 30)
        r = size * 0.39
        x = c + math.cos(ang) * r
        y = c + math.sin(ang) * r
        draw.ellipse((x - 4, y - 4, x + 4, y + 4), fill=(255, 220, 140, 200))
    # keep only ring band
    arr = np.array(img)
    cy = cx = (size - 1) / 2.0
    yy, xx = np.ogrid[:size, :size]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    band = ((d > size * 0.36) & (d < size * 0.48)).astype(np.float32)
    arr[:, :, 3] = (arr[:, :, 3].astype(np.float32) * band).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def make_cell_plate(size: int = 256) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = 10
    # recessed stone/obsidian plate
    draw.rounded_rectangle(
        (margin, margin, size - margin, size - margin),
        radius=22,
        fill=(14, 12, 18, 230),
        outline=(90, 78, 110, 180),
        width=2,
    )
    # inner bevel highlight
    draw.rounded_rectangle(
        (margin + 6, margin + 6, size - margin - 6, size - margin - 6),
        radius=16,
        outline=(180, 150, 90, 70),
        width=1,
    )
    # subtle top light
    light = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ld = ImageDraw.Draw(light)
    ld.rounded_rectangle(
        (margin + 4, margin + 4, size - margin - 4, size // 2),
        radius=14,
        fill=(255, 230, 180, 28),
    )
    light = light.filter(ImageFilter.GaussianBlur(6))
    img = Image.alpha_composite(img, light)
    # center socket for orb
    sock = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sock)
    r = size * 0.28
    cx = cy = size / 2
    sd.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(6, 4, 10, 160), outline=(120, 100, 70, 90), width=2)
    sock = sock.filter(ImageFilter.GaussianBlur(1))
    return Image.alpha_composite(img, sock)


def make_orb(rgb: tuple[int, int, int], size: int = 128, name_hint: str = "") -> Image.Image:
    """Glass-like elemental orb with specular highlight and soft glow."""
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    # outer glow
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    pad = 6
    gd.ellipse((pad, pad, size - pad, size - pad), fill=(*rgb, 70))
    glow = glow.filter(ImageFilter.GaussianBlur(10))
    canvas = Image.alpha_composite(canvas, glow)

    body = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    m = 18
    # base sphere
    bd.ellipse((m, m, size - m, size - m), fill=(*rgb, 235))
    # darker lower gradient via second ellipse
    shade = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    sd.ellipse((m + 4, size * 0.42, size - m - 4, size - m - 2), fill=(0, 0, 0, 90))
    shade = shade.filter(ImageFilter.GaussianBlur(6))
    body = Image.alpha_composite(body, shade)
    # color bright core
    core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    cd = ImageDraw.Draw(core)
    cr = size * 0.22
    cx = size * 0.48
    cy = size * 0.46
    bright = tuple(min(255, int(c * 1.25 + 40)) for c in rgb)
    cd.ellipse((cx - cr, cy - cr, cx + cr, cy + cr), fill=(*bright, 120))
    core = core.filter(ImageFilter.GaussianBlur(5))
    body = Image.alpha_composite(body, core)
    # specular
    spec = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sp = ImageDraw.Draw(spec)
    sp.ellipse((size * 0.30, size * 0.26, size * 0.52, size * 0.44), fill=(255, 255, 255, 200))
    sp.ellipse((size * 0.55, size * 0.52, size * 0.68, size * 0.62), fill=(255, 255, 255, 70))
    spec = spec.filter(ImageFilter.GaussianBlur(2))
    body = Image.alpha_composite(body, spec)
    # rim
    rim = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    rd = ImageDraw.Draw(rim)
    rd.ellipse((m, m, size - m, size - m), outline=(255, 245, 220, 90), width=2)
    body = Image.alpha_composite(body, rim)
    # circular alpha mask
    mask = soft_disk(size, size * 0.42, 3.0)
    arr = np.array(body)
    arr[:, :, 3] = (arr[:, :, 3].astype(np.float32) * mask).astype(np.uint8)
    body = Image.fromarray(arr, "RGBA")
    return Image.alpha_composite(canvas, body)


def make_panel_sheen(size: tuple[int, int] = (512, 128)) -> Image.Image:
    """Subtle dark panel texture for HUD rectangles (not decorative cards)."""
    w, h = size
    img = Image.new("RGBA", (w, h), (16, 12, 22, 210))
    draw = ImageDraw.Draw(img)
    draw.rectangle((0, 0, w - 1, h - 1), outline=(180, 150, 90, 90), width=2)
    draw.rectangle((3, 3, w - 4, h - 4), outline=(255, 230, 180, 28), width=1)
    # faint top sheen
    sheen = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sheen)
    sd.rectangle((4, 4, w - 5, h // 3), fill=(255, 230, 190, 18))
    sheen = sheen.filter(ImageFilter.GaussianBlur(4))
    return Image.alpha_composite(img, sheen)


def main() -> None:
    save(make_magic_circle(1024), "rite-circle-base-v1.png")
    save(make_spin_ring(1024), "rite-circle-spin-v1.png")
    save(make_cell_plate(256), "rite-cell-plate-v1.png")
    save(make_orb((255, 96, 64), name_hint="fire"), "orb-fire-v1.png")
    save(make_orb((72, 150, 255), name_hint="water"), "orb-water-v1.png")
    save(make_orb((96, 210, 130), name_hint="wind"), "orb-wind-v1.png")
    save(make_orb((220, 175, 70), name_hint="earth"), "orb-earth-v1.png")
    save(make_panel_sheen((640, 160)), "rite-panel-v1.png")
    print("done")


if __name__ == "__main__":
    main()
