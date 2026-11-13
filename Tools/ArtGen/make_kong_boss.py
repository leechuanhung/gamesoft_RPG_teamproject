# Draws the Kongjwi demon (기획서.md 1-1) on the village people's 12 px blocks (make_kong.py), facing left:
# a great bottomless crock with a toad wedged in it. The toad's head bulges out of the rim with the red eyes of the
# night at Kongjwi's house, its hind legs kick out of the broken bottom, and Kongjwi's own yellow sleeves come out
# of holes in the crock's sides as its front legs, her flower pin and ribbon caught on the toad.
#   21_KongBoss/kong_boss_<pose>: idle_00, idle_01 (throat puffed), crouch, hop, open (spit, roar), tongue (head
#   lowered, mouth open level with his chest), cast (sleeves raised), crack (the purifying light breaking it),
#   death_00..02 (slumped, then crumbling). Every frame keeps the same 72x66 block canvas, feet on its bottom row
#   and centre, so the game stands them all on one point.
# Props at one texel per art pixel, 24 per unit (13_Props): kong_spit, kong_bean, kong_salt, kong_brine,
#   kong_tongue (a segment, tiled), kong_tongue_tip, kong_bind (the curse cords over a villager), crock, crock_cracked,
#   crock_broken, kong_honey (the dish a broken crock spills),
#   ledge_roof (a thatched roof slab to stand on).
# Usage: python make_kong_boss.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
import framelib
ROOT = sys.argv[1]
W, H, BLOCK = 72, 74, 12
# Poses are laid out on a 66-row figure; DY rows of headroom above it keep the bulging eyes on the canvas.
DY = 8
FEET = H - 1

ONGGI, ONGGI2, ONGGI3, HOLE = (120, 70, 46), (86, 48, 32), (160, 100, 66), (40, 22, 16)
TOAD, TOAD2, TOAD3, BELLY = (112, 124, 66), (80, 90, 48), (146, 156, 88), (214, 196, 140)
RED, RED2, MOUTH, TONGUE, TONGUE2 = (196, 58, 58), (150, 38, 44), (96, 26, 36), (220, 96, 110), (176, 64, 82)
YELLOW, YELLOW2, PINK, SKIN, SKIN2, WHITE = (244, 214, 110), (214, 178, 70), (240, 150, 170), (249, 196, 140), (226, 163, 109), (255, 255, 255)
LIGHT = (255, 244, 200)

def jar(c, top, bottom, x0=14, x1=58):
    """The crock: round body, darker on the far side, a glaze band at the shoulder, cracks, a jagged hole for a bottom."""
    body = c.ellipse(x0, top, x1, bottom + 10) & c.rect(0, 0, W, bottom)
    c.layer(body, ONGGI)
    c.paint(body & c.ellipse(x0 + 18, top - 6, x1 + 14, bottom + 14), ONGGI2)
    c.paint(body & c.rect(0, top + 7, W, top + 9), ONGGI2)
    c.paint(c.rect(x0 + 6, top + 12, x0 + 7, top + 20) | c.rect(x0 + 8, top + 10, x0 + 9, top + 11), ONGGI3)
    c.paint(c.line([(x0 + 22, top + 12), (x0 + 25, top + 19), (x0 + 22, top + 25)]) & body, K)
    c.paint(c.line([(x1 - 8, top + 20), (x1 - 12, top + 27)]) & body, K)
    # the broken bottom: a ragged dark mouth along the lowest rows
    rag = [(x0 + 7, bottom), (x0 + 10, bottom - 4), (x0 + 15, bottom - 2), (x0 + 19, bottom - 6), (x0 + 24, bottom - 3),
           (x0 + 29, bottom - 6), (x0 + 33, bottom - 2), (x1 - 7, bottom - 4), (x1 - 4, bottom)]
    c.paint(c.poly(rag) & body, HOLE)
    rim = c.rect(x0 + 9, top - 3, x1 - 9, top)
    c.layer(rim, ONGGI2); c.paint(c.rect(x0 + 9, top - 3, x1 - 9, top - 3), ONGGI3)

