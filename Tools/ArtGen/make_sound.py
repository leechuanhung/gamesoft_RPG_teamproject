# 팀원 소리가 없는 곳의 배경 음악과 효과음을 합성한다(soundlib.py의 국악기 흉내). 팀원이 만든 wav는 건드리지 않는다.
#   배경 음악 bgm_<곳>.wav: 되풀이할 때 이음매가 없다(끝의 여운을 앞에 접고, 메아리도 돌려 감음).
#   효과음 sfx_<일>.wav, 무기 sfx_w_<무기>.wav.
# 유니티가 팀원 wav는 가져올 때 크기를 최대로 맞추므로(normalize) 게임 속 크기는 실효값 약 0.1~0.28이다.
#   여기서 만든 소리는 .meta에서 normalize를 끄고, 크기를 직접 맞춘다(배경 음악 실효값 0.11~0.17, 효과음 최고 0.85 안팎).
# 평조(솔라도레미)는 밝은 곳, 계면조(라도레미솔, 레를 꺾고 미를 떪)는 어둡고 슬픈 곳에 쓴다.
# 사용법: python make_sound.py Assets/Resources/JinaSound   [--only 이름]
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from soundlib import (SR, NYQ, Mix, seconds, noise, smooth, lowpass, highpass, bandpass, sweep_noise, midi, hz, release,
                      gayageum, daegeum, haegeum, piri, horn, drone, wind, kung, deok, buk, jing, kkwaeng, bell, chime, pak,
                      metal, write)

OUT = sys.argv[1]
ONLY = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
made = []


def save(name, x):
    if ONLY and ONLY != name: return
    if not name.startswith("bgm_"):
        fade = int(.008 * SR); x = x.copy(); x[-fade:] *= np.linspace(1, 0, fade)   # 끝에서 딸깍하지 않게
    if write(f"{OUT}/{name}.wav", x): made.append(name)


def bass(mix, roots, start, slot, bar, gain=.5, pattern=((0, 0, 1.), (6, 7, .7), (9, 12, .45)), seed=0):
    """가야금 저음: 마디마다 밑음, 다섯 음 위, 옥타브를 뜯는다."""
    for b, root in enumerate(roots):
        for i, (at, up, v) in enumerate(pattern):
            mix.add(gayageum(hz(midi(root) + up), slot * 2.5, v, seed=seed + b * 7 + i), start + b * bar + at * slot, gain)


def toad(seed=0):
    """두꺼비 울음: 낮은 음이 빠르게 끊겨 그르르."""
    n = int(.45 * SR); t = seconds(n)
    pulse = (np.sin(2 * np.pi * 24 * t) > -.2).astype(float)
    x = np.sin(2 * np.pi * (92 + 10 * t) * t) * lowpass(pulse, 300, 63)
    return lowpass(x + .2 * noise(n, seed) * pulse, 700, 63) * np.sin(np.pi * t / t[-1]) ** .5


def drop(seed=0, f=900):
    """물방울: 짧게 올라가는 똑."""
    n = int(.12 * SR); t = seconds(n)
    return np.sin(2 * np.pi * np.cumsum(f + 900 * t / t[-1]) / SR) * np.exp(-t / .03)


def twinkle(f, seed=0):
    """도깨비불의 반짝임: 아주 높고 여린 떨림."""
    n = int(1.2 * SR); t = seconds(n)
    return np.sin(2 * np.pi * f * t) * np.exp(-t / .35) * (.6 + .4 * np.sin(2 * np.pi * 9 * t)) * smooth(t / .02)


def jingle(seed=0):
    """말방울 한 줌(역참)."""
    out = np.zeros(int(1.4 * SR)); rng = np.random.default_rng(seed)
    for i in range(7):
        b = bell(2400 * rng.uniform(.95, 1.1), .35, seed + i, .5); at = int(rng.uniform(0, .5) * SR)
        out[at:at + len(b)] += b[:len(out) - at]
    return out


# ══════════════════ 배경 음악 ══════════════════

def bgm_pass():
    """고개와 서낭당: 굿거리, 평조. 대금이 고개를 넘는 길손처럼 느긋하게 부르고, 풍경이 바람에 흔들린다."""
    e = .2833; bar = 12 * e; L = 12 * bar; m = Mix(L)
    melody = (r"A5:6v G5:3 E5:3  D5:9v r:3  E5:3 G5:3 A5:3 B5:3  A5:9v r:3  B5:3 A5:3 G5:2 A5:1 G5:3  E5:9v r:3 "
              r"G5:3 A5:3 G5:3 E5:3  D5:12v  D6:6v B5:3 A5:3  B5:6 A5:3 G5:3  E5:3 G5:3 A5:4 G5:2\  E5:6v D5:3 r:3")
    m.notes(daegeum, melody, 0, e, .9, transpose=-12, seed=1)
    bass(m, ["D3", "D3", "G3", "A3", "G3", "E3", "G3", "D3", "D3", "G3", "A3", "D3"], 0, e, bar, .55, seed=20)
    for b in range(12):
        m.rhythm("D..tT.K.r..." if b % 2 == 0 else "K..tT.K.r..t", b * bar, e, .32)
    m.add(wind(L, 3, 250, 1200, .05), 0)
    rng = np.random.default_rng(4)
    for i in range(9):
        m.add(bell(hz(midi(rng.choice(["D6", "E6", "G6", "A6", "B6"]))), .5, 40 + i, 2.5), rng.uniform(0, L), .2)
    m.reverb(.22, 1.4, 5)
    return m.render(0.15)


def bgm_hill():
    """뒷산: 박자 없이 낮게 깔린 울림, 해금이 흐느끼고, 어디선가 두꺼비가 운다."""
    L = 36.; m = Mix(L); b = .9
    m.add(drone(55, L, 1, 1, fade=0), 0); m.add(drone(82.4, L, .5, 2, fade=0), 0)
    m.notes(haegeum, r"E4:3v D4:1.5\ C4:2 A3:4v", 2 / b, b, .8, seed=10)
    m.notes(haegeum, r"A4:2/ G4:1 E4:3v D4:1\ C4:3", 16 / b, b, .7, seed=20)
    m.notes(haegeum, r"C4:2 D4:2 E4:5v", 28 / b, b, .75, seed=30)
    for at in (9, 9.45, 23.2, 23.6, 24.0):
        m.add(toad(int(at * 10)), at, .5)
    m.add(jing(.6, 5, 82), 0, .7); m.add(jing(.45, 6, 82), 18, .7)
    rng = np.random.default_rng(7)
    for i in range(10): m.add(drop(i, rng.uniform(700, 1300)), rng.uniform(0, L), .12)
    m.add(wind(L, 8, 120, 500, .07), 0)
    m.reverb(.35, 2.2, 9)
    return m.render(0.12)


