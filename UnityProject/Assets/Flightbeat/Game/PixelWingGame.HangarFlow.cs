using System;
using Flightbeat.Economy;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int hangarTab, hangarFighter, hangarPart, previousHangarTab; // +280/+284/+288/+308
        private int partFlash = -1, equipFlash = -1; // f54860 paired +300/+304
        private float assemblyAt; // +68
        private int assemblySoundStep; // +72
        private float UpgradeTop => ScreenLayout.Top + 2f + 104f - 4f; // f54652

        // f54650: active fighter is selected first; the tutorial then focuses 0/0.
        private void EnterHangar()
        {
            moduleInspect = -1; hangarFighter = Economy.ActiveFighter;
            if (Economy.Data.growthTutorial || Economy.Data.unlocked < 3) return;
            hangarPart = 0; hangarTab = 0; hangarFighter = 0;
            if (Economy.GiftStarterParts()) Toast("指挥部配发：巡航者·机首 ×2", uiGold);
        }
        // f54651: first upgrade in fighter-major, part-minor order.
        private void FocusReadyPart()
        {
            for (int fighter = 0; fighter < 5; fighter++)
                for (int part = 0; part < 5; part++)
                    if (Economy.CanUpgradePart(fighter, part)) { hangarPart = part; hangarFighter = fighter; return; }
        }
        // f54529.
        private void StartAssembly(int fighter)
        {
            assemblySoundStep = 0; assemblyFighter = fighter; assemblyAt = ui; MarkUi(722); audioChip.Play("zoom");
        }
        // f54647: source fleet/tab/part, upgrade, assembly and active-fighter
        // branches, followed by module, smelting, supply and bulk-recycle actions.
        private bool OnUpgradeUi(int id)
        {
            if (unchecked((uint)(id - 300)) < 5u)
            {
                int fighter = id - 300;
                if (fighter != hangarFighter) { hangarFighter = fighter; MarkUi(494); audioChip.Play("page"); }
                return true;
            }
            if (unchecked((uint)(id - 310)) < 3u)
            {
                int tab = id - 310;
                if (tab != hangarTab) { previousHangarTab = hangarTab; hangarTab = tab; MarkUi(495); moduleInspect = -1; }
                return true;
            }
            if (unchecked((uint)(id - 320)) < 5u) { hangarPart = id - 320; audioChip.Play("zoom"); return true; }
            if (id == 180)
            {
                int fighter = hangarFighter, part = hangarPart;
                if (!Economy.UpgradePart(fighter, part))
                {
                    audioChip.Play("deny"); Toast(!Economy.Assembled(fighter) ? "先集齐五个部件组装战机" : Economy.PartLevel(fighter, part) > 4 ? "已满级" : "重复件不足，可抽取或熔炼", uiRed);
                    return true;
                }
                partFlash = FleetCatalog.PartIndex(fighter, part); MarkUi(490); audioChip.Play("upgrade"); Economy.CompleteGrowthTutorial();
                int level = Economy.PartLevel(fighter, part);
                FloatUp(FleetCatalog.PartNames[part] + " LV" + level, 270f, UpgradeTop + 180f, FleetCatalog.FighterAccent[fighter]);
                if (part == 0 && FleetCatalog.NosePierce[level] > FleetCatalog.NosePierce[level - 1])
                { Toast("渗透提升到 " + FleetCatalog.NosePierce[level] + " 层", uiCyan); return true; }
                if (Economy.FighterLevel(fighter) == level && level >= 2)
                    Toast("整机 LV" + level + " · 解锁" + (level == 3 ? "专属决战「" + FleetCatalog.FighterFinale[fighter] + "」" : FleetCatalog.StageNames[level]), uiGold);
                return true;
            }
            switch (id)
            {
                case 330: OpenSmelt(FleetCatalog.PartIndex(hangarFighter, hangarPart)); return true;
                case 331:
                    if (Economy.Assemble(hangarFighter)) { MarkUi(491); StartAssembly(hangarFighter); }
                    else { audioChip.Play("deny"); Toast("还差 " + (5 - Economy.Collected(hangarFighter)) + " 个部件", uiRed); }
                    return true;
                case 332:
                    if (Economy.SetActive(hangarFighter)) { MarkUi(492); audioChip.Play("equip"); Toast(FleetCatalog.FighterNames[hangarFighter] + " 设为出战，配装已载入", uiGold); }
                    return true;
                case 333: OpenSupply(0, hangarFighter); return true;
                case 334: moduleInspect = -1; OpenSupply(1, hangarFighter); return true;
            }
            return OnHangarGearUi(id);
        }
        // f54537. Early tap fast-forwards, then close or set active.
        private bool OnAssemblyUi(int id)
        {
            if ((id & -2) != 720 || assemblyFighter < 0) return false;
            if (ui - assemblyAt < 3.31f)
            { assemblySoundStep = 5; float skip = ui - 2.8600001335144043f; assemblyAt = skip > assemblyAt ? assemblyAt : skip; return true; }
            int fighter = assemblyFighter; assemblyFighter = -1;
            if (id == 721 && Economy.SetActive(fighter))
            {
                MarkUi(492); audioChip.Play("equip");
                Toast(FleetCatalog.FighterNames[fighter] + " 出战 · 配装已载入", FleetCatalog.FighterAccent[fighter]);
            }
            return true;
        }
    }
}
