# Builds the boss death frames and the elder hit frames from the existing idle art (no new drawing).
#   boss_death_00 recoil (upper body leans back), 01-02 knees buckle (leg rows removed) and slump forward,
#   03-04 the body breaks into its own 20 px chunks that drift up and vanish.
#   elder_hit_00 recoil, elder_hit_01 knees buckle; the game then tips him over into the existing 80 degree pose.
# Usage: python make_deaths.py Assets/Resources/JinaV4   [--preview DIR]
import sys
from PIL import Image
import numpy as np
root = sys.argv[1]
preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

def q(v, step): return int(round(v / step) * step)

def bend(a, lean, top, hip, step, cut=(0, 0)):
    """Shift rows above `hip` by `lean` (full at `top`, tapering to 0 at hip), then remove rows cut[0]..cut[1]."""
    h, w, _ = a.shape
    out = np.zeros_like(a)
    for y in range(h):
        sh = lean if y < top else q(lean * (hip - y) / (hip - top), step) if y < hip else 0
        out[y] = np.roll(a[y], sh, 0)
        if sh > 0: out[y, :sh] = 0
        if sh < 0: out[y, sh:] = 0
    c0, c1 = cut
    if c1 > c0:  # drop the band and let everything above sink onto the legs
        out = np.concatenate([np.zeros((c1 - c0, w, 4), np.uint8), out[:c0], out[c1:]])
    return out

def clean(out):
    # same clean-up as make_run.py: drop the soft halo, then a 5x5 opening removes 1-4 px slivers
    out[out[:, :, 3] < 128] = 0
    op = out[:, :, 3] > 0
    def erode(m):
        r = m.copy()
        for dy in range(-2, 3):
            for dx in range(-2, 3): r &= np.roll(np.roll(m, dy, 0), dx, 1)
        return r
    def dilate(m):
        r = m.copy()
        for dy in range(-2, 3):
            for dx in range(-2, 3): r |= np.roll(np.roll(m, dy, 0), dx, 1)
        return r
    out[op & ~dilate(erode(op))] = 0
    return out

def scatter(a, level, step, seed=7):
    """Remove blocks whose random threshold is below `level` (upper blocks go first); blocks just above it float up."""
    rng = np.random.default_rng(seed)
    h, w, _ = a.shape
    rows, cols = h // step, w // step
    thr = rng.random((rows, cols)) * .75 + np.linspace(0, .25, rows)[:, None]  # top rows dissolve first
    out = np.zeros_like(a)
    for by in range(rows):
        for bx in range(cols):
            t = thr[by, bx]
            if t < level: continue
            block = a[by * step:(by + 1) * step, bx * step:(bx + 1) * step]
            lift = q((1 - min(1, (t - level) / .25)) * 4 * step, step) if t < level + .25 else 0
            y0 = by * step - lift
            if y0 < 0: continue
            dst = out[y0:y0 + step, bx * step:(bx + 1) * step]
            m = block[:, :, 3] > 0
            dst[m] = block[m]
    return out

# Boss: 10 px art pixels, art faces right, so "back" is left. Canvas 700x600, feet 8 px above the bottom.
B = 10
idle = np.array(Image.open(f"{root}/05_Boss_Brainwashed/boss_brainwashed_idle.png").convert("RGBA"))
W, H = 700, 600
base = np.zeros((H, W, 4), np.uint8)
base[2:2 + idle.shape[0], 60:60 + idle.shape[1]] = idle  # idle bottom row 589 -> 591; bbox centre x 291 -> 351
frames = [
    bend(base, -30, 120, 300, B),                       # recoil
    bend(base, 10, 120, 300, B, (500, 540)),            # knees buckle (cut below the hem)
    bend(base, 40, 160, 330, B, (495, 555)),            # slump forward on the knees
]
frames = [clean(f) for f in frames]
# 2-pixel chunks read as the body crumbling; the source frame is already clean, so no opening here
frames += [scatter(frames[2], .3, 2 * B), scatter(frames[2], .65, 2 * B, 11)]
for i, f in enumerate(frames):
    Image.fromarray(f).save(f"{root}/05_Boss_Brainwashed/boss_death_{i:02d}.png")

# Elder: ~25 px art pixels on a 1254 canvas, art faces right; keep the canvas and feet line of the idle.
E = 25
elder = np.array(Image.open(f"{root}/08_Village_Elder/village_elder_idle.png").convert("RGBA"))
hits = [bend(elder, -50, 500, 860, E), bend(elder, -75, 500, 900, E, (960, 1035))]
for i, f in enumerate(hits):
    Image.fromarray(clean(f)).save(f"{root}/08_Village_Elder/elder_hit_{i:02d}.png")

if preview:
    names = [f"05_Boss_Brainwashed/boss_death_{i:02d}" for i in range(5)] + ["08_Village_Elder/village_elder_idle"] + [f"08_Village_Elder/elder_hit_{i:02d}" for i in range(2)]
    ims = [Image.open(f"{root}/{n}.png").convert("RGBA") for n in names]
    ims = [im.resize((im.width * 600 // 1254, im.height * 600 // 1254), Image.NEAREST) if im.width > 1000 else im for im in ims]
    sheet = Image.new("RGBA", (sum(im.width for im in ims), 620), (60, 80, 90, 255)); x = 0
    for im in ims: sheet.alpha_composite(im, (x, 620 - im.height)); x += im.width
    sheet.save(f"{preview}/deaths_sheet.png")
