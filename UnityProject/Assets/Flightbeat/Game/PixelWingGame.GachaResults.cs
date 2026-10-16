using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54620. The source big argument is unused; preserve the common card.
        private float DrawResultCard(PixelSurface s, int index, float cx, float cy, float age, bool big)
        {
            Color tint = ResultColor(index);
            float flip = OriginalMath.WasmMax(Mathf.Abs(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(age / .35f)) * 3.1415927410125732f) * .5f))), .06f);
            // WAT f54620 folds 1-.8 to 0x1.999998p-3, not the literal .2.
            float growth = OriginalMath.Binary32(OriginalMath.Binary32(Clamp01(OutBack(Clamp01(OriginalMath.Binary32(age / .45f)), 1.4f)) * .19999998807907104f) + .8f);
            float width = OriginalMath.Binary32(OriginalMath.Binary32(flip * 300f) * growth), height = OriginalMath.Binary32(growth * 380f);
            s.Glow(cx, cy, OriginalMath.Binary32(width + 140f), OriginalMath.Binary32(height + 120f), A(tint, .45f), 0f);
            float left = OriginalMath.Binary32(cx - OriginalMath.Binary32(width * .5f)), top = OriginalMath.Binary32(cy - OriginalMath.Binary32(height * .5f));
            Plate(s, left, top, width, height, Mix(tint, uiPanelHi, .55f), SourceColor(4428659729047601807UL, 4575657222445255885UL), tint, 14f);
            if (flip > .6f)
            {
                float fade = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(flip - .6f)) * 2.5f));
                if (partResults != null)
                {
                    PartPull pull = partResults[index]; int fighter = pull.index / 5, part = pull.index % 5;
                    Color accent = FleetCatalog.FighterAccent[fighter];
                    Plate(s, OriginalMath.Binary32(cx - 60f), OriginalMath.Binary32(top + 16f), 120f, 24f, Mix(new Color(.1f, .2f, .28f, 1f), accent, .35f),
                        SourceColor(4441630096489824911UL, 4575657222447940239UL), accent, 5f);
                    s.TextF(FleetCatalog.FighterNames[fighter], cx, OriginalMath.Binary32(top + 19f), 1.05f, Color.white, 0);
                    if (pull.first)
                    {
                        ResultNewPlate(s, OriginalMath.Binary32(OriginalMath.Binary32(width + left) - 86f), OriginalMath.Binary32(top - 12f), 80f, 26f, 5f);
                        Label(s, "新部件!", OriginalMath.Binary32(OriginalMath.Binary32(width + left) - 46f), OriginalMath.Binary32(top - 8f), 1.1f, Color.white);
                    }
                    Color glow = accent; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(fade * .5f) * accent.a);
                    s.Glow(cx, OriginalMath.Binary32(top + 140f), 190f, 190f, glow, 0f);
                    DrawPartThumb(s, fighter, part, cx, OriginalMath.Binary32(top + 140f), 150f, fade, false);
                    Label(s, FleetCatalog.PartNames[part] + " · " + FleetCatalog.PartRoles[part], cx, OriginalMath.Binary32(top + 232f), 1.8f, new Color(1f, 1f, 1f, fade));
                    s.TextF(PartResultNote(pull), cx, OriginalMath.Binary32(top + 274f), 1.05f, A(pull.first ? uiGoldHi : uiCyan, fade), 0);
                    s.TextF(PartResultHint(pull), cx, OriginalMath.Binary32(top + 304f), .95f, A(uiDim, fade), 0);
                }
                else
                {
                    FleetCore core = gachaResults[index];
                    QualityTag(s, core.quality, OriginalMath.Binary32(OriginalMath.Binary32(cx + OriginalMath.Binary32(PixelSurface.Measure(FleetCatalog.QualityNames[core.quality], .95f) * -.5f)) - 5f), OriginalMath.Binary32(top + 18f));
                    if (gachaNew)
                    {
                        ResultNewPlate(s, OriginalMath.Binary32(OriginalMath.Binary32(width + left) - 70f), OriginalMath.Binary32(top - 12f), 64f, 26f, 5f);
                        Label(s, "新!", OriginalMath.Binary32(OriginalMath.Binary32(width + left) - 38f), OriginalMath.Binary32(top - 8f), 1.2f, Color.white);
                    }
                    Color glow = tint; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(tint.a * fade) * .5f);
                    s.Glow(cx, OriginalMath.Binary32(top + 130f), 170f, 170f, glow, 0f);
                    ModuleGlyph(s, core.kind, OriginalMath.Binary32(cx - 64f), OriginalMath.Binary32(top + 64f), 128f, new Color(1f, 1f, 1f, fade));
                    Label(s, core.Spec.name, cx, OriginalMath.Binary32(top + 212f), 2f, A(tint, fade));
                    Label(s, FleetCatalog.SlotNames[core.Slot] + "模块", cx, OriginalMath.Binary32(top + 250f), 1.2f, A(uiDim, fade));
                    s.TextF(core.Spec.Describe(core.quality, 1f), cx, OriginalMath.Binary32(top + 280f), 1.15f, new Color(1f, 1f, 1f, fade), 0);
                    if (core.quality == 4) s.TextF("传说 · " + FleetCatalog.LegendText[core.Slot], cx, OriginalMath.Binary32(top + 312f), 1f, A(uiGold, fade), 0);
                }
            }
            return OriginalMath.Binary32(height + top);
        }
        // Shared literal plate sequence in f54620/f54621, with their own sizes.
        private void ResultNewPlate(PixelSurface s, float x, float y, float width, float height, float bevel)
            => Plate(s, x, y, width, height, SourceColor(4518011146384441344UL, 4575657222458677658UL),
                SourceColor(4453159313463259955UL, 4575657222445255885UL), uiInk, bevel);
        // f54621.
        private void DrawResultMini(PixelSurface s, int index, float x, float y, float width, float height, float age)
        {
            if (age < 0f)
            {
                Plate(s, x, y, width, height, SourceColor(4477658893522881741UL, 4575657222456328847UL),
                    SourceColor(4435865490172401418UL, 4575657222447940239UL), A(uiEdge, .8f), 8f);
                s.Sprite(12, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + x) - 22f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y) - 22f), 44f, 44f, SourceColor(4575657222473777152UL, 4539628425454813184UL), 0f);
                return;
            }
            Color tint = ResultColor(index); float cx = OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + x);
            float flip = OriginalMath.WasmMax(Mathf.Abs(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(age / .2f)) * 3.1415927410125732f) * .5f))), .08f);
            if (age < .3f)
            {
                Color glow = tint; glow.a = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.3f) + 1f)) * .5f) * tint.a);
                s.Glow(cx, OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y), OriginalMath.Binary32(width + width), OriginalMath.Binary32(height * 1.4f), glow, 0f);
            }
            width  =             OriginalMath.Binary32(width*(flip));
            Plate(s, OriginalMath.Binary32(cx + OriginalMath.Binary32(width * -.5f)), y, width, height, Mix(tint, uiPanelHi, .6f), SourceColor(4428659729047601807UL, 4575657222445255885UL), tint, 8f);
            if (flip < .6f) return;
            if (partResults != null)
            {
                PartPull part = partResults[index]; int fighter = part.index / 5, slot = part.index % 5; Color accent = FleetCatalog.FighterAccent[fighter];
                s.Glow(cx, OriginalMath.Binary32(y + 50f), 70f, 70f, A(accent, .4f), 0f); DrawPartThumb(s, fighter, slot, cx, OriginalMath.Binary32(y + 48f), 62f, 1f, false);
                s.TextF(FleetCatalog.FighterNames[fighter], cx, OriginalMath.Binary32(y + 86f), .92f, Color.white, 0);
                s.TextF(FleetCatalog.PartNames[slot], cx, OriginalMath.Binary32(y + 106f), 1f, accent, 0);
                if (part.first)
                {
                    ResultNewPlate(s, OriginalMath.Binary32(cx - 30f), OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 26f), 60f, 20f, 4f);
                    s.TextF("新!", cx, OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 24f), .9f, Color.white, 0);
                }
                else s.TextF("重复 +1", cx, OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 24f), .88f, uiCyan, 0);
            }
            else
            {
                FleetCore core = gachaResults[index];
                s.Glow(cx, OriginalMath.Binary32(y + 46f), 60f, 60f, A(tint, .4f), 0f); ModuleGlyph(s, core.kind, OriginalMath.Binary32(cx - 30f), OriginalMath.Binary32(y + 14f), 60f, Color.white);
                s.TextF(core.Spec.name, cx, OriginalMath.Binary32(y + 84f), .9f, tint, 0);
                s.TextF(FleetCatalog.QualityNames[core.quality], cx, OriginalMath.Binary32(y + 106f), .95f, Color.white, 0);
                s.TextF(FleetCatalog.SlotNames[core.Slot], cx, OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 24f), .88f, uiDim, 0);
            }
        }
    }
}
