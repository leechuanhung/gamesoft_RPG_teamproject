# Helpers for new motions cut from the V6 character art (fake pixel art with ~10-25 px art pixels and a soft halo).
# New frames are never drawn: rows are shifted, bands removed or blocks moved, then the slivers are cleaned away.
import numpy as np
from PIL import Image

def q(v, step):
    """Round to whole art pixels."""
    return int(round(v / step) * step)

def load(path):
    return np.array(Image.open(path).convert("RGBA"))

def save(a, path):
    Image.fromarray(a).save(path)

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

def _box(m, op):
    r = m.copy()
    for dy in range(-2, 3):
        for dx in range(-2, 3): r = op(r, np.roll(np.roll(m, dy, 0), dx, 1))
    return r

def clean(out):
    """Drop the soft halo (it shows as ledges once rows move), then a 5x5 opening removes 1-4 px slivers."""
    out[out[:, :, 3] < 128] = 0
    op = out[:, :, 3] > 0
    out[op & ~_box(_box(op, np.logical_and), np.logical_or)] = 0
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

def preview_sheet(paths, out, height=620):
    """Frames side by side on the village sky colour; very large canvases are shown at half size."""
    ims = [Image.open(p).convert("RGBA") for p in paths]
    ims = [im.resize((im.width * 600 // 1254, im.height * 600 // 1254), Image.NEAREST) if im.width > 1000 else im for im in ims]
    sheet = Image.new("RGBA", (sum(im.width for im in ims), height), (60, 80, 90, 255)); x = 0
    for im in ims: sheet.alpha_composite(im, (x, height - im.height)); x += im.width
    sheet.save(out)

def preview_dir():
    import sys
    return sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
