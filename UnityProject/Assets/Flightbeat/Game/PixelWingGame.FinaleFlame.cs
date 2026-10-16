using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private static float FinaleFract(float value) => OriginalMath.Binary32(value - OriginalMath.Binary32((float)Math.Floor(value)));
        // f54747. Original jet geometry, seeded flame sprites, core licks and embers.
        private void DrawFlamethrower(PixelSurface s, Finale finale, float age, float after)
        {
            float alpha = FinaleAttackFade(age, after); if (alpha <= 0f) return;
            float strength = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)feedback.Stage) * .07f) + .75f);
            float x = finale.Sweeping ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 70f)) * 1.6f) + 270f) : 270f;
            // This source branch subtracts (lift - 10), unlike the fan/nova branch.
            float y = OriginalMath.Binary32(562f - (OriginalMath.Binary32(FinaleLift(age, after) + -10f))), angle = age < 3.15f ? finale.JetAngle : 90f;
            float radians = OriginalMath.Binary32(angle * FinaleRadians), cos = OriginalMath.Cos(radians), sin = OriginalMath.Sin(radians);
            Vector2 nose = new Vector2(x, y);
            for (float a = -90f; a < angle; a  = OriginalMath.Binary32(a+(15f)))
            {
                float rad = OriginalMath.Binary32(a * FinaleRadians);
                s.GlowLine(nose, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Sin(rad) * 760f)), OriginalMath.Binary32(y - OriginalMath.Binary32(OriginalMath.Cos(rad) * 760f))), 46f,
                    SourceColor(4507923084249923584UL, 1028443341u, OriginalMath.Binary32(alpha * .05f)));
            }
            Vector2 start = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(sin * 20f)), OriginalMath.Binary32(y - OriginalMath.Binary32(cos * 20f)));
            float lower = OriginalMath.Binary32((OriginalMath.Binary32(angle + -6f)) * FinaleRadians), upper = OriginalMath.Binary32((OriginalMath.Binary32(angle + 6f)) * FinaleRadians);
            Color red = SourceColor(4510805389557628928UL, 1028443341u, OriginalMath.Binary32(alpha * .24f));
            s.GlowLine(start, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(lower) * 760f) * .85f)), OriginalMath.Binary32(y - OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(lower) * 760f) * .85f))), OriginalMath.Binary32(strength * 90f), red);
            s.GlowLine(start, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(upper) * 760f) * .85f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(upper) * -760f) * .85f))), OriginalMath.Binary32(strength * 90f), red);
            float dx = OriginalMath.Binary32(sin * 760f), dy = OriginalMath.Binary32(cos * -760f);
            s.GlowLine(nose, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(dx * .7f)), OriginalMath.Binary32(y + OriginalMath.Binary32(dy * .7f))), OriginalMath.Binary32(strength * 58f),
                SourceColor(4539628425454813184UL, 1036831949u, OriginalMath.Binary32(alpha * .5f)));
            s.GlowLine(nose, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(dx * .38f)), OriginalMath.Binary32(y + OriginalMath.Binary32(dy * .38f))), OriginalMath.Binary32(strength * 30f),
                SourceColor(4567010310845628416UL, 1055286886u, OriginalMath.Binary32(alpha * .75f)));
            int count = FinaleInt(OriginalMath.Binary32(strength * 64f));
            for (int i = 0; i < count; i++)
            {
                float index = OriginalMath.Binary32((float)i), t = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * .6180340051651001f) + OriginalMath.Binary32(clock * 1.7f)), 1f);
                float distance = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * 760f) * .92f) + 24f);
                float random = FinaleFract(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(index * 78.23300170898438f)) * 43758.55078125f));
                float sideways = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(random + random)) + -1f)) * distance) * .2f)
                    + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(t * 16f) + 3f)) * OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(index * 1.7f) + OriginalMath.Binary32(clock * 11f)))));
                float px = OriginalMath.Binary32(OriginalMath.Binary32(x + OriginalMath.Binary32(sin * distance)) + OriginalMath.Binary32(cos * sideways));
                float py = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(y - OriginalMath.Binary32(cos * distance))) + OriginalMath.Binary32(sin * sideways)) + OriginalMath.Binary32((OriginalMath.Binary32(t * t)) * -40f));
                float size = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(FinaleFract(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(index * 12.98900032043457f)) * 24634.630859375f)) * .5f) + .75f))
                    * (OriginalMath.Binary32(strength * (OriginalMath.Binary32(OriginalMath.Binary32(t * 84f) + 16f)))));
                Color color;
                if (t < .16f)
                {
                    float blend = Clamp01(OriginalMath.Binary32(t / .16f)), one = OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f);
                    color = new Color(one, OriginalMath.Binary32(OriginalMath.Binary32(blend * -.13f) + .98f), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.5f) + .85f), one);
                }
                else if (t < .45f)
                {
                    float blend = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(t + -.16f)) / .29f)), one = OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f);
                    color = new Color(one, OriginalMath.Binary32(OriginalMath.Binary32(blend * -.40000003576278687f) + .85f), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.26999998092651367f) + .35f), one);
                }
                else if (t < .75f)
                {
                    float blend = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(t + -.45f)) / .3f));
                    color = new Color(OriginalMath.Binary32(1f - OriginalMath.Binary32(blend * .3f)), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.32999998331069946f) + .45f), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.04f) + .08f), OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f));
                }
                else
                {
                    float blend = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(t + -.75f)) * 4f));
                    color = new Color(OriginalMath.Binary32(OriginalMath.Binary32(blend * -.5399999618530273f) + .7f), OriginalMath.Binary32(OriginalMath.Binary32(blend * .009999997913837433f) + .12f),
                        OriginalMath.Binary32(OriginalMath.Binary32(blend * .08999999612569809f) + .04f), OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f));
                }
                float fade = OriginalMath.Binary32(alpha * (t < .75f ? .85f : OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(t + -.75f)) * -4f) + 1f)) * .85f) + .1f)));
                s.Glow(px, py, size, OriginalMath.Binary32(size * .82f), A(color, fade), 0f);
                if (t < .4f)
                {
                    float remaining = OriginalMath.Binary32(OriginalMath.Binary32(t / -.4f) + 1f), dot = OriginalMath.Binary32(OriginalMath.Binary32(remaining * 4f) + 1f), half = OriginalMath.Binary32(dot * .5f);
                    s.Rect(OriginalMath.Binary32(px - half), OriginalMath.Binary32(py - half), dot, dot, SourceColor(4573495496714223616UL, 1061158912u, OriginalMath.Binary32(alpha * remaining)));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float index = OriginalMath.Binary32((float)i), length = OriginalMath.Binary32(strength * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(index * 2.1f) + OriginalMath.Binary32(clock * 23f))) * 70f) + 110f)));
                float random = FinaleFract(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(index * 3.7f)) * 9173.099609375f));
                float rad = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(angle + OriginalMath.Binary32((OriginalMath.Binary32(random + -.5f)) * 16f)) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 17f) + index)) * 3f))) * FinaleRadians);
                float lx = OriginalMath.Binary32(length * OriginalMath.Sin(rad)), ly = OriginalMath.Binary32(length * -OriginalMath.Cos(rad));
                s.GlowLine(nose, new Vector2(OriginalMath.Binary32(x + lx), OriginalMath.Binary32(y + ly)), 20f, SourceColor(4554039943403405312UL, 1045220557u, OriginalMath.Binary32(alpha * .45f)));
                s.Segment(nose, new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(lx * .5f)), OriginalMath.Binary32(y + OriginalMath.Binary32(ly * .5f))), 3f, SourceColor(4572054341912887296UL, 1060320051u, OriginalMath.Binary32(alpha * .6f)));
            }
            for (int i = 0; i < 14; i++)
            {
                float index = OriginalMath.Binary32((float)i), t = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * .37f) + OriginalMath.Binary32(clock * .9f)), 1f);
                float random = FinaleFract(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(index * 5.31f)) * 1731.699951171875f)), side = OriginalMath.Binary32(random + -.5f);
                float distance = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(random * 760f) * .7f) + 80f);
                float px = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cos * side) * 80f) + (OriginalMath.Binary32(x + OriginalMath.Binary32(sin * distance)))) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 3f) + index)) * 10f)) + -1.5f);
                float py = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sin * side) * 80f) + (OriginalMath.Binary32(y - OriginalMath.Binary32(cos * distance)))) + OriginalMath.Binary32(t * -90f)) + -1.5f);
                s.Rect(px, py, 3f, 3f, new Color(1f, t < .5f ? .85f : .4f, t < .5f ? .4f : .1f, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t)))));
            }
            s.Glow(x, y, OriginalMath.Binary32(strength * 84f), OriginalMath.Binary32(strength * 84f), SourceColor(4561245704525185024UL, 1053609165u, OriginalMath.Binary32(alpha * .75f)), 0f);
            s.Glow(x, y, 28f, 28f, SourceColor(4561245704517635277UL, 1065353216u, OriginalMath.Binary32(alpha * .9f)), 0f);
        }
    }
}
