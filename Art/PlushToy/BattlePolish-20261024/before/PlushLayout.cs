using UnityEngine;

namespace Flightbeat.Game
{
    // Presentation-only projection. Battle coordinates, timing and level data stay in the original space.
    // The inverse is also used by input, so aiming and queue taps follow the displayed objects.
    public static class PlushLayout
    {
        public static bool Enabled;
        static readonly float[] Source = { 0, 60, 114, 574, 647, 688, 721, 855, 880, 960 };
        static readonly float[] Target = { 0, 54, 60, 456, 590, 612, 679, 843, 880, 960 };
        static float At(int i)
        {
            float extra = ScreenLayout.Height - ScreenLayout.Top - ScreenLayout.Bottom - 960f;
            return ScreenLayout.Top + Target[i] + extra * Mathf.Clamp01((Target[i] - 60f) / 396f);
        }
        public static float Y(float y)
        {
            if (!Enabled) return ScreenLayout.Y(y);
            int i = 0;
            while (i < Source.Length - 2 && y > Source[i + 1]) i++;
            return Mathf.LerpUnclamped(At(i), At(i + 1), (y - Source[i]) / (Source[i + 1] - Source[i]));
        }
        public static float InverseY(float y)
        {
            if (!Enabled) return ScreenLayout.InverseY(y);
            int i = 0;
            while (i < Source.Length - 2 && y > At(i + 1)) i++;
            return Mathf.LerpUnclamped(Source[i], Source[i + 1], (y - At(i)) / (At(i + 1) - At(i)));
        }
        public static Vector2 Point(Vector2 p) => new Vector2(p.x, Y(p.y));
    }
}
