using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: f55041; the original wrapper always passes row zero.
        public bool Deploy(int column)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(55041);
#endif
            return DeployAt(column, 0);
        }

        // src: f55042. DeployAt does not test Kind or DeployAt time.
        public bool DeployAt(int column, int row)
        {
            if (column < 0 || !Active || row < 0 || Reserve.Length <= column
                || Reserve[column].Count <= row) return false;
            Fighter ship = Reserve[column][row];
            if (UnlockedSlots <= Fleet.Count)
            {
                NoticeTime = 2.4f; // WAT i32 1075419546
                Notice = "机位已满";
                Cue?.Invoke("deny");
                return false;
            }
            Reserve[column].RemoveAt(row);
            Fleet.Add(ship);
            ship.Arrival = .32f; // WAT i32 1050924810
            ship.Cooldown = .38f; // WAT i32 1052938076
            ship.Pulse = 1f;
            ship.LaunchedAt = Time;
            State = Phase.Playing;
            Launched?.Invoke(ship, unchecked(Fleet.Count - 1));
            Cue?.Invoke("launch"); // Re-read after Launched, which may change Cue.
            return true;
        }

        // src: f55138, both <Merge>b__0 and <Merge>b__1. No tier/kind filter.
        private sealed class MergeFilter
        {
            internal int Color;
            internal bool Match(Fighter ship) => ship.Color == Color && ship.Arrival <= 0f;
        }

        // src: f55045. Always merge the first three eligible fleet entries.
        private void Merge(int color)
        {
            var filter = new MergeFilter { Color = color };
            var eligible = Fleet.FindAll(filter.Match);
            while (eligible.Count >= 3)
            {
                Fighter[] group = eligible.GetRange(0, 3).ToArray();
                int index = Fleet.IndexOf(group[0]);
                Fighter dominant = group[0];
                foreach (Fighter ship in group)
                    if (ship.Tier > dominant.Tier) dominant = ship;
                float ratio = 1f;
                foreach (Fighter ship in group)
                {
                    float factor = ship.Tier > 2 ? .13f : ship.Tier == 2 ? .18f : ship.Tier == 1 ? .72f : .82f;
                    float interval = OriginalMath.Binary32(Level.shotInterval * factor);
                    ratio = OriginalMath.Min(ratio, OriginalMath.Max(0f, OriginalMath.Binary32(ship.Cooldown / interval)));
                }
                int initialAmmo = 0, ammo = 0, maxTier = 0;
                foreach (Fighter ship in group)
                {
                    initialAmmo = unchecked(initialAmmo + ship.InitialAmmo);
                    ammo = unchecked(ammo + ship.Ammo);
                    maxTier = Math.Max(maxTier, ship.Tier);
                    Fleet.Remove(ship);
                }
                int tier = Math.Min(3, unchecked(maxTier + 1));
                float lastFired = -10f;
                foreach (Fighter ship in group) lastFired = OriginalMath.Max(lastFired, ship.LastFired);
                var fused = new Fighter(color, ammo, dominant.Id)
                {
                    Pulse = 1.8f, // WAT i32 1072064102
                    Tier = tier,
                    InitialAmmo = initialAmmo,
                    Queue = dominant.Queue,
                    Aim = dominant.Aim,
                    ScanColumn = dominant.ScanColumn,
                    ScanDirection = dominant.ScanDirection,
                    BeamX = dominant.BeamX,
                    BeamY = dominant.BeamY,
                    BeamLife = dominant.BeamLife,
                    LastFired = lastFired
                };
                // WAT f32.mul twice: base*factor first, then ratio*interval.
                float newFactor = tier > 2 ? .13f : tier == 2 ? .18f : tier == 1 ? .72f : .82f;
                fused.Cooldown = OriginalMath.Binary32(ratio * (OriginalMath.Binary32(Level.shotInterval * newFactor)));
                Fleet.Insert(Math.Min(index, Fleet.Count), fused);
                Fused?.Invoke(fused, group, Math.Min(index, unchecked(Fleet.Count - 1)));
                Merges = unchecked(Merges + 1);
                if (tier == 3) MaxFusions = unchecked(MaxFusions + 1);
                PeakTier = Math.Max(PeakTier, tier);
                Cue?.Invoke(tier == 3 ? "maxpower" : "merge");
                lastFusionTier = tier;
                if (Pressure.enabled)
                {
                    FusionPushes = unchecked(FusionPushes + 1);
                    PushBack(OriginalMath.Binary32(Pressure.fusionPush * (OriginalMath.Binary32(Loadout.repel + 1f))), RepelKind.Fusion);
                    // PushBack callbacks can change the current loadout and time.
                    if (Loadout.fusionHold > 0f)
                    {
                        float until = OriginalMath.Binary32(Loadout.fusionHold + Time);
                        advanceHeldUntil = until < advanceHeldUntil ? advanceHeldUntil : until;
                    }
                    if (Loadout.fusionFire > 0f) fusionFireUntil = OriginalMath.Binary32(Time + Loadout.fusionFireTime);
                    LegendaryFusion();
                }
                eligible = Fleet.FindAll(filter.Match);
            }
        }
    }
}
