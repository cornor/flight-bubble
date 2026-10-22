#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-17 (user: the revive dialog and the fail page were mostly empty). Both now show a worried Xiao Lan,
    // how far the player got (bar + count), and something to hold on to: on the revive dialog what reviving gives,
    // on the fail page a line that depends on the progress and one tip that depends on why the run was lost.
    public partial class PixelWingGame
    {
        float loseShownAt = -1f;
        Flightbeat.Model.Battle loseBattle;
        string[] loseTip = new string[0];

        // 2026-10-21 21:07 (user: the revive dialog still had the old wooden-groove bar). The battle HUD's glass bar
        // (GardenBattleUiAssets.Header): glass capsule frame, pale frosted groove with a fine shade on its top edge, the
        // cyan-blue glass fill and a soft pulsing spark on its head; proportions of the HUD's 330 x 38 frame.
        void BubbleProgressBar(PixelSurface s, float cx, float y, float w, float p, float alpha, bool spark = true)
        {
            // 21:19 (user: frame + groove, two layers, looked heavy): one frosted groove (a little deeper than the HUD's
            // so it holds on the white card) with the cyan-blue fill inset 3 px; a soft shadow under it. y = top edge.
            float k = w / 330f, h = 22f * k, x = cx - w * .5f, inset = 3f * k, fh = h - inset * 2f;
            p = Mathf.Clamp01(p);
            s.Glow(cx, y + h * .75f, w * 1.02f, h * .9f, new Color(.09f, .2f, .38f, .14f * alpha), 0f);
            if (!GardenBarArt.CapsuleTinted(s, x, y, w, h, new Color(.74f, .86f, .97f, alpha)))
            {
                s.Rect(x, y, w, h, A(Hex(0xBFE0F6), alpha));
                s.Rect(x + inset, y + inset, (w - inset * 2f) * p, fh, A(Hex(0x40C7FF), alpha));
                return;
            }
            if (p > .005f)
            {
                float fw = Mathf.Max((w - inset * 2f) * p, fh * 1.2f);
                GardenBarArt.CapsuleTinted(s, x + inset, y + inset, fw, fh, new Color(.25f, .78f, 1f, alpha));
                if (spark && p < .995f) s.Glow(x + inset + fw - fh * .3f, y + h * .5f, 26f * k, 28f * k, new Color(1f, 1f, 1f, (.55f + .3f * Mathf.Sin(ui * 5f)) * alpha), 0f);
            }
        }

        // ---- revive offer (pixel overlay, map layout as before; buttons keep ids 30 / 31)
        void DrawGardenRevive(PixelSurface s)
        {
            // 2026-10-22: approved mockup. One grouped benefit panel, orange quantities and the shared new icons.
            // Fixed 440 x 560 card, centred on every aspect ratio; the original revive/decline hit ids are retained.
            bool mapped = s.MapLayout;
            try
            {
                float oy = GardenModal(s, "泡泡碰到护栏啦", 200f, 560f, 56f, 34f, .68f);
                float bob = Mathf.Sin(ui * 2.6f) * 3f;
                Asset(s, "ui/guide-worried", 195f, oy + 280f + bob, 150f, 150f, Color.white);
                BubbleProgressBar(s, 270f, oy + 434f, 368f, Battle.Progress, 1f, false);
                GardenBarArt.TextOutline(s, Mathf.FloorToInt(Battle.Progress * 100f) + "%", 270f, oy + 446.3f - 20f * .55f, 20f,
                    GardenBars.Navy, Color.white, 1.1f);

                // All benefits share one soft panel. No glow behind text; both rows share an optical baseline.
                WinRoundRect(s, 72f, oy + 484f, 396f, 132f, 20f, Hex(0xD9EFFC));
                Color line = A(Hex(0x7DBEEA), .52f);
                s.Rect(143f, oy + 505f, 77f, 1.2f, line);
                s.Rect(320f, oy + 505f, 77f, 1.2f, line);
                DialogText(s, "复活后", 270f, oy + 494f, 21f, DialogSoftInk);
                s.Rect(278f, oy + 532f, 1f, 40f, A(line, .65f));
                Asset(s, "scene/plot", 96f, oy + 535f, 68f, 36f, Color.white);
                if (!GardenBars.Icon(s, "bomb", 326f, oy + 550f, 56f))
                    SoapBubble(s, new Vector2(326f, oy + 550f), 45f, Hex(0xD9C2FF), 1f, 3);
                ReviveRewardText(s, "出战位", 174f, oy + 550f);
                ReviveRewardText(s, "炸弹", 365f, oy + 550f);
                GardenSkin.Text(s, "还会清掉最下排，把泡泡推回去", 270f, oy + 586f, 17f, true, DialogSoftInk);
                DialogGreenButton(s, 30, 118f, oy + 624f, 304f, 74f, "复活", true, 2f);
                DialogLink(s, 31, 270f, oy + 710f, "放弃", 22f, Hex(0x555B66));
            }
            finally { s.MapLayout = mapped; }
        }

        static void ReviveRewardText(PixelSurface s, string label, float x, float cy)
        {
            const float size = 18f, quantitySize = 23f;
            GardenBarArt.Text(s, label, x, cy - size * .55f, size, false, GardenBars.Navy);
            GardenBarArt.Text(s, "+1", x + GardenBarArt.TextWidth(label, size) + 3f, cy - quantitySize * .55f,
                quantitySize, false, Hex(0xED790B));
        }

        // Fail page: the prefab keeps its existing card and 失败了 title; this overlay draws the content.
        void DrawLoseScreen(PixelSurface s)
        {
            if (Battle == null || Battle.State != Phase.Lost || !(feedback.EndAge > .48f)) { loseShownAt = -1f; return; }
            if (loseBattle != Battle || loseShownAt < 0f) { loseBattle = Battle; loseShownAt = ui; loseTip = LoseTip(); }
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float oy = (ScreenLayout.Height - 960f) * .5f, t = ui - loseShownAt, fade = Clamp01(t / .3f);
            float p = Battle.Progress, shown = p * Mathf.SmoothStep(0f, 1f, Clamp01((t - .2f) / .8f));
            // 2026-10-22: hero and encouragement, then progress and a compact two-line tip. Keep the shared card.
            float pop = OutBack(Clamp01(t / .35f), 2f), bob = Mathf.Sin(ui * 2.2f) * 3f;
            Asset(s, "ui/guide-worried", 270f - 70f * pop, oy + 250f + 70f * (1f - pop) + bob, 140f * pop, 140f * pop, new Color(1f, 1f, 1f, fade));
            string cheer = p >= .7f ? "就差一点点！" : p >= .5f ? "已经打掉一大半啦！" : p >= .3f ? "打得不错，再来一次！" : "换个顺序，再试一次";
            float c = Clamp01((t - .4f) / .3f);
            GardenBarArt.TextOutline(s, cheer, 270f, oy + 405f, 24f, A(Hex(0xDF791B), c), A(Color.white, c), 1.1f);
            // No level-number line; the animated percentage stays inside the progress bar.
            const float progressWidth = 352f, progressSize = 18f;
            float progressY = oy + 454f, progressCentre = progressY + 22f * (progressWidth / 330f) * .5f;
            BubbleProgressBar(s, 270f, progressY, progressWidth, shown, fade, false);
            GardenBarArt.TextOutline(s, Mathf.FloorToInt(shown * 100f) + "%", 270f, progressCentre - progressSize * .55f,
                progressSize, A(GardenBars.Navy, fade), A(Color.white, fade), 1.1f);
            float tipA = Clamp01((t - .7f) / .3f);
            WinRoundRect(s, 82f, oy + 514f, 376f, 100f, 18f, A(Hex(0xDFEEF8), tipA));
            GardenSkin.Text(s, "下次试试", 270f, oy + 527f, 17f, true, A(DialogSoftInk, tipA));
            for (int i = 0; i < loseTip.Length; i++)
            {
                float size = Mathf.Min(i == 0 ? 19f : 17f, 336f / Mathf.Max(1f, GardenSkin.TextWidth(loseTip[i], 1f)));
                GardenSkin.Text(s, loseTip[i], 270f, oy + 553f + i * 28f, size, true, A(i == 0 ? GardenBars.Navy : DialogSoftInk, tipA));
            }
            s.MapLayout = mapped;
        }

        // Why the run was lost, read once when the fail page opens.
        string[] LoseTip()
        {
            int bombs = Economy != null ? Economy.Data.items[2] : 0, designate = Economy != null ? Economy.Data.items[1] : 0;
            if (Battle.Stalled || Battle.Fleet.Count >= Battle.UnlockedSlots)
                return new[] { "留一个空位，更好合成", "同色同级的三个伙伴可以合成" };
            if (bombs > 0)
                return new[] { "泡泡压下来前，先用炸弹清场", "炸弹还剩 " + bombs + " 个，下次记得早点用" };
            if (designate > 0)
                return new[] { "先派出最下排泡泡同色的伙伴", "「优先出战」可以帮你调整顺序" };
            return new[] { "先观察最下排泡泡的颜色", "再派同色伙伴，三个同级就能合成" };
        }
    }
}
#endif
