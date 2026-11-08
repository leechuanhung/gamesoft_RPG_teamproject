# Draws the people and props of Kongbat village (between the ferry and the pass), where Kongjwi and Patjwi live.
# People are on the peddler's 12 px block grid and palette (make_peddler.py, make_ferry.py), facing left, kept on
# the full grid width so every one has its feet at x 138 (block 11) of the picture:
#   16_Kong/kongjwi, patjwi (girls, a head shorter), anak (a village wife), namu (a woodcutter with his A-frame)
# Props at one texel per art pixel, 24 per unit, standing on their bottom edge:
#   13_Props/jangdok (crocks on a stone stand), well, laundry (poles and a line hung with clothes), feast_list
# Usage: python make_kong.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
ROOT = sys.argv[1]

COLS, ROWS, BLOCK, OX, OY = 30, 47, 12, 2, 1
PAL = {
    'skin': (249, 196, 140), 'skin2': (226, 163, 109), 'hair': (36, 28, 30), 'hair2': (60, 46, 46),
    'cotton': (253, 248, 228), 'cotton2': (220, 214, 200), 'hemp': (206, 190, 156), 'hemp2': (170, 152, 118),
    'yellow': (244, 214, 110), 'yellow2': (214, 178, 70), 'red': (196, 58, 58), 'red2': (150, 38, 44),
    'grey': (126, 130, 146), 'grey2': (96, 98, 114), 'brown': (150, 110, 80), 'brown2': (118, 84, 60),
    'indigo': (64, 70, 120), 'indigo2': (46, 50, 92), 'wood': (122, 82, 50), 'wood2': (92, 60, 36),
    'straw': (214, 178, 106), 'straw2': (166, 128, 70), 'shoe': (196, 160, 96), 'shoe2': (150, 116, 64),
    'silver': (196, 200, 210), 'navy': (49, 49, 70), 'k': K,
    'pink': (240, 150, 170), 'blush': (244, 170, 150), 'mouth': (206, 84, 84), 'white': (255, 255, 255),
    'jade': (150, 204, 186), 'jade2': (110, 168, 152),
}

class Figure:
    """Row spans per part, each with its own black outline, drawn back to front (as make_peddler.py)."""
    def __init__(s): s.grid = np.zeros((ROWS, COLS, 3), np.uint8); s.filled = np.zeros((ROWS, COLS), bool)
    def mask(s, spans):
        m = np.zeros((ROWS, COLS), bool)
        for y, segs in spans.items():
            for x0, x1 in (segs if isinstance(segs, list) else [segs]): m[y + OY, x0 + OX:x1 + OX + 1] = True
        return m
    def layer(s, spans, color):
        m = s.mask(spans); ring = np.zeros_like(m)
        ring[1:] |= m[:-1]; ring[:-1] |= m[1:]; ring[:, 1:] |= m[:, :-1]; ring[:, :-1] |= m[:, 1:]
        ring &= ~m; s.grid[ring] = K; s.filled[ring] = True; s.paint(spans, color)
    def paint(s, spans, color):
        m = s.mask(spans); s.grid[m] = PAL[color]; s.filled[m] = True
    def save(s, path):
        img = np.zeros((ROWS, COLS, 4), np.uint8); img[..., :3] = s.grid; img[..., 3] = np.where(s.filled, 255, 0)
        im = Image.fromarray(img).resize((COLS * BLOCK, ROWS * BLOCK), Image.NEAREST)
        box = im.getbbox()
        im.crop((0, box[1], im.width, box[3])).save(path)   # full width: the feet stay at x 138

def rect(x0, y0, x1, y1): return {y: (x0, x1) for y in range(y0, y1 + 1)}
def join(*parts):
    out = {}
    for p in parts:
        for y, s in p.items(): out.setdefault(y, []).extend(s if isinstance(s, list) else [s])
    return out
