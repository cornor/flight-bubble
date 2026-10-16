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
    public sealed class DailyRecycleWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, coins, fragments, coreCount, dailyClaimed, signCount, ads, bombs;
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
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-daily-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Daily/recycle probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI daily/recycle window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<DailyRecycleWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 90f) throw new TimeoutException("Daily/recycle route timed out at " + stage);
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                if (stage >= 7 && stage <= 14) { Tap(230 + stage - 7); Advance(now, .25f); return; }
                switch (stage)
                {
                    case 0:
                        var economy = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => { }, n => 0);
                        FleetProfile d = economy.Data;
                        d.growthTutorial = true; d.starterGift = true; d.fragments = 100; d.coins = 300; d.nextCore = 1000;
                        d.dayWins = 6; d.dayMerges = 80; d.dayKills = 50000; d.dayItems = 5; d.dayMaxes = 5;
                        d.dayCombo = 5000; d.dayCleanWins = 4; d.dayFastWins = 2; d.dayAds = 2; d.dailyClaimed = 0;
                        d.signCount = 6; d.signKey = economy.Today - 1;
                        d.cores.Clear(); Array.Clear(d.presets, 0, 25); Array.Clear(d.items, 0, 3);
                        for (int id = 1; id <= 10; id++) d.cores.Add(new FleetCore { id = id, kind = 0, quality = id < 9 ? 0 : 2, durability = id < 9 ? 10 : 15 });
                        d.presets[0] = 1; d.revision++;
                        typeof(PixelWingGame).GetProperty("Economy").SetValue(game, economy);
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null); Advance(now); return;
                    case 1: Tap(111); Advance(now); return;
                    case 2: Capture("01-daily-before"); Tap(238); Advance(now); return;
                    case 3:
                        Check(game.Page == DeckPage.Shop, "Incomplete ad task opens shop"); Tap(215); Advance(now); return;
                    case 4:
                        Check(game.Economy.Data.dayAds == 3 && game.Economy.Data.fragments == 105, "Real Test-ad callback increments task and grants fragments"); Tap(121); Advance(now); return;
                    case 5: Tap(111); Advance(now); return;
                    case 6: Capture("02-daily-ready"); Advance(now, .1f); return;
                    case 15: Tap(528); Advance(now); return;
                    case 16:
                        Check(game.Economy.DailyClaimedCount == 9 && game.Economy.DailyChestReady, "Nine tasks claimed and chest ready");
                        Capture("03-chest-ready"); Tap(529); Advance(now); return;
                    case 17:
                        Check(game.Economy.Data.coins == 1940 && game.Economy.Data.fragments == 221 && game.Economy.Data.items[2] == 2
                            && game.Economy.Data.dailyClaimed == 1023 && game.Economy.Data.dayAds == 5, "Task/chest reward ledger");
                        Capture("04-daily-claimed"); Tap(298); Advance(now); return;
                    case 18: Tap(112); Advance(now); return;
                    case 19: Capture("05-sign-before"); Tap(240); Advance(now, .2f); return;
                    case 20: Capture("06-sign-flip"); Advance(now); return;
                    case 21:
                        Check(game.Economy.Data.signCount == 7 && game.Economy.Data.cores.Count == 11 && !game.Economy.CanSign, "Seventh reward saved");
                        Capture("07-sign-claimed"); Tap(240); Advance(now); return;
                    case 22:
                        Check(game.Economy.Data.signCount == 7 && game.Economy.Data.cores.Count == 11, "Disabled same-day sign does not duplicate"); Tap(298); Advance(now); return;
                    case 23: Tap(120); Advance(now); return;
                    case 24: Tap(311); Advance(now); return;
                    case 25: Tap(384); Advance(now); return;
                    case 26: Capture("08-recycle-first"); Tap(775); Advance(now); return;
                    case 27: Tap(760); Advance(now); return;
                    case 28:
                        Check(Picks.Count == 1, "Second-page row toggled"); Capture("09-recycle-second"); Tap(774); Advance(now); return;
                    case 29: Tap(760); Advance(now); return;
                    case 30:
                        Check(Picks.Count == 2, "Selection retained across pages"); Tap(771); Advance(now); return;
                    case 31:
                        Check(Picks.Count == 0, "Clear selections"); Tap(770); Advance(now); return;
                    case 32:
                        Check(Picks.Count == 9 && !Picks.Contains(1) && !Picks.Contains(1000), "Duplicate selection protects preset and newest rare");
                        Capture("10-recycle-selected"); Tap(772); Advance(now); return;
                    case 33:
                        Check(game.Economy.Data.cores.Count == 2 && game.Economy.Data.fragments == 234 && game.Economy.InPreset(0, 0).id == 1, "Duplicate removal ledger and preset protection");
                        Capture("11-recycle-retained"); Tap(760); Advance(now); return;
                    case 34: Tap(772); Advance(now); return;
                    case 35:
                        Check(game.Economy.Data.cores.Count == 1 && game.Economy.Data.fragments == 237
                            && !(bool)typeof(PixelWingGame).GetField("recycleOpen", Flags).GetValue(game), "Manual last free module removal closes empty modal");
                        Tap(121); Advance(now); return;
                    default:
                        Check(game.Page == DeckPage.Bridge && game.InHome, "Return home"); report.returnedHome = true; Finish(null); return;
                }
            }
            catch (Exception e) { Finish("Stage " + stage + ": " + e); }
        }
        private HashSet<int> Picks => (HashSet<int>)typeof(PixelWingGame).GetField("recyclePick", Flags).GetValue(game);
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
                FleetProfile d = game.Economy.Data; report.fragments = d.fragments; report.coreCount = d.cores.Count;
                report.coins = d.coins; report.dailyClaimed = d.dailyClaimed; report.signCount = d.signCount; report.ads = d.dayAds; report.bombs = d.items[2];
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
