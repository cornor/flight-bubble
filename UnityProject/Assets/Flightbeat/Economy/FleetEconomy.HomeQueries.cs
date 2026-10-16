using System;
using System.Collections.Generic;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // src: Assembled f54082, unsigned fighter range.
        public bool Assembled(int fighter) => unchecked((uint)fighter) <= 4u && ((Data.assembled >> fighter) & 1) != 0;

        // src: CanAssemble f54089 / SetActive f54091 / closure f54293.
        public bool CanAssemble(int fighter) => unchecked((uint)(fighter - 1)) < 4u && !Assembled(fighter) && Collected(fighter) == 5;
        public bool SetActive(int fighter)
        {
            if (!Assembled(fighter) || Data.activeFighter == fighter) return false;
            Commit(d => d.activeFighter = fighter);
            return true;
        }

        // src: CoresByRarity f54075 / comparison f54239, cached by profile revision.
        public IReadOnlyList<FleetCore> CoresByRarity()
        {
            if (sortedCores == null || sortedRevision != Data.revision)
            {
                var current = new List<FleetCore>(Data.cores);
                sortedCores = current;
                current.Sort((a, b) => { int order = b.quality.CompareTo(a.quality); return order != 0 ? order : b.id.CompareTo(a.id); });
                sortedRevision = Data.revision;
            }
            return sortedCores;
        }
        // src: ForSlot f54076 / InSlot f53997.
        public List<FleetCore> ForSlot(int slot)
        {
            var result = new List<FleetCore>();
            foreach (FleetCore core in CoresByRarity()) if (core.Spec.slot == slot) result.Add(core);
            return result;
        }
        public FleetCore InSlot(int slot) => InPreset(Data.activeFighter, slot);

        // src: get_CanSortie f54117; no SettleEnergy call in this getter.
        public bool CanSortie
        {
            get
            {
                if (Data.streak > 0) return true;
                long until = Data.unlimitedUntil;
                if (Now < until) return true;
                return Data.energy > 0;
            }
        }

        // src: ZoneReady f54142. Unsigned guard also rejects negative zones.
        public bool ZoneReady(int zone) => unchecked((uint)zone) <= 4u
            && Data.cleared.Contains(unchecked(WarZones.LastLevel(zone) + 1))
            && ((Data.zoneClaimed >> zone) & 1) == 0;

        // src: Collected f54084; the WAT range guard is unsigned.
        public int Collected(int fighter)
        {
            int count = 0;
            for (int part = 0; part < 5; part++)
                if (PartLevel(fighter, part) > 0) count++;
            return count;
        }

        // src: UpgradeCost f54086 / CanUpgradePart f54087.
        public int UpgradeCost(int fighter, int part)
        {
            int level = PartLevel(fighter, part);
            return unchecked((uint)(level - 1)) <= 3u ? FleetCatalog.PartCost[level + 1] : 0;
        }
        public bool CanUpgradePart(int fighter, int part)
        {
            if (!Assembled(fighter) || unchecked((uint)part) > 4u) return false;
            if (PartLevel(fighter, part) <= 0 || PartLevel(fighter, part) > 4) return false;
            int spares = Data.spares[fighter * 5 + part];
            return spares >= UpgradeCost(fighter, part);
        }

        // src: get_PartsReady f54092, in original fighter/part order.
        public int PartsReady
        {
            get
            {
                int count = 0;
                for (int fighter = 0; fighter < 5; fighter++)
                {
                    if (fighter > 0 && !Assembled(fighter) && Collected(fighter) == 5) count++;
                    for (int part = 0; part < 5; part++)
                        if (CanUpgradePart(fighter, part)) count++;
                }
                return count;
            }
        }

        // src: get_DailyReady f54156. Refresh happens before reading the day fields.
        public int DailyReady
        {
            get
            {
                Refresh();
                int ready = 0;
                for (int i = 0; i < 9; i++)
                {
                    int progress;
                    switch (i)
                    {
                        case 0: progress = Data.dayWins; break;
                        case 1: progress = Data.dayMerges; break;
                        case 2: progress = Data.dayKills; break;
                        case 3: progress = Data.dayItems; break;
                        case 4: progress = Data.dayMaxes; break;
                        case 5: progress = Data.dayCombo; break;
                        case 6: progress = Data.dayCleanWins; break;
                        case 7: progress = Data.dayFastWins; break;
                        default: progress = Data.dayAds; break;
                    }
                    if (progress >= FleetCatalog.DailyTarget[i]) ready += (~Data.dailyClaimed >> i) & 1;
                }
                int claimed = Data.dailyClaimed;
                return ready + ((claimed & 511) == 511 && (claimed & 512) == 0 ? 1 : 0);
            }
        }
        public bool CanSign => Data.signKey != Today; // src: f54158

        // src: PowerOf f54164 / get_Power f54165. Integer contributions wrap;
        // module quality is multiplied in f32 before ties-to-even rounding.
        public int Power => PowerOf(Data.activeFighter);
        public int PowerOf(int fighter)
        {
            int power = 850;
            for (int part = 0; part < 5; part++)
                power = unchecked(power + (Math.Max(1, PartLevel(fighter, part)) - 1) * 90);
            power = unchecked(power + Math.Max(1, FighterLevel(fighter)) * 150);
            for (int slot = 0; slot < 5; slot++)
            {
                FleetCore core = InPreset(fighter, slot);
                if (core == null) continue;
                float resonance = slot == FleetCatalog.FighterResonance[fighter] ? 1.25f : 1f;
                int q = core.quality;
                float value = resonance * (float)unchecked((q * 90 + 80) * q + 120);
                double rounded = Math.Round((double)value, MidpointRounding.ToEven);
                int contribution = Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
                power = unchecked(power + contribution);
            }
            return power;
        }
    }
}
