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
    // One grouped stage: source transaction boundaries and complete modal meshes.
    public static class SupplySmeltStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, randomCalls, meshes, checkedVertices;
        }
        private sealed class Rolls
        {
            private readonly Queue<int> bounds = new Queue<int>(), values = new Queue<int>();
            public bool armed;
            public void Add(int bound, int value) { bounds.Enqueue(bound); values.Enqueue(value); }
            public int Next(int bound)
            {
                if (!armed) return 0;
                report.randomCalls++;
                Check(bounds.Count > 0 && bounds.Dequeue() == bound, "RNG call count/order/bound");
                return values.Dequeue();
            }
            public void Done() => Check(bounds.Count == 0 && values.Count == 0, "All source RNG consumed");
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Report report;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-supply-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-supply-report required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Meshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh(Rolls rolls = null, Action<FleetProfile> save = null)
        {
            var e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800,
                d => { report.commits++; save?.Invoke(d); }, n => rolls == null ? 0 : rolls.Next(n));
            e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); Array.Clear(e.Data.spares, 0, 25); Array.Clear(e.Data.parts, 0, 25);
            e.Data.assembled = 1; for (int i = 0; i < 5; i++) e.Data.parts[i] = 1;
            e.Data.fragments = 100000; e.Data.nextCore = 1000; e.Data.epicPity = e.Data.legendPity = 0;
            if (rolls != null) rolls.armed = true; return e;
        }
        private static void Transactions()
        {
            int choices = FleetCatalog.DrawableModules.Length;
            int[] boundary = { 0, 39, 40, 69, 70, 89, 90, 97, 98, 99 }, expected = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };
            for (int i = 0; i < boundary.Length; i++)
            {
                var rng = new Rolls(); var e = Fresh(rng); rng.Add(100, boundary[i]); rng.Add(choices, choices - 1);
                FleetCore result = e.DrawCore(); rng.Done();
                Check(result.quality == expected[i] && result.kind == FleetCatalog.DrawableModules[choices - 1] && result.id == 1000
                    && result.durability == FleetCatalog.DurabilityMax[expected[i]] && ReferenceEquals(result, e.Data.cores[0]), "Quality boundaries, full durability and published identity");
                Check(e.Data.fragments == 99990 && e.Data.draws == 1 && e.Data.nextCore == 1001, "Single debit/id/counter");
            }
            {
                var rng = new Rolls(); var e = Fresh(rng);
                for (int i = 0; i < 20; i++) { rng.Add(100, 0); rng.Add(choices, 0); }
                FleetCore[] first = e.Draw(10), second = e.Draw(10); rng.Done();
                Check(first[8].quality == 0 && first[9].quality == 3 && second[8].quality == 0 && second[9].quality == 4,
                    "Ten-draw epic and twentieth legendary consume RNG and carry pity across batches");
                Check(e.Data.epicPity == 0 && e.Data.legendPity == 0 && e.Data.fragments == 99820 && e.Data.draws == 20, "Batch pity reset and price");
                Check(e.Draw(0) == null && e.Draw(-1) == null, "Nonpositive count denied");
                e.Data.fragments = 9; Check(e.DrawCore() == null, "Insufficient fragments denied before RNG");
                e.Data.fragments = 1000; while (e.Data.cores.Count < 119) e.Data.cores.Add(new FleetCore { id = e.Data.cores.Count + 1 });
                rng.Add(100, 50); rng.Add(choices, 0); Check(e.DrawCore() != null && e.Data.cores.Count == 120, "Last inventory space allowed");
                int revision = e.Data.revision; Check(e.DrawCore() == null && e.Data.revision == revision, "Full inventory denied before debit/RNG"); rng.Done();
            }
            foreach (bool modules in new[] { false, true })
            {
                var rng = new Rolls(); var e = Fresh(rng);
                for (int i = 0; i < 11; i++) { rng.Add(modules ? 100 : 25, 0); if (modules) rng.Add(choices, 0); }
                Check(modules ? e.Draw(11).Length == 11 : e.DrawParts(11, -1).Length == 11, "Source count above ten is not clamped");
                rng.Done(); Check(e.Data.fragments == 99910, "Source >=10 fixed price 90");
            }
            for (int target = 0; target < 5; target++)
            {
                var rng = new Rolls(); var e = Fresh(rng);
                for (int part = 0; part < 5; part++)
                {
                    rng.Add(100, 49); rng.Add(5, part);
                    Check(e.DrawParts(1, target)[0].index == target * 5 + part, "49 hits target fighter");
                }
                int expectedIndex = 0;
                for (int other = 0; other < 20; other++)
                {
                    if (expectedIndex == target * 5) expectedIndex += 5;
                    rng.Add(100, 50); rng.Add(20, other);
                    Check(e.DrawParts(1, target)[0].index == expectedIndex++, "50 maps all other fighter parts once");
                }
                rng.Done();
            }
            {
                var rng = new Rolls(); var e = Fresh(rng); rng.Add(25, 7); rng.Add(25, 7); rng.Add(25, 24);
                PartPull[] parts = e.DrawParts(3, 5); rng.Done();
                Check(parts[0].first && !parts[1].first && parts[2].first && parts[2].index == 24 && e.Data.parts[7] == 1 && e.Data.spares[7] == 1,
                    "Out-of-range target becomes untargeted; repeated part within batch is first only once");
                Check(e.Data.partDraws == 3 && e.Data.fragments == 99970, "Part count/debit");
                e.Data.fragments = 9; Check(e.DrawParts(1, 0) == null && e.DrawParts(0, -1) == null, "Part denial before RNG");
            }
            {
                var e = Fresh(); e.Data.spares[1] = 3;
                Check(!e.Smelt(9, new[] { 1, 1 }) && !e.Smelt(9, new[] { 1, 1, 9 }) && !e.Smelt(9, new[] { 1, 1, 25 })
                    && !e.Smelt(9, new[] { 1, 1, -1 }) && !e.Smelt(-1, new[] { 1, 1, 1 }), "Smelt length/target/range guards");
                Check(e.Smelt(9, new[] { 1, 1, 1 }) && e.Data.parts[9] == 1 && e.Data.spares[9] == 0 && e.Data.spares[1] == 0 && e.Data.smelts == 1,
                    "Three duplicates become one first copy without consuming permanent ownership");
                e.Data.spares[1] = 3; Check(!e.Smelt(9, new[] { 1, 1, 1 }), "Collected unassembled target denied");
                Check(e.Smelt(0, new[] { 1, 1, 1 }) && e.Data.parts[0] == 1 && e.Data.spares[0] == 1, "Assembled target receives spare, no implicit upgrade");
                e.Data.spares[1] = 3; Check(e.Smelt(0, new[] { 1, 1, 1 }), "Fill remaining upgrade shortage");
                e.Data.spares[1] = 3; Check(!e.Smelt(0, new[] { 1, 1, 1 }), "Enough upgrade copies denies smelt");
                e.Data.parts[0] = 5; e.Data.spares[0] = 0; Check(!e.Smelt(0, new[] { 1, 1, 1 }), "Max-level target denied");
                e.Data.spares[1] = 2; Check(!e.Smelt(10, new[] { 1, 1, 1 }), "Repeated materials cannot exceed inventory");
                e.Data.parts[0] = 1; e.Data.spares[0] = 2; e.Data.spares[2] = 1;
                Check(string.Join(",", e.SuggestSmelt(4)) == "0,1,0", "Greedy tie order updates quantities after each pick");
                Array.Clear(e.Data.spares, 0, 25); e.Data.spares[1] = 25; e.Data.parts[7] = 5; e.Data.spares[7] = 2;
                Check(string.Join(",", e.SuggestSmelt(4)) == "7,7,1" && e.Data.spares[7] == 2, "Surplus priority and recommendation does not consume");
                Array.Clear(e.Data.spares, 0, 25); e.Data.spares[1] = 2; Check(e.SuggestSmelt(4) == null, "Insufficient recommendation returns no partial list");
                e.Data.parts[24] = -1; Check(e.StillNeeded(24) == 27, "Needed clamps below zero");
                e.Data.parts[24] = 6; Check(e.StillNeeded(24) == 0, "Needed clamps above five");
            }
            for (int action = 0; action < 3; action++)
            {
                bool fail = false; var rng = new Rolls(); var e = Fresh(rng, d => { if (fail) throw new IOException("expected save failure"); });
                e.Data.spares[1] = 3; FleetProfile original = e.Data; string before = JsonUtility.ToJson(original); fail = true;
                if (action == 0) { rng.Add(100, 98); rng.Add(choices, 0); } else if (action == 1) rng.Add(25, 9);
                try { if (action == 0) e.DrawCore(); else if (action == 1) e.DrawParts(1, -1); else e.Smelt(9, new[] { 1, 1, 1 }); throw new Exception("Save failure swallowed"); }
                catch (IOException) { Check(ReferenceEquals(original, e.Data) && before == JsonUtility.ToJson(e.Data), "Failed save does not publish debit/inventory/pity"); }
                rng.Done();
            }
        }
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static void Meshes()
        {
            var owner = new GameObject("Supply CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false; Field(game, "hud", s);
                var e = Fresh(); typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e); Field(game, "ui", 30f);
                var marks = (Dictionary<int, float>)typeof(PixelWingGame).GetField("uiMarks", Flags).GetValue(game);
                var picks = (List<int>)typeof(PixelWingGame).GetField("smeltPick", Flags).GetValue(game);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                string method = "DrawSupply"; s.Paint = surface => typeof(PixelWingGame).GetMethod(method, Flags).Invoke(game, new object[] { surface });
                using (var vertices = new VertexHelper())
                    for (int viewport = 0; viewport < 2; viewport++)
                    {
                        ScreenLayout.Width = 540f; ScreenLayout.Height = viewport == 0 ? 960f : 1280f;
                        method = "DrawSupply";
                        for (int pool = 0; pool < 2; pool++) for (int target = -1; target < 5; target++) for (int entry = 0; entry < 2; entry++)
                        {
                            Field(game, "supplyPool", pool); Field(game, "supplyTarget", target); marks[423] = entry == 0 ? 29.95f : 29f;
                            e.Data.fragments = entry == 0 ? 0 : 1000; Populate(game, s, populate, vertices);
                        }
                        method = "DrawSmelt";
                        for (int state = 0; state < 4; state++) for (int target = 0; target < 25; target += 6)
                        {
                            for (int i = 0; i < 25; i++) { e.Data.parts[i] = state == 0 ? 0 : 1; e.Data.spares[i] = state == 0 ? 0 : i + 1; }
                            Field(game, "smeltTarget", target); marks[478] = state == 1 ? 29.95f : 29f;
                            picks.Clear(); if (state > 1) { int source = (target + 1) % 25; picks.Add(source); picks.Add(source); if (state == 3) picks.Add((target + 2) % 25); }
                            Populate(game, s, populate, vertices);
                        }
                        method = "DrawGacha";
                        for (int pool = 0; pool < 2; pool++) foreach (int count in new[] { 1, 10 }) for (int rarity = 0; rarity < 5; rarity++)
                        {
                            var parts = new PartPull[count]; var cores = new FleetCore[count];
                            for (int i = 0; i < count; i++) { parts[i] = new PartPull { index = (i * 2 + rarity) % 25, first = i % 2 == 0 }; cores[i] = new FleetCore { id = i + 1, kind = (i + rarity) % 17, quality = rarity, durability = 24 }; }
                            Field(game, "supplyPool", pool); Field(game, "lastDrawCount", count); Field(game, "gachaNew", rarity % 2 == 0); Field(game, "gachaBest", rarity);
                            Field(game, "partResults", pool == 0 ? parts : null); Field(game, "gachaResults", pool == 1 ? cores : null);
                            foreach (float age in new[] { .5f, 1.3f, 1.48f, 1.75f, 4f })
                            { Field(game, "gachaAge", age); Field(game, "gachaRevealed", age >= 1.45f); Populate(game, s, populate, vertices); }
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
                if (!Finite(v.position.x) || !Finite(v.position.y) || !Finite(v.uv0.x) || !Finite(v.uv0.y)) throw new Exception("Non-finite supply mesh");
            }
            report.meshes++; report.checkedVertices += vertices.currentVertCount;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
