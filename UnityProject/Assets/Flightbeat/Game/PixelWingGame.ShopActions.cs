using Flightbeat.Economy;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int offerFlash = -1, buyFlash = -1; // f54860, paired store at +228/+232.

        // f54648; called at its original position in the deck dispatch chain.
        private bool OnShopUi(int id)
        {
            if (unchecked((uint)(id - 210)) < 3u)
            {
                int kind = id - 210;
                if (Economy.BuyItem(kind, 1))
                {
                    buyFlash = kind; MarkUi(217); audioChip.Play("buy");
                    FloatUp("+1 " + FleetCatalog.ItemNames[kind], 270f, ScreenLayout.SafeBottom - 96f - 160f, uiGold);
                }
                else { audioChip.Play("deny"); Toast(Economy.Data.items[kind] > 98 ? "库存已满" : "金币不足", uiRed); }
                return true;
            }
            if (unchecked((uint)(id - 540)) < 4u)
            {
                int offer = id - 540;
                if (!Economy.OfferReady(offer)) { Toast("本轮已领取，稍后刷新", uiDim); return true; }
                ShowAd("offer", () =>
                {
                    // f53960: no animation mark until the full reward is granted.
                    string result = Economy.ClaimOffer(offer);
                    if (result == null) { Toast("模块库存已满", uiRed); return; }
                    if (result.Length == 0)
                    {
                        audioChip.Play("tick");
                        Toast("再看 " + (Economy.OfferAdsNeeded(offer) - Economy.OfferAdsWatched(offer)) + " 次广告即可领取", uiGoldHi);
                        return;
                    }
                    offerFlash = offer; MarkUi(545); audioChip.Play("legend"); Toast("获得 " + result, uiGold);
                });
                return true;
            }
            switch (id)
            {
                case 213:
                    if (Economy.BuyBundle())
                    {
                        buyFlash = 3; MarkUi(217); audioChip.Play("buy");
                        FloatUp("道具各 +2", 270f, ScreenLayout.SafeBottom - 96f - 160f,
                            SourceColor(4543160662393618432UL, 4575657222471474397UL));
                    }
                    else { audioChip.Play("deny"); Toast("金币不足", uiRed); }
                    return true;
                case 216:
                    if (Economy.BuyFragments())
                    {
                        buyFlash = 4; MarkUi(217); audioChip.Play("buy");
                        FloatUp("+10 碎片", 270f, ScreenLayout.SafeBottom - 96f - 160f, uiCyan);
                    }
                    else { audioChip.Play("deny"); Toast("金币不足", uiRed); }
                    return true;
                case 214:
                    if (Economy.Data.adCoins >= 5) { Toast("今日次数已用完", uiDim); return true; }
                    // f54865.
                    ShowAd("coins", () => { if (Economy.AdCoins()) FloatUp("+200 金币", 150f, ScreenLayout.Top + 2f + 60f, uiGold); });
                    return true;
                case 215:
                    if (Economy.Data.adFragments >= 3) { Toast("今日次数已用完", uiDim); return true; }
                    // f54866.
                    ShowAd("fragments", () => { if (Economy.AdFragments()) FloatUp("+5 碎片", 330f, ScreenLayout.Top + 2f + 60f, uiCyan); });
                    return true;
                default: return false;
            }
        }
    }
}
