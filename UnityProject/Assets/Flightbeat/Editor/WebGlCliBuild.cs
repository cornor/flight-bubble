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
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { FlightbeatScenePreparation.ScenePath },
                target = BuildTarget.WebGL,
                locationPathName = output,
                options = BuildOptions.CleanBuildCache,
                extraScriptingDefines = garden ? (probes ? new[] { "FLIGHTBEAT_GARDEN_R1", "FLIGHTBEAT_CLI_PROBE" } : new[] { "FLIGHTBEAT_GARDEN_R1" }) : (probes ? new[] { "FLIGHTBEAT_CLI_PROBE" } : Array.Empty<string>())
            });
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
