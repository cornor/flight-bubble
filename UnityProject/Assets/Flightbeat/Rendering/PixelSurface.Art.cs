using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        private ISurfaceTextureRuntime textureRuntime;
        private ISurfaceTextureRuntime TextureRuntime =>
            textureRuntime ?? (textureRuntime = new UnitySurfaceTextureRuntime());

        // src: f54394, exact atlas multipliers and mode.
        public void Space(float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, Color tint)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54394);
#endif
            var uv = new Rect(OriginalMath.Binary32(sx * 0.0009765625f), OriginalMath.Binary32(1f - OriginalMath.Binary32((OriginalMath.Binary32(sy + sh)) * 0.00048828125f)),
                OriginalMath.Binary32(sw * 0.0009765625f), OriginalMath.Binary32(sh * 0.00048828125f));
            Quad(x, y, w, h, uv, tint, 0f, 13f, true, 0f);
        }

        // src: f54396. First load is cached even when it is null or not alive.
        public void CodexThumb(int level, float x, float y, float w, float h, Color tint)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54396);
#endif
            if (!ResourcesRuntime.Alive(codex))
            {
                var loaded = ResourcesRuntime.LoadTexture("DeckArt/codex-levels");
                codex = loaded;
                if (!ResourcesRuntime.Alive(loaded)) return;
                ResourcesRuntime.SetTexture(ownedMaterial, "_CodexTex", codex);
            }
            level = level < 288 ? level : 288;
            level = level > 0 ? level : 0;
            int row = (level & 65535) / 17;
            float uvY = OriginalMath.Binary32(1f - OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(row * 60f) + 60f)) * 0.0009765625f));
            float uvX = OriginalMath.Binary32(OriginalMath.Binary32(((level - row * 17) & 65535) * 60f) * 0.0009765625f);
            var uv = new Rect(uvX, uvY, Float(0x3D700000u), Float(0x3D700000u));
            Quad(x, y, w, h, uv, tint, 0f, 15f, true, 0f);
        }

        // src: f54399. The upper-bound check is i32.gt_u, so negatives return too.
        public void DeckArt(int id, float x, float y, float w, float h, Color tint, bool fit)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54399);
#endif
            if (unchecked((uint)id) > 19u) return;
            int bank;
            bool sheet = false;
            switch (id)
            {
                case 0: bank = 1; break;
                case 17: bank = 2; break;
                case 18: bank = 3; break;
                case 19: bank = 4; break;
                default: bank = 0; sheet = true; break;
            }
            if (!ResourcesRuntime.Alive(deckTextures[bank])) return;
            float aspect = 1f;
            if (!sheet)
            {
                float width = TextureRuntime.Width(deckTextures[bank]);
                float height = TextureRuntime.Height(deckTextures[bank]);
                aspect = OriginalMath.Binary32(width / height);
            }
            float fittedW = w, fittedH = h;
            if (fit)
            {
                float product = OriginalMath.Binary32(aspect * h);
                fittedW = w < product ? w : product;
                x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(w - fittedW)) * 0.5f) + x);
                fittedH = OriginalMath.Binary32(fittedW / aspect);
                y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(h - fittedH)) * 0.5f) + y);
            }
            float uvSize = 1f, uvX = 0f, uvY = 0f;
            if (sheet)
            {
                int cell = id - 1;
                int row = unchecked((sbyte)cell) / 4;
                uvX = OriginalMath.Binary32(unchecked((sbyte)(cell - (row << 2))) * 0.25f);
                uvY = OriginalMath.Binary32(1f - OriginalMath.Binary32(unchecked((sbyte)(row + 1)) * 0.25f));
                uvSize = 0.25f;
            }
            Quad(x, y, fittedW, fittedH, new Rect(uvX, uvY, uvSize, uvSize), tint,
                0f, bank | 8, true, 0f);
        }

        // src: f54400/f54401; identical instructions apart from mode 9/14.
        public void FleetPiece(int piece, float cx, float cy, float scale, Color tint) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54400);
