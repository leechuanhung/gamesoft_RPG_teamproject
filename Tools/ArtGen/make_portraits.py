# Cuts the dialogue portraits out of the sheet the user drew (Tools/ArtGen/source/kongpat_portraits.webp): two rows of
# three framed panels, Kongjwi above and Patjwi below. Each panel's picture inside its frame is reduced to 112 x 121
# (shown 2x in the talk window).
#   top row:    kongjwi (smiling), kongjwi_stare (the cold look), kongjwi_true (unmasked)
#   bottom row: patjwi (the hard stare), patjwi_angry (shouting), patjwi_down (giving in)
# Usage: python make_portraits.py Assets/Resources/JinaV4/17_Portraits
import sys, os
from PIL import Image

OUT = sys.argv[1]
SHEET = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source", "kongpat_portraits.webp")
SIZE = (112, 121)
# Inside of each panel's frame (left, top, right, bottom), with the corner ornaments trimmed off.
COLS = [(158, 545), (637, 1011), (1100, 1489)]
ROWS = [(40, 466), (550, 976)]
NAMES = [["kongjwi", "kongjwi_stare", "kongjwi_true"], ["patjwi", "patjwi_angry", "patjwi_down"]]

sheet = Image.open(SHEET).convert("RGB")
os.makedirs(OUT, exist_ok=True)
for r, (y0, y1) in enumerate(ROWS):
    for c, (x0, x1) in enumerate(COLS):
        panel = sheet.crop((x0, y0, x1, y1))
        # Keep the panel's shape: fit the width, then take the middle of the height.
        w, h = SIZE
        scaled = panel.resize((w, round(panel.height * w / panel.width)), Image.BOX)
        top = max(0, (scaled.height - h) // 2)
        out = Image.new("RGBA", SIZE)
        out.paste(scaled.crop((0, top, w, top + h)), (0, 0))
        out.save(f"{OUT}/{NAMES[r][c]}.png")
