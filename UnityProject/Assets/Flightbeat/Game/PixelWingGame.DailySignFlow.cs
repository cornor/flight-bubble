using Flightbeat.Economy;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private float signedAt = -10f; // f54860 +208
        private int signedIndex = -1; // +212
        // f54649. Preserve id-based dispatch, including the incomplete ad-task
        // shortcut to the shop and the source Test-ad callbacks.
        private bool OnOverlayUi(int id)
        {
            if (id == 239) { ClaimChest(false); return true; }
            if (id == 529) { ShowAd("daily-chest", () => ClaimChest(true)); return true; } // f54864
            if (unchecked((uint)(id - 230)) < 9u || unchecked((uint)(id - 520)) < 9u)
            {
                int task = id > 519 ? id - 520 : id - 230;
                if (id > 519) ShowAd("daily", () => ClaimTask(task, true)); // f53957
                else if (task == 8 && !Economy.DailyDone(8)) { deckOverlay = DeckOverlay.None; GoPage(DeckPage.Shop); }
                else ClaimTask(task, false);
                return true;
            }
            if (id == 240)
            {
                int index = Economy.SignIndex;
                if (Economy.Sign() == null) return true;
                signedIndex = index; signedAt = ui; audioChip.Play("reward");
                int kind = FleetCatalog.SignKind[index], amount = FleetCatalog.SignAmount[index];
                string text = kind == 5 ? "获得 稀有模块" : kind == 0 ? "+" + amount + " 金币" : kind == 1 ? "+" + amount + " 碎片"
                    : FleetCatalog.ItemNames[kind - 2] + " ×" + amount;
                FloatUp(text, 270f, ScreenLayout.Height * .5f - 140f, uiGold); return true;
            }
            return OnSupplyUi(id);
        }
        // f53953.
        private void ClaimTask(int task, bool doubled)
        {
            if (!Economy.ClaimDaily(task, doubled)) return;
            audioChip.Play("reward"); int multiplier = doubled ? 2 : 1; bool coins = FleetCatalog.DailyCoins[task] > 0;
            int amount = unchecked(multiplier * (coins ? FleetCatalog.DailyCoins[task] : FleetCatalog.DailyFragments[task]));
            FloatUp("+" + amount + (coins ? " 金币" : " 碎片"), 270f, ScreenLayout.Height * .5f - 120f, coins ? uiGold : uiCyan);
        }
        // f54659.
        private void ClaimChest(bool doubled)
        {
            int multiplier = doubled ? 2 : 1; if (!Economy.ClaimDailyChest(doubled)) return;
            audioChip.Play("legend");
            FloatUp("+" + (multiplier * 400) + " 金币  +" + (multiplier * 20) + " 碎片  炸弹 ×" + multiplier,
                270f, ScreenLayout.Height * .5f - 160f, uiGold);
        }
    }
}
