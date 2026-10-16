#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    public sealed partial class StartupWindowProbe
    {
        [Serializable] private sealed class AudioEvent
        {
            public int frame, function;
            public double time, value, nativeDspTime;
            public string cue, source, clip, assignedClip, timeBits;
            public bool clipPresent, assignedClipPresent;
            public float sourceVolume;
            public int[] callPath;
        }
        [Serializable] private sealed class AudioDspInput
        {
            public int ordinal, frame, function = 54169;
            public double result;
            public string resultBits;
        }
        [Serializable] private sealed class AudioCapture
        {
            public string scope = "Actual native AudioSource API entry events during admitted frames, with original-observed managed call ancestry. Native calls execute unchanged. Declared frame/DSP inputs come from the frozen replay; native DSP time and output rate are supplementary observations, not device waveform equivalence.";
            public int outputSampleRate, completedFrames;
            public bool callbacksRemoved;
            public List<AudioEvent> events = new List<AudioEvent>();
            public List<AudioDspInput> dspInputTape = new List<AudioDspInput>();
        }
        private static string audioDirectory;
        private static AudioCapture audioCapture;
        private static readonly List<int> audioMethods = new List<int>();
        private static readonly Dictionary<AudioSource, string> audioSourceNames = new Dictionary<AudioSource, string>();
        private static bool audioCaptureSaved;
        private static void InstallContinuousAudioCapture()
        {
            var args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "--flightbeat-continuous-audio");
            if (arg < 0) return;
            if (arg + 1 >= args.Length) throw new ArgumentException("Fresh audio observation directory required");
            foreach (var value in data.continuous.frames)
                if (String.IsNullOrEmpty(value.timeBits)) throw new ArgumentException("Audio capture requires exact declared clock bits; regenerate the input from the sealed source");
            foreach (var value in data.continuous.dsp)
                if (String.IsNullOrEmpty(value.resultBits)) throw new ArgumentException("Audio capture requires exact declared DSP input bits");
            string directory = Path.GetFullPath(args[arg + 1]);
            if (Directory.Exists(directory) || File.Exists(directory)) throw new IOException("Audio output already exists");
            Directory.CreateDirectory(directory); audioDirectory = directory;
            audioCapture = new AudioCapture { outputSampleRate = AudioSettings.outputSampleRate };
            RuntimeLoopInputs.AudioMethod = ObserveAudioMethod;
            RuntimeLoopInputs.AudioEvent = ObserveAudioEvent;
        }
        private static void ObserveAudioMethod(int function, bool entering)
        {
            if (finished || loop == null) return;
            if (entering) audioMethods.Add(function);
            else
            {
                if (audioMethods.Count == 0 || audioMethods[audioMethods.Count - 1] != function)
                    throw new Exception("Audio method observation nesting mismatch");
                audioMethods.RemoveAt(audioMethods.Count - 1);
            }
        }
        private static string AudioSourceName(AudioSource source)
        {
            if (audioSourceNames.Count == 0)
                foreach (var field in typeof(Flightbeat.Audio.ChipAudio).GetFields(Flags))
                    if (field.FieldType == typeof(AudioSource))
                    {
                        var value = (AudioSource)field.GetValue(game.Audio);
                        if (!value || audioSourceNames.ContainsKey(value)) throw new Exception("Ambiguous audio source field");
                        audioSourceNames.Add(value, field.Name);
                    }
            if (!source || !audioSourceNames.TryGetValue(source, out string name)) throw new Exception("Unregistered actual AudioSource");
            return name;
        }
        private static void ObserveAudioEvent(int function, AudioSource source, AudioClip clip, double value, string cue)
        {
            if (finished || loop == null) return;
            var row = new AudioEvent { frame = loopCursor, time = loop.time, timeBits = BitConverter.DoubleToInt64Bits(loop.time).ToString("x16"), function = function, cue = cue, value = value };
            if (function != 55175)
            {
                row.source = AudioSourceName(source);
                row.clipPresent = clip; row.clip = clip ? clip.name : null;
                var path = new List<int>(audioMethods); path.Add(function); row.callPath = path.ToArray();
                if (function == 54192 || function == 54193 || function == 54194)
                {
                    var assigned = source.clip; row.assignedClipPresent = assigned;
                    row.assignedClip = assigned ? assigned.name : null; row.sourceVolume = source.volume;
                }
                if (function == 54193) row.nativeDspTime = AudioSettings.dspTime;
            }
            audioCapture.events.Add(row);
        }
        private static void ObserveContinuousDspInput(int ordinal, double value)
        {
            if (audioCapture == null || finished || loop == null) return;
            audioCapture.dspInputTape.Add(new AudioDspInput { ordinal = ordinal, frame = loopCursor,
                result = value, resultBits = BitConverter.DoubleToInt64Bits(value).ToString("x16") });
        }
        private static void CheckContinuousAudioBoundary()
        {
            if (audioCapture == null) return;
            if (audioMethods.Count != 0) throw new Exception("Audio method observation remains open at frame end");
            audioCapture.completedFrames++;
        }
        private static void SaveContinuousAudioCapture()
        {
            RuntimeLoopInputs.AudioEvent = null; RuntimeLoopInputs.AudioMethod = null;
            if (audioCapture == null || audioCaptureSaved) return;
            audioCaptureSaved = true; audioCapture.callbacksRemoved = true;
            try
            {
                using (var file = new FileStream(Path.Combine(audioDirectory, "events.json"), FileMode.CreateNew))
                using (var writer = new StreamWriter(file)) writer.Write(JsonUtility.ToJson(audioCapture));
            }
            catch (Exception e) { report.passed = false; report.error = (report.error ?? "") + "\nAudio output failed: " + e; }
        }
    }
}
#endif
