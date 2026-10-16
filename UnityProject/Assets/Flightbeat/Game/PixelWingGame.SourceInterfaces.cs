using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Original standalone interfaces. Existing inlined call sites retain
        // their own instruction order; these restore the original API surface.
        private bool LessonPropsLocked => !evidenceControlled && Battle.Level.lesson > 0; // f54485
        public bool EnergyOpen => energyOpen; // f54572
        public bool StreakPromptOpen => streakPrompt; // f54573
        private bool NeedsEnergy(int index) => index >= 2 && !evidenceControlled && !Economy.CanSortie; // f54574
        public DeckOverlay Overlay => deckOverlay; // compiler-shared f24197, Game +128
        public int BroadcastWins => broadcastWins; // compiler-shared f54699, Game +336
        public int BroadcastLoops => broadcastLoops; // compiler-shared f54700, Game +340
        public bool BroadcastAutomatic => broadcastAutomatic; // f54567
        public Finale FinaleShown => finaleShown; // f54724
        public int RenderWidth => evidenceViewport.x > 0 ? evidenceViewport.x : FrameScreenWidth(); // f54770
        public int RenderHeight => evidenceViewport.y > 0 ? evidenceViewport.y : FrameScreenHeight(); // f54771
        public int SelectionMode => selection; // f54809
        public int ItemStock(int kind) => Economy.Data.items[kind]; // f54846
        public bool TargetPrimed => OriginalMath.Binary32(targetClock - lastTargetTap) < .34f; // f54855
        public float AimRadius => OriginalMath.Binary32((aimMissile ? 150f : 120f) *
            (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f)); // f54807: Battle +360 -> Loadout +60

        // f54806: preserve each add/div/add binary32 boundary and its operands.
        private Vector2 AimWorld() => new Vector2(
            OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(aim.x + -270f) / 1.564977f) + 270f),
            OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(aim.y + -562f) / 1.31f) + 562f));
    }
}
