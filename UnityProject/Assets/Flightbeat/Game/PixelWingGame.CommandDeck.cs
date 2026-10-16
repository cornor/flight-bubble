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
            s.MapLayout = false;
            float t = OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(Since(199) / .35f)));
            int direction = unchecked((int)Page - (int)previousPage);
            float slide = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * t))) + -1f) + 1f)) * (direction != 0 ? direction > 0 ? 60f : -60f : 0f));
            switch (Page)
            {
                case DeckPage.Bridge: DrawHome(s, slide, slide); break;
                case DeckPage.Upgrade: DrawUpgrade(s, slide, slide); break;
                default: DrawShop(s, slide, slide); break;
            }
            DrawTopBar(s);
            DrawNav(s);
            switch (deckOverlay)
            {
                case DeckOverlay.Settings: DrawDeckSettings(s); break;
                case DeckOverlay.Daily: DrawDaily(s); break;
                case DeckOverlay.Sign: DrawSign(s); break;
                case DeckOverlay.Gacha: DrawGacha(s); break;
                case DeckOverlay.Supply: DrawSupply(s); break;
                case DeckOverlay.Smelt: DrawSmelt(s); break;
                case DeckOverlay.Codex: DrawCodex(s); break;
                case DeckOverlay.Zone: DrawZoneReward(s); break;
            }
            DrawAssembly(s);
            DrawRecycle(s);
            DrawEnergySheet(s);
            DrawBrokenNotice(s);
            DrawDevPanel(s);
            if (tuning) DrawTuning(s);
        }

    }
}
