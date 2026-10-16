using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54753 / f54754: exact data-segment tables at 3648000..3648120.
        private static readonly Color[] finaleImpactColors = { new Color(.7f, .95f, 1f, 1f), new Color(.8f, .55f, 1f, 1f),
            new Color(1f, .55f, .15f, 1f), new Color(.55f, 1f, .78f, 1f), new Color(.5f, .9f, 1f, 1f) };
        private static readonly Vector3[] finaleCountDeltas = {
            new Vector3(-.3f, -.020000040531158447f, .1499999761581421f),
            new Vector3(-.19999998807907104f, -.42000001668930054f, .1499999761581421f),
            new Vector3(0f, -.42000001668930054f, -.7000000476837158f),
            new Vector3(-.44999998807907104f, .029999971389770508f, -.0700000524520874f),
            new Vector3(-.5f, -.0700000524520874f, .1499999761581421f)
        };
        private void DrawFinaleImpact(PixelSurface s, Finale finale, float age, float after)
        {
            Color color = unchecked((uint)(finale.Mode - 1)) <= 4u ? finaleImpactColors[finale.Mode - 1] : new Color(1f, .85f, .4f, 1f);
            float lift = FinaleLift(age, after), left = ScreenLayout.Left, height = ScreenLayout.Height, width = ScreenLayout.Width;
            float burst = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -1.65f)) / .45f));
            if (age >= 1.65f && burst < 1f)
            {
                Vector2 nose = FinaleNose(lift);
                float alpha = OriginalMath.Binary32((OriginalMath.Binary32(1f - burst)) * .8f), radius = OriginalMath.Binary32(OriginalMath.Binary32(burst * 420f) + 60f);
                Color ray = Mix(Color.white, color, .3f); ray.a = alpha;
                for (int i = 0; i < 28; i++)
                {
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 12.86f) + OriginalMath.Binary32((float)(i * 37 % 7)))) * .01745329238474369f);
                    float sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle), outer = OriginalMath.Binary32(OriginalMath.Binary32(radius + 120f) + OriginalMath.Binary32((float)(i % 3 * 60)));
                    s.Segment(new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(radius * cos) + 270f), OriginalMath.Binary32(nose.y + OriginalMath.Binary32(radius * sin))),
                        new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(outer * cos) + 270f), OriginalMath.Binary32(nose.y + OriginalMath.Binary32(outer * sin))), (i & 3) != 0 ? 2f : 4f, ray);
                }
                s.Ring(nose, OriginalMath.Binary32(OriginalMath.Binary32(burst * 700f) + 40f), new Color(1f, 1f, 1f, alpha), 6f, 48, 0f, 360f);
            }
            if (finale.Sweeping)
            {
                float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 14f)) * .5f) + .5f);
                s.Glow(OriginalMath.Binary32(left + OriginalMath.Binary32(width * .5f)), -10f, OriginalMath.Binary32(width * 1.2f), 120f, A(color, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .18f)), 0f);
                color.a = OriginalMath.Binary32(OriginalMath.Binary32(pulse * .08f) + .14f);
                s.Glow(OriginalMath.Binary32(left + OriginalMath.Binary32(width * .5f)), OriginalMath.Binary32(height + 10f), OriginalMath.Binary32(width * 1.2f), 120f, color, 0f);
                s.Glow(OriginalMath.Binary32(left + -10f), OriginalMath.Binary32(height * .5f), 120f, OriginalMath.Binary32(height * 1.1f), color, 0f);
                s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(left + width) + 10f), OriginalMath.Binary32(height * .5f), 120f, OriginalMath.Binary32(height * 1.1f), color, 0f);
                color.a = 1f;
            }
            float endAge = OriginalMath.Binary32(OriginalMath.Binary32(age + -3.15f) + .05f);
            if (endAge < 0f) return;
            float fade = OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(after / .6f)));
            if (fade <= 0f) return;
            float scale;
            if (endAge < .18f)
            {
                float t = OutCubic(OriginalMath.Binary32(endAge / .18f));
                scale = t < 0f ? 2.6f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(t, 1f) * -1.5999999046325684f) + 2.6f);
            }
            else scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.WasmMin(OriginalMath.Binary32((OriginalMath.Binary32(endAge + -.18f)) / .3f), 1f) * 3.1415927410125732f)) * .06f) + 1f);
            for (int i = 0; i < 12; i++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(clock * 40f) + OriginalMath.Binary32((float)(i * 30)))) * .01745329238474369f);
                Color ray = (i & 1) != 0 ? Color.white : color; ray.a = OriginalMath.Binary32(fade * .16f);
                s.GlowLine(new Vector2(270f, 330f), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * 320f) + 270f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(angle) * 320f) + 330f)), 46f, ray);
            }
            s.Glow(270f, 330f, OriginalMath.Binary32(scale * 420f), OriginalMath.Binary32(scale * 220f), A(color, OriginalMath.Binary32(fade * .35f)), 0f);
            Title(s, "全 歼", 270f, OriginalMath.Binary32(OriginalMath.Binary32(scale * -40f) + 330f), OriginalMath.Binary32(scale * 4.4f),
                SourceColor(4574216071967408128UL, 1063675494u, fade), SourceColor(4405601299496697856UL, 1034147594u, fade));
            if (endAge < .12f) s.Glow(270f, 330f, 700f, 700f, new Color(1f, 1f, 1f, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(endAge / -.12f) + 1f)) * .7f)), 0f);
        }

        private void DrawFinaleCount(PixelSurface s, Finale finale, float age)
        {
            if (age < 1.65f || age >= 3.71f) return;
            float alpha = age < 3.15f ? 1f : Clamp01(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) / -.7f) + 1f));
            float appear = OutBack(Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -1.65f)) / .3f)));
            float elapsed = OriginalMath.Binary32(ui - finaleMilestoneAt), pop = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(elapsed * -4f) + 1f), 0f) * .35f) + 1f);
            Vector3 delta = unchecked((uint)(finale.Mode - 1)) <= 4u ? finaleCountDeltas[finale.Mode - 1]
                : new Vector3(0f, -.12000000476837158f, -.45000001788139343f);
            float t = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(elapsed / -.4f) + 1f), 0f);
            t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            Color tint = new Color(OriginalMath.Binary32(OriginalMath.Binary32(t * delta.x) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(t * delta.y) + .97f), OriginalMath.Binary32(OriginalMath.Binary32(t * delta.z) + .85f), OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(t * 0f) + 1f))));
            Title(s, "歼灭 ×" + finale.Kills.ToString(), 270f, 146f, OriginalMath.Binary32(OriginalMath.Binary32(appear * 2.4f) * pop), tint,
                SourceColor(4369572502474378445UL, 1036831949u, alpha));
        }
    }
}
