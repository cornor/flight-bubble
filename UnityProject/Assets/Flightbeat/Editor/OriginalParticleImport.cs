using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Flightbeat.Feedback;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flightbeat.Editor
{
    // CLI-only preparation of native assets, using audited original serialized data.
    // The runtime loads ordinary Prefabs; no dependency on the original project or WASM.
    public static class OriginalParticleImport
    {
        [Serializable] private sealed class Binding { public string token, assetPath; }
        [Serializable] private sealed class SourceObject
        {
            public long sourcePathId, localFileId;
            public string type, sourceObjectSha256;
        }
        [Serializable] private sealed class SourcePrefab
        {
            public string name, sourcePath, sourceSha256, templatePath, templateSha256;
            public string assetPath, guid, resourcePath;
            public Binding[] bindings;
            public SourceObject[] objects;
        }
        [Serializable] private sealed class Manifest { public int version; public SourcePrefab[] prefabs; }
        [Serializable] private sealed class Snapshot
        {
            public long sourcePathId, localFileId;
            public string type, json;
            public int instanceId;
            // EditorJsonUtility omits structural GameObject/Transform and script references.
            public int gameObjectInstanceId, parentInstanceId, scriptInstanceId;
            public int[] childInstanceIds, componentInstanceIds;
            // These player-only serialized fields are exposed through the native Renderer API.
            public int lightmapIndex, realtimeLightmapIndex;
            public Vector4 lightmapScaleOffset, realtimeLightmapScaleOffset;
        }
        [Serializable] private sealed class AssetSnapshot
        {
            public string name, assetPath, guid, prefabSha256;
            public int particleSystems, smokeParticleCount;
            public bool resourceLoaded, smokeCleared;
            public List<Snapshot> objects = new List<Snapshot>();
        }
        [Serializable] private sealed class External
        {
            public string assetPath, guid;
            public long localFileId;
            public int instanceId;
        }
        [Serializable] private sealed class Report
        {
            public bool importAndSmokePassed;
            public bool sourceReadbackComparisonPassed; // Set by the external source comparator, not this process.
            public bool playModeExecuted, visualAcceptance;
            public string unityVersion, error;
            public List<AssetSnapshot> prefabs = new List<AssetSnapshot>();
            public List<External> externalReferences = new List<External>();
        }

        public static void Run()
        {
            string input = Argument("-flightbeatParticleInput"), output = Argument("-flightbeatParticleReport");
            var report = new Report { unityVersion = Application.unityVersion };
            try
            {
                Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(input));
                Require(manifest.version == 1 && manifest.prefabs.Length == 16, "original particle manifest");
                string repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
                var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (SourcePrefab source in manifest.prefabs)
                {
                    Require(source.assetPath == "Assets/Flightbeat/Resources/particles/" + source.name + ".prefab", "particle path");
                    Require(Hash(Path.Combine(repository, source.sourcePath)) == source.sourceSha256, "original source SHA-256");
                    Require(Hash(source.templatePath) == source.templateSha256, "audited template SHA-256");
                    string yaml = File.ReadAllText(source.templatePath);
                    foreach (Binding binding in source.bindings)
                    {
                        if (!bindings.TryGetValue(binding.assetPath, out string reference))
                        {
                            Object asset = AssetDatabase.LoadMainAssetAtPath(binding.assetPath);
                            Require(asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long _), "dependency identity");
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long fileId);
                            reference = "{fileID: " + fileId + ", guid: " + guid + ", type: 3}";
                            bindings.Add(binding.assetPath, reference);
                            report.externalReferences.Add(new External { assetPath = binding.assetPath, guid = guid,
                                localFileId = fileId, instanceId = asset.GetInstanceID() });
                        }
                        Require(yaml.Contains(binding.token), "binding slot");
                        yaml = yaml.Replace(binding.token, reference);
                    }
                    Require(!yaml.Contains("__FLIGHTBEAT_ASSET_"), "all external references resolved");
                    WriteGenerated(source.assetPath, yaml);
                    WriteGenerated(source.assetPath + ".meta", "fileFormatVersion: 2\nguid: " + source.guid +
                        "\nPrefabImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n");
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                int total = 0;
                foreach (SourcePrefab source in manifest.prefabs)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source.assetPath);
                    Require(prefab && prefab.name == source.name, "prefab name");
                    Require(Resources.Load<GameObject>(source.resourcePath) == prefab, "f54380 Resources key");
                    Require(prefab.GetComponent<PixelEffect>(), "translated PixelEffect script");
                    var expected = new Dictionary<long, SourceObject>();
                    foreach (SourceObject item in source.objects) expected.Add(item.localFileId, item);
                    var row = new AssetSnapshot { name = source.name, assetPath = source.assetPath, guid = source.guid,
                        prefabSha256 = Hash(source.assetPath), resourceLoaded = true };
                    foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
                    {
                        Capture(transform.gameObject, source, expected, row);
                        foreach (Component component in transform.GetComponents<Component>())
                        {
                            Require(component, "no missing script");
                            Capture(component, source, expected, row);
                            if (component is ParticleSystem) row.particleSystems++;
                        }
                    }
                    Require(row.objects.Count == expected.Count, "all original objects retained");
                    // Native resource/lifecycle smoke only; no claim about rendered particles or original timing.
                    GameObject instance = Object.Instantiate(prefab);
                    try
                    {
                        PixelEffect effect = instance.GetComponent<PixelEffect>();
                        effect.Initialize(source.name); // f54370
                        effect.PlayAt(Vector3.zero, 1f, Color.white); // f54371
                        instance.GetComponent<ParticleSystem>().Simulate(0.02f, true, false, false);
                        row.smokeParticleCount = effect.ParticleCount;
                        effect.Freeze(true); // f54373
                        effect.Clear(); // f54374
                        Require(effect.ParticleCount == 0 && !effect.Alive, "native Clear");
                        row.smokeCleared = true;
                    }
                    finally { Object.DestroyImmediate(instance); }
                    total += row.particleSystems;
                    report.prefabs.Add(row);
                }
                Require(total == 91, "91 original ParticleSystems");
                report.importAndSmokePassed = true;
            }
            catch (Exception error) { report.error = error.ToString(); throw; }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
            }
        }

        private static void Capture(Object value, SourcePrefab source, Dictionary<long, SourceObject> expected, AssetSnapshot row)
        {
            Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long fileId), "native object identity");
            Require(guid == source.guid && expected.TryGetValue(fileId, out SourceObject _), "source-to-native object mapping");
            SourceObject item = expected[fileId];
            Require(value.GetType().Name == (item.type == "MonoBehaviour" ? "PixelEffect" : item.type), "component type");
            var snapshot = new Snapshot { sourcePathId = item.sourcePathId, localFileId = fileId, type = item.type,
                instanceId = value.GetInstanceID(), json = EditorJsonUtility.ToJson(value) };
            if (value is GameObject gameObject)
            {
                Component[] components = gameObject.GetComponents<Component>();
                snapshot.componentInstanceIds = new int[components.Length];
                for (int i = 0; i < components.Length; i++) snapshot.componentInstanceIds[i] = components[i].GetInstanceID();
            }
            if (value is Component component) snapshot.gameObjectInstanceId = component.gameObject.GetInstanceID();
            if (value is Transform transform)
            {
                snapshot.parentInstanceId = transform.parent ? transform.parent.GetInstanceID() : 0;
                snapshot.childInstanceIds = new int[transform.childCount];
                for (int i = 0; i < transform.childCount; i++) snapshot.childInstanceIds[i] = transform.GetChild(i).GetInstanceID();
            }
            if (value is PixelEffect effect) snapshot.scriptInstanceId = MonoScript.FromMonoBehaviour(effect).GetInstanceID();
            if (value is ParticleSystemRenderer renderer)
            {
                snapshot.lightmapIndex = renderer.lightmapIndex;
                snapshot.realtimeLightmapIndex = renderer.realtimeLightmapIndex;
                snapshot.lightmapScaleOffset = renderer.lightmapScaleOffset;
                snapshot.realtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset;
            }
            row.objects.Add(snapshot);
        }
        private static void WriteGenerated(string path, string text)
        {
            if (File.Exists(path)) Require(File.ReadAllText(path) == text, "refuse to overwrite edited asset: " + path);
            else File.WriteAllText(path, text, new UTF8Encoding(false));
        }
        private static string Hash(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            throw new ArgumentException("Missing " + name);
        }
        private static void Require(bool condition, string what)
        {
            if (!condition) throw new InvalidDataException("Original particle import mismatch: " + what);
        }
    }
}
