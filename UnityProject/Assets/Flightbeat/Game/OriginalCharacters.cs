namespace Flightbeat.Game
{
    internal static partial class OriginalCharacters
    {
        // src: CharUnicodeInfo.GetUnicodeCategory f47168 -> f47169.
        // The caller is a UTF-16 char; all table indices remain within source bounds.
        private static int Category(char glyph)
        {
            int second = ((int)glyph >> 3 & 62) | Level1[(int)glyph >> 9] << 6;
            int block = Level2[second] | Level2[second + 1] << 8;
            int third = ((int)glyph & 15) | block << 4;
            return Categories[Level3[third] << 1];
        }

        // src: System.Char.IsPunctuation f67610, Latin1 initializer f67666.
        internal static bool IsPunctuation(char glyph)
        {
            int category = glyph <= 255 ? Latin1[glyph] : Category(glyph);
            return unchecked((uint)(category - 18)) < 7u;
        }

        // src: System.Char.IsWhiteSpace f67495, CharUnicodeInfo f47177.
        internal static bool IsWhiteSpace(char glyph)
        {
            if (glyph <= 255)
                return glyph == 32 || glyph == 160 || unchecked((uint)(glyph - 9)) < 5u || glyph == 133;
            return unchecked((uint)(Category(glyph) - 11)) < 3u;
        }
    }
}
