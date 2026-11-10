# Draws the combat and ambience effects, at the same dot size as the props and characters' effects (24 dots to the unit).
# They have no black outline: light and dust, not things. Colour comes from the game, so most are drawn in white and pale
# grey and tinted when spawned.
#   20_Fx/arc_0..2      the sword's crescent, facing right, thick then thinning (the same shape as in sword_attack_02)
#   20_Fx/spark_0..3    the star of an impact, opening and breaking up
#   20_Fx/dust_0..3     a puff of dust
#   20_Fx/ring_0..3     the ground shock of a heavy landing, seen edge-on
#   20_Fx/glint         the flash before a blow
# Usage: python make_fx.py Assets/Resources/JinaV4/20_Fx
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image, ImageDraw
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
WHITE, PALE, MID = (255, 255, 255), (214, 222, 240), (168, 180, 210)

def save(rgb, a, name):
    img = np.zeros(a.shape + (4,), np.uint8); img[..., :3] = rgb; img[..., 3] = np.where(a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")

def mask(w, h, draw):
    im = Image.new('1', (w, h)); draw(ImageDraw.Draw(im)); return np.array(im, bool)

def arc(frame):
    """A crescent bulging forward: pointed at both ends, thick in the middle, a paler inner edge."""
    w, h = 38, 58
    cx, cy = 6, h / 2
    outer = [(cx + 29 * math.cos(t), cy + 28 * math.sin(t)) for t in np.linspace(-1.5, 1.5, 40)]
    thick = (12, 8, 4)[frame]
    inner = [(cx + (29 - thick) * math.cos(t), cy + 28 * math.sin(t)) for t in np.linspace(1.5, -1.5, 40)]
    a = mask(w, h, lambda d: d.polygon(outer + inner, fill=1))
    inner2 = [(cx + (29 - thick * .55) * math.cos(t), cy + 26 * math.sin(t)) for t in np.linspace(1.4, -1.4, 40)]
    core = mask(w, h, lambda d: d.polygon(outer + inner2, fill=1)) & a
    rgb = np.zeros((h, w, 3), np.uint8)
    rgb[a] = MID; rgb[core] = WHITE
    # It frays as it fades.
    if frame == 2:
        rng = np.random.default_rng(3)
        a &= rng.random(a.shape) > .25
    save(rgb, a, f"arc_{frame}")

def spark(frame):
    s = 21; c = s // 2
    rgb = np.zeros((s, s, 3), np.uint8); a = np.zeros((s, s), bool)
    def dot(x, y, col):
        if 0 <= x < s and 0 <= y < s: a[y, x] = True; rgb[y, x] = col
    length = (4, 8, 10, 10)[frame]; start = (0, 0, 4, 7)[frame]
    for i in range(start, length + 1):
        col = WHITE if i < length - 1 else PALE
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            dot(c + dx * i, c + dy * i, col)
            if i < length - 2 and frame < 2:
                dot(c + dx * i + dy, c + dy * i + dx, col)
        if i < length * .7:
            for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)): dot(c + dx * i, c + dy * i, PALE)
    if frame < 2:
        for y in range(s):
            for x in range(s):
                if (x - c) ** 2 + (y - c) ** 2 <= (2 + frame) ** 2: dot(x, y, WHITE)
    save(rgb, a, f"spark_{frame}")

def dust(frame):
    s = 15
    rng = np.random.default_rng(10)
    blobs = [(4, 9, 3), (8, 7, 4), (11, 10, 3), (6, 11, 3), (9, 11, 3)]
    grow = (.55, .85, 1.05, 1.1)[frame]
    Y, X = np.mgrid[:s, :s]
    a = np.zeros((s, s), bool)
    for cx, cy, r in blobs:
        cx2 = 7 + (cx - 7) * (1 + frame * .22); cy2 = 10 + (cy - 10) * (1 + frame * .35) - frame
        a |= (X - cx2) ** 2 + (Y - cy2) ** 2 <= (r * grow) ** 2
    light = np.zeros_like(a); light[1:] = a[:-1]
    top = a & ~light
    rgb = np.zeros((s, s, 3), np.uint8); rgb[a] = (200, 196, 190); rgb[(Y + X * .2 < 8 + frame) & a] = (232, 228, 220); rgb[top] = (246, 244, 238)
    if frame >= 2:
        hole = rng.random(a.shape) < (.2 if frame == 2 else .5)
        a &= ~hole
    save(rgb, a, f"dust_{frame}")

def ring(frame):
    w, h = 72, 12
    Y, X = np.mgrid[:h, :w]
    r = (14, 24, 32, 35)[frame]
    t = (5, 4, 3, 2)[frame]
    d = ((X - w / 2) / r) ** 2 + ((Y - h / 2) / (r * .15 + 1)) ** 2
    d2 = ((X - w / 2) / max(1, r - t)) ** 2 + ((Y - h / 2) / max(.5, (r - t) * .15 + .6)) ** 2
    a = (d <= 1) & (d2 > 1)
    rgb = np.zeros((h, w, 3), np.uint8); rgb[a] = WHITE; rgb[(Y > h / 2) & a] = PALE
    save(rgb, a, f"ring_{frame}")

def glint():
    s = 15; c = s // 2
    rgb = np.zeros((s, s, 3), np.uint8); a = np.zeros((s, s), bool)
    for i in range(0, c + 1):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            a[c + dy * i, c + dx * i] = True; rgb[c + dy * i, c + dx * i] = WHITE
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        a[c + dy, c + dx] = True; rgb[c + dy, c + dx] = PALE
    save(rgb, a, "glint")

def seal():
    """The seal across the stele road: a veil of violet light on a strip that tiles upward, with paper charms on a rope."""
    w, h = 40, 72
    rng = np.random.default_rng(8)
    Y, X = np.mgrid[:h, :w]
    a = np.zeros((h, w), bool); rgb = np.zeros((h, w, 3), np.uint8)
    # The veil, densest at its heart, its dots falling away toward the edges; the pattern repeats up the strip.
    bayer = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16
    glow = 1 - np.abs(X - w / 2 + .5) / (w / 2)
    wave = .12 * np.sin(2 * np.pi * Y / h * 2 + X * .2)
    a |= (glow + wave) ** 1.2 > bayer[Y % 4, X % 4] + .1
    rgb[a] = (148, 96, 214)
    core = ((glow + wave) > .72) & a
    rgb[core] = (196, 158, 244)
    # The rope with a charm hanging from it.
    rgb[10:12, 2:38] = (120, 92, 60); a[10:12, 2:38] = True
    for x0 in (8, 26):
        rgb[12:30, x0:x0 + 7] = (240, 222, 150); a[12:30, x0:x0 + 7] = True
        rgb[12:30, x0] = (60, 40, 40); rgb[12:30, x0 + 6] = (60, 40, 40); rgb[29, x0:x0 + 7] = (60, 40, 40)
        rgb[15:27, x0 + 3] = (186, 50, 44); rgb[18, x0 + 2:x0 + 5] = (186, 50, 44); rgb[23, x0 + 1:x0 + 6] = (186, 50, 44)
    save(rgb, a, "seal")

for i in range(3): arc(i)
for i in range(4): spark(i); dust(i); ring(i)
glint()
seal()
