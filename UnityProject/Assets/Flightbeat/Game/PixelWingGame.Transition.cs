using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // src: DrawTransition f54816. WAT supplies the arithmetic grouping,
        // paired color stores and original 16 shutter strips / 48 speed lines.
        private void DrawTransition(PixelSurface s)
        {
            if (!transitionActive) return;
            HitBlock(s);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float hold = transitionKind == TransitionKind.Home ? 0f : 1.05f;
            float age = transitionAge, cover;
            if (age < .42f) cover = InOut(OriginalMath.Binary32(age / .42f));
            else if (age < OriginalMath.Binary32(hold + .42f)) cover = 1f;
            else { float elapsed = OriginalMath.Binary32(OriginalMath.Binary32(age + -.42f) - hold); cover = OriginalMath.Binary32(1f - InOut(OriginalMath.Binary32(elapsed + elapsed))); }
            float progress = OriginalMath.Binary32(cover * 1.3f), height = ScreenLayout.Height, row = OriginalMath.Binary32(height * .0625f);
            float stripHeight = OriginalMath.Binary32(row + 1f), left = ScreenLayout.Left, width = ScreenLayout.Width;
            float right = OriginalMath.Binary32(left + width), halfWidth = OriginalMath.Binary32(width * .5f);
            for (int i = 0; i != 16; i++)
            {
                float alternate = OriginalMath.Binary32(OriginalMath.Binary32((float)(i & 1)) * .6f);
                alternate = alternate < 0f ? 0f : OriginalMath.WasmMin(alternate, 1f);
                var tint = new Color(OriginalMath.Binary32(OriginalMath.Binary32(alternate * .02f) + .02f), OriginalMath.Binary32(OriginalMath.Binary32(alternate * .06f) + .06f),
                    OriginalMath.Binary32(OriginalMath.Binary32(alternate * .09f) + .11f), OriginalMath.Binary32(OriginalMath.Binary32(alternate * 0f) + 1f));
                float fill = OriginalMath.Binary32(progress + OriginalMath.Binary32(OriginalMath.Binary32((float)(i & 3)) * -.08f));
                fill = fill < 0f ? 0f : OriginalMath.WasmMin(fill, 1f);
                float xSize = OriginalMath.Binary32(OriginalMath.Binary32(halfWidth * fill) + 2f), y = OriginalMath.Binary32(row * OriginalMath.Binary32((float)i));
                s.Rect(left, y, xSize, stripHeight, tint);
                float rightX = OriginalMath.Binary32(right - xSize);
                s.Rect(rightX, y, xSize, stripHeight, tint);
                if (fill < 1f && fill > 0f)
                {
                    var edge = new Color(.5f, 1f, .95f, .9f);
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(left + xSize) + -3f), y, 3f, stripHeight, edge);
                    s.Rect(rightX, y, 3f, stripHeight, edge);
                }
            }
            float ramp = age < .42f ? OriginalMath.Binary32(age / .42f) : 1f;
            float cy = OriginalMath.Binary32(height * .5f), motion = OriginalMath.Binary32(age * 900f);
            var streak = new Color(.6f, .9f, 1f, OriginalMath.Binary32(cover * .35f));
            for (int i = 0; i != 48; i++)
            {
                float radius = OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(motion * OriginalMath.Binary32((float)(i % 3 + 1))) + OriginalMath.Binary32((float)(i * 53 % 260))), 420f);
                float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.3999600410461426f);
                float sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle);
                var from = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(radius * cos) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(radius * sin)));
                radius  =                 OriginalMath.Binary32(radius+(OriginalMath.Binary32(cover * (OriginalMath.Binary32(ramp * OriginalMath.Binary32((float)(i * 37 % 120 + 40)))))));
                s.Segment(from, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(cos * radius) + 270f), OriginalMath.Binary32(cy + OriginalMath.Binary32(sin * radius))), 2f, streak);
            }
            float cardEnd = OriginalMath.Binary32(OriginalMath.Binary32(hold + .42f) + .3f);
            if (cover > .98f || (transitionKind != TransitionKind.Home && age > .336f && age < cardEnd))
            {
                float cardAge = OriginalMath.Binary32(age + -.336f);
                float entering = OriginalMath.Binary32(cardAge * 6f), leaving = OriginalMath.Binary32((OriginalMath.Binary32(cardEnd - age)) * 5f);
                float alpha = leaving > entering ? entering : leaving;
                alpha = alpha < 0f ? 0f : OriginalMath.WasmMin(alpha, 1f);
                if (transitionKind == TransitionKind.Home) alpha = cover;
                s.Glow(270f, cy, 560f, 200f, new Color(.1f, .6f, 1f, OriginalMath.Binary32(alpha * .25f)), 0f);
                s.Rect(left, OriginalMath.Binary32(cy + -2f), width, 1f, new Color(.5f, 1f, .95f, OriginalMath.Binary32(alpha * .6f)));
                if (transitionKind == TransitionKind.Home)
                    Label(s, "返回指挥舱", 270f, OriginalMath.Binary32(cy + -12f), 1.6f, A(uiCyan, alpha));
                else
                {
                    float eased = OutBack(OriginalMath.Binary32(cardAge / .35f));
                    string level = unchecked(transitionLevel + 1).ToString("D2");
                    Label(s, transitionKind == TransitionKind.Retry ? "重新出击" : "出击", 270f, OriginalMath.Binary32(cy + -86f), 1.5f, A(uiCyan, alpha));
                    float scale = eased < 0f ? 4.2f : OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(eased, 1f) * .3f) + .7f)) * 6f);
                    float x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(scale * OriginalMath.Binary32((float)(level.Length << 3)))) * -.5f) + 270f), y = OriginalMath.Binary32(cy + -44f);
                    CombatFeedback.DrawBigText(s, level, OriginalMath.Binary32(x + 4f), OriginalMath.Binary32(y + 4f), scale, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .8f)));
                    CombatFeedback.DrawBigText(s, level, x, y, scale, new Color(1f, .85f, .35f, alpha));
                    Label(s, "第 " + level + " 关", 270f, OriginalMath.Binary32(cy + 20f), 1.6f, A(uiText, alpha));
                    float progressY = OriginalMath.Binary32(cy + 62f);
                    s.Rect(170f, progressY, 200f, 3f, new Color(.1f, .2f, .3f, alpha));
                    float filled = OriginalMath.Binary32((OriginalMath.Binary32(age + -.42f)) / hold);
                    s.Rect(170f, progressY, filled < 0f ? 0f : OriginalMath.Binary32(OriginalMath.WasmMin(filled, 1f) * 200f), 3f, A(uiCyan, alpha));
                }
            }
            s.MapLayout = mapped;
        }
    }
}