#endif
            DrawFleetPiece(piece, cx, cy, scale, tint, 9f);
        }
        public void FleetSilhouette(int piece, float cx, float cy, float scale, Color color) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54401);
#endif
            DrawFleetPiece(piece, cx, cy, scale, color, 14f);
        }

        private void DrawFleetPiece(int piece, float cx, float cy, float scale, Color color, float mode)
        {
            bool alive = ResourcesRuntime.Alive(deckTextures[1]);
            if (piece < 0 || !alive || piece >= FleetArt.Rect.Length) return;
            RectInt rect = FleetArt.Rect[piece];
            float width = rect.width, height = rect.height;
            var uv = new Rect(OriginalMath.Binary32(rect.x * 0.00048828125f),
                OriginalMath.Binary32(1f - OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(rect.height + rect.y)) / 1740f)),
                OriginalMath.Binary32(width * 0.00048828125f), OriginalMath.Binary32(height / 1740f));
            width = OriginalMath.Binary32(width * scale);
            height = OriginalMath.Binary32(height * scale);
            Quad(OriginalMath.Binary32(cx - OriginalMath.Binary32(width * 0.5f)), OriginalMath.Binary32(cy - OriginalMath.Binary32(height * 0.5f)), width, height, uv, color,
                0f, mode, true, 0f);
        }

        // src: f54402. UV arrays are the original hash-named metadata fields
        // 4032292A... (slot 5816592) and E3C52646... (slot 5816788).
        public void DeckPlate(float x, float y, float w, float h, Color tint)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54402);
#endif
            if (!ResourcesRuntime.Alive(deckTextures[4])) return;
            float right = OriginalMath.Binary32(x + w);
            float side = OriginalMath.WasmMin(OriginalMath.Binary32(w * Float(0x3E75C28Fu)), 20f);
            var xs = new[] { x, OriginalMath.Binary32(side + x), OriginalMath.Binary32(right - side), right };
            float bottom = OriginalMath.Binary32(y + h);
            float edge = OriginalMath.WasmMin(OriginalMath.Binary32(h * Float(0x3EA3D70Au)), 14f);
            var ys = new[] { y, OriginalMath.Binary32(edge + y), OriginalMath.Binary32(bottom - edge), bottom };
            var us = new[] { 0f, Float(0x3DE147AEu), Float(0x3F63D70Au), 1f };
            var vs = new[] { 1f, 0.75f, 0.25f, 0f };
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    var uv = new Rect(us[col], vs[row + 1], OriginalMath.Binary32(us[col + 1] - us[col]), OriginalMath.Binary32(vs[row] - vs[row + 1]));
                    Quad(xs[col], ys[row], OriginalMath.Binary32(xs[col + 1] - xs[col]), OriginalMath.Binary32(ys[row + 1] - ys[row]),
                        uv, tint, 0f, 12f, false, 0f);
                }
        }

        // src: f54403. Only positive story kinds perform the alive/aspect query.
        public void Art(int kind, float x, float y, float w, float h, int frame)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54403);
#endif
            float fittedW = w, fittedH = h;
            if (kind > 0 && ResourcesRuntime.Alive(story[kind]))
            {
                float width = TextureRuntime.Width(story[kind]);
                float height = TextureRuntime.Height(story[kind]);
                float aspect = OriginalMath.Binary32(width / height);
                float product = OriginalMath.Binary32(aspect * h);
                fittedW = w < product ? w : product;
                x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(w - fittedW)) * 0.5f) + x);
                fittedH = OriginalMath.Binary32(fittedW / aspect);
                y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(h - fittedH)) * 0.5f) + y);
            }
            Quad(x, y, fittedW, fittedH, new Rect(0f, 0f, 1f, 1f), Color.white,
                0f, unchecked(kind + 3), true, frame);
        }

        // src: f54404; kind is unused in the source body.
        public void Environment(int kind, float x, float y, float w, float h) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54404);
#endif
            Quad(x, y, w, h, new Rect(0f, 0f, 1f, 1f), Color.white, 0f, 7f, false, 0f);
        }
    }
}
