using UnityEngine;

namespace Flightbeat.Audio
{
    // Native engine boundaries used by the source lifecycle instructions.
    // Rule verification supplies explicit API inputs/traces; production uses Unity.
    public interface IChipAudioLifecycleRuntime : IChipAudioRuntime
    {
        GameObject GameObjectOf(ChipAudio component);
        AudioSource AddSource(GameObject owner);
        void SetPlayOnAwake(AudioSource source, bool value);
        void SetSpatialBlend(AudioSource source, float value);
        void SetLoop(AudioSource source, bool value);
        void SetMute(AudioSource source, bool value);
        AudioClip LoadClip(string path);
        AudioClip GetClip(AudioSource source);
        AudioDataLoadState LoadState(AudioClip clip);
        bool LoadAudioData(AudioClip clip);
        bool IsPlaying(AudioSource source);
        int TimeSamples(AudioSource source);
        double DspTime();
        void PlayScheduled(AudioSource source, double time);
        void Stop(AudioSource source);
        void Pause(AudioSource source);
        void UnPause(AudioSource source);
        bool SourcesEqual(AudioSource a, AudioSource b);
        bool SourcesNotEqual(AudioSource a, AudioSource b);
        void SaveInt(string key, int value);
    }

    internal sealed partial class UnityChipAudioRuntime : IChipAudioLifecycleRuntime
    {
        public GameObject GameObjectOf(ChipAudio component) => component.gameObject;
        public AudioSource AddSource(GameObject owner) => owner.AddComponent<AudioSource>();
        public void SetPlayOnAwake(AudioSource source, bool value) { Observe(54200, source, value: value ? 1 : 0); source.playOnAwake = value; }
        public void SetSpatialBlend(AudioSource source, float value) { Observe(54201, source, value: value); source.spatialBlend = value; }
        public void SetLoop(AudioSource source, bool value) { Observe(54199, source, value: value ? 1 : 0); source.loop = value; }
        public void SetMute(AudioSource source, bool value) { Observe(54202, source, value: value ? 1 : 0); source.mute = value; }
        // Original f55161 uses Resources.Load<AudioClip>. The independent bundle
        // retains those native clips/keys while avoiding a second AAC encoding pass.
        public AudioClip LoadClip(string path) => OriginalAudioAssets.Load(path);
        public AudioClip GetClip(AudioSource source) => source.clip;
        public AudioDataLoadState LoadState(AudioClip clip) => clip.loadState;
        public bool LoadAudioData(AudioClip clip) => clip.LoadAudioData();
        public bool IsPlaying(AudioSource source) => source.isPlaying;
        public int TimeSamples(AudioSource source) => source.timeSamples;
        public double DspTime()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput != null) return (double)Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput(54169);
#endif
            return AudioSettings.dspTime;
        }
        public void PlayScheduled(AudioSource source, double time) { Observe(54193, source, value: time); source.PlayScheduled(time); }
        public void Stop(AudioSource source) { Observe(54195, source); source.Stop(); }
        public void Pause(AudioSource source) { Observe(54196, source); source.Pause(); }
        public void UnPause(AudioSource source) { Observe(54197, source); source.UnPause(); }
        public bool SourcesEqual(AudioSource a, AudioSource b) => a == b;
        public bool SourcesNotEqual(AudioSource a, AudioSource b) => a != b;
        public void SaveInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    }
}
