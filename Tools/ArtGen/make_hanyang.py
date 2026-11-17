# Draws the people and props of the road to Hanyang (기획서.md 1-3): the beacon fortress, the abandoned post
# station, the grave road, the Hanyang street, the examination hall and the minister's house.
# People are on the Kongbat villagers' 12 px block grid and palette (make_kong.py), facing left, kept on the full grid
# width so every one has their feet at x 138 of the picture:
#   24_Hanyang/sangin (a merchant in a bamboo hat), yusaeng (a young scholar with a scroll), cheongji (the minister's
#   steward with a red-and-blue lantern), ai (a little boy), mudang (the shaman in her red robe with fan and bells),
#   jumo (the Hanyang gukbap house's hostess with a ladle), bangmul (the travelling woman pedlar met at Kongbat,
#   her bundle on her head)
# Props at one texel per art pixel, 24 per unit, standing on their bottom edge (13_Props):
#   beacon_off, beacon_on (a round stone beacon tower), fort_gate_shut, fort_gate_open, fort_wall, station_house,
#   cart, grave, dead_tree, shop, lantern, hongsal (the red arrow gate), exam_board, pavilion, drink_table
# Usage: python make_hanyang.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
ROOT = sys.argv[1]

COLS, ROWS, BLOCK, OX, OY = 30, 47, 12, 2, 1
PAL = {
    'skin': (249, 196, 140), 'skin2': (226, 163, 109), 'hair': (36, 28, 30), 'hair2': (60, 46, 46),
    'cotton': (253, 248, 228), 'cotton2': (220, 214, 200), 'hemp': (206, 190, 156), 'hemp2': (170, 152, 118),
    'red': (196, 58, 58), 'red2': (150, 38, 44), 'grey': (126, 130, 146), 'grey2': (96, 98, 114),
    'brown': (150, 110, 80), 'brown2': (118, 84, 60), 'indigo': (64, 70, 120), 'indigo2': (46, 50, 92),
    'wood': (122, 82, 50), 'wood2': (92, 60, 36), 'straw': (214, 178, 106), 'straw2': (166, 128, 70),
    'shoe': (196, 160, 96), 'shoe2': (150, 116, 64), 'navy': (49, 49, 70), 'k': K, 'white': (255, 255, 255),
    'jade': (150, 204, 186), 'jade2': (110, 168, 152), 'sky': (150, 186, 220), 'sky2': (114, 150, 194),
    'gold': (236, 196, 92), 'gold2': (186, 142, 60), 'blush': (244, 170, 150), 'mouth': (206, 84, 84),
    'paper': (253, 248, 228), 'lamp': (255, 226, 150),
}

