#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // The pause-page video action settles through the ordinary victory path.
        // ShowAd owns failure handling and the stale battle/home callback guard.
        void SkipGardenLevelWithAd()
        {
            if (!GardenUiActive || InHome || !settings || transitionActive || Battle == null || !Battle.CanSkipWithReward)
                return;
            var battle = Battle;
            ShowAd("skip-level", () =>
            {
                if (Battle != battle || InHome || !settings || transitionActive || !battle.CompleteRewardedSkip()) return;
                settings = false;
                tuning = false;
                selection = 0;
                finaleGuide = false;
                finaleShown = null;
                finaleShips.Clear();
                finaleBlasts.Clear();
                rescueAimAt = -1f;
                SetPaused(false);
            });
        }
    }
}
#endif
