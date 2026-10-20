using System;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Audio
{
    public sealed partial class ChipAudio
    {
        private IChipAudioPlaybackRuntime Playback => (IChipAudioPlaybackRuntime)Runtime;

        // Inline f32 sub/abs/le/select/add sequence from f55173.
        private static float MoveAudio(float current, float target, float step)
        {
            float difference = OriginalMath.Binary32(target - current);
            return Math.Abs(difference) <= step ? target : OriginalMath.Binary32(current + (difference >= 0f ? step : -step));
        }

        // src: ChipAudio.Update f55173. Each native time/delta read remains at
        // its original call site, and values held across callbacks stay local.
        private void Update()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(55173);
            Flightbeat.Diagnostics.RuntimeLoopInputs.Method?.Invoke(55173);
#endif
            float oldDensity = density;
            float densityStep = OriginalMath.Binary32(Playback.UnscaledDeltaTime() * 6f);
            density = MoveAudio(oldDensity, 0f, densityStep);
            if (paused || awaitingRescue) return;
            if (danger > 0.02f && !terminal && Runtime.UnscaledTime() >= alarmAt)
            {
                Runtime.SetVolume(alarm, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(danger * 0.18f) + 0.16f)) * 0.65f));
                Runtime.Play(alarm);
                float now = Runtime.UnscaledTime();
                float currentDanger = danger;
                alarmAt = OriginalMath.Binary32(now + (currentDanger < 0f ? 1.6f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(currentDanger, 1f) * -1.05f) + 1.6f)));
            }
            AudioSource laserSource = laser;
            float laserVolume = Playback.GetVolume(laserSource);
            float laserTarget = 0f;
            if (Runtime.UnscaledTime() < laserUntil) laserTarget = terminal ? 0f : 0.103999995f; // 0x3dd4fdf3
            float laserStep = OriginalMath.Binary32(OriginalMath.Binary32(Playback.UnscaledDeltaTime() * 4f) * 0.65f);
            Runtime.SetVolume(laserSource, MoveAudio(laserVolume, laserTarget, laserStep));
            if (Playback.GetVolume(laser) <= 0f && Engine.IsPlaying(laser)) Engine.Stop(laser);

            float musicNow = Runtime.UnscaledTime();
            float heldDuckUntil = duckUntil;
            AudioSource leadSource = lead;
            float leadVolume = Playback.GetVolume(leadSource);
            bool leadTerminal = terminal;
            bool ducking = musicNow < heldDuckUntil;
            float leadTarget = !leadTerminal && ducking ? 0.196559995f : leadTerminal ? 0f : 0.233999997f;
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            float gardenMusic = GardenMusicGain(musicNow);   // 2026-10-19: deep duck around the finale release
            leadTarget *= gardenMusic;
#endif
            float leadStep = OriginalMath.Binary32(OriginalMath.Binary32(Playback.UnscaledDeltaTime() * 3f) * 0.65f);
            Runtime.SetVolume(leadSource, MoveAudio(leadVolume, leadTarget, leadStep));
            AudioSource backingSource = backing;
            float backingVolume = Playback.GetVolume(backingSource);
            bool backingTerminal = terminal;
            float backingTarget = !backingTerminal && ducking ? 0.245959982f : backingTerminal ? 0f : 0.285999984f;
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            backingTarget *= gardenMusic;
#endif
            float dt = Playback.UnscaledDeltaTime();
            float backingStep = OriginalMath.Binary32((OriginalMath.Binary32(dt + dt)) * 0.65f);
            Runtime.SetVolume(backingSource, MoveAudio(backingVolume, backingTarget, backingStep));
        }

        // Exact seven-source stop sequence shared by down and terminal cues in
        // f55175. Read each field after the preceding native call, not as an array.
        private void StopCombatVoices()
        {
            Engine.Stop(pulse);
            Engine.Stop(noise);
            Engine.Stop(pulse2);
            Engine.Stop(noise2);
            Engine.Stop(laser);
            Engine.Stop(rocket);
            Engine.Stop(alarm);
        }

        // src: ChipAudio.Play f55175. No global paused/muted gate exists here.
        public void Play(string cue)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(55175);
            Flightbeat.Diagnostics.RuntimeLoopInputs.AudioEvent?.Invoke(55175, null, null, 0, cue);