class Figure:
    """Row spans per part, each with its own black outline, drawn back to front (as make_kong.py)."""
    def __init__(s): s.grid = np.zeros((ROWS, COLS, 3), np.uint8); s.filled = np.zeros((ROWS, COLS), bool)
    def mask(s, spans):
        m = np.zeros((ROWS, COLS), bool)
        for y, segs in spans.items():
            for x0, x1 in (segs if isinstance(segs, list) else [segs]): m[y + OY, max(0, x0 + OX):x1 + OX + 1] = True
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
    return {y: (x0 - (flare * (y - top)) // (bottom - top), x1 + (flare * (y - top)) // (bottom - top)) for y in range(top, bottom + 1)}
def trousers(f, cloth, cloth2):
    f.layer({30: (5, 14), 31: (5, 14), 32: [(5, 8), (10, 14)], 33: [(5, 8), (10, 14)], 34: [(5, 8), (10, 13)]}, cloth)
    f.paint(join(rect(12, 30, 14, 34)), cloth2)
    f.layer(join(rect(6, 35, 7, 39), rect(11, 35, 12, 39)), 'cotton'); f.paint(join(rect(7, 35, 7, 39), rect(12, 35, 12, 39)), 'cotton2')
    f.layer({40: [(4, 8), (10, 13)], 41: [(3, 8), (10, 14)]}, 'shoe'); f.paint({41: [(3, 8), (10, 14)]}, 'shoe2')
def jacket(f, cloth, cloth2, sash):
    f.layer(join({16: (6, 12), 17: (5, 13)}, rect(4, 18, 14, 29)), cloth)
    f.paint(rect(12, 17, 14, 29), cloth2); f.paint(rect(4, 27, 14, 28), sash)
    f.paint(join({17: (7, 7)}, {18: (8, 8)}, {19: (9, 9)}, {20: (10, 10)}), cloth2)
def face(f, top=8, beard=False):
    f.layer(join({top: (4, 10)}, rect(3, top + 1, 10, top + 5), {top + 6: (4, 10)}), 'skin')
    f.paint(join(rect(9, top, 10, top + 5), {top + 1: (11, 11)}), 'hair'); f.paint({top + 2: (4, 4)}, 'k')
    f.paint({top + 4: (6, 6)}, 'skin2')
    if beard: f.paint(join({top + 5: (3, 7)}, {top + 6: (4, 6)}), 'hair2')

def sangin():
    """A Hanyang merchant: a bamboo paeraengi hat, brown clothes, a money pouch and an abacus under his arm."""
    f = Figure()
    trousers(f, 'hemp', 'hemp2'); jacket(f, 'brown', 'brown2', 'navy')
    f.layer(rect(13, 24, 15, 27), 'red2')                                            # pouch at the back
    f.layer(join({18: (3, 5), 19: (2, 6)}, rect(2, 20, 6, 23)), 'brown')
    f.layer(rect(-1, 22, 5, 25), 'wood'); f.paint(join(*[rect(x, 23, x, 24) for x in range(0, 5, 2)]), 'k')   # abacus
    face(f, beard=True)
    f.layer(join(rect(0, 6, 14, 7)), 'straw'); f.layer(rect(4, 2, 10, 5), 'straw'); f.paint(rect(4, 5, 10, 5), 'straw2')
    f.paint(join(*[rect(x, 6, x, 7) for x in range(1, 14, 3)]), 'straw2')
    return f

def yusaeng():
    """A young scholar come for the examination: a black yugeon cap, a jade-blue robe to the ground, a scroll."""
    f = Figure()
    f.layer(skirt(16, 40, 4, 13, 2), 'sky'); f.paint(skirt(16, 40, 11, 13, 2), 'sky2')
    f.paint(rect(8, 24, 8, 39), 'sky2')
    f.layer({41: [(3, 7), (10, 13)]}, 'navy')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 22)), 'sky')
    f.paint(rect(12, 16, 14, 22), 'sky2'); f.paint(join({16: (7, 7)}, {17: (8, 8)}, {18: (9, 9)}), 'cotton')
    f.layer(rect(4, 23, 14, 23), 'navy')
    f.layer(join(rect(1, 17, 6, 24)), 'sky'); f.paint(rect(4, 17, 6, 24), 'sky2')
    f.layer({25: (1, 3)}, 'skin')
    f.layer(rect(-1, 22, 1, 30), 'paper'); f.paint(join(rect(-1, 22, 1, 22), rect(-1, 30, 1, 30)), 'wood')   # the scroll
    face(f, top=7)
    f.layer(join(rect(4, 2, 11, 6)), 'navy'); f.layer(rect(6, 0, 9, 1), 'navy'); f.paint(rect(8, 1, 8, 5), 'k')  # yugeon
    return f

def cheongji():
    """The minister's steward: a grey durumagi, a black horsehair band, a red-and-blue lantern on a stick."""
    f = Figure()
    f.layer(skirt(16, 39, 5, 13, 1), 'grey'); f.paint(skirt(16, 39, 11, 13, 1), 'grey2')
    f.layer({40: [(4, 8), (10, 13)], 41: [(3, 8), (10, 14)]}, 'navy')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 22)), 'grey'); f.paint(rect(12, 16, 14, 22), 'grey2')
    f.layer(rect(4, 23, 14, 23), 'indigo')
    f.layer(rect(0, 14, 0, 23), 'wood')                                                # the lantern pole, held low
    f.layer(join(rect(-1, 24, 1, 24), rect(-2, 25, 2, 29), rect(-1, 30, 1, 30)), 'red'); f.paint(rect(-2, 28, 2, 29), 'indigo')
    f.paint(rect(-1, 26, 0, 27), 'lamp')
    f.layer(join(rect(1, 17, 5, 21)), 'grey'); f.layer({22: (0, 3)}, 'skin')
    face(f, top=7, beard=True)
    f.layer(rect(3, 5, 11, 6), 'k'); f.layer(rect(6, 2, 9, 4), 'hair')                # band and topknot
    return f

