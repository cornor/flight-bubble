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
    // Grouped module transactions and complete page meshes, run once per stage.
    public static class HangarGearStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, merges, meshes, checkedVertices, pickerMeshes, bonusMeshes;
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Report report;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-gear-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-gear-report required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Meshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh()
        {
            var e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => report.commits++, n => 0);
            e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); e.Data.nextCore = 1000; return e;
        }
        private static FleetCore Add(FleetEconomy e, int id, int kind, int quality, int durability = 1)
        {
            var core = new FleetCore { id = id, kind = kind, quality = quality, durability = durability }; e.Data.cores.Add(core); return core;
        }
        private static void Transactions()
        {
            var e = Fresh(); Add(e, 10, 0, 0); e.Data.presets[0] = 10; e.Data.presets[5] = 10;
            Check(e.OwnerOf(10) == 0 && e.IsEquipped(10) && !e.IsEquipped(0) && e.OwnerOf(0) == -1 && e.OwnerOf(999) == -1, "Preset ownership and empty id");
            int revision = e.Data.revision;
            Check(!e.Equip(0, 10) && e.Data.presets[5] == 10 && e.Data.revision == revision, "Already-target equip does not normalize duplicates");
            Check(e.Equip(4, 10) && !e.Assembled(4) && e.OwnerOf(10) == 4 && e.Data.presets[0] == 0 && e.Data.presets[5] == 0, "Equip moves every duplicate, including unassembled target");
            Check(!e.Recycle(10) && !e.Equip(-1, 10) && !e.Equip(5, 10) && !e.Equip(0, 999), "Equipped and range guards");
            Check(!e.Unequip(5, 0) && !e.Unequip(0, -1) && e.Unequip(4, 0) && !e.Unequip(4, 0), "Unequip idempotence and bounds");
            int fragments = e.Data.fragments;
            Check(e.Recycle(10) && e.Data.fragments == fragments + 1 && e.Data.cores.Count == 0 && !e.Recycle(10), "Recycle pays once");
            for (int kind = 0; kind < FleetCatalog.Modules.Length; kind++)
                for (int quality = 0; quality < 4; quality++)
                {
                    e = Fresh(); int slot = FleetCatalog.Modules[kind].slot;
                    Add(e, 2, kind, quality); Add(e, 5, kind, quality); Add(e, 3, kind, quality); Add(e, 4, kind, quality);
                    e.Data.presets[slot] = 2; int before = e.Data.fragments;
                    Check(e.CopiesOf(kind, quality) == 4 && e.CanMergeModule(kind, quality), "Four matching modules");
                    FleetCore merged = e.MergeModules(kind, quality); report.merges++;
                    Check(merged.id == 1000 && merged.quality == quality + 1 && merged.durability == FleetCatalog.DurabilityMax[quality + 1] && merged.activated == 0 && merged.expires == 0, "Fresh full-durability merged module");
                    Check(e.Data.cores.Count == 2 && e.InPreset(0, slot).id == 2 && e.Data.nextCore == 1001 && e.Data.fragments == before, "Three unused consumed before equipped, no fragment cost");
                    e = Fresh(); Add(e, 8, kind, quality); Add(e, 2, kind, quality); Add(e, 5, kind, quality);
                    e.Data.presets[slot] = 2; e.Data.presets[slot + 10] = 2; e.Data.presets[slot + 5] = 5;
                    merged = e.MergeModules(kind, quality); report.merges++;
                    Check(e.Data.cores.Count == 1 && e.Data.presets[slot] == merged.id && e.Data.presets[slot + 5] == merged.id && e.Data.presets[slot + 10] == merged.id, "All consumed preset references replaced without normalization");
                    revision = e.Data.revision;
                    Check(e.MergeModules(kind, quality) == null && !e.CanMergeModule(kind, quality) && e.Data.revision == revision, "Insufficient merge is atomic");
                }
            e = Fresh();
            for (int quality = 0; quality < 5; quality++)
            {
                Add(e, quality + 1, 0, quality, 0);
                Check(e.Repair(quality + 1) && e.Data.cores.Find(c => c.id == quality + 1).durability == FleetCatalog.DurabilityMax[quality], "Repair uses quality maximum");
                revision = e.Data.revision; Check(!e.Repair(quality + 1) && e.Data.revision == revision, "Full repair does not commit");
                fragments = e.Data.fragments;
                Check(e.Recycle(quality + 1) && e.Data.fragments == fragments + FleetCatalog.RecycleValue[quality], "Quality recycle payout");
            }
            for (int i = 1; i <= 3; i++) Add(e, i, 0, 4);
            Check(!e.CanMergeModule(0, 4) && e.MergeModules(0, 4) == null && !e.CanMergeModule(0, -1) && e.MergeModules(0, -1) == null && !e.Repair(999), "Legendary/negative quality and missing repair guards");
            e = Fresh(); Add(e, 1, 0, 4, 24); e.Data.presets[5] = 1;
            // Compare stored f32 bits: Mono may retain a wider intermediate in ==.
            float actualResonance = e.Bonus(1).modules[0], expectedResonance = FleetCatalog.Modules[0].values[4] * 1.25f;
            Check(BitConverter.SingleToInt32Bits(actualResonance) == BitConverter.SingleToInt32Bits(expectedResonance),
                "Resonance enters bonus from real preset: " + actualResonance.ToString("R") + " vs " + expectedResonance.ToString("R"));
            bool fail = false;
            e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => { if (fail) throw new IOException("expected save failure"); }, n => 0);
            e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); for (int i = 1; i <= 3; i++) Add(e, i, 0, 0);
            fail = true; FleetProfile original = e.Data; string snapshot = JsonUtility.ToJson(original);
            foreach (Action action in new Action[] { () => e.Equip(0, 1), () => e.Repair(1), () => e.Recycle(1), () => e.MergeModules(0, 0) })
            {
                try { action(); throw new Exception("Save failure swallowed"); }
                catch (IOException) { Check(ReferenceEquals(original, e.Data) && snapshot == JsonUtility.ToJson(e.Data), "Save failure preserves profile and inventory"); }
            }
        }
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static void Meshes()
        {
            var owner = new GameObject("Gear CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false; Field(game, "hud", s);
                MethodInfo draw = typeof(PixelWingGame).GetMethod("DrawUpgrade", Flags);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                s.Paint = surface => draw.Invoke(game, new object[] { surface, 0f, 1f });
                using (var vertices = new VertexHelper())
                    for (int viewport = 0; viewport < 2; viewport++)
                        for (int fighter = 0; fighter < 5; fighter++)
                            for (int state = 0; state < 3; state++)
                            {
                                ScreenLayout.Width = 540f; ScreenLayout.Height = viewport == 0 ? 960f : 1280f;
                                FleetEconomy e = Fresh(); typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e);
                                if (state != 0)
                                {
                                    e.Data.assembled = 31;
                                    for (int i = 0; i < 25; i++) e.Data.parts[i] = state == 2 ? 5 : 1;
                                    for (int kind = 0; kind < 17; kind++) for (int q = 0; q < 5; q++)
                                    {
                                        int id = kind * 5 + q + 1; Add(e, id, kind, q, state == 1 ? 1 : FleetCatalog.DurabilityMax[q]);
                                        if (q == 4) e.Data.presets[fighter * 5 + FleetCatalog.Modules[kind].slot] = id;
                                    }
                                }
                                Field(game, "hangarFighter", fighter); Field(game, "hangarTab", 1); Field(game, "moduleInspect", -1); Field(game, "gearZoom", 0f); Field(game, "ui", 30f);
                                Populate(game, s, populate, vertices);
                                for (int slot = 0; slot < 5; slot++)
                                {
                                    Field(game, "moduleInspect", slot); Field(game, "previousPickSlot", slot); Field(game, "gearZoom", 1f);
                                    Field(game, "pickPage", state == 2 ? 100 : 0); Populate(game, s, populate, vertices); report.pickerMeshes++;
                                }
                                Field(game, "moduleInspect", -1); Field(game, "hangarTab", 2); Populate(game, s, populate, vertices); report.bonusMeshes++;
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
            Check(vertices.currentVertCount > 0, "Page mesh exists"); UIVertex v = default;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref v, i);
                if (!Finite(v.position.x) || !Finite(v.position.y) || !Finite(v.uv0.x) || !Finite(v.uv0.y)) throw new Exception("Non-finite gear mesh");
            }
            report.meshes++; report.checkedVertices += vertices.currentVertCount;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
