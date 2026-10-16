using System;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public static class EnemyMotion
    {
        public struct Pose
        {
            public int Sprite;
            public float Width, Height, Lift;
        }
        // f54003. The XOR's second operand is masked before XOR, and speed
        // uses the pre-XOR word. Neither is interchangeable with a generic hash.
        public static Pose Sample(int id, int color, float clock, bool compact)
        {
            uint state = unchecked((uint)id * 747796405u - 1403630843u);
            uint word = unchecked(((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u);
            float phase = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)((word >> 22) ^ (word & 1023u))) * .0009765625f)
                + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)((word >> 10) & 255u)) / 255f) * .54f) + .68f)) * clock));
            phase = OriginalMath.Binary32(phase - OriginalMath.Binary32((float)Math.Floor(phase)));
            float frameValue = OriginalMath.Binary32(phase * 4f);
            int frame = Math.Abs(frameValue) < 2147483648f ? (int)frameValue : int.MinValue;
            if (frame == 3) frame = 1;
            float angle = OriginalMath.Binary32(phase * 3.1415927410125732f);
            float pulse = OriginalMath.Binary32(.5f - OriginalMath.Binary32(OriginalMath.Cos(OriginalMath.Binary32(angle + angle)) * .5f));
            return new Pose
            {
                Sprite = compact ? unchecked(color % 3 * 3 + frame + 64) : frame == 1 ? 97 : 96,
                Lift = OriginalMath.Binary32(pulse * .025f),
                Height = OriginalMath.Binary32(OriginalMath.Binary32(pulse * .09f) + .91f),
                Width = OriginalMath.Binary32(OriginalMath.Binary32(pulse * -.07f) + 1f)
            };
        }
    }
}
