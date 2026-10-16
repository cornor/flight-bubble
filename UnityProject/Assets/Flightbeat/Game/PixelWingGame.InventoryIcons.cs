using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54626, including the inlined ModuleGlyph branch.
        private void ModuleIcon(PixelSurface s, FleetCore core, float x, float y, float size)
        {
            Color tint = FleetCatalog.QualityColors[core.quality];
            s.Rect(x, y, size, size, new Color(OriginalMath.Binary32(tint.r * .25f), OriginalMath.Binary32(tint.g * .25f), OriginalMath.Binary32(tint.b * .25f), 1f));
            s.Border(x, y, size, size, tint, 2f);
            if (core.quality >= 3)
            {
                float pulse = OriginalMath.Sin(OriginalMath.Binary32(ui * 4f));
                s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(size * .5f) + x), OriginalMath.Binary32(OriginalMath.Binary32(size * .5f) + y), OriginalMath.Binary32(size * 1.4f), OriginalMath.Binary32(size * 1.4f), A(tint, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .25f)), 0f);
            }
            ModuleGlyph(s, core.kind, OriginalMath.Binary32(x + 3f), OriginalMath.Binary32(y + 3f), OriginalMath.Binary32(size - 6f), Color.white);
            for (int i = 0; i <= core.quality; i++) s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 4f) + OriginalMath.Binary32((float)(i * 6))), OriginalMath.Binary32(OriginalMath.Binary32(y + size) - 7f), 4f, 4f, tint);
        }
        // f54626 reads ModuleSpec.bankIcon (+44), not icon (+16); Sprite retains the unclamped kind.
        private void ModuleGlyph(PixelSurface s, int kind, float x, float y, float size, Color tint)
        {
            int icon = FleetCatalog.Modules[kind < 0 ? 0 : kind >= FleetCatalog.Modules.Length ? FleetCatalog.Modules.Length - 1 : kind].bankIcon;
            if (icon >= 0) s.Hull(icon, x, y, size, size, tint, 0f);
            else s.Sprite(kind + 16, x, y, size, size, tint, 0f);
        }
        // f54622. Bounds use the source assembled offsets and half atlas sizes.
        private void DrawPartThumb(PixelSurface s, int fighter, int slot, float cx, float cy, float box, float alpha, bool ghost)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = -float.MaxValue, maxY = -float.MaxValue;
            int first = FleetArt.First[fighter], end = first + FleetArt.Count[fighter];
            for (int i = first; i < end; i++)
            {
                if (FleetArt.Slot[i] != slot) continue;
                Vector2 p = FleetArt.Assembled[i]; RectInt rect = FleetArt.Rect[i];
                float half = OriginalMath.Binary32(OriginalMath.Binary32((float)rect.height) * .5f);
                float upper = OriginalMath.Binary32(p.y + half), lower = OriginalMath.Binary32(p.y - half);
                maxY = maxY > upper ? maxY : upper; minY = lower > minY ? minY : lower;
                half = OriginalMath.Binary32(OriginalMath.Binary32((float)rect.width) * .5f);
                upper = OriginalMath.Binary32(p.x + half); lower = OriginalMath.Binary32(p.x - half);
                maxX = maxX > upper ? maxX : upper; minX = lower > minX ? minX : lower;
            }
            if (maxX < minX) return;
            float width = OriginalMath.Binary32(maxX - minX), height = OriginalMath.Binary32(maxY - minY);
            float scale = OriginalMath.Binary32(box / (width > height ? width : height)), centerY = OriginalMath.Binary32((OriginalMath.Binary32(maxY + minY)) * .5f), centerX = OriginalMath.Binary32((OriginalMath.Binary32(maxX + minX)) * .5f);
            Color color = new Color(ghost ? .45f : 1f, 1f, ghost ? .95f : 1f, ghost ? OriginalMath.Binary32(alpha * .3f) : alpha);
            for (int i = first; i < end; i++)
                if (FleetArt.Slot[i] == slot)
                    s.FleetPiece(i, OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(FleetArt.Assembled[i].x - centerX))) + cx), OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(FleetArt.Assembled[i].y - centerY))) + cy), scale, color);
        }
    }
}
