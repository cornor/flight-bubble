using System;
using System.IO;
using Flightbeat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flightbeat.Editor
{
    public static class FlightbeatScenePreparation
    {
        public const string ScenePath = "Assets/Flightbeat/Scenes/PixelWing.unity";

        // Source: level0 GameObject pathId=1, Transform pathId=2, MonoBehaviour pathId=6.
        // See Evidence/original/structure/scene-hierarchy.json and T1.1-runtime-creation.json.
        // This creates only the serialized root. Runtime startup is in f54773's translation.
        [MenuItem("Flightbeat/Create source scene root")]
        public static void Create()
        {
            Require(Application.unityVersion == "2022.3.62f3", "fixed Editor version");
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "requires edit mode");
            string sceneDiskPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenePath));
            Require(!File.Exists(sceneDiskPath), "scene already exists; refusing to overwrite it");
            Require(EditorBuildSettings.scenes.Length == 0, "build scene list is already populated");
            FlightbeatProjectSettings.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(sceneDiskPath));
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            // A new batch process starts in Unity's unsaved default scene. Unity
            // refuses Additive creation from that state; replace only that clean
            // batch scene, retaining the existing preservation path for saved scenes.
            bool replaceBatchDefault = Application.isBatchMode && string.IsNullOrEmpty(previous.path) && !previous.isDirty;
            Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceBatchDefault ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(created);
                FlightbeatSceneEnvironment.ApplyKnownFields(created);
                GameObject root = new GameObject("PixelWing Game");
                root.layer = 0;
                root.tag = "Untagged";
                root.SetActive(true);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                root.AddComponent<PixelWingGame>().enabled = true;
                VerifyRoot(created);
                Require(EditorSceneManager.SaveScene(created, ScenePath), "scene save failed");
                FlightbeatSceneEnvironment.VerifyKnownFields(created);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                Debug.Log("Flightbeat serialized scene root saved. Startup integration is in progress; stage A has not passed Play Mode validation.");
            }
            finally
            {
                if (!replaceBatchDefault && previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(created, true);
                }
            }
        }

        // Root-only structural check. Scene rendering settings and dynamic objects
        // require separate import/runtime verification; they are not covered here.
        public static void VerifyRoot(Scene scene)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "root check requires edit mode");
            Require(scene.IsValid() && scene.isLoaded, "scene must be loaded");
            Require(scene.rootCount == 1, "exactly one serialized root");
            GameObject root = scene.GetRootGameObjects()[0];
            Require(root.name == "PixelWing Game", "source root name");
            Require(root.layer == 0 && root.CompareTag("Untagged") && root.activeSelf, "root flags");
            Transform transform = root.transform;
            Require(transform.parent == null && transform.childCount == 0, "no serialized parent or children");
            Require(transform.localPosition.Equals(Vector3.zero), "source position");
            Require(transform.localRotation.Equals(Quaternion.identity), "source rotation");
            Require(transform.localScale.Equals(Vector3.one), "source scale");
            Component[] components = root.GetComponents<Component>();
            Require(components.Length == 2, "source component count");
            Require(components[0] is Transform && components[1] is PixelWingGame, "source component order");
            Require(((PixelWingGame)components[1]).enabled, "source controller enabled");
        }

        private static void Require(bool condition, string detail)
        {
            if (!condition)
                throw new InvalidOperationException("Scene preparation: " + detail);
        }
    }
}