def skirt(top, bottom, x0, x1, flare):
    """A long skirt (chima) from the chest down, widening by `flare` blocks to the hem."""
    return {y: (x0 - (flare * (y - top)) // (bottom - top), x1 + (flare * (y - top)) // (bottom - top)) for y in range(top, bottom + 1)}

def girl(f, t, jacket, jacket2, cuff, chima, chima2, goreum, ribbon, look):
    """A girl a head shorter than the grown-ups, drawn to read as pretty at game size: a large head with bangs,
    a big eye with a highlight and lashes, rosy cheek; a braid down the back tied with a long daenggi ribbon;
    a short jacket with a white collar, a long coat-string and coloured cuffs; a full skirt with folds.
    look: 'smile' (eye closed in a crescent), 'firm' (open eye under a straight brow)."""
    # skirt (chima) from the chest, folds in the darker shade, flower shoes peeking out
    f.layer(skirt(t + 13, 40, 5, 13, 3), chima)
    f.paint(join(skirt(t + 13, 40, 11, 13, 3)), chima2)
    for x in (7, 10): f.paint(rect(x, t + 22, x, 39), chima2)
    f.layer({41: [(4, 7), (9, 12)]}, 'shoe'); f.paint({41: [(4, 5), (9, 10)]}, 'pink')
    # braid behind, crossed in two shades, and the ribbon with two tails
    f.layer(rect(11, t + 6, 12, t + 21), 'hair')
    f.paint(join(*[rect(11, y, 11, y) for y in range(t + 7, t + 21, 2)], *[rect(12, y, 12, y) for y in range(t + 8, t + 21, 2)]), 'hair2')
    f.layer(join(rect(11, t + 22, 13, t + 23), rect(12, t + 24, 13, t + 27), rect(14, t + 24, 14, t + 26)), ribbon)
    # jacket (jeogori): white collar crossing to the left, cuffs of another colour, long coat-string
    f.layer(join({t + 9: (6, 11), t + 10: (5, 12)}, rect(4, t + 11, 13, t + 13)), jacket)
    f.paint(rect(11, t + 10, 13, t + 13), jacket2)
    f.paint({t + 9: (7, 7), t + 10: (7, 8), t + 11: (8, 9), t + 12: (9, 9)}, 'cotton')
    f.layer(join(rect(9, t + 12, 9, t + 19), rect(10, t + 12, 10, t + 17)), goreum)
    # front arm in the sleeve, its cuff, the hand
    f.layer(rect(3, t + 11, 6, t + 14), jacket); f.paint(rect(5, t + 11, 6, t + 14), jacket2)
    f.layer(rect(3, t + 15, 6, t + 15), cuff)
    f.layer({t + 16: (3, 5)}, 'skin')
    # head: a little large for her age, hair swept back over the crown with a shine, bangs over the brow
    f.layer(join({t: (5, 10)}, {t + 1: (4, 11)}, rect(3, t + 2, 12, t + 7), {t + 8: (5, 10)}), 'skin')
    f.paint(join({t: (5, 10)}, {t + 1: (4, 12)}, {t + 2: (4, 12)}, rect(9, t + 3, 12, t + 6), {t + 7: (10, 11)}), 'hair')
    f.paint({t + 3: (4, 5)}, 'hair')
    f.paint({t + 1: (7, 9)}, 'hair2'); f.paint({t + 2: (6, 8)}, 'hair2')
    f.paint(rect(10, t + 3, 10, t + 4), 'skin2')                                   # the ear under the hair
    if look == 'smile':
        f.paint({t + 4: [(4, 4), (6, 6)], t + 3: (5, 5)}, 'k')                    # a closed, smiling eye: a crescent
    else:
        f.paint({t + 3: (4, 6)}, 'k')                                               # a straight brow
        f.paint(rect(5, t + 4, 5, t + 5), 'k'); f.paint({t + 4: (6, 6)}, 'k')      # a big dark eye
        f.paint({t + 4: (5, 5)}, 'white')                                           # its highlight
        f.paint({t + 4: (4, 4)}, 'k')                                               # lashes
    f.paint({t + 6: (6, 7)}, 'blush')
    f.paint({t + 7: (4, 4)}, 'mouth')

def kongjwi():
    f = Figure(); girl(f, 11, 'yellow', 'yellow2', 'pink', 'red', 'red2', 'red2', 'pink', 'smile')
    f.layer(rect(10, 10, 11, 11), 'pink'); f.paint({10: (10, 10)}, 'white')     # a flower pin in her hair
    return f
def patjwi():
    f = Figure(); girl(f, 11, 'jade', 'jade2', 'indigo', 'indigo', 'indigo2', 'red', 'red', 'firm'); return f

def anak():
    """A village wife: white jacket, indigo skirt to the ground, hair in a low bun held by a silver pin."""
    f = Figure()
    f.layer(skirt(20, 40, 5, 13, 2), 'indigo'); f.paint(skirt(20, 40, 11, 13, 2), 'indigo2')
    f.layer({41: [(4, 7), (10, 13)]}, 'shoe')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 20)), 'cotton')
    f.paint(rect(12, 16, 14, 20), 'cotton2')
    f.paint(join(rect(9, 18, 9, 23)), 'indigo2')
    f.layer(join(rect(2, 17, 5, 22)), 'cotton'); f.layer({23: (2, 4), 24: (2, 4)}, 'skin')
    f.layer(join({6: (5, 10)}, rect(4, 7, 11, 12), {13: (5, 10)}), 'skin')
    f.paint(join({6: (5, 11)}, {7: (8, 12)}, rect(9, 8, 12, 11)), 'hair')
    f.layer(rect(12, 10, 14, 13), 'hair'); f.layer(rect(11, 11, 16, 11), 'silver')
    f.paint({9: (5, 5)}, 'k'); f.paint({12: (5, 6)}, 'skin2')
    return f

def namu():
    """A woodcutter: an A-frame (jige) piled with firewood, hemp clothes, a cloth band round his head."""
    f = Figure()
    f.layer(join(rect(16, 6, 16, 41), rect(22, 6, 22, 41), rect(16, 33, 22, 33)), 'wood')
    wood = join(*[rect(14, y, 24, y + 1) for y in range(6, 26, 3)])
    f.layer(wood, 'brown'); f.paint(join(*[rect(14, y + 1, 24, y + 1) for y in range(6, 26, 3)]), 'brown2')
    f.paint(join(rect(19, 5, 19, 27)), 'straw2')
    f.layer({30: (5, 14), 31: (5, 14), 32: [(5, 8), (10, 14)], 33: [(5, 8), (10, 14)], 34: [(5, 8), (10, 13)]}, 'hemp')
    f.paint(join(rect(12, 30, 14, 34)), 'hemp2')
    f.layer(join(rect(6, 35, 7, 39), rect(11, 35, 12, 39)), 'skin'); f.paint(join(rect(7, 35, 7, 39), rect(12, 35, 12, 39)), 'skin2')
    f.layer({40: [(4, 8), (10, 13)], 41: [(3, 8), (10, 14)]}, 'shoe'); f.paint({41: [(3, 8), (10, 14)]}, 'shoe2')
    f.layer(join({16: (6, 12), 17: (5, 13)}, rect(4, 18, 14, 29)), 'hemp')
    f.paint(rect(12, 17, 14, 29), 'hemp2'); f.paint(rect(4, 27, 14, 28), 'navy')
    f.paint(join(rect(11, 17, 11, 27)), 'wood')
    f.layer(join({18: (3, 5), 19: (2, 6)}, rect(2, 20, 6, 24)), 'hemp'); f.layer({25: (2, 4), 26: (2, 4)}, 'skin')
    f.layer(join({8: (4, 10)}, rect(3, 9, 10, 13), {14: (4, 10)}), 'skin')
    f.paint(join(rect(9, 8, 10, 13), {9: (11, 11)}), 'hair'); f.paint({10: (4, 4)}, 'k')
    f.paint(join({13: (4, 8), 14: (5, 8)}), 'hair2')
    f.layer(join(rect(3, 6, 11, 7)), 'cotton'); f.layer(rect(12, 6, 13, 9), 'cotton')
    f.paint({5: (5, 9)}, 'hair')
    return f

os.makedirs(f"{ROOT}/16_Kong", exist_ok=True)
for name, draw in (("kongjwi", kongjwi), ("patjwi", patjwi), ("anak", anak), ("namu", namu)):
    draw().save(f"{ROOT}/16_Kong/{name}.png")

