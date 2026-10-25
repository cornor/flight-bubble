#if UNITY_EDITOR
using System;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class CirclePresentationChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static void Run()
        {
            var root = new GameObject("Circle fixture", typeof(RectTransform));
            root.SetActive(false);
            bool previous = PlushLayout.Enabled;
            try
            {
                var surface = root.AddComponent<PixelSurface>();
                typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(surface, null);
                var game = root.AddComponent<PixelWingGame>();
                using var battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector04").text), null);
                typeof(PixelWingGame).GetProperty("Battle", Flags).SetValue(game, battle);
                using var mesh = new VertexHelper();
                typeof(PixelSurface).GetField("mesh", Flags).SetValue(surface, mesh);
                PlushLayout.Enabled = true;
                foreach (var viewport in new[] { new Vector2Int(540, 960), new Vector2Int(390, 844), new Vector2Int(768, 1024) })
                {
                    ScreenLayout.Configure(viewport.x, viewport.y, new Rect(0, 0, 1, 1));
                    Rect WhiteBounds()
                    {
                        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                        var v = new UIVertex();
                        for (int i = 0; i < mesh.currentVertCount; i++) {
                            mesh.PopulateUIVertex(ref v, i);
                            if (v.color.r != 255 || v.color.g != 255 || v.color.b != 255 || v.color.a != 255) continue;
                            var p = new Vector2(v.position.x, ScreenLayout.Height - v.position.y);
                            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                        }
                        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    }
                    foreach (float y in new[] { 160f, 350f, 530f }) {
                        mesh.Clear(); surface.MapLayout = true;
                        typeof(PixelWingGame).GetField("aim", Flags).SetValue(game, new Vector2(270, y));
                        typeof(PixelWingGame).GetMethod("DrawBombAim", Flags).Invoke(game, new object[] { surface });
                        var b = WhiteBounds();
                        if (!surface.MapLayout || float.IsInfinity(b.width) || Mathf.Abs(b.width - b.height) > .1f)
                            throw new Exception($"Bomb distorted at {viewport}, y={y}: {b}");
                        if ((b.center - PlushLayout.Point(new Vector2(270, y))).magnitude > .1f) throw new Exception("Bomb centre moved");
                    }
                    foreach (float y in new[] { 640f, 694f, 759f }) {
                        mesh.Clear(); surface.MapLayout = true;
                        typeof(PixelWingGame).GetMethod("FinaleScreenRing", Flags).Invoke(game, new object[] { surface, new Vector2(270, y), 120f, Color.white, 4f });
                        var b = WhiteBounds();
                        if (!surface.MapLayout || float.IsInfinity(b.width) || Mathf.Abs(b.width - b.height) > .1f) throw new Exception($"Finale distorted: {b}");
                    }
                    float stretch = (float)typeof(PixelWingGame).GetProperty("SuperAimStretch", Flags).GetValue(game);
                    float modelRadius = 120f / (stretch * ScreenLayout.EnemyScaleY);
                    var center = ScreenLayout.EnemyPoint(270f, 400f);
                    var edge = ScreenLayout.EnemyPoint(270f, 400f + modelRadius);
                    float distance = PlushLayout.Y(edge.y) - PlushLayout.Y(center.y);
                    if (Mathf.Abs(distance - 120f) > .02f) throw new Exception($"Bomb hit radius mismatch: {distance}");
                }
                Debug.Log("CIRCLE_PRESENTATION_PASSED: three viewports, bomb and finale mesh circles, mapped centres and bomb hit radius");
            }
            finally { PlushLayout.Enabled = previous; UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
#endif
