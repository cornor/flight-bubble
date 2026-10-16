using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    // Source: PixelWing.ScreenLayout, static field layout and f54903.
    public static class ScreenLayout
    {
        public static float Height = 960f, Width = 540f, Scale = 1f;
        public static float Top, Bottom, Stretch = 1f;
        public static float MenuLeft = 10000f, MenuTop, MenuBottom, SystemLeft = 10000f;
        public static bool HasMenu;
        public const float AssistantButtonRatio = 1.35f;
        public const float EnemyScaleX = 1.56497705f; // WAT 0x3fc8512b
        public const float EnemyScaleY = 1.31f;

        // src: f54893. Preserve add/multiply/add grouping.
        public static Vector2 EnemyPoint(float x, float y) =>
            new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(x + -270f)) * EnemyScaleX) + 270f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(y + -562f)) * EnemyScaleY) + 562f));

        // src: f54894. The empty menu has x=1, not an all-zero rectangle.
        public static void Configure(int width, int height, Rect safe) =>
            Configure(width, height, safe, new Rect(1f, 0f, 0f, 0f));

        // src: f54895. In particular Top uses 1-(safe.height+safe.y).
        // The decompiler's unparenthesized expression is not authoritative.
        public static void Configure(int width, int height, Rect safe, Rect menu)
        {
            float sx = OriginalMath.Binary32(OriginalMath.Binary32((float)(width > 1 ? width : 1)) / 540f);
            float sy = OriginalMath.Binary32(OriginalMath.Binary32((float)(height > 1 ? height : 1)) / 960f);
            float scale = sx < sy ? sx : sy;
            Scale = scale;
            Width = OriginalMath.Binary32(OriginalMath.Binary32((float)width) / scale);
            Height = OriginalMath.Binary32(OriginalMath.Binary32((float)height) / scale);
            Bottom = OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(Height * safe.y), 0f) + 3f);
            Top = OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Binary32(Height * (OriginalMath.Binary32(1f - (OriginalMath.Binary32(safe.height + safe.y))))), 0f) + 3f);
            HasMenu = menu.width > 0f & menu.height > 0f;
            if (HasMenu)
            {
                MenuLeft = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(540f - Width)) * 0.5f) + OriginalMath.Binary32(menu.x * Width));
                MenuTop = OriginalMath.Binary32(menu.y * Height);
                MenuBottom = OriginalMath.Binary32(Height * (OriginalMath.Binary32(menu.y + menu.height)));
                float upper = OriginalMath.WasmMax(OriginalMath.Binary32(MenuTop + -4f), 0f);
                Top = Top < upper ? Top : upper;
                SystemLeft = OriginalMath.Binary32(OriginalMath.Binary32(MenuLeft + OriginalMath.Binary32((OriginalMath.Binary32(MenuBottom - MenuTop)) * -1.35f)) + -6f);
            }
            else
            {
                MenuLeft = 10000f;
                SystemLeft = 10000f;
                MenuBottom = Top;
                MenuTop = Top;
            }
            Stretch = OriginalMath.WasmMax(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(Height - Top) - Bottom) + -500f)) / 460f), 0.2f);
        }

        // src: f54896; strict overlap tests, no normalization of reversed rows.
        public static float RowRight(float y0, float y1) =>
            HasMenu && OriginalMath.Binary32(MenuTop + -2f) < y1 && OriginalMath.Binary32(MenuBottom + 2f) > y0 ? OriginalMath.Binary32(SystemLeft + -8f) : 528f;

        // src: f54897. Keep Top outside the entire lower-region sum.
        public static float Y(float y)
        {
            float top = Top;
            if (y <= 114f) return OriginalMath.Binary32(top + y);
            if (!(y < 574f)) return OriginalMath.Binary32(top + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(Stretch * 460f) + 114f) + y) + -574f));
            return OriginalMath.Binary32(top + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(y + -114f) * Stretch) + 114f));
        }

        // src: f54898. Keep the two additions before subtracting the span.
        public static float InverseY(float y)
        {
            y = OriginalMath.Binary32(y - Top);
            if (y <= 114f) return y;
            float stretch = Stretch;
            float span = OriginalMath.Binary32(stretch * 460f);
            if (!(y < OriginalMath.Binary32(span + 114f))) return OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(y + 574f) + -114f) - span);
            return OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(y + -114f)) / stretch) + 114f);
        }

        // src: f54899 / f54900 / f54901 / f54902.
        public static Vector2 Point(Vector2 p) => new Vector2(p.x, Y(p.y));
        public static float Left => OriginalMath.Binary32((OriginalMath.Binary32(540f - Width)) * 0.5f);
        public static float Right => OriginalMath.Binary32((OriginalMath.Binary32(Width + 540f)) * 0.5f);
        public static float SafeBottom => OriginalMath.Binary32(Height - Bottom);
    }
}
