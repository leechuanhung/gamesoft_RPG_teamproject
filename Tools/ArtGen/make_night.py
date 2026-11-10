# The night at Kongjwi's house, as full-screen illustrations (640 x 360, one scene is one screen). This is a cutscene,
# nothing here is played in, so it is painted at a far finer grain than the sprites (hqlib): hue-shifted ramps, a light
# field per scene, ordered dithering and one adaptive palette per scene.
#   18_Night/ext_<evening,night,morning>   the house from the road: sunset, moonlight, dawn
#   18_Night/ext_window, ext_clouds, ext_mist, ext_rooster_0/1, fig_seonbi, fig_kongjwi, fig_patjwi   layers on them
#   18_Night/room_<night,morning>          the guest room, its door panes holes; door_<night,morning> is the paper behind
#   18_Night/room_moon                     the moonlight on the floor, faded out while the shape blocks it
#   18_Night/shadow_0..2                   the shape thrown on the paper: a toad's head on a great jar, an arm that feeds it
#   18_Night/bed_<lie,prop,sit>_<night,morning>, quilt_<...>   him in the bedding, the quilt up to his neck or at his lap
#   18_Night/close_door, close_shadow, close_hole_0..2, close_eye_0..2   the close shot of the door
#   18_Night/hole_mark                     the hole he finds in the paper next morning, with its sunbeam
#   18_Night/stare                         Kongjwi's stare: the user's picture, fitted to the screen (stare() is no longer used)
# Usage: python make_night.py Assets/Resources/JinaV4/18_Night
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from hqlib import *
OUT = sys.argv[1]
W, H = 640, 360


# ---------------------------------------------------------------- the house from the road

def sky(c, T):
    if T.kind == "night":
        ramp = [(6, 8, 26), (9, 13, 38), (13, 20, 54), (19, 30, 72), (28, 44, 92), (40, 62, 112), (58, 84, 132), (84, 112, 152), (122, 146, 172)]
        v = c.grad(0, 0, 0, 250) * .55 + c.radial(486, 78, 300) ** 1.6 * .62
    elif T.kind == "morning":
        ramp = [(92, 120, 170), (120, 142, 184), (152, 164, 192), (188, 182, 192), (222, 196, 182), (242, 208, 168), (252, 222, 160), (255, 236, 176), (255, 248, 214)]
        v = c.grad(0, 0, 0, 236) * .72 + c.radial(500, 214, 240) ** 1.4 * .5
    else:
        ramp = [(44, 36, 92), (70, 46, 110), (106, 58, 120), (150, 70, 116), (196, 86, 100), (232, 112, 82), (248, 150, 74), (254, 192, 96), (255, 226, 150)]
        v = c.grad(0, 0, 0, 240) * .66 + c.radial(520, 222, 260) ** 1.3 * .55
    c.paint(c.rect(0, 0, W, H), ramp, v, d=.9, lit=False)
    clouds = noise(W, H, 190, 26, 11, 4)
    streak = np.clip((clouds - .56) * 6, 0, 1) * (c.Y < 200)
    if T.kind == "night":
        # Stars, more where the sky is darkest; none near the moon.
        rng = np.random.default_rng(5); bright = np.array([(150, 160, 190), (200, 206, 224), (240, 240, 250)])
        for _ in range(170):
            x, y = int(rng.random() * W), int(rng.random() * 190); b = rng.integers(0, 3)
            if np.hypot(x - 486, y - 78) > 76: c.rgb[y, x] = bright[b]
        for x, y in ((64, 36), (212, 22), (330, 58), (120, 92), (600, 30)):
            c.rgb[y, x] = (255, 255, 255)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): c.rgb[y + dy, x + dx] = (150, 162, 200)
        c.tint(streak * .22, (70, 90, 130))
        c.tint(c.radial(486, 78, 120) ** 2 * .35, (150, 170, 200))
        disc = c.circle(486, 78, 34)
        maria = noise(W, H, 26, 26, 21, 3)
        c.paint(disc, [(150, 166, 188), (178, 192, 206), (206, 216, 222), (232, 236, 232), (248, 248, 238)], np.clip(.95 - (maria - .35) * 1.6 - c.grad(460, 60, 516, 104) * .18, 0, 1), d=.7, lit=False)
    elif T.kind == "morning":
        c.tint(streak * .5, (255, 226, 198))
        c.tint(c.radial(500, 214, 110) ** 2 * .7, (255, 246, 210))
        c.paint(c.circle(500, 214, 22), [(255, 236, 170), (255, 248, 210), (255, 255, 236)], .8, lit=False)
    else:
        c.tint(streak * .6, (120, 56, 96))
        c.tint(np.clip((noise(W, H, 150, 16, 31, 3) - .6) * 6, 0, 1) * (c.Y < 214) * (c.Y > 90) * .55, (254, 170, 110))
        c.tint(c.radial(520, 222, 110) ** 2 * .75, (255, 226, 140))
        c.paint(c.circle(520, 222, 24), [(255, 196, 96), (255, 220, 130), (255, 240, 176)], .7, lit=False)


def ridge(c, T, base, height, seed, ramp, haze):
    """One mountain range, its colour fading into the sky toward the foot."""
    n = noise(W, 1, 150, 1, seed, 4)[0]
    top = base - height * n
    m = c.Y >= top[c.X]
    v = (c.Y - top[c.X]) / 90
    tex = noise(W, H, 40, 20, seed + 1, 3)
    if T.kind == "night": col = [(14, 22, 48), (18, 30, 60), (24, 40, 74), (32, 52, 88)]
    elif T.kind == "morning": col = [(120, 124, 158), (146, 146, 170), (176, 170, 178), (206, 192, 182)]
    else: col = [(52, 36, 88), (76, 46, 96), (108, 58, 100), (146, 74, 96)]
    if haze < .5: col = [tuple(np.array(x) * .72) for x in col]
    c.paint(m, col, np.clip(haze * .5 + v * .55 + (tex - .5) * .35, 0, 1), d=.8, lit=False)


def tiled_roof(c, T, x0, x1, eave_y, ridge_y, droop=10):
    """A hipped-and-gabled tile roof: the eaves lift at both ends, the ridge rises at its tips."""
    mid = (x0 + x1) / 2; ramp = T(TILE)
    eave = curve((x0 - 16, eave_y - 16), (mid, eave_y + droop + 4), (x1 + 16, eave_y - 16), 40)
    rid = curve((x0 + 58, ridge_y - 7), (mid, ridge_y + 5), (x1 - 58, ridge_y - 7), 30)
    shape = c.poly(eave + list(reversed(rid)))
    sh = c.poly([(x0 + 2, eave_y - 8), (x1 - 2, eave_y - 8), (x1 + 2, eave_y + 20), (x0 - 2, eave_y + 20)])
    c.paint(sh & ~shape, T(WOOD), .1, lit=True)
    # Under-eave rafters catching a little light.
    for x in range(int(x0) + 4, int(x1), 9):
        c.paint(c.rect(x, eave_y + 2, x + 3, eave_y + 14) & ~shape, T(WOOD), .32)
    base = .3 + c.grad(mid, ridge_y, mid, eave_y) * .35
    c.paint(shape, ramp, base, d=.6)
    # Rows of round tiles run down the slope, fanning a little toward the ends.
    for i in range(-42, 43):
        tx = mid + i * (x1 - x0 + 30) / 84
        top_x = mid + i * (x1 - x0 - 116) / 84
        y = eave_y + droop * (1 - ((tx - mid) / ((x1 - x0) / 2 + 16)) ** 2) - 4
        ln = c.line([(top_x, ridge_y), (tx, y)], 1) & shape
        c.paint(ln, ramp, base * .55, d=.3)
        ln2 = c.line([(top_x + 2, ridge_y), (tx + 2, y)], 1) & shape
        c.paint(ln2, ramp, base * 1.25, d=.3)
    # End tiles along the eave, the ridge cap with its lifted ends.
    for i in range(-42, 43):
        tx = mid + i * (x1 - x0 + 30) / 84
        y = eave_y + droop * (1 - ((tx - mid) / ((x1 - x0) / 2 + 16)) ** 2) - 16 * (abs(tx - mid) / ((x1 - x0) / 2 + 16)) ** 3 + 0
        c.paint(c.circle(tx, y - 1, 2), ramp, .62, d=.2)
    cap = c.line(rid, 7)
    c.paint(cap, ramp, .34, d=.4)
    c.paint(c.line([(p[0], p[1] - 3) for p in rid], 1), ramp, .85, d=.3)
    for sx, tip in ((rid[0], -1), (rid[-1], 1)):
        c.paint(c.poly([(sx[0], sx[1] + 3), (sx[0] + tip * 9, sx[1] - 9), (sx[0] + tip * 12, sx[1] - 7), (sx[0] + tip * 3, sx[1] + 5)]), ramp, .42, d=.3)
    return shape


def paper_door(c, T, x0, y0, x1, y1, glow=None, bars=6):
    """A latticed paper door seen from outside (lit warm from within if glow is given)."""
    c.paint(c.rect(x0, y0, x1, y1), T(WOOD), .28)
    paper = c.rect(x0 + 3, y0 + 3, x1 - 3, y1 - 3)
    c.paint(paper, T(PAPER) if glow is None else glow, .62 if glow is None else .7, d=.4, lit=glow is None)
    for i in range(1, bars):
        x = x0 + 3 + (x1 - x0 - 6) * i // bars
        c.paint(c.rect(x, y0 + 3, x, y1 - 3), T(WOOD), .3)
    for f in (.14, .18, .5, .54, .82, .86):
        y = int(y0 + (y1 - y0) * f); c.paint(c.rect(x0 + 3, y, x1 - 3, y), T(WOOD), .3)


