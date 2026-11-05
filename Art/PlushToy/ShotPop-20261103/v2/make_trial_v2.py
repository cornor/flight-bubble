#!/usr/bin/env python3
"""V2: remove the sustained pitched 'du' body; standalone audition only."""
from pathlib import Path
import hashlib
import importlib.util
import json
import sys

import numpy as np

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
V1 = HERE.parent
spec = importlib.util.spec_from_file_location("trial_helpers", V1 / "make_trial.py")
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)
a.HERE = HERE  # Only the helper's output location; game input paths stay intact.
a.SECONDS = 6.0
SR = a.SR
SEED = 202611032


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def taper(t, attack, hold, release):
    onset = .5 - .5 * np.cos(np.minimum(t / attack, 1) * np.pi)
    tail = np.exp(-np.maximum(t - attack - hold, 0) / release)
    end = np.clip((t[-1] - t) / .005, 0, 1)
    return onset * tail * np.sin(end * np.pi / 2) ** 2


def pressure(t, center, width):
    # A single compression/rarefaction event, not a repeating oscillator.
    u = (t - center) / width
    return (1 - u * u) * np.exp(-.5 * u * u)


def tonal_concentration(x):
    power = np.abs(np.fft.rfft(x, 8192)) ** 2
    hz = np.fft.rfftfreq(8192, 1 / SR)
    power[(hz < 120) | (hz > 8000)] = 0
    window = round(100 * 8192 / SR)
    return float(np.convolve(power, np.ones(window), "valid").max() / max(power.sum(), 1e-15))


def synthesize(kind, index, reference):
    rng = np.random.default_rng(SEED + index + (100 if kind == "hit" else 0))
    duration = ([.042, .046, .040, .048, .044, .045, .041, .047] if kind == "shot"
                else [.045, .049, .043, .051, .047, .048, .044, .050])[index]
    t = np.arange(round(duration * SR)) / SR
    if kind == "shot":
        # A short breath shaped broadly around the speech/mouth range. No tone,
        # frequency sweep, periodic modulation, or resonant ringing filter.
        attack = .003 + rng.uniform(-.0003, .0003)
        hold = .0035
        release = .0085 + rng.uniform(-.0005, .0005)
        noise_hi = 2600 + rng.uniform(-180, 180)
        air = a.bandpass(rng.normal(size=len(t)), 550, noise_hi)
        air = a.lowpass(np.tanh(air * 2.0), 4200)
        body = .30 * pressure(t, .0045, .00065)
        x = .88 * air * taper(t, attack, hold, release) + body
        params = {"kind": "short_air_with_one_pressure_pulse", "duration_s": duration,
                  "attack_s": attack, "hold_s": hold, "release_s": release,
                  "air_band_hz": [550, noise_hi], "pressure_width_s": .00065}
    else:
        # Two broadband textures, but only one attack: a small wet snap with
        # rapidly decaying membrane noise. No pitched hollow 'boop' tail.
        attack = .0006 + rng.uniform(-.0001, .0001)
        hold = .006
        release = .0105 + rng.uniform(-.0006, .0006)
        noise_hi = 4300 + rng.uniform(-250, 250)
        membrane = a.bandpass(rng.normal(size=len(t)), 650, noise_hi)
        membrane = a.lowpass(np.tanh(membrane * 3.2), 5500)
        snap = a.bandpass(rng.normal(size=len(t)), 2000, 6500)
        snap *= taper(t, .0004, .0003, .0018)
        body = .30 * pressure(t, .0015, .00035)
        x = .95 * membrane * taper(t, attack, hold, release) + .16 * snap + body
        x = np.tanh(3.0 * x)
        params = {"kind": "broadband_membrane_snap", "duration_s": duration,
                  "attack_s": attack, "hold_s": hold, "release_s": release,
                  "membrane_band_hz": [650, noise_hi], "pressure_width_s": .00035,
                  "soft_saturation_drive": 3.0}
    # Same per-event energy target as v1, subject to a source peak ceiling.
    # Broad noise can need extra headroom; any difference is disclosed below.
    sample = a.finish(x, reference, 0)
    params["sustained_oscillator_count"] = 0
    return sample, params


