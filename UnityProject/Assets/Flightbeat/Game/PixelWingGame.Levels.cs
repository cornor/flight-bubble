using System;
using Flightbeat.Economy;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54861 calls f54769.
        // 2026-11-05 (user: the new levels 1-52 are the game's levels; at most 52 levels): the list stops at game level
        // 52 (training01, training02, sector01..sector50) instead of the catalog's WarZones.SectorCount + 2 = 262.
        // The original sector51..sector260 files moved to Art/levels-a/ (backup, not in the build).
        public const int LevelCount = 52;
        public static readonly string[] Levels = BuildLevels();

        // src: f54769. ToString uses the current culture, as in the source.
        private static string[] BuildLevels()
        {
            int sectors = Math.Min(WarZones.SectorCount, LevelCount - 2);
            string[] levels = new string[sectors + 2];
            levels[0] = "training01";
            levels[1] = "training02";
            for (int i = 0; i < sectors; i++)
                levels[i + 2] = "sector" + (i + 1).ToString("D2");
            return levels;
        }
    }
}
