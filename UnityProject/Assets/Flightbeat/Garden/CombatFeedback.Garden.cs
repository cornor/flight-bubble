#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // Read existing feedback records; never advance clocks or resolve a model shot here.
        public void DrawGardenFeedback(PixelSurface s, Func<int, Color> color) {
            foreach (var ship in battle.Fleet) {
                if (ship.Tier < 2 || ship.BeamLife <= 0) continue;
                int slot = battle.Fleet.IndexOf(ship);
                var from = Position(ship.Id, slot);
                var to = ScreenLayout.EnemyPoint(ship.BeamX, ship.BeamY);
                GardenFx.BubbleStream(s, from, to, ship.Tier >= 3 ? 12 : 10, color(ship.Color), time);
            }
            foreach (var f in flights) {
                var shot = f.shot;
                // As in the original (ProjectileDraw f53879): tier-2+ shots are carried by the continuous stream, so
                // their flights are not drawn (they showed up as one big ball sliding along the MAX stream).
                if (shot.Tier >= 2 && !shot.Rocket) continue;
                var to = ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY);
                if (!shot.Rocket) to = ScreenLayout.EnemyPoint(battle.EnemyX(f.target), battle.EnemyY(f.target));
                var p = Vector2.Lerp(f.from, to, Mathf.Clamp01(shot.Age / shot.Duration));
                // v3 (UI spec v3 4.4, user decision 2026-10-15 20:28): every projectile is a SOLID glossy ball in the
                // shooter's color (the bead texture), 12 px, with two smaller solid after-images. No see-through bubble.
                Color tint = color(f.color);
                for (int g = 2; g >= 1; g--) {
                    var q = Vector2.Lerp(f.from, to, Mathf.Clamp01((shot.Age - g * .025f) / shot.Duration));
                    GardenSkin.Shot(s, tint, q, 12f - g * 2.5f, 1f);
                }
                GardenSkin.Shot(s, tint, p, 12f, 1f);
            }
            // User decision 2026-10-14: ordinary hits have no visual FX, including the old rings.
            // f53855 fusion record; f53871 starts the burst at age .22.
            foreach (var f in fusions)
                GardenFx.Burst(s, "merge-burst", f.at, 96, f.age - .22f, .5f);
            foreach (var t in transitions) {
                var p = Vector2.Lerp(t.from, t.to, Mathf.Clamp01(t.age / t.duration));
                GardenSkin.Creature(s, color(t.color), p.x, p.y + 8, 46, t.tier, "", 0f, 10f, false, false);
            }
            foreach (var k in kamikazes) {
                // f53886/f53878: approach during delay, then impact at exploded; consume the source clock.
                if (!k.exploded) {
                    float t = Mathf.Clamp01((k.age / k.delay - .25f) / .75f);
                    var p = Vector2.Lerp(k.from, new Vector2(k.x, FrontScreenY + 6), t * t);
                    if (t > 0) GardenFx.Draw(s, "dash-trail", p + new Vector2(0, 45), 36, 96, color(k.color));
                    GardenSkin.Plant(s, color(k.color), p, k.tier, "0");
                } else GardenFx.Burst(s, "impact-dust", new Vector2(k.x, FrontScreenY + 6), 80, k.age - k.delay, .5f);
            }
            foreach (var wave in waves) {
                float t = Mathf.Clamp01(wave.age / wave.duration);
                bool revive = wave.kind == RepelKind.Revive;
                float y = (revive ? 574 : FrontScreenY) - t * (revive ? 260 : 50);
                float h = revive ? 180 : 60;
                GardenFx.Draw(s, revive ? "revive-wave" : "repel-gust", new Vector2(270, y - h * .5f), 540, h,
                    new Color(1, 1, 1, (revive ? .85f : .65f) * (1 - t)));
            }
            // f53914: PendingStrike exists during its 1s countdown; SuperDetonate starts superAge.
            // Only the cosmetic can moves; impact timing, target and radius are the original model's.
            var pending = battle.PendingStrike;
            if (pending != null) {
                Vector2 at = ScreenLayout.EnemyPoint(pending.X, pending.Y);
                float t = Mathf.Clamp01(pending.Age);
                GardenFx.Aim(s, at, pending.RadiusX * ScreenLayout.EnemyScaleX, .6f);
                GardenFx.Draw(s, "water-can", at + new Vector2(-32, -100 + t * 48), 96, 96,
                    new Color(1, 1, 1, Mathf.Clamp01(t * 5)), 0, -15 * (1 - t));
            }
            if (lastStrike != null)
                GardenFx.Burst(s, "water-splash", ScreenLayout.EnemyPoint(lastStrike.X, lastStrike.Y), 128, superAge, .7f);
        }
    }
}
#endif
