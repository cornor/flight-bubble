# 2026-10-20 09:31 (user: the charge-up is fine, but after it the release sounds like air going out of a balloon;
# no "噗"). Three release designs that keep going UP and HOLD instead of a falling, decaying puff:
#   1 光炮   a bright impact (short kick, crash) and a sustained beam chord whose filter opens, with a shimmering run
#   2 凯旋   a timpani-and-brass hit, then a brass fanfare C -> F -> G -> C with a rising glockenspiel run
#   3 喷射   an impact into a screaming energy beam (pitch climbs, then holds with vibrato), driven bass, bright stream
# Each writes finale-release.ogg (the release hit; in game it replaces the "maxpower" cue while a finale runs, see
# ChipAudio.Garden.cs; maxpower itself is also the merge sound and stays) and finale-beam.ogg (the sweep, 2.6 s).
# 2026-10-20 09:38: the user picked 1; its two files are in Resources/GardenR1/Audio. The current finale-charge.ogg is kept (user: the charge is fine). No low bloops, no air puffs.
#   python3 scripts/r1v3/bubble_audio_finale2.py --variant 1 --out <dir> [--preview <file.mp3>]
import argparse
from pathlib import Path
import numpy as np
import bubble_audio_finale as base

SR, t, env_exp, noise, lowpass, highpass, bandpass, mix, glock, note = (
    base.SR, base.t, base.env_exp, base.noise, base.lowpass, base.highpass, base.bandpass, base.mix, base.glock, base.note)
rng = np.random.default_rng(1020)


def supersaw(f, d, voices=5, spread=.012):
    x = t(d); y = np.zeros_like(x)
    for i in range(voices):
        dt = spread * (i - (voices - 1) / 2) / ((voices - 1) / 2 or 1)
        y += 2 * ((f * (1 + dt) * x + rng.uniform()) % 1.0) - 1
    return y / voices


def sweep_lowpass(x, f0, f1, d_open):
    """Low-pass whose cut-off opens from f0 to f1 over d_open seconds, then stays open."""
    k = np.clip(np.arange(len(x)) / (SR * d_open), 0, 1)
    fc = f0 * (f1 / f0) ** k
    a = np.exp(-2 * np.pi * fc / SR); y = np.zeros_like(x); s = 0.0
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s; y[i] = s
    return y


def hold(d, attack=.01, sustain=1.6, release=.8):
    x = t(d)
    return np.clip(x / attack, 0, 1) * np.where(x < sustain, 1.0, np.clip(1 - (x - sustain) / release, 0, 1))


def kick(d=.22, f0=170, f1=60):
    x = t(d); f = f1 + (f0 - f1) * np.exp(-x / .025)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env_exp(d, .09, .001)


def crash(d=1.4, decay=.45):
    return highpass(noise(d), 3500) * env_exp(d, decay, .002)


def chord(fs, d, inst):
    return sum(inst(f, d) for f in fs) / len(fs)


def run(notes, step, d_note=.5, vol=1.0):
    return mix(*[(i * step, glock(f, d_note), vol) for i, f in enumerate(notes)])


def finish(y, peak=.95, drive=1.6):
    y = y / (np.max(np.abs(y)) or 1)
    return np.tanh(drive * y) / np.tanh(drive) * peak


C2, G2, C3, E3, G3, C4, E4, G4, C5, E5, G5, C6, E6, G6, C7 = [note(n) for n in (36, 43, 48, 52, 55, 60, 64, 67, 72, 76, 79, 84, 88, 91, 96)]
F3, A3, F4, A4, D4, B3, D5, B4 = [note(n) for n in (53, 57, 65, 69, 62, 59, 74, 71)]


def v1():
    stab = sweep_lowpass(chord([C3, G3, C4, E4, G4], 1.2, supersaw), 2500, 6000, .15) * env_exp(1.2, .5, .004)
    hit = mix((0, kick(), 1.0), (0, crash(1.2, .4), .5), (0, stab, .9), (0, np.sin(2 * np.pi * C2 * t(1.2)) * env_exp(1.2, .45, .004), .6))
    d = 2.6
    pad = sweep_lowpass(chord([C3, G3, C4, G4, C5], d, supersaw), 900, 7000, .7)
    trem = 1 + .18 * np.sin(2 * np.pi * 11 * t(d))
    sub = np.sin(2 * np.pi * C2 * t(d)) * .5
    shimmer = mix(*[(k * .56, run([C6, E6, G6, C7, G6, E6, G6, C7], .07, .35, .5), 1) for k in range(4)])[:int(SR * d)]
    air = highpass(noise(d), 5000) * .12
    beam = (pad * trem + sub + air) * hold(d, .02, 1.7, .85)
    return finish(hit), finish(mix((0, beam), (0, shimmer * hold(len(shimmer) / SR, .01, 1.7, .8), .45)), .9, 1.3)


