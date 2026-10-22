using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Editor-only visual acceptance. Real camera/prefabs/input, staged battle state, isolated no-op saves.
    // Run in batch mode WITH graphics, --idle-test --revive-ui-checks NEW_OUTPUT_DIRECTORY (without -quit).
    public static class GardenReviveUiChecks
    {
        const string Key = "FlightBubble.ReviveUiChecks";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Serializable] sealed class Report
        {
            public bool passed;
            public string evidence = "Unity Editor real rendering and input, staged battle state; not WebGL or device gameplay";
            public string error;
            public List<string> checks = new List<string>(), screenshots = new List<string>(), runtimeErrors = new List<string>();
        }
        static Report report;
        static PixelWingGame game;
        static IEnumerator routine;
        static double started;
        static int lastFrame = -1;
        static string Output => Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "--revive-ui-checks") + 1];

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            if (!Application.isBatchMode || !args.Contains("--idle-test") || !args.Contains("--revive-ui-checks"))
                throw new InvalidOperationException("Batch mode, --idle-test and --revive-ui-checks OUTPUT required");
            if (Directory.Exists(Output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Output);
            GardenBattleUiAssets.RecreateForV3();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity");
            EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
        {
            if (Application.isBatchMode && Environment.GetCommandLineArgs().Contains("--revive-ui-checks"))
                DevelopmentTools.Factory = _ => null;
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
                if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Revive UI checks timeout");
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
            if (error != null) Debug.LogException(error); else Debug.Log("GARDEN_REVIVE_UI_CHECKS_PASSED");
            EditorApplication.Exit(error == null ? 0 : 1);
        }
        static IEnumerator Checks()
        {
            for (int i = 0; i < 4; i++) yield return null;
            game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            Check(game && game.gardenPage, "Real game Awake and packaged page loaded");
            Check((bool)Get(game, "evidenceControlled"), "Save isolation active before actions");
            game.enabled = false; // Keep staged screenshots stable; all renders and input are invoked explicitly below.
            Set(game, "applicationSuspended", false);
            Set(game, "Economy", new FleetEconomy(new FleetProfile { unlocked = 262, energy = 5, energyAt = 1800000000 }, () => 1800000000L, _ => { }, n => 0));
            Set(game, "evidenceControlled", false);
            if (Environment.GetCommandLineArgs().Contains("--bomb-actions-only")) { CheckBombActions(); yield break; }
            Check(GardenBars.HasIcon("bomb") && GardenBars.HasIcon("ad"), "Both replacement icons resolve through shared atlas");
            FreshBattle();
            var bombIcon = game.gardenPage.props[2].transform.Find("Button image/Bomb icon");
            Check(bombIcon && bombIcon.GetComponent<UnityEngine.UI.Image>().sprite, "Prop button uses independent shared bomb sprite");
            Capture("battle-icons-1080x1920", 1080, 1920);
            Set(game.Battle, "Remaining", Mathf.FloorToInt(game.Battle.Total * .32f));
            Set(game.Battle, "State", Phase.ReviveOffer);
            Set(game, "ui", 10f);
            foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 1440) })
            {
                Capture("revive-68-" + viewport.x + "x" + viewport.y, viewport.x, viewport.y);
                float oy = (ScreenLayout.Height - 960f) * .5f;
                Check(Hit(270f, oy + 666f) == 30, "Revive hit aligns at " + viewport);
                Check(Hit(270f, oy + 718f) == 31, "Decline hit aligns at " + viewport);
                Check(Hit(270f, oy + 550f) == 0 && Hit(30f, oy + 800f) == 0, "Modal blocks rewards and background at " + viewport);
            }
            foreach (float p in new[] { 0f, .01f, 1f })
            {
                Set(game.Battle, "Remaining", Mathf.RoundToInt(game.Battle.Total * (1f - p)));
                Capture("revive-progress-" + Mathf.RoundToInt(p * 100f), 1080, 1920);
            }
            FreshBattle(); Set(game.Battle, "State", Phase.ReviveOffer);
            Capture("revive-before-click", 1080, 2340);
            int slots = game.Battle.UnlockedSlots, missiles = game.Battle.RescueMissiles;
            Click(270f, (ScreenLayout.Height - 960f) * .5f + 666f);
            Check(game.Battle.Revived && game.Battle.State != Phase.ReviveOffer, "Actual revive button resumes battle");
            Check(game.Battle.UnlockedSlots == slots + 1 && game.Battle.RescueMissiles == missiles + 1, "Revive keeps original slot and bomb reward");
            FreshBattle(); Set(game.Battle, "State", Phase.ReviveOffer);
            Capture("revive-before-decline", 1080, 1920);
            Click(270f, (ScreenLayout.Height - 960f) * .5f + 718f);
            Check(game.Battle.State == Phase.Lost, "Actual decline button ends battle");
            // Settle only presentation timing after the real decline action; capture the loss page too.
            Set(Get(game, "feedback"), "endAge", 2f);
            Set(game, "loseBattle", game.Battle); Set(game, "loseShownAt", (float)Get(game, "ui") - 2f);
            Set(game, "loseTip", (string[])Call("LoseTip"));
            Set(game.Battle, "Remaining", Mathf.FloorToInt(game.Battle.Total * .32f));
            foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 1440) })
            {
                Capture("lose-68-" + viewport.x + "x" + viewport.y, viewport.x, viewport.y);
                float oy = (ScreenLayout.Height - 960f) * .5f;
                Check(Hit(316f, oy + 698f) == 40, "Retry hit aligns at " + viewport);
                Check(Hit(138f, oy + 700f) == 41, "Home button hit aligns at " + viewport);
                Check(Hit(270f, oy + 565f) == 0, "Loss tip is non-interactive at " + viewport);
            }
            foreach (float p in new[] { 0f, .01f, 1f })
            {
                Set(game.Battle, "Remaining", Mathf.RoundToInt(game.Battle.Total * (1f - p)));
                Capture("lose-progress-" + Mathf.RoundToInt(p * 100f), 1080, 1920);
            }
        }
        static void CheckBombActions()
        {
            FreshBattle(); game.Economy.Data.items[2] = 2;
            Capture("before-aim", 1080, 1920);
            Check((string)Get(game.gardenPage.bombPropWord, "rendered") == "泡泡炸弹" && !game.gardenPage.bombCancelBadge.interactable,
                "Normal bomb prop keeps its name and inactive cancel badge target");
            Click(game.Battle.QueuePositionX(0), ScreenLayout.Y(725f));
            Check(game.Battle.State == Phase.Playing, "Real queue input starts battle");
            Capture("before-bomb", 1080, 1920);
            Click(433f, ScreenLayout.Y(916f));
            Check((int)Get(game, "selection") == 2 && (bool)Get(game, "paused"), "Bomb prop opens paused aiming");
            foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1080, 1440) })
            {
                Capture("bomb-actions-" + viewport.x + "x" + viewport.y, viewport.x, viewport.y);
                Check(Hit(150f, ScreenLayout.Y(774f)) < 0, "Standalone cancel removed at " + viewport);
                Check(Hit(270f, ScreenLayout.Y(774f)) == 9, "Centred confirm aligns at " + viewport);
                Check(Hit(433f, ScreenLayout.Y(916f)) == 12, "Bomb prop cancels at " + viewport);
                Check(Hit(501f, ScreenLayout.Y(876f)) == 12 && Hit(513f, ScreenLayout.Y(885f)) == 12,
                    "Cancel badge hits above and beside prop at " + viewport);
                Check((string)Get(game.gardenPage.bombPropWord, "rendered") == "取消"
                    && (string)Get(game.gardenPage.propAmounts[2], "rendered") == "×", "Aiming shows Cancel and × at " + viewport);
                Check(!game.gardenPage.propCancelLabels[2].activeSelf, "Duplicate bomb cancel hint hidden at " + viewport);
                Check(Hit(270f, ScreenLayout.Y(400f)) < 0, "Board stays available for aiming at " + viewport);
            }
            int count = game.Economy.Data.items[2], used = game.ItemsUsed;
            Click(433f, ScreenLayout.Y(916f));
            Check((int)Get(game, "selection") == 0 && !(bool)Get(game, "paused"), "Cancel resumes battle");
            Check(game.Economy.Data.items[2] == count && game.ItemsUsed == used && game.Battle.PendingStrike == null, "Cancel does not consume or launch a bomb");
            Capture("after-cancel", 1080, 1920);
            Check((string)Get(game.gardenPage.bombPropWord, "rendered") == "泡泡炸弹"
                && (string)Get(game.gardenPage.propAmounts[2], "rendered") == count.ToString()
                && Hit(501f, ScreenLayout.Y(876f)) != 12, "Cancel restores prop name/count and disables extra badge target");
            Click(433f, ScreenLayout.Y(916f));
            Capture("aim-again", 1080, 1920);
            Click(501f, ScreenLayout.Y(876f));
            Check((int)Get(game, "selection") == 0 && !(bool)Get(game, "paused") && game.Battle.PendingStrike == null,
                "Clicking upper part of × exits aiming");
            Check(game.Economy.Data.items[2] == count && game.ItemsUsed == used, "Badge cancel does not consume a bomb");
            Capture("after-badge-cancel", 1080, 1920);
            Click(433f, ScreenLayout.Y(916f));
            Capture("aim-before-confirm", 1080, 1920);
            var aim = (Vector2)Get(game, "aim");
            game.PointerDown(aim); game.PointerMove(aim + new Vector2(12f, 0f)); game.PointerUp(aim + new Vector2(12f, 0f));
            Check((int)Get(game, "selection") == 2 && game.Economy.Data.items[2] == count && (Vector2)Get(game, "aim") != aim, "Drag moves aim without confirming");
            Capture("after-drag", 1080, 1920);
            Click(270f, ScreenLayout.Y(774f));
            Check((int)Get(game, "selection") == 0 && !(bool)Get(game, "paused") && game.Battle.PendingStrike != null, "Confirm launches bomb and resumes battle");
            Check(game.Economy.Data.items[2] == count - 1 && game.ItemsUsed == used + 1, "Confirm consumes exactly one bomb");
            Capture("after-confirm", 1080, 1920);
            Check((string)Get(game.gardenPage.bombPropWord, "rendered") == "泡泡炸弹"
                && (string)Get(game.gardenPage.propAmounts[2], "rendered") == (count - 1).ToString(), "Confirm restores prop label and updated count");
        }
        static void FreshBattle()
        {
            game.Economy.Data.energy = 5;
            Check(game.StartSortie(6), "Start level 7 with isolated profile");
            for (int n = 0; n < 600 && game.Battle.State == Phase.Intro; n++) game.Battle.Tick(.02f);
            Set(game, "toast", ""); Set(game, "lesson", null); Set(game, "transitionActive", false);
            Set(game, "streakPrompt", false); Set(game, "energyOpen", false); Set(game, "settings", false);
        }
        static int Hit(float x, float screenY) => (int)Call("HitTest", new Vector2(x, ScreenLayout.InverseY(screenY)));
        static void Click(float x, float screenY) => game.PointerDown(new Vector2(x, ScreenLayout.InverseY(screenY)));
        static void Capture(string name, int width, int height)
        {
            var camera = (Camera)Get(game, "gameCamera"); var previous = camera.targetTexture;
            var rt = new RenderTexture(width, height, 24); var active = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int remaining = game.Battle.Remaining; var phase = game.Battle.State;
            try {
                rt.Create(); camera.targetTexture = rt;
                Set(game, "evidenceViewport", new Vector2Int(width, height)); Call("ApplyDisplay", width, height);
                game.gardenPage.ApplyLayout(); game.RefreshGardenUi(game.gardenPage);
                ((PixelSurface)Get(game, "surface")).Redraw(); ((PixelSurface)Get(game, "hud")).Redraw();
                ((PixelSurface)Get(game, "gardenOverlay")).Redraw();
                Canvas.ForceUpdateCanvases(); game.Effects.Relayout(); camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); report.screenshots.Add(name + ".png");
                Check(game.Battle.Remaining == remaining && game.Battle.State == phase, "Rendering preserves model: " + name);
            } finally {
                camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
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
