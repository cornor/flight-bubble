using System;
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
            : new Rect(1f, 0f, 0f, 0f);

        // src: compiler-shared f897. The original function body is nop.
        public static void Reveal() { }
    }
}