def bgm_kong_fight():
    """콩쥐 요괴와 싸움: 휘모리, 계면조. 해금이 몰아치고 장구·북·꽹과리가 내달린다."""
    q = .375; s = q / 4; bar = 16 * s; L = 24 * bar; m = Mix(L)
    A = r"D5:2 F5:2 G5:2 A5:4v G5:2 F5:2 D5:4 C5:2 D5:2 F5:4 G5:2\ F5:2 r:2"
    B = r"A5:4v C6:2 A5:2 G5:4\ F5:4 G5:2 A5:2 G5:2 F5:2 D5:8v"
    C = r"D6:6v C6:2 A5:4 G5:4\ A5:2 C6:2 D6:4 C6:2 A5:2 G5:2\ F5:2"
    D = r"D4:2 D4:2 F4:2 D4:2 G4:2 D4:2 A4:4 C5:2 A4:2 G4:2 F4:2 D4:8"
    at = 0
    for k, riff in enumerate([D, D, A, B, A, B, C, C, A, B, C, B]):
        at = m.notes(haegeum, riff, at, s, .85, seed=100 + k * 20)
        if riff is C: m.notes(piri, riff, at - 32, s, .45, transpose=-12, seed=300 + k)
    for b in range(24):
        for i, (pos, note) in enumerate(((0, "D2"), (3, "D3"), (6, "A2"), (8, "D3"), (11, "C3"), (14, "D3"))):
            m.add(gayageum(hz(midi(note)), s * 2, .9 if pos == 0 else .6, seed=b * 10 + i), b * bar + pos * s, .5)
        m.rhythm(["D...T.K.D..tT.K.", "D..tT.K.D...TrK."][b % 2], b * bar, s, .5)
        m.rhythm("B.......b.......", b * bar, s, .55)
        if b >= 8: m.rhythm("G.g.G.g.G.ggG.g." if b % 4 != 3 else "G.G.G.ggG.G.GGGG", b * bar, s, .35)
        if b % 4 == 0: m.add(jing(.8, b), b * bar, .55)
    m.reverb(.18, 1., 11)
    return m.render(0.17)


def bgm_fort():
    """봉화 산성: 자진모리, 평조. 태평소가 경계하듯 부르고, 북과 나발이 성벽을 지킨다."""
    e = .2; bar = 12 * e; L = 16 * bar; m = Mix(L)
    tune = (r"G4:3 A4:3 C5:6v  A4:3 G4:3 F4:6  G4:3 C5:3 D5:3 C5:3  A4:12v "
            r"C5:3 D5:3 F5:6v  D5:3 C5:3 A4:6  G4:3 A4:3 C5:2 A4:1 G4:3  F4:12v")
    m.notes(piri, tune, 0, e, .9, seed=1, bright=1.)
    m.notes(daegeum, tune, 96, e, .9, seed=60)
    for b in range(16):
        m.rhythm("B..b..B..b.." if b % 4 != 3 else "B..b..B.bBBB", b * bar, e, .6)
        m.rhythm("D..T.KK..T..", b * bar, e, .3)
        if b % 4 == 0:
            m.add(jing(.8, b), b * bar, .5)
            m.add(horn(hz(midi("C3")), bar * 2.5, .9, seed=b), b * bar, .55)
    bass(m, ["C3", "F3", "C3", "F3", "C3", "D3", "C3", "F3"] * 2, 0, e, bar, .4, ((0, 0, 1.), (6, 7, .6)), seed=70)
    m.add(wind(L, 12, 300, 1500, .04), 0)
    m.reverb(.25, 1.6, 13)
    return m.render(0.16)


def bgm_station():
    """버려진 역참: 바람만 지나는 빈 마당, 가야금이 드문드문 뜯기고 도깨비불이 반짝인다."""
    L = 40.; m = Mix(L); b = .7
    for at, score in ((1, "E4:2 G4:1 A4:3v"), (7, r"B4:2 A4:1\ G4:1 E4:4v"), (15, "D5:2 B4:2 A4:4v"),
                      (22, r"G4:1 A4:1 B4:2 A4:2\ G4:4"), (30, "E4:2 D4:2 E4:6v")):
        m.notes(gayageum, score, at / b, b, .8, seed=at * 3)
        m.add(gayageum(hz(midi("E2")), 2, .7, seed=at), at, .5)
    m.add(drone(82.4, L, .5, 3, fade=0), 0)
    m.add(wind(L, 14, 200, 1400, .16), 0)
    m.add(jingle(1), 12, .25); m.add(jingle(2), 33, .2)
    rng = np.random.default_rng(15)
    for i in range(14): m.add(twinkle(rng.uniform(2200, 3600), i), rng.uniform(0, L), .08)
    m.reverb(.3, 2., 16)
    return m.render(0.11)


def bgm_grave():
    """북망산 무덤길: 상엿소리처럼 요령이 박자를 짚고, 해금이 서럽게 메긴다."""
    b = 1.1; L = 36 * b; m = Mix(L)
    m.add(drone(55, L, .9, 4, fade=0), 0); m.add(drone(82.4, L, .4, 5, fade=0), 0)
    for i in range(36):
        m.add(bell(1500 if i % 2 == 0 else 1520, .7 if i % 2 == 0 else .4, 200 + i % 4, 1.2), i * b, .45)
        if i % 4 == 0: m.add(buk(.6, i, .8), i * b, .45)
    song = r"E4:2v D4:1\ C4:1 A3:4v  C4:1 D4:1 E4:2v G4:1 E4:1 D4:2\  A4:3v G4:1 E4:2 D4:1\ C4:1  A3:6v r:2 r:4"
    m.notes(haegeum, song, 0, b, .8, seed=40)
    m.notes(daegeum, r"A4:3v G4:1 E4:2 D4:1\ C4:1", 16, b, .5, transpose=-12, seed=50)
    m.add(wind(L, 17, 120, 600, .06), 0)
    m.reverb(.35, 2.4, 18)
    return m.render(0.13)


def bgm_city():
    """한양 거리: 빠른 굿거리, 평조. 가야금이 흥겹게 뜯고 대금이 받쳐 장터가 북적인다."""
    e = .1667; bar = 12 * e; L = 18 * bar; m = Mix(L)
    tune = (r"D5:3 E5:3 G5:3 E5:3  D5:2 C5:1 A4:3 G4:6  A4:3 C5:3 D5:2 E5:1 D5:3  C5:6v r:3 G4:3 "
            r"E5:3 G5:3 A5:3 G5:3  E5:2 D5:1 E5:3 D5:6v  C5:3 D5:3 E5:2 D5:1 C5:3  A4:6 C5:3 D5:3  G4:12v "
            r"G5:3 A5:3 G5:3 E5:3  D5:3 E5:3 G5:6v  A5:3 G5:3 E5:3 D5:3  C5:6 D5:3 E5:3  D5:2 C5:1 A4:3 C5:6 "
            r"D5:3 E5:3 D5:3 C5:3  A4:3 G4:3 A4:3 C5:3  D5:12v  E5:3 D5:3 C5:3 A4:3")
    m.notes(gayageum, tune, 0, e, .9, seed=1)
    holds = "G5 C6 D6 C6 E6 D6 C6 A5 G5 D6 G5 E6 C6 A5 D6 C6 D6 C6".split()
    for i, h in enumerate(holds):
        if i >= 9: m.add(daegeum(hz(midi(h) - 12), bar * .95, .6, seed=80 + i), i * bar, .45)
    roots = "G3 G3 D3 C3 C3 G3 C3 D3 G3 G3 G3 D3 C3 D3 G3 D3 D3 C3".split()
    bass(m, roots, 0, e, bar, .45, seed=90)
    for b in range(18):
        m.rhythm("D..tT.K.rK.t" if b % 2 == 0 else "K..tT.K.r.Tt", b * bar, e, .45)
        m.rhythm("g..g..g..g..", b * bar, e, .2)
        if b % 2 == 0: m.rhythm("b...........", b * bar, e, .35)
    m.reverb(.15, 1., 19)
    return m.render(0.16)


