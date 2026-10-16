using System.Collections.Generic;
using System.IO;
using System.Text;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Audio
{
    // Original diagnostic component; never attached by normal gameplay.
    public sealed class NativeAudioEvidence : MonoBehaviour
    {
        // f54335: allocation order and literal initial capacity/default channel count.
        private readonly object gate = new object();
        private readonly List<float> samples = new List<float>(1920000);
        private int channels = 2;
        private int rate;
        private bool stopped;
        // f54331 -> f54168: actual Unity output sample rate.
        private void Awake() { rate = AudioSettings.outputSampleRate; }
        // f54332: stopped guard inside lock, channels assigned before AddRange.
        private void OnAudioFilterRead(float[] data, int channelCount)
        {
            lock (gate)
            {
                if (stopped) return;
                channels = channelCount;
                samples.AddRange(data);
            }
        }
        // f54333: stop before opening the file; retain samples and stopped state
        // even after an I/O error. BinaryWriter owns/disposes the created stream.
        public void Save(string path)
        {
            lock (gate)
            {
                stopped = true;
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    int bytes = unchecked(samples.Count * 2);
                    // Original string slots: 5838196 / 5845604 / 5850396.
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(unchecked(bytes + 36));
                    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write(unchecked((short)channels));
                    writer.Write(rate);
                    writer.Write(unchecked(rate * channels * 2));
                    writer.Write(unchecked((short)(channels * 2)));
                    writer.Write((short)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(bytes);
                    foreach (float value in samples)
                    {
                        // Literal f32.min/mul/select followed by guarded
                        // i32.trunc_f32_s and BinaryWriter.Write(Int16).
                        float pcm = value < -1f ? -32767f : OriginalMath.Binary32(OriginalMath.Min(value, 1f) * 32767f);
                        int integer = System.Math.Abs(pcm) < 2147483648f ? (int)pcm : int.MinValue;
                        writer.Write(unchecked((short)integer));
                    }
                }
            }
        }
    }
}
