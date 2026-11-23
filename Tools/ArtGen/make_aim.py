# 마우스로 겨눈 공격 그림: 수평 공격 그림에서 팔·무기를 잘라 돌리고, 몸은 겨눈 쪽으로 기울인다(새로 그리지 않음).
#   활 03_Player_Attack_Bow/bow_aim_<u|d><15·30·45·60·75·90>_00~03          (u: 위, d: 아래, 숫자는 각도)
#   칼 02_Player_Attack_Sword/sword_aim_<u|d><30·60·90>_00~03, 칼 없는 그림 sword_aim_empty_<…>_00~03
# 캔버스는 수평 그림의 위아래로 PAD, 오른쪽으로 RIGHT만큼 넓힌다. 발은 새 바닥에서 8+PAD px 위(JinaArt.AimPad).
# 게임이 같은 숫자를 쓴다: PAD(JinaArt.AimPad), LEAN(Combat.AimLean), 활 2번 그림의 어깨(Combat.ArrowLaunch),
#   칼의 축과 기울기(SeonbiVisual.SwingPose). 여기를 바꾸면 그쪽도 바꾼다.
# 사용법: python make_aim.py Assets/Resources/JinaV4   [--preview DIR]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image, ImageDraw
from framelib import load, save, clean, q, preview_sheet, preview_dir
ART = sys.argv[1]
BOW, SWORD = ART + "/03_Player_Attack_Bow", ART + "/02_Player_Attack_Sword"

B = 10                          # 그림 한 칸(px)
PAD, RIGHT = 150, 100           # 돌린 무기가 캔버스 밖으로 나가지 않게 넓히는 양
LEAN = 60                       # 90도로 겨눌 때 어깨 위가 기우는 양(px). 위로 겨누면 뒤로, 아래로 겨누면 앞으로
BOW_ANGLES = (15, 30, 45, 60, 75, 90)
SWORD_ANGLES = (30, 60, 90)

def lean(angle):
    return -round(LEAN * angle / 90 / B) * B

def bend_rows(h, shift, top, hip):
    """줄마다 미는 양: top 위는 다 밀고 hip까지 줄여 0 (framelib.bend와 같은 모양, 넘친 것은 감싸지 않고 버린다)."""
    return [shift if y < top else q(shift * (hip - y) / (hip - top), B) if y < hip else 0 for y in range(h)]

def shift_rows(a, rows):
    out = np.zeros_like(a)
    for y, s in enumerate(rows):
        if s > 0: out[y, s:] = a[y, :-s]
        elif s < 0: out[y, :s] = a[y, -s:]
        else: out[y] = a[y]
    return out

def turn(part, degrees, pivot):
    """원래 해상도에서 최근접으로 돌린다(그림 한 칸이 약 10px라 선이 끊기지 않는다). 양수가 시계 반대 방향."""
    return np.array(Image.fromarray(part).rotate(degrees, resample=Image.NEAREST, center=pivot))

def poly(shape, points):
    im = Image.new("L", (shape[1], shape[0]), 0); ImageDraw.Draw(im).polygon(points, fill=1)
    return np.array(im) > 0

