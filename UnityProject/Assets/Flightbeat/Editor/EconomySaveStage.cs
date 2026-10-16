using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Stage V: original T0.6 save fixtures plus WAT-derived contract boundaries.
    // No new original recording, battle replay, screenshots or player automation.
    public static class EconomySaveStage
    {
        [Serializable] private sealed class Pair { public string name, key, value; }
        [Serializable] private sealed class State { public int version, revision, coins; }
        [Serializable] private sealed class Case
        {
            public string name, kind, target;
            public int version;
            public bool rejected;
            public Pair[] slots;
            public State expected;
        }
        [Serializable] private sealed class Input { public string source, sourceSha256; public Pair[] fixtures; public Case[] cases; }
        [Serializable] private sealed class Report
        {
            public bool passed, playerPrefsRestored;
            public string unityVersion, source, sourceSha256, error;
            public int originalCases, originalEncodings, sourceAssertions;
            public List<string> checks = new List<string>();
        }
        private static Report report;
        private static readonly string[] Keys = { "pw.fleet.v2.a", "pw.fleet.v2.b", "pw.fleet.v1.a", "pw.fleet.v1.b", "pw.fleet.v2" };
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string key) { int i = Array.IndexOf(args, key); if (i < 0 || i + 1 == args.Length) throw new ArgumentException(key); return Path.GetFullPath(args[i + 1]); }
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--flightbeat-economy-save-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            report = new Report { unityVersion = Application.unityVersion };
            try
            {
                Input input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-economy-save-input")));
                report.source = input.source; report.sourceSha256 = input.sourceSha256;
                OriginalSaves(input); SaveBoundaries(); EconomyInterfaces(); report.passed = true;
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool pass, string name)
        {
            report.sourceAssertions++;
            if (!pass) throw new Exception(name);
            report.checks.Add(name);
        }
        private static void OriginalSaves(Input input)
        {
            var fixtures = input.fixtures.ToDictionary(p => p.name, p => p.value);
            foreach (string name in new[] { "a4", "b5", "b4", "v1", "negativeRevision" })
            {
                int revision = name == "a4" || name == "b4" ? 4 : name == "b5" ? 5 : name == "v1" ? 90 : -1;
                int coins = name == "a4" ? 404 : name == "b4" ? 414 : name == "b5" ? 505 : name == "v1" ? 190 : 777;
                string actual = FleetSaveStore.Encode(new FleetProfile { revision = revision, coins = coins, version = name == "v1" ? 1 : 6 });
                if (actual != fixtures[name]) throw new Exception("Original encoded bytes: " + name);
                report.originalEncodings++;
            }
            var backup = Keys.ToDictionary(k => k, k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null);
            try
            {
                foreach (Case c in input.cases)
                {
                    FleetProfile actual;
                    if (c.kind == "load")
                    {
                        foreach (string key in Keys) PlayerPrefs.DeleteKey(key);
                        foreach (Pair slot in c.slots) PlayerPrefs.SetString(slot.key, slot.value);
                        actual = FleetSaveStore.Load();
                    }
                    else if (c.kind == "decode")
                        actual = FleetSaveStore.Decode(FleetSaveStore.Encode(new FleetProfile { version = c.version, revision = 12, coins = 312 }));
                    else
                    {
                        foreach (string key in Keys) PlayerPrefs.SetString(key, fixtures["a4"]);
                        actual = new FleetProfile { revision = c.expected.revision, coins = c.expected.coins };
                        string encoded = FleetSaveStore.Encode(actual); FleetSaveStore.Save(actual);
                        foreach (string key in Keys)
                            if (PlayerPrefs.GetString(key) != (key == c.target ? encoded : fixtures["a4"])) throw new Exception("Save slot isolation: " + c.name);
                    }
                    if (c.rejected ? actual != null : actual == null || actual.version != c.expected.version
                        || actual.revision != c.expected.revision || actual.coins != c.expected.coins)
                        throw new Exception("Original recorded case: " + c.name);
                    report.originalCases++;
                }
            }
            finally
            {
                foreach (var pair in backup) { if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetString(pair.Key, pair.Value); }
                PlayerPrefs.Save();
                report.playerPrefsRestored = Keys.All(k => (PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null) == backup[k]);
                if (!report.playerPrefsRestored) throw new Exception("PlayerPrefs restoration failed");
            }
        }
        private static FleetProfile Decode(FleetProfile data) => FleetSaveStore.Decode(FleetSaveStore.Encode(data));
        private static void SaveBoundaries()
        {
            void Reject(string name, Action<FleetProfile> mutate)
            {
                var d = new FleetProfile(); mutate(d); Check(Decode(d) == null, "f54305 reject " + name);
            }
            Reject("negative coins", d => d.coins = -1);
            Reject("negative pity", d => d.epicPity = -1);
            Reject("items over cap", d => d.items[2] = 100);
            Reject("missing slots", d => d.slots = null);
            Reject("negative spare", d => d.spares[24] = -1);
            Reject("spare over cap", d => d.spares[24] = 10000);
            Reject("negative part", d => d.parts[24] = -1);
            Reject("part over cap", d => d.parts[24] = 6);
            Reject("assembled missing part", d => d.parts[0] = 0);
            Reject("invalid assembly mask", d => d.assembled = int.MinValue | 1);
            Reject("active fighter unassembled", d => d.activeFighter = 1);
            Reject("duplicate cleared", d => d.cleared.AddRange(new[] { 1, 1 }));
            Reject("attempt beyond unlocked", d => { d.attempt = "x"; d.attemptLevel = 2; });
            Reject("negative streak", d => d.streak = -1);
            Reject("energy over cap", d => d.energy = 1000);
            Reject("unknown preset", d => d.presets[0] = 1);
            Reject("duplicate ids", d => { d.cores.Add(new FleetCore { id = 1 }); d.cores.Add(new FleetCore { id = 1 }); d.nextCore = 2; });
            Reject("nextCore not greater", d => { d.cores.Add(new FleetCore { id = 2 }); d.nextCore = 1; });
            Reject("durability below -1", d => { d.cores.Add(new FleetCore { id = 1, durability = -2 }); d.nextCore = 2; });
            var valid = new FleetProfile { nextCore = 2 };
            var core = new FleetCore { id = 1, kind = 0, quality = 0 };
            valid.cores.Add(core); int slot = core.Slot; valid.presets[slot] = 1;
            Check(Decode(valid) != null, "f54305 valid single preset");
            // Cross-fighter same-slot duplication passes slot/type guards but
            // must fail the original final HashSet.Add loop.
            valid.presets[5 + slot] = 1;
            Check(Decode(valid) == null, "f54305 duplicate preset across fighters");
            valid.presets[5 + slot] = 0; valid.slots[slot] = 1;
            Check(Decode(valid) != null, "f54305 legacy slots are not counted as duplicate presets");
            foreach (int level in new[] { 1, 62, 63, 92 })
            {
                var old = new FleetProfile { version = 5, unlocked = level, attempt = "migration", attemptLevel = level };
                old.cleared.Add(level); FleetProfile migrated = Decode(old); int shifted = level > 62 ? level + 20 : level;
                Check(migrated != null && migrated.version == 6 && migrated.unlocked == shifted
                    && migrated.cleared[0] == shifted && migrated.attemptLevel == shifted, "f54307 shift " + level);
            }
            var v2 = new FleetProfile { version = 2, unlocked = 30, attempt = "old", attemptLevel = 30, wins = 1 };
            v2.cleared.Add(30); FleetProfile v6 = Decode(v2);
            Check(v6 != null && v6.unlocked == 32 && v6.attemptLevel == 32 && v6.cleared.SequenceEqual(new[] { 1, 2, 32 })
                && v6.growthTutorial && v6.independentTutorial && v6.energy == 5, "f54305 v2 inserts teaching levels and continues migration");
            var empty = Decode(new FleetProfile { version = 2, attempt = "old", attemptLevel = 1 });
            Check(empty != null && empty.unlocked == 1 && empty.attempt == "" && empty.attemptLevel == 0, "f54305 v2 untouched campaign clears attempt");
            foreach (int version in new[] { 2, 3, 4, 5, 6 })
            {
                int limit = version == 2 ? 30 : version < 6 ? 92 : 262;
                Check(Decode(new FleetProfile { version = version, unlocked = limit }) != null, "f54305 version level cap " + version);
                Check(Decode(new FleetProfile { version = version, unlocked = limit + 1 }) == null, "f54305 version over level cap " + version);
            }
            long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), now = FleetSaveStore.Utc(), after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Check(now >= before && now <= after, "f54310 UTC seconds");
        }
        private static void EconomyInterfaces()
        {
            int saved = 0, clockCalls = 0; bool throwSave = false;
            long now = 1791604800;
            var e = new FleetEconomy(new FleetProfile(), () => { clockCalls++; return now; },
                d => { saved++; if (throwSave) throw new IOException("injected persistence failure"); }, n => 0);
            e.Data.cores.Clear(); Array.Clear(e.Data.presets, 0, 25); e.Data.nextCore = 100;
            Check(e.Equipped == null, "f54065 empty equipment");
            for (int slot = 4; slot >= 0; slot--)
            {
                int kind = Array.FindIndex(FleetCatalog.Modules, m => m.slot == slot && !m.retired);
                e.Data.cores.Add(new FleetCore { id = slot + 1, kind = kind, durability = slot == 0 ? 0 : -1 });
                Check(e.Equip(slot + 1) && e.Equipped.id == slot + 1, "f53996/f54065 first slot " + slot);
            }
            int previous = saved;
            Check(!e.Equip(1) && !e.Equip(999) && !e.Unequip(-1) && saved == previous, "f53996/f54068 rejected operations do not commit");
            e.Data.activeFighter = 2;
            Check(e.Equip(1) && e.Data.presets[0] == 0 && e.Data.presets[10] == 1 && e.Equipped.id == 1, "f53996 current fighter and cross-preset move");
            Check(e.Unequip(0) && e.Equipped == null, "f54068 current fighter only");
            e.Data.activeFighter = 0;
            Check(e.Equipped.id == 2, "f54065 first remaining preset");
            e.Data.spares = new[] { int.MaxValue, 1, -2, 3, 4 };
            Check(e.TotalSpares == int.MinValue + 5, "f54093 full-length wrapping sum");
            e.Data.spares = new int[0]; Check(e.TotalSpares == 0, "f54093 empty array sum");
            foreach (var item in new[] { new[] { int.MinValue, 0 }, new[] { 0, 0 }, new[] { 1, 10 }, new[] { 2, 10 }, new[] { 3, 30 }, new[] { 5, 30 }, new[] { 6, 50 }, new[] { int.MaxValue, 50 } })
            { e.Data.streak = item[0]; Check(e.StreakPercent == item[1], "f54110 streak tier " + item[0]); }
            e.Data.spares = new int[25]; e.Data.officerChats = int.MaxValue;
            int day = e.Data.dayKey, revision = e.Data.revision; now += 86400; previous = saved; int clocks = clockCalls;
            e.Chat();
            Check(e.Data.officerChats == int.MinValue && e.Data.revision == revision + 1 && saved == previous + 1
                && clockCalls == clocks + 1 && e.Data.dayKey == day, "f54150/f54251 wrapping increment, one commit, no daily refresh");
            FleetProfile before = e.Data; throwSave = true; bool threw = false;
            try { e.Chat(); } catch (IOException) { threw = true; }
            Check(threw && ReferenceEquals(e.Data, before) && e.Data.officerChats == int.MinValue, "f53956/f54150 persistence failure does not publish clone");
        }
    }
}
