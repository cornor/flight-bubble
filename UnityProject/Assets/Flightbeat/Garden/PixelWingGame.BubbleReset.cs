#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Home settings → "重置关卡进度". The first tap arms it, a second tap within three seconds
        // clears level progress only: cleared levels, the unlocked level and any unsettled attempt.
        // Coins, fragments, items, tutorials and settings stay as they are.
        const float ResetConfirmSeconds = 3f, ResetDoneSeconds = 1.6f;
        float resetArmedAt = -100f, resetDoneAt = -100f;
        bool ResetArmed => ui - resetArmedAt <= ResetConfirmSeconds;
        string ResetProgressLabel => ResetArmed ? "再点一次确认"
            : ui - resetDoneAt <= ResetDoneSeconds ? "已清空进度" : "重置关卡进度";
        void DisarmResetProgress() { resetArmedAt = -100f; resetDoneAt = -100f; }
        void TapResetProgress()
        {
            if (!ResetArmed)
            {
                resetArmedAt = ui; resetDoneAt = -100f;
                audioChip.Play("ui");
                return;
            }
            resetArmedAt = -100f;
            Economy.Commit(d =>
            {
                d.cleared.Clear();
                d.unlocked = 1;
                d.attempt = string.Empty;
                d.attemptLevel = 0;
            });
            resetDoneAt = ui;
            audioChip.Play("page");
        }
    }
}
#endif
