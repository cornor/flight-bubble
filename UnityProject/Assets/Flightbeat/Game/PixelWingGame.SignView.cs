using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54598: immutable switch data at original memory.bin +3647960.
        // SHA-256 bc409edd...973eb38; these are deck icon ids, not sprite ids.
        private static readonly int[] SignIcons = { 1, 2, 15, 16, 17 };
        private void DrawSign(PixelSurface s)
        {
            Backdrop(s, .8f); float enter = OutBack(OriginalMath.Binary32(Since(240) / .32f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - 250f)));
            Plate(s, 32f, top, 476f, 500f, uiPanelHi, uiPanel, uiGold, 7f, alpha);
            Title(s, "七日签到", 270f, OriginalMath.Binary32(top + 20f), 2.2f, Color.white, uiInk);
            Button(s, 298, 448f, OriginalMath.Binary32(top + 12f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            int next = Economy.SignIndex; bool canSign = Economy.CanSign;
            for (int day = 0; day < 7; day++)
            {
                bool seventh = day == 6, claimed = canSign ? day < next : Economy.Data.signCount > 0 && day <= unchecked(Economy.Data.signCount - 1) % 7;
                bool active = canSign && day == next;
                float x = seventh ? 54f : OriginalMath.Binary32((float)((day % 3) * 148 + 54)), y = seventh ? OriginalMath.Binary32(top + 330f) : OriginalMath.Binary32(OriginalMath.Binary32(top + 80f) + OriginalMath.Binary32((float)((day / 3) * 124)));
                float flipAge = signedIndex == day ? Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(ui - signedAt)) / .4f)) : 1f;
                float width = seventh ? 432f : 136f, height = seventh ? 96f : 112f;
                float wave = active ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 5f)) * .5f) + .5f) : 0f;
                Color edge = active ? Mix(uiGold, Color.white, OriginalMath.Binary32(wave * .5f)) : claimed ? A(uiGreen, .6f) : A(uiEdge, .7f);
                float drawWidth = OriginalMath.Binary32(width * (signedIndex != day || !(flipAge < 1f) ? 1f : OriginalMath.WasmMax(Mathf.Abs(OriginalMath.Cos(OriginalMath.Binary32(flipAge * 3.1415927410125732f))), .05f)));
                float cx = OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + x);
                if (active)
                {
                    Color glow = uiGold; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(glow.a * alpha) * wave) * .3f);
                    s.Glow(cx, OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y), OriginalMath.Binary32(width + 40f), OriginalMath.Binary32(height + 40f), glow, 0f);
                }
                float left = OriginalMath.Binary32(cx + OriginalMath.Binary32(drawWidth * -.5f));
                Plate(s, left, y, drawWidth, height, active ? new Color(.3f, .2f, .05f, alpha) : new Color(.06f, .14f, .2f, alpha),
                    SourceColor(4417130516401346314UL, 1034147594u, alpha), A(edge, alpha), 8f);
                // f54598 active day label uses static +184 (uiGoldHi).
                Label(s, "第" + (day + 1) + "天", cx, OriginalMath.Binary32(y + 8f), 1.05f, A(active ? uiGoldHi : uiDim, alpha));
                int kind = FleetCatalog.SignKind[day], amount = FleetCatalog.SignAmount[day];
                float shade = claimed ? .5f : 1f, iconSize = seventh ? 56f : 46f; Color iconColor = new Color(shade, shade, shade, alpha);
                string reward;
                if (kind == 5)
                {
                    s.Sprite(15, OriginalMath.Binary32(OriginalMath.Binary32(cx + OriginalMath.Binary32(iconSize * -.5f)) + (seventh ? -80f : 0f)), OriginalMath.Binary32(y + (seventh ? 24f : 28f)), iconSize, iconSize, iconColor, 0f);
                    reward = "稀有模块";
                }
                else
                {
                    DeckIcon(s, kind <= 4 ? SignIcons[kind] : 9, seventh ? OriginalMath.Binary32(cx - 80f) : cx, OriginalMath.Binary32(y + (seventh ? 52f : 51f)), iconSize, iconColor);
                    reward = (kind > 1 ? "×" : "+") + amount;
                }
                if (seventh) Label(s, reward, OriginalMath.Binary32(cx + 10f), OriginalMath.Binary32(y + 34f), 1.8f, A(uiGoldHi, alpha), -1);
                else s.TextF(reward, cx, OriginalMath.Binary32(y + 82f), 1.25f, new Color(1f, 1f, 1f, alpha), 0);
                if (claimed)
                {
                    s.Rect(OriginalMath.Binary32(left + 2f), OriginalMath.Binary32(y + 2f), OriginalMath.Binary32(drawWidth - 4f), OriginalMath.Binary32(height - 4f), new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .45f)));
                    DrawCheck(s, cx, OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y), alpha);
                }
            }
            Button(s, 240, 110f, OriginalMath.Binary32(OriginalMath.Binary32(top + 500f) - 56f), 320f, 46f, canSign ? "签到领取" : "明天再来", canSign ? Btn.Primary : Btn.Secondary, 1.6f, canSign);
        }
    }
}
