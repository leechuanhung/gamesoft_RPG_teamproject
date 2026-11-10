# High-fidelity pixel painting for cutscene illustrations (not for the in-game sprites, which use pixlib).
# Every material is a ramp of hand-picked, hue-shifted colours. A scene paints with a brightness value per pixel
# (shape shading x the scene's light), which picks a ramp colour through an ordered dither; a final pass limits the
# whole picture to one adaptive palette so it reads as pixel art rather than a filtered render.
import numpy as np
from PIL import Image, ImageDraw

_b = np.array([[0]])
for _ in range(3):
    _b = np.block([[4 * _b, 4 * _b + 2], [4 * _b + 3, 4 * _b + 1]])
BAYER = (_b + .5) / 64.0


def noise(w, h, sx, sy, seed, octaves=1):
    """Smooth value noise in 0..1; (sx, sy) is the size of the largest blobs in pixels."""
    rng = np.random.default_rng(seed)
    total = np.zeros((h, w), np.float32); weight = 0; amp = 1.0
    for o in range(octaves):
        gw = max(2, int(w / max(1, sx / 2 ** o)) + 2); gh = max(2, int(h / max(1, sy / 2 ** o)) + 2)
        grid = (rng.random((gh, gw)) * 255).astype(np.uint8)
        total += np.asarray(Image.fromarray(grid).resize((w, h), Image.BICUBIC), np.float32) / 255 * amp
        weight += amp; amp *= .5
    return np.clip(total / weight, 0, 1)


def curve(p0, p1, p2, n=24):
    """Points along a quadratic curve."""
    t = np.linspace(0, 1, n)[:, None]
    a, b, c = np.array(p0, float), np.array(p1, float), np.array(p2, float)
    pts = (1 - t) ** 2 * a + 2 * (1 - t) * t * b + t ** 2 * c
    return [tuple(p) for p in pts]