def bgm_hall():
    """과거장 앞: 정악처럼 느리고 점잖게. 박으로 열고, 피리와 대금이 한 가락을 길게 끌며 편경이 짚는다."""
    b = 1.2; L = 32 * b; m = Mix(L)
    song = r"Bb4:3v Ab4:1 Bb4:2 C5:2v  Eb5:4v C5:2 Bb4:2  Ab4:3 Bb4:1 Ab4:2 F4:2v  Eb4:6v r:2"
    m.notes(piri, song, 0, b, .8, seed=1, bright=.3)
    m.notes(daegeum, song, 0, b, .55, transpose=12, seed=30)
    m.add(pak(1, 1), 0, .8)
    for i, note in enumerate(("Bb5", "Eb6", "Ab5", "Eb5")):
        m.add(chime(hz(midi(note))), i * 8 * b, .6)
    for i in range(0, 32, 4): m.add(buk(.45, i, .9), i * b + b * 2, .35)
    m.add(drone(77.8, L, .5, 6, fade=0), 0)
    m.reverb(.3, 2., 21)
    return m.render(0.14)


def bgm_manor():
    """좌의정 댁: 등불 밝은 잔칫집인데 어딘가 어긋났다. 가야금 가락 아래 낮은 울림이 증4도로 부딪는다."""
    b = .9; L = 48 * b; m = Mix(L)
    song = (r"B4:2 D5:1 E5:1 F#5:4v  E5:1 D5:1\ B4:2 C5:2 B4:2v  F#4:2 A4:1 B4:1 D5:3 C5:1  B4:8v "
            r"A4:2 B4:1 D5:1 E5:2\ D5:2  B4:4v r:2 A4:2")
    m.notes(gayageum, song, 0, b, .85, seed=1)
    for i, note in enumerate(("B3", "A3", "F#3", "B3", "E3", "F#3")):
        m.add(piri(hz(midi(note)), 8 * b * .95, .5, seed=40 + i, bright=.2), i * 8 * b, .4)
    m.add(drone(61.7, L, .6, 7, fade=0), 0); m.add(drone(87.3, L, .25, 8, fade=0), 0)
    for i in range(0, 48, 8): m.rhythm("k...", i * b, b, .4)
    m.reverb(.28, 1.8, 22)
    return m.render(0.13)


def whisper(length, seed):
    """속삭임: 사람 목소리 같은 두 띠의 잡음이 음절처럼 끊겼다 이어진다(알아들을 수 없는 망령의 말)."""
    n = int(length * SR); t = seconds(n); rng = np.random.default_rng(seed)
    voice = bandpass(noise(n, seed), 600, 1300) + .6 * bandpass(noise(n, seed + 1), 2100, 3000)
    syl = np.zeros(n); at = 0.
    while at < length - .15:
        d = rng.uniform(.08, .2); i, j = int(at * SR), int(min(length, at + d) * SR)
        syl[i:j] = np.sin(np.pi * np.linspace(0, 1, j - i)) * rng.uniform(.5, 1); at += d + rng.uniform(0, .08)
    return voice * syl * smooth(t / .2) * smooth((length - t) / .3)