#endif
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            cue = GardenCue(cue);   // 2026-10-20: the finale's maxpower becomes the bubble release (ChipAudio.Garden.cs)
#endif
            if (!clips.TryGetValue(cue, out AudioClip clip) || Playback.ClipIsNull(clip)) return;
            if (Interface.Contains(cue))
            {
                Playback.PlayOneShot(ui, clip, cue == "tick" ? 0.35f : cue == "ui" ? 0.5f : 1f);
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue == "pilot-confirm")
            {
                Runtime.SetClip(comms, clip);
                Runtime.SetVolume(comms, 0.325f);
                Runtime.SetPitch(comms, 1f);
                Runtime.Play(comms);
                float until = OriginalMath.Binary32(Runtime.UnscaledTime() + Playback.ClipLength(clip));
                duckUntil = until;
                pilotUntil = until;
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue.StartsWith("finale-"))
            {
                AudioSource source = cue == "finale-charge" ? superWeapon : finale;
                float finaleVolume = cue == "finale-beam" ? 0.480999976f : cue == "finale-ready" ? 0.233999997f : 0.363999993f;
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
                if (cue == GardenReleaseCue) { source = superWeapon; finaleVolume = GardenReleaseVolume; }   // the beam keeps `finale`
#endif
                Runtime.SetClip(source, clip);
                Runtime.SetVolume(source, finaleVolume);
                Runtime.Play(source);
                float previous = duckUntil;
                float until = OriginalMath.Binary32(Runtime.UnscaledTime() + (cue == "finale-ready" ? 0.4f : Playback.ClipLength(clip)));
                duckUntil = previous > until ? previous : until;
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue.StartsWith("super-"))
            {
                Runtime.SetClip(superWeapon, clip);
                Runtime.SetVolume(superWeapon, cue == "super-impact" ? 0.428999990f : cue == "super-count" ? 0.155999988f : 0.246999994f);
                Runtime.Play(superWeapon);
                float previous = duckUntil;
                float until = OriginalMath.Binary32(Runtime.UnscaledTime() + 0.5f);
                duckUntil = previous > until ? previous : until;
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue == "voice")
            {
                if (paused) return;
                Runtime.SetClip(comms, clip);
                Runtime.SetPitch(comms, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(CueCount % 3)) * 0.065f) + 0.94f));
                Runtime.Play(comms);
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue == "shockwave" || cue == "rescue-wave" || cue == "sacrifice" || cue == "purge")
            {
                Runtime.SetClip(pressure, clip);
                Runtime.SetVolume(pressure, cue == "rescue-wave" || cue == "purge" ? 0.325f : 0.26f);
                Runtime.Play(pressure);
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue == "down")
            {
                awaitingRescue = true;
                Engine.Pause(lead);
                Engine.Pause(backing);
                StopCombatVoices();
                laserUntil = 0f;
            }
            if (cue == "revive")
            {
                terminal = false;
                awaitingRescue = false;
                Engine.UnPause(lead);
                Engine.UnPause(backing);
                Begin();
            }
            if (cue == "launch") Begin();
            if (cue == "shot-max" || cue == "shot-rocket")
            {
                laserUntil = OriginalMath.Binary32(Runtime.UnscaledTime() + 0.18f);
                if (!Engine.IsPlaying(laser))
                {
                    Runtime.SetVolume(laser, 0.0832f); // WAT 0x3daa64c3
                    Runtime.Play(laser);
                    CueCount = unchecked(CueCount + 1);
                }
                if (cue == "shot-max") return;
                if (OriginalMath.Binary32(Runtime.UnscaledTime() - rocketAt) < 0.2f) return;
                rocketAt = Runtime.UnscaledTime();
                Runtime.SetClip(rocket, clip);
                Runtime.Play(rocket);
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue == "hit-rocket")
            {
                if (OriginalMath.Binary32(Runtime.UnscaledTime() - blastAt) < 0.22f) return;
                blastAt = Runtime.UnscaledTime();
                Runtime.SetClip(detonation, clip);
                Runtime.Play(detonation);
                float previous = duckUntil;
                float until = OriginalMath.Binary32(Runtime.UnscaledTime() + 0.12f);
                duckUntil = previous > until ? previous : until;
                CueCount = unchecked(CueCount + 1);
                return;
            }
            if (cue.StartsWith("shot") && OriginalMath.Binary32(Runtime.UnscaledTime() - shotAt) < 0.07f) return;
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (cue.StartsWith("hit") && Runtime.UnscaledTime() - hitAt < GardenHitGap()) return;   // 2026-10-19: 80 -> 110 ms when dense
#else
            if (cue.StartsWith("hit") && OriginalMath.Binary32(Runtime.UnscaledTime() - hitAt) < 0.08f) return;
