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
        [Serializable] public sealed class Slot { public string id; public int atlas; public float x, y, w, h, ox, oy, fw, fh; }
        [Serializable] public sealed class Glyph { public int code; public float x, y, w, h, advance, bearingX, bearingY; }
        [Serializable] public sealed class Atlas { public string characters; public int size, cell, fontSize, creatureWidth, creatureHeight; public Slot[] slots; public Glyph[] glyphs; }
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
            Color32 c = color; return GardenColorCatalog.Key((c.r << 16) | (c.g << 8) | c.b);
        }
        public static string Name(Color color) {
            Color32 c = color; return GardenColorCatalog.Name((c.r << 16) | (c.g << 8) | c.b);
        }
        public static bool Has(string id) => Data != null && slots.ContainsKey(id);
        // Visible (trimmed) part of a slot as fractions of its full frame: x, y, width, height in 0..1.
        public static bool Content(string id, out Rect content) {
            content = new Rect(0, 0, 1, 1);
            if (Data == null || !slots.TryGetValue(id, out var slot) || !(slot.fw > 0 && slot.fh > 0)) return false;
            content = new Rect(slot.ox / slot.fw, slot.oy / slot.fh, slot.w / slot.fw, slot.h / slot.fh);
            return true;
        }
        // 2026-10-21: the creature atlas (slot.atlas 1) has its own size (4096 x 2048), given in atlas.json.
        static Rect Uv(Slot slot, int size) {
            float w = size, h = size;
            if (slot.atlas == 1 && atlas != null && atlas.creatureWidth > 0) { w = atlas.creatureWidth; h = atlas.creatureHeight; }
            return new Rect(slot.x / w, 1f - (slot.y + slot.h) / h, slot.w / w, slot.h / h);
        }
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
            s.GardenQuad(x, y, w, h, uv, tint, pixel, rigid, slot.atlas);
            return true;
        }
        // 2026-10-21: an atlas image rotated by `degrees` (clockwise on screen) about `center`, for falling props.
        public static bool AssetRotated(PixelSurface s, string id, Vector2 center, float w, float h, float degrees, Color tint) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            float fw = slot.fw > 0 ? slot.fw : slot.w, fh = slot.fh > 0 ? slot.fh : slot.h, sx = w / fw, sy = h / fh;
            Vector2 off = new Vector2((slot.ox + slot.w * .5f - fw * .5f) * sx, (slot.oy + slot.h * .5f - fh * .5f) * sy);
            float hw = slot.w * sx * .5f, hh = slot.h * sy * .5f, r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), n = Mathf.Sin(r);
            bool mapped = s.MapLayout;
            if (mapped) { center = ScreenLayout.Point(center); s.MapLayout = false; }
            try {
                Vector2 P(float x, float y) { x += off.x; y += off.y; return center + new Vector2(x * c - y * n, x * n + y * c); }
                s.GardenQuadCorners(P(-hw, -hh), P(hw, -hh), P(hw, hh), P(-hw, hh), Uv(slot, data.size), tint, slot.atlas);
            } finally { s.MapLayout = mapped; }
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
        // 2026-10-18: the frame-x range [a, b] (fractions of the full frame width) of an untrimmed-in-x slot, drawn with
        // the scale of a full-width draw `fullW` wide and its left end at `destX`. Used to scroll the tiled rail.
        public static bool AssetSpan(PixelSurface s, string id, float destX, float y, float fullW, float h, float a, float b, Color tint) {
            var data = Data;
            if (!slots.TryGetValue(id, out var slot)) return false;
            float fw = slot.fw > 0 ? slot.fw : slot.w, fh = slot.fh > 0 ? slot.fh : slot.h;
            float sx = fullW / fw, sy = h / fh, u0 = Mathf.Clamp(a * fw - slot.ox, 0f, slot.w), u1 = Mathf.Clamp(b * fw - slot.ox, 0f, slot.w);
            if (u1 - u0 <= .01f) return true;
            s.GardenQuad(destX + (slot.ox + u0 - a * fw) * sx, y + slot.oy * sy, (u1 - u0) * sx, slot.h * sy,
                new Rect((slot.x + u0) / data.size, 1f - (slot.y + slot.h) / data.size, (u1 - u0) / data.size, slot.h / data.size), tint, false, true);
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
        public static void Bug(PixelSurface s, Color tint, Vector2 at, float height, bool teaching, float width = 0f) {
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
            if (width <= 0f) width = pair.main == null ? height : height * pair.main.w / pair.main.h;
            if (pair.main == null) { s.GardenSolid(at.x - width / 2, at.y - height / 2, width, height, tint); return; }
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
        // 2026-10-20 14:07 (user: 小翠 looks clearly smaller). Every tier scales all colours alike (1 + .06 per tier), but
        // three members are drawn thinner on the shared 224x256 canvas: visible body area against the median of the 23
        // members is 小翠 67-78 % (narrowest, 117 wide vs ~160), 小清 77-81 %, 小玫 77-84 % depending on the pose.
        // They are drawn larger by these factors (about the square root of the area gap; 小翠 kept a little lower because
        // it is already of average height). Applies wherever a member's art is drawn: pads, queue, finale, codex, opening.
        public static float ArtScale(Color tint) {
            Color32 c = tint;
            switch ((c.r << 16) | (c.g << 8) | c.b) {
                case 0x0C7E1C: return 1.12f;   // 小翠
                case 0xC5FFE4: return 1.10f;   // 小清
                case 0x9C225E: return 1.07f;   // 小玫
                default: return 1f;
            }
        }
        // 2026-10-21 10:12 (user: the shots cover the face; they still leave from the mouth, small at first, for every
        // member). Mouth centre of each attack pose on the 224x256 canvas, measured on the delivered art (mouth interior
        // and tongue). They range from x 84 (berry, its head sits on the left) to 130, and from y 110 (forest, mint,
        // pale mint) to 149 (lime); the old shared point was (119, 135).
        public static Vector2 MouthCanvas(Color tint) {
            Color32 c = tint;
            switch ((c.r << 16) | (c.g << 8) | c.b) {
                case 0x9C225E: return new Vector2(84f, 146f);    // berry
                case 0x2982F4: case 0x42A9FF: return new Vector2(124f, 135f);   // blue
                case 0x65392D: return new Vector2(120f, 137f);   // brown
                case 0x2D2D2D: return new Vector2(122f, 126f);   // charcoal
                case 0x46D9FF: return new Vector2(117f, 121f);   // cyan
                case 0x0C7E1C: return new Vector2(120f, 110f);   // forest
                case 0x62E18E: return new Vector2(130f, 137f);   // green
                case 0xC8C5FF: return new Vector2(121f, 140f);   // lavender
                case 0x92EF4D: return new Vector2(122f, 149f);   // lime
                case 0x30D183: return new Vector2(120f, 113f);   // mint
                case 0xFF7E27: return new Vector2(118f, 138f);   // orange
                case 0xFF9D30: return new Vector2(118f, 137f);   // orange (light)
                case 0xC5FFE4: return new Vector2(119f, 114f);   // pale mint
                case 0xFFCAA9: return new Vector2(124f, 127f);   // peach
                case 0xFFC5E0: return new Vector2(122f, 139f);   // pink
                case 0xBD80FF: return new Vector2(118f, 141f);   // purple
                case 0xFF525B: return new Vector2(112f, 148f);   // red
                case 0x696CAF: return new Vector2(130f, 126f);   // slate blue
                case 0xB672FD: return new Vector2(118f, 132f);   // violet
                case 0xFFFFFF: return new Vector2(118f, 138f);   // white
                case 0xFFDF48: return new Vector2(120f, 137f);   // yellow
                default: return new Vector2(119f, 135f);
            }
        }
        // Mouth of the member last drawn by Creature() (screen point); the pads keep it per member for the shots.
        public static Vector2 LastMouth;
        public static void Creature(PixelSurface s, Color tint, float centerX, float feet, float canvasHeight, int tier, string ammo,
            float attack = 0f, float numberSize = 14f, bool showTier = true, bool badge = true, float breath = 0f, float phase = 0f,
            float sizeScale = 0f, float squash = 0f, float numberLift = 0f, float fire = -1f, float pulse = 0f, float ready = 0f, bool sleep = false, float outline = 0f) {
            float scale = (sizeScale > 0f ? sizeScale : 1f + Mathf.Min(tier, 3) * .06f) * ArtScale(tint);
            if (Celebrate) breath *= 2.2f;                                                  // happy bounce on a win
            // ~1.6 s cycle; 2026-10-21 sleepers (queue rows behind the front) breathe slower, about 2.6 s.
            float wave = breath > 0f ? Mathf.Sin(Clock * (Celebrate ? 7f : sleep ? 2.4f : 3.9f) + phase) : 0f;
            float spring = attack > 0f ? Mathf.Sin(attack * Mathf.PI) : 0f;                 // 0 -> 1 -> 0 over the shot
            float sy = 1f + breath * wave - .16f * attack + .06f * spring, sx = 1f - breath * .7f * wave + .12f * attack;
            // 2026-10-21 10:12 (user: wind up before firing, a stronger jet while firing). Pads pass `fire` (0..1, held while
            // shots keep coming, eased off over .2 s after the last one), `pulse` (1 -> 0 over .06 s after each shot) and
            // `ready` (has ammo and is not firing). Shots come every .07 s, so the old per-shot squash (.18 s) never got
            // back up and only jittered; now the body stays pressed while firing with a small beat per shot, and leans
            // back (a little taller and narrower) while it waits, ready to blow.
            if (fire >= 0f) {
                float calm = 1f - fire;
                sy = 1f + breath * wave * calm - .09f * fire - .05f * pulse + .045f * ready;
                sx = 1f - breath * .7f * wave * calm + .08f * fire + .035f * pulse - .035f * ready;
            }
            if (sleep && !Celebrate) { sy *= .98f; sx *= 1.015f; }                         // dozing: sunk a little
            if (ammo == "0") { sy = 1f + breath * .4f * wave; sx = 1f; }                    // spent: only a tired slow sway
            sy *= 1f - squash; sx *= 1f + squash;                                           // landing/stretch (feet stay put)
            float ah = canvasHeight * scale * sy, aw = canvasHeight * scale * ArtAspect * sx;
            float ay = feet - ah * FeetLine, ax = centerX - aw / 2;
            string state = ammo == "0" ? "spent" : attack > .1f || fire > .1f ? "attack" : "idle";
            Vector2 mouth = MouthCanvas(tint);
            LastMouth = new Vector2(ax + aw * mouth.x / 224f, ay + ah * mouth.y / 256f);
            // Optional art (art-v3 items 58/59): cheer on a win, and a short random blink while idle.
            if (Celebrate && Has(PlantSlot(tint, "cheer"))) state = "cheer";
            else if (state == "idle" && ready > .5f && Has(PlantSlot(tint, "ready"))) state = "ready";   // puffed cheeks (when delivered)
            else if (state == "idle" && sleep && Has(PlantSlot(tint, "blink"))) state = "blink";          // asleep: the eyes-closed frame
            else if (state == "idle" && breath > 0f) {
                float cycle = 3.1f + Mathf.Abs(phase) % 1.9f;
                if ((Clock + phase * 1.37f) % cycle < .13f && Has(PlantSlot(tint, "blink"))) state = "blink";
            }
            // 2026-10-21 (user): hidden members show the rainbow question-mark bubble when delivered; `outline` (alpha)
            // draws the white silhouette grown by 8 canvas px behind the member (front-row members you can tap).
            Color32 mk = tint; bool mystery = ((mk.r << 16) | (mk.g << 8) | mk.b) == 0x8C919C && Has("plants/mystery-bubble");
            if (outline > 0f) Asset(s, mystery ? "plants/mystery-bubble/outline" : PlantSlot(tint, "outline"), ax, ay, aw, ah, new Color(1f, 1f, 1f, outline));
            if (!Asset(s, mystery ? "plants/mystery-bubble" : PlantSlot(tint, state), ax, ay, aw, ah, Color.white)) {
                float w = aw * .55f, h = ah * .5f;
                s.Rect(centerX - w / 2 - 1, feet - h - 1, w + 2, h + 2, Ink);
                s.Rect(centerX - w / 2, feet - h, w, h, tint);
            }
            float top = ay + ah * BodyTop, numberTop = feet + 2f - numberLift;
            if (ammo.Length > 0) {
                if (badge) {
                    bool coin = showTier && tier > 0 && tier < 3;
                    // 2026-10-21 10:26 (user): as in the original (rank tag in the corner, count always centred), the pill
                    // stays centred under the feet whatever the tier; the tier coin hangs off its left end, overlapping
                    // the rim a little, instead of the coin + pill pair being centred (that pushed the count ~10 px right).
                    float bh = numberSize * 1.3f, bw = numberSize * 3.3f;
                    float cx = centerX;
                    float size = Mathf.Min(numberSize, (bw - 14f) / Mathf.Max(.01f, GardenBarArt.TextWidth(ammo, 1f)));   // 2026-10-21: 7 px each side
                    bool mapped = s.MapLayout;
                    float cy = GardenBars.ScreenY(s, numberTop + bh * .5f); s.MapLayout = false;
                    try {
                        GardenBarArt.Plate(s, "number-pill", cx, cy, bw, bh, Color.white);
                        GardenBarArt.Text(s, ammo, cx, cy - size * .55f, size, true, Color.white);
                        if (coin) {
                            // 2026-10-21 (user): the tier coin goes back to the earlier gold coin art and its Ⅰ / Ⅱ.
                            float c = bh * 1.1f, coinX = cx - bw * .5f - c * .2f;
                            if (!Asset(s, "scene/tier-coin", coinX - c / 2, cy - c / 2, c, c, Color.white))
                                s.Rect(coinX - c / 2, cy - c / 2, c, c, Gold);
                            Text(s, tier == 1 ? "Ⅰ" : "Ⅱ", coinX, cy - c / 2 + c * .14f, c * .62f, true, Hex(0x7A4A00));
                        }
                    } finally { s.MapLayout = mapped; }
                } else GardenBarArt.TextOutline(s, ammo, centerX, numberTop, numberSize, Color.white, Outline, Mathf.Max(1.2f, numberSize * .1f));
            }
            if (showTier && tier >= 3) {
                // 2026-10-21 10:39 (user): back to the earlier crown + gold MAX (the glass chip did not suit it).
                Asset(s, "scene/crown", centerX - 12f, top - 20f, 24f, 18f, Color.white);
                TextOutline(s, "MAX", centerX, top - 7f, 14f, Gold, Outline, 1.5f);
            } else if (showTier && tier > 0 && !badge) TextOutline(s, tier == 1 ? "Ⅰ" : "Ⅱ", centerX + aw * .3f, top, 15f, Gold, Outline, 1.5f);   // 2026-10-21: earlier style
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
            // 2026-10-21 (user): no ball icon; 10:21 a quiet regular-weight note instead of bold.
            if (waiting) GardenBars.QuietChip(s, "等待", at.x, at.y + 30f, 54f, 23f, 15f);
        }
        // UI spec v3 4.3: golden swirl on an empty open pad. Rotate the texture (not the quad) and lay it flat (x0.45).
        // The slot keeps a sqrt(2) transparent margin, so rotated UV corners never leave it.
        public static void Swirl(PixelSurface s, float centerX, float centerY, float width, float angle, float alpha, float flat = .45f) {
            var data = Data;
            if (!slots.TryGetValue("scene/pad-swirl", out var slot)) return;
            float h = width * flat, half = slot.w / 2f / 1.42f, cu = slot.x + slot.w / 2f, cv = slot.y + slot.h / 2f;
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            Vector2 Uv(float px, float py) {
                float rx = px * cs - py * sn, ry = px * sn + py * cs;
                return new Vector2((cu + rx * half) / data.size, 1f - (cv + ry * half) / data.size);
            }
            // bottom-left, bottom-right, top-right, top-left in screen space; texture y grows downward.
            s.GardenQuadUv(centerX - width / 2, centerY - h / 2, width, h, Uv(-1, 1), Uv(1, 1), Uv(1, -1), Uv(-1, -1), new Color(1, 1, 1, alpha));
        }
        // 2026-10-17: any square UI slot spun in place (texture rotated, quad fixed); corners are cropped by sqrt 2.
        public static void Spin(PixelSurface s, string id, float centerX, float centerY, float size, float angle, Color tint) {
            var data = Data;
            if (data == null || !slots.TryGetValue(id, out var slot)) return;
            float half = slot.w / 2f / 1.42f, cu = slot.x + slot.w / 2f, cv = slot.y + slot.h / 2f;
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            Vector2 Uv(float px, float py) {
                float rx = px * cs - py * sn, ry = px * sn + py * cs;
                return new Vector2((cu + rx * half) / data.size, 1f - (cv + ry * half) / data.size);
            }
            s.GardenQuadUv(centerX - size / 2, centerY - size / 2, size, size, Uv(-1, 1), Uv(1, 1), Uv(1, -1), Uv(-1, -1), tint, slot.atlas);
        }
        // 2026-10-17: draw a (possibly trimmed) slot as a size x size frame rotated about its centre; the quad turns,
        // not the texture, so art reaching its edges (the H14 halo) is never cropped or bleeds into neighbours.
        public static bool Rotated(PixelSurface s, string id, float centerX, float centerY, float size, float angle, Color tint) {
            var data = Data;
            if (data == null || !slots.TryGetValue(id, out var slot)) return false;
            float fw = slot.fw > 0 ? slot.fw : slot.w, fh = slot.fh > 0 ? slot.fh : slot.h;
            float x0 = (slot.ox / fw - .5f) * size, x1 = ((slot.ox + slot.w) / fw - .5f) * size;
            float y0 = (slot.oy / fh - .5f) * size, y1 = ((slot.oy + slot.h) / fh - .5f) * size;
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            Vector2 P(float x, float y) => new Vector2(centerX + x * cs - y * sn, centerY + x * sn + y * cs);
            s.GardenQuadCorners(P(x0, y0), P(x1, y0), P(x1, y1), P(x0, y1), Uv(slot, data.size), tint, slot.atlas);
            return true;
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
                // 2026-10-21 14:24: {key words} are coloured in the dialogue bubble.
                case 0: return new[] { "来，点一下{蓝色队员}出战。", "它会自己打{最下排}的{同色泡泡}。" };
                case 1: return new[] { "再派两个{蓝色队员}试试！", "{三个同色同级}，会自动合成。" };
                case 2: return new[] { "瞧，{三个合成一个}啦！", "攻击更强，还空出{两个出战位}。" };
                case 3: return new[] { "别急，同色泡泡{被挡住}啦。", "先清{最下排}，队员就会继续出手。" };
                case 4: return new[] { "看看{最下排}泡泡的颜色。", "派出{同色队员}，一起打！" };
                case 5: return new[] { "脚下的数字是{剩余泡泡数}。", "队员每吐一次，都会{消耗}哦。" };
                case 6: return new[] { "打完的队员会{冲出去}推回泡泡。", "出战位空了，快派{新队员}接力！" };
                default: return new[] { "队员全都出战，就能点{决战}啦。", "一起出手，把泡泡{打光}！" };
            }
        }
    }
}
#endif
