# Builds the ghost guard's death frames from its idle (no new drawing).
#   ghost_death_00 recoil (upper body leans back), 01-02 the body breaks into its own 50 px chunks that drift up.
# Usage: python make_ghost_death.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from framelib import load, save, bend, clean, scatter, preview_sheet, preview_dir
root = sys.argv[1] + "/07_Ghost"

# ~25 px art pixels on a 1254 canvas; the art faces right. Every frame keeps the idle canvas and feet line.
G = 25
idle = load(f"{root}/ghost_guard_idle.png")
recoil = clean(bend(idle, -50, 400, 800, G))
frames = [recoil, scatter(recoil, .3, 2 * G), scatter(recoil, .65, 2 * G, 11)]
names = [f"{root}/ghost_death_{i:02d}.png" for i in range(len(frames))]
for f, name in zip(frames, names): save(f, name)

if preview_dir(): preview_sheet([f"{root}/ghost_guard_idle.png"] + names, preview_dir() + "/ghost_death_sheet.png")
