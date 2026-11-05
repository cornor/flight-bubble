#!/usr/bin/env python3
"""Offline sound-design audition; never writes into UnityProject or Builds.

Requires Python 3, numpy and ffmpeg. Run this file from any working directory.
"""
from pathlib import Path
import hashlib
import json
import math
import shutil
import subprocess
import wave

import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
RES = ROOT / "UnityProject/Assets/Flightbeat/Resources/GardenR1"
FFMPEG = shutil.which("ffmpeg")
SR = 44100
SECONDS = 12.0
SEED = 20261103


def ffmpeg(*args):
    result = subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", *map(str, args)],
                            capture_output=True)
    if result.returncode:
        raise RuntimeError(result.stderr.decode(errors="replace"))
    return result.stdout


def decode(path, channels=1, start=None, duration=None):
    args = [] if start is None else ["-ss", start]
    args += ["-i", path]
    if duration is not None:
        args += ["-t", duration]
    data = ffmpeg(*args, "-ar", SR, "-ac", channels, "-f", "f32le", "pipe:1")
    a = np.frombuffer(data, dtype="<f4").astype(np.float64)
    return a if channels == 1 else a.reshape(-1, channels)


def db(x):
    return float(20 * np.log10(max(float(x), 1e-12)))


def metrics(x):
    mono = x if x.ndim == 1 else x.mean(axis=1)
    spectrum = np.abs(np.fft.rfft(mono)) ** 2
    freqs = np.fft.rfftfreq(len(mono), 1 / SR)
    return {
        "duration_s": round(len(x) / SR, 6),
        "peak_dbfs": round(db(np.abs(x).max()), 3),
        "rms_dbfs": round(db(np.sqrt(np.mean(x * x))), 3),
        "energy": float(np.sum(x * x) / SR),
        "dc": float(np.mean(x)),
        "clipped_samples": int(np.count_nonzero(np.abs(x) >= 1)),
        "energy_2k_to_8k_fraction": round(float(spectrum[(freqs >= 2000) & (freqs < 8000)].sum()
                                                  / max(spectrum.sum(), 1e-15)), 5),
    }


def write_wav(path, x):
    path.parent.mkdir(parents=True, exist_ok=True)
    assert np.isfinite(x).all() and np.abs(x).max() < 1, path
    # 24-bit PCM masters, including exact zero at the one-shot boundaries.
    pcm = np.round(x * 8388607).astype(np.int32).reshape(-1)
    data = np.column_stack([pcm & 255, (pcm >> 8) & 255, (pcm >> 16) & 255]).astype(np.uint8)
    with wave.open(str(path), "wb") as f:
        f.setnchannels(1 if x.ndim == 1 else x.shape[1])
        f.setsampwidth(3)
        f.setframerate(SR)
        f.writeframes(data.tobytes())


def lowpass(x, hz):
    a = math.exp(-2 * math.pi * hz / SR)
    out = np.empty_like(x)
    prev = 0.0
    for i, v in enumerate(x):
        prev = (1 - a) * v + a * prev
        out[i] = prev
    return out


def bandpass(x, low, high):
    y = lowpass(x, high)
    return y - lowpass(y, low)


def envelope(t, attack, decay):
    # Raised-cosine onset, exponential decay, and a quiet 6 ms tail fade.
    onset = .5 - .5 * np.cos(np.pi * np.minimum(t / attack, 1))
    end = np.minimum((t[-1] - t) / .006, 1).clip(0)
    return onset * np.exp(-t / decay) * np.sin(end * np.pi / 2) ** 2


def finish(x, reference, offset_db):
    x = x - lowpass(x, 70)
    edge = np.ones(len(x))
    n = int(.002 * SR)
    edge[-n:] = np.sin(np.linspace(np.pi / 2, 0, n)) ** 2
    edge[0] = 0
    x *= edge
    # Remove the tiny remaining DC without introducing clicks at either end.
    w = np.sin(np.linspace(0, np.pi, len(x))) ** 2
    x -= w * x.sum() / w.sum()
    energy = np.sum(reference * reference) * 10 ** (offset_db / 10)
    x *= np.sqrt(energy / np.sum(x * x))
    # Energy match is subordinate to headroom; any reduction is in manifest.json.
    x *= min(1, .84 / max(np.abs(x).max(), 1e-12))
    x[0] = x[-1] = 0
    return x


