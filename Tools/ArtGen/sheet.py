# Lays pictures side by side, feet on one line, at their in-game sizes, to check a new picture against the
# existing characters and props. Write sheets to the scratchpad, not into Assets.
#   python Tools/ArtGen/sheet.py OUT.png PICTURE [PICTURE ...] [--zoom 2] [--sky village|camp|stele|forest]
# PICTURE is a path under Assets/Resources/JinaV4 (with or without that prefix and .png). Each is scaled by the
# pixels-per-unit JinaArt.Load gives it (mirrored below), to 100 px per world unit times --zoom.
import os, sys
import numpy as np
from PIL import Image

ART = "Assets/Resources/JinaV4/"
SKY = {"village": (61, 77, 87), "camp": (77, 51, 51), "stele": (43, 46, 71), "forest": (26, 59, 51)}

def option(name, default):
    if name in sys.argv:
        i = sys.argv.index(name); value = sys.argv[i + 1]; del sys.argv[i:i + 2]; return value
    return default

def opaque_height(path):
    a = np.array(Image.open(path).convert("RGBA"))[:, :, 3]
    ys = np.where(a.max(1) > 32)[0]
    return ys.max() - ys.min() + 1

# Pixels per world unit, as in JinaArt.Load (keep the two in step).
def ppu(path):
    rel = path[len(ART):-4]
    idle = {"01_Player": ("01_Player/player_idle", 1.9), "05_Boss_Brainwashed": ("05_Boss_Brainwashed/boss_brainwashed_idle", 2.6),
            "06_Boss_Purified": ("06_Boss_Purified/boss_purified_idle", 2.6), "07_Ghost": ("07_Ghost/ghost_guard_idle", 1.65),
            "08_Village_Elder": ("08_Village_Elder/village_elder_idle", 1.8), "12_Peddler": ("12_Peddler/peddler_idle", 1.95)}
    folder = rel.split("/")[0]
    if folder == "13_Props": return 546 / 1.9 / 12
    if folder == "16_Kong": return ppu(ART + "12_Peddler/peddler_idle.png")       # same 12 px blocks
    if folder == "15_Ferryman": return ppu(ART + "12_Peddler/peddler_idle.png")   # same 12 px blocks
    if folder == "21_KongBoss": return ppu(ART + "12_Peddler/peddler_idle.png")   # same 12 px blocks
    if "Player_Attack_Sword" in rel: return 450 / 1.9
    if "Player_Attack_Bow" in rel: return 510 / 1.9
    if "Player_Jump" in rel: return 535 / 1.9
    if "Player_Walk" in rel or "Player_Run" in rel: return 546 / 1.9
    if "boss_attack" in rel: return 480 / 2.6
    if "boss_walk" in rel: return 586 / 2.6
    if folder in idle:   # the idle itself, or a frame cut from it, which shares its scale
        src, height = idle[folder]
        return opaque_height(ART + src + ".png") / height
    return 100

zoom = float(option("--zoom", "1"))
sky = SKY[option("--sky", "village")]
out, names = sys.argv[1], sys.argv[2:]
ims = []
for n in names:
    path = n if n.startswith(ART) else ART + n
    if not path.endswith(".png"): path += ".png"
    im = Image.open(path).convert("RGBA")
    box = im.getbbox()
    if box: im = im.crop((box[0], 0, box[2], box[3]))   # keep the bottom (feet line), drop empty sides
    scale = 100 / ppu(path) * zoom
    ims.append(im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.NEAREST))
pad = 20
w = sum(i.width for i in ims) + pad * (len(ims) + 1); h = max(i.height for i in ims) + pad * 2
sheet = Image.new("RGBA", (w, h), sky + (255,))
x = pad
for im in ims:
    sheet.alpha_composite(im, (x, h - pad - im.height)); x += im.width + pad
os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
sheet.save(out)
print(out, sheet.size)
