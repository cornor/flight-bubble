using System;
using Flightbeat.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Flightbeat.Audio
{
    public sealed partial class ChipAudio
    {
        private AudioSource lead, backing, pulse, noise, accent, laser, rocket, detonation, alarm;
        private AudioSource pressure, superWeapon, ui, pulse2, noise2, finale;
        private bool pulseFlip, noiseFlip;
        // src: ChipAudio..ctor f55176; dictionary initializer is in Speech.
        // Original packed stores at +92 and +100 contain four exact -10f values.
        private float shotAt = -10f, hitAt = -10f, rocketAt = -10f, blastAt = -10f;
        private float laserUntil, duckUntil, alarmAt, density, danger;
        private bool started, terminal, awaitingRescue;
        private IChipAudioLifecycleRuntime Engine => (IChipAudioLifecycleRuntime)Runtime;

        // src: ChipAudio..cctor f55177, original HashSet insertion order.
        private static readonly HashSet<string> Interface = new HashSet<string>
        {
            "ui", "page", "zoom", "equip", "unequip", "recycle", "upgrade", "buy",
            "gacha", "land", "legend", "tick", "warp", "whoosh", "reward"
        };

        // src: ChipAudio.get_MusicMuted f47149 / set_MusicMuted f55155.
        public bool MusicMuted { get; private set; }
        // src: ChipAudio.get_EffectsMuted f55156 / set_EffectsMuted f55157.
        public bool EffectsMuted { get; private set; }
        // src: ChipAudio.get_Muted f55158.
        public bool Muted => MusicMuted && EffectsMuted;
        // src: ChipAudio.get_MusicPlaying f55159, with original short-circuit.
        public bool MusicPlaying => Engine.IsPlaying(lead) && Engine.IsPlaying(backing);
        // src: ChipAudio.get_MusicSample f55160.
        public int MusicSample => Engine.TimeSamples(lead);

        // Common native call sequence in f55161/f55164, before storing each field.
        private AudioSource CreateVoiceAtVolume(float volume)
        {
            AudioSource source = Engine.AddSource(Engine.GameObjectOf(this));
            Engine.SetPlayOnAwake(source, false);
            Runtime.SetVolume(source, volume);
            Engine.SetSpatialBlend(source, 0f);
            return source;
        }

        // src: ChipAudio.Voice f55164.
        private AudioSource Voice(float volume) => CreateVoiceAtVolume(OriginalMath.Binary32(volume * 0.65f));

        // src: ChipAudio.Awake f55161. Decimal literals below round to the WAT
        // f32 values, including the one-ULP details lost in the decompiler text.
        private void Awake()
        {
            lead = CreateVoiceAtVolume(0.233999997f);       // 0x3e6f9db2
            backing = CreateVoiceAtVolume(0.285999984f);    // 0x3e926e97
            pulse = CreateVoiceAtVolume(0.272999972f);      // 0x3e8bc6a7
            noise = CreateVoiceAtVolume(0.233999997f);
            accent = CreateVoiceAtVolume(0.195000008f);     // 0x3e47ae15
            pulse2 = CreateVoiceAtVolume(0.272999972f);
            noise2 = CreateVoiceAtVolume(0.233999997f);
            laser = CreateVoiceAtVolume(0.103999995f);      // 0x3dd4fdf3
            rocket = CreateVoiceAtVolume(0.233999997f);
            detonation = CreateVoiceAtVolume(0.233999997f);
            alarm = CreateVoiceAtVolume(0.181999996f);      // 0x3e3a5e35
            comms = CreateVoiceAtVolume(0.155999988f);      // 0x3e1fbe76
            pressure = CreateVoiceAtVolume(0.272999972f);
            superWeapon = CreateVoiceAtVolume(0.357499987f);// 0x3eb70a3d
            ui = CreateVoiceAtVolume(0.259999990f);         // 0x3e851eb8
            finale = CreateVoiceAtVolume(0.390000015f);    // 0x3ec7ae15
            Runtime.SetClip(alarm, Engine.LoadClip("Audio/critical"));
            Runtime.SetClip(laser, Engine.LoadClip("Audio/laser-loop"));
            Engine.SetLoop(laser, true);
            Runtime.SetClip(lead, Engine.LoadClip("Audio/orbit-drive-lead"));
            Runtime.SetClip(backing, Engine.LoadClip("Audio/orbit-drive-backing"));
            AudioSource leadBeforeLoop = lead;
            Engine.SetLoop(backing, true);
            Engine.SetLoop(leadBeforeLoop, true);
            MusicMuted = false;
            EffectsMuted = false;
            ApplyMute();
            string[] names =
            {
                "voice-a", "voice-e", "voice-o", "pilot-confirm", "legend-pulse", "super-lock", "super-impact", "super-count",
                "voice", "shockwave", "rescue-wave", "down", "revive", "assembly", "shot-twin", "shot-max", "shot-rocket", "hit-rocket",
                "advance", "maxpower", "combo", "combo-high", "reward", "shuffle", "blast", "warning", "arrival", "ready", "rush",
                "launch", "shot", "hit", "hit-twin", "hit-max", "merge", "recall", "empty", "deny", "hurt", "win", "lose", "sacrifice",
                "purge", "ui", "page", "zoom", "equip", "unequip", "recycle", "upgrade", "buy", "gacha", "land", "legend", "tick",
                "warp", "whoosh", "finale-ready", "finale-charge", "finale-beam", "shot-needle", "hit-needle", "fx-pierce", "shot-quake",
                "hit-quake", "fx-quake", "fx-collapse", "shot-blaze", "hit-blaze", "fx-burn", "shot-arc", "hit-arc", "fx-arc", "fx-split"
            };
            foreach (string name in names) clips[name] = Engine.LoadClip("Audio/" + name);
            foreach (AudioClip clip in clips.Values)
                if (Runtime.ClipAlive(clip) && Engine.LoadState(clip) == AudioDataLoadState.Unloaded) Engine.LoadAudioData(clip);
            foreach (AudioSource source in new[] { lead, backing, laser, alarm })
                if (Runtime.ClipAlive(Engine.GetClip(source)) && Engine.LoadState(Engine.GetClip(source)) == AudioDataLoadState.Unloaded)
                    Engine.LoadAudioData(Engine.GetClip(source));
        }

        // src: ChipAudio.Begin f55165. The DSP input and offset are f64.
        public void Begin()
        {
            if (started || terminal) return;
            started = true;
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            GardenPickMusic();   // 2026-10-18: home and level music differ (ChipAudio.Garden.cs)
#endif
            double when = Engine.DspTime() + 0.04;
            Engine.PlayScheduled(lead, when);
            Engine.PlayScheduled(backing, when);
        }

        // src: ChipAudio.Voices f55167. A fresh array in field order each call.
        private AudioSource[] Voices() => new[]
        {
            lead, backing, pulse, noise, accent, laser, rocket, detonation, alarm, comms,
            pressure, superWeapon, ui, pulse2, noise2, finale
        };

        // src: ChipAudio.ResetMusic f55166; ui is excluded using Unity identity.
        public void ResetMusic()
        {
            foreach (AudioSource source in Voices())
                if (Engine.SourcesNotEqual(source, ui)) Engine.Stop(source);
            pilotUntil = 0f; shotAt = -10f;
            started = paused = terminal = awaitingRescue = false; danger = 0f;
            duckUntil = 0f; alarmAt = 0f;
            blastAt = -10f; laserUntil = 0f;
            hitAt = -10f; rocketAt = -10f;
            Runtime.SetVolume(lead, 0.233999997f);
            Runtime.SetVolume(backing, 0.285999984f);
            Runtime.SetVolume(comms, 0.155999988f);
        }

        // src: ChipAudio.SetPaused f55168. Repeat calls still issue native calls.
        public void SetPaused(bool value)
        {
            paused = value;
            foreach (AudioSource source in Voices())
            {
                if (Engine.SourcesEqual(source, ui)) continue;
                if (value) Engine.Pause(source);
                else
                {
                    if (awaitingRescue && (!Engine.SourcesNotEqual(source, lead) || !Engine.SourcesNotEqual(source, backing))) continue;
                    Engine.UnPause(source);
                }
            }
        }

        // src: ChipAudio.SetDanger f55169.
        public void SetDanger(float value)
        {
            if (value <= 0f && danger > 0f) Engine.Stop(alarm);
            danger = value;
        }

        // src: ChipAudio.ApplyMute f55162. Cache references/flags before calls,
        // retaining the chained-property assignment's reversed effects order.
        private void ApplyMute()
        {
            AudioSource first = lead;
            bool music = MusicMuted;
            Engine.SetMute(backing, music);
            Engine.SetMute(first, music);
            AudioSource[] effects = { pulse, noise, pulse2, noise2, accent, laser, rocket, detonation, alarm, comms, pressure, superWeapon, ui, finale };
            bool muted = EffectsMuted;
            for (int i = effects.Length - 1; i >= 0; i--) Engine.SetMute(effects[i], muted);
        }

        // src: ChipAudio.ToggleMusic f55170.
        public void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            Engine.SaveInt("pw.music-muted", MusicMuted ? 1 : 0);
            ApplyMute();
        }
        // src: ChipAudio.ToggleEffects f55171.
        public void ToggleEffects()
        {
            EffectsMuted = !EffectsMuted;
            Engine.SaveInt("pw.sfx-muted", EffectsMuted ? 1 : 0);
            ApplyMute();
        }
        // src: ChipAudio.Toggle f55172, both keys receive the same local value.
        public void Toggle()
        {
            bool muted = !Muted;
            MusicMuted = muted;
            EffectsMuted = muted;
            Engine.SaveInt("pw.music-muted", muted ? 1 : 0);
            Engine.SaveInt("pw.sfx-muted", muted ? 1 : 0);
            ApplyMute();
        }

    }
}
