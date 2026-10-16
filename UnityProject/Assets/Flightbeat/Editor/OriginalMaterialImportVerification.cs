using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Batch entry point; native import/property checks only, not rendered pixels.
    public static class OriginalMaterialImportVerification
    {
        [Serializable] private sealed class Result
        {
            public bool pass;
            public bool EditorExecuted = true;
            public bool PixelValidationExecuted = false;
            public bool WebGLBuilt = false;
            public string error;
            public List<string> imported = new List<string>();
        }

        public static void Run()
        {
            string output = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-flightbeatMaterialReport") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -flightbeatMaterialReport.");
            var result = new Result();
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var paths = Directory.GetFiles("Assets/Flightbeat/Resources", "*.fbmaterial", SearchOption.AllDirectories);
                Array.Sort(paths, StringComparer.Ordinal);
                if (paths.Length != 4) throw new InvalidDataException("Expected four original material descriptors.");
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    if (!(AssetImporter.GetAtPath(path) is OriginalMaterialImporter))
                        throw new InvalidDataException("Original material importer was not selected: " + path);
                    var source = JsonUtility.FromJson<OriginalMaterialImporter.Source>(File.ReadAllText(path));
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    OriginalMaterialImporter.Verify(material, source);
                    if (Resources.Load<Material>(source.resourcePath) != material)
                        throw new InvalidDataException("Typed Resources material path mismatch: " + path);
                    string shaderResource = source.shaderPath.Substring("Assets/Flightbeat/Resources/".Length);
                    shaderResource = shaderResource.Substring(0, shaderResource.Length - ".shader".Length);
                    if (Resources.Load<Shader>(shaderResource) != material.shader)
                        throw new InvalidDataException("Typed Resources shader path mismatch: " + path);
                    if (ShaderUtil.ShaderHasError(material.shader))
                        throw new InvalidDataException("Unity reported shader errors: " + source.shaderPath);
                    result.imported.Add(path);
                }
                result.pass = true;
            }
            catch (Exception e) { result.error = e.ToString(); throw; }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(result, true) + "\n");
            }
        }
    }
}
