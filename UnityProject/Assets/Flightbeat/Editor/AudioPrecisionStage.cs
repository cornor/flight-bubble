using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Run the existing source oracle inside the actual Unity Mono process.
    // Dependencies stay outside Assets; none enter the game build.
    public static class AudioPrecisionStage
    {
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            var args = Environment.GetCommandLineArgs();
            string Arg(string flag)
            {
                int at = Array.IndexOf(args, flag);
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException(flag);
                return Path.GetFullPath(args[at + 1]);
            }
            string input = Arg("--flightbeat-audio-input"), output = Arg("--flightbeat-audio-report");
            if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Assembly Resolve(object sender, ResolveEventArgs e)
            {
                string name = new AssemblyName(e.Name).Name;
                if (!name.StartsWith("System.") && name != "Microsoft.Bcl.AsyncInterfaces") return null;
                string path = Path.Combine(input, name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            }
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            try
            {
                var oracle = Assembly.LoadFrom(Path.Combine(input, "AudioPlaybackOracle.dll"));
                var method = oracle.GetType("FlightbeatAudioPlaybackOracle", true).GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
                string characters = Path.Combine(input, "characters.json");
                var oracleArgs = File.Exists(characters) ? new[] { input, output, characters } : new[] { input, output };
                int code = (int)method.Invoke(null, new object[] { oracleArgs });
                if (code != 0) throw new Exception("Audio precision mismatch; see " + output);
            }
            finally { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
        }
    }
}
