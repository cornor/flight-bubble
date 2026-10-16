using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class CodexZoneStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, rewardRandomCalls, catalogEntries, meshes, checkedVertices;
        }
        private static Report report;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-codex-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-codex-report required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Pages(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh(Func<int, int> rng = null, Action<FleetProfile> save = null)
        {
            return new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800,
                d => { report.commits++; save?.Invoke(d); }, rng ?? (n => 0));
        }
        private static void Transactions()
        {
            int[] coins = { 1500, 2500, 4000, 5500, 8000 }, fragments = { 60, 90, 150, 200, 300 }, counts = { 3, 5, 8, 10, 12 };
            for (int zone = 0; zone < 5; zone++) for (int twice = 0; twice < 2; twice++)
            {
                bool reward = false; int calls = 0;
                var e = Fresh(n => { if (reward) { Check(n == 5, "Parts RNG bound is five"); calls++; report.rewardRandomCalls++; } return 0; });
                Array.Clear(e.Data.parts, 0, 25); Array.Clear(e.Data.spares, 0, 25); e.Data.assembled = 1;
                e.Data.parts[11] = 1; // Fighter 2 wins the source preference without owning the selected part 10.
                int revision = e.Data.revision;
                Check(e.PartTarget == 2 && e.ClaimZone(zone, twice == 1) == null && e.Data.revision == revision, "Uncleared guard and preferred target");
                e.Data.cleared.Add(WarZones.LastLevel(zone) + 1);
                Check(e.ZoneCleared(zone) && e.ZoneReady(zone) && !e.ZoneClaimed(zone), "Last cleared level gates reward");
                int oldCoins = e.Data.coins, oldFragments = e.Data.fragments, oldDraws = e.Data.partDraws;
                int day = e.Data.dayKey; e.Data.dayKey = day - 1; e.Data.dayWins = 4; reward = true;
                PartPull[] result = e.ClaimZone(zone, twice == 1);
                Check(result.Length == counts[zone] && calls == counts[zone] && result[0].first, "Authored count, not doubled; first result is new");
                for (int i = 0; i < result.Length; i++) Check(result[i].index == 10 && result[i].first == (i == 0), "Batch clone tracks first versus duplicate");
                Check(e.Data.parts[10] == 1 && e.Data.spares[10] == counts[zone] - 1 && e.Data.parts[11] == 1, "First copies and spares ledger");
                Check(e.Data.coins == oldCoins + coins[zone] * (twice + 1) && e.Data.fragments == oldFragments + fragments[zone] * (twice + 1), "Currency reward ledger");
                Check(e.Data.partDraws == oldDraws && e.Data.dayKey == day - 1 && e.Data.dayWins == 4, "No draw statistic or day refresh");
                Check(e.ZoneClaimed(zone) && e.ZoneClaimed(zone + 32) && !e.ZoneReady(zone), "Claim bit and masked shift query");
                revision = e.Data.revision;
                Check(e.ClaimZone(zone, twice == 0) == null && e.Data.revision == revision && calls == counts[zone], "Repeated claim consumes neither RNG nor revision");
                Check(e.ClaimZone(-1, false) == null && e.ClaimZone(5, true) == null, "Unsigned zone guard");
            }
            {
                var e = Fresh(); Array.Clear(e.Data.parts, 0, 25); e.Data.assembled = 1;
                Check(e.PartTarget == 1, "All empty unassembled fighters prefer first");
                e.Data.parts[5] = 1; e.Data.parts[10] = 1; Check(e.PartTarget == 1, "Equal collection keeps first");
                e.Data.parts[11] = 1; Check(e.PartTarget == 2, "Highest collection preferred");
                e.Data.assembled |= 4; Check(e.PartTarget == 1, "Assembled candidate excluded");
                e.Data.assembled = 31; e.Data.activeFighter = 4; Check(e.PartTarget == 4, "All assembled uses active fighter");
                e.Data.parts[20] = 5; e.Data.spares[20] = 7; e.Data.cleared.Add(WarZones.LastLevel(4) + 1);
                PartPull[] result = e.ClaimZone(4, true);
                Check(result.Length == 12 && !result[0].first && e.Data.parts[20] == 5 && e.Data.spares[20] == 19, "Granting owned part preserves upgrades, adds spare copies");
            }
            {
                bool fail = false, reward = false; int calls = 0;
                var e = Fresh(n => { if (reward) calls++; return 0; }, d => { if (fail) throw new IOException("expected save failure"); });
                e.Data.cleared.Add(WarZones.LastLevel(0) + 1); FleetProfile prior = e.Data; string before = JsonUtility.ToJson(prior);
                fail = true; reward = true;
                try { e.ClaimZone(0, true); throw new Exception("Save failure swallowed"); }
                catch (IOException) { Check(ReferenceEquals(prior, e.Data) && before == JsonUtility.ToJson(e.Data) && calls == 3, "Failed save consumes planned RNG but publishes neither rewards nor claim"); }
            }
        }
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static object Call(PixelWingGame game, string name, params object[] args) => typeof(PixelWingGame).GetMethod(name, Flags).Invoke(game, args);
        private static void Pages()
        {
            var owner = new GameObject("Codex/zone CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false; Field(game, "hud", s);
                // Inactive fixture objects do not receive Awake. CodexThumb lazily
                // binds its atlas to the real material, so initialize that surface.
                typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(s, null);
                var e = Fresh(); typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e); Field(game, "ui", 30f);
                var marks = (Dictionary<int, float>)typeof(PixelWingGame).GetField("uiMarks", Flags).GetValue(game);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                Check(game.CodexZone == -1 && Call(game, "Entry", -1) == null && Call(game, "Entry", 262) == null, "Constructor and catalog bounds");
                for (int index = 0; index < PixelWingGame.Levels.Length; index++)
                {
                    var entry = (CodexEntry)Call(game, "Entry", index);
                    Check(entry != null && entry.name == PixelWingGame.Levels[index] && entry.swatch.Length == Math.Min(entry.colors, 6)
                        && (string)Call(game, "LevelTitle", index) == entry.title, "Imported catalog matches source level order and swatch count");
                    report.catalogEntries++;
                }
                Check((string)Call(game, "LevelTitle", -1) == "第0关" && (string)Call(game, "LevelTitle", 262) == "第263关", "Missing entry source title fallback");
                for (int zone = 0; zone < 5; zone++)
                {
                    e.Data.unlocked = WarZones.FirstLevel(zone);
                    Check((bool)Call(game, "ZoneOpen", zone) == (zone == 0), "Zone unlock strict boundary");
                    e.Data.unlocked++; Check((bool)Call(game, "ZoneOpen", zone), "Zone opens one above FirstLevel");
                }
                e.Data.cleared.Clear(); e.Data.zoneClaimed = 0;
                Check((int)Call(game, "ZoneRewardReady") == -1, "No pending reward sentinel");
                e.Data.cleared.Add(WarZones.LastLevel(4) + 1); e.Data.cleared.Add(WarZones.LastLevel(1) + 1);
                Check((int)Call(game, "ZoneRewardReady") == 1, "Lowest pending reward wins");
                string method = "DrawCodex"; s.Paint = surface => Call(game, method, surface);
                using (var vertices = new VertexHelper()) for (int viewport = 0; viewport < 2; viewport++)
                {
                    ScreenLayout.Width = 540f; ScreenLayout.Height = viewport == 0 ? 960f : 1280f;
                    method = "DrawCodex"; Field(game, "codexZone", -1); Field(game, "codexLevel", -1);
                    for (int state = 0; state < 3; state++) foreach (float elapsed in new[] { .05f, .3f, 1f })
                    {
                        e.Data.unlocked = state == 0 ? 3 : 262; e.Data.cleared.Clear(); e.Data.zoneClaimed = state == 2 ? 31 : 0;
                        if (state > 0) for (int i = 1; i <= 262; i++) e.Data.cleared.Add(i);
                        marks[555] = 30f - elapsed; Populate(game, s, populate, vertices);
                    }
                    for (int zone = 0; zone < 5; zone++) foreach (int page in new[] { -1, 1, 99 })
                    {
                        Field(game, "codexZone", zone); Field(game, "codexPage", page); marks[555] = 29f;
                        int first = WarZones.FirstLevel(zone); e.Data.unlocked = first + 35;
                        e.Data.cleared.Clear(); for (int i = first + 1; i < e.Data.unlocked; i += 2) e.Data.cleared.Add(i);
                        Field(game, "pressedId", (page > 0 ? 632 : 600)); Field(game, "pressedAt", 29.92f);
                        Populate(game, s, populate, vertices);
                        int expected = page < 0 ? 0 : zone == 0 ? 0 : 1;
                        Check((int)typeof(PixelWingGame).GetField("codexPage", Flags).GetValue(game) == expected, "Draw clamps authored 32-entry pages");
                    }
                    // All 262 actual detail entries at 960, representative two-panel states at 1280.
                    Field(game, "codexZone", -1); marks[1599] = 29.9f;
                    for (int level = 0; level < 262; level += viewport == 0 ? 1 : 29)
                    {
                        Field(game, "codexLevel", level); e.Data.unlocked = level % 3 == 0 ? level : level + 1;
                        e.Data.cleared.Clear(); if (level % 3 == 1) e.Data.cleared.Add(level + 1);
                        Populate(game, s, populate, vertices);
                    }
                    method = "DrawZoneReward";
                    for (int zone = 0; zone < 5; zone++) foreach (float elapsed in new[] { .05f, .25f, 1f })
                    {
                        Field(game, "zoneRewardFor", zone); marks[702] = 30f - elapsed; s.MapLayout = viewport == 1;
                        Populate(game, s, populate, vertices); Check(s.MapLayout == (viewport == 1), "Reward restores surface mapping");
                    }
                    s.MapLayout = false;
                }
            }
            finally
            {
                ScreenLayout.Width = width; ScreenLayout.Height = height;
                var s = owner.GetComponent<PixelSurface>();
                if (s != null) { FieldInfo f = typeof(PixelSurface).GetField("ownedMaterial", Flags); var m = (Material)f.GetValue(s); f.SetValue(s, null); if (m != null) UnityEngine.Object.DestroyImmediate(m); }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static void Populate(PixelWingGame game, PixelSurface s, MethodInfo populate, VertexHelper vertices)
        {
            Call(game, "BeginHits"); populate.Invoke(s, new object[] { vertices }); Check(vertices.currentVertCount > 0, "Codex/reward mesh exists");
            UIVertex v = default;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref v, i);
                if (!Finite(v.position.x) || !Finite(v.position.y) || !Finite(v.uv0.x) || !Finite(v.uv0.y)) throw new Exception("Non-finite codex/reward mesh");
            }
            report.meshes++; report.checkedVertices += vertices.currentVertCount;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
