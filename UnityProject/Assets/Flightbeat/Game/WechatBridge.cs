using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Flightbeat.Game
{
    public interface IWechatBridgeRuntime
    {
        string[] CommandLineArgs();
        Rect ScreenSafeArea();
        int ScreenWidth();
        int ScreenHeight();
    }

    internal sealed class UnityWechatBridgeRuntime : IWechatBridgeRuntime
    {
        public string[] CommandLineArgs() => Environment.GetCommandLineArgs();
        public Rect ScreenSafeArea() => Screen.safeArea;
        public int ScreenWidth() => Screen.width;
        public int ScreenHeight() => Screen.height;
    }

    public static class WechatBridge
    {
        public static bool SimulateMenu;
        private static IWechatBridgeRuntime runtime;
        private static IWechatBridgeRuntime Runtime => runtime ?? (runtime = new UnityWechatBridgeRuntime());

        // src: f54911 / f54912. These are the actual shipped WebGL behaviors.
        public static double Elapsed => -1.0;
        public static int Scenario => Array.IndexOf(Runtime.CommandLineArgs(), "--harness-test") < 0 ? 0 : 2101;

        // src: f54913. Each width/height call is separate in the original.
        public static Rect SafeArea
        {
            get
            {
                if (Array.IndexOf(Runtime.CommandLineArgs(), "--safe-area-test") >= 0)
                    return new Rect(0f, 0.04f, 1f, 0.905f);
                // R1 2026-11-06: Unity's WebGL Screen.safeArea is the whole screen; ask the mini-game host instead.
                if (PlatformInsets.TryRead(out float[] p))
                    return new Rect(p[2] / p[0], (p[1] - p[5]) / p[1], (p[4] - p[2]) / p[0], (p[5] - p[3]) / p[1]);
                Rect safe = Runtime.ScreenSafeArea();
                float x = safe.x / (float)Runtime.ScreenWidth();
                float y = safe.y / (float)Runtime.ScreenHeight();
                float width = safe.width / (float)Runtime.ScreenWidth();
                float height = safe.height / (float)Runtime.ScreenHeight();
                return new Rect(x, y, width, height);
            }
        }

        // src: f54914; all four constants are read from WAT f32 values.
        public static Rect Menu => Array.IndexOf(Runtime.CommandLineArgs(), "--capsule-test") >= 0 || SimulateMenu
            ? new Rect(0.720512807f, 0.0604265407f, 0.223076925f, 0.0379146934f)
            : PlatformInsets.TryRead(out float[] p) && p[8] > p[6] && p[9] > p[7]   // R1 2026-11-06: the host's capsule
                ? new Rect(p[6] / p[0], p[7] / p[1], (p[8] - p[6]) / p[0], (p[9] - p[7]) / p[1])
                : new Rect(1f, 0f, 0f, 0f);

        // src: compiler-shared f897. The original function body is nop.
        public static void Reveal() { }
    }

    // R1 2026-11-06 (user: notches and the capsule, Douyin first): the host's safe area and capsule from
    // Plugins/WebGL/FlightbeatSafeArea.jslib - width, height, safe left/top/right/bottom, capsule left/top/right/bottom, in
    // the window's logical pixels (y down). Read when the layout is configured (start and every resize).
    internal static class PlatformInsets
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string FlightbeatSafeInfo();
#endif
        public static bool TryRead(out float[] values)
        {
            values = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            string text;
            try { text = FlightbeatSafeInfo(); } catch (Exception) { return false; }
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split(',');
            if (parts.Length != 10) return false;
            var v = new float[10];
            for (int i = 0; i < 10; i++)
                if (!float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[i])) return false;
            if (!(v[0] > 0f) || !(v[1] > 0f) || !(v[4] > v[2]) || !(v[5] > v[3])) return false;
            values = v;
            return true;
#else
            return false;
#endif
        }
    }
}
