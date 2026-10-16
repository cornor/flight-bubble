using System;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54151. Queries do not refresh the day; claims refresh before guards.
        public int DailyProgress(int task)
        {
            switch (task)
            {
                case 0: return Data.dayWins; case 1: return Data.dayMerges;
                case 2: return Data.dayKills; case 3: return Data.dayItems;
                case 4: return Data.dayMaxes; case 5: return Data.dayCombo;
                case 6: return Data.dayCleanWins; case 7: return Data.dayFastWins;
                case 8: return Data.dayAds; default: return 0;
            }
        }
        public bool DailyDone(int task) => unchecked((uint)task) <= 8u && DailyProgress(task) >= FleetCatalog.DailyTarget[task]; // f54152
        public bool DailyClaimed(int task) => ((Data.dailyClaimed >> task) & 1) != 0; // f54153, masked shift, no range guard.
        public int DailyClaimedCount // f54154, chest bit excluded.
        {
            get { int count = 0; for (int i = 0; i < 9; i++) count += (Data.dailyClaimed >> i) & 1; return count; }
        }
        public bool DailyChestReady => DailyClaimedCount == 9 && (Data.dailyClaimed & 512) == 0; // f54155
        // f53954/f54270.
        public bool ClaimDaily(int task, bool doubled)
        {
            Refresh(); if (!DailyDone(task) || DailyClaimed(task)) return false;
            int multiplier = doubled ? 2 : 1;
            Commit(d =>
            {
                d.dailyClaimed |= 1 << task;
                d.coins = unchecked(multiplier * FleetCatalog.DailyCoins[task] + d.coins);
                d.fragments = unchecked(d.fragments + multiplier * FleetCatalog.DailyFragments[task]);
            });
            return true;
        }
        // f54157/f54271.
        public bool ClaimDailyChest(bool doubled)
        {
            Refresh(); if (!DailyChestReady) return false; int multiplier = doubled ? 2 : 1;
            Commit(d =>
            {
                d.dailyClaimed |= 512; d.coins = unchecked(d.coins + multiplier * 400);
                d.fragments = unchecked(d.fragments + multiplier * 20); d.items[2] = Math.Min(99, unchecked(multiplier + d.items[2]));
            });
            return true;
        }
        public int SignIndex => Data.signCount % 7; // f54159, signed remainder.
        // f54160/f54272/f54273. All seven rewards consume module RNG. No
        // Refresh or module capacity guard; a non-module reward returns a sentinel.
        public FleetCore Sign()
        {
            if (!CanSign) return null;
            int index = SignIndex, kind = FleetCatalog.SignKind[index], amount = FleetCatalog.SignAmount[index], id = Data.nextCore;
            int module = FleetCatalog.RandomModule(random);
            Commit(d =>
            {
                d.signKey = Today; d.signCount = unchecked(d.signCount + 1);
                if (kind == 0) d.coins = unchecked(amount + d.coins);
                else if (kind == 1) d.fragments = unchecked(amount + d.fragments);
                else if (kind <= 4) d.items[kind - 2] = Math.Min(99, unchecked(amount + d.items[kind - 2]));
                else
                {
                    d.cores.Add(new FleetCore { id = id, kind = module, quality = 2, durability = FleetCatalog.DurabilityMax[2] });
                    d.nextCore = unchecked(id + 1);
                }
            });
            return kind == 5 ? Data.cores.Find(c => c.id == id) ?? new FleetCore() : new FleetCore();
        }
    }
}
