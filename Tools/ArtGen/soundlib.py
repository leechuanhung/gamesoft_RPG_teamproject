# 소리 만들기 도구: 국악기를 흉내 낸 합성 악기, 장단, 섞기, 되풀이 이음매 없는 배경 음악, wav 쓰기.
# 모두 numpy만 쓰고 난수는 고정 시드라서 같은 소리가 다시 나온다(make_sound.py).
import os, wave
import numpy as np

SR = 44100
NYQ = SR / 2


def seconds(n): return np.arange(n) / SR


def noise(n, seed):
    return np.random.default_rng(seed).uniform(-1, 1, n)


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


# ── 거르개(FIR이라 되풀이 계산 없이 numpy로 한 번에)
_kernels = {}


def lowpass(x, cut, taps=127):
    key = (round(cut), taps)
    if key not in _kernels:
        m = np.arange(taps) - (taps - 1) / 2
        k = np.sinc(2 * cut / SR * m) * np.hamming(taps)
        _kernels[key] = k / k.sum()
    return np.convolve(x, _kernels[key], mode="same")


def highpass(x, cut, taps=127): return x - lowpass(x, cut, taps)
def bandpass(x, lo, hi, taps=127): return lowpass(x, hi, taps) - lowpass(x, lo, taps)


def sweep_noise(n, seed, f0, f1, width=.6):
    """가운데가 f0에서 f1로 옮겨 가는 띠 잡음(휘두름, 바람, 불길). 짧은 토막마다 거르고 겹쳐 잇는다."""
    hop = 512; src = noise(n + 4 * hop, seed); out = np.zeros(n + 4 * hop); win = np.hanning(2 * hop)
    for s in range(0, n + 2 * hop, hop):
        c = f0 + (f1 - f0) * min(1, s / max(1, n - 1))
        band = bandpass(src[s:s + 2 * hop], max(40, c * (1 - width)), min(NYQ - 500, c * (1 + width)), 63)
        out[s:s + 2 * hop] += band * win
    return out[hop:hop + n]


# ── 음 높이
NAMES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(name):
    n = NAMES[name[0]]; i = 1
    while i < len(name) and name[i] in "#b":
        n += 1 if name[i] == "#" else -1; i += 1
    return n + 12 * (int(name[i:]) + 1)


def hz(m): return 440 * 2 ** ((m - 69) / 12)


def pitch_curve(f, n, dur, vib=0., bend=0., scoop=0., rate=5.5, settle=.25):
    """음마다 주파수 곡선: 처음에 아래에서 올라붙고(scoop), 끝 무렵 꺾이며(bend, 꺾는음), 지나면 떤다(vib, 농현)."""
    t = seconds(n)
    semis = -scoop * (1 - smooth(t / .09))
    if bend: semis = semis + bend * smooth((t - dur * .55) / max(.05, dur * .35))
    if vib: semis = semis + vib * smooth((t - dur * settle) / .3) * np.sin(2 * np.pi * rate * t)
    return f * 2 ** (semis / 12)


def additive(freq, amps, taus, n, attack=.004, phase_seed=0):
    """배음을 더한다. freq는 곡선, amps·taus는 배음마다 크기와 줄어드는 시간(None이면 그대로)."""
    t = seconds(n); phase = 2 * np.pi * np.cumsum(freq) / SR
    rng = np.random.default_rng(phase_seed); out = np.zeros(n)
    top = freq.max()
    for k, (a, tau) in enumerate(zip(amps, taus), 1):
        if a == 0 or k * top > NYQ - 800: continue
        e = np.exp(-t / tau) if tau else 1
        out += a * e * np.sin(k * phase + rng.uniform(0, 2 * np.pi))
    out *= smooth(t / attack) if attack else 1
    return out


def release(x, start, length):
    """start초부터 length초 동안 줄여 끝낸다."""
    t = seconds(len(x)); return x * (1 - smooth((t - start) / length))


# ── 가락 악기. (주파수, 길이 초, 세기, 농현, 꺾음, 올려붙임) → 소리(길이 뒤 여운 포함)

def gayageum(f, dur, vel=1., vib=0., bend=0., scoop=0., seed=0):
    """가야금: 뜯은 뒤 배음이 빨리 사라지고, 길게 끄는 음은 농현으로 떤다."""
    n = int((dur + .9) * SR)
    fr = pitch_curve(f, n, dur, vib, bend, scoop, rate=5, settle=.3)
    ks = np.arange(1, 11)
    amps = np.abs(np.sin(np.pi * ks * .2)) / ks ** 1.05
    taus = 1.3 / (1 + .45 * (ks - 1))
    x = additive(fr, amps, taus, n, .003, seed)
    pluck = highpass(noise(int(.02 * SR), seed + 7), 1500, 31) * np.exp(-seconds(int(.02 * SR)) / .004)
    x[:len(pluck)] += pluck * .25
    return release(x, dur + .15, .6) * vel * .55


