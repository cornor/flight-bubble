using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54861 static +12, metadata 0x36EDC0, SHA256 603F0C...4696EF62.
        private static readonly int[] bonusBaseStats = { 0, 3, 4, 7, 15 };
        // f54614. Row selection and spacing come from source code/static data.
        private void DrawBonusTab(PixelSurface s, float y, float bottom, float slide)
        {
            int fighter = hangarFighter; FleetBonus bonus = Economy.Bonus(fighter);
            Color accent = FleetCatalog.FighterAccent[fighter]; float left = OriginalMath.Binary32(slide + 24f);
            Label(s, FleetCatalog.FighterNames[fighter] + " · 整体加成", OriginalMath.Binary32(left + 4f), y, 1.5f, Color.white, -1);
            s.TextF("机型 + 部件 + 装备（共鸣×1.25），合计不超过上限", OriginalMath.Binary32(left + 4f), OriginalMath.Binary32(y + 30f), .92f, uiDim, -1);
            var stats = new List<int>();
            for (int i = 0; i < 16; i++) if (Array.IndexOf(bonusBaseStats, i) >= 0 || Math.Abs(bonus.Raw(i)) > .0001f) stats.Add(i);
            y  =             OriginalMath.Binary32(y+(56f));
            float height = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(bottom - y) - 40f) - OriginalMath.Binary32((float)(CountLegend(bonus.legendary) * 28 + 56)))) / OriginalMath.Binary32((float)(stats.Count + 1)));
            height = height < 26f ? 26f : OriginalMath.WasmMin(height, 36f);
            Plate(s, left, y, 492f, height, SourceColor(4489188110456051466UL, 4572054341897116713UL),
                SourceColor(4453159313428698890UL, 4572054341889399194UL), A(accent, .8f), 6f, 1f);
            float textY = OriginalMath.Binary32(OriginalMath.Binary32(y + OriginalMath.Binary32(height * .5f)) - 8f), nameX = OriginalMath.Binary32(left + 10f), leanX = OriginalMath.Binary32(left + 168f), partX = OriginalMath.Binary32(left + 238f), moduleX = OriginalMath.Binary32(left + 308f), right = OriginalMath.Binary32(OriginalMath.Binary32(left + 492f) - 12f);
            s.TextF("项目", nameX, textY, .95f, uiDim, -1); s.TextF("机型", leanX, textY, .95f, uiDim, 0);
            s.TextF("部件", partX, textY, .95f, uiDim, 0); s.TextF("装备", moduleX, textY, .95f, uiDim, 0);
            s.TextF("合计 / 上限", right, textY, .95f, uiDim, 1);
            float rowsTop = OriginalMath.Binary32(OriginalMath.Binary32(y + height) + 2f);
            for (int row = 0; row < stats.Count; row++)
            {
                int stat = stats[row]; float rowY = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(height * OriginalMath.Binary32((float)row)));
                if ((row & 1) == 0) s.Rect(OriginalMath.Binary32(left + 2f), rowY, 488f, OriginalMath.Binary32(height - 2f), SourceColor(4453159313428698890UL, 4554039943379917210UL));
                textY = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + rowY) - 8f);
                s.TextF(FleetCatalog.StatNames[stat], nameX, textY, 1f, Color.white, -1);
                BonusCell(s, stat, bonus.lean[stat], leanX, textY); BonusCell(s, stat, bonus.parts[stat], partX, textY);
                BonusCell(s, stat, bonus.modules[stat], moduleX, textY);
                float cap = FleetCatalog.StatCap[stat], total = bonus.total[stat], ratio = OriginalMath.Binary32(Math.Abs(total) / cap);
                s.Rect(OriginalMath.Binary32(right - 64f), OriginalMath.Binary32(OriginalMath.Binary32(height + rowY) - 7f), 64f, 3f, SourceColor(4483423504138292429UL, 4575657222456999936UL));
                s.Rect(OriginalMath.Binary32(right - 64f), OriginalMath.Binary32(OriginalMath.Binary32(height + rowY) - 7f), ratio < 0f ? 0f : OriginalMath.Binary32(OriginalMath.WasmMin(ratio, 1f) * 64f), 3f, bonus.capped[stat] ? uiGold : accent);
                s.TextF(FleetCatalog.StatValue((FleetCatalog.Stat)stat, total), OriginalMath.Binary32(right - 58f), OriginalMath.Binary32(textY - 1f), 1.05f,
                    bonus.capped[stat] ? uiGoldHi : total < 0f ? uiRed : uiGreen, 1);
                string limit = FleetCatalog.StatValue((FleetCatalog.Stat)stat, total < 0f ? -cap : cap);
                if (!limit.StartsWith("-")) limit = limit.TrimStart('+');
                s.TextF("/" + limit, right, OriginalMath.Binary32(textY + 1f), .8f, bonus.capped[stat] ? uiGold : uiDim, 1);
            }
            y = OriginalMath.Binary32(OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(height * OriginalMath.Binary32((float)stats.Count))) + 8f);
            Plate(s, left, y, 492f, 28f, SourceColor(4471894287194049741UL, 4572054341892754637UL),
                SourceColor(4428659729042233098UL, 4572054341884366029UL), A(uiCyan, .7f), 6f, 1f);
            textY = OriginalMath.Binary32(y + 6f);
            s.TextF("渗透", nameX, textY, 1f, Color.white, -1); s.TextF("—", leanX, textY, 1f, uiDim, 0);
            s.TextF(bonus.pierce > 0 ? bonus.pierce + "层" : "0", partX, textY, 1f, bonus.pierce > 0 ? uiCyan : uiDim, 0);
            s.TextF("—", moduleX, textY, 1f, uiDim, 0);
            s.TextF(bonus.pierce + " 层 / 3", right, textY, 1.05f, bonus.pierce > 0 ? uiGreen : uiDim, 1);
            y  =             OriginalMath.Binary32(y+(32f));
            s.TextF("机首 LV" + Economy.PartLevel(fighter, 0) + "：子弹可穿过 " + bonus.pierce + " 层异色敌人打到同色目标", OriginalMath.Binary32(left + 8f), y, .9f, uiDim, -1);
            y  =             OriginalMath.Binary32(y+(24f));
            for (int slot = 0; slot < 5; slot++)
                if ((bonus.legendary & FleetCatalog.SlotLegend[slot]) != 0)
                {
                    s.TextF("传说·" + FleetCatalog.LegendNames[slot] + "：" + FleetCatalog.LegendText[slot], OriginalMath.Binary32(left + 8f), y, .92f, uiGold, -1);
                    y  =                     OriginalMath.Binary32(y+(24f));
                }
            if (!Economy.Assembled(fighter)) s.TextF("（未组装：按 1 级部件预览）", OriginalMath.Binary32(left + 8f), y, .9f, uiDim, -1);
            y  =             OriginalMath.Binary32(y+(30f));
            if (bottom > OriginalMath.Binary32(y + 120f))
            {
                Plate(s, left, y, 492f, 52f, Mix(new Color(.05f, .14f, .2f, .95f), accent, .12f),
                    SourceColor(4428659729042233098UL, 4572054341884366029UL), A(accent, .8f), 8f, 1f);
                s.TextF("机型特点", OriginalMath.Binary32(left + 12f), OriginalMath.Binary32(y + 6f), .95f, accent, -1);
                s.TextF(FleetCatalog.FighterStyle[fighter], OriginalMath.Binary32(left + 96f), OriginalMath.Binary32(y + 6f), .95f, Color.white, -1);
                s.TextF(LeanText(fighter), OriginalMath.Binary32(left + 12f), OriginalMath.Binary32(y + 28f), .9f, uiText, -1);
                s.TextF("共鸣：" + FleetCatalog.SlotNames[FleetCatalog.FighterResonance[fighter]] + "模块 ×1.25", right, OriginalMath.Binary32(y + 28f), .9f, uiText, 1);
                y  =                 OriginalMath.Binary32(y+(60f));
            }
            if (Economy.Assembled(fighter) && OriginalMath.Binary32(y + 70f) < bottom) DrawStageTrack(s, fighter, left, y, 492f, 64f);
        }
        // f54687.
        private static int CountLegend(LegendaryKind flags)
        {
            int count = 0; for (int slot = 0; slot < 5; slot++) count += (flags & FleetCatalog.SlotLegend[slot]) != 0 ? 1 : 0; return count;
        }
        // f54688.
        private void BonusCell(PixelSurface s, int stat, float value, float x, float y)
        {
            if (Math.Abs(value) < .0001f) s.TextF("—", x, y, 1f, A(uiDim, .6f), 0);
            else s.TextF(FleetCatalog.StatValue((FleetCatalog.Stat)stat, value), x, y, 1f, value < 0f ? uiRed : uiText, 0);
        }
        // f54689.
        private static string LeanText(int fighter)
        {
            var values = new float[16]; FleetCatalog.Lean(fighter, values); var labels = new List<string>();
            for (int i = 0; i < 16; i++) if (Math.Abs(values[i]) > .0001f) labels.Add(FleetCatalog.StatNames[i] + " " + FleetCatalog.StatValue((FleetCatalog.Stat)i, values[i]));
            return labels.Count > 0 ? "机型加成：" + string.Join("  ", labels) : "机型加成：无（均衡基准）";
        }
    }
}
