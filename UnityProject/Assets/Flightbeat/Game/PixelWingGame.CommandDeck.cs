using Flightbeat.Model;
using System;
using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    // Original metadata type 4312.
    public enum DeckOverlay { None, Settings, Daily, Sign, Gacha, Supply, Smelt, Codex, Zone }

    public partial class PixelWingGame
    {
        internal DeckOverlay deckOverlay; // +128
        private int assemblyFighter = -1; // +64; f54860
        internal bool devPanel, energyOpen, recycleOpen, transitionActive; // +96/+97/+216/+700

        // f54590. Original page, overlay and floating modal order.
        private void DrawCommandDeck(PixelSurface s)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            DrawBubbleHome(s);
#endif
        }

    }
}
