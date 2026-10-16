namespace Flightbeat.Game
{
    internal static partial class OriginalCharacters
    {
        // src: f67614 -> invariant CultureInfo f47640/f47743 -> f47613/f47614.
        // The original invariant culture has empty Name (f47685) and invariant
        // ASCII casing (f47608). Culture-dependent branches are not this API.
        internal static char ToUpperInvariant(char glyph)
        {
            int c = glyph;
            if (c <= 127) return c >= 97 && c <= 122 ? (char)(c & 65503) : glyph;
            if (((c - 1415) & 65535) >= 64345) return UpperAt(Upper0, c - 224);
            if (((c - 8180) & 65535) >= 65037) return UpperAt(Upper1, c - 7681);
            if (((c - 8581) & 65535) >= 65515) return UpperAt(Upper2, c - 8560);
            if (((c - 9450) & 65535) >= 65510) return UpperAt(Upper3, c - 9424);
            if (((c - 11492) & 65535) >= 65356) return UpperAt(Upper4, c - 11312);
            if (((c - 11558) & 65535) >= 65498) return UpperAt(Upper5, c - 11520);
            if (((c + 22888) & 65535) >= 65449) return UpperAt(Upper6, c - 42561);
            if (((c + 22643) & 65535) >= 65430) return UpperAt(Upper7, c - 42787);
            if (((c + 165) & 65535) >= 65510) return (char)(c - 32);
            if (c == 7545) return (char)42877;
            if (c == 7549) return (char)11363;
            if (c == 8526) return (char)8498;
            return glyph;
        }

        private static char UpperAt(byte[] table, int index) => (char)(table[index * 2] | table[index * 2 + 1] << 8);
    }
}
