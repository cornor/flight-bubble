using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        private const float WeaponRadians = .01745329238474369f;
        private static readonly float[] weaponThirds = { 0f, .33f, .66f };
        private static Vector2 WeaponDirection(Vector2 delta, Vector2 fallback) =>
            OriginalMath.Binary32(OriginalMath.Binary32(delta.x * delta.x) + OriginalMath.Binary32(delta.y * delta.y)) < 1f ? fallback : ShotDirection(delta);
        private static float WeaponHash(int value)
        {
            value = unchecked(((value ^ (value >> 16)) ^ 61) * 9);
            value = unchecked(((value >> 4) ^ value) * 668265261);
            return OriginalMath.Binary32(OriginalMath.Binary32((float)((unchecked((uint)value) >> 15 ^ unchecked((uint)value)) & 1023u)) / 1023f);
        }
        // f53883. Kinds dispatch exactly as the source br_table: 1/2/3/4/default.
        private void DrawWeaponFx(PixelSurface s)
        {
            float clock = OriginalMath.Binary32(time * 30f);
            int tick = Math.Abs(clock) < 2147483648f ? (int)clock : int.MinValue;
            foreach (WeaponFx fx in weaponFx)
            {
                float t = OriginalMath.Binary32(fx.age / fx.life), fade = OriginalMath.Binary32(1f - t);
                Color lane = palette[fx.color], bright = Mix(lane, Color.white, .6f);
                switch ((int)fx.kind)
                {
                    case 1: DrawPierceWeapon(s, fx, t, fade, lane, bright); break;
                    case 2:
                        if (fx.form >= 3) DrawCollapseWeapon(s, fx, t, fade, lane, bright);
                        else DrawQuakeWeapon(s, fx, t, fade, lane, bright);
                        break;
                    case 3: DrawBurnWeapon(s, fx, lane); break;
                    case 4: DrawArcWeapon(s, fx, t, fade, lane, tick); break;
                    default: DrawSplitWeapon(s, fx, t, lane, bright); break;
                }
            }
        }
        private void DrawPierceWeapon(PixelSurface s, WeaponFx fx, float t, float fade, Color lane, Color bright)
        {
            Vector2 last = fx.extras[fx.extras.Length - 1];
            Vector2 dir = WeaponDirection(last - fx.at, Vector2.down);
            Vector2 end = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(dir.x * 16f) + last.x), OriginalMath.Binary32(OriginalMath.Binary32(dir.y * 16f) + last.y));
            float shrink = OriginalMath.Binary32(OriginalMath.Binary32(t * -.6f) + 1f);
            s.GlowLine(fx.at, end, OriginalMath.Binary32(shrink * (fx.form > 2 ? 26f : 16f)), Alpha(lane, OriginalMath.Binary32((OriginalMath.Binary32(fade * .55f)) * lane.a)));
            s.Segment(fx.at, end, OriginalMath.Binary32(OriginalMath.Binary32(shrink * (fx.form > 2 ? 3.2f : 2.2f)) + .6f), new Color(1f, 1f, 1f, fade));
            if (fx.form >= 3)
            {
                for (int i = 0; i < 2; i++)
                {
                    float x = OriginalMath.Binary32(dir.y * (i == 0 ? 5f : -5f)), y = OriginalMath.Binary32(dir.x * (i == 0 ? -5f : 5f));
                    s.Segment(new Vector2(OriginalMath.Binary32(x + fx.at.x), OriginalMath.Binary32(y + fx.at.y)), new Vector2(OriginalMath.Binary32(end.x + x), OriginalMath.Binary32(end.y + y)), 1.4f,
                        SourceColor(4575657222467066266UL, 1065353216u, OriginalMath.Binary32(fade * .8f)));
                }
            }
            float travel = Unit(OriginalMath.Binary32(t * 2.2f)), blend = OriginalMath.WasmMax(travel, 0f);
            Vector2 center = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(blend * (OriginalMath.Binary32(end.x - fx.at.x))) + fx.at.x), OriginalMath.Binary32(OriginalMath.Binary32(blend * (OriginalMath.Binary32(end.y - fx.at.y))) + fx.at.y));
            float span = OriginalMath.Binary32((float)unchecked(fx.form * 6 + 8)), sideX = OriginalMath.Binary32(-dir.y * span), sideY = OriginalMath.Binary32(dir.x * span);
            s.Segment(new Vector2(OriginalMath.Binary32(center.x - sideX), OriginalMath.Binary32(center.y - sideY)), new Vector2(OriginalMath.Binary32(center.x + sideX), OriginalMath.Binary32(sideY + center.y)), 2f,
                new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * (OriginalMath.Binary32(1f - travel)))));
            s.Glow(center.x, center.y, 24f, 24f, Alpha(bright, OriginalMath.Binary32((OriginalMath.Binary32(fade * (OriginalMath.Binary32((OriginalMath.Binary32(1f - travel)) * .8f)))) * bright.a)), 0f);
            for (int i = 0; i < fx.extras.Length; i++)
            {
                float phase = Unit(OriginalMath.Binary32(OriginalMath.Binary32(t * 3f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.15f)));
                if (phase <= 0f) continue;
                Vector2 at = fx.extras[i];
                float radius = OriginalMath.Binary32(OriginalMath.Binary32(phase * 4f) + 5f), dx = OriginalMath.Binary32(radius * dir.x), dy = OriginalMath.Binary32(radius * dir.y), px = OriginalMath.Binary32(radius * -dir.y);
                float plusX = OriginalMath.Binary32(px + at.x), plusY = OriginalMath.Binary32(dx + at.y), minusX = OriginalMath.Binary32(at.x - px), minusY = OriginalMath.Binary32(at.y - dx);
                float alpha = OriginalMath.Binary32(fade * (OriginalMath.Binary32(OriginalMath.Binary32(phase * -.3f) + 1f)));
                s.Segment(new Vector2(OriginalMath.Binary32(minusX - dx), OriginalMath.Binary32(minusY - dy)), new Vector2(OriginalMath.Binary32(dx + plusX), OriginalMath.Binary32(dy + plusY)), 1.8f,
                    new Color(1f, 1f, 1f, alpha));
                s.Segment(new Vector2(OriginalMath.Binary32(dx + minusX), OriginalMath.Binary32(dy + minusY)), new Vector2(OriginalMath.Binary32(plusX - dx), OriginalMath.Binary32(plusY - dy)), 1.8f,
                    Alpha(bright, OriginalMath.Binary32(bright.a * alpha)));
                float x = OriginalMath.Binary32(OriginalMath.Binary32(phase * -dir.y) * 9f), y = OriginalMath.Binary32(OriginalMath.Binary32(phase * dir.x) * 9f);
                Color dust = Alpha(lane, OriginalMath.Binary32(lane.a * alpha));
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + at.x) + -2f), OriginalMath.Binary32(OriginalMath.Binary32(y + at.y) + -2f), 3f, 3f, dust);
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + -1f) - x), OriginalMath.Binary32(OriginalMath.Binary32(at.y + -1f) - y), 3f, 3f, dust);
            }
            if (fx.form < 3) return;
            float ox = OriginalMath.Binary32(dir.y * -11f), oy = OriginalMath.Binary32(dir.x * 11f);
            foreach (float phase in weaponThirds)
            {
                float cycle = Fraction(OriginalMath.Binary32(OriginalMath.Binary32(t * 1.6f) + phase)), amount = OriginalMath.WasmMax(cycle, 0f);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(amount * (OriginalMath.Binary32(end.x - fx.at.x))) + fx.at.x), y = OriginalMath.Binary32(OriginalMath.Binary32(amount * (OriginalMath.Binary32(end.y - fx.at.y))) + fx.at.y);
                s.Segment(new Vector2(OriginalMath.Binary32(x - ox), OriginalMath.Binary32(y - oy)), new Vector2(OriginalMath.Binary32(ox + x), OriginalMath.Binary32(oy + y)), 1.5f,
                    SourceColor(4575657222466227405UL, 1065353216u, OriginalMath.Binary32(fade * (OriginalMath.Binary32(1f - cycle)))));
            }
        }
        private void DrawBurnWeapon(PixelSurface s, WeaponFx fx, Color lane)
        {
            Color hot = Mix(lane, new Color(1f, .5f, .1f, 1f), .5f);
            for (int i = 0; i < fx.extras.Length; i++)
            {
                // WAT divides by (life * .8), not life followed by multiplying .8.
                float t = Unit(OriginalMath.Binary32((OriginalMath.Binary32(fx.age + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.09f))) / (OriginalMath.Binary32(fx.life * .8f))));
                if (t <= 0f) continue;
                Vector2 at = fx.extras[i], from = i == 0 ? fx.at : fx.extras[i - 1];
                float travel = OriginalMath.WasmMax(OriginalMath.WasmMin(OriginalMath.Binary32(t * 4f), 1f), 0f), fade = OriginalMath.Binary32(1f - t);
                Vector2 to = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(travel * (OriginalMath.Binary32(at.x - from.x))) + from.x), OriginalMath.Binary32(OriginalMath.Binary32(travel * (OriginalMath.Binary32(at.y - from.y))) + from.y));
                s.GlowLine(from, to, OriginalMath.Binary32(OriginalMath.Binary32(fade * 12f) + 2f), SourceColor(4532422664333033472UL, 1036831949u, OriginalMath.Binary32(fade * .7f)));
                float height = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(t * 3.1415927410125732f)) * 18f) + 10f)) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)fx.form) * .25f) + 1f)));
                float shortHeight = OriginalMath.Binary32(height * .65f), bottom = OriginalMath.Binary32(at.y + 2f);
                for (int j = 0; j < 3; j++)
                {
                    float side = OriginalMath.Binary32((float)(j - 1)), x = OriginalMath.Binary32(at.x + OriginalMath.Binary32(side * 4.5f));
                    float yphase = j == 0 ? -2.3f : j == 1 ? 0f : 2.3f;
                    Vector2 top = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 18f) + side) + OriginalMath.Binary32((float)i))) * 3f)),
                        OriginalMath.Binary32(bottom - OriginalMath.Binary32((j == 1 ? height : shortHeight) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 1.7f) + (OriginalMath.Binary32(OriginalMath.Binary32(time * 34f) + yphase)))) * .25f) + .75f)))));
                    s.GlowLine(new Vector2(x, bottom), top, OriginalMath.Binary32(OriginalMath.Binary32(fade * (j == 1 ? 9f : 7f)) + 1f),
                        SourceColor(j == 1 ? 4568451461351997440UL : 4528099208518959104UL,
                            j == 1 ? 1055286886u : 1034147594u, OriginalMath.Binary32(fade * .95f)));
                }
                s.Glow(at.x, bottom, OriginalMath.Binary32(OriginalMath.Binary32(fade * 26f) + 6f), OriginalMath.Binary32(OriginalMath.Binary32(fade * 18f) + 6f), Alpha(hot, OriginalMath.Binary32(hot.a * (OriginalMath.Binary32(fade * .8f)))), 0f);
                for (int j = 0; j < 3; j++)
                {
                    float cycle = Fraction(OriginalMath.Binary32(OriginalMath.Binary32(t * 1.4f) + weaponThirds[j]));
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + OriginalMath.Binary32((float)(j * 2))) + OriginalMath.Binary32((float)i))) * 6f)) + -1f),
                        OriginalMath.Binary32(OriginalMath.Binary32(at.y + OriginalMath.Binary32(cycle * -34f)) + -1f), 2f, 2f,
                        new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32(cycle * -.4f) + .75f), BitConverter.Int32BitsToSingle(1045220557), OriginalMath.Binary32(fade * (OriginalMath.Binary32(1f - cycle)))));
                }
                if (t > .6f)
                {
                    float smoke = OriginalMath.Binary32((OriginalMath.Binary32(t + -.6f)) / .4f);
                    s.Glow(at.x, OriginalMath.Binary32(OriginalMath.Binary32(at.y + -20f) + OriginalMath.Binary32(smoke * -16f)), OriginalMath.Binary32(OriginalMath.Binary32(smoke * 14f) + 18f), OriginalMath.Binary32(OriginalMath.Binary32(smoke * 10f) + 14f),
                        SourceColor(4441630096503917773UL, 1034147594u, OriginalMath.Binary32((OriginalMath.Binary32(1f - smoke)) * .35f)), 0f);
                }
            }
        }
        private static Vector2 SplitCurve(Vector2 from, Vector2 control, Vector2 to, float t)
        {
            // f53943: clamp before both interpolations and retain the WAT
            // multiply/add order. Every operation returns an f32 result.
            t = Unit(t);
            float y = OriginalMath.Binary32(OriginalMath.Binary32(t * OriginalMath.Binary32(control.y - from.y)) + from.y);
            float endY = OriginalMath.Binary32(OriginalMath.Binary32(t * OriginalMath.Binary32(to.y - control.y)) + control.y);
            y = OriginalMath.Binary32(y + OriginalMath.Binary32(t * OriginalMath.Binary32(endY - y)));
            float x = OriginalMath.Binary32(OriginalMath.Binary32(t * OriginalMath.Binary32(control.x - from.x)) + from.x);
            float endX = OriginalMath.Binary32(OriginalMath.Binary32(t * OriginalMath.Binary32(to.x - control.x)) + control.x);
            x = OriginalMath.Binary32(x + OriginalMath.Binary32(t * OriginalMath.Binary32(endX - x)));
            return new Vector2(x, y);
        }
        private void DrawSplitWeapon(PixelSurface s, WeaponFx fx, float t, Color lane, Color bright)
        {
            float flash = Unit(OriginalMath.Binary32(OriginalMath.Binary32(t * -2.5f) + 1f));
            if (flash > 0f)
            {
                float span = OriginalMath.Binary32(OriginalMath.Binary32(flash * 14f) + 4f);
                Color white = new Color(1f, 1f, 1f, flash);
                s.Segment(new Vector2(OriginalMath.Binary32(fx.at.x - span), fx.at.y), new Vector2(OriginalMath.Binary32(span + fx.at.x), OriginalMath.Binary32(fx.at.y + 0f)), 2f, white);
                s.Segment(new Vector2(fx.at.x, OriginalMath.Binary32(fx.at.y - span)), new Vector2(OriginalMath.Binary32(fx.at.x + 0f), OriginalMath.Binary32(span + fx.at.y)), 2f, white);
                s.Glow(fx.at.x, fx.at.y, OriginalMath.Binary32(flash * 34f), OriginalMath.Binary32(flash * 34f), Alpha(bright, OriginalMath.Binary32(flash * bright.a)), 0f);
            }
            float travel = Unit(OriginalMath.Binary32(t / .45f)), trail = Unit(OriginalMath.WasmMax(OriginalMath.Binary32(travel + -.25f), 0f));
            float amount = OriginalMath.WasmMax(travel, 0f), impact = Unit(OriginalMath.Binary32((OriginalMath.Binary32(t + -.45f)) / .55f)), fade = OriginalMath.Binary32(1f - impact);
            float distance = OriginalMath.Binary32(OriginalMath.Binary32(impact * 14f) + 4f);
            for (int i = 0; i < fx.extras.Length; i++)
            {
                Vector2 at = fx.extras[i], dir = ShotDirection(at - fx.at);
                if (travel < 1f)
                {
                    float bend = (i & 1) != 0 ? -16f : 16f;
                    Vector2 control = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(fx.at.x + at.x)) * .5f) - OriginalMath.Binary32(dir.y * bend)),
                        OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(fx.at.y + at.y)) * .5f) + OriginalMath.Binary32(bend * dir.x)));
                    Vector2 head = SplitCurve(fx.at, control, at, amount), tail = SplitCurve(fx.at, control, at, trail);
                    s.Segment(tail, head, 2.2f, bright);
                    s.Rect(OriginalMath.Binary32(head.x + -2f), OriginalMath.Binary32(head.y + -2f), 4f, 4f, Color.white);
                }
                else
                {
                    float size = OriginalMath.Binary32(OriginalMath.Binary32(fade * 24f) + 6f);
                    s.Glow(at.x, at.y, size, size, Alpha(lane, OriginalMath.Binary32(lane.a * (OriginalMath.Binary32(fade * .9f)))), 0f);
                    Color color = Alpha(bright, OriginalMath.Binary32(fade * bright.a));
                    float pp = OriginalMath.Binary32(distance * .7071067690849304f);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(pp + at.x) + -1f), OriginalMath.Binary32(OriginalMath.Binary32(pp + at.y) + -1f), 2.5f, 2.5f, color);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(distance * -.7071067690849304f) + at.x) + -1f), OriginalMath.Binary32(OriginalMath.Binary32(pp + at.y) + -1f), 2.5f, 2.5f, color);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(distance * -.7071068286895752f) + at.x) + -1f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(distance * -.7071067094802856f) + at.y) + -1f), 2.5f, 2.5f, color);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(distance * .7071066498756409f) + at.x) + -1f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(distance * -.70710688829422f) + at.y) + -1f), 2.5f, 2.5f, color);
                }
            }
        }
    }
}
