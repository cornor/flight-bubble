# Bubble-squad audio v1: every original cue re-made as bubbly, toy-like sounds, plus one looping theme.
import numpy as np, subprocess, os, json, wave
SR = 44100
rng = np.random.default_rng(7)
OUT = 'out'; os.makedirs(OUT, exist_ok=True)

def t(d): return np.arange(int(SR * d)) / SR
def env_exp(d, decay, attack=0.002):
    x = t(d); e = np.exp(-x / decay)
    a = np.clip(x / attack, 0, 1) if attack > 0 else 1
    return e * a
def sweep(f0, f1, d, curve=1.0):
    x = t(d); k = (x / d) ** curve
    f = f0 * (f1 / f0) ** k
    return 2 * np.pi * np.cumsum(f) / SR
def noise(d): return rng.standard_normal(int(SR * d))
def lowpass(x, fc):
    a = np.exp(-2 * np.pi * fc / SR); y = np.zeros_like(x); s = 0.0
    for i in range(len(x)): s = (1 - a) * x[i] + a * s; y[i] = s
    return y
def highpass(x, fc): return x - lowpass(x, fc)
def bandpass(x, lo, hi): return highpass(lowpass(x, hi), lo)
def mix(*parts):
    n = max(len(p[1]) + int(p[0] * SR) for p in parts); y = np.zeros(n)
    for off, sig, *g in parts:
        s = int(off * SR); y[s:s + len(sig)] += sig * (g[0] if g else 1.0)
    return y
def norm(y, peak=0.89):
    m = np.max(np.abs(y)) or 1; return y / m * peak
def fade_out(y, d=0.01):
    n = int(SR * d); y = y.copy(); y[-n:] *= np.linspace(1, 0, n); return y

# ---- instruments
def pop(f0=900, f1=1700, d=0.09, decay=0.035, click=0.25, bright=0.3):
    """A soap-bubble 'bloop': a sine whose pitch rises fast, plus a tiny wet click."""
    ph = sweep(f0, f1, d, 0.6)
    y = np.sin(ph) + bright * np.sin(2 * ph) * np.exp(-t(d) / (decay * .5))
    y *= env_exp(d, decay, 0.001)
    c = bandpass(noise(0.012), 2500, 9000) * env_exp(0.012, 0.003, 0) * click
    return mix((0, y), (0, c))
def marimba(f, d=0.6, vol=1.0):
    x = t(d)
    y = (np.sin(2*np.pi*f*x) * np.exp(-x/0.32) + 0.35*np.sin(2*np.pi*f*4*x)*np.exp(-x/0.05)
         + 0.12*np.sin(2*np.pi*f*9.2*x)*np.exp(-x/0.02))
    return y * np.clip(x/0.003, 0, 1) * vol
def glock(f, d=1.2, vol=1.0):
    x = t(d)
    y = (np.sin(2*np.pi*f*x)*np.exp(-x/0.55) + 0.45*np.sin(2*np.pi*f*2.76*x)*np.exp(-x/0.25)
         + 0.2*np.sin(2*np.pi*f*5.4*x)*np.exp(-x/0.12) + 0.08*np.sin(2*np.pi*f*8.93*x)*np.exp(-x/0.06))
    return y * np.clip(x/0.002, 0, 1) * vol
def pluck(f, d=0.5, damp=0.996, vol=1.0):
    n = int(SR * d); p = max(2, int(SR / f)); buf = rng.uniform(-1, 1, p); y = np.zeros(n)
    for i in range(n):
        y[i] = buf[i % p]; buf[i % p] = damp * 0.5 * (buf[i % p] + buf[(i + 1) % p])
    return lowpass(y, 5000) * vol
def bassnote(f, d=0.45, vol=1.0):
    x = t(d); y = np.sin(2*np.pi*f*x) + 0.3*np.sin(4*np.pi*f*x)
    return y * np.exp(-x/0.25) * np.clip(x/0.005, 0, 1) * vol
def woodblock(f=900, d=0.08):
    x = t(d); return (np.sin(2*np.pi*f*x) + .5*np.sin(2*np.pi*f*2.3*x)) * np.exp(-x/0.018)
