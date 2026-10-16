#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Audio;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class NativeAudioEvidenceStage
    {
        [Serializable] private sealed class Op { public string method, path; public int channels; public int[] bits; public bool @null; }
        [Serializable] private sealed class Case { public string name; public int rate; public Op[] operations; }
        [Serializable] private sealed class Input { public Case[] cases; }
        [Serializable] private sealed class State { public int channels, rate, capacity; public bool stopped, gate; public int[] bits; }
        [Serializable] private sealed class Step { public string error; public int[] bytes; public State state; }
        [Serializable] private sealed class Result { public string name; public State initial; public List<Step> steps = new List<Step>(); }
        [Serializable] private sealed class Report { public bool completed, awakeReadsNativeRate; public string error; public int nativeRate, awakeRate; public List<Result> cases = new List<Result>(); }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static string Arg(string name) { var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); if (i < 0) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
        private static FieldInfo Field(string name) => typeof(NativeAudioEvidence).GetField(name, Flags);
        private static State Snapshot(NativeAudioEvidence target)
        {
            var samples = (List<float>)Field("samples").GetValue(target);
            return new State { channels = (int)Field("channels").GetValue(target), rate = (int)Field("rate").GetValue(target),
                stopped = (bool)Field("stopped").GetValue(target), gate = Field("gate").GetValue(target) != null,
                capacity = samples.Capacity, bits = samples.Select(BitConverter.SingleToInt32Bits).ToArray() };
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-capture-report"); if (File.Exists(output)) throw new IOException("Fresh output required");
            string folder = Path.GetDirectoryName(output); Directory.CreateDirectory(folder);
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-capture-input")));
            var report = new Report { awakeReadsNativeRate = true };
            try
            {
                foreach (var row in input.cases)
                {
                    var owner = new GameObject("Source native audio capture fixture"); owner.SetActive(false);
                    try
                    {
                        var capture = owner.AddComponent<NativeAudioEvidence>();
                        var result = new Result { name = row.name, initial = Snapshot(capture) }; report.cases.Add(result);
                        report.nativeRate = AudioSettings.outputSampleRate;
                        typeof(NativeAudioEvidence).GetMethod("Awake", Flags).Invoke(capture, null);
                        report.awakeRate = (int)Field("rate").GetValue(capture);
                        report.awakeReadsNativeRate &= report.awakeRate == report.nativeRate;
                        Field("rate").SetValue(capture, row.rate);
                        foreach (var op in row.operations)
                        {
                            var step = new Step { error = "", bytes = Array.Empty<int>() };
                            try
                            {
                                if (op.method == "Filter")
                                    typeof(NativeAudioEvidence).GetMethod("OnAudioFilterRead", Flags).Invoke(capture, new object[] {
                                        op.@null ? null : op.bits.Select(BitConverter.Int32BitsToSingle).ToArray(), op.channels });
                                else if (op.method == "Save")
                                {
                                    string path = op.path == "null" ? null : op.path == "missing" ? Path.Combine(folder, "declared-missing-parent", row.name + ".wav") : Path.Combine(folder, row.name + ".wav");
                                    capture.Save(path); step.bytes = File.ReadAllBytes(path).Select(v => (int)v).ToArray();
                                }
                                else throw new InvalidDataException(op.method);
                            }
                            catch (Exception ex)
                            {
                                if (ex is TargetInvocationException wrapped && wrapped.InnerException != null) ex = wrapped.InnerException;
                                step.error = ex.GetType().FullName;
                            }
                            step.state = Snapshot(capture); result.steps.Add(step);
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(owner); }
                }
                report.completed = report.awakeReadsNativeRate;
            }
            catch (Exception ex) { report.error = ex.ToString(); }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report)); }
            if (!report.completed) throw new Exception(report.error ?? "Actual sample-rate getter differs");
        }
    }
}
#endif
