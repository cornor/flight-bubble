#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // 2026-10-16 (user item 6): drawing-only geometry supplied by the game. Shots leave from the member's
        // mouth (the original muzzle points belong to the old hulls: some started at the mouth, others lower) and
        // land where the bead is drawn (the advance is smoothed for display, the model moves in pulses).
        public Func<int, Vector2> GardenMouth, GardenEnemyAt;
        public float GardenOffsetDelta;
        public bool GardenComboVisible => comboFade > .01f && !HideCombo;
        int gardenPreviousCombo;
        // Called by the R1 UI refresh after the original feedback tick. Read its sampled count so pause,
        // expiry and the counter use the same frame; the original tick still advances the animation age.
        // Returns true when a 1500 / 2000 / ... milestone fired this frame (the model only cues 1000-steps), so the
        // game can play the milestone sound for it.
        public bool RefreshGardenComboMilestone()
        {
            // 2026-10-21 11:51 (user): after 2000 the show comes only every 1000 (1500, 2000, 3000, 4000 ...).
            bool fired = false;
            int Step(int c) => c >= 2000 ? c / 1000 * 1000 : c >= 1500 ? 1500 : 0;
            int m = Step(lastCombo);
            if (m > 0 && m > Step(gardenPreviousCombo))
            {
                milestoneValue = m;
                comboMilestoneAge = 0f;
                fired = milestoneValue % 1000 != 0;   // 2000, 3000 ... are already cued by Battle.Hits
            }
            gardenPreviousCombo = lastCombo; // A broken chain can earn the same milestones again.
            return fired;
        }

        // 4000–8000 each have their own praise; 9000 starts the same five-title cycle again.
        // Narrow "!" avoids the full-width punctuation's blank side. Milestone timing and FX stay unchanged.
        static readonly string[] gardenPraiseTop = { "全场沸腾!", "巅峰时刻!", "超越极限!", "奇迹降临!", "宇宙最强!" };
        static string GardenPraise(int value)
        {
            switch (value)
            {
                case 50: return "漂亮!";
                case 100: return "太棒了!";
                case 250: return "超厉害!";
                case 500: return "神操作!";
                case 1000: return "泡泡之神!";
                case 1500: return "势不可挡!";
                case 2000: return "无人能敌!";
                case 2500: return "逆天啦!";
                case 3000: return "传说诞生!";
                default: return value >= 4000 ? gardenPraiseTop[(value / 1000 - 4) % gardenPraiseTop.Length] : "漂亮!";
            }
        }
        // 2026-10-21 10:39 (user: cleared beads should burst, for the thrill; this lifts the 2026-10-14 "no hit FX").
        // Every bead the model resolves raises Battle.Hit once; it pops where it was drawn: a short white flash, a ring in
        // a lighter tint of its colour that opens and thins, and five tiny beads flung out with a little gravity, .32 s.
        // Display only (the event is read, the model is untouched). At most 64 at once; the oldest give way.
        // 2026-10-21 17:24 (改动清单 3.5, user): at high combos the overlapping foam formed a white haze across the
        // board right on the row being shot; now at most 32 at once, smaller, closer to the bead colour, softer flash.
        // 2026-10-21 21:03 (user: with one member shooting there is no feeling of a bubble breaking): each pop got a
        // richness from how busy the board is (hits in the last .4 s). 10-22 18:30 (user: it shattered into crumbs, not
        // a bubble popping): the falling film shards and solid droplets are gone. Now a soap-bubble "pop", .22 s: the
        // bead swells to 1.3x with a soft wobble, vanishes, and a thin light ring of its colour flicks open and fades;
        // sparse hits add 4 short comic "pop" strokes; a few tiny soft mist dots drift out and fade (no gravity).
        struct GardenPop { public Vector2 at; public float born, rich; public int color, seed; public bool lite; }
        readonly System.Collections.Generic.List<GardenPop> gardenPops = new System.Collections.Generic.List<GardenPop>();
        readonly System.Collections.Generic.Queue<float> gardenHitTimes = new System.Collections.Generic.Queue<float>();
        bool gardenPopsHooked;
        int gardenPopSeed;
        // Ordinary tiers and MAX's splash attack share one visual admission path.
        // Copy values at impact: the model releases/reuses Shot immediately afterwards.
        const float GardenAttackPopLife = .4f;
        const float GardenAttackPopMergeWindow = .12f;
        const int GardenAttackPopLimit = 32;
        struct GardenAttackPop { public Vector2 at; public float born, size; }
        readonly System.Collections.Generic.List<GardenAttackPop> gardenAttackPops =
            new System.Collections.Generic.List<GardenAttackPop>(GardenAttackPopLimit);
        static float GardenAttackPopSize(int tier) => tier >= 3 ? 120f : tier == 2 ? 60f : tier == 1 ? 40f : 25f;
        void GardenOnImpact(Shot shot)
        {
            // MAX's main shot retains its small bead feedback; only its splash gets the large clip.
            if (!shot.Rocket && shot.Tier >= 3) return;
            Vector2 at = shot.Rocket ? ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY)
                : EnemyShown(shot.TargetIndex, ShownPoint(shot.TargetX, shot.TargetY));
            float size = GardenAttackPopSize(shot.Rocket ? 3 : shot.Tier);
            Vector2 shown = PlushLayout.Point(at);
            for (int i = gardenAttackPops.Count - 1; i >= 0; i--)
            {
                var pop = gardenAttackPops[i];
                float age = time - pop.born;
                if (age < 0f || age >= GardenAttackPopLife) { gardenAttackPops.RemoveAt(i); continue; }
                if (age >= GardenAttackPopMergeWindow) continue;
                // Group adjacent beads, with a wider neighborhood for large bursts.
                float radius = Mathf.Max(24f, Mathf.Min(size, pop.size) * .5f);
                if ((shown - PlushLayout.Point(pop.at)).sqrMagnitude > radius * radius) continue;
                // A stronger impact can take over the clip, without restarting or moving it.
                pop.size = Mathf.Max(size, pop.size);
                gardenAttackPops[i] = pop;
                TrimGardenAttackPops();
                return;
            }
            gardenAttackPops.Add(new GardenAttackPop { at = at, born = time, size = size });
            TrimGardenAttackPops();
        }
        void TrimGardenAttackPops()
        {
            // Retain the original 12-MAX limit as well as the shared visual budget.
            int rockets = 0;
            for (int i = gardenAttackPops.Count - 1; i >= 0; i--)
                if (gardenAttackPops[i].size >= GardenAttackPopSize(3) && ++rockets > 12) gardenAttackPops.RemoveAt(i);
            while (gardenAttackPops.Count > GardenAttackPopLimit) gardenAttackPops.RemoveAt(0);
        }
        void DrawGardenAttackPops(PixelSurface s)
        {
            bool hasArt = GardenFx.Has("rocket-pop");
            for (int i = gardenAttackPops.Count - 1; i >= 0; i--)
            {
                var pop = gardenAttackPops[i];
                float age = time - pop.born;
                if (age >= GardenAttackPopLife || age < 0f) { gardenAttackPops.RemoveAt(i); continue; }
                if (hasArt) GardenFx.Burst(s, "rocket-pop", pop.at, pop.size, age, GardenAttackPopLife);
            }
        }
        void GardenOnHit(int color, float x, float y) {
            int hits = 0;
            foreach (var q in gardenPops) if (!q.lite) hits++;
            if (hits >= 32) { for (int i = 0; i < gardenPops.Count; i++) if (!gardenPops[i].lite) { gardenPops.RemoveAt(i); break; } }
            while (gardenHitTimes.Count > 0 && (time - gardenHitTimes.Peek() > .4f || gardenHitTimes.Peek() > time)) gardenHitTimes.Dequeue();
            gardenHitTimes.Enqueue(time);
            // 1 up to ~5 hits in .4 s (one or two members), fading to 0 by ~16 (a full row firing / combo storm).
            float rich = Mathf.Clamp01((16f - gardenHitTimes.Count) / 11f);
            gardenPops.Add(new GardenPop { at = ShownPoint(x, y), born = time, color = color, seed = gardenPopSeed++, rich = rich });
        }
        static float GardenHash(int n) { float v = Mathf.Sin(n * 12.9898f) * 43758.5453f; return v - Mathf.Floor(v); }
        public float GardenBeadSize = 11f;   // set by the game from the drawn bead size
        void DrawGardenPops(PixelSurface s, Func<int, Color> color) {
            const float life = .22f, swell = .05f;
            float bead = GardenBeadSize;
            for (int i = gardenPops.Count - 1; i >= 0; i--) {
                GardenPop p = gardenPops[i];
                float r = p.lite ? 0f : p.rich, age = time - p.born;
                if (age >= life || age < -1f) { gardenPops.RemoveAt(i); continue; }
                if (age < 0f) continue;
                Color tint = color(p.color);
                // 1. Swell with a soft wobble, then gone.
                if (age < swell) {
                    float k = age / swell, grow = 1f + .3f * Mathf.Sin(k * Mathf.PI * .5f), wob = .08f * Mathf.Sin(k * Mathf.PI * 2f);
                    Color c = tint; c.a = 1f - .25f * k;
                    GardenSkin.Bug(s, c, p.at, bead * grow * (1f - wob), false, bead * grow * (1f + wob));
                    continue;
                }
                float u = (age - swell) / (life - swell), e = 1f - (1f - u) * (1f - u) * (1f - u), fade = 1f - u;
                Color skin = Color.Lerp(tint, Color.white, .45f);
                // 2. The thin film ring flicks open and fades.
                Color ring = skin; ring.a = .9f * fade * fade;
                s.Ring(p.at, bead * (.6f + .6f * e), ring, .5f + 1.3f * fade, 16, 0f, 360f);
                // 3. Sparse hits: four short "pop" strokes just outside the ring.
                if (r > .3f) {
                    for (int k = 0; k < 4; k++) {
                        float ang = (45f + k * 90f + (GardenHash(p.seed * 3 + k) - .5f) * 30f) * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        float r0 = bead * (.85f + .45f * e), len = bead * .38f * fade;
                        Color line = skin; line.a = r * fade;
                        s.Segment(p.at + dir * r0, p.at + dir * (r0 + len), 1.4f, line);
                    }
                }
                // 4. A few tiny soft mist dots drift out and fade (no gravity, no solid crumbs).
                int n = p.lite ? 3 : 4 + Mathf.RoundToInt(r);
                for (int k = 0; k < n; k++) {
                    float ang = (k / (float)n + GardenHash(p.seed * 5 + k) * .2f) * Mathf.PI * 2f;
                    float reach = bead * (.55f + (.7f + .3f * GardenHash(p.seed * 7 + k)) * e);
                    Vector2 q = p.at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * reach;
                    Color mist = skin; mist.a = .75f * fade;
                    float d = bead * (.32f - .14f * u);
                    s.Glow(q.x, q.y, d, d, mist, 0f);
                }
            }
        }
        // ---- Bubble bomb (2026-10-21 10:57) ------------------------------------------------------------------------
        // Fall (the model's 1 s countdown): the rainbow bomb hangs a moment above the field with its fuse fizzing, then
        // drops faster and faster onto the aim point, spinning, with ghost images and a shadow growing under it.
        // Detonation (superAge): .05 s hold on a squashed bomb under a white flash (a felt hit-stop), the board shakes
        // (GardenStrikeShake), a rainbow soap bubble blows up to the blast radius and bursts at .42 s into a thick
        // white shock ring and a spray of film bubbles; the cleared beads pop outward from the centre in a ripple;
        // a few bubbles drift up to finish. Drawing only: target, radius and timing are the model's.
        object gardenBlastSeen;
        readonly System.Collections.Generic.List<Vector3> gardenStrikeCells = new System.Collections.Generic.List<Vector3>();
        void GardenStrikeSnapshot(SuperStrike strike) {
            gardenStrikeCells.Clear();
            for (int i = 0; i < battle.Alive.Length; i++) {
                if (!battle.Alive[i] || !battle.OnBoard(i)) continue;
                float dx = (battle.EnemyX(i) - strike.X) / strike.RadiusX, dy = (battle.EnemyY(i) - strike.Y) / strike.RadiusY;
                float d = dx * dx + dy * dy;
                if (d <= 1f) gardenStrikeCells.Add(new Vector3(i, battle.ColorAt(i), Mathf.Sqrt(d)));
            }
        }
        void GardenStrikePops(SuperStrike strike) {
            int n = gardenStrikeCells.Count, keep = 260, stride = Mathf.Max(1, Mathf.CeilToInt(n / (float)keep));
            gardenPops.RemoveAll(p => p.lite);
            for (int k = 0; k < n; k += stride) {
                Vector3 c = gardenStrikeCells[k]; int i = (int)c.x;
                gardenPops.Add(new GardenPop { at = ShownPoint(battle.EnemyX(i), battle.EnemyY(i)), born = time + .05f + .3f * c.z,
                    color = (int)c.y, seed = gardenPopSeed++, lite = true });
            }
            gardenStrikeCells.Clear();
        }
        // Board shake after a detonation (applied to the beads and pads by the game), in px.
        public float GardenStrikeShake {
            get {
                if (lastStrike == null || superAge < .04f || superAge > .45f) return 0f;
                float u = (superAge - .04f) / .41f;
                return Mathf.Sin(superAge * 95f) * 9f * (1f - u) * (1f - u);
            }
        }
        static readonly Color[] gardenRainbow = {
            new Color(1f, .35f, .4f), new Color(1f, .62f, .25f), new Color(1f, .9f, .3f),
            new Color(.4f, .9f, .45f), new Color(.3f, .7f, 1f), new Color(.65f, .45f, 1f) };
        void GardenBombFall(PixelSurface s, Vector2 at, float t) {
            float u = Mathf.Clamp01((t - .22f) / .78f), e = u * u * u;            // hang, then a hard accelerating drop
            float startY = Mathf.Min(40f, at.y - 170f), size = 60f;
            float wobble = u <= 0f ? Mathf.Sin(t * 40f) * 6f : 0f, appear = Mathf.Clamp01(t / .12f);
            Vector2 p = new Vector2(at.x, Mathf.Lerp(startY, at.y - 6f, e) - (1f - appear) * 20f);
            // 11:51 (user): no tumbling on the way down; it keeps a slight tilt and only sways while hanging.
            float spin = -12f + wobble, stretch = 1f + .18f * e;
            s.Glow(at.x, at.y + 6f, 26f + 50f * e, 12f + 22f * e, new Color(.12f, .05f, .3f, .1f + .3f * e), 0f);   // shadow
            if (e > .05f)
                for (int g = 3; g >= 1; g--) {
                    float eg = Mathf.Max(0f, Mathf.Pow(Mathf.Max(0f, u - g * .05f), 3f));
                    Vector2 q = new Vector2(at.x, Mathf.Lerp(startY, at.y - 6f, eg));
                    GardenSkin.AssetRotated(s, "ui/bar-icon-bomb", q, size, size * stretch, spin, new Color(1f, 1f, 1f, .22f * appear / g));
                }
            if (!GardenSkin.AssetRotated(s, "ui/bar-icon-bomb", p, size * appear, size * stretch * appear, spin, Color.white))
                GardenSkin.Shot(s, new Color(.5f, .3f, .9f), p, size * .8f, 1f);
            // Fuse sparks from the tip (top right of the art), turning with the bomb.
            float r = spin * Mathf.Deg2Rad;
            Vector2 tip = p + new Vector2(.36f * size * Mathf.Cos(r) + .42f * size * Mathf.Sin(r), .36f * size * Mathf.Sin(r) - .42f * size * Mathf.Cos(r)) * appear;
            s.Glow(tip.x, tip.y, 26f, 26f, new Color(1f, .85f, .35f, .7f * appear), 0f);
            for (int k = 0; k < 5; k++) {
                float cyc = time * 3.4f + k * .2f, v = cyc - Mathf.Floor(cyc), seed = Mathf.Floor(cyc) * 7.1f + k * 3.3f;
                float a = GardenHash((int)(seed * 13f)) * Mathf.PI * 2f, reach = 4f + 16f * v;
                Vector2 q = tip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach;
                s.Glow(q.x, q.y, 7f * (1f - v) + 2f, 7f * (1f - v) + 2f, new Color(1f, .95f - .4f * v, .5f, (1f - v) * appear), 0f);
            }
        }
        void GardenBombBlast(PixelSurface s, Vector2 at, float radius, float a) {
            if (a > 1.7f) return;
            float R = Mathf.Max(60f, radius);
            if (a < .05f) GardenSkin.AssetRotated(s, "ui/bar-icon-bomb", at + new Vector2(0f, 6f), 60f * 1.3f, 60f * .7f, 20f, Color.white);
            if (a < .12f) {
                bool mapped = s.MapLayout; s.MapLayout = false;
                s.Rect(0f, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(1f, 1f, 1f, .45f * (1f - a / .12f)));
                s.MapLayout = mapped;
            }
            if (a < .3f) s.Glow(at.x, at.y, R * 2.4f, R * 2.4f, new Color(1f, .97f, .85f, .6f * (1f - a / .3f)), 0f);
            // 11:09 (user: it looked program-drawn). Painted sequences only: the purple soap bubble bursting into a
            // foam ring (water-splash) across the blast, the bright bubble pop with its star flash (rocket-pop) at the
            // core, and a chain of foam bursts (impact-dust) going off around the ring one after another.
            bool art = GardenFx.Has("water-splash") && GardenFx.Has("rocket-pop") && GardenFx.Has("impact-dust");
            if (art) {
                // The painted front grows out from the impact, rather than playing in a fixed-size box.
                float wave = Mathf.Clamp01((a - .05f) / .55f);
                float spread = 1f - Mathf.Pow(1f - wave, 3f);
                GardenFx.Burst(s, "water-splash", at, R * Mathf.Lerp(.55f, 3f, spread), a - .05f, .8f);
                GardenFx.Burst(s, "rocket-pop", at, R * 1.5f, a - .02f, .42f);
                for (int k = 0; k < 7; k++) {
                    float dustAge = a - (.1f + .025f * k), t = Mathf.Clamp01(dustAge / .5f);
                    float travel = 1f - (1f - t) * (1f - t);
                    float ang = (k / 7f + GardenHash(k * 19 + 3) * .08f) * Mathf.PI * 2f;
                    float d = R * Mathf.Lerp(.14f, .95f + .25f * GardenHash(k * 7 + 1), travel);
                    Vector2 q = at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * .8f) * d;
                    float size = R * (.42f + .15f * GardenHash(k * 5 + 2)) * (1f - .3f * t);
                    GardenFx.BurstTint(s, "impact-dust", q, size, dustAge, .5f, new Color(1f, 1f, 1f, 1f - .45f * t));
                }
                // Afterglow: a few whole bubbles (the first frame of the pop) drift up and fade.
                if (a >= .55f)
                    for (int k = 0; k < 7; k++) {
                        float v = (a - .55f - k * .05f) / 1.1f;
                        if (v <= 0f || v >= 1f) continue;
                        float x = at.x + (GardenHash(k * 11 + 1) - .5f) * R * 1.5f + Mathf.Sin(a * 3f + k) * 6f;
                        float y = at.y + (GardenHash(k * 5 + 2) - .3f) * R * .5f - 80f * v, sz = 12f + 10f * GardenHash(k * 3 + 9);
                        GardenFx.Draw(s, "rocket-pop", new Vector2(x, y), sz, sz, new Color(1f, 1f, 1f, .85f * Mathf.Sin(v * Mathf.PI)), 0);
                    }
                return;
            }
            if (a >= .05f && a < .6f) {
                float q = (a - .05f) / .55f;
                s.Ring(at, R * (.3f + 1.2f * (1f - (1f - q) * (1f - q))), new Color(1f, 1f, 1f, 1f - q), 1f + 10f * (1f - q), 48, 0f, 360f);
            }
        }
        // ---- Combo milestone show (2026-10-21 11:31, user approved the local, levelled mock-up) ----------------------
        // A local show around the words (about 380 x 220 at the old popup's place, y 230), never full screen. Nine levels by
        // the milestone; each adds to everything below it:
        //   50 pop-in words + sparkles · 100 + a bubble popping behind · 250 + light sweep, small rays ·
        //   500 + letters slam in one by one, white core flash, the words judder · 1000 + navy backing glow, impact
        //   lines, sparkle ring · 1500 + a second ring, confetti · 2000 + the words burst into foam on exit, thicker
        //   sides · 2500 + bubbles keep rising, counter-turning second rays · 3000+ rainbow words and rays.
        // Size 40 -> 66 and stay 1.0 -> 2.2 s grow with the level; colours follow the old tiers (gold 50/100, orange
        // 250/500, purple 1000+, rainbow 3000+). All drawing is deterministic in comboMilestoneAge.
        static readonly float[] showDur = { 1f, 1.1f, 1.3f, 1.5f, 1.8f, 1.9f, 2f, 2.1f, 2.2f };
        static readonly float[] showSize = { 40f, 44f, 48f, 52f, 58f, 60f, 62f, 64f, 66f };
        static readonly Color showNavy = GardenSkin.Hex(0x1D2F63);
        static Color ShowA(Color c, float a) { c.a *= a; return c; }
        static float ShowBack(float x) { const float c = 1.9f; return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3f) + c * Mathf.Pow(x - 1f, 2f); }
        void DrawGardenComboShow(PixelSurface s) {
            int v = milestoneValue;
            int L = v >= 3000 ? 8 : v >= 2000 ? 6 : v >= 1500 ? 5 : v >= 1000 ? 4 : v >= 500 ? 3 : v >= 250 ? 2 : v >= 100 ? 1 : 0;
            float t = comboMilestoneAge, dur = showDur[L], size = showSize[L], exitT = dur - .3f;
            if (t > dur + .6f) return;
            // 11:51 (user: no slamming feel): the words now drop in from above for .14 s and everything else (flash,
            // rays, rings, lines, particles) fires at the moment they land, instead of while they are still in the air.
            float te = t - .09f;   // 12:22: the first letter lands at .09 s (faster fall)
            bool rainbow = L >= 8, exitPop = L >= 6, slam = L >= 3;
            float outK = 1f - Mathf.Clamp01((t - exitT) / .3f);
            Color top, bottom, side, glow, ray;
            if (L <= 1) { top = GardenSkin.Hex(0xFFF2B0); bottom = GardenSkin.Hex(0xFF8F1A); side = GardenSkin.Hex(0xB5520A); glow = new Color(1f, .82f, .31f); ray = new Color(1f, .89f, .47f); }
            else if (L <= 3) { top = GardenSkin.Hex(0xFFE6C4); bottom = GardenSkin.Hex(0xF06A10); side = GardenSkin.Hex(0xA4410C); glow = new Color(1f, .63f, .31f); ray = new Color(1f, .75f, .47f); }
            else { top = GardenSkin.Hex(0xF6E6FF); bottom = GardenSkin.Hex(0x8A3CF0); side = GardenSkin.Hex(0x43168F); glow = new Color(.75f, .47f, 1f); ray = new Color(.84f, .67f, 1f); }
            if (rainbow) { side = GardenSkin.Hex(0x4A238F); glow = Color.white; ray = Color.white; }
            bool mapped = s.MapLayout;
            float CX = 270f, CY = GardenBars.ScreenY(s, 230f);
            s.MapLayout = false;
            try {
                if (L >= 4) {   // navy backing so the words read on the busy field
                    float a = .5f * Mathf.Clamp01(t / .15f) * outK;
                    s.Glow(CX, CY, 500f, 250f, new Color(.05f, .09f, .25f, a), 0f);
                    s.Glow(CX, CY, 330f, 160f, new Color(.05f, .09f, .25f, a * .6f), 0f);
                }
                { float R = 220f + 24f * L; s.Glow(CX, CY, R * 1.3f, R * .7f, ShowA(glow, (.3f + .04f * L) * Mathf.Clamp01(te / .2f) * outK), 0f); }
                if (L >= 2 && te > 0f) ShowRays(s, CX, CY, te, outK, 1f, 120f + 16f * (L - 2), 8 + 2 * (L - 2), Mathf.Min(.45f, .22f + .04f * (L - 2)), ray, rainbow);
                if (L >= 7 && te > 0f) ShowRays(s, CX, CY, te, outK, -1.3f, 100f + 14f * (L - 2), 10, .2f, ray, rainbow);
                if (L >= 4 && te >= 0f && te < .35f) {
                    float a = .7f * (1f - te / .35f);
                    for (int i = 0; i < 18; i++) {
                        float ang = i / 18f * Mathf.PI * 2f + (i % 3) * .06f, r0 = 120f + (i % 3) * 14f + te * 160f, r1 = r0 + 30f + (i % 4) * 10f;
                        Vector2 d = new Vector2(Mathf.Cos(ang) * 1.25f, Mathf.Sin(ang) * .6f);
                        s.Segment(new Vector2(CX, CY) + d * r0, new Vector2(CX, CY) + d * r1, 2f + i % 3, new Color(1f, 1f, 1f, a));
                    }
                }
                Vector2 C = new Vector2(CX, CY);
                if (L >= 1) GardenFx.Burst(s, "rocket-pop", C, 150f + 18f * L, te, .45f);
                if (L >= 4) GardenFx.Burst(s, "merge-burst", C, 230f + 15f * L, te - .03f, .55f);
                if (L >= 5) GardenFx.Burst(s, "merge-burst", C, 300f + 15f * L, te - .28f, .6f);
                if (te >= 0f && te < .16f) { float R = 70f + 16f * L; s.Glow(CX, CY, R * 2f, R * 2f, new Color(1f, 1f, 1f, (L >= 3 ? .9f : .55f) * (1f - te / .16f)), 0f); }
                ShowParticles(s, C, te, L, top, bottom, rainbow, v);
                if (L >= 7 && t > .3f && t < exitT + .2f)
                    for (int i = 0; i < 8; i++) {
                        float c = (t * .9f + i / 8f) % 1f, sz = 9f + (i % 3) * 4f;
                        Vector2 p = new Vector2(CX + (i - 3.5f) * 40f + Mathf.Sin(t * 3f + i) * 8f, CY + 40f - c * 90f);
                        GardenFx.Draw(s, "rocket-pop", p, sz, sz, new Color(1f, 1f, 1f, .8f * Mathf.Sin(c * Mathf.PI) * outK), 0);
                    }
                // Words.
                float exitK = exitPop ? Mathf.Clamp01((t - exitT) / .1f) : 0f, wordsA = exitPop ? 1f - exitK : outK;
                // 12:22 (user: still no weight): a harder jolt of the whole group on impact, sideways and down.
                float jolt = te > 0f && te < .28f ? (1f - te / .28f) * (1f - te / .28f) : 0f;
                float wob = Mathf.Sin(te * 70f) * (3f + .8f * L) * jolt, dip = Mathf.Abs(Mathf.Sin(te * 55f)) * (2f + .6f * L) * jolt;
                float grow = (1f + .02f * Mathf.Sin(t * 6f) * Mathf.Clamp01((t - .5f) / .3f)) * (1f + exitK * .12f);
                float wy = CY - 10f - (exitPop ? 0f : (1f - outK) * 16f) + dip;
                string title = GardenPraise(v);
                if (wordsA > 0f) {
                    float total = GardenBarArt.TextWidth(title, size), lx = CX - total * .5f, stagger = slam ? .04f : 0f;
                    if (te >= 0f && te < .15f)   // ground flash and foam thrown out at both ends on impact
                        s.Glow(CX, wy + size * .45f, total * 1.7f, size * .9f, new Color(1f, 1f, 1f, .8f * (1f - te / .15f)), 0f);
                    GardenFx.Burst(s, "impact-dust", new Vector2(CX - total * .55f, wy + size * .38f), size * 1.6f, te, .4f);
                    GardenFx.Burst(s, "impact-dust", new Vector2(CX + total * .55f, wy + size * .38f), size * 1.6f, te - .02f, .4f);
                    float sweep = L >= 2 ? (t - .5f) / .4f : -1f, sweepX = CX - total * .5f - size + sweep * (total + size * 2f);
                    for (int i = 0; i < title.Length; i++) {
                        string ch = title[i].ToString(); float w = GardenBarArt.TextWidth(ch, size), lt = t - i * stagger; lx += w;
                        if (lt < 0f) continue;
                        // 12:22 (user: still no slam). Fall in .09 s from 3x (2.2x below 500) and high above, with two ghost
                        // copies; on impact squashed wide and flat on its base line and held .06 s, flashing white; then a
                        // damped spring back; a puff of foam under each letter.
                        const float fallT = .09f, holdT = .06f;
                        float big = slam ? 3f : 2.2f, rise = 40f + 6f * L, e = Mathf.Clamp01(lt / fallT);
                        float sc = lt < fallT ? big + (1f - big) * e * e : 1f, yoff = lt < fallT ? -rise * (1f - e * e) : 0f;
                        float sqx = 1f, sqy = 1f, flashK = 0f;
                        if (lt >= fallT && lt < fallT + holdT) { sqx = 1.3f; sqy = .7f; flashK = 1f - (lt - fallT) / holdT; }
                        else if (lt >= fallT + holdT) {
                            float k = Mathf.Clamp01((lt - fallT - holdT) / .3f), sp = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * (1f - k);
                            sqy = 1f + .25f * sp; sqx = 1f - .15f * sp;
                        }
                        float a = Mathf.Clamp01(lt / .03f) * wordsA, lxC = CX + (lx - w * .5f - CX) * grow + wob;
                        Color lt0 = top, lb0 = bottom;
                        if (rainbow) { float h = (i * .15f + t * .4f) % 1f; lt0 = Color.HSVToRGB(h, .08f, 1f); lb0 = Color.HSVToRGB((h + .11f) % 1f, .75f, .95f); }
                        if (lt < fallT)
                            for (int g = 2; g >= 1; g--) {
                                float lg = lt - g * .025f; if (lg < 0f) continue;
                                float eg = Mathf.Clamp01(lg / fallT);
                                ShowGlyph(s, ch, lxC, wy - rise * (1f - eg * eg), size * grow * (big + (1f - big) * eg * eg), lt0, lb0, side, a * .3f / g, false, -9999f);
                            }
                        if (lt >= fallT && lt < fallT + .32f)
                            GardenFx.Burst(s, "impact-dust", new Vector2(lxC, wy + size * .42f), size * (1f + .03f * L), lt - fallT, .32f);
                        ShowGlyph(s, ch, lxC, wy + yoff, size * sc * grow, Color.Lerp(lt0, Color.white, flashK * .8f), Color.Lerp(lb0, Color.white, flashK * .6f),
                            side, a, L >= 6, sweep >= 0f && sweep <= 1f ? sweepX : -9999f, sqx, sqy);
                    }
                    float st = t - (slam ? .3f : .16f);
                    if (st > 0f) {
                        float k = st < .12f ? 1.18f * (st / .12f) : st < .3f ? 1.18f - .18f * ((st - .12f) / .18f) : 1f;
                        float fs = Mathf.Max(18f, size * .46f) * k * grow, sy = wy + size * .8f * grow - fs * .55f;
                        string num = v + " ", word = "连击";
                        float w1 = GardenBarArt.TextWidth(num, fs), w2 = GardenBarArt.TextWidth(word, fs), x0 = CX - (w1 + w2) * .5f;
                        GardenBarArt.TextOutline(s, num, x0, sy, fs, ShowA(Color.white, wordsA), ShowA(showNavy, wordsA), fs * .13f, false);
                        GardenBarArt.TextRamp(s, num, x0, sy, fs, ShowA(GardenSkin.Hex(0xFFF7C2), wordsA), ShowA(GardenSkin.Hex(0xFFB21F), wordsA), false);
                        GardenBarArt.TextOutline(s, word, x0 + w1, sy, fs, ShowA(Color.white, wordsA), ShowA(showNavy, wordsA), fs * .13f, false);
                    }
                }
                if (exitPop && t >= exitT) {
                    float step = size * .9f;
                    for (int i = 0; i < title.Length; i++)
                        GardenFx.Burst(s, "impact-dust", new Vector2(CX + (i - (title.Length - 1) * .5f) * step, wy), size * 1.7f, t - exitT - i * .03f, .5f);
                    GardenFx.Burst(s, "impact-dust", new Vector2(CX, wy + size * .8f), size * 1.5f, t - exitT - .08f, .5f);
                }
            } finally { s.MapLayout = mapped; }
        }
        // One letter: three steps of darker sides, a thick navy rim, a white rim, the colour ramp, then the light sweep.
        void ShowGlyph(PixelSurface s, string ch, float cx, float cy, float sz, Color top, Color bottom, Color side, float a, bool thick, float sweepX,
            float sqx = 1f, float sqy = 1f) {
            float y = cy - sz * .55f, depth = sz * (thick ? .11f : .06f), navyW = sz * .1f;
            GardenBarArt.StretchX = sqx; GardenBarArt.StretchY = sqy; GardenBarArt.StretchCX = cx; GardenBarArt.StretchBaseY = cy + sz * .45f;
            try { ShowGlyphLayers(s, ch, cx, y, sz, top, bottom, side, a, depth, navyW, sweepX); }
            finally { GardenBarArt.StretchX = GardenBarArt.StretchY = 1f; }
        }
        void ShowGlyphLayers(PixelSurface s, string ch, float cx, float y, float sz, Color top, Color bottom, Color side, float a, float depth, float navyW, float sweepX) {
            Color navy = ShowA(showNavy, a), white = ShowA(Color.white, a);
            for (int k = 3; k >= 1; k--) GardenBarArt.TextOutline(s, ch, cx, y + depth * k / 3f, sz, ShowA(side, a), navy, navyW);
            GardenBarArt.TextOutline(s, ch, cx, y, sz, white, navy, navyW);
            GardenBarArt.TextOutline(s, ch, cx, y, sz, white, navy, navyW * .55f);
            GardenBarArt.TextOutline(s, ch, cx, y, sz, white, white, sz * .045f);
            GardenBarArt.TextRamp(s, ch, cx, y, sz, ShowA(top, a), ShowA(bottom, a));
            if (sweepX > -9000f) GardenBarArt.TextSweep(s, ch, cx, y, sz, sweepX, sz * .55f, new Color(1f, 1f, 1f, .85f * a));
        }
        void ShowRays(PixelSurface s, float cx, float cy, float t, float outK, float dir, float R, int n, float alpha, Color ray, bool rainbow) {
            float sc = .35f + .65f * ShowBack(Mathf.Clamp01(t / .3f)), a = alpha * Mathf.Clamp01(t / .25f) * outK;
            if (a <= 0f) return;
            for (int i = 0; i < n; i++) {
                float ang = i / (float)n * Mathf.PI * 2f + dir * t * .5f, r = R * sc * (i % 2 == 1 ? .78f : 1f);
                Color c = rainbow ? gardenRainbow[i % gardenRainbow.Length] : ray; c.a = a;
                s.GlowLine(new Vector2(cx, cy), new Vector2(cx + Mathf.Cos(ang) * r * 1.25f, cy + Mathf.Sin(ang) * r * .72f), (i % 2 == 1 ? 16f : 26f) * sc, c);
            }
        }
        // Stars, bubbles and (1500+) confetti thrown out and braked hard, so they settle around the words.
        void ShowParticles(PixelSurface s, Vector2 c, float t, int L, Color top, Color bottom, bool rainbow, int seed) {
            int n = 6 + 6 * L;
            for (int i = 0; i < n; i++) {
                int h = seed * 131 + i * 17;
                float delay = GardenHash(h + 1) * .08f, lt = t - delay, life = .8f + GardenHash(h + 2) * .6f + L * .05f;
                if (lt < 0f || lt > life) continue;
                float ang = GardenHash(h + 3) * Mathf.PI * 2f, sp = 220f + GardenHash(h + 4) * (180f + 40f * L);
                float kindR = GardenHash(h + 5);
                int kind = L >= 5 && GardenHash(h + 6) < .35f ? 2 : kindR < .45f ? 0 : 1;   // 0 star, 1 bubble, 2 confetti
                float k = (1f - Mathf.Exp(-4.2f * lt)) / 4.2f;
                Vector2 p = c + new Vector2(Mathf.Cos(ang) * (24f + sp * k), Mathf.Sin(ang) * (14f + sp * .6f * k));
                float fade = 1f - Mathf.Clamp01((lt - life * .55f) / (life * .45f));
                Color col = i % 3 == 0 ? Color.white : rainbow ? gardenRainbow[i % gardenRainbow.Length] : i % 3 == 1 ? top : bottom;
                if (kind == 0) {
                    float r = (6f + GardenHash(h + 7) * 8f) * (.8f + .3f * Mathf.Sin(lt * 18f));
                    s.Glow(p.x, p.y, r * 2.2f, r * 2.2f, ShowA(col, .45f * fade), 0f);
                    s.Segment(p - new Vector2(r, 0f), p + new Vector2(r, 0f), r * .3f, ShowA(col, fade));
                    s.Segment(p - new Vector2(0f, r), p + new Vector2(0f, r), r * .3f, ShowA(col, fade));
                } else if (kind == 1) {
                    float sz = 10f + GardenHash(h + 8) * 14f;
                    GardenFx.Draw(s, "rocket-pop", p - new Vector2(0f, 30f * lt * lt), sz, sz, new Color(1f, 1f, 1f, .9f * fade), 0);
                } else {
                    float sz = 6f + GardenHash(h + 9) * 5f, rot = GardenHash(h + 10) * 6.28f + lt * 9f, flip = Mathf.Abs(Mathf.Cos(rot)) + .2f;
                    Vector2 d = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)) * sz * .5f;
                    p.y += 120f * lt * lt;
                    s.Segment(p - d, p + d, sz * .6f * flip, ShowA(col, fade));
                }
            }
        }
        Vector2 MouthOr(int fighterId, Vector2 fallback) {
            if (GardenMouth == null) return fallback;
            Vector2 m = GardenMouth(fighterId);
            return float.IsNaN(m.x) ? fallback : m;
        }
        Vector2 EnemyShown(int index, Vector2 fallback) {
            if (GardenEnemyAt == null) return fallback;
            Vector2 at = GardenEnemyAt(index);
            return float.IsNaN(at.x) ? fallback : at;
        }
        Vector2 ShownPoint(float x, float y) => ScreenLayout.EnemyPoint(x, y + GardenOffsetDelta);

        // 2026-10-16 22:09 (user: a weighty landing on the pad). Seconds since the member left the queue and where
        // it started (the original pose record of f53854); false when no launch is in progress or recent.
        public bool GardenLaunch(int id, out float age, out Vector2 origin) {
            age = 0f; origin = Vector2.zero;
            if (!poses.TryGetValue(id, out Pose p)) return false;
            age = time - p.born; origin = p.origin;
            return age >= 0f && age < 1f;
        }
        // Youngest MAX fusion (seconds), for the short shake of the pads; large when none.
        public float GardenMaxFusionAge {
            get { float a = 99f; foreach (var f in fusions) if (f.tier >= 3 && f.age < a) a = f.age; return a; }
        }

        // 2026-10-16 22:09 (user: livelier merges). Gather (the original .22 s transitions), burst at .22 s, show-off.
        void DrawBubbleFusion(PixelSurface s, Fusion f, Color tint) {
            float t = f.age - .22f;
            if (t < 0f) return;
            Vector2 at = f.at;
            bool max = f.tier >= 3;
            // White flash and the bubble burst (bigger for MAX).
            if (t < .25f) s.Glow(at.x, at.y, 150f, 150f, new Color(1f, 1f, 1f, (1f - t / .25f) * .7f), 0f);
            GardenFx.Burst(s, "merge-burst", at, max ? 170f : 130f, t, .5f);
            // Spray: same-colour bubbles thrown out, falling and fading.
            for (int k = 0; k < 12; k++) {
                float u = t / .6f;
                if (u >= 1f) break;
                float a = (k * 30f + f.tier * 17f) * Mathf.Deg2Rad, speed = 95f + (k * 37 % 50);
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .8f - .35f) * speed * t + new Vector2(0f, 260f * t * t);
                GardenSkin.Shot(s, tint, p, (k % 3 == 0 ? 10f : 7f) * (1f - u * .5f), 1f - u);
            }
            // Gold sparkles.
            for (int k = 0; k < 4; k++) {
                float u = (t - k * .05f) / .35f;
                if (u < 0f || u >= 1f) continue;
                float a = (45f + k * 90f) * Mathf.Deg2Rad;
                Vector2 p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (26f + 30f * u);
                s.Glow(p.x, p.y, 22f * (1f - u), 22f * (1f - u), new Color(1f, .88f, .35f, 1f - u), 0f);
                s.Glow(p.x, p.y, 8f, 8f, new Color(1f, 1f, 1f, 1f - u), 0f);
            }
            if (max && t < .7f) {
                // Rainbow ring and golden rays.
                float u = t / .7f, r = 30f + 90f * Mathf.Sqrt(u);
                for (int k = 0; k < 18; k++) {
                    float a = k * 20f * Mathf.Deg2Rad + t * 2f;
                    GardenSkin.Shot(s, Color.HSVToRGB((k / 18f + t) % 1f, .55f, 1f), at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, 9f * (1f - u * .6f), 1f - u);
                }
                for (int k = 0; k < 8; k++) {
                    float a = (k * 45f + t * 40f) * Mathf.Deg2Rad;
                    s.GlowLine(at, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 110f, 26f, new Color(1f, .85f, .35f, (1f - u) * .3f));
                }
            }
            // Floating words over the head.
            if (t < .85f) {
                float u = t / .85f, popIn = Mathf.Min(t / .12f, 1f), size = (max ? 22f : 18f) * (.6f + .4f * popIn + (popIn < 1f ? 0f : .12f * Mathf.Max(0f, 1f - (t - .12f) / .15f)));
                float alpha = 1f - Mathf.Clamp01((u - .7f) / .3f);
                string word = max ? "满级！" : f.tier == 2 ? "再升级！" : "升级！";
                // 2026-10-20 (UI redesign 24): a light see-through glass base under the word, and one star / two stars /
                // a crown above it.
                float wy = at.y - 74f - 26f * u, ww = GardenBarArt.TextWidth(word, size) + size * 1.2f, wh = size * 1.45f;
                bool mapped = s.MapLayout;
                float sy = GardenBars.ScreenY(s, wy + size * .5f);
                s.MapLayout = false;
                GardenBars.Capsule(s, at.x, sy, ww, wh, GardenBars.Sky, alpha * .9f, .55f);
                GardenBars.Icon(s, max ? "crown" : f.tier == 2 ? "stars2" : "star", at.x, sy - wh * .5f - size * .45f, size * 1.15f, alpha);
                GardenBarArt.TextOutline(s, word, at.x, sy - size * .5f, size, new Color(1f, .86f, .3f, alpha),
                    new Color(GardenSkin.Outline.r, GardenSkin.Outline.g, GardenSkin.Outline.b, alpha), 2f);
                s.MapLayout = mapped;
            }
        }

        // Read existing feedback records; never advance clocks or resolve a model shot here.
        public void DrawGardenFeedback(PixelSurface s, Func<int, Color> color) {
            if (!gardenPopsHooked) {
                gardenPopsHooked = true;
                battle.Hit += GardenOnHit;
                battle.Impacted += GardenOnImpact;
            }
            foreach (var ship in battle.Fleet) {
                if (ship.Tier < 2 || ship.BeamLife <= 0) continue;
                int slot = battle.Fleet.IndexOf(ship);
                var from = MouthOr(ship.Id, Position(ship.Id, slot));
                var to = ShownPoint(ship.BeamX, ship.BeamY);
                // 2026-10-18 23:40 (user: Ⅱ bullets too small). Stream balls are .48-.62 of the width, so width 10 made
                // 5-6 px balls next to the 12 px Ⅰ shots; now Ⅱ 18 (9-11 px balls) and MAX 22 (11-14 px).
                // 2026-10-22 10:51 (user): sizes as they were, except MAX now uses the Ⅱ width (18, was 22).
                GardenFx.BubbleStream(s, from, to, 18, color(ship.Color), time);
            }
            foreach (var f in flights) {
                var shot = f.shot;
                // As in the original (ProjectileDraw f53879): tier-2+ shots are carried by the continuous stream, so
                // their flights are not drawn (they showed up as one big ball sliding along the MAX stream).
                if (shot.Tier >= 2 && !shot.Rocket) continue;
                var to = ShownPoint(shot.TargetX, shot.TargetY);
                if (!shot.Rocket) to = EnemyShown(f.target, ScreenLayout.EnemyPoint(battle.EnemyX(f.target), battle.EnemyY(f.target)));
                // All shots of a volley start at the mouth; side barrels and rockets only spread a few pixels.
                var from = MouthOr(shot.FighterId, f.from);
                if (shot.Rocket) from.x += shot.X < 270f ? -7f : 7f;
                else if (shot.Tier == 1) from.x += (shot.Barrel - 1) * 5f;
                // v3 (UI spec v3 4.4, user decision 2026-10-15 20:28): every projectile is a SOLID glossy ball in the
                // shooter's color (the bead texture), 12 px, with smaller solid after-images. No see-through bubble.
                // 2026-10-21 10:12 (user: the shots cover the face; small at the mouth, growing). Each ball is sized by
                // its distance from the mouth (GardenFx.MouthGrow: a quarter at the lips, full once past the head).
                // After-images are no longer parked at the mouth before the ball has moved (they were clamped to the
                // start, so with a shot every .07 s a ball sat on the face most of the time); a third one tightens the
                // trail into a jet. (10:39: the three tiny droplets sprayed from the lips read as a fine splash; removed.)
                Color tint = color(f.color);
                for (int g = 3; g >= 0; g--) {
                    float age = shot.Age - g * .018f;
                    if (g > 0 && age <= 0f) continue;
                    var q = Vector2.Lerp(from, to, Mathf.Clamp01(age / shot.Duration));
                    GardenSkin.Shot(s, tint, q, (12f - g * 2.2f) * GardenFx.MouthGrow((q - from).magnitude), 1f);
                }
            }
            DrawGardenPops(s, color);
            DrawGardenAttackPops(s);
            // Retain source effects that the initial four-level skin did not render.
            // These draw existing feedback records; no model or effect clock is changed.
            // Pierce sparks and weapon forms need hangar upgrades this build cannot reach: original drawing kept.
            DrawPierceSparks(s); DrawWeaponFx(s);
            // Third batch (art items 79/80): bubble versions of the MAX rocket burst and the revive purge.
            // Both original routines only draw, so skipping them changes nothing else.
            // Rocket clips already went through DrawGardenAttackPops and its spatial merge.
            if (!GardenFx.Has("rocket-pop")) DrawRocketBlasts(s);
            if (GardenFx.Has("rescue-bubble")) DrawBubblePurge(s); else DrawPurge(s);
            // f53855 fusion record; f53871 starts the burst at age .22.
            // 2026-10-16 22:09: partners rush in with a trail and shrink, wobbling; then burst, spray and words.
            foreach (var t in transitions) {
                float u = Mathf.Clamp01(t.age / t.duration);
                Color tint = color(t.color);
                for (int g = 3; g >= 1; g--) {
                    var q = Vector2.Lerp(t.from, t.to, Mathf.Clamp01(u - g * .12f));
                    GardenSkin.Shot(s, tint, q, 10f - g * 2f, .5f);
                }
                var p = Vector2.Lerp(t.from, t.to, u * u);
                GardenSkin.Creature(s, tint, p.x, p.y + 8, Mathf.Lerp(52f, 30f, u), t.tier, "", 0f, 10f, false, false, 0f, 0f, 0f,
                    Mathf.Sin(u * Mathf.PI * 3f) * .12f * t.spin);
            }
            foreach (var f in fusions) DrawBubbleFusion(s, f, color(f.color));
            foreach (var k in kamikazes) {
                // f53886/f53878: approach during delay, then impact at exploded; consume the source clock.
                if (!k.exploded) {
                    float t = Mathf.Clamp01((k.age / k.delay - .25f) / .75f);
                    var p = Vector2.Lerp(k.from, new Vector2(k.x, FrontScreenY + 6), t * t);
                    if (t > 0) GardenFx.Draw(s, "dash-trail", p + new Vector2(0, 45), 36, 96, color(k.color));
                    GardenSkin.Plant(s, color(k.color), p, k.tier, "0");
                } else GardenFx.Burst(s, "impact-dust", new Vector2(k.x, FrontScreenY + 6), 80, k.age - k.delay, .5f);
            }
            foreach (var wave in waves) {
                float t = Mathf.Clamp01(wave.age / wave.duration);
                bool revive = wave.kind == RepelKind.Revive;
                float y = (revive ? 574 : FrontScreenY) - t * (revive ? 260 : 50);
                float h = revive ? 180 : 60;
                GardenFx.Draw(s, revive ? "revive-wave" : "repel-gust", new Vector2(270, y - h * .5f), 540, h,
                    new Color(1, 1, 1, (revive ? .85f : .65f) * (1 - t)));
            }
            // f53914: PendingStrike exists during its 1s countdown; SuperDetonate starts superAge.
            // Only the cosmetic can moves; impact timing, target and radius are the original model's.
            // 2026-10-21 10:57 (user: the bubble bomb should drop as the rainbow bomb of the button and burst with real
            // power, as bubbles). The 1 s countdown carries the fall; the detonation keeps the model's moment.
            var pending = battle.PendingStrike;
            if (pending != null) {
                Vector2 at = ScreenLayout.EnemyPoint(pending.X, pending.Y);
                float t = Mathf.Clamp01(pending.Age);
                GardenFx.Aim(s, at, pending.RadiusX * ScreenLayout.EnemyScaleX * (1.12f - .12f * t), .45f + .45f * t);
                GardenStrikeSnapshot(pending);
                GardenBombFall(s, at, t);
            }
            if (lastStrike != null) {
                if (!ReferenceEquals(lastStrike, gardenBlastSeen)) { gardenBlastSeen = lastStrike; GardenStrikePops(lastStrike); }
                GardenBombBlast(s, ScreenLayout.EnemyPoint(lastStrike.X, lastStrike.Y), lastStrike.RadiusX * ScreenLayout.EnemyScaleX, superAge);
            }
        }
        // f53915 DrawPurge: same six columns, timing and front line. Rescue bubbles drop from the top onto the
        // front row (the art points up, so it is drawn rotated 180 degrees), then burst into foam there.
        void DrawBubblePurge(PixelSurface s) {
            foreach (PurgeFx purge in purges) {
                for (int i = 0; i < 6; i++) {
                    float x = i * 88 + 50, t = Mathf.Clamp01((purge.age - i * .04f) * 4f);
                    if (t < 1f) {
                        float y = Mathf.Min(t * t, 1f) * (purge.front - 120f) + 120f;
                        // Bubble centre sits 20 px from the frame centre (y 56 of 192 in the art); rotated, it leads downward.
                        GardenFx.Draw(s, "rescue-bubble", new Vector2(x, y - 20f), 48f, 72f, Color.white, 0, 180f);
                    }
                    GardenFx.Burst(s, "impact-dust", new Vector2(x, purge.front), 110f, purge.age - .25f - i * .06f, .6f);
                }
                float age = purge.age;
                if (!(age > .3f && age < 1.5f)) continue;
                float alpha = Mathf.Clamp01((1.5f - age) / .4f), q = age < .3f ? 1f : 1f - Mathf.Min((age - .3f) * 4f, 1f);
                float size = 11f * ((q * q * q) * 1.2f + 1.8f);
                // 2026-10-20 (UI redesign 25): a mint-edged bar with the reinforcement icon; it still swells in.
                string line = "泡泡援军 ×" + purge.count;
                float bh = size * 1.75f;
                bool mapped = s.MapLayout;
                float by = GardenBars.ScreenY(s, purge.front - 72f);
                s.MapLayout = false;
                GardenBars.Bar(s, 270f, by, GardenBars.Width(line, size, bh, "reinforce"), bh, GardenBars.Mint, "reinforce", line, size,
                    GardenBars.Navy, alpha, 1f, GardenBars.Deco.Many, age - .3f);
                s.MapLayout = mapped;
            }
        }

        // 2026-10-16 (user item 1): the original combo counter f53835, redrawn in bubble style. Same records and
        // rules: shown from 5 hits, a punch on every hit (comboPunch < .12 s), band colours (ComboColor), the
        // draining life bar (ComboLife). R1 extends centre bursts beyond 1000 in steps of 500.
        // 2026-10-20 21:22 (user): the badge is only as wide as its digits need and widens (smoothly) when the count
        // gains a digit; it never narrows while shown, and starts small again once it has faded out.
        float gardenComboSlot, gardenComboSlotTarget, gardenComboClock = -1f;
        // The small HUD counter needs one crisp rim; milestone lettering keeps its layered 3D treatment.
        void DrawComboHudText(PixelSurface s, string text, float cx, float cy, float size, Color top, Color bottom, float alpha)
        {
            float y = cy - size * .55f;
            GardenBarArt.TextOutline(s, text, cx, y, size, ShowA(top, alpha), ShowA(showNavy, alpha), 1.25f);
            GardenBarArt.TextRamp(s, text, cx, y, size, ShowA(top, alpha), ShowA(bottom, alpha));
        }
        public void DrawBubbleCombo(PixelSurface s) {
            // 2026-10-21 11:35 (user): the milestone show always plays to its end, even when the chain breaks and the
            // counter fades out meanwhile (only hidden together with the counter, e.g. under the finale).
            if (!HideCombo) DrawGardenComboShow(s);
            if (comboFade <= .01f || HideCombo) {
                if (comboFade <= .01f) { gardenComboSlot = gardenComboSlotTarget = 0f; gardenComboClock = -1f; }
                return;
            }
            int count = lastCombo > 1 ? lastCombo : 1;
            Color color = ComboColor(count);
            float fade = comboFade, punch = comboPunch < .12f ? 1f - comboPunch / .12f : 0f;
            float milestone = comboMilestoneAge < .5f ? 1f - comboMilestoneAge * 2f : 0f;
            string text = count.ToString();
            // UI redesign 21: keep the entire badge above the board (logical y 114), including the punch and life
            // bar. Map one centre so the label, digits and plate also stay together at the layout's stretch boundary.
            // 2026-10-21 11:38 (user): no plate any more, in the style of the milestone show: big outlined digits with a
            // colour ramp in the band colour, a small outlined 「连击 ×」 before them and a thin life bar under them. The
            // digit column still only widens (smoothly) while shown, so the count never shifts left and right; the
            // right edge stays at 520.
            float size = 32f * (1f + punch * .25f + milestone * .3f);
            // 11:51 (user: 「×」 got covered by the digits): the column is reserved for the largest punch, so the digits
            // grow inside it and never reach the label.
            float need = GardenBarArt.TextWidth(text, 32f * 1.55f) + 10f;
            if (need > gardenComboSlotTarget) gardenComboSlotTarget = need;
            float step = gardenComboClock < 0f ? 0f : Mathf.Max(0f, time - gardenComboClock);
            gardenComboClock = time;
            gardenComboSlot = gardenComboSlot <= 0f ? gardenComboSlotTarget : Mathf.MoveTowards(gardenComboSlot, gardenComboSlotTarget, 160f * step);
            float digitSlot = gardenComboSlot, digitX = 516f - digitSlot * .5f, badgeY = GardenBars.ScreenY(s, 145f);
            bool badgeMapped = s.MapLayout;
            s.MapLayout = false;
            try
            {
                // 11:51 (user): 「连击 ×」 in the same lettering and colours as the digits; the life bar is gone. Instead the
                // whole counter dims a little and wavers when the chain is about to break (last 30 % of its life).
                Color hi = Mix(color, Color.white, .7f), lo = Mix(color, GardenBars.Navy, .15f);
                float life = Mathf.Clamp01(battle.ComboLife), warn = life < .3f ? (1f - life / .3f) : 0f;
                float a = fade * (1f - .35f * warn * (.5f + .5f * Mathf.Sin(time * 18f)));
                const float labelSize = 24f;
                float labelW = GardenBarArt.TextWidth("连击 ×", labelSize), right = digitX - digitSlot * .5f;
                DrawComboHudText(s, "连击 ×", right - 3f - labelW * .5f, badgeY + 1f, labelSize, hi, lo, a);
                DrawComboHudText(s, text, digitX, badgeY - 2f, size, hi, lo, a);
            }
            finally { s.MapLayout = badgeMapped; }
        }
    }
}
#endif
