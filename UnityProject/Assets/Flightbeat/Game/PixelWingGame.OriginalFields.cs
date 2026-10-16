using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54860: preserve all original Game-declared field initializers.
        // These fields were omitted while translating their current consumers.
        // Zero-valued result fields have no explicit store in f54860. Colors
        // decode the paired i64 stores, and +592 stores i32 -1054867456 (-10f).
        // Retaining storage does not add a result animation or vibration rule.
#pragma warning disable CS0169, CS0414
        private float resultShownCoins, resultShownFragments; // +52/+56, zero
        private float vibrateAt = -10f; // +592
        private readonly Color white = new Color(0.9647058844566345f, 0.9607843160629272f, 0.8784313797950745f, 1.0f); // +484
        private readonly Color muted = new Color(0.5411764979362488f, 0.7215686440467834f, 0.8352941274642944f, 1.0f); // +500
        private readonly Color gold = new Color(1.0f, 0.6941176652908325f, 0.14509804546833038f, 1.0f); // +516
#pragma warning restore CS0169, CS0414
    }
}
