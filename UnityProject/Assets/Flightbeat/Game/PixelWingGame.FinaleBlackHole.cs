using System;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54744. Launch the singularity, then layer far disk, core and near disk.
        private void DrawBlackHole(PixelSurface s, Finale finale, float age, float after)
        {
            Vector2 at = new Vector2(finale.CX, finale.CY);
            int stage = feedback.Stage;
            Color accent = FleetCatalog.FighterAccent[2];
            float charge = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age + -1.15f) + -.2f)) / .3f);
            if (age < 1.65f)
            {
                charge = charge < 0f ? 0f : OriginalMath.WasmMin(charge, 1f);
                if (charge <= 0f) return;
                Vector2 nose = FinaleNose(FinaleLift(age, after));
                float t = OutCubic(charge); t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
                Vector2 head = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(t * (OriginalMath.Binary32(at.x + -270f))) + 270f), OriginalMath.Binary32(nose.y + OriginalMath.Binary32(t * (OriginalMath.Binary32(at.y - nose.y)))));
                s.GlowLine(nose, head, 26f, A(accent, .35f));
                s.Glow(head.x, head.y, 60f, 60f, SourceColor(1028443341UL, 4561245704496663757UL), 0f);
                for (int i = 0; i < 3; i++)
                    s.Ring(head, 12f, Mix(accent, Color.white, .4f), 2.5f, 6, OriginalMath.Binary32(OriginalMath.Binary32(clock * 400f) + OriginalMath.Binary32((float)(i * 120))), 70f);
                return;
            }
            float alpha = FinaleAttackFade(age, after); if (alpha <= 0f) return;
            float grow = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -1.65f)) / .35f)), collapse = age < 3.15f ? 0f : Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(age + -3.15f)) / .3f));
            float remaining = OriginalMath.Binary32(1f - collapse);
            float core = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)stage) * .07f) + .75f)) * (OriginalMath.Binary32(remaining * (OriginalMath.Binary32(OriginalMath.Binary32(OutCubic(grow) * 14f) + 20f)))));
            float reach = age < 3.15f ? finale.HoleRadius : finale.HoleReach;
            for (int i = 0; i < 14; i++)
                s.Ring(at, OriginalMath.Binary32(reach + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 6f) + OriginalMath.Binary32((float)i))) * 6f)),
                    SourceColor(4543231306011508736UL, 1065353216u, OriginalMath.Binary32(alpha * .45f)), 2f, 3,
                    OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 25.714284896850586f) + OriginalMath.Binary32(clock * 80f)), 14f);
            s.Glow(at.x, at.y, OriginalMath.Binary32(core * 7f), OriginalMath.Binary32(core * 5.5f), SourceColor(4464688526094080410UL, 1057803469u, OriginalMath.Binary32(alpha * .3f)), 0f);
            float outer = OriginalMath.Binary32(reach * .9f), inner = OriginalMath.Binary32(core * 1.3f);
            for (int arm = 0; arm < 4; arm++)
                for (int i = 0; i < 10; i++)
                {
                    float t = OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 10f);
                    float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * 260f) + OriginalMath.Binary32((float)(arm * 90))) - OriginalMath.Binary32(clock * 260f));
                    Vector2 rotated = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(inner + OriginalMath.Binary32(outer * (OriginalMath.Binary32(1f - t)))), 0f), angle);
                    float blend = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f), one = OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f);
                    Color color = new Color(one, OriginalMath.Binary32(OriginalMath.Binary32(blend * .3999999761581421f) + .6f), OriginalMath.Binary32(OriginalMath.Binary32(blend * .8f) + .2f),
                        OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(t * .6f) + .4f))) * one));
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(at.x + rotated.x) + -1.5f), OriginalMath.Binary32(OriginalMath.Binary32(at.y + OriginalMath.Binary32(rotated.y * .8f)) + -1.5f), 3f, 3f, color);
                }
            if (core < 1f) return;
            DiskHalf(s, at, core, alpha, true);
            Color dark = SourceColor(0UL, 1014350479u, OriginalMath.Binary32(alpha * .92f));
            s.Glow(at.x, at.y, OriginalMath.Binary32(core * 2.8f), OriginalMath.Binary32(core * 2.8f), dark, 0f);
            s.Glow(at.x, at.y, OriginalMath.Binary32(core * 2.25f), OriginalMath.Binary32(core * 2.25f), dark, 0f);
            s.Glow(at.x, at.y, OriginalMath.Binary32(core * 1.6999999284744263f), OriginalMath.Binary32(core * 1.6999999284744263f), dark, 0f);
            for (int i = 0; i < 18; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * FinalePi) / 18f) + FinalePi);
                float x = OriginalMath.Binary32(core * OriginalMath.Cos(angle)), y = OriginalMath.Binary32(core * OriginalMath.Sin(angle));
                float shade = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(OriginalMath.Binary32(OriginalMath.Binary32(angle + 1.5707963705062866f) + .6f)) * .45f) + .55f);
                shade = shade < 0f ? 0f : OriginalMath.WasmMin(shade, 1f);
                Color color = new Color(OriginalMath.Binary32(OriginalMath.Binary32(shade * 0f) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(shade * .45f) + .5f), OriginalMath.Binary32(OriginalMath.Binary32(shade * .68f) + .12f), OriginalMath.Binary32(OriginalMath.Binary32(shade * 0f) + 1f));
                float nx = OriginalMath.Binary32(core * OriginalMath.Cos(OriginalMath.Binary32(angle + .1745329350233078f))), ny = OriginalMath.Binary32(core * OriginalMath.Sin(OriginalMath.Binary32(angle + .1745329350233078f)));
                Vector2 from = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(x * 1.45f)), OriginalMath.Binary32(at.y + OriginalMath.Binary32(y * 1.3f))), to = new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(nx * 1.45f)), OriginalMath.Binary32(at.y + OriginalMath.Binary32(ny * 1.3f)));
                s.GlowLine(from, to, OriginalMath.Binary32(core * .55f), A(color, OriginalMath.Binary32(alpha * .35f)));
                s.Segment(from, to, OriginalMath.Binary32(OriginalMath.Binary32(core * .16f) + 1f), A(color, OriginalMath.Binary32(alpha * .9f)));
                s.Segment(new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(x * 1.2f)), OriginalMath.Binary32(at.y - OriginalMath.Binary32(y * 1.05f))), new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(nx * 1.2f)), OriginalMath.Binary32(at.y - OriginalMath.Binary32(ny * 1.05f))),
                    1.5f, A(color, OriginalMath.Binary32(alpha * .45f)));
            }
            for (int i = 0; i < 24; i++)
            {
                float angle = OriginalMath.Binary32((float)(i * 15)), raw = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(OriginalMath.Binary32((OriginalMath.Binary32(angle + 180f)) * FinaleRadians)) * .5f) + .5f);
                float shade = raw < 0f ? 0f : OriginalMath.WasmMin(raw, 1f), one = OriginalMath.Binary32(OriginalMath.Binary32(shade * 0f) + 1f);
                Color color = new Color(one, OriginalMath.Binary32(OriginalMath.Binary32(shade * .19999998807907104f) + .8f), OriginalMath.Binary32(OriginalMath.Binary32(shade * .45f) + .55f),
                    OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(raw * .5f) + .5f))) * one));
                s.Ring(at, OriginalMath.Binary32(core * 1.02f), color, 1.5f, 2, angle, 15f);
            }
            DiskHalf(s, at, core, alpha, false);
            if (collapse > 0f)
            {
                s.Glow(at.x, at.y, OriginalMath.Binary32(collapse * 300f), OriginalMath.Binary32(collapse * 300f), new Color(1f, 1f, 1f, OriginalMath.Binary32(remaining * .9f)), 0f);
                s.Ring(at, OriginalMath.Binary32(OriginalMath.Binary32(collapse * 280f) + 40f), SourceColor(4554039943400888730UL, 1065353216u, remaining), 4f, 40, 0f, 360f);
            }
        }
        // f54756. Two half-disks and angle-selected white streaks retain draw order.
        private void DiskHalf(PixelSurface s, Vector2 at, float core, float fade, bool far)
        {
            float start = far ? FinalePi : 0f, brightAlpha = OriginalMath.Binary32((far ? .5f : .8f) * fade);
            float lineAlpha = OriginalMath.Binary32((far ? .6f : .85f) * fade), glowAlpha = OriginalMath.Binary32((far ? .22f : .3f) * fade);
            float ry = OriginalMath.Binary32(core * .62f), rx = OriginalMath.Binary32(core * 3.3f);
            for (int i = 0; i < 22; i++)
            {
                float angle = OriginalMath.Binary32(start + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * FinalePi) / 22f)), next = OriginalMath.Binary32(angle + .14279967546463013f);
                float cos = OriginalMath.Cos(angle);
                Vector2 from = new Vector2(OriginalMath.Binary32(rx * cos), OriginalMath.Binary32(ry * OriginalMath.Sin(angle)));
                Vector2 to = new Vector2(OriginalMath.Binary32(rx * OriginalMath.Cos(next)), OriginalMath.Binary32(ry * OriginalMath.Sin(next)));
                Vector2 a = CombatFeedback.Rotate(from, -8f) + at, b = CombatFeedback.Rotate(to, -8f) + at;
                float shade = OriginalMath.Binary32(.5f - OriginalMath.Binary32(cos * .5f)); shade = shade < 0f ? 0f : OriginalMath.WasmMin(shade, 1f);
                Color color = new Color(OriginalMath.Binary32(OriginalMath.Binary32(shade * .050000011920928955f) + .95f), OriginalMath.Binary32(OriginalMath.Binary32(shade * .61f) + .32f),
                    OriginalMath.Binary32(OriginalMath.Binary32(shade * .64000004529953f) + .08f), OriginalMath.Binary32(OriginalMath.Binary32(shade * 0f) + 1f));
                s.GlowLine(a, b, OriginalMath.Binary32(core * 1.1f), A(color, glowAlpha));
                s.Segment(a, b, OriginalMath.Binary32(core * .34f), A(color, lineAlpha));
                a = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(from.x * .62f), OriginalMath.Binary32(from.y * .62f)), -8f) + at;
                b = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(to.x * .62f), OriginalMath.Binary32(to.y * .62f)), -8f) + at;
                s.Segment(a, b, OriginalMath.Binary32(OriginalMath.Binary32(core * .16f) + 1f), A(Mix(color, Color.white, .5f), brightAlpha));
            }
            for (int i = 0; i < 8; i++)
            {
                float angle = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * FinalePi) * .25f) + OriginalMath.Binary32(clock * 2.4f)), 6.2831854820251465f);
                if (far != (angle >= FinalePi)) continue;
                float scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(i * 37 % 10)) * .25f) / 10f) + .75f);
                Vector2 head = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(scale * (OriginalMath.Binary32(rx * OriginalMath.Cos(angle)))), OriginalMath.Binary32(scale * (OriginalMath.Binary32(ry * OriginalMath.Sin(angle))))), -8f) + at;
                Vector2 tail = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(scale * (OriginalMath.Binary32(rx * OriginalMath.Cos(OriginalMath.Binary32(angle + -.25f))))), OriginalMath.Binary32(scale * (OriginalMath.Binary32(ry * OriginalMath.Sin(OriginalMath.Binary32(angle + -.25f)))))), -8f) + at;
                s.Segment(tail, head, 2.5f, new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .85f)));
            }
        }
    }
}
