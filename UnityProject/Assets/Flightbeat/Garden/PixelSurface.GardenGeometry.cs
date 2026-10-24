#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        // f54395 at angle=0: reuse identical per-quad coordinates and vertex color.
        // Keep every surviving f32 operation/grouping. The original generic path
        // (and all original game rendering) remains unchanged.
        static readonly Dictionary<Color, Color32> gardenColors = new Dictionary<Color, Color32>(64);
        void GardenAxisQuad(float x, float y, float w, float h, Rect uv, Color color, float mode, bool rigid)
        {
            if (!(w > 0f && h > 0f) || float.IsInfinity(w) || float.IsInfinity(h)) {
                Quad(x, y, w, h, uv, color, 0f, mode, rigid, 0f); return;
            }
            float halfH = OriginalMath.Binary32(h * .5f);
            if (MapLayout) {
                if (rigid) y = OriginalMath.Binary32(PlushLayout.Y(OriginalMath.Binary32(halfH + y)) - halfH);
                else {
                    float bottom = PlushLayout.Y(OriginalMath.Binary32(y + h));
                    y = PlushLayout.Y(y); h = OriginalMath.Binary32(bottom - y);
                    halfH = OriginalMath.Binary32(h * .5f);
                }
            }
            float halfW = OriginalMath.Binary32(w * .5f);
            x = OriginalMath.Binary32(halfW + x);
            float left = OriginalMath.Binary32(x + OriginalMath.Binary32(w * -.5f));
            float right = OriginalMath.Binary32(x + halfW);
            // angle=0 produces sin=-0, positive leftSin and negative rightSin.
            float baseY = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height - y) - halfH);
            float bottomY = OriginalMath.Binary32(OriginalMath.Binary32(h * -.5f) + OriginalMath.Binary32(0f + baseY));
            float topY = OriginalMath.Binary32(halfH + OriginalMath.Binary32(0f + baseY));
            if (!gardenColors.TryGetValue(color, out Color32 packed)) {
                packed = VertexColor(color);
                if (gardenColors.Count >= 64) gardenColors.Clear();
                gardenColors.Add(color, packed);
            }
            var v = UIVertex.simpleVert;
            v.color = packed; v.uv1 = new Vector4(mode, 0, 0, 0);
            float uvRight = OriginalMath.Binary32(uv.width + uv.x), uvTop = OriginalMath.Binary32(uv.y + uv.height);
            v.position = new Vector3(left, bottomY, 0); v.uv0 = new Vector4(uv.x, uv.y, 0, 0); quad[0] = v;
            v.position = new Vector3(right, bottomY, 0); v.uv0 = new Vector4(uvRight, uv.y, 0, 0); quad[1] = v;
            v.position = new Vector3(right, topY, 0); v.uv0 = new Vector4(uvRight, uvTop, 0, 0); quad[2] = v;
            v.position = new Vector3(left, topY, 0); v.uv0 = new Vector4(uv.x, uvTop, 0, 0); quad[3] = v;
            mesh.AddUIVertexQuad(quad);
        }
    }
}
#endif
