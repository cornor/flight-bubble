using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54749. Source fan sweep: filled rays, internal streaks, travelling
        // arcs, two edge beams, central core, muzzle bloom and rotating crosses.
        // Preserve the WAT alpha * (factor * wave/fade) stack grouping before tint alpha.
        private void DrawFanLaser(PixelSurface s, Finale finale, float age, float after)
        {
            float alpha, spread;
            if (!(age < 3.15f))
            {
                alpha = OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) / -.7f) + 1f)) * (OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(after + after)))));
                if (alpha <= 0f) return;
                spread = 90f;
            }
            else
            {
                alpha = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -1.65f)) / .08f));
                if (alpha <= 0f) return;
                spread = finale.Spread;
            }
            float pulse = OriginalMath.Sin(OriginalMath.Binary32(clock * 57f));
            float x = finale.Sweeping ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 70f)) * 1.6f) + 270f) : 270f;
            // f54749 WAT 144..153: subtract the whole (lift - 10) expression.
            float y = OriginalMath.Binary32(562f - OriginalMath.Binary32(FinaleLift(age, after) + -10f));
            Vector2 nose = new Vector2(x, y);
            if (spread > .5f)
            {
                float end = OriginalMath.Binary32(spread + .01f), denominator = OriginalMath.WasmMax(spread, 1f);
                for (float angle = -spread; angle <= end; angle  = OriginalMath.Binary32(angle+(2.5f)))
                {
                    float wave = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(angle * .37f) + OriginalMath.Binary32(clock * 23f))) * .25f) + .75f);
                    float edge = OriginalMath.Binary32(Math.Abs(angle) / denominator);
                    Color tint = Mix(new Color(.75f, .97f, 1f, 1f), RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(angle / 180f) + .5f) + OriginalMath.Binary32(clock * .15f))), OriginalMath.Binary32(OriginalMath.Binary32(edge * .55f) + .45f));
                    float radians = OriginalMath.Binary32(angle * .01745329238474369f);
                    Vector2 to = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Sin(radians) * 1100f)), OriginalMath.Binary32(y - OriginalMath.Binary32(OriginalMath.Cos(radians) * 1100f)));
                    s.GlowLine(nose, to, 52f, A(tint, OriginalMath.Binary32(alpha * OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(edge * .16f) + .24f)) * wave))));
                    s.Segment(nose, to, 7f, A(tint, OriginalMath.Binary32(alpha * OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(edge * .08f) + .1f)) * wave))));
                }
            }
            if (spread > 3f)
            {
                float denominator = OriginalMath.WasmMax(spread, 1f);
                for (float angle = OriginalMath.Binary32(3f - spread); angle < OriginalMath.Binary32(spread + -2f); angle  = OriginalMath.Binary32(angle+(6f)))
                {
                    float wave = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(angle * .9f) + OriginalMath.Binary32(clock * 31f))) * .4f) + .6f);
                    Color tint = Mix(Color.white, RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(angle / 180f) + .5f) + OriginalMath.Binary32(clock * .15f))), .6f);
                    float radians = OriginalMath.Binary32(angle * .01745329238474369f);
                    s.Segment(nose, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Sin(radians) * 1100f)), OriginalMath.Binary32(y - OriginalMath.Binary32(OriginalMath.Cos(radians) * 1100f))), 2.5f,
                        A(tint, OriginalMath.Binary32(alpha * OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(Math.Abs(angle) / denominator) * .25f) + .35f)) * wave))));
                }
            }
            if (spread > 2f)
            {
                float countValue = OriginalMath.Binary32(spread / 5f);
                int segments = Math.Max(6, Math.Abs(countValue) < 2147483648f ? (int)countValue : int.MinValue);
                for (int i = 0; i < 5; i++)
                {
                    float radius = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(clock * 820f) + OriginalMath.Binary32((float)(i * 210))), 1050f), fade = OriginalMath.Binary32(OriginalMath.Binary32(radius / -1050f) + 1f);
                    Color tint = Mix(Color.white, RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 5f) + OriginalMath.Binary32(clock * .3f))), .4f);
                    s.Ring(nose, radius, A(tint, OriginalMath.Binary32(alpha * OriginalMath.Binary32(fade * .55f))), 3f, segments, OriginalMath.Binary32(-90f - spread), OriginalMath.Binary32(spread + spread));
                    s.Ring(nose, OriginalMath.Binary32(radius + 5f), new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * OriginalMath.Binary32(fade * .25f))), 1f, segments, OriginalMath.Binary32(-90f - spread), OriginalMath.Binary32(spread + spread));
                }
            }
            float corePulse = OriginalMath.Binary32(OriginalMath.Binary32(pulse * .15f) + .85f);
            if (spread > .5f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float radians = OriginalMath.Binary32(OriginalMath.Binary32(spread * OriginalMath.Binary32((float)side)) * .01745329238474369f);
                    float cos = OriginalMath.Cos(radians), sin = OriginalMath.Sin(radians);
                    Vector2 to = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(sin * 1100f)), OriginalMath.Binary32(y - OriginalMath.Binary32(cos * 1100f)));
                    Color tint = RainbowAt(OriginalMath.Binary32((side < 0 ? 0f : .5f) + OriginalMath.Binary32(clock * .6f)));
                    s.GlowLine(nose, to, 96f, A(tint, OriginalMath.Binary32(alpha * .3f)));
                    s.GlowLine(nose, to, 44f, A(tint, OriginalMath.Binary32(alpha * .55f)));
                    s.Segment(nose, to, OriginalMath.Binary32(corePulse * 15f), A(Mix(tint, Color.white, .35f), OriginalMath.Binary32(alpha * .95f)));
                    s.Segment(nose, to, 5f, new Color(1f, 1f, 1f, alpha));
                    for (int i = 0; i < 9; i++)
                    {
                        float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 3.1f) + OriginalMath.Binary32(clock * 9f)));
                        float cycle = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 9f) + OriginalMath.Binary32(clock * 1.9f)), 1f), distance = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cycle * 1100f) * .8f) + 60f);
                        float px = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cos * wave) * 9f) + (OriginalMath.Binary32(x + OriginalMath.Binary32(sin * distance))));
                        float py = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sin * wave) * 9f) + (OriginalMath.Binary32(y - OriginalMath.Binary32(cos * distance))));
                        s.Rect(OriginalMath.Binary32(px + -2f), OriginalMath.Binary32(py + -2f), 4f, 4f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - cycle)))));
                    }
                }
            }
            Vector2 up = new Vector2(OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y + -1100f));
            s.GlowLine(nose, up, 150f, SourceColor(4572054341907854131UL, 1065353216u, OriginalMath.Binary32(alpha * .3f)));
            s.GlowLine(nose, up, 64f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .45f)));
            s.Segment(nose, up, OriginalMath.Binary32(corePulse * 24f), SourceColor(4574216071964052685UL, 1065353216u, OriginalMath.Binary32(alpha * .95f)));
            s.Segment(nose, up, 9f, new Color(1f, 1f, 1f, alpha));
            float bloom = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 40f)) * 8f) + 38f);
            s.Glow(x, y, OriginalMath.Binary32(bloom * 5f), OriginalMath.Binary32(bloom * 4f), SourceColor(4572054341912887296UL, 1061997773u, OriginalMath.Binary32(alpha * .5f)), 0f);
            s.Glow(x, y, OriginalMath.Binary32(bloom + bloom), OriginalMath.Binary32(bloom + bloom), new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .9f)), 0f);
            for (int i = 0; i < 4; i++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(clock * 140f) + OriginalMath.Binary32((float)(i * 45)))) * .01745329238474369f);
                float dx = OriginalMath.Binary32(OriginalMath.Binary32(bloom * OriginalMath.Sin(angle)) * 1.5f), dy = OriginalMath.Binary32(OriginalMath.Binary32(bloom * -OriginalMath.Cos(angle)) * 1.5f);
                s.Segment(new Vector2(OriginalMath.Binary32(x - dx), OriginalMath.Binary32(y - dy)), new Vector2(OriginalMath.Binary32(x + dx), OriginalMath.Binary32(y + dy)), 2f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .7f)));
            }
            s.Ring(nose, OriginalMath.Binary32(bloom * .8f), A(RainbowAt(clock), OriginalMath.Binary32(alpha * .8f)), 3f, 24, 0f, 360f);
        }
    }
}
