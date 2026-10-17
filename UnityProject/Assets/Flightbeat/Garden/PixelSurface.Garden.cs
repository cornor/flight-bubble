#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Game;
using UnityEngine;
using UnityEngine.UI;
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
                ownedMaterial.SetTexture("_GardenTex2", Resources.Load<Texture2D>("GardenR1/creatures"));
                gardenMaterialReady = true;
            }
        }
        public void GardenQuad(float x, float y, float w, float h, Rect uv, Color tint, bool pixel = false, bool rigid = true, int atlas = 0) {
            PrepareGardenMaterial();
            GardenAxisQuad(x, y, w, h, uv, tint, (atlas == 1 ? 22f : 20f) + (pixel ? 1f : 0f), rigid);
        }
        // v3 empty-pad swirl: axis-aligned quad with four explicit UV corners (bl, br, tr, tl), so the texture can rotate
        // while the quad stays flat. Rigid like other sprites: keeps its height and follows the mapped center.
        public void GardenQuadUv(float x, float y, float w, float h, Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color tint, int atlas = 0) {
            PrepareGardenMaterial();
            if (MapLayout) y = ScreenLayout.Y(y + h * .5f) - h * .5f;
            float bottom = ScreenLayout.Height - y - h, top = ScreenLayout.Height - y;
            var v = UIVertex.simpleVert;
            v.color = VertexColor(tint); v.uv1 = new Vector4(atlas == 1 ? 22f : 20f, 0, 0, 0);
            v.position = new Vector3(x, bottom, 0); v.uv0 = new Vector4(bl.x, bl.y, 0, 0); quad[0] = v;
            v.position = new Vector3(x + w, bottom, 0); v.uv0 = new Vector4(br.x, br.y, 0, 0); quad[1] = v;
            v.position = new Vector3(x + w, top, 0); v.uv0 = new Vector4(tr.x, tr.y, 0, 0); quad[2] = v;
            v.position = new Vector3(x, top, 0); v.uv0 = new Vector4(tl.x, tl.y, 0, 0); quad[3] = v;
            mesh.AddUIVertexQuad(quad);
        }
        // Mute: run an original draw routine for its side effects only (cues, hit regions, clocks) while its
        // quads go to a scratch buffer that is thrown away; the bubble version is then drawn on the real mesh.
        VertexHelper mutedMesh;
        static VertexHelper muteSink;
        public void BeginMute() {
            if (mutedMesh != null) return;
            mutedMesh = mesh; mesh = muteSink ?? (muteSink = new VertexHelper());
        }
        public void EndMute() {
            if (mutedMesh == null) return;
            mesh.Clear(); mesh = mutedMesh; mutedMesh = null;
        }
        public void GardenFxQuad(float x, float y, float w, float h, Rect uv, Color tint, float angle = 0f) {
            PrepareGardenMaterial();
            Quad(x, y, w, h, uv, tint, angle, 20f, true, 0f);
        }
    }
}
#endif
