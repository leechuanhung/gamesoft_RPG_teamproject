from pathlib import Path
import sys
from PIL import Image, ImageDraw

folder = Path(sys.argv[1])
sword = Image.open(folder / "hwando.png").convert("RGBA")
cover = Image.new("RGBA", (66, 60))
draw = ImageDraw.Draw(cover)
curve = [(28, 41), (39, 32), (49, 23), (58, 13), (64, 3)]
draw.line(curve, fill="#161315", width=8, joint="curve")
draw.line(curve, fill="#58392d", width=6, joint="curve")
draw.line([(x-1,y-1) for x,y in curve], fill="#86553b", width=2)
for x,y in [(29,40),(32,38),(61,8),(64,3)]:
    draw.line([(x-2,y-2),(x+2,y+2)],fill="#bf904b",width=2)
sword.alpha_composite(cover.resize(sword.size, Image.Resampling.NEAREST))
sword.save(folder / "hwando_sheathed.png")
