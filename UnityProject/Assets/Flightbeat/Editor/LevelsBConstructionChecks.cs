using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Flightbeat.Garden;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Reads isolated converter outputs directly. No scene, PlayerPrefs, resource staging or Levels mutation.
    public static class LevelsBConstructionChecks
    {
        [Serializable] sealed class HashSample { public string name, cellsSha256; public int[] cells; }
        [Serializable] sealed class Fixture
        {
            public string name, path, resource, mode, fileSha256, cellsSha256, expectedError;
            public int game, total, lanes, members;
            public int[] colorTotals;
        }
        [Serializable] sealed class Input { public HashSample[] samples; public Fixture[] cases; }
        [Serializable] sealed class HashResult { public string name, culture, cellsSha256; }
        [Serializable] sealed class CaseResult
        {
            public string name, resource, mode, fileSha256, cellsSha256, error;
            public int game, total, lanes, members;
            public bool passed, rejected;
        }
        [Serializable] sealed class Report
        {
            public bool passed;
            public string unityVersion, error;
            public List<HashResult> hashes = new List<HashResult>();
            public List<CaseResult> cases = new List<CaseResult>();
        }

        static string Arg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            if (at < 0 || at + 1 == args.Length) throw new ArgumentException(key);
            return Path.GetFullPath(args[at + 1]);
        }
        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidDataException(message);
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--levels-b-output");
            if (File.Exists(output)) throw new IOException("Fresh report required: " + output);
            var report = new Report { unityVersion = Application.unityVersion };
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--levels-b-input")));
                Require(input.samples != null && input.samples.Length > 0 && input.cases != null && input.cases.Length > 0,
                    "Empty acceptance input");
                foreach (string culture in new[] { "en-US", "ar-SA" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    foreach (var sample in input.samples)
                    {
                        string actual = GardenLevelHashes.CellsSha256(sample.cells);
                        Require(actual == sample.cellsSha256, "Cells hash mismatch: " + sample.name + " / " + culture);
                        report.hashes.Add(new HashResult { name = sample.name, culture = culture, cellsSha256 = actual });
                    }
                }
                foreach (var fixture in input.cases)
                {
                    var result = new CaseResult { name = fixture.name, resource = fixture.resource, game = fixture.game, mode = fixture.mode };
                    report.cases.Add(result);
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(fixture.path);
                        result.fileSha256 = GardenLevelHashes.FileSha256(bytes);
                        Require(result.fileSha256 == fixture.fileSha256, "File hash mismatch: " + fixture.name);
                        var level = JsonUtility.FromJson<LevelData>(System.Text.Encoding.UTF8.GetString(bytes));
                        result.cellsSha256 = GardenLevelHashes.CellsSha256(level.cells);
                        Require(result.cellsSha256 == fixture.cellsSha256, "Parsed cells hash mismatch: " + fixture.name);
                        try
                        {
                            using var battle = new Battle(level, null, true);
                            Require(String.IsNullOrEmpty(fixture.expectedError), "Constructor accepted invalid fixture");
                            result.total = battle.Total; result.lanes = battle.LaneCount;
                            var ammunition = new int[battle.ColorCount];
                            foreach (var lane in battle.Reserve)
                                foreach (var member in lane)
                                {
                                    result.members++;
                                    ammunition[member.Color] += member.Ammo;
                                }
                            Require(result.total == fixture.total && result.lanes == fixture.lanes && result.members == fixture.members,
                                "Constructed totals differ from Python: " + fixture.name);
                            Require(ammunition.Length == fixture.colorTotals.Length, "Color count mismatch");
                            for (int color = 0; color < ammunition.Length; color++)
                                Require(ammunition[color] == fixture.colorTotals[color], "Color budget mismatch: " + color);
                        }
                        catch (ArgumentException e) when (!String.IsNullOrEmpty(fixture.expectedError) && e.Message.Contains(fixture.expectedError))
                        {
                            result.rejected = true;
                        }
                        result.passed = true;
                    }
                    catch (Exception e) { result.error = e.ToString(); }
                }
                report.passed = report.cases.TrueForAll(c => c.passed);
            }
            catch (Exception e) { report.error = e.ToString(); }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("B level construction checks failed: " + output);
            Debug.Log("LEVELS_B_CONSTRUCTION_PASSED " + output);
        }
    }
}
