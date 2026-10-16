using System;
using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public static partial class FleetOpening
    {
        private static Color SourceColor(ulong rg, uint b, float alpha) => new Color(
            BitConverter.Int32BitsToSingle(unchecked((int)rg)),
            BitConverter.Int32BitsToSingle(unchecked((int)(rg >> 32))),
            BitConverter.Int32BitsToSingle(unchecked((int)b)), alpha);

        // f54043. The source game argument is unused. Negative stripe travel uses
        // the original fmodf, and the unrolled stripe rows preserve their draw order.
        public static void Draw(PixelSurface s, Battle battle)
        {
            float time = battle.IntroTime;
            float duration = battle.Level.introDuration > 0f ? battle.Level.introDuration : 4.6f;
            float warningAt = OriginalMath.Binary32(duration * .4f), readyAt = OriginalMath.Binary32(duration * .66f);
            if (time < OriginalMath.Binary32(warningAt + .3f))
            {
                float alpha = OriginalMath.Binary32(Clamp(OriginalMath.Binary32(time * 4f)) * Clamp(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(warningAt + .3f) - time)) * 4f)));
                s.Glow(270f, 118f, 560f, 50f, SourceColor(4489188110478868480UL, 1065353216u, OriginalMath.Binary32(alpha * .5f)), 0f);
                s.Rect(12f, 116f, 516f, 2f, SourceColor(4554039943401727590UL, 1065353216u, alpha));
                for (int n = 0; n != 16; n++)
                    s.Rect(OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(time * 260f) + OriginalMath.Binary32((float)(n * 37))), 516f) + 12f), 114f, 18f, 5f,
                        SourceColor(4568451461351997440UL, 1065353216u, OriginalMath.Binary32(alpha * .6f)));
            }
            if (warningAt <= time && time < readyAt)
            {
                float t = OriginalMath.Binary32((OriginalMath.Binary32(time - warningAt)) / (OriginalMath.Binary32(readyAt - warningAt)));
                float alpha = OriginalMath.Binary32(Clamp(OriginalMath.Binary32(t * 8f)) * Clamp(OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) * 8f))), stripeAlpha = OriginalMath.Binary32(alpha * .8f);
                s.Rect(0f, 242f, 540f, 116f, SourceColor(1028443341UL, 0u, OriginalMath.Binary32(alpha * .82f)));
                s.Glow(270f, 300f, 560f, 170f, SourceColor(4453159313468293120UL, 1017370378u, OriginalMath.Binary32(alpha * .45f)), 0f);
                float positive = OriginalMath.Remainder(OriginalMath.Binary32(time * 120f), 22f), negative = OriginalMath.Remainder(OriginalMath.Binary32(time * -120f), 22f);
                Color stripe = SourceColor(4510805389557628928UL, 1028443341u, stripeAlpha);
                for (int side = 0; side != 2; side++)
                {
                    float y = side != 0 ? 346f : 242f, travel = side != 0 ? negative : positive;
                    for (int n = -1; n != 28; n++)
                    {
                        float start = OriginalMath.Binary32(travel + OriginalMath.Binary32((float)(n * 22)));
                        for (int row = 0; row != 12; row++)
                        {
                            float x = OriginalMath.Binary32(start + OriginalMath.Binary32((float)row));
                            if (x < 0f || x > 534f) continue;
                            s.Rect(x, OriginalMath.Binary32(y + OriginalMath.Binary32((float)row)), 7f, 1f, stripe);
                        }
                    }
                }
                float slide = OriginalMath.Binary32((OriginalMath.Binary32(1f - Clamp(OriginalMath.Binary32(t * 4f)))) * 600f);
                float shake = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 60f)) * (t > .25f ? 1.5f : 0f));
                const string warning = "WARNING";
                float left = OriginalMath.Binary32(slide + (OriginalMath.Binary32(270f - OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(warning.Length << 3)) * 5f) * .5f))));
                CombatFeedback.DrawBigText(s, warning, OriginalMath.Binary32(shake + (OriginalMath.Binary32(left + 3f))), 263f, 5f, new Color(0f, 0f, 0f, stripeAlpha));
                float pulse = Clamp(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 20f)) * .5f) + .5f));
                CombatFeedback.DrawBigText(s, warning, OriginalMath.Binary32(shake + left), 260f, 5f,
                    new Color(OriginalMath.Binary32(OriginalMath.Binary32(pulse * 0f) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(pulse * .65f) + .25f), OriginalMath.Binary32(OriginalMath.Binary32(pulse * .5f) + .1f), OriginalMath.Binary32(alpha + OriginalMath.Binary32(pulse * (OriginalMath.Binary32(alpha - alpha))))));
                string text = battle.Level.difficulty == 2 ? "极难战区" : battle.Level.difficulty == 1 ? "困难战区" : "敌袭警报";
                s.TextF(text, 272f, 308f, 1.8f, new Color(0f, 0f, 0f, stripeAlpha), 0);
                s.TextF(text, 270f, 306f, 1.8f, SourceColor(4564848585086074880UL, 1060320051u, alpha), 0);
            }
            if (time >= readyAt)
            {
                float elapsed = OriginalMath.Binary32(time - readyAt), alpha = Clamp(OriginalMath.Binary32(elapsed * 5f));
                s.Glow(270f, 516f, 420f, 120f, SourceColor(4554039943374884045UL, 1065353216u, OriginalMath.Binary32(alpha * .35f)), 0f);
                float size = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - Clamp(OriginalMath.Binary32(elapsed * 4f)))) * 1.4f) + 2.6f);
                s.TextF("准备出击", 273f, 492f, size, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .8f)), 0);
                s.TextF("准备出击", 270f, 489f, size, SourceColor(4573495496709190451UL, 1065353216u, alpha), 0);
            }
        }
    }
}
