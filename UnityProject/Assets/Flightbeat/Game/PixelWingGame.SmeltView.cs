using Flightbeat.Model;
using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54601: only spare copies appear. Source material rows are truncated
        // to the authored grid capacity; selected material rows preserve order.
        private void DrawSmelt(PixelSurface s)
        {
            Backdrop(s, .84f);
            float enter = OutBack(OriginalMath.Binary32(Since(478) / .3f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float available = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height - ScreenLayout.Top) - 60f), height = available < 760f ? available : 760f;
            float top = OriginalMath.Binary32(ScreenLayout.Top + 40f), center = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - OriginalMath.Binary32(height * .5f));
            top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (top > center ? top : center));
            int fighter = smeltTarget / 5, part = smeltTarget % 5;
            Color accent = FleetCatalog.FighterAccent[fighter]; int level = Economy.PartLevel(fighter, part);
            Plate(s, 20f, top, 500f, height, uiPanelHi, uiPanel, uiGold, 7f, alpha);
            Title(s, "熔炼 · 3 换 1", 270f, OriginalMath.Binary32(top + 14f), 2f, Color.white, uiInk);
            Button(s, 473, 460f, OriginalMath.Binary32(top + 10f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            float y = OriginalMath.Binary32(top + 60f), targetCenterY = OriginalMath.Binary32(y + 52f);
            Plate(s, 36f, y, 468f, 104f, Mix(new Color(.06f, .16f, .22f, .96f), accent, .12f),
                SourceColor(4435865490164012810UL, 4572774917138892718UL), accent, 8f);
            s.Glow(90f, targetCenterY, 100f, 100f, A(accent, .35f), 0f);
            DrawPartThumb(s, fighter, part, 90f, targetCenterY, 80f, 1f, false);
            Label(s, "目标：" + FleetCatalog.PartLabel(smeltTarget), 144f, OriginalMath.Binary32(y + 12f), 1.35f, Color.white, -1);
            string hint;
            if (level == 0)
            {
                s.TextF("补齐首件 → 组装进度 " + Economy.Collected(fighter) + "/5 → " + (Economy.Collected(fighter) + 1) + "/5", 144f, OriginalMath.Binary32(y + 46f), 1f, uiGreen, -1);
                hint = Economy.Collected(fighter) == 4 ? "熔炼后即可组装 " + FleetCatalog.FighterNames[fighter] : "首件永久计入收藏";
            }
            else
            {
                int currentLevel = Economy.PartLevel(fighter, part), spare = Economy.Spare(fighter, part), cost = Economy.UpgradeCost(fighter, part);
                s.TextF("升级 LV" + (currentLevel + 1) + " 进度 " + spare + "/" + cost + " → " + (spare + 1) + "/" + cost, 144f, OriginalMath.Binary32(y + 46f), 1f, uiGreen, -1);
                hint = cost > spare + 1 ? "还需 " + unchecked(cost - spare - 1) + " 件" : "熔炼后即可升级";
            }
            s.TextF(hint, 144f, OriginalMath.Binary32(y + 72f), .92f, uiDim, -1);
            y  =             OriginalMath.Binary32(y+(118f));
            Label(s, "选择 3 件重复件（点击加入，再点已选移除）", 40f, y, 1.05f, uiText, -1);
            float bottom = OriginalMath.Binary32(height + top), rowsTop = OriginalMath.Binary32(y + 28f), rowCount = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(bottom - 236f) - rowsTop)) / 92f);
            int rows = Math.Abs(rowCount) < 2147483648f ? (int)rowCount : int.MinValue;
            int maxCells = unchecked((rows > 1 ? rows : 1) * 5); List<int> sources = SmeltSources();
            if (sources.Count == 0) s.TextF("暂无重复件 · 战机补给可获得", 270f, OriginalMath.Binary32(rowsTop + 30f), 1.05f, uiDim, 0);
            for (int cell = 0; cell < sources.Count && cell < maxCells; cell++)
            {
                int index = sources[cell], picked = Picked(index), copies = Economy.Data.spares[index], needed = Economy.StillNeeded(index);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(cell % 5)) * 92f) + 40f), rowY = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(OriginalMath.Binary32((float)(cell / 5)) * 92f));
                HitBox(s, 440 + index, x, rowY, 86f, 86f); int sourceFighter = index / 5, sourcePart = index % 5;
                float pulse = PressPulse(440 + index), shift = OriginalMath.Binary32(pulse + pulse), size = OriginalMath.Binary32(OriginalMath.Binary32(pulse * -4f) + 86f);
                Plate(s, OriginalMath.Binary32(x + shift), OriginalMath.Binary32(rowY + shift), size, size,
                    picked > 0 ? Mix(new Color(.1f, .22f, .28f, 1f), uiGold, .25f) : new Color(.04f, .1f, .15f, .95f),
                    SourceColor(4417130516401346314UL, 4572054341883023852UL), picked > 0 ? uiGold : A(uiEdge, .6f), 6f);
                float cx = OriginalMath.Binary32(x + 43f);
                DrawPartThumb(s, sourceFighter, sourcePart, cx, OriginalMath.Binary32(rowY + 26f), 38f, unchecked(copies - picked) > 0 ? 1f : .4f, false);
                s.TextF(FleetCatalog.FighterNames[sourceFighter], cx, OriginalMath.Binary32(rowY + 48f), .8f, uiText, 0);
                s.TextF(FleetCatalog.PartNames[sourcePart] + " ×" + Economy.Data.spares[index], cx, OriginalMath.Binary32(rowY + 66f), .82f, FleetCatalog.FighterAccent[sourceFighter], 0);
                if (copies > needed)
                {
                    s.Rect(OriginalMath.Binary32(x + 2f), OriginalMath.Binary32(rowY + 2f), 30f, 15f, A(uiGreen, .9f));
                    s.TextF("溢出", OriginalMath.Binary32(x + 17f), OriginalMath.Binary32(rowY + 2f), .75f, uiInk, 0);
                }
                if (picked > 0)
                {
                    float badgeX = OriginalMath.Binary32(OriginalMath.Binary32(x + 92f) - 18f); Disc(s, badgeX, OriginalMath.Binary32(rowY + 10f), 10f, uiGold);
                    s.TextF(picked.ToString(), badgeX, OriginalMath.Binary32(rowY + 2f), .95f, uiInk, 0);
                }
            }
            y = OriginalMath.Binary32(bottom - 222f);
            Plate(s, 36f, y, 468f, 120f, SourceColor(4464688526072270029UL, 4572774917144596972UL),
                SourceColor(4417130516401346314UL, 4572774917136208364UL), A(uiEdge, .7f), 8f);
            Label(s, "已选 " + smeltPick.Count + "/3", 50f, OriginalMath.Binary32(y + 8f), 1.15f, smeltPick.Count == 3 ? uiGoldHi : uiText, -1);
            Button(s, 470, 310f, OriginalMath.Binary32(y + 6f), 88f, 30f, "推荐", Btn.Secondary, 1f, true);
            Button(s, 471, 406f, OriginalMath.Binary32(y + 6f), 82f, 30f, "清空", Btn.Ghost, 1f, true);
            var distinct = new List<int>(); y  = OriginalMath.Binary32(y+(42f));
            foreach (int index in smeltPick)
            {
                if (distinct.Contains(index)) continue; distinct.Add(index);
                int picked = Picked(index), copies = Economy.Data.spares[index], sourceFighter = index / 5, sourcePart = index % 5;
                string note;
                if (!Economy.Assembled(sourceFighter)) note = "（组装后用于升级）";
                else
                {
                    int sourceLevel = Economy.PartLevel(sourceFighter, sourcePart);
                    note = sourceLevel > 4 ? "（已满级，仅能熔炼）" : "（用于升级：LV" + (sourceLevel + 1) + " 需 " + Economy.UpgradeCost(sourceFighter, sourcePart) + "）";
                }
                HitBox(s, distinct.Count + 473, 44f, OriginalMath.Binary32(y - 2f), 452f, 24f);
                s.TextF("- " + FleetCatalog.PartLabel(index) + " ×" + picked + "：重复件 " + copies + "→" + unchecked(copies - picked) + " " + note, 50f, y, .88f, uiText, -1);
                y  =                 OriginalMath.Binary32(y+(24f));
            }
            if (smeltPick.Count == 0) s.TextF("点击上方部件选择熔炼材料", 50f, y, .92f, uiDim, -1);
            Button(s, 472, 150f, OriginalMath.Binary32(bottom - 86f), 240f, 56f, "熔炼", smeltPick.Count == 3 ? Btn.Primary : Btn.Secondary, 1.8f, smeltPick.Count == 3);
            float flash = Clamp01(OriginalMath.Binary32(OriginalMath.Binary32(Since(479) / -.8f) + 1f));
            if (flash > 0f)
            {
                Color glow = uiGold; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(glow.a * flash) * .7f);
                s.Glow(90f, targetCenterY, OriginalMath.Binary32(flash * 260f), OriginalMath.Binary32(flash * 260f), glow, 0f);
                s.Ring(new Vector2(90f, targetCenterY), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - flash)) * 80f) + 20f), new Color(1f, 1f, 1f, flash), 3f, 28, 0f, 360f);
            }
        }
    }
}