def bgm_final_fight():
    """좌의정(어둑시니)과 싸움: 웅장하고 기괴하게. 느린 반박자 위로 나발 떼가 단조 화음을 짓누르고(증4도·반음으로
    뒤틀림), 큰북과 징이 무너지듯 치며, 피리가 울부짖고 반음 어긋난 해금이 따라 운다. 사이사이 망령의 속삭임과
    심장 소리, 끝에는 해금 넷이 한 옥타브씩 미끄러져 떨어진다."""
    q = .4545; s = q / 2; bar = 8 * s; L = 20 * bar; m = Mix(L)
    # 밑바닥: 반음으로 부딪는 아주 낮은 울림 둘.
    m.add(drone(32.7, L, 1.2, 9, fade=0), 0); m.add(drone(34.65, L, .7, 10, fade=0), 0)
    # 나발 떼: 두 마디마다 한 화음. 뿌리 + 5도 + 옥타브 + 단3도(Gb·G 화음은 위에 증4도를 얹어 뒤튼다).
    chords = [("C2", (0, 7, 12, 15)), ("C2", (0, 7, 12, 15, 19)), ("Db2", (0, 7, 12, 16)), ("Gb1", (0, 6, 12, 18)),
              ("C2", (0, 7, 12, 15)), ("Ab1", (0, 7, 12, 16, 19)), ("Db2", (0, 7, 12, 16)), ("G1", (0, 6, 12, 17)),
              ("C2", (0, 7, 12, 15, 18)), ("Db2", (0, 6, 13, 16))]
    for c, (root, ups) in enumerate(chords):
        for i, up in enumerate(ups):
            m.add(horn(hz(midi(root) + up), bar * 2 * .97, 1 - .1 * i, seed=900 + c * 10 + i), c * 2 * bar, .5)
    # 가야금 저음 되풀이: 쿵쿵 밀고 들어오는 여덟 박.
    for b in range(20):
        root = chords[b // 2][0]; low = midi(root) + (12 if root.endswith("1") else 0)
        for i, (pos, up) in enumerate(((0, 0), (1, 0), (2, 1), (3, 0), (4, 0), (5, 0), (6, 6), (7, 0))):
            m.add(gayageum(hz(low + up), s * 1.6, .9 if pos % 4 == 0 else .55, seed=1000 + b * 8 + i), b * bar + pos * s, .55)
    # 북·장구·징·꽹과리: 처음 넷은 무겁게, 가운데는 밀고, 13~16은 몰아치고, 17~20은 심장만 뛴다.
    for b in range(20):
        at = b * bar
        if b < 4:
            m.rhythm("B.......", at, s, .75); m.rhythm("....b...", at, s, .5)
        elif b < 12:
            m.rhythm("B...B.b.", at, s, .75); m.rhythm("D.tT.KT.", at, s, .35)
        elif b < 16:
            m.rhythm("B.bBB.bB", at, s, .8); m.rhythm("D.tTDtTr", at, s, .4); m.rhythm("GgGgGgGG", at, s, .3)
        else:
            m.rhythm("K.k.....", at, s, .9); m.rhythm("....K.k.", at, s, .7)
        if b % 2 == 0 and b < 16: m.add(jing(1, b, 82), at, .6 if b in (0, 4, 12) else .4)
    m.add(buk(1, 77, .6), 12 * bar, .9); m.add(jing(1, 78, 73), 12 * bar, .7)
    # 피리가 울부짖고, 해금이 반음보다 조금 어긋나 따라 운다(맥놀이로 메스껍게 흔들림).
    A = r"C5:6v Db5:2 C5:4 Bb4:2 Ab4:2 G4:8v r:2 Ab4:2 Bb4:2 Db5:2"
    B = r"Eb5:6v Gb5:2 F5:4\ Eb5:4 Db5:6v C5:2 Bb4:4 C5:4v"
    C = r"C6:8v Bb5:4 Gb5:4\ G5:6v Ab5:2 G5:4 Db5:4v"
    at = 4 * 8
    for k, phrase in enumerate((A, B, C, B)):
        m.notes(piri, phrase, at, s, .95, seed=1100 + k * 30, bright=1.)
        m.notes(haegeum, phrase, at, s, .45, transpose=12.35 if phrase is C else .35, seed=1200 + k * 30)
        if phrase is C: m.notes(horn, phrase.replace("v", "").replace("\\", ""), at, s, .35, transpose=-24, seed=1300)
        at += 32
    # 마지막 네 마디: 해금 넷이 부딪는 음에서 한꺼번에 한 옥타브 미끄러져 떨어진다.
    for i, note in enumerate(("C5", "Db5", "Gb5", "G5")):
        m.add(haegeum(hz(midi(note)), bar * 3.6, .7, vib=.6, bend=-12, seed=1400 + i), 16 * bar + .1 * i, .4)
    # 망령의 속삭임과 바닥에서 올라오는 바람.
    for i, at in enumerate((.5, 3.4, 30.0, 33.0)):
        m.add(whisper(2.4, 1500 + i), at, .07)
    m.add(sweep_noise(int(2.5 * SR), 1600, 150, 2500, .8) * np.linspace(0, 1, int(2.5 * SR)) ** 2, 12 * bar - 2.5, .3)
    m.reverb(.3, 2.2, 23)
    return m.render(0.17)


def bgm_ending():
    """결말: 느린 굿거리, 평조. 대금이 따뜻하게 부르고 가야금이 고르게 받친다."""
    e = .3333; bar = 12 * e; L = 12 * bar; m = Mix(L)
    tune = (r"C5:6v D5:3 C5:3  Bb4:3 G4:3 F4:6v  G4:3 Bb4:3 C5:6v  D5:9v r:3  F5:6v D5:3 C5:3  Bb4:3 C5:3 D5:6v "
            r"C5:3 Bb4:3 G4:3 Bb4:3  C5:12v  D5:3 F5:3 G5:6v  F5:3 D5:3 C5:6  Bb4:3 C5:2 Bb4:1 G4:6  F4:12v")
    m.notes(daegeum, tune, 0, e, .85, seed=1)
    scale = [midi(n) for n in "F3 G3 Bb3 C4 D4 F4 G4 Bb4 C5 D5 F5 G5".split()]
    for b, root in enumerate([0, 2, 1, 4, 0, 2, 3, 3, 4, 0, 1, 0]):
        for j, step in enumerate((0, 2, 5, 7, 5, 2)):
            m.add(gayageum(hz(scale[root + step]), e * 2.5, .55 + .25 * (j == 0), seed=b * 10 + j), b * bar + j * 2 * e, .45)
        m.rhythm("k...........", b * bar, e, .3)
    for i in range(0, 12, 4): m.add(bell(hz(midi("C6")), .4, i, 3), i * bar, .25)
    m.reverb(.28, 1.8, 24)
    return m.render(0.14)


# ══════════════════ 효과음 ══════════════════

def sfx(length): return Mix(length, loop=False, tail=4.)


def sfx_charm():
    """정화 부적: 비석의 빛이 잦아들며 부적이 떠오른다. 맑은 종이 차례로 올라가고 징이 부드럽게 감싼다."""
    m = sfx(3.4)
    for i, n in enumerate(("D5", "E5", "G5", "A5", "B5", "D6", "E6")):
        m.add(bell(hz(midi(n)), .8, 10 + i, 2.5), .12 * i, .7)
    m.add(jing(.5, 3, 147), 0, .5)
    m.add(sweep_noise(int(1.4 * SR), 4, 1500, 6000) * np.sin(np.pi * seconds(int(1.4 * SR)) / 1.4), 0, .08)
    m.add(daegeum(hz(midi("A5")), 1.4, .6, vib=.3, seed=5), .9, .5)
    m.reverb(.3, 1.6, 1)
    return m.render(1, .85)


def sfx_keepsake():
    """은비녀: 물속에서 건진 은빛. 높은 방울이 반짝이고 가야금이 계면으로 서글프게 내려온다."""
    m = sfx(3)
    for i, f in enumerate((3520, 2960, 3320, 2640, 3950)):
        m.add(bell(f, .6, 30 + i, 1.4), .07 * i, .35)
    m.notes(gayageum, r"E5:1 D5:1\ C5:1 A4:3v", .5 / .32, .32, .8, seed=40)
    m.reverb(.3, 1.4, 2)
    return m.render(1, .8)


def sfx_beacon():
    """봉화를 올린다: 불길이 확 일고 탁탁 튄 뒤 북 한 번."""
    m = sfx(2.2); n = int(1.6 * SR); t = seconds(n)
    m.add(sweep_noise(n, 5, 300, 2500, .8) * smooth(t / .15) * np.exp(-t / .7), 0, .7)
    rng = np.random.default_rng(6)
    for i in range(22):
        c = highpass(noise(int(.01 * SR), 60 + i), 1500, 31) * np.exp(-seconds(int(.01 * SR)) / .002)
        m.add(c, rng.uniform(.1, 1.8), rng.uniform(.2, .6))
    m.add(buk(1, 7), .05, .8)
    m.reverb(.2, 1.2, 3)
    return m.render(1, .85)


def sfx_beacon_wrong():
    """차례가 틀렸다: 불이 치익 꺼지고 둔한 북과 어긋난 두 음."""
    m = sfx(1.8); n = int(1.2 * SR); t = seconds(n)
    m.add(sweep_noise(n, 8, 4000, 600, .7) * np.exp(-t / .35), 0, .5)
    m.add(buk(.8, 9, .8), 0, .7)
    m.add(gayageum(hz(midi("C3")), .8, .8, seed=10), .1, .5); m.add(gayageum(hz(midi("F#3")), .8, .8, seed=11), .1, .5)
    m.reverb(.2, 1., 4)
    return m.render(1, .8)


def sfx_gate():
    """성문이 열린다: 북 두 번, 굵은 나무가 삐걱이며 밀리고 징이 운다."""
    m = sfx(4); n = int(2.6 * SR); t = seconds(n)
    creak_f = 70 + 25 * np.sin(2 * np.pi * .7 * t) + 8 * np.sin(2 * np.pi * 13 * t)
    saw = sum(np.sin(k * 2 * np.pi * np.cumsum(creak_f) / SR) / k for k in range(1, 18))
    creak = bandpass(saw * (.6 + .4 * (noise(n, 12) > .3)), 300, 2200) * smooth(t / .3) * (1 - smooth((t - 2.1) / .5))
    m.add(creak, .5, .5)
    m.add(buk(1, 13), 0, .9); m.add(buk(1, 14), .35, .8); m.add(buk(.9, 15, .7), 2.7, .9)
    m.add(jing(1, 16), .4, .6)
    m.reverb(.3, 1.8, 5)
    return m.render(1, .85)


def sfx_purify():
    """정화: 맑은 종이 피어오르고 평조 화음이 환하게 번진다."""
    m = sfx(3.6)
    for i, n in enumerate(("G4", "A4", "C5", "D5", "E5", "G5", "A5", "C6")):
        m.add(bell(hz(midi(n)), .7, 50 + i, 2.6), .1 * i, .55)
    for i, n in enumerate(("G3", "D4", "G4", "B4")):
        m.add(daegeum(hz(midi(n)), 2.2, .5, seed=60 + i), .3, .35)
    m.add(sweep_noise(int(2 * SR), 61, 800, 7000) * np.sin(np.pi * seconds(int(2 * SR)) / 2), 0, .07)
    m.add(jing(.5, 62, 196), .05, .4)
    m.reverb(.35, 2., 6)
    return m.render(1, .85)


def sfx_boss_die():
    """큰 요괴가 쓰러진다: 낮게 울부짖다 흩어지는 바람, 징이 끝을 맺는다."""
    m = sfx(3.5); n = int(2 * SR); t = seconds(n)
    f = 110 * 2 ** (-1.5 * t / 2)
    growl = sum(np.sin(k * 2 * np.pi * np.cumsum(f * (1 + .03 * np.sin(2 * np.pi * 31 * t))) / SR) / k for k in range(1, 12))
    m.add(lowpass(growl, 1200) * smooth(t / .05) * np.exp(-t / .9), 0, .5)
    m.add(sweep_noise(int(2.5 * SR), 17, 3000, 200, .8) * np.exp(-seconds(int(2.5 * SR)) / 1.), .2, .35)
    m.add(buk(1, 18, .7), 0, .8); m.add(jing(1, 19, 82), .6, .6)
    m.reverb(.3, 1.8, 7)
    return m.render(1, .85)


def sfx_transform():
    """변신: 낮은 울림이 차오르다(1.8초) 북·징·꽹과리가 한꺼번에 터진다."""
    m = sfx(4); n = int(1.8 * SR); t = seconds(n)
    rise = drone(55, 1.8, 1, 20, fade=0) * 2 * np.exp((t - 1.8) / .6)
    m.add(rise, 0, .8)
    m.add(sweep_noise(n, 21, 200, 4000, .7) * (t / 1.8) ** 2, 0, .5)
    m.add(buk(1, 22, .8), 1.8, 1); m.add(jing(1, 23, 87), 1.8, .8); m.add(kkwaeng(1, 24), 1.8, .8)
    m.reverb(.3, 1.6, 8)
    return m.render(1, .85)


def sfx_dark():
    """좌의정이 어둠을 두른다: 깊이 빨려 들어가는 소리."""
    m = sfx(3); n = int(2.2 * SR); t = seconds(n)
    m.add(sweep_noise(n, 25, 5000, 150, .8) * smooth(t / 1.6) * (1 - smooth((t - 1.9) / .3)), 0, .6)
    m.add(drone(41.2, 2.8, 1, 26), 0, 1.2)
    m.add(buk(1, 27, .6), 2.1, .9)
    m.reverb(.35, 2., 9)
    return m.render(1, .85)


def sfx_hit():
    """맞는 소리: 짧고 단단한 퍽."""
    n = int(.22 * SR); t = seconds(n)
    x = np.sin(2 * np.pi * np.cumsum(140 * np.exp(-t / .04) + 60) / SR) * np.exp(-t / .05)
    x += bandpass(noise(n, 28), 600, 5000, 63) * np.exp(-t / .015) * .8
    return x / np.abs(x).max() * .7


def sfx_hurt():
    """선비가 맞는다: 둔하고 무거운 쿵, 숨이 막히는 잡음."""
    n = int(.4 * SR); t = seconds(n)
    x = np.sin(2 * np.pi * np.cumsum(90 * np.exp(-t / .06) + 45) / SR) * np.exp(-t / .09)
    x += lowpass(noise(n, 29), 1500, 63) * np.exp(-t / .03) * .7
    x += bandpass(noise(n, 30), 300, 900, 63) * np.exp(-t / .12) * .3
    return x / np.abs(x).max() * .8


def sfx_coin():
    """엽전: 놋쇠 동전 두세 닢이 짤랑."""
    m = sfx(.6)
    for i, (at, f) in enumerate(((0, 3100), (.06, 3700), (.13, 3350))):
        m.add(metal(f, [1, 1.52, 2.33, 3.1], [1, .6, .4, .2], [.12, .08, .06, .04], .4, 1, 70 + i, .5), at, 1 - .2 * i)
    return m.render(1, .7)


def sfx_heal():
    """기운이 돈다(서낭당, 꿀): 부드럽게 올라가는 평조 방울."""
    m = sfx(1.6)
    for i, n in enumerate(("C5", "D5", "E5", "G5", "A5", "C6")):
        m.add(bell(hz(midi(n)), .6, 80 + i, 1.2), .07 * i, .5)
    m.add(sweep_noise(int(.9 * SR), 81, 2000, 8000) * np.sin(np.pi * seconds(int(.9 * SR)) / .9), 0, .05)
    m.reverb(.25, 1.2, 10)
    return m.render(1, .75)


def sfx_ghost_die():
    """넋이 흩어진다: 바람 섞인 울음이 미끄러져 내려간다."""
    n = int(1.3 * SR); t = seconds(n)
    f = 700 * 2 ** (-1.2 * t / 1.3) * 2 ** (.4 * np.sin(2 * np.pi * 6 * t) / 12)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * .6 + sweep_noise(n, 31, 2500, 500, .5) * .8
    x *= smooth(t / .05) * np.exp(-t / .5)
    return x / np.abs(x).max() * .65


def sfx_mob_die():
    """몬스터가 쓰러진다: 털썩 하고 먼지가 일어난다."""
    n = int(.7 * SR); t = seconds(n)
    x = np.sin(2 * np.pi * np.cumsum(110 * np.exp(-t / .05) + 50) / SR) * np.exp(-t / .12)
    x += lowpass(noise(n, 32), 2500, 63) * np.exp(-t / .2) * smooth(t / .03) * .45
    return x / np.abs(x).max() * .75


def sfx_alarm():
    """마을 경보: 꽹과리를 급하게 몰아친다."""
    m = sfx(1.8)
    for i, at in enumerate((0, .12, .24, .3, .42, .54, .6, .72, .84, .9, 1.02, 1.14)):
        m.add(kkwaeng(1 if i % 3 == 0 else .75, 90 + i, i % 3 == 2), at, .9)
    return m.render(1, .8)


def sfx_splash():
    """강에 풍덩: 물보라와 꼬르륵."""
    m = sfx(1.2); n = int(.8 * SR); t = seconds(n)
    m.add(bandpass(noise(n, 33), 300, 6000) * smooth(t / .01) * np.exp(-t / .18), 0, .8)
    m.add(np.sin(2 * np.pi * np.cumsum(150 * np.exp(-t / .03) + 50) / SR) * np.exp(-t / .1), 0, .6)
    rng = np.random.default_rng(34)
    for i in range(9): m.add(drop(i, rng.uniform(500, 1100)), .15 + rng.uniform(0, .7), .25)
    return m.render(1, .8)


def sfx_squad():
    """관군이 몰려온다: 북 세 번과 나발."""
    m = sfx(2.4)
    for i, at in enumerate((0, .3, .6)): m.add(buk(1, 35 + i), at, .9)
    m.add(horn(hz(midi("G3")), 1.2, 1, 36), .6, .9); m.add(horn(hz(midi("D4")), .8, .8, 37), 1.1, .6)
    m.reverb(.3, 1.5, 11)
    return m.render(1, .85)


def sfx_gameover():
    """쓰러짐: 가야금이 계면으로 무너져 내리고 낮은 징이 남는다."""
    m = sfx(4)
    m.notes(gayageum, r"E4:1 D4:1\ C4:1 A3:4v", 0, .42, .9, seed=38)
    m.add(jing(.8, 39, 73), .2, .6)
    m.add(drone(55, 3.5, .8, 40), 0, .6)
    m.reverb(.35, 2., 12)
    return m.render(1, .8)


def sfx_levelup():
    """레벨업: 가야금이 평조로 시원하게 훑어 올라가고 종이 맺는다."""
    m = sfx(1.6)
    for i, n in enumerate(("G4", "A4", "C5", "D5", "E5", "G5", "A5")):
        m.add(gayageum(hz(midi(n)), .35, .8, seed=100 + i), .045 * i, .6)
    m.add(bell(hz(midi("G6")), .7, 107, 1.2), .33, .4)
    m.reverb(.2, 1., 13)
    return m.render(1, .8)


def sfx_rare():
    """귀한 무기를 얻는다: 꽹과리 한 번, 가야금 화음을 훑고 종이 길게 운다."""
    m = sfx(2.2)
    m.add(kkwaeng(1, 110), 0, .6)
    for i, n in enumerate(("D4", "A4", "D5", "E5", "A5")):
        m.add(gayageum(hz(midi(n)), 1.2, .8, seed=111 + i), .08 + .03 * i, .5)
    m.add(bell(hz(midi("D6")), .8, 116, 2), .15, .5); m.add(bell(hz(midi("A6")), .6, 117, 1.6), .25, .35)
    m.reverb(.25, 1.4, 14)
    return m.render(1, .85)


# ── 무기: 귀한 무기는 저마다 소리가 다르다(환도·활은 팀원 소리)

def swish(length, seed, f0, f1, width=.5):
    n = int(length * SR); t = seconds(n)
    return sweep_noise(n, seed, f0, f1, width) * np.sin(np.pi * np.clip(t / length, 0, 1)) ** 1.5


def tv_band(n, seed, centre, width, taps=255, hop=256):
    """가운데 주파수가 centre(표본마다) 곡선을 따라 움직이는 띠 잡음. 짧은 토막마다 거르고 한 창으로 겹쳐 잇는다."""
    src = noise(n + 4 * hop, seed); out = np.zeros(n + 4 * hop); win = np.hanning(2 * hop)
    for s0 in range(0, n + 2 * hop, hop):
        c = centre[min(n - 1, max(0, s0 - hop))]
        lo, hi = max(30, c * (1 - width)), min(NYQ - 500, c * (1 + width))
        out[s0:s0 + 2 * hop] += bandpass(src[s0:s0 + 2 * hop], lo, hi, taps) * win
    return out[hop:hop + n]


def blade(length, seed, lo, hi, peak=.55, body=.5, whistle=.25, whistle_hz=(700, 1500), rustle=.3):
    """칼을 휘두르는 바람 소리(장난감 같은 맑은 음 없이 바람만으로).
    날이 가장 빨리 지나는 peak(비율)에서 가장 크고 가장 높다: 천천히 차올라 짧게 빠진다.
    air: 넓은 띠의 바람이 lo→hi로 올라갔다 내려옴. whistle: 날이 공기를 가르는 좁은 띠의 쉭 소리.
    body: 무거운 칼일수록 큰 낮은 바람(웅). rustle: 휘두르기 직전 소매와 몸이 움직이는 소리."""
    n = int(length * SR); t = seconds(n) / length
    e = np.where(t < peak, (t / peak) ** 2.2, np.exp(-(t - peak) / (1 - peak) * 4.5))
    centre = lo + (hi - lo) * e ** .8
    air = tv_band(n, seed, centre, .75) * e
    whistle_c = whistle_hz[0] + (whistle_hz[1] - whistle_hz[0]) * e
    sing = tv_band(n, seed + 1, whistle_c, .06, 1023, 1024) * e ** 2
    low = lowpass(noise(n, seed + 2), 260, 255) * e ** 1.3
    x = air + whistle * sing / max(1e-9, np.abs(sing).max()) * np.abs(air).max() + body * low / max(1e-9, np.abs(low).max()) * np.abs(air).max()
    k = int(min(.09, length * .3) * SR)
    cloth = bandpass(noise(k, seed + 3), 250, 2500) * np.sin(np.pi * np.linspace(0, 1, k)) ** 2
    x[:k] += rustle * cloth / np.abs(cloth).max() * np.abs(air).max()
    return x


def ring(length, seed, bands, tau):
    """쇠가 떠는 여운을 잡음으로(맑은 사인이 아니라 좁은 띠 잡음이라 장난감처럼 들리지 않는다)."""
    n = int(length * SR); t = seconds(n); x = np.zeros(n)
    for i, (f, a) in enumerate(bands):
        x += a * bandpass(noise(n, seed + i), f * .985, f * 1.015, 2047)
    return x * np.exp(-t / tau) * smooth(t / .004)


def sfx_w_myeonggeom(seed=120, length=.42):
    """명검(장수의 긴 칼): 크고 무겁게 공기를 가른다. 낮은 바람(웅)이 두껍고, 끝에 아주 엷은 쇠 떨림."""
    m = sfx(1.)
    m.add(blade(length, seed, 180, 1400, peak=.45, body=.9, whistle=.18, whistle_hz=(500, 1100), rustle=.35), 0, 1)
    m.add(ring(.6, seed + 10, ((1850, 1), (2710, .6), (4060, .35)), .18), length * .45, .05)
    m.reverb(.12, .7, 15)
    return m.render(1, .9)


def sfx_w_myeonggeom_2(): return sfx_w_myeonggeom(130, .39)


def sfx_w_saingeom(seed=140, length=.32):
    """사인검: 곧고 날카롭게 베는 바람(쉭), 날이 공기를 가르는 높은 소리가 또렷하고, 끝에 쇠가 맑게 떤다."""
    m = sfx(1.2)
    m.add(blade(length, seed, 350, 2600, peak=.45, body=.45, whistle=.32, whistle_hz=(900, 2100), rustle=.25), 0, 1)
    m.add(ring(.9, seed + 10, ((2350, 1), (3480, .7), (5260, .4)), .28), length * .45, .1)
    m.reverb(.15, .9, 16)
    return m.render(1, .85)


def sfx_w_saingeom_2(): return sfx_w_saingeom(150, .3)


def sfx_w_byeokjo():
    """벽조궁(벼락 맞은 대추나무 활): 시위가 퉁 울리고 지지직 번개가 튄다."""
    m = sfx(1.); n = int(.5 * SR); t = seconds(n)
    f = 120 * (1 + .25 * np.exp(-t / .02))
    twang = sum(np.sin(k * 2 * np.pi * np.cumsum(f) / SR) / k ** 1.3 for k in range(1, 14)) * np.exp(-t / .12)
    m.add(lowpass(twang, 3000), 0, .8)
    buzz = np.sign(np.sin(2 * np.pi * 60 * t)) * (noise(n, 126) > .2) * highpass(noise(n, 127), 2000, 63)
    m.add(buzz * np.exp(-t / .15) * smooth(t / .01), .02, .35)
    rng = np.random.default_rng(128)
    for i in range(10):
        c = highpass(noise(int(.006 * SR), 130 + i), 3000, 31)
        m.add(c, rng.uniform(.02, .35), rng.uniform(.3, .7))
    return m.render(1, .85)


def sfx_w_dagger(seed=160, length=.2):
    """단검: 짧고 빠르게 휙. 무게 없이 높은 바람만."""
    m = sfx(.5)
    m.add(blade(length, seed, 700, 4200, peak=.4, body=.08, whistle=.2, whistle_hz=(1500, 3200), rustle=.2), 0, 1)
    m.reverb(.08, .4, 17)
    return m.render(1, .65)


def sfx_w_dagger_2(): return sfx_w_dagger(170, .18)


# ══════════════════ 보스의 공격 ══════════════════
# 공격마다 그 보스다운 소리: 도깨비는 몽둥이와 짐승 같은 몸, 콩쥐 요괴는 독에 낀 두꺼비의 축축한 소리,
# 좌의정(어둑시니)은 뼈마디, 국새, 먹, 그림자, 엽전. 준비 동작이 긴 것은 준비할 때 한 번, 맞히는 순간 한 번 난다.

def growl(length, f0, f1, seed, rough=31., bright=1200, harmonics=12):
    """짐승의 으르렁: 톱니 같은 목소리가 f0→f1로 미끄러지고 거칠게 떤다."""
    n = int(length * SR); t = seconds(n)
    f = f0 * (f1 / f0) ** (t / length)
    phase = 2 * np.pi * np.cumsum(f * (1 + .035 * np.sin(2 * np.pi * rough * t))) / SR
    x = sum(np.sin(k * phase) / k for k in range(1, harmonics))
    x *= .75 + .25 * np.sign(np.sin(2 * np.pi * rough * .5 * t))
    x = lowpass(x, bright) + bandpass(noise(n, seed), 200, bright * 1.5) * .25
    return x * smooth(t / .04) * smooth((length - t) / .15)


def thump(length, f_hi, f_lo, tau, seed, grit=.6):
    """땅이나 나무를 세게 치는 쿵: 음이 빠르게 내려앉고 흙 부서지는 잡음이 얹힌다."""
    n = int(length * SR); t = seconds(n)
    f = f_lo + (f_hi - f_lo) * np.exp(-t / .03)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / tau)
    x += lowpass(noise(n, seed), 900, 63) * np.exp(-t / (tau * .4)) * grit
    return x


