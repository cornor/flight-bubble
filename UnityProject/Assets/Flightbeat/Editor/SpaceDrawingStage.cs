using System;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // One grouped diagnostic across zone boundaries, scrolling phases and aspect ratios.
    public static class SpaceDrawingStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int meshes, checkedVertices;
            public int[] levels = { 0, 1, 2, 31, 32, 81, 82, 141, 142, 201, 202, 261 };
            public float[] clocks = { 0f, .35f, .699f, .7f, 7.3f, 7.31f, 60.5f, 900f };
            public int[] viewportWidths = { 540, 960, 540 }, viewportHeights = { 960, 540, 1280 };
            public int[] zoneSamples = new int[5], maximumVertices = new int[5];
        }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-space-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-space-report required");
            string output = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(output)) throw new IOException("Use a fresh report path");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = new Report { unityVersion = Application.unityVersion };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var owner = new GameObject("Space drawing CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float oldWidth = ScreenLayout.Width, oldHeight = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var surface = owner.AddComponent<PixelSurface>(); surface.MapLayout = false;
                surface.Paint = (Action<PixelSurface>)Delegate.CreateDelegate(typeof(Action<PixelSurface>), game, typeof(PixelWingGame).GetMethod("DrawSpace", flags));
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", flags, null, new[] { typeof(VertexHelper) }, null);
                FieldInfo levelField = typeof(PixelWingGame).GetField("levelIndex", flags), clockField = typeof(PixelWingGame).GetField("clock", flags);
                using (var mesh = new VertexHelper())
                    for (int viewport = 0; viewport < report.viewportWidths.Length; viewport++)
                    {
                        float scale = Math.Min((float)report.viewportWidths[viewport] / 540f, (float)report.viewportHeights[viewport] / 960f);
                        ScreenLayout.Width = (float)report.viewportWidths[viewport] / scale;
                        ScreenLayout.Height = (float)report.viewportHeights[viewport] / scale;
                        foreach (int level in report.levels)
                            foreach (float clock in report.clocks)
                            {
                                levelField.SetValue(game, level); clockField.SetValue(game, clock); populate.Invoke(surface, new object[] { mesh });
                                if (mesh.currentVertCount == 0) throw new Exception("Empty backdrop");
                                int zone = WarZones.OfLevel(level); report.zoneSamples[zone]++; report.meshes++;
                                report.maximumVertices[zone] = Math.Max(report.maximumVertices[zone], mesh.currentVertCount);
                                UIVertex vertex = default;
                                for (int i = 0; i < mesh.currentVertCount; i++)
                                {
                                    mesh.PopulateUIVertex(ref vertex, i); Check(vertex.position.x); Check(vertex.position.y); Check(vertex.position.z);
                                    Check(vertex.uv0.x); Check(vertex.uv0.y); Check(vertex.uv1.x); Check(vertex.uv1.y);
                                    if (vertex.uv0.x < 0f || vertex.uv0.x > 1f || vertex.uv0.y < 0f || vertex.uv0.y > 1f)
                                        throw new Exception("Backdrop UV outside texture");
                                }
                                report.checkedVertices += mesh.currentVertCount;
                            }
                    }
                report.passed = true;
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally
            {
                ScreenLayout.Width = oldWidth; ScreenLayout.Height = oldHeight;
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                PixelSurface surface = owner.GetComponent<PixelSurface>();
                if (surface != null)
                {
                    FieldInfo field = typeof(PixelSurface).GetField("ownedMaterial", flags);
                    var material = (Material)field.GetValue(surface); field.SetValue(surface, null);
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static void Check(float value) { if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("Non-finite background mesh"); }
    }
}
