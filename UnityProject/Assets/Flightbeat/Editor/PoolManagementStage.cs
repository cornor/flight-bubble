#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // CLI emits independently observed results. Source answers never enter Unity.
    public static class PoolManagementStage
    {
        [Serializable] private sealed class Op { public string method, name, type; public int count; public int[] ints; }
        [Serializable] private sealed class Case { public string name; public bool strict; public Op[] operations; }
        [Serializable] private sealed class Input { public Case[] cases; }
        [Serializable] private sealed class Info { public string type; public int[] values; }
        [Serializable] private sealed class Collection { public string type; public int unused; public int[] values, queue; }
        [Serializable] private sealed class Frozen { public string name; public Info[] infos; }
        [Serializable] private sealed class Fields { public int[] floatBits, ints; public bool rocket, splashNull, extraNull; public int[] splash, extra; }
        [Serializable] private sealed class Held { public string name; public int id; public Fields fields; }
        [Serializable] private sealed class State { public bool strict; public int count; public Collection[] collections, retained; public Info[] infos; public Frozen[] frozen; public Held[] held; }
        [Serializable] private sealed class Error { public bool occurred; public string type = "", message = ""; public int hresult; }
        [Serializable] private sealed class Step { public Error error; public bool hasReturned; public Info returned; public State state; }
        [Serializable] private sealed class Result { public string name; public State initial; public List<Step> steps = new List<Step>(); }
        [Serializable] private sealed class Report { public string error; public bool completed, poolCleared; public int exceptionChecks; public List<Result> cases = new List<Result>(); }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly Type Pool = typeof(MemoryPool);
        private static string Arg(string name) { var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); if (i < 0) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
        private static object Get(object owner, string name) => owner.GetType().GetProperty(name, Flags).GetValue(owner);
        private static object GetCollection() => Pool.GetMethod("GetMemoryCollection", Flags).Invoke(null, new object[] { typeof(Shot) });
        private static Type PoolType(string name) => name == "shot" ? typeof(Shot) : name == "object" ? typeof(object) : name == "null" ? null : throw new InvalidDataException(name);
        private static string TypeName(Type type) => type == typeof(Shot) ? "shot" : type == typeof(object) ? "object" : type == null ? "null" : throw new InvalidDataException(type.FullName);
        private static Info SnapshotInfo(MemoryPoolInfo v) => new Info { type = TypeName(v.Type), values = new[] { v.UnusedMemoryCount, v.UsingMemoryCount, v.AcquireMemoryCount, v.ReleaseMemoryCount, v.AddMemoryCount, v.RemoveMemoryCount } };
        private static Fields SnapshotShot(Shot v) => new Fields {
            floatBits = new[] { v.X, v.Y, v.TargetX, v.TargetY, v.Age }.Select(BitConverter.SingleToInt32Bits).ToArray(),
            ints = new[] { v.Color, v.FighterId, v.TargetIndex, v.Tier, v.Barrel, v.Pierce, v.PierceA, v.PierceB, v.PierceC },
            rocket = v.Rocket, splashNull = v.SplashTargets == null, extraNull = v.Extra == null,
            splash = v.SplashTargets == null ? Array.Empty<int>() : v.SplashTargets.ToArray(), extra = v.Extra == null ? Array.Empty<int>() : v.Extra.ToArray()
        };
        private static void CheckExceptions(Report report)
        {
            Action<bool> check = value => { report.exceptionChecks++; if (!value) throw new Exception("GameFrameworkException base delegation check " + report.exceptionChecks); };
            Type type = typeof(TEngine.GameFrameworkException);
            check(type.IsSerializable && type.BaseType == typeof(Exception));
            var empty = new TEngine.GameFrameworkException();
            check(empty.InnerException == null && empty.HResult == new Exception().HResult);
            check(empty.Message == "Exception of type 'TEngine.GameFrameworkException' was thrown.");
            foreach (string message in new[] { null, "", "原版异常 / declared-message" })
            {
                var direct = new TEngine.GameFrameworkException(message);
                var inner = new InvalidOperationException("declared inner");
                var wrapped = new TEngine.GameFrameworkException(message, inner);
                check(direct.Message == (message ?? empty.Message));
                check(wrapped.Message == direct.Message && ReferenceEquals(wrapped.InnerException, inner));
                var info = new SerializationInfo(type, new FormatterConverter());
                wrapped.GetObjectData(info, new StreamingContext(StreamingContextStates.All));
                var ctor = type.GetConstructor(Flags, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                check(ctor != null && ctor.IsFamily);
                var restored = (TEngine.GameFrameworkException)ctor.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
                check(restored.Message == wrapped.Message && ReferenceEquals(restored.InnerException, inner) && restored.HResult == wrapped.HResult);
            }
            foreach (var ctor in type.GetConstructors(Flags))
            {
                byte[] il = ctor.GetMethodBody().GetILAsByteArray();
                // Constructors consist only of argument loads, nops, one base
                // constructor call and return. Match its exact parameter types.
                var signature = ctor.GetParameters().Select(p => p.ParameterType).ToArray(); int calls = 0;
                for (int i = 0; i < il.Length; i++)
                {
                    byte b = il[i];
                    if (b == 0x28)
                    {
                        var target = ctor.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1)); i += 4; calls++;
                        check(target.DeclaringType == typeof(Exception) && target.IsConstructor && target.GetParameters().Select(p => p.ParameterType).SequenceEqual(signature));
                    }
                    else check(b == 0 || (b >= 2 && b <= 5) || b == 0x2a);
                }
                check(calls == 1);
            }
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-pool-report"); if (File.Exists(output)) throw new IOException("Fresh output required");
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-pool-input")));
            var report = new Report(); bool initialStrict = MemoryPool.EnableStrictCheck;
            if (MemoryPool.Count != 0) throw new InvalidOperationException("Fresh CLI pool required; do not discard live state");
            try
            {
                foreach (var row in input.cases)
                {
                    MemoryPool.ClearAll(); MemoryPool.EnableStrictCheck = row.strict;
                    var ids = new Dictionary<object, int>(); var held = new SortedDictionary<string, Shot>(StringComparer.Ordinal);
                    var retained = new List<object>(); var frozen = new List<KeyValuePair<string, MemoryPoolInfo[]>>();
                    Func<object, int> id = value => { if (!ids.TryGetValue(value, out int result)) { result = ids.Count; ids.Add(value, result); } return result; };
                    Func<object, Collection> collection = value => new Collection {
                        type = TypeName((Type)Get(value, "MemoryType")), unused = (int)Get(value, "UnusedMemoryCount"),
                        values = new[] { "UsingMemoryCount", "AcquireMemoryCount", "ReleaseMemoryCount", "AddMemoryCount", "RemoveMemoryCount" }.Select(n => (int)Get(value, n)).ToArray(),
                        queue = ((IEnumerable)value.GetType().GetField("_memories", Flags).GetValue(value)).Cast<object>().Select(id).ToArray()
                    };
                    Func<State> snapshot = () => {
                        var current = ((IEnumerable)Pool.GetField("_memoryCollectionList", Flags).GetValue(null)).Cast<object>().ToArray();
                        foreach (object value in current) if (!retained.Contains(value)) retained.Add(value);
                        return new State {
                            strict = MemoryPool.EnableStrictCheck, count = MemoryPool.Count,
                            collections = current.Select(collection).ToArray(), retained = retained.Select(collection).ToArray(),
                            infos = MemoryPool.GetAllMemoryPoolInfos().Select(SnapshotInfo).ToArray(),
                            frozen = frozen.Select(v => new Frozen { name = v.Key, infos = v.Value.Select(SnapshotInfo).ToArray() }).ToArray(),
                            held = held.Select(v => new Held { name = v.Key, id = id(v.Value), fields = SnapshotShot(v.Value) }).ToArray()
                        };
                    };
                    var result = new Result { name = row.name, initial = snapshot() }; report.cases.Add(result);
                    foreach (var op in row.operations)
                    {
                        var step = new Step { error = new Error(), returned = new Info { type = "", values = Array.Empty<int>() } };
                        try
                        {
                            switch (op.method)
                            {
                                case "AddType": MemoryPool.Add(PoolType(op.type), op.count); break;
                                case "RemoveType": MemoryPool.Remove(PoolType(op.type), op.count); break;
                                case "RemoveAllType": MemoryPool.RemoveAll(PoolType(op.type)); break;
                                case "AddGeneric": MemoryPool.Add<Shot>(op.count); break;
                                case "RemoveGeneric": MemoryPool.Remove<Shot>(op.count); break;
                                case "RemoveAllGeneric": MemoryPool.RemoveAll<Shot>(); break;
                                case "Acquire": held[op.name] = MemoryPool.Acquire<Shot>(); id(held[op.name]); break;
                                case "Release": MemoryPool.Release(held[op.name]); break;
                                case "ClearAll": MemoryPool.ClearAll(); break;
                                case "CaptureInfo": frozen.Add(new KeyValuePair<string, MemoryPoolInfo[]>(op.name, MemoryPool.GetAllMemoryPoolInfos())); break;
                                case "WriteShot": held[op.name].X = op.ints[0]; held[op.name].Tier = op.ints[1]; break;
                                case "SeedCounters":
                                    var c = GetCollection(); var names = new[] { "_usingMemoryCount", "_acquireMemoryCount", "_releaseMemoryCount", "_addMemoryCount", "_removeMemoryCount" };
                                    for (int i = 0; i < names.Length; i++) c.GetType().GetField(names[i], Flags).SetValue(c, op.ints[i]); break;
                                case "CollectionAddOther":
                                    var other = GetCollection(); other.GetType().GetMethods(Flags).Single(m => m.Name == "Add" && m.IsGenericMethod).MakeGenericMethod(typeof(object)).Invoke(other, new object[] { op.count }); break;
                                case "InfoCtor":
                                    step.hasReturned = true; step.returned = SnapshotInfo(new MemoryPoolInfo(PoolType(op.type), op.ints[0], op.ints[1], op.ints[2], op.ints[3], op.ints[4], op.ints[5])); break;
                                default: throw new InvalidDataException(op.method);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is TargetInvocationException target && target.InnerException != null) ex = target.InnerException;
                            step.error = new Error { occurred = true, type = ex.GetType().FullName, message = ex.Message, hresult = ex.HResult };
                        }
                        step.state = snapshot(); result.steps.Add(step);
                    }
                }
                CheckExceptions(report); report.completed = true;
            }
            catch (Exception ex) { report.error = ex.ToString(); }
            finally
            {
                MemoryPool.ClearAll(); MemoryPool.EnableStrictCheck = initialStrict;
                report.poolCleared = MemoryPool.Count == 0 && MemoryPool.EnableStrictCheck == initialStrict;
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(report));
            }
            if (!report.completed || !report.poolCleared) throw new Exception(report.error ?? "Pool stage incomplete");
        }
    }
}
#endif
