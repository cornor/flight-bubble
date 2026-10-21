#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Home settings → "重置关卡进度". 2026-10-17: one tap clears level progress at once (no confirmation):
        // cleared levels, the unlocked level and any unsettled attempt. Coins, fragments, items, tutorials and
        // settings stay as they are.
        // 2026-10-21 18:11 (user): two taps: the first arms it for 3 s (「再点一次确认清空」), the second clears.
        const float ResetDoneSeconds = 1.6f, ResetArmSeconds = 3f;
        float resetDoneAt = -100f, resetArmedAt = -100f;
        string ResetProgressLabel => ui - resetDoneAt <= ResetDoneSeconds ? "已清空进度"
            : ui - resetArmedAt <= ResetArmSeconds ? "再点一次确认清空" : "重置关卡进度";
        void DisarmResetProgress() { resetDoneAt = -100f; resetArmedAt = -100f; }
        void TapResetProgress()
        {
            if (ui - resetArmedAt > ResetArmSeconds) { resetArmedAt = ui; audioChip.Play("ui"); return; }
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
