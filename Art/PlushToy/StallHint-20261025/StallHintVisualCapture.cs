// One-off Editor validation fixture. Copied into Assets/Flightbeat/Editor only for the check run.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class StallHintVisualCapture
    {
        const string SessionKey = "FlightBubble.BarReview";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static PixelWingGame game;
        static PixelSurface hud;
        static IEnumerator routine;
        static int lastFrame = -1;
        static double started;
        static readonly List<string> checks = new List<string>(), errors = new List<string>();
        static string Output => Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "--bar-layout-checks") + 1];
        static object Get(object o, string key) => o.GetType().GetField(key, Flags)?.GetValue(o) ?? o.GetType().GetProperty(key, Flags)?.GetValue(o);
        static void Set(object o, string key, object value)
        {
            var field = o.GetType().GetField(key, Flags);
            if (field != null) field.SetValue(o, value); else o.GetType().GetProperty(key, Flags).SetValue(o, value);
        }
        static void Call(string key, params object[] args) => game.GetType().GetMethod(key, Flags).Invoke(game, args);
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
        public static void Run()
        {
            if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("--idle-test"))
                throw new Exception("Isolated batch mode required");
            Directory.CreateDirectory(Output);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity");
            EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {
            if (Application.isBatchMode && Environment.GetCommandLineArgs().Contains("--bar-layout-checks"))
                DevelopmentTools.Factory = _ => null;
        }
        [InitializeOnLoadMethod] static void Install()
        {
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool(SessionKey, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                started = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += Log;
                routine = Review(); EditorApplication.update += Tick;
            };
        }
        static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
        }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - started > 180) throw new Exception("Review timeout");
                EditorApplication.QueuePlayerLoopUpdate();
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (!routine.MoveNext()) Finish(null);
            }
            catch (Exception e) { Finish(e); }
        }
        [Serializable] class Report { public bool passed; public string error; public string[] checks, runtimeErrors; }
        static void Finish(Exception error)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
            SessionState.SetBool(SessionKey, false);
            bool passed = error == null && errors.Count == 0;
            File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(new Report {
                passed = passed, error = error?.ToString(), checks = checks.ToArray(), runtimeErrors = errors.ToArray()
            }, true));
            Debug.Log("GARDEN_BAR_REVIEW " + (passed ? "PASS" : "FAIL") + " " + error);
            EditorApplication.Exit(passed ? 0 : 1);
        }
        static void Capture(string name, int width, int height, Action<PixelSurface> draw)
        {
            var camera = (Camera)Get(game, "gameCamera"); var previous = camera.targetTexture;
            var rt = new RenderTexture(width, height, 24); var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var active = RenderTexture.active; var paint = hud.Paint;
            try
            {
                rt.Create(); camera.targetTexture = rt;
                hud.Paint = s => { paint(s); s.MapLayout = true; draw(s); };
                game.gardenPage.ApplyLayout(); game.RefreshGardenUi(game.gardenPage);
                ((PixelSurface)Get(game, "surface")).Redraw(); hud.Redraw();
                Canvas.ForceUpdateCanvases(); game.Effects.Relayout(); camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + "-" + width + "x" + height + ".png"), image.EncodeToPNG());
            }
            finally
            {
                hud.Paint = paint; camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        static IEnumerator Review()
        {
            for (int i = 0; i < 4; i++) yield return null;
            game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            Check(game != null && (bool)Get(game, "evidenceControlled"), "Isolated staged visual fixture; not a gameplay regression test");
            game.Economy.Data.unlocked = 262;
            Check(game.StartSortie(5), "Formal battle background loaded");
            game.enabled = false;
            Set(game.Battle, "State", Phase.Playing); Set(game, "applicationSuspended", false);
            Set(game, "ui", 10f); Set(game, "clock", 10f); Set(game, "transitionActive", false);
            Set(game, "paused", false); Set(game, "settings", false);
            hud = (PixelSurface)Get(game, "hud");
            foreach (var viewport in new[] { new Vector2Int(360, 800), new Vector2Int(390, 844), new Vector2Int(540, 960) })
            {
                Set(game, "evidenceViewport", viewport); Call("ApplyDisplay", viewport.x, viewport.y);
                Capture("stall-hint", viewport.x, viewport.y, s => Call("DrawGardenStallHint", s));
            }
        }
    }
}
