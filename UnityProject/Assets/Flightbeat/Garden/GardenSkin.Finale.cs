#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    public static partial class GardenSkin
    {
        // The registered frames share their lower 37% (leaves and pot). Pin that
        // part to the ready frame; only the plush body above it inhales/recoils.
        public const float FinaleKingRoot = .63f;
        const int KingBodyBands = 24;

        // Frame coordinates are normalized, y down. The same deformation places
        // the mouth, so the existing rainbow fan stays attached while inflating.
        public static Vector2 FinaleKingPoint(Vector2 point, Vector2 center, float size, Vector2 body, float sway)
        {
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((FinaleKingRoot - point.y) / .16f));
            return new Vector2(center.x + (point.x - .5f) * size * Mathf.Lerp(1f, body.x, weight) + sway * weight,
                center.y + (FinaleKingRoot - .5f) * size + (point.y - FinaleKingRoot) * size * (point.y < FinaleKingRoot ? body.y : 1f));
        }

        public static bool FinaleKing(PixelSurface s, string bodyArt, string baseArt, Vector2 center, float size, Vector2 body, float sway, Color tint)
        {
            var data = Data;
            if (!slots.TryGetValue(bodyArt, out var top) || !slots.TryGetValue(baseArt, out var bottom)) return false;
            // Free-corner quads use screen coordinates. Map the shared anchor once,
            // rather than each strip, to keep the fur seamless on tall screens.
            if (s.MapLayout) center.y = Flightbeat.Game.PlushLayout.Y(center.y);
            void Band(Slot slot, float from, float to)
            {
                float fw = slot.fw, fh = slot.fh;
                float a = Mathf.Max(from, slot.oy / fh), b = Mathf.Min(to, (slot.oy + slot.h) / fh);
                if (b <= a) return;
                float left = slot.ox / fw, right = (slot.ox + slot.w) / fw;
                Rect uv = Uv(slot, data.size);
                uv = new Rect(uv.x, uv.yMax - (b * fh - slot.oy) / slot.h * uv.height,
                    uv.width, (b - a) * fh / slot.h * uv.height);
                Vector2 P(float x, float y) => FinaleKingPoint(new Vector2(x, y), center, size, body, sway);
                s.GardenQuadCorners(P(left, a), P(right, a), P(right, b), P(left, b), uv, tint, slot.atlas);
            }
            for (int i = 0; i < KingBodyBands; i++)
                Band(top, FinaleKingRoot * i / KingBodyBands, FinaleKingRoot * (i + 1) / KingBodyBands);
            Band(bottom, FinaleKingRoot, 1f);
            return true;
        }
    }
}
#endif
