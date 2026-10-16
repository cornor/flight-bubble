using System;
using System.Runtime.CompilerServices;

namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // WAT f32 instructions round after EACH operation. Mono can retain a
        // wider intermediate inside a C# expression; a non-inlined bit roundtrip
        // materializes the original 32-bit boundary without changing constants.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float Binary32(float value) => BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value));

        // f70695/f70691: original System.Math uses comparison + first-input NaN
        // selection. Equality returns the SECOND operand, including signed zero.
        internal static float Min(float a, float b) => a < b || float.IsNaN(a) ? a : b;
        internal static float Max(float a, float b) => a > b || float.IsNaN(a) ? a : b;

        private static uint FloatBits(float value) => unchecked((uint)BitConverter.SingleToInt32Bits(value));
        private static ulong DoubleBits(double value) => unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        private static float F(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
        private static double D(ulong bits) => BitConverter.Int64BitsToDouble(unchecked((long)bits));
        private static double Abs(double value) => D(DoubleBits(value) & 0x7FFFFFFFFFFFFFFFUL);
        private static double CopySign(double value, double sign) => D((DoubleBits(value) & 0x7FFFFFFFFFFFFFFFUL) | (DoubleBits(sign) & 0x8000000000000000UL));

        // Distinct from the System.Math selection above: f32.min/max choose -/+0
        // on a zero tie, and an arithmetic NaN when a comparison is unordered.
        internal static float WasmMax(float a, float b)
        {
            if (a > b) return a;
            if (a < b) return b;
            if (a == b) return a == 0f ? F(FloatBits(a) & FloatBits(b)) : a;
            return OriginalMath.Binary32(a + b);
        }
        internal static float WasmMin(float a, float b)
        {
            if (a < b) return a;
            if (a > b) return b;
            if (a == b) return a == 0f ? F(FloatBits(a) | FloatBits(b)) : a;
            return OriginalMath.Binary32(a + b);
        }
    }
}
