using System.Collections.Generic;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // Common source sequence in f54071/f54073: keep all preset ids and
        // the highest quality for each kind; equal quality keeps the greater id.
        private HashSet<int> RetainedModules()
        {
            var keep = new HashSet<int>(); foreach (int id in Data.presets) if (id != 0) keep.Add(id);
            var best = new Dictionary<int, FleetCore>();
            foreach (FleetCore core in Data.cores)
                if (!best.TryGetValue(core.kind, out FleetCore prior) || core.quality > prior.quality || (core.quality == prior.quality && core.id > prior.id)) best[core.kind] = core;
            foreach (FleetCore core in best.Values) keep.Add(core.id); return keep;
        }
        // f54073 returns duplicates in inventory order, not rarity order.
        public List<int> DuplicateModules()
        {
            HashSet<int> keep = RetainedModules(); var result = new List<int>();
            foreach (FleetCore core in Data.cores) if (!keep.Contains(core.id)) result.Add(core.id); return result;
        }
        // f54071/f54280/f54281.
        public int RecycleDuplicates()
        {
            HashSet<int> keep = RetainedModules(); int gain = 0, count = 0;
            foreach (FleetCore core in Data.cores) if (!keep.Contains(core.id)) { gain = unchecked(gain + FleetCatalog.RecycleValue[core.quality]); count++; }
            if (count == 0) return 0;
            Commit(d => { d.fragments = unchecked(gain + d.fragments); d.cores.RemoveAll(c => !keep.Contains(c.id)); });
            return gain;
        }
        // f54074/f54282/f54283. Preserve unknown ids in the copied set: a
        // nonempty unknown-only request still commits with zero payout in source.
        public int RecycleMany(IEnumerable<int> ids)
        {
            var selected = new HashSet<int>(ids); int gain = 0;
            foreach (FleetCore core in Data.cores)
            {
                if (selected.Contains(core.id) && !IsEquipped(core.id)) gain = unchecked(gain + FleetCatalog.RecycleValue[core.quality]);
                else selected.Remove(core.id);
            }
            if (selected.Count == 0) return 0;
            Commit(d => { d.fragments = unchecked(gain + d.fragments); d.cores.RemoveAll(c => selected.Contains(c.id)); });
            return gain;
        }
    }
}
