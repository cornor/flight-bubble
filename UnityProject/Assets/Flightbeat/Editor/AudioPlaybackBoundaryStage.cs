using System;
using System.IO;
using Flightbeat.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class AudioPlaybackBoundaryStage
    {
        [Serializable] private sealed class Summary { public string result, unityVersion; public int errors, warnings; public ulong bytes; public double seconds; }
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--flightbeat-native-build");
            if (!Application.isBatchMode || at < 0 || at + 1 >= args.Length) throw new ArgumentException("CLI output required");
            string output = Path.GetFullPath(args[at + 1]);
            if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Fresh build output required");
            const string scenePath = "Assets/Flightbeat/Scenes/AudioPlaybackBoundary.generated.unity";
            if (File.Exists(scenePath)) throw new IOException("Temporary scene already exists");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Native audio boundary fixture").AddComponent<NativeAudioPlaybackProbe>();
                var camera = new GameObject("Fixture camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Save isolated scene failed");
                File.Copy(scenePath, output + ".scene.yaml");
                File.Copy(scenePath + ".meta", output + ".scene.meta");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath }, target = BuildTarget.StandaloneOSX,
                    locationPathName = output, options = BuildOptions.None, extraScriptingDefines = new[] { "FLIGHTBEAT_CLI_PROBE" } });
                File.WriteAllText(output + ".summary.json", JsonUtility.ToJson(new Summary { result = report.summary.result.ToString(), errors = report.summary.totalErrors,
                    warnings = report.summary.totalWarnings, bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds, unityVersion = Application.unityVersion }, true));
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Native audio fixture build failed");
            }
            finally
            {
                try
                {
                    if (Array.Exists(setup, s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                    else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                finally { AssetDatabase.DeleteAsset(scenePath); }
            }
        }
    }
}
