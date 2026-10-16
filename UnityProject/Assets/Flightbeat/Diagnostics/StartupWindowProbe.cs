#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Audio;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Feedback;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // CLI diagnostics for native and WebGL players. The OS route uses external mouse events;
    // continuous replay supplies only the frozen route and API inputs.
    [DefaultExecutionOrder(-32000)]
    public sealed partial class StartupWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Entropy { public int[] values; }
        [Serializable] private sealed class InputData { public string name, encodedSaveKey, encodedSave, expectedEncodedSave; public SavedPreference[] encodedSaveSlots; public int expectedGuidCount; public int screenWidth, screenHeight; public Check[] checks; public Entropy[] guidBytes; public DestructionInput destruction; public ContinuousInput continuous; }
        [Serializable] private sealed class DisposalState { public int shots, effects; public bool pendingStrike; }
        [Serializable] private sealed class DestructionInput { public int fixtureTicks; public float fixtureDelta; public DisposalState before, after; }
        [Serializable] private sealed class DestructionResult
        {
            public bool engineDestroyedGame, retainedNativeEffectsDestroyed, passed;
            public DisposalState before, after;
        }
        [Serializable] private sealed class SavedPreference { public string key, value; public bool present; }
        [Serializable] private sealed class SavedIntPreference { public string key; public int value; public bool present; }
        [Serializable] private sealed class Backup { public SavedPreference[] values; public SavedIntPreference[] audio; }
        [Serializable] private sealed class Failure { public int frame; public string path, expected, actual; }
        [Serializable] private sealed class StateResult { public int checks, scalarComparisons, failureCount; public List<Failure> failures = new List<Failure>(); }
        [Serializable] private sealed class MouseEvent { public int frame; public bool down, held, up, paused, settings; public Vector3 position; public int phase, lesson, humanActions; }
        [Serializable] private sealed class Request { public int ordinal, processId, screenWidth, screenHeight; public string action; public float screenX, screenY; }
        [Serializable] private sealed class Report
        {
            public string unityVersion, graphicsDevice, name, error, scope, inputSaveKey;
            public bool passed, actualAwake, saveRestored, audioPreferencesRestored, startupSaveExact, osMouseRequested, osMouseRoutePassed;
            public int screenWidth, screenHeight, guidConsumed, awakeFrameCount, completedMouseActions, finalLessonStep, audioCueCount;
            public StateResult state; public DestructionResult destruction; public ContinuousResult continuous; public List<MouseEvent> mouseEvents = new List<MouseEvent>();
            public List<Request> requests = new List<Request>();
        }
        private sealed class AdCounters { public int Shown = AdService.Shown, Skipped = AdService.Skipped; }
        private sealed class Roots { public PixelWingGame game; public Battle battle; public FleetProfile profile; public ChipAudio audio; public AdCounters ads = new AdCounters(); }
        private static readonly string[] SaveKeys = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b" };
        private static readonly string[] AudioKeys = { "pw.music-muted", "pw.sfx-muted" }; // f55170/f55171
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static InputData data; private static Report report; private static StateResult result;
        private static PixelWingGame game; private static string output; private static int guidCursor, frame = -1, routeStage, lane, reserveBefore;
        private static bool finished, mouse, destroy, continuous;
        private static Battle disposalBattle; private static PixelEffectPool disposalPool; private static List<GameObject> disposalEffects; private static double started;
        private static FieldInfo Field(Type t, string name) => t.GetField(name, Flags) ?? t.GetField("<" + name + ">k__BackingField", Flags) ?? throw new MissingFieldException(t.FullName, name);
        private static string Arg(string name) { string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name); if (at < 0 || at + 1 == args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[at + 1]); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-startup-report") < 0) return;
            try
            {
                output = Arg("--flightbeat-startup-report"); if (File.Exists(output) || File.Exists(output + ".save-backup.json")) throw new IOException("Fresh report required");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                data = JsonUtility.FromJson<InputData>(File.ReadAllText(Arg("--flightbeat-startup-input")));
                mouse = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-os-mouse") >= 0;
                destroy = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-destroy") >= 0;
                continuous = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-continuous") >= 0;
                if (continuous && (mouse || destroy || data.continuous == null)) throw new ArgumentException("Continuous route requires its frozen input and no other route");
                if (destroy && (mouse || data.destruction == null)) throw new ArgumentException("Destruction requires its original fixture and no OS route");
                report = new Report { name = data.name, unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), osMouseRequested = mouse,
                    scope = "Engine-invoked native Awake, full startup logical-state assertions at zero paint delta, declared save/UTC/GUID inputs. Optional actual OS mouse through normal Unity PlayerLoop. Not a dynamic frame-by-frame original comparison or GPU/audio-output equivalence." };
                var backup = new Backup { values = new SavedPreference[SaveKeys.Length], audio = new SavedIntPreference[AudioKeys.Length] };
                for (int i = 0; i < SaveKeys.Length; i++) backup.values[i] = new SavedPreference { key = SaveKeys[i], present = PlayerPrefs.HasKey(SaveKeys[i]), value = PlayerPrefs.GetString(SaveKeys[i], "") };
                for (int i = 0; i < AudioKeys.Length; i++) backup.audio[i] = new SavedIntPreference { key = AudioKeys[i], present = PlayerPrefs.HasKey(AudioKeys[i]), value = PlayerPrefs.GetInt(AudioKeys[i], 0) };
                File.WriteAllText(output + ".save-backup.json", JsonUtility.ToJson(backup, true));
                foreach (string key in SaveKeys) PlayerPrefs.DeleteKey(key);
                // f54309: recorded fixture slot follows revision parity. Older
                // probe inputs explicitly used B; preserve that input format.
                string inputKey = String.IsNullOrEmpty(data.encodedSaveKey) ? "pw.fleet.v2.b" : data.encodedSaveKey;
                if (inputKey != "pw.fleet.v2.a" && inputKey != "pw.fleet.v2.b") throw new ArgumentException("Unsupported declared input save slot");
                report.inputSaveKey = inputKey;
                if (data.encodedSaveSlots == null) PlayerPrefs.SetString(inputKey, data.encodedSave);
                else
                {
                    // Explicit native preference input: preserve both original
                    // f54309 slots so f54308 performs its own normal selection.
                    if (data.encodedSaveSlots.Length != SaveKeys.Length) throw new ArgumentException("All declared fleet slots required");
                    bool selected = false;
                    for (int i = 0; i < SaveKeys.Length; i++)
                    {
                        SavedPreference slot = data.encodedSaveSlots[i];
                        if (slot == null || slot.key != SaveKeys[i]) throw new ArgumentException("Declared fleet slot order");
                        if (slot.present) PlayerPrefs.SetString(slot.key, slot.value);
                        if (slot.key == inputKey && slot.present && slot.value == data.encodedSave) selected = true;
                    }
                    if (!selected) throw new ArgumentException("Declared selected profile missing from slots");
                }
                PlayerPrefs.Save();
                FleetSaveStore.UtcOverride = () => 1800000000L;
                PixelWingGame.StartupGuidOverride = () =>
                {
                    if (guidCursor >= data.guidBytes.Length) throw new Exception("Undeclared GUID entropy");
                    int[] raw = data.guidBytes[guidCursor++].values; if (raw.Length != 16) throw new Exception("GUID entropy length");
                    byte[] bytes = new byte[16]; for (int i = 0; i < 16; i++) bytes[i] = checked((byte)raw[i]);
                    bytes[7] = (byte)((bytes[7] & 15) | 64); bytes[8] = (byte)((bytes[8] & 63) | 128); return new Guid(bytes);
                };
                PixelWingGame.PaintDeltaOverride = 0f; PixelWingGame.StartupObserver = ObserveAwake;
                if (continuous) InstallContinuousInputs();
                Application.logMessageReceived += Logged; started = Time.realtimeSinceStartupAsDouble;
                var host = new GameObject("Native startup and OS input observer"); DontDestroyOnLoad(host); host.AddComponent<StartupWindowProbe>();
            }
            catch (Exception e) { if (report != null) Finish(e.ToString()); else { Debug.LogException(e); Application.Quit(1); } }
        }
        private static void ObserveAwake(PixelWingGame value)
        {
            game = value;
            try
            {
                result = new StateResult(); report.state = result; report.actualAwake = true;
                report.awakeFrameCount = Time.frameCount; report.screenWidth = Screen.width; report.screenHeight = Screen.height;
                Canvas.ForceUpdateCanvases();
                Compare(new Roots { game = game, battle = game.Battle, profile = game.Economy.Data, audio = game.Audio }, data.checks);
                report.guidConsumed = guidCursor;
                string slot = "pw.fleet.v2." + ((game.Economy.Data.revision & 1) == 0 ? "a" : "b");
                string encoded = FleetSaveStore.Encode(game.Economy.Data), persisted = PlayerPrefs.GetString(slot, "");
                report.startupSaveExact = encoded == data.expectedEncodedSave && persisted == data.expectedEncodedSave;
                if (encoded != data.expectedEncodedSave) Fail("normal startup encoded profile", data.expectedEncodedSave, encoded);
                if (persisted != data.expectedEncodedSave) Fail("normal startup persisted slot/" + slot, data.expectedEncodedSave, persisted);
                if (guidCursor != data.expectedGuidCount) Fail("GUID/count", data.expectedGuidCount, guidCursor);
                int expectedWidth = data.screenWidth > 0 ? data.screenWidth : 540, expectedHeight = data.screenHeight > 0 ? data.screenHeight : 960;
                if (Screen.width != expectedWidth || Screen.height != expectedHeight) Fail("declared viewport", expectedWidth + "x" + expectedHeight, Screen.width + "x" + Screen.height);
                if (game.Lesson != null && !ReferenceEquals(Get(game.Lesson, "battle"), game.Battle)) Fail("lesson/battle identity", true, false);
                if (game.Narration != null && (!ReferenceEquals(Get(game.Narration, "battle"), game.Battle) || !ReferenceEquals(Get(game.Narration, "lesson"), game.Lesson))) Fail("narration identities", true, false);
                if (!continuous) PixelWingGame.PaintDeltaOverride = null; PixelWingGame.StartupObserver = null;
                if (result.failureCount != 0) Finish("Startup state comparison failed");
                else if (!mouse && !destroy && !continuous) Finish(null);
                else if (mouse && (game.InHome || game.Lesson == null || game.Lesson.Step != 0)) Finish("OS route requires normal training01 startup");
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static void Logged(string message, string stack, LogType type)
        { if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(message + "\n" + stack); }
        private void LateUpdate()
        {
            if (finished || continuous) return;
            try
            {
                if (File.Exists(output + ".driver-failure.json")) throw new Exception("OS driver could not run: " + File.ReadAllText(output + ".driver-failure.json"));
                if (Time.realtimeSinceStartupAsDouble - started > 90) throw new Exception("Native startup/OS mouse timeout");
                if (destroy && report.actualAwake) { ObserveDestruction(); return; }
                if (!game || !report.actualAwake || !mouse) return;
                frame = Time.frameCount;
                bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0), held = Input.GetMouseButton(0);
                bool settings = (bool)Get(game, "settings");
                if (down || up || held) report.mouseEvents.Add(new MouseEvent { frame = frame, down = down, up = up, held = held, position = Input.mousePosition, paused = game.Paused, settings = settings, phase = (int)game.Battle.State, lesson = game.Lesson.Step, humanActions = (int)Get(game, "broadcastHumanActions") });
                if (routeStage == 0 && frame >= 12) { SendHit("pause", 1); routeStage = 1; }
                else if (routeStage == 1 && up && game.Paused && settings) { report.completedMouseActions++; SendHit("resume", 24); routeStage = 2; }
                else if (routeStage == 2 && up && !game.Paused && !settings)
                {
                    report.completedMouseActions++; lane = game.Lesson.SuggestedLane; if (lane < 0) throw new Exception("Source lesson has no suggested lane");
                    reserveBefore = game.Battle.Reserve[lane].Count; Send("launch", new Vector2(game.Battle.QueuePositionX(lane), 725f)); routeStage = 3;
                }
                else if (routeStage == 3 && up)
                {
                    if (game.Battle.Reserve[lane].Count != reserveBefore - 1 || game.Battle.State != Phase.Playing) throw new Exception("OS launch did not deploy exactly one aircraft");
                    report.completedMouseActions++; report.finalLessonStep = game.Lesson.Step; report.audioCueCount = game.Audio.CueCount;
                    report.osMouseRoutePassed = true; Finish(null);
                }
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static DisposalState DisposalSnapshot() => new DisposalState
        { shots = disposalBattle.Shots.Count, effects = disposalPool.Created, pendingStrike = disposalBattle.PendingStrike != null };
        private static void CheckDisposal(string label, DisposalState expected, DisposalState actual)
        {
            if (actual.shots != expected.shots) Fail(label + "/shots", expected.shots, actual.shots);
            if (actual.effects != expected.effects) Fail(label + "/effects", expected.effects, actual.effects);
            if (actual.pendingStrike != expected.pendingStrike) Fail(label + "/pendingStrike", expected.pendingStrike, actual.pendingStrike);
        }
        private static void ObserveDestruction()
        {
            if (routeStage == 0 && Time.frameCount >= 12)
            {
                // Declared ownership fixture: pause normally, then deploy through
                // the real model. This does not claim real mouse input coverage.
                game.enabled = false;
                disposalBattle = game.Battle; disposalPool = game.Effects;
                if (!disposalBattle.Deploy(0)) throw new Exception("Destruction fixture could not deploy");
                if (data.destruction.fixtureTicks < 0 || data.destruction.fixtureTicks > 120 || data.destruction.fixtureDelta != .02f) throw new Exception("Invalid original destruction fixture steps");
                for (int i = 0; i < data.destruction.fixtureTicks; i++) disposalBattle.Tick(data.destruction.fixtureDelta);
                disposalEffects = new List<GameObject>();
                foreach (PixelEffect effect in (IEnumerable)Get(disposalPool, "instances")) disposalEffects.Add(effect.gameObject);
                report.destruction = new DestructionResult { before = DisposalSnapshot() };
                CheckDisposal("before destruction", data.destruction.before, report.destruction.before);
                routeStage = 1;
                Destroy(game.gameObject); // Unity, not reflection, delivers OnDestroy.
            }
            else if (routeStage == 1 && !game)
            {
                report.destruction.engineDestroyedGame = true;
                report.destruction.after = DisposalSnapshot();
                report.destruction.retainedNativeEffectsDestroyed = disposalEffects.TrueForAll(value => !value);
                CheckDisposal("after destruction", data.destruction.after, report.destruction.after);
                if (!report.destruction.retainedNativeEffectsDestroyed) Fail("destroyed native effects", true, false);
                report.destruction.passed = result.failureCount == 0;
                Finish(report.destruction.passed ? null : "Native component destruction differed from original f54803");
            }
        }
        private static void SendHit(string action, int id)
        {
            foreach (object hit in (IEnumerable)Get(game, "uiHits")) if ((int)Get(hit, "id") == id)
            { Send(action, new Vector2((float)Get(hit, "x") + (float)Get(hit, "w") * .5f, ScreenLayout.InverseY((float)Get(hit, "y") + (float)Get(hit, "h") * .5f))); return; }
            throw new Exception("Published hit missing: " + id);
        }
        private static void Send(string action, Vector2 logical)
        {
            var rect = (RectTransform)Get(game, "canvasRect");
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(game.GameCamera, rect.TransformPoint(new Vector3(logical.x, ScreenLayout.Height - ScreenLayout.Y(logical.y), 0)));
            var request = new Request { ordinal = report.requests.Count, processId = System.Diagnostics.Process.GetCurrentProcess().Id, screenWidth = Screen.width, screenHeight = Screen.height, action = action, screenX = screen.x, screenY = screen.y };
            report.requests.Add(request);
            string tmp = output + ".request.tmp", dest = output + ".request.json"; File.WriteAllText(tmp, JsonUtility.ToJson(request, true)); if (File.Exists(dest)) File.Delete(dest); File.Move(tmp, dest);
        }
        private static void Finish(string error)
        {
            if (finished) return; finished = true;
            if (game) game.enabled = false;
            report.error = error; report.passed = error == null && report.actualAwake && (!mouse || report.osMouseRoutePassed) && (!destroy || report.destruction != null && report.destruction.passed);
            PixelWingGame.StartupObserver = null; PixelWingGame.StartupGuidOverride = null; PixelWingGame.PaintDeltaOverride = null; FleetSaveStore.UtcOverride = null;
            var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(output + ".save-backup.json"));
            foreach (var item in backup.values) if (item.present) PlayerPrefs.SetString(item.key, item.value); else PlayerPrefs.DeleteKey(item.key);
            foreach (var item in backup.audio) if (item.present) PlayerPrefs.SetInt(item.key, item.value); else PlayerPrefs.DeleteKey(item.key);
            PlayerPrefs.Save(); report.saveRestored = true;
            foreach (var item in backup.values) if (PlayerPrefs.HasKey(item.key) != item.present || item.present && PlayerPrefs.GetString(item.key) != item.value) report.saveRestored = false;
            report.audioPreferencesRestored = true;
            foreach (var item in backup.audio) if (PlayerPrefs.HasKey(item.key) != item.present || item.present && PlayerPrefs.GetInt(item.key) != item.value) report.audioPreferencesRestored = false;
            report.saveRestored &= report.audioPreferencesRestored;
            ResetContinuousInputs();
            report.passed &= report.saveRestored; File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
        [Serializable] private sealed class Check { public string path, kind, text; public int bits; public int[] values; }
        private static object Get(object value, string path)
        {
            foreach (string name in path.Split('/'))
            {
                if (value == null) return null;
                if (Int32.TryParse(name, out int index))
                {
                    if (value is IList list) value = index < list.Count ? list[index] : null;
                    else { var e = ((IEnumerable)value).GetEnumerator(); for (int i = 0; i <= index; i++) if (!e.MoveNext()) return null; value = e.Current; }
                }
                else value = Field(value.GetType(), name).GetValue(value);
            }
            return value;
        }
        private static int Integer(object value) => value is float f ? BitConverter.SingleToInt32Bits(f) : value is bool b ? (b ? 1 : 0) : value is uint u ? unchecked((int)u) : Convert.ToInt32(value);
        private static void Fail(string path, object expected, object actual)
        {
            result.failureCount++; if (result.failures.Count < 30) result.failures.Add(new Failure { frame = frame, path = path, expected = expected?.ToString() ?? "null", actual = actual?.ToString() ?? "null" });
        }
        private static void Compare(Roots roots, Check[] checks)
        {
            foreach (var check in checks)
            {
                result.checks++; object actual = Get(roots, check.path); result.scalarComparisons++;
                if (check.kind == "null") { if (actual != null) Fail(check.path, null, actual); }
                else if (check.kind == "object") { if (actual == null) Fail(check.path, "non-null", null); }
                else if (check.kind == "string" || check.kind == "long" || check.kind == "ulong") { if (actual?.ToString() != check.text) Fail(check.path, check.text, actual); }
                else if (check.kind == "count") { int count = actual is ICollection a ? a.Count : -1; if (count != check.bits) Fail(check.path, check.bits, count); }
                else if (check.kind.EndsWith("[]", StringComparison.Ordinal))
                {
                    if (actual == null) { Fail(check.path, "array", null); continue; }
                    var values = new List<int>(); foreach (var item in (IEnumerable)actual) values.Add(Integer(item));
                    if (values.Count != check.values.Length) Fail(check.path + "/length", check.values.Length, values.Count);
                    for (int i = 0; i < Math.Min(values.Count, check.values.Length); i++) { result.scalarComparisons++; if (values[i] != check.values[i]) Fail(check.path + "/" + i, check.values[i], values[i]); }
                }
                else if (actual == null || Integer(actual) != check.bits) Fail(check.path, check.bits, actual == null ? null : (object)Integer(actual));
            }
        }
    }
}
#endif
