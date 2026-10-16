using System;
using System.IO;
using System.Linq;
using Flightbeat.Diagnostics;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class DevToolsBoundaryStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, editorToolsRegistered, runtimeHasNoToolsReference, defaultPlayerExcludesTools, sceneRootValid;
            public string runtimeAssembly, toolsAssembly, controllerGuid;
            public string[] defaultPlayerAssemblies, optionalTypes;
        }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--flightbeat-devtools-report");
            if (!Application.isBatchMode || at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit CLI report required");
            string path = Path.GetFullPath(args[at + 1]); if (File.Exists(path)) throw new IOException("Fresh report required");
            var runtime = typeof(PixelWingGame).Assembly;
            Type[] types = { typeof(BroadcastPilot), typeof(TuningKnob), typeof(ReviewCapture), typeof(OnboardingCapture), typeof(FleetDeveloper), typeof(NativeAudioPlaybackProbe) };
            var tools = types[0].Assembly;
            var report = new Report { runtimeAssembly = runtime.GetName().Name, toolsAssembly = tools.GetName().Name,
                optionalTypes = types.Select(t => t.FullName).ToArray(), editorToolsRegistered = DevelopmentTools.Available,
                runtimeHasNoToolsReference = runtime.GetReferencedAssemblies().All(a => a.Name != "Flightbeat.DevTools"),
                defaultPlayerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name).OrderBy(n => n).ToArray() };
            report.defaultPlayerExcludesTools = !report.defaultPlayerAssemblies.Contains("Flightbeat.DevTools") && !report.defaultPlayerAssemblies.Contains("Flightbeat.Editor");
            if (report.runtimeAssembly != "Flightbeat.Runtime" || report.toolsAssembly != "Flightbeat.DevTools" || types.Any(t => t.Assembly != tools)
                || !report.editorToolsRegistered || !report.runtimeHasNoToolsReference || !report.defaultPlayerExcludesTools)
                throw new InvalidOperationException("Assembly boundary differs from T9.3");
            var scene = EditorSceneManager.OpenScene(FlightbeatScenePreparation.ScenePath, OpenSceneMode.Single);
            FlightbeatScenePreparation.VerifyRoot(scene); report.sceneRootValid = true;
            var script = MonoScript.FromMonoBehaviour(scene.GetRootGameObjects()[0].GetComponent<PixelWingGame>());
            report.controllerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
            if (script.GetClass() != typeof(PixelWingGame)) throw new InvalidOperationException("Serialized controller binding changed");
            report.passed = true; Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }
    }
}
