using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53885 folded hexagon vertices retain the source's small nonzero Y values.
        private static readonly Vector2[] markHexDirections = {
            new Vector2(.4999999701976776f, .866025447845459f),
            new Vector2(-.5000000596046448f, .8660253882408142f),
            new Vector2(-1f, -8.742277657347586e-08f),
            new Vector2(-.49999991059303284f, -.866025447845459f),
            new Vector2(.5000003576278687f, -.8660252094268799f),
            new Vector2(1f, 1.7484555314695172e-07f)
        };
        // f53885. Model-specific hit/fusion marks, followed by the common fusion glow.
        private void DrawMarks(PixelSurface s)
        {
            int model = Model;
            Color accent = FleetCatalog.FighterAccent[model];
            foreach (Mark mark in marks)
            {
                float size = mark.fusion ? 70f : 14f, t = OriginalMath.Binary32(mark.age / (mark.fusion ? .7f : .3f));
                size = Look != null && Look.stage > 2 ? OriginalMath.Binary32(size * (OriginalMath.Binary32(t + .4f))) : OriginalMath.Binary32(OriginalMath.Binary32(size * (OriginalMath.Binary32(t + .4f))) * .72f);
                float alpha = OriginalMath.Binary32(1f - t);
                switch (model)
                {
                    case 1:
                        float y = OriginalMath.Binary32(OriginalMath.Binary32(size * .5735764503479004f) * 1.4f);
                        float x = OriginalMath.Binary32(OriginalMath.Binary32(size * .8191520571708679f) * 1.4f);
                        Color slash = SourceColor(4572054341910370714UL, 1065353216u, alpha);
                        float width = mark.fusion ? 3f : 1.5f;
                        s.Segment(new Vector2(OriginalMath.Binary32(mark.at.x - x), OriginalMath.Binary32(mark.at.y - y)), new Vector2(OriginalMath.Binary32(x + mark.at.x), OriginalMath.Binary32(y + mark.at.y)), width, slash);
                        x = OriginalMath.Binary32(OriginalMath.Binary32(size * -.8191520571708679f) * 1.4f);
                        s.Segment(new Vector2(OriginalMath.Binary32(mark.at.x - x), OriginalMath.Binary32(mark.at.y - y)), new Vector2(OriginalMath.Binary32(x + mark.at.x), OriginalMath.Binary32(y + mark.at.y)), width, slash);
                        break;
                    case 2:
                        s.Ring(mark.at, size, Alpha(accent, alpha), mark.fusion ? 4f : 2f, mark.fusion ? 32 : 14, 0f, 360f);
                        if (mark.fusion) s.Ring(mark.at, OriginalMath.Binary32(size * .6f), new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .7f)), 2f, 24, 0f, 360f);
                        break;
                    case 3:
                        for (int i = 0; i < (mark.fusion ? 12 : 5); i++)
                        {
                            float theta = OriginalMath.Binary32(mark.at.x + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 2.4f));
                            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Cos(theta)) + mark.at.x) + -1.5f),
                                OriginalMath.Binary32(OriginalMath.Binary32(t * 8f) + (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(size * OriginalMath.Sin(theta)) + mark.at.y) + -1.5f))), 3f, 3f,
                                new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .35f) + .55f), BitConverter.Int32BitsToSingle(1036831949), alpha));
                        }
                        break;
                    case 4:
                        Vector2 previous = new Vector2(OriginalMath.Binary32(size + mark.at.x), OriginalMath.Binary32(mark.at.y + 0f));
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 v = markHexDirections[i];
                            Vector2 next = new Vector2(i == 2 ? OriginalMath.Binary32(mark.at.x - size) : i == 5 ? OriginalMath.Binary32(size + mark.at.x) : OriginalMath.Binary32(OriginalMath.Binary32(size * v.x) + mark.at.x),
                                OriginalMath.Binary32(OriginalMath.Binary32(size * v.y) + mark.at.y));
                            s.Segment(previous, next, mark.fusion ? 3f : 1.5f, SourceColor(4575657222465388544UL, 1060320051u, alpha));
                            previous = next;
                        }
                        break;
                    default:
                        Color lane = palette[mark.color];
                        s.SpriteAt(9, mark.at, OriginalMath.Binary32(size * 1.6f), Alpha(Mix(lane, Color.white, .6f), alpha), 0f);
                        break;
                }
                if (mark.fusion) s.Glow(mark.at.x, mark.at.y, OriginalMath.Binary32(size * 2.4f), OriginalMath.Binary32(size * 2.4f), Alpha(accent, OriginalMath.Binary32(alpha * .35f)), 0f);
            }
        }
    }
}
