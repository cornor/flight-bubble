using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54610. Zoom advances during drawing by original unscaledDeltaTime.
        private void DrawGearTab(PixelSurface s, float y0, float bottom, float slide)
        {
            int fighter = hangarFighter; bool assembled = Economy.Assembled(fighter);
            Color accent = FleetCatalog.FighterAccent[fighter]; int resonance = FleetCatalog.FighterResonance[fighter];
            float target = moduleInspect >= 0 ? 1f : 0f, difference = OriginalMath.Binary32(target - gearZoom), step = OriginalMath.Binary32(PaintDeltaTime * 3.5f);
            gearZoom = Mathf.Abs(difference) <= step ? target : OriginalMath.Binary32(gearZoom + (difference >= 0f ? step : -step));
            float available = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(bottom - y0) - 52f) - 74f) - 30f);
            float height = available < 200f ? 200f : OriginalMath.WasmMin(available, 420f);
            float cx = OriginalMath.Binary32(slide + 270f), cy = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y0) + 8f), shipHeight = OriginalMath.Binary32(height * .94f);
            float explode = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 1.3f)) * .12f) + .78f);
            int focus = moduleInspect >= 0 ? moduleInspect : previousPickSlot; focus = focus < 4 ? focus : 4; focus = focus > 0 ? focus : 0;
            Vector2 part = PartCenter(fighter, focus, cx, cy, shipHeight, explode);
            float smooth = OriginalMath.Binary32(OriginalMath.Binary32(gearZoom * gearZoom) * (OriginalMath.Binary32(3f - (OriginalMath.Binary32(gearZoom + gearZoom))))), zoom = OriginalMath.Binary32(OriginalMath.Binary32(smooth * .85f) + 1f);
            float blend = smooth < 0f ? 0f : OriginalMath.WasmMin(smooth, 1f);
            float py = OriginalMath.Binary32(cy + OriginalMath.Binary32(blend * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(cy - part.y)) * zoom) + OriginalMath.Binary32((OriginalMath.Binary32(PickTop + (OriginalMath.Binary32(UpgradeTop + 150f)))) * .5f)) - cy))));
            float px = OriginalMath.Binary32(cx + OriginalMath.Binary32(blend * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(cx - part.x)) * zoom) + 270f) - cx)))); shipHeight  = OriginalMath.Binary32(shipHeight*(zoom));
            s.Glow(px, py, OriginalMath.Binary32(shipHeight * 1.4f), OriginalMath.Binary32(shipHeight * 1.1f), A(accent, .12f), 0f);
            float rest = OriginalMath.Binary32(1f - smooth); Color ring = accent; ring.a = OriginalMath.Binary32(OriginalMath.Binary32(ring.a * rest) * .07f);
            foreach (float factor in new[] { .34f, .43f, .52f }) s.Ring(new Vector2(cx, OriginalMath.Binary32(OriginalMath.Binary32(height * .08f) + cy)), OriginalMath.Binary32(height * factor), ring, 1f, 48, 0f, 360f);
            float wave = OriginalMath.Sin(OriginalMath.Binary32(ui * 1.6f));
            DrawFighterArt(s, fighter, px, OriginalMath.Binary32(py + OriginalMath.Binary32(rest * (OriginalMath.Binary32(wave + wave)))), shipHeight, explode, moduleInspect >= 0 ? moduleInspect : -1, 1f, false, true);
            if (smooth < .99f)
            {
                Label(s, FleetCatalog.FighterNames[fighter] + " · 装备", OriginalMath.Binary32(slide + 40f), OriginalMath.Binary32(y0 + 2f), 1.9f, new Color(1f, 1f, 1f, rest), -1);
                Color hint = accent; hint.a = OriginalMath.Binary32(OriginalMath.Binary32(hint.a * rest) * .95f);
                s.TextF("点击飞机部位装配模块", OriginalMath.Binary32(slide + 40f), OriginalMath.Binary32(y0 + 32f), 1f, hint, -1);
                Plate(s, OriginalMath.Binary32(slide + 350f), OriginalMath.Binary32(y0 + 4f), 150f, 26f, Mix(new Color(.1f, .2f, .28f, 1f), accent, .25f),
                    SourceColor(4441630096489824911UL, 4575657222447940239UL), A(accent, rest), 6f);
                s.TextF("共鸣：" + FleetCatalog.SlotNames[resonance] + " ×1.25", OriginalMath.Binary32(slide + 425f), OriginalMath.Binary32(y0 + 8f), .95f, new Color(1f, 1f, 1f, rest), 0);
            }
            for (int slot = 0; slot < 5; slot++)
            {
                Vector2 at = PartCenter(fighter, slot, px, py, shipHeight, explode);
                if (moduleInspect < 0 || moduleInspect == slot) DrawGearNode(s, fighter, slot, at, moduleInspect == slot);
            }
            if (moduleInspect >= 0) return;
            float left = OriginalMath.Binary32(slide + 24f), row = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(bottom - 52f) - 74f) - 6f);
            for (int slot = 0; slot < 5; slot++)
            {
                FleetCore core = Economy.InPreset(fighter, slot); float x = OriginalMath.Binary32(left + OriginalMath.Binary32(OriginalMath.Binary32((float)slot) * 98.4000015258789f));
                HitBox(s, 340 + slot, OriginalMath.Binary32(x + 2f), row, 94.4000015258789f, 74f);
                float pulse = PressPulse(340 + slot), shrink = OriginalMath.Binary32(pulse + pulse);
                Color quality = core == null ? A(uiEdge, .7f) : FleetCatalog.QualityColors[core.quality];
                Color top = core == null ? new Color(.04f, .1f, .15f, .9f) : Mix(new Color(.05f, .14f, .21f, .96f), quality, .14f);
                Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 2f) + pulse), OriginalMath.Binary32(row + pulse), OriginalMath.Binary32(94.4000015258789f - shrink), OriginalMath.Binary32(74f - shrink), top,
                    SourceColor(4417130516401346314UL, 4572774917136208364UL), slot == resonance ? accent : A(quality, .8f), 8f);
                float center = OriginalMath.Binary32(x + 49.20000076293945f);
                s.TextF(FleetCatalog.SlotNames[slot], center, OriginalMath.Binary32(row + 4f), .9f, slot == resonance ? accent : uiDim, 0);
                if (core != null)
                {
                    ModuleIcon(s, core, OriginalMath.Binary32(center - 19f), OriginalMath.Binary32(row + 22f), 38f);
                    DurabilityBar(s, core, OriginalMath.Binary32(x + 10f), OriginalMath.Binary32(OriginalMath.Binary32(row + 74f) - 10f), 78.4000015258789f);
                }
                else
                {
                    s.Rect(OriginalMath.Binary32(center - 8f), OriginalMath.Binary32(row + 40f), 16f, 3f, uiDim);
                    s.Rect(OriginalMath.Binary32(center - 1.5f), OriginalMath.Binary32(row + 33f), 3f, 16f, uiDim);
                }
                if (slot == resonance) s.TextF("共鸣", OriginalMath.Binary32(OriginalMath.Binary32(x + 98.4000015258789f) - 18f), OriginalMath.Binary32(row + 4f), .75f, accent, 0);
            }
            if (!assembled) s.TextF("战机组装后预设才会生效；可提前配好", cx, OriginalMath.Binary32(row - 20f), .92f, uiDim, 0);
            float buttonsY = OriginalMath.Binary32(OriginalMath.Binary32(bottom - 52f) + 4f);
            Button(s, 334, left, buttonsY, 236f, 44f, "装备补给", Economy.Data.fragments > 9 ? Btn.Green : Btn.Secondary, 1.4f, true, 2);
            Button(s, 384, OriginalMath.Binary32(slide + 272f), buttonsY, 244f, 44f, "分解模块", Btn.Ghost, 1.25f, true);
        }
        // f54683.
        private void DrawGearNode(PixelSurface s, int fighter, int slot, Vector2 at, bool open)
        {
            HitBox(s, 390 + slot, OriginalMath.Binary32(at.x - 40f), OriginalMath.Binary32(at.y - 40f), 80f, 80f);
            float pulse = PressPulse(390 + slot); FleetCore core = Economy.InPreset(fighter, slot);
            Color accent = FleetCatalog.FighterAccent[fighter]; int resonance = FleetCatalog.FighterResonance[fighter];
            Color tint = core == null ? A(uiDim, .8f) : FleetCatalog.QualityColors[core.quality];
            float radius = OriginalMath.Binary32((open ? 26f : 19f) * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .2f) + 1f)));
            if (resonance == slot) s.Glow(at.x, at.y, OriginalMath.Binary32(radius * 4f), OriginalMath.Binary32(radius * 4f), A(accent, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .1f) + .25f)), 0f);
            if (open)
                for (int arc = 0; arc < 8; arc++) s.Ring(at, OriginalMath.Binary32(radius + 6f), new Color(1f, 1f, 1f, .9f), 2f, 3, OriginalMath.Binary32(OriginalMath.Binary32(ui * 60f) + OriginalMath.Binary32((float)(arc * 45))), 20f);
            Disc(s, at.x, at.y, radius, SourceColor(4428659729042233098UL, 4567010310817107149UL));
            s.Ring(at, radius, tint, 2f, 28, 0f, 360f);
            if (core != null)
            {
                ModuleGlyph(s, core.kind, OriginalMath.Binary32(at.x - OriginalMath.Binary32(radius * .62f)), OriginalMath.Binary32(at.y - OriginalMath.Binary32(radius * .62f)), OriginalMath.Binary32(radius * 1.24f), Color.white);
                float durability = OriginalMath.Binary32(OriginalMath.Binary32((float)core.durability) / OriginalMath.Binary32((float)core.MaxDurability));
                s.Ring(at, OriginalMath.Binary32(radius + 3f), A(DurabilityColor(durability), .9f), 2.5f, 24, -90f, OriginalMath.Binary32(durability * 360f));
            }
            else
            {
                s.Rect(OriginalMath.Binary32(at.x - 7f), OriginalMath.Binary32(at.y - 1.5f), 14f, 3f, uiDim);
                s.Rect(OriginalMath.Binary32(at.x - 1.5f), OriginalMath.Binary32(at.y - 7f), 3f, 14f, uiDim);
            }
            if (!open)
            {
                string name = FleetCatalog.SlotNames[slot]; float width = OriginalMath.Binary32(PixelSurface.Measure(name, .85f) + 8f);
                s.Rect(OriginalMath.Binary32(at.x + OriginalMath.Binary32(width * -.5f)), OriginalMath.Binary32(OriginalMath.Binary32(radius + at.y) + 4f), width, 15f, SourceColor(4405601299457103626UL, 4564848585053527081UL));
                s.TextF(name, at.x, OriginalMath.Binary32(OriginalMath.Binary32(radius + at.y) + 5f), .85f, slot == resonance ? accent : Color.white, 0);
            }
        }
        // f54627 (also inlined in f54683): no durability ratio clamp.
        private Color DurabilityColor(float ratio) => ratio > .5f ? uiGreen : ratio > .2f ? uiGold : uiRed;
        private void DurabilityBar(PixelSurface s, FleetCore core, float x, float y, float width)
        {
            float ratio = OriginalMath.Binary32(OriginalMath.Binary32((float)core.durability) / OriginalMath.Binary32((float)core.MaxDurability));
            Color color = DurabilityColor(ratio);
            s.Rect(x, y, width, 5f, SourceColor(4417130516401346314UL, 4575657222442571530UL));
            s.Rect(OriginalMath.Binary32(x + 1f), OriginalMath.Binary32(y + 1f), OriginalMath.Binary32((OriginalMath.Binary32(width - 2f)) * ratio), 3f, color);
        }
        // f54675.
        private void QualityTag(PixelSurface s, int quality, float x, float y)
        {
            Color color = FleetCatalog.QualityColors[quality];
            float width = OriginalMath.Binary32(PixelSurface.Measure(FleetCatalog.QualityNames[quality], .95f) + 10f);
            s.Rect(x, y, width, 17f, A(color, .9f));
            s.TextF(FleetCatalog.QualityNames[quality], OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + x), OriginalMath.Binary32(y + 2f), .95f, uiInk, 0);
        }
    }
}
