# Builds the player run frames from the walk frames: forward lean, wider stride, bob on frames 1 and 3.
# Usage: python make_run.py Assets/Resources/JinaV4/10_Player_Walk Assets/Resources/JinaV4/11_Player_Run
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image
import numpy as np
from framelib import clean
src, dst = sys.argv[1], sys.argv[2]
STEP = 10                  # art reads as ~10 px logical pixels
NECK, HIP, FEET = 215, 330, 562
W, H = 440, 590           # extra 28 px headroom so lifted frames keep the hat
# frame: (walk source, stride spread px at feet, lift px, lean px at neck)
plan = [(0, 40, 0, 40), (1, 10, 20, 50), (2, 40, 0, 40), (3, 10, 20, 50)]
def q(v): return int(round(v / STEP) * STEP)
for i, (w, spread, lift, lean) in enumerate(plan):
    a = np.array(Image.open(f"{src}/player_walk_{w:02d}.png").convert("RGBA"))
    h, wd, _ = a.shape
    out = np.zeros((H, W, 4), np.uint8)
    base = (W - wd) // 2
    split = wd // 2
    for y in range(h):
        row = a[y]
        if y < NECK: sh = lean
        elif y < HIP: sh = q(lean * (HIP - y) / (HIP - NECK))
        else: sh = 0
        s = q(spread * (y - HIP) / (FEET - HIP)) if y >= HIP else 0
        ty = y + (H - 562) - lift
        if ty < 0: continue
        line = np.zeros((W, 4), np.uint8)
        x0 = base + sh
        # back half moves back, front half moves forward, gap filled with the seam column
        line[x0 - s:x0 - s + split] = row[:split]
        line[x0 + split + s:x0 + wd + s] = row[split:]
        if s: line[x0 - s + split:x0 + split + s] = row[split]
        out[ty] = line
    clean(out)  # drops the soft halo and the 1-4 px slivers left where neighbouring rows shift differently
    Image.fromarray(out).save(f"{dst}/player_run_{i:02d}.png")
