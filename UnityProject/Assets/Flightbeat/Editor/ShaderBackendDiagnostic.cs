using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Read compiler output to diagnose GPU-AD-004. Does not edit a shader or run gameplay.
    public static class ShaderBackendDiagnostic
    {
        [Serializable] private sealed class Entry
        {
            public string stage, file;
            public bool success;
            public string[] messages;
            public int bytes;
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion, shader, target = "StandaloneOSX", compiler = "Metal";
            public List<Entry> entries = new List<Entry>();
        }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-shader-diagnostic");
            if (flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("Diagnostic output required");
            string output = Path.GetFullPath(args[flag + 1]);
            if (Directory.Exists(output)) throw new IOException("Fresh shader diagnostic output required");
            Directory.CreateDirectory(output);
            string asset = "Assets/Flightbeat/Resources/pixellitsurface.shader";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(asset);
            if (shader == null) throw new Exception("Missing source-derived PixelSurface shader");
            var pass = ShaderUtil.GetShaderData(shader).GetSubshader(0).GetPass(0);
            var report = new Report { unityVersion = Application.unityVersion, shader = asset };
            File.WriteAllText(Path.Combine(output, "pass-source.txt"), pass.SourceCode);
            foreach (ShaderType stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
            {
                var compiled = pass.CompileVariant(stage, Array.Empty<string>(), ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
                string file = Path.Combine(output, stage + ".bin");
                var messages = new List<string>(); foreach (var message in compiled.Messages) messages.Add(message.message);
                var entry = new Entry { stage = stage.ToString(), success = compiled.Success, file = file,
                    messages = messages.ToArray(), bytes = compiled.ShaderData?.Length ?? 0 };
                report.entries.Add(entry);
                if (compiled.ShaderData != null)
                {
                    File.WriteAllBytes(file, compiled.ShaderData);
                    File.WriteAllText(Path.Combine(output, stage + ".utf8.txt"), Encoding.UTF8.GetString(compiled.ShaderData));
                }
                File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
                if (!compiled.Success) throw new Exception("Metal diagnostic compilation failed for " + stage);
            }
        }
    }
}
