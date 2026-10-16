using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54805: original data segment +3648120 (three i32 pairs), plus fallback.
        private static readonly Vector2[] spaceZoneTiles = { new Vector2(0f, 1024f), new Vector2(0f, 1504f), new Vector2(512f, 0f), new Vector2(512f, 480f) };
        private static readonly float[] spaceAuroraPhases = { 0f, .9f, 1.8f, 2.6999998092651367f, 3.6f, 4.5f, 5.399999618530273f, 6.299999713897705f };
        private static readonly float[] spaceCanopyX = { .12f, .38f, .64f, .9f };
        private static readonly float[] spaceCanopyPhases = { 0f, 1.7f, 3.4f, 5.100000381469727f };
        private static float SpaceRepeat(float value, float length)
        {
            value  =             OriginalMath.Binary32(value-(OriginalMath.Binary32(length * OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(value / length))))));
            return value < 0f ? 0f : value > length ? length : value;
        }
        private static float SpacePhase(float value)
        {
            value  =             OriginalMath.Binary32(value-(OriginalMath.Binary32((float)Math.Floor(value))));
            return value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
        }

        // f54805 before the shared moon layer: aurora, corona, canopy or scan bands.
        private void DrawSpaceZoneAura(PixelSurface s, int zone, float left, float width, float height)
        {
            if (zone == 1)
            {
                for (int band = 0; band < 3; band++)
                {
                    float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .3f) + OriginalMath.Binary32((float)band))) * 14f) + OriginalMath.Binary32((float)(band * 38 + 40)));
                    Color color = band == 1 ? new Color(.4f, 1f, .75f, .1f) : new Color(.35f, .75f, 1f, .09f);
                    for (int i = 0; i < 8; i++)
                    {
                        float x = OriginalMath.Binary32(left + OriginalMath.Binary32((i == 2 ? OriginalMath.Binary32(width + width) : OriginalMath.Binary32(width * OriginalMath.Binary32((float)i))) / 7f));
                        s.Glow(x, OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(clock * .6f) + spaceAuroraPhases[i]) + OriginalMath.Binary32((float)band))) * 18f)),
                            OriginalMath.Binary32(width * .34f), 70f, color, 0f);
                    }
                }
            }
            else if (zone == 2)
            {
                float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * .7f)) * .5f) + .5f), center = OriginalMath.Binary32(left + OriginalMath.Binary32(width * .5f));
                s.Glow(center, -60f, OriginalMath.Binary32(width * 1.6f), 420f, SourceColor(4532422664333033472UL, 1039516303u, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .08f) + .3f)), 0f);
                s.Glow(center, -40f, OriginalMath.Binary32(width * .9f), 200f, SourceColor(4561245704525185024UL, 1053609165u, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .06f) + .22f)), 0f);
                for (int i = 0; i < 3; i++)
                {
                    float phase = SpacePhase(OriginalMath.Binary32(OriginalMath.Binary32(clock * .08f) + (i == 0 ? 0f : i == 1 ? .33f : .66f)));
                    s.Ring(new Vector2(OriginalMath.Binary32(left + OriginalMath.Binary32(width * (i == 0 ? .2f : i == 1 ? .5f : .8f))), -10f), OriginalMath.Binary32(OriginalMath.Binary32(phase * 140f) + 60f),
                        SourceColor(4543231306015703040UL, 1041865114u, OriginalMath.Binary32((OriginalMath.Binary32(1f - phase)) * .35f)), 3f, 6, OriginalMath.Binary32((float)(i * 20 + 20)), 110f);
                }
            }
            else if (zone == 3)
            {
                for (int i = 0; i < 4; i++)
                    s.Glow(OriginalMath.Binary32(left + OriginalMath.Binary32(width * spaceCanopyX[i])), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .4f) + OriginalMath.Binary32((float)i))) * 10f) + 20f),
                        OriginalMath.Binary32(width * .42f), 120f, SourceColor(4575657222460355379UL, 1055286886u,
                            OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .9f) + spaceCanopyPhases[i])) * .5f) + .5f)) * .05f) + .06f)), 0f);
            }
            else if (zone == 4)
            {
                float center = OriginalMath.Binary32(left + OriginalMath.Binary32(width * .5f)), span = OriginalMath.Binary32(height + 200f);
                for (int i = 0; i < 2; i++)
                {
                    float y = OriginalMath.Binary32(SpaceRepeat(OriginalMath.Binary32(OriginalMath.Binary32(clock * (i == 0 ? 38f : 59f)) + OriginalMath.Binary32((float)(i * 300))), span) + -100f);
                    s.Glow(center, y, OriginalMath.Binary32(width * 1.4f), 26f,
                        SourceColor(i == 0 ? 4568451461340253389UL : 4525216907506221056UL, 4435865490211995648UL), 0f);
                    s.Rect(left, y, width, 1f, SourceColor(i == 0 ? 4572054341904498688UL : 4539628425454813184UL, 4483423504166813696UL));
                }
            }
        }

        // f54805: source atlas regions and per-zone planet halos.
        private void DrawSpaceZonePlanet(PixelSurface s, int zone, float left, float width, float y)
        {
            float x = OriginalMath.Binary32(left + OriginalMath.Binary32(width * .02f));
            switch (zone)
            {
                case 0:
                    s.Space(0f, 768f, 256f, 256f, OriginalMath.Binary32(x + -30f), y, 150f, 150f, SourceColor(4543231306008153293UL, 4561245704519816315UL));
                    break;
                case 1:
                    s.Space(384f, 768f, 128f, 128f, OriginalMath.Binary32(x + -20f), y, 150f, 150f, SourceColor(4561245704520151859UL, 4564848585084397158UL));
                    break;
                case 2:
                    s.Glow(OriginalMath.Binary32(x + 55f), OriginalMath.Binary32(y + 75f), 260f, 260f, SourceColor(4518011146384441344UL, 4483423504138292429UL), 0f);
                    s.Space(256f, 896f, 128f, 128f, OriginalMath.Binary32(x + -20f), y, 150f, 150f, SourceColor(4554039943401727590UL, 4568451461345286554UL));
                    break;
                case 3:
                    s.Glow(OriginalMath.Binary32(x + 55f), OriginalMath.Binary32(y + 75f), 240f, 240f, SourceColor(4575657222458677658UL, 4464688526097435853UL), 0f);
                    s.Space(512f, 960f, 128f, 128f, OriginalMath.Binary32(x + -20f), y, 150f, 150f, SourceColor(4572054341910370714UL, 4568451461349480858UL));
                    break;
                default:
                    s.Glow(OriginalMath.Binary32(x + 55f), OriginalMath.Binary32(y + 75f), 240f, 240f, SourceColor(4525216907502865613UL, 4471894287230959616UL), 0f);
                    s.Space(640f, 960f, 128f, 128f, OriginalMath.Binary32(x + -40f), y, 190f, 190f, SourceColor(4564848585084397158UL, 4568451461351997440UL));
                    break;
            }
        }
    }
}
