#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private void RescueGarden()
        {
            if (Battle.ReviveInSlotOrder()) { rescueAimAt = OriginalMath.Binary32(Battle.Time + 1.6f); reviveSuccessAt = ui; GardenSay("revive-ok"); }
        }
        // 2026-10-30 (界面优化 15, after the reference): for 1.5 s after reviving (the bomb aim starts at 1.6 s), a big
        // 「复活成功！」 over the board with arrows shooting up either side and the black bar 「前排泡泡已清除」, while the
        // light ribbon runs along the bottom of the bubble field (CombatFeedback).
        float reviveSuccessAt = -10f;
        void DrawReviveSuccess(PixelSurface s)
        {
            float t = ui - reviveSuccessAt;
            if (t < 0f || t >= 1.5f || Battle == null || Battle.State != Phase.Playing) return;
            float a = Clamp01(t / .15f) * Clamp01((1.5f - t) / .3f);
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 300f);
            // 2026-10-31 00:27 (user, after the reference): the bar sits on the tray's lower edge (DrawGardenBoard).
            float barY = PlushLayout.Y(562f) + 10f;
            s.MapLayout = false;
            try
            {
                // 2026-10-31 00:36 (user: 上推箭头、标题没有做到位): as in the reference - two soft light-cyan arrows rising
                // from just above the ends of the title (a ribbon that sweeps out and fades towards its foot, a solid head),
                // and the title in chunky white with a thick royal-blue edge and a darker blue 3D underside, gold glints
                // around it and no dark halo.
                const float size = 38f;
                float tw = GardenBarArt.TextWidth("复活成功！", size), rise = 1f - OutCubic(Clamp01(t / .45f));
                float aa = a * Clamp01(t / .2f);
                if (Has("scene/revive-title")) { ReviveSuccessArt(s, cy, t, a, rise, aa); s.MapLayout = false; DarkBar(s, "前排泡泡已清除", barY, t, null, a, 15f); return; }
                for (int side = -1; side <= 1; side += 2)
                    ReviveArrow(s, 270f + side * (tw * .5f - 10f), cy - 122f + rise * 34f + Mathf.Sin(t * 4f + side) * 1.5f, cy - 4f, side, aa);
                float pop = OutBack(Clamp01(t / .35f), 2.2f), sz = size * pop;
                if (pop > .01f)
                {
                    s.Glow(270f, cy, tw * pop + 90f, sz + 46f, new Color(1f, 1f, 1f, .32f * a), 0f);
                    GardenBarArt.TextOutline(s, "复活成功！", 270f, cy - sz * .55f + sz * .09f, sz, A(Hex(0x1743A6), a), A(Hex(0x1743A6), a), sz * .14f);
                    GardenBarArt.TextOutline(s, "复活成功！", 270f, cy - sz * .55f, sz, A(Color.white, a), A(Hex(0x2B6BE8), a), sz * .13f);
                }
                ResultTwinkles(s, 270f, cy + 14f, tw * .5f + 34f, 50f, a);
                ReviveBombLine(s, cy + 86f, t, a);
                s.MapLayout = false;
                DarkBar(s, "前排泡泡已清除", barY, t, null, a, 15f);
            }
            finally { s.MapLayout = mapped; }
        }
        // 2026-10-31 10:53 (user: 交给美术出图): the painted version (Art/PlushToy/ReviveSuccess-20261031) - the wordmark
        // popping in, the two arrows (one art, mirrored) shooting up over its ends with a gentle float, gold sparkles
        // twinkling around it. Screen units (MapLayout off); arrow tip at 78 % / 9 % of its 160x320 canvas.
        void ReviveSuccessArt(PixelSurface s, float cy, float t, float a, float rise, float aa)
        {
            // 11:46 (user picked the preview's top half): the title in the middle (230 wide, as the reference), two bigger
            // arrows (76x152) whose tips stand over its ends, a ring of gold stars round the title and four by each
            // arrow, white specks, and a soft white backlight behind the words - the dark picture bubbles under them
            // had read as a shadow (user: 有黑影的感觉).
            const float titleW = 230f, titleH = titleW * 200f / 720f, arrowW = 76f, arrowH = 152f, reach = 100f;
            float pop = OutBack(Clamp01(t / .35f), 2.2f);
            if (pop > .01f)
            {
                s.Glow(270f, cy, 340f * pop, 150f * pop, new Color(1f, 1f, 1f, .62f * a), 0f);
                s.Glow(270f, cy, 260f * pop, 96f * pop, new Color(1f, .99f, .94f, .55f * a), 0f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float tipX = 270f + side * reach, tipY = cy - 112f + rise * 40f + Mathf.Sin(t * 4f + side) * 2f;
                s.Glow(tipX, tipY + 18f, 70f, 70f, new Color(.67f, .9f, 1f, .47f * aa), 0f);
                float left = side < 0 ? tipX - arrowW * .78f : tipX - arrowW * .22f;
                Asset(s, "scene/revive-arrow", left, tipY - arrowH * .09f, arrowW, arrowH, new Color(1f, 1f, 1f, aa), false, true, side > 0);
                for (int j = 0; j < ArrowStars.Length; j += 3)
                    ReviveStar(s, tipX + side * ArrowStars[j], tipY + ArrowStars[j + 1], ArrowStars[j + 2], t, j * .7f + side, aa);
            }
            if (pop > .01f)
                Asset(s, "scene/revive-title", 270f - titleW * pop * .5f, cy - titleH * pop * .5f, titleW * pop, titleH * pop, new Color(1f, 1f, 1f, a));
            for (int j = 0; j < TitleStars.Length; j += 3)
                ReviveStar(s, 270f + TitleStars[j], cy + TitleStars[j + 1], TitleStars[j + 2], t, j * .41f, a * Clamp01((t - .1f) / .2f));
            ReviveBombLine(s, cy + 86f, t, a);
            for (int i = 0; i < 12; i++)   // tiny white specks drifting round the title
            {
                float ang = i * .53f + .3f + t * .2f, r = 150f + (i % 3) * 22f, k = .5f + .5f * Mathf.Sin(t * 6f + i * 1.3f);
                s.Glow(270f + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r * .45f, 6f, 6f, new Color(1f, 1f, 1f, k * a), 0f);
            }
        }
        // 2026-10-31 12:15 (user: 复活成功后没有弹获得炸弹x1的提示): the revive's bomb (Battle.Revive adds one rescue bomb,
        // aimed automatically at 1.6 s) is announced inside the success show - the rainbow bomb and 「泡泡炸弹 +1」 pop in
        // under the title at .45 s and leave with it, so the automatic aim keeps its original timing.
        void ReviveBombLine(PixelSurface s, float cy, float t, float a)
        {
            float u = t - .45f;
            if (u <= 0f) return;
            float p = OutBack(Clamp01(u / .3f), 2.2f), k = a * Clamp01(u / .12f);
            const string word = "泡泡炸弹 +1";
            float size = 26f * p, icon = 46f * p, tw = GardenBarArt.TextWidth(word, size), left = 270f - (icon + 8f + tw) * .5f;
            float y = cy - Mathf.Clamp01(u / 1f) * 6f;
            s.Glow(270f, y, tw + icon + 90f, 70f, new Color(1f, .95f, .75f, .35f * k), 0f);
            GardenBars.Icon(s, "bomb", left + icon * .5f, y, icon, k);
            GardenBarArt.TextOutline(s, word, left + icon + 8f + tw * .5f, y - size * .55f, size, A(Color.white, k), A(Hex(0x1D3A7A), k), size * .09f);
        }
        // Star spots (dx, dy, size): round the title (centre-relative) and by each arrow tip (dx mirrored per side).
        static readonly float[] TitleStars = { -150f, -6f, 40f, -128f, 34f, 24f, -70f, 48f, 32f, -12f, 52f, 19f, 52f, 50f, 30f,
            118f, 36f, 22f, 150f, -2f, 38f, 140f, -38f, 19f, -140f, -40f, 22f, 0f, -48f, 16f };
        static readonly float[] ArrowStars = { -28f, 8f, 35f, 24f, -6f, 22f, -34f, 62f, 27f, 30f, 48f, 18f };
        // One gold four-point star twinkling on its own beat (size breathing .6-1, brightness with it).
        void ReviveStar(PixelSurface s, float x, float y, float size, float t, float phase, float a)
        {
            if (a <= .01f) return;
            float k = .55f + .45f * Mathf.Sin(t * 5.5f + phase * 2.3f), d = size * (.6f + .4f * k);
            Asset(s, "scene/revive-sparkle", x - d * .5f, y - d * .5f, d, d, new Color(1f, 1f, 1f, a * (.45f + .55f * k)));
        }
        // One rising arrow: 1 px rows (no overlap seams) - a solid light head at `tip`, then a ribbon down to `foot` that
        // widens, sweeps outward (side -1 left, +1 right) and fades out; a white core runs up its middle.
        static void ReviveArrow(PixelSurface s, float x, float tip, float foot, int side, float a)
        {
            if (a <= .01f) return;
            const float headH = 26f, headHalf = 16f;
            float neck = tip + headH;
            s.Glow(x, tip + 12f, 50f, 50f, new Color(.6f, .9f, 1f, .35f * a), 0f);
            for (float y = neck; y < foot; y += 1f)
            {
                float u = (y - neck) / Mathf.Max(1f, foot - neck), cx = x + side * 30f * u * u;
                float half = 5f + 12f * u, k = (1f - u) * (1f - u);
                s.Rect(cx - half, y, half * 2f, 1.05f, new Color(.78f, .93f, 1f, (.2f + .55f * k) * a));
                float core = 1.6f + 1.2f * u;
                s.Rect(cx - core, y, core * 2f, 1.05f, new Color(1f, 1f, 1f, .95f * k * a));
            }
            for (float r = 0f; r < headH; r += 1f)
            {
                float half = headHalf * (r + 1f) / headH;
                s.Rect(x - half, tip + r, half * 2f, 1.05f, new Color(.86f, .97f, 1f, .95f * a));
            }
        }
    }
}
#endif
