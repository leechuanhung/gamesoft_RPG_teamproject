# 도깨비 방망이 두 자루: 정화한 도깨비가 남기는 방망이(dokkaebi_club)와 토벌한 도깨비의 피 묻은 방망이(dokkaebi_club_blood).
# 도깨비(05_Boss_Brainwashed)가 쥔 쇠징 박힌 몽둥이를 본떠, 명검과 같은 528px 캔버스(12px 도트 44칸)에 45°로 눕혀 그린다.
# 손잡이는 왼쪽 아래, 머리는 오른쪽 위. 쥐는 점은 Weapon.cs의 grip(왼쪽 아래 기준 픽셀)과 맞춘다.
#   python Tools/ArtGen/make_clubs.py Assets/Resources/JinaV4/04_Weapons
import math, sys
from pathlib import Path
import numpy as np
from PIL import Image
from pixlib import Canvas, K

N, B = 44, 12                         # 칸 수, 칸 하나의 픽셀
WOOD, WOOD_DARK, WOOD_LIGHT = (107, 63, 45), (76, 43, 31), (146, 92, 60)   # 도깨비 몽둥이의 나무색
IRON, IRON_LIGHT = (44, 42, 54), (104, 102, 120)                          # 쇠징
WRAP, WRAP_DARK = (232, 222, 196), (186, 170, 142)                         # 손잡이 감은 천(도깨비의 붕대색)
BLOOD, BLOOD_DARK = (168, 22, 30), (104, 12, 20)

# 몽둥이의 축: 손잡이 끝(아래 왼쪽)에서 머리 끝(위 오른쪽)으로
START, END = (5.0, 39.0), (39.0, 5.0)
LENGTH = math.dist(START, END)
AXIS = ((END[0] - START[0]) / LENGTH, (END[1] - START[1]) / LENGTH)
SIDE = (-AXIS[1], AXIS[0])            # 축에 수직(그림에서 오른쪽 아래가 +)

def along(x, y):
    """칸 가운데의 (축 방향 0~1, 수직 칸 거리)"""
    dx, dy = x + .5 - START[0], y + .5 - START[1]
    return (dx * AXIS[0] + dy * AXIS[1]) / LENGTH, dx * SIDE[0] + dy * SIDE[1]

def half_width(u):
    """손잡이는 가늘고, 머리 쪽으로 굵어지다 끝에서 둥글게 닫힌다."""
    if u < .36: return 1.55
    if u < .86: return 1.55 + (u - .36) / .5 * 2.75
    return 4.3 * math.sqrt(max(0, 1 - ((u - .86) / .15) ** 2))

def at(u, v):
    """축 위치 u, 수직 v인 칸"""
    x = START[0] + AXIS[0] * u * LENGTH + SIDE[0] * v
    y = START[1] + AXIS[1] * u * LENGTH + SIDE[1] * v
    return int(math.floor(x)), int(math.floor(y))

def club(bloody):
    c = Canvas(N, N)
    body = c.blank()
    for y in range(N):
        for x in range(N):
            u, v = along(x, y)
            if 0 <= u <= 1.01 and abs(v) <= half_width(u): body[y, x] = True
    c.layer(body, WOOD)
    # 한 단계 음영: 오른쪽 아래 면은 어둡게, 왼쪽 위 가장자리는 밝게, 머리에 나뭇결 몇 줄
    for y in range(N):
        for x in range(N):
            if not body[y, x]: continue
            u, v = along(x, y)
            w = half_width(u)
            if v > w * .35: c.c[y, x] = WOOD_DARK
            elif v < -w + 1.1 and u > .3: c.c[y, x] = WOOD_LIGHT
    for u, v in [(.5, -.4), (.55, .2), (.62, -1.2), (.7, .4), (.74, -.6), (.82, -1.8), (.88, .2)]:
        x, y = at(u, v)
        if body[y, x]: c.c[y, x] = WOOD_DARK
    # 손잡이 끝의 둥근 마디와 감은 천
    knob = c.blank()
    for y in range(N):
        for x in range(N):
            u, v = along(x, y)
            if -.035 <= u <= .035 and abs(v) <= 2.1: knob[y, x] = True
    c.layer(knob, WOOD_DARK)
    for y in range(N):
        for x in range(N):
            if not body[y, x]: continue
            u, v = along(x, y)
            if .07 <= u <= .32:
                seam = int((u - .07) * LENGTH) % 3 == 2
                c.c[y, x] = WRAP_DARK if seam or v > .5 else WRAP
    # 쇠징: 머리 가장자리를 따라 튀어나오고, 앞면에도 몇 개
    studs = [(.48, -1), (.48, 1), (.6, -1), (.6, 1), (.72, -1), (.72, 1), (.84, -1), (.84, 1), (.96, 0),
             (.55, 0), (.78, 0)]
    for u, side in studs:
        v = side * (half_width(u) + .2) if side else (-.6 if u < .7 else .4)
        x, y = at(u, v)
        m = c.rect(x, y, x + 1, y + 1)
        c.layer(m, IRON)
        c.c[y, x] = IRON_LIGHT
    if bloody:
        # 피: 머리에 튄 얼룩과 쇠징에 엉긴 피, 축을 따라 손잡이 쪽으로 흘러내린 자국
        #   때린 쪽(머리 끝)일수록 짙게 덮이고, 손잡이 쪽으로는 몇 점만 튄다.
        for y in range(N):
            for x in range(N):
                if not body[y, x] or tuple(c.c[y, x]) in (IRON, IRON_LIGHT): continue
                u, v = along(x, y)
                wave = math.sin(x * 1.7 + y * .9) + math.sin(x * .6 - y * 2.3)
                patch = math.sin(x * 2.9 - y * 1.3) + math.cos(x * 1.1 + y * 2.1)
                if u > .76 + .06 * wave and patch > -.7: c.c[y, x] = BLOOD_DARK if v > half_width(u) * .35 else BLOOD
        for u, v in [(.58, 1.3), (.6, -.9), (.53, .2), (.47, -.4), (.42, .5)]:
            x, y = at(u, v)
            c.c[y, x] = BLOOD
        for u, side in studs[:6]:
            x, y = at(u, side * (half_width(u) + .2))
            c.c[y + 1, x + 1] = BLOOD_DARK
        # 머리 아래쪽 가장자리에서 맺혀 떨어지는 핏방울(그림에서 똑바로 아래로)
        for u, n in [(.7, 3), (.8, 2), (.9, 4)]:
            x, y = at(u, half_width(u) - .5)
            while c.a[y, x]: y += 1
            c.layer(c.rect(x, y, x, y + n - 2), BLOOD)
            c.layer(c.rect(x, y + n, x, y + n), BLOOD_DARK)
    return c

def save(c, path):
    img = np.zeros((N, N, 4), np.uint8)
    img[..., :3] = c.c
    img[..., 3] = np.where(c.a, 255, 0)
    Image.fromarray(img).resize((N * B, N * B), Image.NEAREST).save(path)

def grip():
    """쥐는 점: 감은 천의 가운데(텍스처 왼쪽 아래 기준 픽셀)"""
    x = START[0] + AXIS[0] * .2 * LENGTH
    y = START[1] + AXIS[1] * .2 * LENGTH
    return round(x * B), round(N * B - y * B)

if __name__ == "__main__":
    folder = Path(sys.argv[1])
    save(club(False), folder / "dokkaebi_club.png")
    save(club(True), folder / "dokkaebi_club_blood.png")
    if "--grip" in sys.argv: print("grip", grip())
