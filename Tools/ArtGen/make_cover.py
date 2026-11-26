# 배포용 표지 그림(스팀 라이브러리 표지 같은 세로 그림, 가로 머리 그림, 정사각 아이콘)을 게임 그림으로 짠다.
# 새로 그리지 않는다: 하늘·산·소나무·땅·선비·보스는 모두 Resources/JinaV4의 그림을 그대로 키워 쓴다.
#   python Tools/ArtGen/make_cover.py [출력 폴더]      (기본 Tools/ArtGen/cover)
# 글자는 윈도 바탕체(batang.ttc)로 쓴다. 게임에 들어가지 않으므로 make_all.py에는 넣지 않는다.
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ART = "Assets/Resources/JinaV4/"
OUT = sys.argv[1] if len(sys.argv) > 1 else "Tools/ArtGen/cover"
FONT = "C:/Windows/Fonts/batang.ttc"     # 0: 바탕, 2: 궁서(있으면)
INK = (11, 9, 11)                         # 그림들의 외곽선 색
PAPER = (244, 232, 204)                   # 한지색 글자


def load(name):
    return Image.open(ART + name + ".png").convert("RGBA")


def trim(im):
    return im.crop(im.getbbox())


def dots(im, height):
    """캐릭터 그림(약 12px 도트)을 원하는 키로. 도트가 고르게 보이게 한 번 작게 줄였다가 최근접으로 키운다."""
    im = trim(im)
    w = max(1, round(im.width * height / im.height))
    return im.resize((w, height), Image.LANCZOS)


def tint(im, color, amount):
    """실루엣처럼 한 색 쪽으로 민다(알파는 그대로)."""
    a = np.array(im).astype(float)
    a[..., :3] = a[..., :3] * (1 - amount) + np.array(color) * amount
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def sky(w, h, name="sky_pass"):
    col = load("13_Props/" + name).resize((1, h), Image.BILINEAR)
    return col.resize((w, h), Image.NEAREST)


def strip(img, name, y, height, x0=0):
    """옆으로 이어지는 띠(배경·땅)를 높이에 맞춰 최근접으로 키우고 가로로 이어 붙인다."""
    band = trim(load("13_Props/" + name)) if "ground" not in name else load("13_Props/" + name)
    k = height / band.height
    band = band.resize((max(1, round(band.width * k)), height), Image.NEAREST)
    x = x0 - band.width // 3
    while x < img.width:
        img.alpha_composite(band, (x, y))
        x += band.width


def moon(img, cx, cy, r, color=(250, 228, 180)):
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(glow)
    d.ellipse((cx - r * 2.2, cy - r * 2.2, cx + r * 2.2, cy + r * 2.2), fill=color + (60,))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(r)))
    # 도트 원: 작게 그려 키운다
    s = 6
    small = Image.new("RGBA", (2 * r // s + 2, 2 * r // s + 2), (0, 0, 0, 0))
    ImageDraw.Draw(small).ellipse((0, 0, 2 * r // s, 2 * r // s), fill=color + (255,))
    ImageDraw.Draw(small).ellipse((2 * r // s * 0.55, 2 * r // s * 0.2, 2 * r // s * 0.8, 2 * r // s * 0.45),
                                  fill=(232, 204, 160, 255))
    img.alpha_composite(small.resize((small.width * s, small.height * s), Image.NEAREST), (cx - r, cy - r))


def put(img, im, cx, feet):
    img.alpha_composite(im, (round(cx - im.width / 2), round(feet - im.height)))


def outlined(img, xy, text, font, fill=PAPER, edge=INK, width=6, anchor="mm", shadow=True):
    d = ImageDraw.Draw(img)
    if shadow:
        sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).text((xy[0] + 6, xy[1] + 8), text, font=font, fill=(0, 0, 0, 170), anchor=anchor,
                                stroke_width=width, stroke_fill=(0, 0, 0, 170))
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(4)))
    d.text(xy, text, font=font, fill=fill, anchor=anchor, stroke_width=width, stroke_fill=edge)


def seal(img, x, y, size, text="選"):
    """붉은 낙관(19_UI/seal을 키움) 위에 흰 글자 하나."""
    s = load("19_UI/seal").resize((size, size), Image.NEAREST)
    img.alpha_composite(s, (x, y))
    f = ImageFont.truetype(FONT, int(size * 0.62), index=0)
    ImageDraw.Draw(img).text((x + size / 2, y + size / 2), text, font=f, fill=(250, 236, 214), anchor="mm")


def vignette(img, strength=0.55):
    w, h = img.size
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a = (np.clip(d - 0.6, 0, 1) / 0.8) ** 1.5 * 255 * strength
    v = Image.fromarray(np.dstack([np.zeros((h, w, 3)), a]).astype(np.uint8), "RGBA")
    img.alpha_composite(v)


def scene(w, h, ground_y, hero_h, boss_scale, moon_at, bosses):
    """하늘·달·산·보스 그림자·소나무·땅·선비를 한 장에 쌓는다."""
    img = sky(w, h)
    moon(img, int(moon_at[0] * w), int(moon_at[1] * h), int(h * 0.075))
    # 산 너머로 어른거리는 세 보스(하늘색에 묻힌 그림자, 멀리 있어 산보다 먼저 그린다)
    dusk = (40, 26, 60)
    unit = hero_h / 1.9 * boss_scale
    for name, height, x, amount in bosses:
        b = tint(dots(load(name), int(unit * height)), dusk, amount)
        b.putalpha(b.getchannel("A").point(lambda a: a * 82 // 100))   # 달이 비쳐 보이게 조금 비침
        put(img, b, w * x, ground_y - h * 0.10)
    strip(img, "bg_pass_far", int(ground_y - h * 0.40), int(h * 0.30))
    strip(img, "bg_pass_near", int(ground_y - h * 0.16), int(h * 0.17), x0=w // 5)
    strip(img, "ground_pass", ground_y - int(hero_h * 0.04), int(hero_h * 0.42))
    return img


def hero(img, cx, ground_y, hero_h):
    """칼을 휘두르는 선비(초승달 칼빛까지 그림에 있다). 선 그림의 키를 hero_h로 맞춘다."""
    idle = trim(load("01_Player/player_idle"))
    k = hero_h / idle.height
    slash = trim(load("02_Player_Attack_Sword/sword_attack_02"))
    # 칼 그림의 배율은 450/1.9, 선 그림은 546/1.9 도트 → 같은 도트 크기로 맞춘다
    ks = k * idle.height / 450
    s = slash.resize((round(slash.width * ks), round(slash.height * ks)), Image.LANCZOS)
    put(img, s, cx, ground_y)


# (그림, 게임 속 키 비례, 가로 자리, 하늘색에 묻히는 정도)
CAPSULE_BOSSES = [("25_Final/final_idle_00", 4.2, 0.62, 0.86), ("21_KongBoss/kong_boss_idle_00", 2.6, 0.16, 0.82),
                  ("05_Boss_Brainwashed/boss_brainwashed_idle", 2.5, 0.92, 0.78)]
HEADER_BOSSES = [("21_KongBoss/kong_boss_idle_00", 2.6, 0.50, 0.82), ("25_Final/final_idle_00", 4.2, 0.66, 0.86),
                 ("05_Boss_Brainwashed/boss_brainwashed_idle", 2.5, 0.93, 0.78)]


def capsule(w=600, h=900):
    gy = int(h * 0.86)
    hero_h = int(h * 0.36)
    img = scene(w, h, gy, hero_h, 1.0, (0.80, 0.27), CAPSULE_BOSSES)
    hero(img, w * 0.42, gy, hero_h)
    vignette(img)
    title(img, w / 2, h * 0.115, int(w * 0.165), int(w * 0.040))
    return img


def header(w=920, h=430):
    gy = int(h * 0.92)
    hero_h = int(h * 0.50)
    img = scene(w, h, gy, hero_h, 0.8, (0.08, 0.20), HEADER_BOSSES)
    hero(img, w * 0.73, gy, hero_h)
    vignette(img, 0.45)
    title(img, w * 0.30, h * 0.40, int(h * 0.20), int(h * 0.055))
    return img


def title(img, cx, cy, size, sub):
    face = lambda k: ImageFont.truetype(FONT, int(size * k), index=2)     # 궁서
    outlined(img, (cx, cy - size * 0.58), "지나가던", face(0.60), width=max(3, size // 20))
    outlined(img, (cx, cy + size * 0.42), "선비", face(1.30), width=max(4, size // 12))
    seal(img, int(cx + size * 1.42), int(cy + size * 0.25), int(size * 0.42))
    outlined(img, (cx, cy + size * 1.40), sub_text, ImageFont.truetype(FONT, sub, index=2), fill=(236, 218, 180),
             width=3, shadow=False)


sub_text = "과거 길에 오른 선비의 모험"


def icon(n=512):
    img = sky(n, n)
    moon(img, int(n * 0.70), int(n * 0.28), int(n * 0.13))
    strip(img, "bg_pass_far", int(n * 0.55), int(n * 0.30))
    strip(img, "bg_pass_near", int(n * 0.78), int(n * 0.20))
    p = trim(load("01_Player/player_idle"))
    p = p.crop((0, 0, p.width, int(p.height * 0.55)))   # 갓부터 허리까지
    k = n * 0.92 / p.height
    p = p.resize((round(p.width * k), round(p.height * k)), Image.NEAREST)
    img.alpha_composite(p, (round(n * 0.40 - p.width / 2), n - p.height))
    vignette(img, 0.4)
    return img


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for name, im in (("capsule_600x900", capsule()), ("header_920x430", header()), ("icon_512", icon())):
        im.convert("RGB").save(os.path.join(OUT, name + ".png"))
        print(os.path.join(OUT, name + ".png"))
