#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-29 (界面优化 7/14/17, user: 所有的结算都要统一): the victory, revive and failure pages share one look -
    // a full-screen night-blue shade (no card), a big white headline with a navy edge and a short line under it, the
    // hero in the middle, and capsule buttons from the light theme: blue (ui/button-blue), yellow (ui/button-yellow),
    // white (the white pill, scene/level). Coordinates here are rigid screen units (MapLayout off), 540 wide, with
    // the 960 core centred (oy).
    public partial class PixelWingGame
    {
        static readonly Color ResultNavy = Hex(0x1D3A7A), ResultGold = Hex(0xFFD45C), ResultSub = Hex(0xDCE8FF);

        // Headline: white rounded letters with a navy edge and a soft dark glow, popping in over .35 s.
        void ResultTitle(PixelSurface s, string text, float cy, float size, float t, float a, Color? edge = null)
        {
            float p = OutBack(Clamp01(t / .35f), 2.2f);
            if (p <= .01f) return;
            float sz = size * p;
            sz = Mathf.Min(sz, 500f / Mathf.Max(.01f, GardenTitleArt.TextWidth(text, 1f)));
            s.Glow(270f, cy, GardenTitleArt.TextWidth(text, sz) + 120f, sz + 50f, new Color(.02f, .05f, .16f, .28f * a), 0f);
            GardenTitleArt.TextOutline(s, text, 270f, cy - sz * .55f, sz, A(Color.white, a), A(edge ?? ResultNavy, a), sz * .042f);
        }
        void ResultSubtitle(PixelSurface s, string text, float cy, float size, Color ink, float a)
        {
            if (a <= .01f) return;
            GardenBarArt.Text(s, text, 270f, cy - size * .55f, size, true, A(ink, a));
        }
        // A light-theme capsule button drawn on the surface (the revive page; win / lose use the prefab ones): the art
        // 3-sliced with fully round ends, an optional icon slot before the word.
        // 2026-10-30 (调整, user: 按钮文字照参考图): white bold words with a soft shadow, no edge; navy-grey on white.
        void ResultPill(PixelSurface s, int id, float x, float y, float w, float h, string kind, string word, string icon = null, Color? tint = null)
        {
            HitBox(s, id, x, y, w, h);
            float k = 1f - PressPulse(id) * .05f, bw = w * k, bh = h * k, bx = x + (w - bw) * .5f, by = y + (h - bh) * .5f;
            string slot = kind == "white" ? (Has("plants/uiref/button-white") ? "plants/uiref/button-white" : "scene/level")
                : kind == "yellow" ? "plants/uiref/button-yellow"
                : kind == "red" ? (Has("plants/uiref/button-red") ? "plants/uiref/button-red" : "plants/uiref/button-yellow") : "plants/uiref/button-blue";
            if (kind == "red" && slot.EndsWith("button-red")) tint = null;   // the painted red needs no tint
            float stored = slot == "scene/level" ? 36f : slot.EndsWith("button-white") ? 30f : 52f;   // trimmed round ends
            Color face = tint ?? Color.white;
            if (!Asset9(s, slot, bx, by, bw, bh, stored, 0f, bh * .5f, 0f, face))
                WinRoundRect(s, bx, by, bw, bh, bh * .5f, kind == "white" ? Color.white : Hex(0x2F7BFF));
            bool light = kind == "white";
            Color ink = light ? Hex(0x3A4A6B) : Color.white;
            float size = Mathf.Min(30f, bh * .38f), cy = by + bh * (light ? .5f : .46f);
            float mark = icon != null ? size * 1.15f : 0f, gap = icon != null ? size * .3f : 0f, tw = GardenBarArt.TextWidth(word, size);
            float left = x + (w - mark - gap - tw) * .5f;
            if (icon != null) Asset(s, icon, left, cy - mark * .5f, mark, mark, light ? ink : Color.white);
            ButtonWord(s, word, left + mark + gap, cy, size, ink, !light);
        }
        // The atlas is already bold; one ink pass avoids closing counters with offset overdraw.
        static void ButtonWord(PixelSurface s, string word, float left, float cy, float size, Color ink, bool shadow)
        {
            float y = cy - size * .55f;
            if (shadow)
            {
                Color sh = new Color(0f, .05f, .2f, .22f * ink.a);
                GardenBarArt.Text(s, word, left, y + 1.6f, size, false, sh);
            }
            GardenBarArt.Text(s, word, left, y, size, false, ink);
        }
        // Night-blue stat card with a gold rim: 最高连击 | 通关用时, icon, small label and the big value per half.
        void ResultStats(PixelSurface s, float cy, float a, string combo, string time)
        {
            const float w = 438f, h = 88f, r = 20f;   // 2026-10-30: the reference card
            float x = 270f - w * .5f, y = cy - h * .5f;
            WinRoundRect(s, x - 3f, y - 3f, w + 6f, h + 6f, r + 3f, A(Hex(0xF2C14E), a));
            WinRoundRect(s, x, y, w, h, r, A(Hex(0x152559), a));
            s.Rect(270f, y + 16f, 1.5f, h - 32f, A(new Color(1f, 1f, 1f, .2f), a));
            Half(x + 32f, true, "最高连击", combo);
            Half(270f + 30f, false, "通关用时", time);
            void Half(float left, bool flame, string label, string value)
            {
                if (flame) GardenBars.Icon(s, "flame", left + 20f, cy, 44f, a);
                else if (Has("plants/win/icon-clock")) Asset(s, "plants/win/icon-clock", left, cy - 20f, 40f, 40f, A(Color.white, a));
                GardenBarArt.Text(s, label, left + 54f, cy - 32f, 18f, false, A(ResultSub, a));
                float sv = Mathf.Min(36f, 130f / Mathf.Max(.01f, GardenBarArt.TextWidth(value, 1f)));
                GardenBarArt.Text(s, value, left + 54f, cy - 8f, sv, false, A(ResultGold, a));
            }
        }
        // Small rounded chip (已解锁 / 本关登场 / N 关后加入) under a member.
        void ResultChip(PixelSurface s, string text, float cx, float cy, float a)
        {
            const float size = 17f, h = 30f;
            float w = Mathf.Max(95f, GardenBarArt.TextWidth(text, size) + 26f);
            WinRoundRect(s, cx - w * .5f - 1f, cy - h * .5f - 1f, w + 2f, h + 2f, h * .5f + 1f, A(new Color(1f, 1f, 1f, .22f), a));
            WinRoundRect(s, cx - w * .5f, cy - h * .5f, w, h, h * .5f, A(new Color(.12f, .17f, .36f, .92f), a));
            GardenBarArt.Text(s, text, cx, cy - size * .55f, size, true, A(Color.white, a));
        }
        // Section label with a thin line either side: ——  新伙伴加入  ——
        void ResultSection(PixelSurface s, string text, float cy, float a)
        {
            const float size = 18f;
            float w = GardenBarArt.TextWidth(text, size);
            GardenBarArt.Text(s, text, 270f, cy - size * .55f, size, true, A(ResultSub, a));
            Color line = A(new Color(1f, 1f, 1f, .28f), a);
            s.Rect(270f - w * .5f - 70f, cy, 56f, 1.5f, line);
            s.Rect(270f + w * .5f + 14f, cy, 56f, 1.5f, line);
        }
        // Four-point glints twinkling around a point (the hero).
        void ResultTwinkles(PixelSurface s, float cx, float cy, float rx, float ry, float a)
        {
            for (int i = 0; i < 5; i++)
            {
                float ph = (ui * .8f + i * .29f) % 1f, k = Mathf.Sin(ph * Mathf.PI) * a, r = (i % 2 == 0 ? 11f : 7f) * (.6f + .4f * k);
                float ang = i * 1.37f + 2.2f;
                Vector2 p = new Vector2(cx + Mathf.Cos(ang) * rx, cy + Mathf.Sin(ang) * ry);
                s.Glow(p.x, p.y, r * 2.6f, r * 2.6f, new Color(1f, .95f, .7f, .4f * k), 0f);
                s.Glow(p.x, p.y, r * 4f, r * .55f, new Color(1f, 1f, 1f, k), 0f);
                s.Glow(p.x, p.y, r * .55f, r * 4f, new Color(1f, 1f, 1f, k), 0f);
            }
        }
    }
}
#endif
