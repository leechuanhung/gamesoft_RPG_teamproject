# Draws what the world stands on and under: the ground of each area (a strip that tiles sideways, grass or stones on its
# top edge, layers of earth below reaching past the bottom of the screen) and each area's sky (banded gradient, one
# column wide, stretched across the map). One texel per art pixel, flat fills and one shade, like the props.
#   13_Props/ground_<area>   320 x 150 dots; the walking surface is GroundLip dots from the top
#   13_Props/sky_<area>      1 x 330 dots
#   13_Props/river_deep      the river below its surface strip
#   13_Props/cloud_0..2      flat clouds that drift behind the mountains
# Usage: python make_ground.py Assets/Resources/JinaV4/13_Props
import sys, os, math; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
OUT = sys.argv[1]
W, H, LIP = 320, 150, 6

def save(rgb, a, name):
    img = np.zeros(rgb.shape[:2] + (4,), np.uint8); img[..., :3] = np.clip(rgb, 0, 255); img[..., 3] = np.where(a, 255, 0)
    Image.fromarray(img).save(f"{OUT}/{name}.png")

def wave(x, rng, parts):
    """A curve across the width that wraps around, so the strip tiles."""
    y = np.zeros_like(x, float)
    for amp, cycles in parts:
        y += amp * np.sin(2 * math.pi * x * cycles / W + rng.random() * 6.28)
    return y

STYLES = {
    # top: the surface band and its shade; layers: earth going down; stone: embedded stones (fill, lit top, shadow); tuft: what grows on top
    "stele":   dict(top=[(92, 92, 112), (70, 70, 92)], layers=[(58, 58, 80), (46, 46, 66), (34, 34, 50), (24, 24, 38)], stone=[(82, 82, 104), (112, 112, 132), (40, 40, 58)], tuft=[(74, 96, 92), (56, 76, 80)], tuft_rate=.16, tall=.04),
    "camp":    dict(top=[(120, 78, 62), (96, 60, 52)], layers=[(84, 52, 48), (66, 40, 40), (48, 30, 34), (32, 20, 26)], stone=[(112, 84, 74), (142, 112, 98), (62, 40, 40)], tuft=[(118, 92, 56), (92, 70, 46)], tuft_rate=.1, tall=.05),
    "village": dict(top=[(146, 116, 78), (120, 92, 62)], layers=[(104, 78, 54), (84, 62, 46), (62, 46, 38), (42, 32, 30)], stone=[(150, 138, 122), (182, 172, 154), (88, 76, 66)], tuft=[(104, 136, 66), (78, 110, 58)], tuft_rate=.3, tall=.05),
    "forest":  dict(top=[(62, 100, 62), (46, 80, 54)], layers=[(58, 62, 44), (46, 48, 38), (34, 36, 32), (22, 26, 26)], stone=[(84, 92, 84), (112, 122, 106), (44, 52, 48)], tuft=[(72, 124, 68), (50, 98, 60)], tuft_rate=.55, tall=.1),
    "ferry":   dict(top=[(176, 158, 118), (150, 132, 98)], layers=[(124, 108, 84), (100, 86, 70), (74, 64, 58), (50, 44, 44)], stone=[(152, 150, 140), (186, 184, 172), (92, 90, 84)], tuft=[(120, 142, 92), (92, 118, 82)], tuft_rate=.14, tall=.06),
    "kong":    dict(top=[(158, 124, 76), (130, 100, 62)], layers=[(112, 84, 54), (90, 66, 46), (66, 50, 38), (44, 34, 30)], stone=[(156, 142, 118), (188, 176, 150), (92, 78, 62)], tuft=[(122, 148, 62), (94, 122, 54)], tuft_rate=.34, tall=.07),
    "pass":    dict(top=[(120, 112, 112), (96, 90, 94)], layers=[(84, 78, 84), (68, 62, 72), (50, 46, 58), (34, 32, 44)], stone=[(126, 120, 122), (158, 152, 148), (62, 58, 66)], tuft=[(96, 110, 78), (74, 90, 70)], tuft_rate=.12, tall=.04),
}

