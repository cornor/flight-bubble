using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54617. Four offers share the source deterministic economy getters.
        private void DrawOffers(PixelSurface s, float x, float y)
        {
            float pulse = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .5f) + .5f)) * .3f);
            pulse = pulse < 0f ? 0f : OriginalMath.WasmMin(pulse, 1f);
            // Preserve the original folded f32 lerp operands from the WAT.
            Color edge = new Color(OriginalMath.Binary32(OriginalMath.Binary32(pulse * 0f) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(pulse * .529411792755127f) + .47058823704719543f),
                OriginalMath.Binary32(OriginalMath.Binary32(pulse * .21568626165390015f) + .7843137383460999f), OriginalMath.Binary32(OriginalMath.Binary32(pulse * 0f) + 1f));
            Plate(s, x, y, 492f, 172f, SourceColor(4441630096517339546UL, 4572774917144596972UL),
                SourceColor(4369572502468674191UL, 4572774917133524009UL), edge, 10f, 1f);
            Label(s, "限时福利", OriginalMath.Binary32(x + 16f), OriginalMath.Binary32(y + 8f), 1.3f, SourceColor(4557289601558904832UL, 4575657222472132327UL), -1);
            s.TextF(Clock(Economy.OfferSecondsLeft) + " 后刷新", OriginalMath.Binary32(x + 476f), OriginalMath.Binary32(y + 12f), .95f, uiDim, 1);
            FleetCore module = Economy.OfferModule;
            int fighter = Economy.OfferPartFighter, slot = Economy.OfferPartSlot;
            Color[] tints = { uiGold, uiGoldHi, FleetCatalog.QualityColors[module.quality], FleetCatalog.FighterAccent[fighter] };
            float top = OriginalMath.Binary32(y + 36f), lower = OriginalMath.Binary32(top + 128f), centerY = OriginalMath.Binary32(top + 30f);
            for (int i = 0; i < 4; i++)
            {
                float px = OriginalMath.Binary32(OriginalMath.Binary32(x + 10f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 118f));
                bool ready = Economy.OfferReady(i);
                float flash = offerFlash == i ? Clamp01(OriginalMath.Binary32(OriginalMath.Binary32(Since(545) / -.6f) + 1f)) : 0f;
                int needed = Economy.OfferAdsNeeded(i), watched = Economy.OfferAdsWatched(i);
                HitBox(s, 540 + i, px, top, 114f, 128f);
                float press = PressPulse(540 + i), offset = OriginalMath.Binary32(press + press), shrink = OriginalMath.Binary32(press * 4f);
                Color tint = tints[i];
                Plate(s, OriginalMath.Binary32(px + offset), OriginalMath.Binary32(top + offset), OriginalMath.Binary32(114f - shrink), OriginalMath.Binary32(128f - shrink),
                    ready ? Mix(new Color(.1f, .06f, .12f, 1f), tint, OriginalMath.Binary32(OriginalMath.Binary32(flash * .4f) + .14f)) : new Color(.06f, .06f, .08f, 1f),
                    SourceColor(4369572502451896975UL, 4575657222436867277UL), ready ? A(tint, .9f) : uiEdge, 8f, 1f);
                float cx = OriginalMath.Binary32(px + 57f);
                s.Glow(cx, centerY, 78f, 70f, A(tint, ready ? .32f : .05f), 0f);
                string title = FleetCatalog.OfferNames[i], detail;
                Color detailColor = ready ? Color.white : uiDim;
                if (i <= 1)
                {
                    EnergyIcon(s, OriginalMath.Binary32(cx - 14f), centerY, 40f, true);
                    Label(s, "∞", OriginalMath.Binary32(cx + 18f), OriginalMath.Binary32(centerY - 12f), 1.6f, A(uiGoldHi, ready ? 1f : .5f));
                    detail = FleetCatalog.OfferUnlimitedMinutes[i] + " 分钟";
                }
                else if (i == 2)
                {
                    ModuleIcon(s, module, OriginalMath.Binary32(cx - 22f), OriginalMath.Binary32(centerY - 22f), 44f);
                    title = FleetCatalog.Modules[module.kind].name;
                    detail = FleetCatalog.QualityNames[module.quality] + " ×1";
                    detailColor = ready ? tint : uiDim;
                }
                else
                {
                    DrawPartThumb(s, fighter, slot, cx, centerY, 48f, ready ? 1f : .4f, false);
                    title = FleetCatalog.FighterNames[fighter] + " · " + FleetCatalog.SlotNames[slot];
                    detail = "部件 ×3";
                    detailColor = ready ? tint : uiDim;
                }
                float size = PixelSurface.Measure(title, .95f) > 104f ? .8f : .95f;
                s.TextF(title, cx, OriginalMath.Binary32(top + 58f), size, ready ? Color.white : uiDim, 0);
                s.TextF(detail, cx, OriginalMath.Binary32(top + 76f), .9f, detailColor, 0);
                if (ready)
                {
                    Plate(s, OriginalMath.Binary32(px + 10f), OriginalMath.Binary32(lower - 32f), 94f, 24f, uiGoldHi, uiGoldLo, uiInk, 5f, 1f);
                    AdIcon(s, OriginalMath.Binary32(px + 16f), OriginalMath.Binary32(lower - 29f), 18f);
                    s.TextF(needed <= 1 ? "领取" : watched + "/" + needed + " 次", OriginalMath.Binary32(cx + 10f), OriginalMath.Binary32(lower - 28f), .9f, uiBrown, 0);
                }
                else s.TextF("已领取", cx, OriginalMath.Binary32(lower - 28f), .95f, uiDim, 0);
                if (flash > 0f) s.Ring(new Vector2(cx, centerY), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - flash)) * 60f) + 20f),
                    new Color(1f, 1f, 1f, flash), 3f, 28, 0f, 360f);
            }
        }
    }
}
