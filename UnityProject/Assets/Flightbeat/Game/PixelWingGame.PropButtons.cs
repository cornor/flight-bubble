using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54491 ordinary branch. All coordinates, colors and float grouping
        // follow the WAT; the hit boxes do not shrink with the press animation.
        private void DrawPropButtons(PixelSurface s)
        {
            bool missile = Battle.RescueMissiles > 0 || selection == 2 && aimMissile;
            string[] labels = { "重排", "指定", missile ? "超级导弹" : "超级炸弹" };
            uint packed = unchecked((uint)(missile ? -10366465 : -10710529));
            Color[] colors = {
                SourceColor(4566897280173272301UL, 4575657222468908470UL),
                SourceColor(4554746392497803987UL, 4575657222473777152UL),
                new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 8) & 209u)) / 255f), OriginalMath.Binary32(OriginalMath.Binary32((float)((packed >> 16) & 125u)) / 255f), 1f)
            };
            for (int kind = 0; kind < 3; kind++)
            {
                Color color = colors[kind];
                bool inactive = kind != selection || kind == 0;
                float x = OriginalMath.Binary32((float)(kind * 174 + 18));
                HitBox(s, kind + 10, x, 878f, 156f, 72f);
                float pulse = PressPulse(kind + 10), width = OriginalMath.Binary32(OriginalMath.Binary32(pulse * -8f) + 156f), left = OriginalMath.Binary32(OriginalMath.Binary32(pulse * 4f) + x);
                Color top = inactive ? new Color(.08627451211214066f, .20392157137393951f, .2823529541492462f, 1f) : Mix(color, uiPanelHi, .55f);
                Color bottom = inactive ? new Color(.027450980618596077f, .0941176488995552f, .14901961386203766f, 1f) : Mix(color, uiPanel, .7f);
                Plate(s, left, OriginalMath.Binary32(OriginalMath.Binary32(pulse * 3f) + 878f), width, OriginalMath.Binary32(OriginalMath.Binary32(pulse * -6f) + 72f), top, bottom,
                    inactive ? A(color, .9f) : Color.white, 8f);
                s.Rect(OriginalMath.Binary32(left + 10f), 941f, OriginalMath.Binary32(width + -20f), 2f, A(color, inactive ? .45f : 1f));
                float glow = inactive ? 0f : OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 8f)) * .2f);
                s.Glow(OriginalMath.Binary32(x + 44f), 914f, 90f, 70f, A(color, OriginalMath.Binary32(glow + .2f)), 0f);
                if (kind <= 1) s.DeckArt(kind + 15, OriginalMath.Binary32(x + 12f), 890f, 48f, 48f, Color.white, true);
                else s.DeckArt(17, OriginalMath.Binary32(x + 10f), 888f, 52f, 52f, Color.white, true);
                string text = inactive ? labels[kind] : "取消";
                float size = labels[kind].Length < 3 ? 1.6f : inactive ? 1.35f : 1.6f;
                Label(s, text, OriginalMath.Binary32(x + 106f), 902f, size, Color.white);
                if (kind == 2 && missile)
                    CountBadge(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 156f) + -6f), 880f, "1", SourceColor(4562686855031554048UL, 4575657222458677658UL), uiBrown);
                else if (kind == 2 && Battle.PendingStrike != null)
                {
                    float right = OriginalMath.Binary32(x + 156f);
                    Plate(s, OriginalMath.Binary32(right + -52f), 868f, 52f, 28f, SourceColor(4539628425434680525UL, 4575657222467066266UL),
                        SourceColor(4489188110458735821UL, 4575657222458677658UL), uiInk, 6f);
                    s.Text((OriginalMath.Binary32(1f - Battle.PendingStrike.Age)).ToString("0.0"), OriginalMath.Binary32(right + -44f), 875f, 2f, Color.white, false);
                    return;
                }
                else if (Economy.Data.items[kind] > 0)
                    CountBadge(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 156f) + -6f), 880f, Economy.Data.items[kind].ToString(),
                        SourceColor(4565569160339259392UL, 4575657222459348746UL), uiBrown);
                else
                {
                    float right = OriginalMath.Binary32(x + 156f);
                    Plate(s, OriginalMath.Binary32(right + -40f), 866f, 40f, 32f, SourceColor(4471894287215860122UL, 4575657222425794314UL),
                        SourceColor(4428659729066056745UL, 4575657221408423936UL), uiGoldHi, 6f);
                    AdIcon(s, OriginalMath.Binary32(right + -31f), 873f, 22f);
                }
            }
        }

        // f54489. Measure/Text use the bitmap glyph path, not TextF.
        private void CountBadge(PixelSurface s, float cx, float cy, string text, Color fill, Color ink)
        {
            float width = OriginalMath.WasmMax(OriginalMath.Binary32(PixelSurface.Measure(text, 2f) + 16f), 30f);
            s.Glow(cx, cy, OriginalMath.Binary32(width + 24f), 52f, A(fill, .35f), 0f);
            float x = OriginalMath.Binary32(cx - OriginalMath.Binary32(width * .5f)), y = OriginalMath.Binary32(cy + -15f);
            Plate(s, OriginalMath.Binary32(x + -2f), OriginalMath.Binary32(y + -2f), OriginalMath.Binary32(width + 4f), 34f, uiInk, uiInk, uiInk, 9f);
            Plate(s, x, y, width, 30f, Mix(fill, Color.white, .35f), Mix(fill, new Color(.6f, .3f, 0f, 1f), .35f),
                SourceColor(4575657222473777152UL, 4568451461351997440UL), 8f);
            s.Text(text, OriginalMath.Binary32(OriginalMath.Binary32(cx - OriginalMath.Binary32(PixelSurface.Measure(text, 2f) * .5f)) + 1f), OriginalMath.Binary32(cy + -7f), 2f,
                SourceColor(0UL, 4518011145319088128UL), false);
            s.Text(text, OriginalMath.Binary32(cx - OriginalMath.Binary32(PixelSurface.Measure(text, 2f) * .5f)), OriginalMath.Binary32(cy + -8f), 2f, ink, false);
        }
    }
}
