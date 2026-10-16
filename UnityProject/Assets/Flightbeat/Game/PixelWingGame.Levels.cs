using Flightbeat.Economy;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54861 calls f54769. The catalog determines the sector count.
        public static readonly string[] Levels = BuildLevels();

        // src: f54769. ToString uses the current culture, as in the source.
        private static string[] BuildLevels()
        {
            string[] levels = new string[WarZones.SectorCount + 2];
            levels[0] = "training01";
            levels[1] = "training02";
            for (int i = 0; i < WarZones.SectorCount; i++)
                levels[i + 2] = "sector" + (i + 1).ToString("D2");
            return levels;
        }
    }
}
