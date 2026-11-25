# Draws what the windows are made of: tiling grains in near-white (the game multiplies its colours over them), the brass
# corners, the HUD's small icons, and the soft dark vignette laid over the world.
#   19_UI/grain_paper, grain_wood, grain_night   64 x 64, tile in both directions
#   19_UI/corner                                 8 x 8, top-left (flipped for the other corners)
#   19_UI/icon_<sword,bow,arrow,coin,charm,ward,sandal,goal>   16 x 16 with outlines (pixlib rules)
#   19_UI/vignette                               160 x 90, black with alpha
# Usage: python make_ui.py Assets/Resources/JinaV4/19_UI
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
S = 64

def grey(a): return np.dstack([a, a, a, np.full(a.shape, 255)]).astype(np.uint8)

def periodic(rng, parts, axis_x=True):
    Y, X = np.mgrid[:S, :S]
    v = np.zeros((S, S), float)
    for amp, fx, fy in parts:
        v += amp * np.sin(2 * math.pi * (X * fx + Y * fy) / S + rng.random() * 6.28)
    return v

def paper():
    rng = np.random.default_rng(1)
    v = 251 + periodic(rng, [(1.0, 1, 2), (.8, 3, 1)])
    # Mulberry fibres: short pale and dark curls that wrap around the tile.
    for _ in range(46):
        x, y = rng.random() * S, rng.random() * S; a = rng.random() * 6.28; bend = (rng.random() - .5) * .5
        tone = 240 if rng.random() < .6 else 255
        for k in range(int(rng.integers(3, 8))):
            v[int(y) % S, int(x) % S] = tone
            x += math.cos(a); y += math.sin(a); a += bend
    Image.fromarray(grey(np.clip(v, 0, 255))).save(f"{OUT}/grain_paper.png")

def wood():
    rng = np.random.default_rng(2)
    Y, X = np.mgrid[:S, :S]
    v = 226 + 12 * np.sin(2 * math.pi * Y * 5 / S + 2.2 * np.sin(2 * math.pi * X / S)) + 6 * np.sin(2 * math.pi * Y * 11 / S + rng.random() * 6)
    v = np.round(v / 10) * 10
    # A few long dark grain lines.
    for _ in range(9):
        y = int(rng.integers(0, S)); x = int(rng.integers(0, S))
        for k in range(int(rng.integers(10, 34))):
            v[(y + int(2 * math.sin(k / 5))) % S, (x + k) % S] = 186
    Image.fromarray(grey(np.clip(v, 0, 255))).save(f"{OUT}/grain_wood.png")

def night():
    rng = np.random.default_rng(3)
    v = 244 + periodic(rng, [(3, 1, 1), (2, 2, 3)])
    for _ in range(70):
        v[int(rng.integers(0, S)), int(rng.integers(0, S))] = 255 if rng.random() < .5 else 214
    Image.fromarray(grey(np.clip(v, 0, 255))).save(f"{OUT}/grain_night.png")

def corner():
    c = Canvas(8, 8)
    B, B2, B3 = (214, 170, 84), (150, 108, 48), (246, 214, 138)
    m = c.rect(0, 0, 7, 2) | c.rect(0, 0, 2, 7) | c.rect(3, 3, 4, 4)
    c.paint(m, B, inside=False)
    c.paint(c.rect(0, 0, 7, 0) | c.rect(0, 0, 0, 7), B3, inside=False)
    c.paint(c.rect(1, 2, 7, 2) | c.rect(2, 1, 2, 7), B2, inside=False)
    c.paint(c.rect(1, 1, 1, 1), (255, 240, 190), inside=False)
    save(c, "corner")

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")

def icons():
    STEEL, STEEL2, HILT, GOLD, GOLD2 = (226, 232, 238), (150, 160, 178), (70, 42, 34), (246, 202, 96), (186, 134, 52)
    WOOD, WOOD2, STRING, RED, FEATHER = (146, 96, 56), (104, 66, 40), (232, 226, 206), (196, 58, 52), (232, 226, 206)
    # The hwando, on a slant.
    c = Canvas(16, 16)
    c.layer(c.line([(14, 1), (5, 10)], 2), STEEL)
    c.paint(c.line([(14, 2), (6, 10)]), STEEL2)
    c.layer(c.line([(6, 8), (9, 11)], 2), GOLD)
    c.layer(c.line([(4, 11), (2, 13)], 2), HILT)
    save(c, "icon_sword")
    # The bow, strung.
    c = Canvas(16, 16)
    c.layer(c.line([(10, 1), (13, 4), (14, 8), (13, 12), (10, 14)], 2), WOOD)
    c.paint(c.line([(10, 2), (13, 8), (10, 13)]), STRING, inside=False)
    c.paint(c.line([(3, 8), (13, 8)]), STEEL, inside=False)
    c.layer(c.poly([(1, 8), (4, 6), (4, 10)]), GOLD)
    save(c, "icon_bow")
    # An arrow, slanted.
    c = Canvas(16, 16)
    c.layer(c.line([(3, 12), (12, 3)], 1), WOOD)
    c.layer(c.poly([(10, 2), (14, 1), (13, 5)]), STEEL)
    c.layer(c.poly([(1, 11), (4, 11), (5, 12), (5, 15), (2, 13)]), FEATHER)
    save(c, "icon_arrow")
    # A brass coin with its square hole.
    c = Canvas(16, 16)
    coin = c.ellipse(1, 1, 14, 14)
    c.layer(coin, GOLD)
    c.paint(coin & c.rect(0, 9, 16, 16), GOLD2)
    c.paint(c.ellipse(3, 3, 12, 12) & ~c.ellipse(4, 4, 11, 11), GOLD2)
    c.paint(c.rect(6, 6, 9, 9), K)
    save(c, "icon_coin")
    # The purifying charm.
    c = Canvas(16, 16)
    c.layer(c.rect(4, 1, 11, 14), (246, 226, 150))
    c.paint(c.rect(6, 3, 9, 3) | c.rect(7, 4, 8, 11) | c.rect(5, 7, 10, 7) | c.rect(5, 11, 10, 11), RED)
    save(c, "icon_charm")
    # The ward charm: a little red bundle.
    c = Canvas(16, 16)
    c.layer(c.poly([(8, 1), (13, 6), (12, 14), (4, 14), (3, 6)]), RED)
    c.paint(c.rect(4, 6, 11, 7), GOLD)
    save(c, "icon_ward")
    # A straw sandal.
    c = Canvas(16, 16)
    c.layer(c.ellipse(4, 1, 11, 14), (214, 180, 110))
    for y in (4, 7, 10): c.paint(c.rect(5, y, 10, y), (168, 132, 72))
    save(c, "icon_sandal")
    # A small brush for "what to do".
    c = Canvas(16, 16)
    c.layer(c.line([(13, 2), (6, 9)], 2), WOOD)
    c.layer(c.poly([(6, 8), (8, 10), (4, 14), (2, 14), (2, 12)]), (34, 28, 34))
    save(c, "icon_goal")

