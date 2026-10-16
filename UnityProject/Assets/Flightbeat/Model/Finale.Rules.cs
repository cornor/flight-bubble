using System;

namespace Flightbeat.Model
{
    public sealed partial class Finale
    {
        // f55145: the signature mode starts at fighter stage 3.
        public static int ModeFor(int model, int fighterLevel) => fighterLevel < 3 ? 0
            : model == 0 ? 5 : Math.Max(1, Math.Min(model, 4));

        // f55148. Inclusive start, exclusive end.
        public bool Sweeping => Age >= 1.65f && Age < 3.15f;

        // f55140/f55098/f55099. Clamp comparisons preserve NaN and signed-zero
        // behavior. SpreadAt multiplies 90*t before multiplying the other t.
        private static float Ease(float t)
        {
            t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            float square = OriginalMath.Binary32(t * t);
            float twice = OriginalMath.Binary32(t + t);
            float curve = OriginalMath.Binary32(3f - twice);
            return OriginalMath.Binary32(square * curve);
        }
        public float SweepProgress
        {
            get
            {
                float shifted = OriginalMath.Binary32(Age + -1.65f);
                float t = OriginalMath.Binary32(shifted / 1.5f);
                return t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            }
        }
        public static float SpreadAt(float progress)
        {
            float shifted = OriginalMath.Binary32(progress + -.12f);
            float t = OriginalMath.Binary32(shifted / .88f);
            t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            float scaled = OriginalMath.Binary32(t * 90f);
            float square = OriginalMath.Binary32(t * scaled);
            float twice = OriginalMath.Binary32(t + t);
            float curve = OriginalMath.Binary32(3f - twice);
            return OriginalMath.Binary32(square * curve);
        }

        private float SweepEase()
        {
            float t = OriginalMath.Binary32((OriginalMath.Binary32(Age + -1.65f)) / 1.5f);
            t = OriginalMath.WasmMax(t < 0f ? 0f : OriginalMath.WasmMin(t, 1f), 0f);
            return OriginalMath.Binary32(OriginalMath.Binary32(t * t) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(t + t)))));
        }

        // f55146 / f55147. WAT keeps the easing product inside the radius lerp.
        public float NovaRadius => OriginalMath.Binary32(NovaNear + OriginalMath.Binary32((OriginalMath.Binary32(NovaReach - NovaNear)) * SweepEase()));
        // f55142 / f55143 / f55144. The easing product stays inside the radius multiply.
        public float HoleRadius => OriginalMath.Binary32(HoleReach * SweepEase());
        public float JetAngle => OriginalMath.Binary32(OriginalMath.Binary32(SweepEase() * 180f) + -90f);
        public float BarrierScale => OriginalMath.Binary32((OriginalMath.Binary32(1f - SweepEase())) * 1.08f);
        public float WipeY => OriginalMath.Binary32(OriginalMath.Binary32(SweepEase() * -560f) + 600f); // f55141
        public static float AngleOf(float x, float y) => OriginalMath.Binary32(OriginalMath.Atan2Single(OriginalMath.Binary32(x + -270f), OriginalMath.Binary32(562f - y)) * 57.295780181884766f);

        // f55079: all six source footprints, including inclusive boundaries.
        public bool Reached(float x, float y)
        {
            float dx, dy, radius;
            switch (Mode)
            {
                case 1: return OriginalMath.Binary32(OriginalMath.Binary32(SweepEase() * -560f) + 600f) <= y;
                case 2:
                    dx = OriginalMath.Binary32(x - CX); dy = OriginalMath.Binary32(y - CY); radius = OriginalMath.Binary32(HoleReach * SweepEase());
                    return OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= OriginalMath.Binary32(radius * radius);
                case 3: return AngleOf(x, y) <= OriginalMath.Binary32(OriginalMath.Binary32(SweepEase() * 180f) + -90f);
                case 4:
                    dx = OriginalMath.Binary32((OriginalMath.Binary32(x - CX)) / RX); dy = OriginalMath.Binary32((OriginalMath.Binary32(y - CY)) / RY); radius = OriginalMath.Binary32((OriginalMath.Binary32(1f - SweepEase())) * 1.08f);
                    return OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) >= OriginalMath.Binary32(radius * radius);
                case 5:
                    dx = OriginalMath.Binary32(x + -270f); dy = OriginalMath.Binary32(y + -562f); radius = NovaRadius;
                    return OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= OriginalMath.Binary32(radius * radius);
                default: return Math.Abs(AngleOf(x, y)) <= Spread;
            }
        }
    }
}
