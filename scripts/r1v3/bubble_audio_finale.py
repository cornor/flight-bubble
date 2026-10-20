# 2026-10-19 23:24 (user: the finale's motion is fine now, but the sound still lacks weight).
# Re-made finale sounds, generated apart from bubble_audio_v1.py so the other cues keep their exact noise:
#   finale-charge  1.62 s  the deep breath in: rising tone and breath, bubbles sucked in, then 0.1 s held silence
#   maxpower       1.2 s   the release (played by the original FinaleEvents at 1.65 s): a sub boom with harmonics
#                          that small speakers can carry, a big low bloop, a blast of air and a spray of bubbles
#   finale-beam    2.6 s   the sweep: a roaring stream of bubbles over a low rumble, no glock chord at the start
# Variant B adds a bright major-chord stab to the release ("fanfare"); variant A is the release alone.
#   python3 scripts/r1v3/bubble_audio_finale.py --variant A --out <dir>            write the three .ogg files
#   python3 scripts/r1v3/bubble_audio_finale.py --variant A --preview <file.mp3>   in-game mix preview (with music)
# In game the release plays louder than the original maxpower (.338 -> .55, ChipAudio.Garden), the breath a little softer.
import argparse, os, subprocess, wave
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent
src = (HERE / 'bubble_audio_v1.py').read_text(encoding='utf-8')
ns = {'__name__': 'bubble_finale_helpers'}
exec(src[:src.index('S = {}')].replace("OUT = 'out'; os.makedirs(OUT, exist_ok=True)", ''), ns)   # helpers only
SR, t, env_exp, sweep, noise, lowpass, highpass, bandpass, mix, norm, fade_out, pop, glock, whoosh, sparkle, note = (
    ns[k] for k in 'SR t env_exp sweep noise lowpass highpass bandpass mix norm fade_out pop glock whoosh sparkle note'.split())
ns['rng'].bit_generator.state = np.random.default_rng(1019).bit_generator.state
rng = ns['rng']


def saw(f, d, detune=(0.0,)):
    x = t(d); y = np.zeros_like(x)
    for dt in detune:
        ph = (f * (1 + dt) * x) % 1.0
        y += 2 * ph - 1
    return y / len(detune)


def charge():
    d = 1.62; held = 1.52   # sound stops at 1.52 s; the breath is held until the release at 1.65 s
    x = t(d)
    k = np.clip(x / held, 0, 1)
    vib = 1 + 0.012 * k ** 2 * np.sin(2 * np.pi * (5 + 9 * k) * x)
    f = 170 * (4.2) ** (k ** 1.5) * vib
    ph = 2 * np.pi * np.cumsum(f) / SR
    tone = (np.sin(ph) + .35 * np.sin(2 * ph) + .12 * np.sin(3 * ph)) * (0.15 + 0.85 * k ** 1.3)
    breath = ns['whoosh'](held, 250, 5500) * 0 + 0
    n = noise(d); y = np.zeros_like(n); s = 0.0
    for i in range(len(n)):   # breath in: noise through a rising low-pass, swelling
        fc = 300 * (5000 / 300) ** k[i]; a = np.exp(-2 * np.pi * fc / SR); s = (1 - a) * n[i] + a * s; y[i] = s
    breath = highpass(y, 150) * k ** 1.6
    parts = [(0, tone, .55), (0, breath, 1.1)]
    o = 0.25
    while o < held - .05:   # bubbles sucked in: falling bloops, closer and closer together
        f0 = rng.uniform(1300, 2400)
        parts.append((o, pop(f0, f0 * .55, .07, .025, click=.1), .25 + .35 * o / held))
        o += 0.14 * (1 - o / held) + 0.025
    parts.append((held - .07, pop(520, 260, .08, .03, click=0, bright=.1), .6))   # the gulp
    y = mix(*parts)[:int(SR * d)]
    cut = int(SR * held); y[cut:] = 0; y[cut - int(SR * .012):cut] *= np.linspace(1, 0, int(SR * .012))
    return y


def release(variant):
    d = 1.2
    boom = np.sin(sweep(140, 40, .8, .35)) * env_exp(.8, .3, .001)
    boom = np.tanh(3.0 * boom)   # drive: harmonics at 80-400 Hz so phones can hear the boom
    bloop = pop(110, 420, .45, .17, click=1.0, bright=.8)
    air = lowpass(noise(.5), 2500) * env_exp(.5, .12, .002)
    crack = highpass(noise(.012), 3000) * env_exp(.012, .003, 0)
    parts = [(0, boom, 1.0), (0, bloop, .8), (0, air, .9), (0, crack, .6)]
    for i in range(30):   # spray of bubbles fanning upward
        o = .02 + i * .017 + rng.uniform(0, .01); f0 = rng.uniform(500, 1200) + i * 35
        parts.append((o, pop(f0, f0 * 1.7, .07, .025, click=.15), .55 * (1 - i / 40)))
    if variant == 'B':
        stab = np.zeros(int(SR * .9))
        for f in (note(60), note(64), note(67), note(72), note(76)):
            stab += saw(f, .9, (-.006, 0, .007))
        stab = lowpass(stab, 3200) * env_exp(.9, .28, .006)
        parts += [(.0, stab, .45), (.02, glock(note(84), .9), .35), (.02, glock(note(91), .9), .25)]
    y = mix(*parts)[:int(SR * d)]
    return np.tanh(1.8 * y / (np.max(np.abs(y)) or 1)) / np.tanh(1.8)


