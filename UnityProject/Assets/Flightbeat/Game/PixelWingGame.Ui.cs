using System;
using System.Collections.Generic;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        public enum Btn { Primary, Secondary, Danger, Ghost, Green }
        private struct UiHit { public float x, y, w, h; public int id; }
        private struct FloatText { public string text; public float x, y, t; public Color c; }

        // f54860: original capacities and field initializers.
        private readonly List<UiHit> uiHits = new List<UiHit>(96);
        private readonly List<UiHit> uiHitsBuild = new List<UiHit>(96);
        private readonly List<FloatText> floats = new List<FloatText>();
        private int pressedId = -1;
        private float pressedAt = -10f;

        // f54556 / f54530.
        internal float Since(int key) => uiMarks.TryGetValue(key, out float at) ? OriginalMath.Binary32(ui - at) : 999f;
        internal void MarkUi(int key) => uiMarks[key] = ui;
        // f54507. Preserve the comparison form (including NaN).
        internal static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;
        // f54524 / f54506 / f54514: WAT grouping, not decompiler precedence.
        private static float OutCubic(float t) { t = OriginalMath.Binary32(1f - Clamp01(t)); return OriginalMath.Binary32(1f - OriginalMath.Binary32(t * (OriginalMath.Binary32(t * t)))); }
        internal static float OutBack(float t, float k = 1.9f) { t = OriginalMath.Binary32(Clamp01(t) + -1f); return OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * k))) + (OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * (OriginalMath.Binary32(t * (OriginalMath.Binary32(k + 1f))))))) + 1f))); }
        private static float InOut(float t) { t = Clamp01(t); return OriginalMath.Binary32(OriginalMath.Binary32(t * t) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(t + t))))); }
        // f54490 / f50651. Color.Lerp's original inline clamp uses f32.min.
        internal static Color A(Color c, float a) { c.a = OriginalMath.Binary32(c.a * a); return c; }
        private static Color Mix(Color a, Color b, float t)
        {
            t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            // f50651: each subtract, multiply and add is a distinct f32 result.
            return new Color(OriginalMath.Binary32(a.r + OriginalMath.Binary32(t * OriginalMath.Binary32(b.r - a.r))),
                OriginalMath.Binary32(a.g + OriginalMath.Binary32(t * OriginalMath.Binary32(b.g - a.g))),
                OriginalMath.Binary32(a.b + OriginalMath.Binary32(t * OriginalMath.Binary32(b.b - a.b))),
                OriginalMath.Binary32(a.a + OriginalMath.Binary32(t * OriginalMath.Binary32(b.a - a.a))));
        }

        // f54492. Only the HUD contributes hit regions; mapping happens at build time.
        private void HitBox(PixelSurface s, int id, float x, float y, float w, float h)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54492);
#endif
            ObserveHitBegin(s, id, x, y, w, h);
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (s == gardenOverlay && gardenOverlay != null)
            {
                if (s.MapLayout) { float end = ScreenLayout.Y(y + h); y = ScreenLayout.Y(y); h = end - y; }
                gardenOverlayHits.Add(new UiHit { x = x, y = y, w = w, h = h, id = id });
                ObserveHitEnd(); return;
            }
#endif
            if (s != hud) { ObserveHitEnd(); return; }
            if (s.MapLayout) { float bottom = OriginalMath.Binary32(y + h); y = ScreenLayout.Y(y); h = OriginalMath.Binary32(ScreenLayout.Y(bottom) - y); }
            uiHitsBuild.Add(new UiHit { x = x, y = y, w = w, h = h, id = id });
            ObserveHitEnd();
        }
        // f54522. A modal block is a real zero-id region at its paint-order position.
        private void HitBlock(PixelSurface s)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (s == gardenOverlay && gardenOverlay != null)
                gardenOverlayHits.Add(new UiHit { x = -2000f, y = -2000f, w = 6000f, h = 6000f, id = 0 });
#endif
            if (s == hud) uiHitsBuild.Add(new UiHit { x = -2000f, y = -2000f, w = 6000f, h = 6000f, id = 0 });
        }
        private void BeginHits() {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54831);
#endif
            BeginHitsCore();
        } // f54831
        private void EndHits() {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54832);
