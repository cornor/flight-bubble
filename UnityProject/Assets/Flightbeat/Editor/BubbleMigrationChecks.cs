using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Acceptance harness only: real Awake/StartSortie/SetupSortie and page binding,
    // using a no-op save delegate and a fixed clock. Never shipped in the player.
    public static class BubbleMigrationChecks
    {
        const string Key = "FlightBubble.MigrationChecks";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Serializable] class Level { public int number, lanes, slots, total; public bool deployed; }
        [Serializable] class Report { public bool passed; public string error; public List<string> checks = new List<string>(); public List<Level> levels = new List<Level>(); public List<string> runtimeErrors = new List<string>(); }
        static Report report;
        static IEnumerator routine;
        static double started;
        static int lastFrame = -1;
        static PixelWingGame game;
        static string Output => Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "--bubble-checks") + 1];
        public static void Run()
        {
            if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("--idle-test") || !Environment.GetCommandLineArgs().Contains("--bubble-checks"))
                throw new InvalidOperationException("Batch mode, --idle-test and --bubble-checks DIRECTORY required");
            if (Directory.Exists(Output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Output);
            GardenBattleUiAssets.RefreshSprites();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity");
            EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {
            if (Application.isBatchMode && Environment.GetCommandLineArgs().Contains("--bubble-checks"))
                DevelopmentTools.Factory = _ => null; // Enables existing --idle-test isolation, without developer tools.
        }
        [InitializeOnLoadMethod] static void Install()
        {
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool(Key, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                report = new Report(); Application.logMessageReceived += Log; started = EditorApplication.timeSinceStartup; routine = Checks();
                EditorApplication.update += Tick;
            };
        }
        static void Tick()
        {
            try {
                if (EditorApplication.timeSinceStartup - started > 900) throw new TimeoutException("Migration checks timeout");
                EditorApplication.QueuePlayerLoopUpdate();
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (!routine.MoveNext()) Finish(null);
            } catch (Exception e) { Finish(e); }
        }
        static void Log(string message, string stack, LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) report.runtimeErrors.Add(message+"\n"+stack); }
        static void Finish(Exception error)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SessionState.SetBool(Key, false);
            if(error==null && report.runtimeErrors.Count>0) error=new Exception("Runtime errors: "+String.Join("\n",report.runtimeErrors));
            report.passed = error == null; report.error = error?.ToString();
            File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(report, true));
            if (error != null) Debug.LogException(error); else Debug.Log("BUBBLE_MIGRATION_CHECKS_PASSED " + report.levels.Count);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
        static IEnumerator Checks()
        {
            for (int i = 0; i < 4; i++) yield return null;
            game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            Check(game && game.gardenPage, "Real Awake creates battle-page prefab");
            Check((bool)Get("evidenceControlled"), "Isolated economy before any acceptance actions");
            Set("evidenceControlled", false); Set("applicationSuspended", false);
            var p = new FleetProfile { unlocked = 262, energy = 0, energyAt = 1800000000 };
            p.cleared.AddRange(new[] { 1, 2, 3 });
            Set("Economy", new FleetEconomy(p, () => 1800000000L, _ => { }, n => 0));
            p = game.Economy.Data;
            p.energy = 0;
            Check(!game.Economy.CanSortie, "D6 fixture starts with zero energy");
            Invoke("StartBubbleLevel");
            Check(game.LevelIndex == 3 && !game.InHome && game.Economy.Data.energy == 0, "Start selects first uncleared and tops up exactly one spent energy");
            game.Economy.Data.energy = 0; Check(!game.Economy.CanSortie, "Retry fixture has zero energy"); Invoke("Restart"); Invoke("TickTransition", 2f);
            Check(game.LevelIndex == 3 && !game.TransitionActive && game.Economy.Data.energy == 0, "Zero-energy retry succeeds through original transition");
            game.Economy.Data.energy = 0; Check(!game.Economy.CanSortie, "Next fixture has zero energy"); Invoke("NextLevel");
            Check(game.LevelIndex == 4 && game.Economy.Data.energy == 0, "Zero-energy next succeeds");
            p = game.Economy.Data; p.cleared.Clear(); p.cleared.AddRange(Enumerable.Range(1, 262)); p.energy = 0; Invoke("StartBubbleLevel");
            Check(game.LevelIndex == 261 && !game.InHome, "All cleared starts the last level");
            Set("evidenceViewport", new Vector2Int(540, 1170)); Invoke("ApplyDisplay", 540, 1170);
            for (int i = 0; i < 262; i++)
            {
                game.Economy.Data.energy = 5;
                Check(game.StartSortie(i), "StartSortie " + (i+1));
                var b = game.Battle;
                for (int tick = 0; tick < 600 && b.State == Phase.Intro; tick++) b.Tick(.02f);
                Check(b.State == Phase.Ready, "Ready " + (i+1));
                game.RefreshGardenUi(game.gardenPage); Canvas.ForceUpdateCanvases(); yield return null;
                Check(game.gardenPage.header.activeSelf && game.gardenPage.background.activeSelf, "Packaged page " + (i+1));
                Color GardenColor(int index) => (Color)game.GetType().GetMethod("GardenColor",Flags).Invoke(game,new object[]{index});
                foreach (var lane in b.Reserve) foreach (var fighter in lane)
                    Check(GardenSkin.Has("plants/" + GardenSkin.Key(GardenColor(fighter.Color)) + "/" + ColorUtility.ToHtmlStringRGB(GardenColor(fighter.Color)) + "/idle"), "Creature asset " + (i+1));
                bool deployed = b.Deploy(0);
                Check(deployed && b.State == Phase.Playing && b.Fleet.Count > 0, "Deployment " + (i+1));
                yield return null;
                report.levels.Add(new Level { number=i+1, lanes=b.LaneCount, slots=b.MaximumSlots, total=b.Total, deployed=deployed });
            }
            BubbleFxChecks.Run(game, Check, Capture);
        }
        static void Capture(string name, int height)
        {
            var camera = (Camera)Get("gameCamera");
            var previous = camera.targetTexture;
            var rt = new RenderTexture(540, height, 24);
            var image = new Texture2D(540, height, TextureFormat.RGBA32, false);
            var active = RenderTexture.active;
            try {
                rt.Create(); camera.targetTexture=rt; Invoke("ApplyDisplay",540,height); game.gardenPage.ApplyLayout();
                ((PixelSurface)Get("surface")).Redraw(); ((PixelSurface)Get("hud")).Redraw();
                Canvas.ForceUpdateCanvases(); game.Effects.Relayout(); camera.Render(); RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,540,height),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());
            } finally { camera.targetTexture=previous; RenderTexture.active=active; Invoke("ApplyDisplay",540,height); Canvas.ForceUpdateCanvases(); game.Effects.Relayout(); UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        }
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); if (!message.StartsWith("Creature asset")) report.checks.Add(message); }
        static object Get(string name) { var t=game.GetType(); return t.GetField(name,Flags)?.GetValue(game) ?? t.GetProperty(name,Flags)?.GetValue(game); }
        static void Set(string name, object value) { var t=game.GetType(); var f=t.GetField(name,Flags); if(f!=null) f.SetValue(game,value); else t.GetProperty(name,Flags).SetValue(game,value); }
        static void Invoke(string name, params object[] args) => game.GetType().GetMethod(name,Flags).Invoke(game,args);
    }
}
