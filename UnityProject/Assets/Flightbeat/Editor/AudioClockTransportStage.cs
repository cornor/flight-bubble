#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Diagnose JSON number transport without executing a game frame.
    public static class AudioClockTransportStage
    {
        [Serializable] private sealed class Frame { public int index; public double time; }
        [Serializable] private sealed class Continuous { public Frame[] frames; }
        [Serializable] private sealed class Input { public Continuous continuous; }
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string input = args[Array.IndexOf(args, "--flightbeat-clock-input") + 1];
            string output = args[Array.IndexOf(args, "--flightbeat-clock-output") + 1];
            var data = JsonUtility.FromJson<Input>(File.ReadAllText(input));
            using (var file = new FileStream(output, FileMode.CreateNew))
            using (var writer = new StreamWriter(file))
                foreach (var frame in data.continuous.frames)
                    writer.WriteLine(frame.index + "\t" + BitConverter.DoubleToInt64Bits(frame.time).ToString("x16") + "\t" + JsonUtility.ToJson(frame));
        }
    }
}
#endif
