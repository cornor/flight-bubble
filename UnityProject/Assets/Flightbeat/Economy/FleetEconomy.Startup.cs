using System;
using System.Collections.Generic;
using Flightbeat.Game;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54093: sum the entire spares array with wrapping i32 additions.
        public int TotalSpares
        {
            get
            {
                int total = 0;
                foreach (int count in Data.spares) total = unchecked(total + count);
                return total;
            }
        }

        // f54110: same signed guard and unsigned tiers as f54018.
        public int StreakPercent => FleetCatalog.StreakPercent(Data.streak);

        // f54150 / f54251: commit once without refreshing the daily counters.
        public void Chat() => Commit(d => d.officerChats = unchecked(d.officerChats + 1));

        // f54111 / f54243. Return the prior streak; don't commit a nonpositive one.
        public int BreakStreak()
        {
            int previous = Data.streak;
            if (previous > 0) Commit(d => d.streak = 0);
            return previous;
        }

        // f54144 / f54247. attemptLevel is deliberately retained.
        public void Abandon()
        {
            if (Data.attempt.Length > 0) Commit(d => d.attempt = String.Empty);
        }

        // f54123 / f54258 / f54259. Only durability == 0 is broken. The source
        // removes matching cores and clears presets; it does not clear slots here.
        public List<string> TakeBroken()
        {
            var names = new List<string>();
            var ids = new HashSet<int>();
            foreach (FleetCore core in Data.cores)
            {
                if (core.durability != 0) continue;
                ids.Add(core.id);
                names.Add(core.Spec.name + "·" + FleetCatalog.QualityNames[core.quality]);
            }
            if (ids.Count > 0)
                Commit(d =>
                {
                    d.cores.RemoveAll(c => ids.Contains(c.id));
                    for (int i = 0; i < d.presets.Length; i++)
                        if (ids.Contains(d.presets[i])) d.presets[i] = 0;
                });
            return names;
        }

        // f54107 / f54301.
        public void Begin(int level, string id)
        {
            if (level <= 0 || level > PixelWingGame.Levels.Length || level > Data.unlocked || String.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid sortie");
            Commit(d => { d.attempt = id; d.attemptLevel = level; });
        }

        // f54145 / f54269. Negative merges/kills remain signed contributions when
        // any argument triggers the operation; only maxes is clamped by the source.
        public void RecordBattle(int merges, int kills, int maxes, int bestCombo)
        {
            if (merges <= 0 && kills <= 0 && maxes <= 0 && bestCombo <= 0) return;
            Refresh();
            Commit(d =>
            {
                d.dayMerges = unchecked(d.dayMerges + merges);
                d.dayKills = unchecked(d.dayKills + kills);
                d.totalKills = unchecked(d.totalKills + kills);
                d.dayMaxes = unchecked(d.dayMaxes + Math.Max(0, maxes));
                d.dayCombo = Math.Max(d.dayCombo, bestCombo);
            });
        }

        // f54118 / f54254; capture unlimitedUntil before calling the clock.
        public bool SpendSortie()
        {
            SettleEnergy();
            if (Data.streak > 0) return true;
            long unlimitedUntil = Data.unlimitedUntil;
            if (Now < unlimitedUntil) return true;
            if (Data.energy <= 0) return false;
            long now = Now;
            Commit(d =>
            {
                int energy = d.energy;
                if (energy >= 5) d.energyAt = now;
                d.energy = unchecked(energy - 1);
            });
            return true;
        }

        // f54064 / f53992 / closure f53959. WAT guards are unsigned.
        public int ActiveFighter => Data.activeFighter;
        public FleetCore InPreset(int fighter, int slot)
        {
            if (unchecked((uint)fighter) > 4u || unchecked((uint)slot) > 4u) return null;
            int id = Data.presets[fighter * 5 + slot];
            return id == 0 ? null : Data.cores.Find(c => c.id == id);
        }

        // f54122 / f54257: collect ids from five slots, then mutate one clone.
        public void WearModules(int fighter)
        {
            var ids = new List<int>();
            for (int slot = 0; slot < 5; slot++)
            {
                FleetCore core = InPreset(fighter, slot);
                if (core != null && core.durability > 0) ids.Add(core.id);
            }
            if (ids.Count > 0)
                Commit(d =>
                {
                    foreach (FleetCore core in d.cores)
                        if (ids.Contains(core.id)) core.durability = Math.Max(0, unchecked(core.durability - 1));
                });
        }

        // f53993 / f54085 / f54162 / f54163.
        public int PartLevel(int fighter, int part) => ValidPart(fighter, part) ? Data.parts[fighter * 5 + part] : 0;
        public int FighterLevel(int fighter)
        {
            if (unchecked((uint)fighter) > 4u || ((Data.assembled >> fighter) & 1) == 0) return 0;
            int level = 5;
            for (int part = 0; part < 5; part++) level = Math.Min(level, PartLevel(fighter, part));
            return level;
        }
        public FleetLook Look() => LookOf(Data.activeFighter);
        public FleetLook LookOf(int fighter)
        {
            var look = new FleetLook { model = fighter, stage = Math.Max(1, FighterLevel(fighter)), parts = new int[5] };
            for (int part = 0; part < 5; part++) look.parts[part] = Math.Max(1, PartLevel(fighter, part));
            return look;
        }

        // f53988 refreshes the profile before calculating the active loadout.
        public FleetLoadout Snapshot()
        {
            Refresh();
            return Bonus(Data.activeFighter).Loadout;
        }
    }
}
