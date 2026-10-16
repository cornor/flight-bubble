using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55050. Every guard precedes combo/counter mutation and the hurt cue.
        public bool Breach()
        {
            if (unchecked((uint)((int)State - 1)) > 1u || (State == Phase.Ready && awaitDeployment)
                || OriginalMath.Max(0f, OriginalMath.Binary32(hurtUntil - Time)) > 0f || pushAge < pushDuration
                || TrainingHold || TrainingSafe || ActiveFinale != null) return false;
            Combo = 0;
            Breaches = unchecked(Breaches + 1);
            hurtUntil = OriginalMath.Binary32(Time + .85f);
            Cue?.Invoke("hurt");
            if (Pressure.enabled && !Revived)
            {
                State = Phase.ReviveOffer;
                Cue?.Invoke("down");
            }
            else FinishDefeat();
            return true;
        }

        // f55051. Pending pushes and shots are cleared before target reservations.
        private void FinishDefeat()
        {
            State = Phase.Lost;
            pendingPushes.Clear();
            Dispose();
            Array.Clear(reserved, 0, reserved.Length);
            Cue?.Invoke("lose");
        }

        // f55057. The source allows PurgeFront to win before the rescue push/cue.
        public bool Revive()
        {
            if (State != Phase.ReviveOffer || Revived) return false;
            BlockedTime = 0f;
            State = Phase.Playing;
            Revived = true;
            reviveSlots = 1;
            RescueMissiles = unchecked(RescueMissiles + 1);
            MaximumSlots = unchecked(MaximumSlots + 1);
            hurtUntil = OriginalMath.Binary32(OriginalMath.Binary32(Time + Pressure.reviveDuration) + 2f);
            LastPurge = PurgeFront(Pressure.revivePurgeRows > 0 ? Pressure.revivePurgeRows : 0);
            PushBack(Pressure.revivePush, RepelKind.Revive);
            Cue?.Invoke("revive");
            return true;
        }

        // f55058: take the requested number of live cells per column, scanning
        // upward from the bottom. Empty rows do not consume the per-column quota.
        public int PurgeFront(int rows)
        {
            if (rows <= 0) return 0;
            var killed = new bool[Alive.Length];
            float front = frontGridY < 0f ? -1f : OriginalMath.Binary32(frontGridY + Offset);
            int width = Level.columns, bottom = unchecked(sandRows - 1), count = 0;
            if (width <= 0 || bottom < 0) return 0;
            for (int column = 0; column < width; column++)
            {
                int found = 0;
                for (int row = bottom; row >= 0 && found < rows; row--)
                {
                    int cell = sand[unchecked(row * width + column)];
                    if (cell < 0 || !Alive[cell]) continue;
                    killed[cell] = true;
                    count = unchecked(count + 1);
                    found = unchecked(found + 1);
                }
            }
            if (count == 0) return 0;
            Remove(killed);
            RefreshFronts();
            FrontPurged?.Invoke(front, count);
            Cue?.Invoke("purge");
            if (Remaining == 0) { State = Phase.Won; Cue?.Invoke("win"); }
            return count;
        }
    }
}
