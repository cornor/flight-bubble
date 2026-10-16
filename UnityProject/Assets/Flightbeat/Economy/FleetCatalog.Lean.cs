namespace Flightbeat.Economy
{
    public static partial class FleetCatalog
    {
        // f54017: the original f54689 caller supplies a new float[16].
        // Keep both f32 additions and their store order; do not clamp these values.
        public static void Lean(int fighter, float[] into)
        {
            switch (fighter)
            {
                case 1:
                    into[0] += 0.08f;   // +0x1.47ae14p-4, bits 0x3da3d70a
                    into[3] += -0.08f;  // -0x1.47ae14p-4, bits 0xbda3d70a
                    break;
                case 2:
                    into[3] += 0.12f;   // +0x1.eb851ep-4, bits 0x3df5c28f
                    into[0] += -0.06f;  // -0x1.eb851ep-5, bits 0xbd75c28f
                    break;
                case 3:
                    into[4] += 0.18f;   // +0x1.70a3d8p-3, bits 0x3e3851ec
                    break;
                case 4:
                    into[7] += 0.15f;   // +0x1.333334p-3, bits 0x3e19999a
                    break;
            }
        }
    }
}