def daegeum(f, dur, vel=1., vib=0., bend=0., scoop=0., seed=0):
    """대금: 바람 소리 섞인 둥근 소리, 붙을 때 살짝 아래에서 올라온다."""
    n = int((dur + .25) * SR)
    fr = pitch_curve(f, n, dur, .22 + vib, bend, scoop + .5, rate=4.8)
    x = additive(fr, [1, .22, .12, .05], [None] * 4, n, .07, seed)
    breath = bandpass(noise(n, seed + 3), min(f * 1.2, 9000), min(f * 4, 15000), 63)
    t = seconds(n)
    x += breath * (.10 + .12 * np.exp(-t / .08))
    return release(x, dur, .2) * vel * .5


def haegeum(f, dur, vel=1., vib=0., bend=0., scoop=0., seed=0):
    """해금: 활로 긋는 코맹맹이 소리, 떨림이 크고 미끄러진다."""
    n = int((dur + .2) * SR)
    fr = pitch_curve(f, n, dur, .3 + vib, bend, scoop + .3, rate=6, settle=.2)
    ks = np.arange(1, 16)
    amps = 1 / ks * (1 + 1.2 * np.exp(-((ks - 4) / 1.6) ** 2))
    x = additive(fr, amps, [None] * len(ks), n, .05, seed)
    bow = bandpass(noise(n, seed + 5), 2000, 6000, 63) * .05
    x = lowpass(x + bow, 5000, 63)
    return release(x, dur, .15) * vel * .32


def piri(f, dur, vel=1., vib=0., bend=0., scoop=0., seed=0, bright=1.):
    """피리(밝게 하면 태평소): 겹서 소리, 굵게 떤다."""
    n = int((dur + .15) * SR)
    fr = pitch_curve(f, n, dur, .35 + vib, bend, scoop + .4, rate=4.5, settle=.3)
    ks = np.arange(1, 14)
    amps = np.where(ks % 2 == 1, 1., .55) / ks ** (1.25 - .35 * bright)
    x = additive(fr, amps, [None] * len(ks), n, .03, seed)
    x = lowpass(x, 3500 + 2500 * bright, 63)
    return release(x, dur, .1) * vel * .3


def horn(f, dur, vel=1., seed=0, **_):
    """나발: 낮은 금관 한 음, 천천히 부풀었다 가라앉는다."""
    n = int((dur + .4) * SR); t = seconds(n)
    fr = f * 2 ** ((.08 * np.sin(2 * np.pi * 3.3 * t)) / 12)
    ks = np.arange(1, 12)
    x = additive(fr, 1 / ks ** .9, [None] * len(ks), n, .25, seed)
    x = lowpass(x, 1800, 63) * (.6 + .4 * smooth(t / (dur * .5)))
    return release(x, dur, .35) * vel * .3


def drone(f, dur, vel=1., seed=0, fade=2.):
    """밑에 깔리는 낮은 울림(살짝 어긋난 사인 넷, 천천히 숨 쉰다). fade=0이면 판 전체에 깔아 되풀이 이음매가 없다
    (그때는 어긋남과 숨이 판 길이에 꼭 맞는 횟수만큼 돈다)."""
    n = int(round(dur * SR)); t = seconds(n); x = np.zeros(n)
    fit = (lambda hzv: round(hzv * dur) / dur) if not fade else (lambda hzv: hzv)
    for ratio, a, det in ((1, 1, .0), (1, .7, .35), (1.5, .35, -.2), (2, .3, .25)):
        x += a * np.sin(2 * np.pi * fit(f * ratio + det) * t + ratio)
    x *= .75 + .25 * np.sin(2 * np.pi * fit(.11) * t + seed)
    if fade: x *= smooth(t / fade) * smooth((dur - t) / fade)
    return x * vel * .25


def wind(dur, seed, lo=180, hi=900, vel=1., loop=True):
    """바람: 거른 잡음이 판 길이에 맞춰 몇 번 일었다 잦아든다."""
    n = int(round(dur * SR)); t = seconds(n)
    k = max(1, round(dur / 9))
    gust = .55 + .3 * np.sin(2 * np.pi * k * t / dur + seed) + .15 * np.sin(2 * np.pi * (2 * k + 1) * t / dur)
    return bandpass(noise(n, seed), lo, hi, 127) * gust * vel


