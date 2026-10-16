namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54134. Prefer the most collected unassembled fighter, first on ties.
        // All assembled falls back to the stored active fighter without normalization.
        public int PartTarget
        {
            get
            {
                int target = -1, most = -1;
                for (int fighter = 1; fighter < 5; fighter++)
                    if ((Data.assembled & (1 << fighter)) == 0 && Collected(fighter) > most)
                    { most = Collected(fighter); target = fighter; }
                return target < 0 ? Data.activeFighter : target;
            }
        }
        // f54135. The cloned first-copy map distinguishes duplicates within the
        // batch. This helper changes the supplied transaction profile, not Data.
        internal PartPull[] GrantParts(FleetProfile d, int fighter, int count)
        {
            var result = new PartPull[count]; var owned = (int[])d.parts.Clone();
            for (int i = 0; i < count; i++)
            {
                int index = unchecked(random(5) + fighter * 5);
                bool first = owned[index] == 0; if (first) owned[index] = 1;
                result[i] = new PartPull { index = index, first = first };
            }
            foreach (PartPull p in result)
                if (p.first) d.parts[p.index] = 1; else d.spares[p.index] = unchecked(d.spares[p.index] + 1);
            return result;
        }
        public bool ZoneCleared(int zone) => Data.cleared.Contains(unchecked(WarZones.LastLevel(zone) + 1)); // f54140, no range guard.
        public bool ZoneClaimed(int zone) => ((Data.zoneClaimed >> zone) & 1) != 0; // f54141, masked shift.
        // f54143/f54268. No Refresh; target captured before Commit; only currency
        // rewards double. RNG is consumed inside the transaction's GrantParts.
        public PartPull[] ClaimZone(int zone, bool doubled)
        {
            if (!ZoneReady(zone)) return null;
            int multiplier = doubled ? 2 : 1, target = PartTarget; PartPull[] parts = null;
            Commit(d =>
            {
                d.zoneClaimed |= 1 << zone;
                d.coins = unchecked(d.coins + multiplier * WarZones.RewardCoins[zone]);
                d.fragments = unchecked(d.fragments + multiplier * WarZones.RewardFragments[zone]);
                parts = GrantParts(d, target, WarZones.RewardParts[zone]);
            });
            return parts;
        }
    }
}
