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
                GardenSkin.TextOutline(s, word, at.x, at.y - 74f - 26f * u, size, new Color(1f, .86f, .3f, alpha),
                    new Color(GardenSkin.Outline.r, GardenSkin.Outline.g, GardenSkin.Outline.b, alpha), 2f);
            }
        }

        // Read existing feedback records; never advance clocks or resolve a model shot here.
        public void DrawGardenFeedback(PixelSurface s, Func<int, Color> color) {
            foreach (var ship in battle.Fleet) {
                if (ship.Tier < 2 || ship.BeamLife <= 0) continue;
                int slot = battle.Fleet.IndexOf(ship);
                var from = MouthOr(ship.Id, Position(ship.Id, slot));
                var to = ShownPoint(ship.BeamX, ship.BeamY);
                // 2026-10-18 23:40 (user: Ⅱ bullets too small). Stream balls are .48-.62 of the width, so width 10 made
                // 5-6 px balls next to the 12 px Ⅰ shots; now Ⅱ 18 (9-11 px balls) and MAX 22 (11-14 px).
                GardenFx.BubbleStream(s, from, to, ship.Tier >= 3 ? 22 : 18, color(ship.Color), time);
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
                var p = Vector2.Lerp(from, to, Mathf.Clamp01(shot.Age / shot.Duration));
                // v3 (UI spec v3 4.4, user decision 2026-10-15 20:28): every projectile is a SOLID glossy ball in the
                // shooter's color (the bead texture), 12 px, with two smaller solid after-images. No see-through bubble.
                Color tint = color(f.color);
                for (int g = 2; g >= 1; g--) {
                    var q = Vector2.Lerp(from, to, Mathf.Clamp01((shot.Age - g * .025f) / shot.Duration));
                    GardenSkin.Shot(s, tint, q, 12f - g * 2.5f, 1f);
                }
                GardenSkin.Shot(s, tint, p, 12f, 1f);
            }
            // Retain source effects that the initial four-level skin did not render.
            // These draw existing feedback records; no model or effect clock is changed.
            // Pierce sparks and weapon forms need hangar upgrades this build cannot reach: original drawing kept.
            DrawPierceSparks(s); DrawWeaponFx(s);
            // Third batch (art items 79/80): bubble versions of the MAX rocket burst and the revive purge.
            // Both original routines only draw, so skipping them changes nothing else.
            if (GardenFx.Has("rocket-pop")) DrawBubbleRocketPops(s); else DrawRocketBlasts(s);
            if (GardenFx.Has("rescue-bubble")) DrawBubblePurge(s); else DrawPurge(s);
            // User decision 2026-10-14: ordinary hits have no visual FX, including the old rings.
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
            var pending = battle.PendingStrike;
            if (pending != null) {
                Vector2 at = ScreenLayout.EnemyPoint(pending.X, pending.Y);
                float t = Mathf.Clamp01(pending.Age);
                GardenFx.Aim(s, at, pending.RadiusX * ScreenLayout.EnemyScaleX, .6f);
                GardenFx.Draw(s, "water-can", at + new Vector2(-32, -100 + t * 48), 96, 96,
                    new Color(1, 1, 1, Mathf.Clamp01(t * 5)), 0, -15 * (1 - t));
            }
            if (lastStrike != null)
                GardenFx.Burst(s, "water-splash", ScreenLayout.EnemyPoint(lastStrike.X, lastStrike.Y), 128, superAge, .7f);
        }
        // f53879 DrawRocketBlasts: same records and .4 s life; the ring/glow becomes a bursting bubble.
        void DrawBubbleRocketPops(PixelSurface s) {
            foreach (Blast blast in blasts)
                GardenFx.Burst(s, "rocket-pop", blast.at, 120f, blast.age, .4f);
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
                GardenSkin.TextOutline(s, "泡泡援军 ×" + purge.count, 270f, purge.front - 72f - size * .5f, size,
                    new Color(1f, .93f, .55f, alpha), new Color(GardenSkin.Outline.r, GardenSkin.Outline.g, GardenSkin.Outline.b, alpha), 2f);
            }
        }

        // 2026-10-16 (user item 1): the original combo counter f53835, redrawn in bubble style. Same records and
        // rules: shown from 5 hits, a punch on every hit (comboPunch < .12 s), band colours (ComboColor), the
        // draining life bar (ComboLife) and the centre burst at 50/100/250/500/1000 (comboMilestoneAge < 1.2 s).
        public void DrawBubbleCombo(PixelSurface s) {
            if (comboFade <= .01f || HideCombo) return;
            int count = lastCombo > 1 ? lastCombo : 1;
            Color color = ComboColor(count);
            float fade = comboFade, punch = comboPunch < .12f ? 1f - comboPunch / .12f : 0f;
            float milestone = comboMilestoneAge < .5f ? 1f - comboMilestoneAge * 2f : 0f;
            float flicker = comboBand >= 2 ? Mathf.Sin(time * 12f) * .08f : 0f;
            string text = count.ToString();
            float size = 34f * (1f + punch * .3f + milestone * .35f), right = 524f;
            float width = GardenSkin.TextWidth(text, size), y = 88f - punch * 5f, x = right - width;   // 22:29: up 40 (was 128)
            Color ink = new Color(GardenSkin.Outline.r, GardenSkin.Outline.g, GardenSkin.Outline.b, fade);
            s.Glow(right - width * .5f, y + size * .5f, width + 70f, size + 30f, Alpha(color, fade * (.22f + milestone * .25f + flicker)), 0f);
            GardenSkin.TextOutline(s, text, x, y, size, Alpha(Mix(color, Color.white, punch * .6f), fade), ink, 2.6f + punch, false);
            GardenSkin.TextOutline(s, "×", x - 3f - GardenSkin.TextWidth("×", 20f), y + size - 22f, 20f, Alpha(color, fade), ink, 1.8f, false);
            GardenSkin.TextOutline(s, "连击", right - GardenSkin.TextWidth("连击", 16f), y - 18f, 16f, Alpha(color, fade), ink, 1.4f, false);
            // Life bar: a short capsule under the number that drains until the combo breaks.
            float barW = Mathf.Max(width, 44f), barY = y + size + 3f, life = Mathf.Clamp01(battle.ComboLife);
            s.Rect(right - barW, barY, barW, 5f, new Color(.08f, .16f, .32f, fade * .55f));
            s.Rect(right - barW, barY, barW * life, 5f, Alpha(color, fade));
            if (!(comboMilestoneAge < 1.2f)) return;
            float t = comboMilestoneAge / 1.2f, f = Mathf.Clamp01((1f - t) * 2.5f);
            float rem = 1f - Mathf.Min(t / .2f, 1f), pop = (rem * rem * rem) * 1.6f;
            float ty = 200f - t * 20f, cy = ty + 18f;
            for (int n = 0; n < 10; n++) {
                float a = (time * 60f + n * 36f) * Mathf.Deg2Rad;
                s.GlowLine(new Vector2(270f, cy), new Vector2(270f + Mathf.Cos(a) * 220f, cy + Mathf.Sin(a) * 220f), 40f, Alpha(color, f * .12f));
            }
            GardenFx.Burst(s, "merge-burst", new Vector2(270f, cy), 200f, comboMilestoneAge, .5f);
            string title = milestoneValue > 499 ? "火力全开！" : "连击突破！";
            float titleSize = 36f * (1f + pop * .35f);
            GardenSkin.TextOutline(s, title, 270f, ty - titleSize * .5f + 10f, titleSize, Alpha(color, f), new Color(ink.r, ink.g, ink.b, f), 3f);
            GardenSkin.TextOutline(s, milestoneValue + " 连击", 270f, ty + 34f, 20f, new Color(1f, 1f, 1f, f), new Color(ink.r, ink.g, ink.b, f), 2f);
        }
    }
}
#endif
