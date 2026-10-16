using UnityEngine;

namespace Flightbeat.Audio
{
    public interface IChipAudioPlaybackRuntime : IChipAudioLifecycleRuntime
    {
        float UnscaledDeltaTime();
        float RandomValue();
        float GetVolume(AudioSource source);
        float ClipLength(AudioClip clip);
        bool ClipIsNull(AudioClip clip);
        void PlayOneShot(AudioSource source, AudioClip clip, float volume);
    }

    internal sealed partial class UnityChipAudioRuntime : IChipAudioPlaybackRuntime
    {
        public float UnscaledDeltaTime()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput != null) return (float)Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput(29050);
#endif
            return Time.unscaledDeltaTime;
        }
        public float RandomValue()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput != null) return (float)Flightbeat.Diagnostics.RuntimeLoopInputs.AudioInput(28836);
#endif
            return Random.value;
        }
        public float GetVolume(AudioSource source) => source.volume;
        public float ClipLength(AudioClip clip) => clip.length;
        public bool ClipIsNull(AudioClip clip) => clip == null;
        public void PlayOneShot(AudioSource source, AudioClip clip, float volume) { Observe(54194, source, clip, volume); source.PlayOneShot(clip, volume); }
    }
}
