using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54599. Sprite 12 is the original pod art used by this animation.
        private void DrawGacha(PixelSurface s)
        {
            Backdrop(s, .9f); HitBlock(s);
            float age = gachaAge, height = ScreenLayout.Height, cy = OriginalMath.Binary32(height * .44f);
            Color tint = partResults != null ? gachaNew ? uiGold : FleetCatalog.FighterAccent[partResults[0].index / 5] : FleetCatalog.QualityColors[gachaBest];
            if (!gachaRevealed)
            {
                float travel = OutCubic(OriginalMath.Binary32(age / 1.1f)); travel = travel < 0f ? 0f : OriginalMath.WasmMin(travel, 1f);
                float podY = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(cy + 80f)) * travel) - 80f), podX = age > 1.1f ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(age * 80f)) * 3f) + 270f) : 270f;
                if (age < 1.1f)
                {
                    Color tail = SourceColor(4546834186576592896UL, 4525216907486088397UL);
                    s.GlowLine(new Vector2(270f, OriginalMath.Binary32(podY + 60f)), new Vector2(256f, OriginalMath.Binary32(podY + 160f)), 30f, tail);
                    s.GlowLine(new Vector2(270f, OriginalMath.Binary32(podY + 60f)), new Vector2(270f, OriginalMath.Binary32(podY + 160f)), 30f, tail);
                    s.GlowLine(new Vector2(270f, OriginalMath.Binary32(podY + 60f)), new Vector2(284f, OriginalMath.Binary32(podY + 160f)), 30f, tail);
                }
                // f54599 folds 1.45f-1.1f to 0x1.666668p-2.
                float charge = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age - 1.1f)) / .3500000238418579f));
                if (!(age > 1.2f)) tint = new Color(.6f, .9f, 1f, 1f);
                float rotation = OriginalMath.Binary32(age * 90f), length = OriginalMath.Binary32(OriginalMath.Binary32(charge * 300f) + 120f), width = OriginalMath.Binary32(OriginalMath.Binary32(charge * 40f) + 30f);
                Color ray = tint; ray.a = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(charge * .3f) + .12f)) * tint.a);
                for (int i = 0; i < 10; i++)
                {
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(rotation + OriginalMath.Binary32((float)(i * 36)))) * .01745329238474369f);
                    s.GlowLine(new Vector2(270f, cy), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(length * OriginalMath.Cos(angle)) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(length * OriginalMath.Sin(angle)))), width, ray);
                }
                Color glow = tint; glow.a = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(charge * .4f) + .3f)) * tint.a);
                float glowSize = OriginalMath.Binary32(OriginalMath.Binary32(charge * 160f) + 200f);
                s.Glow(270f, podY, glowSize, glowSize, glow, 0f);
                float scale = OriginalMath.Binary32(OriginalMath.Binary32(charge * .12f) + 1f), half = OriginalMath.Binary32(scale * 64f), size = OriginalMath.Binary32(scale * 128f);
                s.Sprite(12, OriginalMath.Binary32(podX - half), OriginalMath.Binary32(podY - half), size, size, Color.white, 0f);
                float y = OriginalMath.Binary32(height * .72f);
                Label(s, supplyPool != 0 ? "装备补给投放中…" : "战机补给投放中…", 270f, y, 1.4f, A(uiDim, .8f));
                Label(s, "点击跳过", 270f, OriginalMath.Binary32(y + 30f), 1.05f, A(uiDim, .5f));
                return;
            }
            float reveal = OriginalMath.Binary32(age - 1.45f);
            if (reveal < .25f)
            {
                bool mapped = s.MapLayout; s.MapLayout = false;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, height, new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(reveal * -4f) + 1f))); s.MapLayout = mapped;
            }
            float beamWidth = gachaBest > 2 ? 70f : 46f, spin = OriginalMath.Binary32(reveal * (gachaBest > 3 ? 60f : 30f));
            Color beam = tint; beam.a = OriginalMath.Binary32(tint.a * (gachaBest > 2 ? .2f : .12f));
            for (int i = 0; i < 16; i++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(spin + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 22.5f))) * .01745329238474369f);
                s.GlowLine(new Vector2(270f, cy), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * 460f) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(OriginalMath.Sin(angle) * 460f))), beamWidth, beam);
            }
            if (gachaBest >= 4)
                for (int i = 0; i < 14; i++)
                {
                    float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(reveal * 8f) + OriginalMath.Binary32((float)i))) * .5f) + .5f), angle = OriginalMath.Binary32(OriginalMath.Binary32(reveal * 1.3f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.4f));
                    Color star = SourceColor(4568451461351997440UL, 1056964608u, pulse);
                    s.SpriteAt(9, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * OriginalMath.Binary32((float)(i * 14 + 90))) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(OriginalMath.Sin(angle) * OriginalMath.Binary32((float)(i * 10 + 70))))), OriginalMath.Binary32(OriginalMath.Binary32(pulse * 10f) + 10f), star, 0f);
                }
            float resultBottom;
            if (ResultCount == 1) resultBottom = DrawResultCard(s, 0, 270f, cy, reveal, false);
            else
            {
                resultBottom = OriginalMath.Binary32(OriginalMath.Binary32(cy + 150f) + 8f); float gridTop = OriginalMath.Binary32(OriginalMath.Binary32(cy - 150f) - 8f);
                Label(s, (supplyPool != 0 ? "装备补给 ×" : "战机补给 ×") + ResultCount, 270f, OriginalMath.Binary32(gridTop - 44f), 1.6f, Color.white);
                float rowAge = OriginalMath.Binary32(reveal - .25f);
                for (int i = 0; i < ResultCount; i++) DrawResultMini(s, i, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(i % 5)) * 98f) + 28f), OriginalMath.Binary32(gridTop + OriginalMath.Binary32(OriginalMath.Binary32((float)(i / 5)) * 166f)), 92f, 150f, OriginalMath.Binary32(rowAge + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.09f)));
            }
            if (!(RevealDone < reveal)) return;
            int price = lastDrawCount < 10 ? 10 : 90; bool canDraw = price <= Economy.Data.fragments;
            float below = OriginalMath.Binary32(resultBottom + 24f), limit = OriginalMath.Binary32(height - 200f), buttonsY = below < limit ? below : limit;
            Button(s, 202, 60f, buttonsY, 200f, 56f, supplyPool != 0 ? "去装配" : "去机库", Btn.Primary, 1.7f, true);
            Button(s, 201, 280f, buttonsY, 200f, 56f, lastDrawCount > 9 ? "再十连" : "再抽一次", canDraw ? Btn.Green : Btn.Secondary, 1.6f, canDraw);
            s.DeckArt(2, 334f, OriginalMath.Binary32(buttonsY + 60f), 22f, 22f, Color.white, true);
            s.Text(price.ToString(), 358f, OriginalMath.Binary32(buttonsY + 66f), 1f, uiCyan, false);
            Button(s, 200, 200f, OriginalMath.Binary32(buttonsY + 92f), 140f, 40f, "关闭", Btn.Ghost, 1.2f, true);
        }
        // f54672, original procedural pod helper (the shipped reveal uses sprite 12).
        private void DrawPod(PixelSurface s, float cx, float cy, float scale, float time)
        {
            float width = OriginalMath.Binary32(scale * 26f), height = OriginalMath.Binary32(scale * 34f), left = OriginalMath.Binary32(cx + OriginalMath.Binary32(width * -.5f)), half = OriginalMath.Binary32(height * .4f), top = OriginalMath.Binary32(cy - half);
            Color body = SourceColor(4566897280188602071UL, 4575657222472263913UL);
            s.Rect(left, top, width, OriginalMath.Binary32(height * .8f), body);
            s.Rect(OriginalMath.Binary32(cx - OriginalMath.Binary32(width * .35f)), OriginalMath.Binary32(cy + OriginalMath.Binary32(height * -.55f)), OriginalMath.Binary32(width * .7f), OriginalMath.Binary32(height * .2f), body);
            s.Rect(left, OriginalMath.Binary32(cy + OriginalMath.Binary32(height * -.1f)), width, OriginalMath.Binary32(height * .12f), SourceColor(4554463813725847552UL, 4575657222453381321UL));
            s.Rect(OriginalMath.Binary32(cx - OriginalMath.Binary32(width * .2f)), OriginalMath.Binary32(cy - OriginalMath.Binary32(height * .35f)), OriginalMath.Binary32(width * .4f), OriginalMath.Binary32(height * .18f), SourceColor(4565766965044753589UL, 4575657222473777152UL));
            Color feet = SourceColor(4521825962093229237UL, 4575657222463085789UL);
            s.Rect(left, OriginalMath.Binary32(half + cy), OriginalMath.Binary32(width * .25f), OriginalMath.Binary32(height * .14f), feet);
            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(width * .25f) + cx), OriginalMath.Binary32(half + cy), OriginalMath.Binary32(width * .25f), OriginalMath.Binary32(height * .14f), feet);
            s.Border(left, top, width, OriginalMath.Binary32(height * .8f), SourceColor(4485797165064560801UL, 4575657222456013041UL), OriginalMath.WasmMax(scale, 1f));
        }
    }
}