#endif
            EndHitsCore();
        } // f54832
        // f54815 inlines both list operations, without calling f54831/f54832.
        private void BeginHitsCore() => uiHitsBuild.Clear();
        private void EndHitsCore() { uiHits.Clear(); uiHits.AddRange(uiHitsBuild); }
        // f54639. Inclusive bounds and reverse paint order are intentional.
        private int HitTest(Vector2 logical)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (TryGardenUiHit(logical, out int gardenId)) return gardenId;
#endif
            float y = ScreenLayout.Y(logical.y);
            for (int i = uiHits.Count - 1; i >= 0; i--)
            {
                UiHit h = uiHits[i];
                if (logical.x >= h.x && logical.x <= OriginalMath.Binary32(h.x + h.w) && y >= h.y && y <= OriginalMath.Binary32(h.y + h.h)) return h.id;
            }
            return -1;
        }
        private void Pressed(int id) { pressedId = id; pressedAt = ui; } // f54642
        // f54495. 0x40490FDB is the original f32 pi operand.
        private float PressPulse(int id)
        {
            if (pressedId != id) return 0f;
            float t = OriginalMath.Binary32((OriginalMath.Binary32(ui - pressedAt)) / .22f);
            return t >= 1f ? 0f : OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(t * -.3f) + 1f)) * OriginalMath.Sin(OriginalMath.Binary32(Clamp01(t) * 3.1415927410125732f)));
        }

        // f54488.
        internal void Label(PixelSurface s, string text, float x, float y, float size, Color color, int align = 0, float shadow = 1f)
        {
            if (shadow > 0f)
            {
                float offset = OriginalMath.WasmMax(OriginalMath.Binary32(size * .9f), 1f);
                s.TextF(text, OriginalMath.Binary32(offset + x), OriginalMath.Binary32(offset + y), size, new Color(0f, 0f, 0f, OriginalMath.Binary32(OriginalMath.Binary32(color.a * .7f) * shadow)), align);
            }
            s.TextF(text, x, y, size, color, align);
        }
        // f54519. Preserve all eight outline draws, then lower shadow, then face.
        internal void Title(PixelSurface s, string text, float x, float y, float size, Color color, Color outline, int align = 0)
        {
            float k = OriginalMath.WasmMax(OriginalMath.Binary32(size * .8f), 1f);
            const float diagonal = .7071067690849304f;
            s.TextF(text, OriginalMath.Binary32(k + x), OriginalMath.Binary32(OriginalMath.Binary32(k * 0f) + y), size, outline, align);
            float lower = OriginalMath.Binary32(OriginalMath.Binary32(k * diagonal) + y);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * diagonal) + x), lower, size, outline, align);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * -0f) + x), OriginalMath.Binary32(k + y), size, outline, align);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * -diagonal) + x), lower, size, outline, align);
            s.TextF(text, OriginalMath.Binary32(x - k), OriginalMath.Binary32(OriginalMath.Binary32(k * -0f) + y), size, outline, align);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * -diagonal) + x), OriginalMath.Binary32(OriginalMath.Binary32(k * -diagonal) + y), size, outline, align);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * 0f) + x), OriginalMath.Binary32(y - k), size, outline, align);
            s.TextF(text, OriginalMath.Binary32(OriginalMath.Binary32(k * diagonal) + x), OriginalMath.Binary32(OriginalMath.Binary32(k * -diagonal) + y), size, outline, align);
            s.TextF(text, x, OriginalMath.Binary32(OriginalMath.Binary32(k * 1.6f) + y), size, A(outline, .8f), align);
            s.TextF(text, x, y, size, color, align);
        }

        // f54487. Notch strips and segment endpoints follow the WAT; in particular
        // its inset is notch-(stripY-y), not the flattened decompiler expression.
        internal void Plate(PixelSurface s, float x, float y, float w, float h,
            Color top, Color bottom, Color edge, float notch = 6f, float alpha = 1f)
        {
            float limit = OriginalMath.Binary32((w < h ? w : h) * .3f);
            float n = notch < limit ? notch : limit;
            float left = OriginalMath.Binary32(n + x), end = OriginalMath.Binary32(y + h), twice = OriginalMath.Binary32(n + n), middle = OriginalMath.Binary32(w - twice);
            s.Rect(left, end, OriginalMath.Binary32(middle + 3f), 3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .45f)));
            float slicesFloat = OriginalMath.Binary32(h / 6f);
            int slices = Math.Abs(slicesFloat) < 2147483648f ? (int)slicesFloat : int.MinValue;
            slices = slices < 10 ? slices : 10;
            slices = slices > 2 ? slices : 2;
            float step = OriginalMath.Binary32(h / OriginalMath.Binary32((float)slices)), stripHeight = OriginalMath.Binary32(step + .6f), bevelBottom = OriginalMath.Binary32(end - n), bevelTop = OriginalMath.Binary32(n + y);
            for (int i = 0; i != slices; i++)
            {
                float stripY = OriginalMath.Binary32(OriginalMath.Binary32(step * OriginalMath.Binary32((float)i)) + y);
                float inset = bevelTop > stripY ? OriginalMath.Binary32(n - (OriginalMath.Binary32(stripY - y))) : OriginalMath.Binary32(step + stripY) > bevelBottom ? OriginalMath.Binary32(n - (OriginalMath.Binary32(end - (OriginalMath.Binary32(step + stripY))))) : 0f;
                inset = OriginalMath.WasmMax(inset > n ? n : inset, 0f);
                Color color = A(Mix(top, bottom, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)i) + .5f)) / OriginalMath.Binary32((float)slices))), alpha);
                s.Rect(OriginalMath.Binary32(inset + x), stripY, OriginalMath.Binary32(w - (OriginalMath.Binary32(inset + inset))), stripHeight, color);
            }
            Color line = A(edge, alpha);
            s.Rect(left, y, middle, 2f, line);
            s.Rect(left, OriginalMath.Binary32(end + -2f), middle, 2f, line);
            s.Rect(x, bevelTop, 2f, OriginalMath.Binary32(h - twice), line);
            float right = OriginalMath.Binary32(x + w);
            s.Rect(OriginalMath.Binary32(right + -2f), bevelTop, 2f, OriginalMath.Binary32(h - twice), line);
            if (n >= 2f)
            {
                float lx = OriginalMath.Binary32(x + 1f), by = OriginalMath.Binary32(end + -1f), ty = OriginalMath.Binary32(y + 1f);
                s.Segment(new Vector2(lx, bevelTop), new Vector2(left, ty), 2f, line);
                s.Segment(new Vector2(lx, bevelBottom), new Vector2(left, by), 2f, line);
                float rx = OriginalMath.Binary32(right + -1f), innerRight = OriginalMath.Binary32(right - n);
                s.Segment(new Vector2(rx, bevelTop), new Vector2(innerRight, ty), 2f, line);
                s.Segment(new Vector2(rx, bevelBottom), new Vector2(innerRight, by), 2f, line);
            }
            s.Rect(left, OriginalMath.Binary32(y + 2f), middle, 1f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .16f)));
        }
        // f54523.
        private void Card(PixelSurface s, float x, float y, float w, float h, Color edge, float alpha = 1f)
            => Plate(s, x, y, w, h, uiPanelHi, uiPanel, edge, 7f, alpha);
        // f54518. Restore mapping before registering the full-screen blocker.
        internal void Backdrop(PixelSurface s, float alpha = .78f)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            s.Rect(OriginalMath.Binary32(ScreenLayout.Left + -2f), -2f, OriginalMath.Binary32(ScreenLayout.Width + 4f), OriginalMath.Binary32(ScreenLayout.Height + 4f), new Color(.005f, .012f, .03f, alpha));
            s.MapLayout = mapped;
            HitBlock(s);
        }
        // f54516.
        private void Disc(PixelSurface s, float cx, float cy, float r, Color c, float step = 2f)
        {
            float height = OriginalMath.Binary32(step + .3f), half = OriginalMath.Binary32(step * .5f), rr = OriginalMath.Binary32(r * r);
            for (float y = -r; y < r; y  = OriginalMath.Binary32(y+(step)))
            {
                float v = OriginalMath.Binary32(half + y);
                float width = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.WasmMax(OriginalMath.Binary32(rr - OriginalMath.Binary32(v * v)), 0f)));
                if (width > 0f) s.Rect(OriginalMath.Binary32(cx - width), OriginalMath.Binary32(y + cy), OriginalMath.Binary32(width + width), height, c);
            }
        }
    }
}
