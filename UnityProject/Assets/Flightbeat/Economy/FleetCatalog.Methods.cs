#nullable disable
using System;

namespace Flightbeat.Economy
{
    public static partial class FleetCatalog
    {
        // f54009: load the array once, call the supplied selector once with its length.
        public static int RandomModule(Func<int, int> random)
        {
            int[] choices = DrawableModules;
            return choices[random(choices.Length)];
        }

        // f54010: null/empty returns zero; positive levels clamp at 10; i32 sums wrap.
        public static int TechRefund(int[] tech)
        {
            int result = 0;
            if (tech == null) return result;
            for (int i = 0; i < tech.Length; i++)
            {
                int level = tech[i];
                if (level <= 0) continue;
                int count = level < 10 ? level : 10;
                for (int j = 0; j < count; j++) result = unchecked(result + TechCosts[j]);
            }
            return result;
        }

        // f54012: signed division and remainder, then original three-string concatenation.
        public static string PartLabel(int index)
        {
            int fighter = index / 5;
            return string.Concat(FighterNames[fighter], "·", PartNames[unchecked(index - fighter * 5)]);
        }

        // f54013: signed level clamp; every part outside 0..3 takes the engine branch.
        // Text is decoded from the original metadata literal slots, including double spaces.
        public static string PartEffect(int part, int level)
        {
            int q = level < 0 ? 0 : level > 5 ? 5 : level;
            switch (part)
            {
                case 0:
                    if (q < 2) return "射速 +0%  渗透 0 层";
                    return string.Concat(new string[] { "射速 +", P(NoseFire[q]), "%  渗透 ", NosePierce[q].ToString(), " 层" });
                case 1:
                    return string.Concat("合成击退 +", P(LeftRepel[q]), "%");
                case 2:
                    return string.Concat("退场冲击 +", P(RightSacrifice[q]), "%");
                case 3:
                    return string.Concat(new string[] { "连击射速 +", P(BodyCombo[q]), "%  金币 +", P(BodyGold[q]), "%" });
                default:
                    return string.Concat(new string[] { "防线减速 +", P(EngineDanger[q]), "%  出击射速 +", P(EngineLaunch[q]), "%" });
            }
        }

        // f54006, FleetCatalog.P alias (ModuleSpec.Pct shares the original WASM body).
        private static string P(float v) => RoundedPercent(v).ToString();

        // f54016: keep original sign selection, current-culture formatting, and wrapping abs.
        public static string StatValue(Stat s, float v)
        {
            if (((int)s & -2) == 12)
                return string.Concat(v >= 0f ? "+" : "", v.ToString("0.0#"), "秒");
            int rounded = RoundedPercent(v);
            bool reduces = unchecked((uint)((int)s - 5)) <= 1u;
            string sign = reduces ? (rounded >= 0 ? "-" : "+") : (rounded >= 0 ? "+" : "-");
            int mask = rounded >> 31;
            int magnitude = unchecked((rounded + mask) ^ mask);
            return string.Concat(sign, magnitude.ToString(), "%");
        }

        // Shared translation helper for the expanded rounding in f54006 and f54016.
        // Original f32 multiply occurs before f64 nearest-even rounding and the 2^31 guard.
        private static int RoundedPercent(float value)
        {
            double rounded = Math.Round((double)(value * 100f), MidpointRounding.ToEven);
            return Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
        }
    }
}
