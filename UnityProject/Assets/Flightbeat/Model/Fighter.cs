namespace Flightbeat.Model
{
    public sealed partial class Fighter
    {
        // f54954: expand the original paired stores without changing their order.
        public Fighter(int color, int ammo, int id)
        {
            ScanDirection = 1;
            LastFired = -10f; LaunchedAt = -10f;
            ScanColumn = -1f;
            InitialAmmo = ammo;
            Color = color;
            Queue = color;
            Id = id;
            Ammo = ammo;
        }
        // f54955: only Tier 1 has a volley of three, including invalid signed tiers.
        public int Volley => Tier == 1 ? 3 : 1;
        // f54956: multiply AFTER choosing the exact f32 multiplier.
        public float Interval(float baseInterval) => OriginalMath.Binary32((Tier > 2 ? .13f : Tier == 2 ? .18f : Tier == 1 ? .72f : .82f) * baseInterval);
        // f54957: WAT multiplies the denominator BEFORE division.
        public float DPS(float baseInterval)
        {
            float factor = Tier > 2 ? .13f : Tier == 2 ? .18f : Tier == 1 ? .72f : .82f;
            float count = Tier > 2 ? 8f : Tier == 1 ? 3f : 1f;
            return OriginalMath.Binary32(count / (OriginalMath.Binary32(factor * baseInterval)));
        }
    }
}
