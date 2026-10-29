#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // 2026-10-15: body about 55 px with 8 pads (mockup scale).
        float GardenBayCanvas()
        {
            float baySpacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            return Mathf.Min(118f, baySpacing * 1.45f);
        }

        // The locked 160x160 sprite contains the same 160x64 pad at y=96.
        // Match its rendered base, not its full-image center, on every viewport.
        float GardenPadLift()
        {
            if (!PlushLayout.Enabled) return 0f;
            float spacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            float width = Mathf.Min(70f, spacing - 5f);
            return PlushLayout.Y(647f) - (PlushLayout.Y(647f - width * .25f) + width * .3f);
        }
        float GardenRaisedPadY(float y, float progress = 1f) =>
            PlushLayout.InverseY(PlushLayout.Y(y) - GardenPadLift() * progress);
        float GardenPadFeet(int fighterId, float feet)
        {
            float progress = feedback.GardenLaunch(fighterId, out float age, out _) ? Mathf.Clamp01(age / LaunchTime) : 1f;
            return GardenRaisedPadY(feet, progress);
        }

        // 2026-10-16 (user item 5): a smaller start and wider steps so each upgrade reads; tier 3 keeps the old size.
        // 16:51 (user: a merge did not visibly grow the member): steps widened again (+21 %, +16 %, +12 %; MAX stays
        // about the old 1.18 so neighbours on 7 pads do not overlap).
        // 2026-10-22 10:30 (user: bigger on phones): tier 0 +12 % (.76 -> .85); MAX stays (on 8 pads it nearly touches
        // its neighbours), so the steps narrow to +15 % / +11 % / +10 %.
        static float PadTierScale(int tier) => tier >= 3 ? 1.2f : tier == 2 ? 1.09f : tier == 1 ? .98f : .85f;
        readonly Dictionary<int, int> padTier = new Dictionary<int, int>(), padFromTier = new Dictionary<int, int>();
        readonly Dictionary<int, float> padPopAt = new Dictionary<int, float>();
        readonly Dictionary<int, Vector2> padMouth = new Dictionary<int, Vector2>();   // mouth as last drawn, per member
        Flightbeat.Model.Battle padBattle;
        // Size of a pad member; when its tier goes up it swells and springs back over 0.4 s.
        float PadScale(Fighter ship)
        {
            if (padBattle != Battle) { padBattle = Battle; padTier.Clear(); padPopAt.Clear(); padFromTier.Clear(); padMouth.Clear(); }
            if (padTier.TryGetValue(ship.Id, out int was) && ship.Tier > was) { padPopAt[ship.Id] = ui; padFromTier[ship.Id] = was; }
            padTier[ship.Id] = ship.Tier;
            // The growth used to play in the first 0.4 s, hidden under the partners rushing in and the white merge
            // flash (.22 s). Now the member keeps its old size until the burst, then springs up to the new size with
            // an overshoot over 0.55 s, so the change is seen.
            if (padPopAt.TryGetValue(ship.Id, out float at) && padFromTier.TryGetValue(ship.Id, out int from))
            {
                float p = (ui - at - .22f) / .55f;
                if (p < 0f) return PadTierScale(from);
                if (p < 1f) return Mathf.LerpUnclamped(PadTierScale(from), PadTierScale(ship.Tier), OutBack(p, 3.2f));
            }
            return PadTierScale(ship.Tier);
        }

        // 2026-10-16 22:09 (user: sit down on the pad with some weight). Drawing only, inside the original 0.32 s
        // launch (the member cannot fire before Arrival ends): it hops higher than the original 18 px arc, then
        // drops faster and faster onto the pad; on touchdown it squashes and springs back, the pad flashes, foam
        // puffs at its feet and a few small bubbles splash sideways. Returns the squash for Creature.
        const float LaunchTime = .32f, LandTime = .34f;
        float PadLanding(PixelSurface s, Fighter ship, int index, ref Vector2 at, float bayCanvas)
        {
            if (!feedback.GardenLaunch(ship.Id, out float age, out Vector2 from)) return 0f;
            float padX = Battle.FleetPositionX(Battle.VisualSlot(index));
            Color tint = GardenColor(ship.Color);
            if (age < LaunchTime)
            {
                float p = age / LaunchTime, q = Mathf.Pow(p, .7f);
                float x = Mathf.Lerp(from.x, padX, 1f - (1f - p) * (1f - p));
                float y = Mathf.Lerp(from.y, 628f, p) - 58f * Mathf.Sin(q * Mathf.PI);
                at = new Vector2(x, y);
                return q > .5f ? -.12f * Mathf.Sin((q - .5f) * Mathf.PI) : -.06f;   // stretched while dropping
            }
            float t = age - LaunchTime;
            if (t >= LandTime + .2f) return 0f;
            float feet = 640f;
            if (t < .3f) s.Glow(padX, GardenRaisedPadY(647f), 90f, 40f, new Color(1f, .95f, .75f, (1f - t / .3f) * .55f), 0f);
            GardenFx.Burst(s, "impact-dust", new Vector2(padX, GardenRaisedPadY(feet - 6f)), 78f, t, .45f);
            for (int k = 0; k < 6; k++)
            {
                float u = t / .4f;
                if (u >= 1f) break;
                float side = k % 2 == 0 ? -1f : 1f, speed = 55f + k * 9f;
                Vector2 p = new Vector2(padX + side * speed * t, GardenRaisedPadY(feet - 6f - 70f * t + 240f * t * t));
                GardenSkin.Shot(s, tint, p, 6f * (1f - u * .5f), 1f - u);
            }
            if (t >= LandTime) return 0f;
            return .22f * Mathf.Exp(-t * 9f) * Mathf.Cos(t / LandTime * Mathf.PI * 2.5f);
        }

        // 2026-10-26 (user: the toys on the pads should fire with punch, like the PvZ plants). 10-27 11:15 (user):
        // back to the clean version after the body-warp trials (beat pump, sideways lean, turning round, reaching up,
        // growing towards the camera, a stalk) did not read as bending forward: the member's own motion is the
        // original one again; only the effects stay. While firing, the bubble ring, droplets and pot puffs keep a ~.36 s
        // cadence, and after ~1.6 s of steady fire the member goes "all out" (a glow of its colour and speed lines).
        // A member with the hand-drawn pose frames (GardenSkin.BendPose: `reach` bending forward and blowing, optional
        // `lean` leaning back with puffed cheeks) blows in bursts while firing: reach for ~.9 s, then lean (or the
        // puffed frame) for ~.33 s, again; it leans back while ready. Such a member is drawn after the others (`front`)
        // as its reaching body may pass over its neighbours. Drawing only: reads Battle.Time, Ammo and LastFired.
        // The warp/stalk machinery in GardenSkin (BendActive, BendExtend, ...) is kept for later but not used.
        class PadBeat { public float phase, start, last, stopped = -10f; public bool firing; }
        struct PadPump {
            public float frenzy, ring, thrust, period;
            public int pose;
            public bool inhale, front, noRing;
            public GardenSkin.PadCombatPose combat;
            public GardenSkin.PadFlowerPose flower;
        }
        readonly Dictionary<int, PadBeat> padBeat = new Dictionary<int, PadBeat>();
        readonly List<PadPump> padPumps = new List<PadPump>();
        Flightbeat.Model.Battle padBeatBattle;
        PadPump PadPumpFor(Fighter ship, float fire)
        {
            if (padBeatBattle != Battle) { padBeatBattle = Battle; padBeat.Clear(); }
            float now = Battle.Time;
            if (!padBeat.TryGetValue(ship.Id, out PadBeat b)) { b = new PadBeat { last = now }; padBeat[ship.Id] = b; }
            float dt = Mathf.Clamp(now - b.last, 0f, .1f); b.last = now;
            bool firing = fire > .5f && ship.Ammo > 0;
            if (firing && !b.firing) { b.start = now; b.phase = .45f; }
            if (!firing && b.firing) b.stopped = now;
            b.firing = firing;
            var p = new PadPump { ring = -1f, period = .36f };
            Color tint = GardenColor(ship.Color);
            bool flowerRing = GardenSkin.HasFlowerRing(tint);
            if (ship.Ammo <= 0) { if (flowerRing) p.flower = GardenSkin.FlowerSpent(); return p; }
            // 2026-10-28 (user: concept B 花朵泡泡环): the member keeps its original body and frames; the flower on its
            // head sways while it waits and, while it fires, swells a bubble and pops it off every beat.
            if (flowerRing)
            {
                float side = ship.Id % 2 == 0 ? 1f : -1f;
                if (firing)
                {
                    p.frenzy = Mathf.Clamp01((now - b.start - 1.6f) / 1.2f) * .5f;
                    p.period = Mathf.Lerp(.36f, .3f, p.frenzy * 2f);
                    b.phase += dt / p.period;
                    float f = b.phase - Mathf.Floor(b.phase);
                    p.flower = GardenSkin.FlowerBlow(f, now, side);
                    p.ring = f >= .7f ? (f - .7f) / .3f : -1f;                      // droplets and pot puffs with the pop
                    p.thrust = p.ring >= 0f ? Mathf.Exp(-6f * p.ring) : 0f;
                    p.noRing = true;
                }
                else p.flower = GardenSkin.FlowerRest(now, ship.Id * 1.3f);
                return p;
            }
            if (GardenSkin.HasPadCombat(tint))
            {
                float side = ship.Id % 2 == 0 ? 1f : -1f;
                // 10-27 21:14 (user): ready on the pad = the inhale pose; attacking = the forward blowing pose held for as
                // long as it fires (no cycling back to the inhale); after firing a short rebound, then ready again.
                if (firing)
                {
                    p.combat = GardenSkin.PadCombatBlow(now - b.start, side);
                    b.phase += dt / .36f;                                          // droplets / pot puffs keep a beat
                    float f = b.phase - Mathf.Floor(b.phase);
                    p.ring = f >= .5f ? (f - .5f) / .5f : -1f;
                    p.period = .36f;
                    p.thrust = p.ring >= 0f ? Mathf.Exp(-6f * p.ring) : 0f;
                    // The body carries the effort; the existing glow stays behind it at a lower gain.
                    p.frenzy = Mathf.Clamp01((now - b.start - 1.6f) / 1.2f) * .3f;
                    p.front = true;
                    p.noRing = true;   // 10-27 19:50: the white ring sat on the bigger face; droplets and pot puffs stay
                }
                else if (ship.Arrival <= 0f)                                       // landed on the pad
                    p.combat = now - b.stopped < .15f ? GardenSkin.PadCombatRelease(now - b.stopped)
                        : GardenSkin.PadCombatReady(now + ship.Id * .7f);
                return p;
            }
            bool reach = GardenSkin.Has(GardenSkin.PlantSlot(tint, "reach")), lean = GardenSkin.Has(GardenSkin.PlantSlot(tint, "lean"));
            bool breathIn = false;
            if (firing)
            {
                p.frenzy = Mathf.Clamp01((now - b.start - 1.6f) / 1.2f);
                p.period = Mathf.Lerp(.36f, .26f, p.frenzy);
                b.phase += dt / p.period;
                float f = b.phase - Mathf.Floor(b.phase);
                p.ring = f >= .5f ? (f - .5f) / .5f : -1f;
                if (reach)
                {
                    float cycle = Mathf.Lerp(1.25f, 1f, p.frenzy), tc = (now - b.start) % cycle;
                    breathIn = tc > cycle - .33f;
                    if (breathIn) p.ring = -1f;                                // no bubble ring while taking a breath
                }
                p.thrust = p.ring >= 0f ? Mathf.Exp(-6f * p.ring) : 0f;
            }
            if (reach)
            {
                p.pose = firing && !breathIn ? 1 : lean ? 2 : 0;
                p.inhale = firing && breathIn && !lean;                        // no lean frame: the puffed frame
                p.front = p.pose == 1;
            }
            return p;
        }
        // behind: the all-out glow and speed lines (before the member is drawn); otherwise the ring, droplets and pot puffs.
        void PadPumpFx(PixelSurface s, Fighter ship, PadPump p, Vector2 mouth, float x, float feet, float bodyH, bool behind)
        {
            Color tint = GardenColor(ship.Color);
            if (behind)
            {
                if (p.frenzy <= .01f) return;
                s.Glow(x, feet - bodyH * .42f, bodyH * 1.9f, bodyH * 1.9f, new Color(tint.r, tint.g, tint.b, .38f * p.frenzy * (.6f + .4f * p.thrust)), 0f);
                s.Glow(x, feet - bodyH * .42f, bodyH * 1.2f, bodyH * 1.2f, new Color(1f, 1f, .9f, .22f * p.frenzy * p.thrust), 0f);
                bool mapped = s.MapLayout; s.MapLayout = false;
                try
                {
                    for (int k = 0; k < 4; k++)
                    {
                        float ph = (Battle.Time * 2.6f + k * .27f + ship.Id * .13f) % 1f, side = k % 2 == 0 ? -1f : 1f;
                        Vector2 a = PlushLayout.Point(new Vector2(x + side * bodyH * (.42f + .06f * k), feet - bodyH * (.1f + .9f * ph)));
                        s.Segment(a, a + new Vector2(0f, bodyH * .22f), 2.2f, new Color(1f, 1f, 1f, .8f * p.frenzy * Mathf.Sin(ph * Mathf.PI)));
                    }
                }
                finally { s.MapLayout = mapped; }
                return;
            }
            if (p.ring < 0f || float.IsNaN(mouth.x)) return;
            float u = p.ring, age = u * p.period * .5f;
            if (u < .6f)
            {
                float v = u / .6f;
                for (int side = -1; side <= 1; side += 2)
                    s.Glow(x + side * (bodyH * .34f + 18f * v), feet - 3f, 10f + 12f * v, 7f + 6f * v, new Color(1f, 1f, 1f, .6f * (1f - v)), 0f);
            }
            bool wasMapped = s.MapLayout; s.MapLayout = false;
            try
            {
                Vector2 m = PlushLayout.Point(mouth);
                Vector2 c = m + new Vector2(0f, -bodyH * .22f * u);
                float r = bodyH * (.07f + .22f * Mathf.Sqrt(u)), a = Mathf.Pow(1f - u, 1.3f);
                if (!p.noRing) s.Glow(c.x, c.y, r * 2.4f, r * 1.8f, new Color(tint.r, tint.g, tint.b, .35f * a), 0f);
                Vector2 prev = c + new Vector2(r, 0f);
                for (int i = 1; i <= 20 && !p.noRing; i++)
                {
                    float t = i / 20f * Mathf.PI * 2f;
                    Vector2 next = c + new Vector2(Mathf.Cos(t) * r, Mathf.Sin(t) * r * .72f);
                    s.Segment(prev, next, .8f + 2.4f * (1f - u), new Color(1f, 1f, 1f, .95f * a));
                    prev = next;
                }
                for (int k = 0; k < 4; k++)
                {
                    float side = k % 2 == 0 ? -1f : 1f, speed = 70f + k * 22f;
                    Vector2 d = m + new Vector2(side * speed * age, -90f * age + 520f * age * age - k * 2f);
                    GardenSkin.Shot(s, tint, d, (k < 2 ? 6f : 4.5f) * (1f - .4f * u), a);
                }
            }
            finally { s.MapLayout = wasMapped; }
        }

        // Shots leave from the member's mouth. 2026-10-21: each member's own mouth (GardenSkin.MouthCanvas), taken from
        // the pad drawing so it follows the squash, the size pop, the art scale and the shake; computed here otherwise.
        Vector2 GardenMouth(int fighterId)
        {
            var fleet = Battle.Fleet;
            for (int i = 0; i < fleet.Count; i++)
            {
                if (fleet[i].Id != fighterId) continue;
                if (padMouth.TryGetValue(fighterId, out Vector2 drawn)) return drawn;
                Vector2 at = feedback.Position(fighterId, i);
                Color tint = GardenColor(fleet[i].Color);
                float ah = GardenBayCanvas() * PadScale(fleet[i]) * ArtScale(tint);
                Vector2 m = MouthCanvas(tint);
                float mouthY = PlushLayout.Y(GardenPadFeet(fighterId, at.y + 12f)) - ah * (FeetLine - m.y / 256f);
                return new Vector2(at.x + ah * ArtAspect * (m.x / 224f - .5f), PlushLayout.InverseY(mouthY));
            }
            return new Vector2(float.NaN, float.NaN);
        }
        // 2026-10-19 09:45 (user: 等待 kept flashing on and off). Battle.Exposed skips beads already reserved by shots in
        // flight, so it flips for single frames while a color is being cleared. The label now has hysteresis: it turns on
        // after 0.4 s without a free target, off after 0.4 s with one, and fades in/out (about 0.15 s).
        class PadWait { public bool shown; public float since, alpha; public bool last; }
        readonly Dictionary<int, PadWait> padWait = new Dictionary<int, PadWait>();
        Flightbeat.Model.Battle padWaitBattle;
        float PadWaitAlpha(Fighter ship)
        {
            if (padWaitBattle != Battle) { padWaitBattle = Battle; padWait.Clear(); }
            if (!padWait.TryGetValue(ship.Id, out PadWait w)) { w = new PadWait { since = ui, last = true }; padWait[ship.Id] = w; }
            bool blocked = ship.Arrival <= 0f && !Battle.Exposed(ship.Color);
            if (blocked != w.last) { w.last = blocked; w.since = ui; }
            if (blocked != w.shown && ui - w.since >= .4f) w.shown = blocked;
            w.alpha = Mathf.MoveTowards(w.alpha, w.shown ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            return w.alpha;
        }

        // Where a bead is drawn (sand interpolation and smoothed advance), so shots land on the visible bead.
        Vector2 GardenEnemyAt(int index)
        {
            if (index < 0 || index >= Battle.Alive.Length) return new Vector2(float.NaN, float.NaN);
            GardenSandAt(index, out float gx, out float gy);   // 2026-10-21: the falling/rolling bead as drawn
            return ScreenLayout.EnemyPoint(gx, gy + Battle.Offset + feedback.GardenOffsetDelta);
        }

        // 2026-10-16 22:29 (the dashed aim art was too faint on the bead field). The true area is the original
        // reticle's circle (SuperStrikeVisuals.Reticle, screen radius 120/150 x (bombRadius + 1)); it gets a warm
        // tinted fill, a thick white ring with a navy edge, turning gold dashes and a centre mark.
        float BombRadius => (aimMissile ? 150f : 120f) * (Battle.Loadout.bombRadius + 1f);
        bool InBombCircle(Vector2 mapPoint)
        {
            Vector2 a = PlushLayout.Point(aim), p = PlushLayout.Point(mapPoint);
            return (p - a).sqrMagnitude <= BombRadius * BombRadius;
        }
        void DrawBombAim(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            Vector2 c = PlushLayout.Point(aim);
            float pulse = Mathf.Sin(ui * 5f) * .5f + .5f, r = BombRadius * (1f + .015f * pulse);
            Vector2 Rim(float angle, float radius) => c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            s.Glow(c.x, c.y, r * 2.2f, r * 2.2f, new Color(1f, .45f, .3f, .28f + .1f * pulse), 0f);
            for (int pass = 0; pass < 2; pass++)
            {
                Vector2 prev = Rim(0f, r);
                for (int i = 1; i <= 64; i++)
                {
                    float a = i / 64f * Mathf.PI * 2f;
                    Vector2 next = Rim(a, r);
                    s.Segment(prev, next, pass == 0 ? 7f : 3.5f, pass == 0 ? new Color(.1f, .2f, .4f, .85f) : Color.white);
                    prev = next;
                }
            }
            for (int i = 0; i < 12; i++)
            {
                float start = ui * .9f + i * Mathf.PI / 6f;
                Vector2 prev = Rim(start, r + 7f);
                for (int j = 1; j <= 3; j++)
                {
                    float a = start + j * .1f;
                    Vector2 next = Rim(a, r + 7f);
                    s.Segment(prev, next, 3f, new Color(1f, .82f, .3f, .9f));
                    prev = next;
                }
            }
            float m = 9f + 2f * pulse;
            s.Segment(c + new Vector2(-m - 6f, 0f), c + new Vector2(-m * .4f, 0f), 3f, Color.white);
            s.Segment(c + new Vector2(m * .4f, 0f), c + new Vector2(m + 6f, 0f), 3f, Color.white);
            s.Segment(c + new Vector2(0f, -m - 6f), c + new Vector2(0f, -m * .4f), 3f, Color.white);
            s.Segment(c + new Vector2(0f, m * .4f), c + new Vector2(0f, m + 6f), 3f, Color.white);
            s.Glow(c.x, c.y, 10f, 10f, new Color(1f, .5f, .35f, 1f), 0f);
            s.MapLayout = mapped;
        }

        // Mystery members reveal their color at the queue head and can merge after deployment.
        // The source WouldMerge still excludes Kind 2, so the bubble hint uses the revealed color.
        internal static bool GardenMergeHintVisible(Flightbeat.Model.Battle battle, int lane)
        {
            if (battle == null || !battle.Active || battle.ActiveFinale != null
                || lane < 0 || lane >= battle.Reserve.Length || battle.Reserve[lane].Count == 0
                || battle.Fleet.Count >= battle.UnlockedSlots) return false;
            int color = battle.Reserve[lane][0].Color, matching = 0;
            foreach (Fighter ship in battle.Fleet)
                if (ship.Color == color) matching++;
            return matching % 3 == 2;
        }

        // 2026-10-16 (user item 4): the original's golden 「合」 disc (f54834) as a bubble badge instead of the word 可合成.
        // 2026-10-21 10:21 (user): back to the gold coin with 合 (the glass chip did not suit it), with more life: it pops
        // in with an overshoot when the hint appears, keeps its heartbeat, bobs a little and a sparkle glints on its rim.
        void MergeBadge(PixelSurface s, float x, float y, float queueScale, int lane)
        {
            float age = BarAge(8 + Mathf.Clamp(lane, 0, 7));
            float pop = age < .3f ? Mathf.Clamp01(age / .3f) : 1f;
            pop = 1f - (1f - pop) * (1f - pop) + .25f * Mathf.Sin(pop * Mathf.PI);       // 0 -> 1.25 -> 1
            float wave = Mathf.Sin(ui * 6f + lane), c = 24f * queueScale * (1f + Mathf.Max(wave, 0f) * .15f) * pop;
            y -= 1.5f * queueScale * Mathf.Sin(ui * 3f + lane * .7f);
            s.Glow(x, y, c * 2.1f, c * 2.1f, new Color(1f, .84f, .3f, (.3f + .2f * (wave * .5f + .5f)) * Mathf.Min(1f, pop)), 0f);
            if (!Asset(s, "scene/tier-coin", x - c * .5f, y - c * .5f, c, c, Color.white)) s.Rect(x - c * .5f, y - c * .5f, c, c, Gold);
            TextOutline(s, "合", x, y - c * .34f, c * .6f, Cream, Hex(0x7A4A00), 1.3f * queueScale);
            float glint = (ui * .8f + lane * .37f) % 1.6f;                                  // one glint every 2 s
            if (glint < .35f) {
                float g = Mathf.Sin(glint / .35f * Mathf.PI), arm = c * .32f * g;
                Vector2 p = new Vector2(x + c * .3f, y - c * .3f);
                s.Segment(p - new Vector2(arm, 0f), p + new Vector2(arm, 0f), 1.4f * queueScale, new Color(1f, 1f, 1f, g));
                s.Segment(p - new Vector2(0f, arm), p + new Vector2(0f, arm), 1.4f * queueScale, new Color(1f, 1f, 1f, g));
                s.Glow(p.x, p.y, c * .5f, c * .5f, new Color(1f, 1f, .9f, .6f * g), 0f);
            }
        }

        // 2026-10-22 10:21: progress (0..1 over .22 s) of the breath in a member takes when it becomes ready to blow
        // (landed with ammo, or its jet stopped with ammo left); -1 when no breath is playing.
        readonly Dictionary<int, float> padReadyAt = new Dictionary<int, float>();
        readonly HashSet<int> padReady = new HashSet<int>();
        Flightbeat.Model.Battle padReadyBattle;
        float PadInhale(Fighter ship, bool ready)
        {
            if (padReadyBattle != Battle) { padReadyBattle = Battle; padReady.Clear(); padReadyAt.Clear(); }
            if (!ready) { padReady.Remove(ship.Id); padReadyAt.Remove(ship.Id); return -1f; }
            if (padReady.Add(ship.Id)) padReadyAt[ship.Id] = ui;
            if (!padReadyAt.TryGetValue(ship.Id, out float at)) return -1f;
            float u = (ui - at) / .22f;
            if (u >= 1f) { padReadyAt.Remove(ship.Id); return -1f; }
            return u;
        }

        // 2026-10-21 23:33 (user): gold light for the members you can tap. 10-22 09:53 (user): only the ring at the feet
        // is kept (no aura, no floor pool, no rising specks): a gold ellipse round the feet, brighter at the front edge,
        // 2 px wide (14:24, user), breathing gently (~2.6 s), lanes out of step.
        void QueueFootLight(PixelSurface s, float x, float feet, float queueScale, float size, float phase)
        {
            float b = .5f + .5f * Mathf.Sin(ui * 2.4f + phase * 1.3f), y = feet - 2f * queueScale;
            // 11:16 (user: on 4 lanes the ring reached the tube's outer wall, on 5 lanes it sat inside): always inside the
            // inner wall of the glass tube (tube width min(108, 500 / lanes - 8), wall ~14 px).
            float tube = Mathf.Min(108f, 500f / Mathf.Max(1, Battle.LaneCount) - 8f);
            float rx = Mathf.Min(size * .6f, tube * .5f - 14f), ry = rx * .233f;
            Vector2 prev = new Vector2(x + rx, y);
            for (int i = 1; i <= 28; i++)
            {
                float t = i / 28f * Mathf.PI * 2f;
                Vector2 next = new Vector2(x + Mathf.Cos(t) * rx, y + Mathf.Sin(t) * ry);
                float front = .5f + .5f * Mathf.Sin(t - Mathf.PI / 28f);   // 1 at the front (bottom) edge
                s.Segment(prev, next, 2f * queueScale, new Color(1f, .8f, .27f, (.3f + .55f * front) * (.75f + .25f * b)));
                prev = next;
            }
        }

        // 2026-10-21 10:21: queue sleepers. Returns seconds since this member woke up (it reached the front row after
        // dozing; large when it never slept or woke long ago) so the queue can give it a small hop.
        readonly HashSet<int> queueSlept = new HashSet<int>();
        readonly Dictionary<int, float> queueWokeAt = new Dictionary<int, float>();
        Flightbeat.Model.Battle queueBattle;
        float QueueWake(int id, bool asleep)
        {
            if (queueBattle != Battle) { queueBattle = Battle; queueSlept.Clear(); queueWokeAt.Clear(); }
            if (asleep) { queueSlept.Add(id); queueWokeAt.Remove(id); return 99f; }
            if (queueSlept.Remove(id)) queueWokeAt[id] = ui;
            if (!queueWokeAt.TryGetValue(id, out float at)) return 99f;
            if (ui - at > 1f) { queueWokeAt.Remove(id); return 99f; }
            return ui - at;
        }
        // 2026-10-21: seconds since a hidden member was revealed at the front (it was seen hidden further back); large
        // otherwise. Reset with the sleepers when the battle changes.
        readonly HashSet<int> queueHidden = new HashSet<int>();
        readonly Dictionary<int, float> queueRevealAt = new Dictionary<int, float>();
        float QueueReveal(Fighter ship, int row)
        {
            if (queueBattle != Battle) { queueBattle = Battle; queueSlept.Clear(); queueWokeAt.Clear(); }
            if (queueRevealBattle != Battle) { queueRevealBattle = Battle; queueHidden.Clear(); queueRevealAt.Clear(); }
            if (row != 0 && ship.Kind == 2) { queueHidden.Add(ship.Id); return 99f; }
            if (row == 0 && queueHidden.Remove(ship.Id)) queueRevealAt[ship.Id] = ui;
            if (!queueRevealAt.TryGetValue(ship.Id, out float at)) return 99f;
            if (ui - at > 1f) { queueRevealAt.Remove(ship.Id); return 99f; }
            return ui - at;
        }
        Flightbeat.Model.Battle queueRevealBattle;
        // 2026-10-29 (user): the level-start hint. Lane of the member the game itself would suggest (Battle.SuggestedQueue,
        // f55082: a head that merges with members on the pads first, then the one whose colour is at the front of the most
        // columns, ties to the earlier Order), recomputed here read-only (SuggestedQueue / ColumnTarget write pierce
        // fields, so drawing must not call them). Chosen once when the battle is ready and kept until the first deploy
        // (State leaves Ready); -1 otherwise, in lessons (they have their own hint) and while a menu or aiming is open.
        Flightbeat.Model.Battle startHintBattle;
        int startHintLane = -1;
        float startHintAt;
        int GardenStartHintLane()
        {
            if (Battle == null || lesson != null || Battle.State != Phase.Ready || settings || selection != 0) return -1;
            if (startHintBattle != Battle) { startHintBattle = Battle; startHintLane = GardenSuggestLane(); startHintAt = ui; }
            return startHintLane >= 0 && startHintLane < Battle.LaneCount && Battle.Reserve[startHintLane].Count > 0 ? startHintLane : -1;
        }
        int GardenSuggestLane()
        {
            int bestScore = int.MinValue, bestOrder = int.MaxValue, best = -1;
            if (Battle.UnlockedSlots <= Battle.Fleet.Count) return -1;
            for (int q = 0; q < Battle.Reserve.Length; q++)
            {
                if (Battle.Reserve[q].Count == 0) continue;
                Fighter c = Battle.Reserve[q][0];
                if (Battle.Time < c.DeployAt) continue;
                int matching = 0, targets = 0;
                foreach (Fighter ship in Battle.Fleet) if (ship.Color == c.Color) matching++;
                for (int col = 0; col < Battle.Level.columns; col++)
                {
                    int front = Battle.Front(col);
                    if (front >= 0 && Battle.ColorAt(front) == c.Color) targets++;
                }
                int score = matching * 10 + (matching > 1 ? 100000 : 0) + targets * 100;
                if (score > bestScore || (score == bestScore && c.Order < bestOrder)) { bestScore = score; bestOrder = c.Order; best = q; }
            }
            return best;
        }
        // 2026-10-29: a tap on a queue that could not deploy because every pad is taken (BattleInput) - the front member
        // of that lane shakes its head (QueueRefuseShake, degrees for GardenSkin.BendRotate).
        readonly Dictionary<int, float> queueRefusedAt = new Dictionary<int, float>();
        internal void GardenQueueRefused(int lane)
        {
            if (Battle == null || lane < 0 || lane >= Battle.LaneCount || Battle.Reserve[lane].Count == 0) return;
            if (Battle.UnlockedSlots > Battle.Fleet.Count) return;   // refused for another reason: no shake
            queueRefusedAt[lane] = ui;
        }
        static readonly float[] RefuseKeys = { 0f, .06f, .14f, .21f, .27f, .35f }, RefuseAngles = { 0f, 12f, -14f, 9f, -5f, 0f };
        float QueueRefuseShake(int lane)
        {
            if (!queueRefusedAt.TryGetValue(lane, out float at)) return 0f;
            float t = ui - at;
            if (t < 0f || t >= RefuseKeys[RefuseKeys.Length - 1]) { queueRefusedAt.Remove(lane); return 0f; }
            int k = 1;
            while (t > RefuseKeys[k]) k++;
            float u = (t - RefuseKeys[k - 1]) / (RefuseKeys[k] - RefuseKeys[k - 1]);
            return Mathf.Lerp(RefuseAngles[k - 1], RefuseAngles[k], u * u * (3f - 2f * u));
        }
        // Four small twinkles round the head of a member that has just twirled to face you (age 0..~.3 s).
        void QueueTwirlSparkle(PixelSurface s, float x, float y, float canvas, float age)
        {
            float k = Mathf.Clamp01(age / .3f), a = 1f - k;
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 90f + 45f) * Mathf.Deg2Rad, r = canvas * (.28f + .22f * k), d = canvas * .07f * (1f - .5f * k);
                Vector2 p = new Vector2(x + Mathf.Cos(ang) * r, y + Mathf.Sin(ang) * r * .8f);
                Color c = new Color(1f, .95f, .7f, a);
                s.Segment(new Vector2(p.x - d, p.y), new Vector2(p.x + d, p.y), 2f, c);
                s.Segment(new Vector2(p.x, p.y - d), new Vector2(p.x, p.y + d), 2f, c);
            }
        }
        // 2026-10-21 14:18 (user agreed): an ad-unlocked pad is shown on the pad itself instead of a 「已解锁出战位」 bar in
        // the middle. When a pad turns open: its padlock bubble bursts (rocket-pop), a mint ring of light spreads over
        // the pad, six sparkles rise, then the empty pad pulses three times (about 1.4 s in all).
        bool[] padWasOpen;
        Flightbeat.Model.Battle padOpenBattle;
        readonly float[] padUnlockAt = new float[16];
        void PadUnlockFx(PixelSurface s, float plotW)
        {
            int n = Mathf.Min(Battle.MaximumSlots, padUnlockAt.Length);
            if (padOpenBattle != Battle || padWasOpen == null || padWasOpen.Length != n)
            {
                padOpenBattle = Battle; padWasOpen = new bool[n];
                for (int b = 0; b < n; b++) { padWasOpen[b] = Battle.BayOpen(b); padUnlockAt[b] = -10f; }
                return;
            }
            Color mint = GardenBars.Mint;
            for (int b = 0; b < n; b++)
            {
                bool open = Battle.BayOpen(b);
                if (open && !padWasOpen[b]) padUnlockAt[b] = ui;
                padWasOpen[b] = open;
                float t = ui - padUnlockAt[b];
                if (t < 0f || t > 1.4f) continue;
                float cx = Battle.FleetPositionX(b), lockY = 647f - plotW * .25f;
                GardenFx.Burst(s, "rocket-pop", new Vector2(cx, lockY), plotW * 1.7f, t, .45f);
                if (t < .6f)
                {
                    float q = t / .6f, e = 1f - (1f - q) * (1f - q);
                    s.Glow(cx, GardenRaisedPadY(647f), plotW * (1f + 1.8f * e), plotW * (.5f + .9f * e), new Color(mint.r, mint.g, mint.b, .65f * (1f - q)), 0f);
                    s.Glow(cx, GardenRaisedPadY(647f), plotW * 1.2f, plotW * .6f, new Color(1f, 1f, 1f, .5f * (1f - q)), 0f);
                }
                for (int k = 0; k < 6; k++)
                {
                    float st = t - k * .04f;
                    if (st < 0f || st > .8f) continue;
                    float u = st / .8f, ang = (k / 6f) * Mathf.PI * 2f + .4f, r = 4f + 3f * Mathf.Sin(u * Mathf.PI);
                    Vector2 p = new Vector2(cx + Mathf.Cos(ang) * plotW * (.3f + .4f * u), lockY - plotW * .2f - 46f * u + Mathf.Sin(ang) * 8f);
                    Color c = k % 2 == 0 ? Color.white : mint; c.a = 1f - u * u;
                    s.Glow(p.x, p.y, r * 2.4f, r * 2.4f, new Color(c.r, c.g, c.b, c.a * .5f), 0f);
                    s.Segment(p - new Vector2(r, 0f), p + new Vector2(r, 0f), r * .32f, c);
                    s.Segment(p - new Vector2(0f, r), p + new Vector2(0f, r), r * .32f, c);
                }
                if (t > .4f)
                {
                    float pulse = Mathf.Abs(Mathf.Sin((t - .4f) / 1f * Mathf.PI * 3f)) * (1f - (t - .4f) / 1f);
                    s.Glow(cx, GardenRaisedPadY(645f), plotW * 1.4f, plotW * .75f, new Color(mint.r, mint.g, mint.b, .5f * pulse), 0f);
                }
            }
        }
        // A small "z" rising from the second-row sleeper now and then (one lane at a time, offset per lane).
        void SleepZ(PixelSurface s, float x, float y, float queueScale, int lane, float shift = 0f)
        {
            float t = (ui + lane * 1.9f + shift) % 5.5f;
            if (t > 1.8f) return;
            float u = t / 1.8f, a = Mathf.Sin(u * Mathf.PI) * .9f;
            float size = (9f + 4f * u) * queueScale;
            TextOutline(s, "z", x + 5f * u * queueScale, y - 16f * u * queueScale, size,
                new Color(1f, 1f, 1f, a), new Color(Outline.r, Outline.g, Outline.b, a), 1.1f * queueScale);
        }

        // ---- Farewell (2026-10-28, user: like 沙画消消's full bucket popping away) --------------------------------------
        // A member that fires its last shot leaves Battle.Fleet at once (Battle.Tick), so it used to blink off its pad.
        // Now it says goodbye where it stood: a quick crouch, a happy hop up with the cheer face, swelling to 1.2x, then it
        // shrinks away into a bubble pop with a small fan of confetti (member ~.5 s, confetti until ~1 s). Only members
        // that leave with no ammo are celebrated (merge partners and other removals keep their ammo). Drawing only, on the
        // UI clock, drawn before the members so a newcomer landing on the same pad stays in front.
        struct PadSeen { public Fighter ship; public float x, feet, scale; }
        struct PadFarewell { public float born, x, feet, scale; public int color, tier, seed; }
        readonly Dictionary<int, PadSeen> padSeen = new Dictionary<int, PadSeen>();
        readonly List<PadFarewell> padFarewells = new List<PadFarewell>();
        readonly List<int> padGone = new List<int>();
        Flightbeat.Model.Battle padSeenBattle;
        const float FarewellLife = 1f;
        static float FarewellHash(int n) { float v = Mathf.Sin(n * 12.9898f) * 43758.5453f; return v - Mathf.Floor(v); }

        // Where a pad member was drawn this frame (feet as passed to Creature, before the layout mapping).
        void PadSeenRecord(Fighter ship, float x, float feet, float scale) =>
            padSeen[ship.Id] = new PadSeen { ship = ship, x = x, feet = feet, scale = scale };

        void DrawPadFarewells(PixelSurface s, float canvas)
        {
            if (padSeenBattle != Battle) { padSeenBattle = Battle; padSeen.Clear(); padFarewells.Clear(); }
            padGone.Clear();
            foreach (var pair in padSeen)
            {
                bool here = false;
                foreach (var f in Battle.Fleet) if (f.Id == pair.Key) { here = true; break; }
                if (!here) padGone.Add(pair.Key);
            }
            foreach (int id in padGone)
            {
                PadSeen seen = padSeen[id];
                padSeen.Remove(id);
                if (seen.ship == null || seen.ship.Ammo > 0 || Battle.State == Phase.Intro) continue;
                padFarewells.Add(new PadFarewell { born = ui, x = seen.x, feet = seen.feet, scale = seen.scale,
                    color = seen.ship.Color, tier = seen.ship.Tier, seed = id });
            }
            if (padFarewells.Count == 0) return;
            bool mapped = s.MapLayout, cheer = GardenSkin.Celebrate;
            s.MapLayout = true;
            try
            {
                for (int i = padFarewells.Count - 1; i >= 0; i--)
                {
                    PadFarewell f = padFarewells[i];
                    float t = ui - f.born;
                    if (t < 0f || t >= FarewellLife) { padFarewells.RemoveAt(i); continue; }
                    Color tint = GardenColor(f.color);
                    float body = canvas * f.scale, half = body * .42f;
                    // 1. The member: crouch (.08 s), hop up and swell (.22 s), shrink away at the top (.18 s).
                    if (t < .48f)
                    {
                        float lift = 0f, k = 1f, squash;
                        if (t < .08f) squash = .14f * Mathf.Sin(t / .08f * Mathf.PI * .5f);
                        else if (t < .30f)
                        {
                            float u = (t - .08f) / .22f, e = 1f - (1f - u) * (1f - u);
                            lift = 26f * e; k = 1f + .2f * e;
                            squash = .14f * Mathf.Pow(1f - u, 5f) - .12f * Mathf.Sin(u * Mathf.PI);   // crouch -> stretched -> round
                        }
                        else
                        {
                            float u = (t - .30f) / .18f;
                            lift = 26f + 6f * u; k = 1.2f * (1f - u * u); squash = 0f;
                        }
                        if (k > .03f)
                        {
                            // Creature scales about the feet; keep the body's centre still instead.
                            float feet = f.feet - lift, centre = feet - half;
                            feet = centre + (feet - centre) * k;
                            GardenSkin.Celebrate = true;
                            // 2026-10-29 (user: keep the farewell with the flower ring): the flower stays on its head and
                            // swings with the hop instead of vanishing when the member leaves the fleet.
                            if (GardenSkin.HasFlowerRing(tint))
                                GardenSkin.PadFlower = new GardenSkin.PadFlowerPose { enabled = true, bubble = -1f, pop = -1f,
                                    sway = 12f * Mathf.Sin(t * 22f) * (1f - t / .48f), squash = squash * .6f };
                            Creature(s, tint, f.x, feet, canvas, f.tier, "", 0f, 14f, false, false, 0f, f.seed * 1.7f, f.scale * k, squash);
                            GardenSkin.Celebrate = cheer;
                        }
                    }
                    // 2. Gone in a bubble pop and a fan of confetti from where it vanished.
                    float pt = t - .40f;
                    if (pt < 0f) continue;
                    Vector2 c0 = new Vector2(f.x, f.feet - 30f - half);
                    if (GardenFx.Has("rocket-pop")) GardenFx.Burst(s, "rocket-pop", c0, body * 1.3f, pt, .4f);
                    Color ring = Color.Lerp(tint, Color.white, .45f);
                    if (pt < .3f) { ring.a = .85f * (1f - pt / .3f); s.Ring(c0, body * (.35f + .5f * pt / .3f), ring, 2.5f * (1f - pt / .3f) + .5f, 20, 0f, 360f); }
                    float fade = 1f - Mathf.Clamp01((pt - .35f) / .25f);
                    for (int k = 0; k < 12; k++)
                    {
                        float h1 = FarewellHash(f.seed * 31 + k), h2 = FarewellHash(f.seed * 37 + k), h3 = FarewellHash(f.seed * 41 + k);
                        float ang = Mathf.Lerp(200f, 340f, (k + h1) / 12f) * Mathf.Deg2Rad, speed = 150f + 170f * h2;
                        Vector2 p = c0 + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed * pt + new Vector2(0f, 420f * pt * pt);
                        float spin = h3 * 6.28f + pt * (8f + 10f * h1);
                        Vector2 d = new Vector2(Mathf.Cos(spin), Mathf.Sin(spin)) * 3.5f;
                        Color col = k % 3 == 0 ? Color.Lerp(tint, Color.white, .2f) : Color.HSVToRGB(FarewellHash(f.seed * 43 + k), .55f, 1f);
                        col.a = fade;
                        s.Segment(p - d, p + d, 3f, col);
                    }
                }
            }
            finally { s.MapLayout = mapped; GardenSkin.Celebrate = cheer; }
        }
    }
}
#endif
