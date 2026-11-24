# Draws the government soldiers (관군, 기획서.md 1-2) on the seonbi's 12 px art pixels, his height (45 blocks) and slim
# build, facing right like him, but in nothing he wears: a round felt beonggeoji with a red sangmo tassel (an iron
# helmet with a plume and neck flaps at the third rank), a black kwaeja vest over narrow red sleeves (a studded
# dujeonggap coat at the second and third ranks), white leg wraps and straw shoes.
# Their weapons are near his but not his: the blade is a plain iron hwando with an iron guard and a red tassel at the
# pommel (his has a gold guard and no tassel); the bow is red-lacquered with black grips and red-fletched arrows
# (his is a brown horn bow with white wraps), with a quiver on the back.
#   22_Soldier/<kind>_<rank>_<pose>: kind blade or bow, rank 1 (light), 2 (studded coat), 3 (red armour, helmet)
#   blade: idle, walk_00..03, ready (sword drawn back, low), strike (lunging cut), hurt, down
#   bow:   idle, walk_00..03, ready (drawn to the cheek), strike (loosed, hand thrown back), hurt, down
#   13_Props/soldier_arrow (the arrow they shoot, one texel per art pixel like the props, hung by its centre)
# Every frame is on one 48x56 block canvas, feet on its bottom centre, so the game stands them all on one point.
# Usage: python make_soldier.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
ROOT = sys.argv[1]
W, H, BLOCK = 48, 56, 12
GY, FX = 53, 24          # bottom row of the shoes, and the point between the feet

SKIN, SKIN2, HAIR = (249, 196, 140), (226, 163, 109), (36, 28, 30)
FELT, FELT2 = (58, 54, 66), (40, 38, 48)
RED, RED2, RED3 = (196, 58, 58), (150, 38, 44), (224, 96, 84)
VEST, VEST2 = (54, 52, 62), (38, 36, 46)
NAVY, NAVY2 = (64, 70, 120), (46, 50, 92)
GOLD, GOLD2 = (236, 196, 92), (186, 142, 60)
COTTON, COTTON2 = (253, 248, 228), (214, 208, 194)
STRAW, STRAW2 = (214, 178, 106), (166, 128, 70)
STEEL, STEEL2, STEEL3 = (176, 182, 196), (126, 132, 150), (226, 230, 238)
IRON, IRON2 = (110, 112, 124), (80, 82, 92)
WOOD, WOOD2 = (122, 82, 50), (92, 60, 36)
BELT, WHITE = (64, 70, 120), (255, 255, 255)
LACQ, LACQ2 = (178, 54, 46), (128, 34, 34)

class Pose:
    def __init__(s, **k):
        s.lean = 0; s.drop = 0                       # upper body forward and down (knees bent)
        s.back = (-4, 0); s.front = (4, 0)            # feet: x from FX, lift in rows
        s.fhand = (4, 36); s.felbow = (3, 31)         # front arm (in front of the body), from FX / row
        s.bhand = (-3, 36); s.belbow = (-3, 31)       # back arm (behind it)
        s.blade = None                                 # (angle in degrees, 0 = forward, 90 = up), held in the front hand
        s.bow = None                                   # 'held', 'drawn' or 'loosed'
        s.head = 0                                     # head tilt back in blocks (hurt)
        s.__dict__.update(k)

