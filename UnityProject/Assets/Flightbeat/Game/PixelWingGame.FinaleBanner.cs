using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54861 static +68: source rainbow, independent of the current level.
        private static readonly Color[] finaleRainbow = {
            SourceColor(4507131865374654464UL, 4575657222457822349UL),
            SourceColor(4546551607793287168UL, 4575657222452854977UL),
            SourceColor(4569157910457745408UL, 4575657222458085521UL),
            SourceColor(4569157910443632809UL, 4575657222463348961UL),
            SourceColor(4564071492341497985UL, 4575657222473777152UL),
            SourceColor(4535389743691114665UL, 4575657222473777152UL),
            SourceColor(4521825962102012603UL, 4575657222473777152UL)
        };

        private static float FinaleRepeat(float t, float length)
        {
            t  =             OriginalMath.Binary32(t-(OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(t / length))) * length)));
            return t < 0f ? 0f : OriginalMath.WasmMin(t, length);
        }
        // f54726; palette wrap occurs after the f32 phase multiply.
        private static Color RainbowAt(float t)
        {
            t = OriginalMath.Binary32(FinaleRepeat(t, 1f) * OriginalMath.Binary32((float)finaleRainbow.Length));
            int index = Math.Abs(t) < 2147483648f ? (int)t : int.MinValue;
            int at = index % finaleRainbow.Length;
            return Mix(finaleRainbow[at], finaleRainbow[(at + 1) % finaleRainbow.Length], OriginalMath.Binary32(t - OriginalMath.Binary32((float)index)));
        }

        // f54738. The inline copies in f54740 use folded color deltas; preserve
        // those exact operands through BannerGradient rather than rounded RGB.
        private static void BannerFill(PixelSurface s, float cx, float cy, float w, float h, Color top, Color bottom)
        {
            BannerGradient(s, cx, cy, w, h, top,
                new Color(OriginalMath.Binary32(bottom.r - top.r), OriginalMath.Binary32(bottom.g - top.g), OriginalMath.Binary32(bottom.b - top.b), OriginalMath.Binary32(bottom.a - top.a)));
        }
        private static void BannerGradient(PixelSurface s, float cx, float cy, float w, float h, Color top, Color delta)
        {
            float countValue = OriginalMath.Binary32(h / 3f);
            int count = Math.Max(4, Math.Abs(countValue) < 2147483648f ? (int)countValue : int.MinValue);
            float step = OriginalMath.Binary32(h / OriginalMath.Binary32((float)count)), halfStep = OriginalMath.Binary32(step * .5f), halfH = OriginalMath.Binary32(h * .5f);
            float left = OriginalMath.Binary32(cx - OriginalMath.Binary32(w * .5f)), y = OriginalMath.Binary32(cy - halfH), bevel = OriginalMath.Binary32(halfH * .9f);
            for (int i = 0; i < count; i++)
            {
                float t = OriginalMath.Binary32(OriginalMath.Binary32((float)i) / OriginalMath.Binary32((float)(count - 1)));
                t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
                Color c = new Color(OriginalMath.Binary32(top.r + OriginalMath.Binary32(t * delta.r)), OriginalMath.Binary32(top.g + OriginalMath.Binary32(t * delta.g)), OriginalMath.Binary32(top.b + OriginalMath.Binary32(t * delta.b)), OriginalMath.Binary32(top.a + OriginalMath.Binary32(t * delta.a)));
                float row = OriginalMath.Binary32(y + OriginalMath.Binary32(step * OriginalMath.Binary32((float)i))), inset = OriginalMath.Binary32(bevel * (OriginalMath.Binary32(Math.Abs(OriginalMath.Binary32(OriginalMath.Binary32(halfStep + row) - cy)) / halfH)));
                s.Rect(OriginalMath.Binary32(left + inset), row, OriginalMath.Binary32(w - (OriginalMath.Binary32(inset + inset))), OriginalMath.Binary32(step + .5f), c);
            }
        }

        // f54739: six ordered edges, each followed by its starting corner square.
        private static void BannerEdge(PixelSurface s, float cx, float cy, float w, float h, Color c, float thickness)
        {
            float left = OriginalMath.Binary32(cx - OriginalMath.Binary32(w * .5f)), right = OriginalMath.Binary32(OriginalMath.Binary32(w * .5f) + cx), halfH = OriginalMath.Binary32(h * .5f);
            float bevel = OriginalMath.Binary32(halfH * .9f), top = OriginalMath.Binary32(cy - halfH), bottom = OriginalMath.Binary32(halfH + cy);
            Vector2[] points = { new Vector2(left, cy), new Vector2(OriginalMath.Binary32(left + bevel), top),
                new Vector2(OriginalMath.Binary32(right - bevel), top), new Vector2(right, cy),
                new Vector2(OriginalMath.Binary32(right - bevel), bottom), new Vector2(OriginalMath.Binary32(left + bevel), bottom) };
            float halfT = OriginalMath.Binary32(thickness * .5f);
            for (int i = 0; i < 6; i++)
            {
                s.Segment(points[i], points[(i + 1) % 6], thickness, c);
                s.Rect(OriginalMath.Binary32(points[i].x - halfT), OriginalMath.Binary32(points[i].y - halfT), thickness, thickness, c);
            }
        }
    }
}
