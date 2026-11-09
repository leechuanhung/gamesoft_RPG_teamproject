# Draws the ferry crossing (the map right of the forest): the ferryman, on the peddler's 12 px block grid and
# palette, and the props at one texel per art pixel (boat, hut, reeds, river water).
#   15_Ferryman/ferryman_idle.png         facing left like the peddler, feet at x 108 (block 9 of the grid)
#   13_Props/ferry_boat, ferry_hut, reeds, water, thorn_wall (the thorns that bar the forest's end)
# Usage: python make_ferry.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
ROOT = sys.argv[1]

# ---- the ferryman: block grid as make_peddler.py (spans per row, outline per part, back to front) ----
COLS, ROWS, BLOCK, OX, OY = 30, 47, 12, 2, 1
PAL = {
    'skin': (249, 196, 140), 'skin2': (226, 163, 109),
    'straw': (214, 178, 106), 'straw2': (166, 128, 70), 'straw3': (236, 208, 146),
    'hemp': (206, 190, 156), 'hemp2': (170, 152, 118),
    'cotton': (253, 248, 228), 'cotton2': (220, 214, 200),
    'navy': (49, 49, 70), 'wood': (122, 82, 50), 'wood2': (92, 60, 36),
    'shoe': (196, 160, 96), 'shoe2': (150, 116, 64), 'beard': (236, 232, 222), 'k': K,
}
grid = np.zeros((ROWS, COLS, 3), np.uint8); filled = np.zeros((ROWS, COLS), bool)
def mask(spans):
    m = np.zeros((ROWS, COLS), bool)
    for y, segs in spans.items():
        for x0, x1 in (segs if isinstance(segs, list) else [segs]): m[y + OY, x0 + OX:x1 + OX + 1] = True
    return m
def rect(x0, y0, x1, y1): return {y: (x0, x1) for y in range(y0, y1 + 1)}
def join(*parts):
    out = {}
    for p in parts:
        for y, s in p.items(): out.setdefault(y, []).extend(s if isinstance(s, list) else [s])
    return out
def layer(spans, color):
    m = mask(spans); ring = np.zeros_like(m)
    ring[1:] |= m[:-1]; ring[:-1] |= m[1:]; ring[:, 1:] |= m[:, :-1]; ring[:, :-1] |= m[:, 1:]
    ring &= ~m; grid[ring] = K; filled[ring] = True; paint(spans, color)
def paint(spans, color):
    m = mask(spans); grid[m] = PAL[color]; filled[m] = True

