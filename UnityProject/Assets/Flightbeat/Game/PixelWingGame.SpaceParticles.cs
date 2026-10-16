using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54805: seven original drifting atlas objects, after the common stars.
        private void DrawSpaceZoneDebris(PixelSurface s, int zone, float left, float width, float height)
        {
            for (int i = 0; i < 7; i++)
            {
                int band = i % 3;
                float index = OriginalMath.Binary32((float)i), size = OriginalMath.Binary32((float)(band * 16 + 26)); if ((i & 1) != 0) size  = OriginalMath.Binary32(size*(.8f));
                float y = OriginalMath.Binary32(SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 197.6999969482422f) + OriginalMath.Binary32(clock * OriginalMath.Binary32((float)(band * 10 + 8)))), OriginalMath.Binary32(height + (OriginalMath.Binary32(size + size)))) - size);
                float half = OriginalMath.Binary32(size * .5f);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(left + SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 151.3000030517578f) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .05f) + index)) * 20f)), OriginalMath.Binary32(width + size))) - half);
                float brightness = band == 0 ? .45f : band == 1 ? .7f : .9f;
                Color tint = new Color(brightness, brightness, brightness, 1f);
                if (zone == 2)
                    s.Glow(x, y, OriginalMath.Binary32(size * 1.6f), OriginalMath.Binary32(size * 1.6f), SourceColor(4525216907506221056UL, 1036831949u, OriginalMath.Binary32(brightness * .18f)), 0f);
                if (zone < 3)
                    s.Space(OriginalMath.Binary32((float)(((i & 3) | (zone == 1 ? 0 : 4)) << 6)), 1984f, 64f, 64f, OriginalMath.Binary32(x - half), OriginalMath.Binary32(y - half), size, size, tint);
                else
                {
                    if (zone == 3)
                        s.Glow(x, y, OriginalMath.Binary32(size * 1.4f), OriginalMath.Binary32(size * 1.4f), SourceColor(4575657222462033101UL, 1056964608u, OriginalMath.Binary32(brightness * .12f)), 0f);
                    float offset = OriginalMath.Binary32(size * .4f), spriteSize = OriginalMath.Binary32(size * .8f);
                    s.Space(OriginalMath.Binary32((float)((((zone == 3 ? 0 : 4) | (i & 3)) << 6) | 512)), 1088f, 64f, 64f,
                        OriginalMath.Binary32(x - offset), OriginalMath.Binary32(y - offset), spriteSize, spriteSize, tint);
                }
            }
        }

        // f54805: zone-specific snow, embers, spores and horizontal/vertical data.
        private void DrawSpaceZoneParticles(PixelSurface s, int zone, float left, float width, float height)
        {
            if (zone == 1)
            {
                for (int i = 0; i < 36; i++)
                {
                    float index = OriginalMath.Binary32((float)i), movement = OriginalMath.Binary32(clock * OriginalMath.Binary32((float)((i & 3) * 9 + 18)));
                    float y = OriginalMath.Binary32(SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 61.70000076293945f) + movement), OriginalMath.Binary32(height + 20f)) + -10f);
                    float x = OriginalMath.Binary32(left + SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 97.30000305175781f) + OriginalMath.Binary32(movement * -.35f)), width)), size = i % 5 == 0 ? 2f : 1f;
                    s.Rect(OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), size, size,
                        SourceColor(4572054341910370714UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock + clock) + index)) * .25f) + .45f)));
                }
            }
            else if (zone == 2)
            {
                for (int i = 0; i < 30; i++)
                {
                    float index = OriginalMath.Binary32((float)i), phase = SpacePhase(OriginalMath.Binary32(OriginalMath.Binary32(index * .137f) + OriginalMath.Binary32(OriginalMath.Binary32(clock * OriginalMath.Binary32((float)(i % 5 * 7 + 14))) / height)));
                    float remaining = OriginalMath.Binary32(1f - phase), y = OriginalMath.Binary32(height * remaining);
                    float blend = OriginalMath.WasmMax(phase, 0f);
                    float x = OriginalMath.Binary32(left + SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 89.0999984741211f) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .8f) + index)) * 14f)), width));
                    s.Rect(OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), 2f, 2f,
                        new Color(OriginalMath.Binary32(OriginalMath.Binary32(blend * 0f) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.6f) + .85f), OriginalMath.Binary32(OriginalMath.Binary32(blend * -.26999998092651367f) + .35f), OriginalMath.Binary32(remaining * .7f)));
                    if (i % 6 == 0) s.Glow(x, y, 10f, 10f, SourceColor(4539628425454813184UL, 1041865114u, OriginalMath.Binary32(remaining * .35f)), 0f);
                }
            }
            else if (zone == 3)
            {
                for (int i = 0; i < 28; i++)
                {
                    float index = OriginalMath.Binary32((float)i), phase = SpacePhase(OriginalMath.Binary32(OriginalMath.Binary32(index * .173f) + OriginalMath.Binary32(OriginalMath.Binary32(clock * OriginalMath.Binary32((float)((i & 3) * 6 + 10))) / height)));
                    float y = OriginalMath.Binary32(height * (OriginalMath.Binary32(1f - phase))), pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 3f) + index)) * .5f) + .5f);
                    float x = OriginalMath.Binary32(left + SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(index * 71.30000305175781f) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(index * 1.3f) + OriginalMath.Binary32(clock * .7f))) * 22f)), width));
                    s.Rect(OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), 2f, 2f,
                        SourceColor(4575657222467066266UL, 1056964608u, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * .35f) + .35f)) * (OriginalMath.Binary32(OriginalMath.Binary32(phase * -.6f) + 1f)))));
                    if (i % 5 == 0) s.Glow(x, y, 12f, 12f, SourceColor(4575657222465388544UL, 1056964608u, OriginalMath.Binary32(pulse * .3f)), 0f);
                }
            }
            else if (zone == 4)
            {
                for (int i = 0; i < 24; i++)
                {
                    bool vertical = (i & 1) != 0;
                    float index = OriginalMath.Binary32((float)i), phase = SpacePhase(OriginalMath.Binary32(OriginalMath.Binary32(index * .311f) + OriginalMath.Binary32(OriginalMath.Binary32(clock * OriginalMath.Binary32((float)(i % 5 * 25 + 60))) / (vertical ? height : width))));
                    float x = OriginalMath.Binary32(left + (vertical ? SpaceRepeat(OriginalMath.Binary32(index * 97.0999984741211f), width) : OriginalMath.Binary32(width * phase)));
                    float y = vertical ? OriginalMath.Binary32(height * phase) : SpaceRepeat(OriginalMath.Binary32(index * 131.6999969482422f), height);
                    Color color = i % 3 == 0 ? new Color(1f, .45f, .85f, .6f) : new Color(.45f, .9f, 1f, .6f);
                    s.Rect(OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), vertical ? 2f : 6f, vertical ? 6f : 2f, color);
                }
            }
        }
    }
}
