using System;
using System.Collections.Generic;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        internal bool streakPrompt, streakRevive; // f54860, +104 / +116
        private Action streakAction; // +108
        private string streakWhat = ""; // f54860 +112

        // src: DrawEnergySheet f54579. The two UnlimitedEnergy reads and the
        // later countdown reads are kept separate, as in the original renderer.
        private void DrawEnergySheet(PixelSurface s)
        {
            if (!energyOpen) return;
            Backdrop(s, .82f);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float eased = OutBack(OriginalMath.Binary32(Since(504) / .3f)), alpha = Clamp01(OriginalMath.Binary32(eased * 1.3f));
            float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - eased)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -186f)));
            Card(s, 60f, y, 420f, 372f, uiGreen, alpha);
            Title(s, Economy.Energy <= 0 && !Economy.SortieFree ? "体力不足" : "体力", 270f, OriginalMath.Binary32(y + 18f), 2.2f, Color.white, uiInk);
            Button(s, 503, 420f, OriginalMath.Binary32(y + 12f), 48f, 40f, "X", Btn.Ghost, 2f);
            float centerY = OriginalMath.Binary32(y + 106f);
            EnergyIcon(s, 188f, centerY, 64f, Economy.UnlimitedEnergy);
            if (Economy.UnlimitedEnergy)
            {
                Title(s, "无限", 300f, OriginalMath.Binary32(centerY + -18f), 2.2f, uiGoldHi, uiInk);
                Label(s, "剩余 " + Clock(Economy.UnlimitedSeconds), 300f, OriginalMath.Binary32(centerY + 18f), 1.2f, uiGold);
            }
            else
            {
                string text = Economy.Energy.ToString() + " / " + 5.ToString();
                float width = PixelSurface.Measure(text, 3f);
                bool positive = Economy.Energy > 0;
                s.Text(text, OriginalMath.Binary32(300f - OriginalMath.Binary32(width * .5f)), OriginalMath.Binary32(centerY + -22f), 3f,
                    new Color(1f, positive ? 1f : .55f, positive ? 1f : .5f, 1f), false);
                int seconds = Economy.EnergyNextSeconds;
                Label(s, seconds > 0 ? "下一点 " + Clock(seconds) : "体力已满", 300f, OriginalMath.Binary32(centerY + 14f), 1.15f, uiDim);
            }
            float descriptionY = OriginalMath.Binary32(y + 168f);
            Label(s, "每 15 分钟恢复 1 点 · 每次出击消耗 1 点", 270f, descriptionY, 1.05f, uiText);
            string streakText = Economy.Streak > 0 ? "当前 " + Economy.Streak.ToString() + " 连胜：下一局出击免体力" : "连胜中继续出击免体力";
            Label(s, streakText, 270f, OriginalMath.Binary32(descriptionY + 26f), 1.05f, Economy.Streak > 0 ? uiGoldHi : uiDim);
            Button(s, 501, 84f, OriginalMath.Binary32(y + 232f), 372f, 54f, "看广告 体力 +" + 5.ToString(), Btn.Green, 1.6f, true, 0, true);
            int coins = Economy.Data.coins;
            Button(s, 502, 84f, OriginalMath.Binary32(y + 296f), 372f, 50f, 400.ToString() + " 金币 · 体力 +" + 5.ToString(),
                coins <= 399 ? Btn.Secondary : Btn.Primary, 1.45f, coins > 399, 1);
            s.MapLayout = mapped;
        }
        // src: EnergyGained f54580. Clear the continuation before invoking it.
        private void EnergyGained(int amount)
        {
            audioChip.Play("reward");
            FloatUp("体力 +" + amount.ToString(), 270f, OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -150f), uiGreen);
            energyOpen = false;
            Action then = energyThen;
            energyThen = null;
            if (then != null && Economy.CanSortie) then();
        }
        // src: OnEnergyUi f54586 / ad reward f54863.
        private bool OnEnergyUi(int id)
        {
            switch (id)
            {
                case 500: OpenEnergy(null); return true;
                case 503: energyThen = null; energyOpen = false; return true;
                case 501: ShowAd("energy", () => EnergyGained(Economy.AddEnergy(5))); return true;
                case 502:
                    if (Economy.BuyEnergy()) EnergyGained(5);
                    else { audioChip.Play("deny"); Toast("金币不足", uiRed); }
                    return true;
                case 511:
                    streakAction = null;
                    streakPrompt = false;
                    if (streakRevive && Battle != null && Battle.State == Phase.ReviveOffer)
                        ShowAd("revive", Rescue);
                    return true;
                case 510:
                    streakPrompt = false;
                    Action then = streakAction;
                    streakAction = null;
                    int lost = Economy.BreakStreak();
                    if (lost > 0) Toast(lost.ToString() + " 连胜已中断", new Color(1f, .55f, .3f, 1f));
                    then?.Invoke();
                    return true;
                case 530: brokenNotice = false; brokenModules = new List<string>(); return true;
                default: return false;
            }
        }
    }
}
