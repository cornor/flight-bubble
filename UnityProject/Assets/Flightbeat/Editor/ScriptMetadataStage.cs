#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Independent observation; original answers never enter the Unity process.
    public static class ScriptMetadataStage
    {
        [Serializable] private sealed class Bytes { public string file, sha256; public int bytes; }
        [Serializable] private sealed class Call
        {
            public Bytes FilePathsData, TypesData;
            public int TotalTypes, TotalFiles;
            public bool IsEditorOnly;
        }
        [Serializable] private sealed class Report
        {
            public string error = "", unityVersion, scope = "Original metadata compatibility entry, independently invoked by reflection. This does not replace Unity's current-project script registrar or assert managed/native layout identity.";
            public bool completed, freshArrays, constructorDistinctObjects;
            public int constructorDeclaredInstanceFields;
            public List<int> retainedMutations = new List<int>();
            public List<Call> calls = new List<Call>();
        }
        private static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        private static Bytes Save(string output, int index, string name, byte[] value)
        {
            string file = "get-" + index + "-" + name + ".bin";
            using (var stream = new FileStream(Path.Combine(output, file), FileMode.CreateNew)) stream.Write(value, 0, value.Length);
            using (var hash = SHA256.Create()) return new Bytes { file = file, bytes = value.Length,
                sha256 = BitConverter.ToString(hash.ComputeHash(value)).Replace("-", "").ToLowerInvariant() };
        }
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "--flightbeat-script-metadata-output");
            if (arg < 0 || arg + 1 >= args.Length) throw new ArgumentException("Fresh metadata output required");
            string output = Path.GetFullPath(args[arg + 1]);
            if (Directory.Exists(output)) throw new IOException("Output already exists");
            Directory.CreateDirectory(output);
            var report = new Report { unityVersion = Application.unityVersion };
            try
            {
                var type = typeof(PixelWingGame).Assembly.GetType("Flightbeat.Compatibility.OriginalMonoScriptMetadata", true);
                var get = type.GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static);
                if (get == null || get.GetParameters().Length != 0) throw new Exception("Metadata entry signature mismatch");
                var arrays = new List<byte[]>();
                for (int i = 0; i < 3; i++)
                {
                    object value = get.Invoke(null, null);
                    var files = (byte[])Field(value, "FilePathsData");
                    var types = (byte[])Field(value, "TypesData");
                    arrays.Add(files); arrays.Add(types);
                    report.calls.Add(new Call { FilePathsData = Save(output, i, "FilePathsData", files),
                        TypesData = Save(output, i, "TypesData", types), TotalTypes = (int)Field(value, "TotalTypes"),
                        TotalFiles = (int)Field(value, "TotalFiles"), IsEditorOnly = (bool)Field(value, "IsEditorOnly") });
                    if (i < 2)
                    {
                        files[i == 0 ? 0 : files.Length - 1] ^= 1;
                        types[i == 0 ? 0 : types.Length - 1] ^= 1;
                    }
                }
                report.freshArrays = true;
                for (int i = 0; i < arrays.Count; i++) for (int j = 0; j < i; j++)
                    if (ReferenceEquals(arrays[i], arrays[j])) report.freshArrays = false;
                report.retainedMutations.Add(arrays[0][0]); report.retainedMutations.Add(arrays[1][0]);
                report.retainedMutations.Add(arrays[2][arrays[2].Length - 1]); report.retainedMutations.Add(arrays[3][arrays[3].Length - 1]);
                report.constructorDistinctObjects = !ReferenceEquals(Activator.CreateInstance(type), Activator.CreateInstance(type));
                report.constructorDeclaredInstanceFields = type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic).Length;
                report.completed = true;
            }
            catch (Exception e) { report.error = e.ToString(); }
            finally { File.WriteAllText(Path.Combine(output, "metadata.json"), JsonUtility.ToJson(report, true)); }
            if (!report.completed) throw new Exception("Metadata observation failed: " + report.error);
        }
    }
}
#endif
