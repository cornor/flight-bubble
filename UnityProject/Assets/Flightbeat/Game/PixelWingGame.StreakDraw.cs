using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54583. Disable the layout map after the backdrop; retain the source
        // screen-space button/blocker order and separately animated title plate.
        private void DrawStreakPrompt(PixelSurface s)
        {
            if (!streakPrompt) return;
            Backdrop(s, .86f);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float appear = OutBack(OriginalMath.Binary32(Since(512) / .3f)), alpha = Clamp01(OriginalMath.Binary32(appear * 1.3f));
            float shake = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(Since(512) * 40f)) * OriginalMath.WasmMax(OriginalMath.Binary32(1f - OriginalMath.Binary32(Since(512) * 2.5f)), 0f));
            float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - appear)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -200f))), dx = OriginalMath.Binary32(shake * 5f);
            int streak = Economy.Streak, percent = FleetCatalog.StreakPercent(streak);
            Card(s, 50f, y, 440f, 400f, SourceColor(4543231306015703040UL, 4575657222453644493UL), alpha);
            Plate(s, OriginalMath.Binary32(dx + 130f), OriginalMath.Binary32(y + -18f), 280f, 56f, SourceColor(4532422664333033472UL, 1041865114u, alpha),
                SourceColor(4464688526102469018UL, 1025758986u, alpha), A(uiInk, alpha), 10f);
            Title(s, "连胜中断", OriginalMath.Binary32(dx + 270f), OriginalMath.Binary32(y + -8f), 2.4f, Color.white, SourceColor(4417130516434229658UL, 4575657221408423936UL));
            DrawFlame(s, 270f, OriginalMath.Binary32(y + 104f), 28f, y);
            string digits = streak.ToString();
            s.Text(digits, OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure(digits, 3f) * -.5f) + 270f), OriginalMath.Binary32(y + 92f), 3f, Color.white, false);
            string summary = "当前 " + digits + " 连胜 · 奖励加成 +" + percent.ToString() + "%";
            s.TextF(summary, 271.2149963378906f, OriginalMath.Binary32(OriginalMath.Binary32(y + 150f) + 1.215f), 1.35f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiGoldHi.a * .7f)), 0);
            s.TextF(summary, 270f, OriginalMath.Binary32(y + 150f), 1.35f, uiGoldHi, 0);
            for (int n = 0; n != 10; n++)
            {
                float x = OriginalMath.Binary32((float)(100 + n * 34));
                s.Rect(x, OriginalMath.Binary32(y + 184f), 28f, 10f, SourceColor(4369572502457601229UL, 4575657221408423936UL));
                if (streak < n + 1) continue;
                Color fill = n < 2 ? SourceColor(4557642823964295168UL, 4575657222458677658UL)
                    : n < 5 ? SourceColor(4543231306015703040UL, 4575657222453644493UL)
                    : SourceColor(4510805389557628928UL, 4575657222450289050UL);
                s.Rect(OriginalMath.Binary32(x + 1f), OriginalMath.Binary32(y + 185f), 26f, 8f, fill);
            }
            string consequence = streakWhat + "将中断连胜，失去全部加成";
            s.TextF(consequence, 271.0799865722656f, OriginalMath.Binary32(OriginalMath.Binary32(y + 212f) + 1.08f), 1.2f, new Color(0f, 0f, 0f, .7f), 0);
            s.TextF(consequence, 270f, OriginalMath.Binary32(y + 212f), 1.2f, SourceColor(4554039943403405312UL, 4575657222467066266UL), 0);
            Button(s, 511, 80f, OriginalMath.Binary32(y + 256f), 380f, 58f, streakRevive ? "复活出击" : "继续战斗",
                streakRevive ? Btn.Green : Btn.Primary, 2f, true, 0, streakRevive);
            Button(s, 510, 150f, OriginalMath.Binary32(y + 328f), 240f, 44f, streakRevive ? "放弃并中断连胜" : "确认退出", Btn.Ghost, 1.3f);
            s.MapLayout = mapped;
        }

        // f54526. The beat argument is overwritten by the original function;
        // animation reads ui separately for each of its three flame layers.
        private void DrawFlame(PixelSurface s, float cx, float cy, float radius, float beat)
        {
            s.Glow(cx, cy, OriginalMath.Binary32(radius * 3f), OriginalMath.Binary32(radius * 3f), SourceColor(4539628425454813184UL, 4532422664304512205UL), 0f);
            float bottom = OriginalMath.Binary32(OriginalMath.Binary32(radius * .6f) + cy), center = OriginalMath.Binary32(OriginalMath.Binary32(radius * .35f) + cy);
            for (int n = 0; n != 3; n++)
            {
                float time = ui;
                float factor = n == 0 ? -1.6f : n == 1 ? -1.25f : -.9f;
                float width = n == 0 ? radius : n == 1 ? OriginalMath.Binary32(radius * .75f) : OriginalMath.Binary32(radius * .5f);
                Color color = n == 0 ? SourceColor(4518011146384441344UL, 4575657222442571530UL)
                    : n == 1 ? SourceColor(4554039943403405312UL, 4575657222450289050UL)
                    : SourceColor(4573495496714223616UL, 4575657222468743987UL);
                var end = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 5f) + OriginalMath.Binary32((float)n))) * radius) * .15f) + cx),
                    OriginalMath.Binary32(bottom + OriginalMath.Binary32(OriginalMath.Binary32(radius * factor) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + OriginalMath.Binary32((float)n))) * .08f) + 1f)))));
                s.Segment(new Vector2(cx, bottom), end, width, color);
                float y = OriginalMath.Binary32(center + (n == 0 ? OriginalMath.Binary32(OriginalMath.Binary32(radius * 0f) * -.1f) : n == 1 ? OriginalMath.Binary32(radius * -.1f) : OriginalMath.Binary32((OriginalMath.Binary32(radius + radius)) * -.1f)));
                Disc(s, cx, y, OriginalMath.Binary32(width * .55f), color, 1f);
            }
        }
    }
}
