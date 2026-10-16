using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class ModelPrecisionReplay
    {
        [Serializable] private sealed class Check { public string path, kind, text; public int bits; public int[] values; }
        [Serializable] private sealed class ActionInput { public int queue; public bool result; }
        [Serializable] private sealed class Step { public int frame, dtBits; public ActionInput[] actions; public Check[] checks; }
        [Serializable] private sealed class Input { public string levelJson, pressureJson; public Check[] initial; public Step[] steps; }
        [Serializable] private sealed class Failure { public int frame; public string path, expected, actual; }
        [Serializable] private sealed class Report
        {
            public bool passed, arrayHashesPending = true;
            public string unityVersion, error;
            public int frames, actions, checks, failureCount, phase, remaining, timeBits;
            public double seconds;
            public List<Failure> failures = new List<Failure>();
        }
        [Serializable] private sealed class Arrays
        { public int frame; public int[] Alive, sand, x, y, flow, position, fronts; }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Report report;
        private static int frame = -1;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required");
            string output = Arg("--flightbeat-precision-report");
            if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion }; var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Input input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-precision-input")));
                var level = JsonUtility.FromJson<LevelData>(input.levelJson);
                var pressure = JsonUtility.FromJson<PressureSettings>(input.pressureJson);
                // This is a captured object fixture, not an authored JSON load.
                // JsonUtility materializes null string/array entries as empty;
                // restore the explicit nulls from the frozen INPUT before the
                // constructor. Never patch candidate state from expected frames.
                RestoreInputNulls(level, "Level/", input.initial);
                RestoreInputNulls(pressure, "Pressure/", input.initial);
                using (var battle = new Battle(level, pressure, true))
                using (var arrays = new StreamWriter(Path.Combine(Path.GetDirectoryName(output), "arrays.jsonl")))
                {
                    battle.ApplyLoadout(FleetLoadout.Empty); battle.ApplyWeapon(new WeaponProfile());
                    Compare(battle, input.initial);
                    foreach (Step step in input.steps)
                    {
                        frame = step.frame;
                        foreach (ActionInput action in step.actions)
                        { bool actual = battle.Deploy(action.queue); report.checks++; if (actual != action.result) Fail("Deploy", action.result, actual); report.actions++; }
                        battle.Tick(BitConverter.Int32BitsToSingle(step.dtBits)); Compare(battle, step.checks);
                        arrays.WriteLine(JsonUtility.ToJson(new Arrays { frame = frame,
                            Alive = Values(Get(battle,"Alive")), sand = Values(Get(battle,"sand")), x = Values(Get(battle,"x")), y = Values(Get(battle,"y")),
                            flow = Values(Get(battle,"flow")), position = Values(Get(battle,"position")), fronts = Values(Get(battle,"fronts")) }));
                        report.frames++;
                    }
                    report.phase = (int)battle.State; report.remaining = battle.Remaining; report.timeBits = BitConverter.SingleToInt32Bits(battle.Time);
                    report.passed = report.failureCount == 0;
                }
                if (!report.passed) throw new Exception("Recorded model state differs; see failures");
            }
            catch (Exception e) { report.passed = false; report.error = e.ToString(); throw; }
            finally { report.seconds = watch.Elapsed.TotalSeconds; File.WriteAllText(output, JsonUtility.ToJson(report,true)); }
        }
        private static object Get(object value, string path)
        {
            if (path.Length == 0) return value;
            foreach (string name in path.Split('/'))
            {
                if (value == null) return null;
                if (Int32.TryParse(name, out int index))
                { if (value is IList list) value = index < list.Count ? list[index] : null; else { var e=((IEnumerable)value).GetEnumerator(); for(int i=0;i<=index;i++) if(!e.MoveNext())return null; value=e.Current; } }
                else
                { Type t=value.GetType(); var field=t.GetField(name,Flags)??t.GetField("<"+name+">k__BackingField",Flags); if(field==null)throw new MissingFieldException(t.FullName,name); value=field.GetValue(value); }
            }
            return value;
        }
        private static void RestoreInputNulls(object root, string prefix, Check[] checks)
        {
            foreach (Check check in checks)
            {
                if (check.kind != "null" || !check.path.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string path = check.path.Substring(prefix.Length); int slash = path.LastIndexOf('/');
                object parent = slash < 0 ? root : Get(root, path.Substring(0, slash));
                string name = path.Substring(slash + 1);
                parent.GetType().GetField(name, Flags).SetValue(parent, null);
            }
        }
        private static int Integer(object value) => value is float f ? BitConverter.SingleToInt32Bits(f) : value is bool b ? (b ? 1 : 0) : Convert.ToInt32(value);
        private static int[] Values(object value)
        { var result=new List<int>(); foreach(object item in (IEnumerable)value)result.Add(Integer(item));return result.ToArray(); }
        private static void Fail(string path, object expected, object actual)
        { report.failureCount++; if(report.failures.Count<30)report.failures.Add(new Failure{frame=frame,path=path,expected=expected?.ToString()??"null",actual=actual?.ToString()??"null"}); }
        private static void Compare(Battle battle, Check[] checks)
        {
            foreach(Check check in checks)
            {
                report.checks++;object actual=Get(battle,check.path);
                if(check.kind=="null") { if(actual!=null)Fail(check.path,null,actual); }
                else if(check.kind=="object") { if(actual==null)Fail(check.path,"non-null",null); }
                else if(check.kind=="string") { if(!Equals(actual,check.text))Fail(check.path,check.text,actual); }
                else if(check.kind=="count") { int count=actual is ICollection c?c.Count:-1; if(count!=check.bits)Fail(check.path,check.bits,count); }
                else if(check.kind.EndsWith("[]"))
                {
                    if(actual==null){Fail(check.path,"array",null);continue;}
                    int[] values=Values(actual); if(values.Length!=check.values.Length){Fail(check.path+"/length",check.values.Length,values.Length);continue;}
                    for(int i=0;i<values.Length;i++)if(values[i]!=check.values[i])Fail(check.path+"/"+i,check.values[i],values[i]);
                }
                else { if(actual==null||Integer(actual)!=check.bits)Fail(check.path,check.bits,actual==null?null:(object)Integer(actual)); }
            }
        }
    }
}
