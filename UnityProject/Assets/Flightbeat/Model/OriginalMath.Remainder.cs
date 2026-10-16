namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // src: f10845 (fmodf). Integer significand subtraction follows the
        // original WAT, including subnormal normalization and signed zero.
        internal static float Remainder(float x, float y)
        {
            unchecked
            {
                uint uy = FloatBits(y), ux = FloatBits(x);
                int ex = (int)((ux >> 23) & 255), ey = (int)((uy >> 23) & 255);
                if ((uy << 1) == 0 || (uy & 0x7fffffff) > 0x7f800000 || ex == 255)
                {
                    float product = OriginalMath.Binary32(x * y);
                    return OriginalMath.Binary32(product / product);
                }
                if ((uy << 1) >= (ux << 1)) return (uy << 1) == (ux << 1) ? OriginalMath.Binary32(x * 0f) : x;
                uint mx, my;
                if (ex == 0)
                {
                    uint scan = ux << 9;
                    while ((int)scan >= 0) { ex--; scan <<= 1; }
                    mx = ux << (1 - ex);
                }
                else mx = (ux & 0x7fffff) | 0x800000;
                if (ey == 0)
                {
                    uint scan = uy << 9;
                    while ((int)scan >= 0) { ey--; scan <<= 1; }
                    my = uy << (1 - ey);
                }
                else my = (uy & 0x7fffff) | 0x800000;
                while (ex > ey)
                {
                    uint difference = mx - my;
                    if ((int)difference >= 0)
                    {
                        mx = difference;
                        if (mx == 0) return OriginalMath.Binary32(x * 0f);
                    }
                    mx <<= 1;
                    ex--;
                }
                uint last = mx - my;
                if ((int)last >= 0)
                {
                    mx = last;
                    if (mx == 0) return OriginalMath.Binary32(x * 0f);
                }
                while (mx <= 0x7fffff) { ex--; mx <<= 1; }
                uint result = ex > 0 ? (mx - 0x800000) | ((uint)ex << 23) : mx >> (1 - ex);
                return F(result | (ux & 0x80000000));
            }
        }
    }
}
