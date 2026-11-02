#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-16 (user decisions): level opening in bubble dress, replacing the heavy "新队员加入" card.
    // Warning, on the original FleetOpening timeline (protected, not drawn by the bubble HUD), light like the original:
    //   0.40 D - 0.66 D  colour-coded glass with an icon and "泡泡来袭！" / difficulty; slides in and shakes.
    //   0.66 D - D       "准备出战".
    // New members (first level of each character, GardenColorCatalog.Debut): a row of name chips appears with
    // "准备出战", stays for ChipHold seconds, then each chip shrinks and flies to the mouth of the glass tube of the
    // lane holding that member (front-most occurrence, hidden or not), bursts into a small sparkle and is gone.
    // This runs on UI time, so it finishes whether or not the player has tapped to start.
    public partial class PixelWingGame
    {
        const float ChipHold = 1.5f, ChipFly = .5f, ChipStagger = .12f, ChipBurst = .45f;
        const float OpeningReadyY = 412f, DebutRowOffset = 80f;
        Flightbeat.Model.Battle debutBattle;
        float debutStart = -1f;
        int[] debutLanes = new int[0];

        static int Rgb(Color c) { Color32 k = c; return (k.r << 16) | (k.g << 8) | k.b; }

        void DrawGardenOpening(PixelSurface s)
        {
            bool intro = Battle.State == Phase.Intro;
            float duration = Battle.Level.introDuration > 0f ? Battle.Level.introDuration : 4.6f;
            float time = intro ? Battle.IntroTime : duration;
            float warningAt = duration * .4f, readyAt = duration * .66f;
            if (intro && time >= warningAt && time < readyAt) DrawOpeningWarning(s, (time - warningAt) / (readyAt - warningAt));
            // 2026-10-30 (界面优化 11, user: 「准备出战」去掉): the ready bar is no longer drawn; the debut chips still show.
            if (debutBattle != Battle) { debutBattle = Battle; debutStart = -1f; }
            if (debutStart < 0f && (!intro || time >= readyAt)) debutStart = ui;
            if (debutStart >= 0f) DrawDebutChips(s, ui - debutStart);
        }

        void DrawOpeningWarning(PixelSurface s, float t)
        {
            float alpha = Clamp01(t * 8f) * Clamp01((1f - t) * 8f);
            float slide = (1f - Clamp01(t * 4f)) * 600f, shake = t > .25f ? Mathf.Sin(ui * 60f) * 1.5f : 0f;
            const float top = 124f;
            float x0 = 270f + slide + shake;
            int difficulty = Battle.Level.difficulty;
            // 2026-10-30 (界面优化 10, after the reference): normal levels get the big 「泡泡来袭！」 wordmark (N6) popping in over
            // the empty pad row with a yellow double chevron (N7) bouncing down on each side; hard levels keep their bars.
            if (difficulty == 0 && Has("plants/uiref/incoming-title")) { DrawIncomingTitle(s, t); return; }
            // 2026-11-01 22:28 (user: the hard / super-hard bars go; use the 「泡泡来袭！」 look with its own colours): hard and
            // super-hard levels use the same pop-in wordmark and bouncing chevrons, in orange-red / purple.
            if (difficulty > 0 && Has("plants/uiref/incoming-title")) { DrawIncomingTitle(s, t, difficulty); return; }
            string text = difficulty == 2 ? "超难关卡" : difficulty == 1 ? "困难关卡" : "泡泡来袭！";
            // 2026-10-20 (UI redesign 07-09): a glass bar replaces the generic WARNING and hazard stripes: sky with a bubble cluster
            // (泡泡来袭！), gold with two stars and brown ink (困难关卡), purple with three stars (超难关卡).
            Color edge = difficulty == 2 ? GardenBars.Purple : difficulty == 1 ? GardenBars.Gold : GardenBars.Sky;
            Color ink = difficulty == 2 ? GardenBars.DeepPurple : difficulty == 1 ? GardenBars.Brown : GardenBars.Navy;
            string icon = difficulty == 2 ? "stars3" : difficulty == 1 ? "stars2" : "bubbles";
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, top + 60f);
            s.MapLayout = false;
            GardenBars.Bar(s, x0, cy, GardenBars.Width(text, 24f, 50f, icon), 50f, edge, icon, text, 24f, ink, alpha, 1f, GardenBars.Deco.One, t * 2f);
            s.MapLayout = mapped;
        }

        // difficulty 0: 「泡泡来袭！」 in gold; 1: 「困难关卡」 orange-red with rising embers; 2: 「超难关卡」 purple with two
        // flickering bolts, twinkles and the screen dimmed a little round the glow. Until the hard wordmarks are delivered
        // (HardTitle-20261101) their text is drawn in the matching colours.
        void DrawIncomingTitle(PixelSurface s, float t, int difficulty = 0)
        {
            float alpha = Clamp01(t * 6f) * Clamp01((1f - t) * 6f);
            if (alpha <= .01f) return;
            float p = OutBack(Clamp01(t * 3.2f), 2.6f), w = 380f * p, h = w * .25f;
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 600f);
            s.MapLayout = false;
            try
            {
                string key = difficulty == 2 ? "super" : "hard";
                string title = difficulty == 0 ? "plants/uiref/incoming-title" : "plants/uiref/incoming-" + key;
                string chevron = difficulty > 0 && Has("plants/uiref/incoming-chevron-" + key) ? "plants/uiref/incoming-chevron-" + key : "plants/uiref/incoming-chevron";
                Color glow = difficulty == 2 ? new Color(.67f, .27f, 1f, .42f) : difficulty == 1 ? new Color(1f, .43f, .2f, .35f) : new Color(1f, .85f, .4f, .22f);
                if (difficulty == 2) s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.16f, .04f, .31f, .16f * alpha));
                s.Glow(270f, cy, 460f, 120f, A(glow, alpha), 0f);
                if (difficulty == 2) s.Glow(270f, cy, 300f, 80f, A(new Color(.9f, .55f, 1f, .25f), alpha), 0f);
                if (difficulty == 1) IncomingEmbers(s, cy, t, alpha);
                if (difficulty == 2) IncomingBolts(s, cy, w, t, alpha);
                if (Has(title)) Asset(s, title, 270f - w * .5f, cy - h * .5f, w, h, new Color(1f, 1f, 1f, alpha));
                else
                {
                    string text = difficulty == 2 ? "超难关卡" : "困难关卡";
                    float size = h * .62f;
                    Color fill = difficulty == 2 ? Hex(0xD27BFF) : Hex(0xFF9A3C), ink = difficulty == 2 ? Hex(0x2A0B57) : Hex(0x6E1414);
                    GardenBarArt.TextOutline(s, text, 270f, cy - size * .55f, size, A(fill, alpha), A(ink, alpha), Mathf.Max(2f, size * .12f));
                }
                if (Has(chevron) && t > .12f)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float bounce = Mathf.Abs(Mathf.Sin(ui * 7f)) * 7f, c = 46f, x = 270f + side * (w * .5f + 34f);
                        Asset(s, chevron, x - c * .5f, cy - c * .5f - 4f + bounce, c, c, new Color(1f, 1f, 1f, alpha * Clamp01((t - .12f) * 8f)));
                    }
            }
            finally { s.MapLayout = mapped; }
        }
        // 困难关卡: warm sparks drifting up through the glow, each on its own loop.
        void IncomingEmbers(PixelSurface s, float cy, float t, float alpha)
        {
            for (int i = 0; i < 18; i++)
            {
                float u = Mathf.Repeat(ui * (.55f + (i % 5) * .08f) + i * .37f, 1f);
                float x = 70f + Mathf.Repeat(i * 97.3f, 400f) + Mathf.Sin(ui * 2f + i) * 8f, y = cy + 70f - u * 150f;
                float k = alpha * Mathf.Sin(u * Mathf.PI), r = 2.2f + (i % 3) * .9f;
                s.Glow(x, y, r * 5f, r * 5f, new Color(1f, .55f, .2f, .45f * k), 0f);
                s.GardenRoundedRect(x - r, y - r, r * 2f, r * 2f, r, new Color(1f, .9f, .55f, k), new Color(1f, .6f, .25f, k));
            }
        }
        // 超难关卡: a bolt strikes down each side of the wordmark - twice in quick succession, then a pause (.7 s cycle) -
        // with a faint purple flash over the screen on each strike. 2026-11-02 (user: the drawn bolts looked fake): the six
        // white frames from HardBolt-20261102 when delivered (cycled at 15 fps, a purple-tinted larger copy behind as the
        // glow, the right side three frames on); until then a tapering procedural bolt with two side branches.
        void IncomingBolts(PixelSurface s, float cy, float w, float t, float alpha)
        {
            if (t < .1f) return;
            float cyc = Mathf.Repeat(ui, .7f);
            bool strike = cyc < .1f || cyc > .16f && cyc < .26f;
            if (strike)
            {
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.85f, .6f, 1f, .07f * alpha));
                bool art = Has("plants/uiref/incoming-bolt-0");
                int frame = Mathf.FloorToInt(ui * 15f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float bx = 270f + side * (w * .5f + 64f), top = cy - 92f, bh = 176f, bw = bh * .5f;
                    if (art)
                    {
                        string id = "plants/uiref/incoming-bolt-" + ((frame + (side > 0 ? 3 : 0)) % 6);
                        Asset(s, id, bx - bw * .65f, top - bh * .08f, bw * 1.3f, bh * 1.16f, new Color(.72f, .34f, 1f, .55f * alpha));
                        Asset(s, id, bx - bw * .5f, top, bw, bh, new Color(1f, 1f, 1f, alpha));
                    }
                    else ProceduralBolt(s, bx, top, bh, frame * 7 + side * 3, side, alpha);
                }
            }
            if (!Has("scene/revive-sparkle")) return;
            for (int i = 0; i < 7; i++)
            {
                float tw = .5f + .5f * Mathf.Sin(ui * 8f + i * 1.9f), sz = (14f + (i % 3) * 6f) * tw;
                float x = 80f + Mathf.Repeat(i * 71.7f, 380f), y = cy - 60f + Mathf.Repeat(i * 37.1f, 110f);
                Asset(s, "scene/revive-sparkle", x - sz * .5f, y - sz * .5f, sz, sz, new Color(1f, 1f, 1f, alpha * tw));
            }
        }
        // Fallback bolt: eight jittered steps that thin out as they go down, two short branches, a purple glow under a white core.
        void ProceduralBolt(PixelSurface s, float x, float top, float height, int seed, int side, float alpha)
        {
            float R(int n) { float v = Mathf.Sin(n * 12.9898f + seed * 78.233f) * 43758.5453f; return v - Mathf.Floor(v); }
            Vector2 prev = new Vector2(x, top);
            for (int k = 1; k <= 8; k++)
            {
                float u = k / 8f;
                var next = new Vector2(x + (R(k) - .5f) * 34f + side * Mathf.Sin(u * 3f) * 6f, top + u * height);
                float width = Mathf.Lerp(5.5f, 1.6f, u);
                s.GlowLine(prev, next, width * 4f, new Color(.72f, .34f, 1f, .5f * alpha));
                s.GlowLine(prev, next, width, new Color(1f, 1f, 1f, alpha));
                if (k == 3 || k == 5)
                {
                    var tip = next + new Vector2(side * (14f + R(k + 20) * 16f), 16f + R(k + 30) * 12f);
                    s.GlowLine(next, tip, width * 2.6f, new Color(.72f, .34f, 1f, .35f * alpha));
                    s.GlowLine(next, tip, width * .55f, new Color(1f, 1f, 1f, .8f * alpha));
                }
                prev = next;
            }
        }
        // Lane of the front-most queued member with this color (any row, hidden mystery rows included).
        int DebutLane(int rgb)
        {
            int best = -1, bestRow = int.MaxValue;
            for (int l = 0; l < Battle.LaneCount; l++)
            {
                var reserve = Battle.Reserve[l];
                for (int r = 0; r < reserve.Count && r < bestRow; r++)
                    if (Rgb(GardenColor(reserve[r].Color)) == rgb) { best = l; bestRow = r; break; }
            }
            return best >= 0 ? best : Battle.LaneCount / 2;
        }

        // 2026-10-19 11:40 (user picked C): the debut chip is the hint glass capsule with a gold edge, a soft gold halo
        // and a twinkling four-point star on its top-right shoulder (replaces the old popup-panel jelly frame).
        void DebutCapsule(PixelSurface s, float cx, float logicalCy, float w, float h, float alpha, int k)
        {
            bool mapped = s.MapLayout;
            float cy = mapped ? PlushLayout.Y(logicalCy) : logicalCy;
            s.MapLayout = false;
            try
            {
                s.Glow(cx, cy, w + 18f, h + 20f, A(Hex(0xFFD45A), .3f * alpha), 0f);
                GlassCapsule(s, cx, cy, w, h, Hex(0xF5B83A), alpha);
                float r = Mathf.Min(7f, h * .2f), sx = cx + w * .5f - h * .22f, sy = cy - h * .5f + 1f;
                float tw = .75f + .25f * Mathf.Sin(ui * 5f + k * 1.7f);
                Color star = A(Hex(0xFFF3B0), alpha), rim = A(Hex(0xE8A21C), alpha * .9f);
                for (int pass = 0; pass < 2; pass++)
                {
                    float rr = r * tw + (pass == 0 ? 1.2f : 0f), width = pass == 0 ? 3.2f : 2f;
                    Color col = pass == 0 ? rim : star;
                    s.Segment(new Vector2(sx, sy - rr), new Vector2(sx, sy + rr), width, col);
                    s.Segment(new Vector2(sx - rr, sy), new Vector2(sx + rr, sy), width, col);
                }
                s.Rect(sx - 1.5f, sy - 1.5f, 3f, 3f, A(Color.white, alpha));
            }
            finally { s.MapLayout = mapped; }
        }

        void DrawDebutChips(PixelSurface s, float age)
        {
            int[] debut = GardenColorCatalog.Debut(levelIndex);
            int n = debut.Length;
            if (n == 0 || age > ChipHold + ChipFly + ChipStagger * n + ChipBurst) return;
            if (debutLanes.Length != n) debutLanes = new int[n];
            if (age < ChipHold) for (int k = 0; k < n; k++) debutLanes[k] = DebutLane(debut[k]);   // fixed once flying
            const float tagW = 80f, h = 36f;
            float chipW = n >= 4 ? 92f : 96f, gap = n >= 4 ? 12f : 16f;
            // Capsules and portraits keep their pixel size when the board is compressed.
            // Reserve the title's entrance overshoot plus avatar/glint clearance in that same space.
            float rowY = s.MapLayout
                ? PlushLayout.InverseY(PlushLayout.Y(OpeningReadyY) + DebutRowOffset)
                : OpeningReadyY + DebutRowOffset;
            float rowX = 270f - (tagW + n * (chipW + gap)) * .5f;
            float tagAlpha = Clamp01(age * 5f) * (1f - Clamp01((age - ChipHold) / .3f));
            if (tagAlpha > 0f)
            {
                // 2026-10-20 (UI redesign 11): the tag is a gold-edged glass capsule with brown ink, like the chips.
                bool tagMapped = s.MapLayout;
                float tagCy = GardenBars.ScreenY(s, rowY);
                s.MapLayout = false;
                GardenBars.Capsule(s, rowX + tagW * .5f, tagCy, tagW, h - 6f, GardenBars.Gold, tagAlpha);
                GardenBarArt.Text(s, "初次登场", rowX + tagW * .5f, tagCy - 8.5f, 15f, true, A(GardenBars.Brown, tagAlpha));
                s.MapLayout = tagMapped;
            }
            for (int k = 0; k < n; k++)
            {
                Color tint = new Color32((byte)(debut[k] >> 16), (byte)(debut[k] >> 8), (byte)debut[k], 255);
                Vector2 home = new Vector2(rowX + tagW + gap + k * (chipW + gap) + chipW * .5f, rowY);
                // 2026-10-17: the member dives to the BOTTOM of its glass tube and vanishes with a whoosh (stretched,
                // shrinking, with a short streak); only a faint glow in its color is left, no burst.
                Vector2 mouth = new Vector2(Battle.QueuePositionX(debutLanes[k]), 852f);
                float fly = (age - ChipHold - k * ChipStagger) / ChipFly;
                if (fly >= 1f)
                {
                    float g = (fly - 1f) * ChipFly / .25f;
                    if (g < 1f) s.Glow(mouth.x, mouth.y - 12f, 64f, 44f, A(tint, (1f - g) * .45f), 0f);
                    continue;
                }
                float pop = OutBack(Clamp01((age - k * .12f) / .25f));
                if (pop <= 0f) continue;
                float f = fly <= 0f ? 0f : fly * fly, scale = pop * Mathf.Lerp(1f, .35f, f);   // speeds up: whoosh
                Vector2 c = Vector2.Lerp(home, mouth, f) + new Vector2(0f, -Mathf.Sin(f * Mathf.PI) * 60f);   // gentle arc
                float w = chipW * scale, ch = h * scale;
                if (f < .5f)
                {
                    float panelAlpha = 1f - f * 2f;
                    DebutCapsule(s, c.x, c.y, w, ch, panelAlpha, k);
                    if (pop >= .9f && f <= 0f)
                    {
                        bool mapped = s.MapLayout;
                        float textCy = GardenBars.ScreenY(s, c.y);
                        s.MapLayout = false;
                        GardenBarArt.Text(s, Name(tint), c.x + 18f, textCy - 9f, 16f, true, Hex(0x163E6D));
                        s.MapLayout = mapped;
                    }   // 22:40: centred right of the avatar
                }
                if (pop < .9f) continue;
                // The avatar leads the flight (it is what lands in the tube); it starts at the chip's left end.
                Vector2 from = new Vector2(home.x - chipW * .5f + 24f, home.y);
                Vector2 At(float q) => Vector2.Lerp(from, mouth, q) + new Vector2(0f, -Mathf.Sin(q * Mathf.PI) * 60f);
                Vector2 a = At(f);
                float vanish = fly > .6f ? Clamp01((fly - .6f) / .4f) : 0f, stretch = Clamp01((fly - .35f) / .65f);
                float av = 44f * Mathf.Lerp(1f, .8f, f) * (1f - vanish * vanish);   // 22:40: 38 -> 44
                if (fly > .25f)
                    for (int j = 1; j <= 3; j++)
                    {
                        Vector2 b = At(Mathf.Max(0f, (fly - j * .06f) * (fly - j * .06f)));
                        s.GlowLine(b, a, Mathf.Max(6f, av * .55f), A(tint, .32f * (1f - j * .25f) * (1f - vanish * .5f)));
                    }
                if (av > .5f)
                {
                    float art = GardenSkin.ArtScale(tint);   // 2026-10-20: thin members drawn larger (GardenSkin.ArtScale)
                    float sw = av * art * (1f - .35f * stretch), sh = av * art * 256f / 224f * (1f + .7f * stretch);
                    // 2026-10-18 22:40 (user): the avatar sat high in its chip (anchored at 3/4 of the canvas); the body centre
                    // (137/256 of the 224x256 canvas, between BodyTop and FeetLine) now sits on the chip centre.
                    Asset(s, PlantSlot(tint, "idle"), a.x - sw * .5f, a.y - sh * (137f / 256f), sw, sh, Color.white);
                }
            }
        }
    }
}
#endif
