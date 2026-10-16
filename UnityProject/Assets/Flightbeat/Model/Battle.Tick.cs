using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55073. Line references are one-based in the COMPLETE original WAT.
        // Called game mechanisms remain explicit failures until translated.
        public void Tick(float dt)
        {
            // WAT 66..79: NaN dt is not rejected; only the six named phases branch.
            if (dt <= 0f) return;
            switch (State)
            {
                case Phase.Intro:
                    // WAT 81..102 / f70695: first argument is authored duration.
                    float previousIntro = IntroTime;
                    float duration = Level.introDuration;
                    IntroTime = OriginalMath.Min(duration, OriginalMath.Binary32(previousIntro + dt));
                    // WAT 103..120: each invocation reads the current event again.
                    if (previousIntro == 0f) Cue?.Invoke("assembly");
                    // WAT 122..164: reload duration after the assembly callback.
                    duration = Level.introDuration;
                    float arrivalAt = OriginalMath.Binary32(duration * .76f);
                    float warningAt = OriginalMath.Binary32(duration * .49f);
                    if (previousIntro < warningAt && IntroTime >= warningAt)
                        Cue?.Invoke("warning");
                    // WAT 166..190: previousIntro is unchanged by the gated select.
                    if (previousIntro < arrivalAt && IntroTime >= arrivalAt)
                        Cue?.Invoke("arrival");
                    // WAT 192..216: completion reads authored duration again.
                    if (IntroTime >= Level.introDuration)
                    {
                        State = Phase.Ready;
                        Cue?.Invoke("ready");
                    }
                    return;
                case Phase.Ready:
                    // WAT 220..224.
                    if (awaitDeployment) return;
                    break;
                case Phase.ReviveOffer:
                case Phase.Won:
                case Phase.Lost:
                    return;
            }
            // WAT 226..245: an active finale bypasses all other combat work.
            if (ActiveFinale != null)
            {
                Firepower = 0f;
                Time = OriginalMath.Binary32(Time + dt);
                NoticeTime = OriginalMath.Binary32(NoticeTime - dt);
                TickFinale(dt); // WAT 244 / f55074
                return;
            }
            // WAT 247..269: training hold moves position without advancing Time.
            if (!TrainingHold)
            {
                Time = OriginalMath.Binary32(Time + dt);
                Flow(dt); // WAT 261 / f55065
            }
            else MoveFormation(dt, false); // WAT 268 / f55062
            if (!TrainingHold)
            {
                TickSuperStrike(dt); // WAT 277 / f55075
                if (State == Phase.Won) return;
                TickPendingPushes(); // WAT 285 / f55076
                ReapOrphans(dt); // WAT 289 / f55077
                if (State == Phase.Won) return;
            }
            // WAT 299..345: preserve comparisons, grouping and the strict threshold.
            NoticeTime = OriginalMath.Binary32(NoticeTime - dt);
            if (OriginalMath.Binary32(Time - lastHit) > 1.4f) Combo = 0;
            float raw = RawFirepower; // WAT 316 / f54966
            float current = Firepower;
            if (!(raw > current))
                raw = OriginalMath.Binary32(current + OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(dt / -.3f)))) * (OriginalMath.Binary32(raw - current)))); // WAT 329 / f53831
            Firepower = raw < .01f ? 0f : raw;
            // WAT 347..424: reverse removal; Resolve can win before pool release.
            if (!TrainingHold)
                for (int index = Shots.Count - 1; index >= 0; index--)
                {
                    Shot shot = Shots[index]; // WAT 365 / List.get_Item f5549
                    shot.Age = OriginalMath.Binary32(shot.Age + dt);
                    if (!(shot.Duration > shot.Age)) // WAT 373..389: inline f54958
                    {
                        Resolve(shot); // WAT 393 / f55039
                        Shots.RemoveAt(index); // WAT 399 / f62921
                        ReleaseShot(shot); // WAT 411 / f54933
                        if (State == Phase.Won) return;
                    }
                }
            // WAT 430..666: the original enumerator/finally version check is kept.
            bool arrived = false;
            float doubleDt = OriginalMath.Binary32(dt + dt);
            foreach (Fighter fighter in Fleet) // WAT 439 f4256; 478 invoke f1888; 666 f53876
            {
                float arrival = fighter.Arrival;
                arrived = ((arrival > 0f) & (dt >= arrival)) | arrived;
                fighter.Arrival = OriginalMath.Max(0f, OriginalMath.Binary32(arrival - dt)); // WAT 539 / f70691
                fighter.Cooldown = OriginalMath.Binary32(fighter.Cooldown - dt);
                fighter.Pulse = OriginalMath.Max(0f, OriginalMath.Binary32(fighter.Pulse - doubleDt)); // WAT 571 / f70691
                fighter.BeamLife = OriginalMath.Max(0f, OriginalMath.Binary32(fighter.BeamLife - dt)); // WAT 597 / f70691
            }
            if (arrived)
                for (int color = 0; color < ColorCount; color++)
                    Merge(color); // WAT 685 / f55045

            // WAT 696..1615: descending Fleet order, with at most three boosted bursts.
            if (!TrainingHold)
                for (int index = Fleet.Count - 1; index >= 0; index--)
                {
                    Fighter fighter = Fleet[index]; // WAT 719 / f5549
                    if (fighter.Arrival > 0f || fighter.Cooldown > .00001f) continue;
                    float bonus = FireBonusFor(fighter); // WAT 741 / f54967
                    float divisor = OriginalMath.Binary32(bonus + 1f);
                    int burst = 0;
                    while (true)
                    {
                        int firedCount = 0;
                        for (int barrel = 0; ;)
                        {
                            if (fighter.Ammo <= 0) break;
                            // WAT 764 / f55071: its slot/MethodInfo parameters are
                            // dead before first read; the original passes a scratch
                            // pointer. Zero represents only this unused argument.
                            int target = Target(fighter, 0);
                            if (target < 0) break;
                            fighter.Ammo = unchecked(fighter.Ammo - 1);
                            reserved[target] = true;
                            Shot shot = AcquireShot(); // WAT 792 / f55350
                            shot.Pierce = pierceFound;
                            shot.PierceA = pierceA; shot.PierceB = pierceB; shot.PierceC = pierceC;
                            if (shot.Pierce > 0) Pierced = unchecked(Pierced + 1);
                            // WAT 821..919: original inline slot scan and barrel spread.
                            float fleetX = 270f;
                            if (MaximumSlots > 0)
                            {
                                int physical = TickSlotForIndex(index); // WAT 834..883
                                if (MaximumSlots >= 2)
                                    fleetX = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(456f / OriginalMath.Binary32((float)unchecked(MaximumSlots - 1)))) * OriginalMath.Binary32((float)physical)) + 42f);
                            }
                            float spread = 0f;
                            int tier = fighter.Tier;
                            if (tier <= 1)
                                spread = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)barrel) + OriginalMath.Binary32(OriginalMath.Binary32((float)((tier == 1 ? 1 : 0) << 1)) * -.5f))) * 7f);
                            // WAT 923..975; 1141981184 is exactly 581f, not DefenseLine.
                            shot.Y = 581f;
                            shot.X = OriginalMath.Binary32(fleetX + spread);
                            shot.TargetX = x[target];
                            shot.TargetY = OriginalMath.Binary32(y[target] + Offset);
                            shot.Color = fighter.Color;
                            int fighterId = fighter.Id;
                            shot.Barrel = barrel;
                            shot.Tier = tier;
                            shot.TargetIndex = target;
                            shot.FighterId = fighterId;
                            shot.Extra = WeaponExtras(fighter, target); // WAT 975 / f55078
                            Shots.Add(shot); // WAT 978..1018 / inline Add + f5552
                            // WAT 1021..1065: capture the exact values before Fired.
                            fighter.BeamX = shot.TargetX;
                            float targetY = shot.TargetY;
                            fighter.BeamLife = .18f; // bits 0x3E3851EC / store 1043878380
                            fighter.BeamY = targetY;
                            float shotY = shot.Y, shotX = shot.X;
                            fighter.Aim = OriginalMath.Binary32((float)((OriginalMath.Atan2((double)(OriginalMath.Binary32(fighter.BeamX - shotX)), (double)(OriginalMath.Binary32(shotY - targetY))) * 180.0) / Math.PI)); // WAT 1058 / f4705
                            Fired?.Invoke(shot); // WAT 1081 / Action<Shot>
                            firedCount = unchecked(firedCount + 1);
                            // WAT 1088..1237: re-read Tier and Ammo after the callback.
                            if (fighter.Tier >= 3 && fighter.Ammo > 0)
                            {
                                int[] targets = RocketTargets(fighter, shot.TargetX, shot.TargetY); // WAT 1105 / f55072
                                if (targets != null)
                                {
                                    Shot rocket = AcquireShot(); // WAT 1120 / f55350
                                    rocket.X = OriginalMath.Binary32(shot.X + 12f); rocket.Y = shot.Y;
                                    rocket.TargetX = shot.TargetX; rocket.TargetY = shot.TargetY;
                                    rocket.Color = fighter.Color;
                                    fighterId = fighter.Id;
                                    rocket.TargetIndex = target; rocket.FighterId = fighterId;
                                    tier = fighter.Tier;
                                    rocket.SplashTargets = targets;
                                    rocket.Rocket = true; rocket.Barrel = 1; rocket.Tier = tier;
                                    Shots.Add(rocket); // WAT 1170..1208 / inline Add + f5552
                                    Fired?.Invoke(rocket); // WAT 1226 / Action<Shot>
                                    firedCount = unchecked(firedCount + targets.Length);
                                }
                            }
                            barrel = unchecked(barrel + 1);
                            if (barrel >= (fighter.Tier == 1 ? 3 : 1)) break;
                        }
                        if (firedCount > 0)
                        {
                            // WAT 1251..1394: last-fire stamps precede the cooldown/cue.
                            lastFire = Time; fighter.LastFired = Time;
                            float cooldown = OriginalMath.Max(fighter.Cooldown, -dt); // WAT 1281 / f70691
                            int tier = fighter.Tier;
                            float interval = Level.shotInterval; // LevelData +0x5C
                            float factor = tier > 2 ? .13f : tier == 2 ? .18f : tier == 1 ? .72f : .82f;
                            fighter.Cooldown = OriginalMath.Binary32(cooldown + (bonus == 0f ? OriginalMath.Binary32(interval * factor) : OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(interval * factor), .02f) / divisor))); // WAT 1291..1344
                            Cue?.Invoke(tier > 2 ? "shot-rocket" : tier == 2 ? "shot-max" : tier == 1 ? "shot-twin" : "shot"); // WAT 1393
                        }
                        if (fighter.Ammo <= 0)
                        {
                            int slot = TickSlotForIndex(index); // WAT 1400..1459
                            Fleet.RemoveAt(index); // WAT 1466 / f62921
                            Cue?.Invoke("empty"); // WAT 1484 / Action<string>
                            if (Pressure.enabled)
                            {
                                EmptyPushes = unchecked(EmptyPushes + 1);
                                float delay = OriginalMath.WasmMax(Pressure.sacrificeDelay, 0f); // WAT 1506..1509
                                Sacrificed?.Invoke(fighter, slot, delay); // WAT 1521 / Action<Fighter,int,float>
                                pendingPushes.Add(new PendingPush // WAT 1525..1565 / f4317
                                {
                                    at = OriginalMath.Binary32(delay + Time),
                                    distance = OriginalMath.Binary32(Pressure.emptyPush * (OriginalMath.Binary32(Loadout.sacrifice + 1f))), // FleetLoadout +0x18
                                    kind = RepelKind.Empty,
                                });
                                if (delay <= 0f) TickPendingPushes(); // WAT 1573 / f55076
                            }
                        }
                        // WAT 1577..1603: no further burst without ammo/bonus/readiness.
                        if (fighter.Ammo <= 0 || !(bonus > 0f) || !(fighter.Cooldown <= 0f)) break;
                        bool again = burst < 2;
                        burst = unchecked(burst + 1);
                        if (!again) break;
                    }
                }
            // WAT 1617..1650: blocked time, then final physical defense-line test.
            BlockedTime = FullWithoutTarget ? OriginalMath.Binary32(BlockedTime + dt) : 0f; // WAT 1624 / f54977
            if (frontGridY < 0f) return;
            if (!(OriginalMath.Binary32(frontGridY + Offset) >= 562f)) return;
            Breach(); // WAT 1650 / f55050
        }

        // f55073 WAT 834..883 and 1400..1459: duplicated original inline scan.
        // Out-of-range extraBay positions count as slots, matching the branches.
        private int TickSlotForIndex(int index)
        {
            int count = 0;
            for (int physical = 0; physical < MaximumSlots; physical++)
                if (physical < BaseSlots || unchecked(extraBay.Length + BaseSlots) <= physical || extraBay[unchecked(physical - BaseSlots)])
                {
                    if (count == index) return physical;
                    count = unchecked(count + 1);
                }
            return 0;
        }
    }
}