def ground(name, style):
    rng = np.random.default_rng(abs(hash_name(name)))
    rgb = np.zeros((H, W, 3), int); a = np.zeros((H, W), bool)
    x = np.arange(W)
    a[LIP:] = True
    # Earth layers with gently waving boundaries, deepest last.
    bounds = [LIP + 16, LIP + 42, LIP + 76]
    edges = [b + np.round(wave(x, rng, [(3, 2), (1.5, 5)])).astype(int) for b in bounds]
    for col in range(W):
        y0 = LIP
        for i, layer in enumerate(style["layers"]):
            y1 = edges[i][col] if i < len(edges) else H
            rgb[y0:y1, col] = layer
            # A dotted seam where one layer gives way to the next.
            if i < len(edges) and (col + i * 3) % 3 != 0: rgb[max(y0, y1 - 1):y1, col] = style["layers"][min(i + 1, len(style["layers"]) - 1)]
            y0 = y1
    # Stones and flecks of grit, fewer and darker going down; they wrap around the edges.
    def spot(cx, cy, w, h, fill, lit, dark):
        for yy in range(cy, cy + h):
            for xx in range(cx, cx + w):
                corner = (xx in (cx, cx + w - 1)) and (yy in (cy, cy + h - 1))
                if corner or yy >= H or yy < LIP: continue
                c = fill
                if yy == cy: c = lit
                elif yy == cy + h - 1 or xx == cx + w - 1: c = dark
                rgb[yy, xx % W] = c
    for _ in range(56):
        cy = int(LIP + 10 + rng.random() ** 1.5 * 120); depth = (cy - LIP) / (H - LIP)
        fade = 1 - depth * .72
        f, l, d = [tuple(int(v * fade) for v in c) for c in style["stone"]]
        spot(int(rng.integers(0, W)), cy, int(rng.integers(3, 10)), int(rng.integers(2, 5)), f, l, d)
    for _ in range(280):
        px, py = int(rng.integers(0, W)), int(rng.integers(LIP + 3, H))
        base = rgb[py, px]
        rgb[py, px] = base + (10, 9, 7) if rng.random() < .55 else base - (10, 9, 8)
    # The top band: the walked-on surface, a lit first row, a rough lower edge, a shade under it.
    band = LIP + 6 + np.round(wave(x, rng, [(1.6, 3), (1, 7), (.7, 13)])).astype(int)
    top, shade = style["top"]
    for col in range(W):
        rgb[LIP:band[col], col] = top
        rgb[band[col]:band[col] + 2, col] = shade
        rgb[LIP, col] = tuple(min(255, int(v * 1.12) + 6) for v in top)
    for _ in range(44):
        px = int(rng.integers(0, W)); py = int(rng.integers(LIP + 1, LIP + 5)); w = int(rng.integers(1, 4))
        for k in range(w): rgb[py, (px + k) % W] = shade if rng.random() < .6 else tuple(min(255, int(v * 1.08) + 4) for v in top)
    # What grows along the top edge: blades 2-5 dots high, in clumps, a few taller.
    tuft, tuft2 = style["tuft"]
    for col in range(W):
        if rng.random() > style["tuft_rate"]: continue
        h = int(rng.integers(1, 4)) + (int(rng.integers(1, 3)) if rng.random() < style["tall"] * 3 else 0)
        for y in range(LIP - h, LIP + 1):
            rgb[y, col] = tuft if (y - LIP + h) < h // 2 + 1 else tuft2
            a[y, col] = True
    save(rgb, a, "ground_" + name)

def hash_name(s):
    v = 7
    for ch in s: v = v * 31 + ord(ch)
    return v % 100000