def ai():
    """A little boy with a long braid and a red vest, head up, a top in his hand."""
    f = Figure()
    f.layer({33: (5, 13), 34: (5, 13), 35: [(5, 8), (10, 13)], 36: [(5, 8), (10, 13)]}, 'cotton')
    f.paint(rect(11, 33, 13, 36), 'cotton2')
    f.layer(join(rect(6, 37, 7, 39), rect(10, 37, 11, 39)), 'cotton2')
    f.layer({40: [(4, 8), (9, 12)], 41: [(4, 8), (9, 12)]}, 'shoe')
    f.layer(join({23: (6, 11)}, rect(5, 24, 12, 32)), 'red'); f.paint(rect(10, 24, 12, 32), 'red2')
    f.paint(rect(5, 30, 12, 30), 'jade')
    f.layer(rect(12, 16, 13, 30), 'hair'); f.layer(rect(12, 31, 14, 32), 'red')       # braid and ribbon
    f.layer(join(rect(3, 24, 5, 28)), 'cotton'); f.layer({29: (3, 5)}, 'skin')
    f.layer(rect(1, 28, 3, 30), 'wood'); f.paint(rect(2, 30, 2, 31), 'wood2')         # a spinning top
    f.layer(join({14: (5, 11)}, rect(4, 15, 12, 21), {22: (5, 11)}), 'skin')
    f.paint(join({14: (5, 12)}, {15: (7, 12)}, rect(10, 16, 12, 20)), 'hair'); f.paint({17: (5, 5)}, 'k')
    f.paint({19: (6, 7)}, 'blush'); f.paint({20: (4, 4)}, 'mouth')
    return f

def mudang():
    """The shaman of the minister's rites: a red cheollik robe over blue, a black hat dressed with red paper
    flowers, a folded fan in one hand and a bunch of brass bells in the other."""
    f = Figure()
    f.layer(skirt(17, 40, 4, 13, 3), 'red'); f.paint(skirt(17, 40, 11, 13, 3), 'red2')
    for x in (6, 9): f.paint(rect(x, 26, x, 39), 'red2')
    f.layer({41: [(3, 7), (10, 14)]}, 'cotton')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 23)), 'red'); f.paint(rect(12, 16, 14, 23), 'red2')
    f.layer(rect(4, 24, 14, 25), 'indigo'); f.layer(rect(12, 26, 13, 33), 'indigo')
    f.layer(join(rect(1, 17, 6, 22)), 'indigo'); f.layer({23: (1, 3)}, 'skin')
    f.layer(join(rect(-2, 22, 0, 24), {25: (-2, -1)}), 'gold'); f.paint({23: (-1, -1)}, 'gold2')            # bells
    f.layer(join(rect(14, 18, 16, 21)), 'indigo'); f.layer(rect(15, 14, 17, 17), 'paper')                     # fan behind
    f.layer(join({7: (4, 10)}, rect(3, 8, 10, 13), {14: (4, 10)}), 'skin')
    f.paint(join(rect(9, 8, 11, 14), {8: (8, 8)}), 'hair'); f.paint({10: (4, 4)}, 'k'); f.paint({12: (3, 4)}, 'mouth')
    f.paint({11: (5, 6)}, 'blush')
    f.layer(join(rect(0, 6, 14, 6)), 'navy'); f.layer(rect(5, 2, 10, 5), 'navy')
    f.layer(rect(10, 1, 12, 3), 'red'); f.layer(rect(3, 3, 4, 4), 'red'); f.paint({2: (11, 11)}, 'gold')
    return f

