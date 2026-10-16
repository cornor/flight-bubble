using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Flightbeat.Editor
{
    public static partial class ModelCampaignStage
    {
        // Editor evidence transport only. The same JSON lines and model checks
        // pass through either codec; no codec code enters the native game.
        private static string zstdExecutable;
        private static void ConfigureCompression()
        {
            var args = Environment.GetCommandLineArgs();
            zstdExecutable = Array.IndexOf(args, "--flightbeat-campaign-zstd") < 0 ? null : Arg("--flightbeat-campaign-zstd");
            if (zstdExecutable != null && !File.Exists(zstdExecutable)) throw new FileNotFoundException("Declared zstd executable", zstdExecutable);
        }
        private static TextReader OpenSteps(string path)
        {
            if (path.EndsWith(".gz", StringComparison.Ordinal)) return new StreamReader(new GZipStream(File.OpenRead(path), CompressionMode.Decompress));
            if (path.EndsWith(".zst", StringComparison.Ordinal)) return new CodecReader(path);
            throw new InvalidDataException("Unknown steps codec: " + path);
        }
        private static TextWriter OpenArrays(string folder)
        {
            if (zstdExecutable == null) return new StreamWriter(new GZipStream(File.Create(Path.Combine(folder, "arrays.jsonl.gz")), System.IO.Compression.CompressionLevel.Fastest));
            return new CodecWriter(Path.Combine(folder, "arrays.jsonl.zst"));
        }
        private sealed class CodecPipe : IDisposable
        {
            internal readonly Process process;
            private readonly Task transfer;
            private readonly Task<string> error;
            private bool finished;
            internal CodecPipe(string path, bool encode)
            {
                if (zstdExecutable == null) throw new ArgumentException("Zstd input requires --flightbeat-campaign-zstd with an explicit executable path");
                var info = new ProcessStartInfo {
                    FileName = zstdExecutable, Arguments = encode ? "-3 -c --no-progress" : "-dc",
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                process = Process.Start(info) ?? throw new IOException("Could not start zstd");
                error = process.StandardError.ReadToEndAsync();
                // Paths never enter a command line. Feeding stdin also allows
                // immutable CAS symlinks without zstd's force/pass-through mode.
                transfer = Task.Run(async () => {
                    if (encode)
                    {
                        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                        await process.StandardOutput.BaseStream.CopyToAsync(output);
                    }
                    else
                    {
                        using var input = File.OpenRead(path);
                        await input.CopyToAsync(process.StandardInput.BaseStream);
                        process.StandardInput.Close();
                    }
                });
            }
            internal void Finish()
            {
                transfer.GetAwaiter().GetResult();
                if (!process.WaitForExit(10000)) throw new IOException("Zstd did not finish after stream EOF");
                string details = error.GetAwaiter().GetResult();
                if (process.ExitCode != 0) throw new IOException("Zstd exit " + process.ExitCode + ": " + details);
                finished = true;
            }
            public void Dispose()
            {
                if (!finished)
                {
                    // Abort only our own codec child after a reader/writer/check
                    // failure. Keep the original exception and partial evidence.
                    try { if (!process.HasExited) process.Kill(); process.WaitForExit(10000); } catch { }
                    try { transfer.GetAwaiter().GetResult(); } catch { }
                    try { error.GetAwaiter().GetResult(); } catch { }
                }
                process.Dispose();
            }
        }
        private sealed class CodecReader : TextReader
        {
            private readonly CodecPipe pipe;
            private readonly StreamReader reader;
            internal CodecReader(string path) { pipe = new CodecPipe(path, false); reader = new StreamReader(pipe.process.StandardOutput.BaseStream, Encoding.UTF8); }
            public override string ReadLine()
            {
                string line = reader.ReadLine();
                if (line == null) pipe.Finish();
                return line;
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) { reader.Dispose(); pipe.Dispose(); }
                base.Dispose(disposing);
            }
        }
        private sealed class CodecWriter : TextWriter
        {
            private readonly CodecPipe pipe;
            private readonly StreamWriter writer;
            public override Encoding Encoding => Encoding.UTF8;
            internal CodecWriter(string path) { pipe = new CodecPipe(path, true); writer = new StreamWriter(pipe.process.StandardInput.BaseStream, new UTF8Encoding(false), 65536); }
            public override void WriteLine(string value) => writer.WriteLine(value);
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try { writer.Dispose(); pipe.Finish(); }
                    finally { pipe.Dispose(); }
                }
                base.Dispose(disposing);
            }
        }
    }
}
