using System;
using System.Collections.Generic;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54065: first non-null preset in slot order, including broken modules.
        // Each source branch reads the current active fighter again.
        public FleetCore Equipped => InPreset(Data.activeFighter, 0) ?? InPreset(Data.activeFighter, 1)
            ?? InPreset(Data.activeFighter, 2) ?? InPreset(Data.activeFighter, 3) ?? InPreset(Data.activeFighter, 4);

        // f53996 / f54068: delegate to the existing two-argument operations.
        public bool Equip(int id) => Equip(Data.activeFighter, id);
        public bool Unequip(int slot) => Unequip(Data.activeFighter, slot);

        // f54066/f54067. Zero means an empty preset, not an equipped module.
        public bool IsEquipped(int id) => id != 0 && Array.IndexOf(Data.presets, id) >= 0;
        public int OwnerOf(int id)
        {
            if (id == 0) return -1;
            int index = Array.IndexOf(Data.presets, id); return index < 0 ? -1 : index / 5;
        }
        // f53990/f54277, shared id predicate f53959. Find precedes range guard.
        public bool Equip(int fighter, int id)
        {
            FleetCore core = Data.cores.Find(c => c.id == id);
            if (unchecked((uint)fighter) > 4u || core == null) return false;
            int index = core.Spec.slot + fighter * 5;
            if (Data.presets[index] == id) return false;
            Commit(d =>
            {
                int[] presets = d.presets;
                for (int i = 0; i < presets.Length; i++) if (presets[i] == id) presets[i] = 0;
                presets[index] = id;
            });
            return true;
        }
        // f54069/f54278. Unassembled fighters can still have a preset.
        public bool Unequip(int fighter, int slot)
        {
            if (unchecked((uint)fighter) > 4u || unchecked((uint)slot) > 4u || Data.presets[fighter * 5 + slot] == 0) return false;
            Commit(d => d.presets[fighter * 5 + slot] = 0); return true;
        }
        // f54070/f54279 and f53959. Reward is captured before Commit.
        public bool Recycle(int id)
        {
            FleetCore core = Data.cores.Find(c => c.id == id);
            if (core == null || IsEquipped(id)) return false;
            int fragments = FleetCatalog.RecycleValue[core.quality];
            Commit(d => { d.fragments = unchecked(fragments + d.fragments); d.cores.RemoveAll(c => c.id == id); });
            return true;
        }
        // f54077/f54078, quality range is unsigned.
        public int CopiesOf(int kind, int quality)
        {
            int count = 0;
            foreach (FleetCore c in Data.cores) if (c.kind == kind) count = unchecked(count + (c.quality == quality ? 1 : 0));
            return count;
        }
        public bool CanMergeModule(int kind, int quality) => unchecked((uint)quality) <= 3u && CopiesOf(kind, quality) > 2;

        // f54079/f54284..f54288. Unused first, then ascending id. All matching
        // preset references to consumed ids are replaced; no Normalize here.
        public FleetCore MergeModules(int kind, int quality)
        {
            if (unchecked((uint)quality) > 3u || CopiesOf(kind, quality) < 3) return null;
            List<FleetCore> candidates = Data.cores.FindAll(c => c.kind == kind && c.quality == quality);
            candidates.Sort((a, b) => { int order = IsEquipped(a.id).CompareTo(IsEquipped(b.id)); return order == 0 ? a.id.CompareTo(b.id) : order; });
            var used = new HashSet<int>(); used.Add(candidates[0].id); used.Add(candidates[1].id); used.Add(candidates[2].id);
            int id = Data.nextCore;
            Commit(d =>
            {
                d.cores.RemoveAll(c => used.Contains(c.id));
                d.cores.Add(new FleetCore { id = id, kind = kind, quality = quality + 1, durability = FleetCatalog.DurabilityMax[quality + 1] });
                d.nextCore = unchecked(id + 1);
                for (int i = 0; i < d.presets.Length; i++) if (used.Contains(d.presets[i])) d.presets[i] = id;
            });
            return Data.cores.Find(c => c.id == id);
        }
        // f53968/f54260 and f53959. Full or absent modules do not commit.
        public bool Repair(int id)
        {
            FleetCore core = Data.cores.Find(c => c.id == id);
            if (core == null || core.durability >= core.MaxDurability) return false;
            Commit(d => { FleetCore c = d.cores.Find(item => item.id == id); if (c != null) c.durability = c.MaxDurability; });
            return true;
        }
    }
}
