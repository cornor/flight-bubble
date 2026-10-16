using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.Economy
{
    // Source: metadata type 4378 and f54303..f54311. Keep the original two-slot
    // save format, validation order, migration chain and invalid-save behavior.
    public static class FleetSaveStore
    {
        private const string Key = "pw.fleet.v2";
        private const string LegacyKey = "pw.fleet.v1";

        [Serializable]
        private sealed class Envelope
        {
            public string payload;
            public string hash;
        }

        // f54303: UTF-8 -> SHA-256 -> Base64; dispose even if hashing throws.
        private static string Hash(string s)
        {
            using (SHA256 sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }

        // f54304.
        public static string Encode(FleetProfile data)
        {
            string payload = JsonUtility.ToJson(data);
            return JsonUtility.ToJson(new Envelope { payload = payload, hash = Hash(payload) });
        }

        // f54305. Unsigned comparisons in the WAT also reject negative values;
        // the decompiler's unannotated comparisons alone do not express that.
        public static FleetProfile Decode(string source)
        {
            try
            {
                Envelope envelope = JsonUtility.FromJson<Envelope>(source);
                if (envelope == null || String.IsNullOrEmpty(envelope.payload)) return null;
                if (envelope.hash != Hash(envelope.payload)) return null;
                FleetProfile data = JsonUtility.FromJson<FleetProfile>(envelope.payload);
                if (data == null) return null;
                if (data.version == 1) data = Migrate(data);
                if (unchecked((uint)(data.version - 2)) >= 5u) return null;
                if (data.revision < 0 || data.coins < 0 || data.fragments < 0) return null;
                if (data.unlocked <= 0 || data.unlocked > SaveLevelLimit(data.version)) return null;
                if (data.items == null || data.items.Length != 3 || data.slots == null || data.slots.Length != 5
                    || data.cores == null || data.cleared == null || data.cores.Count > 120) return null;
                if (unchecked((uint)data.epicPity) > 9u || unchecked((uint)data.legendPity) > 19u) return null;
                if (data.version <= 3)
                {
                    if (data.tech == null || data.tech.Length != 5) return null;
                    for (int i = 0; i < 5; i++) if (unchecked((uint)data.tech[i]) > 10u) return null;
                }
                for (int i = 0; i < 3; i++) if (unchecked((uint)data.items[i]) > 99u) return null;

                var ids = new HashSet<int>();
                foreach (FleetCore core in data.cores)
                {
                    if (core == null || core.id <= 0 || !ids.Add(core.id) || core.kind < 0
                        || core.kind >= FleetCatalog.Modules.Length || unchecked((uint)core.quality) > 4u) return null;
                }
                if (data.nextCore <= 0 || ids.Contains(data.nextCore) || data.seenUtc < 0 || data.wins < 0) return null;
                for (int slot = 0; slot < 5; slot++)
                {
                    int id = data.slots[slot];
                    if (id == 0) continue;
                    FleetCore core = data.cores.Find(c => c.id == id);
                    if (core == null || core.Slot != slot) return null;
                }
                foreach (FleetCore core in data.cores) if (core.id >= data.nextCore) return null;
                var cleared = new HashSet<int>();
                foreach (int level in data.cleared)
                    if (level <= 0 || level > SaveLevelLimit(data.version) || !cleared.Add(level)) return null;
                if (!String.IsNullOrEmpty(data.attempt) && (data.attemptLevel <= 0 || data.attemptLevel > data.unlocked)) return null;
                data.attempt = data.attempt ?? String.Empty;
                data.settled = data.settled ?? String.Empty;

                if (data.version == 2)
                {
                    if (data.unlocked > 1 || data.cleared.Count > 0 || data.wins > 0)
                    {
                        data.unlocked = unchecked(data.unlocked + 2);
                        for (int i = 0; i < data.cleared.Count; i++) data.cleared[i] = unchecked(data.cleared[i] + 2);
                        data.cleared.Insert(0, 2);
                        data.cleared.Insert(0, 1);
                        if (data.attemptLevel > 0) data.attemptLevel = unchecked(data.attemptLevel + 2);
                        data.growthTutorial = true;
                        data.independentTutorial = true;
                    }
                    else { data.attemptLevel = 0; data.attempt = String.Empty; }
                    data.version = 3;
                }
                if (data.version == 3)
                {
                    data.techRefund = FleetCatalog.TechRefund(data.tech);
                    data.coins = unchecked(data.coins + data.techRefund);
                    data.tech = new int[5];
                    data.activeFighter = 0; data.assembled = 1;
                    data.parts = null; data.spares = null; data.presets = null;
                    FleetEconomy.Normalize(data);
                    data.version = 4;
                }
                if (data.version == 4)
                {
                    data.energyAt = 0;
                    data.energy = 5;
                    data.streak = 0;
                    FleetEconomy.Normalize(data);
                    data.version = 5;
                }
                if (data.version == 5)
                {
                    data.unlocked = Shift(data.unlocked);
                    for (int i = 0; i < data.cleared.Count; i++) data.cleared[i] = Shift(data.cleared[i]);
                    if (data.attemptLevel > 0) data.attemptLevel = Shift(data.attemptLevel);
                    data.version = 6;
                }

                if (data.parts == null || data.parts.Length != 25 || data.spares == null || data.spares.Length != 25
                    || data.presets == null || data.presets.Length != 25) return null;
                if (data.assembled > 31 || (data.assembled & unchecked((int)0x80000001u)) != 1) return null;
                if (unchecked((uint)data.activeFighter) > 4u || ((data.assembled >> data.activeFighter) & 1) == 0) return null;
                if (data.partDraws < 0 || data.smelts < 0) return null;
                for (int i = 0; i < 25; i++)
                {
                    int level = data.parts[i];
                    if (unchecked((uint)level) >= 6u || unchecked((uint)data.spares[i]) > 9999u) return null;
                    if (level == 0 && ((data.assembled >> (i / 5)) & 1) != 0) return null;
                    int id = data.presets[i];
                    if (id == 0) continue;
                    FleetCore core = data.cores.Find(c => c.id == id);
                    if (core == null || core.Slot != i % 5) return null;
                }
                if (unchecked((uint)data.streak) > 10u || unchecked((uint)data.energy) > 999u
                    || data.zoneClaimed < 0 || data.dayAds < 0) return null;
                foreach (FleetCore core in data.cores)
                    if (core.durability < -1 || core.durability > core.MaxDurability) return null;
                // f54305 final HashSet.Add loop: one module id may appear in
                // only one preset across all fighters. Empty slots may repeat.
                var presetIds = new HashSet<int>();
                foreach (int id in data.presets)
                    if (id != 0 && !presetIds.Add(id)) return null;
                return data;
            }
            catch (Exception) { return null; } // The source rejects malformed saves, including migration failures.
        }

        // Inlined twice in f54305; f54307 is the version-5 level migration.
        private static int SaveLevelLimit(int version) => version == 2 ? 30 : version < 6 ? 92 : PixelWingGame.Levels.Length;
        private static int Shift(int level) => level > 62 ? unchecked(level + 20) : level;

        // f54306. Arrays are original metadata blobs 0x36FF30 and 0x36EBC8,
        // identified by BEA1CA05...7286F11 and 11047585...E4373D respectively.
        private static FleetProfile Migrate(FleetProfile old)
        {
            int[] prices = { 200, 350, 550, 800, 1100 };
            int[] tech = old.tech ?? new int[4];
            int refund = 0;
            if (tech.Length > 2)
                for (int i = 0; i < tech[2]; i++) refund = unchecked(refund + prices[i]);
            FleetProfile data = JsonUtility.FromJson<FleetProfile>(JsonUtility.ToJson(old));
            data.version = 2;
            data.tech = new int[]
            {
                tech.Length > 0 ? Math.Min(unchecked(tech[0] * 2), 10) : 0,
                tech.Length > 1 ? Math.Min(unchecked(tech[1] * 2), 10) : 0,
                0, 0, tech.Length > 3 ? Math.Min(unchecked(tech[3] * 2), 10) : 0
            };
            data.coins = unchecked(data.coins + refund);
            data.slots = new int[5];
            int[] kinds = { 0, 3, 10 };
            foreach (FleetCore core in data.cores)
            {
                core.kind = kinds[Math.Max(0, Math.Min(core.kind, 2))];
                core.activated = 0;
                core.expires = 0;
            }
            FleetCore equipped = data.cores.Find(c => c.id == old.equipped);
            if (equipped != null) data.slots[equipped.Slot] = equipped.id;
            data.equipped = 0;
            if (data.items == null || data.items.Length != 3) data.items = new int[] { 1, 1, 1 };
            return data;
        }

        // f54308: prefer v2 if either slot is valid. Ties choose slot a.
        public static FleetProfile Load()
        {
            FleetProfile a = Decode(PlayerPrefs.GetString(Key + ".a", String.Empty));
            FleetProfile b = Decode(PlayerPrefs.GetString(Key + ".b", String.Empty));
            if (a == null && b == null)
            {
                a = Decode(PlayerPrefs.GetString(LegacyKey + ".a", String.Empty));
                b = Decode(PlayerPrefs.GetString(LegacyKey + ".b", String.Empty));
                if (a == null && b == null) return new FleetProfile();
            }
            return a == null ? b : b == null ? a : a.revision < b.revision ? b : a;
        }

        // f54309/f54310: alternate by revision parity and flush immediately.
        public static void Save(FleetProfile data)
        {
            string key = Key + ((data.revision & 1) != 0 ? ".b" : ".a");
            PlayerPrefs.SetString(key, Encode(data));
            PlayerPrefs.Save();
        }
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        // Declared external UTC input at f54310; only Editor/CLI diagnostic builds.
        public static Func<long> UtcOverride;
#endif
        public static long Utc()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            if (UtcOverride != null) return UtcOverride();
#endif
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
