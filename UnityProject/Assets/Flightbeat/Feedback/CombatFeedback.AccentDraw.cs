using System;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53911. Preserve layer order; unfinished active effects fail explicitly.
        public void DrawAccents(PixelSurface s)
        {
            DrawShockwaves(s);
            DrawBeams(s);
            DrawSuperStrike(s);
            DrawPurge(s);
            DrawDefenceWarning(s);
            if (stepAge < .2f)
            {
                Color step = SourceColor(4550437062821040620UL, 1065353216u, OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Binary32(stepAge / .2f))) * .28f));
                s.Glow(18f, 380f, 18f, 210f, step, 0f);
                s.Glow(522f, 380f, 18f, 210f, step, 0f);
            }
            if (sweepAge < .8f)
            {
                Color color = Alpha(palette[sweepColor], OriginalMath.Binary32(OriginalMath.Binary32(sweepAge / -.8f) + 1f));
                float y = OriginalMath.Binary32(OriginalMath.Binary32(sweepAge * -640f) + 570f);
                s.Glow(270f, y, 530f, 100f, color, 0f);
                s.Rect(18f, y, 504f, 3f, color);
            }
            if (maxAge < .9f)
                s.Glow(270f, 606f, 520f, 90f, SourceColor(4546834186576592896UL, 1036831949u, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(maxAge / -.9f) + 1f)) * .35f)), 0f);
            DrawFlash(s);
        }

        // f53916. WAT: 570 - (sin(...)*4 + 6), not 570 - sin(...)*4 + 6.
        private void DrawDefenceWarning(PixelSurface s)
        {
            if (Look == null || Look.model < 4 || (int)battle.State != 2) return;
            float danger = battle.Danger;
            if (danger <= .05f) return;
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 8f)) * .4f) + .6f);
            s.Glow(270f, 572f, 520f, 40f, SourceColor(4575657222462033101UL, 1059481190u, OriginalMath.Binary32(OriginalMath.Binary32(danger * .25f) * pulse)), 0f);
            for (int x = 24; x <= 504; x += 24)
                s.Rect(OriginalMath.Binary32((float)x), OriginalMath.Binary32(570f - (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)x) * .1f) + OriginalMath.Binary32(time * 6f))) * 4f) + 6f))), 12f, 2f,
                    SourceColor(4575657222462033101UL, 1059481190u, OriginalMath.Binary32(danger * .8f)));
            s.Rect(16f, 572f, 508f, 2f, SourceColor(4575657222470421709UL, 1063675494u, OriginalMath.Binary32(OriginalMath.Binary32(danger * .9f) * pulse)));
        }

        // f53917. The full-screen flash temporarily disables layout mapping.
        private void DrawFlash(PixelSurface s)
        {
            if (flashAge >= flashLength) return;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height,
                Alpha(flashColor, OriginalMath.Binary32(flashColor.a * (OriginalMath.Binary32(1f - OriginalMath.Binary32(flashAge / flashLength))))));
            s.MapLayout = mapped;
        }

        // f53922, including the exact source float payloads for warning colors.
        public void DrawDanger(PixelSurface s)
        {
            float fade = Unit(OriginalMath.Binary32(OriginalMath.Binary32(hurtAge / -.65f) + 1f));
            if (!(fade > 0f)) return;
            Color side = SourceColor(4405601299513475072UL, 1020054733u, OriginalMath.Binary32(fade * .6f));
            s.Glow(20f, 388f, 90f, 520f, side, 0f);
            s.Glow(520f, 388f, 90f, 520f, side, 0f);
            s.Corners(13f, 116f, 514f, 468f, SourceColor(4494952716807700480UL, 1039516303u, fade), 25, 3);
            s.Rect(14f, 114f, 512f, 458f, SourceColor(4441630096532439040UL, 1024416809u, OriginalMath.Binary32(fade * .09f)));
            float span = OriginalMath.Binary32(hurtAge * 700f);
            s.Segment(new Vector2(OriginalMath.Binary32(270f - span), 574f), new Vector2(OriginalMath.Binary32(span + 270f), 574f), 3f,
                SourceColor(4563407430284738560UL, 1058642330u, fade));
        }

    }
}
