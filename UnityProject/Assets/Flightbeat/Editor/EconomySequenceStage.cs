using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Original f53956 JSON transactions remain the production implementation.
    // Only the constructor's external clock/save/random delegates are declared.
    public static class EconomySequenceStage
    {
        [Serializable] private sealed class Arg { public string kind, text; public int number; public int[] values; public bool isNull; }
        [Serializable] private sealed class ActionInput
        {
            public string kind, name, returnType, text; public int function;
            public Arg[] args; public bool throwSave, throwClock;
        }
        [Serializable] private sealed class Leaf { public string path, kind, text; }
        [Serializable] private sealed class EventInput
        {
            public string kind, text, encoded, currentEncoded; public int bound, value;
            public bool candidateIsCurrent, inspectCurrent; public Leaf[] profile;
        }
        [Serializable] private sealed class StateInput
        {
            public string encoded, retiredNames, error; public int retiredRefund, techRefund, sortedRevision, randomCursor;
            public bool sortedPresent; public Leaf[] profile, returned; public EventInput[] events;
        }
        [Serializable] private sealed class Case
        {
            public string name, profileJson, utc; public ActionInput[] actions; public StateInput[] states;
        }
        [Serializable] private sealed class Input { public Case[] cases; public uint[] randomWords; }
        [Serializable] private sealed class ActualEvent
        {
            public string kind, text, encoded, currentEncoded; public int bound, value;
            public bool candidateIsCurrent, inspectCurrent; public FleetProfile profile;
        }
        [Serializable] private sealed class ActualState
        {
            public string name, action, encoded, retiredNames, error;
            public int ordinal, retiredRefund, techRefund, profileTechRefund, randomCursor, sortedRevision;
            public bool sortedPresent; public List<ActualEvent> events;
        }
        [Serializable] private sealed class Failure { public string path, actual, expected; }
        [Serializable] private sealed class Report
        {
            public bool passed; public string error, unityVersion;
            public int cases, actions, states, comparisons, failures, randomCalls, clockCalls, saveCallbacks, declaredFailures;
            public List<Failure> firstFailures = new List<Failure>();
            public List<ActualState> actual = new List<ActualState>();
        }
        private static Report report;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static string Text(object x) => x == null ? "null" : Convert.ToString(x, CultureInfo.InvariantCulture);
        private static void Equal(string path, object actual, object expected)
        {
            report.comparisons++;
            if (Equals(actual, expected)) return;
            report.failures++;
            if (report.firstFailures.Count < 50) report.firstFailures.Add(new Failure { path = path, actual = Text(actual), expected = Text(expected) });
        }
        private static object Member(object obj, string name)
        {
            if (obj == null) throw new NullReferenceException(name);
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, Flags) ?? type.GetField("<" + name + ">k__BackingField", Flags);
            if (field != null) return field.GetValue(obj);
            PropertyInfo property = type.GetProperty(name, Flags);
            if (property != null) return property.GetValue(obj);
            throw new MissingFieldException(type.FullName, name);
        }
        private static void Leaves(string label, object actual, Leaf[] expected)
        {
            foreach (Leaf leaf in expected)
            {
                object value = actual;
                try
                {
                    foreach (string part in leaf.path.Split('/').Skip(1))
                        value = value is IList list ? list[Int32.Parse(part, CultureInfo.InvariantCulture)] : Member(value, part);
                    string text;
                    switch (leaf.kind)
                    {
                        case "null": Equal(label + leaf.path, value == null, true); continue;
                        case "count": text = ((IList)value).Count.ToString(CultureInfo.InvariantCulture); break;
                        case "float": text = BitConverter.SingleToInt32Bits((float)value).ToString(CultureInfo.InvariantCulture); break;
                        case "bool": text = (bool)value ? "true" : "false"; break;
                        case "integer": text = Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture); break;
                        case "string": text = (string)value; break;
                        default: throw new InvalidDataException(leaf.kind);
                    }
                    Equal(label + leaf.path, text, leaf.text);
                }
                catch (Exception e) { Equal(label + leaf.path, e.GetType().Name, leaf.kind + ":" + leaf.text); }
            }
        }
        private static object[] Arguments(ActionInput action) => action.args.Select(a => a.kind == "string" ? (object)a.text
            : a.kind == "bool" ? (object)(a.number != 0) : a.kind == "int[]" ? (a.isNull ? null : (object)a.values) : a.number).ToArray();
        private static object Invoke(FleetEconomy economy, ActionInput action)
        {
            object[] args = Arguments(action);
            MethodInfo method = typeof(FleetEconomy).GetMethods(Flags).SingleOrDefault(m => m.Name == action.name && m.GetParameters().Length == args.Length
                && m.GetParameters().Select((p, i) => args[i] == null ? !p.ParameterType.IsValueType : p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
            if (method != null) return method.Invoke(economy, args);
            // T9.3 relocated developer operations; source action arguments and
            // the actual economy instance are unchanged by the assembly boundary.
            object[] extensionArgs = new object[] { economy }.Concat(args).ToArray();
            method = typeof(FleetDeveloper).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == action.name
                && m.GetParameters().Length == extensionArgs.Length
                && m.GetParameters().Select((p, i) => extensionArgs[i] == null ? !p.ParameterType.IsValueType : p.ParameterType.IsInstanceOfType(extensionArgs[i])).All(x => x));
            return method.Invoke(null, extensionArgs);
        }
        private static void RunCase(Input input, Case c)
        {
            long utc = Int64.Parse(c.utc, CultureInfo.InvariantCulture); int randomCursor = 0;
            bool constructing = true; ActionInput action = null; FleetEconomy economy = null;
            var events = new List<ActualEvent>();
            economy = new FleetEconomy(JsonUtility.FromJson<FleetProfile>(c.profileJson), () =>
            {
                events.Add(new ActualEvent { kind = "clock", text = utc.ToString(CultureInfo.InvariantCulture) }); report.clockCalls++;
                if (action != null && action.throwClock) throw new IOException("declared-clock-failure");
                return utc;
            }, next =>
            {
                var e = new ActualEvent { kind = "save", encoded = FleetSaveStore.Encode(next), profile = next };
                if (!constructing)
                {
                    e.inspectCurrent = true; e.currentEncoded = FleetSaveStore.Encode(economy.Data);
                    e.candidateIsCurrent = ReferenceEquals(next, economy.Data);
                }
                events.Add(e); report.saveCallbacks++;
                if (action != null && action.throwSave) throw new IOException("declared-save-failure");
            }, bound =>
            {
                if (bound <= 0 || randomCursor >= input.randomWords.Length) throw new InvalidOperationException("RNG input exhausted/invalid");
                int value = (int)(input.randomWords[randomCursor++] % (uint)bound);
                events.Add(new ActualEvent { kind = "random", bound = bound, value = value }); report.randomCalls++;
                return value;
            });
            void State(int ordinal, object returned, string error)
            {
                StateInput expected = c.states[ordinal]; string label = c.name + "/" + ordinal + "/" + (action?.name ?? "constructor");
                Equal(label + "/error", error ?? "", expected.error ?? "");
                Leaves(label + "/profile", economy.Data, expected.profile);
                Leaves(label + "/return", returned, expected.returned);
                var actual = new ActualState { name = c.name, ordinal = ordinal, action = action?.name ?? "constructor", error = error,
                    encoded = FleetSaveStore.Encode(economy.Data), retiredRefund = economy.RetiredRefund, retiredNames = economy.RetiredNames,
                    techRefund = economy.TechRefund, profileTechRefund = economy.Data.techRefund, randomCursor = randomCursor,
                    sortedRevision = (int)Member(economy, "sortedRevision"), sortedPresent = Member(economy, "sortedCores") != null, events = events };
                Equal(label + "/encoded", actual.encoded, expected.encoded); Equal(label + "/retiredRefund", actual.retiredRefund, expected.retiredRefund);
                Equal(label + "/retiredNames", actual.retiredNames, expected.retiredNames); Equal(label + "/techRefund", actual.techRefund, expected.techRefund);
                Equal(label + "/sortedRevision", actual.sortedRevision, expected.sortedRevision); Equal(label + "/sortedPresent", actual.sortedPresent, expected.sortedPresent);
                Equal(label + "/randomCursor", actual.randomCursor, expected.randomCursor); Equal(label + "/eventCount", events.Count, expected.events.Length);
                for (int i = 0; i < Math.Min(events.Count, expected.events.Length); i++)
                {
                    ActualEvent a = events[i]; EventInput e = expected.events[i]; string prefix = label + "/event" + i;
                    Equal(prefix + "/kind", a.kind, e.kind);
                    if (e.kind == "clock") Equal(prefix + "/clock", a.text, e.text);
                    else if (e.kind == "random") { Equal(prefix + "/bound", a.bound, e.bound); Equal(prefix + "/value", a.value, e.value); }
                    else
                    {
                        Equal(prefix + "/encoded", a.encoded, e.encoded); Leaves(prefix + "/profile", a.profile, e.profile);
                        Equal(prefix + "/inspectCurrent", a.inspectCurrent, e.inspectCurrent);
                        if (e.inspectCurrent) { Equal(prefix + "/oldDataEncoding", a.currentEncoded, e.currentEncoded); Equal(prefix + "/publishedBeforeSave", a.candidateIsCurrent, e.candidateIsCurrent); }
                    }
                }
                report.actual.Add(actual); report.states++;
            }
            State(0, null, null); constructing = false;
            for (int i = 0; i < c.actions.Length; i++)
            {
                action = c.actions[i]; events = new List<ActualEvent>(); object returned = null; string error = null;
                if (action.kind == "utc") utc = Int64.Parse(action.text, CultureInfo.InvariantCulture);
                else
                {
                    try { returned = Invoke(economy, action); }
                    catch (TargetInvocationException e)
                    {
                        Exception cause = e.InnerException;
                        if (action.throwClock && cause.Message == "declared-clock-failure") error = "clock";
                        else if (action.throwSave && cause.Message == "declared-save-failure") error = "save";
                        else throw;
                        report.declaredFailures++;
                    }
                }
                State(i + 1, returned, error); report.actions++;
            }
            report.cases++;
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 == args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
            string output = Arg("--flightbeat-economy-sequences-report");
            if (File.Exists(output)) throw new IOException("Fresh output required"); Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion };
            try
            {
                Input input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-economy-sequences-input")));
                foreach (Case c in input.cases) RunCase(input, c);
                Equal("cases", report.cases, input.cases.Length); report.passed = report.failures == 0;
            }
            catch (Exception e) { report.error = e.ToString(); }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
            if (!report.passed) throw new Exception("Economic sequence mismatch: " + output);
        }
    }
}