# ── 타악기

def kung(vel=1., seed=0):
    """장구 궁편(쿵): 낮게 둥 울리고 음이 내려앉는다."""
    n = int(.6 * SR); t = seconds(n)
    f = 70 + 55 * np.exp(-t / .03)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / .22)
    x += lowpass(noise(n, seed), 400, 63) * np.exp(-t / .03) * .6
    return x * vel * .8


def deok(vel=1., seed=0):
    """장구 채편(덕): 채로 치는 마른 딱 소리."""
    n = int(.25 * SR); t = seconds(n)
    x = bandpass(noise(n, seed + 1), 1200, 7000, 63) * np.exp(-t / .018)
    x += np.sin(2 * np.pi * 380 * t) * np.exp(-t / .05) * .5
    return x * vel * .7


def roll(vel=1., seed=0):
    """더러러러: 채를 굴린다."""
    out = np.zeros(int(.4 * SR))
    for i in range(4):
        d = deok(vel * (.55 + .1 * (i % 2)), seed + i); at = int(i * .055 * SR)
        out[at:at + len(d)] += d[:len(out) - at]
    return out


def buk(vel=1., seed=0, low=1.):
    """북: 크고 깊게 둥."""
    n = int(1.2 * SR); t = seconds(n)
    f = (48 + 45 * np.exp(-t / .05)) * low
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / .4)
    x += .35 * np.sin(2 * np.pi * np.cumsum(f * 1.6) / SR) * np.exp(-t / .15)
    x += lowpass(noise(n, seed + 2), 600, 63) * np.exp(-t / .04) * .7
    return x * vel


def metal(f0, ratios, amps, taus, length, vel, seed, click=.3):
    n = int(length * SR); t = seconds(n); rng = np.random.default_rng(seed); x = np.zeros(n)
    for r, a, tau in zip(ratios, amps, taus):
        if f0 * r < NYQ - 500: x += a * np.sin(2 * np.pi * f0 * r * t + rng.uniform(0, 6.28)) * np.exp(-t / tau)
    x += highpass(noise(n, seed + 9), 2500, 31) * np.exp(-t / .006) * click
    return x * smooth(t / .001) * vel


def jing(vel=1., seed=0, f0=98.):
    """징: 낮게 웅 울리며 맥놀이가 길게 남는다."""
    n = int(4.5 * SR); t = seconds(n)
    x = metal(f0, [1, 1.006, 2.02, 2.98, 4.15, 5.4], [1, .7, .35, .2, .1, .05], [3, 3, 1.6, 1, .6, .4], 4.5, 1, seed, .05)
    x *= smooth(t / .03)
    return x * vel * .55


def kkwaeng(vel=1., seed=0, muted=False):
    """꽹과리: 밝고 쨍한 쇳소리. 막으면 짧게 깽."""
    tau = .05 if muted else .22
    return metal(1150, [1, 1.47, 2.09, 2.83, 3.61, 4.63], [1, .8, .6, .45, .3, .2], [tau, tau * .9, tau * .7, tau * .5, tau * .4, tau * .3],
                 .6 if not muted else .2, vel * .35, seed, .4)


def bell(f, vel=1., seed=0, length=3.):
    """종·풍경·요령: 맑은 쇳소리."""
    return metal(f, [1, 2.0, 2.76, 5.4, 8.93], [1, .45, .4, .18, .08], [length * .8, length * .5, length * .35, length * .15, length * .08],
                 length, vel * .4, seed, .1)


def chime(f, vel=1., seed=0):
    """편경: 돌을 쳐서 나는 단단하고 맑은 소리."""
    return metal(f, [1, 2.3, 3.2, 4.4], [1, .4, .25, .1], [1.6, .7, .4, .2], 2.2, vel * .45, seed, .15)


def pak(vel=1., seed=0):
    """박: 나무판을 모아 치는 딱 소리(음악의 시작과 끝)."""
    n = int(.35 * SR); t = seconds(n)
    x = bandpass(noise(n, seed + 4), 700, 4000, 63) * np.exp(-t / .02)
    x += (np.sin(2 * np.pi * 950 * t) + .7 * np.sin(2 * np.pi * 1420 * t)) * np.exp(-t / .045) * .5
    return x * vel


def both(a, b):
    out = np.zeros(max(len(a), len(b))); out[:len(a)] += a; out[:len(b)] += b; return out


