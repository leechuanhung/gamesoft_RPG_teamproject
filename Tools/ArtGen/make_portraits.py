# Draws dialogue portraits: busts at a finer grain than the sprites (56 x 60 dots, shown 4x in the talk window),
# in the same rules (1-dot black outline per part, flat fill, one shade).
#   17_Portraits/patjwi       open, steady eyes under straight brows, red daenggi ribbon, jade jacket
#   17_Portraits/kongjwi      eyes closed in smiling crescents, flower pin, yellow jacket
#   17_Portraits/kongjwi_true the same face unmasked: eyes open, red slit pupils, a wide grin, shadow over the brow
# Usage: python make_portraits.py Assets/Resources/JinaV4/17_Portraits
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from pixlib import Canvas, K
OUT = sys.argv[1]
SKIN, SKIN2, BLUSH = (250, 214, 180), (232, 176, 140), (242, 156, 150)
HAIR, HAIR2, SHINE = (34, 26, 30), (58, 44, 50), (96, 82, 96)
IRIS, WHITE, MOUTH, LIP = (70, 44, 40), (255, 255, 255), (196, 82, 86), (226, 120, 120)
COTTON = (253, 248, 228)

def bust(jacket, jacket2, goreum):
    c = Canvas(56, 60)
    # back hair falling behind the shoulders
    c.layer(c.ellipse(9, 6, 47, 54) & c.rect(0, 0, 56, 52), HAIR)
    # shoulders and jacket, the white collar (git) crossing left over right, the coat-string bow
    body = c.poly([(6, 59), (12, 49), (22, 45), (34, 45), (44, 49), (50, 59)])
    c.layer(body, jacket)
    c.paint(body & c.rect(31, 0, 56, 60), jacket2)
    c.layer(c.poly([(22, 45), (26, 45), (35, 59), (30, 59)]) | c.poly([(34, 45), (30, 45), (25, 52), (28, 54)]), COTTON)
    c.layer(c.poly([(30, 53), (36, 51), (35, 55)]) | c.poly([(30, 53), (24, 56), (26, 59), (29, 59)]), goreum)
    # neck
    c.layer(c.rect(24, 38, 32, 46), SKIN); c.paint(c.rect(24, 38, 32, 41), SKIN2)
    # face: soft oval narrowing to the chin
    face = c.ellipse(15, 11, 41, 44) & c.rect(0, 0, 56, 36) | c.poly([(15, 30), (41, 30), (36, 40), (28, 45), (20, 40)])
    c.layer(face, SKIN)
    # hair on top: parted in the middle, smooth over the crown, a shine, framing locks down the cheeks
    cap = (c.ellipse(13, 6, 43, 36) & c.rect(0, 0, 56, 19)) | c.poly([(14, 18), (19, 18), (17, 34), (13, 38)]) | c.poly([(42, 18), (37, 18), (39, 34), (43, 38)])
    c.layer(cap, HAIR)
    c.paint(c.rect(27, 7, 28, 18) & cap, HAIR2)
    c.paint((c.ellipse(16, 9, 26, 15) | c.ellipse(30, 9, 40, 15)) & ~c.ellipse(17, 11, 39, 19) & cap, SHINE)
    # cheeks, nose
    c.paint(c.ellipse(17, 31, 22, 34) | c.ellipse(34, 31, 39, 34), BLUSH)
    c.paint(c.rect(28, 31, 28, 32), SKIN2)
    return c

def eyes_open(c, brow_tilt, iris=IRIS, pupil=K):
    for x0, side in ((18, -1), (31, 1)):
        c.paint(c.rect(x0, 24, x0 + 7, 25), K)                       # upper lash line, thick
        c.paint(c.rect(x0 - (1 if side < 0 else -7), 24, x0 - (1 if side < 0 else -7), 24), K)  # outer lash flick
        c.paint(c.rect(x0 + 1, 26, x0 + 6, 29), WHITE)
        c.paint(c.rect(x0 + 2, 26, x0 + 5, 30), iris)
        c.paint(c.rect(x0 + 3, 27, x0 + 4, 29), pupil)
        c.paint(c.rect(x0 + 2, 26, x0 + 3, 27), WHITE)                 # highlight
        c.paint(c.rect(x0 + 1, 30, x0 + 6, 30) & ~c.rect(x0 + 2, 30, x0 + 5, 30), K)
        # brows: tilt > 0 slopes down toward the middle (resolve), 0 straight
        for i in range(6):
            y = 20 + (brow_tilt * i // 5 if side < 0 else brow_tilt * (5 - i) // 5)
            c.paint(c.rect(x0 + 1 + i, y, x0 + 1 + i, y), HAIR)

def eyes_smiling(c):
    for x0 in (18, 31):
        c.paint(c.rect(x0 + 1, 27, x0 + 1, 27) | c.rect(x0 + 2, 26, x0 + 5, 26) | c.rect(x0 + 6, 27, x0 + 6, 27), K)
        c.paint(c.rect(x0, 26, x0, 26) | c.rect(x0 + 7, 26, x0 + 7, 26), K)
        for i in range(6): c.paint(c.rect(x0 + 1 + i, 21, x0 + 1 + i, 21), HAIR)

def patjwi():
    c = bust((150, 204, 186), (110, 168, 152), (196, 58, 58))
    eyes_open(c, 2)
    c.paint(c.rect(26, 37, 30, 37), MOUTH)                                    # small, set mouth
    c.layer(c.rect(42, 30, 46, 33) | c.poly([(44, 33), (48, 41), (45, 41)]), (196, 58, 58))   # daenggi ribbon at the nape
    return c

def kongjwi():
    c = bust((244, 214, 110), (214, 178, 70), (150, 38, 44))
    eyes_smiling(c)
    c.paint(c.rect(25, 36, 31, 36) | c.rect(26, 37, 30, 37), MOUTH); c.paint(c.rect(27, 37, 29, 37), LIP)   # sweet smile
    c.layer(c.ellipse(10, 9, 16, 15), (240, 150, 170)); c.paint(c.rect(13, 12, 13, 12), WHITE)            # flower pin
    return c

def kongjwi_true():
    c = kongjwi()
    # the same face: a shadow falls over the brow, the eyes open with red slit pupils, the smile splits wide
    skin = (c.c == SKIN).all(-1) & c.a
    c.paint(skin & c.ellipse(12, -4, 44, 25), SKIN2)
    eyes_open(c, -2, iris=(200, 40, 40), pupil=(250, 210, 80))
    for x0 in (18, 31): c.paint(c.rect(x0 + 3, 27, x0 + 4, 29) & ~c.rect(x0 + 4, 27, x0 + 4, 29), K)   # slit
    c.paint(c.rect(22, 36, 34, 37) | c.rect(23, 38, 33, 38), (60, 20, 24))                         # wide grin
    for x in range(23, 34, 2): c.paint(c.rect(x, 36, x, 36), WHITE)                                # teeth
    return c

def save(c, name):
    img = np.zeros((c.h, c.w, 4), np.uint8); img[..., :3] = c.c; img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")
    if "--preview" in sys.argv: Image.fromarray(img).resize((c.w * 6, c.h * 6), Image.NEAREST).save(f"{OUT}/{name}_x6.png")

os.makedirs(OUT, exist_ok=True)
for name, draw in (("patjwi", patjwi), ("kongjwi", kongjwi), ("kongjwi_true", kongjwi_true)):
    save(draw(), name)
