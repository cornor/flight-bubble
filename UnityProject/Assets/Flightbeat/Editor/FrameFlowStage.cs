using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Audio;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Complete Game.Update under declared frame inputs. Does not claim PlayerLoop,
    // Game.Awake, native audio or GPU equivalence. Pointer mode also observes
    // native projection and real Paint-published hit regions.
    public static partial class FrameFlowStage
    {
        [Serializable] private sealed class Check { public string path, kind, text; public int bits; public int[] values; }
        [Serializable] private sealed class ApiCall { public int api, arg, bits; public int[] values; }
        [Serializable] private sealed class MouseInput { public bool down, held, up; public int[] positionBits; }
        [Serializable] private sealed class Projection { public int[] screenBits, logicalBits; }
        [Serializable] private sealed class Step { public int frame, dtBits; public int[] keys; public MouseInput mouse; public Projection[] projections; public ApiCall[] calls; public Check[] checks; }
        [Serializable] private sealed class CaseInput { public string name, levelJson, profileJson; public bool pointerMode; public Check[] fields, initial; public Step[] steps; public string[] encodedSaves; }
        [Serializable] private sealed class Input { public CaseInput[] cases; }
        [Serializable] private sealed class Failure { public int frame; public string path, expected, actual; }
        [Serializable] private sealed class Result
        {
            public string name, error;
            public bool passed;
            public int frames, inputCalls, projectionCalls, paints, checks, scalarComparisons, failureCount, phase, timeBits, remaining;
            public List<Failure> failures = new List<Failure>();
        }
        [Serializable] private sealed class Report { public string unityVersion; public bool passed; public double seconds; public List<Result> cases = new List<Result>(); }
        private sealed class Roots { public PixelWingGame game; public Battle battle; public FleetProfile profile; public ChipAudio audio; public int saveCount; }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Result result;
        private static int frame;
        private static FieldInfo Field(Type t, string name) => t.GetField(name, Flags) ?? t.GetField("<" + name + ">k__BackingField", Flags) ?? throw new MissingFieldException(t.FullName, name);
        private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
        private static void Call(object target, string name) => target.GetType().GetMethod(name, Flags, null, Type.EmptyTypes, null).Invoke(target, null);
        private static string Arg(string name) { var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name); if (at < 0 || at + 1 == args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[at + 1]); }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new Exception("CLI only");
            string output = Arg("--flightbeat-frame-report"); if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-frame-input")));
            var report = new Report { unityVersion = Application.unityVersion };
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var previous = PixelWingGame.FrameInputOverride; var previousProjection = PixelWingGame.FrameProjectionObserver; var previousPaintDelta = PixelWingGame.PaintDeltaOverride;
            var layout = new Dictionary<FieldInfo, object>();
            foreach (var field in typeof(ScreenLayout).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (!field.IsLiteral && !field.IsInitOnly) layout[field] = field.GetValue(null);
            try
            {
                foreach (var c in input.cases)
                {
                    result = new Result { name = c.name }; report.cases.Add(result); frame = -1;
                    try { Execute(c); result.passed = result.failureCount == 0; }
                    catch (Exception e) { result.error = e.ToString(); result.passed = false; }
                }
                report.passed = report.cases.TrueForAll(c => c.passed);
            }
            finally
            {
                PixelWingGame.FrameInputOverride = previous; PixelWingGame.FrameProjectionObserver = previousProjection; PixelWingGame.PaintDeltaOverride = previousPaintDelta;
                foreach (var pair in layout) pair.Key.SetValue(null, pair.Value);
                report.seconds = clock.Elapsed.TotalSeconds; File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("Full Update declared-input comparison failed: " + output);
        }
        private static void Execute(CaseInput c)
        {
            var owner = new GameObject("Frame flow declared fixture"); owner.SetActive(false);
            var effectOwner = new GameObject("Frame flow particle owner", typeof(RectTransform));
            Battle battle = null; PixelEffectPool effects = null; PointerContext pointer = null;
            try
            {
                ScreenLayout.Configure(540, 960, new Rect(0, 0, 1, 1));
                var game = owner.AddComponent<PixelWingGame>();
                var audio = owner.AddComponent<ChipAudio>(); // Original fixture runs ctor only, no voices or clips.
                var level = JsonUtility.FromJson<LevelData>(c.levelJson);
                foreach (var check in c.initial)
                    if (check.kind == "null" && check.path.StartsWith("battle/Level/", StringComparison.Ordinal))
                    {
                        string path = check.path.Substring("battle/Level/".Length); int slash = path.LastIndexOf('/');
                        Set(slash < 0 ? level : Get(level, path.Substring(0, slash)), path.Substring(slash + 1), null);
                    }
                battle = new Battle(level, null, true);
                var saves = new List<string>();
                var economy = new FleetEconomy(JsonUtility.FromJson<FleetProfile>(c.profileJson), () => 1800000000L,
                    p => saves.Add(FleetSaveStore.Encode(p)), _ => throw new Exception("Undeclared economy RNG"));
                var colors = (Color[])typeof(PixelWingGame).GetMethod("BuildPalette", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { level });
                if (c.pointerMode) pointer = new PointerContext(game);
                effects = new PixelEffectPool(effectOwner.transform, pointer != null ? pointer.CanvasRect : (RectTransform)effectOwner.transform);
                var feedback = new CombatFeedback(battle, effects, colors);
                Set(game, "Battle", battle); Set(game, "Economy", economy); Set(game, "audioChip", audio);
                Set(game, "effects", effects); Set(game, "feedback", feedback); Set(game, "colors", colors);
                if (pointer == null) foreach (string name in new[] { "surface", "hud" })
                {
                    var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(owner.transform, false);
                    Set(game, name, child.AddComponent<PixelSurface>());
                }
                Call(game, "SetupSortie");
                foreach (var field in c.fields)
                {
                    Type type = Field(typeof(PixelWingGame), field.path).FieldType;
                    object value = field.kind == "float" ? (object)BitConverter.Int32BitsToSingle(field.bits)
                        : field.kind == "string" ? field.text : type == typeof(bool) ? (object)(field.bits != 0)
                        : type.IsEnum ? Enum.ToObject(type, field.bits) : (object)field.bits;
                    Set(game, field.path, value);
                }
                var shown = new float[battle.Alive.Length]; for (int i = 0; i < shown.Length; i++) shown[i] = battle.GridY(i);
                Set(game, "shownY", shown); saves.Clear();
                var roots = new Roots { game = game, battle = battle, profile = economy.Data, audio = audio };
                if (pointer != null) { PixelWingGame.PaintDeltaOverride = 0f; pointer.Paint(); result.paints += 2; }
                Compare(roots, c.initial);
                foreach (var step in c.steps)
                {
                    if (step.frame != result.frames) throw new Exception("Nonconsecutive frame input"); frame = step.frame;
                    int cursor = 0, projectionCursor = 0;
                    PixelWingGame.FrameProjectionObserver = (screen, logical) =>
                    {
                        result.projectionCalls++;
                        if (step.projections == null || projectionCursor >= step.projections.Length) Fail("projection/count", "end", projectionCursor);
                        else
                        {
                            var expected = step.projections[projectionCursor];
                            CompareBits("projection/" + projectionCursor + "/screen", expected.screenBits, new[] { screen.x, screen.y });
                            CompareBits("projection/" + projectionCursor + "/logical", expected.logicalBits, new[] { logical.x, logical.y });
                        }
                        projectionCursor++;
                    };
                    PixelWingGame.FrameInputOverride = (api, arg) =>
                    {
                        object value; int bits;
                        if (api == 50264) { value = 540; bits = 540; }
                        else if (api == 50265) { value = 960; bits = 960; }
                        else if (api == 29050) { value = BitConverter.Int32BitsToSingle(step.dtBits); bits = step.dtBits; }
                        else if (api == 12300)
                        {
                            if (step.mouse?.positionBits == null || step.mouse.positionBits.Length != 3) throw new Exception("Missing declared mouse position");
                            value = new Vector3(BitConverter.Int32BitsToSingle(step.mouse.positionBits[0]), BitConverter.Int32BitsToSingle(step.mouse.positionBits[1]), BitConverter.Int32BitsToSingle(step.mouse.positionBits[2])); bits = 0;
                        }
                        else
                        {
                            bool pressed = api == 12299 ? step.keys.Length > 0 || step.mouse?.down == true
                                : api == 12289 ? Array.IndexOf(step.keys, arg) >= 0
                                : api == 12290 ? step.mouse?.held == true : api == 12291 ? step.mouse?.down == true : step.mouse?.up == true;
                            if (api != 12299 && api != 12289 && api != 12290 && api != 12291 && api != 12292) throw new Exception("Undeclared native API " + api);
                            value = pressed; bits = pressed ? 1 : 0;
                        }
                        if (cursor >= step.calls.Length) Fail("input/" + cursor, "end", api + ":" + arg);
                        else
                        {
                            var expected = step.calls[cursor]; result.scalarComparisons += 3;
                            if (expected.api != api || expected.arg != arg || expected.bits != bits)
                                Fail("input/" + cursor, expected.api + ":" + expected.arg + ":" + expected.bits, api + ":" + arg + ":" + bits);
                            if (api == 12300 && value is Vector3 mouse) CompareBits("input/" + cursor + "/position", expected.values, new[] { mouse.x, mouse.y, mouse.z });
                        }
                        cursor++; result.inputCalls++; return value;
                    };
                    Call(game, "Update");
                    if (projectionCursor != (step.projections?.Length ?? 0)) Fail("projection/count", step.projections?.Length ?? 0, projectionCursor);
                    if (pointer != null) { pointer.Paint(); result.paints += 2; }
                    if (cursor != step.calls.Length) Fail("input/count", step.calls.Length, cursor);
                    // FleetEconomy commits by replacing Data; observe the current object after each frame.
                    roots.profile = economy.Data; roots.saveCount = saves.Count; Compare(roots, step.checks); result.frames++;
                }
                result.scalarComparisons++; if (saves.Count != c.encodedSaves.Length) Fail("save/count", c.encodedSaves.Length, saves.Count);
                for (int i = 0; i < Math.Min(saves.Count, c.encodedSaves.Length); i++) { result.scalarComparisons++; if (saves[i] != c.encodedSaves[i]) Fail("save/" + i, c.encodedSaves[i], saves[i]); }
                result.phase = (int)battle.State; result.timeBits = BitConverter.SingleToInt32Bits(battle.Time); result.remaining = battle.Remaining;
            }
            finally
            {
                PixelWingGame.FrameInputOverride = null; PixelWingGame.FrameProjectionObserver = null; pointer?.Dispose();
                // Editor fixture teardown: remove its native hierarchy immediately, then
                // let the production pool clear already-destroyed references. No gameplay override.
                UnityEngine.Object.DestroyImmediate(effectOwner);
                effects?.Dispose(); battle?.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static void CompareBits(string path, int[] expected, float[] actual)
        {
            result.scalarComparisons++;
            if (expected == null || expected.Length != actual.Length) { Fail(path + "/length", expected?.Length, actual.Length); return; }
            for (int i = 0; i < actual.Length; i++) { result.scalarComparisons++; int bits = BitConverter.SingleToInt32Bits(actual[i]); if (bits != expected[i]) Fail(path + "/" + i, expected[i], bits); }
        }
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
