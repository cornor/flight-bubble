using System;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54100/f54297. Cost is evaluated before the unsigned kind guard.
        // The original accepts signed amounts and wraps integer arithmetic.
        public bool BuyItem(int kind, int amount)
        {
            int cost = unchecked(amount * ItemPrices[Math.Max(0, Math.Min(kind, 2))]);
            if (unchecked((uint)kind) > 2u || unchecked(amount + Data.items[kind]) > 99 || Data.coins < cost) return false;
            Commit(d => { d.coins = unchecked(d.coins - cost); d.items[kind] = unchecked(amount + d.items[kind]); });
            return true;
        }
        // f54102/f54101/f54298. The source bundle guard is >96, not >97.
        public static int BundlePrice => unchecked((ItemPrices[1] + ItemPrices[0] + ItemPrices[2]) * 14) / 10;
        public bool BuyBundle()
        {
            int cost = BundlePrice;
            if (cost > Data.coins || Data.items[0] > 96 || Data.items[1] > 96 || Data.items[2] > 96) return false;
            Commit(d =>
            {
                d.coins = unchecked(d.coins - cost);
                d.items[2] = unchecked(d.items[2] + 2);
                d.items[0] = unchecked(d.items[0] + 2);
                d.items[1] = unchecked(d.items[1] + 2);
            });
            return true;
        }
        // f54104/f54240 and f54105/f54241. CountAd belongs to ShowAd,
        // not these grants; Refresh precedes each daily limit check.
        public bool AdCoins()
        {
            Refresh();
            if (Data.adCoins >= 5) return false;
            Commit(d => { d.adCoins = unchecked(d.adCoins + 1); d.coins = unchecked(d.coins + 200); });
            return true;
        }
        public bool AdFragments()
        {
            Refresh();
            if (Data.adFragments >= 3) return false;
            Commit(d => { d.adFragments = unchecked(d.adFragments + 1); d.fragments = unchecked(d.fragments + 5); });
            return true;
        }
        // f54106/f54242: no Refresh in this purchase.
        public bool BuyFragments()
        {
            if (Data.coins < 500) return false;
            Commit(d => { d.coins = unchecked(d.coins - 500); d.fragments = unchecked(d.fragments + 10); });
            return true;
        }

        // f54131/f54132/f54136. Signed i64 division/remainder and wrapping add.
        public int OfferWindow => unchecked((int)((Now + 28800L) / 14400L));
        public int OfferSecondsLeft => unchecked(14400 - (int)((Now + 28800L) % 14400L));
        public int OfferSeed => Data.offerSeed != 0 ? Data.offerSeed : 1;
        // f53963. Read seed AFTER the clock callback; all hash math is unsigned.
        private uint OfferHash(int salt)
        {
            long now = Now;
            int seed = Data.offerSeed;
            return Mix32(unchecked((uint)((int)((now + 28800L) / 14400L) * -1640531535
                ^ (salt * 97 ^ (seed != 0 ? seed * 40503 : 40503)))));
        }
        // f53962/f54138/f54139. Offers are deterministic per window and save seed.
        public FleetCore OfferModule
        {
            get
            {
                uint hash = OfferHash(11), roll = hash % 100u;
                int quality = roll < 50u ? 1 : roll < 85u ? 2 : 3;
                int kind = FleetCatalog.DrawableModules[Mix32(hash) % (uint)FleetCatalog.DrawableModules.Length];
                return new FleetCore { id = 0, kind = kind, quality = quality, durability = FleetCatalog.DurabilityMax[quality] };
            }
        }
        public int OfferPartFighter => (int)(OfferHash(23) % 5u);
        public int OfferPartSlot => (int)(Mix32(OfferHash(23)) % 5u);
        // f54133: invalid indices do not call the clock. f53965: they do.
        public bool OfferReady(int index)
        {
            if (unchecked((uint)index) > 3u) return false;
            int window = Data.offerWindow;
            return window != OfferWindow || ((Data.offerClaimed >> index) & 1) == 0;
        }
        public int OfferAdsWatched(int index)
        {
            int saved = Data.offerWindow, window = OfferWindow;
            return unchecked((uint)index) > 3u || saved != window ? 0 : (Data.offerProgress >> (index << 2)) & 15;
        }
        public int OfferAdsNeeded(int index) => index == 1 ? 2 : index == 2 ? OfferModule.quality > 2 ? 2 : 1 : 1; // f53964

        // f53961 + captured mutation f54267. null = unavailable/full, empty =
        // progress saved, nonempty = granted. Keep source clock call order.
        public string ClaimOffer(int index)
        {
            Refresh();
            if (!OfferReady(index)) return null;
            int window = OfferWindow;
            int needed = OfferAdsNeeded(index);
            int watched = unchecked(OfferAdsWatched(index) + 1);
            FleetCore module = OfferModule;
            int fighter = OfferPartFighter, slot = OfferPartSlot, nextCore = Data.nextCore;
            if (watched >= needed && index == 2 && Data.cores.Count > 119) return null;
            bool complete = needed <= watched;
            Commit(d =>
            {
                if (d.offerWindow != window) { d.offerClaimed = 0; d.offerProgress = 0; d.offerWindow = window; }
                d.offerProgress = (d.offerProgress & ~(15 << (index << 2))) | (Math.Min(15, watched) << (index << 2));
                if (!complete) return;
                d.offerClaimed |= 1 << index;
                if (index <= 1)
                {
                    long until = d.unlimitedUntil, now = Now;
                    d.unlimitedUntil = unchecked(Math.Max(until, now) + (long)FleetCatalog.OfferUnlimitedMinutes[index] * 60L);
                }
                else if (index == 2)
                {
                    d.cores.Add(new FleetCore { id = nextCore, kind = module.kind, quality = module.quality,
                        durability = FleetCatalog.DurabilityMax[module.quality] });
                    d.nextCore = unchecked(nextCore + 1);
                }
                else
                {
                    int part = slot + fighter * 5;
                    for (int copy = 0; copy < 3; copy++)
                        if (d.parts[part] == 0) d.parts[part] = 1;
                        else d.spares[part] = unchecked(d.spares[part] + 1);
                }
            });
            if (!complete) return String.Empty;
            if (index <= 1) return "无限体力 " + FleetCatalog.OfferUnlimitedMinutes[index] + " 分钟";
            if (index == 2) return FleetCatalog.QualityNames[module.quality] + "模块 " + FleetCatalog.Modules[module.kind].name;
            return FleetCatalog.FighterNames[fighter] + " · " + FleetCatalog.SlotNames[slot] + " ×3";
        }
    }
}
