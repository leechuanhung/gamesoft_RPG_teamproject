# Draws the two new common monsters of the road to Hanyang (기획서.md 1-3) on the seonbi's 12 px art pixels:
#   23_Mobs/wisp_<pose>: dokkaebi-bul, a blue flame with a grinning face that drifts and swoops.
#       idle_00..02 (the flame flickering), dash (stretched into a streak), down (guttering out). 20x24 blocks, hung
#       by its centre (it flies).
#   23_Mobs/brute_<pose>: dueoksini, the head-crusher: a hunched grey thing with a head too big for it, a straw
#       raincoat (doromi) over its back and two great fists, facing right.
#       idle, walk_00..03, ready (fists raised over its head), slam (fists brought down, crouched), down.
#       48x46 blocks, feet on the bottom centre.
# Usage: python make_road_mobs.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
ROOT = sys.argv[1]
BLOCK = 12

CORE, FLAME, FLAME2, FLAME3 = (226, 250, 255), (124, 204, 255), (64, 126, 226), (44, 74, 170)
GREY, GREY2, GREY3 = (128, 118, 146), (96, 86, 114), (160, 152, 176)
STRAW, STRAW2, STRAW3 = (196, 160, 92), (150, 116, 64), (226, 196, 128)
EYE, TEETH, MOUTH, NAIL = (255, 214, 72), (240, 236, 220), (110, 26, 40), (60, 52, 64)

