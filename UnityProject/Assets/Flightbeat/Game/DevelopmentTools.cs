using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    // Optional tool boundary, not a gameplay substitute. Only the separately
    // constrained Flightbeat.DevTools assembly installs implementations.
    public static class DevelopmentTools
    {
        public static Func<PixelWingGame, IGameDevTools> Factory;
        public static bool Available => Factory != null;
    }

    public interface IGameDevTools
    {
        void InitializeBroadcast(string[] args);
        void SetBroadcastAutomatic(bool enabled);
        void BeginBroadcastSortie(int next);
        void TickBroadcast(float dt);
        void SaveBroadcastFrame(string state);
        Texture2D CaptureFrame(int width, int height, Rect? safeOverride);
        void ReportBroadcast();
        bool OnDevUi(int id);
        void DrawDevPanel(PixelSurface surface);
        void TuningTap(int index);
        void DrawTuning(PixelSurface surface);
        IBroadcastPilot CreatePilot(int colors, int seed);
    }

    public partial class PixelWingGame
    {
        private IGameDevTools developmentTools;
        private IGameDevTools DevTools => developmentTools ?? (developmentTools = DevelopmentTools.Factory?.Invoke(this));
    }
}
