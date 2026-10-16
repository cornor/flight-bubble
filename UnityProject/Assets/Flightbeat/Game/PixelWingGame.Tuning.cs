using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        public bool SettingsOpen => settings; // f54808

        // f54646. Load intentionally returns a fresh authored object in this Test
        // build (f54431). Do not silently wire the controls to live battle pressure.
        private void TuningTap(int k) => DevTools?.TuningTap(k);
        // f54603. Preserve layout mapping and all authored ranges/units.
        private void DrawTuning(PixelSurface s) => DevTools?.DrawTuning(s);
    }
}
