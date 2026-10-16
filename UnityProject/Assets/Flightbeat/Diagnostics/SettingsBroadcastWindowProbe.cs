#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Explicit isolated inventory/RNG fixture. All subsequent transactions use
    // rendered hit regions and game pointer handlers, never direct UI commands.
    public sealed class SettingsBroadcastWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, isolatedDiagnosticProfile, automaticStarted, automaticStopped, captureRestored, reviveSettingsPriorityChecked;
            public string unityVersion, graphicsDevice, error;
            public int frames, taps, screenWidth, screenHeight, coins, fragments, zoneClaimed, ads, pilotActions, pilotWins, pilotRetries;
            public List<string> screenshots = new List<string>();
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private string output;
        private Report report;
        private float started, next;
        private int stage;
        private int grantStep;
        private float autoStarted;
        private bool finished;
        private PixelWingGame game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-settings-window");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0) throw new ArgumentException("Settings/broadcast probe requires --idle-test and report path");
            string path = Path.GetFullPath(args[flag + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI settings/broadcast window check"); DontDestroyOnLoad(owner);
            var p = owner.AddComponent<SettingsBroadcastWindowProbe>(); p.output = path; p.started = Time.realtimeSinceStartup;
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
                if (now - started > 90f) throw new TimeoutException("Settings/broadcast route timed out at " + stage);
                game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Economy == null) return;
                report.frames++; if (now < next) return;
                Canvas.ForceUpdateCanvases();
                switch (stage)
                {
                    case 0:
                        var economy = new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800, d => { }, n => 0);
                        FleetProfile d = economy.Data;
                        d.growthTutorial = true; d.starterGift = true; d.fragments = 50; d.coins = 1000; d.energy = 5; d.streak = 3;
                        d.dayAds = 0; d.zoneClaimed = 0; d.cleared.Clear(); Array.Clear(d.items,0,d.items.Length);
                        d.cores.Clear(); d.cores.Add(new FleetCore { id = 77, kind = 0, quality = 0, durability = 10 });
                        Array.Clear(d.presets,0,d.presets.Length); d.presets[0] = 77; d.nextCore = 78;
                        Array.Clear(d.parts,0,25); Array.Clear(d.spares,0,25); d.revision++;
                        typeof(PixelWingGame).GetProperty("Economy").SetValue(game, economy);
                        Set("idleTest",false); typeof(PixelWingGame).GetMethod("EnterDeck", Flags).Invoke(game,null); Advance(now); return;
                    case 1: Tap(100); Advance(now); return;
                    case 2:
                        Check(Overlay == DeckOverlay.Settings, "Settings opened"); Capture("01-settings"); Tap(740); Advance(now); return;
                    case 3:
                        Check((bool)Field("devPanel"),"Version opens dev panel"); Capture("02-dev-panel"); Advance(now); return;
                    case 4:
                        if(grantStep < 12) { Tap(742+grantStep); grantStep++; next=now+.2f; return; }
                        d=game.Economy.Data;
                        Check(d.coins==101000 && d.fragments==5050 && d.energy==25 && d.streak==0 && d.items[0]==10,"Currency/items/streak buttons");
                        Check(d.cores.Count==11 && d.cores[0].durability==1 && d.unlocked==262 && game.Economy.ZoneReady(4),"Module, wear, unlock and clear buttons");
                        Check(game.Economy.UnlimitedSeconds==1800 && d.parts[5]==1 && d.spares[5]==9,"Unlimited and part buttons");
                        Capture("03-granted-resources"); Tap(741); Advance(now); return;
                    case 5:
                        Check(!(bool)Field("devPanel") && Overlay==DeckOverlay.Settings,"Done returns to settings"); Tap(253); Advance(now); return;
                    case 6:
                        // A declared second fixture chooses the first formal level; resource grants above are already checked.
                        game.Economy.Data.unlocked=3; game.Economy.Data.cleared.Clear();
                        Tap(100); Advance(now); return;
                    case 7: Tap(740); Advance(now); return;
                    case 8: Tap(754); autoStarted=now; Advance(now); return;
                    case 9:
                        if(!report.automaticStarted)
                        {
                            Check((bool)Field("broadcastAutomatic") && !game.InHome && !game.SettingsOpen && !game.Paused && !(bool)Field("devPanel"),"Automatic closes overlays and starts sortie");
                            report.automaticStarted=true;
                        }
                        if((int)Field("broadcastWins") < 1 && now-autoStarted<45f) { next=now+.5f; return; }
                        if((bool)Field("transitionActive")){next=now+.25f;return;}
                        Check((int)Field("broadcastActions")>=3,"Automatic pilot took actual game actions");
                        // Capture both source render paths, then confirm original display target/active texture are restored.
                        RenderTexture oldTarget=game.GameCamera.targetTexture, oldActive=RenderTexture.active;
                        Texture2D frame=game.CaptureFrame(540,960,null);
                        string file=Path.Combine(Path.GetDirectoryName(output),"04-auto-capture.png"); File.WriteAllBytes(file,frame.EncodeToPNG()); Destroy(frame); report.screenshots.Add(file);
                        frame=game.CaptureFrame(540,1280,new Rect(0,40,540,1200));
                        file=Path.Combine(Path.GetDirectoryName(output),"05-safe-capture.png");File.WriteAllBytes(file,frame.EncodeToPNG());Destroy(frame);report.screenshots.Add(file);
                        Check(game.GameCamera.targetTexture==oldTarget && RenderTexture.active==oldActive,"Capture restores render targets");report.captureRestored=true;
                        Advance(now); return;
                    case 10:
                        if((bool)Field("transitionActive")){next=now+.25f;return;}
                        Tap(1); Advance(now); return;
                    case 11:
                        Check(game.SettingsOpen && game.Paused,"Battle settings pause auto play"); Tap(740); Advance(now); return;
                    case 12: Capture("06-battle-dev-panel"); Tap(754); Advance(now); return;
                    case 13:
                        Check(!(bool)Field("broadcastAutomatic") && (bool)Field("devPanel") && game.Paused,"Disabling retains open panel and pause");
                        report.automaticStopped=true;Tap(741);Advance(now);return;
                    case 14: Tap(24);Advance(now);return;
                    default:
                        Check(!game.SettingsOpen && !game.Paused,"Return to manual battle");
                        // Explicit modal-priority fixture, separate from the natural automatic sortie.
                        Phase priorPhase=game.Battle.State;Set("settings",true);typeof(Battle).GetProperty("State").SetValue(game.Battle,Phase.ReviveOffer);
                        ((PixelSurface)Field("hud")).Redraw();Canvas.ForceUpdateCanvases();
                        Check(HasHit(30) && !HasHit(20),"Revive sheet takes priority over concurrently set settings flag");
                        typeof(Battle).GetProperty("State").SetValue(game.Battle,priorPhase);Set("settings",false);report.reviveSettingsPriorityChecked=true;
                        Finish(null); return;
                }
            }
            catch (Exception e) { Finish("Stage " + stage + ": " + e); }
        }
        private void Set(string name, object value) => typeof(PixelWingGame).GetField(name,Flags).SetValue(game,value);
        private object Field(string name) => typeof(PixelWingGame).GetField(name, Flags).GetValue(game);
        private DeckOverlay Overlay => (DeckOverlay)Field("deckOverlay");
        private void Advance(float now, float delay = .6f) { stage++; next = now + delay; }
        private static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        private bool HasHit(int id)
        {
            foreach(object hit in (IList)Field("uiHits")) if((int)hit.GetType().GetField("id").GetValue(hit)==id)return true;
            return false;
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
                report.zoneClaimed = d.zoneClaimed; report.ads = d.dayAds; report.pilotActions=(int)Field("broadcastActions");report.pilotWins=(int)Field("broadcastWins");report.pilotRetries=(int)Field("broadcastRetries");
            }
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
