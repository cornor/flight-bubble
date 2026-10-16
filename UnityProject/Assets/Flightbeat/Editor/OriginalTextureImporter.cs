using System;
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Flightbeat.Editor
{
    // src: each original serialized Texture2D record, identified in its container.
    // Bypasses PNG alpha dilation, resampling, recompression and mip generation.
    [ScriptedImporter(2, "fbtexture")]
    public sealed class OriginalTextureImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            OriginalTextureData source = OriginalTextureData.Load(context.assetPath);
            var texture = new Texture2D(source.Width, source.Height, (TextureFormat)source.Format,
                source.MipCount, !source.SRGB);
            texture.name = source.Name;
            texture.filterMode = (FilterMode)source.FilterMode;
            texture.anisoLevel = source.AnisoLevel;
            texture.mipMapBias = BitConverter.Int32BitsToSingle(unchecked((int)source.MipBiasBits));
            texture.wrapModeU = (TextureWrapMode)source.WrapU;
            texture.wrapModeV = (TextureWrapMode)source.WrapV;
            texture.wrapModeW = (TextureWrapMode)source.WrapW;
            texture.LoadRawTextureData(source.Payload);
            texture.Apply(false, !source.Readable);
            Verify(texture, source);
            context.AddObjectToAsset("texture", texture);
            context.SetMainObject(texture);
        }

        public static void Verify(Texture2D texture, OriginalTextureData source)
        {
            if (!texture) throw new InvalidOperationException("原纹理尚未导入：" + source.SourceFile + ":" + source.PathId);
            var differences = new List<string>();
            void Check<T>(string name, T actual, T expected)
            {
                if (!EqualityComparer<T>.Default.Equals(actual, expected))
                    differences.Add(name + "=" + actual + ", expected=" + expected);
            }
            Check("name", texture.name, source.Name);
            Check("width", texture.width, source.Width);
            Check("height", texture.height, source.Height);
            Check("format", (int)texture.format, source.Format);
            Check("mipCount", texture.mipmapCount, source.MipCount);
            Check("readable", texture.isReadable, source.Readable);
            Check("filterMode", (int)texture.filterMode, source.FilterMode);
            Check("anisoLevel", texture.anisoLevel, source.AnisoLevel);
            Check("mipBiasBits", unchecked((uint)BitConverter.SingleToInt32Bits(texture.mipMapBias)), source.MipBiasBits);
            Check("wrapU", (int)texture.wrapModeU, source.WrapU);
            Check("wrapV", (int)texture.wrapModeV, source.WrapV);
            Check("wrapW", (int)texture.wrapModeW, source.WrapW);
            // Source Texture2D.m_ColorSpace describes the stored pixel data.
            // GPU sampling format also depends on the project's active color space;
            // it must not be compared directly to this serialized data flag.
            // Unity 2022.3 API: Texture.isDataSRGB; no pixel/color conversion here.
            Check("dataSRGB", texture.isDataSRGB, source.SRGB);
            if (differences.Count > 0)
                throw new InvalidOperationException("原纹理导入参数不一致：" + source.SourceFile + ":" + source.PathId
                    + " (" + string.Join("; ", differences) + "; device=" + SystemInfo.graphicsDeviceType + ")");
        }
    }
}
