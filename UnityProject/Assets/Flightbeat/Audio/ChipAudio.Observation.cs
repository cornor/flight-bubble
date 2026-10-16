using System.Diagnostics;
using UnityEngine;

namespace Flightbeat.Audio
{
    internal sealed partial class UnityChipAudioRuntime
    {
        // Observe the original API entry boundary; never replace the call or its
        // argument. Native details are read by the explicit diagnostic consumer.
        [Conditional("UNITY_EDITOR"), Conditional("FLIGHTBEAT_CLI_PROBE")]
        private static void Observe(int function, AudioSource source, AudioClip clip = null, double value = 0)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            Flightbeat.Diagnostics.RuntimeLoopInputs.AudioEvent?.Invoke(function, source, clip, value, null);
#endif
        }
    }
}
