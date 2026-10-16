#nullable disable
using System;
using System.Collections.Generic;

namespace Flightbeat.Economy
{
    // Original metadata type 4322; Serializable and all field declarations are retained.
    [Serializable]
    public sealed class FleetCore
    {
        public int id;
        public int kind;
        public int quality;
        public long activated;
        public long expires;
        public int durability;

        // src: FleetCore.get_MaxDurability f54044; signed quality clamp to 0..4.
        public int MaxDurability
        {
            get
            {
                int[] maximums = FleetCatalog.DurabilityMax;
                int q = quality < 4 ? quality : 4;
                return maximums[q > 0 ? q : 0];
            }
        }

        // src: FleetCore.get_Spec f54045; return the original catalog object by identity.
        public ModuleSpec Spec
        {
            get
            {
                ModuleSpec[] modules = FleetCatalog.Modules;
                int last = modules.Length - 1;
                int index = kind;
                return modules[index < 0 ? 0 : index > last ? last : index];
            }
        }

        // src: FleetCore.get_Slot f53991; inlined Spec followed by its slot field load.
        public int Slot => Spec.slot;

        // src: FleetCore..ctor f54046, shared with FleetReward..ctor at offset 40.
        public FleetCore() { durability = -1; }
    }

    // Original metadata type 4323; field names/types/order are the save-data contract.
    [Serializable]
    public sealed class FleetProfile
    {
        public int version;
        public int revision;
        public int coins;
        public int fragments;
        public int unlocked;
        public int equipped;
        public int nextCore;
        public int[] tech;
        public int[] items;
        public int[] slots;
        public List<FleetCore> cores;
        public List<int> cleared;
        public int epicPity;
        public int legendPity;
        public int wins;
        public int officerChats;
        public int totalKills;
        public int draws;
        public long seenUtc;
        public string attempt;
        public string settled;
        public int attemptLevel;
        public bool bombTutorial;
        public bool growthTutorial;
        public bool independentTutorial;
        public bool finaleTutorial;
        public int dayKey;
        public int dayWins;
        public int dayMerges;
        public int dayItems;
        public int dayKills;
        public int dailyClaimed;
        public int signCount;
        public int signKey;
        public int adCoins;
        public int adFragments;
        public int dayMaxes;
        public int dayCombo;
        public int dayCleanWins;
        public int dayFastWins;
        public bool pierceGranted;
        public int activeFighter;
        public int assembled;
        public int[] parts;
        public int[] spares;
        public int[] presets;
        public int partDraws;
        public int smelts;
        public bool starterGift;
        // Original FieldInfo attributes 0x86: public + NotSerialized (used by f53956 JSON cloning).
        [NonSerialized]
        public int techRefund;
        public int streak;
        public int bestStreak;
        public int energy;
        public long energyAt;
        public long unlimitedUntil;
        public int offerWindow;
        public int offerClaimed;
        public int offerProgress;
        public int offerSeed;
        public int zoneClaimed;
        public int dayAds;

        // src: FleetProfile..ctor f53995. Combined i64 store is coins=300, fragments=20.
        // Metadata initializers: 0x36FF68 (items), 0x370078 (parts); no shared arrays.
        public FleetProfile()
        {
            nextCore = 1;
            unlocked = 1;
            coins = 300;
            fragments = 20;
            version = 6;
            tech = new int[5];
            items = new int[] { 2, 2, 1 };
            slots = new int[5];
            cores = new List<FleetCore>();
            cleared = new List<int>();
            string empty = String.Empty;
            assembled = 1;
            settled = empty;
            attempt = empty;
            parts = new int[] { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            spares = new int[25];
            int[] initialPresets = new int[25];
            energy = 5;
            presets = initialPresets;
        }
    }

    // Original metadata type 4325; readonly array references remain mutable in their elements.
    public sealed class FleetBonus
    {
        public readonly float[] lean;
        public readonly float[] parts;
        public readonly float[] modules;
        public readonly float[] total;
        public readonly bool[] capped;
        public int pierce;
        public int fighter;
        public LegendaryKind legendary;
        public FleetLoadout Loadout;

        // src: FleetBonus.Raw f54050; two ordered f32 additions, no cap or total-array read.
        public float Raw(int i) => (lean[i] + parts[i]) + modules[i];

        // src: FleetBonus..ctor f54051; five independent zero-filled arrays.
        public FleetBonus()
        {
            lean = new float[16];
            parts = new float[16];
            modules = new float[16];
            total = new float[16];
            capped = new bool[16];
        }
    }

    // Original metadata type 4326.
    public sealed class FleetLook
    {
        public int model;
        public int stage;
        public int[] parts;

        // src: FleetLook.get_Standard f54052; allocate afresh with f54053 inlined.
        public static FleetLook Standard => new FleetLook();

        // src: FleetLook.Part f54054; missing/out-of-range parts return 1 without clamping values.
        public int Part(int p)
        {
            if (p < 0) return 1;
            int[] values = parts;
            if (values == null || values.Length <= p) return 1;
            return values[p];
        }

        // src: FleetLook..ctor f54053; metadata initializer 0x36FF88, SHA-256 C8C362C5...57033.
        public FleetLook()
        {
            stage = 1;
            parts = new int[] { 1, 1, 1, 1, 1 };
        }
    }

    // Original metadata type 4327; a plain value type without declared methods.
    public struct PartPull
    {
        public int index;
        public bool first;
    }

    // Original metadata type 4328.
    [Serializable]
    public sealed class FleetReward
    {
        public int coins;
        public int fragments;
        public int bonusCoins;
        public bool first;
        public int streak;
        public int streakPercent;
        public int streakCoins;
        public int streakFragments;
        public int zoneComplete;
        public int grantedCore;

        // src: FleetReward..ctor f54046, shared with FleetCore..ctor; offset 40 is zoneComplete here.
        public FleetReward() { zoneComplete = -1; }
    }
}
