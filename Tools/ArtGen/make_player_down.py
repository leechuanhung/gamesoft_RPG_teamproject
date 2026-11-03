# Builds the seonbi's death frames from his idle (no new drawing).
#   player_hit_00 recoil (upper body leans back), player_hit_01 knees buckle (leg rows removed),
#   player_corpse the idle turned a quarter (lossless), lying on his back with his feet where he stood.
# Frames sit on the idle canvas widened by 48 px each side, so the recoil has room; JinaArt adds the same pad.
# Usage: python make_player_down.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from framelib import load, save, bend, clean, preview_sheet, preview_dir
root = sys.argv[1] + "/01_Player"

# ~12 px art pixels; the art faces right, so "back" is left. Robe from the shoulders to y 470, legs below.
P, PAD = 12, 48
idle = load(f"{root}/player_idle.png")
base = np.zeros((idle.shape[0], idle.shape[1] + 2 * PAD, 4), np.uint8)
base[:, PAD:PAD + idle.shape[1]] = idle
frames = {
    "player_hit_00": clean(bend(base, -24, 120, 330, P)),
    "player_hit_01": clean(bend(base, -36, 120, 360, P, (470, 500))),
    "player_corpse": np.rot90(base, 1),
}
for name, f in frames.items(): save(f, f"{root}/{name}.png")

if preview_dir():
    preview_sheet([f"{root}/{n}.png" for n in ["player_idle"] + list(frames)], preview_dir() + "/player_down_sheet.png")
