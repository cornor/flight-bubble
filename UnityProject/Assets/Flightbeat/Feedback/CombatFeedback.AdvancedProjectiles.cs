using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        private static readonly float[] rocketTrailOffsets = { -.06f, -.12f, -.17999999225139618f, -.24f, -.29999998211860657f };
        private static readonly ulong[] rocketTrailColors = {
            4486305805175860756UL, 4477658893547712020UL, 4469011981919563284UL, 4453159313464602132UL, 4435865494503271956UL
        };
        private static Vector2 ShotDirection(Vector2 delta)
        {
            float length = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(delta.x * delta.x) + OriginalMath.Binary32(delta.y * delta.y))));
            return length > .00001f ? new Vector2(OriginalMath.Binary32(delta.x / length), OriginalMath.Binary32(delta.y / length)) : Vector2.zero;
        }
        // f53879 has different sum orders for the rocket head and its trail.
        private static Vector2 RocketTrail(Vector2 from, Vector2 control, Vector2 to, float t)
        {
            float one = OriginalMath.Binary32(1f - t), twice = OriginalMath.Binary32(t * (OriginalMath.Binary32(one + one))), aa = OriginalMath.Binary32(one * one), bb = OriginalMath.Binary32(t * t);
            return new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(control.x * twice) + OriginalMath.Binary32(aa * from.x)) + OriginalMath.Binary32(bb * to.x)), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(control.y * twice) + OriginalMath.Binary32(aa * from.y)) + OriginalMath.Binary32(bb * to.y)));
        }
        private void DrawRocketFlight(PixelSurface s, Flight f, float t)
        {
            Vector2 control = new Vector2(OriginalMath.Binary32((f.from.x < 270f ? -60f : 60f) + OriginalMath.Binary32((OriginalMath.Binary32(f.from.x + f.to.x)) * .5f)),
                OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(f.from.y + f.to.y)) * .5f) + 80f));
            float square = OriginalMath.Binary32(t * t), one = OriginalMath.Binary32(1f - t), aa = OriginalMath.Binary32(one * one), cc = OriginalMath.Binary32(t * (OriginalMath.Binary32(one + one)));
            Vector2 tip = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(square * f.to.x) + (OriginalMath.Binary32(OriginalMath.Binary32(aa * f.from.x) + OriginalMath.Binary32(cc * control.x)))),
                OriginalMath.Binary32(OriginalMath.Binary32(square * f.to.y) + (OriginalMath.Binary32(OriginalMath.Binary32(aa * f.from.y) + OriginalMath.Binary32(cc * control.y)))));
            Vector2 previous = RocketTrail(f.from, control, f.to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.25f), 0f));
            Vector2 delta = tip - previous, direction = ShotDirection(delta);
            // Source Vector2 equality threshold is f32 0x1.b7cdfcp-34, not zero.
            if (OriginalMath.Binary32(OriginalMath.Binary32(direction.x * direction.x) + OriginalMath.Binary32(direction.y * direction.y)) < 9.999999439624929e-11f)
                direction = ShotDirection(f.to - f.from);
            for (int i = 0; i < 5; i++)
            {
                Vector2 at = RocketTrail(f.from, control, f.to, OriginalMath.WasmMax(OriginalMath.Binary32(t + rocketTrailOffsets[i]), 0f));
                float size = OriginalMath.Binary32((float)(18 + i * 4));
                s.Glow(at.x, at.y, size, size, SourceColor(4555481098199708467UL, rocketTrailColors[i]), 0f);
            }
            s.GlowLine(previous, tip, 30f, SourceColor(4518011146384441344UL, 4546834186539683021UL));
            s.Segment(new Vector2(OriginalMath.Binary32(previous.x + OriginalMath.Binary32(delta.x * .5f)), OriginalMath.Binary32(previous.y + OriginalMath.Binary32(delta.y * .5f))), tip, 4f,
                SourceColor(4561245704525185024UL, 4575657222458677658UL));
            s.SpriteAt(10, tip, 20f, Color.white, OriginalMath.Binary32(OriginalMath.Atan2Single(direction.x, -direction.y) * 57.295780181884766f));
            s.Glow(OriginalMath.Binary32(tip.x - OriginalMath.Binary32(direction.x * 8f)), OriginalMath.Binary32(tip.y - OriginalMath.Binary32(direction.y * 8f)), 26f, 26f,
                SourceColor(4546834186576592896UL, 4554039943379917210UL), 0f);
        }
        private void DrawPierceFlight(PixelSurface s, Flight f, Vector2 tip, float t, float length)
        {
            Vector2 delta = f.to - f.from;
            float trail = Unit(OriginalMath.WasmMax(OriginalMath.Binary32(t + OriginalMath.Binary32(-40f / length)), 0f));
            Vector2 tail = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(trail * delta.x) + f.from.x), OriginalMath.Binary32(OriginalMath.Binary32(trail * delta.y) + f.from.y));
            s.GlowLine(tail, tip, 18f, SourceColor(4572054341902820966UL, 4543231306015703040UL));
            s.Segment(tail, tip, 3f, SourceColor(4575657222471260570UL, 4572054341912887296UL));
            float dx = OriginalMath.Binary32(delta.x / length), dy = OriginalMath.Binary32(delta.y / length);
            for (int i = 0; i < 2; i++)
            {
                float wave = OriginalMath.Cos(OriginalMath.Binary32(OriginalMath.Binary32(time * 60f) + (i == 0 ? 0f : 3.1415927410125732f)));
                float x = OriginalMath.Binary32(tip.x - OriginalMath.Binary32(dx * (i == 0 ? 8f : 17f))), y = OriginalMath.Binary32(tip.y - OriginalMath.Binary32(dy * (i == 0 ? 8f : 17f)));
                float sideX = OriginalMath.Binary32(OriginalMath.Binary32(dy * -5f) * wave), sideY = OriginalMath.Binary32(OriginalMath.Binary32(dx * 5f) * wave);
                s.Segment(new Vector2(OriginalMath.Binary32(x - sideX), OriginalMath.Binary32(y - sideY)), new Vector2(OriginalMath.Binary32(x + sideX), OriginalMath.Binary32(y + sideY)), 2f,
                    SourceColor(4575657222462033101UL, 4568451461351997440UL));
            }
        }
        // f53879: these four folded trig pairs differ in their low bits.
        private static readonly Vector2[] pierceSparkDirections = {
            new Vector2(.7073882222175598f, .7068251967430115f),
            new Vector2(-.7068251967430115f, .7073882818222046f),
            new Vector2(-.707388162612915f, -.706825315952301f),
            new Vector2(.7068250775337219f, -.7073883414268494f)
        };
        private void DrawPierceSparks(PixelSurface s)
        {
            foreach (PierceSpark spark in sparks)
            {
                float t = OriginalMath.Binary32((OriginalMath.Binary32(spark.age - spark.delay)) / .26f);
                if (t < 0f || t > 1f) continue;
                float alpha = OriginalMath.Binary32(1f - t), size = OriginalMath.Binary32(OriginalMath.Binary32(t * 20f) + 26f);
                s.Glow(spark.at.x, spark.at.y, size, size, SourceColor(4575657222463710822UL, 1065353216u, OriginalMath.Binary32(alpha * .55f)), 0f);
                float ten = OriginalMath.Binary32(t * 10f);
                s.Ring(spark.at, OriginalMath.Binary32(ten + 4f), SourceColor(4575657222471260570UL, 1065353216u, alpha), 2f, 12, 0f, 360f);
                float outer = OriginalMath.Binary32(ten + 7f), inner = OriginalMath.Binary32(OriginalMath.Binary32(t * 6f) + 3f);
                foreach (Vector2 v in pierceSparkDirections)
                    s.Segment(new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(inner * v.x) + spark.at.x), OriginalMath.Binary32(OriginalMath.Binary32(inner * v.y) + spark.at.y)),
                        new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(outer * v.x) + spark.at.x), OriginalMath.Binary32(OriginalMath.Binary32(outer * v.y) + spark.at.y)), 1.5f, new Color(1f, 1f, 1f, alpha));
            }
        }
        private void DrawRocketBlasts(PixelSurface s)
        {
            foreach (Blast blast in blasts)
            {
                float t = OriginalMath.Binary32(blast.age / .4f), fade = OriginalMath.Binary32(1f - t), alpha = OriginalMath.Binary32(fade * fade);
                s.Ring(blast.at, OriginalMath.Binary32(OriginalMath.Binary32(Unit(OutCubicStatic(t)) * 61f) + 8f), SourceColor(4554039943403405312UL, 1048576000u, alpha),
                    t < .25f ? 4f : 2f, 20, 0f, 360f);
                s.Glow(blast.at.x, blast.at.y, OriginalMath.Binary32(fade * 150f), OriginalMath.Binary32(fade * 80f), SourceColor(4539628425454813184UL, 1039516303u, OriginalMath.Binary32(alpha * .45f)), 0f);
                if (t < .2f) s.Glow(blast.at.x, blast.at.y, 60f, 60f, SourceColor(4575657222473777152UL, 1062836634u, OriginalMath.Binary32(OriginalMath.Binary32(t * -5f) + 1f)), 0f);
            }
        }
    }
}
