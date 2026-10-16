using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55030/f55032/f55033: direct source array access. Valid indices are
        // supplied by their callers; no extra clamping or fallback is added.
        public int ColorAt(int index) => index >= boardCount ? Level.stream[unchecked(index - boardCount)] : Level.cells[index];
        public int Front(int column) => fronts[column];
        public bool Pending(int index) => reserved[index];

        // f54978: a failed penetrating search leaves all four pierce fields
        // untouched. A nonpenetrating search clears only pierceFound, and only
        // after finding a nonempty front; callers rely on those side effects.
        private int ColumnTarget(int column, int color, int depth, bool includeReserved = false)
        {
            int front = fronts[column];
            if (front < 0) return -1;
            if (depth <= 0)
            {
                pierceFound = 0;
                if (!includeReserved && reserved[front]) return -1;
                return ColorAt(front) == color ? front : -1;
            }
            int row = position[front] / Level.columns;
            int count = 0, first = -1, second = -1, third = -1;
            while (row >= 0 && count <= depth)
            {
                int cell = sand[unchecked(row * Level.columns + column)];
                if (cell >= 0 && Alive[cell])
                {
                    if ((includeReserved || !reserved[cell]) && ColorAt(cell) == color)
                    {
                        pierceC = third;
                        pierceB = second;
                        pierceA = first;
                        pierceFound = count;
                        return cell;
                    }
                    if (count == 0) first = cell;
                    else if (count == 1) second = cell;
                    else if (count == 2) third = cell;
                    count = unchecked(count + 1);
                }
                row = unchecked(row - 1);
            }
            return -1;
        }

        // f54981/f55070: probes include already reserved matching targets.
        public bool Exposed(int color)
        {
            int depth = Loadout.pierce < 3 ? Loadout.pierce : 3;
            depth = depth > 0 ? depth : 0;
            for (int column = 0; column < Level.columns; column++)
                if (ColumnTarget(column, color, depth, true) >= 0) return true;
            return false;
        }
        public int ExposedAt(int column, int color)
        {
            if (column < 0 || Level.columns <= column) return -1;
            int depth = Loadout.pierce < 3 ? Loadout.pierce : 3;
            depth = depth > 0 ? depth : 0;
            return ColumnTarget(column, color, depth, true);
        }

        // f55071. The source slot argument is overwritten before its first
        // read. Scan forward first, reverse only when no eligible target exists,
        // and restore direction if both scans fail. Queries retain side effects.
        private int Target(Fighter ship, int slot)
        {
            int columns = Level.columns;
            if (columns > 0)
            {
                int depth = Loadout.pierce < 3 ? Loadout.pierce : 3;
                depth = depth > 0 ? depth : 0;
                int originalDirection = ship.ScanDirection;
                for (int pass = 0; pass < 2; pass++)
                {
                    if (pass != 0) ship.ScanDirection = unchecked(-originalDirection);
                    float nearest = float.MaxValue;
                    int target = -1, found = 0, first = -1, second = -1, third = -1;
                    for (int column = 0; column < columns; column++)
                    {
                        int candidate = ColumnTarget(column, ship.Color, depth);
                        if (candidate < 0) continue;
                        float distance = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)column) - ship.ScanColumn)) * OriginalMath.Binary32((float)ship.ScanDirection));
                        if (!(distance >= -.001f)) continue;
                        if (!(distance < OriginalMath.Binary32(nearest + -.001f)))
                        {
                            if (depth == 0 || !(Math.Abs(OriginalMath.Binary32(distance - nearest)) <= .001f) || pierceFound >= found) continue;
                        }
                        nearest = distance;
                        target = candidate;
                        found = pierceFound;
                        first = pierceA; second = pierceB; third = pierceC;
                    }
                    if (target >= 0)
                    {
                        ship.ScanColumn = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)ship.ScanDirection) * .01f)) + OriginalMath.Binary32((float)(position[target] % columns)));
                        pierceFound = found;
                        pierceA = found > 0 ? first : -1;
                        pierceB = found > 1 ? second : -1;
                        pierceC = found > 2 ? third : -1;
                        return target;
                    }
                }
                ship.ScanDirection = originalDirection;
            }
            pierceFound = 0;
            return -1;
        }

        // f54977: an arriving fighter prevents the full-without-target state.
        // Do not replace the per-fighter column probes with a cached color set:
        // their ordered writes to pierceFound/A/B/C are observable source state.
        public bool FullWithoutTarget
        {
            get
            {
                if (!Active || UnlockedSlots > Fleet.Count || Shots.Count > 0) return false;
                foreach (Fighter ship in Fleet)
                {
                    if (ship.Arrival > 0f) return false;
                    int depth = Loadout.pierce < 3 ? Loadout.pierce : 3;
                    depth = depth > 0 ? depth : 0;
                    for (int column = 0; column < Level.columns; column++)
                        if (ColumnTarget(column, ship.Color, depth, true) >= 0) return false;
                }
                return true;
            }
        }
    }
}
