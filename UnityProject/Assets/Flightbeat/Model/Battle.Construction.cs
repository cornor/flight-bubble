using System;
using System.Collections.Generic;
using Flightbeat.Economy;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55134; metadata 0x36FFF0, SHA E0BC9E5F...23575D9F.
        public static readonly float[] TierWeight = { 1f, 3f, 4.5f, 8f };

        // f55010: a null pressure loads the original authored resource via f54431.
        public Battle(LevelData level, PressureSettings pressure, bool awaitDeployment = true)
        {
            Fleet = new List<Fighter>();
            var shots = new List<Shot>();
            State = Phase.Ready;
            Shots = shots;
            sandDirty = true;
            pushAge = 10f;
            lastHit = -10f; lastFire = -10f;
            frontGridY = -1f;
            Notice = String.Empty;
            pendingPushes = new List<PendingPush>();
            finaleHits = new List<int>();
            FleetLoadout empty = FleetLoadout.Empty;
            legendFusionAt = -30f; fusionFireUntil = -10f;
            Loadout = empty;
            Weapon = new WeaponProfile();
            weaponPick = new List<int>();
            weaponScan = new List<Int2>();

            // f55010 WAT 164..185; validation uses wrapping i32 multiplication.
            if (level == null || level.cells == null || level.columns <= 0 || level.cells.Length != unchecked(level.rows * level.columns))
                throw new ArgumentException("Invalid authored level"); // slot 5832392
            Level = level;
            this.awaitDeployment = awaitDeployment;
            if (pressure == null) pressure = PressureSettings.Load();
            boardCount = level.cells.Length;
            Pressure = pressure;
            int streamCount = level.stream == null ? 0 : level.stream.Length;
            Alive = new bool[unchecked(boardCount + streamCount)];
            reserved = new bool[Alive.Length];
            fronts = new int[level.columns];
            x = new float[Alive.Length];
            y = new float[Alive.Length];
            flow = new float[Alive.Length];
            sandRows = unchecked(level.rows + 3);
            sand = new int[unchecked(level.columns * sandRows)];
            position = new int[Alive.Length];
            for (int i = 0; i < sand.Length; i++) sand[i] = -1;
            for (int i = 0; i < position.Length; i++) position[i] = -1;
            LeadingRow = -1;
            int maximumColor = -1;
            foreach (int color in level.cells) maximumColor = Math.Max(maximumColor, color);
            if (level.stream != null) foreach (int color in level.stream) maximumColor = Math.Max(maximumColor, color);
            if (level.queue != null) foreach (QueueEntry entry in level.queue) maximumColor = Math.Max(maximumColor, entry.color);
            int paletteColors = level.palette == null ? 0 : level.palette.Length / 3;
            ColorCount = Math.Max(unchecked(maximumColor + 1), paletteColors);
            if (ColorCount <= 0) throw new ArgumentException("Level has no playable colors"); // 5833904
            int[] colors = new int[ColorCount];
            for (int i = 0; i < fronts.Length; i++) fronts[i] = -1;
            for (int i = 0; i < boardCount; i++)
            {
                // f32.mul then f32.add; pitch selects BOTH origin and spacing.
                float pitch = Level.pitchY > 0f ? Level.pitchY : 9f;
                float origin = Level.pitchY > 0f ? Level.gridY : 147f;
                y[i] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)(i / Level.columns)))));
                int column = i % level.columns;
                pitch = Level.pitchX > 0f ? Level.pitchX : 10.5f;
                origin = Level.pitchX > 0f ? Level.gridX : 65f;
                x[i] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column))));
                position[i] = i;
                int color = level.cells[i];
                if (color == -1) continue;
                if (color < 0 || color >= ColorCount) throw new ArgumentException("Invalid color"); // 5832408
                Alive[i] = true;
                sand[i] = i;
                Remaining = unchecked(Remaining + 1);
                colors[color] = unchecked(colors[color] + 1);
                fronts[i % level.columns] = i;
                LeadingRow = i / level.columns;
                flow[i] = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(level.seed + i * 73) % 101)) / 125f)) + .7f);
            }
            incoming = new Queue<int>[level.columns];
            for (int column = 0; column < incoming.Length; column++) incoming[column] = new Queue<int>();
            if (streamCount > 0)
            {
                if (level.streamOffsets == null || level.streamOffsets.Length != unchecked(level.columns + 1) || level.streamOffsets[level.columns] != streamCount)
                    throw new ArgumentException("Invalid sand stream offsets"); // 5832592
                for (int column = 0; column < level.columns; column++)
                    for (int n = level.streamOffsets[column]; n < level.streamOffsets[column + 1]; n++)
                    {
                        int color = level.stream[n];
                        if (color < 0 || color >= ColorCount) throw new ArgumentException("Invalid stream color"); // 5832624
                        int i = unchecked(boardCount + n);
                        Alive[i] = true;
                        colors[color] = unchecked(colors[color] + 1);
                        Remaining = unchecked(Remaining + 1);
                        flow[i] = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(level.seed + i * 73) % 101)) / 125f)) + .7f);
                        float pitch = Level.pitchX > 0f ? Level.pitchX : 10.5f;
                        float origin = Level.pitchX > 0f ? Level.gridX : 65f;
                        x[i] = OriginalMath.Binary32(origin + (OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column))));
                        // Stream Y uses a separate gridY > 0 test (WAT 913..932).
                        y[i] = OriginalMath.Binary32((level.gridY > 0f ? level.gridY : 147f) - (Level.pitchY > 0f ? Level.pitchY : 9f));
                        incoming[column].Enqueue(i);
                    }
            }
            Total = Remaining;
            int[] ammunition = new int[ColorCount];
            bool hasQueue = level.queue != null && level.queue.Length != 0;
            if (level.queue != null)
                foreach (QueueEntry entry in level.queue)
                    if (entry.color >= 0 && entry.color < ColorCount)
                        ammunition[entry.color] = unchecked(ammunition[entry.color] + entry.ammo);
            exactBudget = hasQueue;
            for (int i = 0; i < ColorCount && exactBudget; i++)
                if (ammunition[i] != colors[i]) exactBudget = false;
            int lanes = Math.Max(1, level.laneCount);
            if (level.queue != null) foreach (QueueEntry entry in level.queue) lanes = Math.Max(lanes, unchecked(entry.lane + 1));
            if (level.laneCount <= 0) lanes = Math.Max(4, lanes);
            Reserve = new List<Fighter>[lanes];
            for (int i = 0; i < Reserve.Length; i++) Reserve[i] = new List<Fighter>();
            BaseSlots = level.initialSlots > 0 ? (level.initialSlots < 8 ? level.initialSlots : 8) : 5;
            MaximumSlots = level.slotLimit > 0 ? (BaseSlots > level.slotLimit ? BaseSlots : level.slotLimit < 8 ? level.slotLimit : 8) : 7;
            extraBay = new bool[Math.Max(0, unchecked(MaximumSlots - BaseSlots))];
            if (hasQueue)
            {
                foreach (QueueEntry entry in level.queue)
                {
                    if (entry.lane < 0 || entry.lane >= Reserve.Length || entry.color < 0 || entry.color >= ColorCount || entry.ammo <= 0)
                        throw new ArgumentException("Invalid fighter queue"); // 5832476
                    int id = nextId; nextId = unchecked(nextId + 1);
                    var fighter = new Fighter(entry.color, entry.ammo, id);
                    fighter.Queue = entry.lane;
                    fighter.Order = entry.order;
                    fighter.DeployAt = entry.deployAt;
                    fighter.Kind = entry.kind;
                    fighter.Other = entry.other;
                    fighter.SourceToken = entry.sourceToken;
                    fighter.SourceRow = entry.sourceRow;
                    fighter.SourceColumn = entry.sourceColumn;
                    Reserve[entry.lane].Add(fighter);
                }
            }
            else
            {
                for (int color = 0; color < ColorCount; color++)
                {
                    int remaining = colors[color];
                    while (remaining > 0)
                    {
                        int ammo = Math.Min(Math.Max(1, level.capacity), remaining);
                        int lane = color % Reserve.Length;
                        int id = nextId; nextId = unchecked(nextId + 1);
                        var fighter = new Fighter(color, ammo, id);
                        fighter.Queue = lane;
                        Reserve[lane].Add(fighter);
                        remaining = unchecked(remaining - ammo);
                    }
                }
            }
            Settle();
            if (level.introDuration > 0f) State = Phase.Intro;
        }
    }
}
