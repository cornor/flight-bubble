using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Independent GPU arithmetic experiment. Never attached to a game scene,
    // never used as a replacement shader or as whole-game acceptance evidence.
    public static class BlendBackendDiagnostic
    {
        [Serializable] private sealed class Input { public string scope; public int width, height; public Case[] cases; }
        [Serializable] private sealed class Case { public string name; public float[] source, background; }
        [Serializable] private sealed class Sample
        {
            public string name, format;
            public bool blend;
            public double[] values;
            public string[] bits;
        }
        [Serializable] private sealed class Report
        {
            public string scope, unityVersion, renderer, graphicsVersion;
            public List<Sample> results = new List<Sample>();
        }
        private const string Source = @"Shader ""Hidden/Flightbeat/DiagnosticUniformBlend"" {
SubShader {
HLSLINCLUDE
#pragma target 3.0
float4 _Source;
float4 Vert(float4 p : POSITION) : SV_POSITION { return p; }
float4 Frag() : SV_Target { return _Source; }
ENDHLSL
Pass {
Blend SrcAlpha OneMinusSrcAlpha
BlendOp Add
ZWrite Off ZTest Always Cull Off
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
ENDHLSL
}
Pass {
Blend Off
ZWrite Off ZTest Always Cull Off
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
ENDHLSL
}
} Fallback Off }";
        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 >= args.Length) throw new ArgumentException("Missing " + name);
            return Path.GetFullPath(args[i + 1]);
        }
        private static Vector4 Vector(float[] a)
        {
            if (a == null || a.Length != 4) throw new Exception("RGBA stimulus required");
            return new Vector4(a[0], a[1], a[2], a[3]);
        }
        public static void Run()
        {
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-blend-input")));
            if (input.width != 4 || input.height != 4) throw new Exception("Unsupported diagnostic extent");
            string output = Arg("--flightbeat-blend-output");
            if (Directory.Exists(output)) throw new IOException("Fresh blend diagnostic output required");
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "diagnostic.shader"), Source);
            var report = new Report { scope = input.scope, unityVersion = Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceName, graphicsVersion = SystemInfo.graphicsDeviceVersion };
            var previous = RenderTexture.active; bool previousSrgb = GL.sRGBWrite, previousAsync = ShaderUtil.allowAsyncCompilation;
            Shader shader = null; Material material = null; Mesh mesh = null; Texture2D readback = null;
            try
            {
                ShaderUtil.allowAsyncCompilation = false; GL.sRGBWrite = false;
                shader = ShaderUtil.CreateShaderAsset(Source, true); shader.hideFlags = HideFlags.HideAndDontSave;
                if (ShaderUtil.ShaderHasError(shader)) throw new Exception("Diagnostic shader compilation failed");
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                mesh = new Mesh { vertices = new[] { new Vector3(-1, -1, 0), new Vector3(3, -1, 0), new Vector3(-1, 3, 0) }, triangles = new[] { 0, 1, 2 } };
                readback = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, false);
                foreach (var format in new[] { RenderTextureFormat.ARGBHalf, RenderTextureFormat.ARGBFloat })
                {
                    var target = new RenderTexture(4, 4, 0, format, RenderTextureReadWrite.Linear);
                    try
                    {
                        if (!target.Create()) throw new Exception("Diagnostic target creation failed");
                        var expected = format == RenderTextureFormat.ARGBHalf ? UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat : UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
                        if (target.graphicsFormat != expected) throw new Exception("Unexpected actual diagnostic target format");
                        RenderTexture.active = target; GL.Viewport(new Rect(0, 0, 4, 4));
                        foreach (var test in input.cases) foreach (bool blend in new[] { false, true })
                        {
                            GL.Clear(false, true, Vector(test.background));
                            material.SetVector("_Source", Vector(test.source));
                            if (!material.SetPass(blend ? 0 : 1)) throw new Exception("Diagnostic pass unavailable");
                            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                            readback.ReadPixels(new Rect(0, 0, 4, 4), 0, 0, false); readback.Apply(false, false);
                            byte[] bytes = readback.GetRawTextureData();
                            for (int i = 16; i < bytes.Length; i++) if (bytes[i] != bytes[i % 16]) throw new Exception("Uniform draw coverage differs");
                            var values = new double[4]; var bits = new string[4];
                            for (int i = 0; i < 4; i++) { values[i] = BitConverter.ToSingle(bytes, i * 4); bits[i] = BitConverter.ToUInt32(bytes, i * 4).ToString("x8"); }
                            report.results.Add(new Sample { name = test.name, format = format == RenderTextureFormat.ARGBHalf ? "RGBA16F" : "RGBA32F", blend = blend, values = values, bits = bits });
                        }
                    }
                    finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                }
                for (int pass = 0; pass < 2; pass++) foreach (var stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                {
                    var compiled = ShaderUtil.GetShaderData(shader).GetSubshader(0).GetPass(pass).CompileVariant(stage, Array.Empty<string>(), ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
                    if (!compiled.Success) throw new Exception("Diagnostic Metal export failed");
                    File.WriteAllBytes(Path.Combine(output, "pass" + pass + "-" + stage + ".bin"), compiled.ShaderData);
                }
                File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                RenderTexture.active = previous; GL.sRGBWrite = previousSrgb; ShaderUtil.allowAsyncCompilation = previousAsync;
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
                if (shader != null) UnityEngine.Object.DestroyImmediate(shader);
            }
        }
    }
}
