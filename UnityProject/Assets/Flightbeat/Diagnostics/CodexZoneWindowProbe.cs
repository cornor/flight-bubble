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
    // Explicit isolated inventory/RNG fixture. All subsequent transactions use
    // rendered hit regions and game pointer handlers, never direct UI commands.
    public sealed class CodexZoneWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, coins, fragments, zoneClaimed, ads, grantedPart, spareCopies;
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
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-codex-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Codex/zone probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI codex/zone window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<CodexZoneWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 90f) throw new TimeoutException("Codex/zone route timed out at " + stage);
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        var economy = new FleetEconomy(new FleetProfile { unlocked = 70, offerSeed = 1 }, () => 1791604800, d => { }, n => 0);
                        FleetProfile d = economy.Data;
                        d.growthTutorial = true; d.starterGift = true; d.fragments = 50; d.coins = 100; d.dayAds = 0; d.zoneClaimed = 0;
                        d.cleared.Clear(); for (int i = 1; i <= 32; i++) d.cleared.Add(i);
                        Array.Clear(d.parts, 5, 20); Array.Clear(d.spares, 0, 25); d.parts[6] = 1; d.assembled = 1; d.revision++;
                        typeof(PixelWingGame).GetProperty("Economy").SetValue(game, economy);
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null); Advance(now); return;
                    case 1: Tap(554); Advance(now); return;
                    case 2:
                        Check(Overlay == DeckOverlay.Zone, "Home card prioritizes pending zone reward"); Capture("01-zone-reward"); Tap(701); Advance(now); return;
                    case 3:
                        Check(game.Economy.Data.coins == 3100 && game.Economy.Data.fragments == 170 && game.Economy.Data.zoneClaimed == 1
                            && game.Economy.Data.parts[5] == 1 && game.Economy.Data.spares[5] == 2 && game.Economy.Data.dayAds == 1,
                            "Ad doubles currency, grants three parts once, counts ad");
                        Check(Overlay == DeckOverlay.None, "Claim closes reward"); Tap(554); Advance(now); return;
                    case 4:
                        Check(Overlay == DeckOverlay.Codex && game.CodexZone == -1, "No pending reward opens overview");
                        Capture("02-zone-overview"); Tap(562); Advance(now); return;
                    case 5:
                        Check(game.CodexZone == -1 && ToastText == "收复上一战区后开放", "Locked zone rejected"); Tap(561); Advance(now); return;
                    case 6:
                        Check(game.CodexZone == 1 && (int)Field("codexPage") == 1, "Zone starts on current progress page");
                        Capture("03-zone-second-page"); Tap(638); Advance(now); return;
                    case 7:
                        Check((int)Field("codexLevel") == -1 && ToastText == "推进到这一关后可查看情报", "Locked level rejected"); Tap(635); Advance(now); return;
                    case 8:
                        Check((int)Field("codexLevel") == 67, "Page row id resolves zone-relative level"); Capture("04-level-detail"); Tap(553); Advance(now); return;
                    case 9: Tap(556); Advance(now); return;
                    case 10:
                        Check((int)Field("codexPage") == 0 && (int)Field("codexLevel") == -1, "Detail closes and previous page works");
                        Capture("05-zone-first-page"); Tap(600); Advance(now); return;
                    case 11:
                        Check((int)Field("codexLevel") == 32, "First-page detail"); Capture("06-first-level-detail"); Tap(553); Advance(now); return;
                    case 12: Tap(552); Advance(now); return;
                    case 13: Tap(560); Advance(now); return;
                    case 14: Tap(600); Advance(now); return;
                    case 15:
                        Check((int)Field("codexLevel") == 0, "Zone zero includes original training entry"); Capture("07-cleared-training-detail"); Tap(553); Advance(now); return;
                    case 16: Tap(552); Advance(now); return;
                    case 17: Capture("08-overview-return"); Tap(551); Advance(now); return;
                    default:
                        Check(game.Page == DeckPage.Bridge && game.InHome && Overlay == DeckOverlay.None, "Return home"); report.returnedHome = true; Finish(null); return;
                }
            }
            catch (Exception e) { Finish("Stage " + stage + ": " + e); }
        }
        private object Field(string name) => typeof(PixelWingGame).GetField(name, Flags).GetValue(game);
        private DeckOverlay Overlay => (DeckOverlay)Field("deckOverlay");
        private string ToastText => (string)Field("toast");
        private void Advance(float now, float delay = .6f) { stage++; next = now + delay; }
        private static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        private void Tap(int id)
        {
            IList hits = (IList)typeof(PixelWingGame).GetField("uiHits", Flags).GetValue(game);
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                object hit = hits[i]; Type t = hit.GetType();
                if ((int)t.GetField("id").GetValue(hit) != id) continue;
                float x = (float)t.GetField("x").GetValue(hit), y = (float)t.GetField("y").GetValue(hit);
                float width = (float)t.GetField("w").GetValue(hit), height = (float)t.GetField("h").GetValue(hit);
                Vector2 point = id == 0 ? new Vector2(270f, ScreenLayout.InverseY(ScreenLayout.Height * .5f))
                    : new Vector2(x + width * .5f, ScreenLayout.InverseY(y + height * .5f));
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
                FleetProfile d = game.Economy.Data; report.fragments = d.fragments; report.coins = d.coins;
                report.zoneClaimed = d.zoneClaimed; report.ads = d.dayAds; report.grantedPart = d.parts[5]; report.spareCopies = d.spares[5];
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