def shaker(d=0.07): return bandpass(noise(d), 4000, 11000) * env_exp(d, 0.02, 0.004) * 0.5
def kick(d=0.25):
    ph = sweep(150, 50, d, 0.5); return np.sin(ph) * env_exp(d, 0.09, 0.001)
def sparkle(d=0.6, n=10, lo=2000, hi=5200, decay=0.08):
    parts = []
    for i in range(n):
        f = rng.uniform(lo, hi); o = rng.uniform(0, d * 0.7)
        parts.append((o, np.sin(2*np.pi*f*t(0.18)) * env_exp(0.18, decay * rng.uniform(.5, 1.2), 0.001), rng.uniform(.15, .35)))
    return mix(*parts)
def whoosh(d=0.35, lo=400, hi=3000, up=True):
    n = noise(d); x = t(d); k = x / d if up else 1 - x / d
    y = np.zeros_like(n); s = 0.0
    for i in range(len(n)):
        fc = lo * (hi / lo) ** k[i]; a = np.exp(-2*np.pi*fc/SR); s = (1-a)*n[i] + a*s; y[i] = s
    return highpass(y, 200) * np.sin(np.pi * x / d) ** 1.5
def boing(f0=300, d=0.32, up=2.2, wob=9):
    x = t(d); f = f0 * (1 + (up - 1) * np.exp(-x / 0.05)) * (1 + 0.08*np.sin(2*np.pi*wob*x)*np.exp(-x/0.12))
    f = f0 * up * np.exp(-x/0.06) + f0 * (1 + 0.1*np.sin(2*np.pi*wob*x)*np.exp(-x/0.15))
    ph = 2*np.pi*np.cumsum(f)/SR; return (np.sin(ph) + .25*np.sin(2*ph)) * env_exp(d, 0.12, 0.002)
def chord(fs, inst=glock, d=1.0, spread=0.0, vol=1.0):
    return mix(*[(i * spread, inst(f, d), vol / len(fs) * 1.6) for i, f in enumerate(fs)])
def note(n): return 440.0 * 2 ** ((n - 69) / 12)   # MIDI
C5, D5, E5, F5, G5, A5, B5, C6, D6, E6, G6, C7 = [note(n) for n in (72, 74, 76, 77, 79, 81, 83, 84, 86, 88, 91, 96)]
def formant_blip(f0, vowel, d=0.075, vol=1.0):
    # 2026-10-18 22:23 (user: the talking and the result-popup text sounded like noise). The old blip was a
    # band-passed 17-harmonic buzz; retriggered ~30 times a second it read as static. Now a soft rounded "pu/pi" tone:
    # sine + a little 2nd/3rd harmonic, a quick upward chirp then a gentle settle, 5 ms attack, ~25 ms decay.
    # The vowel only shifts the brightness a touch. ChipAudio.Garden also speaks only every third glyph.
    bright = {'a': .30, 'e': .40, 'o': .18}[vowel]
    x = t(d); f = f0 * (1 + 0.09 * np.exp(-x / 0.012) - 0.04 * x / d)
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) + bright * np.sin(2 * ph) + bright * .3 * np.sin(3 * ph)
    env = np.minimum(1, x / 0.005) * np.exp(-x / 0.025)
    env *= np.minimum(1, (d - x) / 0.008)
    return y * env * vol

S = {}
# ---- battle: pops and puffs
S['hit']        = pop(950, 1800, 0.08, 0.03)
S['hit-twin']   = mix((0, pop(900, 1700, 0.08, 0.03)), (0.045, pop(1150, 2100, 0.08, 0.028), .8))
S['hit-max']    = mix((0, pop(700, 1500, 0.12, 0.05, bright=.5)), (0.02, sparkle(0.25, 4, 3000, 6000), .5))
S['hit-rocket'] = mix((0, pop(260, 700, 0.22, 0.09, click=.6, bright=.6)), (0, bandpass(noise(.25), 800, 6000) * env_exp(.25, .06), .35),
                      (0.03, pop(1100, 2000, .07, .025), .5), (0.07, pop(1400, 2400, .07, .02), .4), (0.04, sparkle(.35, 5), .4))
