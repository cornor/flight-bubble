using Flightbeat.Model;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54602/f53958 and the f53959 id-predicate alias used here.
        private void DrawRecycle(PixelSurface s)
        {
            if (!recycleOpen) return;
            Backdrop(s, .84f); List<FleetCore> list = RecycleList();
            recyclePick.RemoveWhere(id => !list.Exists(core => core.id == id));
            int pages = (list.Count + 6) / 7; pages = pages > 1 ? pages : 1;
            recyclePage = recyclePage < 0 ? 0 : recyclePage > pages - 1 ? pages - 1 : recyclePage;
            float enter = OutBack(OriginalMath.Binary32(Since(776) / .3f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.2f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - 320f)));
            Plate(s, 34f, top, 472f, 640f, uiPanelHi, uiPanel, uiCyan, 7f, alpha);
            Title(s, "分解模块", 270f, OriginalMath.Binary32(top + 16f), 2.1f, uiText, uiInk);
            Button(s, 773, 452f, OriginalMath.Binary32(top + 12f), 44f, 40f, "X", Btn.Ghost, 2f, true);
            s.TextF("勾选要分解的模块，已装备的模块不会出现在这里", 270f, OriginalMath.Binary32(top + 58f), .95f, uiDim, 0);
            float rowsTop = OriginalMath.Binary32(top + 86f);
            if (list.Count == 0) Label(s, "没有可分解的模块", 270f, OriginalMath.Binary32(rowsTop + 150f), 1.4f, uiDim);
            for (int row = 0; row < 7; row++)
            {
                int index = recyclePage * 7 + row; if (index >= list.Count) break;
                FleetCore core = list[index]; bool selected = recyclePick.Contains(core.id);
                Color quality = FleetCatalog.QualityColors[core.quality]; float y = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32((float)(row * 62)));
                HitBox(s, 760 + row, 52f, y, 436f, 56f); float press = PressPulse(760 + row), shrink = OriginalMath.Binary32(press + press);
                Plate(s, OriginalMath.Binary32(press + 52f), OriginalMath.Binary32(y + press), OriginalMath.Binary32(436f - shrink), OriginalMath.Binary32(56f - shrink),
                    selected ? Mix(new Color(.12f, .08f, .06f, .96f), uiRed, .22f) : Mix(new Color(.05f, .13f, .2f, .96f), quality, .1f),
                    SourceColor(4417130516401346314UL, 4572774917136208364UL), selected ? uiRed : A(quality, .75f), 7f);
                ModuleIcon(s, core, 62f, OriginalMath.Binary32(y + 7f), 42f);
                s.TextF(core.Spec.name, 114f, OriginalMath.Binary32(y + 7f), 1.05f, Color.white, -1);
                s.TextF(FleetCatalog.QualityNames[core.quality], OriginalMath.Binary32(OriginalMath.Binary32(PixelSurface.Measure(core.Spec.name, 1.05f) + 114f) + 10f), OriginalMath.Binary32(y + 9f), .9f, quality, -1);
                s.TextF("耐久 " + core.durability + "/" + core.MaxDurability, 114f, OriginalMath.Binary32(y + 32f), .85f,
                    unchecked(core.durability * 3) > core.MaxDurability ? uiDim : uiRed, -1);
                DurabilityBar(s, core, 200f, OriginalMath.Binary32(y + 37f), 90f);
                s.TextF("+" + FleetCatalog.RecycleValue[core.quality] + " 碎片", 398f, OriginalMath.Binary32(y + 31f), .95f, uiCyan, 1);
                float checkY = OriginalMath.Binary32(y + 14f);
                if (selected)
                {
                    s.Rect(446f, checkY, 28f, 28f, uiRed); s.Border(446f, checkY, 28f, 28f, Color.white, 2f);
                    s.Segment(new Vector2(452f, OriginalMath.Binary32(checkY + 14f)), new Vector2(458f, OriginalMath.Binary32(checkY + 21f)), 3f, Color.white);
                    s.Segment(new Vector2(458f, OriginalMath.Binary32(checkY + 21f)), new Vector2(469f, OriginalMath.Binary32(checkY + 7f)), 3f, Color.white);
                }
                else
                {
                    s.Rect(446f, checkY, 28f, 28f, SourceColor(4428659729042233098UL, 4575657222445255885UL));
                    s.Border(446f, checkY, 28f, 28f, A(uiEdge, .9f), 2f);
                }
            }
            float bottom = OriginalMath.Binary32(OriginalMath.Binary32(top + 640f) - 132f);
            if (pages >= 2)
            {
                Button(s, 774, 52f, bottom, 64f, 36f, "<", Btn.Secondary, 1.4f, recyclePage > 0);
                s.TextF((recyclePage + 1) + " / " + pages, 270f, OriginalMath.Binary32(bottom + 10f), 1.1f, uiText, 0);
                Button(s, 775, 424f, bottom, 64f, 36f, ">", Btn.Secondary, 1.4f, recyclePage < pages - 1);
            }
            int gain = 0; foreach (FleetCore core in list) if (recyclePick.Contains(core.id)) gain = unchecked(gain + FleetCatalog.RecycleValue[core.quality]);
            Button(s, 770, 52f, OriginalMath.Binary32(bottom + 44f), 210f, 40f, "勾选多余副本", Btn.Secondary, 1.2f, list.Count > 0);
            Button(s, 771, 278f, OriginalMath.Binary32(bottom + 44f), 210f, 40f, "全部取消", Btn.Ghost, 1.2f, recyclePick.Count > 0);
            Button(s, 772, 52f, OriginalMath.Binary32(bottom + 92f), 436f, 48f,
                recyclePick.Count > 0 ? "分解 " + recyclePick.Count + " 件 · +" + gain + " 碎片" : "请选择要分解的模块", Btn.Danger, 1.5f, recyclePick.Count > 0);
        }
    }
}