def jumo():
    """The hostess of a Hanyang gukbap house: a white headscarf, a jade jacket, a brown skirt under a cotton apron,
    a long ladle in her hand."""
    f = Figure()
    f.layer(skirt(20, 40, 5, 13, 2), 'brown'); f.paint(skirt(20, 40, 11, 13, 2), 'brown2')
    f.layer(rect(5, 22, 11, 36), 'cotton'); f.paint(rect(5, 35, 11, 36), 'cotton2')                 # apron
    f.layer({41: [(4, 7), (10, 13)]}, 'shoe')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 20)), 'jade')
    f.paint(rect(12, 16, 14, 20), 'jade2'); f.paint(join({16: (7, 7)}, {17: (8, 8)}, {18: (9, 9)}), 'cotton')
    f.layer(rect(-1, 12, 0, 26), 'wood'); f.layer(rect(-2, 26, 1, 28), 'wood2')                      # the ladle
    f.layer(join(rect(1, 17, 5, 22)), 'jade'); f.layer({23: (0, 3)}, 'skin')
    f.layer(join({6: (5, 10)}, rect(4, 7, 11, 12), {13: (5, 10)}), 'skin')
    f.paint(join(rect(9, 8, 11, 11), {7: (8, 11)}), 'hair'); f.paint({9: (5, 5)}, 'k'); f.paint({11: (5, 6)}, 'mouth')
    f.paint({10: (6, 7)}, 'blush')
    f.layer(join(rect(4, 4, 12, 6), rect(12, 7, 14, 9)), 'cotton'); f.paint(rect(4, 6, 12, 6), 'cotton2')   # headscarf
    return f

def bangmul():
    """A travelling woman pedlar (bangmul-jangsu): a cloth bundle balanced on her head, a white jacket, a grey-blue
    skirt tucked up for the road, a small lacquered box of needles and powder on a cord at her hip."""
    f = Figure()
    f.layer(skirt(20, 38, 5, 13, 1), 'grey'); f.paint(skirt(20, 38, 11, 13, 1), 'grey2')
    f.layer(join(rect(6, 39, 7, 40), rect(11, 39, 12, 40)), 'cotton')
    f.layer({41: [(4, 8), (10, 14)]}, 'straw')
    f.layer(join({15: (6, 12), 16: (5, 13)}, rect(4, 17, 14, 20)), 'cotton')
    f.paint(rect(12, 16, 14, 20), 'cotton2'); f.paint(join(rect(9, 18, 9, 23)), 'red')
    f.layer(rect(13, 24, 16, 27), 'red2'); f.paint(rect(14, 25, 15, 25), 'gold')                   # the little box
    f.layer(join(rect(2, 17, 5, 21)), 'cotton'); f.layer({22: (2, 4)}, 'skin')
    f.layer(join(rect(3, 2, 4, 6)), 'cotton'); f.layer({7: (3, 4)}, 'skin')                         # arm up to the bundle
    f.layer(join({7: (5, 10)}, rect(4, 8, 11, 13), {14: (5, 10)}), 'skin')
    f.paint(join(rect(9, 8, 11, 12), {7: (7, 11)}), 'hair'); f.paint({10: (5, 5)}, 'k'); f.paint({12: (5, 6)}, 'mouth')
    f.layer(rect(10, 10, 13, 12), 'hair')                                                            # low bun
    f.layer(join(rect(4, 1, 13, 5), {0: (6, 11)}), 'indigo'); f.paint(rect(4, 4, 13, 5), 'indigo2')   # the bundle
    f.paint(join(rect(8, 0, 9, 5)), 'sky')
    return f

# ---- props, one texel per art pixel ----
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
STONE, STONE2, STONE3 = (150, 148, 160), (112, 110, 124), (184, 182, 192)
PLASTER, PLASTER2 = (240, 232, 212), (214, 204, 182)
TILE, TILE2, TILE3 = (70, 72, 84), (48, 50, 60), (104, 108, 122)
PAPER, DARK, INK = (253, 248, 228), (46, 36, 34), (40, 34, 40)
RED, RED2, RED3 = (190, 50, 44), (140, 34, 34), (222, 92, 76)
FIRE, FIRE2, FIRE3 = (255, 214, 96), (246, 140, 50), (204, 70, 40)
GOLD, GOLD2 = (236, 196, 92), (186, 142, 60)
MOSS, BARK, BARK2 = (96, 120, 78), (110, 92, 84), (78, 64, 60)
EARTH, EARTH2, GRASS = (122, 104, 84), (96, 80, 66), (110, 128, 80)
BLUE, BLUE2, LAMP = (64, 96, 160), (46, 70, 124), (255, 230, 160)

