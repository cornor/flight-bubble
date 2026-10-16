using System;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // src: ShowAd f54563 and callbacks f53976/f53978. Capture battle/home
        // identity before pausing; a delayed reward cannot credit a new context.
        internal void ShowAd(string placement, Action reward)
        {
            if (AdService.Busy) return;
            bool wasPaused = paused;
            var originalBattle = Battle;
            bool wasHome = InHome;
            SetPaused(true);
            lastTargetTap = -10f;
            aimDown = false;
            AdService.Show(placement, () =>
            {
                if (!this) return;
                SetPaused(wasPaused);
                if (Battle != originalBattle || wasHome != InHome) return;
                Economy.CountAd();
                reward?.Invoke();
                audioChip.Play("reward");
            }, () =>
            {
                if (!this) return;
                SetPaused(wasPaused);
                Toast("广告未完成，请重试", uiDim);
                audioChip.Play("deny");
            });
        }
    }
}
