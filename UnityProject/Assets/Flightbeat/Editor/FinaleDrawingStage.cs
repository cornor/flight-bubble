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
    // One grouped CLI diagnostic. Authored battles reach FinaleReady through public
    // deployment/tick paths; isolated declared fixtures cover exact draw boundaries.
    public static class FinaleDrawingStage
    {
        [Serializable] private sealed class Case
        {
            public int mode, stage, preparationFrames, drawFrames, beamCallbacks, hitCells, kills, maximumVertices;
            public bool won, faded;
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
        private static void Set(PixelWingGame game, string field, object value) => typeof(PixelWingGame).GetField(field, Private).SetValue(game, value);
        private static void Property(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetProperty(name).SetValue(game, value);
        private static Delegate Bind(PixelWingGame game, string name, Type type) => Delegate.CreateDelegate(type, game, typeof(PixelWingGame).GetMethod(name, Private));
        private static int Draw(PixelSurface surface, VertexHelper mesh)
        {
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
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("Non-finite finale geometry");
        }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-finale-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-finale-report required");
            string output = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(output)) throw new IOException("Use a fresh report path");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion,
                scope = "12 sector01 battles with declared model/stage inputs, public deployment/finale events and full DrawFinale meshes; exact-age fixtures for each effect/blast branch. No PlayerPrefs writes, particles/audio simulation, original equivalence or original victory claim." };
            var owner = new GameObject("Finale drawing CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            try
            {
                var game = owner.AddComponent<PixelWingGame>();
                var surface = owner.AddComponent<PixelSurface>(); surface.MapLayout = false;
                var pool = new PixelEffectPool(owner.transform, (RectTransform)owner.transform) { Enabled = false };
                var drawFinale = (Action<PixelSurface>)Bind(game, "DrawFinale", typeof(Action<PixelSurface>));
                var drawBlasts = (Action<PixelSurface, int>)Bind(game, "DrawFinaleBlasts", typeof(Action<PixelSurface, int>));
                Set(game, "effects", pool);
                for (int mode = 0; mode < 6; mode++)
                    foreach (int stage in mode == 0 ? new[] { 1, 2 } : new[] { 3, 5 })
                    {
                        typeof(PixelWingGame).GetMethod("ResetFinale", Private).Invoke(game, null);
                        int model = mode == 5 ? 0 : mode;
                        var profile = new FleetProfile { activeFighter = model };
                        var economy = new FleetEconomy(profile, () => 1791676800L, _ => { }, _ => 0);
                        for (int part = 0; part < 5; part++) economy.Data.parts[model * 5 + part] = stage;
                        Property(game, "Economy", economy);
                        LevelData level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector01").text);
                        var battle = new Battle(level, null, true); Property(game, "Battle", battle);
                        Color[] palette = CombatPalette.ForLevel(level); Set(game, "colors", palette);
                        var feedback = new CombatFeedback(battle, pool, palette) { Look = new FleetLook { model = model, stage = stage, parts = new[] { stage, stage, stage, stage, stage } }, Weapon = WeaponProfile.For(model, stage) };
                        battle.ApplyWeapon(feedback.Weapon); Set(game, "feedback", feedback);
                        var result = new Case { mode = mode, stage = stage }; report.cases.Add(result);
                        battle.FinaleStarted += (Action<Finale>)Bind(game, "OnFinaleStarted", typeof(Action<Finale>));
                        battle.FinaleBeam += (Action<Finale, List<int>>)Bind(game, "OnFinaleBeam", typeof(Action<Finale, List<int>>));
                        battle.FinaleBeam += (_, hits) => { result.beamCallbacks++; result.hitCells += hits.Count; };
                        int frame = 0;
                        for (; frame < 3600 && !battle.FinaleReady && battle.State != Phase.Won && battle.State != Phase.Lost; frame++)
                        {
                            if (frame % 24 == 0) for (int lane = 0; lane < battle.LaneCount; lane++) battle.Deploy(lane);
                            battle.Tick(1f / 60f); feedback.Tick(1f / 60f);
                        }
                        result.preparationFrames = frame;
                        if (!battle.LaunchFinale(mode)) throw new Exception("Authored battle did not reach FinaleReady for mode " + mode + " stage " + stage);
                        Finale finale = battle.ActiveFinale;
                        surface.Paint = drawFinale;
                        using (var mesh = new VertexHelper())
                        {
                            for (int step = 0; step < 330; step++)
                            {
                                float clock = (float)(frame + step) / 60f; Set(game, "clock", clock); Set(game, "ui", clock);
                                battle.Tick(1f / 60f); feedback.Tick(1f / 60f);
                                int count = Draw(surface, mesh); result.maximumVertices = Math.Max(result.maximumVertices, count); result.drawFrames++;
                                if (step == 329) result.faded = count == 0;
                            }
                            result.kills = finale.Kills; result.won = battle.State == Phase.Won;
                            if (!result.won || !result.faded || result.kills == 0 || result.hitCells != result.kills || result.maximumVertices == 0)
                                throw new Exception("Incomplete finale sequence for mode " + mode + " stage " + stage);
                            // Declared age fixtures include charge start, sweep edges and both fade boundaries.
                            string[] names = { "DrawFanLaser", "DrawGale", "DrawBlackHole", "DrawFlamethrower", "DrawBarrier", "DrawPulseNova" };
                            var effect = (Action<PixelSurface, Finale, float, float>)Bind(game, names[mode], typeof(Action<PixelSurface, Finale, float, float>));
                            foreach (float age in new[] { 1.15f, 1.35f, 1.649f, 1.65f, 2.1f, 2.8f, 3.149f, 3.15f, 3.449f, 3.55f, 3.85f })
                                foreach (float after in new[] { 0f, .25f, .5f })
                                {
                                    if (mode != 2 && mode != 4 && age < 1.65f) continue;
                                    finale.Age = age; Set(game, "clock", age + 1.375f);
                                    surface.Paint = s => effect(s, finale, age, after); Draw(surface, mesh); report.fixtureFrames++;
                                }
                            var blasts = (IList)typeof(PixelWingGame).GetField("finaleBlasts", Private).GetValue(game);
                            Type blastType = typeof(PixelWingGame).GetNestedType("FinaleBlast", BindingFlags.NonPublic);
                            foreach (float age in new[] { -.01f, 0f, .089f, .09f, .119f, .12f, .274f, .276f, .49f, .5f, .501f })
                            {
                                blasts.Clear(); object blast = Activator.CreateInstance(blastType);
                                blastType.GetField("at").SetValue(blast, new Vector2(137f, 286f));
                                blastType.GetField("born").SetValue(blast, 0f); blastType.GetField("color").SetValue(blast, new Color(.3f, .7f, .9f, .6f));
                                blastType.GetField("seed").SetValue(blast, 47); blasts.Add(blast); Set(game, "clock", age);
                                surface.Paint = s => drawBlasts(s, mode); int count = Draw(surface, mesh); report.fixtureFrames++;
                                if ((age < 0f || age > .5f) && count != 0) throw new Exception("Expired/delayed blast drew geometry");
                            }
                        }
                        battle.Dispose();
                    }
                report.passed = true;
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally
            {
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                PixelSurface surface = owner.GetComponent<PixelSurface>();
                if (surface != null)
                {
                    FieldInfo field = typeof(PixelSurface).GetField("ownedMaterial", Private);
                    var material = (Material)field.GetValue(surface); field.SetValue(surface, null);
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
