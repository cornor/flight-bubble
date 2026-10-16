#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Diagnostics
{
    public sealed partial class StartupWindowProbe
    {
        [Serializable] private sealed class MeshChannel
        { public string name, sha256, base64; public int stride, count; }
        [Serializable] private sealed class SurfaceMesh
        { public string surface; public bool initialized; public List<MeshChannel> channels = new List<MeshChannel>(); }
        [Serializable] private sealed class DrawFrame
        {
            public string name, scope, unityVersion;
            public int frame, width, height;
            public List<SurfaceMesh> meshes = new List<SurfaceMesh>();
            public ParticleSnapshot particles;
        }
        [Serializable] private sealed class DrawCommand
        {
            public string surface;
            public int sequence, order;
            public int[] callPath, inputBits, vertexBits;
            public bool rigid, mapLayout, lighting;
        }
        [Serializable] private sealed class DrawHit { public int id; public int[] rectBits; }
        [Serializable] private sealed class CommandFrame
        {
            public string name; public int frame;
            public List<DrawCommand> commands = new List<DrawCommand>();
            public DrawObservation.Data observed;
            public List<DrawObservation.InputCall> debugInputs;
            public List<DrawObservation.MeshData> meshes;
            public List<DrawHit> hits = new List<DrawHit>();
        }
        private static bool captureCommands;
        private static DrawObservation commandObservation;
        private static CommandFrame commandFrame;
        private static Dictionary<PixelSurface, string> commandSurfaces;
        private static string drawDirectory;
        private static DrawFrame drawFrame;
        private static byte[] drawPng;
        private static bool meshOnly;
        private static void InstallContinuousDrawCapture()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-continuous-draw") < 0) return;
            string requested = Arg("--flightbeat-continuous-draw");
            if (Directory.Exists(requested) || File.Exists(requested)) throw new IOException("Fresh continuous draw directory required");
            Directory.CreateDirectory(requested);
            drawDirectory = requested;
            meshOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-continuous-mesh-only") >= 0;
            RuntimeLoopInputs.MeshOutput = ObserveContinuousMesh;
            captureCommands = Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-continuous-commands") >= 0;
            if (captureCommands) RuntimeLoopInputs.BeginMeshObservation = () => commandObservation?.Activate();
        }
        private static void BeginContinuousDrawFrame()
        {
            commandObservation?.Dispose(); commandObservation = null; commandFrame = null;
            drawFrame = null; drawPng = null;
            if (!meshOnly) ObserveContinuousParticleClock();
            if (drawDirectory == null || checkpointCursor >= data.continuous.checkpoints.Length ||
                data.continuous.checkpoints[checkpointCursor].frame != loopCursor + 1) return;
            if (captureCommands)
            {
                if (commandSurfaces == null)
                {
                    commandSurfaces = new Dictionary<PixelSurface, string> {
                        { (PixelSurface)Get(game, "surface"), "background" }, { (PixelSurface)Get(game, "hud"), "foreground" } };
                    foreach (var pair in commandSurfaces)
                    {
                        string name = pair.Value;
                        pair.Key.QuadObserver = q => {
                            if (commandFrame == null) throw new Exception("Command outside declared checkpoint");
                            var vertices = new List<int>(); foreach (UIVertex v in q.vertices) vertices.AddRange(DrawObservation.Pack(v));
                            commandFrame.commands.Add(new DrawCommand { surface = name, sequence = commandFrame.commands.Count,
                                order = q.order, callPath = q.callPath, inputBits = Array.ConvertAll(q.input, DrawObservation.Bits),
                                vertexBits = vertices.ToArray(), rigid = q.rigid, mapLayout = q.mapLayout, lighting = q.lighting });
                        };
                    }
                }
                commandObservation = new DrawObservation(commandSurfaces, false);
                commandFrame = new CommandFrame { frame = loopCursor + 1, observed = commandObservation.Result, debugInputs = commandObservation.InputCalls, meshes = commandObservation.Result.meshes };
            }
            drawFrame = new DrawFrame { frame = loopCursor + 1, width = Screen.width, height = Screen.height,
                unityVersion = Application.unityVersion,
                scope = meshOnly ? "Actual f54405 VertexHelper output during engine Canvas update; all nine channels copied. No redraw, framebuffer read or particle observation." :
                    "Actual f54405 VertexHelper output during engine Canvas update; every channel copied without invoking a redraw. Screenshot reads the completed native framebuffer. Particle input scope is recorded in particle-clocks.json and, when explicitly enabled, declared-particle-clock.json; no automatic equivalence claim." };
        }
        private static void ObserveContinuousMesh(PixelSurface surface, VertexHelper helper)
        {
            if (drawFrame == null) return;
            string name = ReferenceEquals(surface, Get(game, "surface")) ? "background" :
                ReferenceEquals(surface, Get(game, "hud")) ? "foreground" : throw new Exception("Undeclared continuous surface");
            foreach (var old in drawFrame.meshes) if (old.surface == name) throw new Exception("Extra mesh population in captured frame");
            var result = new SurfaceMesh { surface = name, initialized = (bool)MeshField(helper, "m_ListsInitalized") };
            if (!result.initialized) throw new Exception("Uninitialized source-equivalent VertexHelper");
            // Layout/list spelling from the project's com.unity.ugui source.
            // Original controller.js reads these same nine lists at f54405 exit.
            string[] fields = { "m_Positions", "m_Colors", "m_Uv0S", "m_Uv1S", "m_Uv2S", "m_Uv3S", "m_Normals", "m_Tangents", "m_Indices" };
            string[] names = { "positions", "colors", "uv0", "uv1", "uv2", "uv3", "normals", "tangents", "indices" };
            int[] strides = { 12, 4, 16, 16, 16, 16, 12, 16, 4 };
            for (int i = 0; i < fields.Length; i++)
            {
                var values = (IList)MeshField(helper, fields[i]);
                byte[] bytes;
                using (var buffer = new MemoryStream(values.Count * strides[i]))
                {
                    using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
                        foreach (object value in values)
                        {
                            if (value is Vector3 v3) { writer.Write(v3.x); writer.Write(v3.y); writer.Write(v3.z); }
                            else if (value is Vector4 v4) { writer.Write(v4.x); writer.Write(v4.y); writer.Write(v4.z); writer.Write(v4.w); }
                            else if (value is Color32 color) { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                            else if (value is int index) writer.Write(index);
                            else throw new Exception("Undeclared vertex channel type");
                        }
                    bytes = buffer.ToArray();
                }
                if (bytes.Length != values.Count * strides[i]) throw new Exception("Mesh channel byte count differs");
                using (var hash = SHA256.Create()) result.channels.Add(new MeshChannel { name = names[i], count = values.Count,
                    stride = strides[i], base64 = Convert.ToBase64String(bytes), sha256 = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() });
            }
            drawFrame.meshes.Add(result);
        }
        private static object MeshField(VertexHelper helper, string name) =>
            (typeof(VertexHelper).GetField(name, Flags) ?? throw new MissingFieldException("VertexHelper", name)).GetValue(helper);
        private static void SaveContinuousDrawFrame(Checkpoint point)
        {
            if (drawDirectory == null) return;
            if (drawFrame == null || drawFrame.frame != point.frame || drawFrame.meshes.Count != 2) throw new Exception("Continuous mesh capture is incomplete");
            if (Path.GetFileName(point.name) != point.name) throw new ArgumentException("Invalid checkpoint filename");
            drawFrame.name = point.name;
            if (captureCommands)
            {
                if (commandFrame == null || commandFrame.observed.meshes.Count != 2) throw new Exception("Incomplete drawing commands");
                commandFrame.name = point.name;
                commandFrame.hits.Clear();
                foreach (object hit in (IEnumerable)Get(game, "uiHits")) commandFrame.hits.Add(new DrawHit {
                    id = (int)Get(hit, "id"), rectBits = new[] { DrawObservation.Bits((float)Get(hit, "x")), DrawObservation.Bits((float)Get(hit, "y")),
                        DrawObservation.Bits((float)Get(hit, "w")), DrawObservation.Bits((float)Get(hit, "h")) } });
                using (var file = new FileStream(Path.Combine(drawDirectory, point.name + ".draw.json.gz"), FileMode.CreateNew))
                using (var gzip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new StreamWriter(gzip)) writer.Write(JsonUtility.ToJson(commandFrame));
            }
            // Multiple original checkpoint names may share a frame. Capture only
            // once, retain every alias, and never advance/simulate/render again.
            if (!meshOnly && drawPng == null)
            {
                drawFrame.particles = ReadContinuousParticles();
                var image = new Texture2D(drawFrame.width, drawFrame.height, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = null;
                    image.ReadPixels(new Rect(0, 0, drawFrame.width, drawFrame.height), 0, 0, false);
                    image.Apply(false, false);
                    drawPng = image.EncodeToPNG();
                }
                finally { RenderTexture.active = previous; Destroy(image); }
            }
            using (var file = new FileStream(Path.Combine(drawDirectory, point.name + ".mesh.json.gz"), FileMode.CreateNew))
            using (var gzip = new GZipStream(file, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzip)) writer.Write(JsonUtility.ToJson(drawFrame));
            if (!meshOnly)
                using (var file = new FileStream(Path.Combine(drawDirectory, point.name + ".png"), FileMode.CreateNew)) file.Write(drawPng, 0, drawPng.Length);
        }
    }
}
#endif
