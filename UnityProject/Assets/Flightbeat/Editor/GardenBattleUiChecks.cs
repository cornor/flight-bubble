using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Garden.UI;
using Flightbeat.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    // CLI Play-mode integration checks. Result phases are explicit UI fixtures,
    // not victory replays. --idle-test isolates the economy from real saves.
    public static class GardenBattleUiChecks
    {
        const string Key = "Flightbeat.GardenUiChecks";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Serializable] class Report { public bool passed; public string error; public List<string> checks = new List<string>(); public List<string> captures = new List<string>(); }
        [Serializable] class SavedInt { public string key; public bool exists; public int value; }
        [Serializable] class SavedPrefs { public SavedInt[] values; }
        static Report report;
        static IEnumerator routine;
        static double started;
        static int lastFrame = -1;
        static PixelWingGame game;
        static string Output => Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "--garden-ui-checks") + 1];

        public static void Run()
        {
            if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("--idle-test") || !Environment.GetCommandLineArgs().Contains("--garden-ui-checks"))
                throw new InvalidOperationException("Batch mode, --idle-test and --garden-ui-checks DIRECTORY required");
            if (Directory.Exists(Output)) throw new IOException("Fresh checks output required");
            Directory.CreateDirectory(Output);
            var saved = new SavedPrefs { values = new[] { "pw.music-muted", "pw.sfx-muted" }.Select(k => new SavedInt { key = k, exists = PlayerPrefs.HasKey(k), value = PlayerPrefs.GetInt(k) }).ToArray() };
            File.WriteAllText(Path.Combine(Output, "preferences-backup.json"), JsonUtility.ToJson(saved, true));
            GardenBattleUiAssets.RefreshSprites();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity");
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod] static void Install()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Key, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                report = new Report(); started = EditorApplication.timeSinceStartup; routine = Checks();
                EditorApplication.update += Tick;
            };
        }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - started > 120) throw new TimeoutException("Garden UI checks timeout");
                EditorApplication.QueuePlayerLoopUpdate();
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (!routine.MoveNext()) Finish(null);
            }
            catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception error)
        {
            EditorApplication.update -= Tick; SessionState.SetBool(Key, false);
            var saved = JsonUtility.FromJson<SavedPrefs>(File.ReadAllText(Path.Combine(Output, "preferences-backup.json")));
            foreach (var item in saved.values) { if (item.exists) PlayerPrefs.SetInt(item.key, item.value); else PlayerPrefs.DeleteKey(item.key); }
            PlayerPrefs.Save();
            report.passed = error == null; report.error = error?.ToString();
            File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(report, true));
            if (error != null) Debug.LogException(error); else Debug.Log("GARDEN_UI_CHECKS_PASSED " + report.checks.Count);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
        static IEnumerator Checks()
        {
            for (int i = 0; i < 4; i++) yield return null;
            game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            Check(game && game.gardenPage, "Real Awake instantiated the battle-page prefab");
            Check((bool)Get(game, "evidenceControlled"), "Isolated diagnostic economy; no live fleet-save writes");
            // Keep the already-created no-op save callback, but exercise normal UI routes.
            Set(game, "evidenceControlled", false);
            Set(game, "applicationSuspended", false);
            var page = game.gardenPage;
            foreach (int height in new[] { 960, 1170 })
            {
                Set(game, "evidenceViewport", new Vector2Int(540, height));
                Invoke(game, "ApplyDisplay", 540, height); page.ApplyLayout();
                game.RefreshGardenUi(page); Canvas.ForceUpdateCanvases(); yield return null;
                var pause = page.header.GetComponentInChildren<GardenUiAction>();
                Vector2 before = Logical((RectTransform)pause.transform);
                Check(Hit(before) == 1, "Pause hit follows rendered transform at 540x" + height);
                var rect = (RectTransform)pause.transform;
                Vector2 saved = rect.anchoredPosition; rect.anchoredPosition += new Vector2(160, 0);
                game.RefreshGardenUi(page); yield return null;
                Check(rect.anchoredPosition == saved + new Vector2(160, 0), "State binding preserves authored control position at " + height);
                Check(Hit(before) != 1 && Hit(Logical(rect)) == 1, "Moved control leaves old hit area and accepts new area at " + height);
                game.Tap(Logical(rect)); yield return null; yield return null;
                Check(game.Paused && page.pauseDialog.activeSelf && page.settingsPanel.activeSelf, "Moved pause opens settings at " + height + ": paused=" + game.Paused + ", view=" + page.pauseDialog.activeSelf + ", settings=" + Get(game, "settings") + ", suspended=" + Get(game, "applicationSuspended"));
                Capture("pause-" + height, height);
                Check(Hit(new Vector2(10, 725)) == 0, "Modal shade blocks planting through the dialog at " + height);
                Click(page.pauseDialog, 24); yield return null; yield return null;
                Check(!game.Paused && !page.pauseDialog.activeSelf, "Continue closes settings and resumes at " + height);
                rect.anchoredPosition = saved;
                Capture("teaching-" + height, height);
            }
            Set(game.Battle, "State", Phase.Won); Set(game.feedback, "endAge", .5f);
            yield return null; yield return null;
            Check(page.lessonComplete.activeInHierarchy && !page.missionResult.activeSelf, "Teaching completion uses its own prefab state");
            Capture("lesson-complete", 1170);
            // Known finite UI fixture: unlock formal stages in the isolated profile.
            game.Economy.Data.unlocked = 4;
            Check(game.StartSortie(2), "Formal-stage fixture initialized through StartSortie");
            Set(game.Battle, "State", Phase.Playing);
            for (int i = 0; i < 4; i++) yield return null;
            Check(page.propButtons.activeSelf && !page.tutorialHint.activeSelf, "Formal stage shows prop controls");
            Capture("formal-controls", 1170);
            Click(page.header, 1); yield return null; yield return null;
            bool oldMusic = game.Audio.MusicMuted; Click(page.pauseDialog, 20); yield return null;
            Check(game.Audio.MusicMuted != oldMusic && page.toggleIcons[0].sprite == (game.Audio.MusicMuted ? page.toggleOff : page.toggleOn), "Music action and toggle state stay linked");
            bool oldSound = game.Audio.EffectsMuted; Click(page.pauseDialog, 21); yield return null;
            Check(game.Audio.EffectsMuted != oldSound, "Sound toggle uses existing action");
            bool oldEffects = game.Effects.Enabled; Click(page.pauseDialog, 22); yield return null;
            Check(game.Effects.Enabled != oldEffects, "Effects toggle uses existing action");
            Click(page.pauseDialog, 24); yield return null;
            Click(page.controls, 11); yield return null; yield return null;
            Check((int)Get(game, "selection") == 1 && game.Paused && page.propCancelLabels[1].activeSelf, "Priority planting selection and cancel badge");
            Click(page.controls, 11); yield return null;
            Check((int)Get(game, "selection") == 0 && !game.Paused, "Priority planting cancels through same action");
            Click(page.controls, 12); yield return null; yield return null;
            Check((int)Get(game, "selection") == 2 && page.propCancelLabels[2].activeSelf, "Water targeting selection and cancel badge");
            Capture("water-targeting", 1170);
            Click(page.controls, 12); yield return null;
            int stock = game.Economy.Data.items[0]; Click(page.controls, 10); yield return null;
            Check(game.Economy.Data.items[0] == stock - 1, "Shuffle consumes exactly one item (no duplicate UI dispatch)");

            Set(game.Battle, "State", Phase.ReviveOffer);
            yield return null; yield return null;
            Check(!page.pauseDialog.activeSelf && !page.resultDialog.activeSelf, "Revive offer keeps fixed dialogs hidden");
            Check(Hit(new Vector2(38, 30)) == 0, "Legacy revive shade covers prefab header");
            Check(Hit(new Vector2(270, 644)) == 30 && Hit(new Vector2(270, 712)) == 31, "Revive actions retain their hit regions on the higher layer");
            Capture("revive-overlay", 1170);
            game.Tap(new Vector2(270, 712)); yield return null;
            if ((bool)Get(game, "streakPrompt")) { game.Tap(new Vector2(270, 630)); yield return null; }
            Check(game.Battle.State == Phase.Lost, "Declining revive routes through existing logic to loss");
            // Terminal phase injection only checks presentation/action routing.
            Set(game.Battle, "State", Phase.Lost); Set(game.feedback, "endAge", 1f);
            game.RefreshGardenUi(page); yield return null; yield return null;
            Check(page.resultDialog.activeSelf && page.missionResult.activeSelf && !page.rewardGroup.activeSelf, "Loss result shows retry without win rewards");
            Check(Hit(new Vector2(38, 30)) == 0, "Result shade blocks header controls");
            Capture("loss-result", 1170);
            Click(page.resultDialog, 40); yield return null;
            Check((bool)Get(game, "transitionActive"), "Result retry enters existing transition");
            while ((bool)Get(game, "transitionActive")) yield return null;
            Check(game.Battle.State != Phase.Lost && !page.resultDialog.activeSelf, "Retry initializes battle and removes terminal dialog");
            Set(game.Battle, "State", Phase.Won); Set(game.feedback, "endAge", 1f);
            for (int i = 0; i < 4; i++) yield return null;
            Check(page.missionResult.activeInHierarchy && page.rewardGroup.activeSelf, "Win result binds existing settled rewards");
            Capture("win-result", 1170);
            Click(page.resultDialog, 40); yield return null;
            while ((bool)Get(game, "transitionActive")) yield return null;
            Check((int)Get(game, "levelIndex") == 3, "Next-level button uses existing progression");
            Click(page.header, 1); yield return null; yield return null;
            Click(page.pauseDialog, 25); yield return null;
            if ((bool)Get(game, "streakPrompt"))
            {
                yield return null;
                Check(Hit(new Vector2(270, 630)) == 510 && Hit(new Vector2(38, 30)) == 0, "Streak confirmation covers pause and accepts its own action");
                Capture("streak-overlay", 1170);
                game.Tap(new Vector2(270, 630)); yield return null;
            }
            while ((bool)Get(game, "transitionActive")) yield return null;
            Check(game.InHome && !page.header.activeSelf && !page.background.activeSelf, "Return home hides all garden prefab layers");
            if (Environment.GetCommandLineArgs().Contains("--garden-fx-checks")) GardenFxChecks.Run(game, Check, Capture);
        }
        static Vector2 Logical(RectTransform rect)
        {
            Vector3 local = game.hudRect.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return new Vector2(local.x, ScreenLayout.InverseY(ScreenLayout.Height - local.y));
        }
        static int Hit(Vector2 point) => (int)Invoke(game, "HitTest", point);
        static void Click(GameObject root, int id)
        {
            var action = root.GetComponentsInChildren<GardenUiAction>(true).First(a => a.actionId == id && a.isActiveAndEnabled);
            Vector2 point = Logical((RectTransform)action.transform);
            Check(Hit(point) == id, "Visible action " + id + " is topmost at its center"); game.Tap(point);
        }
        static void Check(bool pass, string label) { if (!pass) throw new Exception(label); report.checks.Add(label); }
        static object Get(object target, string name)
            => target.GetType().GetField(name, Flags)?.GetValue(target) ?? target.GetType().GetProperty(name, Flags)?.GetValue(target);
        static void Set(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, Flags) ?? target.GetType().GetField("<" + name + ">k__BackingField", Flags);
            if (f != null) f.SetValue(target, value); else target.GetType().GetProperty(name, Flags).SetValue(target, value);
        }
        static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
        static void Capture(string name, int height)
        {
            var camera = game.GameCamera; var previous = camera.targetTexture;
            var rt = RenderTexture.GetTemporary(540, height, 24, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active; Texture2D image = null;
            try
            {
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                image = new Texture2D(540, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 540, height), 0, 0); image.Apply();
                string file = name + ".png"; File.WriteAllBytes(Path.Combine(Output, file), image.EncodeToPNG()); report.captures.Add(file);
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); if (image) UnityEngine.Object.Destroy(image); }
        }
    }
}
