using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54828. Battle pause settings keep their own positions and action IDs.
        private void DrawSettings(PixelSurface s)
        {
            Backdrop(s, .82f);
            float appear = OutBack(OriginalMath.Binary32(Since(20) / .3f)), y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - appear)) * 40f) + 250f);
            Card(s, 60f, y, 420f, ShowTuning ? 500f : 450f, uiEdge, Clamp01(OriginalMath.Binary32(appear * 1.2f)));
            Title(s, "暂停", 270f, OriginalMath.Binary32(y + 22f), 2.2f, uiText, uiInk);
            string[] labels = { "音乐", "音效", "光效" };
            bool[] enabled = { !audioChip.MusicMuted, !audioChip.EffectsMuted, effects.Enabled };
            float rowsTop = OriginalMath.Binary32(y + 86f);
            for (int n = 0; n < 3; n++)
            {
                float row = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32((float)(n * 58)));
                HitBox(s, n + 20, 84f, row, 372f, 48f);
                Plate(s, 84f, row, 372f, 48f, SourceColor(4480541198826560553UL, 4575657222456999936UL),
                    SourceColor(4447394707105235599UL, 4575657222449617961UL), A(uiEdge, .7f));
                s.TextF(labels[n], 109.44000244140625f, OriginalMath.Binary32(OriginalMath.Binary32(row + 12f) + 1.44f), 1.6f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiText.a * .7f)), -1);
                s.TextF(labels[n], 108f, OriginalMath.Binary32(row + 12f), 1.6f, uiText, -1);
                uint packed = enabled[n] ? unchecked((uint)-6898136) : unchecked((uint)-11516356);
                Color tint = new Color(OriginalMath.Binary32(OriginalMath.Binary32((float)(packed & 60u)) / 255f), OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 8) & 254u)) / 255f),
                    OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 16) & 214u)) / 255f), 1f);
                float toggleY = OriginalMath.Binary32(row + 11f);
                s.Rect(372f, OriginalMath.Binary32(toggleY + 2f), 64f, 22f, tint);
                s.Rect(374f, toggleY, 60f, 26f, tint);
                s.Rect(enabled[n] ? 410f : 376f, OriginalMath.Binary32(toggleY + 3f), 26f, 20f, Color.white);
                s.TextF(enabled[n] ? "开" : "关", enabled[n] ? 390f : 418f, OriginalMath.Binary32(toggleY + 5f), 1.1f, enabled[n] ? Color.white : uiDim, 0);
            }
            Button(s, 24, 84f, OriginalMath.Binary32(y + 266f), 372f, 54f, "继续战斗", Btn.Primary);
            Button(s, 23, 84f, OriginalMath.Binary32(y + 330f), 180f, 46f, "重新开始", Btn.Secondary, 1.4f);
            Button(s, 25, 276f, OriginalMath.Binary32(y + 330f), 180f, 46f, "返回主页", Btn.Secondary, 1.4f);
            if (ShowTuning) Button(s, 26, 84f, OriginalMath.Binary32(y + 390f), 372f, 40f, "数值调参面板", Btn.Ghost, 1.2f);
            s.TextF("R 重开  N 下一关  M 静音", 270f, OriginalMath.Binary32(y + (ShowTuning ? 452f : 398f)), 1f, A(uiDim, .6f), 0);
            DrawVersion(s, 270f, OriginalMath.Binary32(y + (ShowTuning ? 474f : 422f)), true);
        }
    }
}
