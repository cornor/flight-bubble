using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55095. The model uses the enemy display expansion, independent of
        // the Unity canvas. These f32 constants come directly from the WAT.
        private float FinaleX(int cell) => OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(x[cell] + -270f)) * 1.5649770498275757f) + 270f);
        private float FinaleY(int cell) => OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(y[cell] + Offset) + -562f)) * 1.31f) + 562f);

        public bool LaunchFinale(int mode)
        {
            if (!FinaleReady) return false;
            var finale = new Finale { Mode = Math.Max(0, Math.Min(mode, 5)) };
            float maxX = float.MinValue, minX = float.MaxValue, maxY = float.MinValue, minY = float.MaxValue;
            int count = 0;
            for (int cell = 0; cell < Alive.Length; cell++)
            {
                if (!Alive[cell] || cell >= position.Length || position[cell] < 0) continue;
                float px = FinaleX(cell), py = FinaleY(cell);
                maxX = px < maxX ? maxX : px; minX = px > minX ? minX : px;
                maxY = py < maxY ? maxY : py; minY = py > minY ? minY : py;
                count++;
            }
            if (count > 0)
            {
                finale.CY = OriginalMath.Binary32((OriginalMath.Binary32(maxY + minY)) * .5f); finale.CX = OriginalMath.Binary32((OriginalMath.Binary32(maxX + minX)) * .5f);
                float ry = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(maxY - minY)) * .5f) * 1.12f) + 10f), 50f);
                float rx = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(maxX - minX)) * .5f) * 1.12f) + 10f), 60f);
                finale.RY = ry; finale.RX = rx;
                finale.HoleReach = OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(ry * ry) + OriginalMath.Binary32(rx * rx)))) + 10f);
                float near = float.MaxValue, far = 0f;
                for (int cell = 0; cell < Alive.Length; cell++)
                {
                    if (!Alive[cell] || cell >= position.Length || position[cell] < 0) continue;
                    float dx = OriginalMath.Binary32(FinaleX(cell) + -270f), dy = OriginalMath.Binary32(FinaleY(cell) + -562f);
                    float distance = OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy));
                    near = distance > near ? near : distance; far = distance < far ? far : distance;
                }
                finale.NovaReach = OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Sqrt(far)) + 12f);
                finale.NovaNear = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Sqrt(near)) + -14f), 0f);
                if (finale.Mode == 4) { finale.RY = ry > 130f ? ry : 130f; finale.RX = rx > 255f ? rx : 255f; }
            }
            finale.Ships.AddRange(Fleet);
            Fleet.Clear();
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                Shot shot = Shots[i];
                ShotCancelled?.Invoke(shot);
                Shots.RemoveAt(i);
                ReleaseShot(shot);
            }
            pendingPushes.Clear();
            FinaleUsed = true;
            ActiveFinale = finale;
            stepRemaining = 0f;
            FinaleStarted?.Invoke(finale);
            Cue?.Invoke("finale-charge");
            return true;
        }

        // f55074. Beam notification follows removal/front refresh, even when
        // its hit list is empty. The sweep-end pass includes off-board stream cells.
        private void TickFinale(float dt)
        {
            Finale finale = ActiveFinale;
            float previous = finale.Age;
            float age = OriginalMath.WasmMin(OriginalMath.Binary32(previous + dt), 3.8500001430511475f);
            finale.Age = age;
            if (previous < 1.65f && age >= 1.65f && Cue != null)
            {
                Cue.Invoke("finale-beam");
                age = finale.Age;
            }
            if (previous < 3.15f && age >= 1.65f)
            {
                float t = OriginalMath.Binary32((OriginalMath.Binary32(age + -1.65f)) / 1.5f);
                t = t < 0f ? -0.13636364042758942f : OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.WasmMin(t, 1f) + -.12f)) / .88f);
                t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
                finale.Spread = OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * 90f))) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(t + t)))));
                var killed = new bool[Alive.Length];
                finaleHits.Clear();
                for (int cell = 0; cell < Alive.Length; cell++)
                {
                    if (!Alive[cell]) continue;
                    if (!(age >= 3.15f) && (cell >= position.Length || position[cell] < 0 || !finale.Reached(FinaleX(cell), FinaleY(cell)))) continue;
                    killed[cell] = true;
                    finaleHits.Add(cell);
                }
                if (finaleHits.Count > 0)
                {
                    int removed = Remove(killed);
                    finale.Kills = unchecked(removed + finale.Kills);
                    FinaleKills = unchecked(removed + FinaleKills);
                    RefreshFronts();
                }
                FinaleBeam?.Invoke(finale, finaleHits);
                if (age >= 3.15f) Cue?.Invoke("super-impact");
            }
            if (finale.Age >= 3.8500001430511475f)
            {
                ActiveFinale = null;
                if (Remaining == 0) { State = Phase.Won; Cue?.Invoke("win"); }
            }
        }
    }
}
