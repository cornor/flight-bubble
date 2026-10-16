namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f53986/f54296: the caller owns the tutorial/level gate.
        public bool GiftStarterParts()
        {
            if (Data.starterGift) return false;
            Commit(d => { d.starterGift = true; d.spares[0] = unchecked(d.spares[0] + 2); });
            return true;
        }
        // f54088/f54291: capture the index and cost before JSON Commit.
        public bool UpgradePart(int fighter, int part)
        {
            if (!CanUpgradePart(fighter, part)) return false;
            int index = unchecked(fighter * 5 + part), cost = UpgradeCost(fighter, part);
            Commit(d => { d.spares[index] = unchecked(d.spares[index] - cost); d.parts[index] = unchecked(d.parts[index] + 1); });
            return true;
        }
        // f54090/f54292: copies, rather than moves, the active fighter's preset.
        // Do not normalize here: the source Commit publishes the clone as-is.
        public bool Assemble(int fighter)
        {
            if (!CanAssemble(fighter)) return false;
            int from = Data.activeFighter;
            Commit(d =>
            {
                d.assembled |= 1 << fighter;
                for (int slot = 0; slot < 5; slot++) d.presets[fighter * 5 + slot] = d.presets[from * 5 + slot];
            });
            return true;
        }
        // f54146/f54248: idempotent tutorial completion.
        public void CompleteGrowthTutorial()
        {
            if (!Data.growthTutorial) Commit(d => d.growthTutorial = true);
        }
        // f53987.
        public int Spare(int fighter, int part) => ValidPart(fighter, part) ? Data.spares[fighter * 5 + part] : 0;
        // f54095, including unsigned range tests on both index and part level.
        public bool SmeltTarget(int index)
        {
            if (unchecked((uint)index) > 24u) return false;
            int fighter = index / 5, part = index % 5, level = Data.parts[index];
            if (((Data.assembled >> fighter) & 1) == 0) return level == 0;
            return unchecked((uint)(level - 1)) <= 3u && Data.spares[index] < UpgradeCost(fighter, part);
        }
        // f54099: every spare outside the target contributes, in array order.
        public int SmeltableSpares(int target)
        {
            int total = 0; int[] spares = Data.spares;
            for (int i = 0; i < spares.Length; i++) if (i != target) total = unchecked(spares[i] + total);
            return total;
        }
    }
}
