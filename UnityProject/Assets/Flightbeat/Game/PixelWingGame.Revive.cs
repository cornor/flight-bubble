using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54856: successful revival schedules the source rescue-missile aim.
        private void Rescue()
        {
            if (Battle.Revive()) rescueAimAt = OriginalMath.Binary32(Battle.Time + 1.6f);
        }

        // f54827, metadata field AE5E2886... at 0x36FE68, four original icons.
        private static readonly int[] reviveIcons = { 17, 12, 5, 17 };
        private static readonly string[] reviveTitles = { "清除前排敌人", "敌阵大幅后退", "额外机位 +1", "超级导弹 ×1" };

        private void DrawRevive(PixelSurface s)
        {
            float age = feedback.RescueAge;
            Backdrop(s, OriginalMath.WasmMin(OriginalMath.Binary32(age * 3f), .86f));
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float center = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Top + OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(ScreenLayout.Height - ScreenLayout.Top)) - ScreenLayout.Bottom)) * .5f)) + -310f);
            float appear = OutBack(OriginalMath.Binary32(age / .35f)), alpha = Clamp01(OriginalMath.Binary32(age / .18f));
            float y = OriginalMath.Binary32(center + OriginalMath.Binary32((OriginalMath.Binary32(1f - appear)) * 40f));
            Plate(s, 50f, OriginalMath.Binary32(y + 36f), 440f, 590f, uiPanelHi, uiPanel, A(uiRed, .9f), 7f, alpha);
            Plate(s, 120f, OriginalMath.Binary32(y + 8f), 300f, 58f,
                SourceColor(4494952716806022758UL, 1043878380u, alpha),
                SourceColor(4417130516439262822UL, 1028443341u, alpha), A(uiInk, alpha), 10f);
            Title(s, "防线失守", OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(age * 45f)) * OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(age * -2.5f) + 1f), 0f)) * 5f) + 270f),
                OriginalMath.Binary32(y + 18f), 2.6f, new Color(1f, 1f, 1f, alpha), SourceColor(1050253722UL, 0u, alpha));
            s.Art(2, 76f, OriginalMath.Binary32(y + 84f), 388f, 150f, 0);
            s.Corners(74f, OriginalMath.Binary32(y + 82f), 392f, 154f, A(uiCyan, alpha), 12, 2);
            Color caption = A(uiGold, alpha);
            s.TextF("复活后立即获得", 271.260009765625f, OriginalMath.Binary32(OriginalMath.Binary32(y + 250f) + 1.26f), 1.4f, new Color(0f, 0f, 0f, OriginalMath.Binary32(caption.a * .7f)), 0);
            s.TextF("复活后立即获得", 270f, OriginalMath.Binary32(y + 250f), 1.4f, caption, 0);
            string[] details = {
                "前 " + Battle.Pressure.revivePurgeRows.ToString() + " 排全部消灭",
                "推回 " + ResultRound(Battle.Pressure.revivePush).ToString() + " 格距离",
                "新增一个出击位", "可自选目标轰炸"
            };
            float cardY = OriginalMath.Binary32(y + 282f), delay = OriginalMath.Binary32(age + -.25f);
            for (int i = 0; i < 4; i++)
            {
                float t = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(delay + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.1f))) / .3f));
                if (t <= 0f) continue;
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(i & 1)) * 207f) + 76f), row = OriginalMath.Binary32(cardY + OriginalMath.Binary32((float)((i >> 1) * 84))), a = OriginalMath.Binary32(alpha * t);
                float q = OriginalMath.Binary32(1f - Clamp01(t));
                Plate(s, OriginalMath.Binary32(x + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(q * (OriginalMath.Binary32(q * q))) + -1f) + 1f)) * 20f)), row, 181f, 74f,
                    SourceColor(4489188110454709289UL, 1049582633u, a),
                    SourceColor(4447394707105235599UL, 1041194025u, a), A(uiCyan, OriginalMath.Binary32(a * .7f)), 7f);
                s.Glow(OriginalMath.Binary32(x + 32f), OriginalMath.Binary32(row + 37f), 60f, 60f, A(uiCyan, OriginalMath.Binary32(a * .25f)), 0f);
                s.DeckArt(reviveIcons[i], OriginalMath.Binary32(x + 8f), OriginalMath.Binary32(row + 13f), 48f, 48f, new Color(1f, 1f, 1f, a), true);
                Color text = A(uiText, a);
                s.TextF(reviveTitles[i], OriginalMath.Binary32(OriginalMath.Binary32(x + 62f) + 1.125f), OriginalMath.Binary32(OriginalMath.Binary32(row + 14f) + 1.125f), 1.25f, new Color(0f, 0f, 0f, OriginalMath.Binary32(text.a * .7f)), -1);
                s.TextF(reviveTitles[i], OriginalMath.Binary32(x + 62f), OriginalMath.Binary32(row + 14f), 1.25f, text, -1);
                s.TextF(details[i], OriginalMath.Binary32(x + 62f), OriginalMath.Binary32(row + 44f), 1f, A(uiDim, a), -1);
            }
            Button(s, 30, 80f, OriginalMath.Binary32(y + 470f), 380f, 68f, "复活出击", Btn.Green, 2.4f, true, 0, true);
            Button(s, 31, 190f, OriginalMath.Binary32(y + 552f), 160f, 40f, "放弃", Btn.Ghost, 1.2f);
            if (age > .3f && dialogue.Length > 0)
            {
                float safe = OriginalMath.Binary32(ScreenLayout.Top + 58f), above = OriginalMath.Binary32(y + -100f);
                DrawDialogue(s, 50f, safe > above ? safe : above, 440f, false);
            }
            s.MapLayout = mapped;
        }
    }
}
