using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54520. Icon bounce is independent of panel alpha; the source draws
        // the white icon at full alpha and only gates it by its animated size.
        private void RewardTile(PixelSurface s, float x, float y, float width, int icon,
            string amount, string tag, float age, float alpha)
        {
            float appear = OutBack(OriginalMath.Binary32(age / .35f));
            if (age < 0f) appear = 0f;
            Color accent = icon == 1 ? uiGold : uiCyan;
            Plate(s, x, y, width, 76f, SourceColor(4489188110456051466UL, 1050253722u, alpha),
                SourceColor(4447394707105235599UL, 1041194025u, alpha), A(accent, OriginalMath.Binary32(alpha * .8f)), 8f);
            float size = OriginalMath.Binary32(appear * 48f), cx = OriginalMath.Binary32(x + 34f), cy = OriginalMath.Binary32(y + 38f), glowSize = OriginalMath.Binary32(appear * 80f);
            s.Glow(cx, cy, glowSize, glowSize, A(accent, OriginalMath.Binary32(alpha * .35f)), 0f);
            if (size > 1f) DeckIcon(s, icon, cx, cy, size, Color.white);
            float available = OriginalMath.Binary32(width + -72f);
            float textSize = PixelSurface.Measure(amount, 3f) <= available ? 3f : 2f;
            float offset = OriginalMath.Binary32((OriginalMath.Binary32(available - PixelSurface.Measure(amount, textSize))) * .5f);
            s.Text(amount, OriginalMath.Binary32(OriginalMath.Binary32(x + 64f) + offset), OriginalMath.Binary32((textSize > 2f ? 38f : 42f) + y), textSize,
                A(icon == 1 ? uiGoldHi : uiCyan, alpha), false);
            if (tag != null && age > .3f)
            {
                float tagWidth = OriginalMath.Binary32(PixelSurface.Measure(tag, .95f) + 12f), right = OriginalMath.Binary32(x + width);
                Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(right - tagWidth) + -6f), OriginalMath.Binary32(y + 6f), tagWidth, 20f,
                    SourceColor(4525216907506221056UL, 1045220557u, alpha),
                    SourceColor(4474776592533631795UL, 1034147594u, alpha), A(uiInk, alpha), 4f);
                s.TextF(tag, OriginalMath.Binary32(OriginalMath.Binary32(right + -6f) + OriginalMath.Binary32(tagWidth * -.5f)), OriginalMath.Binary32(y + 8f), .95f, new Color(1f, 1f, 1f, alpha), 0);
            }
            if (age > 0f && age < .4f)
            {
                float t = OriginalMath.Binary32(age / .4f);
                s.Ring(new Vector2(OriginalMath.Binary32(x + 36f), cy), OriginalMath.Binary32(t * 60f), new Color(1f, 1f, 1f, OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) * alpha)), 2f, 24, 0f, 360f);
            }
        }

        // f54521. The first two cells have a distinct source color; 2..4 and
        // 5..9 are separate bands. Do not replace with the exit prompt's palette.
        private void DrawStreakMeter(PixelSurface s, float cx, float y, int streak, float alpha, float age)
        {
            if (streak <= 0) return;
            float progress = Clamp01(OriginalMath.Binary32(age / .6f));
            int ceiling = CommsInt(OriginalMath.Binary32((float)Math.Ceiling(OriginalMath.Binary32(progress * OriginalMath.Binary32((float)streak)))));
            int shown = ceiling > streak ? streak : ceiling;
            int percent = FleetCatalog.StreakPercent(streak);
            float start = OriginalMath.Binary32(OriginalMath.Binary32(cx + -128f) + 40f);
            DrawFlame(s, OriginalMath.Binary32(start + -66f), OriginalMath.Binary32(y + 12f), 10f, y);
            s.TextF(streak.ToString() + "连胜", OriginalMath.Binary32(start + -40f), OriginalMath.Binary32(y + 2f), 1.05f, A(uiGoldHi, alpha), 0);
            for (int cell = 0; cell < 10; cell++)
            {
                float x = OriginalMath.Binary32(start + OriginalMath.Binary32(OriginalMath.Binary32((float)cell) * 26f));
                s.Rect(x, OriginalMath.Binary32(y + 4f), 22f, 14f, SourceColor(4405601299482269450UL, 1017370378u, alpha));
                if (cell >= shown) continue;
                Color color = cell < 2 ? SourceColor(4559804549723848704UL, 1050253722u, alpha)
                    : new Color(1f, cell < 5 ? .55f : .3f, cell < 5 ? .2f : .15f, alpha);
                s.Rect(OriginalMath.Binary32(x + 1f), OriginalMath.Binary32(y + 5f), 20f, 12f, color);
                if (progress < 1f && cell == unchecked(shown - 1))
                {
                    color.a = OriginalMath.Binary32(alpha * .6f);
                    s.Glow(OriginalMath.Binary32(x + 11f), OriginalMath.Binary32(y + 11f), 40f, 30f, color, 0f);
                }
            }
            s.TextF("+" + percent.ToString() + "%", OriginalMath.Binary32(OriginalMath.Binary32(start + 256f) + 26f), OriginalMath.Binary32(y + 2f), 1.05f,
                SourceColor(4550437062842515456UL, 1050253722u, alpha), 0);
        }
    }
}
