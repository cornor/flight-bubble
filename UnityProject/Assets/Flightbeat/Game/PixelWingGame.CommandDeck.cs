using Flightbeat.Model;
using System;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    // Original metadata type 4312.
    public enum DeckOverlay { None, Settings, Daily, Sign, Gacha, Supply, Smelt, Codex, Zone }

    public partial class PixelWingGame
    {
        private int assemblyFighter = -1, assemblySoundStep;
        private float assemblyAt = 0f; // Retained source assembly renderer dependencies; no UI entry.
        internal DeckOverlay deckOverlay; // +128
        internal bool devPanel, energyOpen, transitionActive; // +96/+97/+216/+700

        // f54590. Original page, overlay and floating modal order.
        private void DrawCommandDeck(PixelSurface s)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            DrawBubbleHome(s);
#endif
        }

    }
}
