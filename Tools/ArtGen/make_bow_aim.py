# Builds the upward and downward bow shots from the level bow attack frames (no new drawing).
# The drawing arm, bow and arrow are cut out in front of the shoulder and turned about the shoulder; the body stays put.
#   bow_up_00..03, bow_down_00..03 (same canvas and feet line as bow_attack_00..03)
# Usage: python make_bow_aim.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from framelib import load, save, clean, preview_sheet, preview_dir
root = sys.argv[1] + "/03_Player_Attack_Bow"

B = 10
ANGLE = 30                      # degrees; PrototypeCombat flies the arrow at the same angle
# Per frame: the pivot (x, y from the top) where the arm leaves the body, the cut line in front of the body, and
# the line left of which the bow never reaches (above the hat and below the waist). The release frame leans
# forward, so its arm starts further right; the follow-through holds the bow close. PrototypeCombat launches the
# arrow from the release frame (02), turned about its pivot.
FRAMES = [((300, 255), 320, 395), ((290, 215), 310, 395), ((400, 245), 395, 395), ((330, 245), 335, 335)]
# The cut: everything in front of the body between the hat and the waist, the bow tip above the hat, and the
# bow's lower limb below the waist, told from the robe by colour (brown wood with its dark outline, and the thin
# string standing clear of everything).
HAT, WAIST = 130, 330

def cut(a, arm_x, limb_x):
    h, w, _ = a.shape
    ys, xs = np.mgrid[0:h, 0:w]
    r, g, b = (a[:, :, k].astype(int) for k in range(3))
    solid = a[:, :, 3] > 0
    wood = solid & (r - b > 50) & (r > 90)
    # the limb's outline: dark pixels within 12 px of the wood
    near = np.zeros_like(wood)
    for d in range(-12, 13, 4):
        near |= np.roll(wood, d, 0) | np.roll(wood, d, 1)
    dark = solid & (r + g + b < 200) & near
    # the string: a thin light line with nothing 12 px to either side
    string = solid & (r + g + b > 600) & ~np.roll(solid, 12, 1) & ~np.roll(solid, -12, 1)
    below = (xs >= limb_x) & (ys > WAIST) & (ys < 490) & (wood | dark | string)
    m = solid & (((xs >= arm_x) & (ys >= HAT) & (ys <= WAIST)) | ((xs >= limb_x) & (ys < HAT)) | below)
    part = np.where(m[:, :, None], a, 0).astype(np.uint8)
    body = np.where(m[:, :, None], 0, a).astype(np.uint8)
    return body, part

def turn(part, degrees, pivot):
    """Rotate at full resolution with nearest sampling: an art pixel is ~10 px, so lines stay whole and the
    1 px steps along the edges vanish at game scale (rotating on the 10 px grid broke the thin bow and string)."""
    im = Image.fromarray(part)
    return np.array(im.rotate(degrees, resample=Image.NEAREST, center=pivot))

names = []
for i in range(4):
    a = load(f"{root}/bow_attack_{i:02d}.png")
    pivot, arm_x, limb_x = FRAMES[i]
    body, part = cut(a, arm_x, limb_x)
    for tag, sign in (("up", 1), ("down", -1)):
        turned = turn(part, ANGLE * sign, pivot)
        frame = body.copy(); m = turned[:, :, 3] > 0; frame[m] = turned[m]
        name = f"{root}/bow_{tag}_{i:02d}.png"
        save(clean(frame), name); names.append(name)

if preview_dir(): preview_sheet([f"{root}/bow_attack_{i:02d}.png" for i in range(4)] + names, preview_dir() + "/bow_aim_sheet.png", 560)
