using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: DrawBigText f53836. Original ASCII icon cells, invariant case
        // table and single-precision round-to-even; unsupported glyphs still advance.
        public static void DrawBigText(PixelSurface s, string text, float x, float y, float scale, Color color)
        {
            float advance = OriginalMath.Binary32(scale * 8f);
            for (int i = 0; i < text.Length; i++)
            {
                char glyph = OriginalCharacters.ToUpperInvariant(text[i]);
                if (unchecked((ushort)(glyph - 96)) >= 65473)
                    s.Icon(glyph - 32, OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), advance, advance, color, 0f, 8, 8);
                else if (glyph == '×')
                    DrawTimes(s, x, y, scale, color);
                x = OriginalMath.Binary32(advance + x);
            }
        }
    }
}
