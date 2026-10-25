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
            // Reference composition: large plush heart, two reward icons and one
            // green action. The close cross keeps the original decline action (31).
            bool mapped = s.MapLayout;
            try
            {
                float oy = GardenModal(s, "复活", 220f, 540f, 33f, 32f, .8f, "plants/dialog/pause-panel");
                float bob = Mathf.Sin(ui * 2.6f) * 2f;
                Asset(s, "plants/dialog/revive-heart", 130f, oy + 300f + bob, 280f, 224f, Color.white);
                ReviveBenefitIcons(s, oy + 518f);
                ReviveEffectHint(s, oy + 608f);
                DialogReferenceButton(s, 30, 148f, oy + 662f, 244f, 78f, "复活", true);
                HitBox(s, 31, 457f, oy + 207f, 50f, 50f);
                Asset(s, "plants/dialog/close", 457f, oy + 207f, 50f, 50f, Color.white);
            }
            finally { s.MapLayout = mapped; }
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

        // Standalone loss composition: gray ribbon, plush hero, and the prefab home/retry/share row.
        void DrawLoseScreen(PixelSurface s)
        {
            if (Battle == null || Battle.State != Phase.Lost || !(feedback.EndAge > .48f)) { loseShownAt = -1f; return; }
            if (loseBattle != Battle || loseShownAt < 0f) { loseBattle = Battle; loseShownAt = ui; }
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            try
            {
                float t = ui - loseShownAt, fade = Clamp01(t / .3f), ribbonTop = ScreenLayout.Height * .15f;
                float ribbonY = ribbonTop - 12f * (1f - Mathf.SmoothStep(0f, 1f, Clamp01(t / .35f)));
                Asset(s, "plants/lose/ribbon", 60f, ribbonY, 420f, 117.6f, A(Color.white, fade));
                GardenBarArt.TextOutline(s, "挑战失败", 270f, ribbonY + 18f, 44f,
                    A(Color.white, fade), A(Hex(0x45516B), fade), 2.7f);
                float fullSize = Mathf.Min(408f, ScreenLayout.Height * .4f);
                float pop = Mathf.SmoothStep(.94f, 1f, Clamp01(t / .4f)), size = fullSize * pop;
                Asset(s, "plants/lose/hero", 270f - size * .5f, ribbonTop + 154f + (fullSize - size) * .5f,
                    size, size, A(Color.white, fade));
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif
