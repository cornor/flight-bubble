#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // One stage-level CLI Play check. Drives the source pointer path through real hit regions;
    // no model edits, timing overrides or original-output tuning. With a native
    // graphics device, save a few camera renders for stage-level visual review.
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public sealed partial class TeachingHudPlayProbe : MonoBehaviour
    {
        private const string Key = "Flightbeat.TeachingHudPlayProbe";
        private static readonly string[] Saves = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b" };
        [Serializable] private sealed class SavedValue { public string key, value; public bool present; }
        [Serializable] private sealed class Backup { public SavedValue[] values; }
        [Serializable] private sealed class Report
        {
            public FreshGrowthResult freshGrowth;
            public bool passed, nativePlay, pauseChecked, resumed, helpChecked, trainingWon, returnedHome, saveRestored;
            public bool pixelEquivalenceChecked;
            public bool chainedToTraining02, training02Won, pointerCoordinatesUsed;
            public bool formalEntered, formalIntroDrawn, formalReady, shuffleChecked, designateChecked, designateCancelChecked;
            public bool streakExitConfirmed;
            public bool bombAimPaused, bombCancelled, bombDragChecked, bombEmptyDenied, bombLaunched, bombDetonated, bombTutorialSaved;
            public bool bombEmptyAvailable;
            public bool formalWon, formalRewardChecked, resultDrawn, nextFormalEntered;
            public bool finaleGuideDrawn, finaleGuidePaused, finaleLaunched, finaleBeamDrawn, finaleTutorialSaved;
            public int finaleKills;
            public int formalRewardCoins, formalRewardFragments;
            public bool reviveOfferDrawn, reviveStreakPrompt, revived, rescueMissileAimed, rescueMissileLaunched, rescueMissileDetonated;
            public bool secondBreachLost, lossDrawn, retryEntered, declineLost, lossRewardUnchanged;
            public int revivePurgeKills, rescueMissileKills;
            public int bombKills, bombCount;
            public float maximumPointerRoundTripError;
            public int frames, taps, maximumVertices, meshesObserved;
            public string unityVersion, graphicsDevice, error;
            public int screenWidth, screenHeight;
            public List<string> events = new List<string>();
            public List<string> cameraFrames = new List<string>();
        }
        private static Report report;
        private static double started, stageAt, nextTap;
        private static int stage, lastFrame = -1;
        private static bool finishing;
        private static float pausedTime;
        private static bool lessonChain, formalItems, superStrike, bombFlightCaptured, missionResult, reviveFlow;
        private static int designateInventory;
        private static int bombInventory, bombItemsUsed, bombCountBefore, bombKillsBefore;
        private static Vector2 bombBestAim;
        private static int coinsBeforeWin, fragmentsBeforeWin, winsBeforeWin, streakBeforeWin, wonLevel;
        private static string wonAttempt;
        private static Battle failedBattle;
        private static int failedLevel;
        private static int beforeLossCoins, beforeLossFragments, beforeLossWins, beforeLossStreak, beforeRescueItems, beforeRescueKills;
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static string nativeOutput;
        private static double Now
        {
            get
            {
                #if UNITY_EDITOR
                return EditorApplication.timeSinceStartup;
                #else
                return Time.realtimeSinceStartupAsDouble;
                #endif
            }
        }
        private static string Output
        {
            get
            {
                #if UNITY_EDITOR
                return SessionState.GetString(Key + ".path", "");
                #else
                return nativeOutput;
                #endif
            }
        }
        #if UNITY_EDITOR
        static TeachingHudPlayProbe() { if (SessionState.GetBool(Key, false)) Attach(); }
        #else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-play-report") < 0) return;
            try
            {
                Run();
                var host = new GameObject("CLI teaching scenario");
                DontDestroyOnLoad(host);
                host.AddComponent<TeachingHudPlayProbe>();
            }
            catch (Exception e) { Debug.LogException(e); Application.Quit(1); }
        }
        private void LateUpdate() => Tick();
        #endif

        public static void Run()
        {
            #if UNITY_EDITOR
            if (!Application.isBatchMode) throw new InvalidOperationException("Editor probe requires batch mode.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Probe requires a fresh editor.");
            #endif
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "--flightbeat-play-report");
            if (option < 0 || option + 1 >= args.Length) throw new ArgumentException("--flightbeat-play-report required");
            string output = Path.GetFullPath(args[option + 1]);
            ConfigureFreshGrowth(args);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (File.Exists(output) || File.Exists(output + ".save-backup.json"))
                throw new InvalidOperationException("Use a fresh output path; never overwrite a save backup.");
            var backup = new Backup { values = new SavedValue[Saves.Length] };
            for (int i = 0; i < Saves.Length; i++)
                backup.values[i] = new SavedValue { key = Saves[i], present = PlayerPrefs.HasKey(Saves[i]), value = PlayerPrefs.GetString(Saves[i], "") };
            File.WriteAllText(output + ".save-backup.json", JsonUtility.ToJson(backup, true));
            nativeOutput = output;
            #if UNITY_EDITOR
            SessionState.SetString(Key + ".path", output);
            SessionState.SetBool(Key + ".finished", false);
            SessionState.SetBool(Key, true);
            #endif
            try
            {
                foreach (string key in Saves) PlayerPrefs.DeleteKey(key);
                InstallFreshGrowthSave();
                PlayerPrefs.Save();
                #if UNITY_EDITOR
                EditorSceneManager.OpenScene("Assets/Flightbeat/Scenes/PixelWing.unity", OpenSceneMode.Single);
                Attach();
                EditorApplication.EnterPlaymode();
                #else
                Attach();
                #endif
            }
            catch (Exception e) { Finish(e.ToString()); }
        }

        private static void Attach()
        {
            ConfigureFreshGrowth(Environment.GetCommandLineArgs());
            lessonChain = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-lesson-chain") >= 0;
            formalItems = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-formal-items") >= 0;
            superStrike = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-super-strike") >= 0;
            missionResult = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-mission-result") >= 0;
            reviveFlow = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-revive-flow") >= 0;
            superStrike |= missionResult;
            formalItems |= superStrike || reviveFlow;
            lessonChain |= formalItems || freshGrowth;
            report = new Report { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString() };
            if (freshGrowth)
            {
                report.freshGrowth = new FreshGrowthResult { resumed = freshCarry != null };
                stage = freshCarry == null ? 0 : 100;
            }
            started = Now;
            #if UNITY_EDITOR
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= PlayChanged;
            EditorApplication.playModeStateChanged += PlayChanged;
            #endif
            Application.logMessageReceived -= Logged;
            Application.logMessageReceived += Logged;
        }
        #if UNITY_EDITOR
        private static void PlayChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + ".finished", false)) RestoreAndExit();
        }
        #endif
        private static void Logged(string message, string stack, LogType type)
        {
            if (!finishing && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Finish(message + "\n" + stack);
        }
        private static void Tick()
        {
            #if UNITY_EDITOR
            if (SessionState.GetBool(Key + ".finished", false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreAndExit();
                return;
            }
            #endif
            if (finishing) return;
            try
            {
                if (Now - started > (reviveFlow ? 600d : missionResult ? 180d : 90d)) throw new TimeoutException("CLI stage exceeded its wall-clock limit.");
                if (!Application.isPlaying || Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                PixelWingGame game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
                if (game == null || game.Battle == null) return;
                report.nativePlay = true;
                report.frames++;
                Canvas.ForceUpdateCanvases();
                if (finishing) return;
                foreach (PixelSurface surface in UnityEngine.Object.FindObjectsOfType<PixelSurface>())
                {
                    Mesh readback = surface.canvasRenderer.GetMesh();
                    if (readback == null) continue;
                    if (readback.vertexCount > 0) report.meshesObserved++;
                    report.maximumVertices = Math.Max(report.maximumVertices, readback.vertexCount);
                }
                double now = Now;
                if (freshGrowth && TickFreshGrowth(game, now)) return;
                switch (stage)
                {
                    case 0:
                        if (game.InHome || game.Battle.Level.lesson != 1 || game.Lesson == null)
                            throw new InvalidOperationException("Fresh save did not enter the original training01 path.");
                        if (report.frames < 12) return;
                        if (freshGrowth) ObserveDefaultProfile(game);
                        #if !UNITY_EDITOR
                        if (Screen.width != 540 || Screen.height != 960) throw new InvalidOperationException("Expected native 540x960, got " + Screen.width + "x" + Screen.height);
                        #endif
                        Capture(game, "01-teaching-ready");
                        Click(game, 1);
                        if (!game.Paused) throw new InvalidOperationException("Pause hit did not pause.");
                        pausedTime = game.Battle.Time;
                        report.pauseChecked = true; stageAt = now; stage = 1;
                        report.events.Add("fresh-save training01; pause via HUD hit region");
                        break;
                    case 1:
                        if (game.Battle.Time != pausedTime) throw new InvalidOperationException("Battle time advanced while paused.");
                        if (now - stageAt < .5d) return;
                        Capture(game, "02-paused");
                        Click(game, 24);
                        if (game.Paused) throw new InvalidOperationException("Resume hit remained paused.");
                        report.resumed = true; stage = 2;
                        report.events.Add("pause clock frozen; resumed via settings button");
                        break;
                    case 2:
                        if (game.Battle.State == Phase.Won)
                        {
                            if (!report.helpChecked) throw new InvalidOperationException("Won without exercising practice help and hand hint.");
                            report.trainingWon = true; stageAt = now; stage = 3;
                            report.events.Add("training01 won; merges=" + game.Battle.Merges + "; seconds=" + game.Battle.Time);
                            return;
                        }
                        if (now < nextTap) return;
                        if (report.helpChecked && !report.events.Contains("help draw interval completed"))
                        {
                            Capture(game, "03-practice-help");
                            report.events.Add("help draw interval completed");
                        }
                        if (game.Lesson != null && game.Lesson.Practice && !report.helpChecked)
                        {
                            Click(game, 50);
                            if (!game.Lesson.HelpUsed) throw new InvalidOperationException("Help hit not applied.");
                            report.helpChecked = true; nextTap = now + .5d;
                            report.events.Add("practice help requested; hand hint allowed to draw");
                            return;
                        }
                        int lane = game.Lesson != null && !game.Lesson.Complete ? game.Lesson.SuggestedLane : game.Battle.SuggestedQueue();
                        if (lane >= 0) Pointer(game, new Vector2(game.Battle.QueuePositionX(lane), 725f));
                        nextTap = now + .16d;
                        break;
                    case 3:
                        if (lessonChain)
                        {
                            if (game.Battle.Level.lesson != 2) return;
                            report.chainedToTraining02 = true;
                            report.events.Add("original automatic handoff entered training02");
                            Capture(game, "06-training02-ready"); stage = 6;
                            return;
                        }
                        // Exercise the original teaching victory overlay before
                        // its 1.9-second automatic handoff to training02.
                        if (now - stageAt < .7d) return;
                        Capture(game, "04-teaching-won");
                        Click(game, 1); stageAt = now; stage = 4;
                        break;
                    case 4:
                        if (now - stageAt < .4d) return;
                        Click(game, 25); stageAt = now; stage = formalItems ? 12 : 5;
                        break;
                    case 5:
                        if (!game.InHome) return;
                        if (now - stageAt < 1.5d) return;
                        Capture(game, "05-home");
                        report.returnedHome = true;
                        report.events.Add(reviveFlow ? "returned to original home via loss result button" : "returned to original home via settings action");
                        if (report.maximumVertices <= 0) throw new InvalidOperationException("No native UI mesh observed.");
                        if (freshGrowth) { BeginFreshGrowth(game, now); break; }
                        Finish(null);
                        break;
                    case 6:
                        if (game.Battle.State == Phase.Won)
                        {
                            report.training02Won = true; stageAt = now; stage = 7;
                            report.events.Add("training02 won; merges=" + game.Battle.Merges + "; seconds=" + game.Battle.Time);
                            return;
                        }
                        if (now < nextTap) return;
                        int nextLane = game.Lesson != null && !game.Lesson.Complete ? game.Lesson.SuggestedLane : game.Battle.SuggestedQueue();
                        if (nextLane >= 0) Pointer(game, new Vector2(game.Battle.QueuePositionX(nextLane), 725f));
                        nextTap = now + .16d;
                        break;
                    case 7:
                        if (formalItems)
                        {
                            if (game.Battle.Level.lesson > 0) return;
                            report.formalEntered = true;
                            report.events.Add("original automatic handoff entered first formal level: " + game.Battle.Level.title);
                            stage = 8;
                            return;
                        }
                        if (now - stageAt < .7d) return;
                        Capture(game, "07-training02-won");
                        Click(game, 1); stageAt = now; stage = 4;
                        break;
                    case 8:
                        if (game.Battle.State == Phase.Intro)
                        {
                            float ratio = game.Battle.IntroTime / game.Battle.Level.introDuration;
                            if (!report.formalIntroDrawn && ratio >= .5f && ratio < .66f)
                            {
                                Capture(game, "08-formal-warning");
                                report.formalIntroDrawn = true;
                            }
                            return;
                        }
                        if (game.Battle.State != Phase.Ready || !report.formalIntroDrawn)
                            throw new InvalidOperationException("Formal introduction did not progress through warning to Ready.");
                        Capture(game, "09-formal-ready");
                        int readyItems = game.Economy.Data.items[0];
                        Click(game, 10);
                        if (game.Economy.Data.items[0] != readyItems || game.Battle.Notice != "先点击战机出击")
                            throw new InvalidOperationException("Ready-phase prop guard did not preserve inventory and show source notice.");
                        report.formalReady = true;
                        int formalLane = game.Battle.SuggestedQueue();
                        if (formalLane < 0) throw new InvalidOperationException("First formal level has no suggested deployment.");
                        Pointer(game, new Vector2(game.Battle.QueuePositionX(formalLane), 725f));
                        if (reviveFlow)
                        {
                            beforeLossCoins = game.Economy.Data.coins; beforeLossFragments = game.Economy.Data.fragments;
                            beforeLossWins = game.Economy.Data.wins; beforeLossStreak = game.Economy.Streak;
                            beforeRescueItems = game.Economy.Data.items[2];
                            report.events.Add("formal single deployment; waiting for natural breach without further combat input");
                            stageAt = now; stage = 30;
                            return;
                        }
                        stageAt = now; stage = 9;
                        break;
                    case 9:
                        if (game.Battle.State != Phase.Playing) throw new InvalidOperationException("Formal deployment did not start battle.");
                        int beforeShuffle = game.Economy.Data.items[0], usedBefore = game.ItemsUsed;
                        var beforeQueues = new List<Fighter[]>();
                        foreach (var queue in game.Battle.Reserve) beforeQueues.Add(queue.ToArray());
                        Click(game, 10);
                        bool anyMoved = false;
                        for (int column = 0; column < beforeQueues.Count; column++)
                        {
                            Fighter[] before = beforeQueues[column];
                            if (before.Length != game.Battle.Reserve[column].Count) throw new InvalidOperationException("Shuffle changed queue lengths.");
                            for (int row = 0; row < before.Length; row++)
                            {
                                Fighter after = game.Battle.Reserve[column][row];
                                if (row >= 3 && after != before[row]) throw new InvalidOperationException("Shuffle changed a row below the original three-row range.");
                                if (row < 3 && after != before[row]) anyMoved = true;
                                if (after.Queue != column) throw new InvalidOperationException("Shuffled fighter Queue was not updated.");
                            }
                        }
                        if (!anyMoved || game.Economy.Data.items[0] != beforeShuffle - 1 || game.ItemsUsed != usedBefore + 1)
                            throw new InvalidOperationException("Shuffle did not rearrange and consume exactly one item.");
                        report.shuffleChecked = true;
                        designateInventory = game.Economy.Data.items[1];
                        Click(game, 11);
                        if (!game.Paused || game.Economy.Data.items[1] != designateInventory)
                            throw new InvalidOperationException("Designation must pause without consuming inventory.");
                        pausedTime = game.Battle.Time; stageAt = now; stage = 10;
                        break;
                    case 10:
                        if (game.Battle.Time != pausedTime) throw new InvalidOperationException("Designation did not freeze battle time.");
                        if (now - stageAt < .25d) return;
                        Capture(game, "10-designate-selection");
                        Click(game, 11);
                        if (game.Paused || game.Economy.Data.items[1] != designateInventory)
                            throw new InvalidOperationException("Cancel designation must unpause without consuming inventory.");
                        report.designateCancelChecked = true;
                        Click(game, 11);
                        for (int column = 0; column < game.Battle.LaneCount; column++)
                        {
                            if (game.Battle.Reserve[column].Count < 2) continue;
                            Fighter chosen = game.Battle.Reserve[column][1];
                            Pointer(game, new Vector2(game.Battle.QueuePositionX(column), 775f));
                            if (game.Paused || !game.Battle.Fleet.Contains(chosen) || game.Economy.Data.items[1] != designateInventory - 1)
                                throw new InvalidOperationException("Designation did not deploy the chosen rear-row fighter and consume one item.");
                            report.designateChecked = true;
                            break;
                        }
                        if (!report.designateChecked) throw new InvalidOperationException("No rear-row fighter available for designation.");
                        report.events.Add("formal controls: Ready denial; top-three-row shuffle; designation pause/cancel/deploy; exact item consumption");
                        stageAt = now; stage = 11;
                        break;
                    case 11:
                        if (now - stageAt < 1d) return;
                        Capture(game, "11-formal-combat");
                        if (superStrike)
                        {
                            bombInventory = game.Economy.Data.items[2]; bombItemsUsed = game.ItemsUsed;
                            bombCountBefore = game.Battle.SuperStrikeCount; bombKillsBefore = game.Battle.SuperStrikeKills;
                            if (bombInventory <= 0) throw new InvalidOperationException("Fresh formal route has no bomb inventory.");
                            Click(game, 12);
                            if (!game.Paused || Read<int>(game, "selection") != 2 || game.Economy.Data.items[2] != bombInventory)
                                throw new InvalidOperationException("Bomb selection did not pause without consuming inventory.");
                            pausedTime = game.Battle.Time; stageAt = now; stage = 13;
                            return;
                        }
                        Click(game, 1); stageAt = now; stage = 4;
                        break;
                    case 12:
                        if (now - stageAt < .4d) return;
                        if (!(bool)typeof(PixelWingGame).GetField("streakPrompt", PrivateInstance).GetValue(game))
                            throw new InvalidOperationException("Formal exit did not show the original streak confirmation.");
                        Capture(game, "12-streak-exit-confirmation");
                        Click(game, 510);
                        if (game.Economy.Streak != 0) throw new InvalidOperationException("Confirmed exit did not break the streak.");
                        report.streakExitConfirmed = true;
                        report.events.Add("formal exit confirmed through source streak dialog; streak cleared");
                        stageAt = now; stage = 5;
                        break;
                    case 13:
                        if (game.Battle.Time != pausedTime) throw new InvalidOperationException("Bomb aim did not freeze battle time.");
                        if (now - stageAt < .25d) return;
                        report.bombAimPaused = true;
                        Capture(game, "13-bomb-aim");
                        Click(game, 12);
                        if (game.Paused || Read<int>(game, "selection") != 0 || game.Economy.Data.items[2] != bombInventory || game.ItemsUsed != bombItemsUsed)
                            throw new InvalidOperationException("Cancelling bomb changed inventory or left aiming paused.");
                        report.bombCancelled = true;
                        Click(game, 12);
                        bombBestAim = Read<Vector2>(game, "aim");
                        // Pick an empty point by reading the live board, never changing it.
                        bool emptyFound = false;
                        for (int y = 160; y <= 500 && !emptyFound; y += 40)
                            for (int x = 28; x <= 512 && !emptyFound; x += 44)
                            {
                                float radius = 120f * (game.Battle.Loadout.bombRadius + 1f);
                                if (game.Battle.StrikeTargets((x - 270f) / ScreenLayout.EnemyScaleX + 270f,
                                    (y - 562f) / 1.31f + 562f, radius / ScreenLayout.EnemyScaleX, radius / (ScreenLayout.Stretch * 1.31f)) != 0) continue;
                                Drag(game, bombBestAim, new Vector2(x, y)); emptyFound = true;
                            }
                        report.bombEmptyAvailable = emptyFound;
                        if (!emptyFound)
                        {
                            report.events.Add("empty-target denial not covered: no empty candidate in this live board/layout; game state unchanged");
                            Drag(game, bombBestAim, new Vector2(bombBestAim.x > 270f ? 28f : 512f, 300f));
                        }
                        stageAt = now; stage = 14;
                        break;
                    case 14:
                        if (game.Battle.Time != pausedTime) throw new InvalidOperationException("Dragging bomb aim advanced battle time.");
                        if (now - stageAt < .2d) return;
                        if (report.bombEmptyAvailable)
                        {
                            Click(game, 9);
                            if (game.Battle.PendingStrike != null || !game.Paused || Read<int>(game, "selection") != 2 || game.Economy.Data.items[2] != bombInventory)
                                throw new InvalidOperationException("Empty bomb confirmation did not deny without consumption.");
                            report.bombEmptyDenied = true;
                        }
                        Drag(game, Read<Vector2>(game, "aim"), bombBestAim);
                        if ((Read<Vector2>(game, "aim") - bombBestAim).magnitude > .01f)
                            throw new InvalidOperationException("Bomb pointer drag did not move the aim to its original target.");
                        report.bombDragChecked = true;
                        Click(game, 9);
                        if (game.Battle.PendingStrike == null || game.Paused || Read<int>(game, "selection") != 0
                            || game.Economy.Data.items[2] != bombInventory - 1 || game.ItemsUsed != bombItemsUsed + 1)
                            throw new InvalidOperationException("Bomb confirmation did not launch and consume exactly one item.");
                        report.bombLaunched = true;
                        report.bombTutorialSaved = game.Economy.Data.bombTutorial;
                        if (!report.bombTutorialSaved) throw new InvalidOperationException("Bomb tutorial completion was not saved.");
                        stageAt = now; stage = 15;
                        break;
                    case 15:
                        if (game.Battle.PendingStrike != null)
                        {
                            if (!bombFlightCaptured && game.Battle.PendingStrike.Age >= .5f)
                            { Capture(game, "14-bomb-flight"); bombFlightCaptured = true; }
                            return;
                        }
                        report.bombCount = game.Battle.SuperStrikeCount - bombCountBefore;
                        report.bombKills = game.Battle.SuperStrikeKills - bombKillsBefore;
                        if (report.bombCount != 1 || report.bombKills <= 0 || game.Economy.Data.items[2] != bombInventory - 1)
                            throw new InvalidOperationException("Bomb did not detonate once with live enemy kills and unchanged post-launch inventory.");
                        report.bombDetonated = true;
                        report.events.Add("bomb: pause/cancel/drag/confirm; empty denial covered=" + report.bombEmptyDenied + "; one item consumed; kills=" + report.bombKills);
                        stageAt = now; stage = 16;
                        break;
                    case 16:
                        if (now - stageAt < .3d) return;
                        Capture(game, "15-bomb-impact"); stage = 17;
                        break;
                    case 17:
                        if (now - stageAt < 2.1d) return;
                        if (missionResult)
                        {
                            coinsBeforeWin = game.Economy.Data.coins; fragmentsBeforeWin = game.Economy.Data.fragments;
                            winsBeforeWin = game.Economy.Data.wins; streakBeforeWin = game.Economy.Streak;
                            wonLevel = Read<int>(game, "levelIndex"); wonAttempt = game.Economy.Data.attempt;
                            stage = 18; nextTap = now;
                            return;
                        }
                        Click(game, 1); stageAt = now; stage = 4;
                        break;
                    case 18:
                        if (Read<bool>(game, "finaleGuide"))
                        {
                            if (!report.finaleGuideDrawn)
                            {
                                if (Read<float>(game, "finaleGuideAge") < .65f) return;
                                Capture(game, "16-finale-guide");
                                report.finaleGuideDrawn = true;
                                report.finaleGuidePaused = game.Paused;
                                if (!game.Paused) throw new InvalidOperationException("Finale guide did not pause battle.");
                                Click(game, 8);
                                report.finaleLaunched = game.Battle.ActiveFinale != null && game.Battle.FinaleUsed && !game.Paused;
                                report.finaleTutorialSaved = game.Economy.Data.finaleTutorial;
                                if (!report.finaleLaunched || !report.finaleTutorialSaved)
                                    throw new InvalidOperationException("Finale launch or tutorial completion failed.");
                                report.events.Add("first finale guide paused battle; button 8 launched the source mode " + game.Battle.ActiveFinale.Mode);
                            }
                            return;
                        }
                        if (!report.finaleBeamDrawn && game.Battle.ActiveFinale != null && game.Battle.ActiveFinale.Age >= 2.3f)
                        {
                            Capture(game, "17-finale-sweep"); report.finaleBeamDrawn = true;
                        }
                        if (game.Battle.State == Phase.Won)
                        {
                            if (game.LastReward == null) return;
                            report.finaleKills = game.Battle.FinaleKills;
                            if (!report.finaleGuideDrawn || !report.finaleLaunched || !report.finaleBeamDrawn || report.finaleKills <= 0)
                                throw new InvalidOperationException("Formal result route did not cover the live finale sequence.");
                            report.formalWon = true;
                            report.formalRewardCoins = game.LastReward.coins; report.formalRewardFragments = game.LastReward.fragments;
                            CheckFormalReward(game);
                            report.formalRewardChecked = true;
                            report.events.Add("first formal level won through live suggested queue inputs; game seconds=" + game.Battle.Time
                                + "; coins=" + report.formalRewardCoins + "; fragments=" + report.formalRewardFragments);
                            stageAt = now; stage = 19;
                            return;
                        }
                        if (game.Battle.State == Phase.Lost || game.Battle.State == Phase.ReviveOffer)
                            throw new InvalidOperationException("Live formal-result route ended without a victory: " + game.Battle.State);
                        if (now < nextTap || game.Paused) return;
                        int resultLane = game.Battle.SuggestedQueue();
                        if (resultLane >= 0) Pointer(game, new Vector2(game.Battle.QueuePositionX(resultLane), 725f));
                        nextTap = now + .16d;
                        break;
                    case 19:
                        if (game.Feedback.EndAge < .75f) return;
                        Capture(game, "18-formal-result-enter"); stage = 20;
                        break;
                    case 20:
                        if (game.Feedback.EndAge < 2.6f) return;
                        Capture(game, "19-formal-rewards");
                        CheckFormalReward(game); // Drawing several frames must not grant again.
                        report.resultDrawn = true;
                        Click(game, 40);
                        if (Read<int>(game, "levelIndex") != wonLevel + 1 || game.Battle.Level.lesson > 0 || game.LastReward != null)
                            throw new InvalidOperationException("Result next-level button did not start the next formal sortie.");
                        report.nextFormalEntered = true;
                        report.events.Add("result animation retained one settlement; next-level HUD hit entered next formal sortie");
                        stageAt = now; stage = 21;
                        break;
                    case 21:
                        if (game.Battle.State == Phase.Intro) return;
                        if (game.Battle.State != Phase.Ready) throw new InvalidOperationException("Next formal level did not reach Ready.");
                        Capture(game, "20-next-formal-ready");
                        Click(game, 1); stageAt = now; stage = 4;
                        break;
                    case 30:
                        if (game.Battle.State != Phase.ReviveOffer)
                        {
                            if (game.Battle.State != Phase.Playing) throw new InvalidOperationException("Expected first natural breach to offer revival, got " + game.Battle.State);
                            return;
                        }
                        if (game.Feedback.RescueAge < 1f) return;
                        Capture(game, "21-revive-offer"); report.reviveOfferDrawn = true;
                        if (game.Battle.Revived || game.Battle.Breaches != 1 || game.Economy.Streak != beforeLossStreak || game.LastReward != null)
                            throw new InvalidOperationException("First breach incorrectly settled the result or skipped the revive offer.");
                        Click(game, 31);
                        if (!Read<bool>(game, "streakPrompt") || !Read<bool>(game, "streakRevive") || game.Battle.State != Phase.ReviveOffer)
                            throw new InvalidOperationException("Decline did not offer the original streak-preservation confirmation.");
                        stageAt = now; stage = 31;
                        break;
                    case 31:
                        if (now - stageAt < .7d) return;
                        Capture(game, "22-revive-streak-confirmation"); report.reviveStreakPrompt = true;
                        int slotsBefore = game.Battle.UnlockedSlots, maximumBefore = game.Battle.MaximumSlots;
                        int remainingBefore = game.Battle.Remaining, missilesBefore = game.Battle.RescueMissiles, adsBefore = AdService.Skipped;
                        Click(game, 511);
                        if (game.Battle.State != Phase.Playing || !game.Battle.Revived || game.Battle.UnlockedSlots != slotsBefore + 1
                            || game.Battle.MaximumSlots != maximumBefore + 1 || game.Battle.RescueMissiles != missilesBefore + 1
                            || game.Battle.LastPurge <= 0 || game.Battle.Remaining != remainingBefore - game.Battle.LastPurge
                            || AdService.Skipped != adsBefore + 1 || game.Paused || Read<bool>(game, "streakPrompt"))
                            throw new InvalidOperationException("Streak revive action failed source purge, extra bay, missile or Test-build ad callback.");
                        report.revived = true; report.revivePurgeKills = game.Battle.LastPurge;
                        report.events.Add("revived through streak dialog; purge=" + report.revivePurgeKills + "; extra bay and rescue missile granted once");
                        stageAt = now; stage = 32;
                        break;
                    case 32:
                        if (Read<int>(game, "selection") != 2) return;
                        if (!game.Paused || !Read<bool>(game, "aimMissile")) throw new InvalidOperationException("Scheduled rescue missile did not enter paused missile targeting.");
                        Capture(game, "23-rescue-missile-aim"); report.rescueMissileAimed = true;
                        beforeRescueKills = game.Battle.SuperStrikeKills;
                        Click(game, 9);
                        if (game.Paused || game.Battle.PendingStrike == null || !game.Battle.PendingStrike.Missile || game.Battle.RescueMissiles != 0
                            || game.Economy.Data.items[2] != beforeRescueItems)
                            throw new InvalidOperationException("Rescue missile launch did not consume only the granted missile.");
                        report.rescueMissileLaunched = true; stageAt = now; stage = 33;
                        break;
                    case 33:
                        if (game.Battle.PendingStrike != null) return;
                        report.rescueMissileKills = game.Battle.SuperStrikeKills - beforeRescueKills;
                        if (report.rescueMissileKills <= 0) throw new InvalidOperationException("Rescue missile detonated without removing targeted enemies.");
                        Capture(game, "24-rescue-missile-impact"); report.rescueMissileDetonated = true;
                        report.events.Add("automatic rescue missile aim, launch and detonation; kills=" + report.rescueMissileKills + "; bomb inventory unchanged");
                        stageAt = now; stage = 34;
                        break;
                    case 34:
                        if (game.Battle.State == Phase.ReviveOffer) throw new InvalidOperationException("Second breach offered another revive.");
                        if (game.Battle.State != Phase.Lost) return;
                        if (game.Feedback.EndAge < 1.2f) return;
                        if (!game.Battle.Revived || game.Battle.Breaches != 2) throw new InvalidOperationException("Loss was not the second natural breach.");
                        CheckLossReward(game);
                        Capture(game, "25-second-breach-loss"); report.secondBreachLost = true; report.lossDrawn = true;
                        failedBattle = game.Battle; failedLevel = Read<int>(game, "levelIndex"); Click(game, 40);
                        report.events.Add("second natural breach lost; no win rewards; retry via result button");
                        stageAt = now; stage = 35;
                        break;
                    case 35:
                        if (Read<bool>(game, "transitionActive") || game.Battle == failedBattle || game.Battle.State == Phase.Intro) return;
                        // f54695 reloads the level JSON into a new LevelData object.
                        if (game.Battle.State != Phase.Ready || Read<int>(game, "levelIndex") != failedLevel
                            || game.Battle.Level.sourceLevelId != failedBattle.Level.sourceLevelId || game.Battle.Revived || game.LastReward != null)
                            throw new InvalidOperationException("Retry did not restore the same formal level to a fresh Ready battle.");
                        Capture(game, "26-retry-ready"); report.retryEntered = true;
                        int retryLane = game.Battle.SuggestedQueue();
                        if (retryLane < 0) throw new InvalidOperationException("Retry has no deployable fighter.");
                        Pointer(game, new Vector2(game.Battle.QueuePositionX(retryLane), 725f));
                        stageAt = now; stage = 36;
                        break;
                    case 36:
                        if (game.Battle.State != Phase.ReviveOffer) return;
                        if (game.Feedback.RescueAge < 1f) return;
                        Capture(game, "27-retry-revive-offer");
                        if (game.Economy.Streak != 0 || game.Battle.Revived) throw new InvalidOperationException("Retry loss retained prior streak or revive use.");
                        Click(game, 31);
                        if (Read<bool>(game, "streakPrompt") || game.Battle.State != Phase.Lost)
                            throw new InvalidOperationException("Zero-streak decline did not directly finish defeat.");
                        report.declineLost = true; stage = 37;
                        break;
                    case 37:
                        if (game.Feedback.EndAge < 1.2f) return;
                        CheckLossReward(game);
                        Capture(game, "28-decline-loss");
                        report.events.Add("retry naturally reached first breach; declined with zero streak; loss result returned home");
                        Click(game, 41); stageAt = now; stage = 5;
                        break;
                }
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static void Capture(PixelWingGame game, string name)
        {
            report.screenWidth = Screen.width; report.screenHeight = Screen.height;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Camera camera = game.GameCamera;
            int width = camera.pixelWidth, height = camera.pixelHeight;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                string path = Path.Combine(Path.GetDirectoryName(Output), name + ".png");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                report.cameraFrames.Add(path);
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        private static void Click(PixelWingGame game, int id)
        {
            var hits = (IList)typeof(PixelWingGame).GetField("uiHits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(game);
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                object hit = hits[i]; Type type = hit.GetType();
                if ((int)type.GetField("id").GetValue(hit) != id) continue;
                float x = (float)type.GetField("x").GetValue(hit), y = (float)type.GetField("y").GetValue(hit);
                float w = (float)type.GetField("w").GetValue(hit), h = (float)type.GetField("h").GetValue(hit);
                Pointer(game, new Vector2(x + w * .5f, ScreenLayout.InverseY(y + h * .5f)));
                return;
            }
            throw new InvalidOperationException("Rendered HUD hit not found: " + id);
        }
        private static T Read<T>(PixelWingGame game, string name) => (T)typeof(PixelWingGame).GetField(name, PrivateInstance).GetValue(game);
        private static void CheckLossReward(PixelWingGame game)
        {
            if (game.LastReward != null || game.Economy.Data.coins != beforeLossCoins || game.Economy.Data.fragments != beforeLossFragments
                || game.Economy.Data.wins != beforeLossWins || game.Economy.Streak != 0)
                throw new InvalidOperationException("Failure granted win rewards or retained the streak.");
            report.lossRewardUnchanged = true;
        }
        private static void CheckFormalReward(PixelWingGame game)
        {
            var data = game.Economy.Data;
            if (data.coins != coinsBeforeWin + report.formalRewardCoins || data.fragments != fragmentsBeforeWin + report.formalRewardFragments
                || data.wins != winsBeforeWin + 1 || data.streak != Math.Min(10, streakBeforeWin + 1)
                || data.unlocked < wonLevel + 2 || !data.cleared.Contains(wonLevel + 1) || data.settled != wonAttempt || data.attempt != String.Empty)
                throw new InvalidOperationException("Formal victory did not settle rewards exactly once, record the cleared level and unlock the next sortie.");
        }
        private static Vector2 PointerPosition(PixelWingGame game, Vector2 logical)
        {
            var canvas = (RectTransform)typeof(PixelWingGame).GetField("canvasRect", PrivateInstance).GetValue(game);
            Vector3 world = canvas.TransformPoint(new Vector3(logical.x, ScreenLayout.Height - ScreenLayout.Y(logical.y), 0f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(game.GameCamera, world);
            Vector2 read = (Vector2)typeof(PixelWingGame).GetMethod("ScreenLogical", PrivateInstance).Invoke(game, new object[] { screen });
            report.maximumPointerRoundTripError = Math.Max(report.maximumPointerRoundTripError, (read - logical).magnitude);
            return read;
        }
        private static void Pointer(PixelWingGame game, Vector2 logical)
        {
            Vector2 read = PointerPosition(game, logical);
            typeof(PixelWingGame).GetMethod("PointerDown", PrivateInstance).Invoke(game, new object[] { read });
            typeof(PixelWingGame).GetMethod("PointerMove", PrivateInstance).Invoke(game, new object[] { read });
            typeof(PixelWingGame).GetMethod("PointerUp", PrivateInstance).Invoke(game, new object[] { read });
            report.pointerCoordinatesUsed = true; report.taps++;
        }
        private static void Drag(PixelWingGame game, Vector2 from, Vector2 to)
        {
            typeof(PixelWingGame).GetMethod("PointerDown", PrivateInstance).Invoke(game, new object[] { PointerPosition(game, from) });
            typeof(PixelWingGame).GetMethod("PointerMove", PrivateInstance).Invoke(game, new object[] { PointerPosition(game, to) });
            typeof(PixelWingGame).GetMethod("PointerUp", PrivateInstance).Invoke(game, new object[] { PointerPosition(game, to) });
            report.pointerCoordinatesUsed = true; report.taps++;
        }
        private static void Finish(string error)
        {
            if (finishing) return;
            finishing = true;
            if (report == null) report = new Report { unityVersion = Application.unityVersion };
            report.error = error; report.passed = error == null;
            File.WriteAllText(Output, JsonUtility.ToJson(report, true));
            #if UNITY_EDITOR
            SessionState.SetBool(Key + ".finished", true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            else RestoreAndExit();
            #else
            PixelWingGame game = UnityEngine.Object.FindObjectOfType<PixelWingGame>();
            if (game != null) game.enabled = false;
            RestoreAndExit();
            #endif
        }
        private static void RestoreAndExit()
        {
            string output = Output;
            var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(output + ".save-backup.json"));
            foreach (SavedValue saved in backup.values)
                if (saved.present) PlayerPrefs.SetString(saved.key, saved.value); else PlayerPrefs.DeleteKey(saved.key);
            PlayerPrefs.Save();
            report = JsonUtility.FromJson<Report>(File.ReadAllText(output));
            report.saveRestored = true;
            foreach (SavedValue saved in backup.values)
                if (PlayerPrefs.HasKey(saved.key) != saved.present || (saved.present && PlayerPrefs.GetString(saved.key) != saved.value))
                    report.saveRestored = false;
            report.passed &= report.saveRestored;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            #if UNITY_EDITOR
            SessionState.EraseBool(Key); SessionState.EraseBool(Key + ".finished"); SessionState.EraseString(Key + ".path");
            EditorApplication.Exit(report.passed ? 0 : 1);
            #else
            Application.Quit(report.passed ? 0 : 1);
            #endif
        }
    }
}

#endif
