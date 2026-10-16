using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: f53871. Keep age updates, removals and particle/event calls in source order.
        public void Tick(float dt)
        {
            TickCombo(dt);
            TickMarks(dt);
            TickWeaponFx(dt);
            TickUpgradeFx(dt);
            TickPressure(dt);
            superAge  =             OriginalMath.Binary32(superAge+(dt));
            time  =             OriginalMath.Binary32(time+(dt));
            hurtAge  =             OriginalMath.Binary32(hurtAge+(dt));
            maxAge  =             OriginalMath.Binary32(maxAge+(dt));
            sweepAge  =             OriginalMath.Binary32(sweepAge+(dt));
            rocketAge  =             OriginalMath.Binary32(rocketAge+(dt));
            stepAge  =             OriginalMath.Binary32(stepAge+(dt));
            flashAge  =             OriginalMath.Binary32(flashAge+(dt));
            shakeAge  =             OriginalMath.Binary32(shakeAge+(dt));
            for (int i = blasts.Count - 1; i >= 0; i--)
            {
                blasts[i].age = OriginalMath.Binary32(blasts[i].age + dt);
                if (blasts[i].age > .4f) blasts.RemoveAt(i);
            }
            for (int i = beams.Count - 1; i >= 0; i--)
            {
                beams[i].age = OriginalMath.Binary32(beams[i].age + dt);
                if (beams[i].age > .7f) beams.RemoveAt(i);
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                sparks[i].age = OriginalMath.Binary32(sparks[i].age + dt);
                if (sparks[i].age > OriginalMath.Binary32(sparks[i].delay + .26f)) sparks.RemoveAt(i);
            }

            if (battle.LaneCount > 0)
            {
                float reserveEase = ClampTick(OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(dt * -13f))));
                for (int lane = 0; lane < battle.LaneCount; lane++)
                {
                    for (int row = 0; row < battle.Reserve[lane].Count && row < 3; row++)
                    {
                        Fighter ship = battle.Reserve[lane][row];
                        float x = battle.QueuePositionX(lane), y = Battle.QueueY(row);
                        if (!reservePoses.TryGetValue(ship.Id, out Vector2 previous))
                            previous = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(Vector2.up.x * 54f)), OriginalMath.Binary32(y + OriginalMath.Binary32(Vector2.up.y * 54f)));
                        reservePoses[ship.Id] = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(reserveEase * (OriginalMath.Binary32(x - previous.x))) + previous.x),
                            OriginalMath.Binary32(OriginalMath.Binary32(reserveEase * (OriginalMath.Binary32(y - previous.y))) + previous.y));
                    }
                }
            }
            retired.Clear();
            foreach (var pair in reservePoses)
            {
                bool found = false;
                for (int lane = 0; lane < battle.LaneCount && !found; lane++)
                    for (int row = 0; row < battle.Reserve[lane].Count && row < 3; row++)
                        found = battle.Reserve[lane][row].Id == pair.Key | found;
                if (!found) retired.Add(pair.Key);
            }
            foreach (int id in retired) reservePoses.Remove(id);

            for (int i = transitions.Count - 1; i >= 0; i--)
            {
                transitions[i].age = OriginalMath.Binary32(transitions[i].age + dt);
                if (transitions[i].age >= transitions[i].duration) transitions.RemoveAt(i);
            }
            for (int i = fusions.Count - 1; i >= 0; i--)
            {
                Fusion fusion = fusions[i];
                fusion.age = OriginalMath.Binary32(fusion.age + dt);
                if (!fusion.burst && fusion.age >= .22f)
                {
                    fusion.burst = true;
                    particles.Play(fusion.tier == 3 ? "PixelMaxFusion" : "PixelFusion", fusion.at, palette[fusion.color]);
                    particles.Play("PixelTierUp", fusion.at, fusion.tier == 3 ? new Color(1f, .8f, .3f, 1f) : palette[fusion.color]);
                    beams.Add(new Beam { x = fusion.at.x, color = fusion.color, tier = fusion.tier });
                    AddHitMark(fusion.at, fusion.color, true);
                    if (fusion.tier == 3)
                    {
                        flashColor.b = .45f; flashColor.a = .5f;
                        flashColor.r = 1f; flashColor.g = .85f;
                        flashAge = 0f; flashLength = .3f;
                        Kick(4f);
                    }
                    else Kick(1.5f);
                }
                if (fusion.age > 1.1f) fusions.RemoveAt(i);
            }

            retired.Clear();
            foreach (var pair in poses)
            {
                bool found = false;
                foreach (Fighter ship in battle.Fleet)
                    if (pair.Key == ship.Id) { found = true; break; }
                if (!found) retired.Add(pair.Key);
            }
            foreach (int id in retired) poses.Remove(id);
            if (battle.Fleet.Count > 0)
            {
                float angleEase = ClampTick(OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(dt * -24f))));
                float beamEase = ClampTick(OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(dt * -42f))));
                float positionEase = ClampTick(OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(dt * -20f))));
                float recoilStep = OriginalMath.Binary32(dt * 9f);
                for (int slot = 0; slot < battle.Fleet.Count; slot++)
                {
                    Fighter ship = battle.Fleet[slot];
                    if (!poses.TryGetValue(ship.Id, out Pose pose)) continue;
                    float x = battle.FleetPositionX(battle.VisualSlot(slot));
                    float age = OriginalMath.Binary32(time - pose.born);
                    if (age < .32f)
                    {
                        float progress = ClampTick(OriginalMath.Binary32(age / .32f));
                        float eased = ClampTick(OriginalMath.Binary32(1f - OriginalMath.Pow(OriginalMath.Binary32(1f - progress), 3f)));
                        Vector2 from = pose.origin;
                        float y = OriginalMath.Binary32(OriginalMath.Binary32(eased * (OriginalMath.Binary32(628f - from.y))) + from.y);
                        x = OriginalMath.Binary32(OriginalMath.Binary32(eased * (OriginalMath.Binary32(x - from.x))) + from.x);
                        float arc = OriginalMath.Sin(OriginalMath.Binary32(progress * 3.1415927410125732f));
                        pose.position = new Vector2(OriginalMath.Binary32(x - OriginalMath.Binary32(OriginalMath.Binary32(arc * Vector2.up.x) * 18f)), OriginalMath.Binary32(y - OriginalMath.Binary32(OriginalMath.Binary32(arc * Vector2.up.y) * 18f)));
                    }
                    else
                    {
                        Vector2 previous = pose.position;
                        pose.position = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(positionEase * (OriginalMath.Binary32(x - previous.x))) + previous.x),
                            OriginalMath.Binary32(OriginalMath.Binary32(positionEase * (OriginalMath.Binary32(628f - previous.y))) + previous.y));
                    }
                    // WAT has 0x1.b7cdfcp-34 (1e-10f); the readable decompiler rounds it to zero.
                    float dx = OriginalMath.Binary32(pose.beam.x - Vector2.zero.x), dy = OriginalMath.Binary32(pose.beam.y - Vector2.zero.y);
                    if (OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) < 1e-10f) pose.beam = ScreenLayout.EnemyPoint(ship.BeamX, ship.BeamY);
                    Vector2 oldBeam = pose.beam, enemy = ScreenLayout.EnemyPoint(ship.BeamX, ship.BeamY);
                    pose.beam = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(beamEase * (OriginalMath.Binary32(enemy.x - oldBeam.x))) + oldBeam.x),
                        OriginalMath.Binary32(OriginalMath.Binary32(beamEase * (OriginalMath.Binary32(enemy.y - oldBeam.y))) + oldBeam.y));
                    float heading = 0f;
                    if (ship.LastFired > -9f && OriginalMath.Binary32(battle.Time - ship.LastFired) < 1.2f)
                    {
                        Vector2 from = ScreenLayout.Point(pose.position), to = ScreenLayout.Point(pose.beam);
                        float distanceY = OriginalMath.Binary32(from.y - to.y);
                        if (distanceY > 4f)
                        {
                            float angle = OriginalMath.Binary32(OriginalMath.Atan2Single(OriginalMath.Binary32(to.x - from.x), distanceY) * 57.295780181884766f);
                            heading = angle < -78f ? -78f : OriginalMath.WasmMin(angle, 78f);
                        }
                    }
                    pose.recoil = OriginalMath.WasmMax(OriginalMath.Binary32(pose.recoil - recoilStep), 0f);
                    pose.angle = OriginalMath.Binary32(pose.angle + OriginalMath.Binary32(angleEase * (OriginalMath.Binary32(heading - pose.angle))));
                }
            }
            TickSacrifice(dt);
            if (ending && flights.Count == 0)
            {
                if (endAge == 0f && battle.State == Phase.Won)
                {
                    particles.Play("PixelVictory", new Vector2(92f, 428f), palette[2]);
                    particles.Play("PixelVictory", new Vector2(448f, 428f), palette[1]);
                    particles.Play("PixelVictory", new Vector2(270f, 300f), new Color(1f, .8f, .3f, 1f));
                }
                endAge  =                 OriginalMath.Binary32(endAge+(dt));
            }
        }
        private static float ClampTick(float value) => value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
    }
}
