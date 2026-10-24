#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
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
        float railRed, railPulse, railScroll;

        // 2026-10-19 11:22 (user: warn earlier, step by step). The original danger (Battle.Danger) only starts 16 px from the
        // rail, so the rail used to go from blue to full red within about half a row. The rail now reads the real
        // distance of the front row (562 - Battle.FrontY, model px) and climbs through stages:
        //   24 px+                 calm blue
        //   24 -> 16 px            1 notice : a faint red (up to 30 %), a little faster, silent
        //   16 -> 8 px             2 warning: redder (to 70 %), a soft beat, faint edge glow; the tape fades in from ~12 px.
        //                          The original alarm sound starts here (Battle.Danger > 0 inside 16 px), unchanged.
        //   8 -> 0 px              3 danger : full red, fast heartbeat, strong edge glow
        // 11:30: measured with the model's own settling (beads drop into the 3 spare sand rows at construction): 260 of
        // the 262 levels start with the front row 26 px from the rail (5 rows at pitch 5; about 34 px on screen), the two
        // lessons about 60 px. So calm covers the opening and the first rows of advance; the first and second cuts
        // (60/35 and 34/25 px) would have opened faintly red.
        // The level eases toward its target (about 1.2 s for the full range) both ways. Sound and rules are unchanged.
        const float RailNotice = 24f, RailWarning = 16f, RailDanger = 8f;
        static float RailTarget(float dist)
        {
            if (dist >= RailNotice) return 0f;
            if (dist >= RailWarning) return .3f * (RailNotice - dist) / (RailNotice - RailWarning);
            if (dist >= RailDanger) return .3f + .4f * (RailWarning - dist) / (RailWarning - RailDanger);
            return .7f + .3f * Mathf.Clamp01((RailDanger - dist) / RailDanger);
        }
        void DrawBubbleRail(PixelSurface s)
        {
            bool live = Battle.State == Phase.Playing && Battle.ActiveFinale == null && Battle.Level.lesson <= 0;
            float front = Battle.FrontY, dist = front < 0f ? 999f : 562f - front;
            float target = live ? RailTarget(dist) : 0f;
            float dt = Time.unscaledDeltaTime;
            railRed = Mathf.MoveTowards(railRed, target, dt * .8f);
            float beatAmt = Mathf.Clamp01((railRed - .3f) / .4f);                 // 0 below stage 2, 1 from stage 3
            railPulse = .5f + .5f * Mathf.Sin(ui * (5f + 9f * railRed));          // ~0.8 Hz faint -> ~2.2 Hz full red
            float pulse = Mathf.Lerp(1f, railPulse, beatAmt);
            railScroll = Mathf.Repeat(railScroll + dt * (14.4f + 95.6f * railRed * railRed), RailPeriod);

            if (beatAmt > .01f)
            {
                float edgeAmt = Mathf.Clamp01((railRed - .3f) / .7f);
                Color side = new Color(1f, .25f, .28f, .32f * edgeAmt * railPulse);
                s.Glow(14f, 420f, 72f, 420f, side, 0f);
                s.Glow(526f, 420f, 72f, 420f, side, 0f);
            }
            if (railRed > .01f)
                s.Glow(270f, 560f, 580f, 70f, new Color(1f, .3f, .32f, .55f * railRed * (.55f + .45f * pulse)), 0f);
            // The rounded tray supplies the calm rim; only danger adds a colored lip.
            float warning = Mathf.Max(railRed, Clamp01(feedback.Hurt));
            if (warning > .01f) s.Rect(15f, 561f, 510f, 4f, new Color(1f, .43f, .53f, warning));
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
            // 11:30: fades in from 12 px (red 0.5) to full at 8 px (red 0.7), inside the warning stage.
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((railRed - .5f) / .2f));
            if (a <= .01f) return;
            float beat = .72f + .28f * railPulse;
            const float top = 509f, h = 28f;
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, top + h * .5f);
            s.MapLayout = false;
            try
            {
                GardenBarArt.Plate(s, "danger", 270f, cy, 524f, h, Color.white, a * beat);
                Color ink = new Color(1f, 1f, 1f, a), edge = new Color(.6f, .03f, .08f, a);
                GardenBarArt.TextOutline(s, "泡泡快压到护栏了！", 270f, cy - 10f, 19f, ink, edge, .7f);
                if (!GardenBars.Icon(s, "warn", 151f, cy, 24f, a * beat)) WarnSign(s, 151f, cy, a * beat);
                if (!GardenBars.Icon(s, "warn", 389f, cy, 24f, a * beat)) WarnSign(s, 389f, cy, a * beat);
            }
            finally { s.MapLayout = mapped; }
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
