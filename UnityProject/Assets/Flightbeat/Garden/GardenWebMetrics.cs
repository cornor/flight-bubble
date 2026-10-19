#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Garden {
    // Read-only wall-clock telemetry. No timeScale/fixedDeltaTime/model/control writes.
    public sealed class GardenWebMetrics : MonoBehaviour {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GardenMetrics(string json);
#endif
        [Serializable] public sealed class Lane { public int color, ammo; public bool exposed, merge; }
        [Serializable] public sealed class Sample {
            public int level, remaining, total, visibleBugs, fleet, maxTier, step, phase, frames;
            public float battleTime, wallSeconds, fps, p50ms, p95ms, p99ms, maxMs;
            public int denseFrames; public float denseSeconds, denseFps, denseP95ms, denseP99ms;
            public bool home, settings, paused, finaleReady, finaleGuide, transition, musicMuted, soundMuted, lightEnabled;
            public int[] fleetColors; public Lane[] lanes;
            // 2026-10-19 B level set: which set is on, where this level was loaded from ("A", "B", "B-test"),
            // Actual resource bytes and parsed board, plus release-list expectations (empty for A).
            public int levelSet;
            public string levelName, levelSource, fileSha256, cellsSha256, expectedFileSha256, expectedCellsSha256;
        }
        readonly List<float> frameTimes = new List<float>(18000);
        readonly List<float> denseTimes = new List<float>(18000);
        double denseSum;
        PixelWingGame game;
        float reportAt; int lastLevel = -1; double sum; int visible;
        Battle hashedBattle;
        PixelWingGame.LevelSetSource loadedSource;
        string loadedFile = "", loadedCells = "";
        public static GardenWebMetrics Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() { new GameObject("Garden R1 metrics").AddComponent<GardenWebMetrics>(); }
        void Awake() { Instance = this; }
        public static void Visible(int count) { if (Instance != null) Instance.visible = count; }
        float Percentile(float[] values, float p) => values.Length == 0 ? 0 : values[Mathf.Min(values.Length - 1, Mathf.CeilToInt(values.Length * p) - 1)];
        void LateUpdate() {
            if (game == null) game = FindObjectOfType<PixelWingGame>();
            if (game == null || game.Battle == null) return;
            if (lastLevel != game.LevelIndex) { frameTimes.Clear(); denseTimes.Clear(); sum = 0; denseSum = 0; lastLevel = game.LevelIndex; }
            Battle b = game.Battle;
            // Capture once when a new battle appears, before the reporting interval. Settings may change
            // the names for the next battle; that must not relabel the battle already in memory.
            if (hashedBattle != b) {
                hashedBattle = b;
                loadedSource = PixelWingGame.LevelSource(game.LevelIndex);
                var asset = Resources.Load<TextAsset>("Levels/" + loadedSource.name);
                loadedFile = asset ? GardenLevelHashes.FileSha256(asset.bytes) : "";
                loadedCells = GardenLevelHashes.CellsSha256(b.Level.cells);
            }
            // Gameplay frames only. Intro, menus, hidden tab and paused time are excluded.
            if (!game.InHome && b.State == Phase.Playing && !game.paused && !game.applicationSuspended) {
                float ms = Time.unscaledDeltaTime * 1000;
                if (frameTimes.Count < 18000) { frameTimes.Add(ms); sum += ms; }
                if (visible >= 1000 && denseTimes.Count < 18000) { denseTimes.Add(ms); denseSum += ms; }
            }
            if (Time.realtimeSinceStartup < reportAt) return;
            reportAt = Time.realtimeSinceStartup + 1;
            var times = frameTimes.ToArray(); Array.Sort(times);
            var dense = denseTimes.ToArray(); Array.Sort(dense);
            var sample = new Sample { level = game.LevelIndex, remaining = b.Remaining, total = b.Total, visibleBugs = visible,
                fleet = b.Fleet.Count, fleetColors = new int[b.Fleet.Count], step = game.TutorialStep, phase = (int)b.State,
                battleTime = b.Time, home = game.InHome, settings = game.settings, paused = game.paused,
                musicMuted = game.Audio.MusicMuted, soundMuted = game.Audio.EffectsMuted, lightEnabled = game.Effects.Enabled,
                finaleReady = b.FinaleReady, finaleGuide = game.FinaleGuideActive, transition = game.TransitionActive,
                lanes = new Lane[b.LaneCount], frames = times.Length, wallSeconds = (float)(sum / 1000),
                denseFrames = dense.Length, denseSeconds = (float)(denseSum / 1000),
                denseFps = denseSum > 0 ? (float)(dense.Length * 1000 / denseSum) : 0, denseP95ms = Percentile(dense,.95f), denseP99ms = Percentile(dense,.99f),
                fps = sum > 0 ? (float)(times.Length * 1000 / sum) : 0,
                p50ms = Percentile(times,.5f), p95ms = Percentile(times,.95f), p99ms = Percentile(times,.99f), maxMs = Percentile(times,1) };
            sample.levelSet = PixelWingGame.LevelSetCurrent; sample.levelName = loadedSource.name; sample.levelSource = loadedSource.kind;
            sample.fileSha256 = loadedFile; sample.cellsSha256 = loadedCells;
            sample.expectedFileSha256 = loadedSource.fileSha256 ?? "";
            sample.expectedCellsSha256 = loadedSource.cellsSha256 ?? "";
            for (int i = 0; i < b.Fleet.Count; i++) { sample.fleetColors[i] = b.Fleet[i].Color; sample.maxTier = Mathf.Max(sample.maxTier, b.Fleet[i].Tier); }
            for (int i = 0; i < b.LaneCount; i++) sample.lanes[i] = b.Reserve[i].Count == 0 ? new Lane { color = -1 } : new Lane {
                color = b.Reserve[i][0].Color, ammo = b.Reserve[i][0].Ammo, exposed = b.Exposed(b.Reserve[i][0].Color), merge = PixelWingGame.GardenMergeHintVisible(b, i) };
#if UNITY_WEBGL && !UNITY_EDITOR
            GardenMetrics(JsonUtility.ToJson(sample));
#endif
        }
    }
}
#endif
