#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // Original FleetEconomy type 4373; f5220/f4574 and f7113..f7117/f6063.
        public FleetProfile Data { get; internal set; }
        private readonly Func<long> clock;
        private readonly Action<FleetProfile> save;
        internal readonly Func<int, int> random;
        private List<FleetCore> sortedCores;
        private int sortedRevision;
        // f54166: metadata blob 0x36FE98, SHA-256 B2145620...981C7DB3.
        public static readonly int[] ItemPrices = { 600, 1000, 2000 };
        public const int CoreCapacity = 120;
        public const int AdCoinsPerDay = 5;
        public const int AdFragmentsPerDay = 3;
        public const int AdCoinAmount = 200;
        public const int AdFragmentAmount = 5;
        public int RetiredRefund { get; private set; }
        public string RetiredNames { get; private set; }
        public int TechRefund { get; private set; }

        // f54055: preserve callback assignment, refund capture and processing order.
        public FleetEconomy(FleetProfile data, Func<long> utc, Action<FleetProfile> persist, Func<int, int> rng)
        {
            sortedRevision = -1;
            RetiredNames = String.Empty;
            if (data == null) data = new FleetProfile();
            random = rng;
            save = persist;
            clock = utc;
            Data = data;
            TechRefund = data.techRefund;
            Normalize(data);
            RetireModules();
            Refresh();
        }

        // f53956: Unity's actual JSON boundary; techRefund is NonSerialized.
        // Callback exceptions propagate. Read current Data AFTER each callback;
        // publish the clone only AFTER persistence returns successfully.
        internal void Commit(Action<FleetProfile> mutation)
        {
            FleetProfile candidate = JsonUtility.FromJson<FleetProfile>(JsonUtility.ToJson(Data));
            mutation(candidate);
            long utc = clock();
            candidate.seenUtc = Math.Max(utc, Data.seenUtc);
            candidate.revision = unchecked(Data.revision + 1);
            save(candidate);
            Data = candidate;
        }

        // f54062/f54059: clock can have side effects; evaluation order matters.
        public long Now => Math.Max(clock(), Data.seenUtc);
        public int Today => DayOf(Now);

        // f53955; the lambda is instance method f54167 (no cached day value).
        public void Refresh()
        {
            if (Data.dayKey != Today)
                Commit(d =>
                {
                    int today = Today;
                    d.dayAds = 0;
                    d.dayWins = 0; d.dayMerges = 0;
                    d.adCoins = 0; d.adFragments = 0;
                    d.dayItems = 0; d.dayKills = 0;
                    d.dailyClaimed = 0;
                    d.dayMaxes = 0; d.dayCombo = 0;
                    d.dayCleanWins = 0; d.dayFastWins = 0;
                    d.dayKey = today;
                });
            SettleEnergy();
        }

        // f54061: reject invalid kind; do not use the clamping Spec accessor here.
        private static bool IsRetired(FleetCore c) =>
            c != null && c.kind >= 0 && c.kind < FleetCatalog.Modules.Length && FleetCatalog.Modules[c.kind].retired;

        // f54057; captured refund and its mutation are f1080/f54276.
        private void RetireModules()
        {
            int refund = 0;
            var names = new List<string>();
            foreach (FleetCore core in Data.cores)
            {
                if (!IsRetired(core)) continue;
                refund = unchecked(refund + 10);
                if (!names.Contains(core.Spec.name)) names.Add(core.Spec.name);
            }
            if (refund == 0) return;
            RetiredRefund = refund;
            RetiredNames = String.Join("、", names); // original string slot 5862288
            Commit(d =>
            {
                foreach (FleetCore core in d.cores)
                {
                    if (!IsRetired(core)) continue;
                    for (int i = 0; i < d.presets.Length; i++)
                        if (d.presets[i] == core.id) d.presets[i] = 0;
                }
                d.cores.RemoveAll(new Predicate<FleetCore>(IsRetired));
                d.fragments = unchecked(refund + d.fragments);
            });
        }

        // f17039/f54112, as used by the original energy system.
        public int EnergyMax => 5;
        public int Energy => Data.energy;

        // f54113 / f54114 / f54115. Capture order is observable through clock.
        public int EnergyNextSeconds
        {
            get
            {
                if (Data.energy > 4) return 0;
                long now = Now;
                return unchecked((int)Math.Max(0L, unchecked(Data.energyAt - now + 900L)));
            }
        }
        public int UnlimitedSeconds
        {
            get { long until = Data.unlimitedUntil; return unchecked((int)Math.Max(0L, unchecked(until - Now))); }
        }
        public bool UnlimitedEnergy
        {
            get { long until = Data.unlimitedUntil; return Now < until; }
        }

        // f54060; captured now/steps and three lambdas: f1080/f54252/f54253.
        private void SettleEnergy()
        {
            long now = Now;
            // Original DisplayClass103_0 owns BOTH fields; keep one capture scope.
            long steps;
            long energyAt = Data.energyAt;
            if (Data.energy >= 5)
            {
                if (energyAt == 0) return;
                if (energyAt > now) Commit(d => d.energyAt = now);
                return;
            }
            if (energyAt <= 0 || energyAt > now)
                Commit(d => d.energyAt = now);
            else
            {
                long elapsed = unchecked(now - energyAt);
                steps = elapsed / 900L;
                if (elapsed < 900L) return;
                Commit(d =>
                {
                    long grant = Math.Min(steps, (long)unchecked(5 - d.energy));
                    d.energy = unchecked(d.energy + (int)grant);
                    d.energyAt = d.energy <= 4 ? unchecked(d.energyAt + steps * 900L) : now;
                });
            }
        }
    }
}
