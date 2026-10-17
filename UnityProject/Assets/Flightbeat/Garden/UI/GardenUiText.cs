#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Garden.UI
{
    // One editable RectTransform per label; reuse the shipped garden glyph atlas.
    [AddComponentMenu("Flightbeat/Garden UI/Text")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GardenUiText : MaskableGraphic
    {
        [TextArea] public string text = "";
        [TextArea] public string alternateText = "";
        [Min(1)] public float fontSize = 18;
        public bool centered = true;
        // 2026-10-16: centre the ink of the string vertically in the rect (default: the em box hangs from the top).
        [System.NonSerialized] public bool middle;
        [Min(0)] public float outlineWidth;
        public Color outlineColor = new Color32(35, 64, 107, 255);   // v3 navy outline (UI spec v3 4.5)
        [SerializeField] Texture2D fontAtlas;
        static Dictionary<char, GardenSkin.Glyph> glyphs;
        bool alternate;
        object[] arguments;
        string rendered;
        public override Texture mainTexture => fontAtlas ? fontAtlas : Resources.Load<Texture2D>("GardenR1/atlas");
        public void SetFontAtlas(Texture2D value) { fontAtlas = value; SetAllDirty(); }
        // Typed overloads are called every LateUpdate by RefreshGardenUi: they return before
        // boxing or string.Format when the bound value is unchanged, so steady frames allocate nothing.
        int boundKind = -1, boundA, boundB; string boundS;
        bool Unchanged(int kind, bool useAlternate) => rendered != null && boundKind == kind && alternate == useAlternate;
        public void Present(bool useAlternate)
        {
            if (Unchanged(0, useAlternate)) return;
            boundKind = 0; alternate = useAlternate; arguments = null; Refresh();
        }
        public void Present(bool useAlternate, int a)
        {
            if (Unchanged(1, useAlternate) && boundA == a) return;
            boundKind = 1; boundA = a; alternate = useAlternate; arguments = new object[] { a }; Refresh();
        }
        public void Present(bool useAlternate, int a, int b)
        {
            if (Unchanged(2, useAlternate) && boundA == a && boundB == b) return;
            boundKind = 2; boundA = a; boundB = b; alternate = useAlternate; arguments = new object[] { a, b }; Refresh();
        }
        public void Present(bool useAlternate, string a)
        {
            if (Unchanged(3, useAlternate) && boundS == a) return;
            boundKind = 3; boundS = a; alternate = useAlternate; arguments = new object[] { a }; Refresh();
        }
        public void Present(bool useAlternate, params object[] values)
        {
            boundKind = -1;
            alternate = useAlternate;
            arguments = values;
            Refresh();
        }
        void Refresh()
        {
            string template = alternate && alternateText.Length > 0 ? alternateText : text;
            string next = arguments != null && arguments.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, template, arguments) : template;
            if (rendered == next) return;
            rendered = next;
            SetVerticesDirty();
        }
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; Refresh(); }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); rendered = null; boundKind = -1; Refresh(); }
#endif
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var data = GardenSkin.Data;
            if (glyphs == null)
            {
                glyphs = new Dictionary<char, GardenSkin.Glyph>();
                foreach (var glyph in data.glyphs) glyphs[(char)glyph.code] = glyph;
            }
            string value = rendered ?? text;
            float width = 0;
            foreach (char c in value) width += glyphs.TryGetValue(c, out var g) ? g.advance * fontSize : fontSize;
            Rect r = rectTransform.rect;
            float x = centered ? r.center.x - width * .5f : r.xMin;
            float top = r.yMax;
            if (middle)
            {
                // Ink extent with the top at 0 (same quad maths as Draw), then shift so its middle meets the rect's.
                float inkTop = float.MinValue, inkBottom = float.MaxValue;
                foreach (char c in value)
                    if (glyphs.TryGetValue(c, out var g) && g.w > 0 && g.h > 0)
                    {
                        float y = -(.86f + g.bearingY) * fontSize;
                        inkTop = Mathf.Max(inkTop, y); inkBottom = Mathf.Min(inkBottom, y - g.h / data.fontSize * fontSize);
                    }
                if (inkTop > inkBottom) top = r.center.y - (inkTop + inkBottom) * .5f;
            }
            if (outlineWidth > 0)
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Draw(vh, value, x + Mathf.Cos(a) * outlineWidth, top - Mathf.Sin(a) * outlineWidth, outlineColor, data);
                }
            Draw(vh, value, x, top, color, data);
        }
        void Draw(VertexHelper vh, string value, float x, float top, Color tint, GardenSkin.Atlas data)
        {
            foreach (char c in value)
            {
                if (!glyphs.TryGetValue(c, out var g)) { x += fontSize; continue; }
                if (g.w > 0 && g.h > 0)
                {
                    float left = x + g.bearingX * fontSize;
                    float y = top - (.86f + g.bearingY) * fontSize;
                    float right = left + g.w / data.fontSize * fontSize, bottom = y - g.h / data.fontSize * fontSize;
                    float u = g.x / data.size, v = 1 - (g.y + g.h) / data.size;
                    float u1 = (g.x + g.w) / data.size, v1 = 1 - g.y / data.size;
                    int n = vh.currentVertCount;
                    vh.AddVert(new Vector3(left, y), tint, new Vector2(u, v1));
                    vh.AddVert(new Vector3(right, y), tint, new Vector2(u1, v1));
                    vh.AddVert(new Vector3(right, bottom), tint, new Vector2(u1, v));
                    vh.AddVert(new Vector3(left, bottom), tint, new Vector2(u, v));
                    vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
                }
                x += g.advance * fontSize;
            }
        }
    }
}
#endif
