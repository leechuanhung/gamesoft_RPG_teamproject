# Draws the tavern, keeper, bench, house, pine, stele and charm at one texel per art pixel.
# Usage: python make_props.py Assets/Resources/JinaV4/13_Props   [--preview]  (--preview also writes 4x *_x4.png copies for viewing)
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixlib import *
OUT = sys.argv[1]
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
WALL, WALL2 = (221, 191, 155), (192, 160, 122)
STRAW, STRAW2, STRAW3, ROPE = (214, 178, 106), (178, 140, 78), (236, 208, 146), (140, 106, 58)
STONE, STONE2, STONE3 = (150, 148, 160), (112, 110, 124), (184, 182, 192)
PAPER, DARK = (253, 248, 228), (46, 36, 34)
ONGGI, ONGGI2, ONGGI3 = (120, 70, 46), (86, 48, 32), (160, 100, 66)
RED = (168, 40, 40)

def stone_base(c, x0, x1, y0, y1):
    m = c.rect(x0, y0, x1, y1); c.layer(m, STONE)
    c.paint(c.rect(x0, y0, x1, y0 + 1), STONE3)
    for y in range(y0 + 3, y1, 4): c.paint(c.rect(x0, y, x1, y), STONE2)
    for row, y in enumerate(range(y0, y1, 4)):
        for x in range(x0 + 5 + (row % 2) * 6, x1, 12): c.paint(c.rect(x, y, x, y + 3) & m, STONE2)

def lattice(c, x0, y0, x1, y1, step=3):
    m = c.rect(x0, y0, x1, y1); c.layer(m, PAPER)
    for x in range(x0 + step, x1, step): c.paint(c.rect(x, y0, x, y1), WOOD3)
    for y in range(y0 + step, y1, step): c.paint(c.rect(x0, y, x1, y), WOOD3)
    c.paint(c.rect(x0, y1 - 1, x1, y1), (230, 220, 192))

