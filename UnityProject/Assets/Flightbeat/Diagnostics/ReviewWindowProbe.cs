#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Passive CLI observer. The original tour controls every gameplay action.
    // The adapter isolates/restores save slots and exits after the source DONE log.
    public sealed class ReviewWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Saved { public string key, value; public bool present; }
        [Serializable] private sealed class Backup { public List<Saved> values = new List<Saved>(); }
        [Serializable] private sealed class Frame
        {
            public string name, state; public bool home, transition; public int level; public float seconds;
        }
        [Serializable] private sealed class Report
        {
            public bool passed, sourceDone, savesRestored, originalEquivalenceChecked;
            public bool artificialSourceFixtures = true;
            public string error, unity, device, only;
            public float elapsedSeconds;
            public List<string> markers = new List<string>();
            public List<Frame> frames = new List<Frame>();
        }
        private static readonly string[] Keys = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b" };
        private readonly HashSet<string> seen = new HashSet<string>();
        private string output;
        private Backup backup;
        private Report report;
        private float began;
        private bool doneLog, finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--flightbeat-review-report");
            if (i < 0) return;
            string path = Path.GetFullPath(args[i + 1]);
            if (File.Exists(path) || File.Exists(path + ".save-backup.json")) throw new IOException("Use a fresh review report path.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI original review observer"); DontDestroyOnLoad(owner);
            var probe = owner.AddComponent<ReviewWindowProbe>();
            probe.output = path; probe.began = Time.realtimeSinceStartup;
            probe.report = new Report { unity = Application.unityVersion, device = SystemInfo.graphicsDeviceName };
            probe.backup = new Backup();
            foreach (string key in Keys)
                probe.backup.values.Add(new Saved { key = key, present = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key, "") });
            File.WriteAllText(path + ".save-backup.json", JsonUtility.ToJson(probe.backup, true));
            foreach (string key in Keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Application.logMessageReceived += probe.Logged;
        }
        private void Logged(string text, string stack, LogType type)
        {
            if (finished) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.error = text + "\n" + stack;
            if (text.StartsWith("REVIEW ")) report.markers.Add(text);
            if (text.StartsWith("REVIEW DONE ")) doneLog = true;
        }
        private void LateUpdate()
        {
            if (finished) return;
            try
            {
                if (!String.IsNullOrEmpty(report.error)) throw new Exception(report.error);
                if (Time.realtimeSinceStartup - began > 600f) throw new TimeoutException("Original review did not finish within observer limit.");
                ReviewCapture capture = FindObjectOfType<ReviewCapture>();
                if (capture == null || String.IsNullOrEmpty(capture.Folder)) return;
                PixelWingGame game = capture.GetComponent<PixelWingGame>();
                foreach (string path in Directory.GetFiles(capture.Folder, "*.png"))
                {
                    if (!seen.Add(path)) continue;
                    // Header validation avoids loading another 162 textures.
                    byte[] bytes = File.ReadAllBytes(path);
                    if (bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 || ReadInt(bytes, 16) != capture.Width || ReadInt(bytes, 20) != capture.Height)
                        throw new Exception("Invalid review image: " + path);
                    report.frames.Add(new Frame { name = Path.GetFileName(path), state = game.Battle.State.ToString(),
                        home = game.InHome, transition = game.TransitionActive, level = game.LevelIndex, seconds = Time.realtimeSinceStartup - began });
                }
                if (!doneLog) return;
                report.sourceDone = capture.Done; report.only = capture.Only;
                if (!capture.Done) throw new Exception("Source logged DONE without Done flag.");
                if (String.IsNullOrEmpty(capture.Only) && seen.Count != 162) throw new Exception("Full tour image count: " + seen.Count);
                Finish(null);
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static int ReadInt(byte[] bytes, int offset) => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        private void Finish(string error)
        {
            if (finished) return;
            finished = true; report.error = error; report.elapsedSeconds = Time.realtimeSinceStartup - began;
            Restore(); report.passed = error == null && report.savesRestored && report.sourceDone;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("REVIEW_OBSERVER " + (report.passed ? "passed" : "failed"));
            Application.Quit(report.passed ? 0 : 6);
        }
        private void Restore()
        {
            foreach (Saved saved in backup.values)
                if (saved.present) PlayerPrefs.SetString(saved.key, saved.value); else PlayerPrefs.DeleteKey(saved.key);
            PlayerPrefs.Save(); report.savesRestored = true;
            foreach (Saved saved in backup.values)
                if (PlayerPrefs.HasKey(saved.key) != saved.present || (saved.present && PlayerPrefs.GetString(saved.key) != saved.value)) report.savesRestored = false;
        }
        private void OnApplicationQuit() { if (!finished && backup != null) Restore(); }
    }
}
#endif