SKIES = {
    # top to horizon; the horizon sits about two thirds of the way down
    "stele":   [(10, 12, 32), (16, 18, 44), (24, 26, 58), (34, 36, 72), (48, 48, 88), (66, 64, 104), (88, 84, 120)],
    "camp":    [(34, 14, 28), (56, 20, 32), (86, 28, 36), (124, 42, 40), (162, 66, 46), (196, 100, 58), (222, 142, 78)],
    "village": [(82, 138, 188), (102, 156, 198), (126, 174, 206), (152, 190, 212), (178, 204, 214), (204, 216, 210), (228, 226, 202)],
    "forest":  [(8, 22, 26), (12, 32, 34), (18, 44, 42), (26, 58, 50), (36, 74, 58), (48, 92, 68), (64, 112, 80)],
    "ferry":   [(92, 116, 142), (110, 132, 154), (130, 150, 166), (150, 168, 178), (170, 184, 188), (190, 200, 198), (208, 214, 206)],
    "kong":    [(112, 158, 196), (140, 176, 200), (172, 192, 196), (202, 206, 184), (230, 216, 164), (246, 218, 142), (252, 208, 122)],
    "pass":    [(38, 30, 70), (60, 40, 88), (92, 54, 102), (134, 72, 106), (178, 96, 104), (220, 128, 98), (246, 170, 108)],
}

def sky(name, ramp):
    """Bands of colour with a dithered step between each, on two-dot rows like everything else."""
    n = 330; rgb = np.zeros((n, 1, 3), int); a = np.ones((n, 1), bool)
    # From the top of the view (well above the screen on a wide window) to the horizon, in steps of a few dots.
    horizon, steps = 250, 26
    r = np.asarray(ramp, float)
    for y in range(n):
        t = np.floor(min(1, y / horizon) * steps) / steps * (len(ramp) - 1)
        i = min(int(t), len(ramp) - 2); f = t - i
        rgb[y, 0] = np.round(r[i] * (1 - f) + r[i + 1] * f)
    save(rgb, a, "sky_" + name)

def river():
    w, h = 96, 90
    rng = np.random.default_rng(77)
    ramp = [(58, 96, 118), (48, 82, 106), (40, 68, 94), (32, 54, 80), (26, 42, 66), (20, 32, 52)]
    rgb = np.zeros((h, w, 3), int); a = np.ones((h, w), bool)
    for y in range(h): rgb[y, :] = ramp[min(len(ramp) - 1, y // 14)]
    for _ in range(60):
        y = int(rng.random() ** 1.4 * (h - 1)); x = int(rng.integers(0, w)); l = int(rng.integers(3, 12))
        shade = ramp[max(0, min(len(ramp) - 1, y // 14 - 1))]
        for k in range(l): rgb[y, (x + k) % w] = shade
    save(rgb, a, "river_deep")

def cloud(i):
    rng = np.random.default_rng(90 + i)
    w, h = [(92, 22), (64, 16), (120, 26)][i]
    img = np.zeros((h, w), bool); shade = np.zeros((h, w), bool)
    for _ in range(int(w / 8)):
        cx = int(rng.integers(8, w - 8)); r = int(rng.integers(3, h // 2))
        cy = h - 2 - r // 2 - int(rng.integers(0, 3))
        for y in range(h):
            for x in range(w):
                if (x - cx) ** 2 / (r * 1.7) ** 2 + (y - cy) ** 2 / r ** 2 <= 1: img[y, x] = True
    img[h - 2:, :] = False
    for x in range(w):
        col = np.where(img[:, x])[0]
        if len(col): shade[col[-1] - min(2, len(col) - 1):col[-1] + 1, x] = True
    rgb = np.zeros((h, w, 3), int); rgb[img] = (255, 255, 255); rgb[shade] = (214, 214, 222)
    save(rgb, img, f"cloud_{i}")

for name, style in STYLES.items(): ground(name, style)
for name, ramp in SKIES.items(): sky(name, ramp)
river()
for i in range(3): cloud(i)