for k, (a, b) in {'hit-needle': (1400, 2600), 'hit-quake': (380, 800), 'hit-blaze': (800, 1500), 'hit-arc': (1100, 2200)}.items():
    S[k] = pop(a, b, 0.1, 0.035)
def puff(f=650, d=0.06):   # gentle blow for a shot: breath + soft plink
    return mix((0, bandpass(noise(d), 900, 4000) * env_exp(d, 0.018, 0.004), .6), (0, pop(f, f * 1.25, d, 0.022, click=0, bright=0), .45))
S['shot']        = puff(650)
S['shot-twin']   = mix((0, puff(650)), (0.035, puff(760), .8))
S['shot-max']    = puff(820, 0.07)
S['shot-rocket'] = mix((0, whoosh(0.25, 600, 4000)), (0.0, boing(420, 0.25, 1.6), .5))
S['shot-needle'] = puff(900); S['shot-quake'] = puff(420, .09); S['shot-blaze'] = puff(700); S['shot-arc'] = puff(800)
# gentle bubbling stream while a member fires fast (looped)
loop = np.zeros(int(SR * 1.0))
for i in range(26):
    f = rng.uniform(700, 1700); p = pop(f, f * 1.6, 0.06, 0.02, click=.1) * rng.uniform(.2, .6); o = int(rng.uniform(0, 1.0) * SR)
    for j in range(len(p)): loop[(o + j) % len(loop)] += p[j]
S['laser-loop'] = loop
# 2026-10-18 23:09 (user picked B): deploying is a hop + landing: a small rising bloop on the tap and a soft round
# plop 0.30 s later, when the member lands on its pad (the flight takes 0.32 s). The noise generator state is restored
# afterwards so every later sound keeps its exact noise.
_rng_state = rng.bit_generator.state
S['launch'] = mix((0, pop(600, 1000, .08, .03, click=0, bright=0), .7), (.30, pop(240, 420, .12, .045, click=0, bright=.1)),
                  (.30, lowpass(noise(.06), 500) * env_exp(.06, .015), .25))
rng.bit_generator.state = _rng_state
S['land']   = mix((0, pop(200, 380, 0.14, 0.05, click=.4, bright=.2)), (0, lowpass(noise(.08), 700) * env_exp(.08, .02), .5))
S['arrival'] = mix((0, boing(420, .22, 1.7)), (.05, pop(1300, 2200, .06, .02), .4))
S['ready']  = chord([C6, E6, G6], glock, .6, .05, .9)
S['rush']   = whoosh(0.3, 500, 4500)
S['merge']  = mix((0, pop(800, 1400, .07, .025)), (.06, pop(1000, 1700, .07, .025)), (.12, pop(1250, 2100, .07, .025)),
                  (.17, glock(C6, .8), .7), (.17, glock(G6, .8), .45), (.2, sparkle(.4, 6), .35))
S['maxpower'] = mix(*[(i * .055, glock(f, .7), .55) for i, f in enumerate([C5, E5, G5, C6, E6, G6, C7])],
                    (.3, sparkle(.7, 14), .6), (0, whoosh(.5, 300, 5000), .35))
