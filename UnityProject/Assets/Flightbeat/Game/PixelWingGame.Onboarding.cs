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
        public Vector2 AimPosition => aim; // f54854
        public bool TransitionActive => transitionActive; // f54858

        // f54694, with exact diagnostic callbacks f1406 (nop) and f896 (zero).
        // This resets a separate in-memory profile, not the player's saved game.

    }
}
