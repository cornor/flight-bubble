using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54819. Safe-area fill, pause hit region, level badge and progress bar.
        private void DrawHeader(PixelSurface s)
        {
            bool mapping = s.MapLayout;
            s.MapLayout = false;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Top,
                SourceColor(4392630932014880522UL, 4572774917131846287UL));
            s.MapLayout = mapping;
            ulong[] gradient = { 4571333766627154985UL, 4567730886066265129UL, 4564128005505375273UL,
                4560525124944485417UL, 4556922248678562857UL, 4553319368117673001UL };
            for (int n = 0; n < gradient.Length; n++)
                s.Rect(10f, OriginalMath.Binary32((float)(n * 14)), 520f, 14.5f, SourceColor(4405601299459251110UL, gradient[n]));
            s.Rect(10f, 83f, 520f, 1f, SourceColor(4532422664312900813UL, 4539628425448102298UL));
            HitBox(s, 1, 12f, 2f, 50f, 50f);
            float pulse = PressPulse(1), inset = OriginalMath.Binary32(pulse + pulse), size = OriginalMath.Binary32(OriginalMath.Binary32(pulse * -4f) + 42f);
            Plate(s, OriginalMath.Binary32(inset + 16f), OriginalMath.Binary32(inset + 6f), size, size, uiPanelHi, uiPanel, uiEdge);
            s.Rect(30f, 18f, 5f, 18f, uiText);
            s.Rect(39f, 18f, 5f, 18f, uiText);
            int difficulty = Battle.Level.difficulty;
            Color badge = difficulty == 2 ? uiRed : difficulty == 1 ? new Color(1f, .62f, .2f, 1f) : uiGold;
            Plate(s, 68f, 9f, 124f, 36f, SourceColor(4489188110456051466UL, 4572054341897787802UL),
                SourceColor(4447394707105235599UL, 4572054341889399194UL), A(badge, .85f));
            Label(s, "第", 90f, 17f, 1.3f, uiText);
            s.Text(unchecked(levelIndex + 1).ToString("D2"), 110f, 17f, 2f, badge, false);
            Label(s, "关", 165f, 17f, 1.3f, uiText);
            if (difficulty > 0)
            {
                float glow = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 6f)) * .35f) + .65f);
                Plate(s, 198f, 13f, 58f, 28f, A(badge, .3f), A(badge, .12f), A(badge, glow), 5f);
                Label(s, difficulty == 2 ? "极难" : "困难", 227f, 17f, 1.2f, badge);
            }
            else DrawRangeChip(s);
            float progress = Battle.Progress;
            s.Rect(16f, 58f, 508f, 14f, SourceColor(4405601299457103626UL, 4575657222441229353UL));
            s.Border(16f, 58f, 508f, 14f, SourceColor(4509392495616230533UL, 4575657222461243585UL), 1f);
            float width = OriginalMath.Binary32(progress * 504f);
            if (width > 0f)
            {
                s.Rect(18f, 60f, width, 10f, SourceColor(4558985074232586481UL, 4575657222469500607UL));
                s.Rect(18f, 60f, width, 3f, SourceColor(4575657222468184747UL, 4575657222471869155UL));
                float end = OriginalMath.Binary32(width + 18f);
                for (float x = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(clock * 40f), 16f) + 18f); x < OriginalMath.Binary32(end + -4f); x  = OriginalMath.Binary32(x+(16f)))
                    s.Rect(x, 63f, 4f, 4f, SourceColor(4575657222473777152UL, 4483423504166813696UL));
                s.Glow(end, 65f, 26f, 22f, SourceColor(4575657222458677658UL, 4554039943400888730UL), 0f);
            }
            // WAT folded constants; calculating n*50.8 at runtime changes rounding.
            float[] ticks = { 66.80000305175781f, 117.5999984741211f, 168.39999389648438f,
                219.1999969482422f, 270f, 320.79998779296875f, 371.6000061035156f,
                422.3999938964844f, 473.20001220703125f };
            foreach (float x in ticks) s.Rect(x, 60f, 1f, 10f, SourceColor(0UL, 4518011145319088128UL));
            float value = OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(progress * 100f)));
            int percentage = Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;
            string text = percentage.ToString() + "%";
            s.Text(text, OriginalMath.Binary32(520f - PixelSurface.Measure(percentage.ToString() + "%", 1f)), 61f, 1f, progress > .55f ? uiInk : uiText, false);
        }

        // f54833. The range label is omitted only by the original lesson guard.
        private void DrawRangeChip(PixelSurface s)
        {
            int lesson = Battle.Level.lesson;
            if (lesson <= 0) return;
            string text = "靶场训练 " + lesson.ToString() + "/2";
            float width = OriginalMath.Binary32(PixelSurface.Measure(text, 1.1f) + 22f);
            Plate(s, 198f, 13f, width, 28f, SourceColor(4489188110450347213UL, 4572054341887050383UL),
                SourceColor(4447394707099866890UL, 4572054341875977421UL), A(uiGreen, .85f), 5f);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + 198f), 18f, 1.1f, uiGreen, 0);
        }
    }
}
