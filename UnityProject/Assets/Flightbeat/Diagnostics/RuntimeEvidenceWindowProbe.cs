#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Harness mode observes the source runner without changing its game state.
    // Capture modes are explicitly artificial terminal-state fixtures: they
    // verify file/exit protocol, not a victory, defeat or original equivalence.
    public sealed class RuntimeEvidenceWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string mode, error, unityVersion, graphicsDevice, terminalState;
            public bool passed, originalEquivalenceChecked, artificialTerminalFixture, isolatedDiagnosticProfile;
            public int frames, screenWidth, screenHeight, expectedExitCode;
            public float terminalAt;
            public List<string> markers = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private string output, directory;
        private Report report;
        private float started;
        private bool finished, readyChecked, mergeSignaled, terminalSet, terminalLogged, harnessEnded;
        private PixelWingGame game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-runtime-window");
            if (flag < 0) return;
            int modeFlag = Array.IndexOf(args, "--flightbeat-runtime-mode");
            if (flag + 1 >= args.Length || modeFlag < 0 || modeFlag + 1 >= args.Length) throw new ArgumentException("Runtime report and mode required");
            string mode = args[modeFlag + 1], path = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(path)) throw new IOException("Fresh report required");
            bool harness = mode == "harness";
            if (harness ? Array.IndexOf(args, "--harness-test") < 0 : Array.IndexOf(args, "--idle-test") < 0 || Array.IndexOf(args, "--capture") < 0)
                throw new ArgumentException("Source diagnostic isolation/capture flags required");
            if (!harness && mode != "won" && mode != "lost" && mode != "idle-lost" && mode != "timeout") throw new ArgumentException("Unknown mode");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI runtime evidence protocol check"); DontDestroyOnLoad(owner);
            var probe = owner.AddComponent<RuntimeEvidenceWindowProbe>(); probe.output = path; probe.directory = Path.GetDirectoryName(path);
            probe.started = Time.realtimeSinceStartup;
            probe.report = new Report { mode = mode, unityVersion = Application.unityVersion,
                isolatedDiagnosticProfile = true, artificialTerminalFixture = !harness,
                expectedExitCode = mode == "lost" || mode == "timeout" ? 2 : 0 };
            Application.logMessageReceived += probe.Logged;
        }
        private void Logged(string text, string stack, LogType type)
        {
            if (finished) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.error = text + "\n" + stack;
            if (text.StartsWith("[GameHarness]") || text.StartsWith("EVIDENCE EXIT")) report.markers.Add(text);
            if (text.Contains("HARNESS_TEST_RESULT"))
            {
                harnessEnded = true;
                if (!text.Contains("status=passed")) report.error = text;
            }
            if (text.StartsWith("EVIDENCE EXIT")) terminalLogged = true;
        }
        private void LateUpdate()
        {
            if (finished) return;
            try
            {
                if (report.error != null) throw new Exception(report.error);
                if (Time.realtimeSinceStartup - started > 45f) throw new TimeoutException("Runtime mode " + report.mode);
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Battle == null) return;
                report.frames++;
                if (report.mode == "harness")
                {
                    if (harnessEnded) Finish(null, true);
                    return;
                }
                string capture = (string)Get("capturePath");
                if (Path.GetFullPath(capture) != Path.GetFullPath(directory)) throw new Exception("Capture/report directory mismatch");
                if (!readyChecked)
                {
                    if (!File.Exists(Path.Combine(directory, "01-ready.png"))) return;
                    CheckImage("01-ready"); readyChecked = true;
                }
                if (!mergeSignaled && game.Battle.Time > 4.5f)
                {
                    CheckImage("02-combat");
                    // Explicit signal fixture exercises the OnCue capture delay, not a fabricated merge count.
                    typeof(PixelWingGame).GetMethod("OnCue", Flags).Invoke(game, new object[] { "merge" }); mergeSignaled = true;
                }
                if (terminalSet || !mergeSignaled || !File.Exists(Path.Combine(directory, "03-fusion.png"))) return;
                // Keep the original 10-second sample running once; other modes reuse the build.
                if (report.mode == "won" && !report.markers.Exists(s => s.Contains("PERFORMANCE_SAMPLE"))) return;
                CheckImage("03-fusion");
                Set("idleTest", report.mode == "idle-lost");
                if (report.mode == "timeout") Set("clock", 480.1f);
                else typeof(Battle).GetProperty("State").SetValue(game.Battle, report.mode == "won" ? Phase.Won : Phase.Lost);
                report.terminalState = game.Battle.State.ToString(); report.terminalAt = Time.realtimeSinceStartup;
                terminalSet = true;
            }
            catch (Exception e) { Finish(e.ToString(), true); }
        }
        private object Get(string name) => typeof(PixelWingGame).GetField(name, Flags).GetValue(game);
        private void Set(string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private void CheckImage(string name)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, name + ".png"));
            var image = new Texture2D(2, 2);
            try { if (!image.LoadImage(bytes) || image.width != 540 || image.height != 960) throw new Exception("Bad source capture " + name); }
            finally { Destroy(image); }
        }
        private void OnApplicationQuit()
        {
            if (finished) return;
            try
            {
                if (!terminalSet || !terminalLogged) throw new Exception("Quit without source terminal protocol");
                CheckImage(report.mode == "won" ? "04-victory" : "04-end");
                string json = File.ReadAllText(Path.Combine(directory, "runtime.json"));
                if (!json.Contains("\"state\": \"" + report.terminalState + "\"") || !json.Contains("\"unity\": \"" + Application.unityVersion + "\"")) throw new Exception("Unexpected runtime JSON");
                if (!report.markers.Exists(s => s == "EVIDENCE EXIT " + report.expectedExitCode + " " + report.terminalState)) throw new Exception("Wrong source exit code");
                Finish(null, false);
            }
            catch (Exception e) { Finish(e.ToString(), false); }
        }
        private void Finish(string error, bool quit)
        {
            if (finished) return;
            finished = true; Application.logMessageReceived -= Logged;
            report.error = error; report.passed = error == null; report.graphicsDevice = SystemInfo.graphicsDeviceType.ToString();
            report.screenWidth = Screen.width; report.screenHeight = Screen.height;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (quit) Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
