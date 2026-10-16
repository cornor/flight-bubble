using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Editor-only serialized configuration readback; never renders or runs gameplay.
    public static class SceneEnvironmentReadback
    {
        [Serializable] private sealed class ObjectRecord
        {
            public string label, serializedJson;
            public string[] propertyPaths;
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public ObjectRecord[] objects;
        }

        public static UnityEngine.Object Settings(Type type, string method)
        {
            MethodInfo member = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (member == null) throw new MissingMethodException(type.FullName, method);
            return (UnityEngine.Object)member.Invoke(null, null);
        }

        public static void Inspect()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-sceneEnvironmentOutput");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -sceneEnvironmentOutput");
            string output = Path.GetFullPath(args[index + 1]);
            if (File.Exists(output)) throw new IOException("Refusing to overwrite scene evidence: " + output);
            var scene = EditorSceneManager.OpenScene(FlightbeatScenePreparation.ScenePath);
            FlightbeatScenePreparation.VerifyRoot(scene);
            FlightbeatSceneEnvironment.VerifyKnownFields(scene);
            var records = new List<ObjectRecord>();
            records.Add(Read("RenderSettings", Settings(typeof(RenderSettings), "GetRenderSettings")));
            records.Add(Read("LightmapSettings", Settings(typeof(LightmapEditorSettings), "GetLightmapSettings")));
            records.Add(Read("NavMeshSettings", Settings(typeof(UnityEditor.AI.NavMeshBuilder), "get_navMeshSettingsObject")));
            if (RenderSettings.skybox) records.Add(Read("SkyboxMaterial", RenderSettings.skybox));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { unityVersion = Application.unityVersion, objects = records.ToArray() }, true) + "\n");
        }

        private static ObjectRecord Read(string label, UnityEngine.Object obj)
        {
            if (!obj) throw new InvalidDataException("Missing settings object: " + label);
            var paths = new List<string>();
            using (var data = new SerializedObject(obj))
            {
                var property = data.GetIterator();
                while (property.Next(true)) paths.Add(property.propertyPath + " : " + property.propertyType);
            }
            return new ObjectRecord { label = label, serializedJson = EditorJsonUtility.ToJson(obj, true), propertyPaths = paths.ToArray() };
        }
    }
}
