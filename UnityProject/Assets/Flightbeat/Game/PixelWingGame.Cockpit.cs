using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54717. Frame inset, two edge strips and all four rivets.
        private void ArmorPanel(PixelSurface s, float x, float y, float width, float height)
        {
            s.Rect(x, y, width, height, armorDark);
            s.Border(x, y, width, height, armorEdge, 2f);
            float left = OriginalMath.Binary32(x + 3f), top = OriginalMath.Binary32(y + 3f), innerWidth = OriginalMath.Binary32(width + -6f);
            s.Rect(left, top, innerWidth, OriginalMath.Binary32(height + -6f), armor);
            s.Rect(left, top, innerWidth, 2f, SourceColor(4536520058822363787UL, 4575657222467066266UL));
            float bottom = OriginalMath.Binary32(y + height);
            s.Rect(left, OriginalMath.Binary32(bottom + -6f), innerWidth, 3f, SourceColor(4449768368022536385UL, 4575657222450749601UL));
            float[] xs = { OriginalMath.Binary32(x + 5f), OriginalMath.Binary32(OriginalMath.Binary32(x + width) + -8f) }, ys = { OriginalMath.Binary32(y + 6f), OriginalMath.Binary32(bottom + -10f) };
            foreach (float px in xs)
                foreach (float py in ys)
                {
                    s.Rect(px, py, 3f, 4f, armorDark);
                    s.Rect(px, py, 2f, 1f, armorEdge);
                }
        }

        // f54718. Cockpit sides use unmapped screen height; restore prior mapping.
        private void DrawCockpit(PixelSurface s)
        {
            bool mapping = s.MapLayout;
            s.MapLayout = false;
            float height = ScreenLayout.Height;
            for (int side = 0; side < 2; side++)
            {
                float x = OriginalMath.Binary32((float)(side * 530));
                s.Rect(x, 0f, 10f, height, armorDark);
                if (!(height > 0f)) continue;
                for (float y = 0f; y < height; y  = OriginalMath.Binary32(y+(48f)))
                {
                    ArmorPanel(s, x, y, 10f, 46f);
                    float value = OriginalMath.Binary32(y / 48f);
                    int row = Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;
                    if (row % 5 != 1) continue;
                    s.Rect(OriginalMath.Binary32(x + 2f), OriginalMath.Binary32(y + 15f), 6f, 14f, SourceColor(4413739571031896305UL, 4575657222442360993UL));
                    s.Rect(OriginalMath.Binary32(x + 3f), OriginalMath.Binary32(y + 17f), 4f, 9f, SourceColor(4504306077541597184UL, 4575657222453907665UL));
                    s.Rect(OriginalMath.Binary32(x + 4f), OriginalMath.Binary32(y + 17f), 2f, 4f, SourceColor(4561245704525185024UL, 4575657222465881992UL));
                    s.Glow(OriginalMath.Binary32(x + 5f), OriginalMath.Binary32(y + 21f), 17f, 25f, SourceColor(4453159313468293120UL, 4494952716770790605UL), 0f);
                }
            }
            for (int n = 0; n < 8; n++) ArmorPanel(s, OriginalMath.Binary32((float)(10 + n * 65)), OriginalMath.Binary32(height + -10f), 65f, 10f);
            s.MapLayout = mapping;
        }

        // f54719. Animated warning stripes and twelve lower lamps.
        private void DrawHazard(PixelSurface s)
        {
            float warning = warningLevel;
            s.Rect(12f, 567f, 516f, 17f, SourceColor(4395654528863617217UL, 4575657222435025073UL));
            float t = warning < 0f ? 0f : OriginalMath.WasmMin(warning, 1f);
            s.Border(12f, 567f, 516f, 17f,
                Mix(armorEdge, new Color(.5882353186607361f, .1882352977991104f, .1568627506494522f, 1f), warning), 1f);
            float pulse = OriginalMath.Binary32(warning * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 10f)) * .4f) + .6f)));
            pulse = pulse < 0f ? 0f : OriginalMath.WasmMin(pulse, 1f);
            Color stripe = new Color(OriginalMath.Binary32(OriginalMath.Binary32(pulse * .0627450942993164f) + .9372549057006836f),
                OriginalMath.Binary32(OriginalMath.Binary32(pulse * -.4313725531101227f) + .7058823704719543f),
                OriginalMath.Binary32(OriginalMath.Binary32(pulse * .019607841968536377f) + .13725490868091583f), OriginalMath.Binary32(OriginalMath.Binary32(pulse * 0f) + 1f));
            float offset = OriginalMath.Remainder(OriginalMath.Binary32(clock * (OriginalMath.Binary32(OriginalMath.Binary32(warning * 30f) + 6f))), 20f);
            for (int n = -1; n != 26; n++)
            {
                float start = OriginalMath.Binary32(offset + OriginalMath.Binary32((float)(n * 20 + 14)));
                for (int row = 0; row < 8; row++)
                {
                    float x = row == 0 ? start : OriginalMath.Binary32(start + OriginalMath.Binary32((float)-row));
                    if (x < 14f || x > 516f) continue;
                    s.Rect(x, OriginalMath.Binary32((float)(570 + row)), 10f, 1f, stripe);
                }
            }
            Color lamp = new Color(OriginalMath.Binary32(OriginalMath.Binary32(t * .92f) + .08f), OriginalMath.Binary32(OriginalMath.Binary32(t * -.55f) + .7f), OriginalMath.Binary32(OriginalMath.Binary32(t * -.79f) + .84f), OriginalMath.Binary32(OriginalMath.Binary32(t * .15f) + .75f));
            for (int n = 0; n < 12; n++) s.Rect(OriginalMath.Binary32((float)(14 + n * 43)), 582f, 37f, 2f, lamp);
            lamp.a = OriginalMath.Binary32(OriginalMath.Binary32(warning * .2f) + .18f);
            s.Glow(270f, 575f, 540f, 26f, lamp, 0f);
        }
    }
}
