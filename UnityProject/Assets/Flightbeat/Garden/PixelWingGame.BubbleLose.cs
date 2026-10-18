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
        string loseTip = "";

        // The HUD progress frame/fill art, scaled to `w` wide; the fill shows `p` (0..1) of the bar.
        void BubbleProgressBar(PixelSurface s, float cx, float y, float w, float p, float alpha)
        {
            float k = w / 414f, h = 42f * k, x = cx - w * .5f;
            Color white = new Color(1f, 1f, 1f, alpha);
            if (!Asset(s, "scene/progress-frame", x, y, w, h, white)) { s.Rect(x, y, w, h, A(Wood, alpha)); }
            if (!AssetPart(s, "scene/progress-fill", x + 9f * k, y + 7.5f * k, 396f * k, 27f * k, p, white))
                s.Rect(x + 9f * k, y + 7.5f * k, 396f * k * Mathf.Clamp01(p), 27f * k, A(Hex(0x62E18E), alpha));
            // A small bubble riding the tip of the fill.
            if (p > .02f) GardenSkin.Shot(s, Hex(0xBFE8FF), new Vector2(x + 9f * k + 396f * k * Mathf.Clamp01(p), y + h * .5f), h * .62f, alpha);
        }

        // ---- revive offer (pixel overlay, map layout as before; buttons keep ids 30 / 31)
        void DrawGardenRevive(PixelSurface s)
        {
            GardenModal(s, "泡泡碰到护栏啦", 210, 560);
            float bob = Mathf.Sin(ui * 2.6f) * 3f;
            Asset(s, "ui/guide-worried", 270f - 58f, 246f + bob, 116f, 116f, Color.white);
            BubbleProgressBar(s, 270f, 372f, 330f, Battle.Progress, 1f);
            Text(s, "已打掉 " + Mathf.FloorToInt(Battle.Progress * 100f) + "%  ·  还剩 " + Battle.Remaining + " 颗", 270, 414, 17, true, Ink);
            // What reviving gives, as two pictures with numbers.
            Text(s, "复活后", 270, 450, 16, true, Hex(0x6A7F9E));
            float cy = 500f;
            s.Glow(170f, cy, 170f, 86f, new Color(.62f, .82f, 1f, .45f), 0f);
            s.Glow(370f, cy, 170f, 86f, new Color(.80f, .66f, 1f, .45f), 0f);
            Asset(s, "scene/plot", 112f, cy - 15f, 58f, 29f, Color.white);
            TextOutline(s, "出战位 +1", 222f, cy - 11f, 19f, Cream, Outline, 1.8f);
            SoapBubble(s, new Vector2(320f, cy), 40f, Hex(0xD9C2FF), 1f, 3);
            TextOutline(s, "炸弹 +1", 400f, cy - 11f, 19f, Cream, Outline, 1.8f);
            Text(s, "还会清掉最下排，把泡泡推回去", 270, 548, 16, true, Hex(0x6A7F9E));
            GardenTitleButton(s, 30, 80, 610, 380, 68, "复 活", true);
            GardenButton(s, 31, 190, 692, 160, 40, "放弃");
        }

        // ---- fail page: drawn over the prefab panel (its portrait / level / progress labels are hidden while lost;
        // the title 这次被泡泡压住了 and the 再 试 一 次 / 返回主页 buttons stay).
        void DrawLoseScreen(PixelSurface s)
        {
            if (Battle == null || Battle.State != Phase.Lost || !(feedback.EndAge > .48f)) { loseShownAt = -1f; return; }
            if (loseBattle != Battle || loseShownAt < 0f) { loseBattle = Battle; loseShownAt = ui; loseTip = LoseTip(); }
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float oy = (ScreenLayout.Height - 960f) * .5f, t = ui - loseShownAt, fade = Clamp01(t / .3f);
            float p = Battle.Progress, shown = p * Mathf.SmoothStep(0f, 1f, Clamp01((t - .2f) / .8f));
            // Worried Xiao Lan under the title (was drawn over it), a slow sigh-bob.
            float pop = OutBack(Clamp01(t / .35f), 2f), bob = Mathf.Sin(ui * 2.2f) * 3f;
            Asset(s, "ui/guide-worried", 270f - 60f * pop, oy + 250f + 60f * (1f - pop) + bob, 120f * pop, 120f * pop, new Color(1f, 1f, 1f, fade));
            Text(s, "第 " + (levelIndex + 1) + " 关", 270, oy + 378f, 18, true, A(Hex(0x6A7F9E), fade));
            BubbleProgressBar(s, 270f, oy + 408f, 340f, shown, fade);
            Text(s, "已打掉 " + Mathf.FloorToInt(shown * 100f) + "%", 270, oy + 450f, 18, true, A(Ink, fade));
            string cheer = p >= .7f ? "就差一点点！" : p >= .5f ? "已经打掉一大半啦！" : p >= .3f ? "打得不错，再来一次！" : "再来一次，摸清这关的套路";
            float c = Clamp01((t - .6f) / .3f);
            TextOutline(s, cheer, 270f, oy + 486f, 24f * (.85f + .15f * OutBack(c, 2.4f)), A(Gold, c), A(Outline, c), 2.2f);
            // One tip, on a soft blue band.
            float tipA = Clamp01((t - .9f) / .3f);
            s.Glow(270f, oy + 574f, 420f, 92f, new Color(.62f, .82f, 1f, .55f * tipA), 0f);
            Text(s, "小提示", 270, oy + 546f, 15, true, A(Hex(0x4A78B0), tipA));
            Text(s, loseTip, 270, oy + 570f, 17, true, A(Ink, tipA));
            s.MapLayout = mapped;
        }

        // Why the run was lost, read once when the fail page opens.
        string LoseTip()
        {
            int bombs = Economy != null ? Economy.Data.items[2] : 0, designate = Economy != null ? Economy.Data.items[1] : 0;
            if (Battle.Stalled || Battle.Fleet.Count >= Battle.UnlockedSlots) return "出战位满了会卡住，先用泡泡炸弹清开最下排";
            if (bombs > 0) return "泡泡炸弹还剩 " + bombs + " 个，压下来之前用掉它";
            if (designate > 0) return "用「优先出战」先派和最下排同色的伙伴";
            return "先派和最下排同色的伙伴，三个同级会合成变强";
        }
    }
}
#endif
