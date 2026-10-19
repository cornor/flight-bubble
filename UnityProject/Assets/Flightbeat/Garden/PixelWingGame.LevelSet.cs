#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Garden;
using UnityEngine;

namespace Flightbeat.Game
{
    // 2026-10-19 B level set (docs/r1/关卡第二套-实施计划.md, step 3). The protected loader reads
    // "Levels/" + Levels[levelIndex]; Levels is a static readonly reference whose elements can change, so the
    // bubble layer swaps single names to "B/sectorNN" (released) or "B-test/sectorNN" (test channel) and
    // restores the original names for set A. No protected file changes.
    //  - Released levels come from Resources/levels/B/released.json, test levels from B-test/test.json (only
    //    present in test builds). A level is enabled only if its file exists and its SHA-256 matches the list;
    //    anything else keeps the A level (per-level fallback).
    //  - The choice is a bubble-layer PlayerPrefs key, never FleetProfile. Evidence and regression runs
    //    (evidenceControlled) stay on A unless --level-set B is passed explicitly; a URL "set=B" / "set=A"
    //    overrides the saved choice for that session only.
    //  - Changing the set only renames entries; the running battle is not touched (next level entry uses it).
    public partial class PixelWingGame
    {
        public const int LevelSetA = 0, LevelSetB = 1;
        const string LevelSetKey = "garden.levelSet";
        [Serializable] sealed class LevelSetEntry { public int game; public string resource, fileSha256, cellsSha256; }
        [Serializable] sealed class LevelSetList { public LevelSetEntry[] levels; }
        public sealed class LevelSetSource { public string name, kind, fileSha256, cellsSha256; }

        static string[] levelsA;
        static Dictionary<int, LevelSetSource> levelsB;   // levelIndex -> enabled B (or B-test) level
        public static int LevelSetCurrent { get; private set; }
        static bool levelSetSessionOverride;

        // Original names, copied before the first change (also used by checks that must run on set A).
        public static string[] OriginalLevelNames
        {
            get { if (levelsA == null) levelsA = (string[])Levels.Clone(); return levelsA; }
        }
        public static int LevelSetAvailableCount { get { LoadLevelSetCatalog(); return levelsB.Count; } }
        public static bool LevelHasB(int levelIndex) { LoadLevelSetCatalog(); return levelsB.ContainsKey(levelIndex); }
        // What the given level loads right now: "A", "B" or "B-test", plus the expected hashes for B.
        public static LevelSetSource LevelSource(int levelIndex)
        {
            LoadLevelSetCatalog();
            if (levelIndex >= 0 && levelIndex < Levels.Length && Levels[levelIndex] != OriginalLevelNames[levelIndex]
                && levelsB.TryGetValue(levelIndex, out var b)) return b;
            return new LevelSetSource { name = levelIndex >= 0 && levelIndex < Levels.Length ? Levels[levelIndex] : "", kind = "A" };
        }

        static void LoadLevelSetCatalog()
        {
            if (levelsB != null) return;
            _ = OriginalLevelNames;
            levelsB = new Dictionary<int, LevelSetSource>();
            AddLevelSetList("B", "Levels/B/released");
            AddLevelSetList("B-test", "Levels/B-test/test");   // overrides released entries with the same level
        }
        static void AddLevelSetList(string folder, string listPath)
        {
            var list = Resources.Load<TextAsset>(listPath);
            if (!list) return;
            LevelSetList parsed;
            try { parsed = JsonUtility.FromJson<LevelSetList>(list.text); }
            catch (Exception e) { Debug.LogWarning("Level set list unreadable: " + listPath + " " + e.Message); return; }
            if (parsed?.levels == null) return;
            foreach (var entry in parsed.levels)
            {
                int index = entry.game - 1;
                // The B set covers game levels 3..262 (sector01..sector260); the name must match the slot.
                if (index < 2 || index >= Levels.Length || entry.resource != OriginalLevelNames[index]) 
                { Debug.LogWarning("Level set entry ignored: " + folder + " " + entry.game + " " + entry.resource); continue; }
                string name = folder + "/" + entry.resource;
                var asset = Resources.Load<TextAsset>("Levels/" + name);
                if (!asset || GardenLevelHashes.FileSha256(asset.bytes) != entry.fileSha256)
                { Debug.LogWarning("Level set file missing or changed, level stays A: " + name); continue; }
                levelsB[index] = new LevelSetSource { name = name, kind = folder, fileSha256 = entry.fileSha256, cellsSha256 = entry.cellsSha256 };
            }
        }

        // Rename the Levels entries for the given set. Safe to call any number of times.
        public static void ApplyLevelSet(int set)
        {
            LoadLevelSetCatalog();
            LevelSetCurrent = set == LevelSetB ? LevelSetB : LevelSetA;
            var a = OriginalLevelNames;
            for (int i = 0; i < Levels.Length; i++)
                Levels[i] = LevelSetCurrent == LevelSetB && levelsB.TryGetValue(i, out var b) ? b.name : a[i];
        }
        public static bool LevelNamesAreOriginal()
        {
            var a = OriginalLevelNames;
            for (int i = 0; i < Levels.Length; i++) if (Levels[i] != a[i]) return false;
            return true;
        }

        // Startup, before the first ResetRuntime.
        void InitializeLevelSet(string[] args)
        {
            int set = LevelSetA;
            int at = Array.IndexOf(args, "--level-set");
            if (at >= 0 && at + 1 < args.Length) { set = args[at + 1] == "B" ? LevelSetB : LevelSetA; levelSetSessionOverride = true; }
            else if (TryLevelSetFromUrl(out int fromUrl)) { set = fromUrl; levelSetSessionOverride = true; }
            else if (!evidenceControlled) set = PlayerPrefs.GetInt(LevelSetKey, LevelSetA) == LevelSetB ? LevelSetB : LevelSetA;
            ApplyLevelSet(set);
            if (set == LevelSetB) Debug.Log("GARDEN_LEVEL_SET B " + LevelSetAvailableCount);
        }
        static bool TryLevelSetFromUrl(out int set)
        {
            set = LevelSetA;
            if (!Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var uri)) return false;
            foreach (string pair in uri.Query.TrimStart('?').Split('&'))
            {
                string[] parts = pair.Split('=');
                if (parts.Length != 2 || parts[0] != "set") continue;
                if (parts[1] == "B" || parts[1] == "b") { set = LevelSetB; return true; }
                if (parts[1] == "A" || parts[1] == "a") { set = LevelSetA; return true; }
            }
            return false;
        }
        // Home settings toggle (action LevelSetAction). Takes effect from the next level entry.
        void ToggleLevelSet()
        {
            int next = LevelSetCurrent == LevelSetB ? LevelSetA : LevelSetB;
            ApplyLevelSet(next);
            if (!levelSetSessionOverride && !evidenceControlled) { PlayerPrefs.SetInt(LevelSetKey, next); PlayerPrefs.Save(); }
            audioChip.Play("ui");
        }
    }
}
#endif
