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
    }
}
#endif
