using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: Battle.StrikeTargets f55124. Reserved cells are included; incoming
        // stream cells with negative positions are excluded. Boundary is inclusive.
        public int StrikeTargets(float cx, float cy, float rx, float ry)
        {
            if (rx <= 0f || ry <= 0f) return 0;
            int count = 0;
            for (int cell = 0; cell < Alive.Length; cell++)
            {
                if (!Alive[cell] || cell >= position.Length || position[cell] < 0) continue;
                float dx = OriginalMath.Binary32((OriginalMath.Binary32(x[cell] - cx)) / rx), dy = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(y[cell] + Offset) - cy)) / ry);
                if (OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= 1f) count = unchecked(count + 1);
            }
            return count;
        }

        // src: Battle.LaunchSuperStrike f55125, including unsigned phase range.
        public bool LaunchSuperStrike(float cx, float cy, float rx, float ry, bool missile)
        {
            if (unchecked((uint)((int)State - 1)) > 1u || TrainingHold || ActiveFinale != null || PendingStrike != null) return false;
            if (missile && RescueMissiles <= 0) return false;
            if (float.IsNaN(cx) || float.IsInfinity(cx) || float.IsNaN(cy) || float.IsInfinity(cy)
                || float.IsNaN(rx) || float.IsInfinity(rx) || ry > 500f || rx > 250f || ry <= 0f || rx <= 0f
                || float.IsNaN(ry) || float.IsInfinity(ry)) return false;
            if (StrikeTargets(cx, cy, rx, ry) == 0) return false;
            if (missile) RescueMissiles = unchecked(RescueMissiles - 1);
            var strike = new SuperStrike { X = cx, Y = cy, RadiusX = rx, RadiusY = ry, Missile = missile };
            PendingStrike = strike;
            StrikeLaunched?.Invoke(strike);
            Cue?.Invoke("super-lock");
            return true;
        }

        // src: Battle.TickSuperStrike f55075. The local strike survives clearing
        // PendingStrike, and events may mutate battle state before later reads.
        private void TickSuperStrike(float dt)
        {
            SuperStrike strike = PendingStrike;
            if (strike == null) return;
            float previous = strike.Age;
            float age = OriginalMath.WasmMin(OriginalMath.Binary32(previous + dt), 1f);
            strike.Age = age;
            if (age < 1f && OriginalIntCast(age) > OriginalIntCast(previous) && Cue != null)
            {
                Cue.Invoke("super-count");
                strike = PendingStrike;
                age = strike.Age;
            }
            if (OriginalMath.Binary32(age + .00001f) < 1f) return;
            PendingStrike = null;
            var killed = new bool[Alive.Length];
            for (int cell = 0; cell < Alive.Length; cell++)
            {
                if (!Alive[cell] || cell >= position.Length || position[cell] < 0) continue;
                float dx = OriginalMath.Binary32((OriginalMath.Binary32(x[cell] - strike.X)) / strike.RadiusX);
                float dy = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(y[cell] + Offset) - strike.Y)) / strike.RadiusY);
                if (OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= 1f) killed[cell] = true;
            }
            int count = Remove(killed);
            float time = Time;
            sandFrozenUntil = OriginalMath.Binary32(time + 1.5f);
            SuperStrikeKills = unchecked(count + SuperStrikeKills);
            SuperStrikeCount = unchecked(SuperStrikeCount + 1);
            sandSlowUntil = OriginalMath.Binary32(time + 3.5f);
            float heldUntil = OriginalMath.Binary32(OriginalMath.Binary32(time + 3f) + Loadout.hold); // source FleetLoadout +56
            advanceHeldUntil = heldUntil;
            int legendary = (int)Loadout.legendary;
            sandClock = advanceClock = stepRemaining = 0f;
            advanceSlowUntil = OriginalMath.Binary32(heldUntil + ((legendary & 8) != 0 ? 2f : 0f));
            float pitch = Level.pitchY;
            PushBack(pitch > 0f ? OriginalMath.Binary32(pitch + pitch) : 18f, RepelKind.Fusion);
            RefreshFronts();
            StrikeDetonated?.Invoke(strike, count);
            Cue?.Invoke("super-impact");
            if (Remaining == 0)
            {
                State = Phase.Won;
                Cue?.Invoke("win");
            }
        }
    }
}
