using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Exact representation of paired i64 stores in the cited WAT bodies.
        internal static Color SourceColor(ulong rg, ulong ba) => SourceColor(rg, (uint)ba,
            BitConverter.Int32BitsToSingle(unchecked((int)(ba >> 32))));
        private static Color SourceColor(ulong rg, uint b, float alpha) => new Color(
            BitConverter.Int32BitsToSingle(unchecked((int)rg)),
            BitConverter.Int32BitsToSingle(unchecked((int)(rg >> 32))),
            BitConverter.Int32BitsToSingle(unchecked((int)b)), alpha);
        private static int SourceInt(float value) => Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;
        private static float Repeat01(float value)
        {
            value  =             OriginalMath.Binary32(value-(OriginalMath.Binary32((float)Math.Floor(value))));
            return value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
        }

        // src: ZoneProgress f54541. Stored cleared levels are one-based.
        private int ZoneProgress(int zone)
        {
            int count = 0;
            for (int index = WarZones.FirstLevel(zone); WarZones.LastLevel(zone) >= index;)
                count = unchecked(count + (Economy.Data.cleared.Contains(unchecked(++index)) ? 1 : 0));
            return count;
        }

        // src: DrawZoneCard f54545.
        private void DrawZoneCard(PixelSurface s, float cx, float y, int level)
        {
            int zone = WarZones.OfLevel(level);
            Color accent = WarZones.Accent[zone];
            float x = OriginalMath.Binary32(cx + -220f);
            HitBox(s, 554, x, y, 440f, 60f);
            float pulse = PressPulse(554), twice = OriginalMath.Binary32(pulse + pulse), shrink = OriginalMath.Binary32(pulse * 4f);
            Plate(s, OriginalMath.Binary32(x + twice), OriginalMath.Binary32(twice + y), OriginalMath.Binary32(440f - shrink), OriginalMath.Binary32(60f - shrink),
                Mix(new Color(.05f, .14f, .22f, .94f), accent, .12f),
                SourceColor(4428659729042233098UL, 4571333766631181517UL), A(accent, .85f), 10f);
            DrawZoneScene(s, zone, OriginalMath.Binary32(x + 6f), OriginalMath.Binary32(y + 6f), 96f, 48f, .9f);
            float tx = OriginalMath.Binary32(x + 112f), ty = OriginalMath.Binary32(y + 7f);
            s.TextF(WarZones.Codes[zone], tx, ty, .95f, A(accent, .95f), -1);
            s.TextF(WarZones.Names[zone], OriginalMath.Binary32(tx + 1.215f), OriginalMath.Binary32((OriginalMath.Binary32(y + 27f)) + 1.215f), 1.35f, new Color(0f, 0f, 0f, .7f), -1);
            s.TextF(WarZones.Names[zone], tx, OriginalMath.Binary32(y + 27f), 1.35f, Color.white, -1);
            int position = WarZones.PositionInZone(level), total = WarZones.LevelsIn(zone), cleared = ZoneProgress(zone);
            float right = OriginalMath.Binary32(x + 440f);
            s.TextF("第 " + unchecked(level + 1).ToString() + " 关", OriginalMath.Binary32(right + -104f), ty, 1f, uiGoldHi, 0);
            float barX = OriginalMath.Binary32(right + -160f);
            s.Rect(barX, OriginalMath.Binary32(y + 32f), 112f, 8f, SourceColor(4417130516401346314UL, 4575657222443913708UL));
            s.Rect(OriginalMath.Binary32(barX + 1f), OriginalMath.Binary32(y + 33f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)cleared) * 110f) / OriginalMath.Binary32((float)total)), 6f, accent);
            string progress = cleared.ToString() + "/" + total.ToString();
            s.Text(progress, OriginalMath.Binary32(OriginalMath.Binary32(barX + 56f) + OriginalMath.Binary32(PixelSurface.Measure(progress, 1f) * -.5f)), OriginalMath.Binary32(y + 44f), 1f, uiDim, false);
            Plate(s, OriginalMath.Binary32(right + -44f), OriginalMath.Binary32(y + 14f), 36f, 32f,
                SourceColor(4505040778913696973UL, 4575657222460019835UL),
                SourceColor(4464688526069585674UL, 4575657222452302316UL), accent);
            s.TextF("图鉴", OriginalMath.Binary32(right + -26f), OriginalMath.Binary32(y + 17f), .8f, Color.white, 0);
            s.Segment(new Vector2(OriginalMath.Binary32(right + -30f), OriginalMath.Binary32(y + 33f)), new Vector2(OriginalMath.Binary32(right + -24f), OriginalMath.Binary32(y + 37f)), 2f, accent);
            s.Segment(new Vector2(OriginalMath.Binary32(right + -24f), OriginalMath.Binary32(y + 37f)), new Vector2(OriginalMath.Binary32(right + -30f), OriginalMath.Binary32(y + 41f)), 2f, accent);
            if (Economy.ZoneReady(0) || Economy.ZoneReady(1) || Economy.ZoneReady(2)
                || Economy.ZoneReady(3) || Economy.ZoneReady(4) || position < 0)
                RedDot(s, OriginalMath.Binary32(right + -10f), OriginalMath.Binary32(y + 8f), 0);
        }

        // src: DrawZoneScene f54546. Crops for zones 1..3 are the original
        // data segment at 3648120: (0,1024), (0,1504), (512,0).
        private void DrawZoneScene(PixelSurface s, int zone, float x, float y, float w, float h, float alpha)
        {
            s.Rect(x, y, w, h, A(WarZones.SkyTop[zone], alpha));
            float sourceHeight = zone != 0 ? 480f : 768f, cropHeight = OriginalMath.Binary32(OriginalMath.Binary32(h * 512f) / w);
            sourceHeight = sourceHeight < cropHeight ? sourceHeight : cropHeight;
            if (zone == 0)
            {
                Color nebula = WarZones.Nebula[0];
                s.Space(0f, 120f, 512f, sourceHeight, x, y, w, h,
                    new Color(OriginalMath.Binary32(nebula.r * 1.5f), OriginalMath.Binary32(nebula.g * 1.5f), OriginalMath.Binary32(nebula.b * 1.5f), alpha));
            }
            else
            {
                int cropX = zone == 3 || zone < 1 || zone > 3 ? 512 : 0;
                int cropY = zone == 1 ? 1024 : zone == 2 ? 1504 : zone == 3 ? 0 : 480;
                s.Space(OriginalMath.Binary32((float)cropX), OriginalMath.Binary32((float)unchecked(cropY + 60)), 512f, sourceHeight, x, y, w, h,
                    SourceColor(4582862983597234586UL, 1067030938u, alpha));
            }
            int stars = SourceInt(OriginalMath.Binary32(OriginalMath.Binary32(w * h) / 180f));
            stars = stars < 40 ? stars : 40;
            for (int i = 0; i < stars; i++)
            {
                // f54546: normalize each star fraction before multiplying its span.
                float opacity = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 1.3f) + OriginalMath.Binary32((float)i))) * .5f) + .5f)) * .5f) + .4f)) * alpha);
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 2f) + OriginalMath.Binary32(OriginalMath.Binary32(w + -4f) * OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(i * 137 % 97)) / 97f))),
                    OriginalMath.Binary32(OriginalMath.Binary32(y + 2f) + OriginalMath.Binary32(OriginalMath.Binary32(h + -4f) * OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(i * 89 % 83)) / 83f))),
                    1f, 1f, SourceColor(4569892616150817178UL, 1065353216u, opacity));
            }
            float sizeA = OriginalMath.Binary32(h * .82f), sizeB = OriginalMath.Binary32(w * .5f), size = sizeA < sizeB ? sizeA : sizeB;
            float cx = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(x + w) + OriginalMath.Binary32(size * -.62f)) + -4f), cy = OriginalMath.Binary32(OriginalMath.Binary32(h * .5f) + y);
            if (zone == 0)
                s.Space(0f, 768f, 256f, 256f, OriginalMath.Binary32(cx - OriginalMath.Binary32(size * .6f)), OriginalMath.Binary32(cy - OriginalMath.Binary32(size * .6f)),
                    OriginalMath.Binary32(size * 1.2f), OriginalMath.Binary32(size * 1.2f), new Color(1f, 1f, 1f, alpha));
            else
            {
                float sx = zone == 1 ? 384f : zone == 2 ? 256f : zone == 3 ? 512f : 640f;
                float sy = zone == 1 ? 768f : zone == 2 ? 896f : 960f;
                s.Space(sx, sy, 128f, 128f, OriginalMath.Binary32(cx - OriginalMath.Binary32(size * .55f)), OriginalMath.Binary32(cy - OriginalMath.Binary32(size * .55f)),
                    OriginalMath.Binary32(size * 1.1f), OriginalMath.Binary32(size * 1.1f), new Color(1f, 1f, 1f, alpha));
                // f54546: normalize each animated mote fraction before multiplying its span.
                if (zone == 1)
                {
                    for (int i = 0; i != 8; i++)
                    {
                        float t = Repeat01(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .13f) + OriginalMath.Binary32(ui * .2f)));
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 4f) + OriginalMath.Binary32(OriginalMath.Binary32(w + -8f) * OriginalMath.Binary32(OriginalMath.Binary32((float)(i * 53 % 89)) / 89f))), OriginalMath.Binary32(OriginalMath.Binary32(y + 2f) + OriginalMath.Binary32((OriginalMath.Binary32(h + -5f)) * t)),
                            2f, 2f, SourceColor(4572054341909531853UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .7f) * (OriginalMath.Binary32(1f - t)))));
                    }
                }
                else if (zone == 2 || zone == 3)
                {
                    for (int i = 0; i != 8; i++)
                    {
                        float t = Repeat01(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * (zone == 2 ? .17f : .13f)) + OriginalMath.Binary32(ui * (zone == 2 ? .35f : .25f))));
                        Color tint = zone == 2 ? new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) * .3f) + .55f), .15f, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .8f) * (OriginalMath.Binary32(1f - t))))
                            : SourceColor(4575657222467066266UL, 1056964608u, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .8f) * (OriginalMath.Binary32(1f - t))));
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 4f) + OriginalMath.Binary32(OriginalMath.Binary32(w + -8f) * OriginalMath.Binary32(OriginalMath.Binary32((float)(i * (zone == 2 ? 61 : 67) % 89)) / 89f))),
                            OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(y + h) + -3f) - OriginalMath.Binary32((OriginalMath.Binary32(h + -5f)) * t)), 2f, 2f, tint);
                    }
                }
                else if (zone == 4)
                {
                    // Folded constants in f54546; do not recompute these ratios.
                    float[] phases = { 0f, .19f, .38f, .57f, .76f, .95f };
                    float[] rows = { 0f, .4939759075641632f, .9879518151283264f, .4819277226924896f, .9759036302566528f, .46987950801849365f };
                    for (int i = 0; i != 6; i++)
                        s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(x + 2f) + OriginalMath.Binary32((OriginalMath.Binary32(w + -8f)) * Repeat01(OriginalMath.Binary32(OriginalMath.Binary32(ui * .5f) + phases[i])))), OriginalMath.Binary32(OriginalMath.Binary32(y + 4f) + OriginalMath.Binary32((OriginalMath.Binary32(h + -8f)) * rows[i])),
                            5f, 1f, SourceColor((i & 1) == 0 ? 4568451461343608832UL : 4539628425454813184UL, 1065353216u, OriginalMath.Binary32(alpha * .8f)));
                }
            }
            Color border = WarZones.Accent[zone];
            border.a = OriginalMath.Binary32(OriginalMath.Binary32(alpha * .6f) * border.a);
            s.Border(x, y, w, h, border, 1f);
        }
    }
}