class Art:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.rgb = np.zeros((h, w, 3), np.float32); s.a = np.zeros((h, w), bool)
        s.Y, s.X = np.mgrid[:h, :w]
        s.bayer = BAYER[s.Y % 8, s.X % 8] - .5
        s.light = np.ones((h, w), np.float32)

    # masks
    def blank(s): return np.zeros((s.h, s.w), bool)
    def rect(s, x0, y0, x1, y1):
        m = s.blank(); m[max(0, int(y0)):max(0, int(y1) + 1), max(0, int(x0)):max(0, int(x1) + 1)] = True; return m
    def poly(s, pts):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).polygon([tuple(p) for p in pts], fill=1); return np.array(im, bool)
    def ellipse(s, x0, y0, x1, y1):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).ellipse((x0, y0, x1, y1), fill=1); return np.array(im, bool)
    def circle(s, cx, cy, r): return s.ellipse(cx - r, cy - r, cx + r, cy + r)
    def line(s, pts, width=1):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).line([tuple(p) for p in pts], fill=1, width=width); return np.array(im, bool)
    def ring(s, m):
        r = s.blank(); r[1:] |= m[:-1]; r[:-1] |= m[1:]; r[:, 1:] |= m[:, :-1]; r[:, :-1] |= m[:, 1:]
        return r & ~m
    def edge_top(s, m, n=1):
        """The top n pixels of a shape (for rim light)."""
        below = s.blank(); below[n:] = m[:-n]
        return m & ~below
    def edge_side(s, m, dx=1, n=1):
        """The n pixels of a shape on one side: dx=1 right, dx=-1 left."""
        shifted = s.blank()
        if dx > 0: shifted[:, :-n] = m[:, n:]
        else: shifted[:, n:] = m[:, :-n]
        return m & ~shifted

    # value fields
    def grad(s, x0, y0, x1, y1):
        """0 at (x0, y0), 1 at (x1, y1)."""
        dx, dy = x1 - x0, y1 - y0; d = dx * dx + dy * dy or 1
        return np.clip(((s.X - x0) * dx + (s.Y - y0) * dy) / d, 0, 1)
    def radial(s, cx, cy, r, sy=1.0):
        """1 at the centre falling to 0 at radius r."""
        return np.clip(1 - np.hypot(s.X - cx, (s.Y - cy) / sy) / r, 0, 1)

    # paint
    def paint(s, m, ramp, v=.5, d=.55, lit=True):
        ramp = np.asarray(ramp, np.float32); n = len(ramp)
        val = np.asarray(v, np.float32) * (s.light if lit else 1)
        x = np.clip(val, 0, 1) * (n - 1) + s.bayer * d
        i = np.clip(np.floor(x + .5), 0, n - 1).astype(int)
        s.rgb[m] = ramp[i][m]; s.a |= m
    def flat(s, m, color):
        s.rgb[m] = color; s.a |= m
    def glow(s, field, color, strength=1.0):
        """Adds light over what is already painted (blended in the float image, re-quantised at the end)."""
        f = (np.asarray(field, np.float32) * strength)[..., None]
        s.rgb += f * np.asarray(color, np.float32) * s.a[..., None]
    def tint(s, field, color):
        """Blends toward a colour by field (0..1)."""
        f = np.clip(np.asarray(field, np.float32), 0, 1)[..., None]
        s.rgb = s.rgb * (1 - f) + np.asarray(color, np.float32) * f
    def erase(s, m):
        s.a &= ~m

    # finish
    def image(s, palette=None, colors=96, spread=7.0):
        """RGBA image limited to an adaptive palette, with an ordered dither of +-spread/2 levels."""
        rgb = np.clip(s.rgb, 0, 255)
        if colors:
            opaque = rgb[s.a].astype(np.uint8)
            if palette is None:
                sample = Image.fromarray(opaque.reshape(-1, 1, 3))
                palette = np.array(sample.quantize(colors=colors, method=Image.MEDIANCUT).getpalette()[:colors * 3], np.float32).reshape(-1, 3)
            dithered = rgb + (s.bayer * spread)[..., None]
            idx = np.zeros((s.h, s.w), int)
            for y0 in range(0, s.h, 64):
                part = dithered[y0:y0 + 64]
                idx[y0:y0 + 64] = ((part[:, :, None, :] - palette[None, None]) ** 2).sum(-1).argmin(-1)
            rgb = palette[idx]
        out = np.zeros((s.h, s.w, 4), np.uint8)
        out[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8); out[..., 3] = np.where(s.a, 255, 0)
        return Image.fromarray(out)
    def palette_of(s, colors=96):
        opaque = np.clip(s.rgb, 0, 255)[s.a].astype(np.uint8)
        sample = Image.fromarray(opaque.reshape(-1, 1, 3))
        return np.array(sample.quantize(colors=colors, method=Image.MEDIANCUT).getpalette()[:colors * 3], np.float32).reshape(-1, 3)
    def save(s, path, palette=None, colors=96, spread=7.0):
        s.image(palette, colors, spread).save(path)


class Theme:
    """Colour grading for a time of day: turns a daylight ramp into the ramp as this light shows it."""
    GRADES = {
        "day": ((1, 1, 1), (0, 0, 0), 0),
        "night": ((.42, .5, .82), (7, 9, 22), .5),
        "morning": ((1.04, .96, .84), (6, 2, 0), 0),
        "evening": ((1.02, .70, .56), (10, 2, 10), .05),
    }
    def __init__(s, kind):
        s.kind = kind; s.mul, s.add, s.desat = s.GRADES[kind]
    def __call__(s, ramp):
        r = np.asarray(ramp, np.float32)
        grey = r @ np.array([.3, .59, .11], np.float32)
        r = r * (1 - s.desat) + grey[:, None] * s.desat
        return r * np.array(s.mul, np.float32) + np.array(s.add, np.float32)


