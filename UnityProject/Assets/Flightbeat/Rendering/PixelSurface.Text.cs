using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        // src: f54415. Negative sizes also take the first threshold branch.
        public static float AsciiScale(float size) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54415);
#endif
            return AsciiScaleCore(size);
        }

        // f54416/f54417 inline these thresholds in WAT; they do not call f54415.
        private static float AsciiScaleCore(float size)
        {
            if (size < 1.2f) return 1f;
            if (size < 2.4f) return 2f;
            if (size < 3.6f) return 3f;
            return OriginalMath.RoundSingle(size);
        }

        // src: f54416. Width uses raw UTF-16 characters before uppercasing.
        // Control/space/unsupported characters still contribute advance.
        public static float Measure(string text, float size)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54416);
#endif
            float ascii = AsciiScaleCore(size);
            if (text.Length <= 0) return 0f;
            float asciiWidth = OriginalMath.Binary32(ascii * 8f), wideWidth = OriginalMath.Binary32(size * 14f), result = 0f;
            for (int i = 0; i < text.Length; i++) result = OriginalMath.Binary32(result + (text[i] > 127 ? wideWidth : asciiWidth));
            return result;
        }

        private static Rect ChineseUv(int index) => new Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(index & 31)) * 32f) * 0.0009765625f),
            OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(index >> 5)) * 32f) + 128f)) + 32f)) * -0.00048828125f) + 1f), 0.03125f, 0.015625f);

        // src: f54417. align<0: left, align==0: center, align>0: right.
        public void TextF(string text, float x, float y, float size, Color color, int align)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54417);
            if (DrawObservation.Current != null) DrawObservation.Current.Input(54417, text, x, y, size, align);
#endif
            if (string.IsNullOrEmpty(text)) return;
            float asciiScale = AsciiScaleCore(size), measured = Measure(text, size);
            float wideAdvance = OriginalMath.Binary32(size * 14f), asciiAdvance = OriginalMath.Binary32(asciiScale * 8f);
            float asciiY = OriginalMath.RoundSingle(OriginalMath.Binary32(OriginalMath.Binary32(size * 0.3f) + (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(wideAdvance - asciiAdvance)) * 0.5f) + y))));
            float cursor = OriginalMath.Binary32(x - (align == 0 ? OriginalMath.Binary32(measured * 0.5f) : align <= 0 ? 0f : measured));
            float wideSize = OriginalMath.Binary32(size * 18f), margin = OriginalMath.Binary32(size + size), wideY = OriginalMath.Binary32(y - margin);
            for (int i = 0; i < text.Length; i++)
            {
                char glyph = OriginalCharacters.ToUpperInvariant(text[i]);
                if (glyph >= 128)
                {
                    int index = ChineseGlyphs.Characters.IndexOf(glyph);
                    if (index >= 0) Quad(OriginalMath.Binary32(cursor - margin), wideY, wideSize, wideSize, ChineseUv(index), color, 0f, 2f, true, 0f);
                    cursor = OriginalMath.Binary32(cursor + wideAdvance);
                }
                else
                {
                    if (((glyph - 96) & 65535) >= 65473)
                    {
                        float rounded = OriginalMath.RoundSingle(cursor);
                        int cell = glyph - 32, row = cell / 16;
                        var uv = new Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(cell - (row << 4))) * 16f) * 0.0009765625f),
                            OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)row) * 16f) + 8f)) * -0.00048828125f) + 1f), 0.0078125f, 0.00390625f);
                        Quad(rounded, asciiY, asciiAdvance, asciiAdvance, uv, color, 0f, 0f, true, 0f);
                    }
                    cursor = OriginalMath.Binary32(cursor + asciiAdvance);
                }
            }
        }

        // src: f54422. Integer-scale text has a distinct baseline and cursor
        // rounding rule from TextF; neither method implements line breaking.
        public void Text(string text, float x, float y, float scale, Color color, bool centered)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54422);
#endif
            float roundedScale = OriginalMath.WasmMax(OriginalMath.RoundSingle(scale), 1f);
            float width = 0f;
            for (int i = 0; i < text.Length; i++) width = OriginalMath.Binary32(width + OriginalMath.Binary32(roundedScale * (text[i] > 127 ? 14f : 8f)));
            float cursor = OriginalMath.RoundSingle(OriginalMath.Binary32(x - (centered ? OriginalMath.Binary32(width * 0.5f) : 0f)));
            float roundedY = OriginalMath.RoundSingle(y);
            if (text.Length <= 0) return;
            float asciiSize = OriginalMath.Binary32(roundedScale * 8f), wideSize = OriginalMath.Binary32(roundedScale * 18f);
            float wideY = OriginalMath.Binary32(roundedY + OriginalMath.Binary32(roundedScale * -4f)), margin = OriginalMath.Binary32(roundedScale + roundedScale);
            for (int i = 0; i < text.Length; i++)
            {
                char glyph = OriginalCharacters.ToUpperInvariant(text[i]);
                int index = ChineseGlyphs.Characters.IndexOf(glyph);
                if (index >= 0) Quad(OriginalMath.Binary32(cursor - margin), wideY, wideSize, wideSize, ChineseUv(index), color, 0f, 2f, true, 0f);
                else if (((glyph - 96) & 65535) >= 65473)
                {
                    var uv = new Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(glyph & 15)) * 16f) * 0.0009765625f),
                        OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(((glyph + 224) >> 4) & 15)) * 16f) + 8f)) * -0.00048828125f) + 1f), 0.0078125f, 0.00390625f);
                    Quad(cursor, roundedY, asciiSize, asciiSize, uv, color, 0f, 0f, true, 0f);
                }
                cursor = OriginalMath.Binary32(cursor + OriginalMath.Binary32(roundedScale * (glyph > 127 ? 14f : 8f)));
            }
        }
    }
}
