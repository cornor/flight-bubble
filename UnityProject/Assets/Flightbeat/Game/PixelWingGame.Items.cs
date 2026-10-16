using System;
using Flightbeat.Economy;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54715 / f53974. Advert rewards grant first; selecting the tool itself
        // does not consume inventory. ShowAd owns the original stale-context guard.
        private void TakeItem(int kind, Action use)
        {
            if (Economy.Data.items[kind] > 0) { use(); return; }
            ShowAd("prop-" + kind.ToString(), () =>
            {
                if (!Economy.GrantItem(kind, 1)) return;
                Toast("获得 " + FleetCatalog.ItemNames[kind] + " ×1", uiGold);
                use();
            });
        }
        // f54847. Only successful economy consumption contributes to ItemsUsed.
        private void ConsumeItem(int kind)
        {
            if (Economy.Consume(kind)) ItemsUsed = unchecked(ItemsUsed + 1);
        }
        // f54812 and closures f54873/f54874. Bomb selection delegates to f54786.
        private void UseProp(int kind)
        {
            if (!evidenceControlled && Battle.Level.lesson > 0) return;
            if (kind == 2)
            {
                if (selection == 2) CancelSuperAim();
                else BeginSuperAim();
                return;
            }
            if (kind == 1 && selection == 1) { selection = 0; SetPaused(false); return; }
            if (Battle.State != Phase.Playing) { Battle.Message("先点击战机出击"); audioChip.Play("deny"); return; }
            if (Battle.TrainingHold) { audioChip.Play("deny"); return; }
            if (kind == 0)
                TakeItem(0, () => { if (Battle.Shuffle()) ConsumeItem(0); else audioChip.Play("deny"); });
            else TakeItem(1, () => { selection = 1; SetPaused(true); });
        }
    }
}
