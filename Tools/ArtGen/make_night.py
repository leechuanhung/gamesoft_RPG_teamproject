# The night at Kongjwi's house, cut from the pictures the user made with GPT (Tools/ArtGen/source/night/, see
# source/night/요청문.md for what each one is). The pictures came as screenshots: each is a wide band in the middle of a
# 16:9 frame, between blurred copies of itself. The band is cut out, every shot is fitted to one cinema frame of
# 2.4 : 1 (1280 x 533; the game puts black bars above and below), and the edited versions of a shot are lined up on
# their base so that only the part that changed is laid over it.
#   18_Night/ext_evening, ext_evening_flinch   the house at sunset, three on the road; Patjwi shrinking back (A1, A2)
#   18_Night/ext_night, ext_night_dark         the house under the moon, one door lit; that door gone dark (B1, B2)
#   18_Night/room_night                        the guest room, him asleep (C1)
#   18_Night/room_night_front                  the same less its paper panes, laid over the shadow on the door
#   18_Night/room_night_floor                  black where the moon falls on the floor, faded in while the shape blocks it
#   18_Night/room_night_sit                    him sitting up, staring at the door (C2)
#   18_Night/shadow_0..2                       the shape on the paper: arms down, a hand to its mouth, chewing (S, white)
#   18_Night/close_paper, close_front          the close shot of the door, and its lattice alone (D1)
#   18_Night/close_hole_0..2                   a wet spot, a tear, a hole (D2, D3, D4)
#   18_Night/close_eye_0..2                    the eye in the hole: open, half, shut (D5, D6)
#   18_Night/close_shadow                      the shape's head, very near (S, white)
#   18_Night/ext_morning, ext_rooster          the house at dawn, the rooster on the wall; the rooster crowing (E1, E2)
#   18_Night/room_morning                      the room in the morning, the hole and its sunbeam (F1)
#   18_Night/stare                             Kongjwi's stare (source/kongjwi_stare_cut.webp), 1280 x 720
# Usage: python make_night.py Assets/Resources/JinaV4/18_Night
import sys, os
import numpy as np
from PIL import Image, ImageFilter

OUT = sys.argv[1]
SOURCE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source")
NIGHT = os.path.join(SOURCE, "night")
W, H = 1280, 533
# Rows of each picture that are the picture itself.
BAND = {"A1": (146, 573), "A2": (152, 568), "B1": (166, 559), "B2": (172, 554), "C1": (49, 674), "C2": (44, 678),
        "D1": (99, 628), "D2": (98, 629), "D3": (105, 622), "D4": (136, 813), "D5": (136, 813), "D6": (136, 813),
        "E1": (0, 941), "E2": (0, 941), "F1": (56, 941)}
# Each edit lined up on its base: scale, then shift down and right (in band pixels, 1280 wide). Found by matching the
# pictures (phase correlation over a sweep of scales); fixed here so the output never drifts.
FIT = {"A2": (1.024, 0, -14), "B2": (1.026, 1, -15), "C2": (.978, 3, -2), "D2": (1, 0, -16), "D3": (1.02, 0, 0),
       "D4": (1.02, 0, -1), "D5": (1.02, 0, 0), "D6": (1.02, 0, 0), "E2": (1, 0, 0)}
# The part of each base band that becomes the screen (left, top, right, bottom), 2.4 : 1.
FRAME = {"A": (175, 0, 1200, 427), "B": (200, 0, 1143, 393), "C": (0, 30, 1280, 563), "D": (5, 0, 1275, 529),
         "E": (0, 80, 1280, 613), "F": (0, 40, 1280, 573)}


def band(name):
    f = [x for x in os.listdir(NIGHT) if x.split(".")[0] == name][0]
    im = Image.open(os.path.join(NIGHT, f)).convert("RGB")
    y0, y1 = BAND[name]
    im = im.crop((0, y0, im.width, y1))
    return im.resize((1280, round(im.height * 1280 / im.width)), Image.LANCZOS) if im.width != 1280 else im


def lined_up(name, base):
    s, dy, dx = FIT[name]
    return band(name).transform(base.size, Image.AFFINE, (1 / s, 0, -dx / s, 0, 1 / s, -dy / s), Image.BICUBIC)


def framed(im, scene):
    """A band (or a mask on it) cut to the scene's frame and brought to the screen's size."""
    return im.crop(FRAME[scene]).resize((W, H), Image.LANCZOS)


def lum(im):
    return np.asarray(im.convert("L")).astype(float)


def soft(mask, grow, blur):
    """A 0..1 mask widened by `grow` pixels and softened."""
    m = Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8))
    if grow: m = m.filter(ImageFilter.MaxFilter(grow * 2 + 1))
    if blur: m = m.filter(ImageFilter.GaussianBlur(blur))
    return np.asarray(m).astype(float) / 255


def changed(base, edit, box, grow=10, blur=6):
    """Where the edit differs from its base, inside box (left, top, right, bottom)."""
    d = np.abs(np.asarray(edit).astype(float) - np.asarray(base).astype(float)).sum(-1)
    m = (d > 70).astype(float)
    keep = np.zeros_like(m); x0, y0, x1, y1 = box; keep[y0:y1, x0:x1] = 1
    m = soft(m * keep, grow, blur)
    return m * soft(keep, 0, 8)


