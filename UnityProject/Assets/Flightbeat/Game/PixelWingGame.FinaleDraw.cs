using System;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private static readonly string[] finaleNames = { "彩虹扇击", "疾风冲锋", "黑洞吞噬", "烈焰焚天", "天穹收束", "脉冲新星" }; // f54861
        private static readonly float[] finaleTrailSteps = { -.035f, -.07f, -.10500000417232513f, -.14f, -.175f };
        private static readonly float[] finaleTrailAlpha = { .42000001668930054f, .34f, .25999999046325684f, .18000000715255737f, .10000002384185791f };

        // f54742. Original six-mode dispatch, including the earlier hole/barrier charge.
        private void DrawFinale(PixelSurface s)
        {
            Finale finale = finaleShown;
            if (finale == null) return;
            if (finaleEndClock < 0f && Battle.ActiveFinale == null) finaleEndClock = ui;
            float age = finale.Age, after = 0f;
            if (finaleEndClock >= 0f) { after = OriginalMath.Binary32(ui - finaleEndClock); if (after > 1.2f) return; }
            s.Rect(12f, 574f, 516f, 300f, SourceColor(4333543705408639599UL, 1022739087u,
                OriginalMath.Binary32(OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(age / .3f)) * (OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(after / .8f))))) * .55f)));
            DrawFinaleTitle(s, age);
            if (finale.Mode == 2 && age >= 1.15f) DrawBlackHole(s, finale, age, after);
            if (finale.Mode == 4 && age >= 1.15f) DrawBarrier(s, finale, age, after);
            if (age >= 1.65f)
            {
                switch (finale.Mode)
                {
                    case 0: DrawFanLaser(s, finale, age, after); break;
                    case 1: DrawGale(s, finale, age, after); break;
                    case 3: DrawFlamethrower(s, finale, age, after); break;
                    case 5: DrawPulseNova(s, finale, age, after); break;
                }
            }
            DrawFinaleBlasts(s, finale.Mode);
            for (int i = 0; i < finaleShips.Count; i++)
            {
                float t = ShipMerge(i);
                if (t >= 1f) continue;
                FinaleShip ship = finaleShips[i];
                Vector2 at = FinaleShipPath(ship.from, t);
                for (int trail = 0; trail < 5; trail++)
                {
                    Vector2 before = FinaleShipPath(ship.from, OriginalMath.WasmMax(OriginalMath.Binary32(t + finaleTrailSteps[trail]), 0f));
                    s.GlowLine(at, before, OriginalMath.Binary32((float)(16 - trail * 2)), A(colors[ship.color], finaleTrailAlpha[trail]));
                }
                float size = CombatFeedback.HullSize(ship.tier);
                float blend = InOut(t); blend = blend < 0f ? 0f : OriginalMath.WasmMin(blend, 1f);
                Vector2 next = FinaleShipPath(ship.from, OriginalMath.WasmMin(OriginalMath.Binary32(t + .02f), 1f));
                float angle = OriginalMath.Binary32(OriginalMath.Atan2Single(OriginalMath.Binary32(next.x - at.x), OriginalMath.Binary32(at.y - next.y)) * 57.295780181884766f);
                feedback.DrawFighter(s, ship.color, ship.tier, at, OriginalMath.Binary32(size + OriginalMath.Binary32((OriginalMath.Binary32(20f - size)) * blend)), angle, Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) / .15f)));
            }
            DrawGiantPlane(s, finale, age, after);
            DrawFinaleImpact(s, finale, age, after);
            DrawFinaleCount(s, finale, age);
        }

        // f54751: eased rotation about the source merge point, followed by lift.
        private static Vector2 FinaleShipPath(Vector2 from, float t)
        {
            Vector2 goal = new Vector2(270f, 684f);
            float ease = InOut(t);
            Vector2 rotated = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(from.x - goal.x), OriginalMath.Binary32(from.y - goal.y)), OriginalMath.Binary32(ease * (from.x < goal.x ? -220f : 220f)));
            float fade = OriginalMath.Binary32(1f - ease), lift = OriginalMath.Sin(OriginalMath.Binary32(ease * 3.1415927410125732f));
            return new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(fade * rotated.x) + goal.x) - OriginalMath.Binary32(OriginalMath.Binary32(lift * 0f) * 40f)), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(fade * rotated.y) + goal.y) - OriginalMath.Binary32(OriginalMath.Binary32(lift * 1f) * 40f)));
        }

        // f54755 / f54736.
        private static float FinaleLift(float age, float after) => OriginalMath.Binary32((age < 1.15f ? 0f : OriginalMath.WasmMin(OriginalMath.Binary32((OriginalMath.Binary32(age + -1.15f)) * 30f), 10f))
            + OriginalMath.Binary32(OutCubic(Clamp01(OriginalMath.Binary32(after / 1.1f))) * 260f));
        private static Vector2 FinaleNose(float lift) => new Vector2(270f, OriginalMath.Binary32(OriginalMath.Binary32(694f - lift) + -122.5f));

        // f54743.
        private void DrawFinaleTitle(PixelSurface s, float age)
        {
            if (age > 1.5f) return;
            float appear = OutBack(OriginalMath.Binary32(age / .3f), 2.4f), alpha = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(1.5f - age)) / .35f)), size = OriginalMath.Binary32(appear * 4.2f);
            if (size < .2f) return;
            s.Glow(270f, 300f, OriginalMath.Binary32(appear * 420f), OriginalMath.Binary32(appear * 150f), SourceColor(4554039943403405312UL, 1050253722u, OriginalMath.Binary32(alpha * .35f)), 0f);
            for (int i = 0; i < 7; i++)
                s.Rect(OriginalMath.Binary32(270f - OriginalMath.Binary32(appear * 150f)), OriginalMath.Binary32(OriginalMath.Binary32(appear * 30f) + OriginalMath.Binary32((float)(286 + i * 4))), OriginalMath.Binary32(appear * 300f), 3f, A(finaleRainbow[i], OriginalMath.Binary32(alpha * .75f)));
            Title(s, "决 战", 270f, 262f, size, SourceColor(4572774917166071808UL, 1061997773u, alpha),
                SourceColor(4369572502481089331UL, 1025758986u, alpha));
            int mode = finaleShown == null ? 0 : finaleShown.Mode;
            string title = FleetCatalog.FighterNames[feedback.Model] + " · " + finaleNames[mode] + "！";
            if (age > .35f)
            {
                Color c = Mix(uiGoldHi, FleetCatalog.FighterAccent[feedback.Model], .45f);
                c.a = OriginalMath.Binary32(OriginalMath.Binary32(c.a * alpha) * Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -.35f)) / .2f)));
                Label(s, title, 270f, 344f, 1.6f, c);
            }
        }
    }
}
