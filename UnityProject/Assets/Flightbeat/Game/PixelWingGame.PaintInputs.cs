using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // The three Paint-time reads of original f29050 remain Time.unscaledDeltaTime
        // in ordinary players. Fixed CLI observations declare the same zero-delta boundary
        // as Tools/draw/capture_fixed.js, without driving Update or touching saves.
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        public static float? PaintDeltaOverride;
#endif
        private static float PaintDeltaTime
        {
            get
            {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
                if (PaintDeltaOverride.HasValue) return PaintDeltaOverride.Value;
#endif
                return Time.unscaledDeltaTime;
            }
        }
    }
}
