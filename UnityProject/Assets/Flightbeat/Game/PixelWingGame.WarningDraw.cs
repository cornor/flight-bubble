using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54822. Warning smoothing uses Unity's unscaled delta in this draw body.
        private void DrawWarning(PixelSurface s)
        {
            float danger = Battle.Danger;
            float target = Battle.State == Phase.Playing && tutorialStep < 0 && Battle.ActiveFinale == null ? danger : 0f;
            float delta = OriginalMath.Binary32(PaintDeltaTime * 3f), difference = OriginalMath.Binary32(target - warningLevel);
            warningLevel = Math.Abs(difference) <= delta ? target : OriginalMath.Binary32(warningLevel + (difference >= 0f ? delta : -delta));
            float level = warningLevel;
            if (level <= .01f) return;
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * (OriginalMath.Binary32(OriginalMath.Binary32(level * 10f) + 8f)))) * .5f) + .5f);
            s.Glow(270f, 560f, 560f, 140f, SourceColor(4417130516449329152UL, 1017370378u,
                OriginalMath.Binary32(OriginalMath.Binary32(level * .35f) * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .4f) + .6f)))), 0f);
            Color side = SourceColor(4441630096532439040UL, 1022739087u, OriginalMath.Binary32(OriginalMath.Binary32(level * .3f) * pulse));
            s.Glow(14f, 420f, 70f, 420f, side, 0f);
            s.Glow(526f, 420f, 70f, 420f, side, 0f);
            float stripeAlpha = OriginalMath.Binary32(level * .55f);
            s.Rect(12f, 532f, 516f, 24f, SourceColor(1028443341UL, 0u, stripeAlpha));
            float[] rows = { 534f, 535.5999755859375f, 537.2000122070312f, 538.7999877929688f,
                540.4000244140625f, 542f, 543.5999755859375f, 545.2000122070312f, 546.7999877929688f,
                548.4000244140625f, 550f, 551.5999755859375f };
            Color stripe = SourceColor(4503599628435849216UL, 1034147594u, stripeAlpha);
            for (int n = 0; n < 30; n++)
            {
                float x = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(clock * 50f) + OriginalMath.Binary32((float)(n * 22))), 528f) + 12f);
                for (int row = 0; row < 12; row++)
                    if (OriginalMath.Binary32(x + OriginalMath.Binary32((float)row)) < 520f) s.Rect(OriginalMath.Binary32(x + OriginalMath.Binary32((float)row)), rows[row], 6f, 1.6f, stripe);
            }
            Color border = SourceColor(4510805389557628928UL, 1036831949u, level);
            s.Rect(12f, 532f, 516f, 2f, border);
            s.Rect(12f, 554f, 516f, 2f, border);
            if (!(level > .25f)) return;
            float alpha = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * .35f) + .65f)) * Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(level + -.25f)) * 3f)));
            Plate(s, 170f, 528f, 200f, 32f, SourceColor(4369572502481089331UL, 1017370378u, OriginalMath.Binary32(alpha * .95f)),
                SourceColor(1042536202UL, 0u, OriginalMath.Binary32(alpha * .95f)), SourceColor(4525216907506221056UL, 1050253722u, alpha));
            s.TextF("敌军逼近防线", 271.1700134277344f, 536.1699829101562f, 1.3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .7f)), 0);
            s.TextF("敌军逼近防线", 270f, 535f, 1.3f, SourceColor(4568451461351997440UL, 1062836634u, alpha), 0);
            Color mark = SourceColor(4561245704525185024UL, 1045220557u, alpha);
            s.Segment(new Vector2(179f, 554f), new Vector2(186f, 538f), 2f, mark);
            s.Segment(new Vector2(186f, 538f), new Vector2(193f, 554f), 2f, mark);
            s.Rect(179f, 553f, 15f, 2f, mark);
            s.Rect(185f, 543f, 2f, 5f, mark);
            s.Segment(new Vector2(347f, 554f), new Vector2(354f, 538f), 2f, mark);
            s.Segment(new Vector2(354f, 538f), new Vector2(361f, 554f), 2f, mark);
            s.Rect(347f, 553f, 15f, 2f, mark);
            s.Rect(353f, 543f, 2f, 5f, mark);
        }
    }
}
