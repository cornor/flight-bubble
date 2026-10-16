using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54752: assembled authored fighter parts, charging particles and rings.
        private void DrawGiantPlane(PixelSurface s, Finale finale, float age, float after)
        {
            float assemble = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -.5f)) / .65f));
            if (assemble <= 0f) return;
            float lift = FinaleLift(age, after), alpha = OriginalMath.Binary32((OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(after / 1.1f)))) * Clamp01(OriginalMath.Binary32(assemble / .35f)));
            float shake = finale.Sweeping ? OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 70f)) * 1.6f) : 0f;
            float liftedY = OriginalMath.Binary32(694f - lift), x = OriginalMath.Binary32(shake + 270f), y = liftedY;
            bool gale = age >= 1.65f && finale.Mode == 1;
            if (gale)
            {
                float wipe = OriginalMath.Binary32((age < 3.15f ? finale.WipeY : OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) * -1500f) + 40f)) + 75f);
                y = wipe > liftedY ? liftedY : wipe;
                shake = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 90f)) * 2.5f); x = OriginalMath.Binary32(shake + 270f);
                s.Glow(x, y, 225f, 450f, SourceColor(4572054341908692992UL, 1065353216u, OriginalMath.Binary32(alpha * .45f)), 0f);
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(clock * 1.2f) + finaleOrbitPhases[i]);
                s.Glow(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Cos(angle) * 40f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Sin(angle) * 30f)), 275f, 275f,
                    A(finaleRainbow[i], OriginalMath.Binary32(alpha * .14f)), 0f);
            }
            s.Glow(x, y, 375f, 325f, SourceColor(4561245704525185024UL, 1053609165u, OriginalMath.Binary32(alpha * .28f)), 0f);
            float q = OriginalMath.Binary32(1f - Clamp01(assemble)); q = OriginalMath.Binary32(OriginalMath.Binary32(q * (OriginalMath.Binary32(q * q))) + -1f);
            DrawFighterArt(s, Economy.ActiveFighter, x, y, 250f, OriginalMath.Binary32((OriginalMath.Binary32(q + 1f)) * .9f), -1, alpha, assemble < .45f, true);
            if (assemble < 1f)
            {
                float size = OriginalMath.Binary32(OriginalMath.Binary32(assemble * 250f) * 1.2f);
                s.Glow(x, y, size, size, new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - assemble)) * .5f) * alpha)), 0f);
            }
            if (!(age >= 1.15f) || !(age < 3.15f && !gale)) return;
            float charge = OriginalMath.Binary32(age + -1.15f); charge = Clamp01(OriginalMath.Binary32(charge + charge));
            float noseX = OriginalMath.Binary32(shake + 270f), noseY = OriginalMath.Binary32(liftedY + -122.5f);
            for (int i = 0; i < 14; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.4f), sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle);
                float cycle = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 14f) + OriginalMath.Binary32(clock * 1.6f)), 1f), radius = OriginalMath.Binary32((OriginalMath.Binary32(1f - cycle)) * 110f);
                Vector2 at = new Vector2(OriginalMath.Binary32(noseX + OriginalMath.Binary32(cos * radius)), OriginalMath.Binary32(noseY + OriginalMath.Binary32(sin * radius)));
                Color c = finaleRainbow[i % 7]; c.a = OriginalMath.Binary32(OriginalMath.Binary32(c.a * alpha) * cycle);
                s.Rect(OriginalMath.Binary32(at.x + -2f), OriginalMath.Binary32(at.y + -2f), 4f, 4f, c);
                if (age < 1.65f)
                {
                    c = finaleRainbow[i % 7]; c.a = OriginalMath.Binary32(OriginalMath.Binary32(c.a * (OriginalMath.Binary32(alpha * .6f))) * cycle);
                    s.Segment(at, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(cos * 10f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(sin * 10f) + at.y)), 2f, c);
                }
            }
            float core = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(charge * 30f) + 12f) + (finale.Sweeping ? OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 50f)) * 6f) : 0f));
            s.Glow(noseX, noseY, OriginalMath.Binary32(core * 4f), OriginalMath.Binary32(core * 4f), A(RainbowAt(OriginalMath.Binary32(clock * .8f)), OriginalMath.Binary32(alpha * .6f)), 0f);
            s.Glow(noseX, noseY, OriginalMath.Binary32(core + core), OriginalMath.Binary32(core + core), new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .85f)), 0f);
            float indexValue = OriginalMath.Binary32(clock * 10f);
            int index = Math.Abs(indexValue) < 2147483648f ? (int)indexValue : int.MinValue;
            Vector2 nose = new Vector2(noseX, noseY);
            s.Ring(nose, OriginalMath.Binary32(core * .6f), A(finaleRainbow[index % 7], OriginalMath.Binary32(alpha * .8f)), 2f, 24, OriginalMath.Binary32(clock * 200f), 300f);
            s.Ring(nose, OriginalMath.Binary32(core * .9500000476837158f), A(finaleRainbow[unchecked(index + 2) % 7], OriginalMath.Binary32(alpha * .8f)), 2f, 24, OriginalMath.Binary32(clock * -200f), 300f);
            s.Ring(nose, OriginalMath.Binary32(core * 1.3f), A(finaleRainbow[unchecked(index + 4) % 7], OriginalMath.Binary32(alpha * .8f)), 2f, 24, OriginalMath.Binary32(clock * 200f), 300f);
        }
    }
}