def onggi(c, cx, bottom, w, h):
    m = c.ellipse(cx - w // 2, bottom - h, cx + w // 2, bottom + h // 5) & c.rect(0, 0, c.w, bottom)
    m |= c.rect(cx - w // 4, bottom - h - 2, cx + w // 4, bottom - h + 1)
    c.layer(m, ONGGI)
    c.paint(m & c.rect(cx + w // 5, 0, c.w, c.h), ONGGI2)
    c.paint(c.rect(cx - w // 3, bottom - h + 3, cx - w // 3 + 1, bottom - h + 6), ONGGI3)
    c.paint(c.rect(cx - w // 2, bottom - h // 2, cx + w // 2, bottom - h // 2) & m, ONGGI2)

def tavern():
    c = Canvas(172, 108)
    G = 106
    stone_base(c, 8, 163, G - 8, G)
    wall = c.rect(14, 50, 157, G - 9); c.layer(wall, WALL)
    c.paint(c.rect(14, 80, 157, G - 9), WALL2)
    for x in (14, 44, 78, 122, 153):
        c.layer(c.rect(x, 50, x + 4, G - 9), WOOD); c.paint(c.rect(x + 3, 50, x + 4, G - 9), WOOD2)
    c.layer(c.rect(14, 50, 157, 54), WOOD); c.paint(c.rect(14, 54, 157, 54), WOOD2)
    c.layer(c.rect(14, 76, 157, 78), WOOD)
    # open door: dark interior with one lattice panel folded aside
    c.layer(c.rect(50, 57, 76, G - 9), DARK)
    c.paint(c.rect(56, 62, 72, 64), (70, 56, 50))
    lattice(c, 50, 57, 57, G - 10, 3)
    c.layer(c.rect(49, 56, 77, 56), WOOD)
    lattice(c, 88, 58, 116, 72); lattice(c, 128, 58, 148, 72)
    c.layer(c.rect(87, 73, 117, 74), WOOD); c.layer(c.rect(127, 73, 149, 74), WOOD)
    onggi(c, 26, G - 9, 14, 13); onggi(c, 38, G - 9, 10, 9)
    # thatched roof: rounded mound, roped net, dark eave
    roof = (c.ellipse(4, 12, 167, 80) & c.rect(0, 0, c.w, 51)) | c.poly([(0, 44), (171, 44), (166, 52), (5, 52)])
    c.layer(roof, STRAW)
    c.paint(roof & c.rect(0, 46, c.w, 52), STRAW2)
    c.paint(roof & c.ellipse(10, 14, 161, 60) & c.rect(0, 0, c.w, 22), STRAW3)
    for y in range(18, 46, 4):
        for x in range((y * 7) % 9, c.w, 9): c.paint(c.rect(x, y, x + 3, y) & roof & ~c.rect(0, 46, c.w, 52), STRAW2)
    net = c.blank()
    for x in range(-60, c.w + 60, 16): net |= c.line([(x, 0), (x + 50, 52)]) | c.line([(x, 0), (x - 50, 52)])
    c.paint(net & roof & c.rect(0, 0, c.w, 45), ROPE)
    c.paint(c.rect(0, 52, c.w, 53) & roof, ROPE)
    ridge = c.ellipse(55, 9, 116, 22) & c.rect(0, 0, c.w, 15)
    c.layer(ridge, STRAW2)
    for x in range(58, 114, 4): c.paint(c.rect(x, 11, x + 1, 13) & ridge, ROPE)
    # wine flag (jugi) on a pole by the door
    c.layer(c.rect(2, 24, 3, G), WOOD)
    flag = c.rect(5, 27, 21, 49); c.layer(flag, PAPER)
    c.paint(c.rect(5, 47, 21, 49), (230, 220, 192))
    c.layer(c.rect(4, 26, 22, 26), WOOD2)
    glyph = ["o...ooooooo", ".o.....o.o.", "......o...o", "o...ooooooo", ".o..o.o.o.o", "....o.o.o.o",
             "....oo...oo", "..o.o.....o", ".o..ooooooo", "o...o.....o", "....ooooooo"]
    for gy, row in enumerate(glyph):
        for gx, ch in enumerate(row):
            if ch == 'o': c.paint(c.rect(8 + gx, 32 + gy, 8 + gx, 32 + gy), RED)
    return c

PLASTER, PLASTER2 = (240, 232, 212), (214, 204, 182)
TILE, TILE2, TILE3 = (70, 72, 84), (48, 50, 60), (104, 108, 122)
LEAF, LEAF2, LEAF3 = (52, 104, 64), (36, 76, 48), (88, 142, 86)
BARK, BARK2 = (150, 84, 58), (106, 58, 40)
MOSS, MOSS2, MOSS3 = (128, 138, 130), (96, 104, 98), (166, 174, 166)

def house():
    c = Canvas(128, 96); G = 95
    stone_base(c, 6, 121, G - 7, G)
    c.layer(c.rect(12, 44, 115, G - 8), PLASTER)
    c.paint(c.rect(12, 74, 115, G - 8), PLASTER2)
    for x in (12, 38, 87, 111):
        c.layer(c.rect(x, 44, x + 4, G - 8), WOOD); c.paint(c.rect(x + 3, 44, x + 4, G - 8), WOOD2)
    c.layer(c.rect(12, 44, 115, 47), WOOD)
    lattice(c, 45, 52, 63, G - 10); lattice(c, 65, 52, 83, G - 10)
    lattice(c, 18, 54, 33, 68); lattice(c, 93, 54, 107, 68)
    c.layer(c.rect(17, 69, 34, 70), WOOD); c.layer(c.rect(92, 69, 108, 70), WOOD)
    # tiled roof with upturned eaves, tile furrows and a mortared ridge
    roof = c.poly([(0, 31), (7, 37), (30, 42), (64, 44), (97, 42), (120, 37), (127, 31), (118, 29), (100, 12), (28, 12), (9, 29)])
    c.layer(roof, TILE)
    for x in range(4, 125, 4):
        lean = (x - 64) / 64 * 8
        c.paint(c.line([(x - lean, 13), (x, 44)]) & roof, TILE2)
    eave = roof & ~c.poly([(0, 28), (7, 34), (30, 39), (64, 41), (97, 39), (120, 34), (127, 28), (127, 0), (0, 0)])
    c.paint(eave, TILE2)
    for x in range(3, 125, 4): c.paint(c.rect(x, 0, x + 1, c.h) & eave, TILE3)
    c.paint(roof & c.rect(0, 12, c.w, 15), TILE3)
    ridge = c.rect(24, 6, 103, 12) | c.rect(21, 2, 25, 9) | c.rect(102, 2, 106, 9)
    c.layer(ridge, TILE2)
    c.paint(c.rect(26, 10, 101, 10), PLASTER2)
    c.paint(c.rect(21, 2, 22, 3) | c.rect(105, 2, 106, 3), TILE3)
    return c

def pine():
    c = Canvas(96, 146); G = 145
    trunk = c.poly([(41, G), (55, G), (52, 110), (54, 70), (50, 34), (45, 34), (46, 70), (43, 110)])
    roots = c.poly([(34, G), (62, G), (54, G - 6), (42, G - 6)])
    c.layer(trunk | roots, BARK)
    c.paint((trunk | roots) & c.rect(50, 0, c.w, c.h), BARK2)
    for y in range(40, G, 7): c.paint(c.rect(45 + (y % 3), y, 48 + (y % 3), y) & trunk, BARK2)
    for pts in ([(48, 60), (26, 46)], [(50, 64), (72, 50)], [(47, 86), (22, 76)], [(52, 90), (76, 82)]):
        c.layer(c.line(pts, 3), BARK)
    clumps = [(48, 20, 24, 11), (26, 44, 22, 9), (70, 48, 22, 9), (48, 64, 30, 10), (20, 76, 17, 7), (76, 82, 17, 7)]
    for cx, cy, rx, ry in clumps:
        m = c.ellipse(cx - rx, cy - ry, cx + rx, cy + ry)
        c.layer(m, LEAF)
        c.paint(m & c.ellipse(cx - rx, cy - ry - ry // 2, cx + rx - 3, cy + ry // 3) & c.rect(0, 0, c.w, cy - 1), LEAF3)
        c.paint(m & c.rect(0, cy + ry // 2, c.w, c.h), LEAF2)
        for y in range(cy - ry + 3, cy + ry - 1, 3):
            for x in range(cx - rx + (y % 5), cx + rx, 6): c.paint(c.rect(x, y, x + 2, y) & m, LEAF2)
    return c

def stele():
    c = Canvas(66, 66); G = 65
    body = c.ellipse(4, 40, 63, 76) & c.rect(0, 0, c.w, G - 3)
    head = c.ellipse(0, 46, 14, 58)
    legs = c.rect(10, G - 5, 18, G) | c.rect(46, G - 5, 54, G)
    c.layer(legs, MOSS2); c.layer(body | head, MOSS)
    c.paint(body & c.rect(0, 56, c.w, c.h), MOSS2)
    for x in range(14, 60, 9): c.paint(c.line([(x, 44), (x - 4, 56)]) & body, MOSS2)
    c.paint(c.rect(4, 50, 5, 51), K)
    slab = (c.ellipse(19, 2, 46, 18) & c.rect(0, 0, c.w, 10)) | c.rect(19, 10, 46, 48)
    c.layer(slab, STONE)
    c.paint(slab & c.rect(19, 0, 21, c.h), STONE3); c.paint(slab & c.rect(44, 0, 46, c.h), STONE2)
    # carved mountain, crescent moon and river marks (not in the puzzle's order)
    c.paint(c.line([(25, 20), (32, 12), (39, 20)]), STONE2)
    c.paint(c.ellipse(27, 24, 37, 34) & ~c.ellipse(30, 23, 40, 32), STONE2)
    for y in (39, 43):
        for x in range(25, 41, 4): c.paint(c.rect(x, y, x + 1, y) | c.rect(x + 2, y + 1, x + 3, y + 1), STONE2)
    for x, y in ((20, 44), (44, 30), (40, 47), (8, 54)): c.paint(c.rect(x, y, x + 2, y + 1), (96, 128, 80))
    return c

def bench():
    c = Canvas(58, 22); G = 21
    c.layer(c.rect(4, 12, 7, G) | c.rect(50, 12, 53, G), WOOD2)
    c.layer(c.rect(2, 7, 55, 12), WOOD3)
    for x in range(12, 50, 10): c.paint(c.rect(x, 7, x, 10), WOOD)
    c.paint(c.rect(2, 11, 55, 12), WOOD)
    c.layer(c.rect(22, 4, 34, 6), WOOD)
    c.layer(c.ellipse(23, 0, 28, 5) & c.rect(0, 0, c.w, 3), PAPER)
    c.layer(c.ellipse(29, 1, 33, 5) & c.rect(0, 0, c.w, 3), ONGGI3)
    return c

def keeper(spread=0):
    SKIN, SKIN2 = (249, 196, 140), (226, 163, 109)
    JEO, JEO2, SKIRT, SKIRT2 = (253, 248, 228), (226, 216, 190), (112, 122, 152), (84, 92, 120)
    APRON, APRON2, GOLD = (240, 234, 220), (210, 200, 180), (226, 186, 74)
    c = Canvas(28, 48); s = c.spans
    c.layer(s({**{y: (4, 19) for y in range(25, 41)}, 41: (3, 20), 42: (3, 20)}), SKIRT)
    c.paint(s({y: (16, 20) for y in range(25, 43)}), SKIRT2)
    c.layer(s({y: (4, 12) for y in range(27, 42)}), APRON)
    c.paint(s({y: (11, 12) for y in range(27, 42)}), APRON2)
    c.layer(s({43: [(4 - spread, 8 - spread), (11 + spread, 15 + spread)]}), JEO)
    c.layer(s({18: (7, 15), 19: (6, 16), 20: (5, 17), 21: (5, 18), 22: (5, 18), 23: (5, 18), 24: (5, 18)}), JEO)
    c.paint(s({y: (15, 18) for y in range(19, 25)}), JEO2)
    c.paint(s({19: (8, 8), 20: (9, 9), 21: (10, 10)}), JEO2)
    c.paint(s({22: (8, 10), 23: (9, 9), 24: (9, 9), 25: (9, 9)}), RED)
    c.layer(s({y: (3, 6) for y in range(20, 26)}), JEO)
    c.layer(s({26: (3, 5), 27: (3, 5)}), SKIN)
    c.layer(s({8: (6, 12), 9: (5, 13), **{y: (5, 13) for y in range(10, 15)}, 15: (6, 12), 16: (7, 11), 12: (4, 13)}), SKIN)
    c.paint(s({8: (11, 13), 9: (11, 14), 10: (12, 14), 11: (12, 14), 12: (13, 14)}), K)
    c.paint(s({11: (11, 11), 12: (12, 12)}), SKIN2)
    c.paint(s({11: (6, 6), 12: (6, 6)}), K)
    c.paint(s({14: (6, 7)}), (200, 90, 80))
    c.layer(s({5: (7, 12), 6: (6, 13), 7: (5, 14), 8: (14, 15)}), K)
    c.layer(s({8: (16, 19), 9: (15, 20), 10: (15, 20), 11: (16, 19)}), (30, 26, 30))
    c.layer(s({9: (21, 24)}), GOLD)
    return c

def charm():
    YEL, YEL2, INK = (236, 198, 80), (204, 160, 56), (180, 32, 38)
    c = Canvas(20, 38)
    c.layer(c.rect(2, 2, 17, 35), YEL)
    c.paint(c.rect(15, 2, 17, 35) | c.rect(2, 34, 17, 35), YEL2)
    c.paint(c.ellipse(6, 4, 13, 11) & ~c.ellipse(8, 6, 11, 9), INK)
    c.paint(c.rect(9, 12, 10, 31), INK)
    for y, (x0, x1) in ((15, (5, 14)), (19, (6, 13)), (23, (4, 15)), (27, (6, 13))): c.paint(c.rect(x0, y, x1, y), INK)
    c.paint(c.line([(5, 29), (9, 32)]) | c.line([(14, 29), (10, 32)]), INK)
    c.paint(c.rect(5, 17, 5, 21) | c.rect(14, 17, 14, 21), INK)
    return c

def shift(c, y0, y1, dx=0, dy=0):
    """Move rows y0..y1 of a drawn canvas sideways (whole rows, so outlines move with their fills) or up."""
    cc, aa = c.c.copy(), c.a.copy()
    c.c[y0:y1 + 1] = 0; c.a[y0:y1 + 1] = False
    for y in range(y0, y1 + 1):
        if 0 <= y - dy < c.h:
            c.c[y - dy] = np.roll(cc[y], dx, 0); c.a[y - dy] = np.roll(aa[y], dx, 0)

# Keeper run frames: she leans forward (left), the hem swings back, feet part or meet, and she bobs a pixel.
# Same canvas for all four: her idle box plus one pixel left, right and above, so the bottom-centre pivot matches.
def keeper_run(spread, lift, sway):
    c = keeper(spread)
    shift(c, 38, 42, dx=sway)
    shift(c, 0, 24, dx=-1)
    if lift: shift(c, 0, c.h - 1, dy=lift)
    return c

for name, fn in (('tavern', tavern), ('house', house), ('pine', pine), ('stele', stele), ('bench', bench), ('keeper', keeper), ('charm', charm)):
    c = fn(); c.save(OUT + '/' + name + '.png')
    if '--preview' in sys.argv: c.save(OUT + '/' + name + '_x4.png', 4)
box = Image.fromarray(np.where(keeper().a, 255, 0).astype(np.uint8)).getbbox()
for i, args in enumerate([(2, 0, 1), (0, 1, 0), (1, 0, 1), (0, 1, 0)]):
    c = keeper_run(*args)
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    im = Image.fromarray(img).crop((box[0] - 1, box[1] - 1, box[2] + 1, box[3]))
    im.save(OUT + '/keeper_run_%02d.png' % i)
    if '--preview' in sys.argv: im.resize((im.width * 4, im.height * 4), Image.NEAREST).save(OUT + '/keeper_run_%02d_x4.png' % i)

