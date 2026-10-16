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
        // f54615. Pagination derives from the resting panel, not its slide-in Y.
        private void DrawModulePicker(PixelSurface s)
        {
            float appear = OutCubic(OriginalMath.Binary32(Since(493) / .28f)), y = OriginalMath.Binary32(PickTop + OriginalMath.Binary32((OriginalMath.Binary32(1f - appear)) * 300f));
            float bottom = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.SafeBottom - 96f) - 6f);
            int slot = moduleInspect, fighter = hangarFighter;
            bool resonance = slot == FleetCatalog.FighterResonance[fighter];
            bool mapped = s.MapLayout; s.MapLayout = false;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, SourceColor(4333543704410193920UL, 1022739087u, OriginalMath.Binary32(appear * .55f)));
            s.MapLayout = mapped;
            HitBox(s, 380, ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height);
            HitBox(s, 0, 16f, y, 508f, OriginalMath.Binary32(bottom - y));
            Card(s, 16f, y, 508f, OriginalMath.Binary32(bottom - y), uiCyan, appear);
            Label(s, FleetCatalog.SlotNames[slot] + " · " + FleetCatalog.SlotRoles[slot] + "模块", 36f, OriginalMath.Binary32(y + 14f), 1.55f, uiText, -1);
            s.TextF(FleetCatalog.FighterNames[fighter] + (resonance ? " · 共鸣槽 效果×1.25" : " 的预设"), 36f, OriginalMath.Binary32(y + 44f), .95f,
                resonance ? FleetCatalog.FighterAccent[fighter] : uiDim, -1);
            Button(s, 380, 456f, OriginalMath.Binary32(y + 10f), 52f, 40f, "X", Btn.Ghost, 2f, true);
            y  =             OriginalMath.Binary32(y+(70f));
            FleetCore current = Economy.InPreset(fighter, slot); float multiplier = resonance ? 1.25f : 1f;
            Color tint = current != null ? FleetCatalog.QualityColors[current.quality] : A(uiEdge, .6f);
            Plate(s, 32f, y, 476f, 78f, current != null ? Mix(tint, uiPanel, .78f) : new Color(.04f, .1f, .15f, 1f),
                SourceColor(4417130516401346314UL, 4575657222443913708UL), tint, 8f);
            if (current != null)
            {
                ModuleIcon(s, current, 44f, OriginalMath.Binary32(y + 9f), 60f);
                Label(s, current.Spec.name, 114f, OriginalMath.Binary32(y + 8f), 1.35f, FleetCatalog.QualityColors[current.quality], -1);
                QualityTag(s, current.quality, OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure(current.Spec.name, 1.35f) + 114f) + 10f), OriginalMath.Binary32(y + 10f));
                s.TextF(current.Spec.Describe(current.quality, multiplier), 114f, OriginalMath.Binary32(y + 34f), 1f, uiText, -1);
                s.TextF("耐久 " + current.durability + "/" + current.MaxDurability, 114f, OriginalMath.Binary32(y + 56f), .9f,
                    unchecked(current.durability * 3) > current.MaxDurability ? uiDim : uiRed, -1);
                DurabilityBar(s, current, 210f, OriginalMath.Binary32(y + 60f), 120f);
                if (current.durability < current.MaxDurability) Button(s, 386, 340f, OriginalMath.Binary32(y + 42f), 70f, 30f, "维修", Btn.Green, 1f, true, 0, true);
                Button(s, 383, 418f, OriginalMath.Binary32(y + 20f), 78f, 38f, "卸下", Btn.Secondary, 1.3f, true);
            }
            else Label(s, "未装配 · 从下方选择模块", 270f, OriginalMath.Binary32(y + 28f), 1.3f, uiDim);
            List<FleetCore> list = PickList(); int per = PickPer;
            float ceil = OriginalMath.Binary32((float)Math.Ceiling((double)(OriginalMath.Binary32(OriginalMath.Binary32((float)list.Count) / OriginalMath.Binary32((float)per)))));
            int pageCount = Math.Abs(ceil) < 2147483648f ? (int)ceil : int.MinValue;
            pageCount = pageCount > 1 ? pageCount : 1;
            int lastPage = pageCount - 1; pickPage = pickPage < 0 ? 0 : pickPage > lastPage ? lastPage : pickPage;
            y  =             OriginalMath.Binary32(y+(92f));
            Label(s, "库存 " + list.Count, 36f, OriginalMath.Binary32(y - 6f), 1.05f, uiDim, -1);
            float rowsY = OriginalMath.Binary32(y + 16f);
            if (list.Count == 0)
            {
                Label(s, "暂无该部位模块，去装备补给抽取吧", 270f, OriginalMath.Binary32(rowsY + 30f), 1.2f, uiDim);
                Button(s, 334, 170f, OriginalMath.Binary32(rowsY + 70f), 200f, 48f, "装备补给", Btn.Primary, 1.6f, true);
            }
            float footer = OriginalMath.Binary32(bottom - 54f);
            for (int row = 0; row < per; row++)
            {
                int index = unchecked(pickPage * per + row); if (index >= list.Count) break;
                FleetCore core = list[index]; float top = OriginalMath.Binary32(rowsY + OriginalMath.Binary32(OriginalMath.Binary32((float)row) * 74f));
                Color quality = FleetCatalog.QualityColors[core.quality];
                bool equipped = Economy.IsEquipped(core.id), merge = Economy.CanMergeModule(core.kind, core.quality);
                Plate(s, 32f, top, 476f, 66f, SourceColor(4469011981886344397UL, 4572054341892754637UL),
                    SourceColor(4428659729042233098UL, 4572054341884366029UL), A(quality, .7f), 6f);
                ModuleIcon(s, core, 40f, OriginalMath.Binary32(top + 7f), 52f);
                Label(s, core.Spec.name, 100f, OriginalMath.Binary32(top + 7f), 1.25f, quality, -1);
                QualityTag(s, core.quality, OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure(core.Spec.name, 1.25f) + 100f) + 8f), OriginalMath.Binary32(top + 8f));
                s.TextF(core.Spec.Describe(core.quality, multiplier), 100f, OriginalMath.Binary32(top + 34f), .98f, uiText, -1);
                int owner = Economy.OwnerOf(core.id);
                s.TextF((owner < 0 ? "" : "装在 " + FleetCatalog.FighterNames[owner] + " · ") + "耐久 " + core.durability + "/" + core.MaxDurability,
                    100f, OriginalMath.Binary32(top + 52f), .82f, owner < 0 ? uiDim : uiGold, -1);
                Button(s, 350 + row, 404f, OriginalMath.Binary32(top + 10f), 96f, 36f, equipped ? "换装" : "装备", Btn.Primary, 1.35f, true);
                if (merge) Button(s, 370 + row, 316f, OriginalMath.Binary32(top + 10f), 82f, 36f, "升阶3→1", Btn.Green, .95f, true);
                else Button(s, 360 + row, 330f, OriginalMath.Binary32(top + 14f), 66f, 30f, "分解", Btn.Ghost, 1f, !equipped);
            }
            if (pageCount >= 2)
            {
                Button(s, 381, 36f, OriginalMath.Binary32(footer + 6f), 70f, 38f, "<", Btn.Secondary, 2f, pickPage > 0);
                Button(s, 382, 434f, OriginalMath.Binary32(footer + 6f), 70f, 38f, ">", Btn.Secondary, 2f, pickPage < lastPage);
                Label(s, (pickPage + 1) + " / " + pageCount, 270f, OriginalMath.Binary32(footer + 14f), 1.2f, uiDim);
            }
        }
    }
}
