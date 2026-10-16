using System;

namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // src: round-to-even instruction blocks in f54415/f54416/f54417/f54422,
        // using original modf f4708. Preserve the f32 addition before floor/ceil.
        internal static float RoundSingle(float value)
        {
            double fraction = SplitFraction(value, out double integral);
            if (value >= 0f)
            {
                if (fraction == 0.5)
                {
                    long integer = Abs(integral) < D(0x43E0000000000000UL) ? (long)integral : long.MinValue;
                    float whole = OriginalMath.Binary32((float)integral);
                    return (integer & 1L) == 0 ? whole : OriginalMath.Binary32(whole + 1f);
                }
                return OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(value + 0.5f)));
            }
            if (fraction == -0.5)
            {
                long integer = Abs(integral) < D(0x43E0000000000000UL) ? (long)integral : long.MinValue;
                float whole = OriginalMath.Binary32((float)integral);
                return (integer & 1L) == 0 ? whole : OriginalMath.Binary32(whole + -1f);
            }
            return OriginalMath.Binary32((float)Math.Ceiling(OriginalMath.Binary32(value + -0.5f)));
        }
    }
}
