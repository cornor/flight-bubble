#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class HelperCase
        {
            public string name, method;
            public int index, kind, streak, uiBits, phase, tutorialStep, firepowerBits, strongBits, timeBits, lastFireBits, idleSlowBits, pierce;
            public bool enabled, finale;
            public int[] floatBits;
        }
        [Serializable] private sealed class HelperInput { public string profileEncoded, pressureJson; public HelperCase[] cases; }
        [Serializable] private sealed class HelperScalar { public string name; public int[] values; }
        [Serializable] private sealed class HelperScalars { public List<HelperScalar> scalars = new List<HelperScalar>(); public int unexpectedSaves; }
        public static void RunSourceHelpers()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            var input = JsonUtility.FromJson<HelperInput>(File.ReadAllText(Arg("--flightbeat-helper-input")));
            string output = Path.GetFullPath(Arg("--flightbeat-helper-report"));
            if (File.Exists(output) || File.Exists(output + ".scalars.json")) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = new Report { unityVersion = Application.unityVersion, mode = "game" };
            var scalars = new HelperScalars();
            var layout = typeof(ScreenLayout).GetFields(BindingFlags.Public | BindingFlags.Static);
            var saved = new Dictionary<FieldInfo, object>();
            foreach (var field in layout) if (!field.IsLiteral && !field.IsInitOnly) saved.Add(field, field.GetValue(null));
            try
            {
                ScreenLayout.Configure(540, 960, new Rect(0f, 0f, 1f, 1f));
                foreach (var c in input.cases)
                {
                    var result = new CaseOutput { name = c.name };
                    var owner = new GameObject("Source drawing helper fixture"); owner.SetActive(false);
                    Battle battle = null; var helpers = new Dictionary<PixelSurface, VertexHelper>();
                    try
                    {
                        var game = owner.AddComponent<PixelWingGame>();
                        battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/training01").text), JsonUtility.FromJson<PressureSettings>(input.pressureJson), true);
                        int saves = 0;
                        var economy = new FleetEconomy(FleetSaveStore.Decode(input.profileEncoded), () => 1800000000L, _ => saves++,
                            _ => throw new InvalidOperationException("Undeclared random input"));
                        saves = 0; economy.Data.streak = c.streak;
                        Set(game, "Battle", battle); Set(game, "Economy", economy);
                        Set(game, "ui", BitConverter.Int32BitsToSingle(c.uiBits)); Set(game, "tutorialStep", c.tutorialStep);
                        Set(battle, "State", (Phase)c.phase); Set(battle, "Time", BitConverter.Int32BitsToSingle(c.timeBits));
                        Set(battle, "Firepower", BitConverter.Int32BitsToSingle(c.firepowerBits)); Set(battle, "lastFire", BitConverter.Int32BitsToSingle(c.lastFireBits));
                        Set(battle, "ActiveFinale", c.finale ? new Finale() : null);
                        battle.Pressure.enabled = c.enabled; battle.Pressure.strongFirepower = BitConverter.Int32BitsToSingle(c.strongBits);
                        battle.Loadout.idleSlow = BitConverter.Int32BitsToSingle(c.idleSlowBits); battle.Loadout.pierce = c.pierce;
                        var method = typeof(PixelWingGame).GetMethod(c.method, Instance | BindingFlags.Static) ?? throw new MissingMethodException(c.method);
                        if (c.method == "ZoneTile")
                        {
                            var v = (Vector2Int)method.Invoke(null, new object[] { c.kind });
                            scalars.scalars.Add(new HelperScalar { name = c.name, values = new[] { v.x, v.y } });
                            continue;
                        }
                        report.cases.Add(result);
                        var surfaces = new Dictionary<PixelSurface, string>(); PixelSurface world = null;
                        foreach (string name in new[] { "world", "hud" })
                        {
                            var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(owner.transform, false);
                            var surface = child.AddComponent<PixelSurface>(); Call(surface, "Awake");
                            surfaces.Add(surface, name); if (name == "world") world = surface;
                            var helper = new VertexHelper(); helpers.Add(surface, helper);
                            // Initialize actual mesh outside capture with no Paint delegate.
                            Call(surface, "OnPopulateMesh", helper);
                        }
                        float[] vls = Array.ConvertAll(c.floatBits, BitConverter.Int32BitsToSingle);
                        object[] args;
                        switch (c.method)
                        {
                            case "Panel": args = new object[] { world, vls[0], vls[1], vls[2], vls[3], new Color(vls[4], vls[5], vls[6], vls[7]) }; break;
                            case "ModuleEmblem": args = new object[] { world, c.kind, vls[0], vls[1], vls[2], vls[3], new Color(vls[4], vls[5], vls[6], vls[7]) }; break;
                            case "DrawStreakBadge": args = new object[] { world, vls[0], vls[1], vls[2] }; break;
                            case "DrawSpeedGauge": args = new object[] { world }; break;
                            default: args = new object[] { world, vls[0], vls[1] }; break;
                        }
                        using (var observation = new DrawObservation(surfaces))
                        {
                            result.observed = observation.Result;
                            foreach (var pair in surfaces)
                            {
                                string name = pair.Value;
                                pair.Key.QuadObserver = q => {
                                    var vertices = new List<int>(); foreach (UIVertex v in q.vertices) vertices.AddRange(Pack(v));
                                    result.commands.Add(new QuadOutput { surface = name, sequence = result.commands.Count, order = q.order, callPath = q.callPath,
                                        inputBits = Array.ConvertAll(q.input, Bits), vertexBits = vertices.ToArray(), rigid = q.rigid, mapLayout = q.mapLayout, lighting = q.lighting });
                                };
                            }
                            method.Invoke(method.IsStatic ? null : game, args);
                            foreach (var pair in surfaces) result.meshes.Add(DrawObservation.ReadMesh(pair.Value, helpers[pair.Key]));
                        }
                        scalars.unexpectedSaves += saves;
                    }
                    catch (Exception e) { result.error = e.ToString(); if (!report.cases.Contains(result)) report.cases.Add(result); }
                    finally { foreach (var h in helpers.Values) h.Dispose(); battle?.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
                }
            }
            finally
            {
                foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
                File.WriteAllText(output, JsonUtility.ToJson(report)); File.WriteAllText(output + ".scalars.json", JsonUtility.ToJson(scalars));
            }
            foreach (var c in report.cases) if (!string.IsNullOrEmpty(c.error)) throw new Exception(c.name + ": " + c.error);
            if (scalars.unexpectedSaves != 0) throw new Exception("Helper saved profile");
        }
    }
}
#endif
