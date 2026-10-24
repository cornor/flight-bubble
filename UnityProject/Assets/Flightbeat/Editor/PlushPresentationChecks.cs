#if UNITY_EDITOR
using System;
using Flightbeat.Game;
using UnityEditor;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class PlushPresentationChecks
    {
        public static void Run()
        {
            bool enabled = PlushLayout.Enabled;
            try
            {
                PlushLayout.Enabled = true;
                foreach (var size in new[] { new Vector2Int(540, 960), new Vector2Int(390, 844), new Vector2Int(430, 932), new Vector2Int(768, 1024) })
                {
                    ScreenLayout.Configure(size.x, size.y, new Rect(0, .015f, 1, .955f));
                    float previous = float.NegativeInfinity;
                    for (float y = -100; y <= 1100; y += .5f)
                    {
                        float projected = PlushLayout.Y(y);
                        if (projected <= previous || Mathf.Abs(PlushLayout.InverseY(projected) - y) > .002f)
                            throw new Exception($"Non-invertible plush layout at {size}, y={y}");
                        previous = projected;
                    }
                    if (PlushLayout.Y(574) >= PlushLayout.Y(688) || PlushLayout.Y(868) >= PlushLayout.Y(880))
                        throw new Exception("Board, queue or footer overlap");
                }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flightbeat/Resources/GardenR1/creatures.png");
                if (!texture || texture.width != 4096 || texture.height != 3072) throw new Exception("Character atlas was resampled; plush fur would blur");
                Debug.Log("PLUSH_PRESENTATION_CHECKS_PASSED: four viewports, monotonic round-trip input, separate regions, full-resolution character atlas");
            }
            finally { PlushLayout.Enabled = enabled; }
        }
    }
}
#endif
