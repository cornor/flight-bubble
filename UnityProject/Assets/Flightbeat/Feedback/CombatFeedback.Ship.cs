using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53945. Nozzles/FlameGlow/FlameCore are the constructor's literal stores.
        // InitializeArray data: global-metadata.dat +0x36EEB0/+0x36EE60/+0x36FFA0.
        private static readonly float[][][] Nozzles = {
            new[] { new[] { 0f }, new[] { -.28f, .28f, 0f }, new[] { 0f }, new[] { 0f, -.19f, .19f } },
            new[] { new[] { -.05f, .05f }, new[] { -.06f, .06f }, new[] { -.06f, .06f }, new[] { -.07f, .07f, -.34f, .34f } },
            new[] { new[] { 0f }, new[] { 0f, -.34f, .34f }, new[] { 0f }, new[] { 0f, -.2f, .2f } },
            new[] { new[] { 0f }, new[] { 0f }, new[] { 0f, -.22f, .22f }, new[] { 0f, -.25f, .25f } },
            new[] { new[] { -.09f, 0f, .09f }, new[] { -.09f, 0f, .09f }, new[] { -.1f, 0f, .1f }, new[] { -.1f, 0f, .1f } }
        };
        private static readonly Color[] FlameGlow = {
            new Color(1f,.55f,.15f,1f), new Color(.4f,.7f,1f,1f), new Color(.72f,.42f,1f,1f),
            new Color(1f,.34f,.06f,1f), new Color(.35f,1f,.6f,1f)
        };
        private static readonly Color[] FlameCore = {
            new Color(1f,.85f,.4f,1f), new Color(.85f,.95f,1f,1f), new Color(1f,.85f,1f,1f),
            new Color(1f,.7f,.2f,1f), new Color(.8f,1f,.85f,1f)
        };
        private static readonly float[] TipSpan = { .4f, .42f, .44f, .4f, .44f };
        private static readonly float[] TipY = { .12f, .18f, .06f, -.08f, .02f };

        private static float Unit(float v) => v < 0f ? 0f : OriginalMath.WasmMin(v, 1f);
        private static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
        private static float Fraction(float v) => Unit(OriginalMath.Binary32(v - OriginalMath.Binary32((float)Math.Floor(v))));
        // f53902/f53903. Raw Look.stage is used for HullCell, not the Stage clamp.
        public int Stage => Look == null ? 1 : Look.stage > 1 ? (Look.stage < 5 ? Look.stage : 5) : 1;
        private int HullCell(int tier)
        {
            tier = tier < 3 ? tier : 3;
            tier = tier > 0 ? tier : 0;
            if (Look != null && tier > 1 && Look.stage > 3) tier = tier == 2 ? 4 : 5;
            return (tier << 1) + Model * 12;
        }

        // f53904/f53905. Field pose visibility, appearance pop, and rounded bob.
        public void DrawFighter(PixelSurface s, int color, int tier, Vector2 at, float size, float angle, float alpha)
            => Ship(s, color, tier, at, size, angle, new Color(1f, 1f, 1f, alpha), 0f, true);
        public void DrawShip(PixelSurface s, Fighter ship, int slot)
        {
            if (!poses.TryGetValue(ship.Id, out Pose p) || time < p.appear) return;
            float size = HullSize(ship.Tier);
            if (p.appear > 0f)
            {
                float t = Unit(OriginalMath.Binary32((OriginalMath.Binary32(time - p.appear)) / .4f));
                size  =                 OriginalMath.Binary32(size*(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) * (OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(t * 3.1415927410125732f)) * .45f))) + 1f))
                    + (t < .12f ? OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(t / -.12f) + 1f)) * .25f) : 0f))));
            }
            float bob = OriginalMath.RoundSingle(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 4f) + OriginalMath.Binary32((float)ship.Id))) * 1.5f));
            Ship(s, ship.Color, ship.Tier, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(bob * 0f) + p.position.x), OriginalMath.Binary32(OriginalMath.Binary32(bob * 1f) + p.position.y)),
                size, p.angle, Color.white, p.recoil, true);
        }

        // f53896. Preserve aura -> engines -> part marks -> three hull layers -> core.
        private void Ship(PixelSurface s, int color, int tier, Vector2 at, float size, float angle, Color tint, float recoil, bool field)
        {
            if (field) size = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(Stage - 1)) * .05f) + 1f)) * size);
            at.x = OriginalMath.RoundSingle(at.x);
            at.y = OriginalMath.RoundSingle(OriginalMath.Binary32(OriginalMath.Binary32(recoil * 5f) + at.y));
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 3.4f) + OriginalMath.Binary32((float)color))) * .5f) + .5f);
            Color lane = palette[color];
            bool maximum = tier > 2 && field;
            float alpha = tint.a;
            if (maximum)
            {
                s.Glow(at.x, at.y, OriginalMath.Binary32(size * 2.3f), OriginalMath.Binary32(size * 2.1f), Alpha(lane, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .26f)))), 0f);
                s.Glow(at.x, at.y, OriginalMath.Binary32(size * 1.5f), OriginalMath.Binary32(size * 1.4f), Alpha(lane, OriginalMath.Binary32(alpha * .4f)), 0f);
                for (int n = 0; n < 6; n++)
                    s.Ring(new Vector2(OriginalMath.Binary32((0f + 0f) + at.x), OriginalMath.Binary32((1f + 1f) + at.y)), OriginalMath.Binary32(size * .8f),
                        new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .5f)), 1.5f, 3, OriginalMath.Binary32(OriginalMath.Binary32(time * 70f) + OriginalMath.Binary32((float)(n * 60))), 18f);
            }
            else if (tier == 2 && field)
            {
                s.Glow(at.x, at.y, OriginalMath.Binary32(size * 2.1f), OriginalMath.Binary32(size * 1.9f), Alpha(lane, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .15f) + .35f)))), 0f);
                for (int n = 0; n < 2; n++)
                {
                    float a = OriginalMath.Binary32(OriginalMath.Binary32(time * 2.4f) + (n == 0 ? 0f : 3.1415927410125732f));
                    float x = OriginalMath.Binary32(at.x + OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Cos(a)) * .62f));
                    float y = OriginalMath.Binary32(at.y + OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Sin(a)) * .35f));
                    s.Glow(x, y, 14f, 14f, new Color(.6f, .9f, 1f, OriginalMath.Binary32(alpha * .8f)), 0f);
                    s.Rect(OriginalMath.Binary32(x + -1f), OriginalMath.Binary32(y + -1f), 3f, 3f, new Color(.9f, 1f, 1f, alpha));
                }
            }
            else if (tier == 1 && field)
                s.Glow(at.x, at.y, OriginalMath.Binary32(size * 1.7f), OriginalMath.Binary32(size * 1.6f), Alpha(lane, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .22f)) * alpha)), 0f);
            else s.Glow(at.x, OriginalMath.Binary32(OriginalMath.Binary32(size * .3f) + at.y), OriginalMath.Binary32(size * .9f), OriginalMath.Binary32(size * 1.3f), Alpha(lane, OriginalMath.Binary32(alpha * .2f)), 0f);
            DrawEngines(s, color, tier, at, size, tint);
            if (field)
            {
                DrawGuardian(s, at, size, tint);
                DrawPartMarks(s, tier, at, size, angle, tint, lane);
            }
            int cell = HullCell(tier);
            s.HullAt(cell, at, OriginalMath.Binary32(size * 1.18f), Alpha(lane, OriginalMath.Binary32(alpha * .55f)), angle);
            s.HullAt(cell, at, size, new Color(OriginalMath.Binary32(lane.r * tint.r), OriginalMath.Binary32(lane.g * tint.g), OriginalMath.Binary32(lane.b * tint.b), alpha), angle);
            s.HullAt(cell | 1, at, size, tint, angle);
            if (!field) return;
            DrawPartCore(s, at, size, angle, tint, lane);
            if (tier > 0)
            {
                float phase = OriginalMath.Binary32(OriginalMath.Binary32(time * 3f) + OriginalMath.Binary32((float)color));
                int whole = Math.Abs(phase) < 2147483648f ? (int)phase : int.MinValue;
                if ((whole & 1) == 0)
                {
                    float dx = OriginalMath.Binary32((tier > 2 ? 1.1f : tier == 2 ? 1.05f : .95f) * (OriginalMath.Binary32(size * TipSpan[Model])));
                    s.Glow(OriginalMath.Binary32(at.x - dx), OriginalMath.Binary32(OriginalMath.Binary32(size * TipY[Model]) + at.y), 10f, 10f, new Color(1f, .3f, .3f, OriginalMath.Binary32(alpha * .9f)), 0f);
                    s.Glow(OriginalMath.Binary32(dx + at.x), OriginalMath.Binary32(OriginalMath.Binary32(size * TipY[Model]) + at.y), 10f, 10f, new Color(.3f, 1f, .5f, OriginalMath.Binary32(alpha * .9f)), 0f);
                }
            }
            if (!maximum) return;
            float[] angles = { 0f, 2.094f, 4.188f };
            for (int n = 0; n < 3; n++)
            {
                float twinkle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + OriginalMath.Binary32((float)(n * 2)))) * .5f) + .5f);
                float a = OriginalMath.Binary32(OriginalMath.Binary32(time * 1.8f) + angles[n]);
                s.SpriteAt(9, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Cos(a)) * .95f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Sin(a)) * .5f) + at.y)),
                    OriginalMath.Binary32(OriginalMath.Binary32(twinkle * 4f) + 5f), new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * twinkle) * .7f)), 0f);
            }
        }

        // f53906. Rank text width uses its UTF-16 length, matching String.Length.
        public static void DrawRank(PixelSurface s, Fighter ship, Vector2 at)
        {
            string text = ship.Tier > 2 ? "MAX" : ship.Tier == 2 ? "III" : ship.Tier == 1 ? "II" : "I";
            float width = (text.Length << 3) | 6;
            s.Rect(at.x, at.y, width, 12f, ship.Tier > 2 ? new Color(.35f, .2f, .02f, .95f) : new Color(.015f, .06f, .1f, .9f));
            if (ship.Tier >= 3) s.Border(at.x, at.y, width, 12f, new Color(1f, .78f, .3f, 1f), 1f);
            s.Text(text, OriginalMath.Binary32(at.x + 3f), OriginalMath.Binary32(at.y + 2f), 1f, ship.Tier > 2 ? new Color(1f, .85f, .35f, 1f) : Color.white, false);
        }

        // f53908. Mystery fighters remain hidden only below the reserve head.
        public void DrawReserve(PixelSurface s, Fighter ship, int lane, int row, float openingOffset, float fade)
        {
            Vector2 pos = ReservePosition(ship, lane, row);
            float targetX = battle.QueuePositionX(lane), targetY = Battle.QueueY(row), y = OriginalMath.Binary32(pos.y + openingOffset);
            float dx = OriginalMath.Binary32(pos.x - targetX), dy = OriginalMath.Binary32(pos.y - targetY);
            float distance = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy))));
            if (distance > 2f)
            {
                float x = OriginalMath.Binary32(pos.x + 0f), start = OriginalMath.Binary32(y + 19f);
                s.GlowLine(new Vector2(x, start), new Vector2(x, OriginalMath.Binary32(start + OriginalMath.WasmMin(OriginalMath.Binary32(distance * .25f), 26f))), 12f, palette[ship.Color]);
            }
            if (row > 0 && ship.Kind == 2)
            {
                s.Glow(pos.x, y, 42f, 38f, new Color(.18f, .25f, .42f, .35f), 0f);
                s.Corners(OriginalMath.Binary32(pos.x + -15f), OriginalMath.Binary32(y + -15f), 30f, 30f, new Color(.2823529541492462f, .30588236451148987f, .3960784375667572f, 1f), 7, 2);
                s.Text("?", OriginalMath.Binary32(pos.x + -8f), OriginalMath.Binary32(y + -7f), 2f, Color.white, false);
                return;
            }
            Color color = palette[ship.Color];
            s.Glow(pos.x, OriginalMath.Binary32(y + -2f), 64f, 52f, Alpha(color, OriginalMath.Binary32(fade * .34f)), 0f);
            s.Rect(OriginalMath.Binary32(pos.x + -22f), OriginalMath.Binary32(y + 12f), 44f, 3f, Alpha(color, OriginalMath.Binary32(fade * .9f)));
            float angle = OriginalMath.Binary32((OriginalMath.Binary32(targetX - pos.x)) * .3f);
            angle = angle < -22f ? -22f : OriginalMath.WasmMin(angle, 22f);
            Ship(s, ship.Color, ship.Tier, new Vector2(OriginalMath.Binary32(pos.x + 0f), OriginalMath.Binary32(y + -6f)), 38f, angle, new Color(1f, 1f, 1f, fade), 0f, false);
        }
    }
}
