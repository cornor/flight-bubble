using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class WebGlCliBuild
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--flightbeat-webgl-build");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length)
                throw new ArgumentException("Batch mode and --flightbeat-webgl-build required");
            string output = Path.GetFullPath(args[flag + 1]);
            bool garden = Array.IndexOf(args, "--flightbeat-garden-r1") >= 0;
            bool probes = Array.IndexOf(args, "--flightbeat-webgl-probes") >= 0;
            if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Use a fresh build output.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (garden)
            {
                GardenBattleUiAssets.RefreshSprites();
                GardenDefaultEvidence.Write(output + ".defaults.json");
            }
            // 2026-10-19 (size and speed): the WebGL player only ever opens OriginalAudio/flightbeat-original-audio (4.4 MB
            // AAC); the 28 MB float-PCM native bundle beside it is for the editor / native players. Anything under
            // Resources ships, so the native bundle is parked outside Assets for the duration of the build and put back
            // afterwards (also on the next build if a crash left it parked).
            RestoreNativeAudio();
            ParkNativeAudio();
            BuildReport report;
            try {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { FlightbeatScenePreparation.ScenePath },
                target = BuildTarget.WebGL,
                locationPathName = output,
                // 2026-10-19 (build speed): incremental by default, so IL2CPP / Emscripten reuse the C++ and objects of
                // unchanged code (Library/Bee). --flightbeat-clean-build forces the old full rebuild, e.g. after a
                // crashed build left a broken cache (the IL2CPP SIGBUS of 2026-10-17).
                options = Array.IndexOf(args, "--flightbeat-clean-build") >= 0 ? BuildOptions.CleanBuildCache : BuildOptions.None,
                extraScriptingDefines = garden ? (probes ? new[] { "FLIGHTBEAT_GARDEN_R1", "FLIGHTBEAT_CLI_PROBE" } : new[] { "FLIGHTBEAT_GARDEN_R1" }) : (probes ? new[] { "FLIGHTBEAT_CLI_PROBE" } : Array.Empty<string>())
            });
            } finally { RestoreNativeAudio(); }
            File.WriteAllText(output + ".summary.json", JsonUtility.ToJson(new Summary {
                gardenR1 = garden, result = report.summary.result.ToString(), unityVersion = Application.unityVersion,
                diagnosticTools = probes, errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds,
                compression = PlayerSettings.WebGL.compressionFormat.ToString(),
                decompressionFallback = PlayerSettings.WebGL.decompressionFallback,
                template = PlayerSettings.WebGL.template
            }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed: " + report.summary.result);
        }
        const string NativeAudio = "Assets/Flightbeat/Resources/OriginalAudio/flightbeat-native-audio.bytes";
        static string ProjectPath(string relative) => Path.Combine(Path.GetDirectoryName(Application.dataPath), relative);
        static string Parked(string relative) => ProjectPath(Path.Combine("NativeAudioParked", Path.GetFileName(relative)));
        static void MoveWithMeta(string from, string to)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(to));
            File.Move(from, to);
            if (File.Exists(from + ".meta")) File.Move(from + ".meta", to + ".meta");
        }
        static void ParkNativeAudio()
        {
            if (!File.Exists(ProjectPath(NativeAudio))) return;
            MoveWithMeta(ProjectPath(NativeAudio), Parked(NativeAudio));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        static void RestoreNativeAudio()
        {
            if (!File.Exists(Parked(NativeAudio)) || File.Exists(ProjectPath(NativeAudio))) return;
            MoveWithMeta(Parked(NativeAudio), ProjectPath(NativeAudio));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        [Serializable] private sealed class Summary
        {
            public string result, unityVersion, compression, template;
            public bool diagnosticTools, decompressionFallback, gardenR1;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }
    }
}
