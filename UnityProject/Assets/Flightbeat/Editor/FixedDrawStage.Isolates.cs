using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class GpuIsolateInput
        {
            public string @case, diagnostic;
            public bool preciseExperiment;
            public int width, height, totalQuads;
            public GpuIsolatePoint[] points;
        }
        [Serializable] private sealed class GpuIsolatePoint { public int quad, x, yBottom; }
        [Serializable] private sealed class GpuCameraTarget
        {
            public bool hdr, observed, isDeclaredTarget;
            public int graphicsFormat, width, height;
            public GpuIsolateSample[] pixels;
        }
        private static GpuCameraTarget ObserveGpuCameraTarget(Camera camera)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-gpu-isolate-input") < 0) return null;
            var input = JsonUtility.FromJson<GpuIsolateInput>(File.ReadAllText(Arg("--flightbeat-gpu-isolate-input")));
            return ObserveGpuCameraTarget(camera, input.points);
        }
        private static GpuCameraTarget ObserveGpuCameraTarget(Camera camera, GpuIsolatePoint[] points)
        {
            var report = new GpuCameraTarget { hdr = camera.allowHDR, pixels = new GpuIsolateSample[points.Length] };
            // Camera.activeTexture is documented as the temporary target exposed
            // in OnPostRender. Read its actual format/data, do not infer it from
            // the declared target or add an image effect that changes the path.
            void Observe(Camera c)
            {
                if (c != camera) return;
                var rt = c.activeTexture;
                if (rt == null) throw new Exception("No active Camera target in OnPostRender");
                report.observed = true; report.graphicsFormat = (int)rt.graphicsFormat;
                report.width = rt.width; report.height = rt.height; report.isDeclaredTarget = rt == c.targetTexture;
                var old = RenderTexture.active;
                var pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
                try
                {
                    RenderTexture.active = rt;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        pixel.ReadPixels(new Rect(p.x, p.yBottom, 1, 1), 0, 0, false); pixel.Apply(false, false);
                        byte[] bytes = pixel.GetRawTextureData();
                        var values = new double[4]; var bits = new string[4];
                        for (int n = 0; n < 4; n++) { values[n] = BitConverter.ToSingle(bytes, n * 4); bits[n] = BitConverter.ToUInt32(bytes, n * 4).ToString("x8"); }
                        report.pixels[i] = new GpuIsolateSample { quad = p.quad, x = p.x, yBottom = p.yBottom,
                            format = "Camera.activeTexture read as RGBA32F", graphicsFormat = (int)rt.graphicsFormat, values = values, bits = bits };
                    }
                }
                finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(pixel); }
            }
            Camera.onPostRender += Observe;
            try { camera.Render(); }
            finally { Camera.onPostRender -= Observe; }
            if (!report.observed) throw new Exception("Camera OnPostRender was not observed");
            foreach (var pixel in report.pixels) if (pixel == null) throw new Exception("Incomplete Camera target diagnostic read");
            return report;
        }
        [Serializable] private sealed class GpuIsolateSample
        {
            public int quad, x, yBottom, graphicsFormat;
            public string format;
            public double[] values;
            public string[] bits;
        }
        [Serializable] private sealed class GpuIsolateReport
        {
            public string scope = "Isolated original-material Quads; target-precision diagnostics, not full-frame acceptance or unblended fragment output.";
            public bool fullEndingExact;
            public List<GpuIsolateSample> samples = new List<GpuIsolateSample>();
        }
        private static GpuIsolateReport DiagnoseGpuIsolates(string name, Camera camera, RenderTexture target,
            Texture2D readback, CanvasRenderer[] renderers, Mesh[] originals)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-gpu-isolate-input") < 0) return null;
            var input = JsonUtility.FromJson<GpuIsolateInput>(File.ReadAllText(Arg("--flightbeat-gpu-isolate-input")));
            if (input.@case != name) return null;
            if (input.width != 540 || input.height != 960 || input.diagnostic != "isolated-quads" || renderers.Length != originals.Length)
                throw new Exception("Unsupported GPU isolate contract");
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)) throw new Exception("ARGBFloat unsupported");
            byte[] full = (byte[])readback.GetRawTextureData().Clone();
            var report = new GpuIsolateReport();
            var copies = new Mesh[originals.Length];
            var indices = new int[originals.Length][];
            int total = 0;
            for (int i = 0; i < originals.Length; i++)
            {
                indices[i] = originals[i].triangles;
                if (indices[i].Length % 6 != 0) throw new Exception("Non-Quad diagnostic mesh");
                total += indices[i].Length / 6;
            }
            if (total != input.totalQuads) throw new Exception("Candidate isolate Quad count differs");
            var floatTarget = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            var floatReadback = new Texture2D(540, 960, TextureFormat.RGBAFloat, false, false);
            try
            {
                for (int i = 0; i < copies.Length; i++) copies[i] = UnityEngine.Object.Instantiate(originals[i]);
                foreach (bool floats in new[] { false, true })
                {
                    var selectedTarget = floats ? floatTarget : target;
                    var selectedReadback = floats ? floatReadback : readback;
                    camera.targetTexture = selectedTarget;
                    foreach (var p in input.points)
                    {
                        if (p.quad < 0 || p.quad >= total || p.x < 0 || p.x >= 540 || p.yBottom < 0 || p.yBottom >= 960)
                            throw new Exception("Invalid isolated Quad/pixel");
                        int first = 0;
                        for (int i = 0; i < copies.Length; i++)
                        {
                            int count = indices[i].Length / 6;
                            var selected = p.quad >= first && p.quad < first + count ? new int[6] : Array.Empty<int>();
                            if (selected.Length != 0) Array.Copy(indices[i], (p.quad - first) * 6, selected, 0, 6);
                            copies[i].triangles = selected; renderers[i].SetMesh(copies[i]); first += count;
                        }
                        camera.Render(); RenderTexture.active = selectedTarget;
                        selectedReadback.ReadPixels(new Rect(0, 0, 540, 960), 0, 0, false); selectedReadback.Apply(false, false);
                        if (floats && selectedTarget.graphicsFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat)
                            throw new Exception("Unexpected actual float attachment");
                        byte[] bytes = selectedReadback.GetRawTextureData();
                        int at = (p.yBottom * 540 + p.x) * (floats ? 16 : 4);
                        var values = new double[4]; var bits = floats ? new string[4] : Array.Empty<string>();
                        for (int c = 0; c < 4; c++)
                        {
                            values[c] = floats ? BitConverter.ToSingle(bytes, at + c * 4) : bytes[at + c];
                            if (floats) bits[c] = BitConverter.ToUInt32(bytes, at + c * 4).ToString("x8");
                        }
                        report.samples.Add(new GpuIsolateSample { quad = p.quad, x = p.x, yBottom = p.yBottom,
                            format = floats ? "RGBA32F" : "RGBA8", graphicsFormat = (int)selectedTarget.graphicsFormat, values = values, bits = bits });
                    }
                }
            }
            finally
            {
                camera.targetTexture = target; RenderTexture.active = target;
                for (int i = 0; i < renderers.Length; i++) renderers[i].SetMesh(originals[i]);
                foreach (var copy in copies) if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
                UnityEngine.Object.DestroyImmediate(floatReadback);
                floatTarget.Release(); UnityEngine.Object.DestroyImmediate(floatTarget);
            }
            camera.Render(); RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 540, 960), 0, 0, false); readback.Apply(false, false);
            byte[] ending = readback.GetRawTextureData();
            if (ending.Length != full.Length) throw new Exception("GPU isolate control length");
            for (int i = 0; i < full.Length; i++) if (ending[i] != full[i]) throw new Exception("GPU isolate changed full render at byte " + i);
            report.fullEndingExact = true;
            return report;
        }
    }
}