def clicks(length, count, seed, lo, hi, tick=.004, start=0.):
    """잘게 흩어지는 딸깍들(자갈, 콩알, 뼈마디)."""
    n = int(length * SR); out = np.zeros(n); rng = np.random.default_rng(seed); k = int(tick * SR)
    for i in range(count):
        c = bandpass(noise(k, seed + 10 + i), lo, hi, 31) * np.exp(-seconds(k) / (tick / 3))
        at = int(rng.uniform(start, length - tick) * SR)
        out[at:at + k] += c * rng.uniform(.4, 1)
    return out


def sfx_dk_growl():
    """도깨비가 몽둥이를 치켜든다: 낮게 그르렁."""
    m = sfx(.8)
    m.add(growl(.55, 85, 110, 200, 27, 1000), 0, .9)
    m.add(swish(.45, 201, 300, 900, .6), .1, .25)
    m.reverb(.12, .6, 30)
    return m.render(1, .6)


def sfx_dk_club():
    """쇠 박힌 몽둥이가 내려친다: 짧은 바람 끝에 나무가 땅을 쿵, 흙이 튄다."""
    m = sfx(1.)
    m.add(blade(.12, 202, 160, 900, peak=.8, body=.9, whistle=.05, rustle=0), 0, .7)
    m.add(thump(.6, 140, 52, .16, 203, .8), .09, 1)
    m.add(deok(.6, 204), .09, .5)
    m.add(clicks(.4, 14, 205, 900, 4000, start=.02), .1, .35)
    m.reverb(.15, .7, 31)
    return m.render(1, .85)