S['combo']      = mix(*[(i * .06, pop(f, f * 1.5, .08, .03), .8) for i, f in enumerate([900, 1150, 1450])])
S['combo-high'] = mix(*[(i * .055, pop(f, f * 1.5, .08, .03), .8) for i, f in enumerate([900, 1100, 1350, 1650, 2000])], (.27, glock(C7, .6), .5))
S['advance']  = mix((0, pop(180, 260, .18, .07, click=0, bright=.1)), (0, lowpass(noise(.12), 400) * env_exp(.12, .04), .3))
S['warning']  = mix((0, marimba(A5, .4)), (.16, marimba(E5, .5)), (.42, marimba(A5, .4), .8), (.58, marimba(E5, .5), .8))
S['critical'] = mix((0, marimba(note(57), .3), 1.0), (.17, marimba(note(57), .3), .7))
S['hurt']     = mix((0, boing(520, .4, .45, 12)), (0, pop(500, 300, .2, .06, bright=.2), .4))
S['deny']     = mix((0, woodblock(520, .1)), (.07, woodblock(390, .14), .8))
S['empty']    = mix((0, pop(500, 380, .12, .04)), (0, lowpass(noise(.15), 1500) * env_exp(.15, .05), .3))
S['blast']    = S['hit-rocket']
S['shuffle']  = mix(*[(i * .045, pop(rng.uniform(700, 1600), rng.uniform(1700, 2500), .06, .02), .6) for i in range(7)], (0, whoosh(.32, 600, 3000), .5))
S['recall']   = mix((0, whoosh(.25, 3000, 500, True)), (.15, pop(1200, 800, .08, .03), .5))
S['shockwave'] = mix((0, whoosh(.6, 300, 5000)), *[(.05 + i * .035, pop(rng.uniform(600, 1800), rng.uniform(1900, 2800), .07, .025), .5) for i in range(10)])
S['rescue-wave'] = mix((0, S['shockwave'], .9), (.1, chord([C6, E6, G6], glock, .9, .06, .8)))
S['purge'] = S['rescue-wave']; S['sacrifice'] = S['shockwave']
S['down']   = mix((0, np.sin(sweep(600, 120, .9, .8)) * env_exp(.9, .5, .01), .6), (0, bandpass(noise(.9), 500, 3000) * np.linspace(1, 0, int(SR * .9)), .25))
S['revive'] = mix(*[(i * .07, glock(f, .8), .6) for i, f in enumerate([C5, G5, C6, E6, G6])], (0, whoosh(.6, 300, 4000), .4), (.3, sparkle(.6, 12), .5))
S['assembly'] = S['merge']
S['legend-pulse'] = mix((0, glock(G6, .6), .6), (0, sparkle(.4, 6), .4))
# bubble bomb
S['super-lock']   = mix((0, glock(E6, .4), .7), (.09, glock(G6, .5), .7))
S['super-count']  = woodblock(1200, .06)
S['super-impact'] = mix((0, pop(180, 520, .35, .14, click=.8, bright=.7)), (0, bandpass(noise(.5), 600, 7000) * env_exp(.5, .12), .5),
                        *[(.03 + i * .03, pop(rng.uniform(700, 2000), rng.uniform(2100, 3000), .07, .025), .45) for i in range(12)], (.05, sparkle(.6, 10), .5))
# finale
S['finale-ready']  = mix((0, chord([C5, E5, G5, C6], glock, 1.4, .06, 1.2)), (.1, sparkle(1.0, 16), .6), (0, whoosh(.8, 300, 4000), .3))
S['finale-charge'] = mix((0, np.sin(sweep(220, 880, 1.5, 1.4)) * np.clip(t(1.5) / 1.2, 0, 1) * env_exp(1.5, 4, 0), .5),
                         (0, whoosh(1.5, 300, 5000), .5), (.9, sparkle(.6, 10), .5))
fb = np.zeros(int(SR * 2.6))
for i in range(60):
    f = rng.uniform(600, 2600); p = pop(f, f * 1.6, .07, .025) * rng.uniform(.2, .5); o = int(rng.uniform(0, 2.3) * SR); fb[o:o + len(p)] += p[:len(fb) - o]
S['finale-beam'] = mix((0, fb), (0, chord([C5, E5, G5, C6, E6], glock, 2.6, .0, 1.4)), (0, whoosh(2.6, 400, 6000) * .5), (1.0, sparkle(1.4, 24), .6))
for k in ['fx-pierce', 'fx-quake', 'fx-collapse', 'fx-burn', 'fx-arc', 'fx-split']: S[k] = mix((0, whoosh(.3, 500, 3500), .6), (.05, pop(900, 1700, .08, .03), .5))
# results
win_notes = [(0, C5), (.12, E5), (.24, G5), (.36, C6), (.6, A5), (.72, C6), (.9, E6)]
S['win'] = mix(*[(o, marimba(f, .6), .8) for o, f in win_notes], (1.1, chord([C6, E6, G6, C7], glock, 1.4, .03, 1.1)), (1.1, sparkle(1.0, 16), .6),
               *[(o, bassnote(f / 4, .5), .6) for o, f in [(0, C5), (.36, G5), (1.1, C5)]])
