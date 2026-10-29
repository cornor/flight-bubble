using System;
using UnityEditor;
using UnityEngine;
namespace Flightbeat.Editor {
    public sealed class GardenAssetImport : AssetPostprocessor {
        // 2026-10-29 (user: slow loading, stutters in WeChat; compress but keep the quality). The phone build of the WebGL
        // player stores the R1 textures (Resources/GardenR1/*.png: the three atlases, home art, backgrounds) as ASTC 4x4
        // (8 bit per pixel, a quarter of RGBA32; tested at ~43 dB PSNR on the plush atlas, visually the same). It is
        // selected by FLIGHTBEAT_WEBGL_ASTC=1 for that Unity run (scripts/r1v3/build.sh builds Build/ and Build-astc/,
        // the page picks Build-astc when the browser has WEBGL_compressed_texture_astc). The custom dependency makes
        // Unity reimport these textures when the mode changes and keep both results in its artifact cache.
        public const string FormatDependency = "flightbeat/webgl-texture-format";
        public static bool Astc => Environment.GetEnvironmentVariable("FLIGHTBEAT_WEBGL_ASTC") == "1";
        static bool IsGardenTexture(string path) =>
            path.StartsWith("Assets/Flightbeat/Resources/GardenR1/", StringComparison.Ordinal)
            && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        void OnPreprocessTexture() {
            if (!IsGardenTexture(assetPath)) return;
            context.DependsOnCustomDependency(FormatDependency);
            var t = (TextureImporter)assetImporter;
            bool atlas = assetPath.EndsWith("/GardenR1/atlas.png") || assetPath.EndsWith("/GardenR1/creatures.png")
                || assetPath.EndsWith("/GardenR1/bars.png");
            if (atlas) {
                t.textureType = TextureImporterType.Default;
                t.sRGBTexture = true; t.mipmapEnabled = false;
                t.alphaIsTransparency = true; t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.maxTextureSize = assetPath.EndsWith("/GardenR1/creatures.png") ? 4096 : 2048;   // 2026-10-21: 4096 x 2048 creatures
                t.textureCompression = TextureImporterCompression.Uncompressed;
            }
            var ps = t.GetPlatformTextureSettings("WebGL");
            if (!Astc) {
                // Desktop build: unchanged import settings (undo an ASTC override in case an earlier run left it behind).
                if (ps.overridden && ps.format == TextureImporterFormat.ASTC_4x4) { ps.overridden = false; t.SetPlatformTextureSettings(ps); }
                return;
            }
            ps.overridden = true;
            ps.format = TextureImporterFormat.ASTC_4x4;
            ps.compressionQuality = 100;   // best encoder effort
            ps.maxTextureSize = Mathf.Max(ps.maxTextureSize, t.maxTextureSize, 2048);
            if (assetPath.EndsWith("/GardenR1/creatures.png")) ps.maxTextureSize = 4096;
            t.SetPlatformTextureSettings(ps);
        }
    }
}
