using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class GpuVaryingSample
        {
            public string mode;
            public int quad, x, yBottom;
            public double[] values;
            public string[] bits;
        }
        [Serializable] private sealed class GpuVaryingReport
        {
            public string scope = "Diagnostic shader copies returning interpolants or unblended fragment output; changes can affect compiler optimization, not full-game acceptance.";
            public bool fullEndingExact;
            public List<GpuVaryingSample> samples = new List<GpuVaryingSample>();
        }
        private static GpuVaryingReport DiagnoseGpuVaryings(string directory, string name, Camera camera, RenderTexture target,
            Texture2D readback, CanvasRenderer[] renderers, Mesh[] originals)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-gpu-varying-input") < 0) return null;
            var input = JsonUtility.FromJson<GpuIsolateInput>(File.ReadAllText(Arg("--flightbeat-gpu-varying-input")));
            if (input.@case != name) return null;
            if (input.width != 540 || input.height != 960 || input.diagnostic != "varyings" || originals.Length != renderers.Length)
                throw new Exception("Unsupported varying diagnostic contract");
            string output = Path.Combine(directory, name + "-varyings");
            if (Directory.Exists(output)) throw new IOException("Fresh varying shader directory required");
            Directory.CreateDirectory(output);
            byte[] full = (byte[])readback.GetRawTextureData().Clone();
            var copies = new Mesh[originals.Length]; var indices = new int[originals.Length][];
            var originalMaterials = new Material[originals.Length];
            int total = 0;
            for (int i = 0; i < originals.Length; i++)
            {
                indices[i] = originals[i].triangles;
                if (indices[i].Length % 6 != 0) throw new Exception("Non-Quad varying mesh");
                total += indices[i].Length / 6;
                originalMaterials[i] = renderers[i].GetMaterial();
                if (originalMaterials[i].shaderKeywords.Length != 0 || originalMaterials[i].shader.name != "PixelWing/Lit Pixel Surface")
                    throw new Exception("Varying diagnostic requires the declared default source shader variant");
            }
            if (total != input.totalQuads) throw new Exception("Varying Quad count differs");
            string shaderSource = File.ReadAllText(Path.Combine(Application.dataPath, "Flightbeat/Resources/pixellitsurface.shader"));
            string fragmentSource = File.ReadAllText(Path.Combine(Application.dataPath, "Flightbeat/Rendering/Shaders/Shader22Program2.hlsl"));
            int first = shaderSource.IndexOf("HLSLPROGRAM", StringComparison.Ordinal), last = shaderSource.IndexOf("ENDHLSL", StringComparison.Ordinal);
            if (first < 0 || last <= first || shaderSource.Split(new[] { "Blend SrcAlpha OneMinusSrcAlpha" }, StringSplitOptions.None).Length != 2)
                throw new Exception("Source shader template changed");
            var report = new GpuVaryingReport();
            var floatTarget = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                camera.targetTexture = floatTarget;
                for (int i = 0; i < copies.Length; i++) copies[i] = UnityEngine.Object.Instantiate(originals[i]);
                var modes = new List<string> { "color", "uv", "fragment" };
                if (input.preciseExperiment) modes.Add("precise-fragment");
                foreach (string mode in modes)
                {
                    string expression = mode == "color" ? "input.vs_COLOR0" : mode == "uv" ? "float4(input.vs_TEXCOORD0.xy,input.vs_TEXCOORD1.xy)" : "FbFrag(input)";
                    string body = fragmentSource;
                    if (mode == "precise-fragment")
                    {
                        // A single explicit counterfactual: retain written
                        // fragment operation order instead of fusing it. This
                        // tests the BE compiler-output observation; never edits
                        // the shipping shader or changes its constants.
                        if (body.Split(new[] { "float4 SV_Target0;" }, StringSplitOptions.None).Length != 2)
                            throw new Exception("Fragment result declaration changed");
                        body = body.Replace("float4 SV_Target0;", "precise float4 SV_Target0;");
                    }
                    string program = "HLSLPROGRAM\n#pragma target 3.0\n#pragma vertex FbVert\n#pragma fragment FbDiagnostic\n#include \"HLSLSupport.cginc\"\n#include \"UnityShaderVariables.cginc\"\n" + body +
                        "\nfloat4 FbDiagnostic(FbVaryings input) : SV_Target { return " + expression + "; }\n";
                    string source = (shaderSource.Substring(0, first) + program + shaderSource.Substring(last))
                        .Replace("Shader \"PixelWing/Lit Pixel Surface\"", "Shader \"Hidden/Flightbeat/Varying-" + mode + "\"")
                        .Replace("Blend SrcAlpha OneMinusSrcAlpha", "Blend Off");
                    File.WriteAllText(Path.Combine(output, mode + ".shader"), source);
                    Shader shader = null; var materials = new Material[renderers.Length];
                    try
                    {
                        shader = ShaderUtil.CreateShaderAsset(source, true); shader.hideFlags = HideFlags.HideAndDontSave;
                        if (ShaderUtil.ShaderHasError(shader)) throw new Exception("Varying diagnostic shader failed: " + mode);
                        for (int i = 0; i < materials.Length; i++)
                        {
                            materials[i] = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                            materials[i].CopyPropertiesFromMaterial(originalMaterials[i]); renderers[i].SetMaterial(materials[i], 0);
                        }
                        foreach (var p in input.points)
                        {
                            if (p.quad < 0 || p.quad >= total || p.x < 0 || p.x >= 540 || p.yBottom < 0 || p.yBottom >= 960)
                                throw new Exception("Invalid diagnostic Quad/pixel");
                            int start = 0;
                            for (int i = 0; i < copies.Length; i++)
                            {
                                int count = indices[i].Length / 6;
                                var selected = p.quad >= start && p.quad < start + count ? new int[6] : Array.Empty<int>();
                                if (selected.Length != 0) Array.Copy(indices[i], (p.quad - start) * 6, selected, 0, 6);
                                copies[i].triangles = selected; renderers[i].SetMesh(copies[i]); start += count;
                            }
                            camera.Render(); RenderTexture.active = floatTarget;
                            if (floatTarget.graphicsFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat)
                                throw new Exception("Unexpected varying target precision");
                            pixel.ReadPixels(new Rect(p.x, p.yBottom, 1, 1), 0, 0, false); pixel.Apply(false, false);
                            byte[] bytes = pixel.GetRawTextureData(); var values = new double[4]; var bits = new string[4];
                            for (int i = 0; i < 4; i++) { values[i] = BitConverter.ToSingle(bytes, i * 4); bits[i] = BitConverter.ToUInt32(bytes, i * 4).ToString("x8"); }
                            report.samples.Add(new GpuVaryingSample { mode = mode, quad = p.quad, x = p.x, yBottom = p.yBottom, values = values, bits = bits });
                        }
                        foreach (var stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                        {
                            var compiled = ShaderUtil.GetShaderData(shader).GetSubshader(0).GetPass(0).CompileVariant(stage, Array.Empty<string>(), ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
                            if (!compiled.Success) throw new Exception("Varying Metal export failed");
                            File.WriteAllBytes(Path.Combine(output, mode + "-" + stage + ".bin"), compiled.ShaderData);
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < renderers.Length; i++) renderers[i].SetMaterial(originalMaterials[i], 0);
                        foreach (var material in materials) if (material != null) UnityEngine.Object.DestroyImmediate(material);
                        if (shader != null) UnityEngine.Object.DestroyImmediate(shader);
                    }
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previousAsync;
                camera.targetTexture = target; RenderTexture.active = target;
                for (int i = 0; i < renderers.Length; i++) renderers[i].SetMesh(originals[i]);
                foreach (var copy in copies) if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
                UnityEngine.Object.DestroyImmediate(pixel); floatTarget.Release(); UnityEngine.Object.DestroyImmediate(floatTarget);
            }
            camera.Render(); RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 540, 960), 0, 0, false); readback.Apply(false, false);
            byte[] ending = readback.GetRawTextureData();
            if (ending.Length != full.Length) throw new Exception("Varying control length");
            for (int i = 0; i < full.Length; i++) if (ending[i] != full[i]) throw new Exception("Varying diagnostic changed full render at byte " + i);
            report.fullEndingExact = true; return report;
        }
    }
}
