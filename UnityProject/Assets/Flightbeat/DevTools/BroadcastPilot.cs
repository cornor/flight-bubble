using System;

namespace Flightbeat.Model
{
    public static class BroadcastMode
    {
        public static bool Enabled => true; // f898, this named alias only
        // f55152, previously inlined by the gameplay broadcast controller.
        // Widen only the remainder so int.MinValue % -1 returns zero as i32.rem_s
        // does; C#'s int remainder would throw even in unchecked code.
        public static int NextLevel(int current, int count) => (int)((long)unchecked(current + 1) % count);
    }

    public sealed class BroadcastPilot : IBroadcastPilot
    {
        private readonly Random random;
        private readonly int[] visible, held, count;
        private float decisionAt, elapsed, unchanged;
        private int remaining;

        // f55153. Even a human-controlled sortie constructs the pilot in Reset.
        public BroadcastPilot(int colors, int seed)
        {
            remaining = -1;
            random = new Random(seed);
            visible = new int[colors];
            held = new int[colors];
            count = new int[colors];
            decisionAt = .7f;
        }

        // f55154. Decision cadence consumes RNG before phase guards. Probes retain
        // ColumnTarget side effects through ExposedAt, including reserved targets.
        public BroadcastDecision Tick(Battle battle, float dt)
        {
            elapsed = OriginalMath.Binary32(elapsed + dt);
            unchanged = battle.Remaining == remaining ? OriginalMath.Binary32(unchanged + dt) : 0f;
            remaining = battle.Remaining;
            if (decisionAt > elapsed) return new BroadcastDecision(BroadcastAction.Wait);
            decisionAt = OriginalMath.Binary32((OriginalMath.Binary32(elapsed + .3f)) + OriginalMath.Binary32(OriginalMath.Binary32((float)random.NextDouble()) * .38f));
            if (battle.State == Phase.ReviveOffer) return new BroadcastDecision(BroadcastAction.Rescue);
            if (!battle.Active || battle.ActiveFinale != null) return new BroadcastDecision(BroadcastAction.Wait);
            if (battle.FinaleReady) return new BroadcastDecision(BroadcastAction.Finale);
            Array.Clear(visible, 0, visible.Length); Array.Clear(held, 0, held.Length); Array.Clear(count, 0, count.Length);
            for (int color = 0; color < visible.Length; color++)
                for (int column = 0; column < battle.Level.columns; column++)
                    if (battle.ExposedAt(column, color) >= 0) visible[color] = unchecked(visible[color] + 1);
            bool attacking = false;
            foreach (Fighter ship in battle.Fleet)
            {
                held[ship.Color] = unchecked(ship.Ammo + held[ship.Color]);
                count[ship.Color] = unchecked(count[ship.Color] + 1);
                if (visible[ship.Color] > 0 || ship.Arrival > 0f) attacking = true;
            }
            if (battle.UnlockedSlots > battle.Fleet.Count)
            {
                float best = -float.MaxValue; int lane = -1;
                for (int i = 0; i < battle.Reserve.Length; i++)
                {
                    if (battle.Reserve[i].Count == 0) continue;
                    int color = battle.Reserve[i][0].Color, targets = visible[color]; bool merge = count[color] % 3 == 2;
                    if (targets == 0 && !merge) continue;
                    if (attacking && !merge && unchecked(battle.UnlockedSlots - battle.Fleet.Count) <= 1 && held[color] > 0) continue;
                    float score = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(targets * 20 + (merge ? 10000 : 0))) + OriginalMath.Binary32(OriginalMath.Binary32((float)held[color]) * -.1f)))
                        + OriginalMath.Binary32(OriginalMath.Binary32((float)random.NextDouble()) * 15f));
                    if (!(score > best)) continue;
                    best = score; lane = i;
                }
                if (lane >= 0) return new BroadcastDecision(BroadcastAction.Deploy, lane);
            }
            if (battle.State == Phase.Ready)
            {
                int lane = battle.SuggestedQueue();
                return new BroadcastDecision(lane >= 0 ? BroadcastAction.Deploy : BroadcastAction.Wait, lane);
            }
            if (battle.PendingStrike != null) return new BroadcastDecision(BroadcastAction.Wait);
            if (!(battle.BlockedTime >= 1.1f) && unchanged < 8f && battle.Danger < .82f) return new BroadcastDecision(BroadcastAction.Wait);
            if (unchanged < 1.8f && battle.Danger < .82f) return new BroadcastDecision(BroadcastAction.Wait);
            if (battle.UnlockedSlots <= battle.Fleet.Count && battle.BaseSlots < battle.MaximumSlots)
                for (int bay = battle.BaseSlots; bay < battle.MaximumSlots; bay++)
                    if (!battle.BayOpen(bay)) return new BroadcastDecision(BroadcastAction.Bay, bay);
            decisionAt = OriginalMath.Binary32(elapsed + 1.1f);
            return new BroadcastDecision(BroadcastAction.Bomb);
        }
    }
}
