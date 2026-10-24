#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    // 2026-10-20 hint-bar redesign (Art/R1/ui-bars-design-20261020, docs/r1/提示条UI-切图与改动清单.md). One light glass
    // capsule for every hint, its edge colour saying what kind of message it is, an icon from the atlas
    // (ui/bar-icon-*; drawn fallbacks stay when a slot is missing) and, for some kinds, small see-through decoration
    // bubbles sitting on the rim. Everything is in screen units: call with MapLayout off.
    public static class GardenBars
    {
        // Edge colours by kind, and the inks.
        public static readonly Color Sky = GardenSkin.Hex(0xB58DEB), Coral = GardenSkin.Hex(0xF08A6C), Ice = GardenSkin.Hex(0x4FD8EE),
            Amber = GardenSkin.Hex(0xF5B731), Mint = GardenSkin.Hex(0x4FD6A0), Gold = GardenSkin.Hex(0xF7C23A), Purple = GardenSkin.Hex(0xA660F0);
        public static readonly Color Navy = GardenSkin.Hex(0x422580), Brown = GardenSkin.Hex(0x7A4A00), DeepPurple = GardenSkin.Hex(0x4A1F86),
            DeepCoral = GardenSkin.Hex(0xB0302A);
        public enum Deco { None, One, Many }
        public static readonly Color Quiet = GardenSkin.Hex(0xB29ACF);

        static Color Al(Color c, float a) { c.a *= a; return c; }

        // Screen y of a logical y under the surface's current mapping (the capsule is drawn in 1 px rows, so callers
        // switch MapLayout off and map the centre themselves, as the battle hints do).
        public static float ScreenY(PixelSurface s, float logicalY) => s.MapLayout ? Flightbeat.Game.PlushLayout.Y(logicalY) : logicalY;

        public static void Chip(PixelSurface s, string text, float cx, float cy, float w, float h,
            Color edge, Color ink, string icon = null, float size = 14f, float alpha = 1f)   // 2026-10-21: default 13 -> 14
        {
            bool mapped = s.MapLayout; cy = ScreenY(s, cy); s.MapLayout = false;
            try { Bar(s, cx, cy, w, h, edge, icon, text, size, ink, alpha); }
            finally { s.MapLayout = mapped; }
        }

        // 2026-10-21 10:21 (user: the bold 等待 drew too much attention; it is a quiet note). Grey-blue glass, a little
        // see-through, with the regular-weight UI font (the rounded font only comes in bold) in a soft slate ink.
        public static void QuietChip(PixelSurface s, string text, float cx, float cy, float w, float h, float size, float alpha = 1f)
        {
            bool mapped = s.MapLayout; cy = ScreenY(s, cy); s.MapLayout = false;
            try
            {
                Capsule(s, cx, cy, w, h, Quiet, alpha * .8f);
                // 2026-10-22 11:16 (user: soft on phones): a darker ink (#4A6688 -> #34507A) so the thin regular strokes hold.
                GardenSkin.Text(s, text, cx, cy - size * .56f, size, true, Al(GardenSkin.Hex(0x34507A), alpha));
            }
            finally { s.MapLayout = mapped; }
        }

        // Painted glass with fixed end caps. The procedural version is only a missing-resource fallback.
        public static void Capsule(PixelSurface s, float cx, float cy, float w, float h, Color edgeColor, float alpha = 1f, float fill = 1f)
        {
            if (GardenBarArt.Plate(s, "capsule", cx, cy, w, h, edgeColor, alpha, fill)) return;
            float r = h * .5f, left = cx - w * .5f + r, right = cx + w * .5f - r, top = cy - r;
            s.Glow(cx, cy + 4f, w + 22f, h + 24f, Al(GardenSkin.Hex(0x3982B7), .12f * alpha), 0f);
            Color upper = Al(GardenSkin.Hex(0xE6F6FF), .98f * alpha * fill), middle = Al(GardenSkin.Hex(0xD6EEFC), .96f * alpha * fill),
                lower = Al(GardenSkin.Hex(0xC2E5F8), .95f * alpha * fill);
            for (int row = 0; row < (int)h; row++)
            {
                float t = (row + .5f) / h, dy = row + .5f - r;
                float cap = Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));
                Color band = t < .5f ? Color.Lerp(upper, middle, t * 2f) : Color.Lerp(middle, lower, (t - .5f) * 2f);
                s.Rect(left - cap, top + row, right - left + cap * 2f, 1f, band);
            }
            // Inner glow of the edge colour, hugging the inside of the rim.
            Color inner = Al(edgeColor, .22f * alpha);
            s.Segment(new Vector2(left, top + 2.4f), new Vector2(right, top + 2.4f), 2.4f, inner);
            s.Segment(new Vector2(left, cy + r - 2.4f), new Vector2(right, cy + r - 2.4f), 2.4f, inner);
            s.Ring(new Vector2(left, cy), r - 2.4f, inner, 2.4f, 24, 90f, 180f);
            s.Ring(new Vector2(right, cy), r - 2.4f, inner, 2.4f, 24, -90f, 180f);
            Color rim = Al(GardenSkin.Hex(0xF7FFFF), .94f * alpha);
            s.Segment(new Vector2(left, top), new Vector2(right, top), 1.3f, rim);
            s.Segment(new Vector2(left, cy + r), new Vector2(right, cy + r), 1f, Al(rim, .75f));
            s.Ring(new Vector2(left, cy), r, rim, 1.2f, 24, 90f, 180f);
            s.Ring(new Vector2(right, cy), r, rim, 1.2f, 24, -90f, 180f);
            s.Segment(new Vector2(left + 4f, top + 1.8f), new Vector2(right - 4f, top + 1.8f), 1f, Al(Color.white, .48f * alpha));
            Color edge = Al(edgeColor, .92f * alpha);
            const float e = 2.6f;
            s.Segment(new Vector2(left, top - 1.6f), new Vector2(right, top - 1.6f), e, edge);
            s.Segment(new Vector2(left, cy + r + 1.6f), new Vector2(right, cy + r + 1.6f), e, edge);
            s.Ring(new Vector2(left, cy), r + 1.6f, edge, e, 28, 90f, 180f);
            s.Ring(new Vector2(right, cy), r + 1.6f, edge, e, 28, -90f, 180f);
        }

        static readonly Dictionary<string, string> iconIds = new Dictionary<string, string>();
        static string IconId(string name)
        {
            if (!iconIds.TryGetValue(name, out var id)) iconIds[name] = id = "ui/bar-icon-" + name;
            return id;
        }
        public static bool HasIcon(string name) => !string.IsNullOrEmpty(name) && GardenSkin.Has(IconId(name));
        public static bool Icon(PixelSurface s, string name, float cx, float cy, float size, float alpha = 1f)
        {
            if (!HasIcon(name)) return false;
            GardenSkin.Content(IconId(name), out var content);
            // `size` describes visible art, not the exported transparent square.
            float frame = size / Mathf.Max(.01f, Mathf.Max(content.width, content.height));
            return GardenSkin.Asset(s, IconId(name), cx - content.center.x * frame, cy - content.center.y * frame,
                frame, frame, new Color(1f, 1f, 1f, alpha));
        }

        // Width a one-line bar needs for its icon and text (ends included).
        public static float Width(string text, float size, float h, string icon)
        {
            float iconW = HasIcon(icon) ? h * .86f + size * .36f : 0f;
            return GardenBarArt.TextWidth(text, size) + iconW + h * .8f;
        }

        // One-line bar: capsule, visible icon (86% of height) and rounded text, centred as a group.
        // `outline` gives the text a white outline (see-through bars over the board).
        public static void Bar(PixelSurface s, float cx, float cy, float w, float h, Color edge, string icon, string text, float size, Color ink,
            float alpha = 1f, float fill = 1f, Deco deco = Deco.None, float age = 1f, bool outline = false)
        {
            // 2026-10-21 (quality pass): one entrance for every bar: a 0.22 s pop from 90 % with a slight overshoot and
            // a 0.12 s fade-in. `age` is seconds since the bar appeared; callers that do not track it pass 1 (settled).
            if (age < .22f)
            {
                float e = Mathf.Clamp01(age / .22f), k = .9f + .1f * (1f - (1f - e) * (1f - e)) + .04f * Mathf.Sin(e * Mathf.PI);
                w *= k; h *= k; size *= k; alpha *= Mathf.Clamp01(age / .12f);
            }
            Capsule(s, cx, cy, w, h, edge, alpha, fill);
            bool hasIcon = HasIcon(icon);
            float iconSize = h * .86f, gap = size * .36f;
            float room = Mathf.Max(1f, w - h * .8f - (hasIcon ? iconSize + gap : 0f));
            size = Mathf.Min(size, room / Mathf.Max(.01f, GardenBarArt.TextWidth(text, 1f)));
            float textW = GardenBarArt.TextWidth(text, size);
            float group = textW + (hasIcon ? iconSize + gap : 0f), x = cx - group * .5f;
            if (hasIcon) { Icon(s, icon, x + iconSize * .5f, cy, iconSize, alpha); x += iconSize + gap; }
            float ty = cy - size * .55f;
            if (outline) GardenBarArt.TextOutline(s, text, x, ty, size, Al(ink, alpha), Al(Color.white, .9f * alpha), 1.2f, false);
            else GardenBarArt.Text(s, text, x, ty, size, false, Al(ink, alpha));
            if (deco != Deco.None) Bubbles(s, cx, cy, w, h, edge, deco, Seed(text), alpha, age);
        }

        public static int Seed(string text)
        {
            int k = 0;
            for (int i = 0; i < text.Length; i++) k = k * 31 + text[i];
            return k & 0x7fffffff;
        }

        // Decoration bubbles on the rim (review 2026-10-20: not all on the left, not loud). See-through glass in the
        // edge colour, 15-20 % of the bar height, sitting on the outline at one of three spots chosen by `seed` (the
        // same text always gets the same spot); Deco.One is exactly one, Many is two or three in total. They pop out of
        // the rim when the bar appears (`age` = seconds since then) and float up a little.
        public static void Bubbles(PixelSurface s, float cx, float cy, float w, float h, Color edge, Deco deco, int seed, float alpha, float age)
        {
            if (deco == Deco.None) return;
            int count = deco == Deco.Many ? 2 + ((seed >> 3) & 1) : 1;
            float d = Mathf.Max(6f, h * .18f);
            for (int i = 0; i < count; i++)
            {
                // A third bubble is a small companion at the first spot, not another pair.
                bool companion = i == 2;
                int spot = (seed % 3 + (companion ? 0 : i * 2)) % 3;
                Vector2 at = spot == 0 ? new Vector2(cx + w * .5f - h * .55f, cy - h * .5f)          // top right
                    : spot == 1 ? new Vector2(cx - w * .5f + h * .6f, cy + h * .5f)                  // bottom left
                    : new Vector2(cx + w * .5f, cy - h * .12f);                                      // right end
                if (companion) at += new Vector2(d * .95f, -d * .7f);
                float t = Mathf.Clamp01((age - i * .08f) / .35f);
                float grow = t < 1f ? Mathf.Sin(t * Mathf.PI * .5f) * (1f + .25f * Mathf.Sin(t * Mathf.PI)) : 1f;
                float rise = (1f - t) * h * .25f;
                One(s, at + new Vector2(0f, rise), d * (companion ? .55f : 1f) * grow, edge, alpha);
            }
        }
        static void One(PixelSurface s, Vector2 at, float d, Color edge, float alpha)
        {
            if (d < 1f) return;
            Color tint = Color.Lerp(edge, Color.white, .55f);
            s.Glow(at.x, at.y, d * 1.15f, d * 1.15f, Al(tint, .35f * alpha), 0f);
            s.Ring(at, d * .5f, Al(tint, .55f * alpha), Mathf.Max(1f, d * .1f), 16, 0f, 360f);
            s.Glow(at.x - d * .18f, at.y - d * .18f, d * .32f, d * .32f, Al(Color.white, .85f * alpha), 0f);
        }
    }
}
#endif
