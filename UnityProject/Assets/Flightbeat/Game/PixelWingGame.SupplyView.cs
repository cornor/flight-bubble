using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54600. Original fixed-height supply panel, authored tabs and two pools.
        private void DrawSupply(PixelSurface s)
        {
            Backdrop(s, .82f);
            float enter = OutBack(OriginalMath.Binary32(Since(423) / .3f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float top = OriginalMath.Binary32(ScreenLayout.Top + 60f), centered = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - 280f);
            top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (top > centered ? top : centered));
            Plate(s, 28f, top, 484f, 560f, uiPanelHi, uiPanel, supplyPool != 0 ? uiCyan : uiGreen, 7f, alpha);
            Title(s, "补给舱", 270f, OriginalMath.Binary32(top + 16f), 2.1f, Color.white, uiInk);
            Button(s, 422, 452f, OriginalMath.Binary32(top + 10f), 48f, 40f, "X", Btn.Ghost, 2f, true);
            float y = OriginalMath.Binary32(top + 62f);
            for (int pool = 0; pool < 2; pool++)
            {
                float x = OriginalMath.Binary32(48f + OriginalMath.Binary32(OriginalMath.Binary32((float)pool) * 222f)); bool selected = supplyPool == pool;
                HitBox(s, 400 + pool, x, y, 222f, 44f);
                Plate(s, OriginalMath.Binary32(x + 2f), y, 218f, 44f, selected ? new Color(.2f, .55f, .62f, 1f) : new Color(.04f, .1f, .15f, .95f),
                    selected ? new Color(.08f, .28f, .34f, 1f) : new Color(.02f, .05f, .09f, .95f), selected ? uiCyan : A(uiEdge, .7f), 8f);
                Label(s, pool == 0 ? "战机补给" : "装备补给", OriginalMath.Binary32(x + 111f), OriginalMath.Binary32(y + 11f), 1.4f, selected ? Color.white : uiDim);
            }
            y  =             OriginalMath.Binary32(y+(58f));
            if (supplyPool == 0)
            {
                Label(s, "选择目标战机", 50f, y, 1.2f, uiText, -1);
                s.TextF("目标机的部件占 50%，其余均分给另外四架", 50f, OriginalMath.Binary32(y + 26f), .92f, uiDim, -1);
                float rowsTop = OriginalMath.Binary32(y + 54f);
                for (int cell = 0; cell < 6; cell++)
                {
                    int fighter = cell - 1; bool selected = supplyTarget == fighter;
                    float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(cell % 3)) * 146.6666717529297f) + 50f), row = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(OriginalMath.Binary32((float)(cell / 3)) * 74f));
                    HitBox(s, 410 + cell, x, row, 140.6666717529297f, 66f);
                    Color accent = cell == 0 ? uiDim : FleetCatalog.FighterAccent[fighter];
                    Plate(s, x, row, 140.6666717529297f, 66f,
                        selected ? Mix(new Color(.1f, .24f, .3f, 1f), accent, .25f) : new Color(.04f, .1f, .15f, .95f),
                        SourceColor(4417130516401346314UL, 4572054341883023852UL), selected ? accent : A(uiEdge, .6f), 8f);
                    if (cell == 0)
                    {
                        s.TextF("不指定", OriginalMath.Binary32(x + 70.33333587646484f), OriginalMath.Binary32(row + 14f), 1.15f, selected ? Color.white : uiDim, 0);
                        s.TextF("25 个部件均分", OriginalMath.Binary32(x + 70.33333587646484f), OriginalMath.Binary32(row + 40f), .85f, uiDim, 0);
                    }
                    else
                    {
                        bool assembled = Economy.Assembled(fighter);
                        DrawFighterArt(s, fighter, OriginalMath.Binary32(x + 32f), OriginalMath.Binary32(row + 33f), 50f, 0f, -1, assembled ? 1f : .9f, !assembled, false);
                        s.TextF(FleetCatalog.FighterNames[fighter], OriginalMath.Binary32(x + 84f), OriginalMath.Binary32(row + 12f), 1.1f, selected ? Color.white : uiText, 0);
                        s.TextF(assembled ? "LV" + Economy.FighterLevel(fighter) : "部件 " + Economy.Collected(fighter) + "/5", OriginalMath.Binary32(x + 84f), OriginalMath.Binary32(row + 38f), .9f, assembled ? accent : uiDim, 0);
                    }
                    if (selected) s.Corners(OriginalMath.Binary32(x - 2f), OriginalMath.Binary32(row - 2f), 144.6666717529297f, 70f, accent, 8, 2);
                }
                y = OriginalMath.Binary32(OriginalMath.Binary32(rowsTop + 148f) + 6f);
                s.TextF("· 一次抽取获得 1 个部件；首件计入收藏并用于组装新机", 50f, y, .92f, uiText, -1);
                s.TextF("· 重复件用于同部位升级，或任意 3 件熔炼 1 件所需部件", 50f, OriginalMath.Binary32(y + 24f), .92f, uiText, -1);
                s.TextF("· 部件升级：2 / 4 / 8 / 12 件（满级共 27 件）", 50f, OriginalMath.Binary32(y + 48f), .92f, uiDim, -1);
            }
            else
            {
                Label(s, "模块全账号共用，可装进任意战机的预设", 50f, y, 1.1f, uiText, -1);
                string[] rates = { "普通 40%", "精良 30%", "稀有 20%", "史诗 8%", "传说 2%" };
                float rowsTop = OriginalMath.Binary32(y + 36f);
                for (int q = 0; q < 5; q++)
                {
                    float x = OriginalMath.Binary32(50f + OriginalMath.Binary32(OriginalMath.Binary32((float)(q % 3)) * 150f)), row = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(OriginalMath.Binary32((float)(q / 3)) * 34f));
                    s.Rect(x, OriginalMath.Binary32(row + 4f), 14f, 14f, FleetCatalog.QualityColors[q]); s.TextF(rates[q], OriginalMath.Binary32(x + 22f), OriginalMath.Binary32(row + 2f), 1f, uiText, -1);
                }
                y  =                 OriginalMath.Binary32(y+(116f));
                Plate(s, 48f, y, 444f, 86f, SourceColor(4477658893517177487UL, 4572054341894096814UL),
                    SourceColor(4435865490164012810UL, 4572054341885708206UL), A(uiEdge, .7f), 8f);
                s.TextF("史诗保底", 64f, OriginalMath.Binary32(y + 12f), 1f, FleetCatalog.QualityColors[3], -1);
                Bar(s, 158f, OriginalMath.Binary32(y + 16f), 300f, OriginalMath.Binary32(OriginalMath.Binary32((float)Economy.Data.epicPity) / 10f), FleetCatalog.QualityColors[3]);
                s.TextF(Economy.Data.epicPity + "/10", 482f, OriginalMath.Binary32(y + 12f), .95f, uiDim, 1);
                s.TextF("传说保底", 64f, OriginalMath.Binary32(y + 48f), 1f, FleetCatalog.QualityColors[4], -1);
                Bar(s, 158f, OriginalMath.Binary32(y + 52f), 300f, OriginalMath.Binary32(OriginalMath.Binary32((float)Economy.Data.legendPity) / 20f), FleetCatalog.QualityColors[4]);
                s.TextF(Economy.Data.legendPity + "/20", 482f, OriginalMath.Binary32(y + 48f), .95f, uiDim, 1);
                s.TextF("· 三个同名同品质模块可在机库「升阶」为更高品质", 50f, OriginalMath.Binary32(y + 104f), .92f, uiText, -1);
                s.TextF("· 装在战机「共鸣」部位的模块效果 ×1.25", 50f, OriginalMath.Binary32(y + 128f), .92f, uiText, -1);
                s.TextF("· 模块仓 " + Economy.Data.cores.Count + "/120", 50f, OriginalMath.Binary32(y + 152f), .92f, uiDim, -1);
            }
            y = OriginalMath.Binary32(OriginalMath.Binary32(top + 560f) - 78f);
            int fragments = Economy.Data.fragments;
            Button(s, 420, 50f, y, 210f, 58f, "单抽", fragments > 9 ? Btn.Green : Btn.Secondary, 1.7f, fragments > 9);
            Button(s, 421, 280f, y, 210f, 58f, "十连", fragments > 89 ? Btn.Primary : Btn.Secondary, 1.7f, fragments > 89);
            s.DeckArt(2, 124f, OriginalMath.Binary32(y + 60f), 18f, 18f, Color.white, true); s.Text("10", 146f, OriginalMath.Binary32(y + 64f), 1f, uiCyan, false);
            s.DeckArt(2, 346f, OriginalMath.Binary32(y + 60f), 18f, 18f, Color.white, true); s.Text("90", 368f, OriginalMath.Binary32(y + 64f), 1f, uiCyan, false);
            Plate(s, 408f, OriginalMath.Binary32(y - 24f), 86f, 22f, SourceColor(4518011146384441344UL, 4575657222458677658UL),
                SourceColor(4453159313464098816UL, 4575657222445255885UL), uiInk, 5f);
            s.TextF("9折", 451f, OriginalMath.Binary32(y - 21f), .95f, Color.white, 0);
        }
        // f54667.
        private void Bar(PixelSurface s, float x, float y, float width, float fraction, Color color)
        {
            s.Rect(x, y, width, 10f, SourceColor(4417130516401346314UL, 4575657222442571530UL));
            s.Rect(OriginalMath.Binary32(x + 1f), OriginalMath.Binary32(y + 1f), OriginalMath.Binary32((OriginalMath.Binary32(width - 2f)) * (fraction < 0f ? 0f : OriginalMath.WasmMin(fraction, 1f))), 8f, color);
        }
    }
}
