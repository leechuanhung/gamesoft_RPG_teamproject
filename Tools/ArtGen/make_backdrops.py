# Draws the background layers behind each area at the props' density (one texel per art pixel, 24 per unit).
# They are silhouettes in flat tones close to each area's sky, one shade each and no black outline, so the
# outlined characters and props stay in front: a far layer (mountains, moon, distant pines) that drifts with
# the camera, and a near layer (walls, graves, standing stones, pines) that stays put.
#   bg_<area>_far.png, bg_<area>_near.png for stele, camp, village, forest, ferry, pass, and on the road to Hanyang
#   fort (the fortress wall on the ridge), station (dry fields), grave (mounds), city (wall, gate, roofs), hall, manor (night)
# Usage: python make_backdrops.py Assets/Resources/JinaV4/13_Props   [--preview DIR]
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas
OUT = sys.argv[1]
preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
PPU = 546 / 1.9 / 12            # art pixels per unit, as JinaArt.PropPixelsPerUnit
# Area spans in units (WorldBuilder). The layers are drawn 4 units wider, then cut to the area's exact width when saved
# (save), so no layer reaches into the next map: the camera stops at a map's edge, and what lay past it showed there.
AREAS = {'stele': 30, 'camp': 28, 'village': 40, 'forest': 36, 'ferry': 46, 'kong': 40, 'pass': 48,
         'fort': 46, 'station': 46, 'grave': 46, 'city': 46, 'hall': 40, 'manor': 50}
SKY = {'stele': (43, 46, 71), 'camp': (118, 58, 46), 'village': (112, 146, 164), 'forest': (30, 66, 54), 'ferry': (114, 136, 150), 'kong': (176, 150, 110), 'pass': (116, 74, 100),
       'fort': (120, 150, 170), 'station': (160, 158, 150), 'grave': (70, 46, 78), 'city': (130, 170, 210), 'hall': (200, 180, 160), 'manor': (40, 22, 42)}

def mix(a, b, t): return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))
def lighter(c, t): return mix(c, (236, 232, 220), t)
def darker(c, t): return mix(c, (8, 8, 12), t)

# What is cut from each end when saving: the 2 extra units.
CUT = int(round(2 * PPU))

def ridge(w, base, amps, seed, taper=CUT):
    """A mountain skyline: summed waves that repeat across the width, rounded to whole art pixels."""
    rng = np.random.default_rng(seed)
    x = np.arange(w)
    h = np.full(w, float(base))
    for amp, period in amps:
        h += amp * np.sin(2 * math.pi * x / period + rng.random() * 6.28)
    env = np.clip(np.minimum(x, w - 1 - x) / taper, 0, 1)
    return np.round(h * (env * env * (3 - 2 * env))).astype(int)

def mountains(c, h, colour, shade):
    """Fill under the skyline; the slopes facing right (the light is from the left) take the shade."""
    for x in range(c.w):
        if h[x] <= 0: continue
        top = c.h - h[x]
        c.c[top:, x] = colour; c.a[top:, x] = True
        if x > 0 and h[x] < h[x - 1]: c.c[top:top + max(2, (h[x - 1] - h[x]) * 2), x] = shade

