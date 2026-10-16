using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53912: these x positions are folded constants in the original WAT.
        private static readonly float[] ShockwaveTeeth = { 30f, 90.74762725830078f, 145.976318359375f,
            193.64907836914062f, 236.4959716796875f, 280.2917785644531f, 330.2125549316406f,
            387.57012939453125f, 448.98431396484375f, 508.51568603515625f };

        // f53912, including upgrade, rescue and loadout-label branches.
        private void DrawShockwaves(PixelSurface s)
        {
            float front = OriginalMath.Binary32(FrontScreenY + 10f);
            float teethBase = OriginalMath.Binary32(front + -6f), labelBase = OriginalMath.Binary32(front + -34f);
            float bottom = OriginalMath.Binary32(front + 6f), extraBase = OriginalMath.Binary32(front + -18f);
            foreach (Wave wave in waves)
            {
                float t = Unit(OriginalMath.Binary32(wave.age / wave.duration));
                float opacity = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(t * 3.1415927410125732f)) * .9f);
                if (wave.kind == RepelKind.Revive)
                {
                    float remaining = OriginalMath.Binary32(1f - t), y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Binary32(remaining * remaining))) * -460f) + 574f);
                    Color tint = SourceColor(4575657222456999936UL, 1062501089u, OriginalMath.Binary32(opacity * .3f));
                    Color line = Alpha(tint, opacity);
                    s.Glow(270f, OriginalMath.Binary32(y + 0f), 540f, 70f, tint, 0f);
                    s.Rect(18f, OriginalMath.Binary32(y + 0f), 504f, 3f, line);
                    s.Glow(270f, OriginalMath.Binary32(y + 16f), 540f, 28f, tint, 0f);
                    s.Rect(18f, OriginalMath.Binary32(y + 16f), 504f, 1f, line);
                    s.Glow(270f, OriginalMath.Binary32(y + 32f), 540f, 28f, tint, 0f);
                    s.Rect(18f, OriginalMath.Binary32(y + 32f), 504f, 1f, line);
                    s.Glow(20f, 350f, 70f, 520f, Alpha(tint, OriginalMath.Binary32(opacity * .35f)), 0f);
                    s.Glow(520f, 350f, 70f, 520f, Alpha(tint, OriginalMath.Binary32(opacity * .35f)), 0f);
                    continue;
                }
                int part = 1;
                Color color = new Color(1f, .33f, .14f, 1f);
                if (wave.kind == RepelKind.Empty || wave.kind == RepelKind.Fusion)
                {
                    color = wave.kind == RepelKind.Empty ? new Color(1f, .55f, .12f, 1f) : new Color(.85f, .65f, 1f, 1f);
                    if (Look != null)
                    {
                        part = Look.Part(wave.kind == RepelKind.Empty ? 2 : 1);
                        part = part < 5 ? part : 5;
                        part = part > 1 ? part : 1;
                        if (part >= 5) color = new Color(1f, .85f, .35f, 1f);
                    }
                }
                float rest = OriginalMath.Binary32(1f - t), span = OriginalMath.Binary32(rest * 30f);
                s.Glow(270f, front, 560f, OriginalMath.Binary32((OriginalMath.Binary32(span + 60f)) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(part - 1)) * .3f) + 1f))), Alpha(color, OriginalMath.Binary32(opacity * .4f)), 0f);
                if (part > 2)
                {
                    float y = OriginalMath.Binary32(extraBase + OriginalMath.Binary32(t * -12f));
                    Vector2 previous = new Vector2(14f, y);
                    for (int n = 1; n < 27; n++)
                    {
                        Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)n) * 19.4f) + 14f),
                            OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(rest * OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)n) * 1.3f) + OriginalMath.Binary32(time * -35f)))) * 5f)));
                        s.Segment(previous, next, 2f, Alpha(color, OriginalMath.Binary32(opacity * .85f)));
                        previous = next;
                    }
                }
                Vector2 point = new Vector2(14f, front);
                for (int n = 1; n < 27; n++)
                {
                    Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)n) * 19.4f) + 14f),
                        OriginalMath.Binary32(front + OriginalMath.Binary32(OriginalMath.Binary32(rest * OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)n) * 1.7f) + OriginalMath.Binary32(time * 40f)))) * 4f)));
                    s.Segment(point, next, 2.5f, new Color(1f, 1f, 1f, opacity));
                    point = next;
                }
                s.Rect(14f, bottom, 512f, 2f, Alpha(color, opacity));
                string label = null;
                float bonus = wave.kind == RepelKind.Empty ? battle.Loadout.sacrifice
                    : wave.kind == RepelKind.Fusion ? battle.Loadout.repel : 0f;
                if (bonus > .001f)
                {
                    // This method promotes the product before rounding (f64),
                    // unlike PixelSurface's f32 RoundSingle instruction blocks.
                    double rounded = System.Math.Round((double)(OriginalMath.Binary32(bonus * 100f)), System.MidpointRounding.ToEven);
                    int percent = System.Math.Abs(rounded) < 2147483648d ? (int)rounded : int.MinValue;
                    label = (wave.kind == RepelKind.Empty ? "退场冲击 +" : "合成击退 +") + percent.ToString() + "%";
                }
                if (label != null)
                {
                    float y = OriginalMath.Binary32(labelBase + OriginalMath.Binary32(t * -26f));
                    s.TextF(label, 271f, OriginalMath.Binary32(y + 1f), 1.05f, new Color(0f, 0f, 0f, OriginalMath.Binary32(opacity * .8f)), 0);
                    s.TextF(label, 270f, y, 1.05f, Alpha(Mix(color, Color.white, .4f), opacity), 0);
                }
                float start = OriginalMath.Binary32(teethBase + OriginalMath.Binary32(t * -30f)), end = OriginalMath.Binary32(start - (OriginalMath.Binary32(span + 20f)));
                foreach (float x in ShockwaveTeeth)
                    s.Segment(new Vector2(x, start), new Vector2(x, end), 2f, Alpha(color, OriginalMath.Binary32(opacity * .8f)));
            }
        }
    }
}
