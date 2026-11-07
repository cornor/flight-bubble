using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Only the dedicated Douyin project receives this editor script.
public sealed class DouyinBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => -10000;
    private const string SdkSymbol = "FLIGHTBEAT_TTSDK";
    private const string GameAppId = "tt6b371d8af54872c407";
    private const string AdIdPath = "Assets/Resources/DouyinRewardedAd.txt";
    private const string BuilderSettings = "Assets/Editor/StarkBuilderSetting.asset";

    [Serializable]
    private sealed class Package
    {
        public string name;
        public string version;
    }

    private static Version CheckOfficialPackage()
    {
        // BGDT retains the historical package identifier for modern TTSDK.
        const string packageName = "com.bytedance.starksdk";
        var registered = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
            .FirstOrDefault(pkg => pkg.name == packageName);
        var manifests = AssetDatabase.GetAllAssetPaths().Where(
            p => p.StartsWith("Assets/") && p.EndsWith("/" + packageName + "/package.json"));
        if (registered != null)
            manifests = manifests.Prepend(Path.Combine(registered.resolvedPath, "package.json"));
        foreach (string path in manifests)
        {
            var pkg = JsonUtility.FromJson<Package>(File.ReadAllText(path));
            if (pkg?.name == packageName && Version.TryParse(pkg.version, out var v)
                    && v >= new Version(6, 9, 6)) return v;
        }
        throw new BuildFailedException("请在官方 BGDT 的 cp 渠道安装 TTSDK 6.9.6 或更新稳定版。");
    }

    [MenuItem("泡泡小队/抖音/安装 TTSDK 后启用广告")]
    public static void Enable()
    {
        var version = CheckOfficialPackage();
        var settings = LoadSettings(out var id);
        if (id.stringValue != GameAppId)
        {
            id.stringValue = GameAppId;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings.targetObject);
        }
        var target = NamedBuildTarget.WebGL;
        var symbols = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => s.Length > 0);
        var required = new[] { "FLIGHTBEAT_GARDEN_R1", "FLIGHTBEAT_DOUYIN", SdkSymbol };
        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols.Union(required)));
        Debug.Log("已启用抖音 TTSDK " + version + " 的激励视频接入。");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        CheckOfficialPackage();
#if !FLIGHTBEAT_DOUYIN || !FLIGHTBEAT_TTSDK || !FLIGHTBEAT_GARDEN_R1
        throw new BuildFailedException("请选择 WebGL 平台，执行「泡泡小队/抖音/安装 TTSDK 后启用广告」并等待编译。");
#else
        if (report.summary.platform != BuildTarget.WebGL)
            throw new BuildFailedException("抖音工程使用 WebGL 目标。");
        bool localPreview = Application.isBatchMode && Environment.GetCommandLineArgs().Contains("-flightbeatDouyinLocalPreview");
        if (!localPreview && (!File.Exists(AdIdPath) || string.IsNullOrWhiteSpace(File.ReadAllText(AdIdPath))))
            throw new BuildFailedException("缺少激励视频广告位 ID：" + AdIdPath);
        var obj = LoadSettings(out var id);
        if (id.stringValue != GameAppId)
            throw new BuildFailedException("导出 AppID 与泡泡小队不一致，请重新执行「安装 TTSDK 后启用广告」。");
        var oldFormat = FindField(obj, "isoldbuildformat");
        if (oldFormat == null || oldFormat.propertyType != SerializedPropertyType.Boolean || oldFormat.boolValue)
            throw new BuildFailedException("未能确认 TTSDK 新包体格式，请检查 IsOldBuildFormat 配置。");
#endif
    }

    private static SerializedObject LoadSettings(out SerializedProperty id)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(BuilderSettings);
        if (asset == null)
            throw new BuildFailedException("请先打开官方 TTSDK 导出面板并保存配置，再执行「安装 TTSDK 后启用广告」。");
        var settings = new SerializedObject(asset);
        id = FindField(settings, "appid");
        if (id == null || id.propertyType != SerializedPropertyType.String)
            throw new BuildFailedException("无法识别 TTSDK 导出配置中的 AppID 字段。");
        return settings;
    }

    private static SerializedProperty FindField(SerializedObject obj, string name)
    {
        var field = obj.GetIterator();
        while (field.Next(true))
        {
            string key = field.name.StartsWith("m_") ? field.name.Substring(2) : field.name;
            if (key.TrimStart('_').Equals(name, StringComparison.OrdinalIgnoreCase)) return field.Copy();
        }
        return null;
    }
}