def synthesize(kind, i, reference):
    rng = np.random.default_rng(SEED + i + (100 if kind == "hit" else 0))
    v = [-.035, .018, -.012, .040, .0, -.026, .030, .010][i]
    if kind == "shot":
        duration = [.058, .062, .056, .065, .060, .063, .057, .061][i]
        attack = .0025 + rng.uniform(-.0004, .0004)
        decay = .013 + rng.uniform(-.001, .0015)
        t = np.arange(round(SR * duration)) / SR
        f = (390 + 245 * np.exp(-t / .007)) * (1 + v)
        phase = 2 * np.pi * np.cumsum(f) / SR
        body = (np.sin(phase) + .09 * np.sin(2 * phase)) * envelope(t, attack, decay)
        air_hi = 3000 + rng.uniform(-300, 350)
        air = bandpass(rng.normal(size=len(t)), 700, air_hi)
        air *= envelope(t, .0038, .010 + rng.uniform(-.001, .001))
        x = .72 * body + .34 * air
        params = {"duration": duration, "attack": attack, "decay": decay,
                  "body_hz_start": float(f[0]), "body_hz_end": float(f[-1]),
                  "air_low_hz": 700, "air_high_hz": air_hi}
    else:
        duration = [.077, .083, .073, .087, .079, .081, .075, .085][i]
        attack = .0007 + rng.uniform(-.00012, .00012)
        decay = .027 + rng.uniform(-.0015, .0015)
        t = np.arange(round(SR * duration)) / SR
        # Fast pressure release into a rounded, hollow body. No metallic ringing.
        f = (810 + 500 * np.exp(-t / .0045)) * (1 + v)
        phase = 2 * np.pi * np.cumsum(f) / SR
        body = (np.sin(phase) + .12 * np.sin(2.03 * phase) * np.exp(-t / .008))
        body *= envelope(t, attack, decay)
        hollow_phase = 2 * np.pi * np.cumsum((420 + 75 * np.exp(-t / .012)) * (1 + v)) / SR
        hollow = np.sin(hollow_phase + .2) * envelope(t, .0018, .012)
        snap_hi = 6100 + rng.uniform(-450, 450)
        snap = bandpass(rng.normal(size=len(t)), 1800, snap_hi) * envelope(t, .0003, .0018)
        x = .84 * body + .12 * hollow + .62 * snap
        # Round off the very brief pressure peak so it does not dictate the
        # whole clip's loudness; a little extra harmonic bite helps the pop.
        x = np.tanh(2.2 * x)
        params = {"duration": duration, "attack": attack, "decay": decay,
                  "body_hz_start": float(f[0]), "body_hz_end": float(f[-1]),
                  "snap_low_hz": 1800, "snap_high_hz": snap_hi, "soft_saturation_drive": 2.2}
    # Alternate very small energy changes around the existing cue, rather than
    # winning an A/B comparison by raising the new set's volume.
    offset = [-.2, .15, -.1, .2, 0, -.15, .1, 0][i]
    params["energy_target_offset_db"] = offset
    return finish(x, reference, offset), params


def events_for(teammates):
    """Illustrative constant-target battle, with current shared audio throttles.

    The target layout / projectile physics are NOT a live Unity recording.
    Only ordinary unmerged teammates; no MAX laser, twin, rocket or combo cues.
    """
    rng = np.random.default_rng(SEED + teammates * 100)
    raw = []
    interval = .12 * .82
    for member in range(teammates):
        phase = member * interval / teammates
        for t in np.arange(.55 + phase, SECONDS - .6, interval):
            frame = math.ceil(t * 60) / 60
            raw.append((frame, "shot"))
            raw.append((math.ceil((t + .135 + .015 * (member % 3)) * 60) / 60, "hit"))
    last = {"shot": -1.0, "hit": -1.0}
    bags = {"shot": [], "hit": []}
    prev_variant = {"shot": -1, "hit": -1}
    density, prev_time = 0.0, 0.0
    events = []
    for time, kind in sorted(raw):
        density = max(0, density - (time - prev_time) * 6)
        prev_time = time
        gap = .07 if kind == "shot" else .08 + .03 * density
        if time - last[kind] < gap - 1e-9:
            continue
        last[kind] = time
        density = min(1, density + (.08 if kind == "shot" else .06))
        volume = (.42 if kind == "shot" else .36) * .65 * (1 - .25 * density) ** 2
        if not bags[kind]:
            bags[kind] = list(map(int, rng.permutation(8)))
            if bags[kind][-1] == prev_variant[kind]:
                bags[kind][0], bags[kind][-1] = bags[kind][-1], bags[kind][0]
        variant = bags[kind].pop()
        prev_variant[kind] = variant
        events.append({"time_s": round(time, 8), "kind": kind, "variant": variant + 1,
                       "gain": volume, "pitch": float(rng.uniform(.97, 1.03)), "density": density})
    return events


def place(target, time, source, gain=1):
    start = round(time * SR)
    count = min(len(source), len(target) - start)
    if count > 0:
        target[start:start + count] += source[:count] * gain


def render(events, bank):
    out = np.zeros((round(SECONDS * SR), 2))
    for e in events:
        sample = bank[e["kind"]][(e["variant"] - 1) % len(bank[e["kind"]])]
        indices = np.arange(0, len(sample) - 1, e["pitch"])
        sample = np.interp(indices, np.arange(len(sample)), sample)
        place(out, e["time_s"], np.column_stack([sample, sample]), e["gain"])
    return out


