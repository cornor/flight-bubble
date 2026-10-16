using System;
using Flightbeat.Model;
using Flightbeat.Feedback;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54750. Mode-specific kill effects, with the source's 320-record window.
        private void DrawFinaleBlasts(PixelSurface s, int mode)
        {
            Finale shown = finaleShown;
            for (int i = Math.Max(0, finaleBlasts.Count - 320); i < finaleBlasts.Count; i++)
            {
                FinaleBlast blast = finaleBlasts[i];
                float age = OriginalMath.Binary32(clock - blast.born);
                if (age < 0f || age > .5f) continue;
                float t = OriginalMath.Binary32(age + age), ease = OutCubic(t), fade = OriginalMath.Binary32(1f - t);
                if (mode == 1)
                {
                    Vector2 from = new Vector2(OriginalMath.Binary32(blast.at.x + 0f), OriginalMath.Binary32(blast.at.y + OriginalMath.Binary32(ease * -20f)));
                    Vector2 to = new Vector2(OriginalMath.Binary32(blast.at.x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(t * 8f) + OriginalMath.Binary32((float)blast.seed))) * 10f) * ease)), OriginalMath.Binary32(blast.at.y + OriginalMath.Binary32(ease * -70f)));
                    s.Segment(from, to, OriginalMath.Binary32(OriginalMath.Binary32(fade + fade) + .5f), A(Mix(blast.color, Color.white, .6f), fade));
                    s.Rect(OriginalMath.Binary32(to.x + -1.5f), OriginalMath.Binary32(to.y + -1.5f), 3f, 3f, new Color(1f, 1f, 1f, fade));
                    continue;
                }
                if (mode == 2)
                {
                    Vector2 center = shown == null ? new Vector2(270f, 330f) : new Vector2(shown.CX, shown.CY);
                    Vector2 delta = blast.at - center;
                    float progress = OriginalMath.Pow(ease, 1.4f), tailProgress = OriginalMath.WasmMax(OriginalMath.Binary32(progress + -.15f), 0f);
                    Vector2 head = (OriginalMath.Binary32(1f - progress)) * CombatFeedback.Rotate(delta, OriginalMath.Binary32(progress * 300f)) + center;
                    Vector2 tail = (OriginalMath.Binary32(1f - tailProgress)) * CombatFeedback.Rotate(delta, OriginalMath.Binary32(tailProgress * 300f)) + center;
                    s.Segment(tail, head, OriginalMath.Binary32(OriginalMath.Binary32(fade * 2.4f) + .5f), A(Mix(blast.color, new Color(.8f, .6f, 1f, 1f), .4f), fade));
                    s.Rect(OriginalMath.Binary32(head.x + -1.5f), OriginalMath.Binary32(head.y + -1.5f), 3f, 3f, A(blast.color, fade));
                    continue;
                }
                if (mode == 3)
                {
                    float height = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)feedback.Stage) * .15f) + 1f)) * -(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(t * FinalePi)) * 20f) + 10f)));
                    float flicker = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 30f) + OriginalMath.Binary32((float)blast.seed))) * .2f) + .8f);
                    Vector2 tip = new Vector2(OriginalMath.Binary32(blast.at.x + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 12f) + OriginalMath.Binary32((float)blast.seed))) * 3f)), OriginalMath.Binary32(blast.at.y + OriginalMath.Binary32(height * flicker)));
                    s.GlowLine(blast.at, tip, OriginalMath.Binary32(OriginalMath.Binary32(fade * 9f) + 2f), SourceColor(4539628425454813184UL, 1036831949u, OriginalMath.Binary32(fade * .85f)));
                    s.GlowLine(blast.at, new Vector2(OriginalMath.Binary32(blast.at.x + 0f), OriginalMath.Binary32(blast.at.y + OriginalMath.Binary32(OriginalMath.Binary32(height * .55f) * flicker))), OriginalMath.Binary32(OriginalMath.Binary32(fade * 5f) + 1f),
                        SourceColor(4568451461351997440UL, 1056964608u, fade));
                    if (t > .55f) s.Rect(OriginalMath.Binary32(blast.at.x + -2f), OriginalMath.Binary32(OriginalMath.Binary32(blast.at.y + -2f) + OriginalMath.Binary32((OriginalMath.Binary32(t + -.55f)) * -20f)), 3f, 3f,
                        SourceColor(4510805389544207155UL, 1050253722u, OriginalMath.Binary32(fade * 1.6f)));
                    continue;
                }
                if (mode == 4)
                {
                    if (age < .12f)
                    {
                        Color color = SourceColor(4575657222467066266UL, 1061997773u, OriginalMath.Binary32(OriginalMath.Binary32(age / -.12f) + 1f));
                        Vector2 from = new Vector2(OriginalMath.Binary32(blast.at.x + 7f), OriginalMath.Binary32(blast.at.y + 0f));
                        foreach (Vector2 offset in finaleBlastHex)
                        {
                            Vector2 to = blast.at + offset; s.Segment(from, to, 1.5f, color); from = to;
                        }
                    }
                    int seedBase = unchecked(blast.seed * 47);
                    for (int p = 0; p < 4; p++)
                    {
                        float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(seedBase + p * 90) % 360)) * FinaleRadians);
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(blast.at.x + OriginalMath.Binary32(ease * (OriginalMath.Binary32(OriginalMath.Cos(angle) * 18f)))) + -1.5f),
                            OriginalMath.Binary32(OriginalMath.Binary32(blast.at.y + (OriginalMath.Binary32(OriginalMath.Binary32(ease * (OriginalMath.Binary32(OriginalMath.Sin(angle) * 14f))) - OriginalMath.Binary32(ease * 12f)))) + -1.5f), 3f, 3f,
                            (p & 1) == 0 ? SourceColor(4575657222466227405UL, 1061997773u, fade) : A(blast.color, fade));
                    }
                    continue;
                }
                if (age < .09f)
                {
                    float size = OriginalMath.Binary32(OriginalMath.Binary32(age * 90f) + 5f), half = OriginalMath.Binary32(size * .5f);
                    s.Rect(OriginalMath.Binary32(blast.at.x - half), OriginalMath.Binary32(blast.at.y - half), size, size, new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(age / -.09f) + 1f)));
                }
                Color c = blast.color; c.a = OriginalMath.Binary32(c.a * (OriginalMath.Binary32(fade * .6f)));
                s.Glow(blast.at.x, blast.at.y, OriginalMath.Binary32(OriginalMath.Binary32(ease * 46f) + 16f), OriginalMath.Binary32(OriginalMath.Binary32(ease * 46f) + 16f), c, 0f);
                s.Ring(blast.at, OriginalMath.Binary32(OriginalMath.Binary32(ease * 15f) + 3f), A(Mix(Color.white, blast.color, t), fade), 2f, 6,
                    OriginalMath.Binary32((float)(unchecked(blast.seed * 37) % 60)), 360f);
                float radius = OriginalMath.Binary32(OriginalMath.Binary32(ease * 26f) + 5f), particle = OriginalMath.Binary32(OriginalMath.Binary32(fade * 3.2f) + 1f), halfParticle = OriginalMath.Binary32(particle * .5f);
                int seed = unchecked(blast.seed * 53);
                for (int p = 0; p < 4; p++)
                {
                    float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(seed + p * 97) % 360)) * .01745329238474369f);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(blast.at.x + OriginalMath.Binary32(radius * OriginalMath.Cos(angle))) - halfParticle),
                        OriginalMath.Binary32(OriginalMath.Binary32(blast.at.y + OriginalMath.Binary32(radius * OriginalMath.Sin(angle))) - halfParticle), particle, particle,
                        A((p & 1) == 0 ? Color.white : blast.color, fade));
                }
            }
        }
        // f54750 contains these already-folded f32 coordinates, not runtime trig.
        private static readonly Vector2[] finaleBlastHex = {
            new Vector2(3.499999761581421f, 6.062178134918213f), new Vector2(-3.500000476837158f, 6.062177658081055f),
            new Vector2(-7f, -6.119594218034763e-07f), new Vector2(-3.4999992847442627f, -6.062178134918213f),
            new Vector2(3.500002384185791f, -6.062176704406738f), new Vector2(7f, 1.2239188436069526e-06f)
        };
    }
}
