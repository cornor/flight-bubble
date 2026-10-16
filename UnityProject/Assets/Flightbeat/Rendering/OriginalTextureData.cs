using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Flightbeat.Rendering
{
    // Container for the original Texture2D payload and public runtime parameters.
    // The payload is copied from the published serialized object, never from PNG.
    public sealed class OriginalTextureData
    {
        private const string Magic = "FBTEX001";
        public string SourceFile { get; private set; }
        public long PathId { get; private set; }
        public string Name { get; private set; }
        public string ResourcePath { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Format { get; private set; }
        public int MipCount { get; private set; }
        public int FilterMode { get; private set; }
        public int AnisoLevel { get; private set; }
        public uint MipBiasBits { get; private set; }
        public int WrapU { get; private set; }
        public int WrapV { get; private set; }
        public int WrapW { get; private set; }
        public bool SRGB { get; private set; }
        public bool Readable { get; private set; }
        public byte[] Payload { get; private set; }
        public byte[] PayloadHash { get; private set; }

        private OriginalTextureData() { }

        public static OriginalTextureData Load(string path)
        {
            using var input = File.OpenRead(path);
            return Read(input);
        }

        public static OriginalTextureData Read(Stream input)
        {
            using var reader = new BinaryReader(input, new UTF8Encoding(false, true), true);
            if (Encoding.ASCII.GetString(ReadExact(reader, 8)) != Magic)
                throw new InvalidDataException("Unknown Flightbeat texture container version.");
            var data = new OriginalTextureData
            {
                SourceFile = reader.ReadString(), PathId = reader.ReadInt64(),
                Name = reader.ReadString(), ResourcePath = reader.ReadString(),
                Width = reader.ReadInt32(), Height = reader.ReadInt32(),
                Format = reader.ReadInt32(), MipCount = reader.ReadInt32(),
                FilterMode = reader.ReadInt32(), AnisoLevel = reader.ReadInt32(),
                MipBiasBits = reader.ReadUInt32(), WrapU = reader.ReadInt32(),
                WrapV = reader.ReadInt32(), WrapW = reader.ReadInt32(),
                SRGB = ReadFlag(reader), Readable = ReadFlag(reader)
            };
            long length = reader.ReadInt64();
            if (length != data.ExpectedPayloadBytes() || length > int.MaxValue)
                throw new InvalidDataException("Original texture payload length does not match its format/mips.");
            data.PayloadHash = ReadExact(reader, 32);
            data.Payload = ReadExact(reader, (int)length);
            if (input.ReadByte() != -1) throw new InvalidDataException("Unexpected trailing texture data.");
            using var hash = SHA256.Create();
            byte[] actual = hash.ComputeHash(data.Payload);
            for (int i = 0; i < actual.Length; i++)
                if (actual[i] != data.PayloadHash[i]) throw new InvalidDataException("Original texture payload SHA-256 mismatch.");
            return data;
        }

        public long ExpectedPayloadBytes()
        {
            if (Width <= 0 || Height <= 0 || MipCount <= 0 || FilterMode < 0 || FilterMode > 2 ||
                AnisoLevel < 0 || AnisoLevel > 16 || WrapU < 0 || WrapU > 3 ||
                WrapV < 0 || WrapV > 3 || WrapW < 0 || WrapW > 3 ||
                float.IsNaN(BitConverter.Int32BitsToSingle(unchecked((int)MipBiasBits))))
                throw new InvalidDataException("Invalid original texture parameters.");
            long size = 0;
            int w = Width, h = Height;
            for (int i = 0; i < MipCount; i++)
            {
                checked
                {
                    if (Format == 3) size += (long)w * h * 3; // Unity TextureFormat.RGB24
                    else if (Format == 4) size += (long)w * h * 4; // RGBA32
                    else if (Format == 12) size += ((w + 3L) / 4) * ((h + 3L) / 4) * 16; // DXT5
                    else throw new NotSupportedException("未实现的原纹理格式：" + Format);
                }
                if (i + 1 < MipCount && w == 1 && h == 1)
                    throw new InvalidDataException("Too many original mip levels.");
                w = Math.Max(1, w / 2); h = Math.Max(1, h / 2);
            }
            return size;
        }

        private static byte[] ReadExact(BinaryReader reader, int count)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException("Truncated original texture container.");
            return bytes;
        }

        private static bool ReadFlag(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid texture flag.");
            return value != 0;
        }
    }
}
