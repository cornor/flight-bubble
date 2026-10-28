using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: f55038. position is deliberately retained; RefreshFronts is a
        // later caller operation. The original duplicate-hit failure is explicit.
        private void ResolveTarget(Shot shot, int target)
        {
            if (!Alive[target]) throw new InvalidOperationException("Projectile target resolved twice");
            sandDirty = true;
            Alive[target] = false;
            sand[position[target]] = -1;
            reserved[target] = false;
            Remaining = unchecked(Remaining - 1);
            Score = unchecked(Score + shot.Tier * 10 + 10);
            float time = Time;
            int combo = OriginalMath.Binary32(time - lastHit) <= 2f ? unchecked(Combo + 1) : 1; // R1 2026-10-28 (user): combo window 1.4 s -> 2 s, like 沙画消消
            lastHit = time;
            Combo = combo;
            BestCombo = Math.Max(BestCombo, combo);
            Rushes = unchecked(Rushes + 1);
            combo = Combo;
            if (combo == 50 || combo == 100 || combo == 250 || combo == 500
                || (combo > 0 && unchecked((uint)combo) % 1000u == 0))
                Cue?.Invoke(unchecked((uint)combo) > 249u ? "combo-high" : "combo");
            // Cue can mutate the shot, coordinates, offset and Hit subscription.
            Hit?.Invoke(shot.Color, x[target], OriginalMath.Binary32(y[target] + Offset));
        }

        // src: f55040, also inlined in the normal-projectile path of f55039.
        // Capture the Extra array once; callback changes to its contents are live,
        // while replacing shot.Extra does not replace this iteration's array.
        private void ResolveExtras(Shot shot)
        {
            int[] targets = shot.Extra;
            if (targets == null) return;
            for (int index = 0; index < targets.Length; index++)
            {
                int target = targets[index];
                if (target < 0 || target >= Alive.Length || !Alive[target]) continue;
                ResolveTarget(shot, target);
                ExtraKills = unchecked(ExtraKills + 1);
            }
        }

        // src: f55039. Rocket targets are strict, unlike ordinary Extra entries.
        private void Resolve(Shot shot)
        {
            if (shot.Rocket)
            {
                int[] targets = shot.SplashTargets;
                if (targets == null || targets.Length == 0)
                    throw new InvalidOperationException("Rocket has no bounded targets");
                for (int index = 0; index < targets.Length; index++) ResolveTarget(shot, targets[index]);
            }
            else
            {
                int target = shot.TargetIndex;
                shot.TargetX = x[target];
                shot.TargetY = OriginalMath.Binary32(y[target] + Offset);
                ResolveTarget(shot, target);
                ResolveExtras(shot);
            }
            RefreshFronts();
            Impacted?.Invoke(shot);
            if (Remaining == 0)
            {
                State = Phase.Won;
                Cue?.Invoke("win");
            }
        }
    }
}
