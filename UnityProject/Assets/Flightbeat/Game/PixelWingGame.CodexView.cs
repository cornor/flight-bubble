using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54552.
        private void DrawCodex(PixelSurface s)
        {
            Backdrop(s, .9f); float enter = OutCubic(OriginalMath.Binary32(Since(555) / .3f)), alpha = Clamp01(OriginalMath.Binary32(enter * 1.4f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 30f) + CodexTop), bottom = CodexBottom;
            Plate(s, 12f, top, 516f, OriginalMath.Binary32(bottom - top), uiPanelHi, uiPanel, uiCyan, 7f, alpha);
            if (codexZone < 0) DrawCodexZones(s, top, bottom); else DrawCodexZone(s, codexZone, top, bottom);
            Button(s, 551, 468f, OriginalMath.Binary32(top + 10f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            if (codexLevel >= 0) DrawCodexLevel(s, codexLevel);
        }
        // f54553. Five authored cards, with source pressed geometry and reward overlay order.
        private void DrawCodexZones(PixelSurface s, float top, float bottom)
        {
            Title(s, "星区图鉴", 270f, OriginalMath.Binary32(top + 16f), 2.2f, Color.white, uiInk);
            s.TextF("沙群档案 · 已收复 " + Economy.Data.cleared.Count + " / " + Levels.Length + " 关", 270f, OriginalMath.Binary32(top + 56f), 1f, uiDim, 0);
            float start = OriginalMath.Binary32(top + 84f), height = OriginalMath.WasmMin(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(bottom - start) + -12f)) / 5f) + -10f), 168f);
            for (int zone = 0; zone < 5; zone++)
            {
                bool open = ZoneOpen(zone); Color accent = WarZones.Accent[zone];
                float y = OriginalMath.Binary32(start + OriginalMath.Binary32((OriginalMath.Binary32(height + 10f)) * OriginalMath.Binary32((float)zone)));
                HitBox(s, zone + 560, 28f, y, 484f, height);
                float pulse = PressPulse(zone + 560), twice = OriginalMath.Binary32(pulse + pulse), shrink = OriginalMath.Binary32(pulse * 4f);
                Plate(s, OriginalMath.Binary32(twice + 28f), OriginalMath.Binary32(y + twice), OriginalMath.Binary32(484f - shrink), OriginalMath.Binary32(height - shrink),
                    SourceColor(4447394707105235599UL, 4575657222449617961UL),
                    SourceColor(4405601299457103626UL, 4575657222441229353UL), open ? accent : A(uiEdge, .6f), 10f);
                DrawZoneScene(s, zone, 36f, OriginalMath.Binary32(y + 8f), 468f, OriginalMath.Binary32(height + -52f), open ? 1f : .35f);
                Plate(s, 40f, OriginalMath.Binary32(y + 14f), 200f, 30f, SourceColor(0UL, 4543231304950349824UL),
                    SourceColor(0UL, 4543231304950349824UL), A(accent, .8f));
                s.TextF(WarZones.Codes[zone] + " · " + WarZones.Names[zone], 140f, OriginalMath.Binary32(y + 20f), 1.05f, Color.white, 0);
                if (!open)
                {
                    float center = OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y);
                    Title(s, "未解锁", 270f, OriginalMath.Binary32(center + -30f), 2f, uiDim, uiInk);
                    s.TextF("收复上一战区后开放", 270f, OriginalMath.Binary32(center + 2f), 1f, uiDim, 0);
                }
                int cleared = ZoneProgress(zone), total = WarZones.LevelsIn(zone); float end = OriginalMath.Binary32(height + y);
                s.TextF(WarZones.Taglines[zone], 44f, OriginalMath.Binary32(end + -38f), 1f, open ? uiText : uiDim, -1);
                string progress = cleared + "/" + total;
                s.Text(progress, OriginalMath.Binary32(470f - PixelSurface.Measure(progress, 1f)), OriginalMath.Binary32(end + -34f), 1f, open ? accent : uiDim, false);
                s.Rect(44f, OriginalMath.Binary32(end + -14f), 440f, 5f, SourceColor(4417130516401346314UL, 4575657222443913708UL));
                s.Rect(45f, OriginalMath.Binary32(end + -13f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)cleared) * 438f) / OriginalMath.Binary32((float)total)), 3f, accent);
                if (Economy.ZoneReady(zone))
                {
                    Button(s, zone + 565, 354f, OriginalMath.Binary32(y + 14f), 140f, 34f, "领取战区奖励", Btn.Primary, 1.05f, true);
                    RedDot(s, 492f, OriginalMath.Binary32(y + 14f), 0);
                }
                else if (Economy.ZoneClaimed(zone))
                {
                    Plate(s, 398f, OriginalMath.Binary32(y + 16f), 96f, 28f, SourceColor(4505040778911012618UL, 4575657222450960138UL),
                        SourceColor(4464688526066565775UL, 4575657222441229353UL), uiGreen);
                    s.TextF("已收复", 446f, OriginalMath.Binary32(y + 20f), 1f, uiGreen, 0);
                }
                else if (open)
                {
                    s.TextF("通关奖励", 452f, OriginalMath.Binary32(y + 14f), .85f, uiGold, 0);
                    s.TextF(WarZones.RewardCoins[zone] + "金", 452f, OriginalMath.Binary32(y + 30f), .85f, uiGoldHi, 0);
                }
            }
        }
        // f54554. Pages contain 32 entries in eight columns; source ids encode
        // zone-relative positions, including the second page's offset.
        private void DrawCodexZone(PixelSurface s, int zone, float top, float bottom)
        {
            Color accent = WarZones.Accent[zone];
            Button(s, 552, 24f, OriginalMath.Binary32(top + 10f), 88f, 40f, "返回", Btn.Ghost, 1.2f, true);
            Title(s, WarZones.Names[zone], 270f, OriginalMath.Binary32(top + 14f), 2f, Color.white, uiInk);
            s.TextF(WarZones.Codes[zone] + " · " + WarZones.Taglines[zone], 270f, OriginalMath.Binary32(top + 50f), .95f, A(accent, .95f), 0);
            float sceneTop = OriginalMath.Binary32(top + 74f);
            DrawZoneScene(s, zone, 24f, sceneTop, 492f, 56f, 1f);
            string[] lore = WarZones.Lore[zone].Split('\n'); float loreTop = OriginalMath.Binary32(sceneTop + 66f);
            Plate(s, 24f, loreTop, 492f, OriginalMath.Binary32((float)(lore.Length * 20 + 16)),
                SourceColor(4441630096489824911UL, 4572774917140234895UL),
                SourceColor(4417130516401346314UL, 4572774917134866186UL), A(accent, .5f), 8f);
            for (int row = 0; row < lore.Length; row++) s.TextF(lore[row], 36f, OriginalMath.Binary32(OriginalMath.Binary32(loreTop + 8f) + OriginalMath.Binary32((float)(row * 20))), 1f, uiText, -1);
            float gridTop = OriginalMath.Binary32(OriginalMath.Binary32(loreTop + OriginalMath.Binary32((float)(lore.Length * 20))) + 28f), headingY = OriginalMath.Binary32(gridTop + -2f);
            Label(s, "敌军排兵样式", 36f, headingY, 1.1f, uiGold, -1);
            int first = WarZones.FirstLevel(zone), count = unchecked(WarZones.LastLevel(zone) - first + 1);
            int pages = Math.Max(SourceInt(OriginalMath.Binary32((float)Math.Ceiling(OriginalMath.Binary32(OriginalMath.Binary32((float)count) * .03125f)))), 1);
            codexPage = codexPage < 0 ? 0 : codexPage > pages - 1 ? pages - 1 : codexPage;
            int begin = codexPage << 5, end = Math.Min(count, unchecked(begin + 32));
            if (pages > 1)
            {
                s.TextF(unchecked(codexPage + 1) + " / " + pages, 461f, headingY, 1f, uiText, 0);
                if (codexPage > 0) Button(s, 556, 394f, OriginalMath.Binary32(gridTop + -8f), 40f, 30f, "<", Btn.Ghost, 1.2f, true);
                if (codexPage < pages - 1) Button(s, 557, 488f, OriginalMath.Binary32(gridTop + -8f), 40f, 30f, ">", Btn.Ghost, 1.2f, true);
            }
            if (begin >= end) return;
            float stride = OriginalMath.WasmMin(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(bottom - gridTop) + -30f)) * .25f), 96f);
            float size = OriginalMath.WasmMin(OriginalMath.Binary32(stride + -20f), 52f), inset = OriginalMath.Binary32((OriginalMath.Binary32(60f - size)) * .5f);
            for (int position = begin; position != end; position++)
            {
                int level = unchecked(position + first), number = unchecked(level + 1), local = position - begin;
                bool open = level < Economy.Data.unlocked, cleared = Economy.Data.cleared.Contains(number);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(local - (local / 8) * 8)) * 60f) + 30f), y = OriginalMath.Binary32(OriginalMath.Binary32(gridTop + 22f) + OriginalMath.Binary32(stride * OriginalMath.Binary32((float)(local / 8))));
                HitBox(s, position + 600, OriginalMath.Binary32(x + 2f), y, 56f, OriginalMath.Binary32(stride + -2f));
                float pulse = PressPulse(position + 600), thumbX = OriginalMath.Binary32(inset + x);
                Color edge = cleared ? A(accent, .9f) : open ? A(uiEdge, .8f) : new Color(.1f, .14f, .18f, 1f);
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(thumbX + -2f) + pulse), OriginalMath.Binary32(OriginalMath.Binary32(y + -2f) + pulse), OriginalMath.Binary32(size + 4f), OriginalMath.Binary32(size + 4f), edge);
                if (open) s.CodexThumb(level, OriginalMath.Binary32(thumbX + pulse), OriginalMath.Binary32(y + pulse), OriginalMath.Binary32(size - (OriginalMath.Binary32(pulse + pulse))), OriginalMath.Binary32(size - (OriginalMath.Binary32(pulse + pulse))), Color.white);
                else
                {
                    s.CodexThumb(level, thumbX, y, size, size, SourceColor(4471894287205122703UL, 4575657222452302316UL));
                    s.TextF("?", OriginalMath.Binary32(x + 30f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(size * .5f) + y) + -10f), 1.4f, uiDim, 0);
                }
                string text = number.ToString("D2");
                s.Text(text, OriginalMath.Binary32(OriginalMath.Binary32(x + 30f) + OriginalMath.Binary32(PixelSurface.Measure(text, 1f) * -.5f)), OriginalMath.Binary32(OriginalMath.Binary32(size + y) + 4f), 1f, cleared ? accent : open ? Color.white : uiDim, false);
                if (cleared) DrawCheck(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 60f) + -16f), OriginalMath.Binary32(y + 8f), .9f);
            }
        }
    }
}