def main():
    game_paths = {k: a.RES / "Audio" / (k + ".ogg") for k in ("shot", "hit")}
    game_paths["music"] = a.RES / "Music/level.ogg"
    v1_paths = {f"{k}-{i:02d}": V1 / "delivery" / k / f"{k}-{i:02d}.wav"
                for k in ("shot", "hit") for i in range(1, 9)}
    before = {str(p): sha(p) for p in [*game_paths.values(), *v1_paths.values(), V1 / "make_trial.py"]}
    old = {k: [a.decode(v1_paths[f"{k}-{i:02d}"]) for i in range(1, 9)] for k in ("shot", "hit")}
    new = {"shot": [], "hit": []}
    manifest = {"seed": SEED, "sample_rate": SR, "feedback": "du du du 的那个声音不好",
                "baseline": "previous audition v1, not original game sounds",
                "source_sha256": before, "samples": {}, "previews": {}}
    for kind in new:
        for i in range(8):
            name = f"{kind}-{i + 1:02d}"
            sample, params = synthesize(kind, i, old[kind][i])
            master = HERE / "masters" / (name + ".wav")
            delivery = HERE / "delivery" / kind / (name + ".wav")
            delivery.parent.mkdir(parents=True, exist_ok=True)
            a.write_wav(master, sample)
            a.ffmpeg("-y", "-i", master, "-codec:a", "pcm_s16le", delivery)
            decoded = a.decode(delivery)
            new[kind].append(decoded)
            manifest["samples"][name] = {
                "parameters": params, "master": a.metrics(sample),
                "delivery": a.metrics(decoded), "sha256": sha(delivery),
                "energy_vs_v1_db": float(10 * np.log10(np.sum(decoded ** 2) / np.sum(old[kind][i] ** 2))),
                "v1_peak_100hz_band_fraction": tonal_concentration(old[kind][i]),
                "v2_peak_100hz_band_fraction": tonal_concentration(decoded),
            }
            assert sample[0] == sample[-1] == 0
            assert np.abs(decoded).max() < .85

    events = a.events_for(6)
    old_full, new_full = a.render(events, old), a.render(events, new)
    audition = np.zeros((12 * SR, 2))
    segments = []
    for j, kind in enumerate(("shot", "hit")):
        for i, sample in enumerate(new[kind]):
            a.place(audition, .3 + j * 3 + i * .32, np.column_stack([sample, sample]),
                    .273 if kind == "shot" else .234)
        segments.append({"start": j * 3, "end": (j + 1) * 3, "kind": kind})
    audition[6 * SR:] = new_full
    segments.append({"start": 6, "end": 12, "kind": "combined_six_teammates"})
    comparison = np.concatenate([old_full, np.zeros((SR, 2)), new_full])
    pending = {"01-v2-separated-demo": audition, "02-v1-v2-dry-ab": comparison}
    # Isolated comparison for each category, with identical timing and pitch.
    for kind in ("shot", "hit"):
        es = [e for e in events if e["kind"] == kind]
        pending[f"03-{kind}-v1-v2-ab"] = np.concatenate(
            [a.render(es, old), np.zeros((SR, 2)), a.render(es, new)])
    music = a.decode(game_paths["music"], 2, start=8, duration=6) * .52
    fade = int(.08 * SR)
    music[:fade] *= np.linspace(0, 1, fade)[:, None]
    music[-fade:] *= np.linspace(1, 0, fade)[:, None]
    pending["04-v1-v2-with-music-ab"] = np.concatenate(
        [old_full + music, np.zeros((SR, 2)), new_full + music])
    common_gain = min(1.0, .82 / max(np.abs(x).max() for x in pending.values()))
    manifest["preview_common_gain"] = common_gain
    for name, track in pending.items():
        manifest["previews"][name] = a.preview(name, track * common_gain)
    assert before == {str(p): sha(p) for p in [*game_paths.values(), *v1_paths.values(), V1 / "make_trial.py"]}
    assert len({x["sha256"] for x in manifest["samples"].values()}) == 16
    manifest["game_and_v1_files_unchanged"] = True
    manifest["validation"] = "Technical checks only; no claim of human listening approval."
    (HERE / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    (HERE / "timelines.json").write_text(json.dumps({
        "demo_segments": segments, "battle_events": events,
        "ab_segments": [{"start": 0, "end": 6, "version": "v1"},
                        {"start": 6, "end": 7, "version": "silence"},
                        {"start": 7, "end": 13, "version": "v2"}],
        "note": "Offline simulation, not a game recording; timing/gains inherited from v1."
    }, indent=2) + "\n")
    print(json.dumps({"samples": 16, "previews": list(pending), "common_gain": common_gain}, indent=2))


if __name__ == "__main__":
    main()
