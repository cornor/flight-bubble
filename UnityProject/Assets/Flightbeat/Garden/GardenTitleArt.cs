#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    // Modal and album headings use native bold glyphs at 96 px in spare space of the existing bars texture.
    // Share the style with prefab text; ordinary rounded labels keep their original glyphs and UVs.
    public static class GardenTitleArt
    {
        public const float Size = 32f, TrackingEm = .025f, EdgeWidth = 1f, ShadowOffset = 1.6f;
        public static readonly Color Ink = GardenSkin.Hex(0x163E6D);
        public static readonly Color Shadow = new Color(.09f, .20f, .34f, .20f);
        static GardenSkin.Atlas data;
        static Dictionary<char, GardenSkin.Glyph> glyphs;
        public static GardenSkin.Atlas Data
        {
            get
            {
                if (data == null)
                {
                    data = JsonUtility.FromJson<GardenSkin.Atlas>(Resources.Load<TextAsset>("GardenR1/titles").text);
                    glyphs = new Dictionary<char, GardenSkin.Glyph>();
                    foreach (var g in data.glyphs) glyphs.Add((char)g.code, g);
                }
                return data;
            }
        }
        public static float TextWidth(string text, float size)
        {
            var atlas = Data;
            float width = Mathf.Max(0, text.Length - 1) * TrackingEm * size;
            foreach (char c in text) width += (glyphs.TryGetValue(c, out var g) ? g.advance : 1f) * size;
            return width;
        }
        public static void Draw(PixelSurface surface, string text, float cx, float cy, float size = Size, float maxWidth = 380f)
        {
            if (string.IsNullOrEmpty(text)) return;
            var atlas = Data;
            size *= Mathf.Min(1f, (maxWidth - 2f * EdgeWidth) / Mathf.Max(1f, TextWidth(text, size)));
            float top = float.MaxValue, bottom = float.MinValue;
            foreach (char c in text)
                if (glyphs.TryGetValue(c, out var g) && g.h > 0)
                {
                    float y = (.86f + g.bearingY) * size;
                    top = Mathf.Min(top, y); bottom = Mathf.Max(bottom, y + g.h / atlas.fontSize * size);
                }
            if (top > bottom) return;
            float x = cx - TextWidth(text, size) * .5f, baseline = cy - (top + bottom) * .5f;
            Paint(surface, text, x, baseline + ShadowOffset, size, Shadow);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Paint(surface, text, x + Mathf.Cos(angle) * EdgeWidth, baseline + Mathf.Sin(angle) * EdgeWidth, size, Color.white);
            }
            Paint(surface, text, x, baseline, size, Ink);
        }
        static void Paint(PixelSurface surface, string text, float x, float top, float size, Color ink)
        {
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out var g)) { x += (1f + TrackingEm) * size; continue; }
                if (g.w > 0 && g.h > 0)
                    surface.GardenQuad(x + g.bearingX * size, top + (.86f + g.bearingY) * size,
                        g.w / data.fontSize * size, g.h / data.fontSize * size,
                        new Rect(g.x / data.size, 1f - (g.y + g.h) / data.size, g.w / data.size, g.h / data.size), ink, atlas: 2);
                x += (g.advance + TrackingEm) * size;
            }
        }
    }
}
#endif
