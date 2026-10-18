#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Home settings → "重置关卡进度". 2026-10-17: one tap clears level progress at once (no confirmation):
        // cleared levels, the unlocked level and any unsettled attempt. Coins, fragments, items, tutorials and
        // settings stay as they are.
        const float ResetDoneSeconds = 1.6f;
        float resetDoneAt = -100f;
        string ResetProgressLabel => ui - resetDoneAt <= ResetDoneSeconds ? "已清空进度" : "重置关卡进度";
        void DisarmResetProgress() { resetDoneAt = -100f; }
        void TapResetProgress()
        {
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
