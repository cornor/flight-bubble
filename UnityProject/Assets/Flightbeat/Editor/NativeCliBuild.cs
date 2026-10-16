using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class NativeCliBuild
    {
        // A diagnostic player is an additional native check; it does not replace
        // the user's Editor Play deliverable or modify source project settings.
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--flightbeat-native-build");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length)
                throw new ArgumentException("Batch mode and --flightbeat-native-build required");
            string output = Path.GetFullPath(args[flag + 1]);
            bool development = Array.IndexOf(args, "--flightbeat-native-release") < 0;
            bool probes = Array.IndexOf(args, "--flightbeat-native-production") < 0;
            if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Use a fresh build output.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool clean = Array.IndexOf(args, "--flightbeat-clean-build") >= 0;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { FlightbeatScenePreparation.ScenePath },
                target = BuildTarget.StandaloneOSX,
                locationPathName = output,
                options = (development ? BuildOptions.Development : BuildOptions.None) | (clean ? BuildOptions.CleanBuildCache : BuildOptions.None),
                extraScriptingDefines = probes ? new[] { "FLIGHTBEAT_CLI_PROBE" } : Array.Empty<string>()
            });
            File.WriteAllText(output + ".summary.json", JsonUtility.ToJson(new Summary
            {
                result = report.summary.result.ToString(), errors = report.summary.totalErrors,
                development = development,
                diagnosticTools = probes,
                warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds, unityVersion = Application.unityVersion
            }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Native build failed: " + report.summary.result);
            // An incremental switch from production can leave a stale assembly
            // load list even when the diagnostic DLL is shipped. Fail before run.
            string assemblies = File.ReadAllText(Path.Combine(output, "Contents/Resources/Data/ScriptingAssemblies.json"));
            if (assemblies.Contains("\"Flightbeat.DevTools.dll\"") != probes)
                throw new Exception("Native scripting assembly list is stale; rebuild with --flightbeat-clean-build");
        }
        [Serializable] private sealed class Summary
        {
            public string result, unityVersion;
            public bool development, diagnosticTools;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }
    }
}
