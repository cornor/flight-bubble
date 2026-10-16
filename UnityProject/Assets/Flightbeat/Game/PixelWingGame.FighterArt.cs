using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // src: f54861 InitializeArray field 5816644, metadata offset 3599920.
        private static readonly int[] PartDrawOrder = { 1, 2, 4, 3, 0 };

        // src: PartTip f54534. Without lowest, the last matching piece wins;
        // lowest chooses the greatest tip Y. Explode interpolation is not clamped.
        private Vector2 PartTip(int fighter, int slot, float cx, float cy, float height, float explode, bool lowest)
        {
            float scale = OriginalMath.Binary32(height / FleetArt.Span[fighter]), best = float.MinValue;
            Vector2 result = new Vector2(cx, cy);
            for (int piece = FleetArt.First[fighter]; piece < FleetArt.First[fighter] + FleetArt.Count[fighter]; piece++)
            {
                if (FleetArt.Slot[piece] != slot) continue;
                Vector2 spread = FleetArt.Exploded[piece], assembled = FleetArt.Assembled[piece];
                float extraX = lowest ? 0f : OriginalMath.Binary32(scale * FleetArt.TipA[piece].x);
                float extraY = lowest ? OriginalMath.Binary32(OriginalMath.Binary32(scale * OriginalMath.Binary32((float)FleetArt.Rect[piece].height)) * .5f) : OriginalMath.Binary32(scale * FleetArt.TipA[piece].y);
                float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(spread.y - assembled.y)) * explode) + assembled.y))) + cy) + extraY);
                float rank = lowest ? y : OriginalMath.Binary32((float)piece);
                if (!(rank > best)) continue;
                result = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(spread.x - assembled.x)) * explode) + assembled.x))) + cx) + extraX), y);
                best = rank;
            }
            return result;
        }

        // src: DrawFighterArt f54535. Authored part order, component tint and
        // eight silhouette draws are retained rather than flattened to one sprite.
        private void DrawFighterArt(PixelSurface s, int fighter, float cx, float cy, float height,
            float explode, int focus, float alpha, bool hologram, bool effectsEnabled)
        {
            fighter = fighter < 4 ? fighter : 4;
            fighter = fighter > 0 ? fighter : 0;
            float span = FleetArt.Span[fighter];
            Color accent = FleetCatalog.FighterAccent[fighter];
            Vector2 nozzle = PartTip(fighter, 4, cx, cy, height, explode, true);
            float engine = Economy != null && Economy.PartLevel(fighter, 4) > 0 ? 1f : .35f;
            Color tail = Mix(new Color(.3f, .9f, 1f, 1f), accent, .35f);
            tail.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(tail.a * alpha) * .5f) * engine);
            s.Glow(nozzle.x, nozzle.y, OriginalMath.Binary32(height * .13f), OriginalMath.Binary32(height * .24f), tail, 0f);
            s.Glow(nozzle.x, OriginalMath.Binary32(OriginalMath.Binary32(height * .03f) + nozzle.y), OriginalMath.Binary32(height * .055f),
                OriginalMath.Binary32(OriginalMath.Binary32(height * .16f) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 20f)) * .2f) + .8f))),
                SourceColor(4575657222470421709UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(alpha * .6f) * engine)), 0f);
            int first = FleetArt.First[fighter], end = first + FleetArt.Count[fighter];
            float scale = OriginalMath.Binary32(height / span);
            foreach (int slot in PartDrawOrder)
            {
                float focused = focus < 0 || slot == focus ? 1f : .78f;
                bool dim = focused < 1f;
                float slotAlpha = OriginalMath.Binary32(focused * alpha);
                float overlayAlpha = OriginalMath.Binary32((OriginalMath.Binary32(alpha * .2f)) * focused);
                for (int piece = first; piece < end; piece++)
                {
                    if (FleetArt.Slot[piece] != slot) continue;
                    Vector2 spread = FleetArt.Exploded[piece], assembled = FleetArt.Assembled[piece];
                    float x = OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(spread.x - assembled.x)) * explode) + assembled.x))) + cx);
                    float y = OriginalMath.Binary32(OriginalMath.Binary32(scale * (OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(spread.y - assembled.y)) * explode) + assembled.y))) + cy);
                    int level = Economy == null ? 1 : Economy.PartLevel(fighter, slot);
                    level = level < 5 ? level : 5;
                    level = level > 0 ? level : 0;
                    bool ghost = hologram || level == 0;
                    bool lit = !ghost && effectsEnabled;
                    if (lit && level >= 5)
                        s.Glow(x, y, OriginalMath.Binary32(OriginalMath.Binary32(scale * OriginalMath.Binary32((float)FleetArt.Rect[piece].width)) * 1.25f),
                            OriginalMath.Binary32(OriginalMath.Binary32(scale * OriginalMath.Binary32((float)FleetArt.Rect[piece].height)) * 1.25f),
                            SourceColor(4559804549723848704UL, 1050253722u,
                                OriginalMath.Binary32(OriginalMath.Binary32(focused * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 3f) + OriginalMath.Binary32((float)slot))) * .08f) + .22f))) * alpha)), 0f);
                    float blue = ghost ? .95f : 1f;
                    if (dim) blue  = OriginalMath.Binary32(blue*(.82f));
                    float red = ghost ? .45f : 1f;
                    if (dim) red  = OriginalMath.Binary32(red*(.72f));
                    float pieceAlpha = ghost ? level != 0 ? .55f : .22f : 1f;
                    if (lit && level >= 2)
                    {
                        float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 3f) + OriginalMath.Binary32((float)slot)));
                        float strength = level > 4 ? .95f : level == 4 ? .85f : level > 2 ? .75f : OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(wave * .25f) + .75f)) * .55f);
                        Color outline = level > 4 ? new Color(1f, .8f, .32f, 1f) : accent;
                        outline.a = OriginalMath.Binary32(OriginalMath.Binary32(focused * strength) * alpha);
                        float r = OriginalMath.WasmMax(OriginalMath.Binary32((level > 2 ? .009f : .006f) * height), 1.5f);
                        const float diagonal = .7071067690849304f;
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + r), OriginalMath.Binary32(y + OriginalMath.Binary32(r * 0f)), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * diagonal)), OriginalMath.Binary32(y + OriginalMath.Binary32(r * diagonal)), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * -0f)), OriginalMath.Binary32(y + r), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * -diagonal)), OriginalMath.Binary32(y + OriginalMath.Binary32(r * diagonal)), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x - r), OriginalMath.Binary32(y + OriginalMath.Binary32(r * -0f)), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * -diagonal)), OriginalMath.Binary32(y + OriginalMath.Binary32(r * -diagonal)), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * 0f)), OriginalMath.Binary32(y - r), scale, outline);
                        s.FleetSilhouette(piece, OriginalMath.Binary32(x + OriginalMath.Binary32(r * diagonal)), OriginalMath.Binary32(y + OriginalMath.Binary32(r * -diagonal)), scale, outline);
                    }
                    s.FleetPiece(piece, x, y, scale, new Color(red, dim ? .76f : 1f, blue, OriginalMath.Binary32(pieceAlpha * alpha)));
                    if (lit && level >= 4)
                    {
                        Color overlay = level > 4 ? new Color(1f, .82f, .4f, 1f) : accent;
                        overlay.a = overlayAlpha;
                        s.FleetSilhouette(piece, x, y, scale, overlay);
                    }
                    if (lit && level >= 2) PartLights(s, piece, new Vector2(x, y), scale, level, accent, slotAlpha);
                }
            }
            if (hologram)
                for (float y = OriginalMath.Binary32(cy - OriginalMath.Binary32(height * .5f)); y < OriginalMath.Binary32(OriginalMath.Binary32(height * .5f) + cy); y  = OriginalMath.Binary32(y+(4f)))
                    s.Rect(OriginalMath.Binary32(cx - OriginalMath.Binary32(height * .5f)), y, height, 1f, SourceColor(4453159312402939904UL, 1041865114u, OriginalMath.Binary32(alpha * .25f)));
        }

        // src: PartLights f54536.
        private void PartLights(PixelSurface s, int piece, Vector2 at, float scale, int level, Color accent, float alpha)
        {
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 3.2f) + OriginalMath.Binary32((float)piece))) * .45f) + .55f);
            Vector2 a = FleetArt.TipA[piece], b = FleetArt.TipB[piece];
            float y = OriginalMath.Binary32(OriginalMath.Binary32(a.y * scale) + at.y), x = OriginalMath.Binary32(OriginalMath.Binary32(a.x * scale) + at.x);
            float size = OriginalMath.WasmMax(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)FleetArt.Rect[piece].width) * scale) * .12f), 6f);
            Color tint = accent;
            tint.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(pulse * .22f) * alpha) * accent.a);
            s.GlowLine(at, new Vector2(x, y), OriginalMath.Binary32(size * .9f), tint);
            float t = Repeat01(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)piece) * .21f) + OriginalMath.Binary32(ui * .8f)));
            tint.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(accent.a * (OriginalMath.Binary32(1f - t))) * .55f) * alpha);
            t = OriginalMath.WasmMax(t, 0f);
            s.Glow(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(x - at.x)) * t) + at.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(y - at.y)) * t) + at.y), OriginalMath.Binary32(size * 1.4f), OriginalMath.Binary32(size * 1.4f), tint, 0f);
            tint.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(pulse * .45f) * alpha) * accent.a);
            s.Glow(x, y, OriginalMath.Binary32(size * 2.2f), OriginalMath.Binary32(size * 2.2f), tint, 0f);
            s.Rect(OriginalMath.Binary32(x + -1.5f), OriginalMath.Binary32(y + -1.5f), 3f, 3f, new Color(1f, 1f, 1f, OriginalMath.Binary32(alpha * .9f)));
            if (level < 3) return;
            y = OriginalMath.Binary32(OriginalMath.Binary32(b.y * scale) + at.y); x = OriginalMath.Binary32(OriginalMath.Binary32(b.x * scale) + at.x);
            float blink = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 5f) + OriginalMath.Binary32((float)piece))) > 0f ? 1f : .35f;
            tint.a = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(accent.a * blink) * .55f) * alpha);
            s.Glow(x, y, OriginalMath.Binary32(size * 1.8f), OriginalMath.Binary32(size * 1.8f), tint, 0f);
            s.Rect(OriginalMath.Binary32(x + -1.5f), OriginalMath.Binary32(y + -1.5f), 3f, 3f, new Color(1f, 1f, 1f, OriginalMath.Binary32(blink * alpha)));
            if (level < 5) return;
            float[] angles = { 0f, 2.09f, 4.18f };
            for (int i = 0; i != 3; i++)
            {
                float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(ui * 1.6f) + angles[i]) + OriginalMath.Binary32((float)piece));
                float vertical = OriginalMath.Binary32(OriginalMath.Sin(angle) * .6f), horizontal = OriginalMath.Cos(angle);
                float shine = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 7f) + OriginalMath.Binary32((float)(i * 2)))) * .5f) + .5f);
                float width = OriginalMath.Binary32((float)FleetArt.Rect[piece].width);
                s.SpriteAt(9, new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(horizontal * width) * scale) * .42f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(vertical * width) * scale) * .42f) + at.y)),
                    OriginalMath.Binary32(OriginalMath.Binary32(shine * 6f) + 5f), SourceColor(4568451461351997440UL, 1056964608u, OriginalMath.Binary32(shine * alpha)), 0f);
            }
        }

        // src: DrawHomeFighterPicker f54605; the unsigned loop comparison visits -1, +1.
        private void DrawHomeFighterPicker(PixelSurface s, float cx, float py, float planeH)
        {
            int assembled = (Economy.Assembled(0) ? 1 : 0) + (Economy.Assembled(1) ? 1 : 0)
                + (Economy.Assembled(2) ? 1 : 0) + (Economy.Assembled(3) ? 1 : 0) + (Economy.Assembled(4) ? 1 : 0);
            if (assembled < 2 || Economy.Data.unlocked < 3) return;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int id = direction < 0 ? 480 : 481;
                float x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(planeH * .5f) + 10f)) * OriginalMath.Binary32((float)direction)) + cx);
                x = x < 40f ? 40f : OriginalMath.WasmMin(x, 500f);
                float pulse = PressPulse(id);
                HitBox(s, id, OriginalMath.Binary32(x + -30f), OriginalMath.Binary32(py + -40f), 60f, 80f);
                Disc(s, x, py, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * -.1f) + 1f)) * 22f), SourceColor(4453159313425678991UL, 4564848585062586778UL));
                s.Ring(new Vector2(x, py), 22f, A(uiCyan, .8f), 2f, 24, 0f, 360f);
                float tipX = OriginalMath.Binary32(x + OriginalMath.Binary32((float)(direction * 7))), backX = OriginalMath.Binary32(tipX + OriginalMath.Binary32((float)(direction * -10)));
                s.Segment(new Vector2(tipX, py), new Vector2(backX, OriginalMath.Binary32(py + -10f)), 4f, uiCyan);
                s.Segment(new Vector2(tipX, py), new Vector2(backX, OriginalMath.Binary32(py + 10f)), 4f, uiCyan);
            }
        }

        // src: DrawAfterburner f54604.
        private void DrawAfterburner(PixelSurface s, float x, float nozzleY, float planeH, float flame, float rise, float t)
        {
            float length = OriginalMath.Binary32(OriginalMath.Binary32((rise < 900f ? rise : 900f) * .9f)
                + OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(flame * .55f) + .25f)) * planeH) * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 48f)) * .15f) + .85f))));
            s.Glow(x, OriginalMath.Binary32(OriginalMath.Binary32(length * .3f) + nozzleY), OriginalMath.Binary32(OriginalMath.Binary32(planeH * .9f) * flame), OriginalMath.Binary32(OriginalMath.Binary32(length * 1.2f) + 60f),
                SourceColor(4539628425454813184UL, 1039516303u, OriginalMath.Binary32(flame * .45f)), 0f);
            float[] offsets = { OriginalMath.Binary32(planeH * -.075f), 0f, OriginalMath.Binary32(planeH * .075f) };
            foreach (float offset in offsets)
            {
                float px = OriginalMath.Binary32(offset + x), endX = OriginalMath.Binary32(px + 0f), endY = OriginalMath.Binary32(OriginalMath.Binary32(length * (offset == 0f ? 1f : .78f)) + nozzleY);
                Vector2 from = new Vector2(px, nozzleY), to = new Vector2(endX, endY);
                s.GlowLine(from, to, OriginalMath.Binary32(planeH * .14f), SourceColor(4532422664333033472UL, 1036831949u, OriginalMath.Binary32(flame * .7f)));
                s.GlowLine(from, to, OriginalMath.Binary32(planeH * .07f), SourceColor(4564848585086074880UL, 1051931443u, OriginalMath.Binary32(flame * .9f)));
                s.Segment(from, new Vector2(OriginalMath.Binary32(px + OriginalMath.Binary32((OriginalMath.Binary32(endX - px)) * .55f)), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(endY - nozzleY)) * .55f) + nozzleY)),
                    OriginalMath.Binary32(OriginalMath.Binary32(planeH * .025f) + 2f), SourceColor(4575657222473777152UL, 1063675494u, flame));
            }
            if (t >= .3f && t < .6f)
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height,
                    SourceColor(4572054341912887296UL, 1062836634u, OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Binary32((OriginalMath.Binary32(t + -.3f)) / .3f))) * .45f)));
        }

        // src: DrawSpeedLines f54606.
        private void DrawSpeedLines(PixelSurface s, float t)
        {
            float alpha = OriginalMath.Binary32(Clamp01(OriginalMath.Binary32(t / .12f)) * .35f);
            for (int i = 0; i != 26; i++)
            {
                float length = OriginalMath.Binary32((float)(i * 53 % 90 + 60)), period = OriginalMath.Binary32(ScreenLayout.Height + length);
                float y = OriginalMath.Binary32(OriginalMath.Binary32(ui * 2600f) + OriginalMath.Binary32((float)(i * 211)));
                y  =                 OriginalMath.Binary32(y-(OriginalMath.Binary32(period * OriginalMath.Binary32((float)System.Math.Floor(OriginalMath.Binary32(y / period))))));
                y = y < 0f ? 0f : y > period ? period : y;
                s.Rect(OriginalMath.Binary32(ScreenLayout.Left + OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Width * OriginalMath.Binary32((float)(i * 197 % 1000))) / 1000f)),
                    OriginalMath.Binary32(y - length), i % 3 != 0 ? 2f : 3f, length, SourceColor(4572054341908692992UL, 1065353216u, alpha));
            }
        }
    }
}
