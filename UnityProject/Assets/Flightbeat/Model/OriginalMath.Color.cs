using System;

namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // f42373: Color -> Color32; clamp, round ties to even, then byte pack.
        // Explicit conversion avoids relying on a different Unity version's
        // implicit operator or platform NaN-to-integer behavior.
        internal static uint Color32Bits(float red, float green, float blue, float alpha)
        {
            float r = RoundColorChannel(red), g = RoundColorChannel(green);
            float b = RoundColorChannel(blue), a = RoundColorChannel(alpha);
            return ColorByte(r) | (ColorByte(g) << 8) | (ColorByte(b) << 16) | (ColorByte(a) << 24);
        }

        private static float RoundColorChannel(float input)
        {
            float value = input < 0f ? 0f : OriginalMath.Binary32(WasmMin(input, 1f) * 255f);
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

        private static uint ColorByte(float value)
        {
            uint unsigned = value >= 0f && value < 4294967296f ? (uint)value : 0u;
            int signed = AbsSingle(value) < 2147483648f ? (int)value : int.MinValue;
            return (value < 0f ? unchecked((uint)signed) : unsigned) & 255u;
        }

        // f4708: exact modf split, including signed zero and raw NaN returns.
        private static double SplitFraction(double value, out double integral)
        {
            ulong bits = DoubleBits(value);
            int encoded = (int)((bits >> 52) & 2047UL), exponent = encoded - 1023;
            double zero = D(bits & 0x8000000000000000UL);
            if (encoded >= 1075)
            {
                integral = value;
                return (bits & 0x000FFFFFFFFFFFFFUL) != 0 && exponent == 1024 ? value : zero;
            }
            if (encoded <= 1022) { integral = zero; return value; }
            if (((bits << exponent) & 0x000FFFFFFFFFFFFFUL) == 0) { integral = value; return zero; }
            integral = D(unchecked((ulong)(-4503599627370496L >> exponent)) & bits);
            return value - integral;
        }
    }
}
