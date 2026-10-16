using System;
using System.Security.Cryptography;
using UnityEngine;

namespace Flightbeat.Audio
{
    // Resource adapter only: original ResourceManager keys and AudioClip fields.
    // WebGL retains original AAC. Native uses float PCM decoded from that AAC
    // without resampling/gain/trimming or lossy encoding (FMOD rejects AAC).
    // f55161/f55165 load, DSP scheduling and playback calls are unchanged.
    public static class OriginalAudioAssets
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        public const string ResourcePath = "OriginalAudio/flightbeat-original-audio";
        public const string BundleSha256 = "78a8e588789ffe625aa312937f87a5cacc326c07b9efb56fbe7a139f7ff04637";
#else
        public const string ResourcePath = "OriginalAudio/flightbeat-native-audio";
        public const string BundleSha256 = "cc8936254eec20c118b9a265641f8c3bd4e9500f83193f8b8d49158dff5c9f45";
#endif
        private static AssetBundle bundle;

        public static AudioClip Load(string path)
        {
            EnsureLoaded();
            return bundle.LoadAsset<AudioClip>(path);
        }

        private static void EnsureLoaded()
        {
            if (bundle) return;
            TextAsset source = Resources.Load<TextAsset>(ResourcePath);
            if (!source) throw new InvalidOperationException("Original audio bundle is missing: " + ResourcePath);
            byte[] bytes = source.bytes;
            using (SHA256 hash = SHA256.Create())
            {
                string digest = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                if (digest != BundleSha256) throw new InvalidOperationException("Original audio bundle SHA-256 mismatch.");
            }
            AssetBundle loaded = AssetBundle.LoadFromMemory(bytes);
            if (!loaded) throw new NotSupportedException("Source-derived AudioClip bundle could not be loaded by Unity "
                + Application.unityVersion + " on " + Application.platform + ". Audio for this engine/platform is unverified; no substitute codec is provided.");
            if (loaded.GetAllAssetNames().Length != 79)
            {
                loaded.Unload(true);
                throw new InvalidOperationException("Original audio bundle must contain all 79 source clips.");
            }
            bundle = loaded;
        }
    }
}
