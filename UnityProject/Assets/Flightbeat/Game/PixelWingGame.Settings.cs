using Flightbeat.Model;
using Flightbeat.Rendering;
using System;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private static readonly bool ShowTuning = false; // f54861 static +84
        public static string VersionText => "版本 " + Application.version; // f54564

        // src: DrawVersion f54565. The underline and hit box are independent of
        // the pressed caption's vertical displacement.
        private void DrawVersion(PixelSurface s, float cx, float y, bool tappable)
        {
            tappable &= DevelopmentTools.Available;
            string text = VersionText;
            float width = OriginalMath.Binary32(PixelSurface.Measure(text, 1.3f) + 28f), pulse = 0f;
            if (tappable) { HitBox(s, 740, OriginalMath.Binary32(cx + OriginalMath.Binary32(width * -.5f)), OriginalMath.Binary32(y + -6f), width, 28f); pulse = PressPulse(740); }
            s.TextF(text, cx, OriginalMath.Binary32(pulse + y), 1.3f, A(uiDim, .9f), 0);
            if (tappable) s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(cx + OriginalMath.Binary32(width * -.5f)) + 14f), OriginalMath.Binary32(y + 21f), OriginalMath.Binary32(width + -28f), 1f, A(uiDim, .35f));
        }
        // f54568. Opening the version panel does not itself pause a battle.
        private bool OnDevUi(int id) => DevTools?.OnDevUi(id) ?? false;
        // f54566. WAT computes the animated top as offset + (half-height - 339).
        private void DrawDevPanel(PixelSurface s) => DevTools?.DrawDevPanel(s);
        // src: DrawDeckSettings f54596. Original Test build hides tuning through
        // the static field above; music/effects/light switches remain separate.

    }
}