def layer(im, alpha):
    rgba = np.dstack([np.asarray(im.convert("RGB")), (np.clip(alpha, 0, 1) * 255).round().astype(np.uint8)])
    return Image.fromarray(rgba.astype(np.uint8), "RGBA")


def save(im, name, scene=None, alpha=None):
    """Save a band picture framed for its scene, with an optional mask (in band pixels) as its alpha."""
    if scene:
        if alpha is not None: im = layer(im, alpha)
        im = framed(im.convert("RGBA"), scene)
    im.save(os.path.join(OUT, name + ".png"))


def edit(scene, base, name, out, box, **kw):
    e = lined_up(name, base)
    save(e, out, scene, changed(base, e, box, **kw))
    return e


def silhouettes():
    """The four shapes, black on white in S: alpha from how dark they are, coloured white for the game to tint."""
    s = Image.open(os.path.join(NIGHT, [x for x in os.listdir(NIGHT) if x.split(".")[0] == "S"][0])).convert("L")
    boxes = [(64, 220, 352, 520), (356, 220, 626, 520), (630, 220, 904, 520), (904, 220, 1280, 530)]
    out = []
    for box in boxes:
        a = np.clip((205 - np.asarray(s.crop(box)).astype(float)) / 150, 0, 1)
        out.append(Image.fromarray(np.dstack([np.full(a.shape + (3,), 255, np.uint8), (a * 255).round().astype(np.uint8)]), "RGBA"))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    # Evening: Patjwi shrinking back is all that changes.
    a1 = band("A1"); save(a1, "ext_evening", "A")
    edit("A", a1, "A2", "ext_evening_flinch", (880, 0, 1150, 427), grow=14, blur=8)
    # Night outside: the lit door, and the glow it throws, going dark.
    b1 = band("B1"); save(b1, "ext_night", "B")
    edit("B", b1, "B2", "ext_night_dark", (500, 120, 860, 393), grow=12, blur=8)
    # The guest room. The paper of the door (right) is where the shape's shadow falls, under the lattice.
    c1 = band("C1"); save(c1, "room_night", "C")
    L = lum(c1)
    door = np.zeros_like(L); door[0:440, 840:1280] = 1
    paper = soft(np.clip((L - 70) / 40, 0, 1) * door, 0, 1)
    save(c1, "room_night_front", "C", 1 - paper)
    floor = np.zeros_like(L); floor[430:625, 640:1280] = 1
    lit = soft(np.clip((L - 85) / 45, 0, 1) * floor, 1, 2)
    save(Image.new("RGB", c1.size, (4, 5, 14)), "room_night_floor", "C", lit)
    edit("C", c1, "C2", "room_night_sit", (0, 150, 515, 625), grow=16, blur=10)
    shapes = silhouettes()
    for i in range(3): shapes[i].save(os.path.join(OUT, f"shadow_{i}.png"))
    # The head is shown four times its size: drawn twice as fine, its edge smoothed, so it does not look blocky.
    head = shapes[3]
    a = head.getchannel("A").resize((head.width * 2, head.height * 2), Image.LANCZOS).filter(ImageFilter.GaussianBlur(2))
    big = Image.new("RGBA", a.size, (255, 255, 255, 0)); big.putalpha(a)
    big.save(os.path.join(OUT, "close_shadow.png"))
    # The close shot: the paper, the lattice alone, the hole as it opens, the eye.
    d1 = band("D1"); save(d1, "close_paper", "D")
    save(d1, "close_front", "D", 1 - soft(np.clip((lum(d1) - 62) / 34, 0, 1), 0, 1))
    hole_box = (360, 0, 860, 529)
    for i, n in enumerate(("D2", "D3", "D4")):
        d4 = edit("D", d1, n, f"close_hole_{i}", hole_box, grow=8, blur=4)
    # The eye fills only the dark of the hole (and its torn rim, where the lids show).
    inside = np.zeros_like(lum(d4)); inside[60:470, 420:800] = 1
    hole = soft((lum(d4) < 40) * inside, 6, 3)
    d5, d6 = lined_up("D5", d1), lined_up("D6", d1)
    save(d5, "close_eye_0", "D", hole)
    save(Image.blend(d5, d6, .5), "close_eye_1", "D", hole)
    save(d6, "close_eye_2", "D", hole)
    # Dawn: the rooster crowing, and the light he crows at.
    e1 = band("E1"); save(e1, "ext_morning", "E")
    edit("E", e1, "E2", "ext_rooster", (1000, 240, 1280, 460), grow=10, blur=6)
    save(band("F1"), "room_morning", "F")
    # Kongjwi's stare is the user's own picture, fitted to the whole screen.
    src = Image.open(os.path.join(SOURCE, "kongjwi_stare_cut.webp")).convert("RGB")
    src.resize((1280, 720), Image.LANCZOS).save(os.path.join(OUT, "stare.png"))


if __name__ == "__main__":
    main()
