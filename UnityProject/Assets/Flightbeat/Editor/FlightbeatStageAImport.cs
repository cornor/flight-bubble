using System.IO;
using UnityEditor.SceneManagement;

namespace Flightbeat.Editor
{
    // One CLI process for the existing source settings/resource/scene preparation.
    // Import checks only; does not claim stage A or Play Mode acceptance.
    public static class FlightbeatStageAImport
    {
        public static void Run()
        {
            FlightbeatProjectSettings.Apply();
            OriginalTextureImportVerification.Run();
            OriginalMaterialImportVerification.Run();
            if (!File.Exists(FlightbeatScenePreparation.ScenePath))
                FlightbeatScenePreparation.Create();
            else
            {
                var scene = EditorSceneManager.OpenScene(FlightbeatScenePreparation.ScenePath);
                FlightbeatScenePreparation.VerifyRoot(scene);
                FlightbeatSceneEnvironment.VerifyKnownFields(scene);
            }
        }
    }
}
