#nullable disable
using System;

namespace Flightbeat.Economy
{
    // Original metadata type 4317. Reference fields intentionally retain original null defaults.
    public sealed class ModuleSpec
    {
        public int id;
        public int slot;
        public int icon;
        public string name;
        public string format;
        public float[] values;
        public float[] second;
        public bool retired;
        public Action<FleetLoadout, float, float> apply;
        public int bankIcon;

        // f54007: this is the only original constructor store.
        public ModuleSpec() { bankIcon = -1; }

        // f54005: preserve quality clamp, both separate f32 products and the six format arguments.
        public string Describe(int quality, float scale = 1f)
        {
            int level = quality < 0 ? 0 : quality > 4 ? 4 : quality;
            float v = values[level] * scale;
            float w = (second != null ? second[level] : 0f) * scale;
            return string.Format(format, new object[]
            {
                Pct(v), Pct(w), v.ToString("0.0#"), w.ToString("0.0#"),
                RoundOriginal(v), (v * 100f).ToString("0.#")
            });
        }

        // f54006: f32 multiplication precedes the original nearest-even integer conversion.
        private static string Pct(float v) => RoundOriginal(v * 100f).ToString();

        // f54005/f54006 both contain this same expanded rounding sequence.
        // Their abs(result)<2^31 guard uses int.MinValue for overflow and nonfinite values.
        private static int RoundOriginal(float value)
        {
            double rounded = Math.Round((double)value, MidpointRounding.ToEven);
            return Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
        }
    }
}