def preview(name, x):
    path = HERE / "previews" / (name + ".wav")
    write_wav(path, x)
    mp3 = path.with_suffix(".mp3")
    ffmpeg("-y", "-i", path, "-codec:a", "libmp3lame", "-b:a", "192k", mp3)
    decoded = decode(mp3, 2)
    assert np.abs(decoded).max() < .99, name
    return {"wav": metrics(x), "mp3_decoded": metrics(decoded)}


def main():
    assert FFMPEG, "ffmpeg is required"
    sources = {k: RES / "Audio" / (k + ".ogg") for k in ("shot", "hit")}
    sources["music"] = RES / "Music/level.ogg"
    hashes_before = {k: hashlib.sha256(p.read_bytes()).hexdigest() for k, p in sources.items()}
    old = {k: [decode(sources[k])] for k in ("shot", "hit")}
    new = {"shot": [], "hit": []}
    manifest = {"seed": SEED, "sample_rate": SR, "source_sha256": hashes_before,
                "baseline": {k: metrics(v[0]) for k, v in old.items()}, "samples": {}, "previews": {}}
    for kind in new:
        for i in range(8):
            sample, params = synthesize(kind, i, old[kind][0])
            name = f"{kind}-{i + 1:02d}"
            master = HERE / "masters" / (name + ".wav")
            target = HERE / "delivery" / kind / (name + ".wav")
            target.parent.mkdir(parents=True, exist_ok=True)
            write_wav(master, sample)
            ffmpeg("-y", "-i", master, "-codec:a", "pcm_s16le", target)
            decoded = decode(target)
            assert np.abs(decoded).max() < .95, name
            # Listen to the actual encoded delivery clips in the previews.
            new[kind].append(decoded)
            manifest["samples"][name] = {"parameters": params, "master": metrics(sample),
                                         "delivery_decoded": metrics(decoded),
                                         "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                                         "energy_vs_old_db": round(10 * np.log10(np.sum(decoded * decoded)
                                                                       / np.sum(old[kind][0] ** 2)), 3)}
    music = decode(sources["music"], 2, start=8, duration=SECONDS)
    assert len(music) == round(SECONDS * SR)
    music *= .52
    fade = round(.08 * SR)
    music[:fade] *= np.linspace(0, 1, fade)[:, None]
    music[-fade:] *= np.linspace(1, 0, fade)[:, None]
    timeline = {}
    pending = {}
    for name, count in (("01-single-ab", 1), ("02-full-ab", 6)):
        events = events_for(count)
        timeline[name] = {"teammates": count, "per_teammate_interval_s": .12 * .82,
                          "events": events, "segments": [
                              {"start": 0, "end": 12, "version": "old"},
                              {"start": 12, "end": 13, "version": "silence"},
                              {"start": 13, "end": 25, "version": "new"}]}
        before, after = render(events, old), render(events, new)
        silence = np.zeros((SR, 2))
        pending[name] = np.concatenate([before + music, silence, after + music])
        # A dry comparison also helps distinguish differences masked by the BGM.
        pending[name + "-dry"] = np.concatenate([before, silence, after])
    # One common preview gain for every clip, never independently normalized.
    common_gain = min(1.0, .82 / max(float(np.abs(x).max()) for x in pending.values()))
    manifest["preview_common_gain"] = common_gain
    manifest["music_gain"] = .52
    manifest["music_source_start_s"] = 8
    for name, x in pending.items():
        manifest["previews"][name] = preview(name, x * common_gain)

    reel = np.zeros((10 * SR, 2))
    for j, kind in enumerate(("shot", "hit")):
        for i, sample in enumerate(new[kind]):
            place(reel, .5 + j * 5 + i * .5, np.column_stack([sample, sample]),
                  (.273 if kind == "shot" else .234) * common_gain)
    manifest["previews"]["03-new-variants"] = preview("03-new-variants", reel)
    assert hashes_before == {k: hashlib.sha256(p.read_bytes()).hexdigest() for k, p in sources.items()}
    assert len({v["sha256"] for v in manifest["samples"].values()}) == 16
    for key, item in timeline.items():
        events = item["events"]
        for kind in ("shot", "hit"):
            filtered = [e for e in events if e["kind"] == kind]
            assert all(a["variant"] != b["variant"] for a, b in zip(filtered, filtered[1:]))
        item["counts"] = {k: sum(e["kind"] == k for e in events) for k in ("shot", "hit")}
    manifest["source_files_unchanged"] = True
    manifest["validation"] = "Technical analysis only; aesthetic approval pending user audition."
    (HERE / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    (HERE / "timelines.json").write_text(json.dumps(timeline, indent=2) + "\n")
    print(json.dumps({"samples": len(manifest["samples"]), "preview_common_gain": common_gain,
                      "event_counts": {k: v["counts"] for k, v in timeline.items()},
                      "previews": list(manifest["previews"])}, indent=2))


if __name__ == "__main__":
    main()
