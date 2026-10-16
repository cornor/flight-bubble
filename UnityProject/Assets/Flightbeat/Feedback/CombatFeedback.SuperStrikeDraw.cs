using System;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53914. Pending strike, curved projectile trails and the detonation
        // share screen-space geometry; restore the caller's layout map at end.
        private void DrawSuperStrike(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            SuperStrike pending = battle.PendingStrike;
            if (pending != null)
            {
                Vector2 point = ScreenLayout.Point(ScreenLayout.EnemyPoint(pending.X, pending.Y));
                Color color = pending.Missile ? new Color(.35f, .95f, 1f, 1f) : new Color(1f, .55f, .18f, 1f);
                float radius = OriginalMath.Binary32(pending.RadiusX * ScreenLayout.EnemyScaleX);
                SuperStrikeVisuals.Reticle(s, point, OriginalMath.Binary32(radius * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - pending.Age)) * .08f) + 1f))), OriginalMath.Binary32(time * 3f), color, false);
                float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 18f)) * .5f) + .5f);
                s.Glow(point.x, point.y, OriginalMath.Binary32(radius * 2.2f), OriginalMath.Binary32(radius * 2.2f), Alpha(color, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .12f)), 0f);
                s.Segment(new Vector2(point.x, ScreenLayout.Y(118f)), point, 2f,
                    SourceColor(4510805389557628928UL, 1045220557u, OriginalMath.Binary32(pulse * .55f)));
                float top = OriginalMath.Binary32(point.y - radius);
                s.Rect(OriginalMath.Binary32(point.x + -40f), OriginalMath.Binary32(top + -38f), 80f, 26f, SourceColor(4392630932023269130UL, 4568451461315087565UL));
                s.Border(OriginalMath.Binary32(point.x + -40f), OriginalMath.Binary32(top + -38f), 80f, 26f, color, 1f);
                string countdown = (OriginalMath.Binary32(1f - pending.Age)).ToString("0.0") + "S";
                s.Text(countdown, OriginalMath.Binary32(point.x + OriginalMath.Binary32(PixelSurface.Measure(countdown, 2f) * -.5f)), OriginalMath.Binary32(top + -33f), 2f, Color.white, false);
                float progress = Unit(OriginalMath.Binary32((OriginalMath.Binary32(pending.Age + -.1f)) / .9f));
                if (progress > 0f) DrawSuperProjectiles(s, point, radius, progress, pending.Missile, color);
            }
            if (lastStrike != null && superAge < 2f) DrawSuperImpact(s);
            s.MapLayout = mapped;
        }

        // Extracted from f53914's inlined quadratic path. Do not replace with
        // Vector2.Lerp: source multiplies and adds each coordinate in this order.
        private static Vector2 StrikeCurve(Vector2 from, Vector2 control, Vector2 to, float t)
        {
            float square = OriginalMath.Binary32(t * t), one = OriginalMath.Binary32(1f - t), twice = OriginalMath.Binary32(t * (OriginalMath.Binary32(one + one))), complement = OriginalMath.Binary32(one * one);
            return new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(square * to.x) + OriginalMath.Binary32(complement * from.x)) + OriginalMath.Binary32(twice * control.x)),
                OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(to.y * square) + OriginalMath.Binary32(complement * from.y)) + OriginalMath.Binary32(twice * control.y)));
        }
        private static readonly float[] StrikeTrailOffsets = { -.05f, -.1f, -.15f, -.2f, -.25f, -.3f };
        private static readonly ulong[] StrikeTrailColors = {
            4494952716805183898UL, 4486305805177035162UL, 4477658893548886426UL,
            4469011981920737690UL, 4453159317760743834UL, 4435865494504446362UL
        };
        private void DrawSuperProjectiles(PixelSurface s, Vector2 point, float radius, float progress, bool missile, Color color)
        {
            float t = OriginalMath.Binary32(progress * (OriginalMath.Binary32(progress * progress))), endY = OriginalMath.Binary32(point.y + 0f);
            int count = missile ? 5 : 1;
            float spread = OriginalMath.Binary32(OriginalMath.Binary32((float)(count - 1)) * .5f);
            for (int i = 0; i < count; i++)
            {
                float lane = missile ? OriginalMath.Binary32(OriginalMath.Binary32((float)i) - spread) : 0f;
                Vector2 from = new Vector2(missile ? OriginalMath.Binary32(point.x + OriginalMath.Binary32(lane * 140f)) : point.x,
                    missile ? OriginalMath.Binary32(ScreenLayout.Y(60f) + OriginalMath.Binary32(Math.Abs(lane) * -30f)) : ScreenLayout.Y(40f));
                Vector2 to = new Vector2(OriginalMath.Binary32(point.x + OriginalMath.Binary32(OriginalMath.Binary32(radius * lane) * .35f)), endY);
                Vector2 control = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(lane * 60f) + OriginalMath.Binary32((OriginalMath.Binary32(from.x + to.x)) * .5f)), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(endY + from.y)) * .5f) + -40f));
                Vector2 current = StrikeCurve(from, control, to, t);
                Vector2 previous = StrikeCurve(from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + -.08f), 0f));
                float dy = OriginalMath.Binary32(current.y - previous.y), dx = OriginalMath.Binary32(current.x - previous.x);
                float length = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dy * dy) + OriginalMath.Binary32(dx * dx))));
                Vector2 direction = length > .00001f ? new Vector2(OriginalMath.Binary32(dx / length), OriginalMath.Binary32(dy / length)) : Vector2.zero;
                // Source tests strictly < 0, not <= 0; preserve even at zero length.
                float zx = OriginalMath.Binary32(direction.x - 0f), zy = OriginalMath.Binary32(direction.y - 0f);
                if (OriginalMath.Binary32(OriginalMath.Binary32(zx * zx) + OriginalMath.Binary32(zy * zy)) < 0f) direction = Vector2.up;
                for (int trail = 0; trail < 6; trail++)
                {
                    Vector2 at = StrikeCurve(from, control, to, OriginalMath.WasmMax(OriginalMath.Binary32(t + StrikeTrailOffsets[trail]), 0f));
                    float size = OriginalMath.Binary32((float)(trail * 5 + 21));
                    s.Glow(at.x, at.y, size, size, SourceColor(4559804549719654400UL, StrikeTrailColors[trail]), 0f);
                }
                s.GlowLine(previous, current, 30f, Alpha(color, .7f));
                if (!missile) s.DeckArt(17, OriginalMath.Binary32(current.x + -26f), OriginalMath.Binary32(current.y + -26f), 52f, 52f, Color.white, true);
                else s.SpriteAt(10, current, 30f, Color.white, OriginalMath.Binary32(OriginalMath.Atan2Single(direction.x, -direction.y) * 57.295780181884766f));
                s.Glow(current.x, current.y, 60f, 60f, Alpha(color, .5f), 0f);
            }
        }

        private void DrawSuperImpact(PixelSurface s)
        {
            Vector2 point = ScreenLayout.Point(ScreenLayout.EnemyPoint(lastStrike.X, lastStrike.Y));
            Color color = lastStrike.Missile ? new Color(.4f, .94f, 1f, 1f) : new Color(1f, .56f, .16f, 1f);
            float fade = Unit(OriginalMath.Binary32(OriginalMath.Binary32(superAge / -1.6f) + 1f));
            float growth = OriginalMath.Binary32(1f - OriginalMath.Pow(OriginalMath.Binary32(1f - Unit(OriginalMath.Binary32(superAge / .6f))), 3f));
            float radius = OriginalMath.Binary32(lastStrike.RadiusX * ScreenLayout.EnemyScaleX);
            if (superAge < .5f)
            {
                // WAT: 1 - (age + age). Decompiled C loses the parentheses.
                float flash = OriginalMath.Binary32(1f - (OriginalMath.Binary32(superAge + superAge)));
                s.Glow(point.x, point.y, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * 3.2f) * growth) + 40f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * 3f) * growth) + 40f), Alpha(color, OriginalMath.Binary32(flash * .55f)), 0f);
                s.Glow(point.x, point.y, OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius + radius)) * growth) + 30f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * 1.9f) * growth) + 30f),
                    SourceColor(4564848585086074880UL, 1051931443u, OriginalMath.Binary32(flash * .8f)), 0f);
                s.Glow(point.x, point.y, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * 1.1f) * growth) + 20f), OriginalMath.Binary32(OriginalMath.Binary32(radius * growth) + 20f),
                    SourceColor(4575657222473777152UL, 1063675494u, flash), 0f);
            }
            float expanded = OriginalMath.Binary32(radius * growth);
            s.Ring(point, OriginalMath.WasmMax(expanded, 2f), Alpha(color, fade), 6f, 64, 0f, 360f);
            s.Ring(point, OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(expanded * 1.18f) + -6f), 2f), Alpha(color, OriginalMath.Binary32(fade * .75f)), 2f, 64, 0f, 360f);
            s.Ring(point, OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(expanded * 1.36f) + -12f), 2f), Alpha(color, OriginalMath.Binary32(fade * .5f)), 2f, 64, 0f, 360f);
            s.Ring(point, OriginalMath.Binary32(radius * .95f), SourceColor(4539628425454813184UL, 1045220557u, OriginalMath.Binary32(fade * .35f)), 2f, 48, 0f, 360f);
            float inner = OriginalMath.Binary32(expanded * .5f);
            for (int i = 0; i < 24; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 3.1415927410125732f) / 12f) + OriginalMath.Binary32(OriginalMath.Binary32((float)(i & 1)) * .1f));
                float sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle);
                float outer = OriginalMath.Binary32(expanded * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(i % 3)) * .18f) + 1.1f)));
                s.Segment(new Vector2(OriginalMath.Binary32(point.x + OriginalMath.Binary32(inner * cos)), OriginalMath.Binary32(point.y + OriginalMath.Binary32(inner * sin))),
                    new Vector2(OriginalMath.Binary32(point.x + OriginalMath.Binary32(cos * outer)), OriginalMath.Binary32(point.y + OriginalMath.Binary32(sin * outer))), (i & 1) != 0 ? 2f : 3f,
                    SourceColor(4568451461351997440UL, 1058642330u, OriginalMath.Binary32(fade * .8f)));
            }
            if (superAge > .15f && superAge < 1.6f)
            {
                float alpha = Unit(OriginalMath.Binary32((OriginalMath.Binary32(1.6f - superAge)) / .4f)), one = OriginalMath.Binary32(1f - Unit(OriginalMath.Binary32((OriginalMath.Binary32(superAge + -.15f)) / .2f)));
                // Keep both source additions; cancelling -1/+1 changes rounding.
                float size = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(one * (OriginalMath.Binary32(one * one))) + -1f)) + 1f)) * 1.4f) + 2.2f);
                float y = OriginalMath.Binary32(OriginalMath.Binary32(point.y + OriginalMath.Binary32(radius * -.5f)) + -30f);
                string text = "击破 ×" + strikeHits.ToString();
                s.TextF(text, OriginalMath.Binary32(point.x + 2f), OriginalMath.Binary32(OriginalMath.Binary32(y - OriginalMath.Binary32(superAge * 14f)) + 2f), size, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .8f)), 0);
                s.TextF(text, point.x, OriginalMath.Binary32(y - OriginalMath.Binary32(superAge * 14f)), size, SourceColor(4572054341912887296UL, 1060320051u, alpha), 0);
            }
        }
    }
}
