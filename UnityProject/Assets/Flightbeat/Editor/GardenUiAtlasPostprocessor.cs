using System;
using UnityEditor;

namespace Flightbeat.Editor
{
    public sealed class GardenUiAtlasPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] from)
        {
            if (Array.Exists(imported, p => p == "Assets/Flightbeat/Resources/GardenR1/atlas.json" || p == "Assets/Flightbeat/Resources/GardenR1/atlas.png"))
                EditorApplication.delayCall += GardenBattleUiAssets.RefreshSprites;
        }
    }
}