def sfx_dk_charge():
    """도깨비의 돌진: 울부짖으며 땅을 쿵쿵 박차고 달려든다."""
    m = sfx(1.1)
    m.add(growl(.8, 95, 150, 206, 33, 1500), 0, .8)
    for i in range(5): m.add(buk(.75 - .07 * i, 207 + i, 1.4), .02 + i * .15, .5)
    m.add(lowpass(noise(int(.8 * SR), 212), 700, 63) * np.sin(np.pi * seconds(int(.8 * SR)) / .8), 0, .25)
    m.reverb(.15, .7, 32)
    return m.render(1, .85)


def sfx_dk_leap():
    """도깨비가 뛰어오른다: 힘쓰는 소리와 솟구치는 바람."""
    m = sfx(.9)
    m.add(growl(.25, 120, 90, 213, 40, 1100), 0, .7)
    m.add(swish(.6, 214, 250, 1400, .7), .05, .6)
    m.reverb(.12, .6, 33)
    return m.render(1, .7)


def sfx_dk_land():
    """도깨비가 내려찍는다: 땅이 울리는 큰 쿵, 낮은 울림이 남고 자갈이 흩어진다."""
    m = sfx(1.8)
    m.add(buk(1, 215, .8), 0, 1)
    m.add(thump(1.2, 110, 38, .45, 216, .9), 0, .9)
    m.add(lowpass(noise(int(1.2 * SR), 217), 260, 255) * np.exp(-seconds(int(1.2 * SR)) / .5) * smooth(seconds(int(1.2 * SR)) / .02), 0, .6)
    m.add(clicks(.7, 26, 218, 700, 3500, start=.03), .02, .35)
    m.reverb(.2, 1., 34)
    return m.render(1, .9)


