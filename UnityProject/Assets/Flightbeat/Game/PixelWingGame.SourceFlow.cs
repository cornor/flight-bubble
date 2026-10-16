using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Standalone original helpers. Existing inlined caller bodies retain
        // their source ordering; these are not substitute gameplay algorithms.
        private void AutoDeploy() // f54800
        {
            if (Battle.State == Phase.ReviveOffer) { Battle.DeclineRevive(); return; }
            int lane = Battle.SuggestedQueue();
            if (lane >= 0) Battle.Deploy(lane);
        }

        private void SettleReward() // f54801
        {
            if (Battle.State == Phase.Won && LastReward == null && sortieId.Length > 0)
                LastReward = Economy.Win(sortieId, sortieLoadout, Battle.Revived, Battle.Time);
        }

        private void RecordSortieStats() // f54802: mark before committing.
        {
            if (statsRecorded || Battle == null || evidenceControlled) return;
            statsRecorded = true;
            Economy.RecordBattle(Battle.Merges, unchecked(Battle.Total - Battle.Remaining), Battle.MaxFusions, Battle.BestCombo);
        }

        private void TryFirstFlight() // f54698
        {
            if (!evidenceControlled && Economy.Data.unlocked == 1 && Economy.Data.cleared.Count == 0)
                StartSortie(0);
        }



        private void NoteBroadcastHumanInput() // f54705: two separate key reads.
        {
            if (FrameAnyKeyDown())
            {
                broadcastOwnAim = false;
                broadcastHumanActions = unchecked(broadcastHumanActions + 1);
            }
            if (FrameAnyKeyDown() || FrameMouseButton(0))
                broadcastHumanUntil = OriginalMath.Binary32(ui + 1.2f);
        }
    }
}