S['lose'] = mix(*[(o, marimba(f, .7), .8) for o, f in [(0, G5), (.22, F5), (.44, E5), (.66, D5)]], (.95, marimba(C5, 1.0), .9),
                (.95, np.sin(sweep(420, 260, .6, 1)) * env_exp(.6, .3), .25))
# interface
# 2026-10-18 22:51 (user: the settings / codex buttons were grating). UI taps are now low, round bubble bloops with no
# hiss or click: ui = one soft bloop, page (open a panel) = two rising bloops. The old page whoosh is still drawn from
# the noise generator (and dropped) so every sound after it keeps its exact noise.
S['ui']   = pop(700, 1050, .07, .022, click=0, bright=0)
S['tick'] = pop(1800, 2400, .03, .008, click=.1)
_unused_page_whoosh = whoosh(.18, 900, 4000)
S['page'] = mix((0, pop(520, 800, .09, .03, click=0, bright=0)), (.075, pop(760, 1120, .09, .028, click=0, bright=0), .75))
S['zoom'] = whoosh(.25, 600, 5000); S['whoosh'] = whoosh(.3, 500, 4000)
S['warp'] = mix((0, whoosh(.7, 300, 6000)), (.3, chord([G5, C6, E6], glock, .8, .04, .8)), (.35, sparkle(.5, 8), .5))
S['reward'] = mix((0, glock(E6, .5), .7), (.08, glock(G6, .5), .7), (.16, glock(C7, .8), .7), (.16, sparkle(.5, 8), .4))
S['legend'] = mix(*[(i * .09, marimba(f, .5), .7) for i, f in enumerate([C5, E5, G5, C6])], (.36, chord([C6, E6, G6], glock, 1.0, .02, 1.0)))
S['equip'] = S['ui']; S['unequip'] = pop(1500, 1100, .06, .02); S['recycle'] = S['shuffle']; S['upgrade'] = S['merge']
S['buy'] = S['reward']; S['gacha'] = S['legend']
# voice: cute babble instead of speech
S['voice-a'] = formant_blip(620, 'a'); S['voice-e'] = formant_blip(700, 'e'); S['voice-o'] = formant_blip(540, 'o')
S['voice']   = mix((0, formant_blip(600, 'a', .09)), (.1, formant_blip(680, 'e', .09)), (.2, formant_blip(560, 'o', .12)))
S['pilot-confirm'] = mix((0, formant_blip(640, 'e', .1)), (.12, formant_blip(760, 'a', .14)), (.12, sparkle(.3, 4), .3))

# ---- music: one looping theme (lead = melody, backing = band), 120 BPM, 16 bars = 32 s, C major
BPM = 120; beat = 60 / BPM; bar = 4 * beat; L = 16 * bar
prog = ['C', 'G', 'Am', 'F', 'C', 'G', 'F', 'G', 'Am', 'F', 'C', 'G', 'F', 'G', 'C', 'C']
roots = {'C': 48, 'G': 43, 'Am': 45, 'F': 41, 'Em': 40}
tri = {'C': (60, 64, 67), 'G': (59, 62, 67), 'Am': (57, 60, 64), 'F': (57, 60, 65), 'Em': (59, 64, 67)}
# melody in 8th-note steps: (step, midi, length in steps); a bouncy pentatonic tune
mel = []
A = [(0, 72, 2), (2, 76, 1), (3, 79, 1), (4, 76, 2), (6, 74, 2), (8, 71, 2), (10, 74, 1), (11, 79, 1), (12, 74, 4),
     (16, 72, 2), (18, 74, 1), (19, 76, 1), (20, 79, 2), (22, 81, 2), (24, 77, 2), (26, 76, 1), (27, 74, 1), (28, 72, 4)]
