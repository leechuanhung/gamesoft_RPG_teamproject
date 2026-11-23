# Draws the last boss (기획서.md 1-3): the Left State Councillor (좌의정) who rules through the boy king, and what he
# becomes. All on the seonbi's 12 px art pixels, facing left (toward the seonbi coming from the left).
#   25_Final/official_<pose>: the man, the seonbi's height and build: a red danryeong robe with a crane badge, a
#       horn belt slung under his belly, the black samo hat with its two wings, a long black beard.
#       idle, cup (holding out the drink), blank (his face gone smooth as an egg: the start of the change),
#       down (what is left when the eodugsini crumbles: the man on his back, still faceless; 56x56 canvas).
#       Feet on the bottom centre of a 40x56 block canvas.
#   25_Final/final_<pose>: the eodugsini councillor. The faceless egg head of the dalgyal-gwisin under his samo,
#       split from brow to chin by a mouth of teeth that opens sideways; red eyes in the hair that hangs to his
#       waist like a maiden ghost's; a neck stretched long and bent forward; the red robe torn open on darkness,
#       the crane badge with an eye in it; no legs, only a pool of shadow with grasping hands, coins spilling into
#       it; arms too long with too many joints, the state seal in one hand and an ink-dripping brush in the other.
#       idle_00, idle_01, open (mouth wide: coins, roar), reach (an arm swept along the ground), raise (the seal
#       lifted over his head), cast (both arms up: ink rain, hands from the ground), death_00..02.
#       Feet (the middle of the shadow pool) on the bottom centre of a 100x112 block canvas.
# Props at one texel per art pixel, 24 per unit, hung by their centre or standing (13_Props):
#   final_ink (a falling gob of ink), final_coin, final_hand (a shadow hand out of the ground), final_seal (the seal
#   alone, flung), final_mark (the red seal imprint left on the ground).
# Usage: python make_final_boss.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
import framelib
ROOT = sys.argv[1]
BLOCK = 12

SKIN, SKIN2, HAIR, HAIR2 = (249, 196, 140), (226, 163, 109), (30, 24, 28), (58, 46, 50)
ROBE, ROBE2, ROBE3 = (176, 42, 46), (124, 26, 36), (212, 78, 72)
BADGE, BADGE2, GOLD, GOLD2 = (40, 46, 96), (30, 32, 70), (236, 196, 92), (186, 142, 60)
HAT, HAT2, WHITE = (36, 34, 44), (58, 56, 70), (255, 255, 255)
EGG, EGG2, EGG3 = (236, 230, 214), (204, 196, 180), (250, 248, 240)
MAW, MAW2, TOOTH = (96, 18, 32), (60, 10, 24), (244, 238, 222)
VOID, VOID2, VOID3 = (24, 18, 30), (44, 32, 56), (72, 52, 88)
EYE, EYE2 = (232, 40, 40), (255, 160, 120)
JADE, JADE2, JADE3, SEALRED = (112, 164, 140), (80, 126, 108), (150, 200, 176), (200, 40, 40)
WOOD, WOOD2, INK, INK2 = (122, 82, 50), (92, 60, 36), (34, 26, 40), (70, 56, 90)
NAIL = (20, 16, 24)

