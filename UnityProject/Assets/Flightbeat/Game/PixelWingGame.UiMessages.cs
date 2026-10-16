using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        internal void Toast(string text, Color tint) { toast = text; toastAt = ui; toastTint = tint; } // f54538
        private void FloatUp(string text, float x, float y, Color c)
            => floats.Add(new FloatText { text = text, x = x, y = y, t = ui, c = c }); // f54561

        // f54817. Toast layout is in physical canvas coordinates, outside MapLayout.
        private void DrawToast(PixelSurface s)
        {
            float age = OriginalMath.Binary32(ui - toastAt);
            if (age > 2.2f || toast.Length == 0) return;
            float alpha = OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(age / .15f)) * Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(2.2f - age)) / .3f)));
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float offset = OriginalMath.Binary32((OriginalMath.Binary32(1f - OutBack(OriginalMath.Binary32(age / .35f), 1.9f))) * 30f);
            float width = OriginalMath.WasmMax(OriginalMath.Binary32(PixelSurface.Measure(toast, 1.3f) + 60f), 220f);
            float y = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Top + 118f) - offset);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(width * -.5f) + 270f), y, width, 40f,
                new Color(.05f, .16f, .22f, OriginalMath.Binary32(alpha * .96f)), new Color(.02f, .08f, .12f, OriginalMath.Binary32(alpha * .96f)),
                new Color(toastTint.r, toastTint.g, toastTint.b, OriginalMath.Binary32(alpha * toastTint.a)), 8f, 1f);
            // Folded f32 literals in f54817; recomputing the shadow offset is not equivalent.
            s.TextF(toast, 271.1700134277344f, OriginalMath.Binary32((OriginalMath.Binary32(y + 11f)) + 1.17f), 1.3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .7f)), 0);
            s.TextF(toast, 270f, OriginalMath.Binary32(y + 11f), 1.3f, new Color(1f, 1f, 1f, alpha), 0);
            s.MapLayout = mapped;
        }
        // f54818. Expired entries are removed by Paint, in reverse list order.
        private void DrawFloats(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            for (int i = floats.Count - 1; i >= 0; i--)
            {
                FloatText item = floats[i];
                float age = OriginalMath.Binary32(ui - item.t);
                if (age > 1.1f) { floats.RemoveAt(i); continue; }
                float alpha = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(1.1f - age)) / .4f)), t = OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(age * 4f)));
                float size = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * t))) + -1f) + 1f)) * .5f) + 1.4f);
                Title(s, item.text, item.x, OriginalMath.Binary32(item.y + OriginalMath.Binary32(age * -46f)), size,
                    A(item.c, alpha), new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .8f)), 0);
            }
            s.MapLayout = mapped;
        }
    }
}