def ferryman():
    # the punting pole (sasdae), held upright in front of him, its foot on the ground
    layer(rect(1, 0, 1, 41), 'wood'); paint(rect(1, 30, 1, 41), 'wood2')
    # hemp trousers rolled to the knee, bare shins, straw sandals
    layer({30: (5, 14), 31: (5, 14), 32: (5, 14), 33: [(5, 8), (10, 14)], 34: [(5, 8), (10, 14)]}, 'hemp')
    paint(join(rect(12, 30, 14, 34), rect(8, 31, 8, 34)), 'hemp2')
    paint({34: [(5, 8), (10, 14)]}, 'cotton2')
    layer(join(rect(6, 35, 7, 39), rect(11, 35, 12, 39)), 'skin')
    paint(join(rect(7, 35, 7, 39), rect(12, 35, 12, 39)), 'skin2')
    layer({40: [(4, 8), (10, 13)], 41: [(3, 8), (10, 14)]}, 'shoe')
    paint({41: [(3, 8), (10, 14)]}, 'shoe2')
    # hemp jacket, a little stooped, sash tied in front
    layer(join({16: (6, 12), 17: (5, 13), 18: (4, 14)}, rect(4, 19, 14, 26), rect(4, 27, 15, 29)), 'hemp')
    paint(join(rect(12, 18, 14, 29), {17: (13, 13)}), 'hemp2')
    paint({16: (7, 7), 17: (7, 8), 18: (8, 8), 19: (8, 9), 20: (9, 9), 21: (9, 10)}, 'cotton')
    paint(rect(4, 26, 15, 27), 'navy')
    paint(join(rect(5, 28, 5, 29)), 'navy')
    # front arm reaching to the pole, hand closed round it
    layer(join({18: (3, 6), 19: (2, 6)}, rect(2, 20, 6, 21)), 'hemp')
    paint(rect(5, 19, 6, 21), 'hemp2')
    layer(rect(1, 21, 3, 23), 'skin'); paint(rect(1, 22, 1, 22), 'wood')
    # an old face turned left: white topknot hair hidden by the hat, long white beard
    layer(join({8: (4, 10)}, rect(3, 9, 10, 13), {14: (4, 10), 11: (2, 10)}), 'skin')
    paint(join(rect(9, 8, 10, 12), {9: (11, 11)}), 'beard')
    paint(rect(4, 10, 4, 10), 'k')
    paint({12: (3, 4)}, 'skin2')
    layer(join({13: (3, 7), 14: (3, 8), 15: (4, 8), 16: (4, 7), 17: (5, 6)}), 'beard')
    paint({15: (7, 8), 16: (7, 7)}, 'cotton2')
    # a wide conical straw hat (satgat) shading the face
    layer(join({2: (7, 8), 3: (6, 9), 4: (5, 10), 5: (4, 11), 6: (2, 13), 7: (0, 15)}), 'straw')
    paint(join({5: (9, 11), 6: (10, 13), 7: (11, 15)}), 'straw2')
    paint(join({3: (7, 7), 5: (5, 6)}), 'straw3')
    paint({7: (0, 15)}, 'straw2')

