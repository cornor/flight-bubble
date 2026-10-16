using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55011: settle in row-descending/column-ascending passes, at most
        // sandRows << 2 passes. The local dirty flag controls repetition;
        // the stored sandDirty is left true before refreshing fronts.
        private void Settle()
        {
            int limit = unchecked(sandRows << 2);
            bool dirty = sandDirty;
            for (int pass = 0; pass < limit && dirty; pass++)
            {
                sandPass = unchecked(sandPass + 1);
                dirty = false;
                for (int row = unchecked(sandRows - 2); row >= 0 && Level.columns > 0; row--)
                    for (int column = 0; column < Level.columns; column++)
                    {
                        int at = unchecked(Level.columns * row + column);
                        int cell = sand[at];
                        if (cell < 0) continue;
                        int below = unchecked(at + Level.columns);
                        if (sand[below] >= 0) continue;
                        if (Level.shapedFormation)
                        {
                            if (below < 0 || boardCount <= below || Level.cells[below] < 0) continue;
                        }
                        else if (below < 0) continue;
                        dirty = true;
                        sandDirty = true;
                        sand[at] = -1;
                        sand[below] = cell;
                        position[cell] = below;
                    }
            }
            for (int i = 0; i < boardCount; i++)
            {
                int at = position[i];
                if (at < 0) continue;
                int row = at / Level.columns;
                float pitch = Level.pitchY > 0f ? Level.pitchY : 9f;
                float origin = Level.pitchY > 0f ? Level.gridY : 147f;
                y[i] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)row))));
                int column = unchecked(at - row * Level.columns);
                pitch = Level.pitchX > 0f ? Level.pitchX : 10.5f;
                origin = Level.pitchX > 0f ? Level.gridX : 65f;
                x[i] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column))));
            }
            sandDirty = true;
            RefreshFronts();
        }

        // f55016: last live sand cell per column; float select uses > rather
        // than Math.Max, preserving its NaN/tie behavior and evaluation order.
        private void RefreshFronts()
        {
            LeadingRow = -1;
            float front = -1f;
            for (int column = 0; column < Level.columns; column++)
            {
                fronts[column] = -1;
                for (int row = unchecked(sandRows - 1); row >= 0; row--)
                {
                    int cell = sand[unchecked(row * Level.columns + column)];
                    if (cell < 0 || !Alive[cell]) continue;
                    fronts[column] = cell;
                    LeadingRow = Math.Max(LeadingRow, row);
                    float cellY = y[cell];
                    front = front > cellY ? front : cellY;
                    break;
                }
            }
            frontGridY = front;
        }
    }
}
