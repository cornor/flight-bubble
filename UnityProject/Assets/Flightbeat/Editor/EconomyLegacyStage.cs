using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Frozen Reverse/probe/verify_economy.js sequence and T0.11 profile records.
    // Calls the formal classes, with no replacement economy/save implementation.
    public static class EconomyLegacyStage
    {
        [Serializable] private sealed class CheckInput { public string name, expectedJson; }
        [Serializable] private sealed class ProfileInput
        {
            public string id, encoded, expectedEncoding, expectedProfile;
            public int expectedTechRefund, expectedGuidConsumed;
        }
        [Serializable] private sealed class Input
        {
            public CheckInput[] checks; public ProfileInput[] profiles;
            public int persistCalls; public int[] rawGuidBytes; public string legacyKey;
        }
        [Serializable] private sealed class CheckRow { public string name, actualJson, expectedJson; public bool passed; }
        [Serializable] private sealed class SaveRow
        {
            public string id, encoded, profileJson; public int techRefund, guidConsumed;
        }
        [Serializable] private sealed class Slot { public string key, value; public bool present; }
        [Serializable] private sealed class Report
        {
            public bool passed, playerPrefsRestored; public string unityVersion, error;
            public int economyChecks, persistCalls, profileCases, profileFields, comparisons, migrationLoads;
            public List<CheckRow> economy = new List<CheckRow>();
            public List<SaveRow> saves = new List<SaveRow>();
            public List<string> failures = new List<string>();
            public List<Slot> before = new List<Slot>(), after = new List<Slot>();
        }
        private static Report report;
        private static readonly string[] Keys = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b", "pw.fleet.v2" };
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public;
        private static void Check(bool pass, string name)
        {
            report.comparisons++;
            if (!pass) report.failures.Add(name);
        }
        // Economy checks contain only strings, bools, integers and nested arrays.
        private static string Json(object value)
        {
            if (value == null) return "null";
            if (value is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            if (value is bool b) return b ? "true" : "false";
            if (value is IEnumerable sequence) return "[" + string.Join(",", sequence.Cast<object>().Select(Json)) + "]";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        private static void Compare(object actual, object expected, string path)
        {
            if (actual == null || expected == null) { Check(actual == expected, path + " null"); return; }
            Type type = expected.GetType();
            if (type.IsPrimitive || expected is string) { Check(actual.Equals(expected), path + ": " + actual + " != " + expected); return; }
            if (expected is IList list)
            {
                var values = (IList)actual; Check(values.Count == list.Count, path + " count");
                for (int i = 0; i < Math.Min(values.Count, list.Count); i++) Compare(values[i], list[i], path + "[" + i + "]");
                return;
            }
            foreach (FieldInfo field in type.GetFields(Fields)) Compare(field.GetValue(actual), field.GetValue(expected), path + "." + field.Name);
        }
        private static SaveRow Save(string id, FleetProfile data, int guidConsumed = 0)
        {
            var row = new SaveRow { id = id, encoded = FleetSaveStore.Encode(data), profileJson = JsonUtility.ToJson(data),
                techRefund = data.techRefund, guidConsumed = guidConsumed };
            report.saves.Add(row); return row;
        }
        private static void Economy(Input input)
        {
            int ordinal = 0; long now = 1800000000L;
            FleetEconomy Fresh() => new FleetEconomy(null, () => now, _ => report.persistCalls++, _ => 0);
            void Record(string name, object actual)
            {
                CheckInput expected = input.checks[ordinal++];
                Check(name == expected.name, "economic sequence " + name);
                string json = Json(actual); bool pass = json == expected.expectedJson;
                Check(pass, name + ": " + json + " != " + expected.expectedJson);
                report.economy.Add(new CheckRow { name = name, actualJson = json, expectedJson = expected.expectedJson, passed = pass });
                report.economyChecks++;
            }
            var e = Fresh();
            Record("new profile currencies", new[] { e.Data.coins, e.Data.fragments });
            Record("new profile inventory", e.Data.items);
            Record("sortie consumes energy", e.SpendSortie() ? 1 : 0); Record("remaining energy", e.Data.energy);
            Record("energy timer starts", e.Data.energyAt.ToString(CultureInfo.InvariantCulture));
            now += 899; Record("no early regeneration", e.Energy); Record("next energy in one second", e.EnergyNextSeconds);
            now++;
            typeof(FleetEconomy).GetMethod("SettleEnergy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(e, null);
            Record("regeneration after settlement at 900 seconds", e.Energy);
            e = Fresh(); e.Data.energy = 0; e.Data.energyAt = now;
            Record("empty cannot sortie", e.SpendSortie() ? 1 : 0);
            e.Data.streak = 1; Record("streak grants free sortie", e.SpendSortie() ? 1 : 0);
            Record("free sortie preserves empty energy", e.Data.energy);
            e.Data.streak = 0; e.Data.unlimitedUntil = now + 60;
            Record("unlimited grants free sortie", e.SpendSortie() ? 1 : 0);
            now += 60; Record("unlimited expires at boundary", e.SpendSortie() ? 1 : 0);
            e = Fresh(); e.Data.coins = 10000;
            Record("buy shuffle", e.BuyItem(0, 1) ? 1 : 0); Record("shuffle costs 600", new object[] { e.Data.coins, e.Data.items });
            Record("buy designate", e.BuyItem(1, 1) ? 1 : 0); Record("designate costs 1000", e.Data.coins);
            Record("buy bomb", e.BuyItem(2, 1) ? 1 : 0); Record("bomb costs 2000", e.Data.coins);
            Record("bundle price", FleetEconomy.BundlePrice); Record("buy bundle", e.BuyBundle() ? 1 : 0);
            Record("bundle adds two each", new object[] { e.Data.coins, e.Data.items });
            e.Data.items[0] = 99; Record("inventory cap 99", e.BuyItem(0, 1) ? 1 : 0);
            e = Fresh();
            for (int level = 1; level < 5; level++)
            {
                e.Data.spares[0] = new[] { 0, 1, 2, 4, 8, 12 }[level + 1];
                Record("part upgrade cost " + level, e.UpgradeCost(0, 0));
                Record("part upgrade " + level, e.UpgradePart(0, 0) ? 1 : 0);
                Record("part level/spares after upgrade " + level, new[] { e.Data.parts[0], e.Data.spares[0] });
            }
            Record("max part upgrade rejected", e.UpgradePart(0, 0) ? 1 : 0); Record("part upgrades cost no coins", e.Data.coins);
            e = Fresh(); Record("single part draw", e.DrawParts(1, 0) != null); Record("single draw costs 10 fragments", e.Data.fragments);
            e.Data.fragments = 90; Record("ten part draws", e.DrawParts(10, 0) != null); Record("ten draws cost 90 fragments", e.Data.fragments);
            // JSON fields are strings in both original and formal Envelope types.
            SaveRow saved = Save("legacy-economy-final", e.Data);
            var envelope = JsonUtility.FromJson<Envelope>(saved.encoded);
            Record("save envelope stores JSON string", envelope.payload == null ? "null" : "string");
            Record("save envelope includes hash", envelope.hash == null ? "null" : "string");
            Check(ordinal == input.checks.Length && ordinal == 43, "all 43 named economic checks");
            Check(report.persistCalls == input.persistCalls, "original persistence callback count");
        }
        [Serializable] private sealed class Envelope { public string payload, hash; }
        private static void Profiles(Input input)
        {
            int consumed = 0;
            Guid DeclaredGuid()
            {
                if (consumed++ != 0) throw new InvalidOperationException("Declared GUID input exhausted");
                byte[] bytes = input.rawGuidBytes.Select(x => checked((byte)x)).ToArray();
                // f70385 masks raw f66600 entropy before constructing the GUID.
                bytes[8] = (byte)((bytes[8] & 63) | 128); bytes[7] = (byte)((bytes[7] & 15) | 64);
                return new Guid(bytes);
            }
            FleetEconomy.NormalizeGuidOverride = DeclaredGuid;
            foreach (ProfileInput p in input.profiles)
            {
                consumed = 0;
                FleetProfile data = FleetSaveStore.Decode(p.encoded);
                if (data == null) throw new Exception("Decode rejected " + p.id);
                var expected = JsonUtility.FromJson<FleetProfile>(p.expectedProfile); expected.techRefund = p.expectedTechRefund;
                Compare(data, expected, p.id); report.profileFields += typeof(FleetProfile).GetFields(Fields).Length;
                Check(consumed == p.expectedGuidConsumed, p.id + " GUID count");
                var saved = Save(p.id, data, consumed);
                Check(saved.encoded == p.expectedEncoding, p.id + " complete re-encoding");
                consumed = 0; Compare(FleetSaveStore.Decode(saved.encoded), expected, p.id + " roundtrip");
                Check(consumed == 0, p.id + " v6 roundtrip GUID count");
                report.profileCases++;
                if (p.expectedGuidConsumed == 0) continue;
                // Actual legacy PlayerPrefs key selection, with all five keys restored.
                foreach (string key in Keys) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.SetString(input.legacyKey, p.encoded);
                consumed = 0; FleetProfile loaded = FleetSaveStore.Load();
                Compare(loaded, expected, p.id + " actual v1-key Load");
                Check(consumed == p.expectedGuidConsumed, p.id + " Load GUID count");
                Check(Save(p.id + "-legacy-key-load", loaded, consumed).encoded == p.expectedEncoding, p.id + " Load encoding");
                report.migrationLoads++;
            }
            Check(report.profileCases == 10 && report.profileFields == 610 && report.migrationLoads == 1, "full profile fixture coverage");
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 == args.Length) throw new ArgumentException(name); return Path.GetFullPath(args[i + 1]); }
            string output = Arg("--flightbeat-economy-legacy-report");
            if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion };
            foreach (string key in Keys) report.before.Add(new Slot { key = key, present = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key, "") });
            File.WriteAllText(output + ".prefs-backup.json", JsonUtility.ToJson(report, true));
            var prior = FleetEconomy.NormalizeGuidOverride;
            try
            {
                FleetEconomy.NormalizeGuidOverride = null;
                Input input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-economy-legacy-input")));
                Economy(input); Profiles(input);
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally
            {
                FleetEconomy.NormalizeGuidOverride = prior;
                foreach (Slot slot in report.before) { if (slot.present) PlayerPrefs.SetString(slot.key, slot.value); else PlayerPrefs.DeleteKey(slot.key); }
                PlayerPrefs.Save();
                foreach (string key in Keys) report.after.Add(new Slot { key = key, present = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key, "") });
                report.playerPrefsRestored = report.before.Zip(report.after, (a, b) => a.key == b.key && a.present == b.present && a.value == b.value).All(x => x);
                Check(report.playerPrefsRestored, "PlayerPrefs restore");
                report.passed = report.error == null && report.failures.Count == 0;
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("Economy/profile mismatch: " + report.failures[0]);
        }
    }
}
