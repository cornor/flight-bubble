using System;
using Flightbeat.Diagnostics;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.DevTools
{
    // Assembly boundary only: retain f54773's opt-in component creation order.
    internal static class DevToolsRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Register()
        {
            DevelopmentTools.Factory = game => new GameTools(game);
            RuntimeLoopInputs.SkipGameBootstrap = () => NativeAudioPlaybackProbe.IsolatedSceneRequested;
            RuntimeLoopInputs.InstallCaptureComponents = (game, args) =>
            {
                if (Array.IndexOf(args, "--review") >= 0)
                    game.gameObject.AddComponent<ReviewCapture>();
                if (Array.IndexOf(args, "--onboarding-test") >= 0)
                    game.gameObject.AddComponent<OnboardingCapture>();
            };
        }
    }
}
