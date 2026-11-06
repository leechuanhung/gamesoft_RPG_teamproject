# Builds the game-over collapse from the seonbi's idle (no new drawing), shown alone on the black screen.
#   player_ko_00 whiplash (upper body thrown far back), player_ko_01 slump (head drops forward, knees buckle),
#   player_ko_02 on his knees, bowed forward about the hip, player_facedown the idle turned a quarter clockwise
#   (lossless), lying face down with his head ahead of him.
# Frames sit on the idle canvas widened by 240 px each side, so the bow has room; JinaArt adds the same pad.
# Usage: python make_player_ko.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from framelib import load, save, bend, clean, preview_sheet, preview_dir
root = sys.argv[1] + "/01_Player"

# ~12 px art pixels; the art faces right. Robe from the shoulders to y 470, legs below, hip near y 400.
P, PAD, HIP = 12, 240, 400
idle = load(f"{root}/player_idle.png")
base = np.zeros((idle.shape[0], idle.shape[1] + 2 * PAD, 4), np.uint8)
base[:, PAD:PAD + idle.shape[1]] = idle
cx = PAD + idle.shape[1] // 2

def bow(a, degrees, hip):
    """Turn everything above `hip` forward about the hip (nearest neighbour at full resolution, like make_bow_aim)."""
    upper, lower = a.copy(), a.copy()
    upper[hip:] = 0; lower[:hip] = 0
    turned = np.array(Image.fromarray(upper).rotate(-degrees, resample=Image.NEAREST, center=(cx, hip)))
    out = lower.copy(); m = turned[:, :, 3] > 0
    out[m] = turned[m]
    return out

kneel = bend(base, 0, 120, HIP, P, (430, 514))   # thighs gone: he sinks 84 px onto his knees
frames = {
    "player_ko_00": clean(bend(base, -72, 100, 400, P, (470, 482))),
    "player_ko_01": clean(bend(base, 36, 120, 380, P, (450, 500))),
    "player_ko_02": clean(bow(kneel, 32, HIP + 84)),
    "player_facedown": np.rot90(base, -1),
}
for name, f in frames.items(): save(f, f"{root}/{name}.png")

if preview_dir():
    preview_sheet([f"{root}/{n}.png" for n in ["player_idle"] + list(frames)], preview_dir() + "/player_ko_sheet.png")
