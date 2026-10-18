#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        bool TryBubbleLevelFromUrl()
        {
            if (!System.Uri.TryCreate(Application.absoluteURL, System.UriKind.Absolute, out var uri)) return false;
            foreach (string pair in uri.Query.TrimStart('?').Split('&'))
            {
                string[] parts = pair.Split('=');
                if (parts.Length != 2 || parts[0] != "level" || !int.TryParse(parts[1], out int requested)) continue;
                if (requested < 1 || requested > Levels.Length) return false;
                Economy.Data.unlocked = System.Math.Max(Economy.Data.unlocked, requested);
                if (!Economy.CanSortie) Economy.AddEnergy(1);
                return StartSortie(requested - 1);
            }
            return false;
        }
        // The level 开始 launches: the first one not yet cleared (the last one when all are cleared).
        int NextBubbleLevel()
        {
            int next = 0;
            while (next < Levels.Length && Economy.Data.cleared.Contains(next + 1)) next++;
            return next == Levels.Length ? Levels.Length - 1 : next;
        }
        void StartBubbleLevel()
        {
            int next = NextBubbleLevel();
            if (!Economy.CanSortie) Economy.AddEnergy(1);
            StartSortie(next);
        }

        // 2026-10-16 home key art (Art/R1/home-v1): background (own texture, see GardenBattlePage), five squad
        // layers on the creature atlas and the title logo. Every layer is a full 1080x1920 frame of the 9:16 core
        // picture (540x960 logical), which stays vertically centred on taller screens. Until the squad art is
        // delivered the page keeps its previous look.
        static readonly string[] HomeSquad = { "orange", "yellow", "green", "purple", "blue" };   // back to front
        static readonly float[] HomePhase = { 0f, 1.9f, 3.7f, 5.1f, 2.6f };
        // Orange and yellow float above the squad (no ground under them): bigger drift, no contact shadow.
        static readonly bool[] HomeFloating = { true, true, false, false, false };
        const float HomeSquadScale = .93f;                  // clears the 150 px side bands (user decision 17:58)
        const float HomePivotX = 270f, HomePivotY = 600f;   // scale about the front cloud line
        bool HomeArtReady => Has("plants/home/squad-blue");

        // One mapping for the background texture and the layers drawn over it. The source is 540x1170 logical
        // (core picture at y 105-1065). It covers the screen at scale k >= 1; the core stays vertically centred
        // when possible. A core point (x, y) lands on screen at (x0 + x * k, y0 + y * k).
        internal static Rect HomeArtView(out float k, out float x0, out float y0)
        {
            float w = ScreenLayout.Width, h = ScreenLayout.Height;
            k = Mathf.Max(1f, w / 540f, h / 1170f);
            float vw = w / k, vh = h / k, left = (540f - vw) * .5f;
            float top = Mathf.Clamp(105f - (h - 960f) * .5f / k, 0f, 1170f - vh);
            x0 = -left * k; y0 = (105f - top) * k;
            return new Rect(left / 540f, 1f - (top + vh) / 1170f, vw / 540f, vh / 1170f);   // texture uvRect
        }

        // 2026-10-17: the start button reads only 「第  N  关」 (two spaces either side since 11:10), large.
        int homeLabelLevel = -1;
        string homeLabel = "";
        void HomeStartButton(PixelSurface s, float y)
        {
            int level = NextBubbleLevel() + 1;
            if (level != homeLabelLevel) { homeLabelLevel = level; homeLabel = "第  " + level + "  关"; }
            GardenButton(s, 800, 100, y, 340, 76, "");
            TextOutline(s, homeLabel, 270f, y + (76f - 32f) * .5f - 2f, 32f, Cream, Outline, 2.4f);
        }

        // ---- squad liveliness (2026-10-17)
        const int HomeMemberAction = 830;   // 830 + squad index
        static readonly int[] HomeSquadColor = { 0xFF7E27, 0xFFDF48, 0x62E18E, 0xBD80FF, 0x2982F4 };
        static readonly int[] HomeHopOrder = { 4, 1, 2, 0, 3 };   // blue, yellow, green, orange, purple
        const float HomeHopEvery = 2.6f, HomeHopTime = 1f;
        readonly float[] homePokeAt = { -9f, -9f, -9f, -9f, -9f };
        // Seconds into the hop of member i after a tap, or -1 when it is resting.
        float HomeActionTime(int i) => -1f;   // taps blow bubbles now (see PokeHomeMember)

        // 2026-10-17 (user decision 10:47): instead of hopping in turn, the member whose turn it is (every 2.6 s)
        // breathes in and blows a big soap bubble from its mouth; the bubble swells, lets go, sways upward and
        // pops after ~2.9 s. Yellow already has a bubble at its mouth in the art: that bubble swells a little and
        // a new one floats off from the same spot. Points are in the 1080x1920 squad frame.
        static readonly Vector2[] HomeMouth = {
            new Vector2(406f, 750f), new Vector2(884f, 718f), new Vector2(320f, 1064f), new Vector2(807f, 1099f), new Vector2(562f, 1062f) };
        static readonly float[] HomeBodyX = { 381f, 756f, 272f, 813f, 547f };
        const float HomeYellowBubble = 74f;   // radius of the bubble in the yellow layer (frame px)
        const float BlowGrow = .6f, BlowPop = 2.9f, BlowEnd = 3.3f;
        // Breath-in puff of the member whose turn it is (positive = wider/shorter).
        float HomeBlowPuff(int i)
        {
            float poke = ui - homePokeAt[i];
            if (poke >= 0f && poke < BlowGrow) return .045f * Mathf.Sin(poke / BlowGrow * Mathf.PI);
            int slot = Mathf.FloorToInt(ui / HomeHopEvery);
            if (HomeHopOrder[((slot % 5) + 5) % 5] != i) return 0f;
            float t = ui - slot * HomeHopEvery;
            return t < BlowGrow ? .045f * Mathf.Sin(t / BlowGrow * Mathf.PI) : 0f;
        }
        void DrawHomeBlownBubbles(PixelSurface s, float fx, float fy, float fw, float fh, float k)
        {
            int now = Mathf.FloorToInt(ui / HomeHopEvery);
            float scale = fw / 1080f;   // frame px -> screen
            // Scheduled blows (the last two turns) plus one blow per tapped member.
            for (int n = 0; n < 2 + homePokeAt.Length; n++)
            {
                int slot = now - 1 + n, i;
                float t;
                if (n < 2) { t = ui - slot * HomeHopEvery; i = HomeHopOrder[((slot % 5) + 5) % 5]; }
                else { i = n - 2; t = ui - homePokeAt[i]; slot = 1000 + i * 37 + Mathf.FloorToInt(homePokeAt[i] * 10f); }
                float seed = ((slot * 7919) % 97 + 97) % 97 / 97f;
                // 15:55 (user): each bubble pops at its own height: 1.0-3.6 s after it lets go (was always 2.3 s).
                float seed2 = ((slot * 104729) % 89 + 89) % 89 / 89f;
                float popAt = BlowGrow + 1f + 2.6f * seed2, endAt = popAt + (BlowEnd - BlowPop);
                if (t < 0f || t >= endAt) continue;
                Vector2 mouth = new Vector2(fx + HomeMouth[i].x * scale, fy + HomeMouth[i].y / 1920f * fh);
                bool yellow = i == 1;
                float full = yellow ? HomeYellowBubble * 1.2f * scale : (30f + 12f * seed) * k;   // radius: 30-42 (logical)
                float r;
                Vector2 c;
                Vector2 dir = new Vector2(Mathf.Sign(HomeMouth[i].x - HomeBodyX[i]) * .55f, -1f).normalized;
                if (t < BlowGrow)
                {
                    float g = 1f - (1f - t / BlowGrow) * (1f - t / BlowGrow);
                    r = yellow ? Mathf.Lerp(HomeYellowBubble * scale, full, g) : full * g;
                    c = yellow ? mouth : mouth + dir * r * .9f;
                }
                else
                {
                    float u = t - BlowGrow;
                    r = full;
                    Vector2 start = yellow ? mouth : mouth + dir * r * .9f;
                    c = start + new Vector2(Mathf.Sin(u * 2.2f + seed * 6f) * 10f * k + dir.x * 14f * u * k, -(u * 52f + u * u * 7f) * k);
                }
                if (t < popAt)
                {
                    float wob = t < BlowGrow ? 0f : Mathf.Sin((t - BlowGrow) * 6f + seed * 3f) * .045f;
                    float d = r * 2f;
                    if (!Asset(s, "plants/home/soap-bubble", c.x - d * (1f + wob) * .5f, c.y - d * (1f - wob) * .5f, d * (1f + wob), d * (1f - wob), new Color(1f, 1f, 1f, .95f)))
                        GardenSkin.Shot(s, Hex(0xCFEAFF), c, d, .6f);
                }
                else
                {
                    float p = (t - popAt) / (BlowEnd - BlowPop);
                    if (Has("plants/home/soap-pop/0"))
                    {
                        int frame = 0;
                        while (Has("plants/home/soap-pop/" + (frame + 1))) frame++;
                        int f = Mathf.Min(frame, (int)(p * (frame + 1)));
                        Asset(s, "plants/home/soap-pop/" + f, c.x - r * 1.3f, c.y - r * 1.3f, r * 2.6f, r * 2.6f, Color.white);
                    }
                    else GardenFx.Burst(s, "rocket-pop", c, r * 2.6f, t - popAt, BlowEnd - BlowPop);
                }
            }
        }
        // Crouch, jump (stretched), land (squash and two small rebounds). Floating members do a soft loop instead.
        static void HomeHop(float t, bool floating, out float lift, out float squash)
        {
            lift = 0f; squash = 0f;
            if (floating)
            {
                float u = Mathf.Clamp01(t / .8f);
                lift = 16f * Mathf.Sin(u * Mathf.PI);
                squash = u < .5f ? -.07f * Mathf.Sin(u * 2f * Mathf.PI) : .06f * Mathf.Sin((u - .5f) * 2f * Mathf.PI);
                return;
            }
            if (t < .12f) { squash = .14f * (t / .12f); return; }
            if (t < .52f) { float u = (t - .12f) / .4f; lift = 22f * Mathf.Sin(u * Mathf.PI); squash = -.1f * Mathf.Sin(u * Mathf.PI); return; }
            float r = t - .52f;
            squash = .15f * Mathf.Exp(-r * 9f) * Mathf.Cos(r * 26f);
        }
        // 2026-10-17 11:10: tapping a member makes it blow a bubble (no hop, no burst).
        void PokeHomeMember(int i)
        {
            if (i < 0 || i >= homePokeAt.Length || ui - homePokeAt[i] < .7f) return;
            homePokeAt[i] = ui;
            audioChip.Play("ui");
        }
        void DrawBubbleHome(PixelSurface s)
        {
            if (!HomeArtReady)
            {
                // Logo (H10) arrived before the squad layers: it replaces the program title on the old page.
                s.MapLayout = true;
                const float bob = 0f;   // still logo (2026-10-16)
                if (!Asset(s, "plants/home/logo", 55f, 152f + bob, 430f, 172f, Color.white))
                    TextOutline(s, "泡泡小队", 270, 238, 48, Cream, Outline, 3f);
                HomeStartButton(s, 530f);
                DrawHomeNextMember(s, 624f);
                DrawHomeCornerButtons(s);   // 2026-10-16 22:35: settings moved to the top-left, codex top-right
                DrawBubbleCodex(s);
                return;
            }
            s.MapLayout = false;
            HomeArtView(out float k, out float ox, out float oy);
            // Title logo in the title band (y 75-210 of the core). 2026-10-16: still (the floating read as dizzy).
            if (!Asset(s, "plants/home/logo", ox + 55f * k, oy + 64f * k, 430f * k, 172f * k, Color.white))
                TextOutline(s, "泡泡小队", ox + 270f * k, oy + 112f * k, 48f, Cream, Outline, 3f);
            // Squad, scaled about the cloud line. 2026-10-16: continuous out-of-step bobbing read as dizzy, so the
            // members only breathe; 2026-10-17 (user: "not lively"): one member at a time does a short hop with
            // squash and stretch, rotating through the squad; 10:47: replaced by blowing soap bubbles (see
            // DrawHomeBlownBubbles); tapping a member still makes it hop.
            float fw = 540f * HomeSquadScale * k, fh = 960f * HomeSquadScale * k;
            float fx = ox + (HomePivotX - HomePivotX * HomeSquadScale) * k, fy = oy + (HomePivotY - HomePivotY * HomeSquadScale) * k;
            for (int i = 0; i < HomeSquad.Length; i++)
            {
                string id = "plants/home/squad-" + HomeSquad[i];
                bool floating = HomeFloating[i];
                float phase = HomePhase[i];
                string art = id;
                float cycle = 3.2f + phase % 1.8f;
                if ((ui + phase * 1.37f) % cycle < .13f && Has(id + "-blink")) art = id + "-blink";
                if (!Content(id, out Rect c)) { Asset(s, art, fx, fy, fw, fh, Color.white); continue; }
                float cx = fx + (c.x + c.width * .5f) * fw, bottom = fy + (c.y + c.height) * fh, head = fy + c.y * fh;
                float act = HomeActionTime(i), lift = 0f, squash = HomeBlowPuff(i);
                if (act >= 0f) HomeHop(act, floating, out lift, out squash);
                float breathe = floating ? 1f : 1f + Mathf.Sin(ui * 2.1f + phase) * .015f;
                float drift = floating ? (Mathf.Sin(ui * 1.2f) * .5f + .5f) * 3f : 0f;
                if (!floating)
                {
                    // Contact shadow: shrinks and fades while the member is in the air.
                    float sw = c.width * fw * .62f * (1f - lift * .012f);
                    s.Glow(cx, bottom - 4f * k, sw, sw * .22f, new Color(.16f, .28f, .5f, .24f * (1f - lift * .02f)), 0f);
                }
                // Feet-anchored squash/stretch, then lifted.
                float sy = breathe * (1f - squash), sx = 1f + squash;
                float w = fw * sx, h = fh * sy, x = cx - (cx - fx) * sx, y = bottom - (bottom - fy) * sy - (lift + drift) * k;
                Asset(s, art, x, y, w, h, Color.white);
                // Hit area: the member's visible body.
                HitBox(s, HomeMemberAction + i, fx + c.x * fw, head - lift * k, c.width * fw, c.height * fh);
            }
            DrawHomeBlownBubbles(s, fx, fy, fw, fh, k);
            // Buttons on the cloud sea, about 20 px (10 logical) below the old plan to clear the platform's edge.
            // 2026-10-16 22:35: only 开始 stays on the cloud sea; settings and the codex sit in the top corners.
            s.MapLayout = true;
            HomeStartButton(s, 752f);
            DrawHomeNextMember(s, 846f);
            DrawHomeCornerButtons(s);
            DrawBubbleCodex(s);
        }
    }
}
#endif
