using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public static class CombatPalette
    {
        // f54002: exact bytes from the nine paired i64 stores, in array order.
        private static readonly Color32[] Display =
        {
            new Color32(250,252,255,255), new Color32(25,225,157,255),
            new Color32(50,215,255,255), new Color32(42,116,255,255),
            new Color32(165,100,53,255), new Color32(185,81,255,255),
            new Color32(255,193,128,255), new Color32(255,224,42,255),
            new Color32(255,66,77,255), new Color32(161,242,39,255),
            new Color32(255,128,20,255), new Color32(99,116,139,255),
            new Color32(32,139,76,255), new Color32(229,42,152,255),
            new Color32(116,119,200,255), new Color32(199,190,255,255),
            new Color32(255,161,211,255), new Color32(190,255,204,255)
        };

        // f53999. Signed modulo and direct indexing retain invalid-id behavior.
        public static Color At(int id)
        {
            Color32 value = Display[id % Display.Length];
            return new Color(value.r / 255f, value.g / 255f, value.b / 255f, value.a / 255f);
        }

        // f54000. Only lessons take RGB from the level, with byte truncation.
        public static Color[] ForLevel(LevelData level)
        {
            int count = level.palette == null ? Display.Length : level.palette.Length / 3;
            var colors = new Color[count];
            for (int i = 0; i < count; i++)
                colors[i] = level.lesson > 0 && level.palette != null
                    ? new Color((level.palette[i * 3] & 255) / 255f, (level.palette[i * 3 + 1] & 255) / 255f,
                        (level.palette[i * 3 + 2] & 255) / 255f, 1f)
                    : At(i);
            return colors;
        }

        // f54001: source's three marks, with original segment endpoint order.
        public static void Mark(PixelSurface s, int color, Vector2 at, float size, Color tint)
        {
            switch (color % 3)
            {
                case 0:
                    float half = size * .5f;
                    s.Border(at.x - half, at.y - half, size, size, tint, 1f);
                    break;
                case 1:
                    float x = at.x + size * -.5f;
                    s.Rect(x, at.y + -2f, size, 1f, tint);
                    s.Rect(x, at.y + 1f, size, 1f, tint);
                    break;
                default:
                    var top = new Vector2(at.x + 0f, size * -.5f + at.y);
                    s.Segment(new Vector2(size * -.5f + at.x, at.y + 0f), top, 1f, tint);
                    s.Segment(top, new Vector2(size * .5f + at.x, at.y + 0f), 1f, tint);
                    break;
            }
        }
    }
}
