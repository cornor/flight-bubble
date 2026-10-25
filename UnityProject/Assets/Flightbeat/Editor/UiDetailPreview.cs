#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Static rendering fixture only: no game startup, profile writes or gameplay claims.
    public static class UiDetailPreview
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static void Capture()
        {
            string output = Path.GetFullPath("../Art/PlushToy/UiDetailSettings-20261024/preview");
            Directory.CreateDirectory(output);
            var root = new GameObject("UI detail preview");
            var camera = new GameObject("Preview camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(root.transform);
            camera.orthographic = true; camera.orthographicSize = 480;
            camera.transform.position = new Vector3(270, 480, -20);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GardenSkin.Hex(0xD0BDF0);
            var canvas = new GameObject("Preview canvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.transform.SetParent(root.transform);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 20;
            canvas.pixelPerfect = true; canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.GetComponent<CanvasScaler>().scaleFactor = 2;
            var surface = new GameObject("Preview art", typeof(RectTransform), typeof(CanvasRenderer), typeof(PixelSurface)).GetComponent<PixelSurface>();
            surface.transform.SetParent(canvas.transform, false);
            var rect = surface.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(540, 960); rect.anchoredPosition = new Vector2(-270, -480);
            typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(surface, null);
            var rt = new RenderTexture(1080, 1920, 24);
            var image = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
            var active = RenderTexture.active; bool plush = PlushLayout.Enabled;
            using var battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector01").text), null);
            var pool = new PixelEffectPool(root.transform, rect) { Enabled = false };
            var feedback = new CombatFeedback(battle, pool, CombatPalette.ForLevel(battle.Level));
            void Set(string field, object value) => typeof(CombatFeedback).GetField(field, Flags).SetValue(feedback, value);
            try
            {
                PlushLayout.Enabled = true;
                ScreenLayout.Configure(540, 960, new Rect(0, 0, 1, 1));
                rt.Create(); camera.targetTexture = rt;
                foreach (int count in new[] { 9, 99, 1000, 8000 })
                foreach (bool punch in new[] { false, true })
                {
                    Set("lastCombo", count); Set("comboFade", 1f);
                    Set("comboPunch", punch ? 0f : 1f); Set("comboMilestoneAge", punch ? 0f : 10f);
                    Set("gardenComboSlot", 0f); Set("gardenComboSlotTarget", 0f);
                    surface.Paint = s => {
                        s.MapLayout = false;
                        int[] colors = { 0x2982F4, 0xFF9D30, 0xC5FFE4, 0xFFCAA9, 0xBD80FF };
                        for (int i = 0; i < colors.Length; i++)
                            GardenSkin.Creature(s, GardenSkin.Hex(colors[i]), 70 + i * 100, 330, 110, 3, "450", breath: .02f, ready: 1f, fire: 0f);
                        GardenBarArt.Text(s, "MAX", 270, 400, 24, true, GardenBars.Navy);
                        feedback.DrawBubbleCombo(s);
                    };
                    surface.Redraw(); Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, $"max-combo-{count}-{(punch ? "punch" : "idle")}.png"), image.EncodeToPNG());
                }
                Debug.Log("UI_DETAIL_PREVIEW_CAPTURED: static MAX poses and four combo widths at rest/peak pulse");
            }
            finally
            {
                PlushLayout.Enabled = plush; RenderTexture.active = active; camera.targetTexture = null;
                Object.DestroyImmediate(root); Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
#endif
