#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class InternalContractsStage
    {
        [Serializable] private sealed class Event { public string kind; public int value; }
        [Serializable] private sealed class Case
        {
            public string name, method, text;
            public int[] ints, floatBits, values;
            public bool error;
            public Event[] calls;
        }
        [Serializable] private sealed class Input { public string profileEncoded; public int[] parts; public Case[] cases; }
        [Serializable] private sealed class Result { public string name, error; public int checks; public List<string> failures = new List<string>(); }
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string error;
            public int unexpectedSaves;
            public List<Result> cases = new List<Result>();
            public List<string> predicates = new List<string>(), singletonTypes = new List<string>(), emptyMethods = new List<string>(), generatedMethods = new List<string>(), defaultCarriers = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static void Set(object obj, string name, object value)
        {
            var t = obj.GetType(); var f = t.GetField(name, Flags) ?? t.GetField("<" + name + ">k__BackingField", Flags);
            if (f != null) f.SetValue(obj, value); else (t.GetProperty(name, Flags) ?? throw new MissingMemberException(name)).SetValue(obj, value);
        }
        private static IEnumerable<Type> Types(Type t) { yield return t; foreach (var n in t.GetNestedTypes(Flags)) foreach (var v in Types(n)) yield return v; }
        private static string Arg(string name) { var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); if (i < 0) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
        private static void Check(Result r, string name, object a, object b) { r.checks++; if (!Equals(a, b)) r.failures.Add(name + ": " + a + " != " + b); }
        private static object Capture(Type t, FleetEconomy economy, PixelWingGame game, Case c, int depth = 0)
        {
            if (depth > 8) throw new InvalidOperationException("Unexpected capture cycle");
            object obj = Activator.CreateInstance(t, true); float[] v = Array.ConvertAll(c.floatBits, BitConverter.Int32BitsToSingle);
            foreach (var f in t.GetFields(Flags).Where(x => !x.IsStatic))
            {
                if (f.Name == "id") f.SetValue(obj, c.ints[0]);
                else if (f.Name == "fighter") f.SetValue(obj, c.ints[0]);
                else if (f.Name == "origin") f.SetValue(obj, new Vector2(v[0], v[1]));
                else if (f.Name == "scale") f.SetValue(obj, v[2]);
                else if (f.Name == "angle") f.SetValue(obj, v[5]);
                else if (f.FieldType == typeof(FleetEconomy)) f.SetValue(obj, economy);
                else if (f.FieldType == typeof(PixelWingGame)) f.SetValue(obj, game);
                else if (f.FieldType.Name.Contains("DisplayClass")) f.SetValue(obj, Capture(f.FieldType, economy, game, c, depth + 1));
            }
            return obj;
        }
        private static object InvokeLocal(MethodInfo m, object owner, FleetEconomy economy, PixelWingGame game, Case c, params object[] values)
        {
            int cursor = 0;
            var args = m.GetParameters().Select(p => p.ParameterType.IsByRef
                ? Capture(p.ParameterType.GetElementType(), economy, game, c)
                : p.ParameterType.Name.Contains("DisplayClass") ? Capture(p.ParameterType, economy, game, c) : values[cursor++]).ToArray();
            object target = m.IsStatic ? null : m.DeclaringType.IsInstanceOfType(owner) ? owner : Capture(m.DeclaringType, economy, game, c);
            return m.Invoke(target, args);
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-internal-report"); if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-internal-input")));
            var report = new Report(); var owner = new GameObject("Source internal contracts"); owner.SetActive(false); Battle battle = null;
            try
            {
                var game = owner.AddComponent<PixelWingGame>();
                battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/training01").text), null, true);
                var economy = new FleetEconomy(FleetSaveStore.Decode(input.profileEncoded), () => 1800000000L, _ => report.unexpectedSaves++, _ => throw new Exception("Undeclared RNG"));
                report.unexpectedSaves = 0; economy.Data.parts = (int[])input.parts.Clone();
                var lesson = new FlightLesson(battle);
                var all = Types(typeof(PixelWingGame)).Concat(Types(typeof(FleetEconomy))).SelectMany(t => t.GetMethods(Flags | BindingFlags.DeclaredOnly)).ToArray();
                var point = all.Single(m => m.Name.StartsWith("<DrawTapPointer>g__Point|"));
                var part = all.Single(m => m.Name.StartsWith("<Bonus>g__Part|"));
                var predicateMethods = all.Where(m => new[] { "InPreset", "Equip", "Repair", "Recycle" }.Any(n => m.Name.StartsWith("<" + n + ">b__"))
                    && m.ReturnType == typeof(bool) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(FleetCore)).ToArray();
                if (predicateMethods.Length != 6) throw new Exception("Expected six id predicate carriers, got " + predicateMethods.Length);
                report.predicates.AddRange(predicateMethods.Select(m => m.DeclaringType.FullName + "." + m.Name));
                report.generatedMethods.AddRange(all.Where(m => m.Name.Contains(">b__") || m.Name.Contains(">g__")).Select(m => m.DeclaringType.FullName + "." + m.Name));
                foreach (var c in input.cases)
                {
                    var r = new Result { name = c.name }; report.cases.Add(r);
                    try
                    {
                        int[] actual = Array.Empty<int>(); float[] v = Array.ConvertAll(c.floatBits, BitConverter.Int32BitsToSingle); var a = c.ints;
                        var calls = new List<Event>(); bool error = false;
                        Action<string, int, bool> emit = (kind, value, throwing) => { calls.Add(new Event { kind = kind, value = value }); if (throwing) throw new InvalidOperationException("declared-callback-error"); };
                        switch (c.method)
                        {
                            case "TutorialButton":
                                Set(game, "lesson", a[0] != 0 ? lesson : null); Set(lesson, "Chapter", a[1]); Set(lesson, "Step", a[2]); Set(lesson, "Age", v[0]); Set(lesson, "HelpUsed", false);
                                typeof(PixelWingGame).GetMethod("TutorialButton", Flags).Invoke(game, null);
                                actual = new[] { lesson.Chapter, lesson.Step, BitConverter.SingleToInt32Bits(lesson.Age), lesson.HelpUsed ? 1 : 0 }; break;
                            case "BonusPart": actual = new[] { (int)InvokeLocal(part, economy, economy, game, c, a[1]) }; break;
                            case "TapPoint": var p = (Vector2)InvokeLocal(point, game, economy, game, c, v[3], v[4]); actual = new[] { BitConverter.SingleToInt32Bits(p.x), BitConverter.SingleToInt32Bits(p.y) }; break;
                            case "IdPredicate":
                                var predicateValues = new List<int>();
                                foreach (var m in predicateMethods)
                                {
                                    var carrier = Capture(m.DeclaringType, economy, game, c);
                                    int value = (bool)m.Invoke(carrier, new object[] { new FleetCore { id = a[1] } }) ? 1 : 0;
                                    predicateValues.Add(value); Check(r, m.Name, value, c.values[0]);
                                }
                                actual = new[] { predicateValues[0] }; break;
                            case "AdCompletion":
                                var ct = typeof(AdService).GetNestedType("CompletionCallbacks", Flags); var closure = Activator.CreateInstance(ct, true);
                                Set(closure, "reward", a[1] == 0 ? null : (Action)(() => emit("reward", 0, a[3] != 0)));
                                Set(closure, "failed", a[2] == 0 ? null : (Action)(() => emit("failed", 0, a[3] != 0)));
                                try { ct.GetMethod("Complete", Flags).Invoke(closure, new object[] { a[0] != 0 }); }
                                catch (TargetInvocationException ex) when (ex.InnerException?.Message == "declared-callback-error") { error = true; }
                                break;
                            case "AdDefault":
                                var dt = typeof(AdService).GetNestedType("DefaultProvider", Flags); var provider = dt.GetField("Instance", Flags).GetValue(null);
                                try { dt.GetMethod("Complete", Flags).Invoke(provider, new object[] { (Action<bool>)(success => emit("done", success ? 1 : 0, a[0] != 0)) }); }
                                catch (TargetInvocationException ex) when (ex.InnerException?.Message == "declared-callback-error") { error = true; }
                                break;
                            case "Broadcast":
                                // Native diagnostic storage is checked here. Original DOM and
                                // exception semantics are compared separately against the jslib.
                                BroadcastHost.Publish(c.text); Check(r, "nativeStatusJson", BroadcastHost.StatusJson, c.text); continue;
                            default: throw new InvalidDataException(c.method);
                        }
                        Check(r, "return-length", actual.Length, c.values.Length);
                        for (int i = 0; i < actual.Length; i++) Check(r, "return-" + i, actual[i], c.values[i]);
                        Check(r, "callback-error", error, c.error); Check(r, "callback-count", calls.Count, c.calls.Length);
                        for (int i = 0; i < Math.Min(calls.Count, c.calls.Length); i++) { Check(r, "callback-kind", calls[i].kind, c.calls[i].kind); Check(r, "callback-value", calls[i].value, c.calls[i].value); }
                    }
                    catch (Exception ex) { r.error = ex.ToString(); }
                }
                foreach (var t in new[] { typeof(PixelWingGame), typeof(FleetEconomy), typeof(RewardedAdGate), typeof(AdService) })
                {
                    Type carrier = t.GetNestedType(t == typeof(AdService) ? "DefaultProvider" : "<>c", Flags) ?? throw new Exception("Missing singleton carrier: " + t);
                    RuntimeHelpers.RunClassConstructor(carrier.TypeHandle);
                    var field = carrier.GetFields(Flags).Single(f => f.IsStatic && f.FieldType == carrier);
                    object singleton = field.GetValue(null); if (singleton == null || !ReferenceEquals(singleton, field.GetValue(null))) throw new Exception("Missing stable singleton");
                    report.singletonTypes.Add(carrier.FullName);
                }
                foreach (var m in typeof(SeegDistribution).GetMethods(Flags | BindingFlags.DeclaredOnly).Concat(new[] { typeof(PixelWingGame).GetMethod("Vibrate", Flags) }))
                {
                    byte[] il = m.GetMethodBody().GetILAsByteArray(); if (il.Any(b => b != 0 && b != 0x2a)) throw new Exception("Source nop has effects: " + m.Name);
                    m.Invoke(m.IsStatic ? null : game, m.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType)).ToArray());
                    report.emptyMethods.Add(m.DeclaringType.FullName + "." + m.Name);
                }
                foreach (var t in new[] { typeof(PixelWingGame), typeof(FleetEconomy), typeof(AdService), typeof(RewardedAdGate) }.SelectMany(Types)
                    .Where(t => t.Name.Contains("DisplayClass") || t.Name == "<>c" || t.Name == "BroadcastStatus" || t.Name == "CompletionCallbacks" || t.Name == "DefaultProvider" || t == typeof(RewardedAdGate)))
                {
                    object carrier = Activator.CreateInstance(t, true);
                    foreach (var f in t.GetFields(Flags).Where(f => !f.IsStatic))
                    {
                        object value = f.GetValue(carrier), zero = f.FieldType.IsValueType ? Activator.CreateInstance(f.FieldType) : null;
                        if (!Equals(value, zero)) throw new Exception("Nondefault capture field in " + t.FullName + "." + f.Name);
                    }
                    report.defaultCarriers.Add(t.FullName);
                }
                var ignoredSave = all.Single(m => m.Name.StartsWith("<InitializeFleet>b__") && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(FleetProfile));
                if (ignoredSave.GetMethodBody().GetILAsByteArray().Any(b => b != 0 && b != 0x2a)) throw new Exception("Diagnostic save callback is not source nop");
                ignoredSave.Invoke(ignoredSave.IsStatic ? null : Activator.CreateInstance(ignoredSave.DeclaringType, true), new object[] { null });
                report.emptyMethods.Add(ignoredSave.DeclaringType.FullName + "." + ignoredSave.Name);
                report.passed = report.unexpectedSaves == 0 && report.cases.All(r => r.error == null && r.failures.Count == 0);
            }
            catch (Exception ex) { report.error = ex.ToString(); }
            finally { battle?.Dispose(); UnityEngine.Object.DestroyImmediate(owner); File.WriteAllText(output, JsonUtility.ToJson(report)); }
            if (!report.passed) throw new Exception(report.error ?? "Internal contract differences; inspect report");
        }
    }
}
#endif
