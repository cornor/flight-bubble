namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // f4704: complete original atan, including raw early returns and signs.
        internal static double Atan(double value)
        {
            ulong original = DoubleBits(value);
            uint high = unchecked((uint)(original >> 32)) & 0x7FFFFFFFu;
            if (high >= 1141899264u)
                return (original & 0x7FFFFFFFFFFFFFFFUL) > 0x7FF0000000000000UL
                    ? value : CopySign(D(0x3FF921FB54442D18UL), value);
            int id = -1;
            bool unreduced;
            if (high <= 1071382527u)
            {
                if (high < 1044381696u) return value;
                unreduced = true;
            }
            else
            {
                unreduced = false;
                value = Abs(value);
                if (high <= 1072889855u)
                {
                    if (high <= 1072037887u)
                    {
                        id = 0;
                        value = ((value + value) + -1.0) / (value + 2.0);
                    }
                    else
                    {
                        id = 1;
                        value = (value + -1.0) / (value + 1.0);
                    }
                }
                else if (high <= 1073971199u)
                {
                    id = 2;
                    value = (value + -1.5) / ((value * 1.5) + 1.0);
                }
                else { id = 3; value = -1.0 / value; }
            }
            double square = value * value;
            double fourth = square * square;
            double odd = fourth * D(0xBFA2B4442C6A6C2FUL);
            odd = (odd + D(0xBFADDE2D52DEFD9AUL)) * fourth;
            odd = (odd + D(0xBFB3B0F2AF749A6DUL)) * fourth;
            odd = (odd + D(0xBFBC71C6FE231671UL)) * fourth;
            odd = (odd + D(0xBFC999999998EBC4UL)) * fourth;
            double even = fourth * D(0x3F90AD3AE322DA11UL);
            even = (even + D(0x3FA97B4B24760DEBUL)) * fourth;
            even = (even + D(0x3FB10D66A0D03D51UL)) * fourth;
            even = (even + D(0x3FB745CDC54C206EUL)) * fourth;
            even = (even + D(0x3FC24924920083FFUL)) * fourth;
            even = (even + D(0x3FD555555555550DUL)) * square;
            if (unreduced) return value - (value * (odd + even));
            double result = D(AtanHi[id]) - (((value * (odd + even)) - D(AtanLo[id])) - value);
            return unchecked((long)original) >= 0 ? result : -result;
        }

        // f4705: complete original atan2(y,x), with original operand order.
        internal static double Atan2(double y, double x)
        {
            ulong yBits = DoubleBits(y), xBits = DoubleBits(x);
            if ((yBits & 0x7FFFFFFFFFFFFFFFUL) > 0x7FF0000000000000UL || (xBits & 0x7FFFFFFFFFFFFFFFUL) > 0x7FF0000000000000UL)
                return y + x;
            uint xHigh = unchecked((uint)(xBits >> 32)), xLow = unchecked((uint)xBits);
            if ((unchecked(xHigh - 1072693248u) | xLow) == 0u) return Atan(y);
            uint xSign = (xHigh >> 30) & 2u;
            uint yHigh = unchecked((uint)(yBits >> 32));
            int quadrant = unchecked((int)(xSign | (yHigh >> 31)));
            uint absYHigh = yHigh & 0x7FFFFFFFu;
            if ((absYHigh | unchecked((uint)yBits)) == 0u)
                return quadrant == 2 ? D(0x400921FB54442D18UL) : quadrant == 3 ? D(0xC00921FB54442D18UL) : y;
            uint absXHigh = xHigh & 0x7FFFFFFFu;
            if ((absXHigh | xLow) == 0u) return CopySign(D(0x3FF921FB54442D18UL), y);
            if (absXHigh == 2146435072u)
                return D(absYHigh == 2146435072u ? Atan2BothInfinite[quadrant] : Atan2XInfinite[quadrant]);
            if (absYHigh == 2146435072u || unchecked(absXHigh + 67108864u) < absYHigh)
                return CopySign(D(0x3FF921FB54442D18UL), y);
            double angle = xSign != 0u && unchecked(absYHigh + 67108864u) < absXHigh ? 0.0 : Atan(Abs(y / x));
            if (quadrant == 0) return angle;
            if (quadrant == 1) return -angle;
            double lowCorrection = D(0xBCA1A62633145C07UL);
            return quadrant == 2 ? D(0x400921FB54442D18UL) - (angle + lowCorrection) : (angle + lowCorrection) + D(0xC00921FB54442D18UL);
        }
    }
}
