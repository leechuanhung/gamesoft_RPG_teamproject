# Draws the background layers behind each area at the props' density (one texel per art pixel, 24 per unit).
# They are silhouettes in flat tones close to each area's sky, one shade each and no black outline, so the
# outlined characters and props stay in front: a far layer (mountains, moon, distant pines) that drifts with
# the camera, and a near layer (walls, graves, standing stones, pines) that stays put.
#   bg_<area>_far.png, bg_<area>_near.png for stele, camp, village, forest, ferry, pass
# Usage: python make_backdrops.py Assets/Resources/JinaV4/13_Props   [--preview DIR]
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas
OUT = sys.argv[1]
preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
PPU = 546 / 1.9 / 12            # art pixels per unit, as JinaArt.PropPixelsPerUnit
# Area spans in units (PrototypeWorld): the layers are 4 units wider and fade out at their ends.
AREAS = {'stele': 30, 'camp': 28, 'village': 40, 'forest': 36, 'ferry': 46, 'pass': 48}
SKY = {'stele': (43, 46, 71), 'camp': (77, 51, 51), 'village': (61, 77, 87), 'forest': (26, 59, 51), 'ferry': (58, 76, 94), 'pass': (70, 66, 86)}

def mix(a, b, t): return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))
def lighter(c, t): return mix(c, (236, 232, 220), t)
def darker(c, t): return mix(c, (8, 8, 12), t)

def ridge(w, base, amps, seed, taper=60):
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
    Image.fromarray(img).save(f"{OUT}/{name}.png")
    return f"{OUT}/{name}.png"

def width(area): return int(round((AREAS[area] + 4) * PPU))

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
    mountains(far, ridge(w, 80, [(24, 330), (12, 120)], 11), lighter(sky, .16), lighter(sky, .09))
    mountains(far, ridge(w, 50, [(12, 220), (6, 80)], 12), lighter(sky, .07), lighter(sky, .02))
    near = Canvas(w, 70)
    # trees showing over the wall, then a low dry-stone wall (doldam) with tiles along its top and a few gaps
    rng = np.random.default_rng(15)
    for x in range(40, w - 40, 110):
        x += int(rng.integers(-20, 20)); r = int(rng.integers(14, 20))
        crown = near.ellipse(x - r, 46 - 2 * r, x + r, 46) | near.ellipse(x - r - 8, 40 - r, x + 4, 50)
        near.paint(near.rect(x - 2, 40, x + 2, 50), darker(sky, .3), inside=False)
        near.paint(crown, darker(sky, .18), inside=False)
        near.paint(crown & near.rect(x + 4, 0, w, 70), darker(sky, .28), inside=False)
    stone, stone2, top = mix(sky, (150, 148, 160), .55), mix(sky, (112, 110, 124), .55), mix(sky, (70, 66, 78), .5)
    gaps = [(int(w * f), int(w * f) + 26) for f in (.22, .61)]
    keep = near.rect(0, 0, w - 1, 69)
    for g0, g1 in gaps: keep &= ~near.rect(g0, 0, g1, 69)
    wall = near.rect(0, 50, w - 1, 69) & keep
    near.paint(wall, stone, inside=False)
    for row, y in enumerate(range(50, 70, 5)):
        near.paint(near.rect(0, y, w - 1, y) & keep, stone2, inside=False)
        for x in range((row % 2) * 7, w, 14): near.paint(near.rect(x, y, x, y + 4) & keep, stone2, inside=False)
    near.paint(near.rect(0, 46, w - 1, 49) & keep, top, inside=False)
    for x in range(0, w, 6): near.paint(near.rect(x, 45, x + 3, 45) & keep, top, inside=False)
    return save(far, 'bg_village_far'), save(near, 'bg_village_near')

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
        near.paint(trunk, darker(sky, .32), inside=False)
        top = 109 - h
        crown = near.ellipse(x - 30, top - 10, x + 30, top + 14)
        near.paint(crown, darker(sky, .2), inside=False)
        near.paint(crown & near.rect(x + 8, 0, w, 200), darker(sky, .3), inside=False)
        for dx in range(-28, 29, 3):   # long strands hanging from the crown (none past the left edge: rect() would wrap)
            if x + dx < 0: continue
            length = 34 + (28 - abs(dx)) + (dx * 7) % 11
            near.paint(near.rect(x + dx, top + 4, x + dx + 1, top + length), darker(sky, .2 if dx < 8 else .3), inside=False)
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

names = [p for fn in (stele, camp, village, forest, ferry, pass_) for p in fn()]
if preview:
    ims = [Image.open(p).convert("RGBA") for p in names]
    for area in AREAS:
        f, n = [Image.open(f"{OUT}/bg_{area}_{k}.png").convert("RGBA") for k in ("far", "near")]
        sheet = Image.new("RGBA", (f.width, 170), SKY[area] + (255,))
        sheet.alpha_composite(f, (0, 170 - 20 - f.height)); sheet.alpha_composite(n, ((f.width - n.width) // 2, 170 - 20 - n.height))
        sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{preview}/bg_{area}.png")
