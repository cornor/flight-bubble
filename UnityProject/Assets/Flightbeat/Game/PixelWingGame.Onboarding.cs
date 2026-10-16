using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        public bool LessonTestReady => !transitionActive && Battle.State != Phase.Intro; // f54693
        public bool FinaleGuideActive => finaleGuide; // f54722
        public int LevelIndex => levelIndex; // f54810
        public int SelectedPart => hangarPart; // f54678
        public Vector2 AimPosition => aim; // f54854
        public bool TransitionActive => transitionActive; // f54858

        // f54694, with exact diagnostic callbacks f1406 (nop) and f896 (zero).
        // This resets a separate in-memory profile, not the player's saved game.
        public void PrepareOnboardingEvidence()
        {
            evidenceControlled = false;
            automatedLessonTest = true;
            var economy = new FleetEconomy(new FleetProfile(), FleetSaveStore.Utc, _ => { }, n => 0);
            Page = DeckPage.Bridge;
            InHome = true;
            levelIndex = 0;
            Economy = economy;
            deckOverlay = DeckOverlay.None;
            ResetRuntime();
            lastOutcome = 0;
            liaisonCheck = 1f;
            homeEnteredAt = -10f;
            if (!evidenceControlled && Economy.Data.unlocked == 1 && Economy.Data.cleared.Count == 0) StartSortie(0);
        }
    }
}