def house(c, T, with_window=False):
    """The tile-roofed house on its stone terrace, left of centre."""
    x0, x1, floor = 96, 392, 262
    # Stone foundation.
    terrace = c.rect(x0 - 14, floor, x1 + 14, floor + 16)
    c.paint(terrace, T(STONE), .36 + (noise(W, H, 14, 6, 41, 2) - .5) * .7, d=.6)
    for y in (floor + 5, floor + 11):
        c.paint(c.rect(x0 - 14, y, x1 + 14, y), T(STONE), .12, d=.2)
    for i, x in enumerate(range(x0 - 14, x1 + 14, 17)):
        y = floor + (0 if i % 2 else 5); c.paint(c.rect(x, y, x, y + 5), T(STONE), .12, d=.2)
    c.paint(c.rect(x0 - 14, floor, x1 + 14, floor), T(STONE), .8, d=.2)
    # Plaster walls and doors between posts.
    wall = c.rect(x0, 170, x1, floor - 1)
    c.paint(wall, T(PLASTER), .34 + c.grad(0, 170, 0, floor) * .4 + (noise(W, H, 26, 20, 42, 2) - .5) * .06, d=.4)
    c.paint(c.rect(x0, 170, x1, 188), T(WOOD), .1, d=.3)
    c.paint(c.rect(x0, 238, x1, floor - 1), T(WOOD), .24)
    bays = [x0 + i * (x1 - x0) // 5 for i in range(6)]
    for i in range(5):
        a, b = bays[i] + 8, bays[i + 1] - 4
        if i == 3:
            paper_door(c, T, a, 188, b, 246, glow=[(190, 120, 60), (226, 156, 76), (248, 192, 104), (255, 224, 150)] if False else None)
        elif i in (1, 2):
            paper_door(c, T, a, 188, b, 246)
    for x in bays:
        post = c.rect(x - 4, 176, x + 4, floor - 1)
        c.paint(post, T(WOOD), .28 + c.grad(x - 4, 0, x + 4, 0) * .22 + (noise(W, H, 3, 26, 43) - .5) * .18, d=.5)
    # Wooden veranda.
    deck = c.rect(x0 - 8, 252, x1 + 8, floor)
    c.paint(deck, T(WOOD), .44 + (noise(W, H, 40, 2, 44) - .5) * .3, d=.5)
    c.paint(c.rect(x0 - 8, 252, x1 + 8, 252), T(WOOD), .8, d=.2)
    for x in bays: c.paint(c.rect(x - 2, floor + 1, x + 2, floor + 15), T(WOOD), .22)
    tiled_roof(c, T, x0 - 6, x1 + 6, 168, 96)


def wall_of_stones(c, T, y0, y1, x0, x1, seed):
    """Dry-stone wall (doldam) with a little tiled cap."""
    rng = np.random.default_rng(seed); ramp = T(STONE)
    c.paint(c.rect(x0, y0, x1, y1), ramp, .08, d=.2)
    y = y0 + 4
    while y < y1:
        x = x0 - rng.integers(0, 12)
        while x < x1:
            w, h = rng.integers(10, 22), rng.integers(7, 11)
            m = c.ellipse(x, y, x + w, y + h)
            v = .34 + rng.random() * .28
            c.paint(m, ramp, v + c.grad(x, y, x, y + h) * -.26, d=.5)
            c.paint(c.edge_top(m) & m, ramp, v + .3, d=.2)
            x += w - 1
        y += 7
    capr = T(TILE)
    c.paint(c.rect(x0 - 2, y0 - 3, x1 + 2, y0 + 3), capr, .3, d=.4)
    c.paint(c.rect(x0 - 2, y0 - 4, x1 + 2, y0 - 4), capr, .7, d=.2)
    for x in range(x0, x1, 5): c.paint(c.rect(x, y0 - 3, x, y0 + 3), capr, .12, d=.1)


def jar(c, T, cx, base, w, h, lid=True):
    """An onggi crock: wide shoulder, narrow foot, a lid, glaze catching the light."""
    ramp = T(ONGGI); top = base - h
    shoulder = top + h * .3
    pts = curve((cx - w * .3, base), (cx - w * .68, shoulder + h * .1), (cx - w * .42, top + 3), 16)
    pts += curve((cx + w * .42, top + 3), (cx + w * .68, shoulder + h * .1), (cx + w * .3, base), 16)
    m = c.poly(pts)
    v = .22 + c.radial(cx + w * .16, shoulder, w * .66) * .6
    c.paint(m, ramp, v, d=.6)
    c.paint(m & c.radial(cx + w * .22, shoulder - h * .1, w * .07) .__gt__(.01), ramp, .9, d=.4)
    c.paint(m & c.rect(cx - w, base - 2, cx + w, base), ramp, .08, d=.2)
    # Rim and lid.
    rim = c.ellipse(cx - w * .36, top - 1, cx + w * .36, top + 5)
    c.paint(rim, ramp, .42, d=.3)
    if lid:
        cap = c.ellipse(cx - w * .34, top - 6, cx + w * .34, top + 3)
        c.paint(cap, ramp, .36 + c.radial(cx - w * .1, top - 4, w * .3) * .36, d=.5)
        c.paint(c.ellipse(cx - 3, top - 9, cx + 3, top - 5), ramp, .5, d=.3)
    return m


def tree(c, T, root, seed=3):
    """The persimmon tree: dark limbs, sparse leaves, bright fruit."""
    rng = np.random.default_rng(seed); limbs = []
    def grow(x, y, angle, length, width):
        ex, ey = x + np.cos(angle) * length, y - np.sin(angle) * length
        limbs.append(((x, y), (ex, ey), width))
        if width <= 1 or length < 10:
            tips.append((ex, ey)); return
        for spread in (-.5 - rng.random() * .3, .4 + rng.random() * .4):
            grow(ex, ey, angle + spread, length * (.68 + rng.random() * .14), max(1, width - 2))
    tips = []
    grow(root[0], root[1], np.pi / 2 + .1, 46, 9)
    ramp = T(BARK)
    for tip in tips:
        lm = c.circle(tip[0] + rng.normal() * 3, tip[1] + rng.normal() * 3, 7 + rng.integers(0, 6))
        tex = noise(W, H, 5, 5, seed + 9, 2)
        c.paint(lm & (tex > .46), T(LEAF), .36 + (tex - .5) * 1.1 + c.grad(0, tip[1] - 8, 0, tip[1] + 10) * -.26, d=.8)
    for a, b, w in limbs:
        c.paint(c.line([a, b], int(w)), ramp, .35, d=.4)
        c.paint(c.line([(a[0] - w / 3, a[1]), (b[0] - w / 3, b[1])], 1), ramp, .75, d=.3)
    for tip in tips:
        if rng.random() < .85:
            fx, fy = tip[0] + rng.normal() * 6, tip[1] + rng.normal() * 5 + 3
            c.paint(c.circle(fx, fy, 3), T(PERSIMMON), .62 + c.grad(fx - 2, fy - 2, fx + 2, fy + 2) * -.42, d=.5)
            c.paint(c.rect(fx - 1, fy - 2, fx - 1, fy - 2), T(PERSIMMON), 1, d=0)
            c.paint(c.rect(fx - 1, fy - 4, fx + 1, fy - 4), T(LEAF), .3, d=0)


def fields(c, T, top):
    """Bean rows running off toward the mountains, and the road in front."""
    y = top
    ground = c.rect(0, top, W, H)
    c.paint(ground, T(EARTH), .3 + c.grad(0, top, 0, H) * .22 + (noise(W, H, 60, 12, 51, 3) - .5) * .3, d=.8)
    tex = noise(W, H, 4, 3, 52, 2)
    row = 0
    while y < 302:
        step = 3 + row * 1.15
        band = c.rect(400, y, W, y + step - 1)
        c.paint(band & (tex > .36 - row * .012), T(LEAF), .34 + (tex - .5) * 1.1 + row * .025, d=.9)
        y += step + 1 + row * .32; row += 1
    road = c.poly([(0, 306), (W, 296), (W, H), (0, H)])
    c.paint(road, T(EARTH), .46 + c.grad(0, 296, 0, H) * .16 + (noise(W, H, 40, 5, 53, 3) - .5) * .4, d=.9)
    rng = np.random.default_rng(54)
    for _ in range(90):
        x, yy = rng.integers(0, W), rng.integers(298, H)
        if road[yy, x]: c.paint(c.rect(x, yy, x + rng.integers(1, 3), yy), T(EARTH), .2 if rng.random() < .5 else .72, d=.1)


def grass(c, T, seed, x0=0, x1=W, top=318):
    """Foreground tufts dark against the road."""
    rng = np.random.default_rng(seed); ramp = T(LEAF)
    for _ in range(190):
        x = rng.integers(x0, x1); y = H - rng.integers(0, 26); h = rng.integers(5, 18)
        if rng.random() < .35: h += rng.integers(0, 12)
        lean = rng.normal() * 3
        c.paint(c.line([(x, y), (x + lean / 2, y - h / 2), (x + lean, y - h)], 1), ramp, .1 + rng.random() * .32, d=.3)


def exterior(kind):
    T = Theme(kind); c = Art(W, H)
    c.light[:] = {"night": .86, "morning": 1.0, "evening": .92}[kind]
    # Light slants from the moon or sun on the right, so forms brighten toward it.
    c.light *= .82 + .3 * c.grad(0, 0, W, 0)
    sky(c, T)
    ridge(c, T, 236, 96, 61, None, .95)
    ridge(c, T, 244, 60, 62, None, .62)
    ridge(c, T, 252, 34, 63, None, .25)
    fields(c, T, 246)
    wall_of_stones(c, T, 244, 268, 400, W, 70)
    tree(c, T, (62, 268))
    house(c, T)
    wall_of_stones(c, T, 268, 300, 0, 110, 71)
    c.paint(c.rect(0, 298, 118, 306), T(EARTH), .3, d=.5)
    for cx, b, w, h in ((30, 318, 48, 52), (78, 322, 56, 62), (128, 330, 42, 44), (6, 332, 44, 44), (58, 342, 38, 34), (104, 346, 30, 26)):
        jar(c, T, cx, b, w, h)
    grass(c, T, 81)
    # Grading passes.
    if kind == "night":
        c.tint(c.grad(0, 200, 0, H) * .25, (6, 8, 20))
        c.tint(np.clip(1 - np.hypot((c.X - W / 2) / (W * .66), (c.Y - H / 2) / (H * .72)), 0, 1) * 0 + np.clip(np.hypot((c.X - W / 2) / (W * .62), (c.Y - H / 2) / (H * .66)) - .62, 0, 1) * 1.1, (2, 3, 10))
    elif kind == "morning":
        c.glow(c.radial(500, 214, 300) ** 2, (70, 44, 14), .8)
        c.tint(np.clip(np.hypot((c.X - W / 2) / (W * .62), (c.Y - H / 2) / (H * .66)) - .74, 0, 1) * .8, (60, 40, 50))
    else:
        c.glow(c.radial(520, 222, 320) ** 2, (86, 34, 4), .9)
        c.tint(np.clip(np.hypot((c.X - W / 2) / (W * .62), (c.Y - H / 2) / (H * .66)) - .66, 0, 1) * 1.1, (36, 14, 40))
    return c


def ext_window():
    """The guest room's lit door on the moonlit house, with its glow on the veranda: switched off when he puts the lamp out."""
    c = Art(W, H)
    a, b, y0, y1 = 96 + 3 * 296 // 5 + 8, 96 + 4 * 296 // 5 - 4, 188, 246
    c.paint(c.rect(a + 3, y0 + 3, b - 3, y1 - 3), [(196, 116, 48), (232, 158, 66), (250, 196, 96), (255, 226, 146)], .5 + c.radial((a + b) / 2, (y0 + y1) / 2, 40) * .5, d=.8, lit=False)
    for i in range(1, 6):
        x = a + 3 + (b - a - 6) * i // 6; c.flat(c.rect(x, y0 + 3, x, y1 - 3), (70, 40, 34))
    for f in (.14, .18, .5, .54, .82, .86):
        y = int(y0 + (y1 - y0) * f); c.flat(c.rect(a + 3, y, b - 3, y), (70, 40, 34))
    # A pool of warm light on the veranda boards.
    pool = c.rect(a - 24, 252, b + 24, 262) & (c.bayer + .5 < c.radial((a + b) / 2, 252, 56, .4) * 1.2)
    c.flat(pool, (150, 96, 56))
    return c


def clouds():
    """Thin clouds to drift over the moon, twice the screen wide; only their solid parts are painted."""
    c = Art(W * 2, 200)
    n = noise(W * 2, 200, 170, 22, 91, 4)
    v = np.clip((n - .53) * 5.5, 0, 1) * np.clip(1 - c.Y / 190, 0, 1) * np.clip(c.Y / 26, 0, 1)
    m = v > .18 + (c.bayer + .5) * .26
    c.paint(m, [(26, 36, 70), (40, 54, 92), (62, 80, 118), (96, 114, 150)], np.clip(v * 1.1, 0, 1), d=.7, lit=False)
    return c


def mist():
    """Morning mist lying over the fields."""
    c = Art(W * 2, H)
    n = noise(W * 2, H, 230, 18, 95, 4)
    v = np.clip((n - .5) * 3.2, 0, 1) * np.clip((c.Y - 264) / 12, 0, 1) * np.clip((300 - c.Y) / 22, 0, 1)
    m = v > .22 + (c.bayer + .5) * .78
    c.flat(m, (255, 240, 218))
    return c


def rooster(frame):
    """On the wall at the right, its head thrown back on the crow."""
    c = Art(W, H)
    x, y = 566, 243
    ramp = [(26, 18, 24), (50, 30, 30), (92, 46, 34), (150, 76, 40), (206, 120, 56)]
    body = c.poly(curve((x - 14, y - 12), (x - 4, y + 3), (x + 12, y - 8), 12) + [(x + 14, y - 16), (x + 6, y - 19), (x - 6, y - 18)])
    c.paint(body, ramp, .34 + c.grad(x, y - 18, x + 12, y - 2) * .3, d=.6, lit=False)
    # Tail plumes arc up and back.
    for i, (dx, dy) in enumerate(((-30, -24), (-33, -14), (-30, -6))):
        c.paint(c.line(curve((x - 12, y - 12), (x - 22, y - 30 + i * 6), (x + dx, y + dy), 10), 2), [(20, 30, 44), (36, 52, 62), (60, 90, 80)], .35 + i * .2, d=.3, lit=False)
    # Legs.
    for dx in (-3, 4): c.paint(c.rect(x + dx, y - 2, x + dx, y + 3), ramp, .7, d=0, lit=False)
    if frame == 0:
        neck = [(x + 6, y - 17), (x + 18, y - 28), (x + 24, y - 22), (x + 14, y - 8)]
        head = (x + 21, y - 28); beak = [(x + 26, y - 30), (x + 33, y - 27), (x + 26, y - 25)]
        comb = [(x + 16, y - 33), (x + 19, y - 38), (x + 21, y - 34), (x + 24, y - 38), (x + 26, y - 31)]
    else:
        neck = [(x + 6, y - 17), (x + 14, y - 32), (x + 21, y - 30), (x + 14, y - 8)]
        head = (x + 19, y - 35); beak = [(x + 22, y - 41), (x + 31, y - 46), (x + 26, y - 37), (x + 32, y - 34), (x + 23, y - 34)]
        comb = [(x + 11, y - 37), (x + 11, y - 43), (x + 15, y - 41), (x + 16, y - 46), (x + 21, y - 40)]
    c.paint(c.poly(neck), ramp, .62, d=.5, lit=False)
    c.paint(c.circle(head[0], head[1], 5), ramp, .6, d=.5, lit=False)
    c.paint(c.poly(comb), [(120, 24, 30), (196, 42, 40)], .8, d=0, lit=False)
    c.paint(c.poly(beak), [(160, 110, 40), (230, 180, 70)], .7, d=0, lit=False)
    c.flat(c.rect(head[0] + 1, head[1] - 1, head[0] + 1, head[1] - 1), (10, 8, 10))
    return c


# ---------------------------------------------------------------- figures on the road at dusk

def fig_base(c, x, base, kind, lean=0):
    """Backlit by the sunset on the right: dark local colour, a burning rim on the side toward the sun."""
    rim = (255, 182, 96)
    def shape(m, ramp, v=.3):
        c.paint(m, ramp, v + c.grad(x - 14, 0, x + 14, 0) * .2, d=.5, lit=False)
    D = Theme("evening")
    def dark(r): return [tuple(np.array(t) * .62) for t in D(r)]
    if kind == "seonbi":
        h = base - 92
        robe = c.poly([(x - 11, h + 21), (x + 11, h + 21), (x + 15, base - 7), (x + 13, base), (x - 13, base), (x - 15, base - 7)])
        shape(robe, dark(COTTON), .5)
        c.paint(c.poly([(x - 2, h + 40), (x + 4, h + 40), (x + 6, base - 12), (x + 1, base - 12)]), dark(INDIGO), .3, d=.2, lit=False)
        c.paint(c.rect(x - 11, h + 38, x + 11, h + 41), dark(INDIGO), .3, d=.2, lit=False)
        arm = c.poly([(x - 12, h + 22), (x - 5, h + 22), (x - 6, h + 52), (x - 14, h + 52)])
        shape(arm, dark(COTTON), .35)
        for dx in (-9, 3): c.paint(c.rect(x + dx, base - 2, x + dx + 8, base), dark(HAIR), .2, d=0, lit=False)
        c.paint(c.rect(x - 3, h + 14, x + 4, h + 21), dark(SKIN), .4, d=0, lit=False)
        head = c.ellipse(x - 7, h + 2, x + 8, h + 18); shape(head, dark(SKIN), .45)
        brim = c.ellipse(x - 18, h + 2, x + 18, h + 7)
        crown = c.poly([(x - 7, h + 4), (x - 6, h - 9), (x + 6, h - 9), (x + 7, h + 4)])
        shape(crown, dark(HAIR), .4); shape(brim, dark(HAIR), .45)
        for m in (robe, head, crown, brim, arm):
            c.flat(c.edge_side(m, 1, 1), rim)
        c.flat(c.edge_top(brim), (255, 214, 150))
    else:
        h = base - 80
        kong = kind == "kongjwi"
        skirt_ramp, top_ramp, ribbon = (ROSE, OCHRE, (214, 80, 96)) if kong else (INDIGO, JADE, (196, 58, 58))
        skirt = c.poly([(x - 8, h + 32), (x + 8, h + 32), (x + 17, base), (x - 17, base)])
        shape(skirt, dark(skirt_ramp), .55)
        for dx in (-8, -2, 5, 10):
            c.paint(c.line([(x + dx * .55, h + 34), (x + dx, base - 1)]), dark(skirt_ramp), .2, d=.1, lit=False)
        top = c.poly([(x - 8, h + 16), (x + 8, h + 16), (x + 9, h + 32), (x - 9, h + 32)])
        shape(top, dark(top_ramp), .5)
        c.paint(c.line([(x, h + 17), (x + 3, h + 30)], 1), [(255, 240, 220)], .5, d=0, lit=False)
        arm_x = 3 if kong else -3
        arm = c.poly([(x + arm_x - 3, h + 17), (x + arm_x + 4, h + 17), (x + arm_x + 5, h + 38), (x + arm_x - 2, h + 38)])
        shape(arm, dark(top_ramp), .35)
        head = c.ellipse(x - 8, h, x + 8, h + 17)
        shape(head, dark(SKIN), .5)
        hair = c.ellipse(x - 9, h - 2, x + 9, h + 9) | c.poly([(x - 9, h + 4), (x - 3, h + 4), (x - 7, h + 14)])
        c.paint(hair, dark(HAIR), .35, d=.2, lit=False)
        braid = c.poly([(x - 10, h + 5), (x - 5, h + 5), (x - 7, h + 40), (x - 11, h + 40)])
        c.paint(braid, dark(HAIR), .3, d=.2, lit=False)
        c.flat(c.rect(x - 12, h + 39, x - 7, h + 45), ribbon)
        if kong:
            c.paint(c.circle(x - 7, h + 2, 3), BLOOM, .8, d=.3, lit=False)
        for m in (skirt, top, head, hair, arm):
            c.flat(c.edge_side(m, 1, 1), rim)
    return c


def figure(kind, x):
    c = Art(W, H)
    fig_base(c, x, 322, kind)
    # The long shadow reaching toward the viewer on the left.
    shadow = c.poly([(x - 12, 322), (x + 12, 322), (x - 58, 340), (x - 96, 340)])
    c.flat(shadow & (c.bayer < .22) & ~c.a, (44, 22, 40))
    return c


# ---------------------------------------------------------------- the guest room

# Where the door stands: everything about the paper and the lattice is measured from here.
DX0, DX1, DY0, DY1 = 424, 612, 40, 228
def door_panes(c):
    """The door's panes: two leaves of tall thin slits crossed by bands of rails (ttisal)."""
    m = c.blank()
    for leaf in ((DX0 + 8, (DX0 + DX1) // 2 - 3), ((DX0 + DX1) // 2 + 3, DX1 - 8)):
        for x in range(leaf[0], leaf[1] - 6, 9):
            m |= c.rect(x, DY0 + 8, min(x + 6, leaf[1]), DY1 - 8)
    for f0 in (.12, .47, .82):
        for i in range(3):
            y = int(DY0 + (DY1 - DY0) * f0) + i * 6
            m &= ~c.rect(DX0, y, DX1, y + 1)
    return m


def floor_lit(c, stretch=1.0):
    """Where the door's light falls on the floor: its panes projected down and to the left."""
    panes = door_panes(c)
    FLOOR_Y = 240
    lit = c.blank()
    for y in range(FLOOR_Y + 1, H):
        depth = (y - FLOOR_Y) / (H - FLOOR_Y)
        src_y = DY1 - (depth * (DY1 - DY0 - 16) * stretch + 8)
        if src_y < DY0: continue
        shift = 40 + depth * 210
        xs = (c.X[y] + shift * (1 - depth * .1)) / (1 + depth * .38) + (DX0 + DX1) / 2 * (depth * .38) / (1 + depth * .38)
        # Map a floor pixel back to the door.
        sx = np.round((c.X[y] + shift - (DX0 + DX1) / 2) / (1 + depth * .5) + (DX0 + DX1) / 2).astype(int)
        ok = (sx >= 0) & (sx < W)
        lit[y][ok] = panes[int(src_y), sx[ok]]
    return lit


def room_scene(T, moon_on=True):
    c = Art(W, H); night = T.kind == "night"
    FLOOR_Y = 240
    # Light: dim everywhere, strongest by the door; in the morning warm and full.
    dist = np.hypot((c.X - (DX0 + DX1) / 2) / 340, (c.Y - 150) / 280)
    base = np.clip(1 - dist, 0, 1)
    c.light[:] = (.74 + base * .62) if night else (.8 + base * .34)
    tex_fine = noise(W, H, 5, 5, 101, 2)
    # Wall.
    wall = c.rect(0, 0, W, FLOOR_Y)
    c.paint(wall, T(WALL), .62 + (noise(W, H, 90, 60, 102, 3) - .5) * .12, d=.4)
    # The wainscot of paper panels, darker below.
    c.paint(c.rect(0, 186, W, FLOOR_Y), T(WALL), .44 + (noise(W, H, 90, 40, 103, 3) - .5) * .1, d=.4)
    for x in range(26, 424, 44):
        c.paint(c.rect(x, 186, x, FLOOR_Y), T(WALL), .26, d=.3)
    c.paint(c.rect(0, 186, W, 186), T(WALL), .8, d=.2)
    # Ceiling shadow.
    c.paint(c.rect(0, 0, W, 22), T(WOOD), .16 + (noise(W, H, 60, 3, 104, 2) - .5) * .18, d=.5)
    c.paint(c.rect(0, 23, W, 34), T(WALL), .24, d=.4)
    # Floor: oiled paper sheets in perspective, sheen under the light.
    fl = c.rect(0, FLOOR_Y, W, H)
    depth = np.clip((c.Y - FLOOR_Y) / (H - FLOOR_Y), 0, 1)
    c.paint(fl, T(FLOOR), .5 + (noise(W, H, 110, 30, 105, 3) - .5) * .14 + depth * .1, d=.45)
    for i in range(-14, 16):
        x0 = W / 2 + i * 62; x1 = W / 2 + i * 104
        c.paint(c.line([(x0, FLOOR_Y), (x1, H)], 1) & fl, T(FLOOR), .3, d=.2)
    for y in (FLOOR_Y + 13, FLOOR_Y + 36, FLOOR_Y + 72):
        c.paint(c.rect(0, y, W, y), T(FLOOR), .3, d=.2)
    # Skirting board and beams.
    c.paint(c.rect(0, FLOOR_Y - 5, W, FLOOR_Y), T(WOOD), .26 + (noise(W, H, 50, 2, 106, 2) - .5) * .2, d=.4)
    c.paint(c.rect(0, FLOOR_Y + 1, W, FLOOR_Y + 2), T(FLOOR), .1, d=.2)
    beam = c.rect(0, 16, W, 30)
    c.paint(beam, T(WOOD), .3 + (noise(W, H, 90, 3, 107, 3) - .5) * .4, d=.5)
    c.paint(c.rect(0, 30, W, 32), T(WOOD), .04, d=.1)
    for x0, x1 in ((0, 14), (404, 420), (624, 639)):
        p = c.rect(x0, 30, x1, FLOOR_Y)
        c.paint(p, T(WOOD), .26 + c.grad(x0, 0, x1, 0) * .28 + (noise(W, H, 3, 60, 108 + x0, 2) - .5) * .28, d=.5)

    # The folding screen behind the bedding: silk borders, an ink landscape running over six panels.
    sx0, sx1, sy0, sy1 = 34, 262, 66, FLOOR_Y - 1
    panels = 6; pw = (sx1 - sx0) / panels
    land = noise(W, 1, 74, 1, 111, 3)[0]
    far = noise(W, 1, 40, 1, 112, 3)[0]
    for i in range(panels):
        a, b = int(sx0 + i * pw), int(sx0 + (i + 1) * pw) - 1
        lean = .10 if i % 2 else -.02
        c.paint(c.rect(a, sy0, b, sy1), T(INDIGO), .62 + lean, d=.3)
        c.paint(c.rect(a, sy0, b, sy0 + 1), T(WOOD), .6, d=.2)
        face = c.rect(a + 3, sy0 + 6, b - 3, sy1 - 11)
        c.paint(face, T(PAPER), .7 + lean + (tex_fine - .5) * .06, d=.4)
        c.paint(c.rect(b, sy0, b, sy1), T(WOOD), .12, d=.1)
    faces = c.blank()
    for i in range(panels):
        a, b = int(sx0 + i * pw), int(sx0 + (i + 1) * pw) - 1
        faces |= c.rect(a + 3, sy0 + 6, b - 3, sy1 - 11)
    ink = T(INK)
    h_far = 150 - far[c.X] * 58
    c.paint(faces & (c.Y > h_far), ink, .78 + (c.Y - h_far) / 300 + (tex_fine - .5) * .14, d=.9)
    h_near = 196 - land[c.X] * 80
    c.paint(faces & (c.Y > h_near), ink, .34 + (c.Y - h_near) / 110 + (tex_fine - .5) * .2, d=.9)
    # A pine and a pale moon on the painting; a column of calligraphy.
    for px, py in ((76, 180), (196, 170)):
        c.paint(c.line([(px, py + 24), (px + 3, py + 8), (px - 2, py)], 2) & faces, ink, .1, d=.2)
        for dx, dy, r in ((-8, 0, 7), (6, -3, 8), (-1, -8, 6), (10, 5, 5)):
            c.paint(c.ellipse(px + dx - r - 2, py + dy - r + 2, px + dx + r + 2, py + dy + r - 2) & faces, ink, .14, d=.5)
    c.paint(c.circle(150, 100, 11) & faces, T(PAPER), 1, d=0)
    for i in range(5):
        c.paint(c.rect(232, 84 + i * 11, 238, 91 + i * 11) & faces, ink, .1 + (i % 2) * .08, d=.7)

    # Clothes rack with his robe and the gat on a peg.
    rack_y = 74
    c.paint(c.rect(272, rack_y, 402, rack_y + 3), T(WOOD), .4, d=.3)
    for x in (272, 400): c.paint(c.rect(x, rack_y - 8, x + 2, rack_y + 3), T(WOOD), .25, d=.2)
    robe = c.poly([(278, rack_y + 3), (396, rack_y + 3), (398, rack_y + 30), (372, rack_y + 34), (376, rack_y + 112), (306, rack_y + 116), (308, rack_y + 34), (276, rack_y + 30)])
    folds = noise(W, H, 12, 90, 121, 2)
    c.paint(robe, T(COTTON), .66 + (folds - .5) * .55 - c.grad(0, rack_y, 0, rack_y + 116) * .2, d=.4)
    c.paint(c.line([(308, rack_y + 30), (276, rack_y + 30)], 1) | c.line([(372, rack_y + 34), (398, rack_y + 30)], 1), T(COTTON), .3, d=.1)
    c.paint(c.line([(340, rack_y + 4), (324, rack_y + 26), (342, rack_y + 112)], 1), T(COTTON), .32, d=.1)
    sash = c.poly([(346, rack_y + 4), (354, rack_y + 4), (358, rack_y + 96), (350, rack_y + 96)])
    c.paint(sash, T(INDIGO), .24, d=.3)
    gat_x, gat_y = 300, 58
    c.paint(c.rect(gat_x - 1, gat_y - 18, gat_x + 2, gat_y - 12), T(WOOD), .2, d=.1)
    crown = c.poly([(gat_x - 11, gat_y - 1), (gat_x - 9, gat_y - 15), (gat_x + 10, gat_y - 15), (gat_x + 12, gat_y - 1)])
    brim = c.ellipse(gat_x - 28, gat_y - 4, gat_x + 28, gat_y + 5)
    c.paint(brim, T(HAIR), .28 + c.grad(gat_x - 28, 0, gat_x + 28, 0) * .4, d=.5)
    c.paint(crown, T(HAIR), .2 + c.grad(gat_x - 9, 0, gat_x + 10, 0) * .5, d=.5)
    c.paint(c.line([(gat_x - 26, gat_y + 2), (gat_x + 26, gat_y + 2)], 1), T(HAIR), .78, d=.2)
    c.paint(c.line(curve((gat_x - 14, gat_y + 4), (gat_x, gat_y + 26), (gat_x + 14, gat_y + 4), 12), 1), T(OCHRE), .5, d=.3)

    # Writing table with stitched books, inkstone and an unlit lamp stand.
    tx0, tx1, ty = 278, 398, 214
    top = c.poly([(tx0 + 6, ty), (tx1 - 6, ty), (tx1, ty + 10), (tx0, ty + 10)])
    c.paint(top, T(WOOD), .5 + (noise(W, H, 50, 3, 122, 2) - .5) * .3, d=.5)
    c.paint(c.rect(tx0, ty + 11, tx1, ty + 15), T(WOOD), .24, d=.3)
    for x in (tx0 + 6, tx1 - 16):
        c.paint(c.rect(x, ty + 16, x + 9, FLOOR_Y + 22), T(WOOD), .3 + c.grad(x, 0, x + 9, 0) * .2, d=.3)
    c.paint(c.rect(tx0 + 16, ty + 16, tx1 - 16, ty + 18), T(WOOD), .16, d=.2)
    # Books.
    for i, (col, w) in enumerate(((INDIGO, 54), (OCHRE, 50), (PAPER, 56))):
        y = ty + 1 - i * 5; x = tx0 + 12 + i * 2
        c.paint(c.rect(x, y - 4, x + w, y), T(col), .45 + i * .08, d=.3)
        c.paint(c.rect(x + 2, y - 3, x + w, y - 1), T(PAPER), .7, d=.2)
        for sx in range(x + 3, x + 12, 4): c.paint(c.rect(sx, y - 4, sx, y), T(HAIR), .3, d=0)
    # Inkstone and brush.
    c.paint(c.ellipse(tx0 + 80, ty - 2, tx0 + 104, ty + 6), T(STONE), .2, d=.3)
    c.paint(c.ellipse(tx0 + 84, ty, tx0 + 100, ty + 4), T(HAIR), .25, d=.1)
    c.paint(c.line([(tx0 + 70, ty + 7), (tx0 + 112, ty + 3)], 1), T(WOOD), .6, d=.1)
    # Lamp on its tall stand, by the post.
    lx = 372
    c.paint(c.rect(lx - 1, 128, lx + 1, ty - 1), T(WOOD), .4, d=.2)
    c.paint(c.ellipse(lx - 10, 124, lx + 10, 133), T(BRASS), .4 + c.grad(lx - 10, 0, lx + 10, 0) * .3, d=.4)
    c.paint(c.rect(lx - 1, 116, lx + 1, 124), T(HAIR), .3, d=.1)

    # Bedding: the mattress lying in perspective.
    yo = c.poly([(22, 340), (336, 340), (304, 268), (58, 268)])
    side = c.poly([(22, 340), (336, 340), (336, 350), (22, 350)])
    c.paint(side, T(COTTON), .3, d=.4)
    c.paint(yo, T(COTTON), .52 + c.grad(0, 268, 0, 340) * .12 + (noise(W, H, 44, 14, 123, 2) - .5) * .12, d=.7)
    c.paint(c.line([(22, 340), (336, 340)], 1), T(COTTON), .85, d=.1)
    # Traditional bolster with an embroidered end.
    pil = c.ellipse(58, 256, 124, 292)
    c.paint(pil, T(COTTON), .46 + c.radial(84, 264, 46) * .36, d=.7)
    disc = c.ellipse(50, 258, 74, 292)
    c.paint(disc, T(ROSE), .44, d=.3)
    c.paint(c.ellipse(54, 263, 70, 287), T(OCHRE), .5, d=.3)
    c.paint(c.ellipse(58, 268, 66, 282), T(ROSE), .5, d=.2)

    # Moonlight or morning sun on the floor through the panes, and spilling on the skirting.
    if moon_on:
        lit = floor_lit(c) & (c.Y > FLOOR_Y)
        boost = lit & (c.bayer < .46)
        c.tint(lit * (.44 if night else .55), (150, 180, 236) if night else (255, 224, 150))
        c.tint(boost * .12, (190, 214, 250) if night else (255, 240, 190))
    # Door frame (panes are cut out at the end).
    frame = c.rect(DX0, DY0, DX1, DY1)
    c.paint(frame, T(WOOD), .32 + (noise(W, H, 4, 70, 131, 2) - .5) * .3, d=.5)
    mid = (DX0 + DX1) // 2
    c.paint(c.rect(mid - 3, DY0, mid + 3, DY1), T(WOOD), .22, d=.3)
    c.paint(c.rect(mid, DY0, mid, DY1), T(WOOD), .02, d=.1)
    c.paint(c.rect(DX0, DY0, DX1, DY0), T(WOOD), .7, d=.2)
    # Rails catch the light from behind on their lower edges.
    panes = door_panes(c)
    edge = c.ring(panes) & frame
    c.paint(edge, T(WOOD), .5 if night else .78, d=.3)
    # Brass door ring and threshold.
    for x in (mid - 9, mid + 7):
        c.paint(c.ellipse(x - 3, 134, x + 5, 142), T(BRASS), .55, d=.3)
    c.paint(c.rect(DX0 - 4, DY1 + 1, DX1 + 4, FLOOR_Y + 3), T(WOOD), .36, d=.3)
    c.paint(c.rect(DX0 - 4, DY1 + 1, DX1 + 4, DY1 + 1), T(WOOD), .85, d=.1)

    # Grading.
    v = np.clip(np.hypot((c.X - W / 2) / (W * .6), (c.Y - H / 2) / (H * .62)) - .56, 0, 1)
    c.tint(v * (1.2 if night else .6), (4, 5, 14) if night else (70, 44, 40))
    if not night:
        c.glow(c.radial((DX0 + DX1) / 2, 160, 380) ** 2, (46, 30, 8), 1)
    c.erase(panes)
    return c


def door_paper(T):
    """The paper behind the lattice: moon-blue or sun-gold, brightest toward the top, with fibres."""
    c = Art(W, H); night = T.kind == "night"
    ramp = [(58, 74, 124), (84, 106, 156), (116, 140, 186), (150, 172, 210), (186, 204, 230), (216, 228, 244)] if night else \
           [(232, 170, 92), (246, 196, 112), (252, 216, 140), (255, 232, 170), (255, 244, 204), (255, 252, 232)]
    m = c.rect(DX0, DY0, DX1, DY1)
    v = .32 + c.radial(DX1 - 30, DY0 + 30, 230) * .6 + (noise(W, H, 60, 50, 141, 3) - .5) * .12
    c.paint(m, ramp, v, d=.8, lit=False)
    rng = np.random.default_rng(142)
    for _ in range(140):
        x, y = rng.integers(DX0, DX1), rng.integers(DY0, DY1)
        a = rng.random() * 3.14; l = rng.integers(3, 8)
        ln = c.line([(x, y), (x + np.cos(a) * l, y + np.sin(a) * l)], 1) & m
        c.rgb[ln] += (10, 12, 16) if night else (6, 6, 4)
    return c


def shadow(frame):
    """The shape on the paper: a great jar with a toad's head wedged in its mouth, thin arms, one feeding it."""
    SW, SH = 240, 200
    c = Art(SW, SH)
    cx = 120
    body = c.poly(curve((cx - 42, SH), (cx - 104, 106), (cx - 46, 72), 20) + curve((cx + 46, 72), (cx + 104, 106), (cx + 42, SH), 20))
    neck = c.rect(cx - 40, 62, cx + 40, 80)
    head = c.ellipse(cx - 46, 20, cx + 46, 74)
    lift = (0, 0, -3)[frame]
    eyes = c.circle(cx - 28, 20 + lift, 15) | c.circle(cx + 28, 20 + lift, 15)
    mouth_y = 66
    arm_l = c.line(curve((cx - 72, 96), (cx - 112, 128), (cx - 96, 168), 12), 7) | c.line([(cx - 96, 168), (cx - 108, 178)], 3) | c.line([(cx - 96, 168), (cx - 96, 182)], 3) | c.line([(cx - 96, 168), (cx - 86, 180)], 3)
    if frame == 0:
        arm_r = c.line(curve((cx + 72, 96), (cx + 112, 128), (cx + 96, 168), 12), 7) | c.line([(cx + 96, 168), (cx + 108, 178)], 3) | c.line([(cx + 96, 168), (cx + 96, 182)], 3) | c.line([(cx + 96, 168), (cx + 86, 180)], 3)
        extra = c.blank()
    elif frame == 1:
        arm_r = c.line(curve((cx + 72, 96), (cx + 114, 100), (cx + 50, mouth_y + 8), 12), 7)
        extra = c.line([(cx + 50, mouth_y + 8), (cx + 34, mouth_y - 2)], 4) | c.line([(cx + 40, mouth_y + 2), (cx + 30, mouth_y + 10)], 3)
    else:
        arm_r = c.line(curve((cx + 72, 96), (cx + 112, 110), (cx + 58, mouth_y + 14), 12), 7)
        extra = c.line([(cx + 58, mouth_y + 14), (cx + 42, mouth_y + 12)], 3)
    shape = body | neck | head | eyes | arm_l | arm_r | extra
    # The soft edge is dithered away from the solid centre.
    solid = shape.copy()
    for _ in range(3):
        grown = c.ring(solid) | solid
        solid = grown
    fringe = solid & ~shape
    keep = shape | (fringe & (c.bayer < -.05))
    c.flat(keep, (255, 255, 255))
    return c


def head_pose(b, T, hx, hy, closed=False):
    """His head in profile looking right: a young man's smooth cheek and small chin, net headband, hair up in a topknot."""
    skin, hair = T(SKIN), T(HAIR)
    neck = b.poly([(hx - 6, hy + 8), (hx + 6, hy + 8), (hx + 8, hy + 26), (hx - 8, hy + 26)])
    b.paint(neck, skin, .34 + b.grad(hx - 6, 0, hx + 7, 0) * .2 + b.grad(0, hy + 12, 0, hy + 24) * .18, d=.3)
    skull = b.ellipse(hx - 15, hy - 17, hx + 13, hy + 13)
    b.paint(skull, skin, .5 + b.radial(hx + 8, hy - 8, 22) * .4, d=.4)
    face = b.poly([(hx + 4, hy - 17), (hx + 12, hy - 14), (hx + 14, hy - 7), (hx + 15, hy - 4), (hx + 18, hy), (hx + 18, hy + 1), (hx + 15, hy + 3), (hx + 15, hy + 7), (hx + 13, hy + 11), (hx + 9, hy + 14), (hx + 3, hy + 14), (hx - 2, hy + 10), (hx - 3, hy - 12)])
    b.paint(face, skin, .68 + b.grad(hx - 2, 0, hx + 18, 0) * .26, d=.4)
    # A touch of colour high on the cheek.
    b.flat(b.ellipse(hx + 3, hy + 1, hx + 11, hy + 7) & (b.bayer < -.12) & face, tuple(np.array(skin[4]) * (1, .86, .84)))
    # Hair: combed back to the crown; the headband runs from the brow behind the ear.
    cap = b.poly([(hx - 16, hy + 5), (hx - 16, hy - 11), (hx - 7, hy - 19), (hx + 6, hy - 20), (hx + 13, hy - 13), (hx + 2, hy - 10), (hx - 4, hy - 1), (hx - 5, hy + 7)])
    b.paint(cap, hair, .24 + b.radial(hx - 1, hy - 16, 13) * .6, d=.4)
    b.paint(b.line([(hx - 15, hy - 3), (hx + 13, hy - 11)], 3), hair, .5, d=.15)
    knot = b.poly([(hx - 7, hy - 18), (hx - 6, hy - 31), (hx - 1, hy - 36), (hx + 4, hy - 32), (hx + 4, hy - 18)])
    b.paint(knot, hair, .22 + b.radial(hx - 1, hy - 31, 8) * .6, d=.3)
    b.paint(b.rect(hx - 10, hy - 29, hx + 7, hy - 28), T(OCHRE), .6, d=.1)
    # A short sideburn and nothing more.
    b.paint(b.poly([(hx - 5, hy - 2), (hx - 1, hy - 3), (hx, hy + 3), (hx - 3, hy + 3)]), hair, .34, d=.2)
    # Brow, eye, nose shade, mouth, ear.
    b.flat(b.line([(hx + 6, hy - 8), (hx + 13, hy - 8)]), hair[3])
    if closed:
        b.flat(b.line([(hx + 7, hy - 3), (hx + 10, hy - 2), (hx + 13, hy - 3)]), hair[1])
    else:
        b.flat(b.rect(hx + 7, hy - 5, hx + 12, hy - 1), (238, 234, 224) if T.kind != "night" else (176, 188, 220))
        b.flat(b.rect(hx + 9, hy - 5, hx + 12, hy - 1), hair[1])
        b.flat(b.rect(hx + 10, hy - 4, hx + 10, hy - 4), (238, 234, 224) if T.kind != "night" else (176, 188, 220))
        b.flat(b.rect(hx + 7, hy - 6, hx + 13, hy - 6), hair[1])
    b.flat(b.line([(hx + 11, hy + 8), (hx + 14, hy + 8)]), skin[2])
    b.flat(b.rect(hx + 15, hy + 2, hx + 15, hy + 2), skin[2])
    b.paint(b.ellipse(hx - 6, hy - 5, hx + 1, hy + 4), skin, .46, d=.2)
    b.paint(b.line([(hx - 4, hy - 3), (hx - 2, hy + 1)]), skin, .22, d=0)


# The quilt's contour in each pose, left to right from the hem to the foot, and where its hem meets the mattress's near edge.
# Lying: chest, belly, thighs, the rise of his toes. Sitting: fallen to his lap, one knee drawn up.
QUILT_NEAR = 345
DRAPE = {
    "lie": ([(124, 267), (140, 260), (162, 257), (188, 260), (214, 265), (246, 270), (280, 273), (304, 272), (318, 267), (330, 272), (338, 292), (344, QUILT_NEAR)], 82),
    "prop": ([(150, 287), (176, 284), (204, 280), (230, 276), (262, 282), (296, 285), (318, 279), (332, 286), (339, 304), (344, QUILT_NEAR)], 112),
    "sit": ([(150, 289), (176, 288), (204, 281), (230, 274), (262, 283), (296, 286), (318, 279), (332, 286), (339, 304), (344, QUILT_NEAR)], 112),
}


def drape(q, T, pose):
    """Silk laid over him and over the mattress: lit where it faces up, falling into shade toward the near edge, creased
    across his body, with a white hem turned back at his end. Everything follows the mattress's perspective."""
    top, near_x = DRAPE[pose]
    shape = q.poly(top + [(near_x, QUILT_NEAR)])
    xs = [p[0] for p in top]; ys = [p[1] for p in top]
    crest = np.interp(q.X, xs, ys)
    v = np.clip((q.Y - crest) / np.maximum(1, QUILT_NEAR - crest), 0, 1)
    # Along the quilt, measured so that a line toward the viewer stays one line.
    slope = top[0][0] - near_x
    u = q.X + v * slope
    folds = noise(W, H, 34, 60, 161, 3)
    bent = folds[q.Y, np.clip(u.astype(int), 0, W - 1)]
    light = .72 - v * .44 + (bent - .5) * .42
    # The ridge of his body catches the light; the fold just below it does not.
    light += np.clip(1 - np.abs(q.Y - crest - 3) / 5, 0, 1) * .16
    light -= np.clip(1 - np.abs(v - .34) / .12, 0, 1) * .1
    q.paint(shape, T(JADE), light, d=.6)
    # A small woven figure, row on row.
    gu, gv = (u / 14).astype(int), (v * 7).astype(int)
    du, dv = u - gu * 14 - 7, (v * 7 - gv) * 11 - 5.5
    figure = (np.abs(du) + np.abs(dv) < 2.6) & ((gu + gv) % 2 == 0)
    q.paint(figure & shape, T(OCHRE), np.clip(light * .86, 0, 1), d=.3)
    # The side that hangs off the mattress's near edge.
    q.paint(shape & q.rect(0, QUILT_NEAR - 6, W, H), T(JADE), np.clip(light * .5, 0, 1), d=.3)
    # Hem: a band of white cotton along his end, a stitched line inside it.
    hem = shape & (u < top[0][0] + 13)
    q.paint(hem, T(COTTON), .86 - v * .34 + (bent - .5) * .2, d=.4)
    q.paint(shape & (u >= top[0][0] + 13) & (u < top[0][0] + 15), T(JADE), np.clip(light * .55, 0, 1), d=.2)
    q.paint(q.edge_top(shape, 1) & ~hem, T(JADE), .92, d=.2)


def seonbi_sitting(b, T, hx, hy):
    """Upright in his white under-jacket, slim as he is in the game, one sleeve resting forward on his knee."""
    skin, cloth = T(SKIN), T(COTTON)
    back = b.poly([(hx - 10, hy + 15), (hx - 22, hy + 27), (hx - 25, hy + 62), (hx - 28, hy + 124), (hx + 26, hy + 124), (hx + 22, hy + 64), (hx + 20, hy + 28), (hx + 8, hy + 15)])
    folds = (noise(W, H, 11, 46, 151, 2) - .5) * .2
    b.paint(back, cloth, .42 + b.grad(hx - 26, 0, hx + 24, 0) * .46 + folds, d=.5)
    # The crossed collar and its tie.
    col = b.poly([(hx - 1, hy + 16), (hx + 7, hy + 16), (hx + 19, hy + 56), (hx + 13, hy + 60)])
    b.paint(col, cloth, 1, d=.1)
    b.paint(b.line([(hx + 14, hy + 58), (hx + 22, hy + 86)], 2), T(INDIGO), .32, d=.1)
    b.paint(b.line([(hx + 14, hy + 58), (hx + 9, hy + 82)], 2), T(INDIGO), .24, d=.1)
    # Sleeve: shoulder to elbow to the hand on his knee.
    sleeve = b.poly([(hx - 12, hy + 28), (hx + 2, hy + 26), (hx + 8, hy + 58), (hx + 40, hy + 86), (hx + 34, hy + 100), (hx + 2, hy + 90), (hx - 14, hy + 62)])
    b.paint(sleeve, cloth, .4 + b.grad(hx - 12, hy + 28, hx + 36, hy + 96) * .4 + folds, d=.5)
    b.paint(b.ellipse(hx + 33, hy + 85, hx + 46, hy + 97), skin, .6, d=.3)
    head_pose(b, T, hx, hy)


SIT_X, SIT_Y = 178, 172      # his head when he sits up, over his hips
def bed_parts(T, pose):
    """His head (and body, sitting up) and the quilt on top, each a full-screen layer."""
    night = T.kind == "night"
    body = Art(W, H); quilt = Art(W, H)
    for a in (body, quilt): a.light[:] = .9 if night else 1.0
    b = body; q = quilt
    if pose == "lie":
        # On his back with his head on the bolster: crown to the left, face to the ceiling, chin toward his feet. Only
        # the head shows; the quilt comes up to his neck.
        temp = Art(W, H); temp.light[:] = b.light
        head_pose(temp, T, 180, 180, closed=True)
        n = 60
        hx, hy = 104, 264
        for name in ("rgb", "a"):
            src = getattr(temp, name)[180 - n:180 + n, 180 - n:180 + n]
            getattr(b, name)[hy - n:hy + n, hx - n:hx + n] = np.rot90(src, 1)
    else:
        temp = Art(W, H); temp.light[:] = b.light
        seonbi_sitting(temp, T, SIT_X, SIT_Y)
        if pose == "prop":
            # Halfway up: the same figure leaning back on its arm, turned about the hip.
            im = Image.fromarray(np.dstack([np.clip(temp.rgb, 0, 255).astype(np.uint8), temp.a.astype(np.uint8) * 255]))
            im = im.rotate(38, Image.NEAREST, center=(SIT_X, 296))
            arr = np.asarray(im); b.rgb = arr[..., :3].astype(np.float32); b.a = arr[..., 3] > 128
        else:
            b.rgb = temp.rgb; b.a = temp.a
    drape(q, T, pose)
    if night:
        for a in (body, quilt): a.tint(a.grad(360, 0, 0, 0) * .2, (10, 12, 30))
    return body, quilt


# ---------------------------------------------------------------- the close shot of the door

C_RAMP = [(40, 54, 104), (58, 76, 128), (84, 106, 156), (116, 140, 186), (150, 172, 210), (186, 204, 230), (216, 228, 244)]
def close_door():
    """Filling the screen: the paper, three huge slits of it between thick rails. Panes are holes for the shadow behind."""
    c = Art(W, H); T = Theme("night")
    c.paint(c.rect(0, 0, W, H), T(WOOD), .28 + (noise(W, H, 8, 160, 171, 3) - .5) * .34, d=.6)
    panes = c.blank()
    for x in (28, 176, 324, 472):
        panes |= c.rect(x, 0, x + 128, H)
    for y in (66, 82, 98, 268, 284, 300):
        panes &= ~c.rect(0, y, W, y + 7)
    # Highlights on the edges of the rails nearest the paper.
    edge = c.ring(panes)
    c.paint(edge, T(WOOD), .62, d=.2)
    c.erase(panes)
    v = np.clip(np.hypot((c.X - W / 2) / (W * .56), (c.Y - H / 2) / (H * .6)) - .58, 0, 1)
    c.tint(v * 1.3, (4, 5, 14))
    return c


def close_paper():
    c = Art(W, H)
    v = .42 + c.radial(420, 60, 560) * .5 + (noise(W, H, 90, 80, 172, 3) - .5) * .12
    c.paint(c.rect(0, 0, W, H), C_RAMP, v, d=.8, lit=False)
    rng = np.random.default_rng(173)
    for _ in range(520):
        x, y = rng.integers(0, W), rng.integers(0, H)
        a = rng.random() * 3.14; l = rng.integers(5, 22)
        ln = c.line(curve((x, y), (x + np.cos(a) * l * .5 + rng.normal() * 3, y + np.sin(a) * l * .5 + rng.normal() * 3), (x + np.cos(a) * l, y + np.sin(a) * l), 6), 1)
        c.rgb[ln] += (14, 16, 18)
    v2 = np.clip(np.hypot((c.X - W / 2) / (W * .56), (c.Y - H / 2) / (H * .6)) - .58, 0, 1)
    c.tint(v2 * 1.3, (4, 5, 14))
    return c


def close_shadow():
    """The shape very near: its head fills the panes, two bulging eyes looming over."""
    c = Art(W + 200, H + 40); cx = (W + 200) // 2
    head = c.ellipse(cx - 280, 74, cx + 280, H + 240)
    eyes = c.circle(cx - 180, 86, 86) | c.circle(cx + 180, 86, 86)
    shape = head | eyes
    grown = shape.copy()
    for _ in range(6): grown = c.ring(grown) | grown
    keep = shape | ((grown & ~shape) & (c.bayer < 0))
    c.flat(keep, (14, 18, 42))
    return c


def close_hole(stage):
    """The hole a wet fingertip makes: a dark spot, a split, then torn paper curling inward."""
    c = Art(W, H); cx, cy, r = 322, 192, (5, 16, 36)[stage]
    if stage == 0:
        c.flat(c.circle(cx, cy, r + 3), (30, 40, 86))
        c.flat(c.circle(cx, cy, r), (8, 8, 20))
        return c
    n = noise(W, H, 8, 8, 181 + stage, 2)
    d = np.hypot(c.X - cx, c.Y - cy) + (n - .5) * r * .55
    hole = d < r
    wet = (d >= r) & (d < r + 7)
    fray = (d >= r + 7) & (d < r + 13)
    c.paint(wet, C_RAMP, .08 + (n - .5) * .3, d=.7, lit=False)
    c.flat(fray & (c.bayer < -.18), (58, 76, 128))
    c.flat(hole, (4, 4, 10))
    # Torn flaps catch the moon.
    rng = np.random.default_rng(183)
    for i in range(14):
        a = i / 14 * 6.283 + rng.random() * .3
        x0, y0 = cx + np.cos(a) * (r + 2), cy + np.sin(a) * (r + 2)
        x1, y1 = cx + np.cos(a + .18) * (r - 5), cy + np.sin(a + .18) * (r - 5)
        x2, y2 = cx + np.cos(a - .18) * (r - 2), cy + np.sin(a - .18) * (r - 2)
        c.paint(c.poly([(x0, y0), (x1, y1), (x2, y2)]), C_RAMP, .78 if i % 2 else .55, d=.2, lit=False)
    return c


def close_eye(frame):
    """The eye at the hole: red, its pupil a slit, wet with the moon; the same eye as her unmasked face."""
    c = Art(W, H); cx, cy = 322, 192; rw, rh = 27, 21
    almond = c.ellipse(cx - rw, cy - rh, cx + rw, cy + rh)
    c.paint(almond, [(70, 40, 44), (120, 86, 84), (176, 146, 136), (214, 196, 178)], .34 + c.radial(cx, cy, 30) * .54, d=.7, lit=False)
    # Veins creeping in from the corners.
    rng = np.random.default_rng(191)
    for i in range(10):
        a = rng.random() * 6.283
        ln = c.line(curve((cx + np.cos(a) * rw, cy + np.sin(a) * rh), (cx + np.cos(a + .3) * rw * .7, cy + np.sin(a + .3) * rh * .7), (cx + np.cos(a) * 15, cy + np.sin(a) * 15), 6)) & almond
        c.flat(ln, (156, 48, 46))
    iris = c.circle(cx + 2, cy, 17)
    c.paint(iris, [(70, 8, 14), (120, 14, 18), (176, 26, 22), (222, 58, 28), (248, 118, 44), (255, 184, 80)], np.clip(.24 + (np.hypot(c.X - cx - 2, c.Y - cy) / 17) * .1 + c.radial(cx + 2, cy + 8, 15) * .68, 0, 1), d=.7, lit=False)
    c.flat(c.ring(iris) & almond, (36, 4, 10))
    slit = c.poly([(cx + 2, cy - 16), (cx + 6, cy), (cx + 2, cy + 16), (cx - 2, cy)])
    c.flat(slit, (4, 2, 6))
    c.flat(c.ellipse(cx - 8, cy - 11, cx - 2, cy - 6), (250, 250, 255))
    c.flat(c.circle(cx + 9, cy + 8, 1), (250, 214, 180))
    # Lids: toad-rough dark skin all round the hole, the opening between them narrowing as it blinks.
    openness = (1.0, .5, .08)[frame]
    upper = curve((cx - rw - 2, cy + 1), (cx, cy - rh * 2.1 * openness), (cx + rw + 2, cy + 1), 22)
    lower = curve((cx + rw + 2, cy + 1), (cx, cy + rh * 1.9 * openness), (cx - rw - 2, cy + 1), 22)
    opening = c.poly(upper + lower)
    tex = noise(W, H, 5, 5, 192, 2)
    d = np.hypot(c.X - cx, c.Y - cy) + (tex - .5) * 8
    lid = (d < 36) & ~opening
    c.paint(lid, [(12, 12, 16), (24, 24, 28), (40, 38, 40), (64, 56, 52), (92, 80, 70)], .2 + (tex - .5) * 1.1 + c.grad(cx - 30, cy - 30, cx + 30, cy + 30) * -.12, d=.6, lit=False)
    c.flat(c.ring(opening) & (d < 36), (2, 2, 4))
    c.a &= d < 36
    return c


def hole_mark():
    """Next morning, the torn hole in one of the door's panes and the thin sunbeam through it across the room."""
    c = Art(W, H)
    hx, hy = 512, 136
    # The beam first, then the hole over it.
    beam = c.line([(hx, hy), (238, 330)], 3)
    fade = 1 - c.grad(hx, hy, 238, 330) * .62
    c.flat(beam & (c.bayer + .5 < fade * .62), (255, 244, 204))
    pool = c.ellipse(222, 324, 256, 336)
    c.flat(pool & (c.bayer + .5 < .34), (255, 246, 210))
    m = c.circle(hx, hy, 3)
    c.flat(c.circle(hx, hy, 5) & (c.bayer < .2), (180, 120, 70))
    c.flat(m, (255, 255, 240))
    return c


# ---------------------------------------------------------------- Kongjwi's stare

def stare():
    """A cut to her eyes between black bars, in the failing light: the smile is gone, the eyes wide and fixed, no blink."""
    c = Art(W, H)
    c.flat(c.rect(0, 0, W, H), (3, 2, 5))
    band = c.rect(0, 82, W, 277)
    skin_r = [(34, 16, 34), (58, 26, 44), (92, 42, 52), (132, 66, 62), (176, 98, 78), (216, 136, 100), (242, 176, 136), (252, 210, 172)]
    hair_r = [(4, 3, 8), (10, 6, 14), (18, 12, 22), (30, 20, 32), (48, 32, 46), (74, 50, 64)]
    c.paint(band, skin_r, .2 + c.grad(30, 0, 640, 0) * .62 + (noise(W, H, 90, 60, 205, 2) - .5) * .06, d=.7, lit=False)
    # Under the fringe and down both sides the light does not reach.
    c.tint(band * np.clip((128 - c.Y) / 46, 0, 1) * .85, (26, 12, 30))
    c.tint(band * np.clip((c.Y - 240) / 40, 0, 1) * .35, (40, 16, 36))
    # A flush high on the cheeks, more like a bruise in this light.
    for cx in (92, 548):
        c.tint(band * c.radial(cx, 262, 92, .55) ** 1.4 * .5, (150, 40, 58))
    # Fringe: heavy strands parted in the middle.
    tips = noise(W, 1, 15, 1, 206, 2)[0]
    strands = noise(W, H, 4, 120, 207, 2)
    edge = 100 + np.abs(c.X - 320) * .09 + tips[c.X] * 16 - 8
    fringe = (c.Y < edge) & band
    c.paint(fringe, hair_r, .18 + (strands - .5) * 1.0 + c.grad(0, 0, 640, 0) * .24, d=.6, lit=False)
    part = c.poly([(316, 82), (324, 82), (321, 108), (319, 108)])
    c.paint(part, skin_r, .3, d=.3, lit=False)
    for x, w, cx in ((0, 44, -1), (596, 44, 1)):
        side = c.poly([(x, 82), (x + w, 82), (x + w - 6 * cx, 180), (x + w + 6 * cx, 277), (x, 277)]) & band
        c.paint(side, hair_r, .16 + (strands - .5) * .9 + (.22 if cx > 0 else 0), d=.6, lit=False)
    # A last hair-thin line of sunset on her right side, and the dull flower of her pin at the left.
    c.flat(c.line([(589, 96), (594, 180), (601, 277)], 2) & band, (240, 118, 74))
    for dx, dy in ((0, -13), (12, -4), (8, 11), (-8, 11), (-12, -4)):
        c.paint(c.circle(46 + dx, 108 + dy, 9) & band, [(58, 22, 44), (92, 34, 58), (132, 54, 74), (170, 80, 94)], .45 + c.grad(36, 0, 58, 0) * .4, d=.3, lit=False)
    c.paint(c.circle(46, 108, 4) & band, [(120, 86, 50), (190, 150, 74)], .7, d=0, lit=False)
    # Eyes: round and large as in her portrait, the iris all but black, one dead point of light.
    for cx, side in ((186, -1), (454, 1)):
        rw, rh, cy = 84, 40, 186
        upper = curve((cx - rw, cy + 8), (cx, cy - rh * 2.05), (cx + rw, cy + 8), 28)
        lower = curve((cx + rw, cy + 8), (cx, cy + rh * 1.65), (cx - rw, cy + 8), 28)
        eye = c.poly(upper + lower)
        white = [(52, 40, 66), (84, 68, 90), (122, 104, 118), (164, 146, 150), (198, 182, 176), (222, 208, 196)]
        c.paint(eye, white, .3 + c.grad(cx - rw, 0, cx + rw, 0) * .42 + c.grad(0, cy - rh, 0, cy + rh) * .28, d=.6, lit=False)
        ix = cx + 12 * (1 if side < 0 else 1)
        iris = c.circle(ix, cy, 40)
        c.paint(iris & eye, [(4, 3, 6), (9, 5, 10), (16, 9, 14), (30, 15, 20), (54, 24, 30), (84, 38, 40)], .08 + c.radial(ix, cy + 32, 42) ** 1.2 * .95, d=.6, lit=False)
        c.flat(c.circle(ix, cy, 22) & eye, (1, 1, 3))
        c.flat(c.rect(ix - 16, cy - 21, ix - 9, cy - 15), (226, 208, 196))
        c.flat(c.ring(iris) & eye, (2, 1, 4))
        # Lash line, thick, lifting at the outer corner.
        lash = c.line(upper, 7)
        c.paint(lash, hair_r, .1, d=.1, lit=False)
        tip = cx + side * rw
        c.paint(c.poly([(tip - side * 8, cy + 2), (tip + side * 20, cy - 14), (tip + side * 14, cy + 2), (tip, cy + 13)]), hair_r, .08, d=0, lit=False)
        c.paint(c.line(lower, 2), skin_r, .08, d=.2, lit=False)
        # Brow: thin, flat, unmoved. No anger in it, which is the worst of it.
        c.paint(c.line(curve((cx - 66, cy - 72), (cx, cy - 82), (cx + 66, cy - 72), 16), 4) & band, hair_r, .2, d=.2, lit=False)
    return c


def main():
    os.makedirs(OUT, exist_ok=True)
    def out(name): return f"{OUT}/{name}.png"
    only = [a for a in sys.argv[2:] if not a.startswith("--")]
    def want(*names): return not only or any(n in only for n in names)
    if want("ext"):
        for kind in ("evening", "night", "morning"):
            exterior(kind).save(out("ext_" + kind), colors=112)
        ext_window().save(out("ext_window"), colors=0)
        clouds().save(out("ext_clouds"), colors=0)
        mist().save(out("ext_mist"), colors=0)
        for f in range(2): rooster(f).save(out(f"ext_rooster_{f}"), colors=0)
        for kind, x in (("seonbi", 468), ("kongjwi", 530), ("patjwi", 586)):
            figure(kind, x).save(out("fig_" + kind), colors=0)
    if want("room"):
        for kind in ("night", "morning"):
            T = Theme(kind)
            lit = room_scene(T, True)
            palette = lit.palette_of(112)
            lit.save(out("room_" + kind), palette, spread=3.5)
            door_paper(T).save(out("door_" + kind), colors=48)
            if kind == "night":
                dark = room_scene(T, False)
                a = np.array(lit.image(palette, spread=3.5)); b = np.array(dark.image(palette, spread=3.5))
                # Over the dark room, what the moon adds: faded while the shape stands at the door.
                diff = (a[..., :3] != b[..., :3]).any(-1) & (a[..., 3] > 0)
                layer = a.copy(); layer[..., 3] = np.where(diff, 255, 0)
                Image.fromarray(layer).save(out("room_moon"))
                Image.fromarray(b).save(out("room_night"))
            for pose in ("lie", "prop", "sit"):
                if kind == "morning" and pose != "sit": continue
                body, quilt = bed_parts(T, pose)
                body.save(out(f"bed_{pose}_{kind}"), colors=48, spread=4)
                quilt.save(out(f"quilt_{pose}_{kind}"), colors=56, spread=4)
        for f in range(3): shadow(f).save(out(f"shadow_{f}"), colors=0)
        hole_mark().save(out("hole_mark"), colors=0)
    if want("close"):
        close_door().save(out("close_door"), colors=48)
        close_paper().save(out("close_paper"), colors=40)
        close_shadow().save(out("close_shadow"), colors=0)
        for i in range(3): close_hole(i).save(out(f"close_hole_{i}"), colors=0)
        for i in range(3): close_eye(i).save(out(f"close_eye_{i}"), colors=40, spread=4)
    if want("stare"):
        # Kongjwi's stare is the user's own picture (Tools/ArtGen/source/kongjwi_stare_cut.webp), fitted to the screen.
        src = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "source", "kongjwi_stare_cut.webp")).convert("RGB")
        src.resize((W, H), Image.BOX).save(out("stare"))

if __name__ == "__main__":
    main()