def legs(c, bottom, spread, reach):
    """The toad's hind legs out of the broken bottom: thighs bent out, webbed feet on the ground (or dangling)."""
    for side in (-1, 1):
        hip = (36 + side * 9, bottom - 3)
        knee = (36 + side * (12 + spread), bottom + 1 + reach // 2)
        foot = (36 + side * (14 + spread), FEET - 2 + (reach - 4 if reach > 4 else 0))
        thigh = c.line([hip, knee], 6) | c.line([knee, foot], 4)
        c.layer(thigh, TOAD if side < 0 else TOAD2)
        fx, fy = foot
        web = c.poly([(fx - 5, fy + 2), (fx + 5, fy + 2), (fx + side * 2, fy - 2)])
        c.layer(web, TOAD2 if side > 0 else TOAD)
        for t in (-4, 0, 4): c.paint(c.rect(fx + t, fy + 1, fx + t, fy + 2), TOAD3)

def sleeve(c, start, elbow, hand, front, raised=False):
    """One of Kongjwi's arms out of a hole in the crock: yellow sleeve bent at the elbow, pink cuff, the hand braced
    on the ground (or raised, fingers spread)."""
    sx, sy = start; hx, hy = hand
    arm = c.line([start, elbow, hand], 7)
    c.layer(c.ellipse(sx - 3, sy - 3, sx + 3, sy + 3), HOLE)
    c.layer(arm, YELLOW if front else YELLOW2)
    if front: c.paint(c.line([(sx + 1, sy + 3), (elbow[0] + 1, elbow[1] + 3), (hx + 2, hy)], 2) & arm, YELLOW2)
    dx, dy = hx - elbow[0], hy - elbow[1]; n = max(1, (dx * dx + dy * dy) ** .5)
    cx, cy = int(round(hx - dx / n * 3)), int(round(hy - dy / n * 3))
    c.layer(c.ellipse(cx - 3, cy - 3, cx + 3, cy + 3), PINK)
    if raised:
        c.layer(c.rect(hx - 2, hy - 4, hx + 2, hy) | c.rect(hx - 3, hy - 6, hx - 2, hy - 4) | c.rect(hx + 1, hy - 6, hx + 2, hy - 4), SKIN)
    else:
        c.layer(c.rect(hx - 4, hy, hx + 3, hy + 2) | c.rect(hx - 5, hy + 2, hx + 4, hy + 2), SKIN)
        c.paint(c.rect(hx - 4, hy + 2, hx + 4, hy + 2), SKIN2)

def head(c, x, y, mouth=0, eyes='red', droop=0):
    """The toad's head, snout to the left, top at (x, y): bulging eyes, warts, a wide mouth that opens `mouth` rows."""
    w, h = 40, 18
    skull = c.ellipse(x, y + 2, x + w, y + h)
    jaw = c.ellipse(x + 2, y + 9 + mouth, x + w - 2, y + h + 4 + mouth)
    if mouth:
        inside = c.poly([(x + 2, y + 11), (x + w - 8, y + 10), (x + w - 8, y + 11 + mouth + 4), (x + 4, y + 12 + mouth)])
        c.layer(inside, MOUTH)
        c.paint(c.ellipse(x + 10, y + 12 + mouth - 2, x + 26, y + 15 + mouth), TONGUE)
    c.layer(jaw, BELLY); c.paint(jaw & c.rect(x + w // 2 + 6, 0, W, H), (186, 168, 116))
    upper = skull & c.rect(0, 0, W, y + 10 + (0 if mouth else 3))
    c.layer(upper | (skull if not mouth else c.blank()), TOAD)
    c.paint(upper & c.ellipse(x + 18, y - 6, x + w + 10, y + 14), TOAD2)
    for wx, wy in ((x + 9, y + 6), (x + 16, y + 4), (x + 27, y + 7), (x + 33, y + 4), (x + 22, y + 9)):
        c.paint(c.rect(wx, wy, wx + 1, wy), TOAD3)
    if mouth:
        for fx in (x + 5, x + 10, x + 16): c.layer(c.rect(fx, y + 11, fx, y + 12), WHITE)   # fangs on the upper jaw
    else:
        c.paint(c.line([(x + 1, y + 11), (x + 14, y + 13), (x + 30, y + 12)]), K)
    # the eyes bulging on top; the far one a little smaller
    for ex, ey, r in ((x + 9, y - 1, 5), (x + 23, y - 2, 4)):
        c.layer(c.ellipse(ex - r - 1, ey - r - 1, ex + r + 1, ey + r + 1), TOAD)
        ball = c.ellipse(ex - r + 1, ey - r + 1 + droop, ex + r - 1, ey + r - 1)
        if eyes == 'red':
            c.paint(ball, RED); c.paint(ball & c.rect(0, ey + 1, W, H), RED2)
            c.paint(c.rect(ex - r + 2, ey, ex + r - 2, ey), K)                     # a slit pupil
            c.paint(c.rect(ex - 1, ey - 2, ex - 1, ey - 2), WHITE)
        elif eyes == 'white':
            c.paint(ball, LIGHT)
        else:
            c.paint(ball, TOAD2); c.paint(c.rect(ex - r + 2, ey, ex + r - 2, ey), K)   # shut
    # Kongjwi's flower pin caught in the warts behind the eyes
    c.layer(c.rect(x + 31, y + 2, x + 33, y + 4), PINK); c.paint(c.rect(x + 32, y + 3, x + 32, y + 3), WHITE)

def ribbon(c, top):
    """Her pink ribbon, snagged on the rim and hanging down the far side."""
    c.layer(c.rect(46, top - 2, 48, top + 10) | c.rect(49, top + 2, 51, top + 14), PINK)
    c.paint(c.rect(46, top + 8, 48, top + 10), (214, 120, 140))

def boss(pose):
    c = Canvas(W, H)
    top, bottom, hx, hy, mouth, eyes, droop = 24, 58, 12, 7, 0, 'red', 0
    arms = ((17, 40), (7, 46), (5, 59)), ((55, 40), (65, 46), (66, 59))
    spread, reach = 0, 0
    if pose == 'idle_01': hy += 1
    if pose == 'crouch': top, bottom, hy, spread = 28, 60, 12, 3; arms = ((17, 44), (5, 50), (3, 60)), ((55, 44), (67, 50), (68, 60))
    if pose == 'hop': top, bottom, hy, reach = 18, 52, 1, 8; arms = ((17, 34), (8, 38), (2, 45)), ((55, 34), (64, 38), (69, 45))
    if pose in ('open', 'cast'): mouth, hy = 7, 2
    if pose == 'tongue': hx, hy, mouth = 2, 22, 6
    if pose == 'cast': arms = ((17, 38), (8, 30), (8, 16), True), ((55, 38), (64, 30), (64, 16), True)
    if pose == 'crack': eyes, mouth, hy = 'white', 4, 4
    if pose.startswith('death'): eyes, hx, hy, droop = 'shut', 6, 16, 0
    top, bottom, hy = top + DY, bottom + DY, hy + DY
    arms = tuple(tuple((x, y + DY) for x, y in arm[:3]) + arm[3:] for arm in arms)
    back, front = arms
    sleeve(c, back[0], back[1], back[2], False, len(back) > 3)
    legs(c, bottom, spread, reach)
    jar(c, top, bottom)
    if pose != 'tongue': ribbon(c, top)
    head(c, hx, hy, mouth, eyes, droop)
    if pose == 'idle_01': c.layer(c.ellipse(hx + 6, hy + 16, hx + 22, hy + 24), BELLY)   # the throat sac puffed out
    sleeve(c, front[0], front[1], front[2], True, len(front) > 3)
    if pose == 'crack':
        seams = c.line([(30, top + 2), (34, top + 12), (30, top + 20), (36, top + 30)], 2) | c.line([(46, top + 6), (42, top + 16), (48, top + 26)], 2) \
            | c.line([(22, top + 14), (26, top + 24), (22, bottom - 4)], 2)
        c.paint(seams & c.a, LIGHT)
    return c

def image(c):
    img = np.zeros((H, W, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    return np.array(Image.fromarray(img).resize((W * BLOCK, H * BLOCK), Image.NEAREST))

os.makedirs(f"{ROOT}/21_KongBoss", exist_ok=True)
for pose in ('idle_00', 'idle_01', 'crouch', 'hop', 'open', 'tongue', 'cast', 'crack', 'death_00'):
    Image.fromarray(image(boss(pose))).save(f"{ROOT}/21_KongBoss/kong_boss_{pose}.png")
slump = image(boss('death_00'))
for i, level in ((1, .35), (2, .7)):
    Image.fromarray(framelib.scatter(slump, level, BLOCK * 2, seed=11)).save(f"{ROOT}/21_KongBoss/kong_boss_death_{i:02d}.png")

# ---- props: what she throws, the cords she binds with, the arena's crocks and roofs ----
def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{ROOT}/13_Props/{name}.png")

def spit():
    """A gob of salted fish: grey-pink paste with silver fish bodies sticking out."""
    c = Canvas(14, 11)
    c.layer(c.ellipse(1, 2, 12, 10), (198, 150, 150)); c.paint(c.ellipse(5, 5, 12, 10), (160, 112, 118))
    c.layer(c.rect(3, 1, 7, 2), (196, 200, 210)); c.layer(c.rect(8, 4, 12, 4), (196, 200, 210))
    c.paint(c.rect(3, 1, 3, 1), K)
    return c
def bean():
    c = Canvas(6, 5); c.layer(c.ellipse(1, 1, 4, 3), (224, 196, 120)); c.paint(c.rect(3, 2, 3, 2), (176, 140, 72)); return c
def salt():
    c = Canvas(8, 8); c.layer(c.rect(2, 2, 5, 5) | c.rect(1, 3, 6, 4) | c.rect(3, 1, 4, 6), (246, 246, 240))
    c.paint(c.rect(4, 4, 5, 5), (200, 206, 214)); return c
def brine():
    """A puddle of salt water left where a gob landed; the game lays it flat on the ground and fades it."""
    c = Canvas(40, 5)
    c.layer(c.ellipse(1, 1, 38, 6) & c.rect(0, 0, 40, 3), (150, 176, 186))
    c.paint(c.rect(6, 1, 30, 1), (210, 226, 230)); c.paint(c.rect(12, 2, 14, 2) | c.rect(26, 2, 27, 2), (198, 150, 150))
    return c
def tongue():
    c = Canvas(8, 6); c.layer(c.rect(0, 1, 7, 4), TONGUE); c.paint(c.rect(0, 4, 7, 4), TONGUE2); return c
def tongue_tip():
    c = Canvas(12, 10); c.layer(c.ellipse(1, 1, 10, 8), TONGUE); c.paint(c.ellipse(4, 4, 10, 8), TONGUE2)
    c.paint(c.rect(3, 3, 4, 3), (250, 170, 180)); return c
def bind():
    """The curse's cords: three bands of black-violet rope tied with a red paper charm, sized to a villager."""
    c = Canvas(22, 38)
    for y in (6, 17, 28):
        c.layer(c.line([(1, y + 2), (11, y), (20, y + 2)], 2), (70, 40, 92))
        c.paint(c.line([(1, y + 2), (11, y)]), (110, 70, 140))
    c.layer(c.rect(9, 18, 13, 25), (200, 60, 50)); c.paint(c.rect(10, 20, 12, 20) | c.rect(11, 22, 11, 23), (60, 20, 20))
    return c
def crock():
    """A single great crock of the hill's pantry; it stops what she throws until it breaks."""
    c = Canvas(30, 34)
    body = c.ellipse(1, 6, 28, 40) & c.rect(0, 0, 30, 33)
    c.layer(body, ONGGI); c.paint(body & c.ellipse(13, 2, 40, 44), ONGGI2)
    c.paint(c.rect(6, 11, 7, 18), ONGGI3)
    c.layer(c.rect(7, 2, 22, 5), ONGGI2); c.paint(c.rect(7, 2, 22, 2), ONGGI3)
    return c
def crock_cracked():
    """The same crock after its first blow: two black cracks running down from the shoulder."""
    c = crock()
    c.paint(c.line([(10, 8), (12, 13), (10, 18), (12, 22)]) & c.a, K)
    c.paint(c.line([(19, 10), (17, 15)]) & c.a, K)
    return c
def honey():
    """A little honey pot that a broken crock spills, gold running over its rim; touching it heals."""
    c = Canvas(13, 13)
    c.layer(c.ellipse(1, 3, 11, 12) & c.rect(0, 0, 13, 11), (214, 168, 96)); c.paint(c.ellipse(6, 2, 14, 13) & c.a, (178, 132, 70))
    c.layer(c.rect(3, 1, 9, 3), (240, 176, 48)); c.paint(c.rect(4, 1, 6, 1), (255, 226, 130))
    c.paint(c.rect(3, 4, 4, 7) & c.a, (240, 176, 48)); c.paint(c.rect(4, 8, 4, 8) & c.a, (240, 176, 48))
    return c
def crock_broken():
    c = Canvas(30, 34)
    base = c.ellipse(1, 6, 28, 40) & c.rect(0, 24, 30, 33)
    c.layer(base | c.poly([(3, 24), (8, 16), (11, 24)]) | c.poly([(18, 24), (23, 18), (27, 24)]), ONGGI)
    c.paint(c.rect(4, 26, 25, 27), HOLE)
    c.paint(base & c.ellipse(13, 2, 40, 44), ONGGI2)
    return c
def ledge_roof():
    """A slab of fallen thatch on two beams, 3 units (72 dots) wide like the other ledges."""
    STRAW, STRAW2, STRAW3, WOOD = (214, 178, 106), (166, 128, 70), (236, 206, 140), (122, 82, 50)
    c = Canvas(72, 18)
    c.layer(c.rect(5, 12, 66, 15), WOOD)
    thatch = c.poly([(1, 4), (8, 1), (64, 1), (71, 4), (70, 11), (2, 11)])
    c.layer(thatch, STRAW); c.paint(thatch & c.rect(0, 8, 72, 18), STRAW2)
    for x in range(5, 68, 5): c.paint(c.line([(x, 3), (x - 1, 10)]) & thatch, STRAW2)
    c.paint(c.rect(4, 2, 67, 2) & thatch, STRAW3)
    return c

for name, draw in (("kong_spit", spit), ("kong_bean", bean), ("kong_salt", salt), ("kong_brine", brine), ("kong_tongue", tongue),
                   ("kong_tongue_tip", tongue_tip), ("kong_bind", bind), ("crock", crock), ("crock_cracked", crock_cracked), ("crock_broken", crock_broken), ("kong_honey", honey), ("ledge_roof", ledge_roof)):
    save(draw(), name)