def stones(c, m, x0, x1, y0, y1, h=5, w=10):
    """Fitted stone courses over mask m, alternate joints, lit top row."""
    c.layer(m, STONE)
    for row, y in enumerate(range(y0, y1 + 1, h)):
        c.paint(c.rect(x0, y, x1, y) & m, STONE2)
        for x in range(x0 + (row % 2) * (w // 2), x1 + 1, w): c.paint(c.rect(x, y, x, y + h - 1) & m, STONE2)
        c.paint(c.rect(x0, y + 1, x1, y + 1) & m & ~c.rect(0, y0 + 1, x0 - 1, y1), STONE3)

def beacon(lit):
    """A squat beacon tower of fitted stone, wider at the foot, a chimney mouth on top; lit, a tall fire and sparks."""
    c = Canvas(40, 64); G = 63
    tower = c.poly([(5, G), (34, G), (30, 28), (9, 28)])
    stones(c, tower, 5, 34, 28, G, 6, 9)
    c.paint(tower & c.rect(26, 0, 40, G), STONE2)
    c.paint(c.rect(8, 52, 12, 57) & tower, MOSS); c.paint(c.rect(27, 38, 30, 41) & tower, MOSS)
    mouth = c.rect(8, 22, 31, 28); c.layer(mouth, STONE2); c.paint(c.rect(10, 22, 29, 24), DARK)
    if lit:
        flame = c.poly([(9, 23), (31, 23), (28, 9), (24, 14), (20, 0), (16, 12), (12, 6)])
        c.layer(flame, FIRE3)
        c.paint(c.poly([(12, 23), (28, 23), (26, 12), (23, 16), (20, 5), (17, 16), (14, 11)]) & flame, FIRE2)
        c.paint(c.poly([(15, 23), (25, 23), (23, 16), (20, 9), (17, 16)]) & flame, FIRE)
        for x, y in ((6, 6), (34, 10), (29, 2), (11, 1)): c.paint(c.rect(x, y, x + 1, y + 1), FIRE, inside=False)
    else:
        c.paint(c.rect(11, 25, 28, 26), (60, 52, 50)); c.paint(c.rect(14, 24, 17, 24) | c.rect(22, 24, 26, 24), WOOD2)
    return c

def fort_gate(open_):
    """A mountain-fortress gate: a stone wall with crenels, a rounded arch, two studded wooden leaves."""
    c = Canvas(110, 130); G = 129
    wall = c.rect(4, 28, 105, G)
    for x in range(4, 106, 14): wall |= c.rect(x, 18, x + 7, 28)
    stones(c, wall, 4, 105, 18, G, 6, 12)
    c.paint(wall & c.rect(86, 0, 110, G), STONE2)
    arch = c.rect(34, 70, 75, G) | c.ellipse(34, 50, 75, 90)
    c.layer(arch, DARK)
    if not open_:
        for x0 in (35, 55):
            leaf = c.rect(x0, 56, x0 + 19, G) & arch
            c.paint(leaf, WOOD)
            for y in range(64, G, 12):
                for x in range(x0 + 3, x0 + 18, 5): c.paint(c.rect(x, y, x, y), GOLD2)
            c.paint(c.rect(x0, 56, x0, G) & arch, WOOD2)
        c.paint(c.rect(53, 90, 56, 96), (60, 60, 70))
    else:
        for x0 in (34, 72):
            c.layer(c.rect(x0 - 1, 58, x0 + 2, G) & ~c.rect(36, 0, 71, G), WOOD)
    c.layer(c.rect(42, 34, 67, 44), WOOD2); c.paint(c.rect(44, 36, 65, 42), (30, 28, 32))      # the name board
    for x in range(47, 64, 6): c.paint(c.rect(x, 38, x + 2, 40), GOLD)
    return c

def fort_wall():
    """A length of fortress wall with crenels and a loophole, to stand behind the road."""
    c = Canvas(96, 62); G = 61
    wall = c.rect(0, 16, 95, G)
    for x in range(0, 96, 14): wall |= c.rect(x, 6, x + 7, 16)
    stones(c, wall, 0, 95, 6, G, 6, 12)
    c.paint(c.rect(42, 30, 47, 38), DARK)
    c.paint(c.rect(0, 52, 30, G) & wall, MOSS)
    return c

def station_house():
    """The abandoned post station: a tiled roof with a hole torn in it, a door hanging off, a faded signboard."""
    c = Canvas(130, 90); G = 89
    c.layer(c.rect(6, G - 6, 123, G), STONE)
    c.layer(c.rect(12, 44, 117, G - 7), PLASTER2)
    c.paint(c.rect(12, 70, 117, G - 7), (184, 172, 150))
    for x in (12, 46, 82, 113): c.layer(c.rect(x, 44, x + 4, G - 7), WOOD2)
    c.paint(c.rect(50, 52, 80, G - 8), DARK)
    c.layer(c.poly([(52, 54), (64, 52), (68, G - 8), (56, G - 8)]), WOOD)             # the door hanging off
    c.layer(c.rect(20, 52, 40, 66), (200, 190, 168)); c.paint(c.line([(22, 54), (38, 64)]) | c.line([(38, 54), (24, 63)]), DARK)
    roof = c.poly([(0, 34), (8, 40), (64, 43), (121, 40), (129, 34), (112, 14), (18, 14)])
    c.layer(roof, TILE)
    for x in range(4, 126, 4): c.paint(c.line([(x - (x - 64) / 8, 15), (x, 43)]) & roof, TILE2)
    hole = c.poly([(70, 15), (92, 15), (96, 30), (84, 38), (74, 30)])
    c.paint(hole & roof, DARK)
    for x, y in ((76, 20), (86, 26), (80, 33)): c.paint(c.rect(x, y, x + 1, y + 1), WOOD2)
    c.layer(c.rect(22, 9, 107, 14), TILE2)
    c.layer(c.rect(52, 46, 78, 50), WOOD); c.paint(c.rect(55, 47, 75, 49), (196, 184, 160))
    return c

def cart():
    c = Canvas(66, 34); G = 33
    c.layer(c.poly([(4, 16), (52, 12), (54, 18), (6, 22)]), WOOD)
    c.paint(c.line([(6, 19), (52, 15)]), WOOD2)
    c.layer(c.line([(50, 14), (64, 28)], 2), WOOD2)
    wheel = c.ellipse(14, 12, 34, G); c.layer(wheel, WOOD); c.paint(c.ellipse(17, 15, 31, 30), DARK)
    for pts in (((24, 13), (24, 32)), ((15, 22), (33, 22)), ((17, 15), (31, 30))): c.paint(c.line(list(pts), 1) & wheel, WOOD2)
    c.layer(c.poly([(36, 30), (52, 26), (56, 30), (40, G)]), WOOD2)                   # a broken wheel on the ground
    return c

def grave():
    """A grassed burial mound with a small stone tablet in front and a bowl for offerings."""
    c = Canvas(64, 30); G = 29
    mound = c.ellipse(4, 6, 58, 48) & c.rect(0, 0, 64, G)
    c.layer(mound, GRASS); c.paint(mound & c.ellipse(26, 4, 70, 50), (88, 104, 70))
    for x in range(8, 56, 5): c.paint(c.rect(x, 8 + abs(x - 31) // 3, x, 9 + abs(x - 31) // 3) & mound, (132, 150, 96))
    c.layer(c.rect(26, 14, 34, G), STONE); c.paint(c.rect(32, 14, 34, G), STONE2); c.paint(c.rect(29, 17, 30, 25), STONE2)
    c.layer(c.rect(14, G - 3, 20, G), (200, 196, 188))
    return c

def dead_tree():
    c = Canvas(56, 96); G = 95
    t = c.line([(26, G), (28, 60), (24, 30), (28, 10)], 5)
    for pts in ([(27, 62), (42, 46), (50, 30)], [(25, 50), (10, 38), (6, 22)], [(25, 32), (36, 20)], [(43, 46), (52, 48)], [(11, 38), (2, 40)]):
        t |= c.line(pts, 3)
    c.layer(t, BARK); c.paint(t & c.rect(27, 0, 56, G), BARK2)
    c.layer(c.poly([(18, G), (36, G), (30, G - 6), (22, G - 6)]), BARK)
    c.layer(c.rect(42, 34, 48, 37), RED2); c.paint(c.rect(46, 38, 47, 46), RED2, inside=False)   # a red rag tied to a branch
    return c

def shop():
    """A Hanyang shop front: tiled roof, an open front with stacked goods and hanging cloth, a signboard."""
    c = Canvas(120, 96); G = 95
    c.layer(c.rect(4, G - 5, 115, G), STONE)
    c.layer(c.rect(10, 40, 109, G - 6), PLASTER)
    for x in (10, 58, 105): c.layer(c.rect(x, 40, x + 4, G - 6), WOOD)
    c.paint(c.rect(15, 48, 57, G - 7), DARK); c.paint(c.rect(63, 48, 104, G - 7), DARK)
    for i, (x, y) in enumerate(((18, 72), (30, 72), (42, 72), (22, 62), (36, 62))):         # bolts of cloth
        col = [(196, 58, 58), (64, 96, 160), (236, 196, 92), (150, 204, 186), (240, 232, 212)][i]
        c.layer(c.rect(x, y, x + 10, y + 8), col)
    for x in range(66, 102, 9):                                                                 # jars and baskets
        c.layer(c.ellipse(x, 70, x + 7, G - 8), (120, 70, 46)); c.layer(c.rect(x + 1, 62, x + 6, 69), (214, 178, 106))
    c.layer(c.rect(14, 82, 105, 88), WOOD); c.paint(c.rect(14, 88, 105, 88), WOOD2)
    roof = c.poly([(0, 30), (7, 35), (60, 38), (113, 35), (119, 30), (104, 12), (16, 12)])
    c.layer(roof, TILE)
    for x in range(4, 116, 4): c.paint(c.line([(x - (x - 60) / 8, 13), (x, 38)]) & roof, TILE2)
    c.layer(c.rect(20, 7, 99, 12), TILE2)
    c.layer(c.rect(42, 40, 78, 47), WOOD2); c.paint(c.rect(44, 41, 76, 46), (236, 222, 180))   # signboard
    for x in range(47, 74, 7): c.paint(c.rect(x, 42, x + 3, 45), INK)
    return c

def lantern():
    """A blue-and-red cheongsa lantern hung from a crooked pole."""
    c = Canvas(22, 70); G = 69
    c.layer(c.rect(3, 6, 5, G), WOOD); c.layer(c.rect(3, 6, 16, 8), WOOD)
    c.layer(c.rect(14, 9, 14, 12), DARK)
    body = c.rect(9, 13, 19, 30)
    c.layer(body, BLUE); c.paint(c.rect(9, 21, 19, 30), RED)
    c.paint(c.rect(11, 15, 13, 19) | c.rect(11, 23, 13, 27), LAMP)
    c.layer(c.rect(10, 31, 18, 32), GOLD2); c.paint(c.rect(13, 33, 15, 38), RED, inside=False)
    return c

def hongsal():
    """The red arrow gate (hongsalmun) before the examination hall: two red posts, a beam of red spikes,
    the taegeuk and trident at the centre."""
    c = Canvas(150, 128); G = 127
    for x in (14, 128):
        c.layer(c.rect(x, 22, x + 7, G), RED); c.paint(c.rect(x + 5, 22, x + 7, G), RED2)
        c.layer(c.rect(x - 3, G - 8, x + 10, G), STONE)
    c.layer(c.rect(8, 24, 141, 30), RED); c.layer(c.rect(8, 44, 141, 49), RED)
    for x in range(26, 126, 7): c.layer(c.rect(x, 4, x + 1, 44), RED); c.paint(c.rect(x, 4, x + 1, 6), RED3)
    tg = c.ellipse(64, 28, 86, 50); c.layer(tg, RED)
    c.paint(tg & c.rect(0, 39, 150, 60), BLUE); c.paint(c.ellipse(64, 34, 75, 45) & c.rect(0, 0, 150, 39), RED)
    c.paint(c.ellipse(75, 34, 86, 45) & c.rect(0, 39, 150, 60), BLUE)
    c.layer(c.rect(74, 0, 76, 26) | c.rect(70, 4, 71, 10) | c.rect(79, 4, 80, 10) | c.rect(70, 10, 80, 11), GOLD)
    return c

def exam_board():
    """A notice board of the examination: two posts, a roofed board with a posted sheet of names."""
    c = Canvas(46, 58); G = 57
    c.layer(c.rect(5, 10, 8, G) | c.rect(37, 10, 40, G), WOOD)
    c.layer(c.rect(3, 14, 42, 40), WOOD2); c.layer(c.rect(7, 17, 38, 37), PAPER)
    for x in range(10, 36, 4): c.paint(c.rect(x, 20, x + 1, 34), INK)
    c.layer(c.poly([(0, 12), (45, 12), (40, 6), (5, 6)]), TILE)
    return c

def pavilion():
    """The minister's pavilion over his lotus pond: a raised floor on stone pillars, red columns, a hipped tile
    roof with upturned eaves, hung with red lanterns."""
    c = Canvas(220, 160); G = 159
    for x in range(14, 210, 38):
        c.layer(c.rect(x, 104, x + 8, G), STONE); c.paint(c.rect(x + 6, 104, x + 8, G), STONE2)
    c.layer(c.rect(6, 96, 213, 104), WOOD); c.paint(c.rect(6, 102, 213, 104), WOOD2)
    c.layer(c.rect(6, 88, 213, 90), WOOD2)
    for x in range(10, 214, 8): c.layer(c.rect(x, 90, x + 1, 95), WOOD2)
    for x in range(16, 210, 38):
        c.layer(c.rect(x, 46, x + 5, 95), RED); c.paint(c.rect(x + 4, 46, x + 5, 95), RED2)
    c.layer(c.rect(10, 40, 209, 47), (64, 120, 104)); c.paint(c.rect(10, 44, 209, 47), (48, 92, 82))   # dancheong beam
    for x in range(14, 206, 12): c.paint(c.rect(x, 41, x + 5, 43), (236, 196, 92))
    roof = c.poly([(0, 28), (10, 36), (110, 40), (209, 36), (219, 28), (200, 24), (176, 4), (44, 4), (20, 24)])
    c.layer(roof, TILE)
    for x in range(4, 216, 4): c.paint(c.line([(x - (x - 110) / 9, 5), (x, 40)]) & roof, TILE2)
    c.paint(roof & c.rect(0, 4, 220, 6), TILE3)
    c.layer(c.rect(40, 0, 180, 4) | c.rect(36, 0, 40, 3) | c.rect(180, 0, 184, 3), TILE2)
    for x in (35, 73, 147, 185):
        c.layer(c.rect(x, 48, x, 52), DARK); lamp = c.ellipse(x - 5, 53, x + 5, 66)
        c.layer(lamp, RED); c.paint(c.ellipse(x - 3, 55, x + 1, 62) & lamp, (255, 170, 120))
    return c

def drink_table():
    """A low lacquered drinking table with a white bottle, two cups and a dish."""
    c = Canvas(44, 26); G = 25
    c.layer(c.rect(2, 12, 41, 15), (120, 40, 36)); c.paint(c.rect(2, 15, 41, 15), (86, 28, 28))
    c.layer(c.rect(5, 16, 7, G) | c.rect(36, 16, 38, G), (86, 28, 28))
    c.layer(c.ellipse(8, 2, 16, 11) | c.rect(11, 0, 13, 3), (236, 236, 228)); c.paint(c.rect(9, 5, 15, 5), (64, 96, 160))
    c.layer(c.rect(20, 9, 23, 11), (236, 236, 228)); c.layer(c.rect(26, 9, 29, 11), (236, 236, 228))
    c.layer(c.ellipse(31, 8, 39, 12), (196, 160, 96)); c.paint(c.rect(33, 8, 37, 9), (196, 58, 58))
    return c

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    im = Image.fromarray(img); box = im.getbbox()
    im.crop((box[0], 0, box[2], c.h)).save(f"{ROOT}/13_Props/{name}.png")

os.makedirs(f"{ROOT}/24_Hanyang", exist_ok=True)
for name, draw in (("sangin", sangin), ("yusaeng", yusaeng), ("cheongji", cheongji), ("ai", ai), ("mudang", mudang), ("jumo", jumo), ("bangmul", bangmul)):
    draw().save(f"{ROOT}/24_Hanyang/{name}.png")
for name, draw in (("beacon_off", lambda: beacon(False)), ("beacon_on", lambda: beacon(True)), ("fort_gate_shut", lambda: fort_gate(False)),
                   ("fort_gate_open", lambda: fort_gate(True)), ("fort_wall", fort_wall), ("station_house", station_house), ("cart", cart),
                   ("grave", grave), ("dead_tree", dead_tree), ("shop", shop), ("lantern", lantern), ("hongsal", hongsal),
                   ("exam_board", exam_board), ("pavilion", pavilion), ("drink_table", drink_table)):
    save(draw(), name)
