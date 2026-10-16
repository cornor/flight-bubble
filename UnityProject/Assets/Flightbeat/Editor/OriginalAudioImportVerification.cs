using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Flightbeat.Audio;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Stage A resource integration check, run by CLI. Reads the frozen source
    // AudioClip fields; does not play audio or assert timing/mixing equivalence.
    public static class OriginalAudioImportVerification
    {
        [Serializable] private sealed class Sources { public Source[] clips; }
        [Serializable] private sealed class PcmSources { public PcmSource[] clips; public string bundleSha256; }
        [Serializable] private sealed class PcmSource { public string name, pcmSha256; }
        [Serializable] private sealed class Source { public string name; public Fields fields; }
        [Serializable] private sealed class Fields
        {
            public int m_Channels, m_Frequency, m_LoadType;
            public float m_Length;
            public bool m_PreloadAudioData, m_LoadInBackground, m_Ambisonic;
        }
        [Serializable] private sealed class Clip
        {
            public string path, name, initialState, finalState, pcmSha256;
            public int channels, frequency, samples, loadType;
            public float length;
            public bool preload, background, ambisonic, acceptedLoad, matchesFields, pcmMatchesDecodedSource;
        }
        [Serializable] private sealed class Result
        {
            public string unityVersion, platform, bundleSha256, error;
            public bool pass, playbackExecuted;
            public List<Clip> clips = new List<Clip>();
        }
        public static void Run()
        {
            string input = null, output = null, bundlePath = null, pcmManifest = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-flightbeatAudioSource") input = args[i + 1];
                if (args[i] == "-flightbeatAudioReport") output = args[i + 1];
                if (args[i] == "-flightbeatAudioBundle") bundlePath = args[i + 1];
                if (args[i] == "-flightbeatAudioPcmManifest") pcmManifest = args[i + 1];
            }
            if (input == null || output == null) throw new ArgumentException("Missing audio source/report path.");
            var result = new Result { unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                bundleSha256 = OriginalAudioAssets.BundleSha256 };
            AssetBundle preparedBundle = null;
            try
            {
                var sources = JsonUtility.FromJson<Sources>("{\"clips\":" + File.ReadAllText(input) + "}");
                if (sources.clips.Length != 79) throw new InvalidOperationException("Expected 79 source AudioClips.");
                PcmSources pcm = pcmManifest == null ? null : JsonUtility.FromJson<PcmSources>(File.ReadAllText(pcmManifest));
                if (bundlePath != null)
                {
                    byte[] bytes = File.ReadAllBytes(bundlePath);
                    result.bundleSha256 = Digest(bytes);
                    if (pcm == null || pcm.bundleSha256 != result.bundleSha256)
                        throw new InvalidOperationException("Prepared PCM bundle hash mismatch.");
                    preparedBundle = AssetBundle.LoadFromMemory(bytes);
                    if (!preparedBundle || preparedBundle.GetAllAssetNames().Length != 79)
                        throw new InvalidOperationException("Prepared PCM bundle load failed.");
                }
                foreach (Source source in sources.clips)
                {
                    string path = "Audio/" + source.name;
                    AudioClip clip = preparedBundle ? preparedBundle.LoadAsset<AudioClip>(path) : OriginalAudioAssets.Load(path);
                    if (!clip) throw new InvalidOperationException("Missing AudioClip: " + path);
                    var row = new Clip { path = path, name = clip.name, channels = clip.channels, frequency = clip.frequency,
                        samples = clip.samples, length = clip.length, loadType = (int)clip.loadType, preload = clip.preloadAudioData,
                        background = clip.loadInBackground, ambisonic = clip.ambisonic, initialState = clip.loadState.ToString() };
                    result.clips.Add(row);
                    Fields f = source.fields;
                    row.matchesFields = row.name == source.name && row.channels == f.m_Channels && row.frequency == f.m_Frequency
                        && row.length == f.m_Length && row.loadType == f.m_LoadType && row.preload == f.m_PreloadAudioData
                        && row.background == f.m_LoadInBackground && row.ambisonic == f.m_Ambisonic;
                    if (!row.matchesFields) throw new InvalidOperationException("AudioClip source field mismatch: " + path);
                    row.acceptedLoad = clip.LoadAudioData();
                    row.finalState = clip.loadState.ToString();
                    if (!row.acceptedLoad || clip.loadState != AudioDataLoadState.Loaded)
                        throw new InvalidOperationException("AudioClip data not loaded synchronously: " + path + ": " + row.finalState);
                    if (pcm != null)
                    {
                        PcmSource decoded = Array.Find(pcm.clips, value => value.name == source.name);
                        float[] samples = new float[clip.samples * clip.channels];
                        if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Could not read PCM: " + path);
                        byte[] bytes = new byte[samples.Length * 4];
                        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
                        row.pcmSha256 = Digest(bytes);
                        row.pcmMatchesDecodedSource = decoded != null && decoded.pcmSha256 == row.pcmSha256;
                        if (!row.pcmMatchesDecodedSource) throw new InvalidOperationException("Decoded PCM changed in native load: " + path);
                    }
                }
                result.pass = true;
            }
            catch (Exception error) { result.error = error.ToString(); throw; }
            finally
            {
                if (preparedBundle) preparedBundle.Unload(true);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(result, true) + "\n");
            }
        }
        private static string Digest(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
