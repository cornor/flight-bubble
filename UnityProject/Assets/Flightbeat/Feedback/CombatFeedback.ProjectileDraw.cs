using System;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53891/f53892. Raw model checks deliberately differ from Model's clamp.
        private float ShotWidth => Look == null ? 1f : Look.model == 1 ? .7f : Look.model == 2 ? 1.35f : 1f;
        private float ShotTail => Look == null ? 1f : Look.model == 1 ? 1.6f : Look.model == 2 ? .85f : 1f;

        // f53879. Helpers preserve the original fleet/projectile/effect order.
        public void DrawTargetsAndShots(PixelSurface s)
        {
            foreach (Fighter ship in battle.Fleet)
            {
                if (ship.Tier < 2 || ship.BeamLife <= 0f || !poses.TryGetValue(ship.Id, out Pose pose)) continue;
                DrawContinuousBeam(s, ship, pose);
            }
            foreach (Flight f in flights)
            {
                Shot shot = f.shot;
                if (shot.Tier >= 2 && !shot.Rocket) continue;
                f.to = shot.Rocket ? ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY)
                    : ScreenLayout.EnemyPoint(battle.EnemyX(f.target), battle.EnemyY(f.target));
                float t = Unit(OriginalMath.Binary32(shot.Age / shot.Duration));
                if (shot.Rocket) { DrawRocketFlight(s, f, t); continue; }
                float deltaX = OriginalMath.Binary32(f.from.x - f.to.x), deltaY = OriginalMath.Binary32(f.from.y - f.to.y);
                float length = OriginalMath.WasmMax(OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(deltaX * deltaX) + OriginalMath.Binary32(deltaY * deltaY)))), 1f);
                float tailScale = ShotTail, widthScale = ShotWidth;
                float amount = OriginalMath.WasmMax(t, 0f);
                deltaX = OriginalMath.Binary32(f.to.x - f.from.x);
                deltaY = OriginalMath.Binary32(f.to.y - f.from.y);
                Vector2 tip = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(amount * deltaX) + f.from.x), OriginalMath.Binary32(OriginalMath.Binary32(amount * deltaY) + f.from.y));
                if (shot.Pierce > 0) DrawPierceFlight(s, f, tip, t, length);
                float tail = Unit(OriginalMath.WasmMax(OriginalMath.Binary32(t + OriginalMath.Binary32(OriginalMath.Binary32(tailScale * -26f) / length)), 0f));
                float wake = Unit(OriginalMath.WasmMax(OriginalMath.Binary32(t + OriginalMath.Binary32(OriginalMath.Binary32(tailScale * -56f) / length)), 0f));
                DrawRound(s, f, tip, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(deltaX * tail) + f.from.x), OriginalMath.Binary32(OriginalMath.Binary32(deltaY * tail) + f.from.y)),
                    new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(deltaX * wake) + f.from.x), OriginalMath.Binary32(OriginalMath.Binary32(deltaY * wake) + f.from.y)), widthScale);
            }
            DrawPierceSparks(s);
            DrawRocketBlasts(s);
            DrawWeaponFx(s);
            DrawUpgradeFx(s);
            DrawMarks(s);
            DrawSacrifices(s);
        }

        // f53880. All five model branches, including the default upgraded-nose glow.
        private void DrawRound(PixelSurface s, Flight f, Vector2 tip, Vector2 tail, Vector2 wake, float widthScale)
        {
            Color lane = palette[f.color];
            int form = Weapon.Form;
            int nose = Look == null ? 1 : Look.Part(0);
            nose = nose < 5 ? nose : 5;
            nose = nose > 1 ? nose : 1;
            float strength = Look == null ? 1f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(nose - 1)) * .28f) + 1f);
            if (Model != 0) { DrawModelRound(s, f, tip, tail, wake, lane, form, strength); return; }
            float size = OriginalMath.Binary32(strength * (f.shot.Tier == 1 ? OriginalMath.Binary32(widthScale * 1.7f) : widthScale));
            s.GlowLine(wake, tip, OriginalMath.Binary32(size * 22f), Alpha(lane, .55f));
            s.GlowLine(tail, tip, OriginalMath.Binary32(OriginalMath.Binary32(strength * (f.shot.Tier == 1 ? 15f : 10f)) * widthScale), Alpha(lane, .9f));
            s.Glow(tip.x, tip.y, OriginalMath.Binary32(size * 24f), OriginalMath.Binary32(size * 28f), Alpha(lane, .9f), 0f);
            s.Segment(tail, tip, OriginalMath.Binary32(size * 2.2f), Mix(lane, Color.white, .6f));
            s.Glow(tip.x, tip.y, 8f, 13f, Color.white, 0f);
            if (Look != null && Look.Part(0) >= 5)
                s.Glow(tip.x, tip.y, 12f, 12f, SourceColor(4564848585086074880UL, 4561245704513440973UL), 0f);
        }

    }
}
