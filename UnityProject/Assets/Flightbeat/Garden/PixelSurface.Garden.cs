#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Game;
using UnityEngine;
namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        bool gardenMaterialReady;
        // Rigid sprites retain the 10.5 / 5.5 logical-pixel size on tall viewports.
        public void GardenSolid(float x, float y, float w, float h, Color tint) => Quad(x, y, w, h, WhiteUv, tint, 0f, 0f, true, 0f);
        void PrepareGardenMaterial() {
            if (!gardenMaterialReady) {
                ownedMaterial.shader = Resources.Load<Shader>("GardenR1/GardenSurface");
                ownedMaterial.SetTexture("_GardenTex", Resources.Load<Texture2D>("GardenR1/atlas"));
                gardenMaterialReady = true;
            }
        }
        public void GardenQuad(float x, float y, float w, float h, Rect uv, Color tint, bool pixel = false, bool rigid = true) {
            PrepareGardenMaterial();
            GardenAxisQuad(x, y, w, h, uv, tint, pixel ? 21f : 20f, rigid);
        }
        // v3 empty-pad swirl: axis-aligned quad with four explicit UV corners (bl, br, tr, tl), so the texture can rotate
        // while the quad stays flat. Rigid like other sprites: keeps its height and follows the mapped center.
        public void GardenQuadUv(float x, float y, float w, float h, Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color tint) {
            PrepareGardenMaterial();
            if (MapLayout) y = ScreenLayout.Y(y + h * .5f) - h * .5f;
            float bottom = ScreenLayout.Height - y - h, top = ScreenLayout.Height - y;
            var v = UIVertex.simpleVert;
            v.color = VertexColor(tint); v.uv1 = new Vector4(20f, 0, 0, 0);
            v.position = new Vector3(x, bottom, 0); v.uv0 = new Vector4(bl.x, bl.y, 0, 0); quad[0] = v;
            v.position = new Vector3(x + w, bottom, 0); v.uv0 = new Vector4(br.x, br.y, 0, 0); quad[1] = v;
            v.position = new Vector3(x + w, top, 0); v.uv0 = new Vector4(tr.x, tr.y, 0, 0); quad[2] = v;
            v.position = new Vector3(x, top, 0); v.uv0 = new Vector4(tl.x, tl.y, 0, 0); quad[3] = v;
            mesh.AddUIVertexQuad(quad);
        }
        public void GardenFxQuad(float x, float y, float w, float h, Rect uv, Color tint, float angle = 0f) {
            PrepareGardenMaterial();
            Quad(x, y, w, h, uv, tint, angle, 20f, true, 0f);
        }
    }
}
#endif
