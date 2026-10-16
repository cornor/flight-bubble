namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f54961: pressure and legacy cycles have distinct lower-bound rules.
        public float AdvanceCycle => Pressure.enabled
            ? OriginalMath.WasmMax(Pressure.stepInterval, .2f)
            : Level.advanceStepInterval > 0f ? Level.advanceStepInterval : 2.4f;

        // f54970: no front (including a front above zero) uses distance 999.
        public float Danger
        {
            get
            {
                if (!Pressure.enabled || Pressure.warnDistance <= 0f) return 0f;
                float distance = 999f;
                if (!(frontGridY < 0f))
                {
                    float screen = OriginalMath.Binary32(frontGridY + Offset);
                    if (!(screen < 0f)) distance = OriginalMath.Binary32(562f - screen);
                }
                float value = OriginalMath.Binary32(1f - (OriginalMath.Binary32(distance / Pressure.warnDistance)));
                return value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
            }
        }

        // f54969: preserve both active/idle branches and final WAT grouping
        // (1 - slow) * (factor * speed), which the decompiler prints ambiguously.
        public float CurrentSpeed
        {
            get
            {
                if (!Pressure.enabled) return Level.advanceSpeed;
                float factor = Shots.Count > 0 || OriginalMath.Binary32(Time - lastFire) < .6f
                    ? Pressure.FirepowerFactor(Firepower)
                    : OriginalMath.Binary32(Pressure.idleFactor * (OriginalMath.Binary32(1f - Loadout.idleSlow)));
                float slow = Loadout.slow;
                float dangerSlow = Danger > 0f ? OriginalMath.Binary32(Loadout.dangerSlow * Danger) : 0f;
                slow = OriginalMath.Binary32(slow + dangerSlow);
                slow = slow < 0f ? 0f : OriginalMath.WasmMin(slow, 1f);
                float speed = Level.commanderTutorial ? Pressure.openingSpeed : Pressure.advanceSpeed;
                return OriginalMath.WasmMax(OriginalMath.Binary32((OriginalMath.Binary32(1f - slow)) * (OriginalMath.Binary32(factor * speed))), 0f);
            }
        }

        // f55062: push interpolation precedes advance gates. The source WAT
        // explicitly computes 3 - (t + t), not the decompiler's "3 - t + t".
        private void MoveFormation(float dt, bool advance = true)
        {
            float age = pushAge, duration = pushDuration;
            if (age < duration)
            {
                age = OriginalMath.Binary32(age + dt);
                age = duration < age ? duration : age;
                pushAge = age;
                float previous = pushApplied;
                float t = OriginalMath.Binary32(age / duration);
                pushApplied = OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(t * t)) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(t + t)))))) * pushDistance);
                float delta = OriginalMath.Binary32(pushApplied - previous);
                Offset = OriginalMath.Binary32(Offset - delta);
                EffectiveRetreat = OriginalMath.Binary32(delta + EffectiveRetreat);
            }
            if (!advance || duration > age || OriginalMath.WasmMax(OriginalMath.Binary32(advanceHeldUntil - Time), 0f) > 0f) return;
            if (TrainingSafe && !(frontGridY < 0f))
            {
                float front = OriginalMath.Binary32(frontGridY + Offset);
                if (!(front < 0f) && OriginalMath.Binary32(562f - front) < 48f) return;
            }
            if (stepRemaining > 0f)
            {
                float step = OriginalMath.Binary32(stepRate * dt);
                step = stepRemaining < step ? stepRemaining : step;
                stepRemaining = OriginalMath.Binary32(stepRemaining - step);
                Offset = OriginalMath.Binary32(Offset + step);
            }
            advanceClock = OriginalMath.Binary32(advanceClock + (OriginalMath.Binary32((Time < advanceSlowUntil ? .5f : 1f) * dt)));
            while (true)
            {
                float cycle = AdvanceCycle;
                if (!(OriginalMath.Binary32(advanceClock + .00001f) >= cycle)) return;
                advanceClock = OriginalMath.WasmMax(OriginalMath.Binary32(advanceClock - cycle), 0f);
                float distance = OriginalMath.Binary32(CurrentSpeed * AdvanceCycle);
                if (distance <= 0f) continue;
                float previous = stepRemaining;
                stepRemaining = distance;
                Offset = OriginalMath.Binary32(previous + Offset);
                cycle = AdvanceCycle;
                AdvanceSteps = unchecked(AdvanceSteps + 1);
                stepRate = OriginalMath.Binary32(distance / OriginalMath.WasmMin(OriginalMath.Binary32(cycle * .5f), .24f));
                // Re-read mutable Pressure/Level/Time at the next cycle after
                // the original advance cue, including callback modifications.
                Cue?.Invoke("advance");
            }
        }

        // f55067. Unshaped formations deliberately accept every integer here;
        // callers perform the separate index/column checks from their bodies.
        private bool FormationCell(int at) => !Level.shapedFormation
            || (at >= 0 && at < boardCount && Level.cells[at] >= 0);

        // f55017, also inlined into f55065: slide only if the cell has no cover
        // directly above. The first/second diagonal priority is caller supplied.
        private void DropSand(int row, int column, int first, int second, bool slide)
        {
            int columns = Level.columns;
            int at = unchecked(columns * row + column);
            int cell = sand[at];
            if (cell < 0) return;
            int below = unchecked(at + columns), target;
            if (sand[below] < 0 && FormationCell(below)) target = below;
            else
            {
                if (!slide || (row != 0 && sand[unchecked(at - columns)] >= 0)) return;
                int side = unchecked(column + first);
                target = unchecked(below + first);
                if (side < 0 || side >= columns || sand[target] >= 0 || !FormationCell(target))
                {
                    side = unchecked(column + second);
                    target = unchecked(below + second);
                    if (side < 0 || side >= columns || sand[target] >= 0 || !FormationCell(target)) return;
                }
            }
            if (target < 0) return;
            sandDirty = true;
            sand[at] = -1;
            sand[target] = cell;
            position[cell] = target;
        }

        // f55068: fill the first gap immediately above an occupied cell. If
        // there is any occupant but no gap, no entry; empty columns use bottom.
        private int StreamEntry(int column)
        {
            int last = unchecked(sandRows - 1), columns = Level.columns;
            for (int row = 0; row < last; row++)
            {
                int at = unchecked(row * columns + column);
                if (sand[at] < 0 && sand[unchecked(at + columns)] >= 0) return at;
            }
            for (int row = 0; row < sandRows; row++)
                if (sand[unchecked(row * columns + column)] >= 0) return -1;
            return unchecked(columns * last + column);
        }

        // f55066: discard dead queued cells, then inject at most one per column.
        // Initial stream Y is one pitch above the assigned sand position.
        private void InjectStreams()
        {
            for (int column = 0; column < Level.columns; column++)
            {
                var queue = incoming[column];
                while (queue.Count > 0 && !Alive[queue.Peek()]) queue.Dequeue();
                if (queue.Count == 0) continue;
                int at = StreamEntry(column);
                if (at < 0) continue;
                sandDirty = true;
                int cell = queue.Dequeue();
                sand[at] = cell;
                position[cell] = at;
                float pitch = Level.pitchX > 0f ? Level.pitchX : 10.5f;
                float origin = Level.pitchX > 0f ? Level.gridX : 65f;
                x[cell] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column))));
                pitch = Level.pitchY > 0f ? Level.pitchY : 9f;
                origin = Level.pitchY > 0f ? Level.gridY : 147f;
                y[cell] = OriginalMath.Binary32((OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)(at / Level.columns)))))) - pitch);
            }
        }

        // f55065: each 0.03f substep has at most six dirty passes; rows go
        // bottom-up, column and diagonal order alternate with sandPass parity.
        private void Flow(float dt)
        {
            MoveFormation(dt);
            if (Time < sandFrozenUntil)
            {
                sandClock = 0f;
                RefreshFronts();
                return;
            }
            sandClock = OriginalMath.Binary32(sandClock + (Time < sandSlowUntil ? OriginalMath.Binary32(dt * .5f) : dt));
            while (sandClock >= .03f)
            {
                sandClock = OriginalMath.Binary32(sandClock + -.03f);
                if (!sandDirty) continue;
                for (int pass = 0; pass < 6; pass++)
                {
                    sandDirty = false;
                    sandPass = unchecked(sandPass + 1);
                    for (int row = unchecked(sandRows - 2); row >= 0 && Level.columns > 0; row--)
                    {
                        bool reverse = (sandPass & 1) != 0;
                        for (int scan = 0; scan < Level.columns; scan++)
                        {
                            int column = reverse ? unchecked(Level.columns + ~scan) : scan;
                            int first = (unchecked(row * 31 - column + sandPass) & 1) != 0 ? -1 : 1;
                            DropSand(row, column, first, -first, true);
                        }
                    }
                    InjectStreams();
                    if (!sandDirty) break;
                }
            }
            for (int column = 0; column < Level.columns; column++)
                for (int row = unchecked(sandRows - 1); row >= 0; row--)
                {
                    int cell = sand[unchecked(Level.columns * row + column)];
                    if (cell < 0) continue;
                    float factor = OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(-(OriginalMath.Binary32((OriginalMath.Binary32(flow[cell] * 3f)) + 10f)) * dt)));
                    factor = factor < 0f ? 0f : OriginalMath.WasmMin(factor, 1f);
                    float previous = x[cell];
                    float pitch = Level.pitchX > 0f ? Level.pitchX : 10.5f;
                    float origin = Level.pitchX > 0f ? Level.gridX : 65f;
                    x[cell] = OriginalMath.Binary32(previous + (OriginalMath.Binary32(factor * (OriginalMath.Binary32((OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column))))) - previous)))));
                    pitch = Level.pitchY > 0f ? Level.pitchY : 9f;
                    origin = Level.pitchY > 0f ? Level.gridY : 147f;
                    y[cell] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)(position[cell] / Level.columns)))));
                }
            RefreshFronts();
        }
    }
}
