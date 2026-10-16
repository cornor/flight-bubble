using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private bool finaleLanded; // +420; reset when the ready banner first appears.
        private static readonly float[] finaleOrbitPhases = { 0f, .9f, 1.8f, 2.6999998092651367f, 3.6f, 4.5f, 5.399999618530273f };

        // f54740. All banner layers and their timing come from the original WAT.
        private void DrawFinaleButton(PixelSurface s)
        {
            if (!FinaleButtonVisible)
            {
                if (Battle == null || !Battle.FinaleReady) finaleReadyAt = -1f;
                return;
            }
            if (finaleReadyAt < 0f)
            {
                finaleLanded = false;
                finaleReadyAt = ui;
                audioChip.Play("finale-ready");
            }
            float age = OriginalMath.Binary32(ui - finaleReadyAt), press = PressPulse(8);
            HitBox(s, 8, 100f, 716f, 340f, 86f);
            float alpha, scale;
            if (age < .16f)
            {
                alpha = OriginalMath.Binary32(age / .16f);
                scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(OriginalMath.Binary32(alpha * alpha), 1f) * -.9f) + 1.9f);
            }
            else
            {
                float t = OriginalMath.Binary32(age + -.16f);
                alpha = 1f;
                scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(t * 38f)) * OriginalMath.Exp(OriginalMath.Binary32(t * -9f))) * .07f) + 1f);
            }
            if (age >= .16f && !finaleLanded)
            {
                finaleLanded = true;
                feedback.Kick(5f);
                audioChip.Play("land");
            }
            scale = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(press * -.05f) + 1f)) * scale);
            float height = OriginalMath.Binary32(scale * 86f), width = OriginalMath.Binary32(scale * 340f), halfH = OriginalMath.Binary32(height * .5f), halfW = OriginalMath.Binary32(width * .5f);
            float flash = age < .16f ? 0f : Clamp01(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -.16f)) / -.3f) + 1f));
            float glowAlpha = OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(flash * .12f) + .07f))), pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 5f)) * .5f) + .5f);
            float radius = OriginalMath.Binary32(OriginalMath.Binary32(pulse * 20f) + 150f);
            for (int i = 0; i < 12; i++)
            {
                Color c = A(RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 12f) + OriginalMath.Binary32(ui * .1f))), glowAlpha);
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(ui * 14f) + OriginalMath.Binary32((float)(i * 30)))) * .01745329238474369f);
                s.GlowLine(new Vector2(270f, 759f), new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Sin(angle)) + 270f),
                    OriginalMath.Binary32(759f - OriginalMath.Binary32(radius * OriginalMath.Cos(angle)))), 10f, c);
            }
            for (int i = 0; i < 7; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(ui * 1.4f) + finaleOrbitPhases[i]);
                s.Glow(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width * OriginalMath.Cos(angle)) * .34f) + 270f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(angle) * 12f) + 759f),
                    OriginalMath.Binary32(width * .55f), OriginalMath.Binary32(height * 1.6f), A(finaleRainbow[i], OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .07f) + .12f)))), 0f);
            }
            s.Glow(270f, 759f, OriginalMath.Binary32(width + 90f), OriginalMath.Binary32(height + 70f),
                SourceColor(4559804549723848704UL, 1051931443u, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(flash * .4f) + OriginalMath.Binary32(pulse * .1f)) + .22f)))), 0f);
            float landedAge = OriginalMath.Binary32(age + -.16f);
            if (age > .16f)
            {
                float cycle = OriginalMath.Binary32(FinaleRepeat(landedAge, 1.2f) / 1.2f);
                Color c = A(Mix(uiGoldHi, RainbowAt(OriginalMath.Binary32(ui * .3f)), .5f), OriginalMath.Binary32((OriginalMath.Binary32(1f - cycle)) * .55f));
                float growth = OriginalMath.Binary32(OriginalMath.Binary32(OutCubic(cycle) * .28f) + 1f);
                BannerEdge(s, 270f, 759f, OriginalMath.Binary32(width * growth), OriginalMath.Binary32(height * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(growth + -1f)) * 1.6f) + 1f))), c, 2f);
            }
            if (age >= .16f && age < .6599999666213989f)
            {
                float t = OriginalMath.Binary32(landedAge + landedAge), fade = OriginalMath.Binary32(1f - t), ease = OutCubic(t);
                BannerEdge(s, 270f, 759f, OriginalMath.Binary32(width * (OriginalMath.Binary32(OriginalMath.Binary32(ease * .9f) + 1f))), OriginalMath.Binary32(height * (OriginalMath.Binary32(OriginalMath.Binary32(ease * 2.2f) + 1f))),
                    new Color(1f, 1f, 1f, OriginalMath.Binary32(fade * .9f)), OriginalMath.Binary32(OriginalMath.Binary32(fade * 4f) + 1f));
                float size = OriginalMath.Binary32(OriginalMath.Binary32(fade * 5f) + 1f), half = OriginalMath.Binary32(size * .5f);
                for (int i = 0; i < 18; i++)
                {
                    float angle = OriginalMath.Binary32(OriginalMath.Binary32((float)(i * 20 + 7)) * .01745329238474369f);
                    float sin = OriginalMath.Sin(angle), cos = OriginalMath.Cos(angle);
                    float x = OriginalMath.Binary32(OriginalMath.Binary32(ease * (OriginalMath.Binary32(sin * 140f))) + (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width * sin) * .45f) + 270f)));
                    float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(height * cos) * -.5f) + 759f) - OriginalMath.Binary32(ease * (OriginalMath.Binary32(cos * 140f))));
                    s.Rect(OriginalMath.Binary32(x - half), OriginalMath.Binary32(y - half), size, size, A(i % 3 == 0 ? Color.white : finaleRainbow[i % 7], fade));
                }
            }
            float side = OriginalMath.Binary32(height * .45f), top = OriginalMath.Binary32(759f - halfH), left = OriginalMath.Binary32(270f - halfW), shadowAlpha = OriginalMath.Binary32(alpha * .45f);
            BannerFill(s, 273f, 763f, width, height, new Color(0f, 0f, 0f, shadowAlpha), new Color(0f, 0f, 0f, shadowAlpha));
            BannerGradient(s, 270f, 759f, width, height,
                new Color(.4627451002597809f, .10196078568696976f, .0941176488995552f, alpha),
                new Color(-.3607843220233917f, -.08627451211214066f, -.0470588244497776f, OriginalMath.Binary32(alpha - alpha)));
            float smallH = OriginalMath.Binary32(height * .3f), sheenAlpha = OriginalMath.Binary32(alpha * .14f);
            BannerGradient(s, 270f, OriginalMath.Binary32(759f - smallH), OriginalMath.Binary32(width - OriginalMath.Binary32(side * .9f)), smallH,
                new Color(1f, .55f, .35f, sheenAlpha), new Color(0f, -.15f, -.04999998211860657f, OriginalMath.Binary32(0f - sheenAlpha)));
            float arrowStart = OriginalMath.Binary32(halfW + OriginalMath.Binary32(side * -.35f)), arrowTravel = OriginalMath.Binary32(arrowStart + OriginalMath.Binary32(scale * -72f));
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            for (int i = 0; i < 4; i++)
            {
                float cycle = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .25f) + OriginalMath.Binary32(ui * 1.1f)), 1f);
                float phase = OriginalMath.Binary32(OriginalMath.Binary32(ui * .25f) + OriginalMath.Binary32(cycle * .6f));
                float h = OriginalMath.Binary32(smallH * (OriginalMath.Binary32(OriginalMath.Binary32(cycle * -.35f) + 1f)));
                float x = sideIndex == 0 ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(arrowTravel * cycle) - arrowStart) + 270f) : OriginalMath.Binary32(OriginalMath.Binary32(arrowStart - OriginalMath.Binary32(arrowTravel * cycle)) + 270f);
                Color c = A(Mix(RainbowAt(OriginalMath.Binary32(phase + (sideIndex == 0 ? 0f : .5f))), Color.white, .25f),
                    OriginalMath.Binary32(OriginalMath.Binary32(alpha * OriginalMath.Sin(OriginalMath.Binary32(cycle * 3.1415927410125732f))) * .9f));
                Vector2 tip = new Vector2(OriginalMath.Binary32(x + OriginalMath.Binary32(h * (sideIndex == 0 ? -.55f : .55f))), 759f);
                s.Segment(new Vector2(x, OriginalMath.Binary32(759f - h)), tip, 4f, c);
                s.Segment(new Vector2(x, OriginalMath.Binary32(h + 759f)), tip, 4f, c);
            }
            BannerEdge(s, 270f, 759f, width, height, SourceColor(4565569160339259392UL, 1055286886u, alpha), 3f);
            BannerEdge(s, 270f, 759f, OriginalMath.Binary32(width + OriginalMath.Binary32(scale * -14f)), OriginalMath.Binary32(height + OriginalMath.Binary32(scale * -10f)),
                SourceColor(4546834186576592896UL, 1050253722u, shadowAlpha), 1f);
            float innerLeft = OriginalMath.Binary32(side + left), innerWidth = OriginalMath.Binary32(width - (OriginalMath.Binary32(side + side))), bottom = OriginalMath.Binary32(height + top);
            float stripeWidth = OriginalMath.Binary32((OriginalMath.Binary32(innerWidth + -8f)) / 24f);
            for (int i = 0; i < 24; i++)
            {
                Color c = RainbowAt(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 24f) + OriginalMath.Binary32(ui * -.5f)));
                float x = OriginalMath.Binary32(OriginalMath.Binary32(innerLeft + 4f) + OriginalMath.Binary32(stripeWidth * OriginalMath.Binary32((float)i)));
                s.Rect(x, OriginalMath.Binary32(top + 5f), OriginalMath.Binary32(stripeWidth + .5f), 3f, A(c, alpha));
                s.Rect(x, OriginalMath.Binary32(bottom + -8f), OriginalMath.Binary32(stripeWidth + .5f), 3f, A(c, OriginalMath.Binary32(alpha * .85f)));
            }
            radius = OriginalMath.Binary32(OriginalMath.Binary32(scale * 30f) * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .06f) + 1f)));
            s.Glow(270f, 755f, OriginalMath.Binary32(radius * 4f), OriginalMath.Binary32(radius * 3f), SourceColor(4554039943403405312UL, 1050253722u, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .2f) + .35f)))), 0f);
            s.Ring(new Vector2(270f, 755f), radius, SourceColor(4565569160339259392UL, 1055286886u, OriginalMath.Binary32(alpha * .85f)), 2.5f, 6, OriginalMath.Binary32(ui * 40f), 360f);
            s.Ring(new Vector2(270f, 755f), OriginalMath.Binary32(radius * .72f), A(RainbowAt(OriginalMath.Binary32(ui * .4f)), OriginalMath.Binary32(alpha * .8f)), 2f, 6, OriginalMath.Binary32(OriginalMath.Binary32(ui * -70f) + 30f), 360f);
            for (int i = 0; i < 6; i++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(ui * 40f) + OriginalMath.Binary32((float)(i * 60)))) * .01745329238474369f);
                s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Cos(angle)) + 270f) + -2f), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Sin(angle)) + 755f) + -2f),
                    4f, 4f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .9f)));
            }
            float titleY = OriginalMath.Binary32(OriginalMath.Binary32(scale * -26f) + 759f);
            float split = age < .7f ? OriginalMath.Binary32((OriginalMath.Binary32(1f - Clamp01(OriginalMath.Binary32(age / .7f)))) * 9f) : FinaleRepeat(ui, 3.4f) < .08f ? 3f : 0f;
            if (split > 0f)
            {
                s.TextF("决 战", OriginalMath.Binary32(270f - split), titleY, OriginalMath.Binary32(scale * 2.9f), SourceColor(4489188110487257088UL, 1050253722u, OriginalMath.Binary32(alpha * .6f)), 0);
                s.TextF("决 战", OriginalMath.Binary32(split + 270f), titleY, OriginalMath.Binary32(scale * 2.9f), SourceColor(4568451461331864781UL, 1065353216u, OriginalMath.Binary32(alpha * .6f)), 0);
            }
            Title(s, "决 战", 270f, titleY, OriginalMath.Binary32(scale * 2.9f), SourceColor(4572774917166071808UL, 1061662228u, alpha),
                SourceColor(4395654528902139021UL, 1025548449u, alpha));
            s.TextF("全机合体 · 扇形激光清场", 270f, OriginalMath.Binary32(OriginalMath.Binary32(halfH + 759f) + OriginalMath.Binary32(scale * -25f)), scale,
                SourceColor(4568451461351997440UL, 1060320051u, OriginalMath.Binary32(alpha * .9f)), 0);
            float sheen = FinaleRepeat(OriginalMath.Binary32(ui * .55f), 1.6f);
            if (sheen < 1f)
            {
                float start = OriginalMath.Binary32(innerLeft + 10f);
                float x = OriginalMath.Binary32(start + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(width + left) - side) + -4f) - start)) * (sheen < 0f ? 0f : OriginalMath.WasmMin(sheen, 1f))));
                s.Segment(new Vector2(OriginalMath.Binary32(x + 10f), OriginalMath.Binary32(top + 7f)), new Vector2(OriginalMath.Binary32(x + -10f), OriginalMath.Binary32(bottom + -7f)), 9f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .2f)));
                s.Segment(new Vector2(OriginalMath.Binary32(x + 20f), OriginalMath.Binary32(top + 7f)), new Vector2(x, OriginalMath.Binary32(bottom + -7f)), 3f, new Color(1f, 1f, 1f, sheenAlpha));
            }
            for (int i = 0; i < 10; i++)
            {
                float cycle = FinaleRepeat(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .137f) + OriginalMath.Binary32(ui * .7f)), 1f), fade = OriginalMath.Binary32(1f - cycle), size = OriginalMath.Binary32(OriginalMath.Binary32(fade * 3f) + 1f);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(innerLeft + OriginalMath.Binary32(OriginalMath.Binary32(innerWidth * OriginalMath.Binary32((float)(i * 67 % 100))) / 100f)) + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 3f) + OriginalMath.Binary32((float)i))) * 4f));
                Color c = finaleRainbow[i % 7]; c.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(c.a * alpha) * fade) * .9f);
                s.Rect(x, OriginalMath.Binary32(top + OriginalMath.Binary32(cycle * -34f)), size, size, c);
            }
            if (flash > 0f) BannerFill(s, 270f, 759f, width, height, new Color(1f, 1f, 1f, OriginalMath.Binary32(flash * .55f)),
                SourceColor(4568451461351997440UL, 1060320051u, OriginalMath.Binary32(flash * .3f)));
            if (press > 0f) BannerFill(s, 270f, 759f, OriginalMath.Binary32(width + -8f), OriginalMath.Binary32(height + -6f),
                new Color(1f, 1f, 1f, OriginalMath.Binary32(press * .35f)), new Color(1f, 1f, 1f, OriginalMath.Binary32(press * .2f)));
        }
    }
}
