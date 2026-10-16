namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: f55046. These selects are not interchangeable with System.Math.
        private void PushBack(float distance, RepelKind kind)
        {
            if (TrainingHold && kind == RepelKind.Fusion && DeferTrainingRecoil)
            {
                trainingRecoil  =                 OriginalMath.Binary32(trainingRecoil+(distance));
                return;
            }
            float previousApplied = pushApplied;
            float floor = Pressure.retreatFloor;
            pushApplied = 0f;
            pushAge = 0f;
            stepRemaining = 0f;
            float proposed = OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(pushDistance - previousApplied), 0f) + distance);
            float prospective = OriginalMath.Binary32(Offset - distance);
            float target = kind == RepelKind.Revive ? (prospective > floor ? floor : prospective) : floor;
            float allowed = OriginalMath.WasmMax(OriginalMath.Binary32(Offset - target), 0f);
            float amount = allowed > proposed ? proposed : allowed;
            pushDistance = amount;
            pushDuration = kind == RepelKind.Revive ? Pressure.reviveDuration : Pressure.pushDuration;
            Repelled?.Invoke(kind, amount);
            Cue?.Invoke(kind == RepelKind.Revive ? "rescue-wave" : kind == RepelKind.Empty ? "sacrifice" : "shockwave");
        }

        // src: f55061. Clear the deferral before invoking PushBack and its events.
        public bool DemonstrateTrainingRecoil()
        {
            if (!TrainingHold || trainingRecoil <= 0f) return false;
            float amount = trainingRecoil;
            DeferTrainingRecoil = false;
            trainingRecoil = 0f;
            PushBack(amount, RepelKind.Fusion);
            return true;
        }

        // src: f55047. Legendary flags are read once on entry.
        private void LegendaryFusion()
        {
            int legendary = (int)Loadout.legendary;
            if ((legendary & 16) != 0 && PeakTier >= 3 && lastFusionTier >= 3)
            {
                float until = OriginalMath.Binary32(Time + 3f);
                advanceHeldUntil = advanceHeldUntil > until ? advanceHeldUntil : until;
            }
            if ((legendary & 2) == 0 || TrainingHold || Merges % 3 != 0) return;
            float time = Time;
            if (OriginalMath.Binary32(time - legendFusionAt) < 20f) return;
            legendFusionAt = time;
            float pitch = Level.pitchY;
            PushBack(pitch > 0f ? OriginalMath.Binary32(pitch + pitch) : 18f, RepelKind.Fusion);
            Cue?.Invoke("legend-pulse");
        }

        // src: f55076. Remove due entries even when inactive; callbacks run after
        // removal and can append entries, change State or replace loadout values.
        private void TickPendingPushes()
        {
            for (int index = 0; index < pendingPushes.Count; index++)
            {
                PendingPush pending = pendingPushes[index];
                if (pending.at > OriginalMath.Binary32(Time + .00001f)) continue;
                pendingPushes.RemoveAt(index);
                index--;
                if (!Active) continue;
                PushBack(pending.distance, pending.kind);
                if (pending.kind == RepelKind.Empty && ((int)Loadout.legendary & 4) != 0)
                {
                    float until = OriginalMath.Binary32(Time + 1.5f);
                    advanceHeldUntil = advanceHeldUntil > until ? advanceHeldUntil : until;
                }
            }
        }
    }
}
