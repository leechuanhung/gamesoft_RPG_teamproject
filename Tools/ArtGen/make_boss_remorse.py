# Builds the purified boss's grieving frames from his purified idle (no new drawing), for when he learns
# he killed the elder: boss_grieve_00 knees buckle (leg rows removed), boss_grieve_01 slumped forward, head bowed;
# both with the eye shut and the grin inked into a sobbing mouth.
# Same 700x600 canvas and feet line as boss_death (idle centred at x 351, feet 8 px above the bottom).
# Usage: python make_boss_remorse.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from framelib import load, save, bend, clean, preview_sheet, preview_dir
root = sys.argv[1]

B = 10
idle = load(f"{root}/06_Boss_Purified/boss_purified_idle.png")
# Grief on the grinning face, in colours already there: the eye shut (skin with one outline row across it) and
# the fang inked over, so the mouth reads as a dark sobbing mouth rather than a grin.
skin, ink = idle[165, 330].copy(), idle[138, 355].copy()
idle[146:174, 348:374] = skin
idle[160:170, 348:374] = ink
fang = (idle[170:202, 330:360, :3].astype(int).sum(2) > 600) & (idle[170:202, 330:360, 3] > 0)
idle[170:202, 330:360][fang] = ink
ys, xs = np.where(idle[:, :, 3] > 32)                 # the opaque box JinaArt measures
dx, dy = 351 - (xs.min() + xs.max() + 1) // 2, 591 - ys.max()
base = np.zeros((600, 700, 4), np.uint8)
base[dy:dy + idle.shape[0], dx:dx + idle.shape[1]] = idle
frames = {
    "boss_grieve_00": clean(bend(base, 10, 120, 300, B, (500, 540))),   # knees buckle (cut below the hem)
    "boss_grieve_01": clean(bend(base, 50, 160, 330, B, (495, 555))),   # slumped forward, head bowed
}
names = [f"{root}/06_Boss_Purified/{n}.png" for n in frames]
for f, name in zip(frames.values(), names): save(f, name)

if preview_dir(): preview_sheet([f"{root}/06_Boss_Purified/boss_purified_idle.png"] + names, preview_dir() + "/boss_remorse_sheet.png")
