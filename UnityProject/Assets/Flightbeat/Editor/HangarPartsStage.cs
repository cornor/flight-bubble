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
    // One grouped stage check; fixtures are not proof of original draw equality.
    public static class HangarPartsStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, upgrades, assemblies, meshes, checkedVertices, animationMeshes;
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Report report;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-hangar-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-hangar-report required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Meshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh()
            => new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => report.commits++, n => 0);
        private static void Transactions()
        {
            FleetEconomy e = Fresh(); int revision = e.Data.revision;
            Check(e.GiftStarterParts() && e.Spare(0, 0) == 2 && e.Data.starterGift && e.Data.revision == revision + 1, "Starter gift commits once");
            revision = e.Data.revision;
            Check(!e.GiftStarterParts() && e.Data.revision == revision && e.UpgradePart(0, 0) && e.PartLevel(0, 0) == 2 && e.Spare(0, 0) == 0, "Gift buys exact first upgrade");
            Check(e.FighterLevel(0) == 1, "Single upgraded part cannot raise whole-airframe level");
            e.CompleteGrowthTutorial(); revision = e.Data.revision; e.CompleteGrowthTutorial();
            Check(e.Data.growthTutorial && e.Data.revision == revision, "Tutorial completion idempotent");
            Check(!e.UpgradePart(-1, 0) && !e.UpgradePart(5, 0) && !e.UpgradePart(0, -1) && !e.UpgradePart(0, 5), "Part range guards");
            Check(e.Spare(-1, 0) == 0 && e.Spare(0, 5) == 0 && !e.SmeltTarget(-1) && !e.SmeltTarget(25), "Read range guards");
            e = Fresh();
            // Distinct preset IDs make copy-from-current-fighter observable.
            for (int slot = 0; slot < 5; slot++) e.Data.presets[slot] = 101 + slot;
            for (int fighter = 1; fighter < 5; fighter++)
            {
                for (int part = 0; part < 4; part++) e.Data.parts[fighter * 5 + part] = 1;
                revision = e.Data.revision;
                Check(!e.Assemble(fighter) && e.Data.revision == revision, "Four pieces cannot assemble");
                e.Data.parts[fighter * 5 + 4] = 1;
                int source = e.ActiveFighter; int[] before = (int[])e.Data.presets.Clone();
                Check(e.Assemble(fighter) && e.Assembled(fighter) && e.Collected(fighter) == 5, "Five first copies assemble"); report.assemblies++;
                for (int slot = 0; slot < 5; slot++)
                    Check(e.Data.presets[fighter * 5 + slot] == before[source * 5 + slot] && e.Data.presets[source * 5 + slot] == before[source * 5 + slot], "Preset copied, source untouched");
                revision = e.Data.revision;
                Check(!e.Assemble(fighter) && e.Data.revision == revision && e.SetActive(fighter) && e.ActiveFighter == fighter, "No duplicate assembly; active switch");
                for (int slot = 0; slot < 5; slot++) e.Data.presets[fighter * 5 + slot] += 10;
            }
            Check(!e.Assemble(-1) && !e.Assemble(0) && !e.Assemble(5), "Assembly range guards");
            for (int fighter = 0; fighter < 5; fighter++)
                for (int next = 2; next <= 5; next++)
                {
                    for (int part = 0; part < 5; part++)
                    {
                        int index = fighter * 5 + part, cost = FleetCatalog.PartCost[next];
                        e.Data.spares[index] = cost - 1; revision = e.Data.revision;
                        Check(!e.UpgradePart(fighter, part) && e.Data.revision == revision && e.SmeltTarget(index), "Shortage is atomic and permits smelt target");
                        e.Data.spares[index] = cost;
                        Check(!e.SmeltTarget(index) && e.UpgradePart(fighter, part) && e.PartLevel(fighter, part) == next && e.Spare(fighter, part) == 0, "Exact upgrade debit"); report.upgrades++;
                        Check(e.FighterLevel(fighter) == (part == 4 ? next : next - 1), "Whole-airframe minimum follows last upgraded part");
                    }
                }
            for (int i = 0; i < 25; i++) { e.Data.spares[i] = 100; Check(!e.UpgradePart(i / 5, i % 5) && !e.SmeltTarget(i), "MAX guards"); }
            Check(e.SmeltableSpares(12) == 2400, "Smelt sum excludes only target");
            e.Data.assembled = 1; e.Data.parts[5] = 0;
            Check(e.SmeltTarget(5), "Uncollected first copy is a target"); e.Data.parts[5] = 1;
            Check(!e.SmeltTarget(5) && !e.UpgradePart(1, 0), "Unassembled first copy cannot be consumed/upgraded");
            bool fail = false;
            e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => { if (fail) throw new IOException("expected save failure"); }, n => 0);
            e.Data.spares[0] = 2; FleetProfile original = e.Data; fail = true;
            try { e.UpgradePart(0, 0); throw new Exception("Save failure swallowed"); }
            catch (IOException) { Check(ReferenceEquals(original, e.Data) && e.Spare(0, 0) == 2 && e.PartLevel(0, 0) == 1, "Save failure preserves old state"); }
        }
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static void Meshes()
        {
            var owner = new GameObject("Hangar CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false;
                Field(game, "hud", s); Field(game, "assemblySoundStep", 5);
                MethodInfo draw = typeof(PixelWingGame).GetMethod("DrawUpgrade", Flags), assembly = typeof(PixelWingGame).GetMethod("DrawAssembly", Flags);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                bool isAssembly = false; float slide = 0f;
                s.Paint = surface => { if (isAssembly) assembly.Invoke(game, new object[] { surface }); else draw.Invoke(game, new object[] { surface, slide, slide }); };
                using (var vertices = new VertexHelper())
                    for (int viewport = 0; viewport < 2; viewport++)
                    {
                        ScreenLayout.Width = 540f; ScreenLayout.Height = viewport == 0 ? 960f : 1280f;
                        for (int fighter = 0; fighter < 5; fighter++)
                        {
                            FleetEconomy e = Fresh(); typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e);
                            Field(game, "hangarFighter", fighter);
                            var marks = (Dictionary<int, float>)typeof(PixelWingGame).GetField("uiMarks", Flags).GetValue(game);
                            isAssembly = false;
                            for (int state = 0; state < 6; state++)
                            {
                                for (int part = 0; part < 5; part++) { e.Data.parts[fighter * 5 + part] = state == 0 ? part % 2 : state; e.Data.spares[fighter * 5 + part] = part % 2 == 0 ? 12 : 0; }
                                e.Data.assembled = state == 0 ? 0 : 1 << fighter;
                                Field(game, "hangarPart", state % 5); Field(game, "partFlash", fighter * 5 + state % 5);
                                Field(game, "ui", 30f + state * .13f); Field(game, "pressedId", 320 + state % 5); Field(game, "pressedAt", 30f);
                                marks[490] = 30f; marks[491] = 30f; marks[495] = 30f; slide = state == 0 ? 60f : 0f;
                                Populate(game, s, populate, vertices);
                            }
                            for (int part = 0; part < 5; part++) e.Data.parts[fighter * 5 + part] = 1;
                            e.Data.assembled = 1 << fighter; Field(game, "assemblyFighter", fighter); Field(game, "assemblyAt", 0f);
                            isAssembly = true;
                            foreach (float age in new[] { 0f, .2f, .55f, .76f, .97f, 1.12f, 1.33f, 1.51f, 1.7f, 1.89f, 2.1f, 2.409f, 2.41f, 2.6100001335144043f, 2.72f, 2.8600001335144043f, 3f, 3.31f, 3.6f, 5f })
                            { Field(game, "ui", age); Populate(game, s, populate, vertices); report.animationMeshes++; }
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
        private static void Populate(PixelWingGame game, PixelSurface surface, MethodInfo populate, VertexHelper vertices)
        {
            typeof(PixelWingGame).GetMethod("BeginHits", Flags).Invoke(game, null);
            populate.Invoke(surface, new object[] { vertices }); Check(vertices.currentVertCount > 0, "Mesh exists");
            UIVertex vertex = default;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                if (!Finite(vertex.position.x) || !Finite(vertex.position.y) || !Finite(vertex.uv0.x) || !Finite(vertex.uv0.y)) throw new Exception("Non-finite hangar mesh");
            }
            report.meshes++; report.checkedVertices += vertices.currentVertCount;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
