#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        public bool CanSkipWithReward => State == Phase.Intro || State == Phase.Ready || State == Phase.Playing;

        // Called only by the rewarded-video success callback. Preserve earned combat
        // counters and emit one normal win cue, so rewards/progression settle normally.
        public bool CompleteRewardedSkip()
        {
            if (!CanSkipWithReward) return false;
            State = Phase.Won;
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                Shot shot = Shots[i];
                ShotCancelled?.Invoke(shot);
                Shots.RemoveAt(i);
                ReleaseShot(shot);
            }
            PendingStrike = null;
            ActiveFinale = null;
            pendingPushes.Clear();
            finaleHits.Clear();
            Array.Clear(Alive, 0, Alive.Length);
            Array.Clear(reserved, 0, reserved.Length);
            for (int i = 0; i < sand.Length; i++) sand[i] = -1;
            for (int i = 0; i < position.Length; i++) position[i] = -1;
            foreach (var queue in incoming) queue.Clear();
            Remaining = 0;
            Firepower = 0f;
            sandDirty = true;
            RefreshFronts();
            Cue?.Invoke("win");
            return true;
        }
    }
}
#endif
