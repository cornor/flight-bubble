using System.Collections.Generic;

namespace Flightbeat.Model
{
    public sealed partial class Finale
    {
        public const float Assemble = 1.15f, Charge = .5f, Sweep = 1.5f, Afterglow = .7f;
        public const float SweepStart = 1.65f, SweepEnd = 3.15f, Duration = 3.8500001f;
        public const float NoseX = 270f, NoseY = 562f, MaxSpread = 90f;
        public const int SignatureLevel = 3;
        // f55096: original constructor.
        public Finale()
        {
            NovaReach = 600f;
            HoleReach = 300f;
            RX = 240f; RY = 180f;
            CX = 270f; CY = 330f;
            Ships = new List<Fighter>();
        }
    }
    public sealed partial class SuperStrike
    {
        public const float Delay = 1f;
        // f55127: separate f32 operations, inclusive boundary, no radius guards.
        public bool Contains(float x, float y)
        {
            float dx = OriginalMath.Binary32((OriginalMath.Binary32(x - X)) / RadiusX);
            float dy = OriginalMath.Binary32((OriginalMath.Binary32(y - Y)) / RadiusY);
            return OriginalMath.Binary32((OriginalMath.Binary32(dx * dx)) + (OriginalMath.Binary32(dy * dy))) <= 1f;
        }
    }
}
