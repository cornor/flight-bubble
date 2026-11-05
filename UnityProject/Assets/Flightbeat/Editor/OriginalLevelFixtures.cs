using System.IO;
using UnityEngine;

namespace Flightbeat.Editor
{
    // 2026-11-05 (user: the original levels 1-52 are not needed in the game any more): the original level files left
    // Resources/levels and live in Art/levels-a/ (byte-identical backup of all 262). Editor checks that were written
    // against the original sector01 / sector04 / training01-02 / sector02 read them from there.
    public static class OriginalLevelFixtures
    {
        public static string Text(string name)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Art/levels-a", name + ".json"));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