# Materials, dark to light, shadows leaning violet and lights leaning warm.
WOOD = [(30, 18, 26), (46, 28, 32), (68, 42, 40), (92, 60, 46), (118, 78, 54), (146, 100, 66), (176, 128, 86), (204, 160, 112)]
WALL = [(84, 70, 78), (112, 94, 92), (140, 120, 106), (168, 146, 122), (192, 172, 140), (214, 196, 160), (232, 216, 182), (244, 232, 204)]
FLOOR = [(70, 44, 34), (98, 62, 36), (130, 84, 40), (164, 112, 48), (194, 142, 62), (218, 170, 84), (236, 196, 116), (250, 222, 158)]
PAPER = [(116, 108, 110), (150, 142, 136), (182, 174, 158), (208, 200, 180), (228, 222, 202), (242, 238, 222), (252, 250, 238)]
COTTON = [(82, 76, 98), (116, 110, 128), (150, 146, 158), (186, 182, 186), (216, 212, 206), (236, 232, 220), (250, 248, 236)]
SKIN = [(82, 46, 52), (122, 72, 66), (168, 106, 84), (210, 146, 112), (238, 184, 146), (250, 212, 178), (255, 232, 208)]
HAIR = [(8, 6, 12), (16, 12, 20), (28, 22, 32), (44, 36, 50), (68, 58, 76), (100, 90, 110)]
ONGGI = [(24, 14, 16), (44, 24, 22), (70, 40, 30), (98, 58, 38), (128, 80, 50), (164, 112, 74), (206, 158, 118), (236, 206, 174)]
STONE = [(34, 32, 44), (54, 52, 62), (78, 74, 82), (104, 100, 102), (132, 128, 122), (160, 156, 142), (190, 186, 166)]
TILE = [(16, 18, 28), (28, 32, 44), (44, 50, 62), (62, 70, 80), (84, 92, 100), (110, 118, 122), (140, 146, 146)]
EARTH = [(40, 28, 30), (64, 44, 38), (92, 64, 46), (122, 88, 58), (152, 114, 74), (182, 144, 98), (208, 174, 128)]
LEAF = [(14, 26, 28), (22, 42, 36), (34, 60, 42), (50, 82, 48), (74, 106, 54), (104, 132, 62), (140, 160, 76)]
BARK = [(20, 14, 22), (34, 24, 30), (50, 36, 38), (70, 50, 46), (92, 68, 56), (118, 90, 70)]
THATCH = [(46, 34, 30), (72, 54, 38), (100, 76, 46), (130, 102, 58), (160, 130, 74), (190, 160, 96), (216, 190, 124)]
PLASTER = [(90, 80, 86), (124, 112, 112), (158, 146, 136), (190, 178, 160), (216, 206, 184), (236, 228, 208)]
INDIGO = [(14, 16, 40), (24, 28, 62), (36, 44, 88), (54, 66, 116), (78, 94, 146), (110, 128, 176)]
JADE = [(22, 48, 54), (36, 72, 72), (56, 100, 92), (82, 132, 116), (116, 166, 144), (154, 198, 174), (192, 224, 204)]
ROSE = [(50, 18, 36), (82, 28, 44), (120, 40, 52), (158, 56, 60), (194, 82, 76), (224, 120, 104), (244, 164, 140)]
OCHRE = [(58, 34, 22), (92, 56, 26), (130, 86, 32), (170, 120, 42), (206, 158, 60), (232, 194, 92), (248, 224, 136)]
BLOOM = [(90, 34, 58), (136, 52, 78), (182, 80, 104), (220, 116, 136), (240, 156, 170), (250, 196, 204)]
PERSIMMON = [(70, 24, 22), (120, 42, 24), (172, 68, 26), (216, 104, 34), (240, 146, 54), (252, 190, 92)]
BRASS = [(40, 26, 18), (74, 50, 24), (116, 84, 34), (160, 124, 50), (204, 170, 76), (240, 214, 118)]
INK = [(40, 38, 46), (70, 68, 74), (102, 100, 104), (136, 134, 132), (170, 168, 160), (204, 200, 186)]
