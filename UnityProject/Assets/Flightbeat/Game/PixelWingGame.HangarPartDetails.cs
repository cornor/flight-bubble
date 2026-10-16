using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54684. Five level marks retain the distinct folded f32 trig values.
        private void DrawPartNode(PixelSurface s, int fighter, int part, Vector2 at)
        {
            HitBox(s, 320 + part, OriginalMath.Binary32(at.x - 40f), OriginalMath.Binary32(at.y - 40f), 80f, 80f);
            float pulse = PressPulse(320 + part); int level = Economy.PartLevel(fighter, part);
            bool selected = part == hangarPart, canUpgrade = Economy.CanUpgradePart(fighter, part);
            float radius = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * .2f) + 1f)) * (selected ? 17f : 14f));
            Color accent = FleetCatalog.FighterAccent[fighter], ring = level <= 0 ? A(uiDim, .6f) : level < 5 ? accent : uiGold;
            if (selected)
            {
                s.Glow(at.x, at.y, OriginalMath.Binary32(radius * 5f), OriginalMath.Binary32(radius * 5f), A(accent, .3f), 0f);
                for (int arc = 0; arc < 8; arc++)
                    s.Ring(at, OriginalMath.Binary32(radius + 5f), new Color(1f, 1f, 1f, .9f), 2f, 3, OriginalMath.Binary32(OriginalMath.Binary32(ui * 60f) + OriginalMath.Binary32((float)(arc * 45))), 20f);
            }
            Disc(s, at.x, at.y, radius, SourceColor(4428659729042233098UL, 1036831949u, selected ? .9f : .72f));
            s.Ring(at, radius, ring, 2f, 24, 0f, 360f);
            float r = OriginalMath.Binary32(radius + 1f), q = OriginalMath.Binary32(r * -.5735763311386108f);
            Color dim = new Color(.15f, .22f, .3f, 1f);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + OriginalMath.Binary32(r * .9396926164627075f)) - 1.5f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(r * -.3420201539993286f) + at.y) - 1.5f), 3f, 3f, level > 0 ? ring : dim);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x - q) - 1.5f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(r * -.8191521167755127f) + at.y) - 1.5f), 3f, 3f, level > 1 ? ring : dim);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + OriginalMath.Binary32(r * -1.1924880638503055e-8f)) - 1.5f), OriginalMath.Binary32(OriginalMath.Binary32(at.y - r) - 1.5f), 3f, 3f, level > 2 ? ring : dim);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + q) - 1.5f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(r * -.8191521167755127f) + at.y) - 1.5f), 3f, 3f, level > 3 ? ring : dim);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + OriginalMath.Binary32(r * -.9396925568580627f)) - 1.5f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(r * -.3420203626155853f) + at.y) - 1.5f), 3f, 3f, level > 4 ? ring : dim);
            if (level > 0) s.Text(level >= 5 ? "M" : level.ToString(), at.x, selected ? OriginalMath.Binary32(at.y - 7f) : OriginalMath.Binary32(at.y - 6f), selected ? 2f : 1f, level >= 5 ? uiGoldHi : Color.white, true);
            else s.Text("?", at.x, OriginalMath.Binary32(at.y - 6f), 1f, uiDim, true);
            if (selected)
            {
                string name = FleetCatalog.PartNames[part]; float width = OriginalMath.Binary32(PixelSurface.Measure(name, .95f) + 10f);
                s.Rect(OriginalMath.Binary32(at.x + OriginalMath.Binary32(width * -.5f)), OriginalMath.Binary32(OriginalMath.Binary32(at.y + radius) + 5f), width, 16f, SourceColor(4405601299457103626UL, 4568451461319449641UL));
                s.TextF(name, at.x, OriginalMath.Binary32(OriginalMath.Binary32(at.y + radius) + 6f), .95f, Color.white, 0);
            }
            if (canUpgrade && Economy.Data.unlocked >= 3)
            {
                float shift = OriginalMath.Binary32(radius * .85f), wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 6f) + OriginalMath.Binary32((float)part)));
                float x = OriginalMath.Binary32(at.x + shift), y = OriginalMath.Binary32(OriginalMath.Binary32(at.y - shift) + (OriginalMath.Binary32(wave + wave)));
                Disc(s, x, y, 8f, uiGreen);
                s.Segment(new Vector2(OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y + 4f)), new Vector2(OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y - 4f)), 2.5f, uiInk);
                s.Segment(new Vector2(OriginalMath.Binary32(x - 4f), OriginalMath.Binary32(y + 0f)), new Vector2(OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y - 4f)), 2.5f, uiInk);
                s.Segment(new Vector2(OriginalMath.Binary32(x + 4f), OriginalMath.Binary32(y + 0f)), new Vector2(OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y - 4f)), 2.5f, uiInk);
            }
        }

        // f54685.
        private void DrawPartCard(PixelSurface s, int fighter, int part, float x, float y, float width, float height)
        {
            bool assembled = Economy.Assembled(fighter); int level = Economy.PartLevel(fighter, part);
            int index = FleetCatalog.PartIndex(fighter, part), spare = Economy.Spare(fighter, part);
            Color accent = FleetCatalog.FighterAccent[fighter];
            Plate(s, x, y, width, height, SourceColor(4471894287194049741UL, 4572774917146610237UL),
                SourceColor(4428659729042233098UL, 4572774917137550541UL), A(accent, .8f), 10f);
            s.Rect(OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(y + 12f), 84f, 84f, SourceColor(4428659729042233098UL, 4575657222445255885UL));
            s.Border(OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(y + 12f), 84f, 84f, A(accent, .7f), 2f);
            s.Glow(OriginalMath.Binary32(x + 54f), OriginalMath.Binary32(y + 54f), 90f, 90f, A(accent, .2f), 0f);
            DrawPartThumb(s, fighter, part, OriginalMath.Binary32(x + 54f), OriginalMath.Binary32(y + 54f), 72f, 1f, level == 0);
            if (level == 0) s.TextF("未获得", OriginalMath.Binary32(x + 54f), OriginalMath.Binary32(y + 46f), .95f, uiDim, 0);
            float tx = OriginalMath.Binary32(x + 108f), right = OriginalMath.Binary32(x + width), bottom = OriginalMath.Binary32(y + height);
            Label(s, FleetCatalog.PartNames[part] + " · " + FleetCatalog.PartRoles[part], tx, OriginalMath.Binary32(y + 10f), 1.45f, Color.white, -1);
            string status = level == 0 ? "未收集" : !assembled ? "已收集" : level <= 4 ? "LV" + level : "MAX";
            float statusWidth = OriginalMath.Binary32(PixelSurface.Measure(status, 1f) + 14f);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(right - statusWidth) - 12f), OriginalMath.Binary32(y + 11f), statusWidth, 22f,
                level <= 4 ? new Color(.08f, .22f, .3f, 1f) : new Color(.4f, .26f, .04f, 1f),
                level <= 4 ? new Color(.03f, .1f, .14f, 1f) : new Color(.2f, .1f, .01f, 1f), level <= 4 ? accent : uiGold, 5f);
            s.TextF(status, OriginalMath.Binary32(OriginalMath.Binary32(right + OriginalMath.Binary32(statusWidth * -.5f)) - 12f), OriginalMath.Binary32(y + 14f), 1f, level >= 5 ? uiGoldHi : Color.white, 0);
            if (!assembled)
            {
                s.TextF(level != 0 ? "首件已收集（永久计入收藏，不会被熔炼）" : "战机补给可抽到；或用 3 件任意重复件熔炼补齐", tx, OriginalMath.Binary32(y + 44f), 1f, level != 0 ? uiGreen : uiDim, -1);
                s.TextF("组装后基础效果：" + FleetCatalog.PartEffect(part, 1), tx, OriginalMath.Binary32(y + 70f), .95f, uiText, -1);
                if (spare > 0) s.TextF("重复件 ×" + spare + "（组装后用于升级）", tx, OriginalMath.Binary32(y + 94f), .95f, uiCyan, -1);
                bool target = Economy.SmeltTarget(index);
                Button(s, 330, OriginalMath.Binary32(right - 176f), OriginalMath.Binary32(bottom - 50f), 164f, 40f, "熔炼", target ? Btn.Secondary : Btn.Ghost, 1.2f, target);
                if (target)
                {
                    string count = "可用重复件 " + Economy.SmeltableSpares(index);
                    s.TextF(count, OriginalMath.Binary32(right - 190f), OriginalMath.Binary32(bottom - 38f), .9f, Economy.SmeltableSpares(index) > 2 ? uiCyan : uiDim, 1);
                }
                return;
            }
            s.TextF("当前", tx, OriginalMath.Binary32(y + 42f), .95f, uiDim, -1);
            s.TextF(FleetCatalog.PartEffect(part, level), OriginalMath.Binary32(tx + 40f), OriginalMath.Binary32(y + 42f), .98f, uiText, -1);
            if (level <= 4)
            {
                s.TextF("下级", tx, OriginalMath.Binary32(y + 64f), .95f, uiDim, -1);
                s.TextF(FleetCatalog.PartEffect(part, level + 1), OriginalMath.Binary32(tx + 40f), OriginalMath.Binary32(y + 64f), .98f, uiGreen, -1);
                s.TextF("外观：" + FleetCatalog.PartLook[level + 1], tx, OriginalMath.Binary32(y + 86f), .92f, A(accent, .95f), -1);
            }
            else s.TextF("已满级 · " + FleetCatalog.PartLook[5], tx, OriginalMath.Binary32(y + 64f), .98f, uiGoldHi, -1);
            int cost = Economy.UpgradeCost(fighter, part); float barY = OriginalMath.Binary32(bottom - 34f);
            if (level <= 4)
            {
                s.TextF("重复件 " + spare + "/" + cost, OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(barY - 22f), 1f, spare < cost ? uiText : uiGreen, -1);
                s.Rect(OriginalMath.Binary32(x + 12f), barY, 176f, 10f, SourceColor(4417130516401346314UL, 4575657222442571530UL));
                float ratio = OriginalMath.Binary32(OriginalMath.Binary32((float)spare) / OriginalMath.Binary32((float)cost));
                s.Rect(OriginalMath.Binary32(x + 13f), OriginalMath.Binary32(barY + 1f), OriginalMath.Binary32((ratio < 0f ? 0f : OriginalMath.WasmMin(ratio, 1f)) * 174f), 8f, spare < cost ? accent : uiGreen);
            }
            else s.TextF("重复件 ×" + spare + "（可熔炼给其他部件）", OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(barY - 18f), 1f, uiDim, -1);
            bool upgrade = Economy.CanUpgradePart(fighter, part), smelt = Economy.SmeltTarget(index);
            if (level > 4) return;
            Button(s, 330, OriginalMath.Binary32(right - 286f), OriginalMath.Binary32(bottom - 50f), 126f, 40f, "熔炼", smelt ? Btn.Secondary : Btn.Ghost, 1.2f, smelt);
            Button(s, 180, OriginalMath.Binary32(right - 150f), OriginalMath.Binary32(bottom - 50f), 138f, 40f, "升级", upgrade ? Btn.Primary : Btn.Secondary, 1.6f, upgrade);
            if (!Economy.Data.growthTutorial && (fighter | part) == 0 && Economy.Data.unlocked >= 3)
            {
                s.Corners(OriginalMath.Binary32(right - 156f), OriginalMath.Binary32(bottom - 56f), 150f, 52f, A(uiGold, OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 5f)) * .5f) + .5f)) * .4f) + .6f)), 10, 3);
                Label(s, "升级机首，解锁「渗透」", OriginalMath.Binary32(right - 80f), OriginalMath.Binary32(bottom - 78f), 1f, uiGoldHi);
            }
        }

        // f54686. Whole-airframe level is the minimum of the five part levels.
        private void DrawStageTrack(PixelSurface s, int fighter, float x, float y, float width, float height)
        {
            int level = Economy.FighterLevel(fighter); Color accent = FleetCatalog.FighterAccent[fighter];
            Plate(s, x, y, width, height, SourceColor(4458923919749142282UL, 4572054341890741371UL),
                SourceColor(4417130516401346314UL, 4572054341883023852UL), A(uiEdge, .7f), 8f);
            s.TextF("整机", OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(y + 6f), .95f, uiDim, -1);
            s.Text("LV" + level, OriginalMath.Binary32(x + 50f), OriginalMath.Binary32(y + 8f), 1f, level >= 5 ? uiGold : accent, false);
            if (level <= 4)
            {
                var parts = new List<string>();
                for (int part = 0; part < 5; part++) if (Economy.PartLevel(fighter, part) <= level) parts.Add(FleetCatalog.PartNames[part]);
                string target = level == 2 ? "专属决战「" + FleetCatalog.FighterFinale[fighter] + "」" : FleetCatalog.StageNames[level + 1];
                s.TextF("解锁" + target + "：" + (parts.Count == 5 ? "五个部位都" : string.Join("·", parts)) + "需升到 LV" + (level + 1),
                    OriginalMath.Binary32(OriginalMath.Binary32(x + width) - 12f), OriginalMath.Binary32(y + 6f), .85f, level == 2 ? uiGoldHi : uiText, 1);
            }
            else s.TextF("五个阶段全部解锁", OriginalMath.Binary32(OriginalMath.Binary32(x + width) - 12f), OriginalMath.Binary32(y + 6f), .9f, uiGoldHi, 1);
            string[] stages = { "机体", "尾焰", "专属决战", "形态进化", "决战强化" };
            float start = OriginalMath.Binary32(x + 46f), row = OriginalMath.Binary32(y + 34f), step = OriginalMath.Binary32((OriginalMath.Binary32(width - 92f)) * .25f);
            s.Rect(start, row, OriginalMath.Binary32(step * 4f), 3f, SourceColor(4489188110461420175UL, 4575657222458006569UL));
            s.Rect(start, row, OriginalMath.Binary32(step * System.Math.Max(level - 1, 0)), 3f, level >= 5 ? uiGold : accent);
            for (int i = 0; i < 5; i++)
            {
                float cx = OriginalMath.Binary32(start + OriginalMath.Binary32(step * OriginalMath.Binary32((float)i))); bool unlocked = level > i;
                Disc(s, cx, OriginalMath.Binary32(row + 1f), unlocked ? 7f : 6f, unlocked ? level >= 5 ? uiGold : accent : new Color(.12f, .2f, .28f, 1f));
                if (unlocked) s.Rect(OriginalMath.Binary32(cx - 1f), row, 3f, 3f, Color.white);
                else if (level == i) s.Ring(new Vector2(cx, OriginalMath.Binary32(row + 1f)), 9f, A(accent, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 4f)) * .5f) + .5f)), 1.5f, 16, 0f, 360f);
                s.TextF(stages[i], cx, OriginalMath.Binary32(row + 10f), .82f, unlocked ? Color.white : uiDim, 0);
            }
        }
    }
}
