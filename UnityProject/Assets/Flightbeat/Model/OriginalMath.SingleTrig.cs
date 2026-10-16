using System;

namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        private static float AbsSingle(float value) => F(FloatBits(value) & 0x7FFFFFFFu);
        private static float CopySignSingle(float value, float sign) => F((FloatBits(value) & 0x7FFFFFFFu) | (FloatBits(sign) & 0x80000000u));

        // f11233/f11234: preserve the WAT expression tree, not the flattened
        // decompiler text. The only single-precision rounding is at the return.
        private static float KernelSinSingle(double value)
        {
            double square = value * value, cube = square * value;
            return OriginalMath.Binary32((float)((cube * (square * square)) * (square * D(0x3EC6CD878C3B46A7UL) + D(0xBF2A00F9E2CAE774UL))
                + (cube * (square * D(0x3F811110896EFBB2UL) + D(0xBFC5555554CBAC77UL)) + value)));
        }
        private static float KernelCosSingle(double value)
        {
            double square = value * value, fourth = square * square;
            return OriginalMath.Binary32((float)((square * fourth) * (square * D(0x3EF99342E0EE5069UL) + D(0xBF56C087E80F1E27UL))
                + (fourth * D(0x3FA55553E1053A42UL) + (square * D(0xBFDFFFFFFD0C5E81UL) + 1.0))));
        }

        // f11236: complete sinf, including signed zero, infinities and NaNs.
        internal static float Sin(float value)
        {
            uint raw = FloatBits(value), magnitude = raw & 0x7FFFFFFFu;
            bool negative = (raw & 0x80000000u) != 0;
            if (magnitude <= 1061752794u)
                return magnitude < 964689920u ? value : KernelSinSingle(value);
            double x = value;
            if (magnitude <= 1081824209u)
            {
                if (magnitude <= 1075235811u)
                    return negative ? -KernelCosSingle(x + D(0x3FF921FB54442D18UL)) : KernelCosSingle(x + D(0xBFF921FB54442D18UL));
                return KernelSinSingle(-((negative ? D(0x400921FB54442D18UL) : D(0xC00921FB54442D18UL)) + x));
            }
            if (magnitude <= 1088565717u)
            {
                if (magnitude <= 1085271519u)
                    return negative ? KernelCosSingle(x + D(0x4012D97C7F3321D2UL)) : -KernelCosSingle(x + D(0xC012D97C7F3321D2UL));
                return KernelSinSingle((negative ? D(0x401921FB54442D18UL) : D(0xC01921FB54442D18UL)) + x);
            }
            if (magnitude >= 0x7F800000u) return OriginalMath.Binary32(value - value);
            int quadrant = ReduceSingle(value, out double y) & 3;
            if (quadrant == 0) return KernelSinSingle(y);
            if (quadrant == 1) return KernelCosSingle(y);
            if (quadrant == 2) return KernelSinSingle(-y);
            return -KernelCosSingle(y);
        }

        // f24580: complete cosf, using the same original reduction closure.
        internal static float Cos(float value)
        {
            uint raw = FloatBits(value), magnitude = raw & 0x7FFFFFFFu;
            bool negative = (raw & 0x80000000u) != 0;
            if (magnitude <= 1061752794u)
                return magnitude < 964689920u ? 1f : KernelCosSingle(value);
            double x = value;
            if (magnitude <= 1081824209u)
            {
                if (magnitude >= 1075235812u)
                    return -KernelCosSingle((negative ? D(0x400921FB54442D18UL) : D(0xC00921FB54442D18UL)) + x);
                return negative ? KernelSinSingle(x + D(0x3FF921FB54442D18UL)) : KernelSinSingle(D(0x3FF921FB54442D18UL) - x);
            }
            if (magnitude <= 1088565717u)
            {
                if (magnitude >= 1085271520u)
                    return KernelCosSingle((negative ? D(0x401921FB54442D18UL) : D(0xC01921FB54442D18UL)) + x);
                return negative ? KernelSinSingle(D(0xC012D97C7F3321D2UL) - x) : KernelSinSingle(x + D(0xC012D97C7F3321D2UL));
            }
            if (magnitude >= 0x7F800000u) return OriginalMath.Binary32(value - value);
            int quadrant = ReduceSingle(value, out double y) & 3;
            if (quadrant == 0) return KernelCosSingle(y);
            if (quadrant == 1) return KernelSinSingle(-y);
            if (quadrant == 2) return -KernelCosSingle(y);
            return KernelSinSingle(y);
        }

        // f11235: medium reduction includes both range correction branches.
        private static int ReduceSingle(float value, out double remainder)
        {
            uint raw = FloatBits(value), magnitude = raw & 0x7FFFFFFFu;
            if (magnitude <= 1305022426u)
            {
                double x = value;
                double rounded = (x * D(0x3FE45F306DC9C883UL) + 6755399441055744.0) + -6755399441055744.0;
                remainder = (x + rounded * D(0xBFF921FB50000000UL)) + rounded * D(0xBE5110B4611A6263UL);
                int n = TruncateInt32(rounded);
                if (remainder < D(0xBFE921FB60000000UL))
                {
                    rounded = rounded + -1.0;
                    remainder = (x + rounded * D(0xBFF921FB50000000UL)) + rounded * D(0xBE5110B4611A6263UL);
                    return unchecked(n - 1);
                }
                if (remainder > D(0x3FE921FB60000000UL))
                {
                    rounded = rounded + 1.0;
                    remainder = (x + rounded * D(0xBFF921FB50000000UL)) + rounded * D(0xBE5110B4611A6263UL);
                    return unchecked(n + 1);
                }
                return n;
            }
            if (magnitude >= 0x7F800000u) { remainder = OriginalMath.Binary32(value - value); return 0; }
            int exponent = (int)(magnitude >> 23) - 150;
            double scaled = F(unchecked(magnitude - (uint)(exponent << 23)));
            int result = ReduceLargeSingle(scaled, exponent, out remainder);
            if ((raw & 0x80000000u) != 0) { remainder = -remainder; result = -result; }
            return result;
        }

        private static int TruncateInt32(double remainder) => Abs(remainder) < 2147483648.0 ? (int)remainder : int.MinValue;

        // f4695 restricted to its f11235 call arguments nx=1, prec=0. This is
        // the complete reachable reduction for ALL finite f32 inputs, not an
        // approximation for normal game angles. Other precisions are not APIs.
        private static int ReduceLargeSingle(double input, int exponent, out double remainder)
        {
            int jv = Math.Max((exponent - 3) / 24, 0);
            int l = exponent - 24 * jv, q0 = l - 24;
            int jk = (int)SingleReductionInitial[0];
            var f = new double[20]; var q = new double[20]; var fq = new double[20]; var iq = new int[20];
            for (int i = 0; i <= jk; i++) f[i] = SingleTwoOverPi[jv + i];
            for (int i = 0; i <= jk; i++) q[i] = input * f[i] + 0.0;
            int jz = jk, n, ih;
            while (true)
            {
                double z = q[jz];
                for (int i = 0, j = jz; i < jz; i++)
                {
                    double high = TruncateInt32(z * D(0x3E70000000000000UL));
                    iq[i] = TruncateInt32(high * -16777216.0 + z);
                    z = q[--j] + high;
                }
                z = ScalePowerOfTwo(z, q0);
                z = z + Math.Floor(z * 0.125) * -8.0;
                n = TruncateInt32(z); z = z - n;
                if (q0 > 0)
                {
                    int top = iq[jz - 1] >> (24 - q0);
                    iq[jz - 1] = iq[jz - 1] - (top << (24 - q0));
                    n = top + n; ih = iq[jz - 1] >> (23 - q0);
                }
                else if (q0 == 0) ih = iq[jz - 1] >> 23;
                else ih = z >= 0.5 ? 2 : 0;
                if (ih > 0)
                {
                    int carry = 0;
                    for (int i = 0; i < jz; i++)
                    {
                        int word = iq[i];
                        if (carry != 0) iq[i] = 16777215 - word;
                        else if (word != 0) { iq[i] = 16777216 - word; carry = 1; }
                    }
                    if (q0 == 1) iq[jz - 1] &= 8388607;
                    else if (q0 == 2) iq[jz - 1] &= 4194303;
                    n++;
                    if (ih == 2)
                    {
                        z = 1.0 - z;
                        if (carry != 0) z = z - ScalePowerOfTwo(1.0, q0);
                    }
                }
                if (z == 0.0)
                {
                    int nonzero = 0;
                    for (int j = jz - 1; j >= jk; j--) nonzero |= iq[j];
                    if (nonzero != 0)
                    {
                        l = q0;
                        do { l -= 24; jz--; } while (iq[jz] == 0);
                        break;
                    }
                    int extra = 1;
                    while (iq[jk - extra] == 0) extra++;
                    int target = jz + extra;
                    do
                    {
                        jz++;
                        f[jz] = SingleTwoOverPi[jz + jv];
                        q[jz] = input * f[jz] + 0.0;
                    } while (jz < target);
                    continue;
                }
                z = ScalePowerOfTwo(z, 24 - l);
                if (z >= 16777216.0)
                {
                    int high = TruncateInt32(z * D(0x3E70000000000000UL));
                    iq[jz] = TruncateInt32((double)high * -16777216.0 + z);
                    iq[++jz] = high;
                }
                else { iq[jz] = TruncateInt32(z); l = q0; }
                break;
            }
            double scale = ScalePowerOfTwo(1.0, l);
            for (int i = jz; i >= 0; i--) { q[i] = scale * iq[i]; scale = scale * D(0x3E70000000000000UL); }
            for (int i = jz; i >= 0; i--)
            {
                double sum = 0.0;
                for (int k = 0; k <= jk && k <= jz - i; k++) sum = D(SinglePiOver2[k]) * q[k + i] + sum;
                fq[jz - i] = sum;
            }
            double result = 0.0;
            for (int i = jz; i >= 0; i--) result = result + fq[i];
            remainder = ih != 0 ? -result : result;
            return n & 7;
        }

        // f1132: original scalbn, including the subnormal staging multipliers.
        private static double ScalePowerOfTwo(double value, int exponent)
        {
            unchecked
            {
                if (exponent >= 1024)
                {
                    value = value * D(0x7FE0000000000000UL);
                    if ((uint)exponent < 2047u) exponent -= 1023;
                    else { value = value * D(0x7FE0000000000000UL); exponent = ((uint)exponent < 3069u ? exponent : 3069) - 2046; }
                }
                else if (exponent <= -1023)
                {
                    value = value * D(0x0360000000000000UL);
                    if ((uint)exponent > (uint)-1992) exponent += 969;
                    else { value = value * D(0x0360000000000000UL); exponent = ((uint)exponent > (uint)-2960 ? exponent : -2960) + 1938; }
                }
                return value * D((ulong)(uint)(exponent + 1023) << 52);
            }
        }

        // f12657: single-precision atan has different coefficients and infinity
        // result from the double f4704 implementation; do not cast that result.
        internal static float AtanSingle(float value)
        {
            uint raw = FloatBits(value), magnitude = raw & 0x7FFFFFFFu;
            if (magnitude >= 1283457024u)
                return magnitude > 0x7F800000u ? value : CopySignSingle(F(0x3FC90FDAu), value);
            int id = -1;
            if (magnitude <= 1054867455u) { if (magnitude < 964689920u) return value; }
            else
            {
                value = AbsSingle(value);
                if (magnitude <= 1066926079u)
                {
                    if (magnitude <= 1060110335u) { id = 0; value = OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(value + value)) + -1f)) / (OriginalMath.Binary32(value + 2f))); }
                    else { id = 1; value = OriginalMath.Binary32((OriginalMath.Binary32(value + -1f)) / (OriginalMath.Binary32(value + 1f))); }
                }
                else if (magnitude <= 1075576831u) { id = 2; value = OriginalMath.Binary32((OriginalMath.Binary32(value + -1.5f)) / (OriginalMath.Binary32(OriginalMath.Binary32(value * 1.5f) + 1f))); }
                else { id = 3; value = OriginalMath.Binary32(-1f / value); }
            }
            float square = OriginalMath.Binary32(value * value), fourth = OriginalMath.Binary32(square * square);
            float even = OriginalMath.Binary32(fourth * (OriginalMath.Binary32(OriginalMath.Binary32(fourth * F(0xBDDA1247u)) + F(0xBE4CCA98u))));
            float odd = OriginalMath.Binary32(square * (OriginalMath.Binary32(OriginalMath.Binary32(fourth * (OriginalMath.Binary32(OriginalMath.Binary32(fourth * F(0x3D7CAC25u)) + F(0x3E11F50Du)))) + F(0x3EAAAAA9u))));
            if (id < 0) return OriginalMath.Binary32(value - OriginalMath.Binary32(value * (OriginalMath.Binary32(even + odd))));
            float result = OriginalMath.Binary32(F(SingleAtanHi[id]) - (OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(value * (OriginalMath.Binary32(even + odd))) - F(SingleAtanLo[id]))) - value)));
            return (raw & 0x80000000u) == 0 ? result : -result;
        }

        // f51136: atan2f(y,x), with original correction and table values.
        internal static float Atan2Single(float y, float x)
        {
            uint yBits = FloatBits(y), xBits = FloatBits(x);
            uint yAbs = yBits & 0x7FFFFFFFu, xAbs = xBits & 0x7FFFFFFFu;
            if (yAbs > 0x7F800000u || xAbs > 0x7F800000u) return OriginalMath.Binary32(y + x);
            if (xBits == 0x3F800000u) return AtanSingle(y);
            uint xSign = (xBits >> 30) & 2u;
            int quadrant = (int)(xSign | (yBits >> 31));
            if (yAbs == 0) return quadrant == 2 ? F(0x40490FDBu) : quadrant == 3 ? F(0xC0490FDBu) : y;
            if (xAbs == 0x7F800000u) return F(yAbs == 0x7F800000u ? SingleAtan2BothInfinite[quadrant] : SingleAtan2XInfinite[quadrant]);
            if (xAbs == 0 || yAbs == 0x7F800000u || unchecked(xAbs + 218103808u) < yAbs) return CopySignSingle(F(0x3FC90FDBu), y);
            float angle = xSign != 0 && unchecked(yAbs + 218103808u) < xAbs ? 0f : AtanSingle(AbsSingle(OriginalMath.Binary32(y / x)));
            if (quadrant == 0) return angle;
            if (quadrant == 1) return -angle;
            return quadrant == 2 ? OriginalMath.Binary32(F(0x40490FDBu) - (OriginalMath.Binary32(angle + F(0x33BBBD2Eu)))) : OriginalMath.Binary32((OriginalMath.Binary32(angle + F(0x33BBBD2Eu))) + F(0xC0490FDBu));
        }
    }
}
