using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    // Replays the scoped source model_driver/input_context adapter, not Game.Update.
    // Pilot executes normally to retain query side effects; frozen actions are checks,
    // never replacements. Original rich profile is a declared fixture, not progression.
    public static partial class ModelCampaignStage
    {
        [Serializable] private sealed class Check { public string path, kind, text; public int bits; public int[] values; }
        [Serializable] private sealed class Checks { public Check[] checks; }
        [Serializable] private sealed class ActionInput { public string action, resultKind; public int[] args, aimBits; public bool result; }
        [Serializable] private sealed class Controller { public int aimKind, aimAgeBits, rescueAgeBits, rescueAimAtBits, saveCount; public int[] aimBits, items; }
        [Serializable] private sealed class Step { public int frame, dtBits; public bool paused; public ActionInput[] actions; public Controller controller; public Check[] checks; }
        [Serializable] private sealed class Input
        {
            public string name, mode, profileJson, mechanism, levelJson, pressureJson, steps;
            public int levelIndex, pilotSeed, frames, expectedPhase, expectedRemaining, expectedTimeBits, rescueAgeMinimumBits;
            public int[] unlockBays, deployLanes, itemKinds;
            public Check[] initial; public Checks[] saves; public Checkpoint[] checkpoints;
        }
        [Serializable] private sealed class Case { public string name, input; }
        [Serializable] private sealed class Campaign { public Case[] cases; }
        [Serializable] private sealed class Failure { public int frame; public string path, expected, actual; }
        [Serializable] private sealed class Result
        {
            public string name, error;
            public bool passed, arrayHashesPending = true;
            public int frames, actions, checks, scalarComparisons, failureCount, phase, remaining, saves, pilotTicks, checkpoints;
            public bool scripted;
            public double seconds;
            public List<Failure> failures = new List<Failure>();
        }
        [Serializable] private sealed class Report { public string unityVersion; public bool passed; public List<Result> cases = new List<Result>(); }
        [Serializable] private sealed class Arrays { public int frame; public int[] Alive, sand, x, y, flow, position, fronts; }
        private sealed class Roots { public Battle battle; public FleetProfile profile; }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static Result result;
        private static int frame;
        private static FieldInfo Field(Type t, string n) => t.GetField(n, Flags) ?? t.GetField("<" + n + ">k__BackingField", Flags) ?? throw new MissingFieldException(t.FullName, n);
        private static void Set(object t, string n, object v) => Field(t.GetType(), n).SetValue(t, v);
        private static object Call(object t, string n, params object[] a) => t.GetType().GetMethod(n, Flags).Invoke(t, a);
        private static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); if (i < 0 || i + 1 == a.Length) throw new ArgumentException(n); return Path.GetFullPath(a[i + 1]); }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            ConfigureCompression();
            string output = Arg("--flightbeat-campaign-output"); if (Directory.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(output);
            var report = new Report { unityVersion = Application.unityVersion };
            var input = JsonUtility.FromJson<Campaign>(File.ReadAllText(Arg("--flightbeat-campaign-input")));
            foreach (var c in input.cases)
            {
                result = new Result { name = c.name }; report.cases.Add(result); frame = -1;
                var watch = System.Diagnostics.Stopwatch.StartNew(); string folder = Path.Combine(output, c.name); Directory.CreateDirectory(folder);
                try { Execute(JsonUtility.FromJson<Input>(File.ReadAllText(c.input)), folder); result.passed = result.failureCount == 0; }
                catch (Exception e) { result.error = e.ToString(); result.passed = false; }
                finally { result.seconds = watch.Elapsed.TotalSeconds; File.WriteAllText(Path.Combine(folder, "report.json"), JsonUtility.ToJson(result, true)); }
                Debug.Log("Model campaign " + c.name + ": " + result.passed + ", frames=" + result.frames + ", failures=" + result.failureCount);
            }
            report.passed = report.cases.TrueForAll(c => c.passed);
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
            if (!report.passed) throw new Exception("Model campaign differs: " + output);
        }
        private static void Execute(Input c, string folder)
        {
            var owner = new GameObject("Frozen model fixture"); owner.SetActive(false);
            Battle battle = null, template = null; PixelEffectPool effects = null;
            try
            {
                ScreenLayout.Configure(540, 960, new Rect(0, 0, 1, 1));
                var game = owner.AddComponent<PixelWingGame>(); var audio = owner.AddComponent<ChipAudio>();
                var level = JsonUtility.FromJson<LevelData>(c.levelJson); var pressure = JsonUtility.FromJson<PressureSettings>(c.pressureJson);
                RestoreNulls(level, "battle/Level/", c.initial); RestoreNulls(pressure, "battle/Pressure/", c.initial);
                battle = new Battle(level, pressure, true);
                // Literal fixture construction in frozen Tools/recorder/input_context.js.
                var profile = new FleetProfile { version = 6, revision = 0, coins = 99999999, fragments = 9999999, energy = 999,
                    unlocked = 262, items = new[] { 99, 99, 99 }, activeFighter = 0, assembled = 31, parts = new int[25], spares = new int[25],
                    seenUtc = 1800000000, energyAt = 1800000000, offerSeed = 1337,
                    bombTutorial = true, growthTutorial = true, independentTutorial = true, finaleTutorial = true };
                for (int i = 0; i < profile.parts.Length; i++) profile.parts[i] = 5;
                bool scripted = c.mode == "revive" || c.mode == "mechanism"; result.scripted = scripted;
                if (!scripted && !String.IsNullOrEmpty(c.mode) && c.mode != "wins") throw new Exception("Unknown campaign mode " + c.mode);
                if (scripted)
                {
                    if (String.IsNullOrEmpty(c.profileJson)) throw new Exception("Missing declared special profile");
                    profile = JsonUtility.FromJson<FleetProfile>(c.profileJson);
                }
                var saves = new List<FleetProfile>();
                var economy = new FleetEconomy(FleetSaveStore.Decode(FleetSaveStore.Encode(profile)), () => 1800000000L,
                    p => saves.Add(p), _ => throw new Exception("Undeclared economy random"));
                effects = new PixelEffectPool(null, null);
                // The original copies feedback from a different Battle, then changes
                // its battle pointer; no feedback event subscriptions on the tested model.
                template = new Battle(level, pressure, true);
                var feedback = new CombatFeedback(template, effects, CombatPalette.ForLevel(level)); Set(feedback, "battle", battle);
                Set(game, "Battle", battle); Set(game, "Economy", economy); Set(game, "feedback", feedback);
                Set(game, "effects", effects); Set(game, "audioChip", audio); Set(game, "levelIndex", c.levelIndex); Set(game, "InHome", false);
                Call(game, "SetupSortie");
                var pilot = new BroadcastPilot(battle.ColorCount, c.pilotSeed); bool ownAim = false;
                var roots = new Roots { battle = battle, profile = economy.Data }; Compare(roots, c.initial);
                var special = scripted ? new SpecialScript(c, game, battle, feedback, economy, saves) : null;
                using var reader = OpenSteps(c.steps);
                using var arrays = OpenArrays(folder);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var step = JsonUtility.FromJson<Step>(line); frame = step.frame;
                    if (frame != result.frames) throw new Exception("Nonconsecutive frame");
                    float dt = BitConverter.Int32BitsToSingle(step.dtBits); var actions = new List<ActionInput>();
                    void Record(string name, int[] args, object value, int[] aim = null) => actions.Add(new ActionInput {
                        action = name, args = args, resultKind = value == null ? "null" : "bool", result = value is bool b && b, aimBits = aim ?? Array.Empty<int>() });
                    if (special != null) special.BeforeTick(actions);
                    else if ((int)Get(game, "selection") == 2)
                    {
                        if (ownAim)
                        {
                            float age = StoredFloat((float)Get(game, "broadcastAimAge") + dt); Set(game, "broadcastAimAge", age);
                            if (age >= .8f)
                            {
                                var aim = (Vector2)Get(game, "aim");
                                Record("superStrike", Array.Empty<int>(), Call(game, "ConfirmSuperStrike"), new[] { Bits(aim.x), Bits(aim.y) });
                                Set(game, "broadcastAimAge", 0f);
                            }
                        }
                    }
                    else if (!game.Paused)
                    {
                        result.pilotTicks++; BroadcastDecision decision = pilot.Tick(battle, dt);
                        switch (decision.action)
                        {
                            case BroadcastAction.Wait: break;
                            case BroadcastAction.Deploy: Record("deploy", new[] { decision.index }, battle.Deploy(decision.index)); break;
                            case BroadcastAction.Bay: economy.CountAd(); Record("unlockBay", new[] { decision.index }, battle.UnlockBay(decision.index)); break;
                            case BroadcastAction.Bomb:
                                if (economy.Data.items[2] <= 0) throw new Exception("Undeclared replenishment input");
                                Record("useProp", new[] { 2 }, Call(game, "UseProp", 2)); Set(game, "broadcastAimAge", 0f); ownAim = (int)Get(game, "selection") == 2; break;
                            case BroadcastAction.Finale: Record("launchFinale", Array.Empty<int>(), Call(game, "StartFinale")); break;
                            case BroadcastAction.Rescue:
                                if (feedback.RescueAge >= 2.5f) { economy.CountAd(); Record("revive", Array.Empty<int>(), Call(game, "Rescue")); ownAim = true; } break;
                            default: throw new Exception("Undeclared Pilot action " + decision.action);
                        }
                    }
                    CompareActions(step.actions, actions);
                    Set(game, "ui", StoredFloat((float)Get(game, "ui") + dt)); Call(game, "UpdateSuperAim", dt);
                    if (!game.Paused || (int)battle.State >= 3) Call(feedback, "TickPressure", dt);
                    var previous = battle.State; if (!game.Paused) battle.Tick(dt);
                    if (previous != Phase.ReviveOffer && battle.State == Phase.ReviveOffer) Call(feedback, "Cue", "down");
                    Equal("paused", step.paused, game.Paused);
                    roots.profile = economy.Data; Compare(roots, step.checks); CompareController(game, feedback, economy, saves.Count, step.controller);
                    arrays.WriteLine(JsonUtility.ToJson(new Arrays { frame = frame, Alive = Values(Get(battle, "Alive")), sand = Values(Get(battle, "sand")),
                        x = Values(Get(battle, "x")), y = Values(Get(battle, "y")), flow = Values(Get(battle, "flow")),
                        position = Values(Get(battle, "position")), fronts = Values(Get(battle, "fronts")) }));
                    result.frames++;
                    special?.AfterTick();
                }
                Equal("frames", c.frames, result.frames); Equal("saves/count", c.saves.Length, saves.Count);
                for (int i = 0; i < Math.Min(c.saves.Length, saves.Count); i++) Compare(saves[i], c.saves[i].checks);
                result.saves = saves.Count; result.phase = (int)battle.State; result.remaining = battle.Remaining;
                Equal("terminal/phase", String.IsNullOrEmpty(c.mode) ? (int)Phase.Won : c.expectedPhase, result.phase);
                Equal("terminal/remaining", String.IsNullOrEmpty(c.mode) ? 0 : c.expectedRemaining, result.remaining);
                if (!String.IsNullOrEmpty(c.mode)) Equal("terminal/time", c.expectedTimeBits, Bits(battle.Time));
                special?.Finish();
                // Source close() verifies these two complete immutable inputs again.
                var configuration = new List<Check>();
                foreach (var check in c.initial)
                    if (check.path.StartsWith("battle/Level/", StringComparison.Ordinal) || check.path.StartsWith("battle/Pressure/", StringComparison.Ordinal)) configuration.Add(check);
                Compare(roots, configuration.ToArray());
            }
            finally
            {
                // Prevent Unity OnDestroy from disposing the same model twice.
                var game = owner.GetComponent<PixelWingGame>(); if (game != null) { Set(game, "Battle", null); Set(game, "effects", null); }
                battle?.Dispose(); template?.Dispose(); effects?.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static void CompareActions(ActionInput[] expected, List<ActionInput> actual)
        {
            Equal("actions/count", expected.Length, actual.Count); result.actions += actual.Count;
            for (int i = 0; i < Math.Min(expected.Length, actual.Count); i++)
            {
                var e = expected[i]; var a = actual[i]; Equal("action/name", e.action, a.action); Equal("action/result-kind", e.resultKind, a.resultKind);
                if (e.resultKind == "bool") Equal("action/result", e.result, a.result);
                CompareArray("action/args", e.args, a.args); CompareArray("action/aim", e.aimBits, a.aimBits);
            }
        }
        private static void CompareController(PixelWingGame g, CombatFeedback f, FleetEconomy e, int saves, Controller c)
        {
            var aim = (Vector2)Get(g, "aim"); Equal("controller/aimKind", c.aimKind, Get(g, "selection"));
            CompareArray("controller/aim", c.aimBits, new[] { Bits(aim.x), Bits(aim.y) });
            Equal("controller/aimAge", c.aimAgeBits, Bits((float)Get(g, "broadcastAimAge")));
            Equal("controller/rescueAge", c.rescueAgeBits, Bits(f.RescueAge)); Equal("controller/rescueAimAt", c.rescueAimAtBits, Bits((float)Get(g, "rescueAimAt")));
            CompareArray("controller/items", c.items, e.Data.items); Equal("controller/saves", c.saveCount, saves);
        }
        private static object Get(object value, string path)
        {
            if (path.Length == 0) return value;
            foreach (string name in path.Split('/'))
            {
                if (value == null) return null;
                if (int.TryParse(name, out int i))
                { if (value is IList list) value = i < list.Count ? list[i] : null; else { var e = ((IEnumerable)value).GetEnumerator(); for (int n = 0; n <= i; n++) if (!e.MoveNext()) return null; value = e.Current; } }
                else { var t = value.GetType(); var f = t.GetField(name, Flags) ?? t.GetField("<" + name + ">k__BackingField", Flags); value = f != null ? f.GetValue(value) : t.GetProperty(name, Flags)?.GetValue(value) ?? throw new MissingFieldException(t.FullName, name); }
            }
            return value;
        }
        private static void RestoreNulls(object root, string prefix, Check[] checks)
        {
            foreach (var c in checks) if (c.kind == "null" && c.path.StartsWith(prefix, StringComparison.Ordinal))
            { string path = c.path.Substring(prefix.Length); int at = path.LastIndexOf('/'); Set(at < 0 ? root : Get(root, path.Substring(0, at)), path.Substring(at + 1), null); }
        }
        // Original recorder writes these adapter clocks directly to f32 memory.
        private static float StoredFloat(float f) => BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(f));
        private static int Bits(float f) => BitConverter.SingleToInt32Bits(f);
        private static int Integer(object v) => v is float f ? Bits(f) : v is bool b ? (b ? 1 : 0) : v is uint u ? unchecked((int)u) : Convert.ToInt32(v);
        private static int[] Values(object v)
        {
            if (v is int[] integers) return integers;
            if (v is float[] floats) { var bits = new int[floats.Length]; for (int i = 0; i < bits.Length; i++) bits[i] = Bits(floats[i]); return bits; }
            if (v is bool[] booleans) { var bits = new int[booleans.Length]; for (int i = 0; i < bits.Length; i++) bits[i] = booleans[i] ? 1 : 0; return bits; }
            var r = new List<int>(); foreach (var x in (IEnumerable)v) r.Add(Integer(x)); return r.ToArray();
        }
        private static void Equal(string path, object e, object a)
        { result.scalarComparisons++; if (!Equals(e, a)) { result.failureCount++; if (result.failures.Count < 30) result.failures.Add(new Failure { frame = frame, path = path, expected = e?.ToString() ?? "null", actual = a?.ToString() ?? "null" }); } }
        private static void CompareArray(string path, int[] e, int[] a)
        {
            Equal(path + "/length", e.Length, a.Length);
            int count = Math.Min(e.Length, a.Length); result.scalarComparisons += count;
            // Same integer-bit comparisons, without boxing matching elements or
            // allocating their diagnostic paths. Mismatch counts/paths are retained.
            for (int i = 0; i < count; i++) if (e[i] != a[i])
            {
                result.failureCount++;
                if (result.failures.Count < 30) result.failures.Add(new Failure { frame = frame, path = path + "/" + i, expected = e[i].ToString(), actual = a[i].ToString() });
            }
        }
        private static void Compare(object root, Check[] checks)
        {
            foreach (var c in checks)
            {
                result.checks++; object a = Get(root, c.path);
                if (c.kind == "null") Equal(c.path, true, a == null);
                else if (c.kind == "object") Equal(c.path, true, a != null);
                else if (c.kind == "string" || c.kind == "long" || c.kind == "ulong") Equal(c.path, c.text, a?.ToString());
                else if (c.kind == "count") Equal(c.path, c.bits, a is ICollection col ? col.Count : -1);
                else if (c.kind.EndsWith("[]")) { if (a == null) Equal(c.path, "array", null); else CompareArray(c.path, c.values, Values(a)); }
                else Equal(c.path, c.bits, a == null ? null : (object)Integer(a));
            }
        }
    }
}
