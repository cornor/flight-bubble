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
    public sealed class SupplyWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, fragments, coreCount, partDraws, draws, smelts, spareNose, duplicateEngine, equippedQuality;
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
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-supply-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Supply probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI supply window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<SupplyWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 90f) throw new TimeoutException("Supply route timed out at " + stage);
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        var e = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => { },
                            n => n == 25 ? 9 : n == 5 ? 4 : 0);
                        FleetProfile d = e.Data;
                        d.growthTutorial = true; d.starterGift = true; d.assembled = 1; d.fragments = 1000; d.nextCore = 1000;
                        d.cores.Clear(); Array.Clear(d.presets, 0, 25); Array.Clear(d.parts, 0, 25); Array.Clear(d.spares, 0, 25);
                        for (int i = 0; i < 14; i++) if (i != 9) d.parts[i] = 1;
                        d.spares[1] = 6; d.epicPity = 9; d.legendPity = 19; d.revision++;
                        typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e);
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null); Advance(now); return;
                    case 1: Tap(120); Advance(now); return;
                    case 2: Tap(333); Advance(now); return;
                    case 3: Tap(412); Advance(now); return;
                    case 4: Capture("01-parts-supply"); Tap(420); Advance(now, .35f); return;
                    case 5: Tap(0); Advance(now, 1f); return;
                    case 6:
                        Check(game.PartResults.Length == 1 && game.PartResults[0].index == 9 && game.PartResults[0].first, "Targeted first-copy result");
                        Capture("02-single-result"); Tap(202); Advance(now); return;
                    case 7:
                        Check(game.Economy.CanAssemble(1), "Single draw completes fighter collection");
                        Tap(302); Advance(now); return;
                    case 8: Tap(324); Advance(now); return;
                    case 9: Tap(330); Advance(now); return;
                    case 10: Tap(471); Advance(now, .3f); return;
                    case 11: Tap(441); Advance(now, .3f); return;
                    case 12: Tap(441); Advance(now, .3f); return;
                    case 13: Tap(441); Advance(now, .3f); return;
                    case 14: Tap(474); Advance(now, .3f); return;
                    case 15:
                        Check(game.SmeltPick.Count == 2, "Selected row removes one copy"); Tap(470); Advance(now); return;
                    case 16: Capture("03-smelt-three"); Tap(472); Advance(now); return;
                    case 17:
                        Check(game.Economy.CanAssemble(2) && game.Economy.Data.smelts == 1 && game.Economy.Data.spares[1] == 3, "Smelt first copy transaction");
                        Tap(300); Advance(now); return;
                    case 18: Tap(320); Advance(now); return;
                    case 19: Tap(330); Advance(now); return;
                    case 20: Tap(472); Advance(now); return;
                    case 21:
                        Check(game.Economy.Data.parts[0] == 1 && game.Economy.Data.spares[0] == 1 && game.Economy.Data.spares[1] == 0, "Smelt spare without implicit upgrade");
                        Tap(333); Advance(now); return;
                    case 22: Tap(410); Advance(now); return;
                    case 23: Tap(421); Advance(now, 3.2f); return;
                    case 24:
                        Check(game.PartResults.Length == 10 && !game.PartResults[0].first && game.Economy.Data.spares[9] == 10, "Untargeted repeated ten draw");
                        Capture("04-ten-parts"); Tap(201); Advance(now, 3.2f); return;
                    case 25:
                        Check(game.Economy.Data.spares[9] == 20 && game.Economy.Data.partDraws == 21, "Repeat ten draws and saves"); Tap(202); Advance(now); return;
                    case 26: Tap(311); Advance(now); return;
                    case 27: Tap(334); Advance(now); return;
                    case 28: Tap(400); Advance(now, .3f); return;
                    case 29: Tap(401); Advance(now); return;
                    case 30: Capture("05-module-supply"); Tap(421); Advance(now, 3.2f); return;
                    case 31:
                        Check(game.GachaResults.Length == 10 && game.GachaResults[0].quality == 4, "Legendary guarantee in ten-module draw");
                        Capture("06-ten-modules"); Tap(202); Advance(now); return;
                    case 32: Capture("07-drawn-module-picker"); Tap(350); Advance(now); return;
                    case 33:
                        FleetCore equipped = game.Economy.InPreset(1, FleetCatalog.Modules[FleetCatalog.DrawableModules[0]].slot);
                        Check(equipped != null && equipped.quality == 4, "Result opens picker and equips legendary");
                        report.equippedQuality = equipped.quality; Tap(121); Advance(now); return;
                    default:
                        Check(game.Page == DeckPage.Bridge && game.InHome && game.Economy.Data.fragments == 720 && game.Economy.Data.draws == 10
                            && game.Economy.Data.cores.Count == 10 && game.Economy.Data.smelts == 2, "Final ledger and return home");
                        report.returnedHome = true; Finish(null); return;
                }
            }
            catch (Exception e) { Finish("Stage " + stage + ": " + e); }
        }
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
                report.partDraws = d.partDraws; report.draws = d.draws; report.smelts = d.smelts; report.spareNose = d.spares[0]; report.duplicateEngine = d.spares[9];
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