def save(c, path, w, h):
    img = np.zeros((h, w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).resize((w * BLOCK, h * BLOCK), Image.NEAREST).save(path)

# ---- dokkaebi-bul ----
WW, WH = 20, 24
def wisp(flick, stretch=0, gutter=0):
    """A teardrop of blue fire, tongues licking up (their shape set by `flick`), a pale core with two hollow
    eyes and a crooked grin. `stretch` drags it into a streak behind (the swoop); `gutter` shrinks it."""
    c = Canvas(WW, WH)
    cx, cy = 10, 15
    r = 6 - gutter
    tongues = [(cx - 4 + flick % 2, cy - 9 - flick % 3), (cx + 3 - flick % 2, cy - 11 + flick % 2), (cx, cy - 13 + (flick + 1) % 3)]
    outer = c.ellipse(cx - r - stretch, cy - r, cx + r, cy + r)
    for tx, ty in tongues[:3 - gutter // 2]:
        outer |= c.poly([(tx - 2, cy - 3), (tx + 2, cy - 3), (tx, ty + gutter * 2)])
    if stretch: outer |= c.poly([(cx - r - stretch - 3, cy), (cx - 2, cy - r + 1), (cx - 2, cy + r - 1)])
    c.layer(outer, FLAME3 if gutter > 2 else FLAME2)
    mid = c.ellipse(cx - r + 1 - stretch // 2, cy - r + 2, cx + r - 1, cy + r - 1)
    for tx, ty in tongues[:2]:
        mid |= c.poly([(tx - 1, cy - 2), (tx + 1, cy - 2), (tx, ty + 3 + gutter * 2)])
    c.paint(mid & outer, FLAME)
    core = c.ellipse(cx - r + 3, cy - r + 4, cx + r - 2, cy + r - 2)
    c.paint(core & outer, CORE)
    if gutter < 3:
        c.paint(c.rect(cx - 2, cy - 1, cx - 1, cy + 1) | c.rect(cx + 2, cy - 1, cx + 3, cy + 1), FLAME3)       # hollow eyes
        c.paint(c.rect(cx - 2, cy + 3, cx + 3, cy + 3) | c.rect(cx - 3, cy + 2, cx - 3, cy + 2) | c.rect(cx + 4, cy + 2, cx + 4, cy + 2), FLAME3)   # grin
        c.paint(c.rect(cx - 1, cy + 3, cx - 1, cy + 3) | c.rect(cx + 1, cy + 3, cx + 1, cy + 3), CORE)                # teeth
    # sparks around it
    for sx, sy in ((cx + r + 2, cy - 5 + flick), (cx - r - 1 - stretch, cy - 7 + flick % 2)):
        if 0 <= sx < WW and 0 <= sy < WH: c.paint(c.rect(sx, sy, sx, sy), FLAME, inside=False)
    return c

# ---- dueoksini ----
BW, BH = 48, 46
GY, FX = 44, 22
def brute(back=(-6, 0), front=(5, 0), drop=0, lean=0, fists=None, mouth=1):
    """Short bowed legs, a straw raincoat over a hunched back, a head as big as its body jutting forward with a
    heavy underbite, yellow eyes, matted hair; long arms ending in huge knuckled fists."""
    c = Canvas(BW, BH)
    ox, oy = lean, drop
    fists = fists or [(-3, 36), (12, 37)]   # back fist, front fist (from FX, row)
    # back arm and fist
    sh = (FX - 3 + ox, 24 + oy)
    bf = (FX + fists[0][0] + ox, fists[0][1] + oy)
    c.layer(c.line([sh, ((sh[0] + bf[0]) // 2 - 1, (sh[1] + bf[1]) // 2), bf], 4), GREY2)
    c.layer(c.ellipse(bf[0] - 3, bf[1] - 3, bf[0] + 3, bf[1] + 3), GREY2)
    # legs
    for side, (fx, lift) in ((-1, back), (1, front)):
        hip = (FX + side * 3 + ox // 2, 34 + oy)
        foot = (FX + fx, GY - 2 - lift)
        knee = (hip[0] + side * 2 + 1, (hip[1] + foot[1]) // 2)
        c.layer(c.line([hip, knee], 4) | c.line([knee, foot], 4), GREY if side > 0 else GREY2)
        c.layer(c.rect(foot[0] - 2, foot[1], foot[0] + 4, foot[1] + 2), GREY if side > 0 else GREY2)
        c.paint(c.rect(foot[0] + 2, foot[1] + 2, foot[0] + 2, foot[1] + 2) | c.rect(foot[0] + 4, foot[1] + 2, foot[0] + 4, foot[1] + 2), NAIL)
    # body under the coat: a round belly
    belly = c.ellipse(FX - 7 + ox, 20 + oy, FX + 8 + ox, 37 + oy)
    c.layer(belly, GREY)
    c.paint(belly & c.ellipse(FX - 2 + ox, 26 + oy, FX + 7 + ox, 36 + oy), GREY3)
    # the straw raincoat over the hunched back, ragged at the hem
    coat = c.poly([(FX - 2 + ox, 12 + oy), (FX + 4 + ox, 14 + oy), (FX - 1 + ox, 24 + oy), (FX - 4 + ox, 33 + oy), (FX - 10 + ox, 35 + oy), (FX - 12 + ox, 30 + oy), (FX - 10 + ox, 18 + oy)])
    c.layer(coat, STRAW)
    for i, x in enumerate(range(FX - 11, FX + 2, 2)):
        c.paint(coat & c.line([(x + ox + 1, 14 + oy), (x + ox - 1, 36 + oy)], 1), STRAW2 if i % 2 else STRAW3)
    rag = c.blank()
    for i, x in enumerate(range(FX - 11, FX - 2, 2)): rag |= c.rect(x + ox, 34 + oy + i % 2, x + ox, 35 + oy + i % 2)
    c.layer(rag, STRAW2)
    # the head: huge, jutting forward from the shoulders
    hx, hy = FX + 4 + ox, 6 + oy
    head = c.ellipse(hx - 7, hy, hx + 8, hy + 15)
    c.layer(head, GREY)
    c.paint(head & c.ellipse(hx - 7, hy, hx + 1, hy + 15), GREY2)
    # matted hair over the crown and down the back
    hair = c.poly([(hx - 8, hy + 10), (hx - 7, hy + 1), (hx - 2, hy - 2), (hx + 4, hy - 1), (hx + 7, hy + 3), (hx + 2, hy + 3), (hx - 3, hy + 5), (hx - 5, hy + 13)])
    c.layer(hair, (44, 38, 52))
    c.paint(c.line([(hx - 4, hy), (hx - 6, hy + 8)], 1) | c.line([(hx + 1, hy - 1), (hx - 3, hy + 3)], 1), (70, 62, 82))
    # brow, eye, nostril, the underbite with tusks
    c.paint(c.rect(hx + 2, hy + 5, hx + 8, hy + 5), (70, 62, 82))
    c.paint(c.rect(hx + 4, hy + 6, hx + 6, hy + 7), EYE); c.paint(c.rect(hx + 6, hy + 7, hx + 6, hy + 7), K)
    c.paint(c.rect(hx + 8, hy + 8, hx + 8, hy + 9), K)
    jaw = c.rect(hx - 1, hy + 11 - mouth, hx + 9, hy + 15) | c.rect(hx, hy + 16, hx + 8, hy + 16)
    c.layer(jaw, GREY)
    c.paint(c.rect(hx + 1, hy + 11 - mouth, hx + 8, hy + 11 + mouth // 2), MOUTH)
    c.paint(c.rect(hx + 2, hy + 12 + mouth // 2, hx + 2, hy + 12 + mouth // 2) | c.rect(hx + 5, hy + 12 + mouth // 2, hx + 5, hy + 12 + mouth // 2) | c.rect(hx + 8, hy + 12 + mouth // 2, hx + 8, hy + 12 + mouth // 2), TEETH)
    c.paint(c.rect(hx + 3, hy + 9 - mouth, hx + 3, hy + 10) | c.rect(hx + 7, hy + 9 - mouth, hx + 7, hy + 10), TEETH)   # tusks
    # front arm and its great fist
    fs = (FX + 3 + ox, 23 + oy)
    ff = (FX + fists[1][0] + ox, fists[1][1] + oy)
    el = ((fs[0] + ff[0]) // 2 + 1, (fs[1] + ff[1]) // 2 + 1)
    c.layer(c.line([fs, el, ff], 4), GREY)
    fist = c.ellipse(ff[0] - 4, ff[1] - 4, ff[0] + 4, ff[1] + 4)
    c.layer(fist, GREY)
    c.paint(fist & c.rect(ff[0] - 4, ff[1] + 2, ff[0] + 4, ff[1] + 4), GREY2)
    for k in (-2, 0, 2): c.paint(c.rect(ff[0] + 2, ff[1] + k, ff[0] + 3, ff[1] + k), GREY3)
    return c

def down_brute():
    """Fallen on its face: the standing picture turned a quarter and grayed toward its shade."""
    c = brute(drop=3, mouth=3)
    img = np.zeros((BH, BW, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    a = np.rot90(img, -1)
    ys, xs = np.where(a[..., 3] > 0); a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    out = np.zeros((BH, BW, 4), np.uint8)
    h, w = a.shape[:2]; w = min(w, BW); x0 = (BW - w) // 2
    out[BH - 1 - h:BH - 1, x0:x0 + w] = a[:, :w]
    return out

out = os.path.join(ROOT, "23_Mobs"); os.makedirs(out, exist_ok=True)
for i in range(3): save(wisp(i), os.path.join(out, f"wisp_idle_{i:02}.png"), WW, WH)
save(wisp(1, stretch=4), os.path.join(out, "wisp_dash.png"), WW, WH)
save(wisp(2, gutter=3), os.path.join(out, "wisp_down.png"), WW, WH)
save(brute(), os.path.join(out, "brute_idle.png"), BW, BH)
steps = [((-7, 0), (5, 2), 0), ((-4, 1), (3, 0), 1), ((-6, 2), (6, 0), 0), ((-3, 0), (2, 1), 1)]
for i, (b, f, d) in enumerate(steps):
    save(brute(back=b, front=f, drop=d, fists=[(-4 + i % 2 * 2, 36), (11 - i % 2 * 2, 37)]), os.path.join(out, f"brute_walk_{i:02}.png"), BW, BH)
save(brute(back=(-7, 0), front=(6, 0), lean=-2, fists=[(-2, 6), (6, 4)], mouth=3), os.path.join(out, "brute_ready.png"), BW, BH)
save(brute(back=(-8, 0), front=(7, 0), drop=4, lean=3, fists=[(9, 40), (14, 40)], mouth=4), os.path.join(out, "brute_slam.png"), BW, BH)
Image.fromarray(down_brute()).resize((BW * BLOCK, BH * BLOCK), Image.NEAREST).save(os.path.join(out, "brute_down.png"))
