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
                ownedMaterial.SetTexture("_GardenBarsTex", Resources.Load<Texture2D>("GardenR1/bars"));
                gardenMaterialReady = true;
            }
        }
        public void GardenQuad(float x, float y, float w, float h, Rect uv, Color tint, bool pixel = false, bool rigid = true, int atlas = 0) {
            PrepareGardenMaterial();
            GardenAxisQuad(x, y, w, h, uv, tint, 20f + atlas * 2f + (pixel ? 1f : 0f), rigid);
        }
        public void GardenGlassQuad(float x, float y, float w, float h, Rect uv, Color edge, float fill)
        {
            PrepareGardenMaterial();
            Quad(x, y, w, h, uv, edge, 0f, 26f, true, Mathf.Clamp01(fill));
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
        // 2026-10-21: an axis-aligned quad with one colour per corner (screen y-down, MapLayout ignored); `atlas` as in
        // GardenQuad (0 main, 1 creatures, 2 bars). Used for colour ramps and light sweeps on the combo words.
        public void GardenQuadColors(float x, float y, float w, float h, Rect uv, Color tl, Color tr, Color br, Color bl, int atlas = 0) {
            PrepareGardenMaterial();
            float hgt = ScreenLayout.Height;
            var v = UIVertex.simpleVert; v.uv1 = new Vector4(20f + atlas * 2f, 0, 0, 0);
            v.color = VertexColor(bl); v.position = new Vector3(x, hgt - y - h, 0); v.uv0 = new Vector4(uv.xMin, uv.yMin, 0, 0); quad[0] = v;
            v.color = VertexColor(br); v.position = new Vector3(x + w, hgt - y - h, 0); v.uv0 = new Vector4(uv.xMax, uv.yMin, 0, 0); quad[1] = v;
            v.color = VertexColor(tr); v.position = new Vector3(x + w, hgt - y, 0); v.uv0 = new Vector4(uv.xMax, uv.yMax, 0, 0); quad[2] = v;
            v.color = VertexColor(tl); v.position = new Vector3(x, hgt - y, 0); v.uv0 = new Vector4(uv.xMin, uv.yMax, 0, 0); quad[3] = v;
            mesh.AddUIVertexQuad(quad);
        }
        // 2026-10-17: a textured quad with free corners (screen y-down, MapLayout ignored), for rotating art.
        public void GardenQuadCorners(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl, Rect uv, Color tint, int atlas = 0) {
            PrepareGardenMaterial();
            float hgt = ScreenLayout.Height;
            var v = UIVertex.simpleVert;
            v.color = VertexColor(tint); v.uv1 = new Vector4(atlas == 1 ? 22f : 20f, 0, 0, 0);
            v.position = new Vector3(bl.x, hgt - bl.y, 0); v.uv0 = new Vector4(uv.xMin, uv.yMin, 0, 0); quad[0] = v;
            v.position = new Vector3(br.x, hgt - br.y, 0); v.uv0 = new Vector4(uv.xMax, uv.yMin, 0, 0); quad[1] = v;
            v.position = new Vector3(tr.x, hgt - tr.y, 0); v.uv0 = new Vector4(uv.xMax, uv.yMax, 0, 0); quad[2] = v;
            v.position = new Vector3(tl.x, hgt - tl.y, 0); v.uv0 = new Vector4(uv.xMin, uv.yMax, 0, 0); quad[3] = v;
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
