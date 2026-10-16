using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54510. All hand geometry and colors come from the original draw body.
        private void DrawTapPointer(PixelSurface s, Vector2 target, Vector2 tip, float angle, float atTime)
        {
            float phase = OriginalMath.Binary32(OriginalMath.Remainder(atTime >= 0f ? atTime : clock, 1.1f) / 1.1f);
            float reach = phase < .32f ? InOut(OriginalMath.Binary32(phase / .32f))
                : phase < .46f ? 1f : OriginalMath.Binary32(1f - InOut(OriginalMath.Binary32((OriginalMath.Binary32(phase + -.46f)) / .54f)));
            bool pressed = phase < .46f & phase >= .3f;
            float scale = pressed ? 1.222f : 1.3f;
            Vector2 direction = CombatFeedback.Rotate(new Vector2(0f, -1f), angle);
            float retreat = OriginalMath.Binary32((OriginalMath.Binary32(1f - reach)) * 12f);
            Vector2 origin = new Vector2(OriginalMath.Binary32(tip.x - OriginalMath.Binary32(retreat * direction.x)), OriginalMath.Binary32(tip.y - OriginalMath.Binary32(retreat * direction.y)));
            if (phase >= .32f)
            {
                float ring = OriginalMath.Binary32((OriginalMath.Binary32(phase + -.32f)) / .68f);
                if (ring >= 0f)
                {
                    float fade = OriginalMath.Binary32(1f - ring);
                    s.Ring(target, OriginalMath.Binary32(OriginalMath.Binary32(ring * 30f) + 10f), SourceColor(4575657222467066266UL, 1064514355u, OriginalMath.Binary32(fade * .9f)),
                        OriginalMath.Binary32(OriginalMath.Binary32(fade * 3f) + 1f), 32, 0f, 360f);
                    s.Ring(target, OriginalMath.Binary32(OriginalMath.Binary32(ring * 18f) + 6f), new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .7f)), 2f, 24, 0f, 360f);
                }
            }
            if (pressed) s.Glow(tip.x, tip.y, 50f, 50f, SourceColor(4575657222468743987UL, 4543231306014864179UL), 0f);
            Vector2 glow = Point(8f, 28f);
            s.Glow(glow.x, glow.y, 90f, 90f, SourceColor(4572054341899465523UL, 4507923084249923584UL), 0f);
            Vector2 shadow = Point(9f, 30f);
            Disc(s, OriginalMath.Binary32(shadow.x + 4f), OriginalMath.Binary32(shadow.y + 5f), OriginalMath.Binary32(scale * 14f), SourceColor(0UL, 4525216906440867840UL), 1f);
            for (int layer = 0; layer < 3; layer++)
            {
                Color color = layer == 0 ? new Color(.3764705955982208f, .9098039269447327f, 1f, 1f)
                    : layer == 1 ? new Color(.062745101749897f, .1568627506494522f, .23529411852359772f, 1f)
                    : new Color(.9882352948188782f, .9921568632125854f, 1f, 1f);
                float pad = layer == 0 ? 3.2f : layer == 1 ? 1.6f : 0f;
                float finger = OriginalMath.Binary32(pad + 4.4f);
                Capsule(0f, 4f, 0f, 21f, finger, color);
                Blob(9f, 22.5f, OriginalMath.Binary32(pad + 5.2f), color);
                Blob(15f, 25f, OriginalMath.Binary32(pad + 5f), color);
                Blob(20f, 29f, finger, color);
                Blob(8f, 31f, OriginalMath.Binary32(pad + 10.5f), color);
                Blob(14f, 33f, OriginalMath.Binary32(pad + 9.5f), color);
                Blob(4f, 34f, OriginalMath.Binary32(pad + 8f), color);
                Capsule(-4f, 26f, -9f, 33f, OriginalMath.Binary32(pad + 4.2f), color);
                Color wrist = layer == 2 ? new Color(.23529411852359772f, .7843137383460999f, .886274516582489f, 1f) : color;
                Capsule(3f, 43f, 17f, 42f, OriginalMath.Binary32(pad + 5.4f), wrist);
            }
            Color crease = SourceColor(4564636649921235145UL, 4575657222472527085UL);
            Blob(15.5f, 35f, 6.2f, crease);
            Blob(12.5f, 32f, 6.6f, SourceColor(4575092064906968317UL, 4575657222473777152UL));
            Line(12f, 20.5f, 12f, 27.5f, 1.4f, crease);
            Line(17.6f, 23.5f, 17.6f, 30f, 1.4f, crease);
            Line(-1.5f, 28f, 3f, 31.5f, 1.4f, crease);
            Capsule(4f, 44.599998474121094f, 16f, 43.599998474121094f, 1.5f,
                SourceColor(4528607852875526321UL, 4575657222466210957UL));
            Vector2 glint = Point(-1.3f, 6.5f);
            s.Rect(OriginalMath.Binary32(glint.x + -1f), OriginalMath.Binary32(glint.y + -2.5f), 2.5f, 5f, SourceColor(4571333766656011796UL, 4572054341912887296UL));

            Vector2 Point(float x, float y)
            {
                Vector2 rotated = CombatFeedback.Rotate(new Vector2(OriginalMath.Binary32(scale * x), OriginalMath.Binary32(scale * y)), angle);
                return new Vector2(OriginalMath.Binary32(origin.x + rotated.x), OriginalMath.Binary32(origin.y + rotated.y));
            }
            // f54511: segment first, then its two circular caps.
            void Capsule(float x0, float y0, float x1, float y1, float radius, Color color)
            {
                Vector2 a = Point(x0, y0), b = Point(x1, y1);
                s.Segment(a, b, OriginalMath.Binary32(scale * (OriginalMath.Binary32(radius + radius))), color);
                Disc(s, a.x, a.y, OriginalMath.Binary32(scale * radius), color, 1f);
                Disc(s, b.x, b.y, OriginalMath.Binary32(scale * radius), color, 1f);
            }
            // f54512.
            void Blob(float x, float y, float radius, Color color)
            {
                Vector2 at = Point(x, y);
                Disc(s, at.x, at.y, OriginalMath.Binary32(scale * radius), color, 1f);
            }
            // f54513: width is deliberately not multiplied by the hand scale.
            void Line(float x0, float y0, float x1, float y1, float width, Color color) =>
                s.Segment(Point(x0, y0), Point(x1, y1), width, color);
        }
    }
}
