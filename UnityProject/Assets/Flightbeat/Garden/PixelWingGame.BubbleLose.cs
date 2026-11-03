#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        float loseShownAt = -1f;
        Flightbeat.Model.Battle loseBattle;
        // ---- revive offer (pixel overlay, map layout as before; buttons keep ids 30 / 31)
        void DrawGardenRevive(PixelSurface s)
        {
            // 2026-10-29 (界面优化 14, user: 所有的结算都要统一): a full-screen page like the result pages, no card:
            // 「差一点就成功了！」 / 「再给小队一次机会」, the winged heart, the two rewards (+1 出战位, +1 泡泡炸弹) on dark
            // tiles, what reviving does, the red 看视频复活 (30), and 返回主页 (32) · 重新挑战 (33) instead of the close cross.
            bool mapped = s.MapLayout;
            try
            {
                if (reviveBattle != Battle || reviveShownAt < 0f) { reviveBattle = Battle; reviveShownAt = ui; }
                float t = ui - reviveShownAt, fade = Clamp01(t / .25f);
                HitBlock(s);
                s.MapLayout = false;
                float oy = (ScreenLayout.Height - 960f) * .5f;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.035f, .055f, .16f, .84f * fade));
                // 2026-10-30 (调整 14): positions and sizes measured from the reference; a warm light falls from above
                // onto the heart; the reward tiles are see-through glass.
                ResultTitle(s, "差一点就成功了！", oy + 162f, 46f, t, fade);
                ResultSubtitle(s, "再给小队一次机会", oy + 218f, 26f, ResultGold, Clamp01((t - .15f) / .2f) * fade);
                float hp = OutBack(Clamp01((t - .1f) / .4f), 1.8f), bob = Mathf.Sin(ui * 2.6f) * 3f;
                float lightA = fade * (.85f + .15f * Mathf.Sin(ui * 1.7f));
                // 2026-10-31 16:26 (user: 爱心底下有那种好看的金光, reference): a golden radiance behind and under the heart -
                // a soft beam falling from above, a wide warm glow, a hot core under the heart and a glow pooling below
                // it - and gold four-point stars twinkling round it (were faint warm washes and white glints).
                s.GlowLine(new Vector2(270f, oy + 140f), new Vector2(270f, oy + 470f), 260f, new Color(1f, .86f, .42f, .2f * lightA));
                s.Glow(270f, oy + 350f, 400f * hp, 340f * hp, new Color(1f, .8f, .32f, .5f * lightA), 0f);
                s.Glow(270f, oy + 368f, 240f * hp, 200f * hp, new Color(1f, .92f, .58f, .6f * lightA), 0f);
                s.Glow(270f, oy + 452f, 300f * hp, 64f * hp, new Color(1f, .84f, .42f, .5f * lightA), 0f);
                Asset(s, "plants/dialog/revive-heart", 270f - 150f * hp, oy + 345f - 120f * hp + bob, 300f * hp, 240f * hp, A(Color.white, fade));
                if (Has("scene/revive-sparkle"))
                    for (int j = 0; j < HeartStars.Length; j += 3)
                    {
                        float k = .55f + .45f * Mathf.Sin(ui * 4.5f + j * .9f), d = HeartStars[j + 2] * (.6f + .4f * k) * hp;
                        Asset(s, "scene/revive-sparkle", 270f + HeartStars[j] - d * .5f, oy + 345f + HeartStars[j + 1] - d * .5f, d, d, A(Color.white, fade * (.5f + .5f * k)));
                    }
                else ResultTwinkles(s, 270f, oy + 345f, 165f, 95f, fade);
                float ra = Clamp01((t - .35f) / .2f) * fade;
                ReviveTile(s, 186f, oy + 500f, false, ra);
                ReviveTile(s, 362f, oy + 500f, true, ra);
                // 2026-10-31: regular weight, soft white (was bold pure white, competing with the tiles and buttons).
                GardenBarArt.Text(s, "清除前排泡泡", 270f, oy + 586f - 21f * .55f, 21f, true, A(Hex(0xE4EAF3), ra));
                GardenBarArt.Text(s, "泡泡上推，继续挑战", 270f, oy + 618f - 21f * .55f, 21f, true, A(Hex(0xE4EAF3), ra));
                ResultPill(s, 30, 115f, oy + 667f, 315f, 76f, "red", "看视频复活", Has("plants/uiref/icon-video") ? "plants/uiref/icon-video" : "plants/dialog/video", Has("plants/uiref/button-red") ? (Color?)null : new Color(1f, .45f, .42f));
                ResultPill(s, 32, 80f, oy + 770f, 185f, 60f, "blue", "返回主页", Has("plants/uiref/icon-home") ? "plants/uiref/icon-home" : null);
                ResultPill(s, 33, 280f, oy + 770f, 183f, 60f, "white", "重新挑战", Has("plants/uiref/icon-replay") ? "plants/uiref/icon-replay" : null);
            }
            finally { s.MapLayout = mapped; }
        }
        float reviveShownAt = -1f;
        Flightbeat.Model.Battle reviveBattle;
        // One reward on a dark rounded tile: the open-lock pad (+1 出战位) or the rainbow bomb (+1 泡泡炸弹), 「+1」 beside it.
        // 2026-10-31 00:11 (user, after the reference): dark frosted glass - only a shade lighter than the page shade and
        // hiding what is behind it - with a thin light rim all round (was a milky tile with a white bar along the top:
        // its full-size rim fill showed through the translucent face). Bigger icons, the lock on a lilac pad.
        // Gold star spots round the revive heart (dx, dy from its centre, size): bottom corners as in the reference, sides, top.
        static readonly float[] HeartStars = { -118f, 78f, 34f, 112f, 84f, 30f, -168f, 4f, 20f, 170f, -14f, 24f, -60f, -112f, 16f, 74f, -104f, 20f, 0f, 112f, 18f };
        void ReviveTile(PixelSurface s, float cx, float cy, bool bomb, float a)
        {
            if (a <= .01f) return;
            const float w = 157f, h = 95f;
            // 2026-11-01 09:46 (user: 两个道具框加一点透明度): the glass lets the board show through - rim .5 and face .55
            // (where they overlap ~.78, was ~1).
            // 09:54 (user: 参照图是灰黑，透明度再大点): back to the reference's grey-black (rim #5E6573, face #3E4758),
            // more see-through - rim .35, face .42 (~.62 where they overlap).
            WinRoundRect(s, cx - w * .5f, cy - h * .5f, w, h, 18f, A(new Color(.37f, .40f, .46f, .35f), a));   // rim #5E6573
            WinRoundRect(s, cx - w * .5f + 2f, cy - h * .5f + 2f, w - 4f, h - 4f, 16f, A(new Color(.245f, .28f, .345f, .42f), a));   // face #3E4758
            float ix = cx - 30f;
            if (bomb) GardenBars.Icon(s, "bomb", ix, cy, 62f, a);
            else
            {
                Asset(s, "scene/plot", ix - 35f, cy + 8f, 70f, 28f, A(new Color(.87f, .69f, .95f, 1f), a));
                GardenBars.Icon(s, "unlock", ix, cy - 6f, 56f, a);
            }
            GardenBarArt.Text(s, "+1", cx + 12f, cy - 18f, 32f, false, A(Color.white, a));
        }

        static void ReviveEffectHint(PixelSurface s, float y)
        {
            const float size = 21f;
            const string lead = "同时", clear = "清除前排泡泡", join = "，并将泡泡", push = "推回";
            float x = 270f - GardenSkin.TextWidth(lead + clear + join + push, size) * .5f;
            Part(lead, Hex(0x553094));
            Part(clear, Hex(0x277EA7));
            Part(join, Hex(0x553094));
            Part(push, Hex(0xC56D18));

            void Part(string text, Color color)
            {
                GardenSkin.Text(s, text, x, y, size, false, color);
                x += GardenSkin.TextWidth(text, size);
            }
        }

        static void ReviveBenefitIcons(PixelSurface s, float y)
        {
            Asset9(s, "plants/dialog/pause-row", 114f, y, 144f, 80f, 24f, 16f, Color.white);
            Asset9(s, "plants/dialog/pause-row", 282f, y, 144f, 80f, 24f, 16f, Color.white);
            // The same platform and open lock used in battle make the extra slot recognizable.
            Asset(s, "scene/plot", 130f, y + 49f, 64f, 25.6f, Color.white);
            GardenBars.Icon(s, "unlock", 162f, y + 29f, 41f);
            GardenBars.Icon(s, "bomb", 326f, y + 39f, 62f);
            GardenBarArt.TextOutline(s, "+1", 222f, y + 26f, 28f, Hex(0xF28A23), Color.white, 1.2f);
            GardenBarArt.TextOutline(s, "+1", 390f, y + 26f, 28f, Hex(0xF28A23), Color.white, 1.2f);
        }

        // 2026-10-29 (界面优化 17, user: 所有的结算都要统一): like the victory page - 「挑战失败」 big and white, 「护栏没守住，
        // 再来一局！」, the sad plush in the middle (lose/hero; the new sad art ui/lose-hero-sad when delivered), and the
        // prefab's blue 重新挑战 with white 返回主页 · 分享 below (GardenBattleUiAssets.Result).
        void DrawLoseScreen(PixelSurface s)
        {
            if (Battle == null || Battle.State != Phase.Lost || !(feedback.EndAge > .48f)) { loseShownAt = -1f; return; }
            if (loseBattle != Battle || loseShownAt < 0f) { loseBattle = Battle; loseShownAt = ui; }
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            try
            {
                float t = ui - loseShownAt, fade = Clamp01(t / .3f), oy = (ScreenLayout.Height - 960f) * .5f;
                // 2026-10-30 (调整 17): measured from the reference: a very big title, the sad bear filling the middle.
                ResultTitle(s, "挑战失败", oy + 78f, 72f, t, fade);
                ResultSubtitle(s, "护栏没守住，再来一局！", oy + 152f, 25f, Color.white, Clamp01((t - .15f) / .2f) * fade);
                string hero = Has("plants/uiref/lose-hero-sad") ? "plants/uiref/lose-hero-sad" : "plants/lose/hero";
                float size = 440f * Mathf.SmoothStep(.94f, 1f, Clamp01(t / .4f));
                s.Glow(270f, oy + 400f, 460f, 400f, new Color(.45f, .6f, 1f, .16f * fade), 0f);
                Asset(s, hero, 280f - size * .5f, oy + 405f - size * .5f, size, size, A(Color.white, fade));
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif
