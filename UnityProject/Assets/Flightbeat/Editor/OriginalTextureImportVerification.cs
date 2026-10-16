using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Rendering;
using UnityEditor;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Batch entry point: actual Editor import/load and public-parameter validation.
    // This does not claim GPU pixel, native serialized-payload or WebGL equality.
    public static class OriginalTextureImportVerification
    {
        [Serializable] private sealed class Item
        {
            public string path, guid, sourceFile, name, resourcePath, graphicsFormat;
            public long sourcePathId;
            public int width, height, format, mipCount;
            public bool resourceLoaded, dataSRGB;
        }

        [Serializable] private sealed class Result
        {
            public bool pass, EditorExecuted = true, NativeImportExecuted, PixelValidationExecuted, WebGLBuilt;
            public string error, activeColorSpace, graphicsDevice;
            public List<Item> textures = new List<Item>();
        }

        public static void Run()
        {
            string output = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-flightbeatTextureReport") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -flightbeatTextureReport path.");
            var result = new Result { activeColorSpace = QualitySettings.activeColorSpace.ToString(),
                graphicsDevice = SystemInfo.graphicsDeviceType.ToString() };
            try
            {
                string[] paths = Directory.GetFiles("Assets/Flightbeat", "*.fbtexture", SearchOption.AllDirectories);
                Array.Sort(paths, StringComparer.Ordinal);
                if (paths.Length != 17) throw new InvalidOperationException("Expected 17 original texture sources.");
                foreach (string path in paths)
                {
                    var source = OriginalTextureData.Load(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    if (!(AssetImporter.GetAtPath(path) is OriginalTextureImporter))
                        throw new InvalidOperationException("Original texture importer was not selected: " + path);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (!texture) throw new InvalidOperationException("Texture import produced no Texture2D: " + path);
                    OriginalTextureImporter.Verify(texture, source);
                    bool resourceLoaded = false;
                    if (!string.IsNullOrEmpty(source.ResourcePath))
                    {
                        resourceLoaded = Resources.Load<Texture2D>(source.ResourcePath) == texture;
                        if (!resourceLoaded) throw new InvalidOperationException("Resources path mismatch: " + source.ResourcePath);
                    }
                    result.textures.Add(new Item { path = path, guid = AssetDatabase.AssetPathToGUID(path),
                        sourceFile = source.SourceFile, sourcePathId = source.PathId, name = source.Name,
                        resourcePath = source.ResourcePath, width = texture.width, height = texture.height,
                        format = (int)texture.format, mipCount = texture.mipmapCount, resourceLoaded = resourceLoaded,
                        dataSRGB = texture.isDataSRGB, graphicsFormat = texture.graphicsFormat.ToString() });
                }
                result.NativeImportExecuted = true;
                result.pass = true;
            }
            catch (Exception e)
            {
                result.error = e.ToString();
                throw;
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(result, true) + "\n");
            }
        }
    }
}
