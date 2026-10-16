using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private const float FinalePi = 3.1415927410125732f, FinaleRadians = .01745329238474369f;
        // Shared inline source fade in f54744..f54748.
        private static float FinaleAttackFade(float age, float after) =>
            OriginalMath.Binary32((age < 3.15f ? 1f : Clamp01(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) / -.7f) + 1f))) * (OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(after + after)))));
        private static int FinaleInt(float value) => Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;
        private static readonly float[] galeRingPhases = { 0f, .2f, .4f, .6f, .8f };

        // f54746. Rising streaks, sweep front, chevrons and five travelling rings.
        private void DrawGale(PixelSurface s, Finale finale, float age, float after)
        {
            float alpha = FinaleAttackFade(age, after); if (alpha <= 0f) return;
            float y = age < 3.15f ? finale.WipeY : 40f, strength = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)feedback.Stage) * .07f) + .75f);
            Color accent = FleetCatalog.FighterAccent[1];
            int count = FinaleInt(OriginalMath.Binary32(strength * 34f));
            for (int i = 0; i < count; i++)
            {
                float top = OriginalMath.Binary32(FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(700f - OriginalMath.Binary32(clock * OriginalMath.Binary32((float)(i % 5 * 260 + 900)))) - OriginalMath.Binary32((float)unchecked(i * 53))), 760f) + -60f);
                if (top < OriginalMath.Binary32(y + -20f)) continue;
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(i * 97) % 100)) / 100f) * 512f) + 14f);
                Color color = i % 3 == 0 ? accent : i % 3 == 1 ? Color.white : new Color(.7f, .95f, 1f, 1f);
                s.Segment(new Vector2(x, top), new Vector2(x, OriginalMath.Binary32(top + OriginalMath.Binary32((float)((i & 3) * 40 + 50)))), (i & 3) == 0 ? 2.5f : 1.5f,
                    A(color, OriginalMath.Binary32(alpha * .55f)));
            }
            s.Glow(270f, OriginalMath.Binary32(y + 60f), 640f, 200f, SourceColor(4568451461345286554UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .22f) * strength)), 0f);
            s.Glow(270f, y, 640f, OriginalMath.Binary32(strength * 70f), A(Mix(accent, Color.white, .5f), OriginalMath.Binary32(alpha * .55f)), 0f);
            s.Rect(12f, OriginalMath.Binary32(y + -2f), 516f, 4f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .95f)));
            for (int i = 0; i < 12; i++)
            {
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 20f) + OriginalMath.Binary32((float)i))) * 6f) + OriginalMath.Binary32((float)(i * 44 + 24)));
                float top = OriginalMath.Binary32(OriginalMath.Binary32(y + -22f) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 25f) + OriginalMath.Binary32((float)(i << 1)))) * -8f));
                s.Segment(new Vector2(x, OriginalMath.Binary32(y + -4f)), new Vector2(OriginalMath.Binary32(x + 18f), top), 2f,
                    SourceColor(4573495496711707034UL, 1065353216u, OriginalMath.Binary32(alpha * .8f)));
            }
            Vector2 apex = new Vector2(270f, OriginalMath.Binary32(y + -24f));
            foreach (float x in new[] { -30f, 570f })
            {
                Vector2 tail = new Vector2(x, OriginalMath.Binary32(y + 260f));
                s.GlowLine(apex, tail, OriginalMath.Binary32(strength * 40f), A(accent, OriginalMath.Binary32(alpha * .25f)));
                s.Segment(apex, tail, 3f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .7f)));
            }
            // f54746 WAT 914..969: (y - 24) + (t * 120 + 20), alpha * ((1 - t) * .6).
            foreach (float phase in galeRingPhases)
            {
                float t = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(clock * 3f) + phase), 1f);
                s.Ring(new Vector2(270f, OriginalMath.Binary32(OriginalMath.Binary32(y + -24f) + OriginalMath.Binary32(OriginalMath.Binary32(t * 120f) + 20f))), OriginalMath.Binary32(OriginalMath.Binary32(t * 90f) + 14f),
                    SourceColor(4572054341909531853UL, 1065353216u, OriginalMath.Binary32(alpha * OriginalMath.Binary32(OriginalMath.Binary32(1f - t) * .6f))), 2f, 5, 200f, 140f);
            }
        }
        private static readonly float[] novaTrailAlpha = { .38f, .26f, .14000001549720764f };
        // f54748. Three trailing half-rings, the leading wave and outward streaks.
        private void DrawPulseNova(PixelSurface s, Finale finale, float age, float after)
        {
            float alpha = FinaleAttackFade(age, after); if (alpha <= 0f) return;
            int stage = feedback.Stage;
            float x = finale.Sweeping ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 70f)) * 1.6f) + 270f) : 270f;
            // f54748 WAT 128..135: 562 - (lift - 10).
            float y = OriginalMath.Binary32(562f - OriginalMath.Binary32(FinaleLift(age, after) + -10f));
            float radius = age < 3.15f ? finale.NovaRadius : OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) * 400f) + finale.NovaReach);
            Vector2 at = new Vector2(x, y);
            s.Glow(x, y, OriginalMath.Binary32(radius * 2.1f), OriginalMath.Binary32(radius * 2.1f), SourceColor(4546834186561493402UL, 1065353216u, OriginalMath.Binary32(alpha * .16f)), 0f);
            for (int i = 0; i < 3; i++)
            {
                float trail = OriginalMath.WasmMax(OriginalMath.Binary32(radius + OriginalMath.Binary32((float)(-34 * (i + 1)))), 0f);
                if (trail <= 4f) continue;
                Color color = Mix(new Color(.5f, .92f, 1f, 1f), RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(clock * .3f) + OriginalMath.Binary32(OriginalMath.Binary32((float)(i + 1)) * .25f))), .35f);
                s.Ring(at, trail, A(color, OriginalMath.Binary32(alpha * novaTrailAlpha[i])), 3f, 48, 180f, 180f);
            }
            if (radius > 4f)
            {
                float strength = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)stage) * .07f) + .75f);
                s.Ring(at, radius, SourceColor(4569892616144945152UL, 1065353216u, OriginalMath.Binary32(alpha * .55f)), OriginalMath.Binary32(strength * 14f), 64, 180f, 180f);
                s.Ring(at, radius, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .95f)), 4f, 64, 180f, 180f);
                for (int i = 0; i < 24; i++)
                {
                    float pulse = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 30f) + OriginalMath.Binary32((float)i)));
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 7.5f) + 180f) + (OriginalMath.Binary32(pulse + pulse)))) * FinaleRadians);
                    float r = OriginalMath.Binary32(radius + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 40f) + OriginalMath.Binary32((float)(i * 3)))) * 6f));
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + OriginalMath.Binary32(r * OriginalMath.Cos(angle))) + -2f), OriginalMath.Binary32(OriginalMath.Binary32(y + OriginalMath.Binary32(r * OriginalMath.Sin(angle))) + -2f), 4f, 4f,
                        new Color(1f, 1f, 1f, alpha));
                }
            }
            for (int i = 0; i < 16; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)(i * 10 + 190)) * FinaleRadians), cos = OriginalMath.Cos(angle), sin = OriginalMath.Sin(angle);
                float t = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .37f) + OriginalMath.Binary32(clock * 2.4f)), 1f), r = OriginalMath.Binary32(radius + OriginalMath.Binary32(t * 60f));
                Vector2 from = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(cos * r)), OriginalMath.Binary32(y + OriginalMath.Binary32(sin * r)));
                s.Segment(from, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(cos * 10f) + from.x), OriginalMath.Binary32(OriginalMath.Binary32(sin * 10f) + from.y)), 2f,
                    SourceColor(4569892616144945152UL, 1065353216u, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t)))));
            }
            float bloom = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 45f)) * 10f) + 40f);
            s.Glow(x, y, OriginalMath.Binary32(bloom * 4f), OriginalMath.Binary32(bloom * 3f), SourceColor(4569892616144945152UL, 1065353216u, OriginalMath.Binary32(alpha * .6f)), 0f);
            s.Glow(x, y, OriginalMath.Binary32(bloom * 1.6f), OriginalMath.Binary32(bloom * 1.6f), new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .9f)), 0f);
        }
    }
}
