using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // One CLI stage check for the advanced combat drawing group. No PlayerPrefs writes.
    // Authored-level runs use public model/event paths. Declared fixed render fixtures
    // additionally cover transient phases that the model run need not visit.
    public static class AdvancedCombatStage
    {
        [Serializable] private sealed class Case
        {
            public int model, nose, frames, fired, bursts, vertices, meshes, rocketObservations, pierceObservations, weaponFrames;
            public string state;
        }
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, particleSimulationChecked;
            public string unityVersion, scope, error;
            public int fixtureFrames, checkedVertices;
            public List<Case> cases = new List<Case>();
        }
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo Populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null);
        private static Report report;
        private static IList List(CombatFeedback feedback, string name) => (IList)typeof(CombatFeedback).GetField(name, Private).GetValue(feedback);
        private static object Add(CombatFeedback feedback, string list, string type, params object[] fields)
        {
            object value = Activator.CreateInstance(typeof(CombatFeedback).GetNestedType(type, BindingFlags.NonPublic), true);
            for (int i = 0; i < fields.Length; i += 2) value.GetType().GetField((string)fields[i]).SetValue(value, fields[i + 1]);
            List(feedback, list).Add(value); return value;
        }
        private static int Draw(PixelSurface surface, CombatFeedback feedback, VertexHelper mesh)
        {
            surface.Paint = feedback.DrawTargetsAndShots;
            Populate.Invoke(surface, new object[] { mesh });
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                Check(vertex.position.x); Check(vertex.position.y); Check(vertex.position.z);
                Check(vertex.uv0.x); Check(vertex.uv0.y); Check(vertex.uv1.x); Check(vertex.uv1.y);
            }
            report.checkedVertices += mesh.currentVertCount;
            return mesh.currentVertCount;
        }
        private static void Check(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("Non-finite rendered vertex");
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new Exception("CLI batch mode required");
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-advanced-report");
            if (flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("--flightbeat-advanced-report required");
            string output = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(output)) throw new IOException("Use a fresh report path");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion,
                scope = "15 authored sector01 model/event simulations (45 seconds, dt=1/60, render each 6 steps) and 270 declared phase fixtures. No victory evidence or strict original equivalence claim. Native particles disabled only in this renderer diagnostic." };
            var owner = new GameObject("Advanced combat CLI fixtures", typeof(RectTransform));
            try
            {
                var surface = owner.AddComponent<PixelSurface>(); surface.MapLayout = false;
                var pool = new PixelEffectPool(owner.transform, (RectTransform)owner.transform) { Enabled = false };
                TextAsset asset = Resources.Load<TextAsset>("Levels/sector01");
                if (asset == null) throw new Exception("Missing authored sector01");
                for (int model = 0; model < 5; model++)
                    foreach (int nose in new[] { 1, 3, 5 })
                    {
                        LevelData level = JsonUtility.FromJson<LevelData>(asset.text);
                        var battle = new Battle(level, null, true);
                        Color[] palette = CombatPalette.ForLevel(level);
                        var look = new FleetLook { model = model, stage = nose, parts = new[] { nose, nose, nose, nose, nose } };
                        var weapon = WeaponProfile.For(model, nose); battle.ApplyWeapon(weapon);
                        var feedback = new CombatFeedback(battle, pool, palette) { Look = look, Weapon = weapon };
                        var result = new Case { model = model, nose = nose }; report.cases.Add(result);
                        using (var mesh = new VertexHelper())
                        {
                            for (int frame = 0; frame < 2700; frame++)
                            {
                                if (frame % 24 == 0) for (int lane = 0; lane < battle.LaneCount; lane++) battle.Deploy(lane);
                                battle.Tick(1f / 60f); feedback.Tick(1f / 60f); result.frames++;
                                if (frame % 6 != 0) continue;
                                result.vertices = Math.Max(result.vertices, Draw(surface, feedback, mesh)); result.meshes++;
                                if (List(feedback, "weaponFx").Count > 0) result.weaponFrames++;
                                foreach (object flight in List(feedback, "flights"))
                                {
                                    Shot shot = (Shot)flight.GetType().GetField("shot").GetValue(flight);
                                    if (shot.Rocket) result.rocketObservations++;
                                    if (shot.Pierce > 0) result.pierceObservations++;
                                }
                            }
                            result.fired = feedback.Fired; result.bursts = feedback.WeaponBursts; result.state = battle.State.ToString();
                            if (result.fired == 0 || result.vertices == 0) throw new Exception("No combat observed");
                            if ((model != 0 || nose > 1) && result.bursts == 0) throw new Exception("No advanced weapon event observed");
                        }
                        battle.Dispose();
                        // All fixture data is test-only. Nothing is patched in the game implementation.
                        foreach (float phase in new[] { 0f, .05f, .149f, .28f, .45f, .6f, .72f, .9f, 1f })
                            foreach (float startX in new[] { 120f, 420f })
                            {
                                var fixtureBattle = new Battle(level, null, true);
                                var fixture = new CombatFeedback(fixtureBattle, pool, palette) { Look = look, Weapon = weapon };
                                typeof(CombatFeedback).GetField("time", Private).SetValue(fixture, phase + 1.375f);
                                int target = Array.FindIndex(level.cells, color => color >= 0);
                                var at = new Vector2(270f, 310f);
                                Add(fixture, "flights", "Flight", "from", new Vector2(startX, 680f), "target", target, "color", 0,
                                    "shot", new Shot { Age = phase * .24f, Rocket = true, TargetX = 270f, TargetY = 310f });
                                Add(fixture, "flights", "Flight", "from", new Vector2(startX, 700f), "target", target, "color", 0,
                                    "shot", new Shot { Age = phase * .15f, Pierce = 3 });
                                Add(fixture, "sparks", "PierceSpark", "at", at, "age", phase * .26f, "delay", 0f);
                                Add(fixture, "blasts", "Blast", "at", at, "age", phase * .4f);
                                Add(fixture, "marks", "Mark", "at", at, "age", phase * .3f, "color", 0, "fusion", false);
                                Add(fixture, "marks", "Mark", "at", new Vector2(240f, 500f), "age", phase * .7f, "color", 0, "fusion", true);
                                Add(fixture, "weaponFx", "WeaponFx", "at", at, "extras", new[] { at, new Vector2(285f, 295f), new Vector2(285f, 265f), new Vector2(260f, 245f) },
                                    "color", 0, "kind", (WeaponKind)model, "form", weapon.Form, "age", phase, "life", 1f);
                                List(fixture, "muzzles").Add(new Vector3(startX, 700f, phase * .09f));
                                List(fixture, "goldSparks").Add(new Vector4(startX, 600f, phase * .7f, startX < 270f ? -1f : 1f));
                                using (var mesh = new VertexHelper()) if (Draw(surface, fixture, mesh) == 0) throw new Exception("Empty phase fixture");
                                report.fixtureFrames++; fixtureBattle.Dispose();
                            }
                    }
                report.passed = true;
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally
            {
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                // This probe runs in edit mode; clean its temporary material through the
                // editor API, leaving PixelSurface's original runtime teardown intact.
                PixelSurface surface = owner.GetComponent<PixelSurface>();
                if (surface != null)
                {
                    FieldInfo materialField = typeof(PixelSurface).GetField("ownedMaterial", Private);
                    var material = (Material)materialField.GetValue(surface);
                    materialField.SetValue(surface, null);
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
