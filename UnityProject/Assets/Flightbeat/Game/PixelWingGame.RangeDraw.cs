using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54830. Keep the original mapping flag even on the full-screen flashes.
        private void DrawRange(PixelSurface s)
        {
            if (Battle == null) return;
            int chapter = Battle.Level.lesson;
            if (chapter > 0 && Battle.State == Phase.Won)
            {
                float age = feedback.EndAge, size = OutBack(Clamp01(OriginalMath.Binary32(age / .3f)));
                float fade = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(2.2f - age)) / .3f));
                s.Glow(270f, 420f, OriginalMath.Binary32(size * 460f), OriginalMath.Binary32(size * 160f), A(uiGreen, OriginalMath.Binary32(fade * .3f)), 0f);
                Title(s, "靶场 " + chapter.ToString() + "/2 完成", 270f, 388f, OriginalMath.Binary32(size * 2.6f),
                    new Color(1f, 1f, 1f, fade), A(uiInk, fade));
                s.TextF(chapter == 1 ? "下一轮：更大的靶标" : "下一步：正式御敌", 270f, 440f, 1.2f, A(uiGreen, fade), 0);
                if (age > 1.65f)
                    s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height,
                        new Color(1f, 1f, 1f, OriginalMath.Binary32(Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age + -1.9f) + .25f)) * 4f)) * .85f)));
            }
            float since = OriginalMath.Binary32(ui - rangeHandoffAt);
            if (since < 0f || since > 1.6f) return;
            if (since < .45f)
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(1f, 1f, 1f, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(since / -.45f) + 1f)) * .85f)));
            if (rangeHandoffTo != 2 || !(since > .2f)) return;
            float appear = OutBack(Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(since + -.2f)) / .3f))), alpha = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(1.5f - since)) / .3f));
            s.Glow(270f, 300f, OriginalMath.Binary32(appear * 480f), OriginalMath.Binary32(appear * 160f), A(uiRed, OriginalMath.Binary32(alpha * .3f)), 0f);
            Title(s, "正式御敌", 270f, 272f, OriginalMath.Binary32(appear * 3f), SourceColor(4572054341912887296UL, 1063339950u, alpha),
                SourceColor(4369572502481089331UL, 1025758986u, alpha));
            s.TextF("靶场结业 · 沙群已进入防区", 270f, 326f, 1.2f, A(uiGoldHi, alpha), 0);
        }

        // f54692. The original enumerator resolves IndexOf for each fighter.
        private void DrawLegendaryFeedback(PixelSurface s)
        {
            if (!Battle.Overclocked && !Battle.FusionBoosted) return;
            bool overclocked = Battle.Overclocked;
            Color color = overclocked ? new Color(1f, .3f, .35f, 1f) : new Color(.4f, .9f, 1f, 1f);
            foreach (Fighter ship in Battle.Fleet)
            {
                float x = Battle.FleetPositionX(Battle.VisualSlot(Battle.Fleet.IndexOf(ship)));
                s.Corners(OriginalMath.Binary32(x + -24f), 607f, 48f, 38f, A(color, .6f), 7, 1);
                s.Glow(x, 630f, 65f, 60f, A(color, .2f), 0f);
            }
            string text = Battle.Overclocked ? "赤焰超频" : "跃迁加速";
            s.TextF(text, 271.0799865722656f, 546.0800170898438f, 1.2f, SourceColor(0UL, 4528099211748573184UL), 0);
            s.TextF(text, 270f, 545f, 1.2f, A(color, .6f), 0);
        }
    }
}
