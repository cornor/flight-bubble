using System;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Game
{
    // Original metadata types 4310/4311; constructors are empty f1080 aliases.
    [Serializable] public sealed class CodexEntry
    {
        public string name, title;
        public int total, colors, lanes, difficulty, columns;
        public int[] swatch;
    }
    [Serializable] internal sealed class CodexIndex { public CodexEntry[] levels; }

    public partial class PixelWingGame
    {
        private int codexZone = -1, codexLevel = -1, codexPage; // f54860 +76/+80/+88.
        private CodexEntry[] codexEntries; // +92.
        public int CodexZone => codexZone; // f7132, this alias only.
        private float CodexTop => ScreenLayout.Top + 2f + 50f; // f54549.
        private float CodexBottom => ScreenLayout.SafeBottom + -12f; // f54551.
        // f54539. Cache an empty array if absent; do not unload this source asset.
        private CodexEntry Entry(int index)
        {
            if (codexEntries == null)
            {
                TextAsset asset = Resources.Load<TextAsset>("Levels/codex");
                codexEntries = asset ? JsonUtility.FromJson<CodexIndex>(asset.text)?.levels : null;
                if (codexEntries == null) codexEntries = new CodexEntry[0];
            }
            return index < 0 || index >= codexEntries.Length ? null : codexEntries[index];
        }
        private string LevelTitle(int index) // f54540.
        {
            CodexEntry entry = Entry(index);
            return entry != null && !String.IsNullOrEmpty(entry.title) ? entry.title : "第" + unchecked(index + 1) + "关";
        }
        private bool ZoneOpen(int zone) => zone == 0 || WarZones.FirstLevel(zone) < Economy.Data.unlocked; // f54542.
        private int ZoneRewardReady() // f54543.
        {
            for (int zone = 0; zone < 5; zone++) if (Economy.ZoneReady(zone)) return zone;
            return -1;
        }
        private void OpenCodex(int zone) // f54544: page is not reset here.
        {
            codexLevel = -1; codexZone = zone; deckOverlay = DeckOverlay.Codex;
            MarkUi(555); audioChip.Play("page");
        }
        private void OpenZoneReward(int zone) // f54558: only negative guard.
        {
            if (zone < 0) return;
            deckOverlay = DeckOverlay.Zone; zoneRewardFor = zone; MarkUi(702); audioChip.Play("legend");
        }
        private void ClaimZoneReward(bool doubled) // f54560.
        {
            int zone = zoneRewardFor; PartPull[] parts = Economy.ClaimZone(zone, doubled);
            zoneRewardFor = -1; deckOverlay = DeckOverlay.None;
            if (parts == null) return;
            audioChip.Play("legend"); int multiplier = doubled ? 2 : 1;
            FloatUp("+" + unchecked(multiplier * WarZones.RewardCoins[zone]) + " 金币  +" + unchecked(multiplier * WarZones.RewardFragments[zone]) + " 碎片",
                270f, ScreenLayout.Height * .5f + -160f, uiGold);
            Toast(FleetCatalog.FighterNames[parts.Length != 0 ? parts[0].index / 5 : 0] + " 部件 ×" + parts.Length + " 已入库", uiGold);
        }
        // f54562/f54862. Range guards below are unsigned WAT comparisons.
        private bool OnCodexUi(int id)
        {
            switch (id)
            {
                case 551: codexLevel = -1; deckOverlay = DeckOverlay.None; return true;
                case 552: codexLevel = -1; codexZone = -1; MarkUi(555); return true;
                case 553: codexLevel = -1; return true;
                case 554:
                    int current = unchecked(Economy.Data.unlocked - 1);
                    current = current < 0 ? 0 : current > Levels.Length - 1 ? Levels.Length - 1 : current;
                    WarZones.OfLevel(current); // Source computes this even though the return value is unused.
                    int ready = ZoneRewardReady(); if (ready >= 0) OpenZoneReward(ready); else OpenCodex(-1);
                    return true;
            }
            if (unchecked((uint)(id - 565)) < 5u) { OpenZoneReward(id - 565); return true; }
            if (unchecked((uint)(id - 560)) < 5u)
            {
                int zone = id - 560;
                if (!ZoneOpen(zone)) { audioChip.Play("deny"); Toast("收复上一战区后开放", uiDim); return true; }
                codexLevel = -1; codexZone = zone;
                int position = unchecked(Economy.Data.unlocked - WarZones.FirstLevel(zone) - 1), last = WarZones.LevelsIn(zone) - 1;
                codexPage = position < 0 ? 0 : (position > last ? last : position) / 32;
                MarkUi(555); return true;
            }
            if (id == 556) { codexPage = Math.Max(unchecked(codexPage - 1), 0); audioChip.Play("page"); return true; }
            if (id == 557) { codexPage = unchecked(codexPage + 1); audioChip.Play("page"); return true; }
            if (unchecked((uint)(id - 600)) < 62u)
            {
                if (codexZone < 0) return false;
                int level = unchecked(WarZones.FirstLevel(codexZone) + id - 600);
                if (level >= Economy.Data.unlocked) { audioChip.Play("deny"); Toast("推进到这一关后可查看情报", uiDim); return true; }
                codexLevel = level; MarkUi(1599); return true;
            }
            if (id == 700) { ClaimZoneReward(false); return true; }
            if (id == 701) { ShowAd("zone", () => ClaimZoneReward(true)); return true; }
            return false;
        }
    }
}
