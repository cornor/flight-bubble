using System;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        // Observe the engine's real Awake, without substituting its body.
        public static Action<PixelWingGame> StartupObserver;
        // Guid.NewGuid native boundary f70385, called from f54696.
        public static Func<Guid> StartupGuidOverride;
        // f53952 -> f28835(0, upper). Explicit API input for CLI replay only.
        public static Func<int, int> StartupRandomOverride;
#endif
        private static int FleetRandom(int upper)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (StartupRandomOverride != null) return StartupRandomOverride(upper);
#endif
            return UnityEngine.Random.Range(0, upper);
        }
        private static Guid SortieGuid()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (StartupGuidOverride != null) return StartupGuidOverride();
#endif
            return Guid.NewGuid();
        }
    }
}
