# Builds the elder's alarm, hit and corpse frames from his idle (no new drawing).
#   elder_cane     the cane cut out below his hand (one outline row caps it)
#   elder_startle  recoil, cane still in hand
#   elder_draw_00  the cane cut away (one outline row closes his fist); elder_draw_01 leaning in, sword ready
#   elder_hit_00   recoil; elder_hit_01 knees buckle (both without the cane, since the alarm always comes first)
#   elder_corpse   the caneless body turned a quarter (lossless), lying on his back with his feet where he stood
# Usage: python make_elder.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from framelib import load, save, bend, clean, preview_sheet, preview_dir
root = sys.argv[1] + "/08_Village_Elder"

# ~25 px art pixels on a 1254 canvas; the art faces right. Every frame keeps the idle canvas and feet line.
E = 25
elder = load(f"{root}/village_elder_idle.png")
ink = elder[750, 830].copy()                    # the outline beside his fist
cane = np.zeros_like(elder); cane[780:1170, 755:860] = elder[780:1170, 755:860]
cane[780:803, 760:815] = ink
bare = elder.copy(); bare[780:1170, 755:860] = 0
bare[778:803, 755:840] = ink

frames = {
    "elder_cane": cane,
    "elder_startle": clean(bend(elder, -50, 500, 860, E)),
    "elder_draw_00": bare,
    "elder_draw_01": clean(bend(bare, 25, 500, 860, E)),
    # Feet (x 600, bottom edge 1166 from the top) land at x 1166, 600 from the bottom of the turned canvas.
    "elder_corpse": np.rot90(bare, 1),
    "elder_hit_00": clean(bend(bare, -50, 500, 860, E)),
    "elder_hit_01": clean(bend(bare, -75, 500, 900, E, (960, 1035))),
}
for name, f in frames.items(): save(f, f"{root}/{name}.png")

if preview_dir():
    preview_sheet([f"{root}/{n}.png" for n in ["village_elder_idle"] + list(frames)], preview_dir() + "/elder_sheet.png")
