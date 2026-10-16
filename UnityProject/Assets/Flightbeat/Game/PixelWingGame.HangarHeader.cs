using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54592 dispatch order: gear is behind the header, other tabs follow it.
        private void DrawUpgrade(PixelSurface s, float slide, float enter)
        {
            float top = UpgradeTop, tabs = OriginalMath.Binary32(top + 102f), content = OriginalMath.Binary32(tabs + 54f), bottom = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.SafeBottom - 96f) - 6f);
            if (hangarTab == 1) DrawGearTab(s, content, bottom, slide);
            DrawFleetStrip(s, top, slide); DrawHangarTabs(s, tabs, slide);
            if (hangarTab == 0) DrawPartsTab(s, content, bottom, slide);
            else if (hangarTab != 1) DrawBonusTab(s, content, bottom, slide);
            if (moduleInspect >= 0) DrawModulePicker(s);
            DrawEquipFx(s);
        }
        // f54611.
        private void DrawFleetStrip(PixelSurface s, float y, float slide)
        {
            for (int fighter = 0; fighter < 5; fighter++)
            {
                bool selected = hangarFighter == fighter, active = Economy.ActiveFighter == fighter, assembled = Economy.Assembled(fighter);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(slide + 12f) + OriginalMath.Binary32(OriginalMath.Binary32((float)fighter) * 104f));
                HitBox(s, 300 + fighter, x, y, 100f, 94f);
                float pulse = PressPulse(300 + fighter), shift = OriginalMath.Binary32(pulse + pulse), shrink = OriginalMath.Binary32(pulse * 4f);
                Color tint = FleetCatalog.FighterAccent[fighter];
                Plate(s, OriginalMath.Binary32(x + shift), OriginalMath.Binary32(shift + y), OriginalMath.Binary32(100f - shrink), OriginalMath.Binary32(94f - shrink),
                    selected ? Mix(new Color(.08f, .22f, .3f, .97f), tint, .18f) : new Color(.04f, .11f, .17f, .93f),
                    SourceColor(4417130516401346314UL, 4572054341883023852UL), selected ? tint : A(uiEdge, .7f), 8f, 1f);
                float cx = OriginalMath.Binary32(x + 50f);
                if (selected)
                {
                    s.Glow(cx, OriginalMath.Binary32(y + 47f), 130f, 114f, A(tint, .18f), 0f);
                    s.Rect(OriginalMath.Binary32(x + 14f), OriginalMath.Binary32(OriginalMath.Binary32(y + 94f) - 4f), 72f, 3f, tint);
                }
                DrawFighterArt(s, fighter, cx, OriginalMath.Binary32(y + 38f), 54f, 0f, -1, assembled ? 1f : .9f, !assembled, false);
                Label(s, FleetCatalog.FighterNames[fighter], cx, OriginalMath.Binary32(y + 66f), 1.05f, assembled ? Color.white : uiDim);
                if (assembled)
                {
                    int level = Economy.FighterLevel(fighter);
                    s.Text("LV" + level, OriginalMath.Binary32(x + 6f), OriginalMath.Binary32(y + 6f), 1f, level > 4 ? uiGold : uiCyan, false);
                    if (active)
                    {
                        float right = OriginalMath.Binary32(x + 100f);
                        Plate(s, OriginalMath.Binary32(right - 44f), OriginalMath.Binary32(y + 3f), 40f, 18f, SourceColor(4489188110472157594UL, 4575657222434182922UL),
                            SourceColor(4441630096508950938UL, 4575657222417405706UL), uiGold, 4f, 1f);
                        s.TextF("出战", OriginalMath.Binary32(right - 24f), OriginalMath.Binary32(y + 4f), .9f, uiGoldHi, 0);
                    }
                }
                else
                {
                    int collected = Economy.Collected(fighter);
                    s.Rect(OriginalMath.Binary32(x + 8f), OriginalMath.Binary32(y + 84f), 84f, 5f, SourceColor(4417130516401346314UL, 4575657222442571530UL));
                    s.Rect(OriginalMath.Binary32(x + 9f), OriginalMath.Binary32(y + 85f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)collected) * 82f) / 5f), 3f, Economy.CanAssemble(fighter) ? uiGreen : uiCyan);
                    s.TextF(collected + "/5", OriginalMath.Binary32(OriginalMath.Binary32(x + 100f) - 20f), OriginalMath.Binary32(y + 4f), .9f, Economy.CanAssemble(fighter) ? uiGreen : uiDim, 0);
                }
                if (HangarReadyCount(fighter) != 0 && Economy.Data.unlocked >= 3) RedDot(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 100f) - 4f), OriginalMath.Binary32(y + 26f), 0);
            }
        }
        // f54611/f54612 source addition order, with no short-circuit query skips.
        private int HangarReadyCount(int fighter)
        {
            int count = Economy.CanAssemble(fighter) ? 1 : 0;
            for (int part = 0; part < 5; part++) count += Economy.CanUpgradePart(fighter, part) ? 1 : 0;
            return count;
        }
        // f54612.
        private void DrawHangarTabs(PixelSurface s, float y, float slide)
        {
            float x = OriginalMath.Binary32(slide + 30f);
            Plate(s, x, y, 480f, 46f, SourceColor(4447394707105235599UL, 4572054341888728105UL),
                SourceColor(4417130516401346314UL, 4572054341883023852UL), A(uiEdge, .8f), 10f, 1f);
            float start = OriginalMath.Binary32(OriginalMath.Binary32(x + 4f) + OriginalMath.Binary32(OriginalMath.Binary32((float)previousHangarTab) * 160f)), end = OriginalMath.Binary32(OriginalMath.Binary32(x + 4f) + OriginalMath.Binary32(OriginalMath.Binary32((float)hangarTab) * 160f));
            Plate(s, OriginalMath.Binary32(start + OriginalMath.Binary32((OriginalMath.Binary32(end - start)) * OutBack(OriginalMath.Binary32(Since(495) / .3f), 1.1f))), OriginalMath.Binary32(y + 4f), 152f, 38f,
                SourceColor(4543231305995570381UL, 4575657222467401810UL), SourceColor(4507923084218717962UL, 4575657222460019835UL), uiCyan, 8f, 1f);
            string[] labels = { "部件", "装备", "加成" };
            for (int tab = 0; tab < 3; tab++)
            {
                float tx = OriginalMath.Binary32(x + OriginalMath.Binary32((float)(tab * 160)));
                HitBox(s, 310 + tab, tx, y, 160f, 46f);
                Label(s, labels[tab], OriginalMath.Binary32(tx + 80f), OriginalMath.Binary32(y + 11f), 1.5f, hangarTab == tab ? Color.white : uiDim);
            }
            if (Economy.Data.unlocked >= 3 && HangarReadyCount(hangarFighter) != 0) RedDot(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 80f) + 34f), OriginalMath.Binary32(y + 8f), 0);
        }
        // f54616.
        private void DrawEquipFx(PixelSurface s)
        {
            if (equipFlash < 0) return;
            float age = Since(192);
            if (age > .9f) { equipFlash = -1; return; }
            float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(UpgradeTop + 156f) + 58f) + OriginalMath.Binary32((float)(equipFlash * 84))) + 36f);
            float t = OriginalMath.Binary32(age / .9f), fade = OriginalMath.Binary32(1f - t), scale = OriginalMath.Binary32(OriginalMath.Binary32(t * -.5f) + 1f);
            Color glow = uiCyan; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(glow.a * fade) * .6f);
            s.Glow(110f, y, OriginalMath.Binary32(scale * 200f), OriginalMath.Binary32(scale * 120f), glow, 0f);
            s.Ring(new Vector2(110f, y), OriginalMath.Binary32(OriginalMath.Binary32(t * 80f) + 20f), new Color(1f, 1f, 1f, fade), 3f, 32, 0f, 360f);
        }
    }
}
