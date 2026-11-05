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
    //  - 2026-11-05 (user: the new levels 1-52 are the game's levels; drop the switch): always set B, for players,
    //    evidence and regression runs alike. The original files left Resources/levels the same day (backup in
    //    Art/levels-a/), so there is no set A any more: the settings switch, its PlayerPrefs choice, the URL "set=A"
    //    and --level-set A are gone. Every level must be released (publish.py --check); a missing B file would fall
    //    back to the now absent original name and fail to load.
    public partial class PixelWingGame
    {
        public const int LevelSetA = 0, LevelSetB = 1;
        [Serializable] sealed class LevelSetEntry { public int game; public string resource, fileSha256, cellsSha256; }
        [Serializable] sealed class LevelSetList { public LevelSetEntry[] levels; public string defaultSet; }
        public sealed class LevelSetSource { public string name, kind, fileSha256, cellsSha256; }

        static string[] levelsA;
        static Dictionary<int, LevelSetSource> levelsB;   // levelIndex -> enabled B (or B-test) level
        public static int LevelSetCurrent { get; private set; }

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
                // The B set covers game levels 1..52 (Levels.LevelCount since 2026-11-05; training01/02 since 11-03); the name
                // must match the slot.
                if (index < 0 || index >= Levels.Length || entry.resource != OriginalLevelNames[index]) 
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
            ApplyLevelSet(LevelSetB);
            Debug.Log("GARDEN_LEVEL_SET B " + LevelSetAvailableCount);
            if (LevelSetAvailableCount < Levels.Length) Debug.LogError("Levels without a released file: " + (Levels.Length - LevelSetAvailableCount));
        }
    }
}
#endif
