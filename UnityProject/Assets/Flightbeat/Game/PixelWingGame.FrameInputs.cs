using System;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54783 native input boundaries. Player builds retain the original API
        // reads at every call site; declared inputs require an explicit diagnostic.
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        public static Func<int, int, object> FrameInputOverride;
        public static Action<Vector2, Vector2> FrameProjectionObserver;
#endif
        private static int FrameScreenWidth()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (int)FrameInputOverride(50264, 0);
#endif
            return Screen.width;
        }
        private static int FrameScreenHeight()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (int)FrameInputOverride(50265, 0);
#endif
            return Screen.height;
        }
        private static float FrameDelta()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (float)FrameInputOverride(29050, 0);
#endif
            return Time.unscaledDeltaTime;
        }
        private static bool FrameAnyKeyDown()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (bool)FrameInputOverride(12299, 0);
#endif
            return Input.anyKeyDown;
        }
        private static bool FrameKeyDown(KeyCode key)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (bool)FrameInputOverride(12289, (int)key);
#endif
            return Input.GetKeyDown(key);
        }
        private static bool FrameMouseButton(int button)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (bool)FrameInputOverride(12290, button);
#endif
            return Input.GetMouseButton(button);
        }
        private static bool FrameMouseDown(int button)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (bool)FrameInputOverride(12291, button);
#endif
            return Input.GetMouseButtonDown(button);
        }
        private static bool FrameMouseUp(int button)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (bool)FrameInputOverride(12292, button);
#endif
            return Input.GetMouseButtonUp(button);
        }
        private static Vector3 FrameMousePosition()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (FrameInputOverride != null) return (Vector3)FrameInputOverride(12300, 0);
#endif
            return Input.mousePosition;
        }
    }
}
