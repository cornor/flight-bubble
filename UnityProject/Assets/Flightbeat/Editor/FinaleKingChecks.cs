#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class FinaleKingChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            var root = new GameObject("Finale king fixture", typeof(RectTransform));
            root.SetActive(false);
            var surface = root.AddComponent<PixelSurface>();
            var game = root.AddComponent<PixelWingGame>();
            bool plush = PlushLayout.Enabled;
            try
            {
                typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(surface, null);
                PlushLayout.Enabled = true;
                foreach (var viewport in new[] { new Vector2Int(540, 960), new Vector2Int(390, 844) })
                using (var mesh = new VertexHelper())
                {
                    ScreenLayout.Configure(viewport.x, viewport.y, new Rect(0, 0, 1, 1));
                    typeof(PixelSurface).GetField("mesh", Flags).SetValue(surface, mesh);
                    surface.MapLayout = true;
                    UIVertex[] fixedBase = null;
                    float normalBodyHeight = 0f;
                    foreach (float age in new[] { 1.15f, 1.649f, 1.65f, 1.8f, 2.5f, 3.299f })
                    {
                        var finale = new Finale { Age = age };
                        typeof(PixelWingGame).GetField("clock", Flags).SetValue(game, age);
                        var args = new object[] { finale, age, Vector2.zero, 0f };
                        typeof(PixelWingGame).GetMethod("KingBodyPose", Flags).Invoke(game, args);
                        var body = (Vector2)args[2]; float sway = (float)args[3];
                        string art = age >= 1.65f ? "plants/finale/king-blow" : "plants/finale/king-ready";
                        float lift = Mathf.Min((age - 1.15f) * 30f, 10f);
                        Vector2 center = new Vector2(270f, 694f - lift);
                        mesh.Clear();
                        Require(GardenSkin.FinaleKing(surface, art, "plants/finale/king-ready", center, 250f, body, sway, Color.white), "Both registered frames must exist");
                        Require(surface.MapLayout && mesh.currentVertCount <= 100, "Bounded mesh and unchanged layout flag");
                        var vertices = new UIVertex[mesh.currentVertCount];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            mesh.PopulateUIVertex(ref vertices[i], i);
                            var v = vertices[i];
                            Require(!float.IsNaN(v.position.x) && !float.IsNaN(v.position.y) && v.uv0.x >= 0f && v.uv0.x <= 1f && v.uv0.y >= 0f && v.uv0.y <= 1f, "Finite positions and valid atlas UVs");
                        }
                        // All four actual base vertices and texels remain identical, even
                        // across ready/blow; remove only the original shared lift.
                        var baseVertices = vertices.Skip(vertices.Length - 4).ToArray();
                        for (int i = 0; i < 4; i++) baseVertices[i].position.y += PlushLayout.Y(center.y);
                        if (fixedBase == null) fixedBase = baseVertices;
                        else for (int i = 0; i < 4; i++)
                            Require((fixedBase[i].position - baseVertices[i].position).sqrMagnitude < .001f && fixedBase[i].uv0 == baseVertices[i].uv0, "Pot must not grow, shake or switch textures");
                        float top = vertices.Take(vertices.Length - 4).Max(v => v.position.y);
                        float rootY = ScreenLayout.Height - (PlushLayout.Y(center.y) + (GardenSkin.FinaleKingRoot - .5f) * 250f);
                        float height = top - rootY;
                        if (age == 1.15f) normalBodyHeight = height;
                        if (age == 1.649f) Require(height / normalBodyHeight > 1.7f && height / normalBodyHeight < 1.8f && body.x > 1.4f, "Last inhale must visibly grow the body, not the base");
                        var slot = GardenSkin.Data.slots.First(s => s.id == art);
                        var sourceMouth = (Vector2)typeof(PixelWingGame).GetField(age >= 1.65f ? "KingBlowMouth" : "KingReadyMouth", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                        var uv = new Vector2((slot.x + sourceMouth.x * slot.fw - slot.ox) / GardenSkin.Data.creatureWidth,
                            1f - (slot.y + sourceMouth.y * slot.fh - slot.oy) / GardenSkin.Data.creatureHeight);
                        bool found = false;
                        for (int i = 0; i < vertices.Length - 4; i += 4)
                        {
                            var bl = vertices[i]; var br = vertices[i + 1]; var tr = vertices[i + 2]; var tl = vertices[i + 3];
                            if (uv.y < bl.uv0.y || uv.y > tl.uv0.y) continue;
                            float u = (uv.x - bl.uv0.x) / (br.uv0.x - bl.uv0.x), v = (uv.y - bl.uv0.y) / (tl.uv0.y - bl.uv0.y);
                            Vector3 drawn = Vector3.Lerp(Vector3.Lerp(bl.position, br.position, u), Vector3.Lerp(tl.position, tr.position, u), v);
                            var mouth = (Vector2)typeof(PixelWingGame).GetMethod("KingMouthAt", Flags).Invoke(game, new object[] { finale, age, 0f });
                            Require((new Vector2(drawn.x, ScreenLayout.Height - drawn.y) - PlushLayout.Point(mouth)).sqrMagnitude < .01f, "Fan origin must follow the actual rendered mouth");
                            found = true; break;
                        }
                        Require(found, "Mouth must lie inside a rendered body strip");
                    }
                }
                Debug.Log("FINALE_KING_CHECKS_PASSED: fixed pot vertices/UV, 1.75x inhale, registered mouth through ready/recoil/blow, bounded mesh, standard/tall viewports");
            }
            finally
            {
                PlushLayout.Enabled = plush;
                var materialField = typeof(PixelSurface).GetField("ownedMaterial", Flags);
                var material = (Material)materialField.GetValue(surface); materialField.SetValue(surface, null);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
