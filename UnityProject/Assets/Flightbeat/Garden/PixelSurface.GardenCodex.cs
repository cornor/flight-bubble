#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        // Flat, antialiased UI surfaces. One quad per shape; colors interpolate from top to bottom.
        // Coordinates are already in screen space, like the rest of the album (MapLayout is off).
        public void GardenRoundedRect(float x, float y, float w, float h, float radius, Color top, Color bottom)
        {
            if (w <= 0f || h <= 0f) return;
            PrepareGardenMaterial();
            float hw = w * .5f, hh = h * .5f, sh = ScreenLayout.Height;
            var v = UIVertex.simpleVert;
            v.uv1 = new Vector4(30f, Mathf.Clamp01(radius / Mathf.Min(hw, hh)), 0, 0);
            v.color = VertexColor(bottom);
            v.position = new Vector3(x, sh - y - h, 0); v.uv0 = new Vector4(-1, -1, 0, 0); quad[0] = v;
            v.position = new Vector3(x + w, sh - y - h, 0); v.uv0 = new Vector4(1, -1, 0, 0); quad[1] = v;
            v.color = VertexColor(top);
            v.position = new Vector3(x + w, sh - y, 0); v.uv0 = new Vector4(1, 1, 0, 0); quad[2] = v;
            v.position = new Vector3(x, sh - y, 0); v.uv0 = new Vector4(-1, 1, 0, 0); quad[3] = v;
            mesh.AddUIVertexQuad(quad);
        }
        public void GardenSilhouetteQuad(float x, float y, float w, float h, Rect uv, Color color)
        {
            PrepareGardenMaterial();
            GardenAxisQuad(x, y, w, h, uv, color, 28f, true);
        }
    }
}
#endif
