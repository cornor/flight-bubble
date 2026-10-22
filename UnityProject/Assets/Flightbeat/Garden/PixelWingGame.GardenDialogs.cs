#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-21 20:51 (user: the 连胜中断 dialog and the other dialogs should match the restyled pause / settings).
    // The overlay dialogs drawn here (连胜中断, revive, 体力) use the same parts as the prefab dialogs: deep indigo shade
    // (as the victory page), the white card panel (ui4/panel-card), a navy rounded title with a fine white edge, plain
    // navy body text, the green jelly button for the main choice and a quiet blue-grey text link for the other one.
    // Rigid layout centred like the prefab dialogs (no band mapping), so the panel keeps its shape on tall screens.
    public partial class PixelWingGame
    {
        static readonly Color DialogShade = new Color(.055f, .078f, .2f, .8f);
        static readonly Color DialogTitleInk = Hex(0x163E6D), DialogLinkInk = Hex(0x496384), DialogSoftInk = Hex(0x56698A);

        // Shade, card and title. Leaves MapLayout off and returns the vertical offset of the 960-high frame; callers
        // restore MapLayout when done. `y` / `height` are in the 540 x 960 frame.
        float GardenModal(PixelSurface s, string title, float y, float height,
            float titleOffset = 38f, float titleSize = GardenTitleArt.Size, float shadeAlpha = .8f)
        {
            HitBlock(s);
            s.MapLayout = false;
            float oy = (ScreenLayout.Height - 960f) * .5f;
            Color shade = DialogShade; shade.a = shadeAlpha;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, shade);
            // 2026-10-22: revive uses the same border and background as all other dialogs.
            if (!Asset9(s, "plants/ui/panel-card", 50f, oy + y, 440f, height, 36f, 24f, Color.white))
                GardenPanel(s, 50f, oy + y, 440f, height);
            GardenTitleArt.Draw(s, title, 270f, oy + y + titleOffset, titleSize);
            return oy;
        }

        // The victory / pause green jelly button (3-sliced), rounded white word with a deep green edge; `ad` puts the
        // video mark before the word. Coordinates are already rigid (MapLayout off).
        void DialogGreenButton(PixelSurface s, int id, float x, float y, float w, float h, string word, bool ad, float outlineWidth = 3f)
        {
            HitBox(s, id, x, y, w, h);
            float k = 1f - PressPulse(id) * .06f, bw = w * k, bh = h * k, bx = x + (w - bw) * .5f, by = y + (h - bh) * .5f;
            float size = Mathf.Min(36f, h * .43f) * k, centre = by + bh * .46f;   // 10-22 11:27: thinner base, bigger words
            if (!Asset9(s, "plants/win/button-next", bx, by, bw, bh, 64f, 0f, bh * 64f / 126f, 0f, Color.white))
            {
                GardenButton(s, id, x, y, w, h, "");
                centre = y + h * .5f;
            }
            float mark = ad ? size * 1.08f : 0f, gap = ad ? size * .4f : 0f, tw = GardenBarArt.TextWidth(word, size);
            float left = x + w * .5f - (mark + gap + tw) * .5f;
            if (ad && !GardenBars.Icon(s, "ad", left + mark * .5f, centre, mark))
                GardenBarArt.TextOutline(s, "▶", left + mark * .5f, centre - mark * .55f, mark, Hex(0xFFE27A), Hex(0x1B7A2A), 2f);
            GardenBarArt.TextOutline(s, word, left + mark + gap + tw * .5f, centre - size * .55f, size, Color.white, Hex(0x1B7A2A), outlineWidth * k);
        }

        // Small links use the same single-pass plain atlas as the codex descriptions, with a generous hit area.
        void DialogLink(PixelSurface s, int id, float cx, float y, string word, float size = 18f, Color? inkOverride = null)
        {
            float tw = GardenSkin.TextWidth(word, size), press = PressPulse(id);
            HitBox(s, id, cx - tw * .5f - 24f, y - 10f, tw + 48f, size + 20f);
            Color ink = inkOverride ?? Color.Lerp(DialogLinkInk, DialogTitleInk, press);
            GardenSkin.Text(s, word, cx, y, size, true, ink);
            s.Rect(cx - tw * .5f, y + size * 1.12f, tw, 1.2f, new Color(ink.r, ink.g, ink.b, .55f));
        }

        // 21:07 (user: some words were not crisp): small body text gets a second pass .6 px to the right, like the key
        // words in the dialogue bubble, so the thin strokes of the rounded type read solid on the white card.
        void DialogText(PixelSurface s, string text, float cx, float y, float size, Color? ink = null)
        {
            Color c = ink ?? GardenBars.Navy;
            GardenBarArt.Text(s, text, cx, y, size, true, c);
            if (size <= 20f) GardenBarArt.Text(s, text, cx + .6f, y, size, true, c);
        }

        // ---- 连胜中断 (ids 511 keep going / 510 break the streak)
        void DrawGardenStreakPrompt(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            try
            {
                float oy = GardenModal(s, "连胜中断", 300f, 340f);
                if (!GardenBars.Icon(s, "broken-link", 270f, oy + 400f, 46f)) DialogText(s, "!", 270f, oy + 382f, 30f, dialogueKey);
                GardenSkin.Text(s, streakWhat + "将中断连胜", 270f, oy + 432f, 19f, true, GardenBars.Navy);
                if (Economy != null && Economy.Streak > 0) GardenSkin.Text(s, "当前 " + Economy.Streak + " 连胜", 270f, oy + 460f, 17f, true, DialogSoftInk);
                DialogGreenButton(s, 511, 140f, oy + 494f, 260f, 84f, streakRevive ? "复活继续" : "继续挑战", streakRevive);
                DialogLink(s, 510, 270f, oy + 596f, "确认中断连胜");
            }
            finally { s.MapLayout = mapped; }
        }

        // ---- 体力 (ids 501 ad / 502 coins / 503 close); only reachable when energy runs out
        void DrawGardenEnergy(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            try
            {
                float oy = GardenModal(s, "体力", 300f, 340f);
                DialogText(s, Economy.Energy + " / 5", 270f, oy + 380f, 26f);
                DialogGreenButton(s, 501, 120f, oy + 430f, 300f, 84f, "补充体力 +5", true);
                DialogLink(s, 502, 270f, oy + 536f, "400 金币补充体力 +5");
                DialogLink(s, 503, 270f, oy + 584f, "关闭");
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif
