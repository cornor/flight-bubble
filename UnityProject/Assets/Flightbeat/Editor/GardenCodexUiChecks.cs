using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Isolated editor acceptance: original sprites, shader output, collection states and real input routing.
    public static class GardenCodexUiChecks
    {
        const string Key = "FlightBubble.CodexUiChecks";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Serializable] sealed class Report
        {
            public bool passed;
            public string evidence = "Unity Editor camera rendering and real input routing with isolated staged profiles; not device gameplay";
            public string error;
            public List<string> checks = new List<string>(), screenshots = new List<string>(), runtimeErrors = new List<string>();
        }
        static Report report;
        static PixelWingGame game;
        static IEnumerator routine;
        static double started;
        static int lastFrame = -1;
        static string Output => Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "--codex-ui-checks") + 1];
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            if (!Application.isBatchMode || !args.Contains("--idle-test") || !args.Contains("--codex-ui-checks"))
                throw new InvalidOperationException("Batch mode, --idle-test and --codex-ui-checks OUTPUT required");
            if (Directory.Exists(Output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Output);
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity");
            EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
        {
            if (Application.isBatchMode && Environment.GetCommandLineArgs().Contains("--codex-ui-checks")) DevelopmentTools.Factory = _ => null;
        }
        [InitializeOnLoadMethod] static void Install()
        {
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool(Key, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                report = new Report(); started = EditorApplication.timeSinceStartup;
                Application.logMessageReceived += Log; routine = Checks(); EditorApplication.update += Tick;
            };
        }
        static void Tick()
        {
            try {
                if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Codex UI checks timeout");
                EditorApplication.QueuePlayerLoopUpdate();
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (!routine.MoveNext()) Finish(null);
            } catch (Exception e) { Finish(e); }
        }
        static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.runtimeErrors.Add(message + "\n" + stack);
        }
        static void Finish(Exception error)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SessionState.SetBool(Key, false);
            if (error == null && report.runtimeErrors.Count > 0) error = new Exception(string.Join("\n", report.runtimeErrors));
            report.passed = error == null; report.error = error?.ToString();
            File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(report, true));
            if (error != null) Debug.LogException(error); else Debug.Log("GARDEN_CODEX_UI_CHECKS_PASSED");
            EditorApplication.Exit(error == null ? 0 : 1);
        }
        static IEnumerator Checks()
        {
            for (int i = 0; i < 4; i++) yield return null;
            game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            Check(game && game.gardenPage, "Real game and packaged UI loaded");
            Check((bool)Get(game, "evidenceControlled"), "Save isolation active before actions");
            game.enabled = false;
            Set(game, "applicationSuspended", false); Set(game, "transitionActive", false);
            Set(game, "settings", false); Set(game, "evidenceControlled", false); game.InHome = true;
            var profile = new FleetProfile { unlocked = 5, energy = 5, energyAt = 1800000000, cleared = new List<int> { 1, 2, 3, 4 } };
            Set(game, "Economy", new FleetEconomy(profile, () => 1800000000L, _ => { }, n => 0));
            profile = ((FleetEconomy)Get(game, "Economy")).Data; // Constructor refresh publishes a normalized clone.
            Set(game, "ui", 10f);
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 1440) })
            {
                Capture("home-wave-" + size.x + "x" + size.y, size.x, size.y);
                Rect start = PixelWingGame.HomeWaveRect(105f, 736f, 330f, 110f);
                Rect album = PixelWingGame.HomeWaveRect(448f, 745f, 70f, 70f);
                Rect preview = PixelWingGame.HomeWaveRect(86f, 865f, 427f, 140f);
                Check(start.yMax < preview.y && start.xMax < album.x, "Start, album and partner preview remain separate at " + size);
                Check(Hit(start.center.x, start.center.y) == 800 && Hit(start.center.x, start.yMax - 12f) == 800, "Both start label and level badge start a game at " + size);
                Check(Hit(album.center.x, album.center.y) == 803 && Hit(preview.center.x, preview.center.y) == 803, "Album and partner preview retain their real input targets at " + size);
                Check(preview.yMax < ScreenLayout.SafeBottom, "Partner preview fits above the safe bottom at " + size);
                Check(Hit(ScreenLayout.Left + 50f, ScreenLayout.Top + 46f) == 801, "Settings stays in the top safe area at " + size);
                Check((string)Get(game, "homeNextName") == "小霞", "Level-five preview uses the actual next partner at " + size);
            }
            Capture("home-before", 1080, 1920);
            PixelWingGame.HomeWaveView(out float homeScale, out float homeX, out float homeY);
            Check(Hit(ScreenLayout.Right - 42f, homeY + 410f * homeScale + 120f) != 806, "Hidden level picker has no home hit region");
            Click(homeX + 483f * homeScale, homeY + 780f * homeScale);
            Check((bool)Get(game, "codexOpen"), "Home entry opens album through normal input routing");
            string saved = JsonUtility.ToJson(profile);
            var list = (IList)typeof(PixelWingGame).GetProperty("Codex", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Check(list.Count == 20 && list.Cast<object>().Count(e => (bool)Call("CodexObtained", e)) == 8, "First four clears unlock exactly 8 of 20 members");
            foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 1440) })
            {
                Set(game, "ui", 12f); Set(game, "codexSelected", 0); Set(game, "codexPickedAt", 9f);
                Capture("album-8-" + viewport.x + "x" + viewport.y, viewport.x, viewport.y);
                float oy = (ScreenLayout.Height - 960f) * .5f;
                Check(Hit(500f, ScreenLayout.Top + 44f) == 804, "Close hit stays at the top safe area at " + viewport);
                Check(Hit(270f, oy + 460f) == 0 && Hit(15f, oy + 800f) == 0, "Album blocks underlying home controls at " + viewport);
                for (int i = 0; i < 20; i++)
                {
                    float x = 70f + i % 5 * 100f, y = oy + 554f + i / 5 * 102f;
                    Check(Hit(x, y) == 810 + i, "Card " + i + " hit aligns at " + viewport);
                    Click(x, y);
                    Check((int)Get(game, "codexSelected") == i, "Card " + i + " selects via real input at " + viewport);
                }
                Check((bool)Get(game, "codexOpen") && game.InHome, "Locked cards stay in album at " + viewport);
            }
            Set(game, "ui", 14f); Capture("album-locked-19", 1080, 1920);
            Check(saved == JsonUtility.ToJson(((FleetEconomy)Get(game, "Economy")).Data), "Opening and selecting every card never changes saved collection data");
            profile.cleared.Clear(); Set(game, "codexSelected", -1);
            Color empty = Capture("album-empty", 1080, 1920);
            Check(list.Cast<object>().All(e => !(bool)Call("CodexObtained", e)), "Empty profile keeps all members locked");
            profile.cleared.Add(5);
            Check(list.Cast<object>().Count(e => (bool)Call("CodexObtained", e)) == 4, "Only clearing level 5 unlocks its four debut members, without unlocking earlier members");
            profile.cleared = Enumerable.Range(1, 19).ToList(); Set(game, "codexSelected", 0);
            Color full = Capture("album-complete", 1080, 1920);
            Check(list.Cast<object>().All(e => (bool)Call("CodexObtained", e)), "Complete profile unlocks all 20 members");
            Check(empty.r > full.r + .18f && full.g > full.r + .15f && full.g > full.b + .15f,
                "Actual rendered meter changes from empty to full green at the right end");
            Set(game, "codexSelected", 18); Set(game, "codexPickedAt", 13.85f);
            foreach (int height in new[] { 1920, 2100, 2340 })
                Capture("album-frog-bounce-" + height, 1080, height);
            Click(500f, ScreenLayout.Top + 44f); Check(!(bool)Get(game, "codexOpen") && game.InHome, "Close returns to home through normal input");
            Capture("home-after-close", 1080, 1920);
            Check((string)Get(game, "homeNextLine") == "小队伙伴已集齐", "A complete collection fills the same partner area with the assembled-squad state");
            profile.cleared.Clear();
            Capture("home-empty-profile", 1080, 2340);
            Check((string)Get(game, "homeNextName") == "小蓝", "A fresh profile previews its first real partner");
            Capture("home-restored-standard", 1080, 1920);
            var background = (RawImage)Get(game.gardenPage, "homeBackground");
            Check(background.texture.name == "home-background", "Closing restores original home texture");
            Check(background.rectTransform.offsetMin == Vector2.zero && background.rectTransform.offsetMax == Vector2.zero, "Closing restores original home bounds");
            int selectedSet = PixelWingGame.LevelSetCurrent;
            Click(50f, ScreenLayout.Top + 46f);
            Check((bool)Get(game, "settings"), "Home settings still opens via its existing button");
            Capture("home-settings-hidden-levels", 1080, 1920);
            var newLevels = (GameObject)Get(game.gardenPage, "levelSetRow");
            Check(newLevels && !newLevels.activeInHierarchy, "New-level setting is hidden");
            var divider = (GameObject)Get(game.gardenPage, "levelSetDivider");
            Check(!divider || !divider.activeInHierarchy, "Hidden setting leaves no trailing divider");
            foreach (var action in game.gardenPage.pauseDialog.GetComponentsInChildren<Flightbeat.Garden.UI.GardenUiAction>(true).Where(a => a.actionId == 805))
                Check(!action.gameObject.activeInHierarchy, "New-level setting has no active hit target");
            Check(selectedSet == PixelWingGame.LevelSetCurrent, "Hiding settings preserves the selected level set");
        }
        static Color Capture(string name, int width, int height)
        {
            var camera = (Camera)Get(game, "gameCamera"); var previous = camera.targetTexture;
            var rt = new RenderTexture(width, height, 24); var active = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try {
                rt.Create(); camera.targetTexture = rt;
                Set(game, "evidenceViewport", new Vector2Int(width, height)); Call("ApplyDisplay", width, height);
                game.gardenPage.ApplyLayout(); game.RefreshGardenUi(game.gardenPage);
                ((PixelSurface)Get(game, "surface")).Redraw(); ((PixelSurface)Get(game, "hud")).Redraw();
                ((PixelSurface)Get(game, "gardenOverlay")).Redraw();
                Canvas.ForceUpdateCanvases(); game.Effects.Relayout(); camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); report.screenshots.Add(name + ".png");
                if ((bool)Get(game, "codexOpen"))
                {
                    var bg = (RawImage)Get(game.gardenPage, "homeBackground");
                    Check(bg.texture.name == "codex-background", "Independent cloud background active: " + name);
                    Color leftEdge = image.GetPixel(2, height / 2), rightEdge = image.GetPixel(width - 3, height / 2);
                    Check(leftEdge.b > .5f && rightEdge.b > .5f, "Cloud background covers both viewport edges: " + name);
                }
                return image.GetPixel(Mathf.RoundToInt((370f - ScreenLayout.Left) * ScreenLayout.Scale), Mathf.RoundToInt(height - (ScreenLayout.Top + 119f) * ScreenLayout.Scale));
            } finally {
                camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        static int Hit(float x, float y) => (int)Call("HitTest", new Vector2(x, ScreenLayout.InverseY(y)));
        static void Click(float x, float y) => game.PointerDown(new Vector2(x, ScreenLayout.InverseY(y)));
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); report.checks.Add(message); }
        static object Get(object target, string name) => target.GetType().GetField(name, Flags)?.GetValue(target) ?? target.GetType().GetProperty(name, Flags)?.GetValue(target);
        static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Flags);
            if (field != null) field.SetValue(target, value); else target.GetType().GetProperty(name, Flags).SetValue(target, value);
        }
        static object Call(string name, params object[] args) => game.GetType().GetMethod(name, Flags).Invoke(game, args);
    }
}
