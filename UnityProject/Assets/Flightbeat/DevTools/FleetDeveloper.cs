using System;
using System.IO;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Economy;
using Flightbeat.Game;
using UnityEngine;
using System.Collections.Generic;
namespace Flightbeat.Economy
{
    public static class FleetDeveloper
    {
        // f54124 / f54261. Currency sums are signed i64; energy/items wrap i32.
        public static void DevGrant(this FleetEconomy economy,int coins, int fragments, int energy, int items)
        {
            economy.Commit(d =>
            {
                d.coins = unchecked((int)Math.Min(99999999L, (long)coins + d.coins));
                d.fragments = unchecked((int)Math.Min(9999999L, (long)fragments + d.fragments));
                d.energy = Math.Min(999, unchecked(energy + d.energy));
                for (int i = 0; i < d.items.Length; i++) d.items[i] = Math.Min(99, unchecked(d.items[i] + items));
            });
        }

        // f54125 / f54262. Five batches, ascending fighter order, one commit.
        public static void DevParts(this FleetEconomy economy,int perFighter)
        {
            economy.Commit(d => { for (int fighter = 0; fighter < 5; fighter++) economy.GrantParts(d, fighter, perFighter); });
        }

        // f54126 / f54263. Quality RNG precedes kind RNG. Even an empty grant commits.
        public static int DevModules(this FleetEconomy economy,int count)
        {
            int added = 0;
            economy.Commit(d =>
            {
                for (int i = 0; i < count && d.cores.Count < 120; i++)
                {
                    int quality = economy.random(5), kind = FleetCatalog.RandomModule(economy.random);
                    d.cores.Add(new FleetCore { id = d.nextCore, kind = kind, quality = quality, durability = FleetCatalog.DurabilityMax[quality] });
                    d.nextCore = unchecked(d.nextCore + 1); added = unchecked(added + 1);
                }
            });
            return added;
        }

        // f54127 / f54264. Return distinct nonzero preset IDs, including stale IDs.
        public static int DevWearAll(this FleetEconomy economy)
        {
            var used = new HashSet<int>();
            foreach (int id in economy.Data.presets) if (id != 0) used.Add(id);
            if (used.Count == 0) return 0;
            economy.Commit(d => { foreach (FleetCore core in d.cores) if (used.Contains(core.id)) core.durability = 1; });
            return used.Count;
        }

        public static void DevStreak(this FleetEconomy economy,int delta) => economy.Commit(d => d.streak = Math.Max(0, Math.Min(10, unchecked(d.streak + delta)))); // f54128 / f54265

        public static void DevUnlockAll(this FleetEconomy economy) => economy.Commit(d => d.unlocked = PixelWingGame.Levels.Length); // f54129 / f54246

        // f54130 / f54266. Clearing does not claim the zone reward or record wins.
        public static void DevClearZone(this FleetEconomy economy,int zone)
        {
            int first = WarZones.FirstLevel(zone), last = WarZones.LastLevel(zone);
            economy.Commit(d =>
            {
                for (int i = first; i <= last; i++) if (!d.cleared.Contains(unchecked(i + 1))) d.cleared.Add(unchecked(i + 1));
                d.unlocked = Math.Min(PixelWingGame.Levels.Length, Math.Max(d.unlocked, unchecked(last + 2)));
            });
        }

    }
}
