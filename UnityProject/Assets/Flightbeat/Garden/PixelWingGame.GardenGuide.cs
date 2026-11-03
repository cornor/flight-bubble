#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // 2026-10-29 21:07 (界面优化 2/3/8, user): the teaching levels just show what to tap - no dialogue card, goal capsule
        // or bottom banner. A white speech bubble over the member to tap, a pointing hand and the gold glow:
        //   level 1, step 0  「点击蓝色队员」
        //            step 3  「再派2个蓝色队员，合成升级！」 -> after one more is sent 「再派1个，合成升级！」; gone once merged
        //   level 2, step 10 「还能吐N次泡泡」 (N = the front member's number)
        // Other steps say nothing; a player who stalls still gets the hand (and glow) on the suggested member.
        bool GuideStep(out string[] lines, out float logicalCy)
        {
            lines = null; logicalCy = 548f;
            if (lesson == null || lesson.Complete) return false;
            int lane = lesson.SuggestedLane;
            if (lane < 0 || Battle.Reserve[lane].Count == 0) return false;
            if (lesson.Chapter == 1 && lesson.Step == 0) { lines = new[] { "点击{蓝色}队员" }; logicalCy = 594f; return true; }
            if (lesson.Chapter == 1 && lesson.Step == 3) {
                int blue = 0;
                foreach (Fighter ship in Battle.Fleet) if (ship.Color == 0 && ship.Tier == 0) blue++;
                lines = blue >= 2 ? new[] { "再派1个，{合成升级！}" } : new[] { "再派2个{蓝色}队员", "{合成升级！}" };
                return true;
            }
            if (lesson.Chapter != 1 && lesson.Step == 10) {
                lines = new[] { "还能吐{" + Battle.Reserve[lane][0].Ammo + "}次泡泡" };
                logicalCy = 594f;   // nobody on the pads yet: sit in the empty pad row, clear of the debut chips
                return true;
            }
            return false;
        }
        // Lane whose front member the guide points at right away (no waiting for HintLevel), or -1.
        int GuideLane => GuideStep(out _, out _) ? lesson.SuggestedLane : -1;

        string guideShownText; float guideShownAt = -10f;
        // 2026-10-30 (调整 2/3/8, user: 选中者整个发光的状态，全屏遮罩都需要): while a guide line shows, a dark full-screen
        // mask (overlay layer, above the header too) with a rounded window over each member card to tap - the suggested
        // one, or every blue front member while 「再派2个」 - and a pulsing gold glow round the window.
        readonly System.Collections.Generic.List<int> guideHoles = new System.Collections.Generic.List<int>();
        void DrawGuideMask(PixelSurface s)
        {
            if (!GuideStep(out _, out _)) return;
            guideHoles.Clear();
            if (lesson.Chapter == 1 && lesson.Step == 3)
            {
                for (int l = 0; l < Battle.LaneCount; l++)
                    if (Battle.Reserve[l].Count > 0 && Battle.Reserve[l][0].Color == 0) guideHoles.Add(l);
            }
            if (guideHoles.Count == 0) guideHoles.Add(lesson.SuggestedLane);
            bool mapped = s.MapLayout; s.MapLayout = false;
            try
            {
                float top = PlushLayout.Y(688f), bottom = PlushLayout.Y(868f), rowH = (bottom - top) / 3f;
                float w = Mathf.Min(108f, 500f / Battle.LaneCount - 8f), y0 = top - 4f, y1 = top + rowH - 1f, r = 14f;
                float L = ScreenLayout.Left, R = ScreenLayout.Left + ScreenLayout.Width, H = ScreenLayout.Height;
                float age = ui - guideShownAt, a = Clamp01(age / .25f);
                Color shade = new Color(.02f, .04f, .12f, .62f * a);
                s.Rect(L, 0f, R - L, y0, shade);
                s.Rect(L, y1, R - L, H - y1, shade);
                for (int row = 0; row < Mathf.CeilToInt(y1 - y0); row++)
                {
                    float y = y0 + row, dyTop = row + .5f, dyBottom = (y1 - y0) - row - .5f, dy = Mathf.Min(dyTop, dyBottom);
                    float inset = dy < r ? r - Mathf.Sqrt(Mathf.Max(0f, r * r - (r - dy) * (r - dy))) : 0f;
                    float x = L;
                    foreach (int lane in guideHoles)
                    {
                        float qx = Battle.QueuePositionX(lane), hx0 = qx - w * .5f - 3f + inset, hx1 = qx + w * .5f + 3f - inset;
                        if (hx0 > x) s.Rect(x, y, hx0 - x, 1f, shade);
                        x = Mathf.Max(x, hx1);
                    }
                    if (R > x) s.Rect(x, y, R - x, 1f, shade);
                }
                float pulse = .6f + .4f * Mathf.Sin(ui * 4f);
                foreach (int lane in guideHoles)
                {
                    float qx = Battle.QueuePositionX(lane), hx0 = qx - w * .5f - 3f, hx1 = qx + w * .5f + 3f;
                    Color gold = new Color(1f, .82f, .3f, a * pulse);
                    s.GlowLine(new Vector2(hx0 + r, y0), new Vector2(hx1 - r, y0), 14f, new Color(1f, .85f, .35f, .45f * a * pulse));
                    s.GlowLine(new Vector2(hx0 + r, y1), new Vector2(hx1 - r, y1), 14f, new Color(1f, .85f, .35f, .45f * a * pulse));
                    s.GlowLine(new Vector2(hx0, y0 + r), new Vector2(hx0, y1 - r), 14f, new Color(1f, .85f, .35f, .45f * a * pulse));
                    s.GlowLine(new Vector2(hx1, y0 + r), new Vector2(hx1, y1 - r), 14f, new Color(1f, .85f, .35f, .45f * a * pulse));
                    s.Rect(hx0 + r, y0, hx1 - hx0 - 2f * r, 2.5f, gold);
                    s.Rect(hx0 + r, y1 - 2.5f, hx1 - hx0 - 2f * r, 2.5f, gold);
                    s.Rect(hx0, y0 + r, 2.5f, y1 - y0 - 2f * r, gold);
                    s.Rect(hx1 - 2.5f, y0 + r, 2.5f, y1 - y0 - 2f * r, gold);
                    for (int c = 0; c < 4; c++)   // rounded gold corners
                    {
                        Vector2 cc = new Vector2(c % 2 == 0 ? hx0 + r : hx1 - r, c < 2 ? y0 + r : y1 - r);
                        s.Ring(cc, r - 1.25f, gold, 2.5f, 8, c == 0 ? 180f : c == 1 ? 270f : c == 2 ? 90f : 0f, 90f);
                    }
                }
            }
            finally { s.MapLayout = mapped; }
        }
        void DrawGardenGuide(PixelSurface s)
        {
            if (lesson == null || lesson.Complete) return;
            int lane = lesson.SuggestedLane;
            bool talking = GuideStep(out string[] lines, out float logicalCy);
            if (lane < 0 || !talking && lesson.HintLevel < 3) { guideShownText = null; return; }
            float qx = Battle.QueuePositionX(lane);
            // The hand taps the front member (from lower right, like the finale guide's), in logical coordinates.
            float headY = QueueDisplayY(725f) - 30f;   // on the member's body, above its number pill
            if (!GuideHand(s, new Vector2(qx + 4f, headY + 16f)))
                DrawTapPointer(s, new Vector2(qx, headY + 14f), new Vector2(qx + 14f, headY + 13f), -42f, -1f);
            if (!talking) { guideShownText = null; return; }
            string key = string.Join("\n", lines);
            if (key != guideShownText) { guideShownText = key; guideShownAt = ui; }
            GuideBubble(s, lines, qx, logicalCy + Mathf.Sin(ui * 2.2f) * 2f, ui - guideShownAt);
        }

        // The painted glove (N9, 160 px canvas, fingertip at (36, 40)) tapping `tip` (logical): it reaches in from the lower
        // right over .35 s, presses (a little smaller) with a ripple ring, and eases back; a 1.1 s loop like the old pointer.
        bool GuideHand(PixelSurface s, Vector2 tip)
        {
            if (!GardenSkin.Has("plants/uiref/guide-hand")) return false;
            float phase = ui % 1.1f / 1.1f;
            float reach = phase < .32f ? Mathf.SmoothStep(0f, 1f, phase / .32f) : phase < .46f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (phase - .46f) / .54f);
            bool pressed = phase >= .3f && phase < .46f;
            bool mapped = s.MapLayout;
            Vector2 at = new Vector2(tip.x, GardenBars.ScreenY(s, tip.y)); s.MapLayout = false;
            try
            {
                if (phase >= .32f)
                {
                    float ring = (phase - .32f) / .68f, fade = 1f - ring;
                    s.Ring(at, 8f + ring * 26f, new Color(1f, .85f, .35f, fade * .9f), 1f + fade * 2.5f, 32, 0f, 360f);
                    s.Ring(at, 5f + ring * 15f, new Color(1f, 1f, 1f, fade * .7f), 2f, 24, 0f, 360f);
                }
                float size = pressed ? 66f : 72f, k = size / 160f, back = (1f - reach) * 14f;
                Vector2 tipNow = at + new Vector2(back, back);
                GardenSkin.Asset(s, "plants/uiref/guide-hand", tipNow.x - 36f * k, tipNow.y - 40f * k, size, size, Color.white);
            }
            finally { s.MapLayout = mapped; }
            return true;
        }

        static readonly Color guideInk = GardenSkin.Hex(0x1D3A7A), guideEdge = GardenSkin.Hex(0xC9D8F0);
        // White speech bubble centred near `targetX` (clamped to the screen), its tail pointing down at targetX.
        // Lines use {key words} like the old dialogue: blue for 蓝..., orange otherwise. Pops in over .18 s.
        void GuideBubble(PixelSurface s, string[] lines, float targetX, float logicalCy, float age)
        {
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, logicalCy); s.MapLayout = false;
            try
            {
                const float size = 24f, lineGap = 32f, padX = 30f, padY = 16f, tail = 14f;
                float textW = 0f;
                foreach (string line in lines) textW = Mathf.Max(textW, GardenBarArt.TextWidth(line.Replace("{", "").Replace("}", ""), size));
                float pop = Mathf.Clamp01(age / .18f), scale = .85f + .15f * (1f + 2.70158f * Mathf.Pow(pop - 1f, 3f) + 1.70158f * Mathf.Pow(pop - 1f, 2f));
                float alpha = Mathf.Clamp01(age / .12f);
                float w = (textW + padX * 2f) * scale, h = (lines.Length * lineGap + padY * 2f - (lineGap - size)) * scale;
                float cx = Mathf.Clamp(targetX, 12f + w * .5f, 528f - w * .5f), x = cx - w * .5f, y = cy - h * .5f;
                Color A(Color c, float k = 1f) { c.a *= alpha * k; return c; }
                // Soft shadow, edge, fill.
                s.GardenRoundedRect(x + 2f, y + 5f, w, h, 20f * scale, A(new Color(.1f, .18f, .4f, .16f)), A(new Color(.1f, .18f, .4f, .16f)));
                s.GardenRoundedRect(x - 2f, y - 2f, w + 4f, h + 4f, 22f * scale, A(guideEdge), A(guideEdge));
                s.GardenRoundedRect(x, y, w, h, 20f * scale, A(Color.white), A(GardenSkin.Hex(0xF4F8FF)));
                // Tail: 1 px rows narrowing to the target, edge first then the white body.
                float tx = Mathf.Clamp(targetX, x + 26f, x + w - 26f), base0 = y + h - 1f;
                for (int r = 0; r < tail * scale; r++)
                {
                    float k = 1f - r / (tail * scale), half = 11f * scale * k, lean = (targetX - tx) * .25f * (r / (tail * scale));
                    s.GardenRoundedRect(tx + lean - half - 2f, base0 + r, half * 2f + 4f, 1.2f, 0f, A(guideEdge), A(guideEdge));
                    s.GardenRoundedRect(tx + lean - half, base0 + r - 1f, half * 2f, 1.2f, 0f, A(GardenSkin.Hex(0xF4F8FF)), A(GardenSkin.Hex(0xF4F8FF)));
                }
                float first = cy - (lines.Length - 1) * lineGap * scale * .5f;
                for (int i = 0; i < lines.Length; i++) GuideLine(s, lines[i], cx, first + i * lineGap * scale, size * scale, alpha);
            }
            finally { s.MapLayout = mapped; }
        }
        // One centred line in the rounded bar font (already bold; one ink pass); "{...}" key words coloured.
        void GuideLine(PixelSurface s, string line, float cx, float centreY, float size, float alpha)
        {
            string plain = line.Replace("{", "").Replace("}", "");
            float x = cx - GardenBarArt.TextWidth(plain, size) * .5f, y = centreY - size * .55f;
            bool key = false; int start = 0;
            for (int i = 0; i <= line.Length; i++)
            {
                if (i < line.Length && line[i] != '{' && line[i] != '}') continue;
                if (i > start)
                {
                    string part = line.Substring(start, i - start);
                    Color ink = key ? (part.Contains("蓝") ? dialogueBlue : dialogueKey) : guideInk; ink.a *= alpha;
                    GardenBarArt.Text(s, part, x, y, size, false, ink);
                    x += GardenBarArt.TextWidth(part, size);
                }
                if (i < line.Length) key = line[i] == '{';
                start = i + 1;
            }
        }

        // 2026-10-29 21:07 (界面优化 12/13/16, user): one style for the short prompts - a long capsule of 80 % black with
        // bold white words (and an optional white icon at the left). Logical centre y; rigid on tall screens.
        // Pops in over .15 s (`age` seconds since it appeared), `alpha` fades it out.
        void DarkBar(PixelSurface s, string text, float logicalCy, float age = 1f, string icon = null, float alpha = 1f, float size = 20f)
        {
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, logicalCy); s.MapLayout = false;
            try
            {
                float pop = Mathf.Clamp01(age / .15f), k = .9f + .1f * pop;
                alpha *= Mathf.Clamp01(age / .1f);
                bool hasIcon = icon != null && GardenBars.HasIcon(icon);
                float textW = GardenBarArt.TextWidth(text, size), iconW = hasIcon ? size * 1.3f + 8f : 0f;
                float h = (size + 22f) * k, w = Mathf.Min(520f, Mathf.Max(240f, textW + iconW + 56f)) * k;
                s.GardenRoundedRect(270f - w * .5f, cy - h * .5f, w, h, h * .5f, new Color(0f, 0f, 0f, .8f * alpha), new Color(0f, 0f, 0f, .8f * alpha));
                float left = 270f - (textW + iconW) * k * .5f;
                if (hasIcon) GardenBars.Icon(s, icon, left + size * .65f * k, cy, size * 1.3f * k, alpha);
                Color white = new Color(1f, 1f, 1f, alpha);
                float tx = left + iconW * k + textW * k * .5f, ty = cy - size * k * .55f;
                GardenBarArt.Text(s, text, tx, ty, size * k, true, white);
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif
