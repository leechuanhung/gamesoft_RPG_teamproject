# Draws the guest room at Patjwi's house, where the seonbi spends the night (shown only in the cutscene), at one texel
# per art pixel, 24 per unit, the same scale as the characters and props: the room is 10 x 5.5 units, and the cutscene's
# camera moves in so it fills the screen.
#   13_Props/room_bg        walls, floor, folding screen, the seonbi's gat on a peg, low table; the door's panes are holes
#   13_Props/room_door      the plain paper behind those holes (the game tints it moonlit or sunlit)
#   13_Props/door_shadow    what passes outside at night, thrown on the paper: a hunched thing with a great jar for a body
#   13_Props/bed_under      the mattress and pillow
#   13_Props/quilt_sleep, quilt_sit   the quilt, drawn over his body so his robe runs on underneath it
#   01_Player/player_sit    the in-game idle (same face, robe, scale) with the gat off and a topknot, cut at the lap
#   01_Player/player_sleep  that same picture turned a quarter, lying on his back as the corpse does
# Usage: python make_room.py Assets/Resources/JinaV4/13_Props
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
from framelib import load, save as save_frame
OUT = sys.argv[1]
WOOD, WOOD2, WOOD3 = (122, 82, 50), (92, 60, 36), (150, 104, 66)
WALL, WALL2 = (221, 203, 165), (196, 176, 136)
FLOOR, FLOOR2, FLOOR3 = (208, 170, 98), (182, 144, 78), (226, 192, 124)
PAPER, PAPER2, INK = (238, 228, 200), (214, 202, 170), (70, 66, 70)
HAT, HAT2 = (42, 40, 48), (70, 68, 78)
QUILT, QUILT2, QUILT3, YO, YO2 = (150, 58, 58), (116, 42, 46), (232, 190, 110), (226, 216, 190), (196, 184, 156)
BRASS, FLAME = (176, 136, 60), (250, 190, 80)

def room_bg():
    c = Canvas(240, 132)
    FLOOR_Y = 88
    # wall and floor
    c.paint(c.rect(0, 0, 239, FLOOR_Y - 1), WALL, inside=False)
    c.paint(c.rect(0, 65, 239, FLOOR_Y - 1), WALL2, inside=False)
    c.paint(c.rect(0, FLOOR_Y, 239, 131), FLOOR, inside=False)
    for y in range(FLOOR_Y + 11, 132, 11): c.paint(c.rect(0, y, 239, y), FLOOR2, inside=False)
    for row, y in enumerate(range(FLOOR_Y, 132, 11)):
        for x in range((row % 2) * 15, 240, 30): c.paint(c.rect(x, y, x, y + 10), FLOOR2, inside=False)
    c.paint(c.rect(0, FLOOR_Y, 239, FLOOR_Y), FLOOR3, inside=False)
    # beams and posts, skirting board
    c.layer(c.rect(0, 85, 239, 87), WOOD2)
    c.layer(c.rect(0, 0, 239, 7), WOOD); c.paint(c.rect(0, 6, 239, 7), WOOD2)
    for x0, x1 in ((0, 5), (156, 160), (234, 239)):
        c.layer(c.rect(x0, 8, x1, 87), WOOD); c.paint(c.rect(x1 - 1, 8, x1, 87), WOOD2)
    # folding screen with ink mountains
    for x in range(13, 85, 12):
        c.layer(c.rect(x, 39, x + 11, 86), PAPER)
        c.paint(c.rect(x + 10, 40, x + 11, 86), PAPER2)
        c.layer(c.rect(x, 37, x + 11, 39), WOOD3)
    hills = c.poly([(15, 75), (28, 52), (39, 67), (51, 48), (65, 70), (75, 59), (83, 75)])
    c.paint(hills & c.rect(13, 40, 84, 86), (176, 174, 170))
    c.paint(c.poly([(25, 75), (37, 63), (49, 75)]) | c.poly([(55, 75), (69, 61), (80, 75)]), (120, 118, 118))
    # his gat hanging from a peg, its chin cord below
    c.layer(c.rect(110, 18, 112, 21), WOOD2)
    c.layer(c.rect(101, 31, 121, 32), HAT); c.layer(c.rect(106, 22, 116, 31), HAT)
    c.paint(c.rect(108, 24, 109, 29), HAT2)
    c.paint(c.line([(103, 33), (111, 43), (119, 33)]), (40, 38, 46), inside=False)
    # low writing table with books and an oil lamp
    c.layer(c.rect(119, 75, 153, 77), WOOD3)
    c.layer(c.rect(122, 78, 124, 90) | c.rect(148, 78, 150, 90), WOOD)
    c.layer(c.rect(125, 71, 138, 74), PAPER)
    c.layer(c.rect(127, 68, 136, 70), (92, 110, 130))
    c.layer(c.rect(144, 69, 146, 74), BRASS); c.layer(c.rect(142, 67, 148, 68), BRASS)
    c.paint(c.ellipse(144, 62, 146, 66), FLAME, inside=False)
    # the door: a frame and lattice whose panes are holes, showing the paper behind
    c.layer(c.rect(165, 17, 228, 87), WOOD)
    panes = c.blank()
    for col in range(4):
        for row in range(4):
            x0, y0 = 168 + col * 15, 20 + row * 17
            panes |= c.rect(x0, y0, x0 + 12, y0 + 14)
    c.c[panes] = 0; c.a[panes] = False
    return c

