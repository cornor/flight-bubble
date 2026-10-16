using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53833. Tick inlines this same body; keep each original f32 boundary.
        private void TickCombo(float dt)
        {
            int combo = battle.Combo;
            int band = combo > 999 ? 5 : combo > 499 ? 4 : combo > 249 ? 3 : combo > 99 ? 2 : combo > 49 ? 1 : 0;
            if (band > comboBand)
            {
                comboMilestoneAge = 0f;
                milestoneValue = combo > 999 ? 1000 : combo > 499 ? 500 : combo > 249 ? 250 : combo < 100 ? 50 : 100;
            }
            comboBand = band;
            comboMilestoneAge = OriginalMath.Binary32(comboMilestoneAge + dt);
            float punch = lastCombo >= combo ? comboPunch : 0f;
            lastCombo = combo;
            comboPunch = OriginalMath.Binary32(punch + dt);
            float target = combo > 4 ? 1f : 0f, fade = comboFade;
            float step = OriginalMath.Binary32((combo > 4 ? 8f : 2.5f) * dt);
            float difference = OriginalMath.Binary32(target - fade);
            comboFade = Math.Abs(difference) <= step ? target : OriginalMath.Binary32(fade + (difference >= 0f ? step : -step));
        }

        // f53834. Literal channel bits and signed thresholds, including negatives.
        private static Color ComboColor(int combo) => combo > 999 ? new Color(1f, .35f, .45f, 1f)
            : combo > 499 ? new Color(1f, .55f, .2f, 1f)
            : combo > 249 ? new Color(.8f, .5f, 1f, 1f)
            : combo < 100 ? new Color(.45f, .95f, 1f, 1f) : new Color(1f, .85f, .3f, 1f);

        // f53909/f53910. The unsigned range test means kind 101..199.
        // These helpers exist in the original even though DrawReserve does not call them.
        private static string QueueBadge(Fighter ship)
        {
            int kind = ship.Kind;
            switch (kind)
            {
                case 0: case 2: return String.Empty;
                case 1: return "L";
                case 3: return "I" + QueueParameter(ship);
                case 4: return "B" + QueueParameter(ship);
                case 5: return "F";
                case 501: return "K";
                default: return unchecked((uint)(kind - 200)) >= unchecked((uint)-99)
                    ? "C" + unchecked(kind - 100).ToString() : kind.ToString();
            }
        }
        private static string QueueParameter(Fighter ship) => ship.Other == null || ship.Other.Length == 0 ? String.Empty : ship.Other[0].ToString();

        // f53926: two ordered segments, then restore the surface mapping flag.
        private static void DrawTimes(PixelSurface s, float x, float y, float scale, Color color)
        {
            bool mapped = s.MapLayout;
            float right = OriginalMath.Binary32(OriginalMath.Binary32(scale * 6f) + x);
            float bottom = OriginalMath.Binary32(OriginalMath.Binary32(scale * 6.5f) + y);
            float left = OriginalMath.Binary32(x + scale);
            float top = OriginalMath.Binary32(OriginalMath.Binary32(scale * 1.5f) + y);
            float width = OriginalMath.Binary32(scale * 1.6f);
            s.Segment(new Vector2(left, top), new Vector2(right, bottom), width, color);
            s.Segment(new Vector2(right, top), new Vector2(left, bottom), width, color);
            s.MapLayout = mapped;
        }

        // f53927/f53928/f53888. Width uses the raw model, accent uses Model's clamp.
        private Color Accent => FleetCatalog.FighterAccent[Model];
        private int PartLv(int part) => Look == null ? 1 : Math.Max(1, Math.Min(Look.Part(part), 5));
        private float BeamWidth => Look == null ? 1f : Look.model == 1 ? .8f : Look.model == 2 ? 1.25f : 1f;

        // f53940/f53942. A multiplies alpha; the separate Alpha helper replaces it.
        private static Vector2 Perp(Vector2 direction) => new Vector2(-direction.y, direction.x);
        private static Color A(Color color, float alpha) => new Color(color.r, color.g, color.b, OriginalMath.Binary32(color.a * alpha));
    }
}
