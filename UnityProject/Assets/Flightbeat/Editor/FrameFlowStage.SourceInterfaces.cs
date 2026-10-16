#if UNITY_EDITOR
using System;
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
    public static partial class FrameFlowStage
    {
        [Serializable] private sealed class FlowEntropy { public int[] values; }
        [Serializable] private sealed class FlowStep { public Check[] checks; public ApiCall[] calls; public int guidCount; }
        [Serializable] private sealed class FlowCase
        {
            public string name, method, levelJson, profileJson;
            public bool nullBattle, existingReward;
            public Check[] fields, battleChanges, initial;
            public FlowStep[] steps;
            public string[] encodedSaves, levelNulls;
            public FlowEntropy[] guidBytes;
        }
        [Serializable] private sealed class FlowInput { public FlowCase[] cases; }
        [Serializable] private sealed class FlowReport
        {
            public bool passed;
            public int operations;
            public string scope = "Direct original flow interfaces, declared pre-call fields and native input/entropy. Full logical Battle/profile/reward/save comparisons. No Awake/PlayerLoop, natural victory, device-audio or native resource-handle equivalence claim. Result failure frame numbers denote method operation indices.";
            public List<Result> cases = new List<Result>();
        }
        private static void ApplyFlowField(object target, Check x)
        {
            Type t = Field(target.GetType(), x.path).FieldType;
            object value = x.kind == "null" ? null : x.kind == "float" ? (object)BitConverter.Int32BitsToSingle(x.bits)
                : x.kind == "string" ? x.text : t == typeof(bool) ? (object)(x.bits != 0)
                : t.IsEnum ? Enum.ToObject(t, x.bits) : (object)x.bits;
            Set(target, x.path, value);
        }
        public static void RunSourceInterfaces()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-flow-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input = JsonUtility.FromJson<FlowInput>(File.ReadAllText(Arg("--flightbeat-flow-input")));
            if (input.cases == null || input.cases.Length == 0) throw new InvalidDataException("No cases");
            var report = new FlowReport();
            var oldInput = PixelWingGame.FrameInputOverride; var oldGuid = PixelWingGame.StartupGuidOverride;
            try
            {
                foreach (var c in input.cases)
                {
                    result = new Result { name = c.name }; report.cases.Add(result); frame = -1;
                    var owner = new GameObject("Direct flow fixture"); owner.SetActive(false);
                    var effectOwner = new GameObject("Direct flow effects", typeof(RectTransform));
                    Battle initialBattle = null; PixelWingGame game = null; PixelEffectPool effects = null;
                    try
                    {
                        game = owner.AddComponent<PixelWingGame>();
                        var audio = owner.AddComponent<ChipAudio>();
                        // The source uses its isolated page's initialized native audio
                        // host. Here real Unity voices are initialized on this owner.
                        // Their handles/schedule times are outside logical assertions.
                        Call(audio, "Awake");
                        var level = JsonUtility.FromJson<LevelData>(c.levelJson);
                        // Preserve explicit nulls in the declared level JSON input.
                        // Unity JsonUtility otherwise expands them into empty arrays/strings.
                        foreach (string path in c.levelNulls)
                        {
                            int slash = path.LastIndexOf('/');
                            Set(slash < 0 ? level : Get(level, path.Substring(0, slash)), path.Substring(slash + 1), null);
                        }
                        initialBattle = new Battle(level, null, true);
                        var saves = new List<string>();
                        var economy = new FleetEconomy(JsonUtility.FromJson<FleetProfile>(c.profileJson), () => 1800000000L,
                            p => saves.Add(FleetSaveStore.Encode(p)), _ => throw new Exception("Undeclared economy random input"));
                        effects = new PixelEffectPool(effectOwner.transform, (RectTransform)effectOwner.transform);
                        Set(game, "Battle", c.nullBattle ? null : initialBattle); Set(game, "Economy", economy);
                        Set(game, "effects", effects); Set(game, "audioChip", audio);
                        foreach (var x in c.fields) ApplyFlowField(game, x);
                        foreach (var x in c.battleChanges) ApplyFlowField(initialBattle, x);
                        if (c.existingReward) Set(game, "LastReward", new FleetReward());
                        int guidCount = 0;
                        PixelWingGame.StartupGuidOverride = () =>
                        {
                            if (guidCount >= c.guidBytes.Length) throw new Exception("Undeclared GUID entropy");
                            var raw = c.guidBytes[guidCount++].values; if (raw.Length != 16) throw new Exception("GUID length");
                            byte[] bytes = new byte[16]; for (int i = 0; i < bytes.Length; i++) bytes[i] = checked((byte)raw[i]);
                            bytes[7] = (byte)((bytes[7] & 15) | 64); bytes[8] = (byte)((bytes[8] & 63) | 128); return new Guid(bytes);
                        };
                        saves.Clear();
                        var roots = new Roots { game = game, battle = game.Battle, profile = economy.Data, audio = audio };
                        Compare(roots, c.initial);
                        for (int i = 0; i < c.steps.Length; i++)
                        {
                            frame = i; var step = c.steps[i]; int cursor = 0;
                            PixelWingGame.FrameInputOverride = (api, arg) =>
                            {
                                if (cursor >= step.calls.Length) throw new Exception("Unexpected input read");
                                var expected = step.calls[cursor++]; result.inputCalls++; result.scalarComparisons += 2;
                                if (api != expected.api || arg != expected.arg) Fail("input/order", expected.api + ":" + expected.arg, api + ":" + arg);
                                if (api != 12299 && api != 12290) throw new Exception("Unexpected input API " + api);
                                return expected.bits != 0;
                            };
                            Call(game, c.method);
                            if (cursor != step.calls.Length) Fail("input/count", step.calls.Length, cursor);
                            if (guidCount != step.guidCount) Fail("GUID/count", step.guidCount, guidCount);
                            roots.battle = game.Battle; roots.profile = economy.Data; roots.saveCount = saves.Count;
                            Compare(roots, step.checks); report.operations++;
                        }
                        if (saves.Count != c.encodedSaves.Length) Fail("save/count", c.encodedSaves.Length, saves.Count);
                        for (int i = 0; i < Math.Min(saves.Count, c.encodedSaves.Length); i++)
                        { result.scalarComparisons++; if (saves[i] != c.encodedSaves[i]) Fail("save/" + i, c.encodedSaves[i], saves[i]); }
                        result.passed = result.failureCount == 0;
                    }
                    catch (Exception e) { result.error = e.ToString(); result.passed = false; }
                    finally
                    {
                        PixelWingGame.FrameInputOverride = null; PixelWingGame.StartupGuidOverride = null;
                        var current = game != null ? game.Battle : null;
                        if (game != null) { Set(game, "Battle", null); Set(game, "effects", null); }
                        current?.Dispose(); if (initialBattle != current) initialBattle?.Dispose();
                        UnityEngine.Object.DestroyImmediate(effectOwner); effects?.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
                    }
                }
                report.passed = report.cases.TrueForAll(c => c.passed);
            }
            finally
            {
                PixelWingGame.FrameInputOverride = oldInput; PixelWingGame.StartupGuidOverride = oldGuid;
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("Flow interface comparison failed: " + output);
        }
    }
}
#endif
