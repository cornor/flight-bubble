using System;
using System.IO;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54569. Clear the same overlays/aim state; do not change finaleGuide.
        private void SetBroadcastAutomatic(bool enabled) => DevTools?.SetBroadcastAutomatic(enabled);
        // f54570 / f54867. StartSortie follows ShowAd immediately in the source.
        private void BeginBroadcastSortie(int next) => DevTools?.BeginBroadcastSortie(next);
        // f54706 / shared transition closures f53969; NextLevel is f55152.
        private void TickBroadcast(float dt) => DevTools?.TickBroadcast(dt);
        // f54709. First matching published hit; coordinates pass through InverseY.
        public bool TapUi(int id)
        {
            foreach (UiHit hit in uiHits)
            {
                if (hit.id != id) continue;
                float y = ScreenLayout.InverseY(hit.y + hit.h * .5f);
                Tap(new Vector2(hit.x + hit.w * .5f, y)); return true;
            }
            return false;
        }
        // f54707. Empty output skips capture; named frames are optional diagnostics.
        private void SaveBroadcastFrame(string state) => DevTools?.SaveBroadcastFrame(state);
        // f54711. Native Unity rendering with the original texture formats,
        // relayout order, safe-area override and display restoration.
        public Texture2D CaptureFrame(int width, int height, Rect? safeOverride) => DevTools?.CaptureFrame(width, height, safeOverride) ?? throw new System.NotSupportedException("Development tools are excluded from this build");
    }
}
