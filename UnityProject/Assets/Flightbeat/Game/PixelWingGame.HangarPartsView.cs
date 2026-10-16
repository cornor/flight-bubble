using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54613. Diagram nodes use the unbobbed centre; only the ship bobs.
        private void DrawPartsTab(PixelSurface s, float y0, float bottom, float slide)
        {
            int fighter = hangarFighter; bool assembled = Economy.Assembled(fighter);
            Color accent = FleetCatalog.FighterAccent[fighter];
            float extra = assembled ? 72f : 0f;
            float space = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(bottom - y0) - 52f) - extra) - 160f) - 24f);
            float height = space < 200f ? 200f : OriginalMath.WasmMin(space, 420f);
            float cy = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + y0) + 4f), cx = OriginalMath.Binary32(slide + 270f);
            float explode = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 1.3f)) * .12f) + .78f);
            s.Glow(cx, cy, OriginalMath.Binary32(height * 1.4f), OriginalMath.Binary32(height * 1.1f), A(accent, .12f), 0f);
            foreach (float factor in new[] { .34f, .43f, .52f })
                s.Ring(new Vector2(cx, OriginalMath.Binary32(OriginalMath.Binary32(height * .08f) + cy)), OriginalMath.Binary32(height * factor), A(accent, .07f), 1f, 48, 0f, 360f);
            float flash = partFlash < 0 ? 0f : Clamp01(OriginalMath.Binary32(OriginalMath.Binary32(Since(490) / -.9f) + 1f));
            float bob = OriginalMath.Sin(OriginalMath.Binary32(ui * 1.6f)), shipHeight = OriginalMath.Binary32(height * .94f);
            DrawFighterArt(s, fighter, cx, OriginalMath.Binary32(cy + (OriginalMath.Binary32(bob + bob))), shipHeight, explode, hangarPart, 1f, false, true);
            Label(s, FleetCatalog.FighterNames[fighter], OriginalMath.Binary32(slide + 40f), OriginalMath.Binary32(y0 + 2f), 1.9f, Color.white, -1);
            s.TextF(FleetCatalog.FighterStyle[fighter], OriginalMath.Binary32(slide + 40f), OriginalMath.Binary32(y0 + 32f), 1f, A(accent, .95f), -1);
            if (assembled)
            {
                string title = "整机 LV" + Economy.FighterLevel(fighter); float width = OriginalMath.Binary32(PixelSurface.Measure(title, 1.1f) + 20f);
                Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(500f - width) + slide), OriginalMath.Binary32(y0 + 4f), width, 26f,
                    SourceColor(4489188110456051466UL, 4572054341897116713UL), SourceColor(4441630096489824911UL, 4572054341887050383UL), accent, 6f);
                s.TextF(title, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width * -.5f) + 500f) + slide), OriginalMath.Binary32(y0 + 8f), 1.1f, Color.white, 0);
                s.TextF("战力 " + Economy.PowerOf(fighter), OriginalMath.Binary32(slide + 500f), OriginalMath.Binary32(y0 + 34f), 1f, uiGold, 1);
            }
            else
            {
                Plate(s, OriginalMath.Binary32(slide + 392f), OriginalMath.Binary32(y0 + 4f), 108f, 26f,
                    SourceColor(4471894287194049741UL, 4572054341892754637UL), SourceColor(4428659729042233098UL, 4572054341884366029UL), A(uiCyan, .8f), 6f);
                s.TextF("组装 " + Economy.Collected(fighter) + "/5", OriginalMath.Binary32(slide + 446f), OriginalMath.Binary32(y0 + 8f), 1.1f, Economy.CanAssemble(fighter) ? uiGreen : uiCyan, 0);
            }
            for (int part = 0; part < 5; part++) DrawPartNode(s, fighter, part, PartCenter(fighter, part, cx, cy, shipHeight, explode));
            if (flash > 0f && partFlash / 5 == fighter)
            {
                Vector2 at = PartCenter(fighter, partFlash - partFlash / 5 * 5, cx, cy, shipHeight, explode);
                Color glow = accent; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(glow.a * flash) * .7f);
                s.Glow(at.x, at.y, OriginalMath.Binary32(flash * 220f), OriginalMath.Binary32(flash * 220f), glow, 0f);
                float rest = OriginalMath.Binary32(1f - flash);
                s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(rest * 90f) + 20f), new Color(1f, 1f, 1f, flash), 3f, 32, 0f, 360f);
                for (int ray = 0; ray < 10; ray++)
                {
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(rest * 120f) + OriginalMath.Binary32((float)(ray * 36)))) * .01745329238474369f);
                    float cos = OriginalMath.Cos(angle), sin = OriginalMath.Sin(angle);
                    s.Segment(new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(rest * 40f) + 24f)) * cos)), OriginalMath.Binary32(at.y + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(rest * 40f) + 24f)) * sin))),
                        new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(rest * 80f) + 36f)) * cos)), OriginalMath.Binary32(at.y + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(rest * 80f) + 36f)) * sin))), 3f, A(accent, flash));
                }
            }
            float done = Clamp01(OriginalMath.Binary32(OriginalMath.Binary32(Since(491) / -1.2f) + 1f));
            if (done > 0f && assembled)
            {
                float size = OriginalMath.Binary32((OriginalMath.Binary32(height + height)) * done);
                s.Glow(cx, cy, size, size, new Color(1f, 1f, 1f, OriginalMath.Binary32(done * .6f)), 0f);
                float alpha = Clamp01(OriginalMath.Binary32(done + done));
                Title(s, "组装完成！", cx, OriginalMath.Binary32(OriginalMath.Binary32(cy - 20f) + OriginalMath.Binary32((OriginalMath.Binary32(1f - done)) * -30f)), 2.4f, A(uiGoldHi, alpha), A(uiInk, alpha));
            }
            float cardY = OriginalMath.Binary32(OriginalMath.Binary32(height + y0) + 10f);
            DrawPartCard(s, fighter, hangarPart, OriginalMath.Binary32(slide + 24f), cardY, 492f, 160f);
            if (assembled) DrawStageTrack(s, fighter, OriginalMath.Binary32(slide + 24f), OriginalMath.Binary32(cardY + 168f), 492f, OriginalMath.Binary32(extra - 8f));
            float row = OriginalMath.Binary32(OriginalMath.Binary32(bottom - 52f) + 4f);
            Button(s, 333, OriginalMath.Binary32(slide + 24f), row, 236f, 44f, "战机补给", Economy.Data.fragments > 9 ? Btn.Green : Btn.Secondary, 1.4f, true, 2);
            if (!assembled)
            {
                bool ready = Economy.CanAssemble(fighter);
                Button(s, 331, OriginalMath.Binary32(slide + 272f), row, 244f, 44f, ready ? "组装战机" : "组装（差 " + (5 - Economy.Collected(fighter)) + " 件）",
                    ready ? Btn.Primary : Btn.Secondary, 1.35f, ready);
            }
            else if (Economy.ActiveFighter == fighter)
            {
                Plate(s, OriginalMath.Binary32(slide + 272f), row, 244f, 44f, SourceColor(4477658893534625792UL, 4568451461309383311UL),
                    SourceColor(4428659729061694669UL, 4568451461295625994UL), A(uiGold, .8f), 8f);
                Label(s, "出战中 · 下一局使用", OriginalMath.Binary32(slide + 394f), OriginalMath.Binary32(row + 13f), 1.2f, uiGoldHi);
            }
            else Button(s, 332, OriginalMath.Binary32(slide + 272f), row, 244f, 44f, "设为出战", Btn.Primary, 1.5f, true);
        }

        // f54681: area-weighted assembled/exploded atlas-piece centres.
        private Vector2 PartCenter(int fighter, int slot, float cx, float cy, float height, float explode)
        {
            float scale = OriginalMath.Binary32(height / FleetArt.Span[fighter]), area = 0f; Vector2 sum = Vector2.zero;
            for (int piece = FleetArt.First[fighter]; piece < FleetArt.First[fighter] + FleetArt.Count[fighter]; piece++)
            {
                if (FleetArt.Slot[piece] != slot) continue;
                RectInt rect = FleetArt.Rect[piece]; float weight = OriginalMath.Binary32((float)unchecked(rect.height * rect.width)); area  = OriginalMath.Binary32(area+(weight));
                Vector2 a = FleetArt.Assembled[piece], e = FleetArt.Exploded[piece];
                sum.x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(e.x - a.x)) * explode) + a.x))) + cx)) * weight) + sum.x);
                sum.y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(e.y - a.y)) * explode) + a.y))) + cy)) * weight) + sum.y);
            }
            return area > 0f ? new Vector2(OriginalMath.Binary32(sum.x / area), OriginalMath.Binary32(sum.y / area)) : new Vector2(cx, cy);
        }
    }
}
