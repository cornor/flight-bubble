#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // CLI-only isolated part-inventory scenario. Drives existing pointer handlers
    // through actual rendered hit regions, not OS input or progression unlocks.
    public sealed class HangarWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, activeFighter, assembled, starterLevel, assemblies, fastForwards;
            public bool growthTutorial, starterGift;
            public int[] parts, spares;
            public List<string> screenshots = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private string output;
        private Report report;
        private float started, next;
        private int stage, fighter = 1;
        private bool finished;
        private PixelWingGame game;
        private readonly Queue<int> actions = new Queue<int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-hangar-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Hangar probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI hangar window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<HangarWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
            p.report = new Report { unityVersion = Application.unityVersion, isolatedDiagnosticProfile = true };
            Application.logMessageReceived += p.Logged;
        }
        private void Logged(string text, string stack, LogType type)
        {
            if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(text + "\n" + stack);
        }
        private void LateUpdate()
        {
            if (finished) return;
            try
            {
                float now = Time.realtimeSinceStartup;
                if (now - started > 60f) throw new TimeoutException("Hangar route timed out");
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        game.Economy.Data.unlocked = 3;
                        for (int p = 1; p < 5; p++) game.Economy.Data.spares[p] = 2;
                        for (int i = 5; i < 25; i++) game.Economy.Data.parts[i] = 1;
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null);
                        stage++; next = now + .8f; return;
                    case 1:
                        Tap(120); stage++; next = now + .8f; return;
                    case 2:
                        if (game.Page != DeckPage.Upgrade || !game.Economy.Data.starterGift || game.Economy.Spare(0, 0) != 2) throw new Exception("Hangar starter gift missing");
                        Capture("01-parts"); Tap(180); stage++; next = now + .5f; return;
                    case 3:
                        if (!game.Economy.Data.growthTutorial || game.Economy.PartLevel(0, 0) != 2) throw new Exception("First upgrade missing");
                        for (int p = 1; p < 5; p++) { actions.Enqueue(320 + p); actions.Enqueue(180); }
                        stage++; return;
                    case 4:
                        if (actions.Count > 0) { Tap(actions.Dequeue()); next = now + .3f; return; }
                        if (game.Economy.FighterLevel(0) != 2) throw new Exception("Whole-airframe LV2 missing");
                        Capture("02-upgraded"); Tap(301); stage++; next = now + .5f; return;
                    case 5:
                        Tap(331);
                        if (!game.Economy.Assembled(fighter) || (int)typeof(PixelWingGame).GetField("assemblyFighter", Flags).GetValue(game) != fighter) throw new Exception("Assembly did not start");
                        report.assemblies++; stage++; next = now + .2f; return;
                    case 6:
                        if (fighter == 1) Capture("03-assembly-start");
                        stage++; next = now + 1.15f; return;
                    case 7:
                        if (fighter == 1) Capture("04-assembly-pieces");
                        if (fighter == 2) { Tap(720); report.fastForwards++; stage++; next = now + .7f; }
                        else { stage++; next = now + 2.3f; }
                        return;
                    case 8:
                        if (fighter == 1) Capture("05-assembly-complete");
                        Tap(fighter == 2 ? 720 : 721); stage++; next = now + .5f; return;
                    case 9:
                        if ((int)typeof(PixelWingGame).GetField("assemblyFighter", Flags).GetValue(game) != -1) throw new Exception("Assembly overlay did not close");
                        if (fighter == 2) Tap(332);
                        if (game.Economy.ActiveFighter != fighter) throw new Exception("Active fighter not switched");
                        stage++; next = now + .5f; return;
                    case 10:
                        if (fighter < 4) { fighter++; Tap(300 + fighter); stage = 5; next = now + .5f; return; }
                        Capture("06-hangar-fleet"); Tap(121); stage++; next = now + .8f; return;
                    default:
                        if (game.Page != DeckPage.Bridge || !game.InHome || game.Economy.Data.assembled != 31) throw new Exception("Return to bridge/full fleet failed");
                        report.returnedHome = true; Capture("07-home"); Finish(null); return;
                }
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private void Tap(int id)
        {
            IList hits = (IList)typeof(PixelWingGame).GetField("uiHits", Flags).GetValue(game);
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                object hit = hits[i]; Type t = hit.GetType();
                if ((int)t.GetField("id").GetValue(hit) != id) continue;
                float x = (float)t.GetField("x").GetValue(hit), y = (float)t.GetField("y").GetValue(hit);
                float width = (float)t.GetField("w").GetValue(hit), height = (float)t.GetField("h").GetValue(hit);
                Vector2 point = new Vector2(x + width * .5f, ScreenLayout.InverseY(y + height * .5f));
                foreach (string method in new[] { "PointerDown", "PointerMove", "PointerUp" })
                    typeof(PixelWingGame).GetMethod(method, Flags).Invoke(game, new object[] { point });
                report.taps++; return;
            }
            throw new Exception("Missing rendered hit " + id);
        }
        private void Capture(string name)
        {
            Camera camera = game.GameCamera; int width = camera.pixelWidth, height = camera.pixelHeight;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("Native graphics required");
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture priorTarget = camera.targetTexture, priorActive = RenderTexture.active; Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0); pixels.Apply();
                string file = Path.Combine(Path.GetDirectoryName(output), name + ".png"); File.WriteAllBytes(file, pixels.EncodeToPNG()); report.screenshots.Add(file);
            }
            finally { camera.targetTexture = priorTarget; RenderTexture.active = priorActive; RenderTexture.ReleaseTemporary(target); if (pixels != null) Destroy(pixels); }
        }
        private void Finish(string error)
        {
            if (finished) return; finished = true; Application.logMessageReceived -= Logged;
            report.error = error; report.passed = error == null; report.graphicsDevice = SystemInfo.graphicsDeviceType.ToString();
            report.screenWidth = Screen.width; report.screenHeight = Screen.height;
            if (game != null && game.Economy != null)
            {
                FleetProfile d = game.Economy.Data; report.activeFighter = d.activeFighter; report.assembled = d.assembled; report.parts = d.parts; report.spares = d.spares;
                report.growthTutorial = d.growthTutorial; report.starterGift = d.starterGift; report.starterLevel = game.Economy.FighterLevel(0);
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
