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
    // CLI-only isolated module-inventory scenario. Drives existing pointer handlers
    // through actual rendered hit regions, not OS input or progression unlocks.
    public sealed class GearWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, fragments, coreCount, ads, selectedNose, wingOwner;
            public List<string> screenshots = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private string output;
        private Report report;
        private float started, next;
        private int stage;
        private bool finished;
        private PixelWingGame game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-gear-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Gear probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI gear window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<GearWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 60f) throw new TimeoutException("Gear route timed out");
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        FleetProfile d = game.Economy.Data;
                        d.unlocked = 3; d.growthTutorial = true; d.starterGift = true; d.assembled = 31; d.fragments = 100; d.nextCore = 100;
                        d.cores.Clear(); Array.Clear(d.presets, 0, 25);
                        for (int i = 0; i < 25; i++) d.parts[i] = 1;
                        for (int i = 1; i <= 3; i++) d.cores.Add(new FleetCore { id = i, kind = 0, quality = 0, durability = 1 });
                        d.cores.Add(new FleetCore { id = 4, kind = 1, quality = 1, durability = 1 }); d.presets[0] = 4;
                        d.cores.Add(new FleetCore { id = 5, kind = 3, quality = 4, durability = 24 }); d.presets[6] = 5;
                        foreach (int kind in new[] { 6, 10, 14 })
                        {
                            int id = d.cores.Count + 1; d.cores.Add(new FleetCore { id = id, kind = kind, quality = 4, durability = 24 });
                            d.presets[FleetCatalog.Modules[kind].slot] = id;
                        }
                        d.revision++;
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null);
                        stage++; next = now + .8f; return;
                    case 1: Tap(120); stage++; next = now + .8f; return;
                    case 2: Tap(311); stage++; next = now + .8f; return;
                    case 3: Capture("01-gear"); Tap(340); stage++; next = now + .8f; return;
                    case 4: Capture("02-picker-damaged"); Tap(386); stage++; next = now + .4f; return;
                    case 5:
                        if (game.Economy.InPreset(0, 0).durability != 12) throw new Exception("Repair reward missing");
                        Tap(382); stage++; next = now + .4f; return;
                    case 6:
                        if ((int)typeof(PixelWingGame).GetField("pickPage", Flags).GetValue(game) != 1) throw new Exception("Next page missing");
                        Capture("03-page-two"); Tap(381); stage++; next = now + .4f; return;
                    case 7: Tap(370); stage++; next = now + .5f; return;
                    case 8:
                        FleetCore merged = game.Economy.Data.cores.Find(c => c.id == 100);
                        if (merged == null || merged.quality != 1 || merged.durability != 12 || game.Economy.CopiesOf(0, 0) != 0) throw new Exception("Merge transaction missing");
                        Capture("04-merged"); Tap(350); stage++; next = now + .6f; return;
                    case 9:
                        if (game.Economy.InPreset(0, 0).id != 100) throw new Exception("Merged module not equipped");
                        Tap(340); stage++; next = now + .6f; return;
                    case 10: Tap(383); stage++; next = now + .4f; return;
                    case 11:
                        if (game.Economy.InPreset(0, 0) != null) throw new Exception("Unequip missing");
                        Tap(360); stage++; next = now + .4f; return;
                    case 12:
                        if (game.Economy.Data.cores.Exists(c => c.id == 100) || game.Economy.Data.fragments != 102) throw new Exception("Recycle payout missing");
                        Tap(350); stage++; next = now + .6f; return;
                    case 13:
                        if (game.Economy.InPreset(0, 0).id != 4) throw new Exception("Re-equip missing");
                        Tap(341); stage++; next = now + .7f; return;
                    case 14: Capture("05-cross-fighter"); Tap(350); stage++; next = now + .6f; return;
                    case 15:
                        if (game.Economy.OwnerOf(5) != 0 || game.Economy.InPreset(1, 1) != null) throw new Exception("Cross-fighter transfer missing");
                        Tap(312); stage++; next = now + .8f; return;
                    case 16: Capture("06-bonus"); Tap(121); stage++; next = now + .8f; return;
                    default:
                        if (game.Page != DeckPage.Bridge || !game.InHome || game.Economy.Data.cores.Count != 5) throw new Exception("Return home/inventory failed");
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
                FleetProfile d = game.Economy.Data; report.fragments = d.fragments; report.coreCount = d.cores.Count;
                report.ads = d.dayAds; report.selectedNose = d.presets[0]; report.wingOwner = game.Economy.OwnerOf(5);
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
