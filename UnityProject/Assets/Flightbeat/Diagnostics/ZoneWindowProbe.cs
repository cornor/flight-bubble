#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Short native render check using original --level/--idle-test startup.
    // The original diagnostic profile has a no-op persistence delegate.
    public sealed class ZoneWindowProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public int levelIndex, zone, frames, meshes, maximumVertices, fired, screenWidth, screenHeight;
            public float gameTime;
            public string unityVersion, graphicsDevice, state, screenshot, error;
        }
        private string output;
        private Report report;
        private float started;
        private bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-zone-report");
            if (flag < 0) return;
            if (flag + 1 >= args.Length || Array.IndexOf(args, "--idle-test") < 0 || Array.IndexOf(args, "--level") < 0)
                throw new ArgumentException("ZoneWindowProbe requires report, original --level and --idle-test");
            string path = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(path)) throw new IOException("Use a fresh report path");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var owner = new GameObject("CLI zone window check"); DontDestroyOnLoad(owner);
            var probe = owner.AddComponent<ZoneWindowProbe>(); probe.output = path; probe.started = Time.realtimeSinceStartup;
            probe.report = new Report { unityVersion = Application.unityVersion, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(), screenshot = Path.Combine(Path.GetDirectoryName(path), "zone.png") };
            Application.logMessageReceived += probe.Logged;
        }
        private void Logged(string text, string stack, LogType type)
        {
            if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish(text + "\n" + stack);
        }
        private void Update()
        {
            if (finished) return;
            try
            {
                if (Time.realtimeSinceStartup - started > 30f) throw new TimeoutException("Zone window did not complete");
                PixelWingGame game = FindObjectOfType<PixelWingGame>(); if (game == null || game.Battle == null) return;
                report.frames++;
                Canvas.ForceUpdateCanvases();
                foreach (PixelSurface surface in FindObjectsOfType<PixelSurface>())
                {
                    Mesh mesh = surface.canvasRenderer.GetMesh(); if (mesh == null || mesh.vertexCount == 0) continue;
                    report.meshes++; report.maximumVertices = Math.Max(report.maximumVertices, mesh.vertexCount);
                }
                if (Time.realtimeSinceStartup - started < 8f || game.Battle.Time < 1f) return;
                if (game.InHome || game.Battle.State != Phase.Playing || report.maximumVertices == 0) throw new Exception("Zone scene did not reach rendered live battle");
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("Native graphics required");
                report.levelIndex = (int)typeof(PixelWingGame).GetField("levelIndex", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
                report.zone = WarZones.OfLevel(report.levelIndex); report.state = game.Battle.State.ToString(); report.gameTime = game.Battle.Time;
                report.fired = game.Feedback.Fired; report.screenWidth = Screen.width; report.screenHeight = Screen.height;
                Capture(game.GameCamera); Finish(null);
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private void Capture(Camera camera)
        {
            int width = camera.pixelWidth, height = camera.pixelHeight;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(report.screenshot, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); if (pixels != null) Destroy(pixels);
            }
        }
        private void Finish(string error)
        {
            if (finished) return; finished = true; Application.logMessageReceived -= Logged;
            report.error = error; report.passed = error == null;
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
