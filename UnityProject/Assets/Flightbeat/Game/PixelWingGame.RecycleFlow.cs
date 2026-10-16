using System.Collections.Generic;
using Flightbeat.Economy;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int recyclePage; // f54860 +220
        private readonly HashSet<int> recyclePick = new HashSet<int>(); // +224
        // f54658.
        private void OpenRecycle() { recyclePage = 0; recycleOpen = true; recyclePick.Clear(); MarkUi(776); audioChip.Play("page"); }
        // f54625.
        private List<FleetCore> RecycleList()
        {
            var result = new List<FleetCore>(); foreach (FleetCore core in Economy.CoresByRarity()) if (!Economy.IsEquipped(core.id)) result.Add(core); return result;
        }
        // f54645. While open, even unrecognized ids are consumed by this modal.
        private bool OnRecycleUi(int id)
        {
            if (!recycleOpen) return false;
            if (id == 773) { recycleOpen = false; return true; }
            List<FleetCore> list = RecycleList();
            if (unchecked((uint)(id - 760)) < 7u)
            {
                int index = unchecked(id + recyclePage * 7 - 760); if (index >= list.Count) return true;
                int core = list[index].id; if (!recyclePick.Remove(core)) recyclePick.Add(core);
                audioChip.Play("tick"); return true;
            }
            switch (id)
            {
                case 774: recyclePage = unchecked(recyclePage - 1); return true;
                case 775: recyclePage = unchecked(recyclePage + 1); return true;
                case 771: recyclePick.Clear(); return true;
                case 770:
                    List<int> duplicate = Economy.DuplicateModules(); recyclePick.Clear(); foreach (int core in duplicate) recyclePick.Add(core);
                    if (duplicate.Count == 0) Toast("没有多余副本（每种只保留最好的一件）", uiDim); else audioChip.Play("tick");
                    return true;
                case 772:
                    if (recyclePick.Count == 0) return true;
                    int gain = Economy.RecycleMany(recyclePick), count = recyclePick.Count; recyclePick.Clear();
                    if (gain > 0)
                    {
                        FloatUp("+" + gain + " 碎片", 270f, NavTop - 120f, uiCyan); audioChip.Play("recycle");
                        Toast("已分解 " + count + " 件模块", uiCyan);
                    }
                    if (RecycleList().Count == 0) recycleOpen = false; return true;
                default: return true;
            }
        }
    }
}
