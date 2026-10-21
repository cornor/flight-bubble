using UnityEditor;
using UnityEngine;
namespace Flightbeat.Editor {
    public sealed class GardenAssetImport : AssetPostprocessor {
        void OnPreprocessTexture() {
            if (!assetPath.EndsWith("/GardenR1/atlas.png") && !assetPath.EndsWith("/GardenR1/creatures.png")
                && !assetPath.EndsWith("/GardenR1/bars.png")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.sRGBTexture = true; t.mipmapEnabled = false;
            t.alphaIsTransparency = true; t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.maxTextureSize = assetPath.EndsWith("/GardenR1/creatures.png") ? 4096 : 2048;   // 2026-10-21: 4096 x 2048 creatures
            t.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