ferryman()
img = np.zeros((ROWS, COLS, 4), np.uint8); img[..., :3] = grid; img[..., 3] = np.where(filled, 255, 0)
im = Image.fromarray(img).resize((COLS * BLOCK, ROWS * BLOCK), Image.NEAREST)
box = im.getbbox()
os.makedirs(f"{ROOT}/15_Ferryman", exist_ok=True)
im.crop(box).save(f"{ROOT}/15_Ferryman/ferryman_idle.png")
print("ferryman feet x in the crop:", (OX + 9) * BLOCK + BLOCK // 2 - box[0])

# ---- props (one texel per art pixel, 24 per unit; ground at the bottom) ----
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
STRAW, STRAW2, STRAW3, ROPE = (214, 178, 106), (178, 140, 78), (236, 208, 146), (140, 106, 58)
WALL, WALL2, DARK = (221, 191, 155), (192, 160, 122), (46, 36, 34)
REED, REED2, REED3 = (150, 150, 92), (112, 116, 70), (196, 176, 120)
WATER, WATER2, FOAM = (64, 104, 132), (48, 80, 108), (150, 190, 206)

def boat():
    """A flat-bottomed ferry, 4 units long: planked hull with a raised bow and stern, plank deck on top."""
    c = Canvas(98, 22)
    hull = c.poly([(1, 4), (8, 9), (89, 9), (96, 4), (90, 20), (7, 20)])
    c.layer(hull, WOOD)
    c.paint(hull & c.rect(0, 15, c.w, c.h), WOOD2)
    for y in (12, 16): c.paint(c.rect(0, y, c.w, y) & hull, WOOD2)
    c.layer(c.rect(8, 7, 89, 9), WOOD3)
    for x in range(14, 88, 12): c.paint(c.rect(x, 7, x, 9), WOOD)
    return c

def hut():
    """The ferryman's thatched hut: mud wall, a reed-mat door, a straw roof tied down with rope."""
    c = Canvas(84, 70)
    G = 68
    wall = c.rect(10, 34, 73, G); c.layer(wall, WALL)
    c.paint(c.rect(10, 56, 73, G), WALL2)
    c.layer(c.rect(10, 34, 13, G) | c.rect(70, 34, 73, G), WOOD)
    door = c.rect(30, 42, 46, G); c.layer(door, DARK)
    mat = c.rect(31, 43, 38, G); c.layer(mat, REED)
    for y in range(45, G, 3): c.paint(c.rect(31, y, 38, y), REED2)
    roof = c.poly([(2, 38), (16, 10), (68, 10), (82, 38)]) | c.ellipse(14, 4, 70, 22)
    c.layer(roof, STRAW)
    c.paint(roof & c.rect(0, 30, c.w, c.h), STRAW2)
    for x in (24, 42, 60): c.paint(c.line([(x, 8), (x - 6 + (x - 42) // 3, 38)]) & roof, ROPE)
    c.paint(c.rect(18, 12, 30, 13) & roof, STRAW3)
    return c

def reeds():
    """A clump of riverside reeds with brown seed heads, 1.5 units tall."""
    c = Canvas(30, 38)
    rng = np.random.default_rng(5)
    stems = c.blank(); heads = c.blank()
    for i, x in enumerate(range(3, 28, 4)):
        top = int(rng.integers(4, 12)); lean = int(rng.integers(-3, 4))
        stems |= c.line([(x, 37), (x + lean, top)])
        heads |= c.ellipse(x + lean - 1, top - 4, x + lean + 1, top + 2)
    c.layer(stems, REED)
    c.paint(stems & c.rect(15, 0, c.w, c.h), REED2)
    c.layer(heads, (138, 98, 58))
    leaves = c.line([(6, 36), (1, 24)]) | c.line([(22, 36), (28, 22)])
    c.layer(leaves, REED); c.paint(leaves & c.rect(0, 30, c.w, c.h), REED2)
    return c

def water():
    """A 4-unit strip of river surface: two bands with wave crests, tiling side by side."""
    c = Canvas(96, 20)
    c.paint(c.rect(0, 2, 95, 19), WATER, inside=False)
    c.paint(c.rect(0, 11, 95, 19), WATER2, inside=False)
    for x in range(0, 96, 16):
        c.paint(c.rect(x + 2, 2, x + 7, 2) | c.rect(x + 4, 1, x + 6, 1), FOAM, inside=False)
        c.paint(c.rect(x + 10, 9, x + 13, 9), FOAM, inside=False)
    return c

def thorn_wall():
    """A thicket of dark thorny vines from the ground past the top of the screen, 8.5 units tall, 2 wide."""
    c = Canvas(48, 204)
    rng = np.random.default_rng(9)
    VINE, VINE2, THORN, LEAF = (54, 44, 52), (78, 60, 62), (150, 140, 120), (44, 74, 58)
    for x0 in (8, 24, 40, 15, 32):
        points = []
        for y in range(203, -8, -14):
            points.append((x0 + int(rng.integers(-7, 8)), y))
        vine = c.line(points, 4)
        c.layer(vine, VINE)
        c.paint(vine & c.line([(p[0] - 1, p[1]) for p in points], 1), VINE2)
        for (x, y) in points[1::2]:
            side = 1 if rng.random() < .5 else -1
            thorn = c.poly([(x + side, y - 2), (x + side * 7, y - 1), (x + side, y + 2)])
            c.layer(thorn, THORN)
    for _ in range(14):
        x, y = int(rng.integers(5, 43)), int(rng.integers(6, 198))
        c.layer(c.ellipse(x - 2, y - 1, x + 2, y + 1), LEAF)
    return c

def save_prop(c, name):
    # keep the full canvas (no crop): the water strip must tile and the boat's deck line is measured
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{ROOT}/13_Props/{name}.png")

for name, draw in (("ferry_boat", boat), ("ferry_hut", hut), ("reeds", reeds), ("water", water), ("thorn_wall", thorn_wall)):
    save_prop(draw(), name)
