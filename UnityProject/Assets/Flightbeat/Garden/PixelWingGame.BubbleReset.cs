#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Home settings → "重置关卡进度". 2026-10-17: one tap clears level progress at once (no confirmation):
        // cleared levels, the unlocked level and any unsettled attempt. Coins, fragments, items, tutorials and
        // settings stay as they are.
        // 2026-10-21 18:11 (user): two taps: the first arms it for 3 s (「再点一次确认清空」), the second clears.
        // 2026-10-30 14:32 (user, 界面优化 5: the two-tap arming was not noticed): one tap opens a confirmation -
        // 「确定清空所有关卡进度吗？」 with 取消 / 清空 - drawn on the overlay above the settings dialog.
        const float ResetDoneSeconds = 1.6f;
        const int ResetCancelAction = 870, ResetConfirmAction = 871;
        float resetDoneAt = -100f;
        bool resetConfirm;
        float resetConfirmAt;
        string ResetProgressLabel => ui - resetDoneAt <= ResetDoneSeconds ? "已清空进度" : "重置关卡进度";
        void DisarmResetProgress() { resetDoneAt = -100f; resetConfirm = false; }
        void TapResetProgress()
        {
            if (resetConfirm) return;
            resetConfirm = true; resetConfirmAt = ui;
            audioChip.Play("page");
        }
        // Taps while the confirmation is open (home settings); true when consumed.
        bool ResetConfirmTap(int id)
        {
            if (!resetConfirm) return false;
            if (id == ResetCancelAction) { resetConfirm = false; audioChip.Play("ui"); }
            else if (id == ResetConfirmAction)
            {
                resetConfirm = false;
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
            return true;
        }
        void DrawResetConfirm(PixelSurface s)
        {
            if (!resetConfirm) return;
            bool mapped = s.MapLayout;
            try
            {
                HitBlock(s);
                s.MapLayout = false;
                float t = ui - resetConfirmAt, a = Clamp01(t / .15f), p = OutBack(Clamp01(t / .25f), 1.8f);
                float cy = ScreenLayout.Height * .5f;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.035f, .055f, .16f, .55f * a));
                float w = 400f * p, h = 250f * p, x = 270f - w * .5f, y = cy - h * .5f;
                if (!Asset9(s, "plants/dialog/pause-panel", x, y, w, h, 48f, 32f, A(Color.white, a)))
                    WinRoundRect(s, x, y, w, h, 28f, A(Color.white, a));
                if (p < .9f) return;
                ResultSubtitle(s, "确定清空所有关卡进度吗？", cy - 62f, 24f, ResultNavy, a);
                GardenBarArt.Text(s, "通关记录和图鉴收集都会清空", 270f, cy - 27f, 18f, true, A(Hex(0x6B7A99), a));   // 2026-11-03 10:56 (user: text review): 18 px (was 16).
                ResultPill(s, ResetCancelAction, 92f, cy + 24f, 168f, 64f, "white", "取消");
                ResultPill(s, ResetConfirmAction, 280f, cy + 24f, 168f, 64f, "red", "清空");
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif
