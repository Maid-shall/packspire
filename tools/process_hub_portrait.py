"""Cut out hub/roster portraits: remove black matte, add dark ink outline."""
from __future__ import annotations

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
PORTRAITS = [
 ROOT / "unity/PackspireUnity/Assets/Resources/Art/Portraits/DD/hero-sena-hub-v1.png",
 ROOT / "unity/PackspireUnity/Assets/Resources/Art/Portraits/DD/hero-sena-front-v1.png",
]


def flood_dark_background(alpha: np.ndarray, rgb: np.ndarray, threshold: int = 34) -> np.ndarray:
 h, w = alpha.shape
 dark = rgb.max(axis=2) < threshold
 bg = np.zeros((h, w), dtype=bool)
 q: deque[tuple[int, int]] = deque()
 for x in range(w):
  for y in (0, h - 1):
   if dark[y, x]:
    bg[y, x] = True
    q.append((y, x))
 for y in range(h):
  for x in (0, w - 1):
   if dark[y, x] and not bg[y, x]:
    bg[y, x] = True
    q.append((y, x))
 while q:
  y, x = q.popleft()
  for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
   ny, nx = y + dy, x + dx
   if 0 <= ny < h and 0 <= nx < w and dark[ny, nx] and not bg[ny, nx]:
    bg[ny, nx] = True
    q.append((ny, nx))
 out = alpha.copy()
 out[bg] = 0
 return out


def add_ink_outline(rgba: np.ndarray, radius: int = 3, color=(18, 10, 8, 230)) -> np.ndarray:
 alpha = rgba[:, :, 3].astype(np.float32) / 255.0
 mask = Image.fromarray((alpha * 255).astype(np.uint8), "L")
 dilated = mask.filter(ImageFilter.MaxFilter(radius * 2 + 1))
 d = np.array(dilated, dtype=np.float32) / 255.0
 outline = np.clip(d - alpha, 0, 1)
 out = rgba.copy()
 for c in range(3):
  out[:, :, c] = np.clip(
   rgba[:, :, c] * alpha + color[c] * outline * (1 - alpha) + color[c] * outline * alpha,
   0,
   255,
  ).astype(np.uint8)
 out[:, :, 3] = np.clip((alpha + outline * (1 - alpha)) * 255, 0, 255).astype(np.uint8)
 return out


def process(path: Path, threshold: int = 34) -> None:
 im = Image.open(path).convert("RGBA")
 arr = np.array(im)
 rgb = arr[:, :, :3].astype(np.int16)
 alpha = flood_dark_background(arr[:, :, 3].copy(), rgb, threshold=threshold)
 arr[:, :, 3] = alpha
 # soften cut edge
 soft = Image.fromarray(arr, "RGBA")
 a = soft.split()[3].filter(ImageFilter.GaussianBlur(0.6))
 arr = np.array(soft)
 arr[:, :, 3] = np.array(a)
 arr = add_ink_outline(arr)
 Image.fromarray(arr, "RGBA").save(path)
 a = arr[:, :, 3]
 print(
  path.name,
  f"alpha mean={a.mean():.1f}",
  f"transparent={(a < 16).sum() / a.size:.1%}",
  f"opaque={(a > 240).sum() / a.size:.1%}",
 )


def main() -> None:
 for path in PORTRAITS:
  if not path.exists():
   print("skip missing", path)
   continue
  process(path)


if __name__ == "__main__":
 main()