def soldier(kind, rank, p):
    c = Canvas(W, H)
    ox, oy = p.lean, p.drop                           # upper body offset
    hipy = GY - 17 + oy
    coat = RED if rank == 3 else NAVY if rank == 2 else VEST
    coat2 = RED2 if rank == 3 else NAVY2 if rank == 2 else VEST2
    sleeve, sleeve2 = (NAVY, NAVY2) if rank == 3 else (RED, RED2)

    # the quiver on the back (archers): a red-lacquered tube, arrow nocks and red fletching over the shoulder
    if kind == 'bow':
        qx, qy = FX - 5 + ox, 26 + oy
        tube = c.line([(qx, qy + 12), (qx - 3, qy)], 3)
        c.layer(tube, LACQ); c.paint(c.line([(qx + 1, qy + 12), (qx - 2, qy)], 1) & tube, LACQ2)
        for i, dx in enumerate((-4, -3, -2)):
            c.layer(c.rect(qx + dx - 1, qy - 4 + i, qx + dx - 1, qy - 1), RED3 if i != 1 else COTTON)

    # back arm, behind everything
    sh = (FX - 2 + ox, 27 + oy)
    arm(c, sh, (FX + p.belbow[0] + ox, p.belbow[1] + oy), (FX + p.bhand[0] + ox, p.bhand[1] + oy), sleeve2, sleeve2, rank)

    # legs: white trousers into wrapped shins, straw shoes; the back leg in shade
    for side, (fx, lift) in ((-1, p.back), (1, p.front)):
        hip = (FX + side + ox // 2, hipy)
        foot = (FX + fx, GY - 2 - lift)
        knee = ((hip[0] + foot[0]) // 2 + (2 if oy > 0 else 1), (hip[1] + foot[1]) // 2 + (0 if oy > 0 else 0))
        cloth = COTTON if side > 0 else COTTON2
        c.layer(c.line([hip, knee], 3) | c.line([knee, foot], 3), cloth)
        shin = c.line([knee, foot], 3)
        c.paint(shin & ~c.rect(0, 0, W, knee[1] + 1), COTTON2 if side > 0 else (190, 184, 170))
        for y in range(knee[1] + 2, foot[1] + 1, 2): c.paint(shin & c.rect(0, y, W, y), NAVY2)
        shoe = c.rect(foot[0] - 2, foot[1] + 1, foot[0] + 2, foot[1] + 2) | c.rect(foot[0] + 3, foot[1] + 2, foot[0] + 3, foot[1] + 2)
        c.layer(shoe, STRAW if side > 0 else STRAW2)
        c.paint(c.rect(foot[0] - 2, foot[1] + 2, foot[0] + 3, foot[1] + 2), STRAW2)

    # the coat: vest (rank 1) or studded coat (2, 3) from the shoulders to the knee, split at the front
    top, hem = 26 + oy, GY - 8 + oy // 2
    body = c.poly([(FX - 4 + ox, top), (FX + 4 + ox, top), (FX + 6 + ox // 2, hem), (FX - 6 + ox // 2, hem)])
    c.layer(body, coat)
    c.paint(body & c.poly([(FX - 4 + ox, top), (FX - 1 + ox, top), (FX - 2 + ox // 2, hem), (FX - 6 + ox // 2, hem)]), coat2)
    c.paint(body & c.line([(FX + 2 + ox, top + 8), (FX + 2 + ox // 2, hem)], 1), coat2)
    if rank == 1:
        # red sleeves show under the vest's armholes; the white roundel with the red mark of the troops on the chest
        c.paint(c.rect(FX - 3 + ox, top, FX + 3 + ox, top + 2) & body, sleeve)
        badge = c.ellipse(FX + ox - 1, top + 4, FX + ox + 3, top + 8)
        c.layer(badge, COTTON); c.paint(c.rect(FX + ox, top + 6, FX + ox + 2, top + 6) | c.rect(FX + ox + 1, top + 5, FX + ox + 1, top + 7), RED)
    else:
        # brass studs in rows, a trimmed hem and collar
        for y in range(top + 3, hem - 1, 3):
            for x in range(FX - 6, FX + 7, 2):
                xx = x + (ox * (hem - y)) // max(1, hem - top) + (1 if (y // 3) % 2 else 0)
                if body[y, xx]: c.paint(c.rect(xx, y, xx, y), GOLD)
        c.paint(body & c.rect(0, hem - 1, W, hem), GOLD2 if rank == 3 else RED)
        c.paint(body & c.rect(FX - 3 + ox, top, FX + 3 + ox, top), GOLD2 if rank == 3 else RED)
    # the sash (jeondae) tied at the waist, its ends hanging at the back
    waist = top + 9
    c.layer(body & c.rect(0, waist, W, waist + 1), BELT)
    c.layer(c.rect(FX - 7 + ox, waist + 1, FX - 6 + ox, waist + 5), BELT)
    if rank == 3:   # shoulder guards
        c.layer(c.ellipse(FX - 1 + ox, top - 1, FX + 5 + ox, top + 4), coat); c.paint(c.rect(FX + ox, top + 2, FX + 4 + ox, top + 2), GOLD)

    # the head: square jaw, a soldier's moustache, a hard eye under a straight brow; the head tips back when hit
    hx, hy = FX + ox - p.head, 17 + oy + abs(p.head) // 2
    head = c.rect(hx - 3, hy, hx + 3, hy + 7) | c.rect(hx - 2, hy + 8, hx + 2, hy + 8)
    c.layer(head, SKIN)
    c.paint(c.rect(hx - 3, hy, hx - 1, hy + 5) | c.rect(hx - 3, hy, hx + 3, hy + 1), HAIR)
    c.paint(c.rect(hx - 1, hy + 3, hx - 1, hy + 4), SKIN2)                # ear
    c.paint(c.rect(hx + 1, hy + 3, hx + 3, hy + 3), HAIR)                 # brow
    c.paint(c.rect(hx + 2, hy + 4, hx + 2, hy + 4), K)                    # eye
    c.paint(c.rect(hx + 1, hy + 6, hx + 3, hy + 6) | c.rect(hx + 3, hy + 7, hx + 3, hy + 7), HAIR)   # moustache
    c.paint(c.rect(hx - 2, hy + 7, hx - 1, hy + 8), SKIN2)                # jaw shade
    c.layer(c.rect(hx - 1, hy + 9, hx + 1, hy + 9), SKIN2)               # neck

    # the hat
    if rank < 3:
        brim = c.rect(hx - 6, hy - 1, hx + 6, hy - 1) | c.rect(hx - 7, hy, hx - 6, hy) | c.rect(hx + 6, hy, hx + 7, hy)
        dome = c.ellipse(hx - 3, hy - 6, hx + 3, hy + 1) & c.rect(0, 0, W, hy - 1)
        c.layer(dome | brim, FELT)
        c.paint(dome & c.rect(hx - 3, 0, hx - 1, H), FELT2); c.paint(brim & c.rect(0, hy, W, hy), FELT2)
        c.paint(c.rect(hx - 3, hy - 2, hx + 3, hy - 2) & dome, RED2 if rank == 1 else GOLD2)   # hat band
        # the red sangmo tassel springing from the crown and falling down the back
        tassel = c.rect(hx - 1, hy - 9, hx + 1, hy - 7) | c.rect(hx - 3, hy - 8, hx - 2, hy - 6) | c.rect(hx - 5, hy - 6, hx - 4, hy - 3)
        c.layer(tassel, RED); c.paint(c.rect(hx - 1, hy - 9, hx + 1, hy - 9), RED3)
        if rank == 2: c.layer(c.rect(hx, hy - 11, hx, hy - 10), GOLD)                      # a brass finial
    else:
        # iron helmet: a ridged bowl, a peak, a spike with a red plume, red neck flaps studded with brass
        flaps = c.rect(hx - 5, hy, hx - 3, hy + 8) | c.rect(hx - 4, hy + 9, hx - 2, hy + 9)
        c.layer(flaps, RED); c.paint(c.rect(hx - 4, hy + 2, hx - 4, hy + 2) | c.rect(hx - 4, hy + 5, hx - 4, hy + 5) | c.rect(hx - 4, hy + 8, hx - 4, hy + 8), GOLD)
        bowl = c.ellipse(hx - 4, hy - 6, hx + 4, hy + 2) & c.rect(0, 0, W, hy)
        c.layer(bowl, STEEL)
        c.paint(bowl & c.rect(hx - 4, 0, hx - 2, H), STEEL2); c.paint(c.rect(hx + 1, hy - 4, hx + 2, hy - 3) & bowl, STEEL3)
        c.paint(c.rect(hx - 4, hy, hx + 4, hy) & bowl, GOLD2)
        c.layer(c.rect(hx + 3, hy, hx + 6, hy), IRON)                                       # the peak
        c.layer(c.rect(hx, hy - 9, hx, hy - 7), IRON)
        c.layer(c.rect(hx - 2, hy - 12, hx + 1, hy - 10) | c.rect(hx - 4, hy - 11, hx - 3, hy - 8), RED)

    # front arm, then what it holds
    fs = (FX + 2 + ox, 27 + oy)
    fe = (FX + p.felbow[0] + ox, p.felbow[1] + oy)
    fh = (FX + p.fhand[0] + ox, p.fhand[1] + oy)
    if p.bow: bow(c, p, fh, (FX + p.bhand[0] + ox, p.bhand[1] + oy), hx, hy)
    arm(c, fs, fe, fh, sleeve, sleeve2, rank)
    if p.blade is not None: blade(c, fh, p.blade, rank)
    if p.bow == 'drawn':   # the drawing hand in front of the chin, pinching the nock
        bh = (FX + p.bhand[0] + ox, p.bhand[1] + oy)
        c.layer(c.rect(bh[0], bh[1], bh[0] + 1, bh[1] + 1), SKIN)
    return c

def arm(c, shoulder, elbow, hand, colour, shade, rank):
    sleeve = c.line([shoulder, elbow], 3) | c.line([elbow, hand], 2)
    c.layer(sleeve, colour)
    c.paint(c.line([elbow, hand], 1) & sleeve, shade)
    if rank >= 2:   # leather bracer over the forearm
        mid = ((elbow[0] + hand[0]) // 2, (elbow[1] + hand[1]) // 2)
        c.paint(c.line([mid, hand], 2) & sleeve, WOOD)
    c.layer(c.rect(hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1), SKIN)
    c.paint(c.rect(hand[0] - 1, hand[1] + 1, hand[0] + 1, hand[1] + 1), SKIN2)

def blade(c, hand, angle, rank):
    """An iron hwando: black-wrapped grip behind the hand with a red tassel off the pommel, an iron guard,
    a grey blade with a pale edge. Longer at the third rank."""
    a = np.radians(angle); d = np.array([np.cos(a), -np.sin(a)])
    h = np.array(hand, float)
    length = 15 if rank == 3 else 13
    pommel = h - d * 3; guard = h + d * 2; tip = h + d * length
    P = lambda v: (int(round(v[0])), int(round(v[1])))
    edge = np.array([-d[1], d[0]])
    if edge[1] < 0: edge = -edge
    c.layer(c.line([P(guard), P(tip)], 2), STEEL)
    c.paint(c.line([P(guard + edge * .8), P(tip + edge * .8)], 1) & c.line([P(guard), P(tip)], 3), STEEL3)
    c.layer(c.line([P(pommel), P(guard)], 2), HAIR)
    g0, g1 = guard + edge * 2, guard - edge * 2
    c.layer(c.line([P(g0), P(g1)], 1), IRON)
    t = P(pommel - d * 1)
    c.layer(c.rect(t[0], t[1], t[0], t[1] + 3) | c.rect(t[0] - 1, t[1] + 3, t[0] + 1, t[1] + 4), RED)
    c.layer(c.rect(hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1), SKIN)

def bow(c, p, grip, draw, hx, hy):
    """The red-lacquered bow held upright in the front hand: limbs curving back to dark tips, a black grip.
    `drawn`: string pulled to the draw hand with a red-fletched arrow along it; `loosed`: string straight again."""
    gx, gy = grip
    span = 11
    curve = 3 if p.bow == 'drawn' else 2
    pts_top = [(gx, gy), (gx + curve, gy - 4), (gx + curve, gy - 8), (gx + 1, gy - span)]
    pts_bot = [(gx, gy), (gx + curve, gy + 4), (gx + curve, gy + 8), (gx + 1, gy + span)]
    limbs = c.line(pts_top, 2) | c.line(pts_bot, 2)
    c.layer(limbs, LACQ)
    c.paint(c.line(pts_top[1:], 1) & limbs, LACQ2)
    c.layer(c.rect(gx + 1, gy - span - 1, gx + 1, gy - span) | c.rect(gx + 1, gy + span, gx + 1, gy + span + 1), HAIR)
    top, bot = (gx + 1, gy - span), (gx + 1, gy + span)
    if p.bow == 'drawn':
        string = c.line([top, draw], 1) | c.line([draw, bot], 1)
        c.paint(string & ~limbs, COTTON2)
        shaft = c.line([(draw[0] - 1, draw[1]), (gx + 5, gy)], 1)
        c.layer(shaft, WOOD)
        c.paint(c.rect(gx + 5, gy - 1, gx + 6, gy + 1) & ~limbs, STEEL)            # the iron head
        c.layer(c.rect(draw[0] + 1, draw[1] - 1, draw[0] + 3, draw[1] - 1) | c.rect(draw[0] + 1, draw[1] + 1, draw[0] + 3, draw[1] + 1), RED3)
    else:
        c.paint(c.line([top, bot], 1) & ~limbs, COTTON2)
    c.layer(c.rect(gx - 1, gy - 1, gx + 1, gy + 1), HAIR)

def poses(kind):
    walk = [dict(back=(-5, 0), front=(4, 1), fhand=(5, 35), bhand=(-4, 35)),
            dict(back=(-2, 1), front=(2, 0), drop=1),
            dict(back=(-4, 1), front=(5, 0), fhand=(3, 36), bhand=(-2, 35)),
            dict(back=(-1, 0), front=(1, 1), drop=1)]
    if kind == 'blade':
        rest = dict(fhand=(5, 35), felbow=(4, 31), blade=-50)
        return {
            'idle': Pose(**rest),
            **{f'walk_{i:02}': Pose(**{**rest, **w, 'fhand': (5, 35 + w.get('drop', 0))}) for i, w in enumerate(walk)},
            'ready': Pose(lean=-2, back=(-6, 0), front=(5, 0), drop=2, felbow=(-2, 30), fhand=(-6, 29), bhand=(-5, 33), belbow=(-4, 30), blade=165),
            'strike': Pose(lean=3, drop=3, back=(-8, 0), front=(6, 0), felbow=(7, 29), fhand=(10, 31), bhand=(-5, 33), belbow=(-5, 30), blade=-12),
            'hurt': Pose(lean=-2, head=2, back=(-5, 0), front=(3, 0), felbow=(5, 27), fhand=(7, 25), blade=60),
        }
    rest = dict(fhand=(5, 33), felbow=(4, 30), bow='held')
    return {
        'idle': Pose(**rest),
        **{f'walk_{i:02}': Pose(**{**rest, **w, 'fhand': (5, 33 + w.get('drop', 0))}) for i, w in enumerate(walk)},
        'ready': Pose(back=(-5, 0), front=(5, 0), felbow=(7, 27), fhand=(10, 27), belbow=(3, 29), bhand=(3, 25), bow='drawn'),
        'strike': Pose(back=(-5, 0), front=(5, 0), felbow=(7, 27), fhand=(10, 27), belbow=(-4, 26), bhand=(-7, 25), bow='loosed'),
        'hurt': Pose(lean=-2, head=2, back=(-5, 0), front=(3, 0), felbow=(5, 28), fhand=(7, 26), bow='held'),
    }

def save(c, path):
    img = np.zeros((H, W, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).resize((W * BLOCK, H * BLOCK), Image.NEAREST).save(path)

def down(c, path):
    """Lying on his back: the hurt frame turned a quarter (head to the back), feet kept on the canvas centre."""
    img = np.zeros((H, W, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    a = np.rot90(img, 1)                                     # head to the left (behind him), feet to the right
    ys, xs = np.where(a[..., 3] > 0)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    out = np.zeros((H, W, 4), np.uint8)
    h, w = a.shape[:2]
    x0 = max(0, FX - w // 2); out[H - 1 - h:H - 1, x0:x0 + w] = a[:, :min(w, W - x0)]
    Image.fromarray(out).resize((W * BLOCK, H * BLOCK), Image.NEAREST).save(path)

def arrow(path):
    c = Canvas(16, 3)
    c.layer(c.rect(1, 1, 13, 1), WOOD)
    c.layer(c.rect(13, 0, 14, 2), STEEL)
    c.layer(c.rect(0, 0, 2, 0) | c.rect(0, 2, 2, 2), RED3)
    c.save(path)

out = os.path.join(ROOT, "22_Soldier"); os.makedirs(out, exist_ok=True)
for kind in ('blade', 'bow'):
    for rank in (1, 2, 3):
        frames = poses(kind)
        for name, p in frames.items():
            save(soldier(kind, rank, p), os.path.join(out, f"{kind}_{rank}_{name}.png"))
        down(soldier(kind, rank, frames['hurt']), os.path.join(out, f"{kind}_{rank}_down.png"))
os.makedirs(os.path.join(ROOT, "13_Props"), exist_ok=True)
arrow(os.path.join(ROOT, "13_Props", "soldier_arrow.png"))
