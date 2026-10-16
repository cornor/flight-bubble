using System.Runtime.InteropServices;

namespace Flightbeat.Game
{
    // Source host: Reverse/input/framework.js, _PixelWingBroadcastStatus.
    // Native Unity has no DOM: retain the exact JSON for native diagnostics.
    // WebGL also delivers it to the original optional broadcast-status element.
    public static class BroadcastHost
    {
        public static string StatusJson { get; private set; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PixelWingBroadcastStatus(string json);
#endif
        public static void Publish(string json)
        {
            StatusJson = json;
#if UNITY_WEBGL && !UNITY_EDITOR
            PixelWingBroadcastStatus(json);
#endif
        }
    }
}
