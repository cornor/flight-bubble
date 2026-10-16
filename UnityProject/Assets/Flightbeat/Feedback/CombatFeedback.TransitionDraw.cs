using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53923. Trail and ship coordinates have different WAT addition orders.
        public void DrawTransitions(PixelSurface s)
        {
            foreach (Transition move in transitions)
            {
                float t = Unit(OriginalMath.Binary32(move.age / move.duration));
                t = OriginalMath.Binary32(OriginalMath.Binary32(t * t) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(t + t)))));
                Vector2 from = move.from, to = move.to;
                Vector2 control = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.x + to.x)) * .5f) + 0f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.y + to.y)) * .5f) + -60f));
                Color lane = palette[move.color];
                DrawTransitionTrail(s, from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.07f), 0f), 23f, Alpha(lane, .37f));
                DrawTransitionTrail(s, from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.14f), 0f), 20f, Alpha(lane, .29f));
                DrawTransitionTrail(s, from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.21f), 0f), 17f, Alpha(lane, .21f));
                DrawTransitionTrail(s, from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.28f), 0f), 14f, Alpha(lane, .13f));
                float tt = OriginalMath.Binary32(t * t), u = OriginalMath.Binary32(1f - t), uu = OriginalMath.Binary32(u * u), cross = OriginalMath.Binary32(t * (OriginalMath.Binary32(u + u)));
                Vector2 at = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(tt * to.x) + (OriginalMath.Binary32(OriginalMath.Binary32(uu * from.x) + OriginalMath.Binary32(control.x * cross)))),
                    OriginalMath.Binary32(OriginalMath.Binary32(tt * to.y) + (OriginalMath.Binary32(OriginalMath.Binary32(uu * from.y) + OriginalMath.Binary32(control.y * cross)))));
                float size = move.tier > 0 ? 46f : 40f;
                Ship(s, move.color, move.tier, at, OriginalMath.Binary32(size + OriginalMath.Binary32(Unit(t) * (OriginalMath.Binary32(26f - size)))), OriginalMath.Binary32(OriginalMath.Binary32(t * move.spin) * 360f),
                    Color.white, 0f, false);
                s.Glow(at.x, at.y, 40f, 40f, new Color(1f, 1f, 1f, OriginalMath.Binary32(t * .4f)), 0f);
            }
            foreach (Fusion fusion in fusions)
            {
                Vector2 at = fusion.at;
                if (fusion.age < .2f)
                {
                    float grow = OriginalMath.Binary32(fusion.age / .2f);
                    s.Glow(at.x, at.y, OriginalMath.Binary32(grow * 70f), OriginalMath.Binary32(grow * 70f), new Color(1f, 1f, 1f, OriginalMath.Binary32(grow * .5f)), 0f);
                    continue;
                }
                float k = OriginalMath.Binary32((OriginalMath.Binary32(fusion.age + -.22f)) / .8f);
                Color color = fusion.tier == 3 ? new Color(1f, .82f, .35f, 1f) : palette[fusion.color];
                if (k < .25f) s.Glow(at.x, at.y, 120f, 120f, new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(k * -4f) + 1f)), 0f);
                float raw = OriginalMath.Binary32(k * 1.6f), ring = Unit(raw), radius = fusion.tier == 3 ? 90f : 60f;
                if (ring > 0f && ring < 1f) s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(ring * radius) + 10f), Alpha(color, OriginalMath.Binary32(1f - ring)), 3f, 28, 0f, 360f);
                ring = Unit(OriginalMath.Binary32(raw + -.2f));
                if (ring > 0f && ring < 1f) s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(ring * radius) + 10f), Alpha(color, OriginalMath.Binary32(1f - ring)), 2f, 28, 0f, 360f);
                string rank = fusion.tier > 2 ? "MAX" : fusion.tier == 2 ? "III" : "II";
                float fade = Unit(OriginalMath.Binary32((OriginalMath.Binary32(1f - k)) * 2.2f));
                float scale = OriginalMath.Binary32((k < .18f ? OriginalMath.Binary32(1.6f - OriginalMath.Binary32(k * 2.2f)) : 1f) * (fusion.tier > 2 ? 4f : 3f));
                float x = OriginalMath.Binary32(at.x + OriginalMath.Binary32(OriginalMath.Binary32(scale * OriginalMath.Binary32((float)(rank.Length << 3))) * -.5f));
                float y = OriginalMath.Binary32(OriginalMath.Binary32(at.y + -58f) + OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(k + -.4f), 0f) * -50f));
                Color shadow = new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .85f));
                DrawBigText(s, rank, OriginalMath.Binary32(x + -2f), OriginalMath.Binary32(y + 0f), scale, shadow);
                DrawBigText(s, rank, OriginalMath.Binary32(x + 2f), OriginalMath.Binary32(y + 0f), scale, shadow);
                DrawBigText(s, rank, OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y + -2f), scale, shadow);
                DrawBigText(s, rank, OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y + 2f), scale, shadow);
                DrawBigText(s, rank, x, y, scale, Alpha(color, fade));
                if (!(k < .8f) || fusion.tier <= 2) continue;
                float pulse = OriginalMath.Binary32(OriginalMath.Binary32(k / -.8f) + 1f);
                s.GlowLine(at, new Vector2(at.x, 120f), 90f, SourceColor(4561245704525185024UL, 1051931443u, OriginalMath.Binary32(pulse * .35f)));
                s.Segment(at, new Vector2(at.x, 120f), OriginalMath.Binary32(pulse * 10f), SourceColor(4572054341912887296UL, 1060320051u, OriginalMath.Binary32(pulse * .8f)));
                float rotation = OriginalMath.Binary32(k * 200f), outer = OriginalMath.Binary32(OriginalMath.Binary32(k * 110f) + 40f), inner = OriginalMath.Binary32(OriginalMath.Binary32(k * 50f) + 20f);
                for (int ray = 0; ray < 8; ray++)
                {
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(rotation + OriginalMath.Binary32((float)(ray * 45)))) * .01745329238474369f);
                    float cosine = OriginalMath.Cos(angle), sine = OriginalMath.Sin(angle);
                    s.Segment(new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(inner * cosine) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(inner * sine) + at.y)),
                        new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(outer * cosine) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(outer * sine) + at.y)), 3f,
                        SourceColor(4568451461351997440UL, 1056964608u, pulse));
                }
            }
        }

        private static void DrawTransitionTrail(PixelSurface s, Vector2 from, Vector2 control, Vector2 to,
            float t, float size, Color color)
        {
            float u = OriginalMath.Binary32(1f - t), cross = OriginalMath.Binary32(t * (OriginalMath.Binary32(u + u))), uu = OriginalMath.Binary32(u * u), tt = OriginalMath.Binary32(t * t);
            s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(control.x * cross) + OriginalMath.Binary32(uu * from.x)) + OriginalMath.Binary32(tt * to.x)),
                OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(control.y * cross) + OriginalMath.Binary32(uu * from.y)) + OriginalMath.Binary32(tt * to.y)), size, size, color, 0f);
        }
    }
}
