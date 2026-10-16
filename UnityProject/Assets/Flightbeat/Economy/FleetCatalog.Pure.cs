namespace Flightbeat.Economy
{
    // Independent pure-method slice. Static data initialization and the other
    // FleetCatalog members are not implemented by this file.
    public static partial class FleetCatalog
    {
        // src: FleetCatalog.Stat, metadata type index 4318, underlying int32.
        public enum Stat
        {
            Fire = 0, FireHigh = 1, FireLow = 2, Repel = 3, Sacrifice = 4,
            Slow = 5, IdleSlow = 6, Danger = 7, Combo = 8, Chain = 9,
            Launch = 10, FusionFire = 11, FusionHold = 12, Hold = 13,
            BombRadius = 14, Gold = 15
        }

        // src: FleetCatalog.PartIndex f54011; i32.mul(5), then i32.add.
        public static int PartIndex(int fighter, int part) => unchecked(fighter * 5 + part);

        // src: FleetCatalog.StatSeconds f54014; i32.and(-2), then i32.eq(12).
        public static bool StatSeconds(Stat s) => ((int)s & -2) == 12;

        // src: FleetCatalog.StatReduces f54015; i32.sub(5), then i32.lt_u(2).
        // Keep the unsigned comparison and wrapping subtraction for all int32 inputs.
        public static bool StatReduces(Stat s) => unchecked((uint)((int)s - 5)) < 2u;

        // src: FleetCatalog.StreakPercent f54018; signed <=0 guard, then
        // unsigned <6 and >=3 selects. Positive input permits the int comparisons.
        public static int StreakPercent(int streak)
        {
            if (streak <= 0) return 0;
            return streak >= 3 ? (streak < 6 ? 30 : 50) : 10;
        }
    }
}
