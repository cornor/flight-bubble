using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public static partial class FleetOpening
    {
        private static float Clamp(float value) => value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
        private static int Sequence(int index) => unchecked(index * 73 + 19) % 101;

        // f54037; f54039..f54041 inline the same instruction tree. The entire
        // delay is subtracted from time, despite the decompiler's flattened text.
        private static float Delay(int index, Vector2 target)
        {
            float x = (target.x + -12f) / 516f, y = (target.y + -114f) / 460f;
            float horizontal = x < 0f ? 0f : OriginalMath.WasmMin(x, 1f) * .55f;
            float vertical = y < 0f ? .45f : (1f - OriginalMath.WasmMin(y, 1f)) * .45f;
            return (float)Sequence(index) * .0035f + (horizontal + vertical) * 1.05f;
        }
        // f54038.
        public static float EnemyReveal(int index, float time)
        {
            float t = Clamp((time + (float)Sequence(index) * -.012f) / .3f);
            return t * t * (3f - (t + t));
        }
        // f54039: landing glow begins only after the fall completes.
        public static float Landing(int index, float time, Vector2 target)
        {
            float t = time - Delay(index, target);
            t = t + t;
            return t < 1f ? 0f : Clamp((t + -1f) * -3.5f + 1f);
        }
        // f54040.
        public static float Progress(int index, float time, Vector2 target)
        {
            float t = time - Delay(index, target);
            return Clamp(t + t);
        }
        // f54041: keep the original signed-byte remainder in the horizontal offset.
        public static Vector2 EnemyPosition(int index, float time, Vector2 target)
        {
            int hash = Sequence(index);
            float fromY = (float)hash * -3.2f + -60f;
            float t = Progress(index, time, target);
            t = Clamp(t * t * (t * -.6f + 1.6f));
            float resultY = fromY + (target.y - fromY) * t;
            float fromX = target.x + (float)(unchecked((sbyte)(unchecked((sbyte)hash) % 9 - 4)) * 5);
            return new Vector2(fromX + (target.x - fromX) * t, resultY);
        }
        // f54042. Preserve t*(t*t) and the quintic grouping from WAT.
        public static float BayOffset(float time, int lane, int row)
        {
            float t = Clamp((time + -2.85f + (float)lane * -.065f + (float)row * -.05f) / .7f);
            return (1f - (t * (t * t)) * (t * (t * 6f + -15f) + 10f)) * 160f;
        }
    }
}
