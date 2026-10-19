#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Flightbeat.Garden
{
    // Shared by editor acceptance and the future level-set loader. Matches levelsb_common.py.
    public static class GardenLevelHashes
    {
        public static string FileSha256(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static string CellsSha256(int[] cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var text = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i != 0) text.Append(',');
                text.Append(cells[i].ToString(CultureInfo.InvariantCulture));
            }
            return FileSha256(Encoding.UTF8.GetBytes(text.ToString()));
        }
    }
}
#endif
