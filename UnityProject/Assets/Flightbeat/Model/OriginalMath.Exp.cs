namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // f53831 and f53832: original expf reduction, data table and polynomial.
        // Keep every f64 multiply/add separated; the final conversion is f32.
        internal static float Exp(float value)
        {
            uint top = (FloatBits(value) >> 20) & 2047u;
            if (top >= (FloatBits(88f) >> 20))
            {
                if (FloatBits(value) == 0xFF800000u) return 0f;
                if (top >= (0x7F800000u >> 20)) return OriginalMath.Binary32(value + value);
                if (value > F(0x42B17217u)) return ScaleOverflow(F(0x70000000u)); // f11240
                if (value < F(0xC2CFF1B4u)) return ScaleOverflow(F(0x10000000u)); // f11241
            }
            double z = D(ExpInvLn2N[0]) * (double)value;
            double shift = D(ExpShift[0]);
            double rounded = z + shift;
            double remainder = z - (rounded - shift);
            double polynomial = D(ExpC2[0]) * remainder;
            polynomial = polynomial + D(ExpC1[0]);
            double square = remainder * remainder;
            polynomial = polynomial * square;
            double linear = D(ExpC0[0]) * remainder;
            linear = linear + 1.0;
            polynomial = polynomial + linear;
            ulong encoded = DoubleBits(rounded);
            ulong scale = unchecked((encoded << 47) + ExpTable[unchecked((int)((uint)encoded & 31u))]);
            return OriginalMath.Binary32((float)(polynomial * D(scale)));
        }

        // f11238/f11239, reached with sign=0 from f53831. The original identity
        // store/load only forces an f32 value before its f32 multiplication.
        private static float ScaleOverflow(float value) => OriginalMath.Binary32(value * value);
    }
}