def beam():
    d = 2.6; x = t(d)
    stream = np.zeros(int(SR * d))
    for i in range(130):
        o = rng.uniform(0, 2.35) ** 1.3 / 2.35 ** .3; f = rng.uniform(450, 2400)
        p = pop(f, f * 1.6, .07, .025, click=.12) * rng.uniform(.25, .6) * (1 - .55 * o / d)
        s = int(o * SR); stream[s:s + len(p)] += p[:len(stream) - s]
    rumble = lowpass(noise(d), 220) * 3.0
    roar = bandpass(noise(d), 300, 2500) * .5
    body = (rumble + roar) * np.clip(x / .03, 0, 1) * np.exp(-x / 1.4)
    tail = sparkle(1.1, 18)
    y = mix((0, stream), (0, body, .8), (0, whoosh(d, 600, 6000) * .4), (1.45, tail, .5))[:int(SR * d)]
    return y


def write(y, path, peak=.89):
    y = fade_out(norm(np.asarray(y, float), peak), .008)
    wav = str(path) + '.wav'
    with wave.open(wav, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((np.clip(y, -1, 1) * 32767).astype(np.int16).tobytes())
    subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', wav, '-c:a', 'libvorbis', '-q:a', '5', str(path)], check=True)
    os.remove(wav)


def load(path):
    raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(path), '-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True).stdout
    return np.frombuffer(raw, np.float32).astype(float)


def preview(clips, out, extra=(), maxpower_gain=.55):
    """In-game mix of the finale: music (ducked as in game), charge at 0 s, release cues at 1.65 s."""
    audio = HERE.parents[1] / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'
    d = 5.0; n = int(SR * d); tl = np.arange(n) / SR
    music = np.tile(load(audio / 'Music/level.ogg'), 2)[int(SR * 8):int(SR * 8) + n]
    gain = np.full(n, .52)
    gain[(tl < 0 + 1.5)] *= .84                                  # finale-charge duck
    deep = (tl >= 1.45) & (tl < 2.4); gain[deep] = .52 * .22      # garden deep duck
    rec = (tl >= 2.4) & (tl < 3.3); gain[rec] = .52 * (.22 + .78 * (tl[rec] - 2.4) / .9)
    gain = np.convolve(gain, np.ones(2000) / 2000, mode='same')
    y = music * gain
    def put(at, clip, g):
        s = int(at * SR); y[s:s + len(clip)] += clip[:n - s] * g
    put(0, clips['finale-charge'], .364)
    put(1.65, clips['finale-beam'], .481)
    put(1.65, clips['maxpower'], maxpower_gain)
    for name, g in extra: put(1.65, load(audio / f'Audio/{name}.ogg'), g)
    y = y / max(1.0, np.max(np.abs(y)) / .98)
    wav = str(out) + '.wav'
    with wave.open(wav, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype(np.int16).tobytes())
    subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', wav, '-b:a', '192k', str(out)], check=True)
    os.remove(wav)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--variant', choices=['A', 'B', 'current'], default='A')
    ap.add_argument('--out'); ap.add_argument('--preview')
    a = ap.parse_args()
    audio = HERE.parents[1] / 'UnityProject/Assets/Flightbeat/Resources/GardenR1/Audio'
    if a.variant == 'current':
        clips = {k: load(audio / f'{k}.ogg') for k in ('finale-charge', 'finale-beam', 'maxpower')}
        preview(clips, a.preview, [('super-impact', .429), ('shockwave', .26)], .338); return
    made = {'finale-charge': charge(), 'maxpower': release(a.variant), 'finale-beam': beam()}
    tmp = Path(a.out or '/tmp/finale-audio'); tmp.mkdir(parents=True, exist_ok=True)
    for k, y in made.items(): write(y, tmp / f'{k}.ogg', .7 if k == 'finale-charge' else .89)   # a softer breath leaves room for the release
    if a.preview: preview({k: load(tmp / f'{k}.ogg') for k in made}, a.preview)


if __name__ == '__main__':
    main()
