#if FLIGHTBEAT_TTSDK
using System;
using System.IO;
using Flightbeat.Editor;
using TTSDK.Tool;
using UnityEditor;
using UnityEngine;

public static class FlightPreviewExport
{
    public static void Configure()
    {
        var settings = StarkBuilderSettings.Instance;
        settings.framework = Framework.Wasm;
        settings.wasmSubFramework = WasmSubFramework.WebGL;
        settings.isOldBuildFormat = false;
        settings.orientation = StarkBuilderSettings.Orientation.Portrait;
        settings.OutputDir = Path.GetFullPath("../DouyinPreview");
        settings.needCompress = true;
        settings.dataLoadType = DataLoadType.Package;
        settings.buildOptions = BuildOptions.None;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        DouyinBuildCheck.Enable();
    }

    public static async void Run()
    {
        var settings = StarkBuilderSettings.Instance;
        string configuredOutput = settings.OutputDir;
        string staging = Environment.GetEnvironmentVariable("FLIGHTBEAT_DOUYIN_OUTPUT");
        int exitCode = 1;
        try
        {
            if (!string.IsNullOrEmpty(staging)) settings.OutputDir = staging;
            GardenBattleUiAssets.RefreshSprites();
            AssetDatabase.RegisterCustomDependency(GardenAssetImport.FormatDependency, Hash128.Compute("astc4x4"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string output = await TTSDK.Tool.API.BuildManager.Build(Framework.Wasm, false);
            Debug.Log("FLIGHT_PREVIEW_OUTPUT " + output);
            exitCode = string.IsNullOrWhiteSpace(output) ? 2 : 0;
        }
        catch (Exception ex) { Debug.LogException(ex); }
        finally
        {
            settings.OutputDir = configuredOutput;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(exitCode);
        }
    }
}
#endif
