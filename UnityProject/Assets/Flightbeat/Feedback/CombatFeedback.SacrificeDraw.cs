using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53886. Literal stores and folded f32 offsets from WAT, including
        // .21/.42 and the fourth flame phase that decompilation rounds away.
        private static readonly float[] SacrificeTrail = { -.07f, -.14f, -.21000000834465027f, -.28f, -.35f, -.42000001668930054f };
        private static readonly ulong[] SacrificeTrailRG = {
            4525216907506221056UL, 4532422664333033472UL, 4539628425454813184UL,
            4543231306015703040UL, 4546834186576592896UL, 4550437062842515456UL };
        private static readonly ulong[] SacrificeTrailBA = {
            4530981513789754573UL, 4522334602161605837UL, 4513687690533457101UL,
            4505040778905308365UL, 4489188114745314509UL, 4471894291489017037UL };
        private static readonly float[] SacrificeFlamePhase = { 0f, 1.3f, 2.6f, 3.8999998569488525f, 5.2f };
        private static readonly Vector2[] SacrificeDebris = {
            new Vector2(.8080275058746338f, -.589144766330719f),
            new Vector2(.3058169186115265f, -.952090322971344f),
            new Vector2(-.3138105571269989f, -.9494855999946594f),
            new Vector2(-.8129520416259766f, -.5823306441307068f),
            new Vector2(-.9999646544456482f, -.008407343178987503f),
            new Vector2(-.8030458688735962f, -.5959172248840332f),
            new Vector2(-.29780179262161255f, -.9546277523040771f),
            new Vector2(.3217819929122925f, -.9468137621879578f),
            new Vector2(.8178191184997559f, -.5754753351211548f) };

        private void DrawSacrifices(PixelSurface s)
        {
            foreach (Kamikaze value in kamikazes)
            {
                float front = OriginalMath.Binary32(FrontScreenY + 6f);
                if (!value.exploded)
                {
                    float fraction = OriginalMath.Binary32(value.age / value.delay), shake = 0f, progress;
                    if (fraction < .25f)
                    {
                        float sine = OriginalMath.Sin(OriginalMath.Binary32(value.age * 90f));
                        shake = OriginalMath.Binary32(sine + sine); progress = 0f;
                    }
                    else { float t = OriginalMath.Binary32((OriginalMath.Binary32(fraction + -.25f)) / .75f); progress = OriginalMath.Binary32(t * t); }
                    float distance = OriginalMath.Binary32(front - value.from.y);
                    for (int n = 0; n < 6; n++)
                    {
                        float t = Unit(OriginalMath.WasmMax(OriginalMath.Binary32(progress + SacrificeTrail[n]), 0f));
                        float y = OriginalMath.Binary32(OriginalMath.Binary32(value.from.y + OriginalMath.Binary32(t * distance)) + OriginalMath.Binary32((float)((n + 1) * 2)));
                        float size = OriginalMath.Binary32((float)(27 - n * 3));
                        s.Glow(value.x, y, size, size, SourceColor(SacrificeTrailRG[n], SacrificeTrailBA[n]), 0f);
                    }
                    var at = new Vector2(OriginalMath.Binary32(shake + value.x), OriginalMath.Binary32(value.from.y + OriginalMath.Binary32(OriginalMath.WasmMin(progress, 1f) * distance)));
                    s.Glow(at.x, at.y, 70f, 80f, SourceColor(4525216907506221056UL, 4543231305978793165UL), 0f);
                    Ship(s, value.color, value.tier, at, OriginalMath.Binary32((float)(value.tier * 6 + 40)), OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(value.age * 30f)) * 6f),
                        SourceColor(4546834186576592896UL, 4575657222462033101UL), 0f, false);
                    foreach (float phase in SacrificeFlamePhase)
                    {
                        float angle = OriginalMath.Binary32(OriginalMath.Binary32(time * 20f) + phase);
                        s.Glow(OriginalMath.Binary32(at.x + OriginalMath.Binary32(OriginalMath.Sin(angle) * 10f)), OriginalMath.Binary32(OriginalMath.Binary32(at.y + 12f) + OriginalMath.Binary32(OriginalMath.Cos(angle) * 6f)),
                            14f, 20f, SourceColor(4561245704525185024UL, 4561245704505052365UL), 0f);
                    }
                    continue;
                }
                float elapsed = OriginalMath.Binary32(value.age - value.delay), t2 = OriginalMath.Binary32(elapsed + elapsed), fade = OriginalMath.Binary32(1f - t2);
                int part = Look == null ? 1 : Look.Part(2);
                part = part < 5 ? part : 5;
                part = part > 1 ? part : 1;
                float strength = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(part - 1)) * .16f) + 1f);
                float green = part >= 5 ? .85f : .6f, blue = part >= 5 ? .3f : .2f;
                float shrink = OriginalMath.Binary32(OriginalMath.Binary32(t2 * -.5f) + 1f);
                // f53886 WAT multiplies strength by the dimension before shrink.
                s.Glow(value.x, front, OriginalMath.Binary32(shrink * OriginalMath.Binary32(strength * 220f)), OriginalMath.Binary32(shrink * OriginalMath.Binary32(strength * 120f)),
                    SourceColor(4532422664333033472UL, 1034147594u, OriginalMath.Binary32(fade * .6f)), 0f);
                if (t2 < .3f)
                    s.Glow(value.x, front, OriginalMath.Binary32(strength * 90f), OriginalMath.Binary32(strength * 90f),
                        SourceColor(4575657222473777152UL, 1061997773u, OriginalMath.Binary32(OriginalMath.Binary32(t2 / -.3f) + 1f)), 0f);
                var center = new Vector2(value.x, front);
                float radial = OriginalMath.Binary32(t2 * 110f);
                s.Ring(center, OriginalMath.Binary32(OriginalMath.Binary32(radial * strength) + 20f), new Color(1f, green, blue, OriginalMath.Binary32(fade * .75f)), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)part) * .3f) + 2f), 24, 0f, 360f);
                if (part >= 4)
                    s.Ring(center, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t2 * 70f) * strength) + 10f), SourceColor(4575657222473777152UL, 1061997773u, OriginalMath.Binary32(fade * .8f)), 2f, 20, 0f, 360f);
                if (part >= 3)
                {
                    float outer = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t2 * 90f) * strength) + 26f), inner = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(t2 * 60f) * strength) + 16f);
                    for (int n = 0; n < 8; n++)
                    {
                        float angle = OriginalMath.Binary32((OriginalMath.Binary32(value.x + OriginalMath.Binary32((float)(n * 45)))) * .01745329238474369f);
                        float sine = OriginalMath.Sin(angle), cosine = OriginalMath.Cos(angle);
                        // Original Segment receives the inner point first.
                        s.Segment(new Vector2(OriginalMath.Binary32(value.x + OriginalMath.Binary32(inner * cosine)), OriginalMath.Binary32(front + OriginalMath.Binary32(inner * sine))),
                            new Vector2(OriginalMath.Binary32(value.x + OriginalMath.Binary32(outer * cosine)), OriginalMath.Binary32(front + OriginalMath.Binary32(outer * sine))), 2.5f, new Color(1f, green, blue, fade));
                    }
                }
                if (Model != 3) continue;
                s.Ring(center, OriginalMath.Binary32(OriginalMath.Binary32(t2 * 160f) + 12f), SourceColor(4518011146384441344UL, 1028443341u, OriginalMath.Binary32(fade * .9f)), 4f, 28, 0f, 360f);
                s.Glow(value.x, OriginalMath.Binary32(front + -20f), OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(t2 * -.3f) + 1f)) * 260f), 90f,
                    SourceColor(4510805389557628928UL, 1017370378u, OriginalMath.Binary32(fade * .45f)), 0f);
                float dx = OriginalMath.Binary32(OriginalMath.Binary32(t2 * 90f) + 30f), dy = OriginalMath.Binary32(radial + 20f);
                Color debris = SourceColor(4554039943403405312UL, 1045220557u, fade);
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(value.x + dx) + -2f), OriginalMath.Binary32(OriginalMath.Binary32(front + OriginalMath.Binary32(dy * -0f)) + -2f), 4f, 4f, debris);
                foreach (Vector2 direction in SacrificeDebris)
                    s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(value.x + OriginalMath.Binary32(dx * direction.x)) + -2f), OriginalMath.Binary32(OriginalMath.Binary32(front + OriginalMath.Binary32(dy * direction.y)) + -2f), 4f, 4f, debris);
            }
        }
    }
}
