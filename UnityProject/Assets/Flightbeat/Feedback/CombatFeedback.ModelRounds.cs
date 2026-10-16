using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53880: original inlined filled-disc scanlines, with 2.3px row height.
        private static void RoundDisc(PixelSurface s, Vector2 at, float radius, Color color)
        {
            float square = OriginalMath.Binary32(radius * radius);
            for (float y = -radius; y < radius; y  = OriginalMath.Binary32(y+(2f)))
            {
                float middle = OriginalMath.Binary32(y + 1f);
                float half = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.WasmMax(OriginalMath.Binary32(square - OriginalMath.Binary32(middle * middle)), 0f)));
                if (half > 0f) s.Rect(OriginalMath.Binary32(at.x - half), OriginalMath.Binary32(y + at.y), OriginalMath.Binary32(half + half), 2.3f, color);
            }
        }
        // f53880 WAT 964/970 stores 0x1.cccccep-1 (0x3f666667), not rounded decimal .9f.
        private static readonly float[] roundTrailPhases = { .3f, .6f, BitConverter.Int32BitsToSingle(0x3f666667) };
        private static readonly float[] roundTrailSizes = { 11f, 8f, 5f };
        private static readonly float[] roundOrbitPhases = { 0f, 2.1f, 4.2f };

        // f53880 models 1..4, preserving primitive order and the WAT sum trees.
        private void DrawModelRound(PixelSurface s, Flight f, Vector2 tip, Vector2 tail, Vector2 wake,
            Color lane, int form, float strength)
        {
            switch (Model)
            {
                case 1:
                    s.GlowLine(wake, tip, OriginalMath.Binary32(strength * 12f), Alpha(lane, OriginalMath.Binary32(lane.a * .45f)));
                    s.Segment(tail, tip, OriginalMath.Binary32(strength * 2.2f), Mix(lane, Color.white, .75f));
                    s.Segment(wake, tail, 1.2f, Alpha(lane, OriginalMath.Binary32(lane.a * .8f)));
                    if (form >= 2)
                    {
                        Vector2 delta = tip - tail, dir = ShotDirection(delta);
                        float blend = OriginalMath.WasmMax(Fraction(OriginalMath.Binary32(OriginalMath.Binary32(time * 14f) + OriginalMath.Binary32((float)f.target))), 0f);
                        Vector2 center = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(delta.x * blend) + tail.x), OriginalMath.Binary32(OriginalMath.Binary32(delta.y * blend) + tail.y));
                        float x = OriginalMath.Binary32(dir.y * -5f), y = OriginalMath.Binary32(dir.x * 5f);
                        s.Segment(new Vector2(OriginalMath.Binary32(center.x - x), OriginalMath.Binary32(center.y - y)), new Vector2(OriginalMath.Binary32(x + center.x), OriginalMath.Binary32(y + center.y)),
                            1.5f, SourceColor(4575657222468743987UL, 4568451461351997440UL));
                    }
                    s.Glow(tip.x, tip.y, OriginalMath.Binary32(strength * 14f), OriginalMath.Binary32(strength * 14f), Color.white, 0f);
                    break;
                case 2:
                    s.Glow(tip.x, tip.y, OriginalMath.Binary32(strength * 34f), OriginalMath.Binary32(strength * 34f), Alpha(lane, OriginalMath.Binary32(lane.a * .7f)), 0f);
                    RoundDisc(s, tip, OriginalMath.Binary32(strength * 5.5f), Mix(lane, Color.white, .35f));
                    RoundDisc(s, tip, OriginalMath.Binary32(strength * 2.8f), Color.white);
                    float pulse = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 30f) + OriginalMath.Binary32((float)f.target)));
                    s.Ring(tip, OriginalMath.Binary32(OriginalMath.Binary32(strength * 8f) + (OriginalMath.Binary32(pulse + pulse))), SourceColor(4550437062839998874UL, 4561245704525185024UL),
                        1.5f, 14, 0f, 360f);
                    for (int i = 0; i < 3; i++)
                    {
                        float phase = roundTrailPhases[i], size = OriginalMath.Binary32(strength * roundTrailSizes[i]);
                        s.Glow(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(tail.x - tip.x)) * phase) + tip.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(tail.y - tip.y)) * phase) + tip.y), size, size,
                            Alpha(lane, OriginalMath.Binary32(lane.a * .35f)), 0f);
                    }
                    break;
                case 3:
                    s.GlowLine(wake, tip, OriginalMath.Binary32(strength * 20f), SourceColor(4532422664333033472UL, 4532422664301827850UL));
                    s.GlowLine(tail, tip, OriginalMath.Binary32(strength * 12f), Alpha(lane, OriginalMath.Binary32(lane.a * .8f)));
                    float size26 = OriginalMath.Binary32(strength * 26f);
                    s.Glow(tip.x, tip.y, OriginalMath.Binary32(size26 * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 40f) + OriginalMath.Binary32((float)f.target))) * .15f) + .85f))),
                        size26, SourceColor(4554039943403405312UL, 4561245704505052365UL), 0f);
                    RoundDisc(s, tip, OriginalMath.Binary32(strength * 3.5f), Mix(lane, new Color(1f, .95f, .7f, 1f), .5f));
                    for (int i = 1; i <= 4; i++)
                    {
                        float cycle = Fraction(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 6f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .25f)) + OriginalMath.Binary32(OriginalMath.Binary32((float)f.target) * .1f))), fade = OriginalMath.Binary32(1f - cycle);
                        float blend = OriginalMath.WasmMax(cycle, 0f);
                        float x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(wake.x - tip.x)) * blend) + tip.x), y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(wake.y - tip.y)) * blend) + tip.y);
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 20f) + OriginalMath.Binary32((float)i))) * 3f) + x) + -1f), OriginalMath.Binary32(y + -1f), 2f, 2f,
                            new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32(fade * .3f) + .6f), BitConverter.Int32BitsToSingle(1041865114), fade));
                    }
                    break;
                case 4:
                    s.Glow(tip.x, tip.y, OriginalMath.Binary32(strength * 24f), OriginalMath.Binary32(strength * 24f), Alpha(lane, OriginalMath.Binary32(lane.a * .75f)), 0f);
                    RoundDisc(s, tip, OriginalMath.Binary32(strength * 4f), Mix(lane, Color.white, .55f));
                    foreach (float phase in roundOrbitPhases)
                    {
                        float theta = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 25f) + phase) + OriginalMath.Binary32((float)f.target));
                        float sin = OriginalMath.Sin(theta), cos = OriginalMath.Cos(theta);
                        Vector2 at = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(strength * (OriginalMath.Binary32(cos * 6f))) + tip.x), OriginalMath.Binary32(OriginalMath.Binary32(strength * (OriginalMath.Binary32(sin * 6f))) + tip.y));
                        s.Segment(tip, at, 1.5f, SourceColor(4575657222468743987UL, 4568451461349480858UL));
                        s.Segment(at, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(strength * (OriginalMath.Binary32(sin * 4f))) + at.x), OriginalMath.Binary32(at.y - OriginalMath.Binary32(strength * (OriginalMath.Binary32(cos * 4f))))),
                            1.2f, SourceColor(4575657222473777152UL, 4561245704525185024UL));
                    }
                    s.GlowLine(tail, tip, OriginalMath.Binary32(strength * 8f), Alpha(lane, OriginalMath.Binary32(lane.a * .5f)));
                    break;
            }
        }
    }
}