DRUMS = {"D": lambda s: both(kung(1, s), deok(.9, s)), "K": lambda s: kung(1, s), "k": lambda s: kung(.55, s),
         "T": lambda s: deok(1, s), "t": lambda s: deok(.5, s), "r": lambda s: roll(.8, s),
         "B": lambda s: buk(1, s), "b": lambda s: buk(.55, s), "J": lambda s: jing(1, s),
         "G": lambda s: kkwaeng(1, s), "g": lambda s: kkwaeng(.7, s, True)}
_drums = {}


def drum(ch, i):
    """같은 북소리라도 넷 중 하나를 돌아가며 써서 기계처럼 똑같지 않게 한다."""
    key = (ch, i % 4)
    if key not in _drums: _drums[key] = DRUMS[ch](100 + 7 * (i % 4) + ord(ch))
    return _drums[key]


# ── 섞기

class Mix:
    """길이가 정해진 판에 소리를 얹는다. loop면 끝을 넘친 여운을 앞에 접어 이음매 없이 되풀이된다."""

    def __init__(self, length, loop=True, tail=8.):
        self.n = int(round(length * SR)); self.loop = loop
        self.buf = np.zeros(self.n + int(tail * SR))

    def add(self, sound, at, gain=1.):
        i = int(round(at * SR))
        if i >= len(self.buf) or i < 0: return
        m = min(len(sound), len(self.buf) - i)
        self.buf[i:i + m] += sound[:m] * gain

    def notes(self, inst, score, start, beat, gain=1., transpose=0, seed=0, **kw):
        """score 글: 'A4:2v C5:1 D5:1\\ r:2' — 음이름:박 뒤에 v(농현), \\(꺾음), /(올려붙임). r은 쉼표. 끝난 박을 돌려준다."""
        b = start
        for i, tok in enumerate(score.split()):
            name, _, rest = tok.partition(":")
            mods = rest.lstrip("0123456789.")
            length = float(rest[:len(rest) - len(mods)] or 1)
            if name != "r":
                f = hz(midi(name) + transpose); dur = length * beat
                self.add(inst(f, dur * .97, vib=.45 if "v" in mods else 0, bend=-2 if "\\" in mods else 0,
                              scoop=1.5 if "/" in mods else 0, seed=seed + i, **kw), b * beat, gain)
            b += length
        return b

    def rhythm(self, pattern, start, slot, gain=1., bars=1):
        """장단 글: 칸마다 한 글자(D 덩, K 쿵, T 덕, t 기, r 더러러, B 북, J 징, G 꽹과리, . 쉼)."""
        k = 0
        for bar in range(bars):
            for i, ch in enumerate(pattern):
                if ch in DRUMS: self.add(drum(ch, k), start + (bar * len(pattern) + i) * slot, gain); k += 1

    def reverb(self, wet=.25, length=1.2, seed=0):
        """여러 갈래로 늦게 돌아오는 메아리(되풀이 판이면 돌려 감아 이음매가 없다)."""
        dry = self.render_raw(); damp = lowpass(dry, 3500, 63)
        rng = np.random.default_rng(seed); out = dry.copy()
        for j in range(18):
            d = .021 + (length * (j + 1) / 18) * rng.uniform(.85, 1.15)
            g = wet * np.exp(-d / (length * .4)) * rng.uniform(.6, 1)
            s = int(d * SR)
            if self.loop: out += np.roll(damp, s) * g
            else: out[s:] += damp[:-s] * g
        self.done = out

    def render_raw(self):
        if self.loop:
            x = self.buf[:self.n].copy(); tail = self.buf[self.n:]
            k = 0
            while k < len(tail):
                m = min(self.n, len(tail) - k); x[:m] += tail[k:k + m]; k += m
            return x
        nz = np.nonzero(np.abs(self.buf) > 1e-5)[0]
        return self.buf[:nz[-1] + 1 if len(nz) else self.n].copy()

    def render(self, rms=.1, peak=.9):
        x = getattr(self, "done", None)
        if x is None: x = self.render_raw()
        x = x - x.mean()
        r = np.sqrt((x ** 2).mean()); p = np.abs(x).max()
        x = x * min(rms / max(r, 1e-9), peak / max(p, 1e-9))
        if not self.loop:
            fade = int(.01 * SR); x[-fade:] *= np.linspace(1, 0, fade)
        return x


def write(path, x):
    """16비트 모노 wav. 이미 있는 파일과 거의 같으면(2 이하 차이, 다른 컴퓨터의 소수점 오차) 건드리지 않는다."""
    data = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    if os.path.exists(path):
        try:
            with wave.open(path) as w:
                old = np.frombuffer(w.readframes(w.getnframes()), "<i2")
            if len(old) == len(data) and np.abs(old.astype(int) - data).max() <= 2: return False
        except (wave.Error, EOFError):
            pass
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    return True
