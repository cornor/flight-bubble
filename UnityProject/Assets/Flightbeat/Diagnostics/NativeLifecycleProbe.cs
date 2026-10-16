#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Flightbeat.Diagnostics
{
    // Explicit stage-level native lifecycle check. Normal source UI/PlayerLoop;
    // no model state, resource, RNG, clock or particle-simulation injection.
    public sealed class NativeLifecycleProbe : MonoBehaviour
    {
        [Serializable] private sealed class Slot { public string key, value; public bool present; }
        [Serializable] private sealed class Save { public string profile; public Slot[] values; }
        [Serializable] private sealed class Ship { public int color, tier, ammo, id; }
        [Serializable] private sealed class Inventory { public string level; public List<Ship> ships = new List<Ship>(); }
        [Serializable] private sealed class PoolInfo { public string type; public int usingCount, unused, acquired, released; }
        [Serializable] private sealed class Sample
        {
            public int cycle, games, voices, listeners, surfaces, effects, createdEffects, livingEffects, oldBattlesAlive, oldFeedbackAlive;
            public long allocatedBytes, monoUsedBytes;
            public PoolInfo[] pools;
        }
        [Serializable] private sealed class Report
        {
            public bool passed, saveRestored, sceneUnloaded;
            public string error, unityVersion, initialProfile, finalProfile;
            public int completedCycles, pointerActions;
            public List<Inventory> inventories = new List<Inventory>();
            public List<Sample> samples = new List<Sample>();
            public List<string> inputRefresh = new List<string>();
        }
        private static readonly string[] Keys = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b" };
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static string output;
        private static Save backup;
        private readonly Report report = new Report();
        private readonly List<WeakReference> oldBattles = new List<WeakReference>(), oldFeedback = new List<WeakReference>();
        private PixelWingGame game;
        private bool finished;
        private double started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--flightbeat-lifecycle-report");
            if (at < 0) return;
            try
            {
                if (at + 1 == args.Length) throw new ArgumentException("Lifecycle report path required");
                output = Path.GetFullPath(args[at + 1]);
                int seed = Array.IndexOf(args, "--flightbeat-lifecycle-save");
                if (seed < 0 || seed + 1 == args.Length) throw new ArgumentException("Actual carried save required");
                if (File.Exists(output) || File.Exists(output + ".backup.json")) throw new IOException("Fresh output required");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                Save input = JsonUtility.FromJson<Save>(File.ReadAllText(Path.GetFullPath(args[seed + 1])));
                if (input == null || input.values == null || input.values.Length != Keys.Length || FleetSaveStore.Decode(input.profile) == null)
                    throw new ArgumentException("Complete valid carried save required");
                for (int i = 0; i < Keys.Length; i++) if (input.values[i] == null || input.values[i].key != Keys[i]) throw new ArgumentException("Save order");
                backup = ObserveSave();
                File.WriteAllText(output + ".backup.json", JsonUtility.ToJson(backup, true));
                Restore(input);
                var host = new GameObject("CLI lifecycle check");
                DontDestroyOnLoad(host);
                host.AddComponent<NativeLifecycleProbe>();
            }
            catch (Exception e)
            {
                if (backup != null) Restore(backup);
                Debug.LogException(e); Application.Quit(1);
            }
        }
        private static Save ObserveSave()
        {
            var value = new Save { values = new Slot[Keys.Length] };
            for (int i = 0; i < Keys.Length; i++) value.values[i] = new Slot
            { key = Keys[i], present = PlayerPrefs.HasKey(Keys[i]), value = PlayerPrefs.GetString(Keys[i], "") };
            return value;
        }
        private static void Restore(Save value)
        {
            foreach (Slot s in value.values) if (s.present) PlayerPrefs.SetString(s.key, s.value); else PlayerPrefs.DeleteKey(s.key);
            PlayerPrefs.Save();
        }
        private void OnEnable() { Application.logMessageReceived += Logged; }
        private void OnDisable() { Application.logMessageReceived -= Logged; }
        private void Logged(string message, string stack, LogType type)
        { if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(message + "\n" + stack); }
        private IEnumerator Start()
        {
            started = Time.realtimeSinceStartupAsDouble;
            report.unityVersion = Application.unityVersion;
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0 && !finished)
            {
                object yielded = null; bool hasYield = false;
                try
                {
                    if (Time.realtimeSinceStartupAsDouble - started > 600d) throw new TimeoutException("Lifecycle stage exceeded ten minutes");
                    if (!stack.Peek().MoveNext()) stack.Pop();
                    else if (stack.Peek().Current is IEnumerator nested) stack.Push(nested);
                    else { yielded = stack.Peek().Current; hasYield = true; }
                }
                catch (Exception e) { Finish(e.ToString()); }
                if (hasYield && !finished) yield return yielded;
            }
            if (!finished) Finish(null);
        }
        private IEnumerator Run()
        {
            yield return Until(() => (game = Object.FindObjectOfType<PixelWingGame>()) != null && game.Battle != null && HomeReady(), "initial home");
            report.initialProfile = FleetSaveStore.Encode(game.Economy.Data);
            ObserveInventory("training01"); ObserveInventory("training02");
            for (int cycle = 1; cycle <= 51; cycle++)
            {
                // f54641 page routes; no purchases or gifts after the completed
                // growth tutorial in this actual carried BP profile.
                yield return ClickAndSettle(120);
                Require(game.Page == DeckPage.Upgrade, "Upgrade page");
                yield return ClickAndSettle(122);
                Require(game.Page == DeckPage.Shop, "Shop page");
                yield return ClickAndSettle(121);
                Require(game.Page == DeckPage.Bridge, "Bridge page");
                TrackRetired();
                yield return ClickAndSettle(100);
                yield return ClickAndSettle(299); // Source settings training replay.
                yield return Until(() => BattleReady(), "training replay Ready");
                int lane = game.Lesson.SuggestedLane;
                Require(lane >= 0, "Training has a deployment");
                Vector2 launch = new Vector2(game.Battle.QueuePositionX(lane), 725f);
                int oldHit = Hit(launch);
                // f54783 updates transition state before the Canvas rebuild;
                // f54816's previous HitBlock can still be in the last paint.
                Canvas.ForceUpdateCanvases();
                report.inputRefresh.Add("cycle=" + cycle + "; hitBefore=" + oldHit + "; hitAfter=" + Hit(launch));
                Pointer(launch);
                Require(game.Battle.State == Phase.Playing && game.Battle.Fleet.Count > 0, "Rendered queue tap deploys a fighter");
                yield return Until(() => game.Battle.Shots.Count > 0, "live shot before restart");
                yield return ClickAndSettle(1);
                Require(game.Paused, "Pause");
                float frozen = game.Battle.Time;
                yield return new WaitForSecondsRealtime(.1f);
                Require(game.Battle.Time == frozen, "Paused model clock");
                yield return ClickAndSettle(24);
                Require(!game.Paused, "Resume");
                yield return ClickAndSettle(1);
                TrackRetired();
                yield return ClickAndSettle(23); // f54713 -> Restart f54776.
                yield return Until(() => BattleReady(), "restart Ready");
                Require(game.Battle.Shots.Count == 0 && game.Battle.Remaining == game.Battle.Total, "Restart clears old shots and board state");
                yield return ClickAndSettle(1);
                yield return ClickAndSettle(25);
                yield return Until(() => HomeReady(), "return home");
                for (int n = 0; n < 4; n++) yield return null;
                Collect();
                Sample sample = Snapshot(cycle);
                Require(sample.games == 1 && sample.voices == 16 && sample.listeners == 1 && sample.surfaces == 2, "No duplicate game, audio or surfaces");
                Require(sample.effects == sample.createdEffects && sample.createdEffects <= PixelEffectPool.Capacity && sample.livingEffects == 0, "Effect instances remain owned and within source capacity");
                Require(sample.oldBattlesAlive == 0 && sample.oldFeedbackAlive == 0, "Retired models and event subscribers are collectible");
                foreach (PoolInfo pool in sample.pools) Require(pool.usingCount == 0 && pool.acquired == pool.released, "All shot loans returned after retry");
                Require(game.Economy.Data.attempt.Length == 0, "Exit settles active attempt");
                report.samples.Add(sample); report.completedCycles = cycle;
                File.WriteAllText(output + ".progress.json", JsonUtility.ToJson(report, true));
                if (cycle % 10 == 0) Debug.Log("NATIVE_LIFECYCLE completed cycles=" + cycle);
            }
            report.finalProfile = FleetSaveStore.Encode(game.Economy.Data);
            // Unload the actual scene: f54773 creates camera/canvases as scene
            // roots, so destroying just the game component is not scene teardown.
            TrackRetired();
            Scene oldScene = game.gameObject.scene;
            Scene empty = SceneManager.CreateScene("Lifecycle finished");
            SceneManager.SetActiveScene(empty);
            game = null;
            yield return SceneManager.UnloadSceneAsync(oldScene);
            for (int n = 0; n < 4; n++) yield return null;
            Collect();
            Sample final = Snapshot(52);
            Require(final.games == 0 && final.voices == 0 && final.listeners == 0 && final.surfaces == 0 && final.effects == 0,
                "Scene teardown destroys owned runtime objects");
            Require(final.oldBattlesAlive == 0 && final.oldFeedbackAlive == 0, "Scene teardown releases final event graph");
            foreach (PoolInfo pool in final.pools) Require(pool.usingCount == 0, "Scene teardown returns every borrowed object");
            report.samples.Add(final); report.sceneUnloaded = true;
        }
        private bool HomeReady() => game != null && game.InHome && !Read<bool>("transitionActive");
        private bool BattleReady() => game != null && !game.InHome && game.Battle.Level.lesson == 1
            && game.Battle.State == Phase.Ready && !Read<bool>("transitionActive");
        private T Read<T>(string name) => (T)typeof(PixelWingGame).GetField(name, Flags).GetValue(game);
        private int Hit(Vector2 point) => (int)typeof(PixelWingGame).GetMethod("HitTest", Flags).Invoke(game, new object[] { point });
        private IEnumerator Until(Func<bool> condition, string name)
        {
            double until = Time.realtimeSinceStartupAsDouble + 15d;
            while (!condition()) { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException(name); yield return null; }
        }
        private IEnumerator ClickAndSettle(int id)
        { Canvas.ForceUpdateCanvases(); Click(id); yield return new WaitForSecondsRealtime(.2f); }
        private void Click(int id)
        {
            var hits = (IList)typeof(PixelWingGame).GetField("uiHits", Flags).GetValue(game);
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                object hit = hits[i]; Type t = hit.GetType();
                if ((int)t.GetField("id").GetValue(hit) != id) continue;
                float x = (float)t.GetField("x").GetValue(hit), y = (float)t.GetField("y").GetValue(hit);
                float w = (float)t.GetField("w").GetValue(hit), h = (float)t.GetField("h").GetValue(hit);
                Pointer(new Vector2(x + w * .5f, ScreenLayout.InverseY(y + h * .5f))); return;
            }
            throw new InvalidOperationException("Missing rendered UI control " + id);
        }
        private void Pointer(Vector2 logical)
        {
            var canvas = Read<RectTransform>("canvasRect");
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(game.GameCamera,
                canvas.TransformPoint(new Vector3(logical.x, ScreenLayout.Height - ScreenLayout.Y(logical.y), 0f)));
            Vector2 point = (Vector2)typeof(PixelWingGame).GetMethod("ScreenLogical", Flags).Invoke(game, new object[] { screen });
            foreach (string method in new[] { "PointerDown", "PointerMove", "PointerUp" })
                typeof(PixelWingGame).GetMethod(method, Flags).Invoke(game, new object[] { point });
            report.pointerActions++;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TrackRetired()
        { oldBattles.Add(new WeakReference(game.Battle)); oldFeedback.Add(new WeakReference(game.Feedback)); }
        private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        private static int Alive(List<WeakReference> values) { int count = 0; foreach (WeakReference value in values) if (value.IsAlive) count++; return count; }
        private Sample Snapshot(int cycle)
        {
            MemoryPoolInfo[] infos = MemoryPool.GetAllMemoryPoolInfos();
            var pools = new PoolInfo[infos.Length];
            for (int i = 0; i < infos.Length; i++) pools[i] = new PoolInfo { type = infos[i].Type.FullName,
                usingCount = infos[i].UsingMemoryCount, unused = infos[i].UnusedMemoryCount,
                acquired = infos[i].AcquireMemoryCount, released = infos[i].ReleaseMemoryCount };
            return new Sample { cycle = cycle, games = Object.FindObjectsOfType<PixelWingGame>(true).Length,
                voices = Object.FindObjectsOfType<AudioSource>(true).Length, listeners = Object.FindObjectsOfType<AudioListener>(true).Length,
                surfaces = Object.FindObjectsOfType<PixelSurface>(true).Length, effects = Object.FindObjectsOfType<PixelEffect>(true).Length,
                createdEffects = game ? game.Effects.Created : 0, livingEffects = game ? game.Effects.Alive : 0,
                oldBattlesAlive = Alive(oldBattles), oldFeedbackAlive = Alive(oldFeedback), pools = pools,
                allocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                monoUsedBytes = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() };
        }
        private void ObserveInventory(string level)
        {
            var battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/" + level).text), null, true);
            try
            {
                var row = new Inventory { level = level };
                foreach (var queue in battle.Reserve) foreach (Fighter fighter in queue)
                    row.ships.Add(new Ship { color = fighter.Color, tier = fighter.Tier, ammo = fighter.Ammo, id = fighter.Id });
                report.inventories.Add(row);
            }
            finally { battle.Dispose(); }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private void Finish(string error)
        {
            if (finished) return; finished = true;
            if (game) game.enabled = false;
            report.error = error;
            Restore(backup);
            report.saveRestored = true;
            foreach (Slot s in backup.values)
                if (PlayerPrefs.HasKey(s.key) != s.present || s.present && PlayerPrefs.GetString(s.key) != s.value) report.saveRestored = false;
            report.passed = error == null && report.completedCycles == 51 && report.sceneUnloaded && report.saveRestored;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
