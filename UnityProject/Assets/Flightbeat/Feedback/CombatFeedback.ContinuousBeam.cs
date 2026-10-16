using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        private static float BeamCycle(float value) => Unit(OriginalMath.Binary32(value - OriginalMath.Binary32((float)Math.Floor(value))));
        private static readonly float[] BeamRingPhases = { 0f, .33f, .66f };
        private static readonly float[] BeamSparkPhases = { 0f, 1.3f, 2.6f, 3.9f, 5.2f };

        // f53879 fleet loop: all five source beam appearances. Pose.beam is
        // already a logical target; the surface owns the subsequent layout map.
        private void DrawContinuousBeam(PixelSurface s, Fighter ship, Pose pose)
        {
            Vector2 from = Muzzle(pose, ship.Id, new Vector2(0f, OriginalMath.Binary32(NoseReach(ship.Tier) * -HullSize(ship.Tier))));
            Vector2 to = pose.beam;
            float tier = ship.Tier > 2 ? 1.3f : 1f;
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 71f) + OriginalMath.Binary32((float)ship.Id))) * .15f) + .85f);
            Color lane = palette[ship.Color];
            float modelWidth = BeamWidth;
            float width = OriginalMath.Binary32(tier * modelWidth), alpha = Unit(OriginalMath.Binary32(ship.BeamLife / .07f));
            s.GlowLine(from, to, OriginalMath.Binary32(width * 74f), Alpha(lane, OriginalMath.Binary32(alpha * .25f)));
            s.GlowLine(from, to, OriginalMath.Binary32(width * 40f), Alpha(lane, OriginalMath.Binary32(alpha * .7f)));
            Color core = Mix(lane, Color.white, .3f);
            core.a = OriginalMath.Binary32(core.a * alpha);
            s.Segment(from, to, OriginalMath.Binary32(OriginalMath.Binary32(pulse * 16f) * width), core);
            s.Segment(from, to, OriginalMath.Binary32(OriginalMath.Binary32(pulse * 6f) * width), SourceColor(4574216071967408128UL, 1064011039u, alpha));
            float dx = OriginalMath.Binary32(to.x - from.x), dy = OriginalMath.Binary32(to.y - from.y);
            float length = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy))));
            Vector2 direction = length > .00001f ? new Vector2(OriginalMath.Binary32(dx / length), OriginalMath.Binary32(dy / length)) : Vector2.zero;
            float crossX = OriginalMath.Binary32(width * (OriginalMath.Binary32(direction.y * -13f))), crossY = OriginalMath.Binary32(width * (OriginalMath.Binary32(direction.x * 13f)));
            for (int i = 0; i < 4; i++)
            {
                float t = BeamCycle(OriginalMath.Binary32(OriginalMath.Binary32(time * 3.2f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .25f))), blend = OriginalMath.WasmMax(t, 0f);
                float x = OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * blend)), y = OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * blend));
                s.Segment(new Vector2(OriginalMath.Binary32(x - crossX), OriginalMath.Binary32(y - crossY)), new Vector2(OriginalMath.Binary32(crossX + x), OriginalMath.Binary32(crossY + y)), 2f,
                    SourceColor(4573495496711707034UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .7f) * (OriginalMath.Binary32(1f - t)))));
            }
            float sideAlpha = OriginalMath.Binary32(alpha * .8f);
            Vector2 previous = from;
            for (int i = 1; i <= 8; i++)
            {
                float t = Unit(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .125f)), wave = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.6f) + OriginalMath.Binary32(time * 31f))) * 5f) + 10f);
                Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * t)) + OriginalMath.Binary32(width * (OriginalMath.Binary32(direction.y * wave)))),
                    OriginalMath.Binary32(OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * t)) + OriginalMath.Binary32(width * (OriginalMath.Binary32(wave * -direction.x)))));
                s.Segment(previous, next, 1.4f, Alpha(lane, sideAlpha));
                previous = next;
            }
            previous = from;
            for (int i = 1; i <= 8; i++)
            {
                float t = Unit(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .125f)), wave = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.6f) + OriginalMath.Binary32(time * 31f))) * 5f) + 10f);
                Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * t)) + OriginalMath.Binary32(width * (OriginalMath.Binary32(wave * -direction.y)))),
                    OriginalMath.Binary32(OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * t)) + OriginalMath.Binary32(width * (OriginalMath.Binary32(direction.x * wave)))));
                s.Segment(previous, next, 1.4f, Alpha(lane, sideAlpha));
                previous = next;
            }
            switch (Model)
            {
                case 1:
                    s.Segment(from, to, OriginalMath.Binary32(pulse * 2.2f), new Color(1f, 1f, 1f, alpha));
                    break;
                case 2:
                    foreach (float phase in BeamRingPhases)
                    {
                        float t = BeamCycle(OriginalMath.Binary32(OriginalMath.Binary32(time * 2.4f) + phase)), blend = OriginalMath.WasmMax(t, 0f);
                        s.Ring(new Vector2(OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * blend)), OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * blend))), OriginalMath.Binary32(width * (OriginalMath.Binary32(OriginalMath.Binary32(t * 10f) + 14f))),
                            SourceColor(4546834186573237453UL, 1065353216u, OriginalMath.Binary32(sideAlpha * (OriginalMath.Binary32(1f - t)))), 2f, 18, 0f, 360f);
                    }
                    break;
                case 3:
                    s.GlowLine(from, to, OriginalMath.Binary32(OriginalMath.Binary32(width * 58f) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 50f)) * .1f) + .9f))),
                        SourceColor(4532422664333033472UL, 1034147594u, OriginalMath.Binary32(alpha * .28f)));
                    for (int i = 0; i < 4; i++)
                    {
                        float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 20f) + OriginalMath.Binary32((float)i)));
                        float blend = OriginalMath.WasmMax(BeamCycle(OriginalMath.Binary32(OriginalMath.Binary32(time * 3f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .25f))), 0f);
                        s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(wave * -direction.y) * 10f) + (OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * blend)))),
                            OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(wave * direction.x) * 10f) + (OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * blend)))), 16f, 16f,
                            SourceColor(4554039943403405312UL, 1045220557u, sideAlpha), 0f);
                    }
                    break;
                case 4:
                    float serialValue = OriginalMath.Binary32(time * 50f);
                    int serial = Math.Abs(serialValue) < 2147483648f ? (int)serialValue : int.MinValue;
                    previous = from;
                    for (int i = 1; i <= 10; i++)
                    {
                        float blend = Unit(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 10f));
                        float jitter = i != 10 ? OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(i * 5 + serial) % 7 - 3)) * 3f) : 0f;
                        Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(from.x + OriginalMath.Binary32(dx * blend)) - OriginalMath.Binary32(direction.y * jitter)),
                            OriginalMath.Binary32(OriginalMath.Binary32(from.y + OriginalMath.Binary32(dy * blend)) + OriginalMath.Binary32(jitter * direction.x)));
                        s.Segment(previous, next, 2f, SourceColor(4575657222468743987UL, 1062836634u, alpha));
                        previous = next;
                    }
                    break;
            }
            s.Glow(to.x, to.y, OriginalMath.Binary32(width * 90f), OriginalMath.Binary32(width * 70f), Alpha(lane, OriginalMath.Binary32(alpha * .6f)), 0f);
            s.Glow(to.x, to.y, 30f, 30f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .9f)), 0f);
            for (int i = 0; i < 5; i++)
            {
                float radius = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(time * 60f) + OriginalMath.Binary32((float)(i * 7))), 18f) + 10f);
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + BeamSparkPhases[i]) + OriginalMath.Binary32((float)ship.Id));
                s.Segment(to, new Vector2(OriginalMath.Binary32(to.x + OriginalMath.Binary32(OriginalMath.Cos(angle) * radius)),
                    OriginalMath.Binary32(to.y + OriginalMath.Binary32(radius * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(angle) * .6f) + -.5f))))), 2f,
                    SourceColor(4575657222473777152UL, 1062836634u, sideAlpha));
            }
            s.Glow(from.x, from.y, 54f, 46f, SourceColor(4570613191403162829UL, 1065353216u, sideAlpha), 0f);
        }
    }
}
