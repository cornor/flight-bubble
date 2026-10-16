using System;
using System.Collections.Generic;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: Battle.DebitAmmunition f55060. Reverse reserve lanes and entries,
        // then reverse Fleet; zero ammo removes immediately, without sacrifice.
        private void DebitAmmunition(int color, int count)
        {
            unchecked
            {
                for (int lane = Reserve.Length - 1; lane >= 0 && count > 0; lane--)
                    for (int index = Reserve[lane].Count - 1; index >= 0 && count > 0; index--)
                    {
                        Fighter ship = Reserve[lane][index];
                        if (ship.Color != color) continue;
                        int debit = Math.Min(count, ship.Ammo);
                        ship.Ammo -= debit; count -= debit;
                        if (ship.Ammo == 0) Reserve[lane].RemoveAt(index);
                    }
                for (int index = Fleet.Count - 1; index >= 0 && count > 0; index--)
                {
                    Fighter ship = Fleet[index];
                    if (ship.Color != color) continue;
                    int debit = Math.Min(count, ship.Ammo);
                    ship.Ammo -= debit; count -= debit;
                    if (ship.Ammo == 0) Fleet.RemoveAt(index);
                }
            }
        }

        // src: Battle.Remove f55059. The supplied mask is counted first (without
        // an Alive guard), and remains live through cancellation callbacks.
        private int Remove(bool[] killed)
        {
            unchecked
            {
                int count = 0;
                var debits = new int[ColorCount];
                for (int cell = 0; cell < killed.Length; cell++)
                {
                    if (!killed[cell]) continue;
                    count++;
                    if (!reserved[cell]) debits[ColorAt(cell)]++;
                }
                for (int index = Shots.Count - 1; index >= 0; index--)
                {
                    Shot shot = Shots[index];
                    bool cancel;
                    if (shot.Rocket)
                    {
                        var remaining = new List<int>();
                        int[] targets = shot.SplashTargets;
                        for (int i = 0; i < targets.Length; i++)
                            if (!killed[targets[i]]) remaining.Add(targets[i]);
                        shot.SplashTargets = remaining.ToArray();
                        cancel = remaining.Count == 0;
                    }
                    else
                    {
                        if (shot.Extra != null)
                        {
                            var remaining = new List<int>();
                            int[] targets = shot.Extra;
                            for (int i = 0; i < targets.Length; i++)
                                if (!killed[targets[i]]) remaining.Add(targets[i]);
                            if (killed[shot.TargetIndex] && remaining.Count > 0)
                            {
                                shot.TargetIndex = remaining[0];
                                remaining.RemoveAt(0);
                            }
                            shot.Extra = remaining.Count > 0 ? remaining.ToArray() : null;
                        }
                        cancel = killed[shot.TargetIndex];
                    }
                    if (!cancel) continue;
                    ShotCancelled?.Invoke(shot);
                    Shots.RemoveAt(index);
                    ReleaseShot(shot);
                }
                for (int cell = 0; cell < killed.Length; cell++)
                {
                    if (!killed[cell]) continue;
                    Alive[cell] = false;
                    reserved[cell] = false;
                    int at = position[cell];
                    if (at >= 0) sand[at] = -1;
                }
                for (int color = 0; color < debits.Length; color++)
                    if (debits[color] > 0) DebitAmmunition(color, debits[color]);
                sandDirty = true;
                Remaining -= count;
                Score += count * 10;
                return count;
            }
        }

        // src: Battle.ReapOrphans f55077. Counts all fleet/reserve ammo and the
        // pending projectile target count, including stream cells not on board.
        private void ReapOrphans(float dt)
        {
            if (!exactBudget) return;
            orphanClock  =             OriginalMath.Binary32(orphanClock+(dt));
            if (orphanClock < 1f) return;
            orphanClock = 0f;
            unchecked
            {
                var ammunition = new int[ColorCount];
                foreach (Fighter ship in Fleet) ammunition[ship.Color] += ship.Ammo;
                foreach (List<Fighter> lane in Reserve)
                    foreach (Fighter ship in lane) ammunition[ship.Color] += ship.Ammo;
                foreach (Shot shot in Shots)
                    ammunition[shot.Color] += shot.Rocket ? shot.SplashTargets.Length : (shot.Extra == null ? 1 : shot.Extra.Length + 1);
                var killed = new bool[Alive.Length];
                var byColor = new int[ColorCount];
                bool any = false;
                for (int cell = 0; cell < Alive.Length; cell++)
                {
                    if (!Alive[cell] || reserved[cell]) continue;
                    int color = ColorAt(cell);
                    if (ammunition[color] != 0) continue;
                    any = true;
                    killed[cell] = true;
                    byColor[color]++;
                }
                if (!any) return;
                Remove(killed);
                RefreshFronts();
                for (int color = 0; color < ColorCount; color++)
                    if (byColor[color] > 0) OrphansCleared?.Invoke(color, byColor[color]);
                if (Remaining == 0)
                {
                    State = Phase.Won;
                    Cue?.Invoke("win");
                }
            }
        }
    }
}
