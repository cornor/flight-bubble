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

        // 2026-10-16 (user item 5): a smaller start and wider steps so each upgrade reads; tier 3 keeps the old size.
        static float PadTierScale(int tier) => tier >= 3 ? 1.18f : tier == 2 ? 1.07f : tier == 1 ? .955f : .84f;
        readonly Dictionary<int, int> padTier = new Dictionary<int, int>();
        readonly Dictionary<int, float> padPopAt = new Dictionary<int, float>();
        Flightbeat.Model.Battle padBattle;
        // Size of a pad member; when its tier goes up it swells and springs back over 0.4 s.
        float PadScale(Fighter ship)
        {
            if (padBattle != Battle) { padBattle = Battle; padTier.Clear(); padPopAt.Clear(); }
            if (padTier.TryGetValue(ship.Id, out int was) && ship.Tier > was) padPopAt[ship.Id] = ui;
            padTier[ship.Id] = ship.Tier;
            float pop = 1f;
            if (padPopAt.TryGetValue(ship.Id, out float at))
            {
                float p = (ui - at) / .4f;
                if (p >= 0f && p < 1f) pop = 1f + .25f * Mathf.Sin(p * Mathf.PI) * (1f - p * .5f);
            }
            return PadTierScale(ship.Tier) * pop;
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
            if (t < .3f) s.Glow(padX, 647f, 90f, 40f, new Color(1f, .95f, .75f, (1f - t / .3f) * .55f), 0f);
            GardenFx.Burst(s, "impact-dust", new Vector2(padX, feet - 6f), 78f, t, .45f);
            for (int k = 0; k < 6; k++)
            {
                float u = t / .4f;
                if (u >= 1f) break;
                float side = k % 2 == 0 ? -1f : 1f, speed = 55f + k * 9f;
                Vector2 p = new Vector2(padX + side * speed * t, feet - 6f - 70f * t + 240f * t * t);
                GardenSkin.Shot(s, tint, p, 6f * (1f - u * .5f), 1f - u);
            }
            if (t >= LandTime) return 0f;
            return .22f * Mathf.Exp(-t * 9f) * Mathf.Cos(t / LandTime * Mathf.PI * 2.5f);
        }

        // Shots leave from the member's mouth (attack pose: mouth at y 135/256 of the 224x256 canvas).
        Vector2 GardenMouth(int fighterId)
        {
            var fleet = Battle.Fleet;
            for (int i = 0; i < fleet.Count; i++)
            {
                if (fleet[i].Id != fighterId) continue;
                Vector2 at = feedback.Position(fighterId, i);
                float ah = GardenBayCanvas() * PadScale(fleet[i]);
                return new Vector2(at.x + ah * ArtAspect * .03f, at.y + 12f - ah * (FeetLine - 135f / 256f));
            }
            return new Vector2(float.NaN, float.NaN);
        }
        // Where a bead is drawn (sand interpolation and smoothed advance), so shots land on the visible bead.
        Vector2 GardenEnemyAt(int index)
        {
            if (index < 0 || index >= Battle.Alive.Length) return new Vector2(float.NaN, float.NaN);
            float gy = shownY != null && index < shownY.Length ? shownY[index] : Battle.GridY(index);
            return ScreenLayout.EnemyPoint(Battle.EnemyX(index), gy + Battle.Offset + feedback.GardenOffsetDelta);
        }

        // 2026-10-16 22:29 (the dashed aim art was too faint on the bead field). The true area is the original
        // reticle's circle (SuperStrikeVisuals.Reticle, screen radius 120/150 x (bombRadius + 1)); it gets a warm
        // tinted fill, a thick white ring with a navy edge, turning gold dashes and a centre mark.
        float BombRadius => (aimMissile ? 150f : 120f) * (Battle.Loadout.bombRadius + 1f);
        bool InBombCircle(Vector2 mapPoint)
        {
            Vector2 a = ScreenLayout.Point(aim), p = ScreenLayout.Point(mapPoint);
            return (p - a).sqrMagnitude <= BombRadius * BombRadius;
        }
        void DrawBombAim(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            Vector2 c = ScreenLayout.Point(aim);
            float pulse = Mathf.Sin(ui * 5f) * .5f + .5f, r = BombRadius * (1f + .015f * pulse);
            s.Glow(c.x, c.y, r * 2.2f, r * 2.2f, new Color(1f, .45f, .3f, .28f + .1f * pulse), 0f);
            for (int pass = 0; pass < 2; pass++)
            {
                Vector2 prev = c + new Vector2(r, 0f);
                for (int i = 1; i <= 64; i++)
                {
                    float a = i / 64f * Mathf.PI * 2f;
                    Vector2 next = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    s.Segment(prev, next, pass == 0 ? 7f : 3.5f, pass == 0 ? new Color(.1f, .2f, .4f, .85f) : Color.white);
                    prev = next;
                }
            }
            for (int i = 0; i < 12; i++)
            {
                float start = ui * .9f + i * Mathf.PI / 6f;
                Vector2 prev = c + new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * (r + 7f);
                for (int j = 1; j <= 3; j++)
                {
                    float a = start + j * .1f;
                    Vector2 next = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r + 7f);
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

        // 2026-10-16 (user item 4): the original's golden 「合」 disc (f54834) as a bubble badge instead of the word 可合成.
        void MergeBadge(PixelSurface s, float x, float y, float queueScale, int lane)
        {
            float wave = Mathf.Sin(ui * 6f + lane), c = 24f * queueScale * (1f + Mathf.Max(wave, 0f) * .15f);
            s.Glow(x, y, c * 2.1f, c * 2.1f, new Color(1f, .84f, .3f, .3f + .2f * (wave * .5f + .5f)), 0f);
            if (!Asset(s, "scene/tier-coin", x - c * .5f, y - c * .5f, c, c, Color.white)) s.Rect(x - c * .5f, y - c * .5f, c, c, Gold);
            TextOutline(s, "合", x, y - c * .34f, c * .6f, Cream, Hex(0x7A4A00), 1.3f * queueScale);
        }
    }
}
#endif
