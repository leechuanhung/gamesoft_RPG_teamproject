# Builds the boss death frames from the brainwashed idle (no new drawing).
#   boss_death_00 recoil (upper body leans back), 01-02 knees buckle (leg rows removed) and slump forward,
#   03-04 the body breaks into its own 20 px chunks that drift up and vanish.
# Usage: python make_boss_death.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from framelib import load, save, bend, clean, scatter, preview_sheet, preview_dir
root = sys.argv[1] + "/05_Boss_Brainwashed"

# 10 px art pixels; the art faces right, so "back" is left. Canvas 700x600, feet 8 px above the bottom.
B = 10
idle = load(f"{root}/boss_brainwashed_idle.png")
base = np.zeros((600, 700, 4), np.uint8)
base[2:2 + idle.shape[0], 60:60 + idle.shape[1]] = idle  # idle bottom row 589 -> 591; bbox centre x 291 -> 351
frames = [clean(f) for f in (
    bend(base, -30, 120, 300, B),                       # recoil
    bend(base, 10, 120, 300, B, (500, 540)),            # knees buckle (cut below the hem)
    bend(base, 40, 160, 330, B, (495, 555)),            # slump forward on the knees
)]
# 2-pixel chunks read as the body crumbling; the source frame is already clean, so no opening here
frames += [scatter(frames[2], .3, 2 * B), scatter(frames[2], .65, 2 * B, 11)]
names = [f"{root}/boss_death_{i:02d}.png" for i in range(len(frames))]
for f, name in zip(frames, names): save(f, name)

if preview_dir(): preview_sheet(names, preview_dir() + "/boss_death_sheet.png")
