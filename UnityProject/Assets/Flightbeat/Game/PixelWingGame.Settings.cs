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
        private void DrawDeckSettings(PixelSurface s)
        {
            Backdrop(s, .8f);
            float eased = OutBack(OriginalMath.Binary32(Since(250) / .3f));
            float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - eased)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -210f)));
            Card(s, 60f, y, 420f, ShowTuning ? 490f : 420f, uiEdge, Clamp01(OriginalMath.Binary32(eased * 1.2f)));
            Title(s, "设置", 270f, OriginalMath.Binary32(y + 22f), 2.2f, uiText, uiInk);
            string[] labels = { "音乐", "音效", "光效" };
            bool[] enabled = { !audioChip.MusicMuted, !audioChip.EffectsMuted, effects.Enabled };
            float rowsTop = OriginalMath.Binary32(y + 86f);
            for (int i = 0; i != 3; i++)
            {
                float rowY = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32((float)(i * 58)));
                HitBox(s, i + 250, 84f, rowY, 372f, 48f);
                Plate(s, 84f, rowY, 372f, 48f, new Color(.07f, .17f, .25f, 1f),
                    new Color(.03f, .09f, .14f, 1f), A(uiEdge, .7f), 6f, 1f);
                Label(s, labels[i], 108f, OriginalMath.Binary32(rowY + 12f), 1.6f, uiText, -1);
                // WAT packs the original two switch colors before these masks.
                uint packed = enabled[i] ? unchecked((uint)-6898136) : unchecked((uint)-11516356);
                var tint = new Color(OriginalMath.Binary32(OriginalMath.Binary32((float)(packed & 60u)) / 255f), OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 8) & 254u)) / 255f),
                    OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 16) & 214u)) / 255f), 1f);
                float toggleY = OriginalMath.Binary32(rowY + 11f);
                s.Rect(372f, OriginalMath.Binary32(toggleY + 2f), 64f, 22f, tint);
                s.Rect(374f, toggleY, 60f, 26f, tint);
                s.Rect(enabled[i] ? 410f : 376f, OriginalMath.Binary32(toggleY + 3f), 26f, 20f, Color.white);
            }
            Button(s, 253, 84f, OriginalMath.Binary32(y + 266f), 372f, 54f, "完成", Btn.Primary, 2f);
            Button(s, 299, 84f, OriginalMath.Binary32(y + 330f), 372f, 40f, "重温飞行训练", Btn.Ghost, 1.25f);
            if (ShowTuning) Button(s, 254, 84f, OriginalMath.Binary32(y + 390f), 372f, 40f, "数值调参面板", Btn.Ghost, 1.2f);
            DrawVersion(s, 270f, OriginalMath.Binary32(y + (ShowTuning ? 446f : 382f)), true);
        }
    }
}
