#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
namespace Flightbeat.Diagnostics
{
    // Only an explicit CLI diagnostic installs these native API inputs. No
    // gameplay state, rule, resource, or callback body is substituted.
    public static class RuntimeLoopInputs
    {
        // Optional tooling registers before scene load. Runtime never references
        // the DevTools assembly; these hooks are absent from ordinary players.
        public static Func<IDisposable> BeginMeshObservation;
        public static Func<bool> SkipGameBootstrap;
        public static Action<Flightbeat.Game.PixelWingGame, string[]> InstallCaptureComponents;
        public static Func<int, double> AudioInput;
        public static Action<int> Method;
        public static Action<int, bool> AudioMethod;
        public static Action<int, UnityEngine.AudioSource, UnityEngine.AudioClip, double, string> AudioEvent;
        public readonly struct AudioMethodScope : IDisposable
        {
            private readonly Action<int, bool> observer;
            private readonly int function;
            public AudioMethodScope(int function)
            { this.function = function; observer = AudioMethod; observer?.Invoke(function, true); }
            public void Dispose() => observer?.Invoke(function, false);
        }
        // Read-only, explicit CLI recording after original f54405 Paint returns.
        public static Action<Flightbeat.Rendering.PixelSurface, UnityEngine.UI.VertexHelper> MeshOutput;
    }
}
#endif