def room_door():
    c = Canvas(240, 132)
    c.paint(c.rect(165, 17, 228, 87), (255, 255, 255), inside=False)
    return c

def door_shadow():
    """No outline: a shadow. A great round jar with a toad's two bulging eyes on top and thin hunched arms."""
    c = Canvas(48, 71)
    d = (255, 255, 255)
    c.paint(c.ellipse(5, 23, 43, 68), d, inside=False)
    c.paint(c.rect(14, 15, 34, 26), d, inside=False)
    c.paint(c.ellipse(10, 4, 22, 17) | c.ellipse(26, 4, 38, 17), d, inside=False)
    c.paint(c.line([(6, 37), (0, 50), (3, 63)], 3) | c.line([(42, 37), (47, 50), (44, 63)], 3), d, inside=False)
    return c

def bed_under():
    c = Canvas(68, 14)
    c.layer(c.rect(1, 7, 66, 12), YO)
    c.paint(c.rect(1, 11, 66, 12), YO2)
    c.layer(c.rect(2, 2, 16, 7), PAPER); c.paint(c.rect(2, 6, 16, 7), PAPER2)
    return c

def quilt(c, shape):
    c.layer(shape, QUILT)
    c.paint(shape & c.rect(0, c.h - 7, c.w, c.h), QUILT2)
    for x in range(10, c.w - 4, 11): c.paint(c.line([(x, 0), (x - 2, c.h)]) & shape, QUILT3)
    return c

def quilt_sleep():
    """Pulled up to his chest: tall enough to hide his body, which lies as wide as his robe."""
    c = Canvas(50, 21)
    return quilt(c, c.poly([(1, 19), (3, 5), (10, 2), (28, 1), (42, 5), (48, 19)]))

def quilt_sit():
    """Bunched over his lap and legs, wider than his robe so nothing shows past it."""
    c = Canvas(56, 15)
    return quilt(c, c.poly([(1, 13), (4, 5), (12, 2), (30, 1), (46, 4), (54, 13)]))

def seonbi_in_bed():
    """The idle with its gat taken off: rows above the brim go, hair and a topknot on the same 12 px blocks take their
    place, the hat's lining is blacked over. Below the lap nothing remains, so only the quilt covers him."""
    root = OUT.replace("\\", "/").rsplit("/", 1)[0] + "/01_Player"
    a = load(f"{root}/player_idle.png")
    HAIR_DOT = (11, 9, 11, 255); SHINE = (60, 58, 70, 255)
    a[:104] = 0   # the brim's last row of black too
    a[100:122] = np.where((a[100:122, :, 3:] > 32) & (a[100:122, :, :3].sum(2, keepdims=True) < 330), np.array(HAIR_DOT, np.uint8), a[100:122])
    for y0, y1, x0, x1 in ((88, 112, 72, 157), (76, 88, 84, 145), (64, 76, 96, 133), (40, 64, 104, 124)):
        a[y0:y1, x0:x1] = HAIR_DOT
    a[70:76, 104:122] = SHINE; a[46:52, 110:118] = SHINE
    a[326:] = 0
    save_frame(a, f"{root}/player_sit.png")
    save_frame(np.rot90(a, 1), f"{root}/player_sleep.png")

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")

for name, draw in (("room_bg", room_bg), ("room_door", room_door), ("door_shadow", door_shadow), ("bed_under", bed_under), ("quilt_sleep", quilt_sleep), ("quilt_sit", quilt_sit)):
    save(draw(), name)
seonbi_in_bed()
