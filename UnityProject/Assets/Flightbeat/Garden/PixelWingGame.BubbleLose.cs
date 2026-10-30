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
                s.GlowLine(new Vector2(270f, oy + 150f), new Vector2(270f, oy + 430f), 300f, new Color(1f, .9f, .65f, .10f * lightA));
                s.Glow(270f, oy + 345f, 420f * hp, 330f * hp, new Color(1f, .8f, .55f, .30f * lightA), 0f);
                s.Glow(270f, oy + 345f, 240f * hp, 200f * hp, new Color(1f, .55f, .55f, .30f * lightA), 0f);
                Asset(s, "plants/dialog/revive-heart", 270f - 150f * hp, oy + 345f - 120f * hp + bob, 300f * hp, 240f * hp, A(Color.white, fade));
                ResultTwinkles(s, 270f, oy + 345f, 165f, 95f, fade);
                float ra = Clamp01((t - .35f) / .2f) * fade;
                ReviveTile(s, 186f, oy + 500f, false, ra);
                ReviveTile(s, 362f, oy + 500f, true, ra);
                ResultSubtitle(s, "清除前排泡泡", oy + 586f, 21f, Color.white, ra);
                ResultSubtitle(s, "泡泡上推，继续挑战", oy + 618f, 21f, Color.white, ra);
                ResultPill(s, 30, 115f, oy + 667f, 315f, 76f, "red", "看视频复活", Has("plants/uiref/icon-video") ? "plants/uiref/icon-video" : "plants/dialog/video", Has("plants/uiref/button-red") ? (Color?)null : new Color(1f, .45f, .42f));
                ResultPill(s, 32, 80f, oy + 770f, 185f, 60f, "blue", "返回主页");
                ResultPill(s, 33, 280f, oy + 770f, 183f, 60f, "white", "重新挑战");
            }
            finally { s.MapLayout = mapped; }
        }
        float reviveShownAt = -1f;
        Flightbeat.Model.Battle reviveBattle;
        // One reward on a dark rounded tile: the open-lock pad (+1 出战位) or the rainbow bomb (+1 泡泡炸弹), 「+1」 beside it.
        void ReviveTile(PixelSurface s, float cx, float cy, bool bomb, float a)
        {
            if (a <= .01f) return;
            const float w = 157f, h = 95f;
            WinRoundRect(s, cx - w * .5f - 1.5f, cy - h * .5f - 1.5f, w + 3f, h + 3f, 19f, A(new Color(1f, 1f, 1f, .28f), a));
            WinRoundRect(s, cx - w * .5f, cy - h * .5f, w, h, 18f, A(new Color(.62f, .7f, .9f, .16f), a));
            s.Rect(cx - w * .5f + 16f, cy - h * .5f + 2f, w - 32f, 1.5f, A(new Color(1f, 1f, 1f, .25f), a));
            float ix = cx - 26f;
            if (bomb) GardenBars.Icon(s, "bomb", ix, cy, 54f, a);
            else
            {
                Asset(s, "scene/plot", ix - 30f, cy + 6f, 60f, 24f, A(Color.white, a));
                GardenBars.Icon(s, "unlock", ix, cy - 8f, 38f, a);
            }
            GardenBarArt.Text(s, "+1", cx + 22f, cy - 17f, 30f, false, A(Color.white, a));
            GardenBarArt.Text(s, "+1", cx + 22.8f, cy - 17f, 30f, false, A(Color.white, a));
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
