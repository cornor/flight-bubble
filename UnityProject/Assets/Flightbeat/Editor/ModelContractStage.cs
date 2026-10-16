using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // One grouped source-contract check. Expectations come from the extracted
    // WAT runner, not duplicate candidate formulas. Does not load player saves.
    public static class ModelContractStage
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        [Serializable] private sealed class Patch { public string name, kind; public int index, bits; }
        [Serializable] private sealed class Case
        {
            public string id, owner, member, kind;
            public int function, expected;
            public string[] argTypes;
            public int[] args;
            public bool staticCall;
            public Patch[] patches;
        }
        [Serializable] private sealed class Accessor { public string method, source; public int fIndex; }
        [Serializable] private sealed class Accessors { public Accessor[] properties, events; }
        [Serializable] private sealed class Input { public Patch[] @base; public Case[] cases; public Accessors accessors; public int[] sortOrder; }
        [Serializable] private sealed class Result { public string id; public int function, expected, actual; public bool passed; }
        [Serializable] private sealed class Report
        {
            public bool passed, wholeGameEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, queryCases, properties, events;
            public int[] retainedCells;
            public List<Result> results = new List<Result>();
        }
        private static Report report;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required");
            string output = Arg("--flightbeat-model-contract-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-model-contract-input")));
            report = new Report { unityVersion = Application.unityVersion };
            try
            {
                Queries(input); Properties(input.accessors); Events(input.accessors); Constructors(); TiedFinale(input.sortOrder);
                Check(report.results.All(r => r.passed), "Query bit mismatch; see result rows");
                report.passed = true;
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool condition, string reason) { report.assertions++; if (!condition) throw new Exception(reason); }
        private static Battle Fresh() => new Battle(new LevelData
        {
            columns = 2, rows = 2, cells = new[] { 0, 0, 0, 0 }, shades = new[] { 7, 8 },
            stream = new[] { 0, 0 }, streamOffsets = new[] { 0, 1, 2 }, capacity = 20,
            initialSlots = 3, slotLimit = 7, laneCount = 4, seed = 168,
            gridX = 17, gridY = 50, pitchX = 13, pitchY = 11
        }, new PressureSettings(), false);
        private static object Get(object target, string name)
        {
            Type t = target.GetType(); var field = t.GetField(name, Flags);
            return field != null ? field.GetValue(target) : t.GetProperty(name, Flags).GetValue(target);
        }
        private static object ConvertValue(Type type, int bits)
        {
            if (type == typeof(float)) return BitConverter.Int32BitsToSingle(bits);
            if (type == typeof(bool)) return bits != 0;
            if (type.IsEnum) return Enum.ToObject(type, bits);
            return bits;
        }
        private static void Apply(Battle battle, Finale finale, Patch patch)
        {
            if (patch.name == "shotCount")
            { battle.Shots.Clear(); for (int i = 0; i < patch.bits; i++) battle.Shots.Add(new Shot()); return; }
            string[] names = patch.name.Split('.'); object target = battle;
            int start = 0;
            if (names[0] == "Finale") { target = finale; start = 1; }
            for (int i = start; i < names.Length - 1; i++) target = Get(target, names[i]);
            string name = names[names.Length - 1];
            if (patch.index >= 0)
            { var array = (Array)Get(target, name); array.SetValue(ConvertValue(array.GetType().GetElementType(), patch.bits), patch.index); return; }
            Type t = target.GetType(); var field = t.GetField(name, Flags);
            if (field != null) field.SetValue(target, ConvertValue(field.FieldType, patch.bits));
            else { var p = t.GetProperty(name, Flags); p.SetValue(target, ConvertValue(p.PropertyType, patch.bits)); }
        }
        private static void Queries(Input input)
        {
            foreach (Case c in input.cases)
                using (Battle battle = Fresh())
                {
                    var finale = new Finale();
                    foreach (Patch p in input.@base) Apply(battle, finale, p);
                    foreach (Patch p in c.patches) Apply(battle, finale, p);
                    Type t = c.owner == "Battle" ? typeof(Battle) : c.owner == "Finale" ? typeof(Finale) : typeof(BroadcastMode);
                    object target = c.staticCall ? null : c.owner == "Battle" ? (object)battle : finale;
                    var property = t.GetProperty(c.member, Flags); object value;
                    if (property != null) value = property.GetValue(target);
                    else
                    {
                        object[] args = new object[c.args.Length];
                        for (int i = 0; i < args.Length; i++) args[i] = ConvertValue(c.argTypes[i] == "float" ? typeof(float) : typeof(int), c.args[i]);
                        value = t.GetMethod(c.member, Flags).Invoke(target, args);
                    }
                    if (c.kind == "held") value = Get(battle, "advanceHeldUntil");
                    int actual = c.kind == "float" || c.kind == "held" ? BitConverter.SingleToInt32Bits((float)value)
                        : c.kind == "bool" ? ((bool)value ? 1 : 0) : (int)value;
                    report.results.Add(new Result { id = c.id, function = c.function, expected = c.expected, actual = actual, passed = actual == c.expected });
                    report.queryCases++;
                    // These are count-only dummy shots, not acquired pool entries.
                    battle.Shots.Clear();
                }
        }
        private static void Properties(Accessors accessors)
        {
            using (Battle battle = Fresh())
                foreach (string name in accessors.properties.Select(a => a.method.Substring(4)).Distinct())
                {
                    PropertyInfo p = typeof(Battle).GetProperty(name, Flags); Type t = p.PropertyType;
                    Check(p.GetGetMethod(true).IsPublic && p.GetSetMethod(true).IsPrivate, name + " accessor visibility");
                    object value = t == typeof(string) ? "source-contract" : t == typeof(float) ? (object)BitConverter.Int32BitsToSingle(unchecked((int)0x80000000))
                        : t == typeof(bool) ? true : t == typeof(int) ? Int32.MinValue : t.IsEnum ? Enum.ToObject(t, -1) : Activator.CreateInstance(t);
                    p.SetValue(battle, value); object read = p.GetValue(battle);
                    Check(t == typeof(float) ? BitConverter.SingleToInt32Bits((float)read) == BitConverter.SingleToInt32Bits((float)value)
                        : t.IsValueType ? value.Equals(read) : ReferenceEquals(value, read), name + " exact getter/setter storage");
                    if (!t.IsValueType) { p.SetValue(battle, null); Check(p.GetValue(battle) == null, name + " null storage"); }
                    report.properties++;
                }
        }
        private static void Ignore(int id) { }
        private static Delegate Handler(Type type, int id)
        {
            var parameters = type.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
            return Expression.Lambda(type, Expression.Call(typeof(ModelContractStage).GetMethod(nameof(Ignore), Flags), Expression.Constant(id)), parameters).Compile();
        }
        private static void Events(Accessors accessors)
        {
            using (Battle battle = Fresh())
                foreach (string name in accessors.events.Select(a => a.method.Substring(a.method.IndexOf('_') + 1)).Distinct())
                {
                    EventInfo e = typeof(Battle).GetEvent(name); FieldInfo field = typeof(Battle).GetField(name, Flags);
                    Delegate a = Handler(e.EventHandlerType, 1), b = Handler(e.EventHandlerType, 2);
                    // Mono's EventInfo.AddEventHandler adapter dereferences null;
                    // invoke the actual generated accessor to check source behavior.
                    e.GetAddMethod().Invoke(battle, new object[] { null }); Check(field.GetValue(battle) == null, name + " null add");
                    e.AddEventHandler(battle, a); e.AddEventHandler(battle, b); e.AddEventHandler(battle, a);
                    Check(((Delegate)field.GetValue(battle)).GetInvocationList().SequenceEqual(new[] { a, b, a }), name + " combine order and duplicate");
                    e.RemoveEventHandler(battle, a);
                    Check(((Delegate)field.GetValue(battle)).GetInvocationList().SequenceEqual(new[] { a, b }), name + " remove last matching delegate");
                    e.GetRemoveMethod().Invoke(battle, new object[] { null }); e.RemoveEventHandler(battle, a); e.RemoveEventHandler(battle, b);
                    Check(field.GetValue(battle) == null, name + " remove all"); report.events++;
                }
        }
        private static void Constructors()
        {
            var d = new BroadcastDecision(BroadcastAction.Deploy, Int32.MinValue);
            Check(d.action == BroadcastAction.Deploy && d.index == Int32.MinValue, "f3463 field mapping");
            var pilot = new BroadcastPilot(3, 137);
            Check((int)Get(pilot, "remaining") == -1 && BitConverter.SingleToInt32Bits((float)Get(pilot, "decisionAt")) == 1060320051, "f55153 initial sentinels");
            foreach (string name in new[] { "visible", "held", "count" }) Check(((int[])Get(pilot, name)).SequenceEqual(new int[3]), "f55153 zeroed independent array");
            Check(!ReferenceEquals(Get(pilot, "visible"), Get(pilot, "held")) && !ReferenceEquals(Get(pilot, "held"), Get(pilot, "count")), "f55153 separate allocations");
            Check(((System.Random)Get(pilot, "random")).NextDouble() == new System.Random(137).NextDouble(), "f55153 seed forwarded");
        }
        private static void TiedFinale(int[] originalOrder)
        {
            var level = new LevelData { columns = 32, rows = 1, cells = new int[32], gridY = 100, pitchY = 9,
                capacity = 64, initialSlots = 3, laneCount = 4, stream = new int[0],
                queue = new[] { new QueueEntry { lane = 0, color = 0, ammo = 32 } } };
            using (var battle = new Battle(level, PressureSettings.Legacy, false))
            {
                Check(battle.StageFinaleForEvidence(17) == 17, "f55097 retain boundary count");
                report.retainedCells = Enumerable.Range(0, 32).Where(i => battle.Alive[i]).ToArray();
                Check(report.retainedCells.SequenceEqual(originalOrder.Take(17).OrderBy(i => i)), "f55097 tied-Y survival matches original captured sort");
                Check(battle.Score == 0 && battle.Reserve.All(q => q.Count == 0), "f55097 no removal score and queues cleared");
            }
        }
    }
}
