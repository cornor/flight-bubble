using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54597: source row sizing, staggered entrance, chest and local-day countdown.
        private void DrawDaily(PixelSurface s)
        {
            Backdrop(s, .8f);
            float enter = OutBack(OriginalMath.Binary32(Since(230) / .32f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float rowHeight = OriginalMath.WasmMin(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height - ScreenLayout.Top) - 200f)) / 11.4f), 62f);
            float height = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(rowHeight * 9f) + 150f) + 34f), minimum = OriginalMath.Binary32(ScreenLayout.Top + 8f), centered = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - OriginalMath.Binary32(height * .5f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (centered < minimum ? minimum : centered));
            Plate(s, 40f, top, 460f, height, uiPanelHi, uiPanel, uiGreen, 7f, alpha);
            Title(s, "每日任务", 270f, OriginalMath.Binary32(top + 18f), 2.2f, Color.white, uiInk);
            Button(s, 298, 440f, OriginalMath.Binary32(top + 12f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            int claimed = Economy.DailyClaimedCount; bool chestReady = Economy.DailyChestReady, chestClaimed = Economy.DailyClaimed(9);
            float y = OriginalMath.Binary32(top + 70f);
            Color edge = chestReady ? uiGold : uiEdge; edge.a = OriginalMath.Binary32(OriginalMath.Binary32(alpha * .85f) * edge.a);
            Plate(s, 56f, y, 428f, 64f, SourceColor(4453159313442456207UL, 1022739087u, alpha),
                SourceColor(4405601299476565197UL, 1008981770u, alpha), edge, 8f);
            // f54597 reads uiGoldHi at static +184 for the progress caption.
            Label(s, "今日进度", 72f, OriginalMath.Binary32(y + 10f), 1.2f, A(uiGoldHi, alpha), -1);
            s.TextF(claimed + "/9", OriginalMath.Binary32(PixelSurface.Measure("今日进度 ", 1.2f) + 72f), OriginalMath.Binary32(y + 10f), 1.2f, new Color(1f, 1f, 1f, alpha), -1);
            for (int i = 0; i < 9; i++)
            {
                float x = OriginalMath.Binary32((float)(i * 26 + 72));
                s.Rect(x, OriginalMath.Binary32(y + 40f), 22f, 10f, SourceColor(4417130516401346314UL, 1034147594u, alpha));
                if (claimed > i) s.Rect(OriginalMath.Binary32(x + 1f), OriginalMath.Binary32(y + 41f), 20f, 8f, A(uiGold, alpha));
            }
            float scale = 1f;
            if (chestReady)
            {
                scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 6f)) * .08f) + 1f);
                Color glow = uiGold; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(glow.a * alpha) * .45f);
                s.Glow(332f, OriginalMath.Binary32(y + 32f), 90f, 80f, glow, 0f);
            }
            float shade = chestClaimed ? .5f : 1f, half = OriginalMath.Binary32(scale * 20f), size = OriginalMath.Binary32(scale * 40f);
            s.Sprite(15, OriginalMath.Binary32(332f - half), OriginalMath.Binary32(OriginalMath.Binary32(y + 32f) - half), size, size, new Color(shade, shade, shade, alpha), 0f);
            if (chestClaimed) Label(s, "已开启", 420f, OriginalMath.Binary32(y + 22f), 1.2f, A(uiDim, alpha));
            else if (chestReady)
            {
                Button(s, 239, 366f, OriginalMath.Binary32(y + 6f), 110f, 26f, "开启宝箱", Btn.Primary, 1.05f, true);
                Button(s, 529, 366f, OriginalMath.Binary32(y + 36f), 110f, 24f, "双倍", Btn.Green, .95f, true, 0, true);
            }
            else
            {
                s.TextF("全部领取后开启", 420f, OriginalMath.Binary32(y + 12f), .95f, A(uiDim, alpha), 0);
                Color gold = uiGold; gold.a = OriginalMath.Binary32(OriginalMath.Binary32(gold.a * alpha) * .9f);
                s.TextF("金币·碎片·炸弹", 420f, OriginalMath.Binary32(y + 36f), .95f, gold, 0);
            }
            float cardHeight = OriginalMath.Binary32(rowHeight - 4f), rowsTop = OriginalMath.Binary32(top + 146f);
            for (int task = 0; task < 9; task++)
            {
                float appear = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(Since(230) - .08f) + OriginalMath.Binary32(OriginalMath.Binary32((float)task) * -.045f))) * 4f)); if (appear <= 0f) continue;
                float remaining = OriginalMath.Binary32(1f - Clamp01(appear)), slide = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(remaining * (OriginalMath.Binary32(remaining * remaining))) - 1f) + 1f)) * 30f);
                int target = FleetCatalog.DailyTarget[task], progress = Economy.DailyProgress(task), shown = progress < target ? progress : target;
                bool done = progress >= target, taken = Economy.DailyClaimed(task);
                float rowY = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(rowHeight * OriginalMath.Binary32((float)task))), x = OriginalMath.Binary32(slide + 56f), fade = OriginalMath.Binary32(alpha * appear);
                Color rowEdge = !taken && done ? uiGreen : uiEdge; rowEdge.a = OriginalMath.Binary32(OriginalMath.Binary32(fade * .8f) * rowEdge.a);
                Plate(s, x, rowY, 428f, cardHeight, SourceColor(4477658893514493133UL, 1045220557u, fade),
                    SourceColor(4435865490164012810UL, 1036831949u, fade), rowEdge, 8f);
                if (taken) s.Rect(OriginalMath.Binary32(x + 3f), OriginalMath.Binary32(rowY + 3f), 422f, OriginalMath.Binary32(cardHeight - 6f), new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .28f)));
                float left = OriginalMath.Binary32(x + 14f), bottom = OriginalMath.Binary32(cardHeight + rowY), barY = OriginalMath.Binary32(bottom - 18f);
                Label(s, FleetCatalog.DailyNames[task], left, OriginalMath.Binary32(rowY + 9f), 1.2f, A(taken ? uiDim : Color.white, fade), -1);
                s.Rect(left, barY, 150f, 10f, SourceColor(4417130516401346314UL, 1034147594u, fade));
                s.Rect(OriginalMath.Binary32(x + 15f), OriginalMath.Binary32(barY + 1f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)shown) * 148f) / OriginalMath.Binary32((float)target)), 8f, A(done ? uiGreen : uiCyan, fade));
                s.Text(shown + "/" + target, OriginalMath.Binary32(OriginalMath.Binary32(left + 150f) + 8f), OriginalMath.Binary32(barY + 1f), 1f, A(done ? uiGreen : uiDim, fade), false);
                bool coins = FleetCatalog.DailyCoins[task] > 0; int reward = coins ? FleetCatalog.DailyCoins[task] : FleetCatalog.DailyFragments[task];
                string rewardText = "+" + reward; float rewardCenter = OriginalMath.Binary32(x + 352f);
                float rewardX = OriginalMath.Binary32(rewardCenter + OriginalMath.Binary32((OriginalMath.Binary32(PixelSurface.Measure(rewardText, 2f) + 26f)) * -.5f));
                DeckIcon(s, coins ? 1 : 2, OriginalMath.Binary32(rewardX + 11f), OriginalMath.Binary32(rowY + 14f), 22f, new Color(1f, 1f, 1f, fade));
                s.Text(rewardText, OriginalMath.Binary32(rewardX + 26f), OriginalMath.Binary32(rowY + 7f), 2f, A(coins ? uiGold : uiCyan, fade), false);
                if (taken)
                {
                    DrawCheck(s, OriginalMath.Binary32(rewardCenter - 30f), OriginalMath.Binary32(bottom - 16f), OriginalMath.Binary32(fade * .9f));
                    Label(s, "已领取", OriginalMath.Binary32(rewardCenter + 10f), OriginalMath.Binary32(bottom - 24f), 1.05f, A(uiDim, fade));
                }
                else if (done)
                {
                    Button(s, task + 230, OriginalMath.Binary32(rewardCenter - 74f), OriginalMath.Binary32(bottom - 30f), 68f, 26f, "领取", Btn.Green, 1.05f, true);
                    Button(s, task + 520, OriginalMath.Binary32(rewardCenter - 2f), OriginalMath.Binary32(bottom - 30f), 78f, 26f, "×2", Btn.Primary, 1.05f, true, 0, true);
                }
                else Button(s, task + 230, OriginalMath.Binary32(rewardCenter - 52f), OriginalMath.Binary32(bottom - 30f), 104f, 26f, task == 8 ? "去看广告" : "未完成", task == 8 ? Btn.Secondary : Btn.Ghost, 1.05f, task == 8);
            }
            uint seconds = unchecked(86400u - (uint)(unchecked(Economy.Now + 28800L) % 86400L));
            long hours = seconds / 3600u, minutes = (seconds / 60u & 65535u) % 60u;
            Label(s, "刷新倒计时 " + hours.ToString("D2") + ":" + minutes.ToString("D2"), 270f, OriginalMath.Binary32(OriginalMath.Binary32(height + top) - 28f), 1.05f, A(uiDim, alpha));
        }
        // f54557.
        private void DrawCheck(PixelSurface s, float cx, float cy, float alpha)
        {
            Color color = A(uiGreen, alpha);
            s.Segment(new Vector2(OriginalMath.Binary32(cx - 12f), cy), new Vector2(OriginalMath.Binary32(cx - 3f), OriginalMath.Binary32(cy + 9f)), 5f, color);
            s.Segment(new Vector2(OriginalMath.Binary32(cx - 3f), OriginalMath.Binary32(cy + 9f)), new Vector2(OriginalMath.Binary32(cx + 14f), OriginalMath.Binary32(cy - 10f)), 5f, color);
        }
    }
}
