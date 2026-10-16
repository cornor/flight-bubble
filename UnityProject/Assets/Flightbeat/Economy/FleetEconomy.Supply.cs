using System;
using System.Collections.Generic;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54080: draw RNG even when the legendary guarantee is already due.
        private int RollQuality(FleetProfile d)
        {
            int roll = random(100);
            if (d.legendPity > 18) return 4;
            int quality = roll < 40 ? 0 : roll < 70 ? 1 : roll < 90 ? 2 : roll < 98 ? 3 : 4;
            return quality < 3 && d.epicPity > 8 ? 3 : quality;
        }
        // f53989/f54289/f54290. Roll all results before Commit; publish and
        // return the saved objects. Original counts >=10 all cost 90 (no clamp).
        public FleetCore[] Draw(int count)
        {
            Refresh(); int price = count >= 10 ? 90 : unchecked(count * 10);
            if (count <= 0 || Data.fragments < price || unchecked(Data.cores.Count + count) > 120) return null;
            var result = new FleetCore[count]; int first = Data.nextCore;
            var kinds = new int[count]; var qualities = new int[count];
            var pity = new FleetProfile { epicPity = Data.epicPity, legendPity = Data.legendPity };
            for (int i = 0; i < count; i++)
            {
                int quality = qualities[i] = RollQuality(pity);
                kinds[i] = FleetCatalog.RandomModule(random);
                pity.epicPity = quality > 2 ? 0 : unchecked(pity.epicPity + 1);
                pity.legendPity = quality == 4 ? 0 : unchecked(pity.legendPity + 1);
            }
            Commit(d =>
            {
                d.fragments = unchecked(d.fragments - price); d.draws = unchecked(count + d.draws);
                for (int i = 0; i < count; i++) d.cores.Add(new FleetCore
                {
                    id = unchecked(first + i), kind = kinds[i], quality = qualities[i],
                    durability = FleetCatalog.DurabilityMax[qualities[i]]
                });
                d.nextCore = unchecked(first + count); d.epicPity = pity.epicPity; d.legendPity = pity.legendPity;
            });
            for (int i = 0; i < count; i++) { int index = i; result[i] = Data.cores.Find(c => c.id == unchecked(first + index)); }
            return result;
        }
        // f54081.
        public FleetCore DrawCore() { FleetCore[] result = Draw(1); return result == null ? null : result[0]; }
        // f54094/f54294. Clone first-copy ownership while planning a batch so
        // the same part can be new once and a duplicate later in the same draw.
        public PartPull[] DrawParts(int count, int target)
        {
            Refresh(); int price = count >= 10 ? 90 : unchecked(count * 10);
            if (count <= 0 || price > Data.fragments) return null;
            var result = new PartPull[count]; var owned = (int[])Data.parts.Clone();
            target = target < 5 ? target : -1;
            for (int i = 0; i < count; i++)
            {
                int index;
                if (target >= 0)
                {
                    if (random(100) <= 49) index = random(5) + target * 5;
                    else { int choice = random(20), fighter = choice / 5; index = (fighter + (fighter >= target ? 1 : 0)) * 5 + choice - fighter * 5; }
                }
                else index = random(25);
                bool first = owned[index] == 0; if (first) owned[index] = 1;
                result[i] = new PartPull { index = index, first = first };
            }
            Commit(d =>
            {
                d.fragments = unchecked(d.fragments - price); d.partDraws = unchecked(count + d.partDraws);
                foreach (PartPull p in result) if (p.first) d.parts[p.index] = 1; else d.spares[p.index] = unchecked(d.spares[p.index] + 1);
            });
            return result;
        }
        // f54096/f54295: exactly three spare copies, never the target itself.
        public bool Smelt(int target, int[] sources)
        {
            if (!SmeltTarget(target) || sources == null || sources.Length != 3) return false;
            var counts = new int[25];
            foreach (int source in sources)
            {
                if (unchecked((uint)source) > 24u || source == target) return false;
                counts[source] = unchecked(counts[source] + 1);
            }
            for (int i = 0; i < counts.Length; i++) if (counts[i] > Data.spares[i]) return false;
            bool first = Data.parts[target] == 0;
            Commit(d =>
            {
                for (int i = 0; i < counts.Length; i++) d.spares[i] = unchecked(d.spares[i] - counts[i]);
                if (first) d.parts[target] = 1; else d.spares[target] = unchecked(d.spares[target] + 1);
                d.smelts = unchecked(d.smelts + 1);
            });
            return true;
        }
        // f54097: includes the first copy in the original cumulative table.
        public int StillNeeded(int index)
        {
            int level = Data.parts[index]; level = level < 5 ? level : 5; level = level > 0 ? level : 0;
            return unchecked(FleetCatalog.PartTotal[5] - FleetCatalog.PartTotal[level]);
        }
        // f54098: three greedy picks, strict greater-than preserves first-index
        // ties. Surplus beyond max-level needs has the source +1000 priority.
        public int[] SuggestSmelt(int target)
        {
            int[] copies = (int[])Data.spares.Clone(); var result = new List<int>();
            for (int pick = 0; pick < 3; pick++)
            {
                int best = -1; float bestScore = -float.MaxValue;
                for (int i = 0; i < copies.Length; i++)
                {
                    if (i == target || copies[i] <= 0) continue;
                    int excess = unchecked(copies[i] - StillNeeded(i));
                    float score = (excess > 0 ? (float)excess + 1000f : 0f) + (float)copies[i];
                    if (score > bestScore) { best = i; bestScore = score; }
                }
                if (best < 0) return null;
                result.Add(best); copies[best] = unchecked(copies[best] - 1);
            }
            return result.ToArray();
        }
    }
}
