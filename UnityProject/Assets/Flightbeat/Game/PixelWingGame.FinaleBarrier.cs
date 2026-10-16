using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54745. Charge draws a progressively completed ellipse; sweep shrinks it.
        private void DrawBarrier(PixelSurface s, Finale finale, float age, float after)
        {
            Vector2 at = new Vector2(finale.CX, finale.CY);
            int stage = feedback.Stage;
            Color accent = FleetCatalog.FighterAccent[4];
            float charge = OriginalMath.Binary32(age + -1.15f); charge = OriginalMath.Binary32(charge + charge);
            charge = charge < 0f ? 0f : OriginalMath.WasmMin(charge, 1f);
            bool charging = age < 1.65f;
            float alpha, rx, ry;
            if (charging)
            {
                if (charge <= 0f) return;
                alpha = charge; rx = OriginalMath.Binary32(finale.RX * 1.08f); ry = OriginalMath.Binary32(finale.RY * 1.08f);
            }
            else
            {
                alpha = FinaleAttackFade(age, after); if (alpha <= 0f) return;
                float scale = age < 3.15f ? finale.BarrierScale : 0f;
                rx = OriginalMath.Binary32(scale * finale.RX); ry = OriginalMath.Binary32(scale * finale.RY);
                if (!(scale > .02f)) { DrawBarrierEnd(s, at, age); return; }
            }
            s.Glow(at.x, at.y, OriginalMath.Binary32(rx * 2.2f), OriginalMath.Binary32(ry * 2.2f), SourceColor(4575657222466227405UL, 1061662228u, OriginalMath.Binary32(alpha * .1f)), 0f);
            float strength = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)stage) * .07f) + .75f), limit = OriginalMath.Binary32(charge * 40f);
            for (int i = 0; i < 40; i++)
            {
                if (charging && limit < OriginalMath.Binary32((float)i)) break;
                float wave = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 8f) - OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .6f))) * .4f) + .6f);
                float blend = OriginalMath.Binary32(wave * .4f); blend = blend < 0f ? 0f : OriginalMath.WasmMin(blend, 1f);
                Color color = new Color(OriginalMath.Binary32(OriginalMath.Binary32(blend * .45f) + .55f), OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(blend * .2200000286102295f) + .78f), OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f));
                float a = OriginalMath.Binary32(OriginalMath.Binary32((float)i) * FinalePi); a = OriginalMath.Binary32((OriginalMath.Binary32(a + a)) / 40f);
                float b = OriginalMath.Binary32(OriginalMath.Binary32((float)(i + 1)) * FinalePi); b = OriginalMath.Binary32((OriginalMath.Binary32(b + b)) / 40f);
                Vector2 from = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(rx * OriginalMath.Cos(a))), OriginalMath.Binary32(at.y + OriginalMath.Binary32(ry * OriginalMath.Sin(a))));
                Vector2 to = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(rx * OriginalMath.Cos(b))), OriginalMath.Binary32(at.y + OriginalMath.Binary32(ry * OriginalMath.Sin(b))));
                s.Segment(from, to, OriginalMath.Binary32(strength * ((i & 1) != 0 ? 2.5f : 4f)), A(color, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(wave * .3f) + .7f)))));
                if ((i & 3) == 0)
                {
                    s.Glow(from.x, from.y, OriginalMath.Binary32(strength * 26f), OriginalMath.Binary32(strength * 26f), A(accent, OriginalMath.Binary32(alpha * .55f)), 0f);
                    s.Rect(OriginalMath.Binary32(from.x + -2f), OriginalMath.Binary32(from.y + -2f), 4f, 4f, new Color(1f, 1f, 1f, alpha));
                }
            }
            Vector2 nose = FinaleNose(FinaleLift(age, after));
            for (int i = 0; i < 3; i++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(clock * 20f) + OriginalMath.Binary32((float)(i * 60)))) * FinaleRadians);
                float dx = OriginalMath.Binary32(rx * OriginalMath.Cos(angle)), dy = OriginalMath.Binary32(ry * OriginalMath.Sin(angle));
                s.Segment(new Vector2(OriginalMath.Binary32(at.x + dx), OriginalMath.Binary32(at.y + dy)), new Vector2(OriginalMath.Binary32(at.x - dx), OriginalMath.Binary32(at.y - dy)), 1f,
                    SourceColor(4575657222466227405UL, 1061662228u, OriginalMath.Binary32(alpha * .18f)));
            }
            for (int i = 0; i < 2; i++)
            {
                Vector2 to = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(rx * (i == 0 ? -.7f : .7f))), OriginalMath.Binary32(at.y + OriginalMath.Binary32(ry * .7f)));
                s.GlowLine(nose, to, 10f, A(accent, OriginalMath.Binary32(alpha * .35f)));
                s.Segment(nose, to, 1.5f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .6f)));
            }
            DrawBarrierEnd(s, at, age);
        }
        private void DrawBarrierEnd(PixelSurface s, Vector2 at, float age)
        {
            if (!(age >= 3.15f)) return;
            float t = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) / .4f)), fade = OriginalMath.Binary32(1f - t), size = OriginalMath.Binary32(OriginalMath.Binary32(t * 260f) + 40f);
            s.Glow(at.x, at.y, size, size, new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .9f)), 0f);
            s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(t * 300f) + 20f), SourceColor(4575657222466227405UL, 1061662228u, fade), 4f, 48, 0f, 360f);
        }
    }
}
