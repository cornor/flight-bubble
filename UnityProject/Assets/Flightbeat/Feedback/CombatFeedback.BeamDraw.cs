using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53913. Fusion beam (distinct from a fighter's sustained weapon beam).
        private void DrawBeams(PixelSurface s)
        {
            foreach (Beam beam in beams)
            {
                float front = FrontScreenY;
                Color color = beam.tier > 2 ? new Color(1f, .85f, .4f, 1f) : Mix(palette[beam.color], Color.white, .35f);
                float fade = beam.age < .12f ? 1f : Unit(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(beam.age + -.12f)) / -.55f) + 1f));
                float progress = Unit(OriginalMath.Binary32(beam.age / .12f)), remaining = progress < 0f ? 1f : OriginalMath.Binary32(1f - progress);
                float tipY = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(front + -600f)) * Unit(OriginalMath.Binary32(1f - OriginalMath.Binary32(remaining * (OriginalMath.Binary32(remaining * remaining)))))) + 600f);
                float width = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(fade * .4f) + .6f)) * (beam.tier > 2 ? 46f : 30f));
                Vector2 from = new Vector2(beam.x, 604f), to = new Vector2(beam.x, tipY);
                s.GlowLine(from, to, OriginalMath.Binary32(width * 2.4f), Alpha(color, OriginalMath.Binary32(fade * .35f)));
                s.Segment(from, to, OriginalMath.Binary32(width * .5f), Alpha(color, OriginalMath.Binary32(fade * .9f)));
                s.Segment(from, to, OriginalMath.Binary32(width * .18f), new Color(1f, 1f, 1f, fade));
                if (!(progress >= 1f)) continue;
                float t = OriginalMath.Binary32((OriginalMath.Binary32(beam.age + -.12f)) / .35f);
                remaining = t < 0f ? 1f : OriginalMath.Binary32(1f - OriginalMath.WasmMin(t, 1f));
                float radius = OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Binary32(remaining * (OriginalMath.Binary32(remaining * remaining))))) * 300f);
                s.Glow(beam.x, front, OriginalMath.Binary32(OriginalMath.Binary32(radius + radius) + 60f), 46f, Alpha(color, OriginalMath.Binary32(fade * .55f)), 0f);
                s.Segment(new Vector2(OriginalMath.Binary32(beam.x - radius), OriginalMath.Binary32(front + 4f)), new Vector2(OriginalMath.Binary32(radius + beam.x), OriginalMath.Binary32(front + 4f)),
                    3f, new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .9f)));
                for (int n = -6; n < 7; n++)
                {
                    float sine = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 50f) + OriginalMath.Binary32((float)n))), x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Binary32((float)n)) / 6f) + beam.x);
                    s.Segment(new Vector2(x, OriginalMath.Binary32(front + 2f)),
                        new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(sine * 6f)), OriginalMath.Binary32(OriginalMath.Binary32(front + -14f) - OriginalMath.Binary32((float)(Math.Abs(n) % 3 * 6)))),
                        2f, Alpha(color, OriginalMath.Binary32(fade * .8f)));
                }
                s.Glow(beam.x, front, 70f, 70f, new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .8f)), 0f);
            }
        }
    }
}
