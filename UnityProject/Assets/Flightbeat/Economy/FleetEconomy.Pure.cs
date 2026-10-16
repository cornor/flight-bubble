namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54063: i64.add wraps, i64.div_s truncates toward zero, i32.wrap_i64 wraps.
        public static int DayOf(long utc) => unchecked((int)((utc + 28800L) / 86400L));

        // f54083: two i32.lt_u operations, followed by i32.and.
        private static bool ValidPart(int fighter, int part) =>
            unchecked((uint)fighter) < 5u & unchecked((uint)part) < 5u;

        // f54137: unsigned shifts and two wrapping i32 multiplications.
        private static uint Mix32(uint h)
        {
            unchecked
            {
                h = ((h >> 16) ^ h) * 0x7feb352du; // WAT i32.const 2146121005
                h = ((h >> 15) ^ h) * 0x846ca68bu; // WAT i32.const -2073254261
                return (h >> 16) ^ h;
            }
        }
    }
}
