using UnityEditor;
using UnityEngine;
namespace Flightbeat.Editor {
    public sealed class GardenAssetImport : AssetPostprocessor {
        void OnPreprocessTexture() {
            if (!assetPath.EndsWith("/GardenR1/atlas.png")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.sRGBTexture = true; t.mipmapEnabled = false;
            t.alphaIsTransparency = true; t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp; t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
