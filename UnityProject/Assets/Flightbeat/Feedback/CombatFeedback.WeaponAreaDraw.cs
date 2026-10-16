using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53883 kind 2, form >=3. Original smoothstep, orbit and inward/outward phases.
        private void DrawCollapseWeapon(PixelSurface s, WeaponFx fx, float t, float fade, Color lane, Color bright)
        {
            Vector2 at = fx.at;
            float alpha = OriginalMath.WasmMin(OriginalMath.Binary32(fade * 3f), 1f), phase = Unit(OriginalMath.Binary32(t / .7f));
            float smooth = OriginalMath.Binary32(OriginalMath.Binary32(phase * phase) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(phase + phase)))));
            float radius = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(smooth * -.55f) + 1f)) * 40f);
            s.Glow(at.x, at.y, OriginalMath.Binary32(radius * 2.6f), OriginalMath.Binary32(radius * 2.2f), SourceColor(1017370378UL, 1031127695u, OriginalMath.Binary32(alpha * .45f)), 0f);
            for (int arm = 0; arm < 3; arm++)
                for (int i = 0; i < 6; i++)
                {
                    float p = OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 6f), size = OriginalMath.Binary32(OriginalMath.Binary32(p * -1.4f) + 2.6f), half = OriginalMath.Binary32(size * .5f);
                    float r = OriginalMath.Binary32(radius * (OriginalMath.Binary32(OriginalMath.Binary32(p * -.82f) + 1f))), theta = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(p * 230f) + OriginalMath.Binary32((float)(arm * 120))) - OriginalMath.Binary32(t * 520f))) * WeaponRadians);
                    float sin = OriginalMath.Sin(theta), cos = OriginalMath.Cos(theta), blend = Unit(p), b = OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f);
                    // .28 below is the original folded (1 - .72), whose f32 is one ULP lower than literal .28.
                    Color color = new Color(OriginalMath.Binary32(OriginalMath.Binary32(blend * .2799999713897705f) + .72f), OriginalMath.Binary32(OriginalMath.Binary32(blend * .55f) + .45f), b,
                        OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(p * .5f) + .5f))) * b));
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + (OriginalMath.Binary32(OriginalMath.Binary32(r * cos) - OriginalMath.Binary32(sin * 0f)))) - half), OriginalMath.Binary32(OriginalMath.Binary32(at.y + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(r * sin) + OriginalMath.Binary32(cos * 0f))) * .72f)) - half), size, size, color);
                }
            if (fx.extras.Length > 0)
            {
                float inner = OriginalMath.Binary32(1f - smooth), tailScale = OriginalMath.Binary32(1f - OriginalMath.WasmMax(OriginalMath.Binary32(smooth + -.12f), 0f));
                float angle = OriginalMath.Binary32(smooth * 280f), a = OriginalMath.Binary32(angle * WeaponRadians), b = OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(angle + -40f), 0f) * WeaponRadians);
                float sin = OriginalMath.Sin(a), cos = OriginalMath.Cos(a), tailSin = OriginalMath.Sin(b), tailCos = OriginalMath.Cos(b);
                foreach (Vector2 extra in fx.extras)
                {
                    if (!(t < .72f)) continue;
                    Vector2 delta = extra - at;
                    Vector2 trail = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(tailScale * (OriginalMath.Binary32(OriginalMath.Binary32(tailCos * delta.x) - OriginalMath.Binary32(tailSin * delta.y))))),
                        OriginalMath.Binary32(at.y + OriginalMath.Binary32(tailScale * (OriginalMath.Binary32(OriginalMath.Binary32(tailSin * delta.x) + OriginalMath.Binary32(tailCos * delta.y))))));
                    Vector2 head = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(inner * (OriginalMath.Binary32(OriginalMath.Binary32(cos * delta.x) - OriginalMath.Binary32(sin * delta.y))))),
                        OriginalMath.Binary32(at.y + OriginalMath.Binary32(inner * (OriginalMath.Binary32(OriginalMath.Binary32(sin * delta.x) + OriginalMath.Binary32(cos * delta.y))))));
                    s.Segment(trail, head, OriginalMath.Binary32(OriginalMath.Binary32(inner * 2.2f) + .8f), Alpha(bright, OriginalMath.Binary32(bright.a * .9f)));
                    s.Rect(OriginalMath.Binary32(head.x + -2f), OriginalMath.Binary32(head.y + -2f), 4f, 4f, lane);
                }
            }
            if (t < .72f)
            {
                float size = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(smooth * 4f) + 2f)) * 6f);
                s.Glow(at.x, at.y, size, size, SourceColor(4546834186574076314UL, 4554039943403405312UL), 0f);
                s.Rect(OriginalMath.Binary32(at.x + -1f), OriginalMath.Binary32(at.y + -1f), 2f, 2f, Color.white);
                return;
            }
            float outward = Unit(OriginalMath.Binary32((OriginalMath.Binary32(t + -.72f)) / .28f)), remaining = OriginalMath.Binary32(1f - outward);
            float span = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(outward * -.4f) + 1f)) * 70f);
            s.Segment(new Vector2(OriginalMath.Binary32(at.x - span), at.y), new Vector2(OriginalMath.Binary32(span + at.x), OriginalMath.Binary32(at.y + 0f)), OriginalMath.Binary32(OriginalMath.Binary32(remaining * 3f) + .5f),
                new Color(1f, 1f, 1f, remaining));
            float vertical = OriginalMath.Binary32(remaining * 22f);
            s.Segment(new Vector2(at.x, OriginalMath.Binary32(at.y - vertical)), new Vector2(OriginalMath.Binary32(at.x + 0f), OriginalMath.Binary32(vertical + at.y)), OriginalMath.Binary32((OriginalMath.Binary32(remaining + remaining)) + .5f),
                SourceColor(4554039943400888730UL, 1065353216u, remaining));
            float glow = OriginalMath.Binary32(OriginalMath.Binary32(remaining * 80f) + 20f);
            s.Glow(at.x, at.y, glow, glow, SourceColor(4543231306012347597UL, 1065353216u, OriginalMath.Binary32(remaining * .8f)), 0f);
            for (int i = 0; i < 3; i++)
                s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(outward * 62f) + 14f), SourceColor(4546834186573237453UL, 1065353216u, OriginalMath.Binary32(remaining * .9f)),
                    OriginalMath.Binary32((OriginalMath.Binary32(remaining + remaining)) + .6f), 3, OriginalMath.Binary32(OriginalMath.Binary32(outward * 60f) + OriginalMath.Binary32((float)(i * 120))), 46f);
        }
        private void DrawQuakeWeapon(PixelSurface s, WeaponFx fx, float t, float fade, Color lane, Color bright)
        {
            Vector2 at = fx.at;
            s.Glow(at.x, at.y, OriginalMath.Binary32(fade * 46f), OriginalMath.Binary32(fade * 38f), SourceColor(4554039943400888730UL, 1065353216u, OriginalMath.Binary32(fade * .6f)), 0f);
            float xspan = OriginalMath.Binary32(fade * 10f), yspan = OriginalMath.Binary32(fade * 7f);
            Color white = new Color(1f, 1f, 1f, fade);
            s.Segment(new Vector2(OriginalMath.Binary32(at.x - xspan), at.y), new Vector2(OriginalMath.Binary32(xspan + at.x), OriginalMath.Binary32(at.y + 0f)), 2f, white);
            s.Segment(new Vector2(at.x, OriginalMath.Binary32(at.y - yspan)), new Vector2(OriginalMath.Binary32(at.x + 0f), OriginalMath.Binary32(yspan + at.y)), 2f, white);
            float after = OriginalMath.Binary32(t + -.28f), impact = OriginalMath.Binary32(1f - Unit(OriginalMath.Binary32(after * 2.2f))), spread = OriginalMath.Binary32(2f - impact);
            float travel = Unit(OriginalMath.Binary32(t * 3.5f)), oneThird = OriginalMath.WasmMin(travel, .3333333432674408f),
                twoThirds = OriginalMath.WasmMin(travel, .6666666865348816f);
            float gravity = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(after * after) * 160f) * .2f);
            for (int i = 0; i < fx.extras.Length; i++)
            {
                Vector2 extra = fx.extras[i], delta = extra - at, dir = WeaponDirection(delta, Vector2.right);
                Vector2 previous = at;
                for (int j = 1; j <= 3; j++)
                {
                    float part = j == 1 ? oneThird : j == 2 ? twoThirds : travel;
                    float offset = OriginalMath.Binary32((OriginalMath.Binary32(WeaponHash(unchecked(i * 31 + fx.color + j)) + -.5f)) * 7f);
                    if (j == 3) offset  = OriginalMath.Binary32(offset*(0f));
                    Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(at.x + OriginalMath.Binary32(delta.x * part)) - OriginalMath.Binary32(offset * dir.y)), OriginalMath.Binary32(OriginalMath.Binary32(at.y + OriginalMath.Binary32(delta.y * part)) + OriginalMath.Binary32(offset * dir.x)));
                    s.Segment(previous, next, OriginalMath.Binary32(OriginalMath.Binary32(fade * 2.2f) + .4f), SourceColor(4558363399215466414UL, 1065353216u, fade));
                    previous = next;
                    if (travel <= part) break;
                }
                if (travel >= 1f)
                {
                    s.Rect(OriginalMath.Binary32(extra.x - OriginalMath.Binary32(spread * 6f)), OriginalMath.Binary32(extra.y - (OriginalMath.Binary32(impact + impact))), OriginalMath.Binary32(spread * 12f), OriginalMath.Binary32(OriginalMath.Binary32(impact * 4f) + .5f),
                        Alpha(bright, OriginalMath.Binary32(impact * bright.a)));
                    for (int j = 0; j < 4; j++)
                    {
                        float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(i * 23 + 45 + j * 90)) * WeaponRadians);
                        Color color = (j & 1) == 0 ? Alpha(lane, OriginalMath.Binary32(fade * lane.a)) : Alpha(bright, OriginalMath.Binary32(fade * bright.a));
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(extra.x + OriginalMath.Binary32(OriginalMath.Binary32(after * OriginalMath.Cos(angle)) * 70f)) + -1.5f),
                            OriginalMath.Binary32(OriginalMath.Binary32(extra.y + (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(after * OriginalMath.Sin(angle)) * 50f) + gravity))) + -1.5f), 3f, 3f, color);
                    }
                }
            }
            for (int i = 0; i < 2; i++)
                s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(t * (fx.form == 2 ? 46f : 34f)) + 12f), SourceColor(4546834186573237453UL, 1065353216u, OriginalMath.Binary32(fade * .8f)),
                    OriginalMath.Binary32(OriginalMath.Binary32(fade * 2.4f) + .4f), 3, i == 0 ? 30f : 210f, 70f);
        }
        // f53883 kind 4. Each complete link becomes the start of the following link.
        private void DrawArcWeapon(PixelSurface s, WeaponFx fx, float t, float fade, Color lane, int tick)
        {
            float alpha = t < .15f ? 1f : fade;
            Vector2 from = fx.at;
            int clockHash = unchecked(tick / 2 * 7);
            for (int i = 0; i < fx.extras.Length; i++)
            {
                float travel = Unit(OriginalMath.Binary32(OriginalMath.Binary32(t * 5f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.5f)));
                if (travel <= 0f) return;
                Vector2 at = fx.extras[i];
                float amount = OriginalMath.WasmMax(travel, 0f);
                Vector2 end = new Vector2(OriginalMath.Binary32(from.x + OriginalMath.Binary32(amount * (OriginalMath.Binary32(at.x - from.x)))), OriginalMath.Binary32(from.y + OriginalMath.Binary32(amount * (OriginalMath.Binary32(at.y - from.y)))));
                Vector2 delta = end - from, dir = WeaponDirection(delta, Vector2.right);
                s.GlowLine(from, end, 14f, Alpha(lane, OriginalMath.Binary32((OriginalMath.Binary32(alpha * .45f)) * lane.a)));
                Vector2 rawDirection = ShotDirection(delta), previous = from;
                float theta = (unchecked(i + tick) & 1) != 0 ? -.6981316804885864f : .6981316804885864f;
                float sin = OriginalMath.Sin(theta), cos = OriginalMath.Cos(theta);
                for (int j = 1; j <= 4; j++)
                {
                    float part = Unit(OriginalMath.Binary32(OriginalMath.Binary32((float)j) * .25f)), offset = j == 4 ? 0f :
                        OriginalMath.Binary32((OriginalMath.Binary32(WeaponHash(unchecked(i * 29 + clockHash + j * 13 + fx.color)) + -.5f)) * 10f);
                    Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(from.x + OriginalMath.Binary32(delta.x * part)) - OriginalMath.Binary32(offset * dir.y)),
                        OriginalMath.Binary32(OriginalMath.Binary32(from.y + OriginalMath.Binary32(delta.y * part)) + OriginalMath.Binary32(offset * dir.x)));
                    s.Segment(previous, next, OriginalMath.Binary32(OriginalMath.Binary32(alpha * 2.4f) + .6f), SourceColor(4573855784337460429UL, 1065353216u, alpha));
                    if (j == 2)
                    {
                        float x = OriginalMath.Binary32(rawDirection.x * 8f), y = OriginalMath.Binary32(rawDirection.y * 8f);
                        s.Segment(next, new Vector2(OriginalMath.Binary32(next.x + (OriginalMath.Binary32(OriginalMath.Binary32(x * cos) - OriginalMath.Binary32(y * sin)))), OriginalMath.Binary32(next.y + (OriginalMath.Binary32(OriginalMath.Binary32(x * sin) + OriginalMath.Binary32(y * cos))))), 1.2f,
                            SourceColor(4572054341906176410UL, 1065353216u, OriginalMath.Binary32(alpha * .8f)));
                    }
                    previous = next;
                }
                if (travel >= 1f)
                {
                    s.Glow(at.x, at.y, 26f, 26f, SourceColor(4572054341907015270UL, 1065353216u, OriginalMath.Binary32(alpha * .85f)), 0f);
                    s.Segment(new Vector2(OriginalMath.Binary32(at.x + -6f), at.y), new Vector2(OriginalMath.Binary32(at.x + 6f), OriginalMath.Binary32(at.y + 0f)), 1.6f, new Color(1f, 1f, 1f, alpha));
                    s.Segment(new Vector2(at.x, OriginalMath.Binary32(at.y + -6f)), new Vector2(OriginalMath.Binary32(at.x + 0f), OriginalMath.Binary32(at.y + 6f)), 1.6f, new Color(1f, 1f, 1f, alpha));
                }
                if (travel < 1f) return;
                from = at;
            }
        }
    }
}
