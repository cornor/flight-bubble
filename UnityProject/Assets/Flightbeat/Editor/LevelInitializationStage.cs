using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // T2.1 current Unity resource/constructor boundary, using the frozen original
    // Reverse/probe/verify_levels.js result. No Tick, Pilot, saves or gameplay.
    public static class LevelInitializationStage
    {
        [Serializable] private sealed class Row
        {
            public int index, columns, rows, total, remaining, catalogTotal, lanes, state;
            public string name;
            public bool ok;
        }
        [Serializable] private sealed class Oracle { public int levels; public string[] errors; public Row[] rows; }
        [Serializable] private sealed class Result
        {
            public bool passed;
            public string unityVersion, inputSha256, runtimeAssemblySha256, error;
            public int levels, checks, modelTicks, pilotTicks, saveWrites;
            public List<Row> rows = new List<Row>();
        }
        private static Result result;
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static void Check(bool condition, string message)
        {
            result.checks++;
            if (!condition) throw new InvalidDataException(message);
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string key)
            {
                int i = Array.IndexOf(args, key);
                if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(key);
                return Path.GetFullPath(args[i + 1]);
            }
            string input = Arg("--flightbeat-level-init-input"), output = Arg("--flightbeat-level-init-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            result = new Result { unityVersion = Application.unityVersion, inputSha256 = Hash(input),
                runtimeAssemblySha256 = Hash(typeof(Battle).Assembly.Location) };
            try
            {
                Oracle oracle = JsonUtility.FromJson<Oracle>(File.ReadAllText(input));
                Check(oracle.levels == 262 && oracle.rows.Length == oracle.levels && oracle.errors.Length == 0, "Frozen oracle must be complete");
                Check(PixelWingGame.Levels.Length == oracle.levels, "Actual game catalog count");
                for (int index = 0; index < oracle.rows.Length; index++)
                {
                    Row expected = oracle.rows[index];
                    Check(expected.ok && expected.index == index && expected.name == PixelWingGame.Levels[index], "Catalog order at " + index);
                    TextAsset asset = Resources.Load<TextAsset>("Levels/" + PixelWingGame.Levels[index]);
                    Check(asset != null, "Missing level resource " + expected.name);
                    LevelData level = JsonUtility.FromJson<LevelData>(asset.text);
                    // f55010: normal null-pressure resource load, awaitDeployment=true.
                    using (var battle = new Battle(level, null, true))
                    {
                        Check(battle.Level.columns == expected.columns, expected.name + " columns");
                        Check(battle.Level.rows == expected.rows, expected.name + " rows");
                        Check(battle.Total == expected.total, expected.name + " Total");
                        Check(battle.Remaining == expected.remaining, expected.name + " Remaining");
                        Check(battle.Total == expected.catalogTotal, expected.name + " original catalog Total");
                        Check(battle.LaneCount == expected.lanes, expected.name + " lanes");
                        Check((int)battle.State == expected.state, expected.name + " initial Phase");
                        result.rows.Add(new Row { index = index, name = expected.name,
                            columns = level.columns, rows = level.rows, total = battle.Total,
                            remaining = battle.Remaining, catalogTotal = expected.catalogTotal,
                            lanes = battle.LaneCount, state = (int)battle.State, ok = true });
                        result.levels++;
                    }
                }
                result.passed = true;
            }
            catch (Exception e) { result.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(result, true)); }
        }
    }
}
