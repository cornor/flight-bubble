using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54559. Negative target guard, mapping restoration, twelve authored rays.
        private void DrawZoneReward(PixelSurface s)
        {
            int zone = zoneRewardFor; if (zone < 0) return;
            Color accent = WarZones.Accent[zone]; Backdrop(s, .88f);
            bool mapped = s.MapLayout; s.MapLayout = false;
            float age = Since(702), enter = OutBack(OriginalMath.Binary32(age / .35f), 1.9f), alpha = Clamp01(OriginalMath.Binary32(enter * 1.3f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) + -235f))), cy = OriginalMath.Binary32(top + 120f);
            for (int ray = 0; ray < 12; ray++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age * 20f) + OriginalMath.Binary32((float)(ray * 30)))) * .01745329238474369f);
                Color tint = accent; tint.a = OriginalMath.Binary32(accent.a * (OriginalMath.Binary32(alpha * .08f)));
                s.GlowLine(new Vector2(270f, cy), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * 400f) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(OriginalMath.Sin(angle) * 400f))), 70f, tint);
            }
            Plate(s, 44f, top, 452f, 470f, uiPanelHi, uiPanel, accent, 7f, alpha);
            Plate(s, 120f, OriginalMath.Binary32(top + -20f), 300f, 58f, SourceColor(4561245704525185024UL, 1050253722u, alpha),
                SourceColor(4532422664329678029UL, 1034147594u, alpha), A(uiInk, alpha), 10f);
            Title(s, "战区收复！", 270f, OriginalMath.Binary32(top + -10f), 2.4f, Color.white, uiBrown);
            DrawZoneScene(s, zone, 64f, OriginalMath.Binary32(top + 50f), 412f, 110f, 1f);
            Label(s, WarZones.Codes[zone] + " · " + WarZones.Names[zone], 270f, OriginalMath.Binary32(top + 172f), 1.5f, Color.white);
            float rewardY = OriginalMath.Binary32(top + 214f);
            RewardTile(s, 64f, rewardY, 196f, 1, "+" + WarZones.RewardCoins[zone], null, OriginalMath.Binary32(age + -.2f), alpha);
            RewardTile(s, 280f, rewardY, 196f, 2, "+" + WarZones.RewardFragments[zone], null, OriginalMath.Binary32(age + -.35f), alpha);
            string parts = FleetCatalog.FighterNames[Economy.PartTarget] + " 部件 ×" + WarZones.RewardParts[zone];
            Label(s, parts, 270f, OriginalMath.Binary32(rewardY + 92f), 1.3f, FleetCatalog.FighterAccent[Economy.PartTarget]);
            float bottom = OriginalMath.Binary32(top + 470f);
            Button(s, 701, 74f, OriginalMath.Binary32(bottom + -120f), 392f, 56f, "看广告 金币碎片翻倍", Btn.Green, 1.6f, true, 0, true);
            Button(s, 700, 150f, OriginalMath.Binary32(bottom + -54f), 240f, 40f, "直接领取", Btn.Ghost, 1.25f, true);
            s.MapLayout = mapped;
        }
    }
}
