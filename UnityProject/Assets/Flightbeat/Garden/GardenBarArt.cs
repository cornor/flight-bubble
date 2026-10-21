#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    // A separate atlas keeps the legacy UI glyphs and the already-full main atlas intact.
    public static class GardenBarArt
    {
        static GardenSkin.Atlas data;
        static Dictionary<string, GardenSkin.Slot> slots;
        static Dictionary<char, GardenSkin.Glyph> glyphs;
        public static GardenSkin.Atlas Data { get { Ready(); return data; } }
        static bool Ready()
        {
            if (data != null) return true;
            var resource = Resources.Load<TextAsset>("GardenR1/bars");
            if (resource == null) return false;
            data = JsonUtility.FromJson<GardenSkin.Atlas>(resource.text);
            slots = new Dictionary<string, GardenSkin.Slot>();
            glyphs = new Dictionary<char, GardenSkin.Glyph>();
            foreach (var slot in data.slots) slots.Add(slot.id, slot);
            foreach (var glyph in data.glyphs) glyphs[(char)glyph.code] = glyph;
            return true;
        }
        public static bool Plate(PixelSurface s, string id, float cx, float cy, float w, float h, Color edge, float alpha = 1f, float fill = 1f)
        {
            if (!Ready() || !slots.TryGetValue(id, out var slot)) return false;
            // Three horizontal slices: both caps scale uniformly with height; only the quiet centre stretches.
            float cap = id == "capsule" ? slot.h * 1.1f : slot.h * .75f;
            cap = Mathf.Min(cap, slot.w * .4f);
            float drawnCap = Mathf.Min(cap / slot.h * h, w * .48f);
            float left = cx - w * .5f, top = cy - h * .5f;
            // 2026-10-21 (quality pass): a faint navy shadow just under the glass so light bars still separate from the
            // pale sky and clouds behind them.
            if (id == "capsule" && fill > .9f) s.Glow(cx, cy + h * .4f, w * 1.02f, h * .7f, new Color(.09f, .2f, .38f, .16f * alpha), 0f);
            for (int i = 0; i < 3; i++)
            {
                float sx = i == 0 ? 0f : i == 1 ? cap : slot.w - cap;
                float sw = i == 1 ? slot.w - cap * 2f : cap;
                float dx = i == 0 ? left : i == 1 ? left + drawnCap : left + w - drawnCap;
                float dw = i == 1 ? w - drawnCap * 2f : drawnCap;
                var uv = new Rect((slot.x + sx) / data.size, 1f - (slot.y + slot.h) / data.size, sw / data.size, slot.h / data.size);
                if (id == "capsule") s.GardenGlassQuad(dx, top, dw, h, uv, new Color(edge.r, edge.g, edge.b, alpha), fill);
                else s.GardenQuad(dx, top, dw, h, uv, new Color(1, 1, 1, alpha), atlas: 2);
            }
            return true;
        }
        public static bool Ring(PixelSurface s, float cx, float cy, float size, float alpha)
        {
            if (!Ready() || !slots.TryGetValue("ring", out var slot)) return false;
            s.GardenQuad(cx - size * .5f, cy - size * .5f, size, size,
                new Rect(slot.x / data.size, 1f - (slot.y + slot.h) / data.size, slot.w / data.size, slot.h / data.size),
                new Color(1, 1, 1, alpha), atlas: 2);
            return true;
        }
        public static bool Art(PixelSurface s, string id, float cx, float cy, float w, float h, float alpha = 1f,
            Color? glassColor = null)
        {
            if (!Ready() || !slots.TryGetValue(id, out var slot)) return false;
            var uv = new Rect(slot.x / data.size, 1f - (slot.y + slot.h) / data.size, slot.w / data.size, slot.h / data.size);
            if (glassColor.HasValue) {
                Color c = glassColor.Value; c.a *= alpha;
                s.GardenGlassQuad(cx - w * .5f, cy - h * .5f, w, h, uv, c, 1f);
            } else s.GardenQuad(cx - w * .5f, cy - h * .5f, w, h, uv, new Color(1f, 1f, 1f, alpha), atlas: 2);
            return true;
        }
        public static float TextWidth(string text, float size)
        {
            if (!Ready()) return GardenSkin.TextWidth(text, size);
            float w = 0f;
            foreach (char c in text) w += (glyphs.TryGetValue(c, out var g) ? g.advance : 1f) * size;
            return w;
        }
        public static void Text(PixelSurface s, string text, float x, float y, float size = 17f, bool center = true, Color? color = null)
        {
            if (!Ready()) { GardenSkin.Text(s, text, x, y, size, center, color); return; }
            if (center) x -= TextWidth(text, size) * .5f;
            foreach (char c in text)
            {
                if (glyphs.TryGetValue(c, out var g))
                {
                    if (g.w > 0 && g.h > 0)
                    {
                        float qx = x + g.bearingX * size, qy = y + (.86f + g.bearingY) * size, qw = g.w / data.fontSize * size, qh = g.h / data.fontSize * size;
                        Stretch(ref qx, ref qy, ref qw, ref qh);
                        s.GardenQuad(qx, qy, qw, qh, new Rect(g.x / data.size, 1f - (g.y + g.h) / data.size, g.w / data.size, g.h / data.size),
                            color ?? GardenBars.Navy, atlas: 2);
                    }
                    x += g.advance * size;
                }
                else { GardenSkin.Text(s, c.ToString(), x, y, size, false, color); x += size; }
            }
        }
        // 2026-10-21 (combo show): glyphs with a vertical colour ramp over the em box (all letters share it), and a light
        // sweep whose strength at each quad corner follows its horizontal distance to `sweepX`. Screen coordinates.
        // 2026-10-21 12:22: optional squash and stretch for the combo words, about a centre x and a base line y (screen
        // coordinates); 1 / 1 leaves every text untouched. Callers set it around one letter and reset it.
        public static float StretchX = 1f, StretchY = 1f, StretchCX, StretchBaseY;
        static void Stretch(ref float x, ref float y, ref float w, ref float h)
        {
            if (StretchX == 1f && StretchY == 1f) return;
            x = StretchCX + (x - StretchCX) * StretchX; w *= StretchX;
            y = StretchBaseY + (y - StretchBaseY) * StretchY; h *= StretchY;
        }
        static Rect GlyphUv(GardenSkin.Glyph g) => new Rect(g.x / data.size, 1f - (g.y + g.h) / data.size, g.w / data.size, g.h / data.size);
        public static void TextRamp(PixelSurface s, string text, float x, float y, float size, Color top, Color bottom, bool center = true)
        {
            if (!Ready()) { Text(s, text, x, y, size, center, Color.Lerp(top, bottom, .5f)); return; }
            if (center) x -= TextWidth(text, size) * .5f;
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out var g)) { x += size; continue; }
                if (g.w > 0 && g.h > 0)
                {
                    float gy = y + (.86f + g.bearingY) * size, gh = g.h / data.fontSize * size;
                    Color ct = Color.Lerp(top, bottom, Mathf.Clamp01((gy - y) / size)), cb = Color.Lerp(top, bottom, Mathf.Clamp01((gy + gh - y) / size));
                    float qx = x + g.bearingX * size, qw = g.w / data.fontSize * size;
                    Stretch(ref qx, ref gy, ref qw, ref gh);
                    s.GardenQuadColors(qx, gy, qw, gh, GlyphUv(g), ct, ct, cb, cb, 2);
                }
                x += g.advance * size;
            }
        }
        public static void TextSweep(PixelSurface s, string text, float x, float y, float size, float sweepX, float band, Color light, bool center = true)
        {
            if (!Ready()) return;
            if (center) x -= TextWidth(text, size) * .5f;
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out var g)) { x += size; continue; }
                if (g.w > 0 && g.h > 0)
                {
                    float gx = x + g.bearingX * size, gw = g.w / data.fontSize * size, gy = y + (.86f + g.bearingY) * size, gh = g.h / data.fontSize * size;
                    // The band leans: the top meets the sweep a little later than the bottom.
                    float Bump(float px) { float d = Mathf.Clamp01(1f - Mathf.Abs(px - sweepX) / band); return d * d; }
                    float lean = size * .25f, l1 = Bump(gx - lean), r1 = Bump(gx + gw - lean), l2 = Bump(gx + lean), r2 = Bump(gx + gw + lean);
                    if (l1 + r1 + l2 + r2 > .001f)
                    {
                        Color A(float k) { Color q = light; q.a *= k; return q; }
                        Stretch(ref gx, ref gy, ref gw, ref gh);
                        s.GardenQuadColors(gx, gy, gw, gh, GlyphUv(g), A(l1), A(r1), A(r2), A(l2), 2);
                    }
                }
                x += g.advance * size;
            }
        }
        public static void TextOutline(PixelSurface s, string text, float x, float y, float size, Color fill, Color outline,
            float thickness = 1.6f, bool center = true)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Text(s, text, x + Mathf.Cos(a) * thickness, y + Mathf.Sin(a) * thickness, size, center, outline);
            }
            Text(s, text, x, y, size, center, fill);
        }
    }
}
#endif
