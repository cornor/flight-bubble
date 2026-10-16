using System;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public static class SuperStrikeVisuals
    {
        private const float Radians = .01745329238474369f; // WAT 0x3c8efa35
        private static Color SourceColor(ulong rg, ulong ba) => new Color(
            BitConverter.Int32BitsToSingle(unchecked((int)rg)),
            BitConverter.Int32BitsToSingle(unchecked((int)(rg >> 32))),
            BitConverter.Int32BitsToSingle(unchecked((int)ba)),
            BitConverter.Int32BitsToSingle(unchecked((int)(ba >> 32))));
        private static Vector2 Polar(Vector2 p, float radius, float angle) => new Vector2(
            OriginalMath.Cos(angle) * radius + p.x, OriginalMath.Sin(angle) * radius + p.y);

        // f54425. Full source reticle, including outer ticks, inner arcs, four
        // crosshair arms, center dot, glow and preview marker. Keep arc grouping.
        public static void Reticle(PixelSurface s, Vector2 p, float radius, float time, Color color, bool preview)
        {
            Color shadow = SourceColor(4333543704410193920UL, 4561245704477202186UL);
            for (int pass = 0; pass < 2; pass++)
            {
                Vector2 previous = new Vector2(p.x + radius, radius * 0f + p.y);
                for (int i = 1; i <= 64; i++)
                {
                    Vector2 next = Polar(p, radius, ((float)i * 360f * .015625f + 0f) * Radians);
                    s.Segment(previous, next, pass == 0 ? 5f : 2f, pass == 0 ? shadow : color);
                    previous = next;
                }
            }
            Color tick = new Color(color.r, color.g, color.b, .75f);
            for (int i = 0; i < 24; i++)
            {
                float start = time * 9f + (float)(i * 15);
                Vector2 previous = Polar(p, radius, start * Radians);
                for (int j = 1; j <= 3; j++)
                {
                    Vector2 next = Polar(p, radius, (start + (float)j * 7f / 3f) * Radians);
                    s.Segment(previous, next, 1f, tick);
                    previous = next;
                }
            }
            shadow = SourceColor(4356602133986934784UL, 4568451461306698957UL);
            for (int side = 0; side < 4; side++)
            {
                float start = time * 20f + (float)(side * 90);
                for (int pass = 0; pass < 2; pass++)
                {
                    Vector2 previous = Polar(p, 33f, start * Radians);
                    for (int j = 1; j <= 8; j++)
                    {
                        Vector2 next = Polar(p, 33f, (start + (float)j * 52f * .125f) * Radians);
                        s.Segment(previous, next, pass == 0 ? 5f : 2f, pass == 0 ? shadow : color);
                        previous = next;
                    }
                }
                float angle = (float)side * 3.1415927410125732f * .5f;
                s.Segment(Polar(p, 8f, angle), Polar(p, 26f, angle), 5f, shadow);
                s.Segment(Polar(p, 10f, angle), Polar(p, 24f, angle), 2f, color);
                s.Segment(Polar(p, 42f, angle), Polar(p, 53f, angle), 1f, color);
                s.Segment(Polar(p, radius + -6f, angle), Polar(p, radius + 7f, angle), 2f, color);
            }
            s.Rect(p.x + -2f, p.y + -2f, 4f, 4f, Color.white);
            s.Glow(p.x, p.y, 80f, 80f, new Color(color.r, color.g, color.b, .13f), 0f);
            if (preview) s.Text("+", p.x + radius + -10f, p.y + -16f, 1f, color, false);
        }
    }
}
