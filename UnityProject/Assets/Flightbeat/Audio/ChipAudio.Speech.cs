using System;
using Flightbeat.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Flightbeat.Audio
{
    // Exact Unity API boundary, shared by production and API-trace verification.
    // There is no game rule or fallback audio implementation in this interface.
    public interface IChipAudioRuntime
    {
        float UnscaledTime();
        bool ClipAlive(AudioClip clip);
        void SetClip(AudioSource source, AudioClip clip);
        void SetVolume(AudioSource source, float volume);
        void SetPitch(AudioSource source, float pitch);
        void Play(AudioSource source);
    }

    internal sealed partial class UnityChipAudioRuntime : IChipAudioRuntime
    {
        internal static readonly UnityChipAudioRuntime Instance = new UnityChipAudioRuntime();
        public float UnscaledTime()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput != null) return (float)Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput(29049);
#endif
            return Time.unscaledTime;
        }
        public bool ClipAlive(AudioClip clip) => clip;
        public void SetClip(AudioSource source, AudioClip clip) { Observe(54191, source, clip); source.clip = clip; }
        public void SetVolume(AudioSource source, float volume) { Observe(54188, source, value: volume); source.volume = volume; }
        public void SetPitch(AudioSource source, float pitch) { Observe(54182, source, value: pitch); source.pitch = pitch; }
        public void Play(AudioSource source) { Observe(54192, source); source.Play(); }
    }

    public sealed partial class ChipAudio
    {
        private AudioSource comms; // source +52
        private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(); // +84; f55176.
        private float pilotUntil; // +88
        private bool paused; // +125
        private IChipAudioRuntime runtime;
        private IChipAudioRuntime Runtime => runtime ?? UnityChipAudioRuntime.Instance;

        // src: ChipAudio.get_CueCount f13573 / set_CueCount f13574.
        public int CueCount { get; private set; }
        // src: ChipAudio.get_SpokenGlyphs f9380 / set_SpokenGlyphs f13575.
        public int SpokenGlyphs { get; private set; }

        // src: ChipAudio.SpeakGlyph f55174. Source gates on paused/pilotUntil;
        // EffectsMuted is applied to AudioSources elsewhere, not checked here.
        public void SpeakGlyph(char glyph, int index)
        {
            if (paused) return;
            if (Runtime.UnscaledTime() < pilotUntil) return;
            string[] names = { "voice-a", "voice-e", "voice-o" }; // slots 5859868/72/76
            if (!clips.TryGetValue(names[unchecked(glyph + index) % 3], out AudioClip clip)) return;
            if (!Runtime.ClipAlive(clip)) return;
            Runtime.SetClip(comms, clip);
            Runtime.SetVolume(comms, 0.13f);
            Runtime.SetPitch(comms, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(index % 4)) * 0.035f) + 0.96f));
            Runtime.Play(comms);
            CueCount = unchecked(CueCount + 1);
            SpokenGlyphs = unchecked(SpokenGlyphs + 1);
        }
    }
}
