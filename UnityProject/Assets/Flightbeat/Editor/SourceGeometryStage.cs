#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class SourceGeometryStage
    {
        [Serializable] private sealed class Case
        {
            public string name, method, kind;
            public int index, stage, clockBits, ageBits, assembled;
            public bool hasEconomy, hasLook;
            public int[] ints, floatBits, parts, spares, expected;
        }
        [Serializable] private sealed class Input { public string profileEncoded, levelJson, pressureJson; public Case[] cases; }
        [Serializable] private sealed class Failure { public string name; public int component, expected, actual; }
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string error;
            public int cases, values, unexpectedSaves;
            public List<Failure> failures = new List<Failure>();
            public List<int> functions = new List<int>();
        }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static void Set(object target, string name, object value) =>
            (target.GetType().GetField(name, Flags) ?? target.GetType().GetField("<" + name + ">k__BackingField", Flags)
                ?? throw new MissingFieldException(name)).SetValue(target, value);
        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name);
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException(name);
            return Path.GetFullPath(args[at + 1]);
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-geometry-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-geometry-input")));
            if (input.cases == null || input.cases.Length == 0) throw new InvalidDataException("No declared cases");
            var report = new Report();
            var owner = new GameObject("Original geometry helper fixture"); owner.SetActive(false);
            Battle battle = null;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); // no Awake/PlayerLoop
                battle = new Battle(JsonUtility.FromJson<LevelData>(input.levelJson), JsonUtility.FromJson<PressureSettings>(input.pressureJson), true);
                int saves = 0;
                var economy = new FleetEconomy(FleetSaveStore.Decode(input.profileEncoded), () => 1800000000L, _ => saves++,
                    _ => throw new InvalidOperationException("Undeclared random request"));
                int initialSaves = saves;
                var feedback = new CombatFeedback(battle, null, new Color[7]);
                Set(game, "feedback", feedback);
                var finale = new Finale();
                foreach (var c in input.cases)
                {
                    Set(game, "Economy", c.hasEconomy ? economy : null);
                    Set(game, "clock", BitConverter.Int32BitsToSingle(c.clockBits));
                    feedback.Look = c.hasLook ? new FleetLook { stage = c.stage } : null;
                    finale.Age = BitConverter.Int32BitsToSingle(c.ageBits);
                    economy.Data.assembled = c.assembled;
                    economy.Data.parts = (int[])c.parts.Clone();
                    economy.Data.spares = (int[])c.spares.Clone();
                    float[] f = c.floatBits.Select(BitConverter.Int32BitsToSingle).ToArray();
                    object[] args;
                    switch (c.method)
                    {
                        case "AssemblyIndex": case "AssemblyEntry": case "FinaleHue": args = new object[] { c.ints[0] }; break;
                        case "UpgradeAffordable": args = Array.Empty<object>(); break;
                        case "LookLevel": args = new object[] { c.ints[0], c.ints[1] }; break;
                        case "PieceCentre": args = new object[] { c.ints[0], f[0], f[1], f[2], f[3] }; break;
                        case "BannerPoint": args = new object[] { f[0], f[1], f[2], f[3], c.ints[0] }; break;
                        case "FinaleJitter": args = new object[] { finale }; break;
                        case "FanDir": case "Frac": args = new object[] { f[0] }; break;
                        case "FinaleFade": args = new object[] { f[0], f[1] }; break;
                        case "StageGain": args = null; break;
                        default: throw new InvalidDataException(c.method);
                    }
                    object actual = c.method == "StageGain" ? typeof(PixelWingGame).GetProperty(c.method, Flags).GetValue(game)
                        : typeof(PixelWingGame).GetMethod(c.method, Flags).Invoke(game, args);
                    int[] values;
                    if (actual is Vector2 v) values = new[] { BitConverter.SingleToInt32Bits(v.x), BitConverter.SingleToInt32Bits(v.y) };
                    else if (actual is Color color) values = new[] { BitConverter.SingleToInt32Bits(color.r), BitConverter.SingleToInt32Bits(color.g), BitConverter.SingleToInt32Bits(color.b), BitConverter.SingleToInt32Bits(color.a) };
                    else values = new[] { actual is float n ? BitConverter.SingleToInt32Bits(n) : Convert.ToInt32(actual) };
                    if (values.Length != c.expected.Length) throw new InvalidDataException("Result shape: " + c.name);
                    for (int i = 0; i < values.Length; i++)
                    {
                        report.values++;
                        if (values[i] != c.expected[i]) report.failures.Add(new Failure { name = c.name, component = i, expected = c.expected[i], actual = values[i] });
                    }
                    report.cases++;
                    if (!report.functions.Contains(c.index)) report.functions.Add(c.index);
                }
                report.unexpectedSaves = saves - initialSaves;
                report.passed = report.failures.Count == 0 && report.unexpectedSaves == 0;
            }
            catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            finally
            {
                battle?.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("Geometry helper comparison failed: " + output);
        }
    }
}
#endif
