// Generated from frozen level0 serialized fields, not observed screenshots.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Flightbeat.Editor
{
    public static class FlightbeatSceneEnvironment
    {
        // These three fields are absent from Editor SerializedObject. Check them
        // in the actual player level0, not by inventing an Editor setter. Stage CD
        // verifies all three against the source in the existing NativeRelease-v2.
        public const string PlayerSerializedFields = "RenderSettings.m_GeneratedSkyboxReflection; RenderSettings.m_IndirectSpecularColor; LightmapSettings.m_EnlightenSceneMapping";

        public static void ApplyKnownFields(Scene scene)
        {
            RequireEditScene(scene);
            // src: level0 pathId=3 m_Fog
            RenderSettings.fog = false;
            // src: level0 pathId=3 m_FogColor
            RenderSettings.fogColor = new Color(F(0x3f000000u), F(0x3f000000u), F(0x3f000000u), F(0x3f800000u));
            // src: level0 pathId=3 m_FogMode
            RenderSettings.fogMode = (FogMode)3;
            // src: level0 pathId=3 m_FogDensity
            RenderSettings.fogDensity = F(0x3c23d70au);
            // src: level0 pathId=3 m_LinearFogStart
            RenderSettings.fogStartDistance = F(0x00000000u);
            // src: level0 pathId=3 m_LinearFogEnd
            RenderSettings.fogEndDistance = F(0x43960000u);
            // src: level0 pathId=3 m_AmbientSkyColor
            RenderSettings.ambientSkyColor = new Color(F(0x3e591687u), F(0x3e6872b0u), F(0x3e849ba6u), F(0x3f800000u));
            // src: level0 pathId=3 m_AmbientEquatorColor
            RenderSettings.ambientEquatorColor = new Color(F(0x3de978d5u), F(0x3e000000u), F(0x3e083127u), F(0x3f800000u));
            // src: level0 pathId=3 m_AmbientGroundColor
            RenderSettings.ambientGroundColor = new Color(F(0x3d408312u), F(0x3d3020c5u), F(0x3d0f5c29u), F(0x3f800000u));
            // src: level0 pathId=3 m_AmbientIntensity
            RenderSettings.ambientIntensity = F(0x3f800000u);
            // src: level0 pathId=3 m_AmbientMode
            RenderSettings.ambientMode = (AmbientMode)0;
            // src: level0 pathId=3 m_SubtractiveShadowColor
            RenderSettings.subtractiveShadowColor = new Color(F(0x3ed70a3du), F(0x3ef4bc6au), F(0x3f208312u), F(0x3f800000u));
            // src: level0 pathId=3 m_HaloStrength
            RenderSettings.haloStrength = F(0x3f000000u);
            // src: level0 pathId=3 m_FlareStrength
            RenderSettings.flareStrength = F(0x3f800000u);
            // src: level0 pathId=3 m_FlareFadeSpeed
            RenderSettings.flareFadeSpeed = F(0x40400000u);
            // src: level0 pathId=3 m_DefaultReflectionMode
            RenderSettings.defaultReflectionMode = (DefaultReflectionMode)0;
            // src: level0 pathId=3 m_DefaultReflectionResolution
            RenderSettings.defaultReflectionResolution = 128;
            // src: level0 pathId=3 m_ReflectionBounces
            RenderSettings.reflectionBounces = 1;
            // src: level0 pathId=3 m_ReflectionIntensity
            RenderSettings.reflectionIntensity = F(0x3f800000u);
            // src: level0 pathId=3 m_AmbientProbe: all 27 source coefficients are +0.
            RenderSettings.ambientProbe = new SphericalHarmonicsL2();
            // src: level0 pathId=3 m_Sun: null.
            RenderSettings.sun = null;
            // src: level0 pathId=4 m_Lightmaps: empty; m_LightmapsMode: source enum.
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            LightmapSettings.lightmapsMode = (LightmapsMode)1;
            EditorSceneManager.MarkSceneDirty(scene);
            VerifyKnownFields(scene);
            Debug.Log("Scene environment Editor fields verified. Player-only serialization requires package readback: " + PlayerSerializedFields);
        }

        public static void VerifyKnownFields(Scene scene)
        {
            RequireEditScene(scene);
            Require(RenderSettings.fog == false, "m_Fog");
            Require(Same(RenderSettings.fogColor, new Color(F(0x3f000000u), F(0x3f000000u), F(0x3f000000u), F(0x3f800000u))), "m_FogColor");
            Require(RenderSettings.fogMode == (FogMode)3, "m_FogMode");
            Require(Same(RenderSettings.fogDensity, F(0x3c23d70au)), "m_FogDensity");
            Require(Same(RenderSettings.fogStartDistance, F(0x00000000u)), "m_LinearFogStart");
            Require(Same(RenderSettings.fogEndDistance, F(0x43960000u)), "m_LinearFogEnd");
            Require(Same(RenderSettings.ambientSkyColor, new Color(F(0x3e591687u), F(0x3e6872b0u), F(0x3e849ba6u), F(0x3f800000u))), "m_AmbientSkyColor");
            Require(Same(RenderSettings.ambientEquatorColor, new Color(F(0x3de978d5u), F(0x3e000000u), F(0x3e083127u), F(0x3f800000u))), "m_AmbientEquatorColor");
            Require(Same(RenderSettings.ambientGroundColor, new Color(F(0x3d408312u), F(0x3d3020c5u), F(0x3d0f5c29u), F(0x3f800000u))), "m_AmbientGroundColor");
            Require(Same(RenderSettings.ambientIntensity, F(0x3f800000u)), "m_AmbientIntensity");
            Require(RenderSettings.ambientMode == (AmbientMode)0, "m_AmbientMode");
            Require(Same(RenderSettings.subtractiveShadowColor, new Color(F(0x3ed70a3du), F(0x3ef4bc6au), F(0x3f208312u), F(0x3f800000u))), "m_SubtractiveShadowColor");
            Require(Same(RenderSettings.haloStrength, F(0x3f000000u)), "m_HaloStrength");
            Require(Same(RenderSettings.flareStrength, F(0x3f800000u)), "m_FlareStrength");
            Require(Same(RenderSettings.flareFadeSpeed, F(0x40400000u)), "m_FlareFadeSpeed");
            Require(RenderSettings.defaultReflectionMode == (DefaultReflectionMode)0, "m_DefaultReflectionMode");
            Require(RenderSettings.defaultReflectionResolution == 128, "m_DefaultReflectionResolution");
            Require(RenderSettings.reflectionBounces == 1, "m_ReflectionBounces");
            Require(Same(RenderSettings.reflectionIntensity, F(0x3f800000u)), "m_ReflectionIntensity");
            SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    Require(Same(probe[channel, coefficient], F(0u)), "m_AmbientProbe coefficient");
            Require(RenderSettings.sun == null, "m_Sun");
            Require(LightmapSettings.lightmaps.Length == 0, "m_Lightmaps");
            Require((int)LightmapSettings.lightmapsMode == 1, "m_LightmapsMode");
            // Public read only: reject a nonempty default instead of assuming it.
            Require(LightmapSettings.lightProbes == null, "m_LightProbes");
            VerifyEditorReferences();
        }

        // src: level0 pathIds 3/4/5; sharedassets0.assets Material pathId 2.
        // Stage CD resolves the original pointers and proves the built player
        // settings/material bytes match. Editor defaults are checked, not assumed.
        // No pixel, compiled shader or decoded texture equivalence is asserted.
        private static void VerifyEditorReferences()
        {
            using (var data = new SerializedObject(SceneEnvironmentReadback.Settings(typeof(RenderSettings), "GetRenderSettings")))
            {
                Require(Field(data, "m_HaloTexture").objectReferenceValue == null, "m_HaloTexture");
                Require(Field(data, "m_CustomReflection").objectReferenceValue == null, "m_CustomReflection");
                Require(!Field(data, "m_UseRadianceAmbientProbe").boolValue, "m_UseRadianceAmbientProbe");
                // Original level0 external 3 = Library/unity default resources,
                // Texture2D pathId 10001 (Soft). Candidate keeps that identity.
                VerifyBuiltin(Field(data, "m_SpotCookie").objectReferenceValue, "0000000000000000e000000000000000", 10001, "m_SpotCookie");
            }
            using (var data = new SerializedObject(SceneEnvironmentReadback.Settings(typeof(LightmapEditorSettings), "GetLightmapSettings")))
            {
                Require(Field(data, "m_LightingSettings").objectReferenceValue == null, "m_LightingSettings");
                // No lighting data was authored in the source. Reject an Editor
                // bake asset that could populate the player-only mapping/probes.
                Require(Field(data, "m_LightingDataAsset").objectReferenceValue == null, "unexpected lighting bake asset");
                foreach (string name in new[] { "m_BounceScale", "m_IndirectOutputScale", "m_AlbedoBoost" })
                    Require(Same(Field(data, "m_GISettings." + name).floatValue, F(0x3f800000u)), name);
                Require(Field(data, "m_GISettings.m_EnvironmentLightingMode").intValue == 0, "m_EnvironmentLightingMode");
                Require(Field(data, "m_GISettings.m_EnableBakedLightmaps").boolValue, "m_EnableBakedLightmaps");
                Require(!Field(data, "m_GISettings.m_EnableRealtimeLightmaps").boolValue, "m_EnableRealtimeLightmaps");
            }
            using (var data = new SerializedObject(SceneEnvironmentReadback.Settings(typeof(UnityEditor.AI.NavMeshBuilder), "get_navMeshSettingsObject")))
                Require(Field(data, "m_NavMeshData").objectReferenceValue == null, "m_NavMeshData");

            Material skybox = RenderSettings.skybox;
            VerifyBuiltin(skybox, "0000000000000000f000000000000000", 10304, "m_SkyboxMaterial");
            Require(skybox.name == "Default-Skybox" && skybox.shader && skybox.shader.name == "Skybox/Procedural", "source skybox shader binding");
            using (var data = new SerializedObject(skybox))
            {
                var valid = Field(data, "m_ValidKeywords");
                Require(valid.arraySize == 1 && valid.GetArrayElementAtIndex(0).stringValue == "_SUNDISK_SIMPLE", "skybox keyword");
                foreach (string name in new[] { "m_InvalidKeywords", "disabledShaderPasses", "stringTagMap", "m_BuildTextureStacks", "m_SavedProperties.m_TexEnvs", "m_SavedProperties.m_Ints" })
                    Require(Field(data, name).arraySize == 0, "skybox " + name);
                Require(Field(data, "m_LightmapFlags").intValue == 5 && Field(data, "m_CustomRenderQueue").intValue == -1, "skybox flags/queue");
                Require(!Field(data, "m_EnableInstancingVariants").boolValue && !Field(data, "m_DoubleSidedGI").boolValue, "skybox GI/instancing");
            }
            Require(Same(skybox.GetFloat("_AtmosphereThickness"), F(0x3f800000u)), "skybox atmosphere");
            Require(Same(skybox.GetFloat("_Exposure"), F(0x3fa66666u)), "skybox exposure");
            Require(Same(skybox.GetFloat("_SunDisk"), F(0x3f800000u)), "skybox sun disk");
            Require(Same(skybox.GetFloat("_SunSize"), F(0x3d23d70au)), "skybox sun size");
            // Editor builtin material omits this saved property; its shader
            // default is 5. The player serializes it explicitly (source hash match).
            Require(Same(skybox.GetFloat("_SunSizeConvergence"), F(0x40a00000u)), "skybox convergence");
            Require(Same(skybox.GetColor("_GroundColor"), new Color(F(0x3ebced91u), F(0x3eb2b021u), F(0x3eae978du), F(0x3f800000u))), "skybox ground");
            Require(Same(skybox.GetColor("_SkyTint"), new Color(F(0x3f000000u), F(0x3f000000u), F(0x3f000000u), F(0x3f800000u))), "skybox tint");
        }

        private static SerializedProperty Field(SerializedObject value, string path) =>
            value.FindProperty(path) ?? throw new InvalidOperationException("Scene environment Editor field missing: " + path);
        private static void VerifyBuiltin(UnityEngine.Object value, string guid, long localId, string name)
        {
            Require(value && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string actualGuid, out long actualId)
                && actualGuid == guid && actualId == localId, name + " builtin identity");
        }

        private static float F(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
        private static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
        private static bool Same(Color a, Color b) => Same(a.r, b.r) && Same(a.g, b.g) && Same(a.b, b.b) && Same(a.a, b.a);
        private static void RequireEditScene(Scene scene)
        {
            Require(Application.unityVersion == "2022.3.62f3", "fixed Editor version");
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "requires edit mode");
            Require(scene.IsValid() && scene.isLoaded && SceneManager.GetActiveScene() == scene, "target must be the active loaded scene");
        }
        private static void Require(bool condition, string field)
        {
            if (!condition) throw new InvalidOperationException("Scene environment: " + field);
        }
    }
}
