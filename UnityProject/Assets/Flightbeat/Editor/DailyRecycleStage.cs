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
    public static class DailyRecycleStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, meshes, checkedVertices;
        }
        private static Report report;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-daily-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-daily-report required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Meshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh(Func<long> clock = null, Action<FleetProfile> save = null, Func<int, int> rng = null)
        {
            var e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, clock ?? (() => 1791604800),
                d => { report.commits++; save?.Invoke(d); }, rng ?? (n => 0));
            e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); e.Data.nextCore = 1000;
            return e;
        }
        private static readonly string[] ProgressFields = { "dayWins", "dayMerges", "dayKills", "dayItems", "dayMaxes", "dayCombo", "dayCleanWins", "dayFastWins", "dayAds" };
        private static void Ready(FleetProfile d) { for (int i = 0; i < 9; i++) typeof(FleetProfile).GetField(ProgressFields[i]).SetValue(d, FleetCatalog.DailyTarget[i]); }
        private static void Add(FleetEconomy e, int id, int kind, int quality) => e.Data.cores.Add(new FleetCore { id = id, kind = kind, quality = quality, durability = FleetCatalog.DurabilityMax[quality] });
        private static void Transactions()
        {
            for (int twice = 0; twice < 2; twice++)
            {
                long now = 1791604800; var e = Fresh(() => now); int coins = e.Data.coins, fragments = e.Data.fragments;
                for (int task = 0; task < 9; task++)
                {
                    typeof(FleetProfile).GetField(ProgressFields[task]).SetValue(e.Data, FleetCatalog.DailyTarget[task] - 1);
                    Check(!e.DailyDone(task) && !e.ClaimDaily(task, twice == 1), "Below target cannot claim");
                    typeof(FleetProfile).GetField(ProgressFields[task]).SetValue(e.Data, FleetCatalog.DailyTarget[task]);
                    Check(e.DailyDone(task) && e.ClaimDaily(task, twice == 1), "Exactly at target claims");
                    Check(e.DailyClaimed(task) && !e.ClaimDaily(task, twice == 0), "Other multiplier cannot reclaim same task");
                }
                Check(e.Data.coins == coins + 840 * (twice + 1) && e.Data.fragments == fragments + 61 * (twice + 1), "Nine authored rewards ledger");
                Check(e.DailyClaimedCount == 9 && e.DailyChestReady && e.DailyReady == 1, "Chest ready only after nine claims");
                e.Data.items[2] = 98;
                Check(e.ClaimDailyChest(twice == 1) && !e.ClaimDailyChest(twice == 0) && e.Data.items[2] == 99, "Chest once and bomb cap");
                Check(e.DailyClaimedCount == 9 && !e.DailyChestReady && e.DailyReady == 0, "Chest bit excluded from task count");
                Check(!e.DailyDone(-1) && !e.ClaimDaily(9, false) && e.DailyProgress(9) == 0 && e.DailyClaimed(32), "Original query bounds and masked shifts");
                now += 86400;
                Check(e.DailyClaimedCount == 9, "Pure queries do not refresh");
                Check(!e.ClaimDaily(0, false) && e.DailyClaimedCount == 0 && e.DailyProgress(0) == 0 && !e.DailyChestReady, "Claim refreshes day before progress guard");
            }
            {
                long now = 1791604800; int calls = 0; var e = Fresh(() => now, null, n => { calls++; Check(n == FleetCatalog.DrawableModules.Length, "Sign RNG bound"); return 0; });
                calls = 0; int coins = e.Data.coins, fragments = e.Data.fragments, rerolls = e.Data.items[0], bombs = e.Data.items[2];
                for (int day = 0; day < 7; day++)
                {
                    Check(e.SignIndex == day && e.CanSign, "Seven-day index"); int before = calls;
                    FleetCore result = e.Sign(); Check(result != null && calls == before + 1 && !e.CanSign && e.Sign() == null && calls == before + 1, "Every successful reward consumes RNG, repeated day does not");
                    if (day < 6) Check(result.durability == -1 && result.id == 0 && e.Data.cores.Count == 0, "Non-module success sentinel");
                    else Check(ReferenceEquals(result, e.Data.cores[0]) && result.id == 1000 && result.quality == 2 && result.durability == 15, "Seventh-day saved rare module");
                    now += 172800; // Missing a day does not reset the sequence.
                }
                Check(e.SignIndex == 0 && e.Data.coins == coins + 500 && e.Data.fragments == fragments + 30
                    && e.Data.items[0] == rerolls + 2 && e.Data.items[2] == bombs + 1 && e.Data.nextCore == 1001, "Seven rewards ledger and wrapping index");
                e.Data.signCount = 6; while (e.Data.cores.Count < 120) Add(e, e.Data.cores.Count + 1, 0, 0);
                Check(e.Sign() != null && e.Data.cores.Count == 121, "Source sign reward has no warehouse-capacity guard");
                now += 86400; e.Data.signCount = 2; e.Data.items[0] = 98; Check(e.Sign() != null && e.Data.items[0] == 99, "Sign item cap");
            }
            {
                var e = Fresh(); Add(e, 10, 0, 1); Add(e, 20, 0, 2); Add(e, 30, 0, 2); Add(e, 40, 1, 4); Add(e, 50, 1, 0); Add(e, 60, 3, 3);
                e.Data.presets[20] = 10; e.Data.presets[6] = 40; e.Data.presets[24] = 999;
                Check(string.Join(",", e.DuplicateModules()) == "20,50", "Keep higher quality/newer id and all presets; list preserves inventory order");
                int fragments = e.Data.fragments;
                Check(e.RecycleDuplicates() == 4 && e.Data.cores.Count == 4 && e.Data.fragments == fragments + 4 && e.Data.presets[24] == 999, "Duplicate payout, no preset normalization");
                int revision = e.Data.revision; Check(e.RecycleDuplicates() == 0 && e.Data.revision == revision, "No duplicates is no commit");
                var selected = new List<int> { 10, 40, 30, 30, 123456 }; fragments = e.Data.fragments;
                Check(e.RecycleMany(selected) == 3 && e.Data.cores.Count == 3 && selected.Count == 5 && e.Data.fragments == fragments + 3, "Many deduplicates input, preserves equipped and caller list");
                revision = e.Data.revision; Check(e.RecycleMany(new[] { 123456 }) == 0 && e.Data.revision == revision + 1, "Unknown-only source request still commits");
                revision = e.Data.revision; Check(e.RecycleMany(new[] { 10, 40 }) == 0 && e.Data.revision == revision && e.RecycleMany(Array.Empty<int>()) == 0, "Protected-only and empty no commit");
                try { e.RecycleMany(null); throw new Exception("Null enumerable accepted"); } catch (ArgumentNullException) { Check(true, "Source null enumerable failure"); }
            }
            for (int action = 0; action < 5; action++)
            {
                bool fail = false; var e = Fresh(null, d => { if (fail) throw new IOException("expected save failure"); });
                Ready(e.Data); if (action == 1) e.Data.dailyClaimed = 511;
                Add(e, 1, 0, 0); Add(e, 2, 0, 1);
                FleetProfile prior = e.Data; string before = JsonUtility.ToJson(prior); fail = true;
                try
                {
                    switch (action) { case 0: e.ClaimDaily(0, true); break; case 1: e.ClaimDailyChest(true); break; case 2: e.Sign(); break; case 3: e.RecycleDuplicates(); break; default: e.RecycleMany(new[] { 1 }); break; }
                    throw new Exception("Save failure swallowed");
                }
                catch (IOException) { Check(ReferenceEquals(prior, e.Data) && before == JsonUtility.ToJson(e.Data), "Save failure does not publish reward, claim or removal"); }
            }
        }
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static void Meshes()
        {
            var owner = new GameObject("Daily/recycle CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false; Field(game, "hud", s);
                var e = Fresh(); typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e); Field(game, "ui", 30f);
                var marks = (Dictionary<int, float>)typeof(PixelWingGame).GetField("uiMarks", Flags).GetValue(game);
                var picks = (HashSet<int>)typeof(PixelWingGame).GetField("recyclePick", Flags).GetValue(game);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                string method = "DrawDaily"; s.Paint = surface => typeof(PixelWingGame).GetMethod(method, Flags).Invoke(game, new object[] { surface });
                using (var vertices = new VertexHelper()) for (int viewport = 0; viewport < 2; viewport++)
                {
                    ScreenLayout.Width = 540f; ScreenLayout.Height = viewport == 0 ? 960f : 1280f;
                    method = "DrawDaily";
                    for (int state = 0; state < 4; state++) foreach (float elapsed in new[] { .05f, .3f, 1f })
                    {
                        for (int i = 0; i < 9; i++) typeof(FleetProfile).GetField(ProgressFields[i]).SetValue(e.Data, state == 0 ? 0 : FleetCatalog.DailyTarget[i]);
                        e.Data.dailyClaimed = state == 0 ? 0 : state == 1 ? 85 : state == 2 ? 511 : 1023; marks[230] = 30f - elapsed;
                        Populate(game, s, populate, vertices);
                    }
                    method = "DrawSign"; marks[240] = 29f;
                    for (int day = 0; day < 7; day++) for (int state = 0; state < 2; state++) foreach (float flip in new[] { .1f, .2f, .5f })
                    {
                        e.Data.signCount = day + state; e.Data.signKey = state == 1 ? e.Today : e.Today - 1;
                        Field(game, "signedIndex", day); Field(game, "signedAt", 30f - flip); Populate(game, s, populate, vertices);
                    }
                    method = "DrawRecycle"; Field(game, "recycleOpen", true); marks[776] = 29f;
                    foreach (int count in new[] { 0, 1, 8, 20 }) foreach (int page in new[] { -1, 1, 99 })
                    {
                        e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); picks.Clear();
                        for (int i = 0; i < count; i++) { Add(e, i + 1, i % 17, i % 5); if (i % 2 == 0) picks.Add(i + 1); }
                        if (count > 1) e.Data.presets[0] = 1;
                        picks.Add(999); e.Data.revision++; Field(game, "recyclePage", page); Populate(game, s, populate, vertices);
                        Check(!picks.Contains(999) && (count <= 1 || !picks.Contains(1)), "Draw prunes stale/equipped selection");
                    }
                }
            }
            finally
            {
                ScreenLayout.Width = width; ScreenLayout.Height = height;
                PixelSurface s = owner.GetComponent<PixelSurface>();
                if (s != null) { FieldInfo f = typeof(PixelSurface).GetField("ownedMaterial", Flags); var m = (Material)f.GetValue(s); f.SetValue(s, null); if (m != null) UnityEngine.Object.DestroyImmediate(m); }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static void Populate(PixelWingGame game, PixelSurface s, MethodInfo populate, VertexHelper vertices)
        {
            typeof(PixelWingGame).GetMethod("BeginHits", Flags).Invoke(game, null); populate.Invoke(s, new object[] { vertices });
            Check(vertices.currentVertCount > 0, "Modal mesh exists"); UIVertex v = default;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref v, i);
                if (!Finite(v.position.x) || !Finite(v.position.y) || !Finite(v.uv0.x) || !Finite(v.uv0.y)) throw new Exception("Non-finite daily/recycle mesh");
            }
            report.meshes++; report.checkedVertices += vertices.currentVertCount;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
