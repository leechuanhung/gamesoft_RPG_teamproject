# Builds the ghost's attack frames from its idle (no new drawing). The water ghosts use the same frames, tinted.
#   ghost_attack_00 windup: upper body reared back, the clawed arm raised about the shoulder
#   ghost_attack_01 swipe: upper body thrown forward, the arm flung out level
#   ghost_attack_02 follow-through: still bent forward, the arm swept down
# Every frame keeps the idle's 1254 px canvas, scale and feet line, so size and design stay the same.
# Usage: python make_ghost_attack.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image, ImageDraw
from framelib import load, save, bend, clean, preview_sheet, preview_dir
root = sys.argv[1] + "/07_Ghost"

G = 25   # one art pixel; the art faces right
idle = load(f"{root}/ghost_guard_idle.png")
# The sleeve and clawed hand, cut along the robe; it turns about the shoulder.
ARM = [(740, 660), (1010, 670), (1010, 890), (850, 890), (800, 780), (740, 740)]
SHOULDER = (750, 690)

def pose(lean, top, hip, degrees):
    """The body bent by `lean`, its arm first turned `degrees` counter-clockwise about the shoulder."""
    im = Image.new("1", (idle.shape[1], idle.shape[0])); ImageDraw.Draw(im).polygon(ARM, fill=1)
    mask = np.array(im, bool)
    arm = idle.copy(); arm[~mask] = 0
    body = idle.copy(); body[mask] = 0
    # Turned on the art's own 25 px grid, so the arm keeps the same chunky dots as the rest of the ghost.
    small = Image.fromarray(arm[G // 2::G, G // 2::G])
    small = small.rotate(degrees, resample=Image.NEAREST, center=(SHOULDER[0] / G, SHOULDER[1] / G))
    cells = np.array(small)
    solid = cells[:, :, 3] > 128
    ring = np.zeros_like(solid)   # rotating breaks the outline, so it is drawn again around the turned arm
    ring[1:] |= solid[:-1]; ring[:-1] |= solid[1:]; ring[:, 1:] |= solid[:, :-1]; ring[:, :-1] |= solid[:, 1:]
    cells[ring & ~solid] = (11, 9, 11, 255)
    grown = np.kron(cells, np.ones((G, G, 1), np.uint8))
    turned = np.zeros_like(idle); turned[:grown.shape[0], :grown.shape[1]] = grown
    m = turned[:, :, 3] > 128
    body[m] = turned[m]
    return clean(bend(body, lean, top, hip, G))

frames = [
    pose(-75, 300, 850, 65),
    pose(100, 300, 850, 25),
    pose(75, 300, 850, -20),
]
names = [f"{root}/ghost_attack_{i:02d}.png" for i in range(len(frames))]
for f, name in zip(frames, names): save(f, name)

if preview_dir(): preview_sheet([f"{root}/ghost_guard_idle.png"] + names, preview_dir() + "/ghost_attack_sheet.png")
