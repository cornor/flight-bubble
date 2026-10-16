namespace Flightbeat.Economy
{
    public static partial class WarZones
    {
        public const int Count = 5;
        public const int MaxZoneLevels = 62;

        // f54905: its callers supply at most five zones. This method does not clamp.
        private static int Sum(int zones)
        {
            int total = 0;
            for (int i = 0; i < zones; i++) total = unchecked(total + Sizes[i]);
            return total;
        }

        // f54883: original inline Sum after clamping the count to [0, 5].
        public static int FirstSector(int zone) => Sum(zone < 0 ? 0 : zone > 5 ? 5 : zone);

        // f54882: two teaching levels precede the sector indices.
        public static int OfLevel(int index)
        {
            if (index < 2) return 0;
            int sector = index - 2;
            for (int zone = 0; zone < 4; zone++)
                if (FirstSector(zone + 1) > sector) return zone;
            _ = FirstSector(5); // Original calls this and drops the result before returning 4.
            return 4;
        }

        // f54906: zone 0 includes the two teaching levels.
        public static int FirstLevel(int zone) => zone <= 0 ? 0 : unchecked(FirstSector(zone) + 2);

        // f54907: the zone is clamped to [0, 4] before advancing to its end.
        public static int LastLevel(int zone)
        {
            int clamped = zone < 0 ? 0 : zone > 4 ? 4 : zone;
            return unchecked(FirstSector(clamped + 1) + 1);
        }

        // f54908: original inlines the two boundary calculations, including their distinct clamps.
        public static int LevelsIn(int zone) => unchecked(LastLevel(zone) - FirstLevel(zone) + 1);

        // f54909: one-based display position relative to the original zone start.
        public static int PositionInZone(int index) => unchecked(index - FirstLevel(OfLevel(index)) + 1);
    }
}
