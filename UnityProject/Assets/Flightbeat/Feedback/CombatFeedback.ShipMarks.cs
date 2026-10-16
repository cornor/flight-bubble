using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53900. WAT grouping retained for rotated starts/ends and nose extensions.
        private void DrawPartMarks(PixelSurface s, int tier, Vector2 at, float size, float angle, Color tint, Color lane)
        {
            int model = Model;
            float alpha = tint.a;
            Color bright = new Color(OriginalMath.Binary32(lane.r + OriginalMath.Binary32((OriginalMath.Binary32(1f - lane.r)) * .35f)),
                OriginalMath.Binary32(lane.g + OriginalMath.Binary32((OriginalMath.Binary32(1f - lane.g)) * .35f)), OriginalMath.Binary32(lane.b + OriginalMath.Binary32((OriginalMath.Binary32(1f - lane.b)) * .35f)), alpha);
            int raw = Look == null ? 0 : Look.Part(0), level = raw < 5 ? raw : 5;
            if (Look != null && level >= 2)
            {
                float radians = OriginalMath.Binary32(angle * .01745329238474369f);
                float cos = OriginalMath.Cos(radians), sin = OriginalMath.Sin(radians);
                float sinZero = OriginalMath.Binary32(sin * 0f), cosZero = OriginalMath.Binary32(cos * 0f);
                float length = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)level) * .05f) + .06f)) * size);
                float endY = OriginalMath.Binary32(sinZero - OriginalMath.Binary32(cos * length)), endX = OriginalMath.Binary32(cosZero + OriginalMath.Binary32(sin * length));
                float nose = OriginalMath.Binary32(NoseReach(tier) * -size), back = OriginalMath.Binary32(nose * .8f);
                float noseCos = OriginalMath.Binary32(cos * back), noseSin = OriginalMath.Binary32(sin * back);
                bool doubleMark = level > 3;
                float width = OriginalMath.WasmMax(OriginalMath.Binary32(size * .05f), 2f);
                for (int n = 0; n < (doubleMark ? 2 : 1); n++)
                {
                    float side = OriginalMath.Binary32((n != 0 ? size : -size) * .06f);
                    side = doubleMark ? side : 0f;
                    float startX = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cos * side) - noseSin) + at.x);
                    float startY = OriginalMath.Binary32(OriginalMath.Binary32(noseCos + OriginalMath.Binary32(sin * side)) + at.y);
                    float x = OriginalMath.Binary32(endX + startX), y = OriginalMath.Binary32(endY + startY);
                    s.Segment(new Vector2(startX, startY), new Vector2(x, y), width, new Color(.86f, .9f, .96f, alpha));
                    s.Rect(OriginalMath.Binary32(x + -1.5f), OriginalMath.Binary32(y + -1.5f), 3f, 3f, bright);
                }
                if (raw >= 5)
                {
                    float offset = OriginalMath.Binary32(nose + OriginalMath.Binary32(length * -.8f));
                    s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(cosZero - OriginalMath.Binary32(sin * offset)) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(sinZero + OriginalMath.Binary32(cos * offset)) + at.y),
                        OriginalMath.Binary32(size * .3f), OriginalMath.Binary32(size * .3f), Alpha(lane, OriginalMath.Binary32(alpha * .3f)), 0f);
                }
            }
            float tip = TipSpan[model], rotation = OriginalMath.Binary32(angle * .01745329238474369f);
            float sine = OriginalMath.Sin(rotation), cosine = OriginalMath.Cos(rotation);
            if (Look == null) return;
            // f53900 WAT 430..444: tierFactor * (tip * size), not (tierFactor * tip) * size.
            float span = OriginalMath.Binary32((tier > 2 ? 1.08f : tier == 2 ? 1.02f : .9f) * OriginalMath.Binary32(tip * size));
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                level = Look.Part(sideIndex + 1);
                level = level < 5 ? level : 5;
                if (level < 2) continue;
                float sign = sideIndex == 0 ? -1f : 1f;
                float extension = OriginalMath.Binary32(OriginalMath.Binary32(sign * size) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(level - 1)) * .045f) + .07f)));
                float shortExtension = OriginalMath.Binary32(extension * .7f);
                float length = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)level) * .03f) + .09f)) * size);
                float cosLength = OriginalMath.Binary32(cosine * length), sinLength = OriginalMath.Binary32(sine * length);
                for (int n = 0; n < (level == 2 ? 1 : 2); n++)
                {
                    float row = OriginalMath.Binary32(OriginalMath.Binary32((float)n) * size);
                    float localX = OriginalMath.Binary32(sign * (OriginalMath.Binary32(span + OriginalMath.Binary32(row * -.08f))));
                    float localY = OriginalMath.Binary32(OriginalMath.Binary32(row * .06f) + OriginalMath.Binary32(TipY[model] * size));
                    float startY = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sine * localX) + OriginalMath.Binary32(cosine * localY)) + at.y);
                    float startX = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cosine * localX) - OriginalMath.Binary32(sine * localY)) + at.x);
                    float amount = n != 0 ? shortExtension : extension;
                    float endX = OriginalMath.Binary32(startX + (OriginalMath.Binary32(OriginalMath.Binary32(cosine * amount) - sinLength)));
                    float endY = OriginalMath.Binary32(startY + (OriginalMath.Binary32(cosLength + OriginalMath.Binary32(sine * amount))));
                    s.Segment(new Vector2(startX, startY), new Vector2(endX, endY), 2f, new Color(.86f, .9f, .96f, alpha));
                    s.Rect(OriginalMath.Binary32(endX + -1f), OriginalMath.Binary32(endY + -1f), 2.5f, 2.5f, bright);
                }
                if (level >= 4)
                {
                    float localX = OriginalMath.Binary32(span * sign), localY = OriginalMath.Binary32(TipY[model] * size);
                    s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cosine * localX) - OriginalMath.Binary32(sine * localY)) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sine * localX) + OriginalMath.Binary32(cosine * localY)) + at.y),
                        OriginalMath.Binary32((float)(level + 8)), OriginalMath.Binary32((float)(level + 8)),
                        Alpha(lane, OriginalMath.Binary32((OriginalMath.Binary32(alpha * .45f)) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(time * 9f) + OriginalMath.Binary32((float)(sideIndex << 1))) + OriginalMath.Binary32((float)tier))) * .2f) + .8f)))), 0f);
                }
                if (tier <= 0 || level < 3) continue;
                float wingX = OriginalMath.Binary32(span * sign), wingY = OriginalMath.Binary32(TipY[model] * size);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(cosine * wingX) - OriginalMath.Binary32(sine * wingY)) + at.x), y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(sine * wingX) + OriginalMath.Binary32(cosine * wingY)) + at.y);
                float reach = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(level - 3)) * .06f) + .22f);
                s.Segment(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32(reach * 0f) * size)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(reach * 1f) * size))),
                    1.2f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .35f)));
            }
        }
    }
}
