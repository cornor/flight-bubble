using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        public static void RunGpuStageAndBuild()
        {
            OriginalMaterialImportVerification.Run();
            Run();
            NativeCliBuild.Run();
        }
        [Serializable] private sealed class GpuOutput
        {
            public string rgbaFile, pngFile, format = "RGBA8", rowOrder = "bottom-up", graphicsDevice, graphicsVersion, colorSpace;
            public int width = 540, height = 960, samples, depth, graphicsFormat;
            public List<GpuSurface> surfaces = new List<GpuSurface>();
            public float[] projection;
            public GpuPrefixReport prefixDiagnostics;
            public GpuIsolateReport isolateDiagnostics;
            public GpuCameraTarget cameraTargetDiagnostic;
            public GpuVaryingReport varyingDiagnostics;
        }
        [Serializable] private sealed class GpuSurface
        {
            public string name, shader; public int vertices, indices; public float[] localToWorld;
        }
        private static float[] Matrix(Matrix4x4 m)
        {
            var values = new float[16]; for (int i = 0; i < 16; i++) values[i] = m[i]; return values;
        }
        private static GpuOutput RenderGpu(string directory, string name, Dictionary<PixelSurface, string> surfaces, Dictionary<PixelSurface, Mesh> meshes)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("GPU capture requires graphics-enabled Unity CLI");
            Directory.CreateDirectory(directory);
            var result = new GpuOutput { rgbaFile = Path.Combine(directory, name + ".rgba"), pngFile = Path.Combine(directory, name + ".png"),
                graphicsDevice = SystemInfo.graphicsDeviceName, graphicsVersion = SystemInfo.graphicsDeviceVersion, colorSpace = QualitySettings.activeColorSpace.ToString() };
            if (File.Exists(result.rgbaFile) || File.Exists(result.pngFile)) throw new IOException("Fresh GPU output required");
            var root = new GameObject("Declared GPU Canvas submission");
            var cameraObject = new GameObject("Declared GPU Camera", typeof(Camera)); cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            var target = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Texture2D readback = null; RenderTexture old = RenderTexture.active;
            var submittedMeshes = new List<Mesh>();
            try
            {
                // f54773 source camera and canvas geometry; isolated layer only prevents
                // unrelated Editor scene objects from entering this diagnostic render.
                camera.targetTexture = target; camera.cullingMask = 1 << 31;
                camera.rect = new Rect(0, 0, 1, 1); camera.orthographic = true; camera.orthographicSize = 480;
                camera.transform.position = new Vector3(270, 480, -20);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 1);
                foreach (var pair in surfaces)
                {
                    bool hud = pair.Value == "hud";
                    var canvasObject = new GameObject(pair.Value + " Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvasObject.layer = 31; canvasObject.transform.SetParent(root.transform, false);
                    var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = hud ? 16 : 20; canvas.pixelPerfect = true; if (hud) canvas.sortingOrder = 20;
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                    var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1; scaler.referenceResolution = new Vector2(540, 960); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    var display = new GameObject(pair.Value + " submitted mesh", typeof(RectTransform), typeof(CanvasRenderer));
                    display.layer = 31; display.transform.SetParent(canvasObject.transform, false);
                    var rect = display.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = Vector2.zero; rect.sizeDelta = new Vector2(540, 960); rect.anchoredPosition = new Vector2(-270, -480);
                    var renderer = display.GetComponent<CanvasRenderer>();
                    renderer.materialCount = 1; renderer.SetMaterial(pair.Key.materialForRendering, 0); renderer.SetTexture(pair.Key.mainTexture);
                    renderer.SetMesh(meshes[pair.Key]);
                    submittedMeshes.Add(meshes[pair.Key]);
                    result.surfaces.Add(new GpuSurface { name = pair.Value, shader = pair.Key.materialForRendering.shader.name,
                        vertices = meshes[pair.Key].vertexCount, indices = meshes[pair.Key].triangles.Length });
                }
                Canvas.ForceUpdateCanvases();
                result.cameraTargetDiagnostic = ObserveGpuCameraTarget(camera);
                if (result.cameraTargetDiagnostic == null) camera.Render();
                result.projection = Matrix(camera.projectionMatrix);
                var renderers = root.GetComponentsInChildren<CanvasRenderer>();
                for (int i = 0; i < renderers.Length; i++) result.surfaces[i].localToWorld = Matrix(renderers[i].transform.localToWorldMatrix);
                result.samples = target.antiAliasing; result.depth = target.depth; result.graphicsFormat = (int)target.graphicsFormat;
                RenderTexture.active = target;
                readback = new Texture2D(540, 960, TextureFormat.RGBA32, false, false);
                readback.ReadPixels(new Rect(0, 0, 540, 960), 0, 0, false); readback.Apply(false, false);
                File.WriteAllBytes(result.rgbaFile, readback.GetRawTextureData()); File.WriteAllBytes(result.pngFile, readback.EncodeToPNG());
                result.prefixDiagnostics = DiagnoseGpuPrefixes(name, camera, target, readback, renderers, submittedMeshes.ToArray());
                result.isolateDiagnostics = DiagnoseGpuIsolates(name, camera, target, readback, renderers, submittedMeshes.ToArray());
                result.varyingDiagnostics = DiagnoseGpuVaryings(directory, name, camera, target, readback, renderers, submittedMeshes.ToArray());
                return result;
            }
            finally
            {
                RenderTexture.active = old; camera.targetTexture = null;
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
