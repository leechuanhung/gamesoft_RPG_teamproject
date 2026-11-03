# Usage: python make_peddler.py Assets/Resources/JinaV4/12_Peddler/peddler_idle.png
# Draws the bobusang (peddler) on a logical pixel grid in the Jina sprite palette, then scales up.
# Each part is a set of row spans with its own black outline, drawn back to front.
import sys
from PIL import Image
import numpy as np
COLS, ROWS, BLOCK, OX, OY = 30, 47, 12, 2, 1
K = (11, 9, 11)
PAL = {
    'skin': (249, 196, 140), 'skin2': (226, 163, 109),
    'straw': (214, 178, 106), 'straw2': (166, 128, 70),
    'cotton': (253, 248, 228), 'cotton2': (220, 214, 200),
    'jacket': (221, 191, 155), 'jacket2': (184, 150, 112),
    'pants': (147, 108, 83), 'pants2': (120, 88, 66),
    'wrap': (253, 248, 228), 'wrap2': (226, 216, 190),
    'shoe': (196, 160, 96), 'shoe2': (150, 116, 64),
    'navy': (49, 49, 70), 'wood': (122, 82, 50), 'wood2': (92, 60, 36),
    'cloth': (64, 84, 132), 'cloth2': (46, 60, 100), 'rope': (214, 178, 106), 'k': K,
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

# A-frame carrier (jige): poles reach the ground behind the heels, tips show above the load
layer(join(rect(15, 1, 15, 43), rect(22, 1, 22, 43), rect(15, 29, 22, 29), rect(15, 36, 22, 36)), 'wood')
paint(join(rect(15, 30, 15, 43), rect(22, 30, 22, 43)), 'wood2')
# cloth-wrapped load roped onto the carrier
load = join({3: (16, 21), 4: (15, 23), 22: (15, 23), 23: (16, 21)}, rect(14, 5, 24, 21))
layer(load, 'cloth')
paint(join({4: (21, 23), 22: (21, 23)}, rect(22, 5, 24, 21)), 'cloth2')
paint(join(rect(14, 12, 24, 12), rect(19, 3, 19, 23)), 'rope')
paint(rect(18, 11, 20, 13), 'straw2')
# baggy trousers gathered into leg wraps (haengjeon), straw sandals
layer({30: (5, 14), 31: (5, 14), 32: (4, 14), 33: (4, 14), 34: [(4, 8), (10, 14)], 35: [(5, 8), (10, 13)], 36: [(5, 8), (10, 13)]}, 'pants')
paint(join(rect(13, 30, 14, 34), rect(12, 35, 13, 36), rect(8, 32, 8, 36)), 'pants2')
layer(join(rect(5, 37, 8, 39), rect(10, 37, 13, 39)), 'wrap')
paint(join(rect(5, 38, 8, 38), rect(10, 38, 13, 38), rect(13, 37, 13, 39), rect(8, 37, 8, 39)), 'wrap2')
layer({40: [(4, 7), (10, 13)], 41: [(2, 7), (10, 14)]}, 'shoe')
paint({41: [(2, 7), (10, 14)]}, 'shoe2')
# short jacket (jeogori) with white collar, sash, and the carrier strap over the shoulder
layer(join({16: (6, 11), 17: (5, 12), 18: (4, 13)}, rect(4, 19, 14, 25), rect(3, 26, 14, 28), {29: (4, 14)}), 'jacket')
paint(join(rect(12, 18, 14, 29), {17: (12, 12)}), 'jacket2')
paint({16: (7, 7), 17: (7, 8), 18: (8, 8), 19: (8, 9), 20: (9, 9), 21: (9, 10)}, 'cotton')
paint(join({22: (8, 9), 23: (7, 8), 24: (7, 7)}), 'jacket2')
paint(rect(3, 27, 14, 28), 'navy')
paint(rect(11, 17, 11, 26), 'wood')
# front arm hanging in a wide sleeve, hand below the cuff
layer(join({18: (3, 5), 19: (2, 6)}, rect(2, 20, 6, 23), {24: (1, 6), 25: (1, 6)}), 'jacket')
paint(join(rect(5, 19, 6, 25)), 'jacket2')
paint({25: (1, 6)}, 'cotton')
layer({26: (2, 4), 27: (2, 4)}, 'skin')
paint({27: (4, 4), 26: (4, 4)}, 'skin2')
# head facing left: hair at the back, eye, short beard along the jaw
layer(join({8: (4, 10)}, rect(3, 9, 10, 13), {14: (4, 10), 11: (2, 10)}), 'skin')
paint(join(rect(9, 8, 10, 13), {9: (11, 11)}), 'k')
paint(rect(9, 10, 9, 11), 'skin2')
paint(rect(4, 10, 4, 11), 'k')
paint(join({13: (5, 6), 14: (5, 8), 15: (6, 7)}), 'k')
paint({12: (3, 4)}, 'skin2')
# paeraengi bamboo hat with the bobusang cotton tufts at both brim ends
layer(join({3: (5, 9)}, rect(4, 4, 10, 5), {6: (1, 13), 7: (0, 14)}), 'straw')
paint(join({5: (4, 10), 7: (0, 14)}), 'straw2')
paint(join(rect(6, 3, 6, 4), rect(8, 3, 8, 4)), 'straw2')
layer({8: (0, 2), 9: (0, 2), 10: (1, 1)}, 'cotton')
layer({8: (12, 14), 9: (12, 14), 10: (13, 13)}, 'cotton')
paint({9: [(0, 2), (12, 14)], 10: [(1, 1), (13, 13)]}, 'cotton2')

img = np.zeros((ROWS, COLS, 4), np.uint8); img[..., :3] = grid; img[..., 3] = np.where(filled, 255, 0)
big = Image.fromarray(img).resize((COLS * BLOCK, ROWS * BLOCK), Image.NEAREST)
big.crop(big.getbbox()).save(sys.argv[1])
