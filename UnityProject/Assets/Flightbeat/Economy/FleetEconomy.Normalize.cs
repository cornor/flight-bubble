#nullable disable
using System;
using System.Collections.Generic;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
#if UNITY_EDITOR
        // Declared f70385 input for CLI migration checks; absent from players.
        public static Func<Guid> NormalizeGuidOverride;
#endif
        // src: FleetEconomy.Normalize f54056. No persistence or clock is used here.
        public static void Normalize(FleetProfile d)
        {
            if (d.parts == null || d.parts.Length != 25)
            {
                int[] initial = new int[25];
                initial[4] = 1;
                initial[2] = 1;
                initial[3] = 1;
                initial[0] = 1;
                initial[1] = 1;
                d.parts = initial;
            }
            if (d.spares == null || d.spares.Length != 25) d.spares = new int[25];
            if (d.presets == null || d.presets.Length != 25) d.presets = new int[25];
            d.assembled |= 1;
            for (int i = 4; i >= 0; i--)
            {
                int level = d.parts[i];
                d.parts[i] = level > 1 ? level : 1;
            }
            // f54056 uses i32.le_u and i32.shr_u, despite the decompiler's untyped <=/>>.
            int active = d.activeFighter;
            if (unchecked((uint)active) > 4u || ((unchecked((uint)d.assembled) >> active) & 1u) == 0) d.activeFighter = 0;
            if (d.slots == null || d.slots.Length != 5) d.slots = new int[5];
            bool empty = true;
            for (int i = 24; i >= 0; i--)
                if (d.presets[i] != 0) { empty = false; break; }
            if (empty)
                for (int i = 0; i < 5; i++) d.presets[i] = d.slots[i];
            // f54056 clears index 4, then the 2/3 and 0/1 pairs.
            d.slots[4] = 0;
            d.slots[2] = 0;
            d.slots[3] = 0;
            d.slots[0] = 0;
            d.slots[1] = 0;
            if (d.offerSeed == 0)
            {
#if UNITY_EDITOR
                Guid guid = NormalizeGuidOverride != null ? NormalizeGuidOverride() : Guid.NewGuid();
#else
                Guid guid = Guid.NewGuid();
#endif
                // f70426 GUID hash, then f54056 i32.rem_u (not signed remainder).
                d.offerSeed = unchecked((int)((uint)guid.GetHashCode() % 2000000000u + 1u));
            }
            foreach (FleetCore core in d.cores)
            {
                if (core == null) continue;
                int durability = core.durability;
                if (durability >= 0 && durability <= core.MaxDurability) continue;
                core.durability = core.MaxDurability;
            }
            var used = new HashSet<int>();
            var fighters = new List<int> { 0, 1, 2, 3, 4 };
            // src: display-class f54274 (order) / f54275 (comparison), capturing d only.
            int Order(int fighter) => d.activeFighter == fighter ? -1 : fighter;
            fighters.Sort((a, b) => Order(a).CompareTo(Order(b)));
            foreach (int fighter in fighters)
            {
                for (int slot = 0; slot < 5; slot++)
                {
                    int index = fighter * 5 + slot;
                    int id = d.presets[index];
                    if (id != 0 && !used.Add(id)) d.presets[index] = 0;
                }
            }
            int streak = d.streak;
            streak = streak < 10 ? streak : 10;
            streak = streak > 0 ? streak : 0;
            d.streak = streak;
            int energy = d.energy;
            energy = energy < 999 ? energy : 999;
            d.energy = energy > 0 ? energy : 0;
            int best = d.bestStreak;
            d.bestStreak = best > streak ? best : streak;
        }
    }
}
