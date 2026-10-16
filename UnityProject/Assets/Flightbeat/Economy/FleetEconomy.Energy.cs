using System;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        public int Streak => Data.streak; // src: f54109
        // src: f54116. Keep the captured deadline before the clock invocation.
        public bool SortieFree
        {
            get { if (Data.streak > 0) return true; long until = Data.unlimitedUntil; return Now < until; }
        }
        // f54121 / f54256. Capture Now before Commit; extend an existing future
        // deadline and multiply signed minutes in i64, without clamping negatives.
        public void GrantUnlimited(int minutes)
        {
            long now = Now;
            Commit(d => d.unlimitedUntil = unchecked(Math.Max(d.unlimitedUntil, now) + (long)minutes * 60L));
        }
        // src: AddEnergy f54119 / mutation f54255. Returns the requested amount,
        // even when the original 999 cap reduces the amount actually credited.
        public int AddEnergy(int amount)
        {
            if (amount <= 0) return 0;
            SettleEnergy();
            Commit(d => d.energy = Math.Min(999, unchecked(amount + d.energy)));
            return amount;
        }
        // src: BuyEnergy f54120 / f54244. Read affordability before settlement.
        public bool BuyEnergy()
        {
            int coins = Data.coins;
            if (coins >= 400)
            {
                SettleEnergy();
                Commit(d => { d.coins = unchecked(d.coins - 400); d.energy = Math.Min(999, unchecked(d.energy + 5)); });
            }
            return coins > 399;
        }
        // src: CountAd f53977 / f54245.
        public void CountAd()
        {
            Refresh();
            Commit(d => d.dayAds = unchecked(d.dayAds + 1));
        }
    }
}
