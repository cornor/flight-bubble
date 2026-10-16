using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54860: topic=3, text=""; age/chars default to zero.
        private int liaisonTopic = 3;
        private string liaisonText = "";
        private float liaisonAge;
        private int liaisonChars;

        // src: LiaisonAction f54630, including topics with no action.
        private static string LiaisonAction(int topic)
        {
            switch (topic)
            {
                case 0: case 1: return "出击";
                case 2: case 10: return "去升级";
                case 6: case 7: return "签收";
                case 8: return "去装备";
                case 9: return "去抽取";
                case 11: return "去补给";
                case 12: return "去组装";
                default: return null;
            }
        }

        // src: DrawLiaison f54607. Draw at most two wrapped lines; empty source
        // lines do not consume a display line. Portrait frame has blink/talk bits.
        private void DrawLiaison(PixelSurface s, float x, float y, float width)
        {
            if (liaisonText.Length == 0) return;
            float back = OutBack(OriginalMath.Binary32(liaisonAge / .3f)), alpha = Clamp01(OriginalMath.Binary32(liaisonAge / .15f));
            string action = LiaisonAction(liaisonTopic);
            bool typing = liaisonChars < liaisonText.Length;
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - back)) * 10f) + y);
            HitBox(s, 116, x, OriginalMath.Binary32(top + -12f), width, 88f);
            float press = PressPulse(116);
            s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(width * .5f) + x), OriginalMath.Binary32(top + 38f), OriginalMath.Binary32(width + 40f), 126f, A(A(uiCyan, alpha), .08f), 0f);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(press * 3f) + x), OriginalMath.Binary32(OriginalMath.Binary32(top + press) + press), OriginalMath.Binary32(width + OriginalMath.Binary32(press * -6f)), OriginalMath.Binary32(OriginalMath.Binary32(press * -4f) + 76f),
                SourceColor(4477658893514493133UL, 1047904911u, OriginalMath.Binary32(alpha * .93f)),
                SourceColor(4435865490164012810UL, 1039516303u, OriginalMath.Binary32(alpha * .93f)), A(uiCyan, OriginalMath.Binary32(alpha * .8f)), 8f);
            int frame = typing ? (~SourceInt(OriginalMath.Binary32(liaisonAge * 11f))) & 1 : 0;
            frame |= (OriginalMath.Remainder(ui, 3.9f) < .14f ? 1 : 0) << 1;
            float portraitX = OriginalMath.Binary32(x + 8f), portraitY = OriginalMath.Binary32(top + 8f);
            s.Rect(portraitX, portraitY, 60f, 60f, SourceColor(4464688526066565775UL, 1043878380u, alpha));
            s.Art(0, portraitX, portraitY, 60f, 60f, frame);
            for (int line = 0; line < 15; line++)
                s.Rect(portraitX, OriginalMath.Binary32(portraitY + OriginalMath.Binary32((float)(line * 4))), 60f, 1f, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .12f)));
            s.Corners(OriginalMath.Binary32(x + 7f), OriginalMath.Binary32(top + 7f), 62f, 62f, A(uiCyan, alpha), 7, 1);
            Plate(s, OriginalMath.Binary32(x + 80f), OriginalMath.Binary32(top + -12f), 108f, 22f,
                SourceColor(4525216907477699789UL, 1055622431u, alpha),
                SourceColor(4489188110450347213UL, 1048911544u, alpha), A(uiCyan, alpha), 5f);
            s.TextF("艾拉 · 通讯官", OriginalMath.Binary32(x + 134f), OriginalMath.Binary32(top + -9f), 1f, new Color(1f, 1f, 1f, alpha), 0);
            float voice = typing ? 6f : 2f;
            for (int i = 0; i < 4; i++)
            {
                float h = OriginalMath.Binary32(voice * Math.Abs(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 9f) + OriginalMath.Binary32((float)i)))));
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 196f) + OriginalMath.Binary32((float)(i * 4))), OriginalMath.Binary32(OriginalMath.Binary32(top + -2f) - h), 2f, OriginalMath.Binary32(h + 2f), A(uiGreen, OriginalMath.Binary32(alpha * .8f)));
            }
            float actionWidth = action != null ? 76f : 0f;
            int perLine = Math.Max(SourceInt(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(width + -90f) - actionWidth)) / 16.1f)), 6);
            string[] lines = liaisonText.Substring(0, Math.Min(liaisonText.Length, liaisonChars)).Split('\n');
            int row = 0;
            foreach (string line in lines)
                for (int start = 0; start < line.Length && row < 2; start += perLine)
                {
                    string part = line.Substring(start, Math.Min(perLine, line.Length - start));
                    s.TextF(part, OriginalMath.Binary32(x + 82f), OriginalMath.Binary32(OriginalMath.Binary32(top + 14f) + OriginalMath.Binary32((float)(row * 25))), 1.15f, A(uiText, alpha), -1);
                    row++;
                }
            if (action != null)
            {
                float buttonX = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(x + width) - actionWidth) + 2f), buttonY = OriginalMath.Binary32(top + 21f);
                float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 4f)) * .5f) + .5f);
                Plate(s, buttonX, buttonY, OriginalMath.Binary32(actionWidth + -12f), 34f,
                    A(Mix(uiGoldHi, Color.white, OriginalMath.Binary32(pulse * .25f)), alpha), A(uiGoldLo, alpha),
                    SourceColor(4481275904541636793UL, 1019265217u, alpha), 6f);
                s.TextF(action, OriginalMath.Binary32(buttonX + OriginalMath.Binary32((OriginalMath.Binary32(actionWidth + -12f)) * .5f)), OriginalMath.Binary32(buttonY + 8f), 1.1f, A(uiBrown, alpha), 0);
            }
            else if (!typing && (SourceInt(OriginalMath.Binary32(ui * 2.5f)) & 1) == 0)
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + width) + -16f), OriginalMath.Binary32(OriginalMath.Binary32(top + 76f) + -14f), 6f, 6f, A(uiCyan, alpha));
        }
    }
}
