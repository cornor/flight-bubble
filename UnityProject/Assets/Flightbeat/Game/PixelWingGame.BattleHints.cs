using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54491, including original inventory/advert/cancel/countdown branches.
        private void DrawCommandButtons(PixelSurface s)
        {
            if (evidenceControlled || Battle.Level.lesson <= 0) { DrawPropButtons(s); return; }
            Plate(s, 18f, 878f, 504f, 72f, uiPanelHi, uiPanel, uiEdge, 8f);
            string line = Battle.Level.lesson == 1 ? "观察颜色 · 派出战机 · 同色合成" : "留意弹药 · 及时补位 · 保持火力";
            s.TextF(line, 271.2149963378906f, 894.2150268554688f, 1.35f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiText.a * .7f)), 0);
            s.TextF(line, 270f, 893f, 1.35f, uiText, 0);
            s.TextF("敌人触及黄黑防线即失守", 271f, 923f, 1.05f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiDim.a * .7f)), 0);
            s.TextF("敌人触及黄黑防线即失守", 270f, 922f, 1.05f, uiDim, 0);
        }

        // f54825/f54826/f54829. Keep folded TextF shadow coordinates.
        private void DrawReadyHint(PixelSurface s)
        {
            float bob = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 4f)) * 3f);
            Plate(s, 120f, OriginalMath.Binary32(bob + 498f), 300f, 40f, SourceColor(4489188110453031567UL, 4571333766643261112UL),
                SourceColor(4441630096484456202UL, 4571333766633865871UL), uiCyan, 8f);
            float y = OriginalMath.Binary32(bob + 508f);
            s.TextF("点击下方战机，开始出击", 271.1700134277344f, OriginalMath.Binary32(y + 1.17f), 1.3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiText.a * .7f)), 0);
            s.TextF("点击下方战机，开始出击", 270f, y, 1.3f, uiText, 0);
        }
        private void DrawHoldBadge(PixelSurface s)
        {
            Plate(s, 150f, 124f, 240f, 34f, SourceColor(4489188110450347213UL, 4569892616137563177UL),
                SourceColor(4441630096484456202UL, 4569892616127496847UL), uiCyan);
            string text = "敌阵压制 " + Battle.AdvanceHeld.ToString("0.0") + " 秒";
            s.TextF(text, 271.1700134277344f, 132.1699981689453f, 1.3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiCyan.a * .7f)), 0);
            s.TextF(text, 270f, 131f, 1.3f, uiCyan, 0);
        }
        private void DrawPausedVeil(PixelSurface s)
        {
            Backdrop(s, .55f);
            Title(s, "已暂停", 270f, 420f, 3f, uiText, uiInk);
            Button(s, 1, 170f, 490f, 200f, 52f, "继续", Btn.Primary);
        }

        // f54823.
        private void DrawCommandAlert(PixelSurface s)
        {
            if (selection != 0 || !Battle.Stalled || Battle.State != Phase.Playing || settings || Battle.ActiveFinale != null) return;
            Plate(s, 40f, 280f, 460f, 66f, uiPanelHi, uiPanel, uiGold, 8f);
            Label(s, "机位已满，前排颜色挡住了火力", 270f, 290f, 1.25f, uiText);
            Label(s, "点击超级炸弹，炸开前排再接力", 270f, 317f, 1.2f, uiGold);
            s.Corners(366f, 878f, 156f, 72f, uiGold, 12, 2);
        }

        // f54486.
        private void DrawCombatHints(PixelSurface s)
        {
            if (settings || paused || selection > 0) return;
            int state = (int)Battle.State;
            if (state <= 5 && ((1 << state) & 57) != 0) return;
            if (Battle.ActiveFinale != null) return;
            for (int n = 0; n < Battle.Fleet.Count; n++)
            {
                Fighter ship = Battle.Fleet[n];
                float x = Battle.FleetPositionX(Battle.VisualSlot(n));
                if (!(ship.Arrival <= 0f) || Battle.Exposed(ship.Color)) continue;
                Plate(s, OriginalMath.Binary32(x + -27f), 586f, 54f, 18f, SourceColor(4458923919767261676UL, 4572054341864904458UL),
                    uiPanel, uiGold, 3f);
                Label(s, "等待", x, 588f, .85f, uiGold);
            }
            if (Battle.Level.lesson == 2 && lesson != null && !lesson.Complete)
                Label(s, "数字 = 弹药", 445f, 579f, 1f, uiCyan);
            if (Battle.Level.lesson == 0 && !Economy.Data.bombTutorial && Battle.Time > 3f && Battle.Time < 13f && !Battle.Stalled)
            {
                Plate(s, 30f, 130f, 480f, 66f, uiPanelHi, uiPanel, uiCyan, 8f);
                Label(s, "遇到拥堵时，可以呼叫支援", 270f, 140f, 1.2f, uiText);
                Label(s, "重排队列 · 指定后排 · 炸开敌阵", 270f, 166f, 1.1f, uiDim);
            }
            if (Battle.NoticeTime > 0f && lesson != null && !lesson.Complete)
                Label(s, Battle.Notice, 270f, 842f, 1.05f, uiGold);
        }
    }
}