def small_bits(mask, least, k=4):
    """mask에서 least px보다 작은 덩어리(k px 칸끼리 여덟 방향으로 이어 봄)."""
    h, w = mask.shape; H, W = -(-h // k), -(-w // k)
    m = np.zeros((H * k, W * k), bool); m[:h, :w] = mask
    counts = m.reshape(H, k, W, k).sum(axis=(1, 3)); cells = counts > 0
    label = np.zeros((H, W), int); sizes = [0]
    for y, x in zip(*np.nonzero(cells)):
        if label[y, x]: continue
        n = len(sizes); label[y, x] = n; stack = [(y, x)]; size = 0
        while stack:
            cy, cx = stack.pop(); size += counts[cy, cx]
            for ny in (cy - 1, cy, cy + 1):
                for nx in (cx - 1, cx, cx + 1):
                    if 0 <= ny < H and 0 <= nx < W and cells[ny, nx] and not label[ny, nx]:
                        label[ny, nx] = n; stack.append((ny, nx))
        sizes.append(size)
    small = np.array(sizes) < least; small[0] = False
    return np.repeat(np.repeat(small[label], k, 0), k, 1)[:h, :w] & mask

def aim(a, mask, pivot, angle, tilt, top, hip, behind):
    """mask 부분을 pivot(원래 좌표) 중심으로 angle만큼 돌리고, 나머지 몸은 tilt도로 겨눈 만큼 top~hip을 축으로 기울인다.
    돌린 부분은 어깨 줄이 기운 만큼 같이 옮긴다. behind면 돌린 부분을 몸 뒤에 그린다(먼 쪽 팔)."""
    a = np.pad(a, ((PAD, PAD), (0, RIGHT), (0, 0))); mask = np.pad(mask, ((PAD, PAD), (0, RIGHT)))
    px, py = pivot[0], pivot[1] + PAD
    body = np.where(mask[:, :, None], 0, a).astype(np.uint8)
    part = np.where(mask[:, :, None], a, 0).astype(np.uint8)
    body[small_bits(body[:, :, 3] > 0, 1500)] = 0           # 잘린 활·시위 조각이 몸에 남지 않게
    rows = bend_rows(a.shape[0], lean(tilt), top + PAD, hip + PAD)
    body = shift_rows(body, rows)
    part = turn(shift_rows(part, [rows[py]] * a.shape[0]), angle, (px + rows[py], py))
    under, over = (part, body) if behind else (body, part)
    out = under.copy(); m = over[:, :, 3] > 0; out[m] = over[m]
    out = clean(out)
    out[small_bits(out[:, :, 3] > 0, 120)] = 0              # 돌리며 생긴 부스러기
    return out

def name(angle):
    return ("u" if angle > 0 else "d") + str(abs(angle))

# ── 활: 앞(왼손) 팔·활·화살을 어깨 중심으로 돌린다. 왼손은 먼 쪽 팔이라 몸 뒤에 그린다(곧게 위로 쏘면 활이 갓 뒤로 간다).
# 그림마다 팔이 몸을 떠나는 어깨(x, 위에서 y), 몸 앞을 자르는 선, 갓 위·허리 아래에서 활만 고르는 선.
#   쏘는 그림(2)이 기울어 있어 팔이 더 오른쪽에서 시작한다. Combat.ArrowLaunch는 2번의 어깨를 쓴다.
FRAMES = [((300, 255), 320, 395), ((290, 215), 310, 395), ((400, 245), 395, 395), ((330, 245), 335, 335)]
HAT, WAIST = 130, 330
BOW_TOP, BOW_HIP = 260, 430     # 몸이 기우는 축: 어깨 위는 다 기울고 무릎 위에서 0

def cut(a, arm_x, limb_x):
    """몸 앞의 팔·활(갓과 허리 사이), 갓 위의 활 끝, 허리 아래의 활 아래 끝(나무색과 그 외곽선), 시위 전체.
    갓 챙 끝(어두운 색이고 나무에서 먼 것)은 몸에 남긴다. 시위는 몸 앞에서 끊기지 않게 통째로 활과 함께 돈다."""
    h, w, _ = a.shape
    ys, xs = np.mgrid[0:h, 0:w]
    r, g, b = (a[:, :, k].astype(int) for k in range(3))
    solid = a[:, :, 3] > 0
    wood = solid & (r - b > 50) & (r > 90)
    near = np.zeros_like(wood)
    for d in range(-12, 13, 4):
        near |= np.roll(wood, d, 0) | np.roll(wood, d, 1)
    dark = solid & (r + g + b < 200) & near
    string = solid & (r + g + b > 600) & ~np.roll(solid, 12, 1) & ~np.roll(solid, -12, 1)
    brim = solid & (r + g + b < 200) & ~near & (ys < HAT + 30)
    below = (xs >= limb_x) & (ys > WAIST) & (ys < 490) & (wood | dark | string)
    above = (ys < HAT) & ((xs >= limb_x) | wood | dark)
    m = solid & ((((xs >= arm_x) & (ys >= HAT) & (ys <= WAIST)) | above | below) & ~brim | string)
    return m

names = []
for i in range(4):
    a = load(f"{BOW}/bow_attack_{i:02d}.png")
    pivot, arm_x, limb_x = FRAMES[i]
    m = cut(a, arm_x, limb_x)
    for angle in BOW_ANGLES:
        for sign in (1, -1):
            path = f"{BOW}/bow_aim_{name(angle * sign)}_{i:02d}.png"
            save(aim(a, m, pivot, angle * sign, angle * sign, BOW_TOP, BOW_HIP, True), path); names.append(path)

# ── 칼: 2번(팔을 뻗어 벤 그림)은 소매·주먹·칼·칼빛을 어깨 중심으로, 0·3번은 코등이와 칼날만 손잡이 중심으로 돌린다.
#   1번(머리 위로 든 칼)은 돌리지 않고 기울이기만 한다(위로 베든 아래로 베든 들어 올리는 자세는 같다).
#   2번을 60도 넘게 위로 돌리면 소매가 얼굴을 덮으므로 몸 뒤에 그린다.
SWORD_TOP, SWORD_HIP = 300, 470

def cream(a):
    r, b = a[:, :, 0].astype(int), a[:, :, 2].astype(int)
    return (r > 170) & (r - b > 12)                         # 두루마기(칼날은 푸르스름한 흰색이라 빠진다)

def sword_part(i, a):
    shape = a.shape[:2]; solid = a[:, :, 3] > 0
    if i == 0: return poly(shape, [(272, 398), (302, 398), (302, 418), (585, 440), (585, 520), (300, 470), (272, 452)]) & solid & ~cream(a), (280, 428)
    if i == 1: return np.zeros(shape, bool), (425, 170)
    if i == 2: return solid & (poly(shape, [(380, 0), (670, 0), (670, 585), (380, 585)])
                               | poly(shape, [(355, 290), (380, 290), (380, 585), (355, 585)])
                               | poly(shape, [(303, 295), (355, 295), (355, 402), (303, 402)])), (290, 330)
    return poly(shape, [(372, 425), (400, 425), (615, 520), (615, 580), (555, 585), (395, 480), (372, 475)]) & solid & ~cream(a), (380, 440)

for tag in ("", "empty_"):
    for i in range(4):
        a = load(f"{SWORD}/sword_attack_{tag}{i:02d}.png")
        m, pivot = sword_part(i, load(f"{SWORD}/sword_attack_{i:02d}.png"))   # 칼 없는 그림도 칼 있는 그림의 자리로 자른다
        m &= a[:, :, 3] > 0
        for angle in SWORD_ANGLES:
            for sign in (1, -1):
                deg = angle * sign
                path = f"{SWORD}/sword_aim_{tag}{name(deg)}_{i:02d}.png"
                # 1번은 돌리지 않아도 몸은 겨눈 쪽으로 기운다
                save(aim(a, m, pivot, 0 if i == 1 else deg, deg, SWORD_TOP, SWORD_HIP, i == 2 and deg >= 60), path)
                names.append(path)

if preview_dir():
    d = preview_dir()
    preview_sheet([f"{BOW}/bow_aim_{name(s * x)}_02.png" for s in (1, -1) for x in BOW_ANGLES], d + "/bow_aim_sheet.png", 840)
    preview_sheet([f"{SWORD}/sword_aim_{name(s * x)}_{i:02d}.png" for s in (1, -1) for x in SWORD_ANGLES for i in range(4)], d + "/sword_aim_sheet.png", 900)