def brass(f, d, open_t=.08):
    x = supersaw(f, d, 3, .006)
    return sweep_lowpass(x, 700, 4200, open_t)


def v2():
    timp = np.sin(2 * np.pi * np.cumsum(95 - 8 * np.exp(-t(1.2) / .1)) / SR) * env_exp(1.2, .4, .002)
    stab = chord([C4, E4, G4, C5], 1.2, brass) * env_exp(1.2, .55, .006)
    hit = mix((0, timp, 1.0), (0, kick(.2, 150, 70), .6), (0, crash(1.4, .5), .45), (0, stab, 1.0))
    d = 2.6
    seq = [(0.0, [C4, E4, G4, C5]), (.75, [F4, A4, C5, F3]), (1.3, [G4, B4, D5, G3]), (1.75, [C5, E5, G5, C4])]
    parts = []
    for i, (o, fs) in enumerate(seq):
        nd = (seq[i + 1][0] if i + 1 < len(seq) else d) - o + .05
        parts.append((o, chord(fs, nd, brass) * hold(nd, .015, nd - .06, .06), 1.0))
    horns = mix(*parts)[:int(SR * d)] * hold(d, .01, 2.1, .5)
    glk = run([C5, E5, G5, C6, E6, G6, C7, G6, C7, E6, G6, C7], .09, .6, .6)
    roll = bandpass(noise(d), 60, 300) * .5 * hold(d, .01, 1.6, .9)
    return finish(hit), finish(mix((0, horns), (0, glk, .5), (0, roll), (1.75, crash(.9, .4), .3)), .9, 1.3)


def v3():
    hit = mix((0, kick(.25, 180, 55), 1.0), (0, highpass(noise(.3), 1500) * env_exp(.3, .06, .001), .6),
              (0, crash(1.2, .35), .4), (0, sweep_lowpass(chord([C3, C4, G4], 1.1, supersaw), 3000, 8000, .05) * env_exp(1.1, .4, .003), .7))
    d = 2.6; x = t(d)
    f = (C5 * (2 ** np.clip(x / .3, 0, 1))) * (1 + .012 * np.sin(2 * np.pi * 7 * x))   # climbs an octave, then holds
    ph = 2 * np.pi * np.cumsum(f) / SR
    scream = (np.sin(ph) + .5 * np.sin(2 * ph) + .25 * np.sin(3 * ph) + .12 * np.sin(5 * ph)) * .6
    bass = np.tanh(3 * supersaw(C2, d, 3, .004)) * .55
    roar = sweep_lowpass(bandpass(noise(d), 800, 9000), 1500, 9000, .4) * .4
    stream = np.zeros(int(SR * d))
    for i in range(160):   # bright rising pops: a stream of bubbles rushing out (no low bloops)
        o = rng.uniform(0, 2.2); f0 = rng.uniform(1500, 3200)
        p = base.pop(f0, f0 * 1.5, .05, .015, click=.1, bright=.2) * rng.uniform(.15, .35)
        s = int(o * SR); stream[s:s + len(p)] += p[:len(stream) - s]
    beam = (scream + bass + roar + stream) * hold(d, .015, 1.75, .8)
    return finish(hit), finish(beam, .9, 1.4)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--variant', choices=['1', '2', '3'], required=True)
    ap.add_argument('--out', required=True); ap.add_argument('--preview')
    a = ap.parse_args()
    hit, beam = {'1': v1, '2': v2, '3': v3}[a.variant]()
    out = Path(a.out); out.mkdir(parents=True, exist_ok=True)
    base.write(hit, out / 'finale-release.ogg', .95); base.write(beam, out / 'finale-beam.ogg', .9)
    if a.preview:
        audio = base.HERE.parents[1] / 'UnityProject/Assets/Flightbeat/Resources/GardenR1/Audio'
        clips = {'finale-charge': base.load(audio / 'finale-charge.ogg'), 'maxpower': base.load(out / 'finale-release.ogg'),
                 'finale-beam': base.load(out / 'finale-beam.ogg')}
        base.preview(clips, a.preview, (), .55)


if __name__ == '__main__':
    main()
