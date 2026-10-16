using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54509. Even disabled buttons register their original hit region;
        // the action dispatcher decides whether the action is available.
        internal void Button(PixelSurface s, int id, float x, float y, float w, float h,
            string label, Btn style, float textSize = 2f, bool enabled = true, int icon = 0, bool ad = false)
        {
            HitBox(s, id, x, y, w, h);
            float pulse = PressPulse(id), shrink = OriginalMath.Binary32(pulse * .06f);
            float cx = OriginalMath.Binary32(OriginalMath.Binary32(w * .5f) + x), cy = OriginalMath.Binary32(OriginalMath.Binary32(h * .5f) + y);
            w = OriginalMath.Binary32((OriginalMath.Binary32(1f - shrink)) * w);
            x = OriginalMath.Binary32(cx - OriginalMath.Binary32(w * .5f));
            h = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(shrink * -1.4f) + 1f)) * h);
            y = OriginalMath.Binary32(cy - OriginalMath.Binary32(h * .5f));
            Color top, bottom, edge, text;
            switch (style)
            {
                case Btn.Primary:
                    top = uiGoldHi; bottom = uiGoldLo;
                    edge = new Color(.360784322f, .172549024f, .0235294122f, 1f);
                    text = uiBrown;
                    break;
                case Btn.Danger:
                    top = new Color(1f, .549019635f, .431372553f, 1f);
                    bottom = new Color(.768627465f, .180392161f, .156862751f, 1f);
                    edge = new Color(.313725501f, .0392156877f, .0313725509f, 1f);
                    text = uiText;
                    break;
                case Btn.Ghost:
                    top = new Color(.05f, .14f, .2f, .7f);
                    bottom = new Color(.02f, .07f, .11f, .7f);
                    edge = A(uiEdge, .9f); text = uiDim;
                    break;
                case Btn.Green:
                    top = new Color(.588235319f, 1f, .745098054f, 1f);
                    bottom = new Color(.117647059f, .666666687f, .431372553f, 1f);
                    edge = new Color(.0235294122f, .250980407f, .156862751f, 1f);
                    text = new Color(.0235294122f, .172549024f, .109803922f, 1f);
                    break;
                default:
                    top = new Color(.109803922f, .305882365f, .407843143f, 1f);
                    bottom = new Color(.0392156877f, .141176477f, .211764708f, 1f);
                    edge = uiCyan; text = uiText;
                    break;
            }
            if (!enabled)
            {
                bottom = Mix(bottom, new Color(.1f, .12f, .15f, 1f), .75f);
                top = Mix(top, new Color(.2f, .24f, .28f, 1f), .75f);
                edge = Mix(edge, uiEdge, .7f); text = uiDim;
            }
            else if (style == Btn.Primary)
                s.Glow(cx, cy, OriginalMath.Binary32(w + 40f), OriginalMath.Binary32(h + 30f), new Color(1f, .7f, .2f, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .06f) + .18f)), 0f);
            Plate(s, x, y, w, h, top, bottom, edge, OriginalMath.WasmMin(OriginalMath.Binary32(h * .22f), 8f), 1f);
            if (enabled && (style == Btn.Primary || style == Btn.Green))
            {
                float sweep = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)id) * .13f) + OriginalMath.Binary32(ui * .45f))) % 1.6f);
                if (sweep < 1f)
                {
                    float sx = OriginalMath.Binary32(OriginalMath.Binary32(x + 6f) + OriginalMath.Binary32((OriginalMath.Binary32(w + -24f)) * sweep));
                    s.Rect(sx, OriginalMath.Binary32(y + 3f), 6f, OriginalMath.Binary32(h + -6f), new Color(1f, 1f, 1f, .28f));
                    s.Rect(OriginalMath.Binary32(sx + 8f), OriginalMath.Binary32(y + 3f), 2f, OriginalMath.Binary32(h + -6f), new Color(1f, 1f, 1f, .18f));
                }
            }
            if (pulse > 0f) s.Rect(OriginalMath.Binary32(x + 3f), OriginalMath.Binary32(y + 3f), OriginalMath.Binary32(w + -6f), OriginalMath.Binary32(h + -6f), new Color(1f, 1f, 1f, OriginalMath.Binary32(pulse * .35f)));
            float adWidth = ad ? 22f : 0f, iconSize = OriginalMath.Binary32(h * .62f);
            float iconWidth = icon != 0 ? OriginalMath.Binary32(iconSize + 4f) : 0f;
            float tx = OriginalMath.Binary32(cx + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(adWidth + iconWidth) + PixelSurface.Measure(label, textSize))) * -.5f));
            if (icon != 0)
            {
                float tint = enabled ? 1f : .6f;
                s.DeckArt(icon, tx, OriginalMath.Binary32(cy + OriginalMath.Binary32(h * -.31f)), iconSize, iconSize, new Color(tint, tint, tint, 1f), true);
                tx = OriginalMath.Binary32(iconWidth + tx);
            }
            if (ad) { AdIcon(s, tx, OriginalMath.Binary32(cy + -9f), 18f); tx = OriginalMath.Binary32(adWidth + tx); }
            float ty = OriginalMath.Binary32(cy + OriginalMath.Binary32(textSize * -7f));
            if (style == Btn.Primary || style == Btn.Green) s.TextF(label, tx, ty, textSize, text, -1);
            else Label(s, label, tx, ty, textSize, text, -1);
        }

        // f54609. Caption nullness (not string emptiness) changes the hit height.
        private void RoundButton(PixelSurface s, int id, float cx, float cy, float r, int icon,
            string caption, Color ring, int dot = 0, float bob = 0f)
        {
            HitBox(s, id, OriginalMath.Binary32(cx - r), OriginalMath.Binary32(cy - r), OriginalMath.Binary32(r + r), OriginalMath.Binary32(OriginalMath.Binary32(r + r) + (caption != null ? 20f : 0f)));
            float pulse = PressPulse(id), y = OriginalMath.Binary32(cy + bob), radius = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(pulse * -.08f) + 1f)) * r);
            s.Glow(cx, y, OriginalMath.Binary32(radius * 3f), OriginalMath.Binary32(radius * 3f), A(ring, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.5f) + OriginalMath.Binary32((float)id))) * .08f) + .22f)), 0f);
            Disc(s, OriginalMath.Binary32(cx + 2f), OriginalMath.Binary32(y + 4f), radius, new Color(0f, 0f, 0f, .5f));
            Disc(s, cx, y, radius, new Color(.05f, .13f, .2f, .97f));
            float inner = OriginalMath.Binary32(radius * .72f);
            Disc(s, cx, OriginalMath.Binary32(y + OriginalMath.Binary32(radius * .25f)), inner, new Color(.02f, .07f, .12f, .9f));
            var center = new Vector2(cx, y);
            s.Ring(center, OriginalMath.Binary32(radius + -1.5f), ring, 3f, 32, 0f, 360f);
            s.Ring(center, OriginalMath.Binary32(radius + -5f), A(ring, .35f), 1f, 32, 0f, 360f);
            float angle = OriginalMath.Binary32(OriginalMath.Binary32(ui * 40f) + OriginalMath.Binary32((float)unchecked(id * 30)));
            s.Ring(center, OriginalMath.Binary32(radius + 3f), A(ring, .8f), 2f, 6, angle, 40f);
            s.Ring(center, OriginalMath.Binary32(radius + 3f), A(ring, .8f), 2f, 6, OriginalMath.Binary32(angle + 180f), 40f);
            if (icon != 0) s.DeckArt(icon, OriginalMath.Binary32(cx - OriginalMath.Binary32(radius * .66f)), OriginalMath.Binary32(y - OriginalMath.Binary32(radius * .66f)), OriginalMath.Binary32(radius * 1.32f), OriginalMath.Binary32(radius * 1.32f), Color.white, true);
            if (pulse > 0f) s.Glow(cx, y, OriginalMath.Binary32(radius + radius), OriginalMath.Binary32(radius + radius), new Color(1f, 1f, 1f, OriginalMath.Binary32(pulse * .5f)), 0f);
            if (caption != null)
            {
                // f54609 folds the shadow offset to the literal 1.035f.
                float ty = OriginalMath.Binary32(OriginalMath.Binary32(y + radius) + 2f);
                s.TextF(caption, OriginalMath.Binary32(cx + 1.035f), OriginalMath.Binary32(ty + 1.035f), 1.15f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiText.a * .7f)), 0);
                s.TextF(caption, cx, ty, 1.15f, uiText, 0);
            }
            if (dot > 0) RedDot(s, OriginalMath.Binary32(inner + cx), OriginalMath.Binary32(y - inner), dot);
        }
        // f54547.
        private void RedDot(PixelSurface s, float x, float y, int count)
        {
            float time = ui;
            s.Glow(x, y, 26f, 26f, new Color(1f, .2f, .1f, .4f), 0f);
            float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMax(OriginalMath.Sin(OriginalMath.Binary32(time * 6f)), 0f) * .12f) + 1f);
            Disc(s, x, y, OriginalMath.Binary32(pulse * 9f), Color.white, 1f);
            Disc(s, x, y, OriginalMath.Binary32(pulse * 7f), new Color(1f, .243137255f, .196078435f, 1f), 1f);
            if (count >= 2) s.TextF(count.ToString(), x, OriginalMath.Binary32(y + -7f), 1f, Color.white, 0);
        }
        // f54493.
        private void AdIcon(PixelSurface s, float x, float y, float size)
        {
            var goldFace = new Color(1f, .815686285f, .282352954f, 1f);
            s.Rect(x, OriginalMath.Binary32(y + 2f), size, OriginalMath.Binary32(size + -4f), goldFace);
            s.Rect(OriginalMath.Binary32(x + 2f), y, OriginalMath.Binary32(size + -4f), size, goldFace);
            s.Rect(OriginalMath.Binary32(x + 2f), OriginalMath.Binary32(y + 2f), OriginalMath.Binary32(size + -4f), OriginalMath.Binary32(size + -4f), new Color(.729411781f, .384313732f, .0549019612f, 1f));
            float extent = OriginalMath.Binary32(size * .42f);
            int count = Math.Abs(extent) < 2147483648f ? (int)extent : int.MinValue;
            if (count <= 0) return;
            float height = OriginalMath.Binary32(size * .5f), py = OriginalMath.Binary32(OriginalMath.Binary32(size * .25f) + y), px = OriginalMath.Binary32(OriginalMath.Binary32(size * .36f) + x);
            for (int i = 0; i != count; i++)
            {
                float t = OriginalMath.Binary32(OriginalMath.Binary32((float)i) / extent);
                s.Rect(OriginalMath.Binary32(px + OriginalMath.Binary32((float)i)), OriginalMath.Binary32(py + OriginalMath.Binary32(OriginalMath.Binary32(height * t) * .5f)), 1f, OriginalMath.Binary32(OriginalMath.Binary32(height * (OriginalMath.Binary32(1f - t))) + .5f), Color.white);
            }
        }
        // f54525: normalized bounds from f54861, no guessed icon centering.
        private void DeckIcon(PixelSurface s, int id, float cx, float cy, float box, Color tint)
        {
            if (unchecked((uint)(id - 1)) >= 16u)
            {
                float half = OriginalMath.Binary32(box * .5f);
                s.DeckArt(id, OriginalMath.Binary32(cx - half), OriginalMath.Binary32(cy - half), box, box, tint, true);
                return;
            }
            Vector4 b = DeckIconBox[id];
            float scale = OriginalMath.Binary32(box / (b.w < b.z ? b.z : b.w));
            s.DeckArt(id, OriginalMath.Binary32(cx - OriginalMath.Binary32((OriginalMath.Binary32(b.x + .5f)) * scale)), OriginalMath.Binary32(cy - OriginalMath.Binary32((OriginalMath.Binary32(b.y + .5f)) * scale)), scale, scale, tint, true);
        }
        // f54619. This counter animation belongs to Paint and uses the raw
        // unscaledDeltaTime; it must not be replaced by Update's capped delta.
        private void ResourcePill(PixelSurface s, int id, float x, float y, float w, int icon,
            int value, ref float shown, Color tint, bool plus)
        {
            float target = OriginalMath.Binary32((float)value), initial = shown;
            if (Math.Abs(OriginalMath.Binary32(initial - target)) < 1f) shown = target;
            else
            {
                float t = OriginalMath.Binary32(1f - OriginalMath.Exp(OriginalMath.Binary32(PaintDeltaTime * -9f)));
                t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
                shown = OriginalMath.Binary32(initial + OriginalMath.Binary32((OriginalMath.Binary32(target - initial)) * t));
            }
            float remaining = Math.Abs(OriginalMath.Binary32(shown - target));
            if (id > 0) HitBox(s, id, x, y, w, 40f);
            Plate(s, OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(y + 5f), OriginalMath.Binary32(w + -12f), 30f, new Color(.02f, .07f, .12f, .92f),
                new Color(.01f, .04f, .08f, .92f), A(tint, .7f), 5f, 1f);
            s.Glow(OriginalMath.Binary32(x + 18f), OriginalMath.Binary32(y + 20f), 52f, 48f, A(tint, remaining >= 1f ? .5f : .22f), 0f);
            DeckIcon(s, icon, OriginalMath.Binary32(x + 16f), OriginalMath.Binary32(y + 20f), 34f, Color.white);
            double rounded = Math.Round((double)shown, MidpointRounding.ToEven);
            int number = Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
            float right = OriginalMath.Binary32(OriginalMath.Binary32(x + w) + (plus ? -24f : -6f)), left = OriginalMath.Binary32(x + 36f);
            string label = CompactNumber(number);
            float size = PixelSurface.Measure(label, 1.5f) <= OriginalMath.Binary32(right - left) ? 1.5f : 1.1f;
            Label(s, label, OriginalMath.Binary32((OriginalMath.Binary32(left + right)) * .5f), OriginalMath.Binary32((size > 1.2f ? 11f : 13f) + y), size, remaining >= 1f ? Color.white : uiText);
            if (plus)
            {
                float px = OriginalMath.Binary32(OriginalMath.Binary32(x + w) + -22f), py = OriginalMath.Binary32(y + 10f);
                s.Rect(px, py, 18f, 20f, A(tint, .9f));
                s.Rect(OriginalMath.Binary32(px + 2f), OriginalMath.Binary32(py + 2f), 14f, 16f, new Color(.03f, .1f, .14f, 1f));
                s.Rect(OriginalMath.Binary32(px + 4f), OriginalMath.Binary32(py + 9f), 10f, 2f, tint);
                s.Rect(OriginalMath.Binary32(px + 8f), OriginalMath.Binary32(py + 5f), 2f, 10f, tint);
            }
        }
        // f54628 / f54859: keep original formatting and integer thresholds.
        private static string CompactNumber(int v) => v < 10000 ? v.ToString() : v >= 100000000 ? "9999万+" : (OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(OriginalMath.Binary32((float)v) / 1000f))) / 10f)).ToString("0.#") + "万";
        private static string Fmt(int v) => v <= 99999 ? v.ToString() : (v / 1000).ToString() + "K";
    }
}