B = [(0, 76, 1), (1, 79, 1), (2, 84, 2), (4, 83, 2), (6, 79, 2), (8, 77, 2), (10, 79, 1), (11, 81, 1), (12, 79, 4),
     (16, 76, 2), (18, 77, 2), (20, 79, 2), (22, 74, 2), (24, 77, 2), (26, 76, 1), (27, 74, 1), (28, 72, 4)]
for sec, phrase in enumerate([A, A, B, A]):
    for st, n, ln in phrase:
        if sec == 1 and st >= 28: n = 79 if st == 28 else n   # second time ends up on G
        mel.append((sec * 32 + st, n, ln))
def place(track, start, sig, g=1.0):
    s = int(start * SR) % len(track); e = s + len(sig)
    if e <= len(track): track[s:e] += sig * g
    else: k = len(track) - s; track[s:] += sig[:k] * g; track[:e - len(track)] += sig[k:] * g   # wrap tails round the loop
N = int(L * SR)
lead = np.zeros(N); back = np.zeros(N)
for st, n, ln in mel:
    f = note(n); d = ln * beat / 2
    place(lead, st * beat / 2, glock(f, max(.6, d + .4)), .55)
    place(lead, st * beat / 2, marimba(f, max(.4, d + .3)), .45)
for b, ch in enumerate(prog):
    t0 = b * bar; r = note(roots[ch])
    for k, (o, rr) in enumerate([(0, r), (1.5, r), (2, r * 1.5), (3, r)]):   # bouncy bass
        place(back, t0 + o * beat, bassnote(rr, .45), .7)
    for o in (1, 3):                                                          # ukulele-ish strums on 2 and 4
        for i, m in enumerate(tri[ch]):
            place(back, t0 + o * beat + i * .012, pluck(note(m), .45), .28)
    for i in range(8):                                                        # shaker 8ths, kick on 1 and 3, woodblock offbeat
        place(back, t0 + i * beat / 2, shaker(), .35 if i % 2 else .22)
    for o in (0, 2): place(back, t0 + o * beat, kick(), .55)
    if b % 2 == 1: place(back, t0 + 3.5 * beat, woodblock(1100, .07), .25)
    if b % 4 == 3:                                                            # a few bubble pops as ear candy
        for i in range(3): place(back, t0 + (3 + i * .17) * beat, pop(900 + i * 300, 1600 + i * 400, .07, .025), .25)
S['orbit-drive-lead'] = lead; S['orbit-drive-backing'] = back

# ---- write
meta = {}
for name, y in S.items():
    y = np.asarray(y, float)
    music = name.startswith('orbit-drive')
    loopy = music or name in ('laser-loop',)
    if name == 'orbit-drive-lead': y = y / (np.max(np.abs(np.r_[lead, back])) or 1) * 0.85
    elif name == 'orbit-drive-backing': y = y / (np.max(np.abs(back)) or 1) * 0.85   # band about as loud as the tune
    # 2026-10-18 22:40 (user: shots too loud): every launch sound (shot, shot-twin, shot-max, shot-rocket, shot-needle,
    # ...) at half the usual peak, -6 dB; several members fire at once, so these stack up more than any other cue.
    elif name.startswith('shot'): y = norm(y, 0.445)
    elif name in ('ui', 'page', 'equip'): y = norm(y, 0.6)   # 22:51: soft interface sounds, a bit below the rest
    else: y = norm(y, 0.89)
    if not loopy: y = fade_out(y, 0.008)
    pcm = (np.clip(y, -1, 1) * 32767).astype(np.int16)
    wav = f'{OUT}/{name}.wav'
    with wave.open(wav, 'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    q = '4' if music else '5'
    subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', wav, '-c:a', 'libvorbis', '-q:a', q, f'{OUT}/{name}.ogg'], check=True)
    os.remove(wav)
    meta[name] = round(len(y) / SR, 3)
json.dump(meta, open('durations.json', 'w'), ensure_ascii=False, indent=1)
print(len(meta), 'files', sum(os.path.getsize(f'{OUT}/{n}.ogg') for n in meta) // 1024, 'KB')
