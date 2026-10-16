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
    // CLI-only isolated rich-profile scenario. Drives existing pointer handlers
    // through actual rendered hit regions, not OS input or progression unlocks.
    public sealed class ShopWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, returnedHome;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, coins, fragments, adCoins, adFragments, offerClaimed, cores;
            public int[] items;
            public List<string> screenshots = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private string output;
        private Report report;
        private float started, next;
        private int stage;
        private bool finished;
        private PixelWingGame game;
        private readonly Queue<int> actions = new Queue<int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-shop-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Shop probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI shop window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<ShopWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 40f) throw new TimeoutException("Shop route timed out");
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        game.Economy.Data.coins = 20000; game.Economy.Data.offerSeed = 12345; game.Economy.Data.unlocked = 3;
                        typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game, null);
                        stage++; next = now + .8f; return;
                    case 1:
                        Tap(122); stage++; next = now + .8f; return;
                    case 2:
                        if (game.Page != DeckPage.Shop) throw new Exception("Shop did not open");
                        Capture("01-shop");
                        foreach (int id in new[] { 210, 211, 212, 213, 216, 214, 214, 214, 214, 214, 214, 215, 215, 215, 215 }) actions.Enqueue(id);
                        stage++; next = now + .25f; return;
                    case 3:
                        if (actions.Count > 0) { Tap(actions.Dequeue()); next = now + .25f; return; }
                        FleetProfile d = game.Economy.Data;
                        if (d.coins != 11860 || d.fragments != 45 || d.items[0] != 5 || d.items[1] != 5 || d.items[2] != 4 || d.adCoins != 5 || d.adFragments != 3)
                            throw new Exception("Purchase/daily reward totals differ");
                        Capture("02-purchases");
                        for (int i = 0; i < 4; i++) { int count = game.Economy.OfferAdsNeeded(i); for (int a = 0; a < count; a++) actions.Enqueue(540 + i); }
                        stage++; next = now + .25f; return;
                    case 4:
                        if (actions.Count > 0) { Tap(actions.Dequeue()); next = now + .4f; return; }
                        if (game.Economy.Data.offerClaimed != 15 || game.Economy.Data.cores.Count != 1) throw new Exception("Offer rewards missing");
                        int revision = game.Economy.Data.revision;
                        for (int id = 540; id < 544; id++) Tap(id);
                        if (revision != game.Economy.Data.revision) throw new Exception("Repeated offer tap committed twice");
                        stage++; next = now + .8f; return;
                    case 5:
                        Capture("03-all-claimed"); Tap(121); stage++; next = now + .8f; return;
                    default:
                        if (game.Page != DeckPage.Bridge || !game.InHome) throw new Exception("Return to bridge failed");
                        report.returnedHome = true;
                        Capture("04-home"); Finish(null); return;
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
                FleetProfile d = game.Economy.Data; report.coins = d.coins; report.fragments = d.fragments; report.items = d.items;
                report.adCoins = d.adCoins; report.adFragments = d.adFragments; report.offerClaimed = d.offerClaimed; report.cores = d.cores.Count;
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
