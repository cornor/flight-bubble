using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int resultTick; // original instance +60, zero-initialized

        // IL2CPP Mathf.RoundToInt in f54517: round the binary32 input to even,
        // then preserve the original invalid/out-of-range conversion sentinel.
        internal static int ResultRound(float value)
        {
            double rounded = Math.Round((double)value, MidpointRounding.ToEven);
            return Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
        }

        // f54517. Full win/loss panel. Rewards are already committed by the
        // original frame loop; rendering animates their display, never grants again.
        private void DrawMissionResult(PixelSurface s)
        {
            Phase state = Battle.State;
            bool won = state == Phase.Won;
            float age = OriginalMath.Binary32(feedback.EndAge + -.48f);
            Backdrop(s, OriginalMath.WasmMin(OriginalMath.Binary32(age * 3f), .84f));
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float baseY = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Top + OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(ScreenLayout.Height - ScreenLayout.Top)) - ScreenLayout.Bottom)) * .5f)) + -300f);
            float appear = OutBack(OriginalMath.Binary32(age / .4f)), alpha = Clamp01(OriginalMath.Binary32(age / .2f));
            float rayY = OriginalMath.Binary32(baseY + 90f), spin = OriginalMath.Binary32((won ? 18f : 6f) * age);
            for (int n = 0; n < 12; n++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(spin + OriginalMath.Binary32((float)(n * 30)))) * .01745329238474369f);
                s.GlowLine(new Vector2(270f, rayY), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * 420f) + 270f),
                    OriginalMath.Binary32(rayY + OriginalMath.Binary32(OriginalMath.Sin(angle) * 420f))), 70f,
                    new Color(1f, won ? .78f : .3f, won ? .3f : .2f, OriginalMath.Binary32(alpha * (won ? .1f : .05f))));
            }
            float width = appear < 0f ? 374f : OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(appear, 1f) * .15f) + .85f)) * 440f);
            float half = OriginalMath.Binary32(width * .5f), y = OriginalMath.Binary32(baseY + OriginalMath.Binary32((OriginalMath.Binary32(1f - appear)) * 30f)), left = OriginalMath.Binary32(270f - half);
            Color edge = won ? uiGold : uiRed;
            Plate(s, left, OriginalMath.Binary32(y + 40f), width, 560f, uiPanelHi, uiPanel, A(edge, .9f), 7f, alpha);
            Plate(s, 120f, OriginalMath.Binary32(y + 12f), 300f, 58f,
                new Color(won ? 1f : .8f, won ? .8f : .2f, won ? .3f : .18f, alpha),
                new Color(won ? .8f : .45f, won ? .45f : .06f, won ? .08f : .06f, alpha), A(uiInk, alpha), 10f);
            // WAT: 1 - (age + age), not the decompiler's flattened expression.
            float shake = won ? 0f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(age * 40f)) * OriginalMath.WasmMax(OriginalMath.Binary32(1f - (OriginalMath.Binary32(age + age))), 0f)) * 4f);
            Title(s, won ? "任务完成" : "任务失败", OriginalMath.Binary32(shake + 270f), OriginalMath.Binary32(y + 22f), 2.6f,
                new Color(1f, 1f, 1f, alpha), A(won ? uiBrown : new Color(.3f, 0f, 0f, 1f), alpha));
            Label(s, "第 " + unchecked(levelIndex + 1).ToString("D2") + " 关", 270f, OriginalMath.Binary32(y + 80f), 1.3f, A(uiDim, alpha));
            float contentX = OriginalMath.Binary32(left + 30f), contentWidth = OriginalMath.Binary32(width + -60f);
            s.Art(won ? 1 : 3, contentX, OriginalMath.Binary32(y + 104f), contentWidth, 164f, 0);
            s.Corners(OriginalMath.Binary32(left + 28f), OriginalMath.Binary32(y + 102f), OriginalMath.Binary32(width + -56f), 168f, A(edge, alpha), 12, 2);
            float row = OriginalMath.Binary32(y + 284f), homeHeight, homeOffset;
            string homeLabel;
            if (won && LastReward != null)
            {
                Label(s, "战利品", 270f, row, 1.3f, A(uiGold, alpha));
                float gap = OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure("战利品", 1.3f) * .5f) + 12f);
                float lineX = OriginalMath.Binary32(left + 36f), lineY = OriginalMath.Binary32(row + 9f);
                s.Rect(lineX, lineY, OriginalMath.Binary32(OriginalMath.Binary32(270f - gap) - lineX), 1f, A(uiGold, OriginalMath.Binary32(alpha * .45f)));
                float right = OriginalMath.Binary32(gap + 270f);
                s.Rect(right, lineY, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width + left) + -36f) - right), 1f, A(uiGold, OriginalMath.Binary32(alpha * .45f)));
                float coinAge = OriginalMath.Binary32(age + -.45f), fragmentAge = OriginalMath.Binary32(age + -.75f);
                float coinProgress = Clamp01(OriginalMath.Binary32(coinAge / .8f)), fragmentProgress = Clamp01(OriginalMath.Binary32(fragmentAge / .8f));
                int coins = ResultRound(OriginalMath.Binary32(OutCubic(coinProgress) * OriginalMath.Binary32((float)LastReward.coins)));
                int fragments = ResultRound(OriginalMath.Binary32(OutCubic(fragmentProgress) * OriginalMath.Binary32((float)LastReward.fragments)));
                if ((coinProgress < 1f && coinProgress > 0f) || (fragmentProgress > 0f && fragmentProgress < 1f))
                {
                    int tick = CommsInt(OriginalMath.Binary32(age * 14f));
                    if (tick != resultTick) { resultTick = tick; audioChip.Play("tick"); }
                }
                row = OriginalMath.Binary32(row + 30f);
                string tag = LastReward.streakPercent > 0 ? "连胜 +" + LastReward.streakPercent.ToString() + "%" : null;
                RewardTile(s, contentX, row, OriginalMath.Binary32(half + -40f), 1, "+" + coins.ToString(), tag, coinAge, alpha);
                RewardTile(s, 280f, row, OriginalMath.Binary32(half + -40f), 2, "+" + fragments.ToString(), tag, fragmentAge, alpha);
                row = OriginalMath.Binary32(row + 84f);
                DrawStreakMeter(s, 270f, row, LastReward.streak, alpha, OriginalMath.Binary32(age + -.9f));
                Label(s, "最高连击 " + Battle.BestCombo.ToString() + "   用时 " + ResultRound(Battle.Time).ToString() + " 秒",
                    270f, OriginalMath.Binary32(row + 34f), 1.05f, A(uiDim, alpha));
                if (LastReward.zoneComplete >= 0)
                {
                    float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 5f)) * .5f) + .5f);
                    Plate(s, OriginalMath.Binary32(left + 40f), OriginalMath.Binary32(row + 54f), OriginalMath.Binary32(width + -80f), 28f,
                        SourceColor(4505040778930474189UL, 1025758986u, alpha),
                        SourceColor(4453159313446818284UL, 1008981770u, alpha), A(Mix(uiGold, Color.white, OriginalMath.Binary32(pulse * .4f)), alpha), 6f);
                    s.TextF(WarZones.Names[LastReward.zoneComplete] + " 收复！战区奖励已送达", 270f, OriginalMath.Binary32(row + 58f), 1f, A(uiGoldHi, alpha), 0);
                }
                else if (brokenModules.Count > 0)
                    s.TextF("装备损坏：" + String.Join("、", brokenModules), 270f, OriginalMath.Binary32(row + 58f), .95f,
                        SourceColor(4543231306015703040UL, 1055286886u, alpha), 0);
                Button(s, 40, contentX, OriginalMath.Binary32(y + 478f), contentWidth, 62f,
                    Levels.Length > unchecked(levelIndex + 1) ? "下一关" : "返回主页", Btn.Primary, 2.2f);
                homeHeight = 40f; homeOffset = 550f;
                homeLabel = levelIndex == 1 && !Economy.Data.growthTutorial ? "回港升级" : "返回主页";
            }
            else
            {
                int percent = CommsInt(OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(Battle.Progress * 100f))));
                Label(s, "已清除 " + percent.ToString() + "%", 270f, row, 1.8f, A(uiText, alpha));
                float progress = Battle.Progress;
                s.Rect(OriginalMath.Binary32(left + 50f), OriginalMath.Binary32(row + 40f), OriginalMath.Binary32(width + -100f), 12f, SourceColor(4428659729042233098UL, 1036831949u, alpha));
                s.Rect(OriginalMath.Binary32(left + 52f), OriginalMath.Binary32(row + 42f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(width + -104f)) * progress) * OutCubic(OriginalMath.Binary32(age / .8f))), 8f, A(uiRed, alpha));
                Label(s, "升级战机可提升射速与击退", 270f, OriginalMath.Binary32(row + 72f), 1.2f, A(uiDim, alpha));
                if (brokenModules.Count > 0)
                    s.TextF("装备损坏：" + String.Join("、", brokenModules), 270f, OriginalMath.Binary32(row + 100f), .95f,
                        SourceColor(4543231306015703040UL, 1055286886u, alpha), 0);
                Button(s, 40, contentX, OriginalMath.Binary32(y + 430f), contentWidth, 64f, "再试一次", Btn.Primary, 2.2f);
                homeHeight = 42f; homeOffset = 510f; homeLabel = "返回主页";
            }
            Button(s, 41, 180f, OriginalMath.Binary32(y + homeOffset), 180f, homeHeight, homeLabel, Btn.Ghost, 1.2f);
            if (age > .3f && dialogue.Length > 0)
            {
                float safe = OriginalMath.Binary32(ScreenLayout.Top + 58f), above = OriginalMath.Binary32(y + -100f);
                DrawDialogue(s, 50f, safe > above ? safe : above, 440f, lesson != null);
            }
            s.MapLayout = mapped;
        }
    }
}