# ---- props ----
STONE, STONE2, STONE3 = (150, 148, 160), (112, 110, 124), (184, 182, 192)
ONGGI, ONGGI2, ONGGI3 = (120, 70, 46), (86, 48, 32), (160, 100, 66)
WOOD, WOOD2, WOOD3, ROPE = (122, 82, 50), (92, 60, 36), (150, 104, 66), (140, 106, 58)
WATER, PAPER, INK, CLOTHW, CLOTHB, CLOTHR = (64, 104, 132), (240, 230, 200), (52, 40, 36), (240, 236, 222), (110, 130, 186), (196, 80, 80)

def onggi(c, cx, bottom, w, h):
    m = c.ellipse(cx - w // 2, bottom - h, cx + w // 2, bottom + h // 5) & c.rect(0, 0, c.w, bottom)
    m |= c.rect(cx - w // 4, bottom - h - 2, cx + w // 4, bottom - h + 1)
    c.layer(m, ONGGI)
    c.paint(m & c.rect(cx + w // 5, 0, c.w, c.h), ONGGI2)
    c.paint(c.rect(cx - w // 3, bottom - h + 3, cx - w // 3 + 1, bottom - h + 6), ONGGI3)
    c.layer(c.rect(cx - w // 4 - 1, bottom - h - 4, cx + w // 4 + 1, bottom - h - 2), ONGGI2)   # lid

def jangdok():
    """Crocks of sauce and salted fish on a low stone stand, big ones at the back."""
    c = Canvas(84, 46)
    stand = c.rect(2, 38, 81, 45); c.layer(stand, STONE)
    c.paint(c.rect(2, 43, 81, 45), STONE2); c.paint(c.rect(2, 38, 81, 38), STONE3)
    for cx, w, h in ((16, 24, 26), (42, 28, 32), (68, 24, 26)): onggi(c, cx, 37, w, h)
    for cx, w, h in ((28, 16, 14), (57, 16, 14)): onggi(c, cx, 37, w, h)
    return c

def well():
    """A stone well with a wooden frame and a gourd dipper on its rim."""
    c = Canvas(44, 50)
    ring = c.rect(4, 30, 39, 49); c.layer(ring, STONE)
    for y in (34, 40, 46): c.paint(c.rect(4, y, 39, y), STONE2)
    for row, y in enumerate((30, 36, 42)):
        for x in range(8 + (row % 2) * 6, 38, 12): c.paint(c.rect(x, y, x, y + 5), STONE2)
    c.layer(c.rect(2, 28, 41, 30), STONE3)
    c.layer(c.rect(6, 4, 8, 28) | c.rect(35, 4, 37, 28), WOOD)
    c.layer(c.rect(2, 2, 41, 5), WOOD3); c.paint(c.rect(2, 5, 41, 5), WOOD)
    c.layer(c.line([(22, 6), (22, 20)]), ROPE)
    c.layer(c.ellipse(26, 22, 34, 28), (214, 178, 106))
    return c

def laundry():
    """Two poles and a line between them, hung with a white jacket, a blue skirt and a red cloth."""
    c = Canvas(110, 70)
    c.layer(c.rect(3, 6, 5, 69) | c.rect(104, 6, 106, 69), WOOD)
    c.layer(c.line([(5, 9), (55, 14), (104, 9)]), ROPE)
    c.layer(c.poly([(14, 11), (36, 13), (38, 22), (33, 22), (32, 36), (18, 35), (17, 21), (12, 21)]), CLOTHW)
    c.layer(c.poly([(46, 14), (66, 14), (72, 48), (40, 48)]), CLOTHB)
    c.paint(c.poly([(58, 14), (66, 14), (72, 48), (60, 48)]), (86, 104, 160))
    c.layer(c.rect(78, 12, 94, 30), CLOTHR)
    return c

def feast_list():
    """A sheet of the feast's bill of fare weighted by a stone, long columns of writing."""
    c = Canvas(26, 14)
    c.layer(c.rect(1, 5, 24, 13), PAPER)
    for x in range(4, 22, 3): c.paint(c.rect(x, 7, x, 12), INK)
    c.layer(c.ellipse(16, 1, 23, 7), STONE)
    return c

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{ROOT}/13_Props/{name}.png")

for name, draw in (("jangdok", jangdok), ("well", well), ("laundry", laundry), ("feast_list", feast_list)):
    save(draw(), name)
