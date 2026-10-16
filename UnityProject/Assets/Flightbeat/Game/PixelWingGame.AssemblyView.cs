using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54533: entry directions are f32 tables at memory 3647928/3647944.
        // f54861 static AssemblyOrder; f54533 reads this array at static +0.
        private static readonly int[] AssemblyOrder = { 4, 3, 1, 2, 0 };
        private static readonly float[] AssemblyFromX = { 0f, -1f, 1f, -.35f };
        private static readonly float[] AssemblyFromY = { -1f, -.25f, -.25f, 1f };

        private void DrawAssembly(PixelSurface s)
        {
            int fighter = assemblyFighter; if (fighter < 0) return;
            bool mapped = s.MapLayout; s.MapLayout = false; HitBlock(s);
            float screenHeight = ScreenLayout.Height, age = OriginalMath.Binary32(ui - assemblyAt), fade = Clamp01(OriginalMath.Binary32(age * 4f));
            float height = OriginalMath.WasmMin(OriginalMath.Binary32(screenHeight * .5f), 460f), cy = OriginalMath.Binary32(screenHeight * .46f);
            float scale = OriginalMath.Binary32(height / FleetArt.Span[fighter]); Color accent = FleetCatalog.FighterAccent[fighter];
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, screenHeight, SourceColor(4342767078957322863UL, 1022739087u, OriginalMath.Binary32(fade * .93f)));
            Color grid = accent; grid.a = OriginalMath.Binary32(OriginalMath.Binary32(grid.a * fade) * .07f);
            float scroll = OriginalMath.Binary32(age * 30f);
            for (float y = OriginalMath.Remainder(scroll, 32f); y < screenHeight; y  = OriginalMath.Binary32(y+(32f))) s.Rect(ScreenLayout.Left, y, ScreenLayout.Width, 1f, grid);
            grid.a = OriginalMath.Binary32(OriginalMath.Binary32(accent.a * fade) * .06f);
            for (int x = 6; x <= 518; x += 32) s.Rect(OriginalMath.Binary32((float)x), 0f, 1f, screenHeight, grid);
            Vector2 center = new Vector2(270f, cy);
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = OriginalMath.Binary32(height * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)ring) * .13f) + .42f))), offset = OriginalMath.Binary32((float)(ring * 30));
                Color tint = accent; tint.a = OriginalMath.Binary32(OriginalMath.Binary32(tint.a * fade) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)ring) * -.06f) + .25f)));
                s.Ring(center, radius, tint, 1.5f, 48, 0f, 360f);
                float rotation = (ring & 1) != 0 ? OriginalMath.Binary32(age * -55f) : OriginalMath.Binary32(age * 40f);
                tint.a = OriginalMath.Binary32(OriginalMath.Binary32(accent.a * fade) * .7f);
                for (int arc = 0; arc < 4; arc++) s.Ring(center, radius, tint, 3f, 4, OriginalMath.Binary32(OriginalMath.Binary32(rotation + OriginalMath.Binary32((float)(arc * 90))) + offset), 24f);
            }
            Color glow = accent; glow.a = OriginalMath.Binary32(OriginalMath.Binary32(glow.a * fade) * .14f);
            s.Glow(270f, cy, OriginalMath.Binary32(height * 1.4f), OriginalMath.Binary32(height * 1.2f), glow, 0f);
            float scanAge = OriginalMath.Binary32(age - 2.4100000858306885f);
            float scan = Clamp01(OriginalMath.Binary32(scanAge / .45f)), completedAge = OriginalMath.Binary32(scanAge - .45f);
            float flash = age >= 2.8600001335144043f ? Clamp01(OriginalMath.Binary32(1f - (OriginalMath.Binary32(completedAge + completedAge)))) : 0f;
            float show = Clamp01(OriginalMath.Binary32(completedAge / .4f));
            int first = FleetArt.First[fighter], end = first + FleetArt.Count[fighter], completed = 0;
            for (int sequence = 0; sequence < 5; sequence++)
            {
                int slot = AssemblyOrder[sequence]; float start = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)sequence) * .36f) + .55f);
                float progress = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age - start)) / .42f)); if (progress >= 1f) completed++;
                float eased = OriginalMath.Binary32(1f - OriginalMath.Pow(OriginalMath.Binary32(1f - progress), 3f));
                float impactAge = OriginalMath.Binary32(age - (OriginalMath.Binary32(start + .42f))); bool impact = impactAge >= 0f && impactAge < .35f;
                float impactFade = OriginalMath.Binary32(1f - OriginalMath.Binary32(impactAge / .35f));
                for (int piece = first; piece < end; piece++)
                {
                    if (FleetArt.Slot[piece] != slot) continue;
                    Vector2 offset = FleetArt.Assembled[piece];
                    float x = OriginalMath.Binary32(OriginalMath.Binary32(scale * offset.x) + 270f), y = OriginalMath.Binary32(cy + OriginalMath.Binary32(scale * offset.y));
                    if (progress <= 0f)
                    {
                        s.FleetSilhouette(piece, x, y, scale, SourceColor(4572054341899465523UL, 1065353216u,
                            OriginalMath.Binary32(fade * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 6f) + OriginalMath.Binary32((float)piece))) * .06f) + .12f)))));
                        continue;
                    }
                    float dx = slot > 3 ? .35f : AssemblyFromX[slot], dy = slot > 3 ? 1f : AssemblyFromY[slot];
                    float fromX = OriginalMath.Binary32(x + OriginalMath.Binary32(dx * 620f)), fromY = OriginalMath.Binary32(y + OriginalMath.Binary32(dy * 620f));
                    float travelX = OriginalMath.Binary32(x - fromX), travelY = OriginalMath.Binary32(y - fromY);
                    float px = OriginalMath.Binary32(fromX + OriginalMath.Binary32(eased * travelX)), py = OriginalMath.Binary32(fromY + OriginalMath.Binary32(eased * travelY));
                    if (progress < 1f)
                    {
                        // WAT has individually folded offsets; retain their exact bits.
                        float[] behind = { .07f, .14f, .21000000834465027f, .28f };
                        float[] opacity = { .096f, .072f, .048f, .024f };
                        for (int trail = 0; trail < 4; trail++)
                        {
                            float t = OriginalMath.WasmMax(OriginalMath.Binary32(eased - behind[trail]), 0f);
                            s.FleetSilhouette(piece, OriginalMath.Binary32(fromX + OriginalMath.Binary32(travelX * t)), OriginalMath.Binary32(fromY + OriginalMath.Binary32(travelY * t)), scale, A(accent, opacity[trail]));
                        }
                        s.GlowLine(new Vector2(px, py), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - eased)) * (OriginalMath.Binary32(dx * 140f))) + px), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - eased)) * (OriginalMath.Binary32(dy * 140f))) + py)), 40f, A(accent, .35f));
                    }
                    s.FleetPiece(piece, px, py, scale, Color.white);
                    if (!impact) continue;
                    Color spark = Mix(accent, Color.white, .5f); spark.a = OriginalMath.Binary32(spark.a * impactFade);
                    float outer = OriginalMath.Binary32(OriginalMath.Binary32(impactAge * 190f) + 22f), inner = OriginalMath.Binary32(OriginalMath.Binary32(impactAge * 120f) + 10f);
                    for (int ray = 0; ray < 8; ray++)
                    {
                        float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)(ray * 45 + piece * 17)) * .01745329238474369f);
                        float sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle);
                        s.Segment(new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(inner * cos)), OriginalMath.Binary32(y + OriginalMath.Binary32(inner * sin))), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(outer * cos)), OriginalMath.Binary32(y + OriginalMath.Binary32(outer * sin))), 3f, spark);
                    }
                    float alpha = OriginalMath.Binary32(impactFade * .6f);
                    s.FleetSilhouette(piece, x, y, scale, new Color(1f, 1f, 1f, alpha));
                    s.Glow(x, y, OriginalMath.Binary32(impactFade * 160f), OriginalMath.Binary32(impactFade * 160f), A(accent, alpha), 0f);
                }
            }
            if (assemblySoundStep < completed) { assemblySoundStep = completed; audioChip.Play(completed == 5 ? "maxpower" : "land"); }
            if (scan > 0f && scan < 1f)
            {
                float y = OriginalMath.Binary32(OriginalMath.Binary32(cy + OriginalMath.Binary32(height * .5f)) + OriginalMath.Binary32(OriginalMath.Binary32(height * scan) * -1.1f));
                for (int piece = first; piece < end; piece++)
                {
                    Vector2 offset = FleetArt.Assembled[piece]; float py = OriginalMath.Binary32(cy + OriginalMath.Binary32(scale * offset.y));
                    float distance = OriginalMath.Binary32(Mathf.Abs(OriginalMath.Binary32(py - y)) / (OriginalMath.Binary32(height * .25f)));
                    if (distance < 1f)
                    {
                        Color tint = accent; tint.a = OriginalMath.Binary32(OriginalMath.Binary32(tint.a * (OriginalMath.Binary32(1f - distance))) * .55f);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(OriginalMath.Binary32(scale * offset.x) + 270f), py, scale, tint);
                    }
                }
                s.Rect(OriginalMath.Binary32(270f - OriginalMath.Binary32(height * .5f)), y, height, 2f, SourceColor(4575657222473777152UL, 4561245704525185024UL));
                s.Glow(270f, y, OriginalMath.Binary32(height * 1.2f), 40f, A(accent, .5f), 0f);
            }
            if (age >= 2.6100001335144043f)
            {
                Vector2 tip = PartTip(fighter, 4, 270f, cy, height, 0f, true);
                float t = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(scanAge - .2f)) / .3f)), flicker = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 40f)) * .15f) + .85f);
                s.Glow(tip.x, OriginalMath.Binary32(OriginalMath.Binary32(height * .08f) + tip.y), OriginalMath.Binary32(OriginalMath.Binary32(height * .18f) * t), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .4f) * t) * flicker),
                    SourceColor(4546834186576592896UL, 4554039943383272653UL), 0f);
                s.Glow(tip.x, OriginalMath.Binary32(OriginalMath.Binary32(height * .04f) + tip.y), OriginalMath.Binary32(OriginalMath.Binary32(height * .07f) * t), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * .2f) * t) * flicker),
                    SourceColor(4575657222473777152UL, 4568451461349480858UL), 0f);
            }
            if (show > 0f) DrawFighterArt(s, fighter, 270f, cy, height, 0f, -1, show, false, true);
            if (flash > 0f)
            {
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, screenHeight, new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(flash * flash) * .7f)));
                float rest = OriginalMath.Binary32(1f - flash);
                for (int ring = 0; ring < 3; ring++)
                {
                    float t = OriginalMath.Binary32(rest + (ring == 0 ? 0f : ring == 1 ? .12f : .24f));
                    if (t < 1f) s.Ring(center, OriginalMath.Binary32(OriginalMath.Binary32(t * 420f) + 40f), A(ring == 1 ? Color.white : accent, OriginalMath.Binary32(1f - t)), OriginalMath.Binary32((float)(6 - ring * 2)), 48, 0f, 360f);
                }
            }
            if (age >= 2.8600001335144043f)
            {
                float t = OriginalMath.Binary32(Clamp01(Clamp01(OriginalMath.Binary32(completedAge / .35f))) - 1f);
                float bounce = OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * 2.2f))) + (OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(t * (OriginalMath.Binary32(t * 3.2f))))) + 1f)));
                for (int star = 0; star < 14; star++)
                {
                    float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 7f) + OriginalMath.Binary32((float)star))) * .5f) + .5f);
                    float radius = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui + ui) + OriginalMath.Binary32((float)star))) * .05f) + .55f);
                    float angle = OriginalMath.Binary32((OriginalMath.Binary32(scroll + OriginalMath.Binary32(OriginalMath.Binary32((float)star) * 25.700000762939453f))) * .01745329238474369f);
                    Vector2 at = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * OriginalMath.Cos(angle)) * radius) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(OriginalMath.Binary32(height * OriginalMath.Sin(angle)) * radius)));
                    s.SpriteAt(9, at, OriginalMath.Binary32(OriginalMath.Binary32(pulse * 10f) + 8f), SourceColor(4567010310845628416UL, 1056964608u, OriginalMath.Binary32(show * pulse)), 0f);
                }
                float top = OriginalMath.Binary32(cy - OriginalMath.Binary32(height * .5f));
                Title(s, FleetCatalog.FighterNames[fighter], 270f, OriginalMath.Binary32(top - 78f), OriginalMath.Binary32(OriginalMath.WasmMax(bounce, .2f) * 3.2f),
                    Color.white, SourceColor(4417130516420807885UL, 4575657221408423936UL));
                Label(s, "组装完成！新战机加入舰队", 270f, OriginalMath.Binary32(top - 22f), 1.4f, A(uiGoldHi, show));
                s.TextF(FleetCatalog.FighterStyle[fighter], 270f, OriginalMath.Binary32(OriginalMath.Binary32(cy + OriginalMath.Binary32(height * .5f)) + 14f), 1.1f, A(accent, show), 0);
            }
            else Label(s, age < .55f ? "部件就位 · 开始组装" : "组装中 " + assemblySoundStep + "/5", 270f, OriginalMath.Binary32(OriginalMath.Binary32(cy + OriginalMath.Binary32(height * -.5f)) - 40f), 1.4f, A(accent, fade));
            if (age >= 3.31f)
            {
                float a = OriginalMath.Binary32(screenHeight - 150f), b = OriginalMath.Binary32(OriginalMath.Binary32(cy + OriginalMath.Binary32(height * .5f)) + 56f), y = a < b ? a : b;
                Button(s, 721, 110f, y, 320f, 56f, "设为出战", Btn.Primary, 1.9f, true);
                Button(s, 720, 170f, OriginalMath.Binary32(y + 66f), 200f, 42f, "返回机库", Btn.Ghost, 1.3f, true);
            }
            else HitBox(s, 720, ScreenLayout.Left, 0f, ScreenLayout.Width, screenHeight);
            s.MapLayout = mapped;
        }
    }
}
