# Makes the seonbi a clean-shaven young man of about twenty: the beard and the black strap down his cheek come off the V6
# frames, and his bare chin, neck and the V of his collar go on. Only the head and collar are touched; hat, robe and pose
# stay. Every frame's head is the same drawing on an art-pixel grid measured from its face (see FACE), so each edit is
# a few blocks written over the frame. Writes are fixed blocks, so running this again changes nothing. Frames derived
# from these (run, hit, dodge, game over, aimed bow) are remade after it by make_all.py.
# Usage: python make_young.py Assets/Resources/JinaV4
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from framelib import load, save
root = sys.argv[1]

SKIN = (254, 196, 134, 255)
SHADE = (222, 158, 110, 255)      # the neck, under the chin
CREAM = (250, 243, 224, 255)
BLACK = (12, 10, 12, 255)
CLEAR = (0, 0, 0, 0)
INK = {"S": SKIN, "s": SHADE, "C": CREAM, "K": BLACK, " ": CLEAR}

# The usual head, by row of the grid. Each string starts at column -5; '.' keeps what is there. Column 0 is the face's
# left edge as drawn. The eye moves in from the edge of the face to column 1 so that it reads as an eye with a cheek
# before it; the strap on column -1 stays only as a short sideburn, and hair takes column -3 so the ear stands alone.
HEAD = {0: "..KK..SS",
        1: ".KKSKSKSK ",              # hair, ear, sideburn, cheek, eye, the bridge of the nose
        2: ".KKSSSSSK ",
        3: ".KKSSSSSK ",
        4: "...sSSSK  ",              # a bare chin where the beard began, the neck behind it in shade
        5: "...ssKK   ",              # the line under the chin
        6: "...KsK    "}              # the neck going into the collar

def face(**rows):
    return {**HEAD, **{int(k[1:]): v for k, v in rows.items()}}

# Where something stands right in front of his face, the blocks beyond the nose stay as they are.
CLOSE = {1: ".KKSKSKSK", 2: ".KKSSSSSK", 3: ".KKSSSSSK"}

# frame: (left, top, right, bottom of the face's skin as drawn in the V6 frame, rows)
FACE = {
    "01_Player/player_idle": ((123, 111, 157, 160), face(r7="....KC")),
    "10_Player_Walk/player_walk_00": ((188, 137, 229, 191), face(r7="....KCK")),
    "10_Player_Walk/player_walk_01": ((198, 135, 239, 189), face(r7="....KCK ", r8=".....CCK")),
    "10_Player_Walk/player_walk_02": ((210, 139, 251, 192), face(r7="....KCK", r8=".....CK")),
    "10_Player_Walk/player_walk_03": ((183, 140, 223, 193), face(r7="....KCK", r8=".....CK")),
    "02_Player_Attack_Sword/sword_attack_00": ((222, 218, 251, 260), face()),
    "02_Player_Attack_Sword/sword_attack_01": ((331, 237, 362, 273), {**face(r4="....SSSK", r5="...KsK", r6=""), **CLOSE}),
    "02_Player_Attack_Sword/sword_attack_02": ((269, 237, 298, 277), face(r6="...KsKK")),
    "02_Player_Attack_Sword/sword_attack_03": ((315, 265, 346, 307), face(r5="...ssK    ", r6="...KsK", r7="....K")),
    "03_Player_Attack_Bow/bow_attack_00": ((264, 128, 300, 177), face(r6="...KsKK", r7="....KC")),
    "03_Player_Attack_Bow/bow_attack_01": ((260, 154, 296, 198), {**face(r4="", r5="", r6=""), **CLOSE}),      # the drawing hand is at his chin
    "03_Player_Attack_Bow/bow_attack_02": ((328, 151, 363, 196), face(r5="...ssKK", r6="...KsKC", r7="....KCC")),
    "03_Player_Attack_Bow/bow_attack_03": ((224, 159, 258, 205), face(r5="...ssKK", r6="...KsKC", r7="....KCC")),
    "09_Player_Jump/jump_00": ((311, 251, 348, 301), face(r7="..CCK ", r8="..CCK ")),
    "09_Player_Jump/jump_01": ((314, 123, 350, 172), face(r7="...CK ")),
    "09_Player_Jump/jump_02": ((299, 148, 336, 197), face(r7="...KKK ")),
    "09_Player_Jump/jump_03": ((304, 238, 342, 286), face(r7="...KK")),
}

for name, ((x0, y0, x1, y1), rows) in FACE.items():
    path = f"{root}/{name}.png"
    a = load(path)
    # Three columns and four rows of art pixels make up the face.
    sx, sy = (x1 - x0) / 3, (y1 - y0) / 4
    for r, row in rows.items():
        for i, ink in enumerate(row):
            if ink == ".": continue
            c = i - 5
            a[int(round(y0 + r * sy)):int(round(y0 + (r + 1) * sy)), int(round(x0 + c * sx)):int(round(x0 + (c + 1) * sx))] = INK[ink]
    save(a, path)
