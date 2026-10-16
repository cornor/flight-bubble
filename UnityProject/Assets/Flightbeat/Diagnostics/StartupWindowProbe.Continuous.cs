#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    public sealed partial class StartupWindowProbe
    {
        [Serializable] private sealed class Progress
        { public int levelIndex, phase, remaining, merges, maxFusions, lessonStep, tutorialStep, broadcastActions; public float time; public bool broadcastAutomatic, lessonPresent; }
        [Serializable] private sealed class NativeInputs { public bool anyKeyDown, held, down, up; public int[] keys, queriedKeys; public Vector3 position; }
        [Serializable] private sealed class LoopFrame { public int index; public float dt; public double time; public string timeBits; public Progress progress; public int[] order; public NativeInputs native; }
        [Serializable] private sealed class Checkpoint { public string name, encoded; public int frame, guidCount; public Check[] checks; }
        [Serializable] private sealed class Switch { public int frame; public bool enabled; }
        [Serializable] private sealed class NativeResult { public int ordinal, frame, function; public double[] args; public double result; public string resultBits; }
        [Serializable] private sealed class ContinuousInput
        { public bool nativeInputsRecorded; public LoopFrame[] frames; public Checkpoint[] checkpoints; public Switch[] switches; public NativeResult[] random, dsp; }
        [Serializable] private sealed class ContinuousResult
        {
            public int completedFrames, completedCheckpoints, randomConsumed, dspConsumed, finalLevel, finalPhase;
            public bool passed;
            public string carriedSaveKey, carriedSaveValue;
            public SavedPreference[] carriedSaveSlots;
            public List<string> checkpointNames = new List<string>();
        }
        private static int loopCursor = -1, checkpointCursor, randomCursor, dspCursor;
        private static LoopFrame loop;
        private static readonly List<int> loopOrder = new List<int>();
        private static void InstallContinuousInputs()
        {
            report.continuous = new ContinuousResult();
            PixelWingGame.StartupRandomOverride = upper =>
            {
                if (randomCursor >= data.continuous.random.Length) throw new Exception("Extra economy random API call");
                NativeResult expected = data.continuous.random[randomCursor];
                if (loop == null || expected.ordinal != randomCursor || expected.frame != loop.index || expected.function != 28835 ||
                    expected.args == null || expected.args.Length != 2 || expected.args[0] != 0 || expected.args[1] != upper)
                    throw new Exception("Economy random API arguments/order differ at frame " + loopCursor);
                double reply = DeclaredDouble(expected.resultBits, expected.result);
                int value = checked((int)reply);
                if (value != reply) throw new Exception("Noninteger recorded Random.Range result");
                randomCursor++;
                return value;
            };
            report.scope = "Real native Awake, Game.Update, ChipAudio.Update and Canvas rendering through the engine PlayerLoop. Frozen original route, declared API clocks/GUID/RNG/DSP and recorded keyboard/mouse API values (or explicitly no input). All-frame progress plus recorded selected full logical-state checkpoints and exact persisted encodings. No OS input, GPU pixel, native particle or device-audio equivalence claim.";
            PixelWingGame.FrameInputOverride = (id, arg) =>
            {
                if (id == 50264) return Screen.width;
                if (id == 50265) return Screen.height;
                if (id == 29050) return loop == null ? 0f : loop.dt;
                if (data.continuous.nativeInputsRecorded && loop != null)
                {
                    NativeInputs input = loop.native ?? throw new Exception("Missing original native inputs");
                    if (id == 12300) return input.position;
                    if (id == 12299) return input.anyKeyDown;
                    if (id == 12289)
                    {
                        if (input.queriedKeys == null || Array.IndexOf(input.queriedKeys, arg) < 0) throw new Exception("Original keyboard API value not recorded: " + arg);
                        return Array.IndexOf(input.keys, arg) >= 0;
                    }
                    if (id == 12290) return input.held;
                    if (id == 12291) return input.down;
                    if (id == 12292) return input.up;
                }
                if (id == 12300) return Vector3.zero;
                if (id == 12299 || id == 12289 || id == 12290 || id == 12291 || id == 12292) return false;
                throw new Exception("Undeclared frame API " + id);
            };
            RuntimeLoopInputs.AudioInput = id =>
            {
                if (id == 29049) return loop == null ? 0 : loop.time;
                if (id == 29050) return loop == null ? 0 : loop.dt;
                // Original route recording starts after Awake. Its initial native
                // audio schedule is outside this comparison, as in startup stage AH.
                if (loop == null && id == 54169) return AudioSettings.dspTime;
                NativeResult expected;
                if (id == 28836)
                {
                    if (randomCursor >= data.continuous.random.Length) throw new Exception("Extra random API call");
                    expected = data.continuous.random[randomCursor++];
                }
                else if (id == 54169)
                {
                    if (dspCursor >= data.continuous.dsp.Length) throw new Exception("Extra DSP API call");
                    expected = data.continuous.dsp[dspCursor++];
                }
                else throw new Exception("Undeclared audio API " + id);
                if (loop == null || expected.frame != loop.index || expected.function != id) throw new Exception("Native API call differs at frame " + loopCursor + ": " + id + ", expected " + expected.frame);
                double reply = DeclaredDouble(expected.resultBits, expected.result);
                if (id == 54169) ObserveContinuousDspInput(expected.ordinal, reply);
                return reply;
            };
            RuntimeLoopInputs.Method = id => { if (loop != null) loopOrder.Add(id); };
            InstallContinuousDrawCapture();
            InstallDeclaredParticleClock();
            InstallContinuousAudioCapture();
        }
        private static void ResetContinuousInputs()
        {
            PixelWingGame.StartupRandomOverride = null;
            PixelWingGame.FrameInputOverride = null;
            RuntimeLoopInputs.AudioInput = null; RuntimeLoopInputs.Method = null;
            RuntimeLoopInputs.MeshOutput = null; RuntimeLoopInputs.BeginMeshObservation = null;
            commandObservation?.Dispose(); commandObservation = null;
            if (commandSurfaces != null) foreach (var pair in commandSurfaces) pair.Key.QuadObserver = null;
            ResetDeclaredParticleClock();
            SaveContinuousParticleClocks();
            SaveContinuousAudioCapture();
        }
        private void Update()
        {
            if (!continuous || finished) return;
            try
            {
                if (!report.actualAwake || !game) throw new Exception("Continuous PlayerLoop started without real Awake");
                if (Time.realtimeSinceStartupAsDouble - started > 180) throw new Exception("Continuous route timed out");
                loopCursor++; frame = loopCursor;
                if (loopCursor >= data.continuous.frames.Length) throw new Exception("Unobserved frame completion");
                loop = data.continuous.frames[loopCursor]; loopOrder.Clear();
                loop.time = DeclaredDouble(loop.timeBits, loop.time);
                BeginContinuousDrawFrame();
                PixelWingGame.PaintDeltaOverride = loop.dt;
                foreach (var change in data.continuous.switches)
                    if (change.frame == loopCursor) typeof(PixelWingGame).GetMethod("SetBroadcastAutomatic", Flags).Invoke(game, new object[] { change.enabled });
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static double DeclaredDouble(string bits, double legacy)
        {
            if (String.IsNullOrEmpty(bits)) return legacy;
            if (bits.Length != 16) throw new ArgumentException("Declared binary64 input must contain sixteen hex digits");
            return BitConverter.Int64BitsToDouble(unchecked((long)Convert.ToUInt64(bits, 16)));
        }
        private IEnumerator Start()
        {
            if (!continuous) yield break;
            while (!finished)
            {
                yield return new WaitForEndOfFrame();
                if (!finished) ObserveContinuousFrame();
            }
        }
        private void ObserveContinuousFrame()
        {
            try
            {
                if (loop == null) throw new Exception("End of frame without declared input");
                CheckContinuousAudioBoundary();
                string order = String.Join(",", loopOrder), expectedOrder = String.Join(",", loop.order);
                if (order != expectedOrder) Fail("PlayerLoop/method order", expectedOrder, order);
                var progress = new Progress { levelIndex = game.LevelIndex, phase = (int)game.Battle.State, time = game.Battle.Time,
                    remaining = game.Battle.Remaining, merges = game.Battle.Merges, maxFusions = game.Battle.MaxFusions,
                    lessonStep = game.Lesson == null ? 0 : game.Lesson.Step, lessonPresent = game.Lesson != null, tutorialStep = (int)Get(game,"tutorialStep"),
                    broadcastActions = (int)Get(game,"broadcastActions"), broadcastAutomatic = (bool)Get(game,"broadcastAutomatic") };
                foreach (var f in typeof(Progress).GetFields(Flags))
                {
                    result.scalarComparisons++; result.checks++;
                    object actual = f.GetValue(progress), expected = f.GetValue(loop.progress);
                    if (Integer(actual) != Integer(expected)) Fail("progress/" + f.Name, Integer(expected), Integer(actual));
                }
                report.continuous.completedFrames++;
                report.continuous.randomConsumed = randomCursor; report.continuous.dspConsumed = dspCursor;
                report.continuous.finalLevel = game.LevelIndex; report.continuous.finalPhase = (int)game.Battle.State;
                while (checkpointCursor < data.continuous.checkpoints.Length && data.continuous.checkpoints[checkpointCursor].frame == loopCursor + 1)
                {
                    Checkpoint point = data.continuous.checkpoints[checkpointCursor++];
                    SaveContinuousDrawFrame(point);
                    Compare(new Roots { game = game, battle = game.Battle, profile = game.Economy.Data, audio = game.Audio }, point.checks);
                    string encoded = FleetSaveStore.Encode(game.Economy.Data);
                    string key = "pw.fleet.v2." + ((game.Economy.Data.revision & 1) == 0 ? "a" : "b");
                    if (encoded != point.encoded || PlayerPrefs.GetString(key, "") != point.encoded) Fail(point.name + "/encoded saved profile", point.encoded, encoded);
                    if (guidCursor != point.guidCount) Fail(point.name + "/GUID count", point.guidCount, guidCursor);
                    report.continuous.checkpointNames.Add(point.name); report.continuous.completedCheckpoints++;
                }
                if (result.failureCount > 0) { Finish("Continuous original comparison failed"); return; }
                if (loopCursor + 1 == data.continuous.frames.Length)
                {
                    report.continuous.randomConsumed = randomCursor; report.continuous.dspConsumed = dspCursor;
                    report.continuous.finalLevel = game.LevelIndex; report.continuous.finalPhase = (int)game.Battle.State;
                    report.continuous.passed = randomCursor == data.continuous.random.Length && dspCursor == data.continuous.dsp.Length && checkpointCursor == data.continuous.checkpoints.Length;
                    if (report.continuous.passed)
                    {
                        // Observe the actual f54309 slot before the probe restores
                        // user preferences. A later CLI process can reload this
                        // exact candidate save; no expected state is written here.
                        string key = "pw.fleet.v2." + ((game.Economy.Data.revision & 1) == 0 ? "a" : "b");
                        string saved = PlayerPrefs.GetString(key, "");
                        if (saved != FleetSaveStore.Encode(game.Economy.Data)) throw new Exception("Final actual saved slot differs from game profile");
                        report.continuous.carriedSaveKey = key;
                        report.continuous.carriedSaveValue = saved;
                        report.continuous.carriedSaveSlots = new SavedPreference[SaveKeys.Length];
                        for (int i = 0; i < SaveKeys.Length; i++) report.continuous.carriedSaveSlots[i] = new SavedPreference
                        { key = SaveKeys[i], present = PlayerPrefs.HasKey(SaveKeys[i]), value = PlayerPrefs.GetString(SaveKeys[i], "") };
                    }
                    Finish(report.continuous.passed ? null : "Frozen input tape or checkpoints not fully consumed");
                }
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
    }
}
#endif