def vignette():
    w, h = 160, 90
    Y, X = np.mgrid[:h, :w]
    d = np.hypot((X - w / 2 + .5) / (w / 2), (Y - h / 2 + .5) / (h / 2))
    alpha = np.clip((d - .62) / .78, 0, 1) ** 1.7 * 255
    img = np.zeros((h, w, 4), np.uint8); img[..., 3] = alpha.astype(np.uint8)
    Image.fromarray(img).save(f"{OUT}/vignette.png")

def seal():
    # A cinnabar stamp: square, the ink thinner at the edges and missing in a few places like a real impression.
    rng = np.random.default_rng(5)
    c = Canvas(14, 14)
    RED, RED2, RED3 = (196, 46, 36), (156, 30, 26), (222, 84, 62)
    c.paint(c.rect(0, 0, 13, 13), RED2, inside=False)
    c.paint(c.rect(1, 1, 12, 12), RED, inside=False)
    c.paint(c.rect(1, 1, 12, 1) | c.rect(1, 1, 1, 12), RED3, inside=False)
    for _ in range(9):
        x, y = int(rng.integers(0, 14)), int(rng.integers(0, 14))
        if x in (0, 13) or y in (0, 13): c.a[y, x] = False
        else: c.c[y, x] = RED2
    for x, y in ((0, 0), (13, 0), (0, 13), (13, 13)): c.a[y, x] = False
    save(c, "seal")

def dancheong():
    # The painted band under the eaves: red, a green and a blue panel with white rims and a yellow bead, black lines.
    c = Canvas(16, 6)
    RED, GREEN, BLUE, WHITE, YELLOW = (178, 52, 40), (58, 140, 96), (52, 104, 168), (236, 230, 210), (238, 196, 72)
    c.paint(c.rect(0, 0, 15, 5), RED, inside=False)
    c.paint(c.rect(0, 0, 15, 0) | c.rect(0, 5, 15, 5), K, inside=False)
    c.paint(c.rect(1, 1, 6, 4), WHITE, inside=False); c.paint(c.rect(2, 2, 5, 3), GREEN, inside=False)
    c.paint(c.rect(9, 1, 14, 4), WHITE, inside=False); c.paint(c.rect(10, 2, 13, 3), BLUE, inside=False)
    c.paint(c.rect(3, 2, 4, 3) | c.rect(11, 2, 12, 3), YELLOW, inside=False)
    save(c, "dancheong")

def roller():
    # A scroll's roller seen end-on from the front: a dark wood rod with brass knobs above and below.
    c = Canvas(10, 26)
    WOOD, WOOD2, WOOD3 = (110, 66, 38), (76, 44, 26), (150, 98, 58)
    BRASS, BRASS2, BRASS3 = (214, 170, 84), (150, 108, 48), (246, 214, 138)
    rod = c.rect(2, 3, 7, 22)
    c.layer(rod, WOOD)
    c.paint(c.rect(6, 3, 7, 22), WOOD2); c.paint(c.rect(3, 3, 3, 22), WOOD3)
    for y0, y1 in ((0, 3), (22, 25)):
        knob = c.rect(1, y0, 8, y1)
        c.layer(knob, BRASS)
        c.paint(c.rect(7, y0, 8, y1), BRASS2); c.paint(c.rect(2, y0, 2, y1), BRASS3)
    save(c, "roller")

def brush():
    # One stroke of a dry brush: full and wet at the left, splitting into hairs towards the right.
    rng = np.random.default_rng(8)
    w, h = 120, 18
    a = np.zeros((h, w), bool)
    for y in range(h):
        d = abs(y - h / 2 + .5) / (h / 2)
        start = int(2 + 5 * d * d + rng.integers(0, 2))
        end = int(w - 4 - 30 * d ** 1.5 - rng.integers(0, 6))
        a[y, start:end] = True
        # Dry streaks: gaps that grow towards the end of the stroke.
        x = start + 40 + int(rng.integers(0, 20))
        while x < end:
            gap = int(rng.integers(1, 3 + (x - start) // 25))
            a[y, x:x + gap] = False
            x += gap + int(rng.integers(4, 14))
    img = np.zeros((h, w, 4), np.uint8); img[..., :3] = K; img[..., 3] = np.where(a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/brush.png")

paper(); wood(); night(); corner(); icons(); vignette()
seal(); dancheong(); roller(); brush()
