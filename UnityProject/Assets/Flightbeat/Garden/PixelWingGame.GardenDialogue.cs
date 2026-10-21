#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Teaching and the finale guide share the same painted panel and rigid text/portrait layout.
        void DrawGardenDialogue(PixelSurface s, string face, string[] lines, float logicalCy, float alpha = 1f)
        {
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, logicalCy); s.MapLayout = false;
            try
            {
                GardenBarArt.Art(s, "speech", 303f, cy, 414f, 104f, alpha);
                GardenSkin.Asset(s, face, 14f, cy - 48f, 96f, 96f, new Color(1f, 1f, 1f, alpha));
                GardenBars.Bar(s, 154f, cy - 35f, 62f, 22f, GardenBars.Sky, null, "小蓝", 14f,   // 2026-10-21: 13 -> 14 (smallest size)
                    GardenBars.Navy, alpha);
                // 2026-10-21 14:24 (user: the words sat too far left, were small and hard to read). Centred in the bubble
                // body (x 121-506 of the art, centre 315), size 18 (16 before), 28 px apart, below the name tag; words in
                // {braces} are key words: coloured (blue for 蓝色..., orange otherwise) and a touch bolder.
                float lineGap = 28f, first = cy + 7f - (lines.Length - 1) * lineGap * .5f;
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
            float size = Mathf.Min(18f, 340f / Mathf.Max(.01f, GardenBarArt.TextWidth(plain, 1f)));
            float x = cx - GardenBarArt.TextWidth(plain, size) * .5f, y = centreY - size * .55f;
            bool key = false; int start = 0;
            for (int i = 0; i <= line.Length; i++)
            {
                if (i < line.Length && line[i] != '{' && line[i] != '}') continue;
                if (i > start)
                {
                    string part = line.Substring(start, i - start);
                    Color ink = key ? (part.Contains("蓝") ? dialogueBlue : dialogueKey) : GardenBars.Navy; ink.a = alpha;
                    GardenBarArt.Text(s, part, x, y, size, false, ink);
                    if (key) GardenBarArt.Text(s, part, x + .7f, y, size, false, ink);   // a touch bolder
                    x += GardenBarArt.TextWidth(part, size);
                }
                if (i < line.Length) key = line[i] == '{';
                start = i + 1;
            }
        }
    }
}
#endif
