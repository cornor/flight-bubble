using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class GpuPrefixInput
        {
            public string @case;
            public int width, height, totalQuads;
            public bool intermediate;
            public int[] prefixes;
            public GpuPrefixPoint[] points;
        }
        [Serializable] private sealed class GpuPrefixPoint { public int x, yBottom; }
        [Serializable] private sealed class GpuPrefixPixel { public int x, yBottom; public int[] rgba; }
        [Serializable] private sealed class GpuPrefixSample { public int prefix; public GpuPrefixPixel[] pixels; public GpuCameraTarget intermediate; }
        [Serializable] private sealed class GpuPrefixReport
        {
            public string scope = "Diagnostic partial draw prefixes only; never rendering acceptance or a permitted mask.";
            public int totalQuads;
            public bool fullEndingExact;
            public List<GpuPrefixSample> samples = new List<GpuPrefixSample>();
        }
        private static GpuPrefixReport DiagnoseGpuPrefixes(string name, Camera camera, RenderTexture target,
            Texture2D readback, CanvasRenderer[] renderers, Mesh[] originals)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-gpu-prefix-input") < 0) return null;
            var input = JsonUtility.FromJson<GpuPrefixInput>(File.ReadAllText(Arg("--flightbeat-gpu-prefix-input")));
            if (input.@case != name) return null;
            if (input.width != 540 || input.height != 960 || renderers.Length != originals.Length)
                throw new Exception("Unsupported GPU prefix contract");
            byte[] full = (byte[])readback.GetRawTextureData().Clone();
            var report = new GpuPrefixReport();
            var copies = new Mesh[originals.Length];
            var indices = new int[originals.Length][];
            for (int i = 0; i < originals.Length; i++)
            {
                indices[i] = originals[i].triangles;
                if (indices[i].Length % 6 != 0) throw new Exception("Non-Quad diagnostic mesh");
                report.totalQuads += indices[i].Length / 6;
            }
            if (report.totalQuads != input.totalQuads) throw new Exception("Candidate diagnostic Quad count differs");
            GpuCameraTarget intermediate = null;
            var points = Array.ConvertAll(input.points, p => new GpuIsolatePoint { x = p.x, yBottom = p.yBottom });
            byte[] Read()
            {
                if (input.intermediate)
                {
                    intermediate = ObserveGpuCameraTarget(camera, points);
                    if (intermediate.graphicsFormat != (int)UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat)
                        throw new Exception("Unexpected prefix intermediate target");
                }
                else camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 540, 960), 0, 0, false); readback.Apply(false, false);
                return readback.GetRawTextureData();
            }
            void Same(byte[] actual)
            {
                if (actual.Length != full.Length) throw new Exception("GPU prefix control length");
                for (int i = 0; i < full.Length; i++) if (actual[i] != full[i])
                    throw new Exception("GPU prefix changed full render at byte " + i);
            }
            try
            {
                for (int i = 0; i < copies.Length; i++) copies[i] = UnityEngine.Object.Instantiate(originals[i]);
                foreach (int prefix in input.prefixes)
                {
                    if (prefix < 0 || prefix > report.totalQuads) throw new Exception("Invalid GPU prefix");
                    int remaining = prefix;
                    for (int i = 0; i < copies.Length; i++)
                    {
                        int keep = Math.Min(remaining, indices[i].Length / 6); remaining -= keep;
                        var selected = new int[keep * 6]; Array.Copy(indices[i], selected, selected.Length);
                        copies[i].triangles = selected; renderers[i].SetMesh(copies[i]);
                    }
                    byte[] pixels = Read();
                    if (prefix == report.totalQuads) Same(pixels);
                    var sample = new GpuPrefixSample { prefix = prefix, pixels = new GpuPrefixPixel[input.points.Length], intermediate = intermediate };
                    for (int i = 0; i < input.points.Length; i++)
                    {
                        var p = input.points[i];
                        if (p.x < 0 || p.x >= 540 || p.yBottom < 0 || p.yBottom >= 960) throw new Exception("Invalid diagnostic pixel");
                        int at = (p.yBottom * 540 + p.x) * 4;
                        sample.pixels[i] = new GpuPrefixPixel { x = p.x, yBottom = p.yBottom,
                            rgba = new[] { (int)pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3] } };
                    }
                    report.samples.Add(sample);
                }
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].SetMesh(originals[i]);
                foreach (var copy in copies) if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }
            Same(Read()); report.fullEndingExact = true;
            return report;
        }
    }
}
