namespace Flightbeat.Economy
{
    // Original metadata type 4324. Fields retain original names/types and declaration order.
    public sealed class FleetLoadout
    {
        public float fire;
        public float fireHigh;
        public float fireLow;
        public float repel;
        public float sacrifice;
        public float slow;
        public float idleSlow;
        public float dangerSlow;
        public float fusionHold;
        public float fusionFire;
        public float fusionFireTime;
        public float comboFire;
        public float hold;
        public float bombRadius;
        public float gold;
        public float launchFire;
        public float comboChain;
        public int extraBays;
        public int pierce;
        public LegendaryKind legendary;

        // f1080: the shared original constructor is a nop; allocation supplies zero fields.
        public FleetLoadout() { }

        // f54047 allocates a fresh object on every access, with the empty constructor inlined.
        public static FleetLoadout Empty => new FleetLoadout();

        // f54048: br_table entries 0..14; Gold and every other int32 value take the default.
        public float Get(FleetCatalog.Stat s) => s switch
        {
            FleetCatalog.Stat.Fire => fire,
            FleetCatalog.Stat.FireHigh => fireHigh,
            FleetCatalog.Stat.FireLow => fireLow,
            FleetCatalog.Stat.Repel => repel,
            FleetCatalog.Stat.Sacrifice => sacrifice,
            FleetCatalog.Stat.Slow => slow,
            FleetCatalog.Stat.IdleSlow => idleSlow,
            FleetCatalog.Stat.Danger => dangerSlow,
            FleetCatalog.Stat.Combo => comboFire,
            FleetCatalog.Stat.Chain => comboChain,
            FleetCatalog.Stat.Launch => launchFire,
            FleetCatalog.Stat.FusionFire => fusionFire,
            FleetCatalog.Stat.FusionHold => fusionHold,
            FleetCatalog.Stat.Hold => hold,
            FleetCatalog.Stat.BombRadius => bombRadius,
            _ => gold
        };

        // f54049: same branch table and offsets as Get, one f32 store and no normalization.
        public void Set(FleetCatalog.Stat s, float value)
        {
            switch (s)
            {
                case FleetCatalog.Stat.Fire: fire = value; break;
                case FleetCatalog.Stat.FireHigh: fireHigh = value; break;
                case FleetCatalog.Stat.FireLow: fireLow = value; break;
                case FleetCatalog.Stat.Repel: repel = value; break;
                case FleetCatalog.Stat.Sacrifice: sacrifice = value; break;
                case FleetCatalog.Stat.Slow: slow = value; break;
                case FleetCatalog.Stat.IdleSlow: idleSlow = value; break;
                case FleetCatalog.Stat.Danger: dangerSlow = value; break;
                case FleetCatalog.Stat.Combo: comboFire = value; break;
                case FleetCatalog.Stat.Chain: comboChain = value; break;
                case FleetCatalog.Stat.Launch: launchFire = value; break;
                case FleetCatalog.Stat.FusionFire: fusionFire = value; break;
                case FleetCatalog.Stat.FusionHold: fusionHold = value; break;
                case FleetCatalog.Stat.Hold: hold = value; break;
                case FleetCatalog.Stat.BombRadius: bombRadius = value; break;
                default: gold = value; break;
            }
        }
    }
}
