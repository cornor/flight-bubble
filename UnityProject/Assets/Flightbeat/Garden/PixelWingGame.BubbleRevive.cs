#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private void RescueGarden()
        {
            if (Battle.ReviveInSlotOrder()) rescueAimAt = OriginalMath.Binary32(Battle.Time + 1.6f);
        }
    }
}
#endif