def sfx_kong_spit():
    """젓갈 침: 목구멍이 꾸르륵 하고 축축하게 퉤."""
    m = sfx(.8)
    m.add(toad(220)[:int(.25 * SR)], 0, .6)
    n = int(.25 * SR); t = seconds(n)
    m.add(bandpass(noise(n, 221), 500, 3200, 63) * np.exp(-t / .05) * smooth(t / .006), .2, .9)
    m.add(drop(222, 500), .22, .4); m.add(drop(223, 700), .28, .3)
    m.reverb(.12, .5, 35)
    return m.render(1, .75)


def sfx_kong_beans():
    """콩알 산탄: 볼을 부풀려 푸 하고 마른 콩이 우르르 튄다."""
    m = sfx(.8)
    n = int(.18 * SR); t = seconds(n)
    m.add(lowpass(noise(n, 224), 900, 63) * np.exp(-t / .05) * smooth(t / .01), 0, .8)
    m.add(clicks(.4, 26, 225, 1800, 6000, .006, .02), .03, .7)
    m.reverb(.1, .5, 36)
    return m.render(1, .7)


def sfx_kong_tongue():
    """혀 채찍: 축축하게 쭉 뻗어 짝 때리고 후루룩 감겨 들어간다."""
    m = sfx(.9)
    m.add(swish(.12, 226, 500, 2600, .5), 0, .7)
    k = int(.01 * SR)
    m.add(highpass(noise(k, 227), 1500, 31) * np.exp(-seconds(k) / .002), .11, .45)
    m.add(thump(.15, 300, 120, .04, 228, .3), .11, .4)
    m.add(swish(.4, 229, 2200, 350, .45), .2, .35)
    m.reverb(.1, .5, 37)
    return m.render(1, .8)


def sfx_kong_salt():
    """소금 비: 두꺼비가 울고, 하늘에서 소금이 쏴아 하고 흩뿌려진다."""
    m = sfx(1.6)
    m.add(toad(230), 0, .5)
    n = int(1.1 * SR); t = seconds(n)
    m.add(highpass(noise(n, 231), 4000, 63) * smooth(t / .3) * np.exp(-t / .6), .3, .35)
    m.add(clicks(1., 60, 232, 4000, 9000, .003, .1), .35, .45)
    m.reverb(.18, .9, 38)
    return m.render(1, .7)


