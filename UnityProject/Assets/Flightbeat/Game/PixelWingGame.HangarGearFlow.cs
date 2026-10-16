using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int pickPage, previousPickSlot; // +296/+316
        private float gearZoom; // +312
        // f54682: greater of authored top and the bottom-anchored minimum.
        private float PickTop
        {
            get { float top = ScreenLayout.Top + 2f + 104f - 4f + 330f, minimum = ScreenLayout.SafeBottom - 96f - 470f; return top > minimum ? top : minimum; }
        }
        // f54657. The decompiler loses parentheses around PickTop+170.
        private int PickPer
        {
            get
            {
                float rows = (ScreenLayout.SafeBottom - 96f - 6f - 54f - (PickTop + 170f)) / 74f;
                int count = Math.Abs(rows) < 2147483648f ? (int)rows : int.MinValue;
                return count > 1 ? count : 1;
            }
        }
        // f54655.
        private void OpenPicker(int slot)
        {
            previousPickSlot = slot; moduleInspect = slot; pickPage = 0; MarkUi(493); audioChip.Play("zoom");
        }
        // f54656/f53966: remove the current id from a new, rarity-sorted list.
        private List<FleetCore> PickList()
        {
            List<FleetCore> list = Economy.ForSlot(moduleInspect);
            FleetCore equipped = Economy.InPreset(hangarFighter, moduleInspect);
            if (equipped != null) list.RemoveAll(c => c.id == equipped.id);
            return list;
        }
        // Remaining module branches of f54647, in their source order.
        private bool OnHangarGearUi(int id)
        {
            if (unchecked((uint)(id - 340)) < 5u) { OpenPicker(id - 340); return true; }
            if (unchecked((uint)(id - 390)) < 5u)
            {
                int slot = id - 390;
                if (slot == moduleInspect) { moduleInspect = -1; audioChip.Play("page"); }
                else OpenPicker(slot);
                return true;
            }
            switch (id)
            {
                case 380: moduleInspect = -1; audioChip.Play("page"); return true;
                case 381: pickPage = Math.Max(unchecked(pickPage - 1), 0); return true;
                case 382: pickPage = unchecked(pickPage + 1); return true;
                case 383:
                    if (moduleInspect < 0) return false;
                    if (Economy.Unequip(hangarFighter, moduleInspect)) { Toast("已卸下", uiDim); audioChip.Play("unequip"); }
                    return true;
                case 386:
                    if (moduleInspect < 0) return false;
                    FleetCore current = Economy.InPreset(hangarFighter, moduleInspect);
                    if (current != null)
                    {
                        int coreId = current.id;
                        ShowAd("repair", () => // f53967
                        {
                            if (Economy.Repair(coreId)) { Toast("维修完成 · 耐久已满", uiGreen); audioChip.Play("upgrade"); }
                        });
                    }
                    return true;
            }
            if (unchecked((uint)(id - 350)) < 10u && moduleInspect >= 0)
            {
                List<FleetCore> list = PickList(); int index = unchecked(id + pickPage * PickPer - 350);
                if (index >= list.Count) return true;
                FleetCore core = list[index]; int owner = Economy.OwnerOf(core.id);
                if (!Economy.Equip(hangarFighter, core.id)) return true;
                if (owner >= 0 && owner != hangarFighter) Toast("已从 " + FleetCatalog.FighterNames[owner] + " 换装过来", uiGold);
                equipFlash = moduleInspect; MarkUi(192); audioChip.Play("equip");
                float resonance = FleetCatalog.FighterResonance[hangarFighter] == moduleInspect ? 1.25f : 1f;
                FloatUp(core.Spec.Describe(core.quality, resonance), 270f, UpgradeTop + 200f, FleetCatalog.QualityColors[core.quality]);
                moduleInspect = -1; return true;
            }
            if (unchecked((uint)(id - 360)) < 10u && moduleInspect >= 0)
            {
                List<FleetCore> list = PickList(); int index = unchecked(id - 360 + pickPage * PickPer);
                if (index >= list.Count) return true;
                FleetCore core = list[index]; int fragments = FleetCatalog.RecycleValue[core.quality];
                if (Economy.Recycle(core.id)) { FloatUp("+" + fragments + " 碎片", 360f, NavTop - 120f, uiCyan); audioChip.Play("recycle"); }
                else { audioChip.Play("deny"); Toast("预设中的模块不能分解", uiRed); }
                return true;
            }
            if (unchecked((uint)(id - 370)) < 10u && moduleInspect >= 0)
            {
                List<FleetCore> list = PickList(); int index = unchecked(id - 370 + pickPage * PickPer);
                if (index >= list.Count) return true;
                FleetCore core = list[index], upgraded = Economy.MergeModules(core.kind, core.quality);
                if (upgraded != null)
                {
                    audioChip.Play(upgraded.quality > 2 ? "maxpower" : "reward");
                    FloatUp("升阶 · " + FleetCatalog.QualityNames[upgraded.quality] + upgraded.Spec.name, 270f, UpgradeTop + 220f, FleetCatalog.QualityColors[upgraded.quality]);
                }
                return true;
            }
            if (id == 384) { OpenRecycle(); return true; }
            return false;
        }
    }
}
