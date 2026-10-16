using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Flightbeat.Editor
{
    // src: resources.assets Material pathIds 1..4; original saved properties and ResourceManager.
    // Import-time dependencies resolve actual subasset identities; no guessed Texture2D fileIDs.
    [ScriptedImporter(2, "fbmaterial", 1)]
    public sealed class OriginalMaterialImporter : ScriptedImporter
    {
        // Declare dependencies before import ordering is chosen. Waiting until
        // OnImportAsset can make the first import race the texture importer.
        private static string[] GatherDependenciesFromSourceFile(string path)
        {
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(path));
            var paths = new List<string> { source.shaderPath };
            foreach (TextureProperty texture in source.textures)
                if (texture.sourcePathId != 0) paths.Add(texture.assetPath);
            return paths.ToArray();
        }

        [Serializable] public sealed class TextureProperty
        {
            public string name, assetPath;
            public long sourcePathId;
            public float[] scale, offset;
        }
        [Serializable] public sealed class FloatProperty { public string name; public float value; }
        [Serializable] public sealed class ColorProperty { public string name; public float[] rgba; }
        [Serializable] public sealed class Source
        {
            public int version, lightmapFlags, customRenderQueue;
            public string sourceFile, sourceObjectSha256, name, resourcePath, shaderPath;
            public long sourcePathId, sourceShaderPathId;
            public bool enableInstancing, doubleSidedGI;
            public string[] validKeywords, invalidKeywords, disabledPasses;
            public TextureProperty[] textures;
            public FloatProperty[] floats;
            public ColorProperty[] colors;
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            Source source = JsonUtility.FromJson<Source>(File.ReadAllText(context.assetPath));
            if (source.version != 1 || source.sourceFile != "resources.assets" ||
                source.sourcePathId < 1 || source.sourcePathId > 4)
                throw new InvalidDataException("未实现的原材质记录：" + context.assetPath);
            Shader shader = LoadDependency<Shader>(context, source.shaderPath);
            var material = new Material(shader) { name = source.name };
            using (var serialized = new SerializedObject(material))
            {
                Field(serialized, "m_LightmapFlags").intValue = source.lightmapFlags;
                Field(serialized, "m_EnableInstancingVariants").boolValue = source.enableInstancing;
                Field(serialized, "m_DoubleSidedGI").boolValue = source.doubleSidedGI;
                Field(serialized, "m_CustomRenderQueue").intValue = source.customRenderQueue;
                Strings(Field(serialized, "m_ValidKeywords"), source.validKeywords);
                Strings(Field(serialized, "m_InvalidKeywords"), source.invalidKeywords);
                Strings(Field(serialized, "disabledShaderPasses"), source.disabledPasses);
                // All four original tag maps, integer properties and texture stacks are empty.
                Field(serialized, "stringTagMap").arraySize = 0;
                Field(serialized, "m_BuildTextureStacks").arraySize = 0;
                Field(serialized, "m_SavedProperties.m_Ints").arraySize = 0;
                var textures = Field(serialized, "m_SavedProperties.m_TexEnvs");
                textures.arraySize = source.textures.Length;
                for (int i = 0; i < source.textures.Length; i++)
                {
                    TextureProperty value = source.textures[i];
                    var pair = Pair(textures, i, value.name);
                    Texture2D texture = null;
                    if (value.sourcePathId != 0)
                        texture = LoadDependency<Texture2D>(context, value.assetPath);
                    else if (!string.IsNullOrEmpty(value.assetPath))
                        throw new InvalidDataException("原空纹理引用不能指向候选资源。");
                    pair.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
                    pair.FindPropertyRelative("m_Scale").vector2Value = Vector(value.scale);
                    pair.FindPropertyRelative("m_Offset").vector2Value = Vector(value.offset);
                }
                var floats = Field(serialized, "m_SavedProperties.m_Floats");
                floats.arraySize = source.floats.Length;
                for (int i = 0; i < source.floats.Length; i++)
                    Pair(floats, i, source.floats[i].name).floatValue = source.floats[i].value;
                var colors = Field(serialized, "m_SavedProperties.m_Colors");
                colors.arraySize = source.colors.Length;
                for (int i = 0; i < source.colors.Length; i++)
                {
                    var value = source.colors[i];
                    if (value.rgba.Length != 4) throw new InvalidDataException("Invalid original color.");
                    Pair(colors, i, value.name).colorValue = new Color(value.rgba[0], value.rgba[1], value.rgba[2], value.rgba[3]);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Verify(material, source);
            context.AddObjectToAsset("material", material);
            context.SetMainObject(material);
        }

        public static void Verify(Material material, Source source)
        {
            Require(material && material.name == source.name, "material name");
            Require(material.shader == AssetDatabase.LoadAssetAtPath<Shader>(source.shaderPath), "shader identity");
            using (var serialized = new SerializedObject(material))
            {
                Require(Field(serialized, "m_LightmapFlags").intValue == source.lightmapFlags, "lightmap flags");
                Require(Field(serialized, "m_CustomRenderQueue").intValue == source.customRenderQueue, "render queue");
                Require(Field(serialized, "m_EnableInstancingVariants").boolValue == source.enableInstancing, "instancing");
                Require(Field(serialized, "m_DoubleSidedGI").boolValue == source.doubleSidedGI, "double-sided GI");
                VerifyStrings(Field(serialized, "m_ValidKeywords"), source.validKeywords);
                VerifyStrings(Field(serialized, "m_InvalidKeywords"), source.invalidKeywords);
                VerifyStrings(Field(serialized, "disabledShaderPasses"), source.disabledPasses);
                foreach (string path in new[] { "stringTagMap", "m_BuildTextureStacks", "m_SavedProperties.m_Ints" })
                    Require(Field(serialized, path).arraySize == 0, path);
                var textures = Field(serialized, "m_SavedProperties.m_TexEnvs");
                Require(textures.arraySize == source.textures.Length, "texture count");
                for (int i = 0; i < source.textures.Length; i++)
                {
                    var expected = source.textures[i];
                    var actual = ReadPair(textures, i, expected.name);
                    var reference = string.IsNullOrEmpty(expected.assetPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(expected.assetPath);
                    Require(string.IsNullOrEmpty(expected.assetPath) || reference, expected.name + " dependency exists");
                    Require(actual.FindPropertyRelative("m_Texture").objectReferenceValue == reference, expected.name + " reference");
                    var scale = actual.FindPropertyRelative("m_Scale").vector2Value;
                    var offset = actual.FindPropertyRelative("m_Offset").vector2Value;
                    Require(Bits(scale.x, expected.scale[0]) && Bits(scale.y, expected.scale[1]) &&
                        Bits(offset.x, expected.offset[0]) && Bits(offset.y, expected.offset[1]), expected.name + " transform");
                }
                var floats = Field(serialized, "m_SavedProperties.m_Floats");
                Require(floats.arraySize == source.floats.Length, "float count");
                for (int i = 0; i < source.floats.Length; i++)
                    Require(Bits(ReadPair(floats, i, source.floats[i].name).floatValue, source.floats[i].value), "float value");
                var colors = Field(serialized, "m_SavedProperties.m_Colors");
                Require(colors.arraySize == source.colors.Length, "color count");
                for (int i = 0; i < source.colors.Length; i++)
                {
                    Color c = ReadPair(colors, i, source.colors[i].name).colorValue;
                    var v = source.colors[i].rgba;
                    Require(Bits(c.r, v[0]) && Bits(c.g, v[1]) && Bits(c.b, v[2]) && Bits(c.a, v[3]), "color value");
                }
            }
        }

        private static void Require(bool condition, string field)
        {
            if (!condition) throw new InvalidDataException("原材质导入不一致：" + field);
        }
        private static bool Bits(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
        private static SerializedProperty ReadPair(SerializedProperty array, int index, string name)
        {
            var pair = array.GetArrayElementAtIndex(index);
            Require(pair.FindPropertyRelative("first").stringValue == name, "property name/order");
            return pair.FindPropertyRelative("second");
        }
        private static void VerifyStrings(SerializedProperty array, string[] values)
        {
            Require(array.arraySize == values.Length, "keyword/pass count");
            for (int i = 0; i < values.Length; i++) Require(array.GetArrayElementAtIndex(i).stringValue == values[i], "keyword/pass value");
        }

        private static T LoadDependency<T>(AssetImportContext context, string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/Flightbeat/Resources/", StringComparison.Ordinal) || path.Contains(".."))
                throw new InvalidDataException("Invalid original material dependency path.");
            context.DependsOnArtifact(path);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) throw new InvalidDataException("原材质依赖尚未导入：" + path);
            return asset;
        }

        private static SerializedProperty Field(SerializedObject value, string path) =>
            value.FindProperty(path) ?? throw new InvalidOperationException("Unity material serialization field missing: " + path);
        private static SerializedProperty Pair(SerializedProperty array, int index, string name)
        {
            var pair = array.GetArrayElementAtIndex(index);
            pair.FindPropertyRelative("first").stringValue = name;
            return pair.FindPropertyRelative("second");
        }
        private static Vector2 Vector(float[] value)
        {
            if (value.Length != 2) throw new InvalidDataException("Invalid original texture transform.");
            return new Vector2(value[0], value[1]);
        }
        private static void Strings(SerializedProperty array, string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
