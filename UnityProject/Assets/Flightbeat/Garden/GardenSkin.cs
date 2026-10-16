#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    // R1 presentation only. Colors are supplied directly from the current LevelData palette.
    public static class GardenSkin
    {
        // v3 bubble squad (UI spec v3): navy ink, glass-white paper, sky transition, #8C919C mosaic panel.
        public static readonly Color Ink = Hex(0x2A3B55), Paper = Hex(0xF6FAFF), Soil = Hex(0x9FD4FF), Wood = Hex(0x5A80B8), Leaf = Hex(0x86A67E), Field = Hex(0x8C919C);
        public static bool HideOriginalParticles;
        // v2: trimmed slots keep their full frame (fw, fh) and the trimmed offset (ox, oy) in stored pixels.
        [Serializable] public sealed class Slot { public string id; public float x, y, w, h, ox, oy, fw, fh; }
        [Serializable] public sealed class Glyph { public int code; public float x, y, w, h, advance, bearingX, bearingY; }
        [Serializable] public sealed class Atlas { public string characters; public int size, cell, fontSize; public Slot[] slots; public Glyph[] glyphs; }
        static Atlas atlas;
        static Dictionary<string, Slot> slots;
        static Dictionary<char, Glyph> glyphs;
        sealed class BugPair { public Slot main, fixedLayer; }
        static readonly Dictionary<int, BugPair> bugs = new Dictionary<int, BugPair>();
        public static Atlas Data {
            get {
                if (atlas == null) {
                    atlas = JsonUtility.FromJson<Atlas>(Resources.Load<TextAsset>("GardenR1/atlas").text);
                    slots = new Dictionary<string, Slot>();
                    foreach (var slot in atlas.slots) slots.Add(slot.id, slot);
                    glyphs = new Dictionary<char, Glyph>();
                    foreach (var glyph in atlas.glyphs) glyphs[(char)glyph.code] = glyph;
                }
                return atlas;
            }
        }
        public static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        public static string Key(Color color) {
            Color32 c = color; int rgb = (c.r << 16) | (c.g << 8) | c.b;
            switch (rgb) {
                case 0x42A9FF: case 0x2982F4: return "morning-glory";
                case 0xFF9D30: case 0xFF7E27: return "carrot";
                case 0x62E18E: case 0x30D183: return "broccoli";
                case 0xBD80FF: return "eggplant";
                case 0xFFDF48: return "sunflower";
                case 0xFFFFFF: return "cabbage";
                case 0xFF525B: return "strawberry";
                case 0x2D2D2D: return "truffle";
                default: return "unknown";
            }
        }
        // v3 creature names; internal slot ids keep the v2 keys (UI spec v3 section 5).
        public static string Name(Color c) {
            switch (Key(c)) {
                case "morning-glory": return "小蓝"; case "carrot": return "小橙";
                case "broccoli": return "小绿"; case "eggplant": return "小紫";
                case "sunflower": return "小黄"; case "cabbage": return "小白";
                case "strawberry": return "小红"; case "truffle": return "小黑";
                default: return "队员";
            }
        }
        public static bool Has(string id) => Data != null && slots.ContainsKey(id);
        static Rect Uv(Slot slot, int size) => new Rect(slot.x / size, 1f - (slot.y + slot.h) / size, slot.w / size, slot.h / size);
        // (x, y, w, h) is the full frame; a trimmed slot is drawn at its offset inside it.
        public static bool Asset(PixelSurface s, string id, float x, float y, float w, float h, Color tint, bool pixel = false, bool rigid = true, bool flipX = false, bool flipY = false) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            if (slot.fw > 0 && slot.fh > 0) {
                float sx = w / slot.fw, sy = h / slot.fh;
                float ox = flipX ? slot.fw - slot.ox - slot.w : slot.ox, oy = flipY ? slot.fh - slot.oy - slot.h : slot.oy;
                x += ox * sx; y += oy * sy; w = slot.w * sx; h = slot.h * sy;
            }
            Rect uv = Uv(slot, data.size);
            if (flipX) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            if (flipY) uv = new Rect(uv.x, uv.y + uv.height, uv.width, -uv.height);
            s.GardenQuad(x, y, w, h, uv, tint, pixel, rigid);
            return true;
        }
        // Horizontal fill bar: draws the left `fraction` of the full frame.
        public static bool AssetPart(PixelSurface s, string id, float x, float y, float w, float h, float fraction, Color tint) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            float fw = slot.fw > 0 ? slot.fw : slot.w, fh = slot.fh > 0 ? slot.fh : slot.h;
            float sx = w / fw, sy = h / fh, cut = Mathf.Clamp(Mathf.Clamp01(fraction) * fw - slot.ox, 0f, slot.w);
            if (cut <= 0f) return true;
            s.GardenQuad(x + slot.ox * sx, y + slot.oy * sy, cut * sx, slot.h * sy,
                new Rect(slot.x / data.size, 1f - (slot.y + slot.h) / data.size, cut / data.size, slot.h / data.size), tint, false, true);
            return true;
        }
        // Nine-slice: `border` is in stored atlas pixels, `edge` is the drawn border in logical pixels.
        public static bool Asset9(PixelSurface s, string id, float x, float y, float w, float h, float border, float edge, Color tint) =>
            Asset9(s, id, x, y, w, h, border, border, edge, edge, tint);
        // Separate horizontal/vertical borders (bx, by in stored pixels; ex, ey drawn): wide banners and buttons use by = 0.
        public static bool Asset9(PixelSurface s, string id, float x, float y, float w, float h, float bx, float by, float ex, float ey, Color tint) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            // Vertically sliced panels and speech bubbles must map as one rect.
            // Independently mapping rigid tile centers leaves gaps on tall screens.
            // Horizontal-only planks (by == 0) retain their fixed-height behavior.
            bool mapped = s.MapLayout && by > 0;
            if (mapped) { float bottom = ScreenLayout.Y(y + h); y = ScreenLayout.Y(y); h = bottom - y; s.MapLayout = false; }
            try {
                float[] sxs = { 0, bx, slot.w - bx, slot.w }, sys = { 0, by, slot.h - by, slot.h };
                float[] dxs = { x, x + ex, x + w - ex, x + w }, dys = { y, y + ey, y + h - ey, y + h };
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++) {
                        float u0 = slot.x + sxs[i], v0 = slot.y + sys[j], uw = sxs[i + 1] - sxs[i], vh = sys[j + 1] - sys[j];
                        s.GardenQuad(dxs[i], dys[j], dxs[i + 1] - dxs[i], dys[j + 1] - dys[j],
                            new Rect(u0 / data.size, 1f - (v0 + vh) / data.size, uw / data.size, vh / data.size), tint, false, true);
                    }
                return true;
            } finally { if (mapped) s.MapLayout = true; }
        }
        // Full-screen art in the three layout bands (ScreenLayout.Y is piecewise at 114 and 574),
        // so stretched viewports keep the fence and plots where the logic expects them.
        static readonly float[] Bands = { 0f, 114f, 574f, 960f };
        public static bool AssetBands(PixelSurface s, string id, Color tint) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            for (int i = 0; i < 3; i++) {
                float a = Bands[i], b = Bands[i + 1];
                float py0 = slot.y + slot.h * a / 960f, py1 = slot.y + slot.h * b / 960f;
                s.GardenQuad(0f, a, 540f, b - a, new Rect(slot.x / data.size, 1f - py1 / data.size, slot.w / data.size, (py1 - py0) / data.size), tint, false, false);
            }
            return true;
        }
        // Lower-band art from `top` to the bottom of the 540x960 layout (stretches with the bottom band on tall screens).
        public static bool AssetBandsFrom(PixelSurface s, string id, float top) {
            if (!Has(id)) return false;
            return Asset(s, id, 0f, top, 540f, 960f - top, Color.white, false, false);
        }
        public static string HexKey(Color color) { Color32 c = color; return ((c.r << 16) | (c.g << 8) | c.b).ToString("X6"); }
        // v2 plants are finished full-color art per level color.
        public static string PlantSlot(Color tint, string state) => "plants/" + Key(tint) + "/" + HexKey(tint) + "/" + state;
        // Cache the two atlas slots per palette color: no per-bug strings, objects or materials.
        public static void Bug(PixelSurface s, Color tint, Vector2 at, float height, bool teaching) {
            var data = Data; Color32 c = tint; int key = (c.r << 16) | (c.g << 8) | c.b;
            int cacheKey = key | (teaching ? 1 << 24 : 0);
            if (!bugs.TryGetValue(cacheKey, out var pair)) {
                pair = new BugPair();
                if (teaching) {
                    // v2 bead: tinted body + fixed highlight, feet and the plant mark (color-blind aid).
                    slots.TryGetValue("bugs/bead-teaching-main", out pair.main);
                    if (!slots.TryGetValue("bugs/bead-teaching-" + Key(tint) + "-fixed", out pair.fixedLayer))
                        slots.TryGetValue("bugs/bead-teaching-fixed", out pair.fixedLayer);
                } else {
                    slots.TryGetValue("bugs/bead-main", out pair.main);
                    slots.TryGetValue(key == 0xFFFFFF ? "bugs/bead-white-fixed" : key == 0x2D2D2D ? "bugs/bead-dark-fixed" : "bugs/bead-fixed", out pair.fixedLayer);
                }
                bugs[cacheKey] = pair;
            }
            if (pair.main == null) { s.GardenSolid(at.x - height / 2, at.y - height / 2, height, height, tint); return; }
            float width = height * pair.main.w / pair.main.h;
            DrawBugLayer(s, pair.main, at, width, height, tint, data.size, teaching);
            if (pair.fixedLayer != null) DrawBugLayer(s, pair.fixedLayer, at, width, height, Color.white, data.size, teaching);
        }
        static void DrawBugLayer(PixelSurface s, Slot slot, Vector2 at, float w, float h, Color tint, int atlasSize, bool pixel) {
            s.GardenQuad(at.x - w / 2, at.y - h / 2, w, h,
                new Rect(slot.x / atlasSize, 1f - (slot.y + slot.h) / atlasSize, slot.w / atlasSize, slot.h / atlasSize), tint, pixel);
        }
        public static void Text(PixelSurface s, string text, float x, float y, float size = 17f, bool center = true, Color? color = null) {
            var data = Data;
            float width = 0;
            foreach (char c in text) width += glyphs.TryGetValue(c, out var g) ? g.advance * size : size;
            if (center) x -= width * .5f;
            foreach (char c in text) {
                if (glyphs.TryGetValue(c, out var glyph)) {
                    if (glyph.w > 0 && glyph.h > 0) s.GardenQuad(x + glyph.bearingX * size, y + (.86f + glyph.bearingY) * size, glyph.w / data.fontSize * size, glyph.h / data.fontSize * size,
                        new Rect(glyph.x / data.size, 1f - (glyph.y + glyph.h) / data.size, glyph.w / data.size, glyph.h / data.size), color ?? Ink);
                    x += glyph.advance * size;
                } else {
                    x += size;
                }
            }
        }
        // Game-style label: fill plus an eight-direction outline (UI spec T2/T4/N1).
        public static readonly Color Outline = Hex(0x23406B), Cream = Hex(0xFFFFFF), Gold = Hex(0xFFD54A);
        public static void TextOutline(PixelSurface s, string text, float x, float y, float size, Color fill, Color outline, float thickness = 1.6f, bool center = true) {
            for (int i = 0; i < 8; i++) {
                float a = i * Mathf.PI / 4f;
                Text(s, text, x + Mathf.Cos(a) * thickness, y + Mathf.Sin(a) * thickness, size, center, outline);
            }
            Text(s, text, x, y, size, center, fill);
        }
        public static float TextWidth(string text, float size) {
            var data = Data; float width = 0;
            foreach (char c in text) width += glyphs.TryGetValue(c, out var g) ? g.advance * size : size;
            return width;
        }
        // v3 creature canvases are 224x256 (7:8) with the feet on y=212 (UI spec v3 4.2); body is at most ~150/256 tall.
        public const float ArtAspect = 112f / 128f, FeetLine = 212f / 256f, BodyTop = 62f / 256f;
        // Draws a creature standing on `feet`. Number: a navy badge under the feet (bays) or a plain outlined number (queue).
        // Presentation clock for idle motion; set once per frame by the HUD (the game's UI time, never Battle time).
        public static float Clock;
        // True while the level is won: creatures switch to their cheer pose (if delivered) and bounce.
        public static bool Celebrate;
        // `breath` > 0 adds a gentle idle squash-and-stretch (amplitude) with its own `phase`; `attack` (0..1, decays over
        // .18 s after a real shot) squashes the body and lets it spring back. Feet stay on the same line.
        public static void Creature(PixelSurface s, Color tint, float centerX, float feet, float canvasHeight, int tier, string ammo,
            float attack = 0f, float numberSize = 14f, bool showTier = true, bool badge = true, float breath = 0f, float phase = 0f) {
            float scale = 1f + Mathf.Min(tier, 3) * .06f;
            if (Celebrate) breath *= 2.2f;                                                  // happy bounce on a win
            float wave = breath > 0f ? Mathf.Sin(Clock * (Celebrate ? 7f : 3.9f) + phase) : 0f;  // ~1.6 s cycle
            float spring = attack > 0f ? Mathf.Sin(attack * Mathf.PI) : 0f;                 // 0 -> 1 -> 0 over the shot
            float sy = 1f + breath * wave - .16f * attack + .06f * spring, sx = 1f - breath * .7f * wave + .12f * attack;
            if (ammo == "0") { sy = 1f + breath * .4f * wave; sx = 1f; }                    // spent: only a tired slow sway
            float ah = canvasHeight * scale * sy, aw = canvasHeight * scale * ArtAspect * sx;
            float ay = feet - ah * FeetLine, ax = centerX - aw / 2;
            string state = ammo == "0" ? "spent" : attack > .1f ? "attack" : "idle";
            // Optional art (art-v3 items 58/59): cheer on a win, and a short random blink while idle.
            if (Celebrate && Has(PlantSlot(tint, "cheer"))) state = "cheer";
            else if (state == "idle" && breath > 0f) {
                float cycle = 3.1f + Mathf.Abs(phase) % 1.9f;
                if ((Clock + phase * 1.37f) % cycle < .13f && Has(PlantSlot(tint, "blink"))) state = "blink";
            }
            if (!Asset(s, PlantSlot(tint, state), ax, ay, aw, ah, Color.white)) {
                float w = aw * .55f, h = ah * .5f;
                s.Rect(centerX - w / 2 - 1, feet - h - 1, w + 2, h + 2, Ink);
                s.Rect(centerX - w / 2, feet - h, w, h, tint);
            }
            float top = ay + ah * BodyTop, numberTop = feet + 2f;
            if (ammo.Length > 0) {
                if (badge) {
                    float bh = numberSize * 1.3f, bw = Mathf.Max(bh * 1.6f, TextWidth(ammo, numberSize) + bh * .9f);
                    if (!Asset9(s, "scene/badge-ammo", centerX - bw / 2, numberTop, bw, bh, 14f, 0f, bh * .5f, 0f, Color.white))
                        s.Rect(centerX - bw / 2, numberTop, bw, bh, Hex(0x1F3557));
                    Text(s, ammo, centerX, numberTop + (bh - numberSize) * .5f - numberSize * .06f, numberSize, true, Color.white);
                    if (showTier && tier > 0 && tier < 3) {
                        float c = bh * 1.1f, cx = centerX - bw / 2 - c * .55f;
                        if (!Asset(s, "scene/tier-coin", cx - c / 2, numberTop + (bh - c) / 2, c, c, Color.white))
                            s.Rect(cx - c / 2, numberTop + (bh - c) / 2, c, c, Gold);
                        Text(s, tier == 1 ? "Ⅰ" : "Ⅱ", cx, numberTop + (bh - c) / 2 + c * .14f, c * .62f, true, Hex(0x7A4A00));
                    }
                } else TextOutline(s, ammo, centerX, numberTop, numberSize, Color.white, Outline, Mathf.Max(1.2f, numberSize * .1f));
            }
            if (showTier && tier >= 3) {
                Asset(s, "scene/crown", centerX - 12f, top - 20f, 24f, 18f, Color.white);
                TextOutline(s, "MAX", centerX, top - 5f, 11f, Gold, Outline, 1.3f);
            } else if (showTier && tier > 0 && !badge) TextOutline(s, tier == 1 ? "Ⅰ" : "Ⅱ", centerX + aw * .3f, top, 12f, Gold, Outline, 1.3f);
        }
        // Projectile core: the bead body tinted with the shooter's color plus its fixed highlight (UI spec v3 4.4).
        public static void Shot(PixelSurface s, Color tint, Vector2 at, float size, float alpha) {
            Color c = new Color(tint.r, tint.g, tint.b, alpha);
            if (!Asset(s, "bugs/bead-main", at.x - size / 2, at.y - size / 2, size, size, c)) { s.Rect(at.x - size / 2, at.y - size / 2, size, size, c); return; }
            Color32 k = tint; int key = (k.r << 16) | (k.g << 8) | k.b;   // same rim as the grid beads: white and charcoal get a gray rim
            Asset(s, key == 0xFFFFFF ? "bugs/bead-white-fixed" : key == 0x2D2D2D ? "bugs/bead-dark-fixed" : "bugs/bead-fixed",
                  at.x - size / 2, at.y - size / 2, size, size, new Color(1, 1, 1, alpha));
        }
        // Kamikaze and other single creatures (no badge): `at` is the body center.
        public static void Plant(PixelSurface s, Color tint, Vector2 at, int tier, string ammo, float attack = 0f, bool waiting = false, bool showName = true, float displayScale = 1f) {
            Creature(s, tint, at.x, at.y + 18f * displayScale, 76f * displayScale, tier, "", attack, 12f, false, false);
            if (waiting) TextOutline(s, "等待", at.x, at.y + 22f, 11f, Cream, Outline, 1.2f);
        }
        // UI spec v3 4.3: golden swirl on an empty open pad. Rotate the texture (not the quad) and lay it flat (x0.45).
        // The slot keeps a sqrt(2) transparent margin, so rotated UV corners never leave it.
        public static void Swirl(PixelSurface s, float centerX, float centerY, float width, float angle, float alpha) {
            var data = Data;
            if (!slots.TryGetValue("scene/pad-swirl", out var slot)) return;
            float h = width * .45f, half = slot.w / 2f / 1.42f, cu = slot.x + slot.w / 2f, cv = slot.y + slot.h / 2f;
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            Vector2 Uv(float px, float py) {
                float rx = px * cs - py * sn, ry = px * sn + py * cs;
                return new Vector2((cu + rx * half) / data.size, 1f - (cv + ry * half) / data.size);
            }
            // bottom-left, bottom-right, top-right, top-left in screen space; texture y grows downward.
            s.GardenQuadUv(centerX - width / 2, centerY - h / 2, width, h, Uv(-1, 1), Uv(1, 1), Uv(1, -1), Uv(-1, -1), new Color(1, 1, 1, alpha));
        }
        public static string Goal(int step) {
            switch (step) {
                case 0: return "让蓝色队员打掉蓝色泡泡";
                case 3: return "再派两个蓝色队员，合成！";
                case 4: return "合成成功，队员变强了！";
                case 5: return "看最下排泡泡的颜色，换队员";
                case 10: return "留意脚下数字，守住护栏";
                case 12: return "队员打完了，及时接力";
                default: return "打光泡泡，守住护栏";
            }
        }
        public static string[] Narration(int topic) {
            switch (topic) {
                case 0: return new[] { "来，点一下蓝色队员出战。", "它会自己打最下排的同色泡泡。" };
                case 1: return new[] { "再派两个蓝色队员试试！", "三个同色同级，会自动合成。" };
                case 2: return new[] { "瞧，三个合成一个啦！", "攻击更强，还空出两个出战位。" };
                case 3: return new[] { "别急，同色泡泡被挡住啦。", "先清最下排，队员就会继续出手。" };
                case 4: return new[] { "看看最下排泡泡的颜色。", "派出同色队员，一起打！" };
                case 5: return new[] { "脚下的数字是剩余泡泡数。", "队员每吐一次，都会消耗哦。" };
                case 6: return new[] { "打完的队员会冲出去推回泡泡。", "出战位空了，快派新队员接力！" };
                default: return new[] { "队员全都出战，就能点决战啦。", "一起出手，把泡泡打光！" };
            }
        }
    }
}
#endif
