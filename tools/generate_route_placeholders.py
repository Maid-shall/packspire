"""Generate simple readable placeholder silhouettes for route exits (no chroma green)."""
from PIL import Image, ImageDraw
import os

out = os.path.join("unity", "PackspireUnity", "Assets", "Resources", "Art", "RouteKeyed", "placeholders")
os.makedirs(out, exist_ok=True)

def save(name, im):
    path = os.path.join(out, name)
    im.save(path)
    print("OK", path)

def road():
    im = Image.new("RGBA", (128, 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(40, 20), (88, 20), (110, 150), (18, 150)], fill=(120, 105, 80, 210))
    d.line([(64, 30), (64, 140)], fill=(200, 180, 120, 160), width=3)
    for y in range(40, 140, 18):
        d.ellipse([58, y, 70, y + 6], fill=(230, 210, 140, 90))
    save("exit-road.png", im)

def gate():
    im = Image.new("RGBA", (140, 180), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([20, 30, 120, 170], fill=(90, 78, 62, 230), outline=(180, 150, 90, 255), width=3)
    d.pieslice([20, 0, 120, 80], 180, 0, fill=(90, 78, 62, 230))
    d.rectangle([48, 70, 92, 170], fill=(40, 32, 28, 240))
    d.ellipse([78, 110, 88, 120], fill=(220, 190, 100, 255))
    save("exit-gate.png", im)

def door():
    im = Image.new("RGBA", (100, 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([15, 10, 85, 150], radius=8, fill=(110, 72, 48, 235), outline=(190, 150, 90, 255), width=3)
    d.ellipse([62, 75, 74, 87], fill=(210, 180, 90, 255))
    save("exit-door.png", im)

def rubble():
    im = Image.new("RGBA", (140, 120), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(10, 90), (40, 30), (70, 70), (100, 25), (130, 95), (20, 110)], fill=(95, 85, 75, 230))
    d.polygon([(30, 100), (55, 55), (80, 100)], fill=(70, 62, 55, 220))
    for i in range(8):
        d.ellipse([20 + i * 12, 20 + (i % 3) * 8, 28 + i * 12, 28 + (i % 3) * 8], fill=(160, 140, 110, 100))
    save("exit-rubble.png", im)

def crack():
    im = Image.new("RGBA", (90, 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.line([(45, 10), (40, 50), (52, 90), (38, 130), (48, 155)], fill=(180, 170, 150, 200), width=4)
    d.line([(40, 50), (28, 70)], fill=(150, 140, 120, 160), width=2)
    d.line([(52, 90), (65, 110)], fill=(150, 140, 120, 160), width=2)
    save("exit-crack.png", im)

def lantern():
    im = Image.new("RGBA", (48, 72), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([18, 8, 30, 18], fill=(80, 70, 50, 230))
    d.ellipse([10, 18, 38, 55], fill=(255, 200, 90, 180))
    d.ellipse([16, 24, 32, 48], fill=(255, 230, 140, 220))
    save("exit-lantern.png", im)

def glow():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    px = im.load()
    cx, cy = 47.5, 47.5
    for y in range(96):
        for x in range(96):
            dist = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 / 48.0
            if dist < 1:
                a = int((1 - dist) ** 1.6 * 140)
                px[x, y] = (255, 220, 140, a)
    save("exit-glow.png", im)

def building():
    im = Image.new("RGBA", (160, 180), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(20, 70), (80, 20), (140, 70)], fill=(70, 65, 60, 230))
    d.rectangle([30, 70, 130, 170], fill=(85, 78, 70, 230), outline=(160, 130, 80, 255), width=2)
    d.rectangle([65, 100, 95, 170], fill=(35, 30, 28, 240))
    save("exit-building.png", im)

def grass():
    im = Image.new("RGBA", (120, 60), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for i in range(12):
        x = 8 + i * 9
        d.line([(x, 55), (x - 4, 15)], fill=(70, 110, 55, 180), width=2)
        d.line([(x, 55), (x + 5, 20)], fill=(90, 130, 60, 160), width=2)
    save("exit-grass.png", im)

for fn in (road, gate, door, rubble, crack, lantern, glow, building, grass):
    fn()
print("done")
