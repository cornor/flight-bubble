using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53898. Model-specific nozzles, layered flame, upgrade exhaust and rings.
        private void DrawEngines(PixelSurface s, int color, int tier, Vector2 at, float size, Color tint)
        {
            int model = Model;
            float flutter = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 40f) + OriginalMath.Binary32((float)unchecked(color * 3))));
            int engine = Look == null ? 1 : Look.Part(4);
            engine = engine < 5 ? engine : 5;
            engine = engine > 1 ? engine : 1;
            float boost = Look == null ? 0f : OriginalMath.Binary32(OriginalMath.Binary32((float)(engine - 1)) * .42f);
            Color glow = FlameGlow[model], core = FlameCore[model];
            float breadth = Look == null ? 1f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(engine - 1)) * .16f) + 1f);
            int rank = tier < 3 ? tier : 3;
            rank = rank > 0 ? rank : 0;
            float[] nozzles = Nozzles[model][rank];
            if (nozzles.Length == 0) return;
            float lengthFactor = OriginalMath.Binary32(OriginalMath.Binary32((model == 3 ? 1.3f : 1f) * boost) + 1f);
            float trailFactor = model == 1 ? 2.6f : model == 3 ? 2.2f : 1.8f;
            float y = OriginalMath.Binary32(OriginalMath.Binary32((model == 3 ? (tier > 1 ? .44f : .42f) : .42f) * size) + at.y);
            float glowHeight = OriginalMath.Binary32(size * .09f), seed = OriginalMath.Binary32(OriginalMath.Binary32((float)color) * .3f);
            float widthFactor = OriginalMath.Binary32((model == 2 ? 1.5f : model == 1 ? .7f : model == 4 ? .75f : 1f) * breadth);
            float glowWidth = OriginalMath.Binary32(OriginalMath.Binary32(size * .14f) * widthFactor);
            float flameWidth = OriginalMath.Binary32(OriginalMath.Binary32(size * .26f) * widthFactor), trailWidth = OriginalMath.Binary32(OriginalMath.Binary32(size * .16f) * widthFactor);
            float innerWidth = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(size * .05f) * widthFactor), 1f);
            float outerWidth = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(size * .1f) * widthFactor), 2f);
            float mainLength = OriginalMath.Binary32(OriginalMath.Binary32((tier > 2 ? .55f : .42f) * size) * (OriginalMath.Binary32(OriginalMath.Binary32(flutter * .25f) + .75f)));
            float sideLength = OriginalMath.Binary32(mainLength * .7f);
            foreach (float nozzle in nozzles)
            {
                float x = OriginalMath.Binary32(OriginalMath.Binary32(nozzle * size) + at.x);
                float length = OriginalMath.Binary32(lengthFactor * (Math.Abs(nozzle) > .15f ? sideLength : mainLength));
                float alpha = tint.a;
                if (Look != null && Look.stage >= 2)
                {
                    float trail = OriginalMath.Binary32(length * trailFactor);
                    s.GlowLine(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(trail * 0f)), OriginalMath.Binary32(y + OriginalMath.Binary32(trail * 1f))), trailWidth,
                        Alpha(FleetCatalog.FighterAccent[Model], OriginalMath.Binary32(alpha * .14f)));
                    if (model == 1)
                    {
                        for (int n = 0; n < 2; n++)
                        {
                            float t = Fraction(OriginalMath.Binary32(OriginalMath.Binary32(nozzle + OriginalMath.Binary32(time * 3f)) + (n == 0 ? 0f : .5f)));
                            s.Rect(OriginalMath.Binary32(x + -1f), OriginalMath.Binary32(y + OriginalMath.Binary32(trail * t)), 2f, 6f, new Color(.85f, .95f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t))) * .8f)));
                        }
                    }
                    else if (model == 2)
                    {
                        float t = Fraction(OriginalMath.Binary32(time * 1.6f));
                        s.Ring(new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * trail) * 0f) * .7f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * trail) * 1f) * .7f))),
                            OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(t * .12f) + .08f)) * size), new Color(.8f, .55f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t))) * .7f)), 1.5f, 14, 0f, 360f);
                    }
                    else if (model == 3)
                    {
                        float[] offsets = { 0f, .33f, .66f };
                        for (int n = 0; n < 3; n++)
                        {
                            float t = Fraction(OriginalMath.Binary32(OriginalMath.Binary32(nozzle + OriginalMath.Binary32(time * 2.2f)) + offsets[n])), remaining = OriginalMath.Binary32(1f - t);
                            s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + OriginalMath.Binary32((float)(n * 2)))) * size) * .06f)) + -1.5f),
                                OriginalMath.Binary32(y + OriginalMath.Binary32(trail * t)), 3f, 3f, new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32(remaining * .3f) + .6f), .15f, OriginalMath.Binary32(alpha * remaining)));
                        }
                    }
                    else if (model == 4)
                    {
                        float t = Fraction(OriginalMath.Binary32(nozzle + OriginalMath.Binary32(time * 1.2f)));
                        s.Rect(OriginalMath.Binary32(x + -2f), OriginalMath.Binary32(y + OriginalMath.Binary32(trail * t)), 4f, 4f, new Color(.5f, 1f, .7f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t))) * .8f)));
                    }
                    else
                    {
                        float t = Fraction(OriginalMath.Binary32((OriginalMath.Binary32(nozzle + time)) + time));
                        s.Glow(x, OriginalMath.Binary32(y + OriginalMath.Binary32(trail * t)), 8f, 8f, new Color(.6f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - t))) * .8f)), 0f);
                    }
                }
                s.GlowLine(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(length * 0f)), OriginalMath.Binary32(y + OriginalMath.Binary32(length * 1f))), flameWidth, Alpha(glow, OriginalMath.Binary32(alpha * .6f)));
                s.Segment(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(length * 0f) * .7f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(length * 1f) * .7f))), outerWidth, Alpha(core, alpha));
                s.Segment(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(length * 0f) * .35f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(length * 1f) * .35f))), innerWidth, new Color(1f, 1f, 1f, alpha));
                if (Look == null) continue;
                if (Look.Part(4) >= 3)
                {
                    for (int n = 0; n < engine - 2; n++)
                    {
                        float t = OriginalMath.Binary32(OriginalMath.Binary32((float)(n + 1)) / OriginalMath.Binary32((float)(engine - 1)));
                        s.Glow(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * length) * 0f) * .8f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t * length) * 1f) * .8f)),
                            glowWidth, glowHeight, new Color(1f, 1f, .9f, OriginalMath.Binary32(alpha * .45f)), 0f);
                    }
                }
                if (Look.Part(4) < 4) continue;
                // f53898 WAT 1699..1706: seed + (nozzle + time * 3.2), with both f32.add boundaries.
                float fraction = Fraction(OriginalMath.Binary32(seed + OriginalMath.Binary32(nozzle + OriginalMath.Binary32(time * 3.2f)))), position = OriginalMath.Binary32(OriginalMath.Binary32(fraction * .6f) + .25f);
                // f53898 WAT: widthFactor * (((fraction * .06) + .06) * size).
                // The decompiler flattens the two final f32.mul operations.
                s.Ring(new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(position * length) * 0f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(position * length) * 1f))),
                    OriginalMath.Binary32(widthFactor * OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(fraction * .06f) + .06f) * size)),
                    new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * (OriginalMath.Binary32(1f - fraction))) * (Look.Part(4) > 4 ? .7f : .5f))), 1f, 10, 0f, 360f);
            }
        }

        // f53899. Raw model >=4 admits guardian rings; Model itself is clamped.
        private void DrawGuardian(PixelSurface s, Vector2 at, float size, Color tint)
        {
            if (Look == null || Look.model < 4) return;
            float alpha = OriginalMath.Binary32(tint.a * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 2.4f)) * .5f) + .5f)) * .15f) + .25f)));
            for (int n = 0; n < 6; n++)
                s.Ring(at, OriginalMath.Binary32(size * .62f), new Color(.4f, 1f, .65f, alpha), 1.5f, 3, OriginalMath.Binary32(OriginalMath.Binary32(time * 40f) + OriginalMath.Binary32((float)(n * 60))), 28f);
        }

        // f53901. The core follows the rotated offset; the two arcs use world axes.
        private void DrawPartCore(PixelSurface s, Vector2 at, float size, float angle, Color tint, Color lane)
        {
            if (Look == null) return;
            int level = Look.Part(3);
            level = level < 5 ? level : 5;
            if (level < 2) return;
            float offset = OriginalMath.Binary32(size * .06f), radians = OriginalMath.Binary32(angle * .01745329238474369f);
            float cos = OriginalMath.Cos(radians), sin = OriginalMath.Sin(radians);
            float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cos * 0f) - OriginalMath.Binary32(offset * sin)) + at.x), y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sin * 0f) + OriginalMath.Binary32(offset * cos)) + at.y);
            float width = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)level) * .012f) + .035f)) * size);
            s.Rect(OriginalMath.Binary32(x - OriginalMath.Binary32(width * .5f)), OriginalMath.Binary32(y - OriginalMath.Binary32(width * .5f)), width, width,
                new Color(1f, 1f, 1f, OriginalMath.Binary32(OriginalMath.Binary32(tint.a * .9f) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 6f)) * .25f) + .75f)))));
            if (level < 3) return;
            for (int n = 0; n < 2; n++)
                s.Ring(new Vector2(x, y), OriginalMath.Binary32(width * 2.2f), new Color(1f, 1f, 1f, OriginalMath.Binary32(tint.a * .55f)),
                    1f, 2, OriginalMath.Binary32(OriginalMath.Binary32(time * 220f) + OriginalMath.Binary32((float)(n * 180))), 60f);
        }
    }
}
