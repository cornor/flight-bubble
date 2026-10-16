using System;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static partial class FrameFlowStage
    {
        // Real native Camera/Canvas and RectTransformUtility projection. The geometry
        // is f54773's setup; offscreen 540x960 only fixes the declared viewport.
        private sealed class PointerContext : IDisposable
        {
            private readonly GameObject root;
            private readonly RenderTexture target;
            private readonly Camera camera;
            private readonly PixelSurface[] surfaces = new PixelSurface[2];
            private readonly VertexHelper[] helpers = new VertexHelper[2];
            public RectTransform CanvasRect { get; private set; }
            public PointerContext(PixelWingGame game)
            {
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new Exception("Pointer projection stage requires graphics-enabled Unity CLI");
                root = new GameObject("Frame pointer native geometry");
                var cameraObject = new GameObject("Declared source camera", typeof(Camera)); cameraObject.transform.SetParent(root.transform, false);
                camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
                target = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                camera.targetTexture = target; camera.rect = new Rect(0, 0, 1, 1); camera.orthographic = true; camera.orthographicSize = 480;
                camera.transform.position = new Vector3(270, 480, -20); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0, 0, 0, 1);
                Set(game, "gameCamera", camera);
                for (int i = 0; i < 2; i++)
                {
                    bool hud = i == 1;
                    var canvasObject = new GameObject(hud ? "Foreground HUD" : "Pixel Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvasObject.transform.SetParent(root.transform, false);
                    var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = hud ? 16 : 20; canvas.pixelPerfect = true; if (hud) canvas.sortingOrder = 20;
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                    var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1; scaler.referenceResolution = new Vector2(540, 960); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    var display = new GameObject(hud ? "Protected counters and controls" : "Arcade Display", typeof(RectTransform), typeof(CanvasRenderer));
                    display.transform.SetParent(canvasObject.transform, false);
                    var rect = display.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = Vector2.zero; rect.sizeDelta = new Vector2(540, 960); rect.anchoredPosition = new Vector2(-270, -480);
                    var surface = display.AddComponent<PixelSurface>(); surfaces[i] = surface; helpers[i] = new VertexHelper();
                    Set(game, hud ? "hud" : "surface", surface); Set(game, hud ? "hudRect" : "canvasRect", rect); Set(game, hud ? "hudScale" : "mainScale", scaler);
                    if (!hud) CanvasRect = rect;
                }
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < 2; i++)
                {
                    var method = typeof(PixelWingGame).GetMethod(i == 0 ? "Draw" : "DrawHUD", Flags);
                    surfaces[i].Paint = (Action<PixelSurface>)Delegate.CreateDelegate(typeof(Action<PixelSurface>), game, method);
                }
            }
            public void Paint()
            {
                for (int i = 0; i < surfaces.Length; i++)
                    typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null).Invoke(surfaces[i], new object[] { helpers[i] });
            }
            public void Dispose()
            {
                foreach (var helper in helpers) helper?.Dispose();
                // These materials belong only to the Editor fixture. Runtime OnDestroy
                // uses delayed destruction; release them immediately before teardown.
                foreach (var surface in surfaces)
                {
                    if (surface == null) continue;
                    var material = (Material)Field(typeof(PixelSurface), "ownedMaterial").GetValue(surface);
                    Set(surface, "ownedMaterial", null);
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                }
                if (camera != null) camera.targetTexture = null;
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
