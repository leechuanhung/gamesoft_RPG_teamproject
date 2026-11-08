# Draws the mountain pass props (the map beyond the ferry) at one texel per art pixel, 24 per unit:
#   jangseung (a pair of carved village guardians), seonang_tree (the shrine tree hung with cloth and straw rope),
#   cairn (the shrine's stone pile), boulder (the rock that rolls down the pass).
# Usage: python make_pass.py Assets/Resources/JinaV4/13_Props
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas
OUT = sys.argv[1]
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
STONE, STONE2, STONE3 = (150, 148, 160), (112, 110, 124), (184, 182, 192)
STRAW, ROPE = (214, 178, 106), (140, 106, 58)
RED, BLUE, YEL, GRN, WHT = (186, 52, 46), (60, 86, 156), (226, 186, 70), (70, 132, 82), (240, 236, 224)
LEAF, LEAF2, INK = (62, 104, 66), (44, 80, 52), (34, 26, 24)

def jangseung():
    """Two carved poles side by side: big round eyes, a grinning mouth, a hat on the taller (the general)."""
    c = Canvas(44, 86)
    for x0, top, hat in ((3, 6, True), (25, 16, False)):
        pole = c.rect(x0 + 2, top + 10, x0 + 14, 85)
        c.layer(pole, WOOD3)
        c.paint(c.rect(x0 + 11, top + 10, x0 + 14, 85), WOOD)
        head = c.ellipse(x0, top, x0 + 16, top + 22)
        c.layer(head, WOOD3)
        c.paint(c.ellipse(x0 + 9, top, x0 + 16, top + 22) & head, WOOD)
        for ex in (x0 + 3, x0 + 9):
            c.layer(c.ellipse(ex, top + 7, ex + 4, top + 11), WHT)
            c.paint(c.rect(ex + 2, top + 9, ex + 2, top + 9), INK)
        c.paint(c.rect(x0 + 7, top + 12, x0 + 9, top + 15), WOOD)
        mouth = c.rect(x0 + 4, top + 17, x0 + 12, top + 19)
        c.paint(mouth, INK)
        for tx in range(x0 + 5, x0 + 12, 2): c.paint(c.rect(tx, top + 17, tx, top + 17), WHT)
        if hat:
            c.layer(c.rect(x0 - 1, top - 2, x0 + 17, top + 1) | c.rect(x0 + 4, top - 6, x0 + 12, top - 2), INK)
        for y in range(top + 30, 82, 8): c.paint(c.rect(x0 + 5, y, x0 + 11, y + 3), INK)   # carved title, unreadable at this size
    return c

def seonang_tree():
    """A broad old tree, its trunk bound with a straw rope hung with strips of five-coloured cloth."""
    c = Canvas(110, 120)
    trunk = c.poly([(46, 119), (66, 119), (62, 60), (70, 40), (60, 44), (55, 34), (50, 44), (40, 40), (48, 60)])
    c.layer(trunk, WOOD)
    c.paint(trunk & c.rect(57, 0, c.w, c.h), WOOD2)
    crown = c.ellipse(4, 2, 70, 50) | c.ellipse(40, 0, 106, 44) | c.ellipse(20, 20, 90, 60)
    c.layer(crown, LEAF)
    c.paint(crown & c.rect(0, 40, c.w, c.h), LEAF2)
    c.paint(crown & c.rect(70, 0, c.w, c.h), LEAF2)
    for row, y in enumerate(range(8, 58, 6)):   # leaf texture, dashes as on the pines
        for x in range(6 + (row % 2) * 5, 104, 10): c.paint(c.rect(x, y, x + 4, y) & crown, LEAF2 if x < 70 and y < 40 else LEAF)
    c.layer(c.rect(44, 78, 67, 81), STRAW)
    for i, x in enumerate(range(46, 67, 4)):
        colour = (RED, BLUE, YEL, GRN, WHT)[i % 5]
        c.layer(c.rect(x, 82, x + 2, 92 + (i * 5) % 9), colour)
    return c

def cairn():
    """The shrine's pile of stones, small ones on big, a flat one on top."""
    c = Canvas(40, 30)
    stones = [(2, 18, 18, 29), (16, 17, 38, 29), (7, 8, 22, 19), (20, 9, 33, 19), (12, 1, 27, 10)]
    for i, (x0, y0, x1, y1) in enumerate(stones):
        s = c.ellipse(x0, y0, x1, y1)
        c.layer(s, STONE if i % 2 == 0 else STONE3)
        c.paint(s & c.rect((x0 + x1) // 2 + 2, 0, c.w, c.h), STONE2)
    return c

def boulder():
    """A round rock, 1 unit across, shaded from the left."""
    c = Canvas(24, 24)
    rock = c.ellipse(1, 1, 22, 22)
    c.layer(rock, STONE)
    c.paint(rock & ~c.ellipse(-4, -4, 18, 18), STONE2)
    c.paint(c.rect(6, 6, 9, 7) | c.rect(5, 8, 6, 9), STONE3)
    c.paint(c.line([(12, 4), (14, 10), (11, 15)]) & rock, STONE2)
    return c

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")

for name, draw in (("jangseung", jangseung), ("seonang_tree", seonang_tree), ("cairn", cairn), ("boulder", boulder)):
    save(draw(), name)
