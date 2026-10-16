using Flightbeat.Model;
using Flightbeat.Economy;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54555. Detail is an information sheet; there is no launch action in source.
        private void DrawCodexLevel(PixelSurface s, int level)
        {
            CodexEntry entry = Entry(level); int zone = WarZones.OfLevel(level); Color accent = WarZones.Accent[zone];
            Backdrop(s, .75f); float enter = OutBack(OriginalMath.Binary32(Since(1599) / .28f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 30f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -260f)));
            Plate(s, 40f, top, 460f, 520f, uiPanelHi, uiPanel, accent, 7f, alpha);
            Button(s, 553, 440f, OriginalMath.Binary32(top + 10f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            s.TextF("第 " + unchecked(level + 1) + " 关 · " + WarZones.Names[zone], 270f, OriginalMath.Binary32(top + 18f), 1f, A(accent, .95f), 0);
            Title(s, LevelTitle(level), 270f, OriginalMath.Binary32(top + 40f), 2f, Color.white, uiInk);
            s.Rect(137f, OriginalMath.Binary32(top + 88f), 266f, 266f, A(accent, .8f));
            s.CodexThumb(level, 140f, OriginalMath.Binary32(top + 91f), 260f, 260f, Color.white);
            if (entry != null)
            {
                string difficulty = entry.difficulty == 2 ? "极难" : entry.difficulty == 1 ? "困难" : "普通";
                Color difficultyColor = entry.difficulty == 2 ? uiRed : entry.difficulty == 1 ? new Color(1f, .62f, .2f, 1f) : uiGreen;
                float row = OriginalMath.Binary32(top + 366f);
                Plate(s, 60f, row, 200f, 64f, SourceColor(4464688526069585674UL, 4575657222452302316UL),
                    SourceColor(4428659729042233098UL, 4575657222445255885UL), A(uiEdge, .7f), 8f);
                s.TextF("敌军数量", 160f, OriginalMath.Binary32(row + 8f), .95f, uiDim, 0);
                string total = entry.total.ToString(); s.Text(total, OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure(total, 2f) * -.5f) + 160f), OriginalMath.Binary32(row + 34f), 2f, Color.white, false);
                Plate(s, 280f, row, 200f, 64f, SourceColor(4464688526069585674UL, 4575657222452302316UL),
                    SourceColor(4428659729042233098UL, 4575657222445255885UL), A(uiEdge, .7f), 8f);
                s.TextF("难度", 380f, OriginalMath.Binary32(row + 8f), .95f, uiDim, 0);
                Label(s, difficulty, 380f, OriginalMath.Binary32(row + 32f), 1.4f, difficultyColor);
                s.TextF("颜色 " + entry.colors + " 种 · 跑道 " + entry.lanes + " 条", 270f, OriginalMath.Binary32(row + 76f), 1f, uiText, 0);
                if (entry.swatch != null)
                    for (int i = 0; i < entry.swatch.Length; i++)
                    {
                        int rgb = entry.swatch[i]; float x = OriginalMath.Binary32((float)(i * 30 + entry.swatch.Length * -15 + 270)), y = OriginalMath.Binary32(row + 102f);
                        s.Rect(x, y, 24f, 14f, new Color(OriginalMath.Binary32(OriginalMath.Binary32((float)((rgb >> 16) & 255)) / 255f), OriginalMath.Binary32(OriginalMath.Binary32((float)((rgb >> 8) & 255)) / 255f), OriginalMath.Binary32(OriginalMath.Binary32((float)(rgb & 255)) / 255f), 1f));
                        s.Border(x, y, 24f, 14f, SourceColor(0UL, 4546834185511239680UL), 1f);
                    }
            }
            bool cleared = Economy.Data.cleared.Contains(unchecked(level + 1));
            Label(s, cleared ? "已收复" : Economy.Data.unlocked > level ? "待出击" : "尚未解锁", 270f, OriginalMath.Binary32(OriginalMath.Binary32(top + 520f) + -34f), 1.2f, cleared ? uiGreen : uiDim);
        }
    }
}
