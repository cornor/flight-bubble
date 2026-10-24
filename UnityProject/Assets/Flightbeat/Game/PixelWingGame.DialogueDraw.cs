using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54505. Dialogue is laid out in physical screen coordinates, then the
        // caller's mapping flag is restored. Text wraps by UTF-16 code units.
        private void DrawDialogue(PixelSurface s, float x, float y, float width, bool immediate)
        {
            bool mapped = s.MapLayout;
            if (mapped) y = PlushLayout.Y(y);
            s.MapLayout = false;
            float top = OriginalMath.Binary32(y + OriginalMath.Binary32((OriginalMath.Binary32(1f - OutBack(OriginalMath.Binary32(dialogueAge / .28f)))) * 14f));
            float alpha = Clamp01(OriginalMath.Binary32(dialogueAge / .15f));
            Plate(s, x, top, width, 78f, SourceColor(4480541198824882831UL, 1048911544u, OriginalMath.Binary32(alpha * .95f)),
                SourceColor(4435865490164012810UL, 1039516303u, OriginalMath.Binary32(alpha * .95f)), A(uiCyan, OriginalMath.Binary32(alpha * .75f)), 8f);
            s.Rect(OriginalMath.Binary32(x + 10f), OriginalMath.Binary32(top + 3f), OriginalMath.Binary32(width + -20f), 1f, A(uiCyan, OriginalMath.Binary32(alpha * .5f)));
            float faceX = OriginalMath.Binary32(x + 8f), faceY = OriginalMath.Binary32(top + 7f);
            s.Rect(faceX, faceY, 64f, 64f, SourceColor(4464688526066565775UL, 1043878380u, alpha));
            s.Art(0, faceX, faceY, 64f, 64f, AssistantFrame);
            Color scanline = new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .12f));
            for (int row = 0; row < 16; row++) s.Rect(faceX, OriginalMath.Binary32(faceY + OriginalMath.Binary32((float)(row * 4))), 64f, 1f, scanline);
            s.Rect(faceX, OriginalMath.Binary32(faceY + OriginalMath.Remainder(OriginalMath.Binary32(clock * 40f), 64f)), 64f, 2f,
                SourceColor(4575657222465388544UL, 1065353216u, OriginalMath.Binary32(alpha * .12f)));
            s.Corners(OriginalMath.Binary32(x + 7f), OriginalMath.Binary32(top + 6f), 66f, 66f, A(uiCyan, alpha), 7, 1);
            Plate(s, OriginalMath.Binary32(x + 84f), OriginalMath.Binary32(top + -12f), 112f, 22f, SourceColor(4525216907477699789UL, 1055622431u, alpha),
                SourceColor(4489188110450347213UL, 1048911544u, alpha), A(uiCyan, alpha), 5f);
            s.TextF("艾拉 · 通讯官", OriginalMath.Binary32(x + 140f), OriginalMath.Binary32(top + -9f), 1.05f, new Color(1f, 1f, 1f, alpha), 0);
            float baseline = OriginalMath.Binary32(top + -2f), right = OriginalMath.Binary32(x + width), barsX = OriginalMath.Binary32(right + -26f);
            for (int n = 0; n < 4; n++)
            {
                float height = OriginalMath.Binary32(Math.Abs(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 9f) + OriginalMath.Binary32((float)n)))) * 6f);
                s.Rect(OriginalMath.Binary32(barsX + OriginalMath.Binary32((float)(n * 4))), OriginalMath.Binary32(baseline - height), 2f, OriginalMath.Binary32(height + 2f), A(uiGreen, OriginalMath.Binary32(alpha * .8f)));
            }
            string shown = immediate ? dialogue : dialogue.Substring(0, spokenCharacters > dialogue.Length ? dialogue.Length : spokenCharacters);
            int columns = CommsInt(OriginalMath.Binary32((OriginalMath.Binary32(width + -96f)) / 18.19999885559082f));
            columns = columns > 6 ? columns : 6;
            int line = 0;
            foreach (string paragraph in shown.Split('\n'))
            {
                if (paragraph.Length == 0) { line++; continue; }
                for (int start = 0; start < paragraph.Length; start += columns)
                {
                    int count = paragraph.Length - start;
                    s.TextF(paragraph.Substring(start, count > columns ? columns : count), OriginalMath.Binary32(x + 86f),
                        OriginalMath.Binary32(OriginalMath.Binary32(top + 16f) + OriginalMath.Binary32((float)(line * 25))), 1.3f, A(uiText, alpha), -1);
                    line++;
                }
            }
            if (spokenCharacters < dialogue.Length || (CommsInt(OriginalMath.Binary32(clock * 2.5f)) & 1) == 0)
                s.Rect(OriginalMath.Binary32(right + -16f), OriginalMath.Binary32(OriginalMath.Binary32(top + 78f) + -14f), 6f, 6f, A(uiCyan, alpha));
            s.MapLayout = mapped;
        }

        // f54508. The ReviveOffer phase is intentionally not filtered here.
        private void DrawTutorial(PixelSurface s)
        {
            if (lesson == null) return;
            int state = (int)Battle.State;
            if (state <= 5 && ((1 << state) & 49) != 0) return;
            if (settings || selection > 0 || finaleGuide) return;
            DrawDialogue(s, 32f, 136f, 476f, true);
            if (!lesson.Complete)
            {
                Label(s, lesson.Goal, 270f, 105f, 1.2f, uiGold);
                if (lesson.Practice) Button(s, 50, 410f, 225f, 98f, 28f, "提示", Btn.Ghost, 1f);
                int lane = lesson.SuggestedLane, hint = lesson.HintLevel;
                if (lane >= 0 && hint > 0)
                {
                    float x = Battle.QueuePositionX(lane);
                    s.Corners(OriginalMath.Binary32(x + -45f), 696f, 90f, 64f, Mix(uiCyan, Color.white, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 5f)) * .5f) + .5f)), 12, 2);
                    if (hint >= 3) DrawTapPointer(s, new Vector2(x, 722f), new Vector2(OriginalMath.Binary32(x + 14f), 721f), -42f, -1f);
                }
            }
            Label(s, "训练保护中", 445f, 555f, 1f, uiGreen);
        }
    }
}
