from PIL import Image
import os

src_root = os.path.join("unity", "PackspireUnity", "Assets", "Resources", "Art")
out_root = os.path.join("unity", "PackspireUnity", "Assets", "Resources", "Art", "RouteKeyed")
os.makedirs(out_root, exist_ok=True)

files = [
    ("Hub/hub-gate-v1.png", True, False),
    ("Hub/hub-forge-v1.png", True, False),
    ("Hub/hub-guild-close-v2.png", True, False),
    ("Hub/hub-street-floor-v1.png", True, False),
    ("Hub/hub-vista-mid-v1.png", True, False),
    ("Prototype2_5D/character-v1.png", False, True),
    ("Prototype2_5D/foreground-v1.png", False, True),
    ("Prototype2_5D/far-background-v1.png", False, True),
    ("Prototype2_5D/midground-v1.png", False, True),
]

for rel, key_g, key_b in files:
    path = os.path.join(src_root, *rel.split("/"))
    if not os.path.isfile(path):
        print("MISSING", path)
        continue
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if key_b and r < 18 and g < 18 and b < 18:
                px[x, y] = (0, 0, 0, 0)
                continue
            if key_g:
                competing = max(r, b)
                dominance = g - competing
                if dominance > 3 and g > 45:
                    t = min(1.0, max(0.0, (dominance - 3) / 22.0) * max(0.0, min(1.0, (g - 45) / 80.0)))
                    spill = max(0.0, (g - competing) * 0.9 / 255.0)
                    kill = max(t, spill * 0.85)
                    na = int(a * (1.0 - min(1.0, kill)))
                    ng = min(g, int(competing * 1.04 + 5))
                    if kill > 0.92:
                        px[x, y] = (0, 0, 0, 0)
                    else:
                        px[x, y] = (r, ng, b, na)
    name = os.path.basename(rel)
    out = os.path.join(out_root, name)
    im.save(out)
    residual = 0
    opaque = 0
    px = im.load()
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            r, g, b, a = px[x, y]
            if a < 50:
                continue
            opaque += 1
            if g > r + 30 and g > b + 30 and g > 90:
                residual += 1
    ratio = residual / max(1, opaque)
    print(f"OK {name} residual_green={ratio:.3f} opaque_samples={opaque}")

print("done")
