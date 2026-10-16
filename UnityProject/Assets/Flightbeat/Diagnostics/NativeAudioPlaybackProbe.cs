#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Flightbeat.Audio;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Explicit isolated CLI scene. Uses the actual ChipAudio component, clocks,
    // AudioSources and f54332 audio-thread callback; never touches game saves.
    public sealed class NativeAudioPlaybackProbe : MonoBehaviour
    {
        internal static bool IsolatedSceneRequested => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "AudioPlaybackBoundary.generated"
            && Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-audio-playback-output") >= 0;
        [Serializable] private sealed class Segment
        {
            public string name; public int beginSample, endSample, nonzeroSamples;
            public double beforeDsp, afterDsp; public bool passed;
        }
        [Serializable] private sealed class AudioCall
        {
            public int function; public string clip; public double value, nativeDsp;
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion, bundleSha256, error, floatSha256, wavSha256, scope;
            public int rate, channels, samples, listenerCount, chipAudioCount; public bool passed, callbackStopped, ordinaryClocks, gameLoaded;
            public List<Segment> segments = new List<Segment>();
            public List<AudioCall> calls = new List<AudioCall>();
        }
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Report report = new Report();
        private string output; private ChipAudio chipAudio; private NativeAudioEvidence capture;
        private object gate; private List<float> samples; private bool finished; private double started;
        private int Count() { lock (gate) return samples.Count; }
        private int Nonzero(int begin, int end)
        { int count = 0; lock (gate) for (int i = begin; i < end; i++) if (samples[i] != 0f) count++; return count; }
        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--flightbeat-audio-playback-output");
            if (at < 0 || at + 1 >= args.Length) { enabled = false; Application.Quit(1); return; }
            try
            {
                string requested = Path.GetFullPath(args[at + 1]);
                if (Directory.Exists(requested) || File.Exists(requested)) throw new IOException("Fresh output directory required");
                Directory.CreateDirectory(requested); output = requested;
                started = Time.realtimeSinceStartupAsDouble;
                Application.logMessageReceived += Logged;
                report.unityVersion = Application.unityVersion; report.bundleSha256 = OriginalAudioAssets.BundleSha256;
                report.scope = "Isolated real ChipAudio/AudioSource playback; f54332 captures listener mixer PCM through native audio callbacks. No game, injected DSP/time/RNG, resampling setting changes, device loopback or browser waveform equivalence.";
                report.ordinaryClocks = RuntimeLoopInputs.AudioInput == null;
                if (!report.ordinaryClocks || RuntimeLoopInputs.AudioEvent != null) throw new InvalidOperationException("Existing diagnostic input/observer");
                RuntimeLoopInputs.AudioEvent = (fn, source, clip, value, cue) =>
                {
                    if (fn == 54192 || fn == 54193 || fn == 54194 || fn == 54195 || fn == 55175)
                        report.calls.Add(new AudioCall { function = fn, clip = clip ? clip.name : source && source.clip ? source.clip.name : cue,
                            value = value, nativeDsp = AudioSettings.dspTime });
                };
                gameObject.AddComponent<AudioListener>();
                capture = gameObject.AddComponent<NativeAudioEvidence>();
                gate = typeof(NativeAudioEvidence).GetField("gate", Flags).GetValue(capture);
                samples = (List<float>)typeof(NativeAudioEvidence).GetField("samples", Flags).GetValue(capture);
                chipAudio = new GameObject("Actual original-derived ChipAudio").AddComponent<ChipAudio>();
                report.rate = AudioSettings.outputSampleRate;
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private IEnumerator Start()
        {
            if (finished || !enabled) yield break;
            report.gameLoaded = FindObjectsOfType<Flightbeat.Game.PixelWingGame>().Length != 0;
            report.listenerCount = FindObjectsOfType<AudioListener>().Length;
            report.chipAudioCount = FindObjectsOfType<ChipAudio>().Length;
            if (report.gameLoaded || report.listenerCount != 1 || report.chipAudioCount != 1)
            { Finish("Isolated fixture contains unexpected game/audio components"); yield break; }
            // Declared brief API fixture, not a recorded gameplay route.
            yield return new WaitForSecondsRealtime(.2f);
            foreach (string name in new[] { "tick", "launch", "music" })
            {
                if (finished) yield break;
                var segment = new Segment { name = name, beginSample = Count(), beforeDsp = AudioSettings.dspTime };
                report.segments.Add(segment);
                if (name == "music") chipAudio.Begin(); else chipAudio.Play(name);
                float seconds = name == "music" ? .6f : OriginalAudioAssets.Load("Audio/" + name).length + .2f;
                yield return new WaitForSecondsRealtime(seconds);
                segment.endSample = Count(); segment.afterDsp = AudioSettings.dspTime;
                segment.nonzeroSamples = Nonzero(segment.beginSample, segment.endSample);
                segment.passed = segment.afterDsp > segment.beforeDsp && segment.endSample > segment.beginSample && segment.nonzeroSamples > 0;
                chipAudio.ResetMusic();
                yield return new WaitForSecondsRealtime(.2f);
            }
            Finish(null);
        }
        private void Update()
        { if (!finished && Time.realtimeSinceStartupAsDouble - started > 20) Finish("Native callback fixture timeout"); }
        private void Logged(string message, string stack, LogType type)
        { if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(message + "\n" + stack); }
        private static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private void Finish(string error)
        {
            if (finished) return; finished = true; report.error = error;
            Application.logMessageReceived -= Logged; RuntimeLoopInputs.AudioEvent = null;
            try
            {
                if (output != null && capture)
                {
                    string wav = Path.Combine(output, "listener.wav"); capture.Save(wav);
                    report.callbackStopped = (bool)typeof(NativeAudioEvidence).GetField("stopped", Flags).GetValue(capture);
                    report.channels = (int)typeof(NativeAudioEvidence).GetField("channels", Flags).GetValue(capture);
                    report.samples = Count(); byte[] bytes = new byte[report.samples * 4];
                    lock (gate) Buffer.BlockCopy(samples.ToArray(), 0, bytes, 0, bytes.Length);
                    File.WriteAllBytes(Path.Combine(output, "listener.f32"), bytes); report.floatSha256 = Hash(bytes);
                    report.wavSha256 = Hash(File.ReadAllBytes(wav));
                }
                report.passed = error == null && !report.gameLoaded && report.listenerCount == 1 && report.chipAudioCount == 1
                    && report.ordinaryClocks && report.callbackStopped && report.segments.Count == 3 && report.segments.TrueForAll(s => s.passed);
            }
            catch (Exception e) { report.error = (report.error ?? "") + e; report.passed = false; }
            finally
            {
                if (chipAudio) foreach (var source in chipAudio.GetComponents<AudioSource>()) source.Stop();
                if (output != null) File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(report, true));
                else Debug.LogError(error);
                Application.Quit(report.passed ? 0 : 1);
            }
        }
    }
}
#endif
