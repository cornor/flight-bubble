#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        // The pad members' drawing (set by the game each frame): canvas, size per tier and feet for a pad y.
        public float GardenPadCanvas;
        public Func<int, float> GardenPadScaleOf;
        public Func<float, float> GardenPadFeetAt;
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
        struct GardenPop { public Vector2 at; public float born, rich; public int color, seed, tier; public bool lite, spark, small; }
        readonly System.Collections.Generic.List<GardenPop> gardenPops = new System.Collections.Generic.List<GardenPop>();
        readonly System.Collections.Generic.Queue<float> gardenHitTimes = new System.Collections.Generic.Queue<float>();
        bool gardenPopsHooked;
        // 2026-10-31 23:36: Ⅱ / MAX streams - per member, the latest target of each stream and when it was set (Battle.Time).
        sealed class GardenStreams { public int next; public readonly Vector2[] to = new Vector2[4]; public readonly float[] at = { -10f, -10f, -10f, -10f }; }
        readonly Dictionary<int, GardenStreams> streams = new Dictionary<int, GardenStreams>();
        Flightbeat.Model.Battle streamsBattle;
        static int StreamCount(int tier) => tier > 2 ? 4 : 3;
        void GardenOnFired(Shot shot) {
            if (shot.Tier < 2 || shot.Rocket) return;
            if (!streams.TryGetValue(shot.FighterId, out var st)) streams[shot.FighterId] = st = new GardenStreams();
            int k = st.next % StreamCount(shot.Tier);
            st.next = (k + 1) % StreamCount(shot.Tier);
            st.to[k] = new Vector2(shot.TargetX, shot.TargetY);
            st.at[k] = battle.Time;
        }
        int gardenPopSeed, gardenSeedAtImpact;
        // Ordinary tiers and MAX's splash attack share one visual admission path.
        // Copy values at impact: the model releases/reuses Shot immediately afterwards.
        const float GardenAttackPopLife = .4f;
        // 120 ms / 1.2: dense hits can start 20% more clips per second.
        const float GardenAttackPopMergeWindow = .1f;
        const int GardenAttackPopLimit = 32;
        struct GardenAttackPop { public Vector2 at; public float born, size; public int tier; }
        readonly System.Collections.Generic.List<GardenAttackPop> gardenAttackPops =
            new System.Collections.Generic.List<GardenAttackPop>(GardenAttackPopLimit);
        static float GardenAttackPopSize(int tier) => tier >= 3 ? 120f : tier == 2 ? 84f : tier == 1 ? 60f : 40f;
        // Keep the original neighborhoods independent of the larger visual sizes.
        static float GardenAttackPopMergeRadius(int tier) => tier >= 3 ? 60f : tier == 2 ? 30f : 24f;
        static bool GardenRocketPopOn = false;
        void GardenOnImpact(Shot shot)
        {
            // 2026-11-02 14:24 (user: stronger hit feedback): the board gives a small jolt for MAX rockets and, at most every
            // .35 s, for Ⅱ / MAX streams (several kills at once kick it too, in GardenOnHit).
            // 2026-11-02 15:59 (user: MAX - small spark-splash explosions instead of the bubble pop): the pops this shot just
            // raised (Battle.Hit fires before Impacted in the same Resolve) turn into spark bursts; one within 60 px and
            // .1 s of an earlier spark burst is dropped, so a MAX stream or rocket sprays in clusters, not every bead.
            // 19:49 (user: higher tiers burst wider): the pops this shot raised take its tier.
            int shotTier = shot.Rocket ? 3 : shot.Tier;
            for (int i = 0; i < gardenPops.Count; i++) {
                var q = gardenPops[i];
                if (q.lite || q.seed < gardenSeedAtImpact || q.born < time - .001f) continue;
                q.tier = shotTier; gardenPops[i] = q;
            }
            // 20:45 (user: tiers 0 to MAX burst wider and wider; MAX stronger - wider, with more bubbles): with the painted
            // firework pop, MAX keeps it (drawn bigger and doubled, see DrawGardenFirework) instead of the spark burst; a MAX
            // pop within 60 px and .1 s of an earlier full MAX burst plays the small burst, so a MAX stream is not a wall of
            // big bursts. The spark burst below stays only as the fallback without the art.
            if (shotTier >= 3 && GardenSkin.Has("plants/fx/burst-0-1-color")) {
                for (int i = gardenPops.Count - 1; i >= 0; i--) {
                    var q = gardenPops[i];
                    if (q.lite || q.small || q.seed < gardenSeedAtImpact || q.born < time - .001f) continue;
                    foreach (var o in gardenPops)
                        if (o.seed < q.seed && o.tier >= 3 && !o.small && !o.lite && time - o.born < .1f
                            && (PlushLayout.Point(o.at) - PlushLayout.Point(q.at)).sqrMagnitude < 3600f) { q.small = true; gardenPops[i] = q; break; }
                }
            }
            else if (shot.Tier >= 3) {
                for (int i = gardenPops.Count - 1; i >= 0; i--) {
                    var q = gardenPops[i];
                    if (q.lite || q.spark || q.seed < gardenSeedAtImpact || q.born < time - .001f) continue;
                    if (q.small) { gardenPops.RemoveAt(i); continue; }   // MAX never shows the soap pop
                    bool near = false;
                    foreach (var o in gardenPops)
                        if (o.spark && time - o.born < .1f && (PlushLayout.Point(o.at) - PlushLayout.Point(q.at)).sqrMagnitude < 3600f) { near = true; break; }
                    if (near) { gardenPops.RemoveAt(i); continue; }
                    q.spark = true; gardenPops[i] = q;
                }
            }
            gardenSeedAtImpact = gardenPopSeed;
            if (shot.Rocket) GardenHitKick(3f);
            else if (shot.Tier >= 2 && time - gardenStreamKickAt > .35f) { gardenStreamKickAt = time; GardenHitKick(1.5f); }
            // 2026-11-02 14:24 (user: turn off the extra pop): ordinary shots no longer add the big generic burst clip on top
            // of the bead's own pop (it hid which bead broke and fogged dense rows); only MAX rockets keep it. 20:45 (user: turn
            // off the extra art pop on MAX too): off for rockets as well; the switch keeps the code for later.
            if (!GardenRocketPopOn || !shot.Rocket) return;
            Vector2 at = shot.Rocket ? ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY)
                : EnemyShown(shot.TargetIndex, ShownPoint(shot.TargetX, shot.TargetY));
            int tier = shot.Rocket ? 3 : shot.Tier;
            float size = GardenAttackPopSize(tier);
            Vector2 shown = PlushLayout.Point(at);
            for (int i = gardenAttackPops.Count - 1; i >= 0; i--)
            {
                var pop = gardenAttackPops[i];
                float age = time - pop.born;
                if (age < 0f || age >= GardenAttackPopLife) { gardenAttackPops.RemoveAt(i); continue; }
                if (age >= GardenAttackPopMergeWindow) continue;
                // Group adjacent beads, with a wider neighborhood for large bursts.
                float radius = Mathf.Min(GardenAttackPopMergeRadius(tier), GardenAttackPopMergeRadius(pop.tier));
                if ((shown - PlushLayout.Point(pop.at)).sqrMagnitude > radius * radius) continue;
                // A stronger impact can take over the clip, without restarting or moving it.
                pop.size = Mathf.Max(size, pop.size);
                pop.tier = Mathf.Max(tier, pop.tier);
                gardenAttackPops[i] = pop;
                TrimGardenAttackPops();
                return;
            }
            gardenAttackPops.Add(new GardenAttackPop { at = at, born = time, size = size, tier = tier });
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
            // 21:09: 48 (was 32) - the firework pop lives .44 s, and dropping the oldest cut fading bursts off early.
            if (hits >= 48) { for (int i = 0; i < gardenPops.Count; i++) if (!gardenPops[i].lite) { gardenPops.RemoveAt(i); break; } }
            while (gardenHitTimes.Count > 0 && (time - gardenHitTimes.Peek() > .4f || gardenHitTimes.Peek() > time)) gardenHitTimes.Dequeue();
            gardenHitTimes.Enqueue(time);
            int burst = 0; foreach (float h in gardenHitTimes) if (time - h <= .12f) burst++;
            if (burst >= 4 && time - gardenBurstKickAt > .3f) { gardenBurstKickAt = time; GardenHitKick(2f); }
            // 1 up to ~5 hits in .4 s (one or two members), fading to 0 by ~16 (a full row firing / combo storm).
            float rich = Mathf.Clamp01((16f - gardenHitTimes.Count) / 11f);
            // 16:31 (user screenshot: a blue swirl where a stream kept hitting one corner): a Ⅱ / MAX stream pops a bead every
            // frame next to the last one, so the big pops piled up into rings of shards. A bead within 34 px of a big pop
            // started less than .12 s ago gets the small soap pop instead; the stream reads as a string of pops, not a swirl.
            Vector2 at = ShownPoint(x, y);
            bool crowded = false;
            foreach (var o in gardenPops)
                if (!o.lite && !o.small && time - o.born < .12f && (PlushLayout.Point(o.at) - PlushLayout.Point(at)).sqrMagnitude < 34f * 34f) { crowded = true; break; }
            gardenPops.Add(new GardenPop { at = at, born = time, color = color, seed = gardenPopSeed++, rich = rich, small = crowded });
        }
        static float GardenHash(int n) { float v = Mathf.Sin(n * 12.9898f) * 43758.5453f; return v - Mathf.Floor(v); }
        public float GardenBeadSize = 11f;   // set by the game from the drawn bead size
        // 2026-11-02 (user: hits should feel great; pops bigger, not tied to the bead size). 15:50 (user: it did not read as
        // a bubble breaking - the expanding ring looked like a gust following the shots): tiers 0-2 now blow the bead up
        // into a see-through soap bubble of its own colour (.08 s, to 26 px radius, rim, inner sheen, highlight), which
        // then pops - a flash, six curved film shards flying out and turning, and up to 10 small beads of the colour flung
        // out and dropping (.28 s in all, ~100 px across). MAX hits (spark) are small spark-splash explosions instead. The
        // combo boost (from 20 hits, full at 100) makes both up to 30 % bigger with more beads / sparks; busy boards (rich)
        // throw fewer. The bomb's ripple of pops (lite) keeps the small soap pop.
        const float GardenPopLife = .36f, GardenPopSwell = .06f, GardenPopRadius = 26f;   // 17:15: .06 swell + .3 splash
        void DrawGardenPops(PixelSurface s, Func<int, Color> color) {
            float boost = Mathf.Clamp01((battle.Combo - 20) / 80f);
            bool art = GardenSkin.Has("plants/fx/bubble-color"), firework = GardenSkin.Has("plants/fx/burst-0-1-color");
            for (int i = gardenPops.Count - 1; i >= 0; i--) {
                GardenPop p = gardenPops[i];
                float age = time - p.born;
                // 18:10: a crowded stream pop (small) throws a smaller bubble burst when the bubble is in the atlas.
                bool alive = p.small && firework ? DrawGardenFirework(s, p, color(p.color), age, boost, true)
                    : p.small && art ? DrawGardenBubbleBurst(s, p, color(p.color), age, boost, true)
                    : p.lite || p.small ? DrawGardenLitePop(s, p, color(p.color), age)
                    : p.spark ? DrawGardenSparkPop(s, p, color(p.color), age, boost)
                    : DrawGardenSoapPop(s, p, color(p.color), age, boost);
                if (!alive) gardenPops.RemoveAt(i);
            }
        }
        bool DrawGardenSoapPop(PixelSurface s, GardenPop p, Color tint, float age, float boost) {
            if (age >= GardenPopLife || age < -1f) return false;
            if (age < 0f) return true;
            float R = GardenPopRadius * (1f + .3f * boost);
            // 18:10 (user picked concept B, the jelly water crown). 18:19 (user: too big and heavy, it covers the board):
            // the painted impact-dust burst. 19:49 (user: its dense middle smears into one blob on black and white beads alike;
            // burst straight into small see-through bubbles, wider for higher tiers): the bead bursts into clear bubbles of its
            // colour (plants/fx/bubble) flung out from its edge - no core, no ring.
            // 20:27 (user: like fireworks bursting, no light, nothing heavy where it bursts; art BubbleBurst-20261102, concept
            // A "Pearl Rays"): strings of clear bubbles flung out from the hit, painted per tier.
            if (GardenSkin.Has("plants/fx/burst-0-1-color")) return DrawGardenFirework(s, p, tint, age, boost, false);
            if (GardenSkin.Has("plants/fx/bubble-color")) return DrawGardenBubbleBurst(s, p, tint, age, boost, false);
            if (age < GardenPopSwell) {
                float k = age / GardenPopSwell, r = Mathf.Lerp(GardenBeadSize * .5f, R, 1f - (1f - k) * (1f - k));
                // 16:50 (user: why is there a circle on every hit): no outlined shell - the bead itself swells (to 1.8x, a
                // soft glow of its colour behind) and pops into its beads; the painted pop replaces this when delivered.
                Color soft = tint; soft.a = .35f * k;
                s.Glow(p.at.x, p.at.y, r * 2.4f, r * 2.4f, soft, 0f);
                float bs = GardenBeadSize * (1f + .8f * k);
                GardenSkin.Bug(s, tint, p.at, bs, false, bs);
                return true;
            }
            float u = (age - GardenPopSwell) / (GardenPopLife - GardenPopSwell), e = 1f - (1f - u) * (1f - u) * (1f - u), fade = 1f - u;
            if (u < .2f) {
                Color flash = Color.Lerp(tint, Color.white, .6f); flash.a = .7f * (1f - u / .2f);
                s.Glow(p.at.x, p.at.y, R * 2.2f, R * 2.2f, flash, 0f);
            }
            // 16:33 (user: the ring of shards looks bad): no film shards. 17:15 (user: the bits are all solid, no feeling of
            // bubble water splashing): the painted splash of see-through bubbles and droplets (fx impact-dust, six frames)
            // tinted a little lighter than the bead, ~84 px (up to 30 % more with the combo boost), over .3 s.
            if (GardenFx.Has("impact-dust")) {
                Color wet = Color.Lerp(tint, Color.white, .3f);
                GardenFx.BurstTint(s, "impact-dust", p.at, R * 3.2f, age - GardenPopSwell, GardenPopLife - GardenPopSwell, wet);
                return true;
            }
            int n = Mathf.RoundToInt(Mathf.Lerp(4f, 10f + 3f * boost, p.rich));
            for (int k = 0; k < n; k++) {
                float ang = (k / (float)n + GardenHash(p.seed * 5 + k) * .1f) * Mathf.PI * 2f, sp = R * (1.1f + .9f * GardenHash(p.seed * 7 + k));
                Vector2 q = p.at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp * e + new Vector2(0f, R * .5f * u * u);
                GardenSkin.Shot(s, tint, q, (7f - 3.5f * u) * (1f + .2f * boost), Mathf.Min(1f, fade * 1.3f));
            }
            return true;
        }
        // The painted firework pop: the tier's frames (the art's last two are empty), drawn 180 px wide (about 1.4x the
        // art's board size - at that size the bubbles were too small to read), 20 % more with the combo boost; a crowded
        // stream pop (small) plays tier 0 at 70 %. Every other pop is mirrored. White and pale beads are tinted towards a
        // light blue-grey, or their see-through bubbles vanish on white rows. 21:09 (user: it disappears too fast): the
        // burst out (frames 0-5) keeps the art's 20 ms a frame, the spread and fade after it (frames 6-13) run at 40 ms -
        // .44 s in all instead of .28 s.
        const float GardenFireworkSize = 180f, GardenFireworkLife = .44f;
        static int GardenFireworkFrameAt(float age) => age < .12f ? Mathf.FloorToInt(age / .02f) : 6 + Mathf.FloorToInt((age - .12f) / .04f);
        bool DrawGardenFirework(PixelSurface s, GardenPop p, Color tint, float age, float boost, bool small) {
            bool max = !small && p.tier >= 3;
            if (age >= GardenFireworkLife + (max ? .04f : 0f) || age < -1f) return false;
            if (age < 0f) return true;
            int tier = small ? 0 : Mathf.Clamp(p.tier, 0, 2), f = GardenFireworkFrameAt(age);
            float size = GardenFireworkSize * (small ? .7f : max ? 1.3f + .2f * boost : 1f + .2f * boost);
            float pale = Mathf.Clamp01((Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b)) - .7f) / .3f);
            Color c = Color.Lerp(tint, new Color(.62f, .70f, .88f), .6f * pale); c.a = 1f;
            string id = "plants/fx/burst-" + tier + "-" + f;
            if (f < 14 && GardenSkin.Has(id + "-color")) {
                bool flip = GardenHash(p.seed * 11 + 3) < .5f;
                float x = p.at.x - size * .5f, y = p.at.y - size * .5f;
                GardenSkin.Asset(s, id + "-color", x, y, size, size, c, false, true, flip);
                GardenSkin.Asset(s, id + "-light", x, y, size, size, Color.white, false, true, flip);
            }
            // MAX: a second, smaller copy 40 ms behind, turned half a ray (16 deg) so its bubbles fill the gaps - twice the
            // bubbles, ~1.3x the reach of tier 2.
            int f2 = age < .04f ? -1 : GardenFireworkFrameAt(age - .04f);
            string id2 = "plants/fx/burst-2-" + f2;
            if (max && f2 >= 0 && f2 < 14 && GardenSkin.Has(id2 + "-color")) {
                float s2 = size * .8f, rot = 16f + 20f * GardenHash(p.seed * 13 + 5);
                GardenSkin.AssetRotated(s, id2 + "-color", p.at, s2, s2, rot, c);
                GardenSkin.AssetRotated(s, id2 + "-light", p.at, s2, s2, rot, Color.white);
            }
            return true;
        }
        // 7 / 9 / 12 clear bubbles (8-16 px) for tiers 0 / 1 / 2, flying out to 26 / 38 / 52 px from the bead (about 55 /
        // 80 / 105 px across; 30 % more with the combo boost), easing out and drifting up a little; each lives .27-.37 s
        // and swells slightly as it fades. A crowded stream pop (small) throws 4 at 70 % of the reach. Busy boards (rich)
        // throw up to 40 % fewer.
        const float GardenBubbleLife = .34f;
        bool DrawGardenBubbleBurst(PixelSurface s, GardenPop p, Color tint, float age, float boost, bool small) {
            if (age >= GardenBubbleLife * 1.1f || age < -1f) return false;
            if (age < 0f) return true;
            int tier = Mathf.Clamp(p.tier, 0, 2);
            float reach = (tier == 2 ? 52f : tier == 1 ? 38f : 26f) * (1f + .3f * boost) * (small ? .7f : 1f);
            int n = small ? 4 : Mathf.RoundToInt((tier == 2 ? 12f : tier == 1 ? 9f : 7f) * Mathf.Lerp(.6f, 1f, p.rich));
            for (int k = 0; k < n; k++) {
                float life = GardenBubbleLife * (.8f + .3f * GardenHash(p.seed * 3 + k)), u = age / life;
                if (u >= 1f) continue;
                float ang = (k + .5f * GardenHash(p.seed * 5 + k)) / n * Mathf.PI * 2f;
                float dist = reach * (.5f + .5f * GardenHash(p.seed * 7 + k)), e = 1f - (1f - u) * (1f - u) * (1f - u);
                float r = 4f + (.25f + .75f * e) * dist;
                Vector2 q = p.at + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r - 6f * u * u);
                float size = (8f + 8f * GardenHash(p.seed * 11 + k)) * (small ? .8f : 1f) * (1f + .25f * Mathf.Max(0f, (u - .75f) / .25f));
                float a = u < .7f ? 1f : 1f - (u - .7f) / .3f;
                Color c = tint; c.a = a;
                GardenSkin.Asset(s, "plants/fx/bubble-color", q.x - size * .5f, q.y - size * .5f, size, size, c);
                GardenSkin.Asset(s, "plants/fx/bubble-light", q.x - size * .5f, q.y - size * .5f, size, size, new Color(1f, 1f, 1f, a));
            }
            return true;
        }
        // A see-through soap bubble of the bead's colour: a pale fill, a solid rim, an inner sheen, a white highlight arc
        // at the upper left and a small glint at the lower right.
        void GardenSoapShell(PixelSurface s, Vector2 at, float r, Color tint, float a) {
            Color fill = Color.Lerp(tint, Color.white, .3f); fill.a = .47f * a;
            // 16:31 (user screenshot: a pale disc floating over the pads): GardenRoundedRect does not apply the layout
            // mapping that Ring / Glow do, so the fill is placed at the mapped point with the mapping off.
            bool mapped = s.MapLayout;
            Vector2 c = mapped ? PlushLayout.Point(at) : at;
            s.MapLayout = false;
            s.GardenRoundedRect(c.x - r, c.y - r, r * 2f, r * 2f, r, fill, fill);
            s.MapLayout = mapped;
            Color rim = tint; rim.a = a;
            s.Ring(at, r, rim, Mathf.Max(1.5f, r * .2f), 24, 0f, 360f);
            Color sheen = Color.Lerp(tint, Color.white, .65f); sheen.a = .5f * a;
            s.Ring(at, r * .86f, sheen, Mathf.Max(1f, r * .06f), 24, 0f, 360f);
            s.Ring(at, r * .72f, new Color(1f, 1f, 1f, .95f * a), Mathf.Max(1f, r * .13f), 6, 200f, 50f);
            s.Glow(at.x + r * .45f, at.y + r * .42f, r * .24f, r * .24f, new Color(1f, 1f, 1f, .8f * a), 0f);
        }
        // MAX: a small spark-splash explosion, .32 s - a gold-white flash, then 14 sparks shooting out and dropping, each
        // a white-hot head with a little gold glow and a tail in the bead's colour, thinning as they go, and a few gold
        // embers. No ring, ~100 px across.
        bool DrawGardenSparkPop(PixelSurface s, GardenPop p, Color tint, float age, float boost) {
            const float life = .32f, flash = .04f;
            if (age >= life || age < -1f) return false;
            if (age < 0f) return true;
            float S = 50f * (1f + .3f * boost);
            Color gold = new Color(1f, .84f, .35f), white = new Color(1f, .98f, .9f);
            if (age < flash) {
                float k = age / flash;
                s.Glow(p.at.x, p.at.y, S * (.5f + .3f * k), S * (.5f + .3f * k), new Color(gold.r, gold.g, gold.b, .9f), 0f);
                s.Glow(p.at.x, p.at.y, S * .24f, S * .24f, white, 0f);
                return true;
            }
            float u = (age - flash) / (life - flash), fade = 1f - u, e = 1f - (1f - u) * (1f - u);
            if (u < .25f) s.Glow(p.at.x, p.at.y, S * .6f * (1f - u), S * .6f * (1f - u), new Color(gold.r, gold.g, gold.b, .6f * (1f - u / .25f)), 0f);
            int n = Mathf.RoundToInt(Mathf.Lerp(8f, 14f + 4f * boost, p.rich));
            float a = Mathf.Min(1f, fade * 1.3f), w = 4.5f * (1f - u) + 1.5f;
            for (int k = 0; k < n; k++) {
                float ang = (k / (float)n + GardenHash(p.seed * 3 + k) * .06f) * Mathf.PI * 2f, v = S * (.7f + .7f * GardenHash(p.seed * 7 + k));
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 head = p.at + dir * v * e + new Vector2(0f, S * .9f * u * u);
                float tl = S * (.25f * (1f - u) + .05f);
                Vector2 tail = head - dir * tl - new Vector2(0f, S * .25f * u), mid = Vector2.Lerp(tail, head, .5f);
                Color tc = Color.Lerp(tint, Color.white, .2f); tc.a = a * .7f;
                s.Segment(tail, mid, w * .6f, tc);
                Color hc = Color.Lerp(white, gold, Mathf.Min(1f, u * 1.6f)); hc.a = a;
                s.Segment(mid, head, w, hc);
                s.Glow(head.x, head.y, (6f * (1f - u) + 2f) * 2f, (6f * (1f - u) + 2f) * 2f, new Color(gold.r, gold.g, gold.b, .9f * fade), 0f);
                GardenSkin.Shot(s, white, head, w, fade);
            }
            for (int k = 0; k < 6; k++) {
                float ang = GardenHash(p.seed * 13 + k) * Mathf.PI * 2f, v = S * (.3f + .4f * GardenHash(p.seed * 17 + k));
                Vector2 q = p.at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * v * e + new Vector2(0f, S * .6f * u * u);
                s.Glow(q.x, q.y, 6f, 6f, new Color(gold.r, gold.g, gold.b, fade), 0f);
            }
            return true;
        }
        // The bomb's pops (lite): the earlier small soap pop - swell, a thin light ring and three mist dots, .22 s.
        bool DrawGardenLitePop(PixelSurface s, GardenPop p, Color tint, float age) {
            const float life = .22f, swell = .05f;
            float bead = GardenBeadSize;
            if (age >= life || age < -1f) return false;
            if (age < 0f) return true;
            if (age < swell) {
                float k = age / swell, grow = 1f + .3f * Mathf.Sin(k * Mathf.PI * .5f), wob = .08f * Mathf.Sin(k * Mathf.PI * 2f);
                Color c = tint; c.a = 1f - .25f * k;
                GardenSkin.Bug(s, c, p.at, bead * grow * (1f - wob), false, bead * grow * (1f + wob));
                return true;
            }
            float u = (age - swell) / (life - swell), e = 1f - (1f - u) * (1f - u) * (1f - u), fade = 1f - u;
            Color skin = Color.Lerp(tint, Color.white, .45f);
            // 16:50 (user: a circle on every hit): the thin film ring is gone from the small pop too; only the mist remains.
            for (int k = 0; k < 3; k++) {
                float ang = (k / 3f + GardenHash(p.seed * 5 + k) * .2f) * Mathf.PI * 2f;
                float reach = bead * (.55f + (.7f + .3f * GardenHash(p.seed * 7 + k)) * e);
                Vector2 q = p.at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * reach;
                Color mist = skin; mist.a = .75f * fade;
                float d = bead * (.32f - .14f * u);
                s.Glow(q.x, q.y, d, d, mist, 0f);
            }
            return true;
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
                float hit = GardenHitShake;
                if (lastStrike == null || superAge < .04f || superAge > .45f) return hit;
                float u = (superAge - .04f) / .41f;
                return hit + Mathf.Sin(superAge * 95f) * 9f * (1f - u) * (1f - u);
            }
        }
        // 2026-11-02: the small hit jolt - a .18 s decaying wobble of the given amplitude (px); a stronger kick takes over.
        float gardenHitKickAt = -10f, gardenHitKickAmp, gardenStreamKickAt = -10f, gardenBurstKickAt = -10f;
        void GardenHitKick(float amp) {
            float u = (time - gardenHitKickAt) / .18f;
            if (u >= 0f && u < 1f && gardenHitKickAmp * (1f - u) * (1f - u) > amp) return;
            gardenHitKickAt = time; gardenHitKickAmp = amp;
        }
        float GardenHitShake {
            get {
                float t = time - gardenHitKickAt, u = t / .18f;
                if (u < 0f || u >= 1f) return 0f;
                return Mathf.Sin(t * 110f) * gardenHitKickAmp * (1f - u) * (1f - u);
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
            // 2026-11-02 15:57 (user: a quality shock wave spreading from the bomb; art if code is not enough): the painted
            // ring (BombWave-20261102, four stages thick -> thin -> scattering -> gone). 17:29 (user: not seen when the bomb
            // went off; the build showed it hidden under the white flash, then thin white on the white beads and pale tray):
            // it now starts as the flash clears (.08 s), grows from .3x to 1.8x the blast radius over .7 s (easing out),
            // holds the thick first stage longest (stages at 0 / .38 / .66 / .86), stays full strength for the first half,
            // and a soft blue copy just behind it gives the white foam an edge on white beads.
            const float WaveStart = .08f, WaveTime = .7f;
            if (a >= WaveStart && a < WaveStart + WaveTime && GardenSkin.Has("plants/fx/bomb-wave-0")) {
                float wu = (a - WaveStart) / WaveTime, grow = 1f - Mathf.Pow(1f - wu, 2.2f);
                float size = R * 2f * Mathf.Lerp(.3f, 1.8f, grow) / .9f;
                int stage = wu < .38f ? 0 : wu < .66f ? 1 : wu < .86f ? 2 : 3;
                float alpha = wu < .5f ? 1f : Mathf.Pow(1f - (wu - .5f) / .5f, 1.2f);
                string id = "plants/fx/bomb-wave-" + stage;
                float back = size * 1.04f;
                GardenSkin.Asset(s, id, at.x - back * .5f, at.y - back * .5f + 2f, back, back, new Color(.35f, .55f, .95f, .45f * alpha));
                GardenSkin.Asset(s, id, at.x - size * .5f, at.y - size * .5f, size, size, new Color(1f, 1f, 1f, alpha));
            }
            // 11:09 (user: it looked program-drawn). Painted sequences only: the purple soap bubble bursting into a
            // foam ring (water-splash) across the blast, the bright bubble pop with its star flash (rocket-pop) at the
            // core, and a chain of foam bursts (impact-dust) going off around the ring one after another.
            bool art = GardenFx.Has("water-splash") && GardenFx.Has("rocket-pop") && GardenFx.Has("impact-dust");
            if (art) {
                // The painted front grows out from the impact, rather than playing in a fixed-size box.
                float wave = Mathf.Clamp01((a - .05f) / .55f);
                float spread = 1f - Mathf.Pow(1f - wave, 3f);
                // 2026-11-02: with the painted shock wave the old foam ring (water-splash) would be a second ring on top of it.
                if (!GardenSkin.Has("plants/fx/bomb-wave-0"))
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
        // 2026-11-06 (user: let it hit first, then move): while a spent member flies up to the field (and the .07 s it is
        // squashed there) the members on the pads keep their places; then they slide over to close the gap.
        // Not while a member is landing on a pad (it would land where a held member still stands). Drawing only: the
        // game holds the drawn x (PadSlideX); the poses themselves (original code) are untouched.
        // The game reports where a spent member was drawn when that differs from its pose (held by GardenHoldSlide), so
        // its flight starts there; matched to the new kamikaze by its start x.
        readonly List<Vector2> gardenTakeoffs = new List<Vector2>();   // pose x, drawn x
        readonly Dictionary<Kamikaze, float> gardenKamikazeX = new Dictionary<Kamikaze, float>();
        public void GardenNoteTakeoff(float poseX, float drawnX) { if (gardenTakeoffs.Count < 16) gardenTakeoffs.Add(new Vector2(poseX, drawnX)); }
        float GardenKamikazeX(Kamikaze k) {
            if (gardenKamikazeX.TryGetValue(k, out float x)) return x;
            for (int i = 0; i < gardenTakeoffs.Count; i++)
                if (Mathf.Abs(gardenTakeoffs[i].x - k.from.x) < 1f) { x = gardenTakeoffs[i].y; gardenTakeoffs.RemoveAt(i); gardenKamikazeX[k] = x; return x; }
            if (k.age > .05f) { gardenKamikazeX[k] = k.x; if (gardenTakeoffs.Count > 0 && kamikazes.Count <= 1) gardenTakeoffs.Clear(); }
            return k.x;   // the game may report the take-off later this frame
        }
        public bool GardenHoldSlide() {
            if (GardenPadCanvas <= 0f || kamikazes.Count == 0) return false;
            foreach (var pose in poses.Values) if (time - pose.born < .32f) return false;
            foreach (var k in kamikazes) if (!k.exploded || k.age - k.delay < .07f) return true;
            return false;
        }
        readonly Dictionary<Kamikaze, Vector2> gardenKamikazeHit = new Dictionary<Kamikaze, Vector2>();
        readonly List<Kamikaze> gardenKamikazeGone = new List<Kamikaze>();
        // The lowest bead of the whole field as drawn (FrontScreenY when the field is empty).
        float GardenFieldBottom() {
            float any = float.MinValue;
            for (int c = 0; c < battle.Level.columns; c++) {
                int front = battle.Front(c);
                if (front < 0 || !battle.Alive[front]) continue;
                float y = EnemyShown(front, ShownPoint(battle.EnemyX(front), battle.EnemyY(front))).y;
                if (y > any) any = y;
            }
            return any > float.MinValue ? any : FrontScreenY;
        }
        // The bubble field's lower edge as drawn: the lowest front bead within 36 px of screen x, or the lowest front
        // bead anywhere when that part of the field is already cleared; the original front line when none is left.
        Vector2 GardenFieldEdge(float x) {
            float near = float.MinValue, any = float.MinValue;
            for (int c = 0; c < battle.Level.columns; c++) {
                int front = battle.Front(c);
                if (front < 0 || !battle.Alive[front]) continue;
                Vector2 at = EnemyShown(front, ShownPoint(battle.EnemyX(front), battle.EnemyY(front)));
                if (at.y > any) any = at.y;
                if (Mathf.Abs(at.x - x) <= 36f && at.y > near) near = at.y;
            }
            float y = near > float.MinValue ? near : any > float.MinValue ? any : FrontScreenY;
            return new Vector2(x, y + 6f);
        }

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
                battle.Fired += GardenOnFired;
            }
            if (streamsBattle != battle) { streamsBattle = battle; streams.Clear(); }
            foreach (var ship in battle.Fleet) {
                if (ship.Tier < 2 || ship.BeamLife <= 0) continue;
                int slot = battle.Fleet.IndexOf(ship);
                var mouth = MouthOr(ship.Id, Position(ship.Id, slot));
                // 2026-10-18 23:40 (user: Ⅱ bullets too small). Stream balls are .48-.62 of the width, so width 10 made
                // 5-6 px balls next to the 12 px Ⅰ shots; now Ⅱ 18 (9-11 px balls) and MAX 22 (11-14 px).
                // 2026-10-22 10:51 (user): sizes as they were, except MAX now uses the Ⅱ width (18, was 22).
                Color streamTint = color(ship.Color);
                bool ring = GardenSkin.HasFlowerRing(streamTint);
                // 2026-10-31 23:36 (user: Ⅱ 3 道, MAX 4 道, same firing speed): the shots themselves are unchanged
                // (one target each, at the original rate); they are dealt round to 3 / 4 streams, each stream holding
                // on its own latest target for the beam's life (.18 s), so the member sprays several bubbles at once.
                if (streams.TryGetValue(ship.Id, out var st)) {
                    int n = StreamCount(ship.Tier);
                    for (int k = 0; k < n; k++) {
                        float since = battle.Time - st.at[k];
                        if (!(since >= 0f && since < .18f)) continue;
                        var from = mouth + new Vector2((k - (n - 1) * .5f) * 5f, 0f);
                        // 2026-11-01: fanned apart in the air (3: -48 / 0 / +48, 4: -60 / -20 / +20 / +60) so they read as separate streams.
                        float fan = (k - (n - 1) * .5f) * (n > 3 ? 40f : 48f);
                        GardenFx.BubbleStream(s, from, ShownPoint(st.to[k].x, st.to[k].y), 18, streamTint, time + k * .13f, ring, ship.Tier, fan, true, k == 0);
                    }
                }
                else GardenFx.BubbleStream(s, mouth, ShownPoint(ship.BeamX, ship.BeamY), 18, streamTint, time, ring, ship.Tier);
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
                else if (shot.Tier == 1) from.x += (shot.Barrel - .5f) * 8f;   // 2026-10-31: Ⅰ fires 2 a volley (was 3)
                // v3 (UI spec v3 4.4, user decision 2026-10-15 20:28): every projectile is a SOLID glossy ball in the
                // shooter's color (the bead texture), 12 px, with smaller solid after-images. No see-through bubble.
                // 2026-10-21 10:12 (user: the shots cover the face; small at the mouth, growing). Each ball is sized by
                // its distance from the mouth (GardenFx.MouthGrow: a quarter at the lips, full once past the head).
                // After-images are no longer parked at the mouth before the ball has moved (they were clamped to the
                // start, so with a shot every .07 s a ball sat on the face most of the time); a third one tightens the
                // trail into a jet. (10:39: the three tiny droplets sprayed from the lips read as a fine splash; removed.)
                Color tint = color(f.color);
                // 2026-10-28 10:40 (user): with the flower ring a shot leaves the ring as a soap bubble and turns into the
                // solid ball as it flies (GardenSkin.BlownShot: 11:47 it condenses in a blink); no MouthGrow.
                bool blown = !shot.Rocket && GardenSkin.HasFlowerRing(tint);
                for (int g = 3; g >= 0; g--) {
                    float age = shot.Age - g * .018f;
                    if (g > 0 && age <= 0f) continue;
                    float tq = Mathf.Clamp01(age / shot.Duration);
                    var q = blown ? GardenSkin.BlownCurve(from, to, GardenSkin.BlownEase(tq), shot.Tier == 1 ? (shot.Barrel - .5f) * 10f : (shot.Barrel - 1) * 7f) : Vector2.Lerp(from, to, tq);
                    float size = 12f - g * 2.2f, dist = (q - from).magnitude;
                    if (blown) {
                        if (g > 0 && dist < GardenSkin.BlownCondense + GardenSkin.BlownCondenseSpan) continue;   // no trailing images of the bubble
                        GardenSkin.BlownShot(s, tint, q, size * GardenSkin.BlownSoapScale, size, dist, 1f, g == 0);
                    }
                    else GardenSkin.Shot(s, tint, q, size * GardenFx.MouthGrow(dist), 1f);
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
            if (gardenKamikazeHit.Count > kamikazes.Count || gardenKamikazeX.Count > kamikazes.Count) {
                gardenKamikazeGone.Clear();
                foreach (var key in gardenKamikazeHit.Keys) if (!kamikazes.Contains(key)) gardenKamikazeGone.Add(key);
                foreach (var key in gardenKamikazeX.Keys) if (!kamikazes.Contains(key) && !gardenKamikazeGone.Contains(key)) gardenKamikazeGone.Add(key);
                foreach (var key in gardenKamikazeGone) { gardenKamikazeHit.Remove(key); gardenKamikazeX.Remove(key); }
            }
            foreach (var k in kamikazes) {
                // f53886/f53878: approach during delay, then impact at exploded; consume the source clock.
                // 2026-11-05 (user): it flies to the bubble field's current lower edge above its pad (GardenFieldEdge),
                // not to the original front line, which stays at the full board's bottom row as the field is cleared.
                // The burst stays where it hit while the field is pushed back up.
                float kx = GardenKamikazeX(k);
                if (!gardenKamikazeHit.TryGetValue(k, out var hit)) {
                    hit = GardenFieldEdge(kx);
                    if (k.exploded) { gardenKamikazeHit[k] = hit; GardenHitKick(4f); }   // 11-06: a jolt of the field on impact
                }
                if (!k.exploded) {
                    float t = Mathf.Clamp01((k.age / k.delay - .25f) / .75f);
                    if (GardenPadScaleOf != null && GardenPadFeetAt != null && GardenPadCanvas > 0f) {
                        // 2026-11-05 (user: one toy flying up, not two): the pad farewell is off while this runs, so the
                        // member itself takes off - same size and feet as on its pad - until its head is in the field's edge.
                        float scale = GardenPadScaleOf(k.tier), body = GardenPadCanvas * scale;
                        float feet = Mathf.Lerp(GardenPadFeetAt(k.from.y), hit.y - 8f + body * .8f, t * t);
                        if (t > 0) GardenFx.Draw(s, "dash-trail", new Vector2(kx, feet + 27f), 36, 96, color(k.color));
                        GardenSkin.Creature(s, color(k.color), kx, feet, GardenPadCanvas, k.tier, "", 0f, 12f, false, false,
                            0f, 0f, scale);
                    } else {
                        var p = Vector2.Lerp(k.from, hit, t * t);
                        if (t > 0) GardenFx.Draw(s, "dash-trail", p + new Vector2(0, 45), 36, 96, color(k.color));
                        GardenSkin.Plant(s, color(k.color), p, k.tier, "0");
                    }
                } else {
                    // 2026-11-05 (user): the spent member bursts as a soap bubble where it hits the field (was a dust puff).
                    // 11-06 (user: too weak): first it is squashed flat against the edge for .07 s, and the burst is bigger.
                    float hitAge = k.age - k.delay;
                    if (hitAge < .07f && GardenPadScaleOf != null && GardenPadCanvas > 0f) {
                        float scale = GardenPadScaleOf(k.tier), body = GardenPadCanvas * scale, q = hitAge / .07f;
                        GardenSkin.Creature(s, color(k.color), kx, hit.y - 8f + body * .8f * (1f - .35f * (1f - q)), GardenPadCanvas, k.tier, "",
                            0f, 12f, false, false, 0f, 0f, scale, .35f * (1f - q));
                    }
                    if (!GardenFx.SoapPop(s, hit, 54f, color(k.color), hitAge, k.color * 7 + (int)k.x))
                        GardenFx.Burst(s, "impact-dust", hit, 80, hitAge, .5f);
                }
            }
            float fieldBottom = GardenFieldBottom();
            foreach (var wave in waves) {
                float t = Mathf.Clamp01(wave.age / wave.duration);
                bool revive = wave.kind == RepelKind.Revive;
                // 2026-11-05 (user): merges and spent members each push the field and each started a gust, so a merge chain
                // stacked several. A gust with a newer one less than .3 s behind it is not drawn; the newer one is drawn
                // brighter and rises further for each gust it took over. It starts at the field's current lower edge
                // (FrontScreenY stays at the full board's bottom row).
                int merged = 0;
                if (!revive) {
                    bool absorbed = false;
                    foreach (var other in waves) {
                        if (other == wave || other.kind == RepelKind.Revive) continue;
                        if (other.age < wave.age && wave.age - other.age < .3f) { absorbed = true; break; }
                        if (other.age > wave.age && other.age - wave.age < .3f) merged++;
                    }
                    if (absorbed) continue;
                }
                float boost = 1f + .3f * Mathf.Min(merged, 3);
                float y = (revive ? 574 : fieldBottom) - t * (revive ? 260 : 50 * boost);
                float h = revive ? 180 : 60;
                // 2026-10-31 00:27 (user, after the reference): no blue water any more - a wavy gold-and-white light ribbon in
                // the gap under the bubble field (following its edge as it is pushed up), a band of small foam bubbles,
                // coloured bubbles popping out of it and gold glints. All of it stays inside the tray (logical y <= 568).
                if (revive)
                {
                    const float floor = 568f;
                    float edge = Mathf.Min(FrontScreenY + 16f, floor - 12f);
                    float k = Mathf.Clamp01(wave.age / .2f) * Mathf.Clamp01((1f - t) / .35f), ph = wave.age * 3.2f;
                    s.Glow(270, edge, 640, 70, new Color(1f, .93f, .7f, .4f * k), 0f);
                    // 10:53: the painted ribbon (ReviveSuccess V3) when present: across the tray, bobbing gently,
                    // with the coloured bubbles below still rising out of it.
                    bool painted = GardenSkin.Has("scene/revive-ribbon");
                    if (painted)
                    {
                        // 11:11 (checked against the reference): fuller and longer - a third taller, holding at full
                        // strength until the last fifth of the wave (it had faded before the title was even settled).
                        const float rw = 532f, rh = rw * 200f / 1080f * 1.33f;
                        float kr = Mathf.Clamp01(wave.age / .15f) * Mathf.Clamp01((1f - t) / .2f);
                        float ry = Mathf.Min(edge, floor - rh * .32f) + Mathf.Sin(wave.age * 4f) * 2.5f;
                        GardenSkin.Asset(s, "scene/revive-ribbon", 4f, ry - rh * .5f, rw, rh, new Color(1f, 1f, 1f, kr));
                    }
                    Vector2 prevA = default, prevB = default;
                    for (int i = 0; i <= (painted ? -1 : 45); i++)
                    {
                        float x = -12f + i * 12.8f;
                        var a = new Vector2(x, edge + Mathf.Sin(x * .016f + ph) * 7f);
                        var b = new Vector2(x, edge + Mathf.Sin(x * .016f + ph + 2.3f) * 5f + 2f);
                        if (i > 0)
                        {
                            s.GlowLine(prevA, a, 26f, new Color(1f, .82f, .35f, .35f * k));
                            s.GlowLine(prevA, a, 6f, new Color(1f, .9f, .55f, .95f * k));
                            s.GlowLine(prevB, b, 4f, new Color(1f, 1f, 1f, .9f * k));
                        }
                        prevA = a; prevB = b;
                    }
                    for (int i = 0; i < (painted ? 0 : 46); i++)   // foam: small white bubbles hugging the ribbon
                    {
                        float x = (i * 53.7f + 11f) % 540f, yy = edge - 14f + (i * 37 % 26) + Mathf.Sin(wave.age * 4f + i) * 2f;
                        GardenSkin.Shot(s, Color.white, new Vector2(x, Mathf.Min(yy, floor)), 5f + (i % 4) * 1.6f, .55f * k);
                    }
                    if (painted)
                    {
                        // 11:46 (user: 按建议 - the art band plus the preview's extras): clear soap bubbles in the foam and
                        // gold stars twinkling along the band, over the painted ribbon.
                        float kr2 = Mathf.Clamp01(wave.age / .15f) * Mathf.Clamp01((1f - t) / .2f);
                        bool soap = GardenSkin.Has("plants/home/soap-bubble");
                        for (int i = 0; i < 12 && soap; i++)
                        {
                            float d = 12f + (i % 4) * 5f, x = (i * 83f + 30f) % 530f + 5f;
                            float yy = Mathf.Min(edge - 12f - (i * 29 % 30) - Mathf.Sin(wave.age * 2.5f + i) * 3f, floor - d * .5f);
                            GardenSkin.Asset(s, "plants/home/soap-bubble", x - d * .5f, yy - d * .5f, d, d, new Color(1f, 1f, 1f, .85f * kr2));
                        }
                        for (int i = 0; i < 16; i++)
                        {
                            float tw = .55f + .45f * Mathf.Sin(wave.age * 6f + i * 1.9f), d = (14f + (i % 3) * 8f) * (.6f + .4f * tw);
                            float x = 15f + i * 34f + (i % 3) * 6f, yy = Mathf.Min(edge - 10f + Mathf.Sin(i * 2.1f) * 20f, floor - d * .5f);
                            GardenSkin.Asset(s, "scene/revive-sparkle", x - d * .5f, yy - d * .5f, d, d, new Color(1f, 1f, 1f, kr2 * (.45f + .55f * tw)));
                        }
                    }
                    for (int i = 0; i < (painted ? 30 : 20); i++)   // coloured bubbles popping up out of the band
                    {
                        float u = (wave.age * (.7f + (i % 5) * .1f) + i * .23f) % 1f, x = 14f + (i * 97 % 512);
                        float yy = Mathf.Min(edge + 6f - u * 42f + Mathf.Sin(i * 1.7f) * 6f, floor), tw = Mathf.Sin(u * Mathf.PI) * k;
                        Color bc = (i % 3) switch { 0 => new Color(1f, .58f, .2f), 1 => new Color(1f, .85f, .25f), _ => new Color(.25f, .6f, 1f) };
                        GardenSkin.Shot(s, bc, new Vector2(x, yy), 8f + (i % 3) * 2.5f, .9f * tw);
                    }
                    for (int i = 0; i < (painted ? 0 : 9); i++)   // gold glints (the painted ribbon has its own)
                    {
                        float u = (wave.age * 1.3f + i * .31f) % 1f, tw = Mathf.Sin(u * Mathf.PI) * k;
                        float x = 30f + i * 60f, yy = Mathf.Min(edge - 8f + Mathf.Sin(i * 2.1f) * 16f, floor - 4f), r = i % 2 == 0 ? 11f : 7f;
                        s.Glow(x, yy, r * 2.6f, r * 2.6f, new Color(1f, .9f, .5f, .45f * tw), 0f);
                        s.Glow(x, yy, r * 3.6f, r * .5f, new Color(1f, .97f, .8f, tw), 0f);
                        s.Glow(x, yy, r * .5f, r * 3.6f, new Color(1f, .97f, .8f, tw), 0f);
                    }
                }
                else
                GardenFx.Draw(s, revive ? "revive-wave" : "repel-gust", new Vector2(270, y - h * .5f), 540, h,
                    new Color(1, 1, 1, Mathf.Min(1f, (revive ? .85f : .65f) * boost) * (1 - t)));
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
        // f53915 DrawPurge: same six columns, timing and front line.
        // 2026-10-31 (user, revive reference): the revive shows the light ribbon (DrawGardenFeedback) - the rescue
        // bubbles that dropped from the top onto the front row and the 「泡泡援军 ×N」 bar (it sat on the ribbon) are
        // gone.
        // 10:22 (user screenshot): the foam bursts spilled below the tray as big white clusters - gone too.
        void DrawBubblePurge(PixelSurface s) { }

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
            // Reserve room for the largest punch; the label follows the actual digit edge below.
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
                // 11:51 (user): 「连击 ×」 in the same lettering and colours as the digits; the life bar is gone.
                // 2026-10-28 (user, after 沙画消消): no more dim/waver warning. The chain window is now 2 s (Battle.Hits /
                // Battle.Tick, was 1.4 s). Like 沙画消消, the counter sits still for 1 s without a hit, then the whole
                // counter flies off to the right over 1 s with a back-in ease (small wind-up to the left, then accelerating
                // away) and is fully gone at the moment the model breaks the chain (ComboLife 0, 2 s). A hit during
                // the flight refills ComboLife, so the counter snaps straight home and the chain really is rescued.
                // After the break ComboLife stays 0, so the fade-out happens off-screen.
                Color hi = Mix(color, Color.white, .7f), lo = Mix(color, GardenBars.Navy, .15f);
                const float flyStart = .5f, back = 1.70158f;
                float life = Mathf.Clamp01(battle.ComboLife), fly = life < flyStart ? 1f - life / flyStart : 0f;
                float flyX = 600f * fly * fly * ((back + 1f) * fly - back);
                float a = fade, homeX = digitX + flyX;
                const float labelSize = 24f;
                float labelW = GardenBarArt.TextWidth("连击 ×", labelSize), digitLeft = homeX - GardenBarArt.TextWidth(text, size) * .5f;
                DrawComboHudText(s, "连击 ×", digitLeft - 5f - labelW * .5f, badgeY + 1f, labelSize, hi, lo, a);
                DrawComboHudText(s, text, homeX, badgeY - 2f, size, hi, lo, a);
            }
            finally { s.MapLayout = badgeMapped; }
        }
    }
}
#endif
