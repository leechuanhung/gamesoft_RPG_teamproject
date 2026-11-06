# Draws the inventory item icons at one texel per art pixel, on a 24-dot grid (the props' style: 1-dot black
# outline, flat fill and one shade). The game shows them scaled up in the inventory and the peddler's pack.
# Usage: python make_items.py Assets/Resources/JinaV4/14_Items   [--preview]  (--preview also writes 8x copies)
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixlib import *
OUT = sys.argv[1]
STRAW, STRAW2, STRAW3, ROPE = (214, 178, 106), (178, 140, 78), (236, 208, 146), (140, 106, 58)
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
HEMP, HEMP2 = (206, 190, 156), (170, 152, 118)
RICE, RICE2, LAVER, LAVER2 = (246, 244, 236), (214, 210, 198), (40, 52, 44), (62, 80, 66)
GOLD, GOLD2, GOLD3 = (236, 198, 80), (196, 150, 50), (252, 236, 160)
RED, RED2 = (168, 40, 40), (124, 28, 30)
YEL, YEL2, INK = (236, 198, 80), (204, 160, 56), (180, 32, 38)
STEEL, FEATHER, FEATHER2 = (184, 182, 192), (236, 232, 222), (196, 190, 178)

def misutgaru():
    """A hemp pouch of roasted-grain powder, its neck tied with straw rope, a little powder spilling."""
    c = Canvas(24, 24)
    body = c.ellipse(3, 8, 20, 22) | c.poly([(8, 9), (15, 9), (17, 4), (6, 4)])
    c.layer(body, HEMP)
    c.paint(c.ellipse(12, 12, 20, 22) & body, HEMP2)
    c.paint(c.rect(7, 4, 16, 4) & body, HEMP2)
    c.layer(c.rect(6, 8, 17, 9), ROPE)
    c.layer(c.rect(17, 9, 18, 13), ROPE)
    c.paint(c.rect(7, 14, 8, 15) | c.rect(10, 17, 10, 17), (232, 220, 190))
    return c

def jumeokbap():
    """A rice ball wrapped at the bottom in laver."""
    c = Canvas(24, 24)
    ball = c.poly([(12, 3), (21, 18), (19, 21), (5, 21), (3, 18)])
    c.layer(ball, RICE)
    c.paint(c.poly([(12, 3), (21, 18), (19, 21), (15, 21)]) & ball, RICE2)
    c.layer(c.rect(7, 15, 17, 21) & ball | c.rect(7, 15, 17, 21), LAVER)
    c.paint(c.rect(13, 15, 17, 21), LAVER2)
    for x, y in ((9, 8), (12, 6), (8, 12), (14, 11), (11, 12)): c.paint(c.rect(x, y, x, y), RICE2)
    return c

def cheongsimhwan():
    """A gold-foiled pill on its little red wax case."""
    c = Canvas(24, 24)
    c.layer(c.rect(3, 15, 20, 21), RED)
    c.paint(c.rect(3, 19, 20, 21), RED2)
    c.layer(c.ellipse(6, 4, 17, 15), GOLD)
    c.paint(c.ellipse(11, 9, 17, 15) & c.ellipse(6, 4, 17, 15), GOLD2)
    c.paint(c.rect(8, 6, 9, 7), GOLD3)
    return c

def ward_charm():
    """A small folded talisman of yellow paper, red ink, hung on a red cord (smaller than the purifying charm)."""
    c = Canvas(24, 24)
    c.layer(c.line([(12, 1), (12, 4)], 1), RED)
    c.layer(c.rect(6, 5, 17, 22), YEL)
    c.paint(c.rect(15, 5, 17, 22) | c.rect(6, 21, 17, 22), YEL2)
    c.paint(c.rect(9, 7, 14, 7) | c.rect(11, 8, 12, 19) | c.rect(8, 11, 15, 11) | c.rect(8, 15, 15, 15), INK)
    c.paint(c.line([(8, 18), (10, 20)]) | c.line([(15, 18), (13, 20)]), INK)
    return c

def jipsin():
    """A pair of straw sandals, woven, with rope loops."""
    c = Canvas(24, 24)
    for x0 in (2, 12):
        sole = c.ellipse(x0, 3, x0 + 9, 22)
        c.layer(sole, STRAW)
        c.paint(c.ellipse(x0 + 5, 3, x0 + 9, 22) & sole, STRAW2)
        for y in range(6, 21, 3): c.paint(c.rect(x0 + 2, y, x0 + 7, y) & sole, STRAW3)
        c.layer(c.rect(x0 + 1, 8, x0 + 1, 13) | c.rect(x0 + 8, 8, x0 + 8, 13), ROPE)
    return c

def arrow_bundle():
    """Five arrows tied in a sheaf, fanning a little: white fletching on top, steel heads below, a straw tie."""
    c = Canvas(24, 24)
    tops, bottoms = (3, 7, 11, 15, 19), (8, 10, 12, 14, 16)
    for t, b in zip(tops, bottoms): c.layer(c.line([(t, 5), (b, 12), (b, 18)]), WOOD3)
    for t in tops: c.layer(c.rect(t - 1, 1, t, 4), FEATHER); c.paint(c.rect(t, 1, t, 4), FEATHER2)
    for b in bottoms: c.layer(c.rect(b, 19, b, 21), STEEL)
    c.layer(c.rect(7, 12, 17, 13), ROPE)
    return c

def binyeo():
    """A silver hairpin lying aslant, a plum blossom at its head, tarnished green where the river kept it."""
    SILVER, SILVER2, SILVER3, TARNISH = (196, 200, 210), (140, 146, 160), (236, 240, 246), (122, 150, 132)
    c = Canvas(24, 24)
    pin = c.line([(4, 21), (17, 7)], 2)
    c.layer(pin, SILVER)
    c.paint(c.line([(5, 21), (17, 8)]) & pin, SILVER2)
    c.paint(c.rect(6, 18, 7, 19) & pin | c.rect(9, 15, 9, 15) & pin, TARNISH)
    head = c.ellipse(14, 2, 22, 10)
    c.layer(head, SILVER)
    c.paint(c.ellipse(18, 6, 22, 10) & head, SILVER2)
    for x, y in ((17, 3), (20, 4), (16, 7), (19, 8)): c.paint(c.rect(x, y, x, y), SILVER3)
    c.paint(c.rect(18, 5, 18, 6), (200, 90, 110))
    return c

ICONS = {"item_misutgaru": misutgaru, "item_jumeokbap": jumeokbap, "item_cheongsimhwan": cheongsimhwan,
         "item_ward": ward_charm, "item_jipsin": jipsin, "item_arrows": arrow_bundle, "item_binyeo": binyeo}
os.makedirs(OUT, exist_ok=True)
for name, draw in ICONS.items():
    c = draw()
    c.save(f"{OUT}/{name}.png")
    if "--preview" in sys.argv: c.save(f"{OUT}/{name}_x8.png", 8)