def sfx_kong_land():
    """독째로 쿵 내려앉는다: 둔한 옹기 소리."""
    m = sfx(.7)
    n = int(.5 * SR); t = seconds(n)
    clay = np.sin(2 * np.pi * np.cumsum(150 + 60 * np.exp(-t / .02)) / SR) * np.exp(-t / .09)
    clay += .4 * np.sin(2 * np.pi * np.cumsum(410 + 90 * np.exp(-t / .02)) / SR) * np.exp(-t / .05)
    m.add(clay, 0, .9)
    m.add(thump(.4, 90, 45, .12, 233, .5), 0, .6)
    return m.render(1, .65)


def sfx_kong_cast():
    """콩쥐 요괴가 소매를 들어 마을을 묶는다: 두꺼비 울음이 겹겹이 번지고 해금이 비틀려 올라간다."""
    m = sfx(2.2)
    for i in range(4): m.add(toad(234 + i), .12 * i, .45)
    m.add(haegeum(hz(midi("E4")), 1.2, .7, bend=2, seed=238), .2, .4)
    m.add(drone(55, 1.6, 1, 239), 0, .7)
    m.reverb(.25, 1.2, 39)
    return m.render(1, .8)


def sfx_fb_sweep():
    """어둑시니의 팔이 땅을 휩쓴다: 마디마다 우드득 꺾이며 길게 끌리는 낮은 바람."""
    m = sfx(1.)
    m.add(swish(.55, 240, 140, 700, .7), 0, .9)
    m.add(clicks(.5, 16, 241, 300, 1500, .008), 0, .8)
    m.add(lowpass(noise(int(.5 * SR), 242), 200, 255) * np.sin(np.pi * seconds(int(.5 * SR)) / .5), 0, .5)
    m.reverb(.2, .8, 40)
    return m.render(1, .8)


def sfx_fb_raise():
    """국새를 머리 위로 든다: 낮은 울림이 차오른다."""
    m = sfx(1.2); n = int(.95 * SR); t = seconds(n)
    m.add(drone(49, 1.1, 1, 243, fade=.2) * 2.5, 0, .8)
    m.add(sweep_noise(n, 244, 200, 1800, .6) * (t / .95) ** 2, 0, .4)
    m.reverb(.2, .8, 41)
    return m.render(1, .5)


def sfx_fb_slam():
    """국새를 내려찍는다: 돌 같은 인장이 땅을 쾅, 북과 징이 함께 운다."""
    m = sfx(3.)
    m.add(buk(1, 245, .7), 0, 1); m.add(thump(.8, 160, 45, .3, 246, 1), 0, .8)
    m.add(chime(330, 1, 247), 0, .35)
    m.add(jing(.9, 248, 73), .02, .55)
    m.add(clicks(.5, 20, 249, 600, 3000, start=.02), .01, .3)
    m.reverb(.28, 1.4, 42)
    return m.render(1, .9)


def sfx_fb_ink():
    """붓을 휘둘러 먹물을 뿌린다: 붓털이 쉭, 먹물이 촤악 흩날린다."""
    m = sfx(.8)
    m.add(swish(.25, 250, 600, 2600, .6), 0, .7)
    n = int(.35 * SR); t = seconds(n)
    m.add(bandpass(noise(n, 251), 700, 4000, 63) * np.exp(-t / .1) * smooth(t / .02), .15, .5)
    m.reverb(.12, .5, 43)
    return m.render(1, .7)


def sfx_fb_ink_drop():
    """먹물 덩이가 떨어져 철퍽."""
    n = int(.3 * SR); t = seconds(n)
    x = bandpass(noise(n, 252), 250, 2000, 63) * np.exp(-t / .045) * smooth(t / .003)
    x += np.sin(2 * np.pi * np.cumsum(220 * np.exp(-t / .05) + 90) / SR) * np.exp(-t / .07) * .6
    return x / np.abs(x).max() * .6


def sfx_fb_hands():
    """그림자 웅덩이가 번진다: 여럿이 숨죽여 속삭인다."""
    m = sfx(1.4); n = int(1.1 * SR); t = seconds(n)
    for i, (lo, hi) in enumerate(((600, 1400), (900, 2300), (1500, 3500))):
        voice = bandpass(noise(n, 253 + i), lo, hi, 127) * (.5 + .5 * np.sin(2 * np.pi * (5 + 2 * i) * t + i)) ** 2
        m.add(voice * np.sin(np.pi * t / 1.1), .08 * i, .5)
    m.add(drone(44, 1.2, 1, 256, fade=.3), 0, .6)
    m.reverb(.3, 1.2, 44)
    return m.render(1, .6)


def sfx_fb_hand():
    """땅속에서 손이 쑥 솟아 움켜쥔다."""
    m = sfx(.6)
    m.add(thump(.3, 120, 50, .08, 257, .7), 0, .8)
    m.add(swish(.18, 258, 300, 1500, .6), 0, .5)
    m.add(clicks(.15, 5, 259, 400, 1800, .006), .1, .5)
    return m.render(1, .6)


def sfx_fb_coins():
    """갈라진 얼굴이 엽전을 토해 낸다: 우웩 하는 신음과 놋쇠 엽전이 와르르."""
    m = sfx(1.2)
    m.add(growl(.4, 140, 80, 260, 19, 900), 0, .6)
    rng = np.random.default_rng(261)
    for i in range(14):
        f = rng.uniform(2800, 3900)
        m.add(metal(f, [1, 1.52, 2.33, 3.1], [1, .6, .4, .2], [.1, .07, .05, .03], .35, 1, 262 + i, .5), .1 + rng.uniform(0, .45), rng.uniform(.3, .7))
    m.reverb(.15, .7, 45)
    return m.render(1, .8)


BOSS_ATTACKS = [sfx_dk_growl, sfx_dk_club, sfx_dk_charge, sfx_dk_leap, sfx_dk_land, sfx_kong_spit, sfx_kong_beans,
                sfx_kong_tongue, sfx_kong_salt, sfx_kong_land, sfx_kong_cast, sfx_fb_sweep, sfx_fb_raise, sfx_fb_slam,
                sfx_fb_ink, sfx_fb_ink_drop, sfx_fb_hands, sfx_fb_hand, sfx_fb_coins]

JOBS = [bgm_pass, bgm_hill, bgm_kong_fight, bgm_fort, bgm_station, bgm_grave, bgm_city, bgm_hall, bgm_manor,
        bgm_final_fight, bgm_ending, sfx_charm, sfx_keepsake, sfx_beacon, sfx_beacon_wrong, sfx_gate, sfx_purify,
        sfx_boss_die, sfx_transform, sfx_dark, sfx_hit, sfx_hurt, sfx_coin, sfx_heal, sfx_ghost_die, sfx_mob_die,
        sfx_alarm, sfx_splash, sfx_squad, sfx_gameover, sfx_levelup, sfx_rare, sfx_w_myeonggeom, sfx_w_myeonggeom_2,
        sfx_w_saingeom, sfx_w_saingeom_2, sfx_w_byeokjo, sfx_w_dagger, sfx_w_dagger_2]
JOBS += BOSS_ATTACKS

for job in JOBS:
    if ONLY and ONLY != job.__name__: continue
    save(job.__name__, job())
print(f"{len(JOBS)} sounds, {len(made)} written" + (": " + ", ".join(made) if made else ""))