def pine(c, cx, bottom, height, colour, shade):
    """A distant pine: three stacked tiers on a short trunk, shaded on the right."""
    trunk = c.rect(cx - 1, bottom - height // 5, cx + 1, bottom)
    m = trunk.copy()
    for i in range(3):
        y1 = bottom - height // 6 - i * height // 4
        half = height // 3 - i * height // 10
        m |= c.poly([(cx - half, y1), (cx + half, y1), (cx, y1 - height // 3)])
    c.paint(m, colour, inside=False)
    c.paint(m & c.rect(cx + 1, 0, c.w, c.h), shade, inside=False)

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    keep = int(AREAS[name.split('_')[1]] * PPU)   # never wider than the area
    left = (c.w - keep) // 2
    Image.fromarray(img[:, left:left + keep]).save(f"{OUT}/{name}.png")
    return f"{OUT}/{name}.png"

def width(area): return int(round((AREAS[area] + 4) * PPU))

def leaf(near, x, r, rng, colour, shade, light):
    """A tree showing over the wall: a short trunk and a lumpy crown of overlapping clumps, lit from the upper left."""
    near.paint(near.rect(x - 2, 38, x + 2, 50), (72, 66, 62), inside=False)
    clumps = [(x + int(rng.integers(-r, r)), 44 - int(rng.integers(0, 2 * r)), int(rng.integers(r // 2, r))) for _ in range(9)]
    crown = near.ellipse(x - r, 46 - 2 * r, x + r, 46)
    for cx, cy, cr in clumps: crown |= near.ellipse(cx - cr, cy - cr, cx + cr, cy + cr)
    crown &= near.rect(0, 0, near.w - 1, 49)
    near.paint(crown, colour, inside=False)
    for cx, cy, cr in clumps:
        near.paint(near.ellipse(cx - cr + cr // 2, cy, cx + cr, cy + cr) & crown, shade, inside=False)
    for cx, cy, cr in clumps:
        near.paint(near.ellipse(cx - cr + 1, cy - cr + 1, cx, cy - cr // 2) & crown, light, inside=False)

def stele():
    sky, w = SKY['stele'], width('stele')
    far = Canvas(w, 150)
    moon = far.ellipse(w // 3 - 18, 6, w // 3 + 18, 42)
    far.paint(far.ellipse(w // 3 - 24, 0, w // 3 + 24, 48), lighter(sky, .12), inside=False)
    far.paint(moon, (226, 222, 196), inside=False)
    far.paint(moon & far.ellipse(w // 3 - 8, 6, w // 3 + 28, 42), (204, 200, 176), inside=False)
    mountains(far, ridge(w, 70, [(22, 260), (10, 97)], 3), lighter(sky, .14), lighter(sky, .08))
    mountains(far, ridge(w, 42, [(14, 180), (7, 61)], 4), lighter(sky, .06), sky)
    near = Canvas(w, 60)
    # mist along the ground and rows of weathered standing stones
    near.paint(near.rect(0, 46, w, 59), lighter(sky, .1), inside=False)
    rng = np.random.default_rng(5)
    stone, shade = darker(sky, .25), darker(sky, .4)
    for i, x in enumerate(range(30, w - 30, 52)):
        x += int(rng.integers(-10, 10))
        if i % 5 == 2:
            # a three-storey stone pagoda: stacked roofs narrowing upward on a plinth
            m = near.rect(x - 9, 52, x + 9, 59)
            for k in range(3):
                y = 52 - k * 11
                m |= near.rect(x - 4 + k, y - 7, x + 4 - k, y) | near.rect(x - 9 + k * 2, y - 9, x + 9 - k * 2, y - 7)
            m |= near.rect(x - 1, 15, x + 1, 22)
        else:
            # a weathered standing stone, round-topped, of varying girth
            hgt, half = int(rng.integers(14, 30)), int(rng.integers(3, 7))
            m = near.rect(x - half, 59 - hgt, x + half, 59) | near.ellipse(x - half, 56 - hgt, x + half, 63 - hgt)
        near.paint(m, stone, inside=False)
        near.paint(m & near.rect(x + 2, 0, w, 60), shade, inside=False)
    return save(far, 'bg_stele_far'), save(near, 'bg_stele_near')

def camp():
    sky, w = SKY['camp'], width('camp')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 74, [(22, 333), (12, 131), (6, 47), (3, 19)], 7), lighter(sky, .1), lighter(sky, .04))
    mountains(far, ridge(w, 40, [(12, 110), (6, 37)], 8), darker(sky, .08), darker(sky, .16))
    near = Canvas(w, 70)
    mound, mound2 = darker(sky, .3), darker(sky, .42)
    rng = np.random.default_rng(9)
    for x in range(40, w - 40, 70):
        x += int(rng.integers(-12, 12))
        if rng.random() < .55:
            # a burial mound with its stone marker
            m = near.ellipse(x - 20, 52, x + 20, 84) & near.rect(0, 0, w, 69)
            near.paint(m, mound, inside=False); near.paint(m & near.rect(x + 6, 0, w, 70), mound2, inside=False)
            near.paint(near.rect(x - 2, 44, x + 2, 56), darker(sky, .5), inside=False)
        else:
            # a dead tree: a bent trunk and bare branches
            t = near.line([(x, 69), (x + 2, 40), (x - 1, 22)], 3)
            for pts in ([(x + 1, 44), (x + 13, 34), (x + 17, 26)], [(x, 36), (x - 12, 28)], [(x - 1, 28), (x + 6, 16)]):
                t |= near.line(pts, 2)
            near.paint(t, darker(sky, .5), inside=False)
    return save(far, 'bg_camp_far'), save(near, 'bg_camp_near')

def village():
    sky, w = SKY['village'], width('village')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 80, [(24, 330), (12, 120)], 11), (142, 170, 180), (124, 154, 168))
    mountains(far, ridge(w, 50, [(12, 220), (6, 80)], 12), (112, 146, 138), (96, 130, 126))
    near = Canvas(w, 70)
    # trees showing over the wall, then a low dry-stone wall (doldam) with tiles along its top and a few gaps
    rng = np.random.default_rng(15)
    for x in range(40, w - 40, 110):
        x += int(rng.integers(-20, 20)); r = int(rng.integers(14, 20))
        leaf(near, x, r, rng, (86, 120, 92), (68, 100, 84), (108, 142, 100))
    stone, stone2, top = (148, 150, 150), (116, 120, 124), (78, 80, 92)
    keep = near.rect(0, 0, w - 1, 69)
    wall = near.rect(0, 50, w - 1, 69) & keep
    near.paint(wall, stone, inside=False)
    for row, y in enumerate(range(50, 70, 5)):
        near.paint(near.rect(0, y, w - 1, y) & keep, stone2, inside=False)
        for x in range((row % 2) * 7, w, 14): near.paint(near.rect(x, y, x, y + 4) & keep, stone2, inside=False)
    near.paint(near.rect(0, 46, w - 1, 49) & keep, top, inside=False)
    for x in range(0, w, 6): near.paint(near.rect(x, 45, x + 3, 45) & keep, top, inside=False)
    return save(far, 'bg_village_far'), save(near, 'bg_village_near')

def kong():
    """Low golden hills, a stone wall with trees over it, the same wall as the first village but a warmer hour."""
    sky, w = SKY['kong'], width('kong')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 70, [(20, 300), (10, 110)], 41), (176, 164, 138), (160, 148, 128))
    mountains(far, ridge(w, 44, [(10, 210), (5, 74)], 42), (148, 146, 104), (130, 130, 96))
    near = Canvas(w, 70)
    rng = np.random.default_rng(43)
    for x in range(30, w - 30, 96):
        x += int(rng.integers(-18, 18)); r = int(rng.integers(13, 19))
        leaf(near, x, r, rng, (112, 130, 72), (90, 110, 66), (144, 160, 84))
    stone, stone2, top = (160, 146, 122), (130, 116, 98), (92, 78, 70)
    keep = near.rect(0, 0, w - 1, 69)
    near.paint(near.rect(0, 50, w - 1, 69), stone, inside=False)
    for row, y in enumerate(range(50, 70, 5)):
        near.paint(near.rect(0, y, w - 1, y), stone2, inside=False)
        for x in range((row % 2) * 7, w, 14): near.paint(near.rect(x, y, x, y + 4), stone2, inside=False)
    near.paint(near.rect(0, 46, w - 1, 49), top, inside=False)
    for x in range(0, w, 6): near.paint(near.rect(x, 45, x + 3, 45), top, inside=False)
    return save(far, 'bg_kong_far'), save(near, 'bg_kong_near')

def forest():
    sky, w = SKY['forest'], width('forest')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 64, [(20, 240), (8, 70)], 13), lighter(sky, .1), lighter(sky, .05))
    rng = np.random.default_rng(14)
    for x in range(10, w - 10, 17):
        pine(far, x + int(rng.integers(-4, 4)), 149, int(rng.integers(44, 70)), lighter(sky, .04), sky)
    near = Canvas(w, 110)
    for x in range(20, w - 20, 41):
        pine(near, x + int(rng.integers(-10, 10)), 109, int(rng.integers(60, 96)), darker(sky, .22), darker(sky, .36))
    return save(far, 'bg_forest_far'), save(near, 'bg_forest_near')

def ferry():
    """Low blue hills across a wide river, and drooping willows on the near bank."""
    sky, w = SKY['ferry'], width('ferry')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 58, [(16, 300), (6, 90)], 21), lighter(sky, .12), lighter(sky, .06))
    mountains(far, ridge(w, 30, [(8, 160), (3, 50)], 22), lighter(sky, .05), sky)
    near = Canvas(w, 110)
    rng = np.random.default_rng(23)
    for x in range(30, w - 30, 150):
        x += int(rng.integers(-24, 24)); h = int(rng.integers(70, 96))
        trunk = near.poly([(x - 3, 109), (x + 3, 109), (x + 1, 109 - h), (x - 1, 109 - h)])
        near.paint(trunk, (72, 76, 74), inside=False)
        top = 109 - h
        leaf_a, leaf_b, leaf_c = (92, 128, 106), (76, 110, 96), (116, 150, 116)
        crown = near.ellipse(x - 30, top - 10, x + 30, top + 14)
        for k in range(7):
            cx = x + int(rng.integers(-26, 26)); cy = top + int(rng.integers(-8, 8)); r = int(rng.integers(6, 12))
            crown |= near.ellipse(cx - r, cy - r, cx + r, cy + r)
        near.paint(crown, leaf_a, inside=False)
        near.paint(crown & near.rect(x + 8, 0, w, 200), leaf_b, inside=False)
        near.paint(near.ellipse(x - 26, top - 8, x + 2, top + 2) & crown, leaf_c, inside=False)
        for dx in range(-28, 29, 3):   # long strands hanging from the crown (none past the left edge: rect() would wrap)
            if x + dx < 0: continue
            length = 34 + (28 - abs(dx)) + (dx * 7) % 11
            near.paint(near.rect(x + dx, top + 4, x + dx + 1, top + length), leaf_a if dx < 8 else leaf_b, inside=False)
    return save(far, 'bg_ferry_far'), save(near, 'bg_ferry_near')

def pass_():
    """High peaks at dusk behind the pass, and a ridge of pines on the near slope."""
    sky, w = SKY['pass'], width('pass')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 96, [(36, 260), (14, 80)], 31), lighter(sky, .14), lighter(sky, .07))
    mountains(far, ridge(w, 50, [(14, 180), (6, 60)], 32), lighter(sky, .05), sky)
    near = Canvas(w, 110)
    mountains(near, ridge(w, 26, [(10, 200), (4, 70)], 33), darker(sky, .12), darker(sky, .2))
    rng = np.random.default_rng(34)
    for x in range(16, w - 16, 33):
        pine(near, x + int(rng.integers(-8, 8)), 109, int(rng.integers(50, 84)), darker(sky, .22), darker(sky, .34))
    return save(far, 'bg_pass_far'), save(near, 'bg_pass_near')

def roofs(c, y, colour, shade, rng, gap=(10, 30), size=(40, 70), height=(12, 20)):
    """A row of tiled roofs seen over a wall: shallow curves with upturned ends, a ridge on top."""
    x = int(rng.integers(0, 20))
    while x < c.w - 10:
        w = int(rng.integers(*size)); h = int(rng.integers(*height))
        w = min(w, c.w - 1 - x)
        r = c.poly([(x, y), (x + 4, y - 3), (x + w // 2, y - 4), (x + w - 4, y - 3), (x + w, y), (x + w - 8, y - h), (x + 8, y - h)])
        c.paint(r, colour, inside=False)
        c.paint(r & c.rect(x + w // 2, 0, x + w, c.h), shade, inside=False)
        c.paint(c.rect(x + 6, y - h - 2, max(x + 6, x + w - 6), y - h), shade, inside=False)
        x += w + int(rng.integers(*gap))

def crenels(c, base, colour, h=8, step=10):
    """A wall following the skyline `base` (heights per column), with crenels along its top."""
    for x in range(c.w):
        if base[x] <= 0: continue
        top = c.h - base[x] - h - (3 if (x // (step // 2)) % 2 == 0 else 0)
        c.c[top:c.h - base[x] + 2, x] = colour; c.a[top:c.h - base[x] + 2, x] = True

def fort():
    """Morning peaks with the fortress wall crawling along the near ridge; pines below."""
    sky, w = SKY['fort'], width('fort')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 100, [(34, 280), (12, 90)], 61), lighter(sky, .14), lighter(sky, .07))
    h = ridge(w, 58, [(16, 200), (6, 66)], 62)
    mountains(far, h, lighter(sky, .04), sky)
    crenels(far, h, darker(sky, .14))
    near = Canvas(w, 110)
    rng = np.random.default_rng(63)
    for x in range(20, w - 20, 37):
        pine(near, x + int(rng.integers(-8, 8)), 109, int(rng.integers(52, 86)), darker(sky, .28), darker(sky, .38))
    return save(far, 'bg_fort_far'), save(near, 'bg_fort_near')

def station():
    """Flat, dry country under a grey sky, a line of poplars; near, a broken fence."""
    sky, w = SKY['station'], width('station')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 50, [(12, 320), (5, 100)], 71), lighter(sky, .08), lighter(sky, .03))
    mountains(far, ridge(w, 26, [(4, 160), (2, 40)], 72), darker(sky, .06), darker(sky, .1))
    rng = np.random.default_rng(73)
    for x in range(30, w - 30, 46):
        x += int(rng.integers(-10, 10)); t = int(rng.integers(36, 54))
        far.paint(far.ellipse(x - 5, 150 - t, x + 5, 150 - 20) | far.rect(x, 130, x + 1, 149), darker(sky, .14), inside=False)
    near = Canvas(w, 70)
    post = darker(sky, .42)
    for x in range(14, w - 14, 22):
        if rng.random() < .3: continue
        lean = int(rng.integers(-2, 3))
        near.paint(near.line([(x, 69), (x + lean, 46)], 3), post, inside=False)
        if rng.random() < .7: near.paint(near.line([(x - 10, 56 + int(rng.integers(-2, 3))), (x + 12, 54 + int(rng.integers(-2, 3)))], 2), post, inside=False)
    return save(far, 'bg_station_far'), save(near, 'bg_station_near')

def grave():
    """Dark hills crowded with burial mounds under a violet dusk; near, mounds, stones and dead trees."""
    sky, w = SKY['grave'], width('grave')
    far = Canvas(w, 150)
    mx = w * 2 // 3
    moon = far.ellipse(mx - 14, 10, mx + 14, 38)
    far.paint(moon, (214, 200, 170), inside=False); far.paint(moon & far.ellipse(mx - 6, 6, mx + 22, 34), lighter(sky, .2), inside=False)
    h = ridge(w, 66, [(22, 300), (8, 80)], 81)
    mountains(far, h, lighter(sky, .08), lighter(sky, .03))
    rng = np.random.default_rng(82)
    for x in range(20, w - 20, 26):
        x += int(rng.integers(-6, 6)); y = 150 - h[min(w - 1, x)] + int(rng.integers(4, 30))
        far.paint(far.ellipse(x - 8, y - 5, x + 8, y + 5) & far.rect(0, 0, w, y), darker(sky, .06), inside=False)
    near = Canvas(w, 70)
    mound, mound2 = darker(sky, .35), darker(sky, .5)
    for x in range(30, w - 30, 54):
        x += int(rng.integers(-12, 12))
        if rng.random() < .7:
            m = near.ellipse(x - 18, 52, x + 18, 84) & near.rect(0, 0, w, 69)
            near.paint(m, mound, inside=False); near.paint(m & near.rect(x + 5, 0, w, 70), mound2, inside=False)
            near.paint(near.rect(x - 2, 46, x + 2, 56), darker(sky, .6), inside=False)
        else:
            t = near.line([(x, 69), (x - 2, 38), (x + 2, 18)], 3)
            for pts in ([(x - 1, 40), (x - 14, 30)], [(x + 1, 30), (x + 12, 20)], [(x, 24), (x - 6, 12)]): t |= near.line(pts, 2)
            near.paint(t, darker(sky, .6), inside=False)
    return save(far, 'bg_grave_far'), save(near, 'bg_grave_near')

def city():
    """Hanyang by day: a great rocky mountain behind, the city wall and a gate tower on the far ridge;
    near, a sea of tiled roofs over the street's wall."""
    sky, w = SKY['city'], width('city')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 92, [(34, 240), (10, 70)], 91), lighter(sky, .2), lighter(sky, .12))
    h = ridge(w, 40, [(8, 260), (4, 80)], 92)
    mountains(far, h, lighter(sky, .08), lighter(sky, .04))
    crenels(far, h, lighter(sky, .02), 6, 8)
    gx = w // 2; base = 150 - h[gx] - 6
    gate = far.rect(gx - 30, base - 18, gx + 30, base) | far.poly([(gx - 44, base - 18), (gx + 44, base - 18), (gx + 30, base - 36), (gx - 30, base - 36)])
    gate |= far.poly([(gx - 34, base - 36), (gx + 34, base - 36), (gx + 22, base - 50), (gx - 22, base - 50)])
    far.paint(gate, darker(sky, .08), inside=False)
    far.paint(far.ellipse(gx - 8, base - 14, gx + 8, base + 6) & far.rect(0, 0, w, base), darker(sky, .3), inside=False)
    near = Canvas(w, 70)
    rng = np.random.default_rng(93)
    roofs(near, 46, darker(sky, .3), darker(sky, .4), rng)
    stone, stone2 = darker(sky, .18), darker(sky, .26)
    near.paint(near.rect(0, 50, w - 1, 69), stone, inside=False)
    for row, y in enumerate(range(50, 70, 5)):
        near.paint(near.rect(0, y, w - 1, y), stone2, inside=False)
        for x in range((row % 2) * 7, w, 14): near.paint(near.rect(x, y, x, y + 4), stone2, inside=False)
    return save(far, 'bg_city_far'), save(near, 'bg_city_near')

def hall():
    """The examination hall in the gold of the afternoon: hills, the great hall's roof, ginkgo trees over a wall."""
    sky, w = SKY['hall'], width('hall')
    far = Canvas(w, 150)
    mountains(far, ridge(w, 84, [(26, 300), (10, 90)], 101), lighter(sky, .12), lighter(sky, .06))
    gx, base = w // 2, 132
    hall_ = far.rect(gx - 60, base - 16, gx + 60, 149) | far.poly([(gx - 80, base - 16), (gx + 80, base - 16), (gx + 58, base - 44), (gx - 58, base - 44)])
    far.paint(hall_, darker(sky, .12), inside=False)
    near = Canvas(w, 80)
    rng = np.random.default_rng(102)
    for x in range(40, w - 40, 120):
        x += int(rng.integers(-20, 20)); r = int(rng.integers(16, 22))
        leaf(near, x, r, rng, (196, 170, 74), (170, 142, 60), (226, 202, 104))
    stone, stone2, top = (176, 160, 140), (146, 132, 116), (92, 84, 88)
    near.paint(near.rect(0, 50, w - 1, 79), stone, inside=False)
    for row, y in enumerate(range(50, 80, 5)):
        near.paint(near.rect(0, y, w - 1, y), stone2, inside=False)
        for x in range((row % 2) * 7, w, 14): near.paint(near.rect(x, y, x, y + 4), stone2, inside=False)
    near.paint(near.rect(0, 46, w - 1, 49), top, inside=False)
    for x in range(0, w, 6): near.paint(near.rect(x, 45, x + 3, 45), top, inside=False)
    return save(far, 'bg_hall_far'), save(near, 'bg_hall_near')

def manor():
    """The minister's house by night: a red moon over the palace roofs; near, his high wall with lit lanterns."""
    sky, w = SKY['manor'], width('manor')
    far = Canvas(w, 150)
    mx = w // 4
    moon = far.ellipse(mx - 20, 4, mx + 20, 44)
    far.paint(far.ellipse(mx - 28, 0, mx + 28, 52), lighter(sky, .06), inside=False)
    far.paint(moon, (196, 70, 60), inside=False); far.paint(moon & far.ellipse(mx - 6, 4, mx + 30, 40), (164, 52, 50), inside=False)
    mountains(far, ridge(w, 70, [(22, 300), (8, 80)], 111), lighter(sky, .06), lighter(sky, .02))
    rng = np.random.default_rng(112)
    roofs(far, 140, darker(sky, .2), darker(sky, .35), rng, gap=(30, 60), size=(60, 110), height=(16, 26))
    far.paint(far.rect(0, 140, w - 1, 149), darker(sky, .2), inside=False)
    near = Canvas(w, 80)
    wall, wall2, top = darker(sky, .1), darker(sky, .25), darker(sky, .45)
    near.paint(near.rect(0, 40, w - 1, 79), wall, inside=False)
    for y in range(40, 80, 6): near.paint(near.rect(0, y, w - 1, y), wall2, inside=False)
    near.paint(near.rect(0, 34, w - 1, 39), top, inside=False)
    for x in range(0, w, 6): near.paint(near.rect(x, 33, x + 3, 33), top, inside=False)
    for x in range(60, w - 40, 140):
        near.paint(near.rect(x, 24, x, 33), top, inside=False)
        near.paint(near.ellipse(x - 5, 14, x + 5, 26), (200, 64, 52), inside=False)
        near.paint(near.ellipse(x - 2, 17, x + 2, 22), (255, 180, 120), inside=False)
    return save(far, 'bg_manor_far'), save(near, 'bg_manor_near')

names = [p for fn in (stele, camp, village, kong, forest, ferry, pass_, fort, station, grave, city, hall, manor) for p in fn()]
if preview:
    ims = [Image.open(p).convert("RGBA") for p in names]
    for area in AREAS:
        f, n = [Image.open(f"{OUT}/bg_{area}_{k}.png").convert("RGBA") for k in ("far", "near")]
        sheet = Image.new("RGBA", (f.width, 170), SKY[area] + (255,))
        sheet.alpha_composite(f, (0, 170 - 20 - f.height)); sheet.alpha_composite(n, ((f.width - n.width) // 2, 170 - 20 - n.height))
        sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{preview}/bg_{area}.png")
