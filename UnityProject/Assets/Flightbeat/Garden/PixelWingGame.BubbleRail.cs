#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-18 (user: the guard line is hard to see; make it flow like the original and turn red on warning, but on
    // the rail itself, no extra band). The rail art is tiled to one exact 130 px period by the atlas build, so the
    // bubble beads can scroll right seamlessly. Calm: a slow drift. As the beads close in (Battle.Danger, smoothed like
    // the original's warningLevel) a red recolour of the same rail fades in, the flow speeds up and pulses, and a red
    // glow rises behind the rail and along the screen edges (as in the original warning, f54822).
    public partial class PixelWingGame
    {
        const float RailPeriod = 65f;          // 130 px of the 1080 px rail art, in logical px
        float railWarn, railRed, railPulse, railScroll;

        void DrawBubbleRail(PixelSurface s)
        {
            float target = Battle.State == Phase.Playing && Battle.ActiveFinale == null && Battle.Level.lesson <= 0 ? Battle.Danger : 0f;
            float dt = Time.unscaledDeltaTime;
            railWarn = Mathf.MoveTowards(railWarn, target, dt * 3f);
            railRed = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((railWarn - .08f) / .3f));
            railPulse = .5f + .5f * Mathf.Sin(ui * (8f + 10f * railWarn));
            railScroll = Mathf.Repeat(railScroll + dt * Mathf.Lerp(14.4f, 110f, railRed), RailPeriod);

            if (railRed > .01f)
            {
                Color side = new Color(1f, .25f, .28f, .32f * railRed * railPulse);
                s.Glow(14f, 420f, 72f, 420f, side, 0f);
                s.Glow(526f, 420f, 72f, 420f, side, 0f);
                s.Glow(270f, 560f, 580f, 70f, new Color(1f, .3f, .32f, .55f * railRed * (.55f + .45f * railPulse)), 0f);
            }
            Color hurt = Color.Lerp(Color.white, new Color(1f, .45f, .45f, 1f), Clamp01(feedback.Hurt));
            if (!RailLayer(s, "scene/fence", hurt)) { s.Rect(10, 560, 520, 4, Wood); return; }
            float redA = railRed * (.85f + .15f * railPulse);
            if (redA > .01f) RailLayer(s, "scene/fence-red", new Color(hurt.r, hurt.g, hurt.b, redA));
        }

        // The rail at y 551..573, scrolled right by railScroll: the part that slid in on the left is the previous period.
        bool RailLayer(PixelSurface s, string id, Color tint)
        {
            const float x = 0f, y = 551f, w = 540f, h = 22f;
            float off = railScroll, q = RailPeriod / w;
            if (!AssetSpan(s, id, x + off, y, w, h, 0f, 1f - off / w, tint)) return false;
            if (off > .01f) AssetSpan(s, id, x, y, w, h, q - off / w, q, tint);
            return true;
        }

        // 2026-10-18 22:21 (user: the red capsule read as a button). A flat, full-width, see-through red tape just above
        // the rail with the line and a warning sign either side; it fades in with the same smoothed danger as the rail
        // from 0.25 (the original's threshold for 敌军逼近防线) and blinks with the heartbeat.
        void DrawRailWarning(PixelSurface s)
        {
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((railWarn - .25f) / .12f));
            if (a <= .01f) return;
            float beat = .72f + .28f * railPulse;
            const float top = 509f, h = 28f;
            s.Rect(0f, top, 540f, h, new Color(.78f, .08f, .14f, .55f * a * beat));
            s.Rect(0f, top, 540f, 1.5f, new Color(1f, .62f, .62f, .8f * a));
            s.Rect(0f, top + h - 1.5f, 540f, 1.5f, new Color(1f, .62f, .62f, .8f * a));
            Color ink = new Color(1f, 1f, 1f, a), edge = new Color(.45f, .02f, .06f, a);
            TextOutline(s, "泡泡快压到护栏了！", 270f, top + 4f, 18f, ink, edge, 1.8f);
            WarnSign(s, 108f, top + h * .5f, a * beat);
            WarnSign(s, 432f, top + h * .5f, a * beat);
        }

        // A small rounded warning triangle with an exclamation mark.
        static void WarnSign(PixelSurface s, float cx, float cy, float alpha)
        {
            Color fill = new Color(1f, .86f, .3f, alpha), line = new Color(.45f, .02f, .06f, alpha);
            Vector2 a = new Vector2(cx, cy - 9f), b = new Vector2(cx + 10f, cy + 8f), c = new Vector2(cx - 10f, cy + 8f);
            for (float k = 0f; k <= 1f; k += .125f)   // filled by stacked horizontal strokes
            {
                float y = Mathf.Lerp(a.y + 1.5f, b.y - 1f, k), half = (y - a.y) / (b.y - a.y) * 10f;
                s.Segment(new Vector2(cx - half + .5f, y), new Vector2(cx + half - .5f, y), 2.6f, fill);
            }
            s.Segment(a, b, 2f, line); s.Segment(b, c, 2f, line); s.Segment(c, a, 2f, line);
            s.Rect(cx - 1f, cy - 3.5f, 2f, 6.5f, line);
            s.Rect(cx - 1f, cy + 4f, 2f, 2f, line);
        }
    }
}
#endif
