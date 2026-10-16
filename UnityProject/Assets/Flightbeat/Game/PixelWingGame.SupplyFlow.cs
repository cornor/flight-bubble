using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int supplyPool, supplyTarget = -1, lastDrawCount = 1, smeltTarget = -1; // f54860 +236..276
        private bool gachaNew;
        private readonly List<int> smeltPick = new List<int>();
        public FleetCore[] GachaResults => gachaResults; // f22243
        public PartPull[] PartResults => partResults; // f22245
        public int SupplyPool => supplyPool; // f23824
        public int SmeltTargetIndex => smeltTarget; // f22255
        public IReadOnlyList<int> SmeltPick => smeltPick; // f22257
        private int ResultCount => partResults != null ? partResults.Length : gachaResults != null ? gachaResults.Length : 0; // f54668
        private float RevealDone => ResultCount > 1 ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)ResultCount) * .09f) + .35f) : .5f; // f54670
        // f54654.
        private void OpenSupply(int pool, int target)
        {
            supplyPool = pool; deckOverlay = DeckOverlay.Supply; supplyTarget = target < 5 ? target : -1;
            MarkUi(423); audioChip.Play("zoom");
        }
        // f54666. The module NEW badge compares only the first result's kind
        // with every other saved id, including other results in this batch.
        private void SupplyDraw(int count)
        {
            lastDrawCount = count;
            if (supplyPool == 0)
            {
                PartPull[] results = Economy.DrawParts(count, supplyTarget);
                if (results == null) { audioChip.Play("deny"); Toast("碎片不足，出击或到商店获取", uiRed); return; }
                gachaNew = false; gachaResults = null; partResults = results;
                foreach (PartPull p in results) if (p.first) gachaNew = true;
                gachaBest = gachaNew ? 4 : 2;
            }
            else
            {
                FleetCore[] results = Economy.Draw(count);
                if (results == null)
                {
                    audioChip.Play("deny"); Toast(Economy.Data.cores.Count > unchecked(120 - count) ? "模块仓已满，请分解" : "碎片不足，出击或到商店获取", uiRed); return;
                }
                gachaNew = true;
                foreach (FleetCore core in Economy.Data.cores) if (core.kind == results[0].kind && core.id != results[0].id) gachaNew = false;
                gachaBest = 0; partResults = null; gachaResults = results;
                foreach (FleetCore core in results) gachaBest = gachaBest > core.quality ? gachaBest : core.quality;
            }
            gachaRevealed = false; gachaAge = 0f; deckOverlay = DeckOverlay.Gacha; audioChip.Play("gacha");
        }
        // f54653.
        private void OpenSmelt(int target)
        {
            if (!Economy.SmeltTarget(target))
            {
                audioChip.Play("deny"); Toast(Economy.Assembled(target / 5) ? "该部件当前不需要熔炼" : "首件已收集，无需熔炼", uiDim); return;
            }
            smeltTarget = target; smeltPick.Clear(); int[] suggested = Economy.SuggestSmelt(target);
            if (suggested != null) smeltPick.AddRange(suggested);
            deckOverlay = DeckOverlay.Smelt; MarkUi(478); audioChip.Play("zoom");
        }
        // f54623/f54624: source list is index-ordered; repeated picks count.
        private List<int> SmeltSources()
        {
            var result = new List<int>(); for (int i = 0; i < 25; i++) if (i != smeltTarget && Economy.Data.spares[i] > 0) result.Add(i); return result;
        }
        private int Picked(int index)
        {
            int count = 0; foreach (int pick in smeltPick) if (pick == index) count = unchecked(count + 1); return count;
        }
        // f54660. IDs are deliberately dispatched without adding overlay guards.
        private bool OnSupplyUi(int id)
        {
            if (id == 400 || id == 401) { supplyPool = id - 400; return true; }
            if (id == 422) { deckOverlay = DeckOverlay.None; return true; }
            if (unchecked((uint)(id - 410)) < 6u) { supplyTarget = id - 411; return true; }
            switch (id)
            {
                case 200: deckOverlay = DeckOverlay.None; return true;
                case 201: SupplyDraw(lastDrawCount); return true;
                case 202:
                    deckOverlay = DeckOverlay.None;
                    if (Page != DeckPage.Upgrade) { DeckPage before = Page; Page = DeckPage.Upgrade; previousPage = before; MarkUi(199); EnterHangar(); }
                    if (partResults != null && partResults.Length != 0)
                    {
                        PartPull choice = partResults[0]; foreach (PartPull part in partResults) if (part.first) { choice = part; break; }
                        hangarTab = 0; hangarFighter = choice.index / 5; hangarPart = choice.index % 5;
                    }
                    else if (gachaResults != null && gachaResults.Length != 0) { hangarTab = 1; OpenPicker(gachaResults[0].Slot); }
                    return true;
                case 420: SupplyDraw(1); return true;
                case 421: SupplyDraw(10); return true;
                case 473: deckOverlay = DeckOverlay.None; return true;
            }
            if (unchecked((uint)(id - 440)) < 25u)
            {
                int index = id - 440;
                if (smeltPick.Count <= 2 && Picked(index) < Economy.Data.spares[index]) { smeltPick.Add(index); audioChip.Play("ui"); }
                else if (Picked(index) > 0) smeltPick.Remove(index);
                else { audioChip.Play("deny"); Toast("已选满 3 件", uiDim); }
                return true;
            }
            if (unchecked((uint)(id - 474)) < 3u)
            {
                var distinct = new List<int>(); foreach (int pick in smeltPick) if (!distinct.Contains(pick)) distinct.Add(pick);
                int index = id - 474; if (index < distinct.Count) smeltPick.Remove(distinct[index]); return true;
            }
            switch (id)
            {
                case 470:
                    smeltPick.Clear(); int[] suggested = Economy.SuggestSmelt(smeltTarget);
                    if (suggested != null) smeltPick.AddRange(suggested); else Toast("重复件不足 3 件", uiDim);
                    return true;
                case 471: smeltPick.Clear(); return true;
                case 472:
                    int fighter = smeltTarget / 5, part = smeltTarget % 5, level = Economy.PartLevel(fighter, part);
                    if (smeltPick.Count != 3 || !Economy.Smelt(smeltTarget, smeltPick.ToArray()))
                    { audioChip.Play("deny"); Toast("请选择 3 件重复件", uiRed); return true; }
                    audioChip.Play("legend"); MarkUi(479);
                    FloatUp(level != 0 ? FleetCatalog.PartLabel(smeltTarget) + " +1" : "补齐 " + FleetCatalog.PartLabel(smeltTarget), 270f, OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - 200f), uiGoldHi);
                    hangarPart = part; hangarFighter = fighter; deckOverlay = DeckOverlay.None; partFlash = smeltTarget; MarkUi(490);
                    if (level == 0 && Economy.CanAssemble(fighter)) Toast(FleetCatalog.FighterNames[fighter] + " 部件已齐，可以组装！", uiGold);
                    return true;
            }
            return false;
        }
        // f54671.
        private Color ResultColor(int index) => partResults != null
            ? partResults[index].first ? uiGold : FleetCatalog.FighterAccent[partResults[index].index / 5]
            : FleetCatalog.QualityColors[gachaResults[index].quality];
        // f54673/f54674.
        private string PartResultNote(PartPull part) => !part.first ? "重复件 +1" : Economy.Assembled(part.index / 5)
            ? "新部件已收藏" : "组装进度 " + Economy.Collected(part.index / 5) + "/5";
        private string PartResultHint(PartPull pull)
        {
            int fighter = pull.index / 5, part = pull.index % 5;
            if (!Economy.Assembled(fighter)) return Economy.CanAssemble(fighter) ? "五个部件已齐，去机库组装！" : "重复件留作升级，或 3 件熔炼补齐";
            int level = Economy.PartLevel(fighter, part);
            return level >= 5 ? "该部位已满级，可用于熔炼" : "升级 LV" + (level + 1) + "：重复件 " + Economy.Spare(fighter, part) + "/" + Economy.UpgradeCost(fighter, part);
        }
    }
}
