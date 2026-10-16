namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // src: powf f11242, helpers f11237..f11244 and original table bytes.
        // CombatFeedback f53871 uses this with exponent 3; multiplying x*x*x
        // would change its rounding. Retain the full original branch structure.
        internal static float Pow(float x, float y)
        {
            unchecked
            {
                uint ix = FloatBits(x), iy = FloatBits(y), signBias = 0;
                if (ZeroInfinityNaN(iy))
                {
                    if (ix == 0x3F800000u || (iy << 1) == 0) return 1f;
                    uint twiceX = ix << 1, twiceY = iy << 1;
                    if (!(twiceY < 0xFF000001u && twiceX <= 0xFF000000u)) return OriginalMath.Binary32(x + y);
                    if (twiceX == 0x7F000000u) return 1f;
                    return ((~iy >> 31) != 0) == (twiceX < 0x7F000000u) ? 0f : OriginalMath.Binary32(y * y);
                }
                if (!(ix - 0x7F800000u >= 0x81000000u))
                {
                    if (ZeroInfinityNaN(ix))
                    {
                        float square = OriginalMath.Binary32(x * x);
                        if ((int)ix < 0 && IntegerParity(iy) == 1) square = -square;
                        return (int)iy >= 0 ? square : OriginalMath.Binary32(1f / square);
                    }
                    if ((int)ix < 0)
                    {
                        int integer = IntegerParity(iy);
                        if (integer == 0) { float invalid = OriginalMath.Binary32(x - x); return OriginalMath.Binary32(invalid / invalid); }
                        signBias = integer == 1 ? 65536u : 0u;
                        ix &= 0x7FFFFFFFu;
                    }
                    if (ix <= 0x007FFFFFu)
                        ix = (FloatBits(OriginalMath.Binary32(x * 8388608f)) & 0x7FFFFFFFu) - 192937984u;
                }
                uint temporary = ix - 1060306944u;
                int pair = (int)((temporary >> 15) & 240u) / 8;
                double normalized = F(ix - (temporary & 0xFF800000u));
                double r = normalized * D(PowLogTable[pair]) + -1.0;
                double r2 = r * r;
                double first = (D(PowLogCoefficients[0]) * r + D(PowLogCoefficients[1])) * (r2 * r2);
                double second = (D(PowLogCoefficients[2]) * r + D(PowLogCoefficients[3])) * r2;
                double linear = D(PowLogCoefficients[4]) * r + (D(PowLogTable[pair + 1]) + (double)((int)temporary >> 23));
                double exponent = (first + (second + linear)) * (double)y;
                if ((DoubleBits(exponent) & 9223231299366420480UL) >= 4638426141214900225UL)
                {
                    if (exponent > D(0x405FFFFFFFD1D571UL)) return PowScaleOverflow(signBias, F(0x70000000u));
                    if (exponent <= -150.0) return PowScaleOverflow(signBias, F(0x10000000u));
                }
                double shift = D(PowExpCoefficients[0]);
                double rounded = shift + exponent;
                double remainder = exponent - (rounded - shift);
                double polynomial = (D(PowExpCoefficients[1]) * remainder + D(PowExpCoefficients[2])) * (remainder * remainder);
                polynomial = polynomial + (D(PowExpCoefficients[3]) * remainder + 1.0);
                ulong encoded = DoubleBits(rounded);
                ulong scale = ((encoded + signBias) << 47) + ExpTable[(int)((uint)encoded & 31u)];
                return OriginalMath.Binary32((float)(polynomial * D(scale)));
            }
        }

        // f11243/f11244: 0 = not an integer, 1 = odd, 2 = even.
        private static bool ZeroInfinityNaN(uint bits) => unchecked((bits << 1) + 16777216u) < 16777217u;
        private static int IntegerParity(uint bits)
        {
            uint exponent = (bits >> 23) & 255u;
            if (exponent < 127u) return 0;
            if (exponent > 150u) return 2;
            uint bit = 1u << (150 - (int)exponent);
            if ((bits & (bit - 1u)) != 0) return 0;
            return (bits & bit) != 0 ? 1 : 2;
        }
        private static float PowScaleOverflow(uint sign, float value) => OriginalMath.Binary32((sign != 0 ? -value : value) * value);
    }
}
