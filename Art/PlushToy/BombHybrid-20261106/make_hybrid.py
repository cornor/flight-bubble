# Bubble bomb: hybrids of the Mixkit 8-bit bomb (2811) and cartoon bubbles popping (732), plus a round synthetic boom.
import numpy as np, wave, subprocess, re, sys
from scipy.signal import butter, sosfilt, resample
SR = 44100; W = '/home/claude/web/hybrid/'; OUT = sys.argv[1]
def load(n):
    w = wave.open(W + n + '.wav'); return np.frombuffer(w.readframes(w.getnframes()), '<i2') / 32768
def save(p, y):
    with wave.open(p, 'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(SR); f.writeframes((np.clip(y, -1, 1) * 32767).astype('<i2').tobytes())
def lufs(y):
    save('/tmp/_m.wav', y)
    e = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', '/tmp/_m.wav', '-af', 'ebur128', '-f', 'null', '-'], capture_output=True, text=True).stderr
    return float(re.findall(r'I:\s+(-?[\d.]+) LUFS', e)[-1])
def lp(x, f): return sosfilt(butter(4, f, 'low', fs=SR, output='sos'), x)
def hp(x, f): return sosfilt(butter(2, f, 'high', fs=SR, output='sos'), x)
def t(d): return np.arange(int(SR * d)) / SR
def shape(x, hold, decay):          # keep the hit, then a faster fade than the 8-bit tail
    k = t(len(x) / SR); e = np.where(k < hold, 1, np.exp(-(k - hold) / decay)); return x * e
def boom(d=.45, f0=420, f1=105, fall=.02, decay=.11):   # the round toy "嘭" (as in version A of 11-05)
    k = t(d); f = f1 + (f0 - f1) * np.exp(-k / fall); ph = 2 * np.pi * np.cumsum(f) / SR
    return (np.sin(ph) + .25 * np.sin(2 * ph) * np.exp(-k / .1)) * np.exp(-k / decay) * np.clip(k / .002, 0, 1)
def pitch(x, semis): return resample(x, int(len(x) / 2 ** (semis / 12)))
def put(y, x, at, g):
    s = int(at * SR); m = min(len(x), len(y) - s); y[s:s + m] += x[:m] * g
def finish(y, target=-17.5):
    n = int(SR * .06); y = y.copy(); y[-n:] *= np.linspace(1, 0, n) ** 2
    y = hp(y, 45)
    for _ in range(5):
        y *= 10 ** ((target - lufs(y)) / 20)
        y = .89 * np.tanh(y / .89)   # soft limiter: the boom's peak no longer holds the loudness down
    return y
bomb, bub = load('2811-8bit-bomb-explosion'), load('732-cartoon-bubbles-popping')
V = {}
# 1 嘭为主：8-bit 爆炸剪短变软 + 圆润的嘭打底，后面几颗泡泡
y = np.zeros(int(SR * 1.0)); b = shape(lp(bomb[:int(SR * 1.0)], 5000), .12, .18)
put(y, b, 0, .8); put(y, boom(), 0, .55); put(y, bub, .16, .45); V['1-嘭为主'] = y
# 2 一半一半：爆炸更短，泡泡更多（原声 + 升调一份）
y = np.zeros(int(SR * 1.05)); b = shape(lp(bomb[:int(SR * .8)], 5000), .08, .12)
put(y, b, 0, .75); put(y, boom(), 0, .45); put(y, bub, .08, .8); put(y, pitch(bub, 4), .3, .5); V['2-一半一半'] = y
# 3 最软：爆炸去掉更多高频、更短，嘭和泡泡为主
y = np.zeros(int(SR * .95)); b = shape(lp(bomb[:int(SR * .7)], 2500), .06, .1)
put(y, b, 0, .7); put(y, boom(.5, 380, 95, .025, .13), 0, .7); put(y, bub, .1, .75); put(y, pitch(bub, 2), .32, .4); V['3-最软'] = y
# 4 原味：8-bit 爆炸几乎不加工，只剪到 1 秒自然收尾，中间夹泡泡
y = np.zeros(int(SR * 1.1)); b = shape(bomb[:int(SR * 1.1)], .35, .22)
put(y, b, 0, .8); put(y, bub, .2, .55); V['4-原味8bit'] = y
for name, y in V.items():
    y = finish(y); p = f'{OUT}/炸弹混合-{name}'
    save(p + '.wav', y)
    subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', p + '.wav', '-c:a', 'libmp3lame', '-q:a', '2', p + '.mp3'], check=True)
    print(name, round(len(y) / SR, 2), 's', lufs(y), 'LUFS')