#endif
            AudioSource selected = accent;
            bool shot = false, hit = false;
            if (cue.StartsWith("shot"))
            {
                shotAt = Runtime.UnscaledTime();
                bool previous = pulseFlip;
                pulseFlip = !previous;
                selected = previous ? pulse2 : pulse;
                density = OriginalMath.WasmMin(OriginalMath.Binary32(density + 0.08f), 1f);
                shot = true;
            }
            else if (cue.StartsWith("hit") || cue == "rush")
            {
                hitAt = Runtime.UnscaledTime();
                bool previous = noiseFlip;
                noiseFlip = !previous;
                selected = previous ? noise2 : noise;
                density = OriginalMath.WasmMin(OriginalMath.Binary32(density + 0.06f), 1f);
                hit = true;
            }
            else
            {
                float now = Runtime.UnscaledTime();
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
                if (!GardenSkipsDuck(cue))   // 2026-10-19: frequent small cues no longer keep the music ducked
#endif
                duckUntil = OriginalMath.Binary32(now + (cue == "maxpower" || cue == "blast" ? 0.35f : 0.18f));
            }
            if (cue == "win" || cue == "lose")
            {
                terminal = true;
                awaitingRescue = false;
                StopCombatVoices();
                laserUntil = 0f;
            }
            float pitch;
            if (shot || hit)
            {
                Runtime.SetVolume(selected, OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(density * -0.25f) + 1f)) * (shot ? 0.42f : 0.36f)) * 0.65f)
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
                    * GardenDenseGain()   // 2026-10-19: a further x0.75 at full density
#endif
                    );
                pitch = OriginalMath.Binary32(OriginalMath.Binary32(Playback.RandomValue() * 0.06f) + 0.97f);
            }
            else
            {
                float volume;
                if (cue.StartsWith("fx-")) volume = cue == "fx-collapse" || cue == "fx-quake" ? 0.26f : 0.195000008f;
                else if (cue == "advance") volume = 0.0519999973f;
                else if (cue == "combo" || cue == "combo-high") volume = 0.195000008f;
                else if (cue == "empty") volume = 0.13f;
                else if (cue == "hurt") volume = 0.325f;
                else if (cue == "merge") volume = 0.299f;
                else if (cue == "maxpower") volume = 0.337999970f;
                else if (cue == "blast") volume = 0.299f;
                else if (cue == "launch") volume = 0.195000008f;
                else volume = 0.168999985f;
                Runtime.SetVolume(selected, volume);
                pitch = 1f;
            }
            Runtime.SetPitch(selected, pitch);
            Runtime.SetClip(selected, clip);
            Runtime.Play(selected);
            CueCount = unchecked(CueCount + 1);
        }
    }
}
