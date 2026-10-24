#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Teaching and the finale guide share the same painted panel and rigid text/portrait layout.
        void DrawGardenDialogue(PixelSurface s, string face, string[] lines, float logicalCy, float alpha = 1f, bool compact = false)
        {
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, logicalCy); s.MapLayout = false;
            try
            {
                float panelHeight = compact ? 66f : 104f, portrait = compact ? 72f : 96f;
                GardenSkin.Asset9(s, "scene/level", 99f, cy - panelHeight * .5f, 411f, panelHeight,
                    24f, 18f, new Color(1f, 1f, 1f, alpha));
                GardenSkin.Asset(s, face, 14f, cy - portrait * .5f, portrait, portrait, new Color(1f, 1f, 1f, alpha));
                // 2026-10-21 20:45 (user): the "小蓝" name tag is gone from every line (the portrait says who speaks).
                // 2026-10-21 14:24 (user: the words sat too far left, were small and hard to read). Centred in the bubble
                // body (x 121-506 of the art, centre 315), size 18 (16 before), 28 px apart, below the name tag; words in
                // {braces} are key words: coloured (blue for 蓝色..., orange otherwise). Use plain text throughout
                // so small counters stay open; colour supplies the emphasis without offset bold overdraw.
                float lineGap = compact ? 24f : 28f, first = cy + 2f - (lines.Length - 1) * lineGap * .5f;
                for (int i = 0; i < lines.Length; i++)
                    DialogueLine(s, lines[i], 315f, first + i * lineGap, alpha);
            }
            finally { s.MapLayout = mapped; }
        }
        static readonly Color dialogueKey = GardenSkin.Hex(0xF0640E), dialogueBlue = GardenSkin.Hex(0x1C6DE0);
        // One line centred on (cx, centreY) in screen space; "{...}" marks key words (an unclosed brace, e.g. while the
        // finale line is still being typed, highlights to the end; the braces themselves are never drawn).
        void DialogueLine(PixelSurface s, string line, float cx, float centreY, float alpha)
        {
            string plain = line.Replace("{", "").Replace("}", "");
            float size = Mathf.Min(18f, 340f / Mathf.Max(.01f, GardenSkin.TextWidth(plain, 1f)));
            float x = cx - GardenSkin.TextWidth(plain, size) * .5f, y = centreY - size * .55f;
            bool key = false; int start = 0;
            for (int i = 0; i <= line.Length; i++)
            {
                if (i < line.Length && line[i] != '{' && line[i] != '}') continue;
                if (i > start)
                {
                    string part = line.Substring(start, i - start);
                    Color ink = key ? (part.Contains("蓝") ? dialogueBlue : dialogueKey) : GardenBars.Navy; ink.a = alpha;
                    GardenSkin.Text(s, part, x, y, size, false, ink);
                    x += GardenSkin.TextWidth(part, size);
                }
                if (i < line.Length) key = line[i] == '{';
                start = i + 1;
            }
        }
    }
}
#endif