# ---- the man ----
OW, OH, OGY, OFX = 40, 56, 53, 20
def official(pose):
    c = Canvas(OW, OH)
    fx = OFX
    # the robe to the ankles, fuller at the belly, slits at the side; black boots under it
    c.layer(c.rect(fx - 5, OGY - 2, fx - 1, OGY) | c.rect(fx + 1, OGY - 2, fx + 5, OGY) | c.rect(fx - 6, OGY, fx - 5, OGY), HAT)
    robe = c.poly([(fx - 4, 25), (fx + 5, 25), (fx + 8, 36), (fx + 8, OGY - 3), (fx - 8, OGY - 3), (fx - 8, 36)])
    c.layer(robe, ROBE)
    c.paint(robe & c.rect(fx + 4, 0, OW, OH), ROBE2)
    c.paint(robe & c.line([(fx + 1, 38), (fx + 2, OGY - 3)], 1), ROBE2)
    c.paint(robe & c.rect(fx - 7, 30, fx - 6, 44), ROBE3)
    # the crane badge on the chest, the belt under the belly
    badge = c.rect(fx - 4, 29, fx + 2, 35); c.layer(badge, BADGE)
    c.paint(c.rect(fx - 3, 31, fx + 1, 31) | c.rect(fx - 2, 30, fx - 2, 34) | c.rect(fx, 32, fx + 1, 33), WHITE)
    c.paint(c.rect(fx - 4, 29, fx + 2, 29) | c.rect(fx - 4, 35, fx + 2, 35), GOLD2)
    belt = c.poly([(fx - 9, 39), (fx + 8, 37), (fx + 8, 39), (fx - 9, 41)])
    c.layer(belt, HAT)
    for x in range(fx - 7, fx + 8, 3): c.paint(c.rect(x, 38 + (fx - x) // 8, x + 1, 38 + (fx - x) // 8) & belt, GOLD)
    # arms in wide red sleeves
    if pose == 'cup':
        sl = c.line([(fx - 2, 27), (fx - 6, 31), (fx - 11, 31)], 4)
        c.layer(sl, ROBE); c.paint(c.line([(fx - 6, 33), (fx - 11, 33)], 1) & sl, ROBE2)
        c.layer(c.rect(fx - 14, 30, fx - 12, 32), SKIN)
        c.layer(c.rect(fx - 15, 27, fx - 12, 29), WHITE); c.paint(c.rect(fx - 14, 27, fx - 13, 27), (230, 220, 170))   # the cup
    else:
        sl = c.line([(fx - 2, 27), (fx - 4, 33), (fx - 3, 38)], 4)
        c.layer(sl, ROBE); c.paint(c.line([(fx - 2, 33), (fx - 1, 38)], 1) & sl, ROBE2)
        c.layer(c.rect(fx - 5, 39, fx - 3, 40), SKIN)
    # the head, the beard, the samo
    hx, hy = fx - 1, 15
    head = c.rect(hx - 4, hy, hx + 3, hy + 8) | c.rect(hx - 3, hy + 9, hx + 2, hy + 9)
    c.layer(head, EGG if pose == 'blank' else SKIN)
    if pose == 'blank':
        c.paint(c.rect(hx + 1, hy, hx + 3, hy + 9) & head, EGG2)
        c.paint(c.rect(hx - 2, hy + 2, hx - 2, hy + 8), (180, 170, 156))              # a seam where the face was
    else:
        c.paint(c.rect(hx + 1, hy, hx + 3, hy + 6) & head, HAIR)                      # hair behind the ear
        c.paint(c.rect(hx + 1, hy + 4, hx + 1, hy + 5), SKIN2)
        c.paint(c.rect(hx - 4, hy + 3, hx - 2, hy + 3), HAIR)                          # a heavy brow
        c.paint(c.rect(hx - 3, hy + 4, hx - 3, hy + 4), K)                             # a narrow eye
        c.paint(c.rect(hx - 1, hy + 6, hx, hy + 6), SKIN2)
        beard = c.rect(hx - 4, hy + 7, hx - 1, hy + 12) | c.rect(hx - 3, hy + 13, hx - 2, hy + 15) | c.rect(hx - 5, hy + 6, hx - 2, hy + 6)
        c.layer(beard, HAIR); c.paint(c.rect(hx - 3, hy + 9, hx - 3, hy + 13), HAIR2)
        if pose == 'cup': c.paint(c.rect(hx - 4, hy + 7, hx - 3, hy + 7), (206, 84, 84))   # a smile in the beard
    crown = c.rect(hx - 4, hy - 6, hx + 3, hy) | c.rect(hx - 3, hy - 8, hx + 2, hy - 7)
    wings = c.ellipse(hx - 10, hy - 6, hx - 5, hy - 3) | c.ellipse(hx + 4, hy - 6, hx + 9, hy - 3)
    c.layer(wings, HAT); c.paint(wings & c.rect(0, hy - 4, OW, hy), HAT2)
    c.layer(c.line([(hx - 6, hy - 5), (hx - 4, hy - 5)]) | c.line([(hx + 3, hy - 5), (hx + 5, hy - 5)]), HAT)
    c.layer(crown, HAT); c.paint(c.rect(hx - 4, hy - 2, hx + 3, hy - 1), HAT2)
    return c

# ---- the creature ----
W, H = 100, 112
GY, FX = 111, 52
def creature(pose, t=0):
    c = Canvas(W, H)
    sway = {'idle_01': 1, 'open': -1, 'reach': -4, 'raise': 1, 'cast': 0, 'death_00': -2}.get(pose, 0)
    drop = {'idle_01': 2, 'death_00': 26, 'reach': 8}.get(pose, 0)
    hx, hy = 24 + sway * 2, 24 + drop                     # the head's top-left
    # the shadow pool and its grasping hands, coins spilling into it
    pool = c.ellipse(FX - 40, GY - 20, FX + 40, GY + 16) & c.rect(0, 0, W, GY)
    c.layer(pool, VOID)
    c.paint(pool & ~c.ellipse(FX - 38, GY - 18, FX + 38, GY + 18), VOID3)
    c.paint(pool & c.ellipse(FX - 30, GY - 14, FX + 22, GY + 4) & c.rect(0, 0, W, GY - 9), VOID2)
    for k, (x0, x1) in enumerate(((FX - 30, FX - 42), (FX + 28, FX + 42), (FX - 14, FX - 20))):
        tip_y = GY - 10 - (k % 2) * 4
        arm = c.line([(x0, GY - 3), ((x0 + x1) // 2, GY - 7), (x1, tip_y)], 2)
        c.layer(arm, VOID2)
        for f in (-2, 0, 2): c.layer(c.rect(x1 + f, tip_y - 3, x1 + f, tip_y - 1), VOID2)
    for x, y in ((FX - 26, GY - 5), (FX + 30, GY - 9), (FX + 22, GY - 3), (FX - 33, GY - 11)):
        c.layer(c.ellipse(x - 2, y - 2, x + 2, y + 2), GOLD); c.paint(c.rect(x, y, x, y), VOID)
    # the back arm, behind the body: up from the shoulder, the brush dripping ink
    back = {'idle_00': [(66, 52), (80, 64), (86, 80)], 'idle_01': [(66, 54), (81, 66), (87, 82)],
            'open': [(66, 50), (84, 46), (94, 34)], 'reach': [(64, 58), (78, 70), (84, 86)],
            'raise': [(66, 52), (80, 62), (88, 74)], 'cast': [(66, 50), (78, 34), (84, 16)],
            'death_00': [(64, 76), (80, 88), (92, 100)]}.get(pose)
    if back:
        back = [(x, y + (drop if pose != 'death_00' else 0)) if pose not in ('death_00',) else (x, y) for x, y in back]
        limb(c, back, VOID3, VOID2)
        bx, by = back[-1]
        brush = c.line([(bx, by - 8), (bx, by + 2)], 2); c.layer(brush, WOOD)
        c.layer(c.poly([(bx - 2, by + 3), (bx + 2, by + 3), (bx, by + 9)]), INK)
        c.paint(c.rect(bx, by + 11, bx, by + 12) | c.rect(bx - 1, by + 15, bx - 1, by + 15), INK, inside=False)
        hand(c, bx, by, 1)
    # the body: the torn red robe hanging from hunched shoulders into the shadow
    top = 46 + drop
    robe = c.poly([(40, top), (68, top + 2), (74, GY - 26), (78, GY - 16), (28, GY - 16), (33, GY - 28)])
    c.layer(robe, ROBE)
    c.paint(robe & c.poly([(60, top), (70, top), (78, GY - 16), (64, GY - 16)]), ROBE2)
    c.paint(robe & c.line([(36, top + 12), (34, GY - 12)], 1), ROBE3)
    for (x, y, r) in ((40, top + 28, 5), (64, top + 38, 5), (46, GY - 24, 4)):                   # torn open on darkness
        hole = c.ellipse(x - r, y - r * 2, x + r, y + r) & robe
        c.paint(hole, VOID); c.paint(hole & c.ellipse(x - r + 2, y - r * 2 + 2, x + r - 1, y), VOID2)
    # ragged hem
    for x in range(29, 78, 5): c.paint(c.poly([(x, GY - 18), (x + 4, GY - 18), (x + 2, GY - 11)]), ROBE2, inside=False)
    # the crane badge with an eye in it, and the belt slipping
    bx0, by0 = 53, top + 8
    badge = c.rect(bx0, by0, bx0 + 13, by0 + 12); c.layer(badge, BADGE)
    c.paint(c.rect(bx0, by0, bx0 + 13, by0) | c.rect(bx0, by0 + 12, bx0 + 13, by0 + 12), GOLD2)
    c.paint(c.line([(bx0 + 2, by0 + 3), (bx0 + 5, by0 + 5), (bx0 + 3, by0 + 9)]) | c.line([(bx0 + 11, by0 + 3), (bx0 + 8, by0 + 5), (bx0 + 10, by0 + 9)]), WHITE)
    c.layer(c.ellipse(bx0 + 4, by0 + 4, bx0 + 9, by0 + 8), WHITE); c.paint(c.rect(bx0 + 6, by0 + 5, bx0 + 7, by0 + 7), EYE)
    c.paint(c.rect(bx0 + 6, by0 + 6, bx0 + 6, by0 + 6), K)
    belt = c.poly([(30, top + 34), (74, top + 28), (74, top + 31), (30, top + 37)]) & robe
    c.paint(belt, HAT)
    for x in range(32, 74, 6): c.paint(c.rect(x, top + 33 - (x - 30) // 7, x + 2, top + 34 - (x - 30) // 7) & belt, GOLD)
    # the long neck, bent forward from the shoulders to the head
    neck_pts = [(48, top + 2), (44, top - 8), (hx + 14, hy + 30)]
    c.layer(c.line(neck_pts, 6), VOID2); c.paint(c.line([(p[0] + 2, p[1]) for p in neck_pts], 2), VOID3)
    # hair: long black strands from under the hat to the waist, with red eyes among them
    hair = c.poly([(hx + 12, hy + 6), (hx + 25, hy + 8), (hx + 26, hy + 30), (hx + 27, hy + 54), (hx + 23, hy + 58), (hx + 20, hy + 48), (hx + 17, hy + 60), (hx + 15, hy + 42), (hx + 12, hy + 30)])
    c.layer(hair, HAIR)
    for x in range(hx + 14, hx + 26, 3): c.paint(c.line([(x, hy + 10), (x + 1, hy + 56)], 1) & hair, HAIR2)
    for ex, ey in ((hx + 17, hy + 22), (hx + 21, hy + 32), (hx + 15, hy + 40), (hx + 22, hy + 47)):
        c.paint(c.rect(ex, ey, ex + 2, ey) & hair, EYE); c.paint(c.rect(ex + 1, ey, ex + 1, ey) & hair, EYE2)
    # the egg face, split from brow to chin
    mouth = {'open': 3, 'cast': 2, 'reach': 1, 'death_00': 0}.get(pose, 0)
    face = c.ellipse(hx, hy + 6, hx + 20, hy + 34)
    c.layer(face, EGG)
    c.paint(face & c.ellipse(hx + 8, hy + 6, hx + 24, hy + 36), EGG2)
    c.paint(face & c.ellipse(hx + 2, hy + 9, hx + 7, hy + 16), EGG3)
    cx = hx + 8
    if mouth == 0:
        c.paint(c.line([(cx, hy + 12), (cx - 1, hy + 20), (cx, hy + 30)], 1), MAW)
        for y in range(hy + 14, hy + 30, 3): c.paint(c.rect(cx - 1, y, cx + 1, y), MAW)                      # stitches
    else:
        w = 2 + mouth * 2
        maw = c.poly([(cx, hy + 9), (cx - w, hy + 16), (cx - w - 1, hy + 22), (cx - w, hy + 28), (cx, hy + 33), (cx + w, hy + 28), (cx + w + 1, hy + 22), (cx + w, hy + 16)])
        c.layer(maw, MAW); c.paint(maw & c.rect(cx - 1, 0, cx + 1, H), MAW2)
        for y in range(hy + 12, hy + 32, 3):
            for side in (-1, 1):
                edge = [x for x in range(cx - w - 1, cx + w + 2) if maw[y, x]]
                if edge:
                    xx = min(edge) if side < 0 else max(edge)
                    c.paint(c.rect(xx - side * 1, y, xx, y + 1) if side > 0 else c.rect(xx, y, xx + 1, y + 1), TOOTH)
        if mouth == 3: c.paint(c.rect(cx - 1, hy + 20, cx + 1, hy + 24), (160, 40, 60))                    # a tongue
    # the samo, its wings grown into long horns
    hat = c.rect(hx + 4, hy, hx + 18, hy + 7) | c.rect(hx + 6, hy - 4, hx + 16, hy)
    wing_f = c.poly([(hx + 4, hy + 2), (hx - 10, hy - 8), (hx - 14, hy - 12), (hx - 6, hy - 4), (hx + 4, hy - 2)])
    wing_b = c.poly([(hx + 18, hy + 2), (hx + 32, hy - 10), (hx + 36, hy - 15), (hx + 30, hy - 4), (hx + 18, hy - 2)])
    c.layer(wing_f | wing_b, HAT); c.paint((wing_f | wing_b) & c.rect(0, hy - 3, W, hy + 3), HAT2)
    c.layer(hat, HAT); c.paint(c.rect(hx + 4, hy + 5, hx + 18, hy + 6), HAT2)
    # the front arm, in front of everything: three joints, the seal in its claws
    front = {'idle_00': [(40, 52), (30, 66), (22, 80), (18, 92)], 'idle_01': [(40, 54), (29, 68), (21, 82), (17, 94)],
             'open': [(40, 50), (24, 44), (12, 36), (6, 26)], 'reach': [(40, 58), (26, 74), (16, 92), (10, 103)],
             'raise': [(40, 50), (36, 32), (32, 16), (28, 6)], 'cast': [(40, 50), (30, 34), (22, 20), (16, 8)],
             'death_00': [(40, 76), (26, 90), (14, 100), (6, 106)]}.get(pose)
    if front:
        front = [(x, y + drop) if pose not in ('death_00', 'raise', 'cast', 'open', 'reach') else (x, y) for x, y in front]
        limb(c, front, VOID3, VOID2)
        sx, sy = front[-1]
        hand(c, sx, sy, -1)
        if pose != 'cast': seal(c, sx - 3, sy - 3 if pose != 'raise' else sy - 6)
    return c

def limb(c, pts, colour, shade):
    """An arm too long, thin, with knobbed joints."""
    m = c.blank()
    for a, b in zip(pts, pts[1:]): m |= c.line([a, b], 3)
    c.layer(m, colour)
    for a, b in zip(pts, pts[1:]): c.paint(c.line([(a[0] + 1, a[1] + 1), (b[0] + 1, b[1] + 1)], 1) & m, shade)
    for p in pts[1:-1]: c.layer(c.ellipse(p[0] - 2, p[1] - 2, p[0] + 2, p[1] + 2), colour)

def hand(c, x, y, side):
    """Long black-nailed fingers splayed from a bony palm."""
    c.layer(c.ellipse(x - 3, y - 3, x + 3, y + 3), VOID3)
    for k in (-3, 0, 3):
        c.layer(c.line([(x + k, y + 2), (x + k + side, y + 7)], 1), NAIL)

def seal(c, x, y):
    """The state seal: a square jade block with a crouching haetae on top and a red face."""
    block = c.rect(x - 5, y, x + 5, y + 8); c.layer(block, JADE)
    c.paint(c.rect(x + 2, y, x + 5, y + 8), JADE2); c.paint(c.rect(x - 4, y + 1, x - 2, y + 2), JADE3)
    c.paint(c.rect(x - 5, y + 7, x + 5, y + 8), SEALRED)
    knob = c.poly([(x - 4, y), (x - 3, y - 4), (x, y - 6), (x + 3, y - 4), (x + 4, y)])
    c.layer(knob, JADE); c.paint(c.rect(x - 2, y - 4, x - 2, y - 4), K)

def to_image(c, w, h):
    img = np.zeros((h, w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0); return img

def big(a): return np.repeat(np.repeat(a, BLOCK, 0), BLOCK, 1)

def prop(c, name):
    img = to_image(c, c.w, c.h); Image.fromarray(img).save(f"{ROOT}/13_Props/{name}.png")

def ink():
    c = Canvas(10, 14)
    c.layer(c.ellipse(1, 4, 8, 12) | c.poly([(4, 0), (6, 0), (8, 7), (1, 7)]), INK)
    c.paint(c.rect(3, 6, 4, 7), INK2); return c
def coin():
    c = Canvas(9, 9)
    c.layer(c.ellipse(0, 0, 8, 8), GOLD); c.paint(c.rect(3, 3, 5, 5), (60, 40, 30)); c.paint(c.rect(4, 4, 4, 4), (0, 0, 0)); c.paint(c.rect(1, 2, 2, 3), (255, 236, 160))
    return c
def shadow_hand():
    c = Canvas(18, 30)
    arm = c.poly([(5, 29), (12, 29), (11, 12), (6, 12)]); c.layer(arm, VOID2)
    palm = c.ellipse(3, 8, 14, 16); c.layer(palm, VOID2)
    for x0, x1, top in ((3, 1, 1), (6, 5, 0), (9, 10, 0), (12, 15, 2)):
        c.layer(c.line([(x0, 9), (x1, top)], 1), VOID3)
    c.paint(c.rect(7, 18, 8, 26), VOID3); return c
def seal_alone():
    c = Canvas(26, 26); seal(c, 12, 10); return c
def seal_mark():
    c = Canvas(30, 6)
    c.layer(c.rect(2, 1, 27, 4), SEALRED); c.paint(c.rect(5, 2, 7, 3) | c.rect(11, 2, 13, 3) | c.rect(17, 2, 19, 3) | c.rect(23, 2, 24, 3), (130, 20, 20))
    return c

os.makedirs(f"{ROOT}/25_Final", exist_ok=True)
for pose in ('idle', 'cup', 'blank'):
    Image.fromarray(big(to_image(official(pose), OW, OH))).save(f"{ROOT}/25_Final/official_{pose}.png")
# the faceless man left lying when the creature crumbles: the blank pose turned a quarter, head to the left
lying = np.rot90(to_image(official('blank'), OW, OH), 1)
ys, xs = np.where(lying[..., 3] > 0); lying = lying[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
corpse = np.zeros((OH, 56, 4), np.uint8); h, w = lying.shape[:2]
corpse[OH - 1 - h:OH - 1, (56 - w) // 2:(56 - w) // 2 + w] = lying
Image.fromarray(big(corpse)).save(f"{ROOT}/25_Final/official_down.png")
for pose in ('idle_00', 'idle_01', 'open', 'reach', 'raise', 'cast', 'death_00'):
    Image.fromarray(big(to_image(creature(pose), W, H))).save(f"{ROOT}/25_Final/final_{pose}.png")
# crumbling into the dark, like the other bosses
slumped = to_image(creature('death_00'), W, H)
for i, level in ((1, .3), (2, .62)):
    Image.fromarray(big(framelib.scatter(slumped, level, 1, 50 + i))).save(f"{ROOT}/25_Final/final_death_{i:02}.png")
for name, draw in (("final_ink", ink), ("final_coin", coin), ("final_hand", shadow_hand), ("final_seal", seal_alone), ("final_mark", seal_mark)):
    prop(draw(), name)
