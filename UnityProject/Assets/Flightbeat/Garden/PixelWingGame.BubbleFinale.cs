#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // Third batch (art items 77/78, docs/r1/v3-第三批接入计划.md): the finale ("决战") in bubble dress.
    // The squad flies to the centre and merges into the Bubble King, who blows a rainbow fan of bubbles.
    // The original Finale*/GiantPlane/FanLaser routines are protected and carry side effects (button cues and
    // landing kick, the hit region, finaleEndClock, guide hit regions), so each is still called, muted: its quads
    // go to a scratch buffer. Everything below only reads finale state and clocks.
    public partial class PixelWingGame
    {
        const string KingReady = "plants/finale/king-ready", KingBlow = "plants/finale/king-blow", FinaleButtonArt = "finale/button";
        const float KingSize = 250f;            // same logical size as the original giant plane
        const float KingMouth = -.222f;         // mouth centre in king-blow (measured y 142.5 of 512), fraction of KingSize from centre
        static readonly Color FinaleInk = Hex(0x7A3A00);
        bool BubbleFinaleArt => GardenSkin.Has(KingReady) && GardenSkin.Has(FinaleButtonArt);

        // ---- button (f54740 timing: pop-in .16 s, then a damped wobble; same 340x86 box at (270, 759))
        void DrawBubbleFinaleButton(PixelSurface s)
        {
            if (!BubbleFinaleArt) { DrawFinaleButton(s); return; }
            s.BeginMute();
            try { DrawFinaleButton(s); } finally { s.EndMute(); }
            if (!FinaleButtonVisible || finaleReadyAt < 0f) return;
            float age = ui - finaleReadyAt, alpha, scale;
            if (age < .16f) { alpha = age / .16f; scale = Mathf.Min(alpha * alpha, 1f) * -.9f + 1.9f; }
            else { float t = age - .16f; alpha = 1f; scale = Mathf.Sin(t * 38f) * Mathf.Exp(t * -9f) * .07f + 1f; }
            scale *= 1f - PressPulse(8) * .05f;
            float w = 340f * scale, h = 86f * scale, pulse = Mathf.Sin(ui * 5f) * .5f + .5f;
            s.Glow(270f, 759f, w + 90f, h + 70f, new Color(1f, .82f, .3f, alpha * (.22f + .12f * pulse)), 0f);
            Asset(s, FinaleButtonArt, 270f - w * .5f, 759f - h * .5f, w, h, new Color(1f, 1f, 1f, alpha));
            // 2026-10-16 22:29: title and line as one block centred on the button face; the line keeps ~10 px off
            // the rainbow rim (at 759+34 the line touched it).
            float title = 34f * scale, line = 10.5f * scale, gap = 4f * scale;
            float top = 759f - (title + gap + line) * .5f - 2f * scale;
            TextOutline(s, "决 战", 270f, top, title, A(Cream, alpha), A(FinaleInk, alpha), 2.4f * scale);
            TextOutline(s, "全员合体 · 彩虹泡泡扫射", 270f, top + title + gap, line, A(Cream, alpha), A(FinaleInk, alpha), 1.2f * scale);
        }

        // ---- first-time guide (f54731 layout: mask with a hole over the button, title, dialogue, pointer)
        const string FinaleGuideLine = "全员都出战啦！\n点「决战」，大家合体一口气打光！";
        void DrawBubbleFinaleGuide(PixelSurface s)
        {
            if (!BubbleFinaleArt) { DrawFinaleGuide(s); return; }
            s.BeginMute();
            try { DrawFinaleGuide(s); } finally { s.EndMute(); }   // keeps HitBlock and the two hit boxes
            if (!FinaleButtonVisible) return;
            float alpha = Clamp01(finaleGuideAge * 4f), pulse = Mathf.Sin(ui * 6f) * .5f + .5f;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float y = ScreenLayout.Y(710f), bottom = ScreenLayout.Y(808f);
            Color mask = new Color(.04f, .1f, .22f, alpha * .7f);
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, y, mask);
            s.Rect(ScreenLayout.Left, bottom, ScreenLayout.Width, ScreenLayout.Height - bottom, mask);
            s.Rect(ScreenLayout.Left, y, 94f - ScreenLayout.Left, bottom - y, mask);
            s.Rect(446f, y, ScreenLayout.Right - 446f, bottom - y, mask);
            s.MapLayout = mapped;
            s.Glow(270f, 759f, 460f, 176f, new Color(1f, .85f, .35f, alpha * (.2f + .15f * pulse)), 0f);
            GardenCorners(s, 92f, 708f, 356f, 102f, A(Color.Lerp(Gold, Color.white, pulse * .5f), alpha));
            TextOutline(s, "决 战 时 刻", 270f, 478f, 34f, A(Gold, alpha), A(Outline, alpha), 2.6f);
            TextOutline(s, "全员已出战 · 泡泡不到 " + Flightbeat.Model.Battle.FinaleThreshold + " 个", 270f, 528f, 16f, A(Cream, alpha), A(Outline, alpha), 1.6f);
            // Speech bubble with Xiao Lan; reveal at the original typing speed (spokenCharacters of the source line).
            if (!Asset9(s, "ui/speech-bubble", 96f, 586f, 414f, 92f, 30f, 30f, 18f, 18f, new Color(1f, 1f, 1f, alpha))) GardenPanel(s, 32f, 588f, 476f, 88f);
            Asset(s, "ui/guide-happy", 14f, 582f, 96f, 96f, new Color(1f, 1f, 1f, alpha));
            TextOutline(s, "小蓝", 150f, 580f, 13f, A(Cream, alpha), A(Outline, alpha), 1.4f);
            int shown = dialogue.Length == 0 ? FinaleGuideLine.Length
                : Mathf.Clamp(Mathf.CeilToInt(FinaleGuideLine.Length * (float)spokenCharacters / dialogue.Length), 0, FinaleGuideLine.Length);
            string[] lines = FinaleGuideLine.Substring(0, shown).Split('\n');
            for (int i = 0; i < lines.Length; i++) Text(s, lines[i], 124f, 612f + i * 29f, 16f, false, A(Ink, alpha));
            float bounce = Mathf.Abs(Mathf.Sin(ui * 5f)) * 6f;
            Asset(s, "fx/arrow", 256f, 684f - bounce, 28f, 28f, new Color(1f, 1f, 1f, alpha));
        }

        // ---- the finale itself (f54742, mode 0 "彩虹扇击": the only mode without a hangar)
        void DrawBubbleFinale(PixelSurface s)
        {
            if (!BubbleFinaleArt) { DrawFinale(s); return; }
            // 2026-10-16 22:29 (stutter during the finale): the original DrawFinale is no longer run muted. Its giant
            // plane, laser fan, title and up to 320 blast records were built every frame only to be thrown away;
            // its single side effect is the end clock below (f54742 line 1), reproduced here in the same order.
            Finale finale = finaleShown;
            if (finale == null) return;
            if (finaleEndClock < 0f && Battle.ActiveFinale == null) finaleEndClock = ui;
            float age = finale.Age, after = 0f;
            if (finaleEndClock >= 0f) { after = ui - finaleEndClock; if (after > 1.2f) return; }
            float dim = Clamp01(age / .3f) * (1f - Clamp01(after / .8f));
            s.Rect(12f, 574f, 516f, 300f, new Color(.05f, .12f, .26f, dim * .45f));
            DrawBubbleFinaleTitle(s, age);
            Vector2 mouth = KingMouthAt(finale, age, after);
            if (age >= 1.65f) DrawBubbleFan(s, finale, age, after, mouth);
            // Kill pops: the original 320-record window and .5 s life.
            for (int i = Math.Max(0, finaleBlasts.Count - 120); i < finaleBlasts.Count; i++)   // newest 120 (was 320)
            {
                FinaleBlast blast = finaleBlasts[i];
                float b = clock - blast.born;
                if (b < 0f || b > .5f) continue;
                GardenFx.Draw(s, "rocket-pop", blast.at, 34f, 34f, Color.Lerp(Color.white, blast.color, .45f), Mathf.Min(5, (int)(b / .5f * 6f)));
            }
            // Squad members spiral in along the original path and shrink into the king.
            for (int i = 0; i < finaleShips.Count; i++)
            {
                float t = ShipMerge(i);
                if (t >= 1f) continue;
                FinaleShip ship = finaleShips[i];
                Color tint = GardenColor(ship.color);
                for (int k = 3; k >= 1; k--)
                    GardenSkin.Shot(s, tint, FinaleShipPath(ship.from, Mathf.Max(t - k * .045f, 0f)), 11f - k * 2.2f, .55f);
                Vector2 at = FinaleShipPath(ship.from, t);
                float canvas = Mathf.Lerp(52f, 30f, InOut(t));
                Creature(s, tint, at.x, at.y + canvas * .42f, canvas, ship.tier, "", t > .2f && t < .8f ? .6f : 0f, 10f, false, false);
            }
            DrawBubbleKing(s, finale, age, after, mouth);
            if (age >= 1.65f && age < 1.85f)
            {
                // The release: one white flash over the whole screen.
                bool mapped = s.MapLayout;
                s.MapLayout = false;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(1f, 1f, 1f, .45f * (1f - (age - 1.65f) / .2f)));
                s.MapLayout = mapped;
            }
            DrawBubbleFinaleEnd(s, finale, age, after, mouth);
            DrawBubbleFinaleCount(s, finale, age);
        }

        void DrawBubbleFinaleTitle(PixelSurface s, float age)
        {
            if (age > 1.5f) return;
            float appear = OutBack(age / .3f, 2.4f), alpha = Clamp01((1.5f - age) / .35f);
            if (appear < .05f) return;
            s.Glow(270f, 300f, appear * 420f, appear * 150f, new Color(1f, .9f, .5f, alpha * .35f), 0f);
            for (int i = 0; i < 7; i++)
                s.Rect(270f - appear * 150f, appear * 30f + 286f + i * 4f, appear * 300f, 3f, A(finaleRainbow[i], alpha * .75f));
            TextOutline(s, "决 战", 270f, 262f - 26f * appear, 52f * appear, A(Gold, alpha), A(Outline, alpha), 3f * appear);
            if (age > .35f)
            {
                float a = alpha * Clamp01((age - .35f) / .2f);
                TextOutline(s, "泡泡大王 · 彩虹扫射！", 270f, 336f, 20f, A(Cream, a), A(Outline, a), 1.8f);
            }
        }

        // 2026-10-16 22:09 (user: start small, swell up, then let rip). Size of the king as a fraction of KingSize
        // on the original finale clock: appears at 40 % and puffs up in four gulps while the squad flies in
        // (.5-.9 s), swells like a balloon to 115 % while charging (1.15-1.65 s), recoils with a wobble when the
        // sweep starts and slowly deflates to 85 % while blowing. wide > 1 widens the cheeks (height / wide).
        float KingScale(float age, out float wide, out float tremble)
        {
            wide = 1f; tremble = 0f;
            if (age < 1.15f)
            {
                float gulp = age > .5f && age < .9f ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin((age - .5f) / .1f * Mathf.PI)), 2f) * .05f : 0f;
                wide = 1f + gulp * 1.6f;
                return .4f + .3f * Mathf.SmoothStep(0f, 1f, Clamp01((age - .5f) / .4f)) + gulp;
            }
            if (age < 1.65f)
            {
                float c = (age - 1.15f) / .5f;
                wide = 1f + .07f * c; tremble = 1f + 4f * c;
                return .7f + .45f * c * c;
            }
            if (age < 1.95f)
            {
                float r = (age - 1.65f) / .3f, w = Mathf.Exp(-r * 6f);
                wide = 1f - .08f * w;
                return 1f + .15f * w * Mathf.Cos(r * Mathf.PI * 3f);
            }
            return 1f - .15f * Clamp01((age - 1.95f) / 1.35f);
        }

        // The king stands where the giant plane assembled: centre (270, 694 - lift), shaking while sweeping.
        Vector2 KingMouthAt(Finale finale, float age, float after)
        {
            float scale = KingScale(age, out _, out float tremble);
            float lift = FinaleLift(age, after), shake = finale.Sweeping ? Mathf.Sin(clock * 70f) * 1.6f : Mathf.Sin(clock * 83f) * tremble;
            return new Vector2(270f + shake, 694f - lift + KingMouth * KingSize * scale);
        }
        void DrawBubbleKing(PixelSurface s, Finale finale, float age, float after, Vector2 mouth)
        {
            float assemble = Clamp01((age - .5f) / .65f);
            if (assemble <= 0f) return;
            float alpha = (1f - Clamp01(after / 1.1f)) * Clamp01(assemble / .35f);
            float scale = KingScale(age, out float wide, out _);
            float x = mouth.x, y = mouth.y - KingMouth * KingSize * scale;
            for (int i = 0; i < 7; i++)
            {
                float angle = clock * 1.2f + finaleOrbitPhases[i];
                s.Glow(x + Mathf.Cos(angle) * 40f, y + Mathf.Sin(angle) * 30f, 275f, 275f, A(finaleRainbow[i], alpha * .12f), 0f);
            }
            s.Glow(x, y, 375f, 325f, new Color(1f, 1f, 1f, alpha * .25f), 0f);
            float size = KingSize * scale;
            bool blowing = age >= 1.65f && age < 3.3f;
            string art = blowing && GardenSkin.Has(KingBlow) ? KingBlow : KingReady;
            float squash = wide * (blowing ? 1f + Mathf.Sin(clock * 30f) * .015f : 1f);
            if (age >= 1.15f && age < 1.65f)   // charging glow grows with the swell
                s.Glow(x, y, size * 1.5f, size * 1.5f, new Color(1f, .92f, .6f, alpha * .35f * (age - 1.15f) / .5f), 0f);
            Asset(s, art, x - size * .5f * squash, y - size * .5f / squash, size * squash, size / squash, new Color(1f, 1f, 1f, alpha));
            if (assemble < 1f) s.Glow(x, y, size * 1.2f, size * 1.2f, new Color(1f, 1f, 1f, (1f - assemble) * .5f * alpha), 0f);
            // Charging: rainbow bubbles drawn in towards the mouth.
            if (age >= 1.15f && age < 1.65f)
                for (int i = 0; i < 14; i++)
                {
                    float angle = i * 2.4f, cycle = FinaleRepeat(i / 14f + clock * 1.6f, 1f), radius = (1f - cycle) * 110f;
                    GardenSkin.Shot(s, finaleRainbow[i % 7], mouth + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, 7f + 5f * (1f - cycle), alpha * cycle);
                }
        }

        // f54749 spread and fade: rays every 7.5 degrees up to finale.Spread, then fully open while fading out.
        void DrawBubbleFan(PixelSurface s, Finale finale, float age, float after, Vector2 mouth)
        {
            float alpha, spread;
            if (age >= 3.15f) { alpha = Clamp01(1f - (age - 3.15f) / .7f) * (1f - Clamp01(after / .6f)); spread = 90f; }
            else { alpha = Clamp01((age - 1.65f) / .08f); spread = finale.Spread; }
            if (alpha <= 0f || spread <= .5f) return;
            const float step = 7.5f, gap = 36f;   // 2026-10-16: 36 (was 24) to cut the per-frame quads
            float flow = (clock * 420f) % gap;
            for (float angle = -spread; angle <= spread + .01f; angle += step)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
                Color tint = RainbowAt((angle / 180f + .5f) + clock * .15f);
                s.GlowLine(mouth, mouth + dir * 1100f, 34f, A(Color.Lerp(tint, Color.white, .35f), alpha * .2f));
                int n = 0;
                for (float d = 18f + flow; d < 1100f; d += gap, n++)
                {
                    Vector2 p = mouth + dir * d;
                    if (p.y < -30f || p.x < -30f || p.x > 570f) break;
                    float size = (n % 3 == 0 ? 15f : n % 3 == 1 ? 11f : 13f) * Mathf.Clamp01((d - 18f) / 30f);
                    // Only every third (largest) bubble gets the rim layer; the others are a single quad.
                    if (n % 3 == 0) GardenSkin.Shot(s, tint, p, size, alpha);
                    else Asset(s, "bugs/bead-main", p.x - size * .5f, p.y - size * .5f, size, size, A(tint, alpha));
                }
            }
            if (age < 2.1f) GardenFx.Burst(s, "rocket-pop", mouth, 220f, age - 1.65f, .45f);
        }

        // f54753: big end title at (270, 330) from age 3.1, fading with the closing clock.
        void DrawBubbleFinaleEnd(PixelSurface s, Finale finale, float age, float after, Vector2 mouth)
        {
            float endAge = age - 3.15f + .05f;
            if (endAge < 0f) return;
            float fade = 1f - Clamp01(after / .6f);
            if (fade <= 0f) return;
            float scale = endAge < .18f ? 2.6f - 1.6f * Mathf.Min(OutCubic(endAge / .18f), 1f)
                : Mathf.Sin(Mathf.Min((endAge - .18f) / .3f, 1f) * Mathf.PI) * .06f + 1f;
            for (int i = 0; i < 12; i++)
            {
                float angle = (clock * 40f + i * 30f) * Mathf.Deg2Rad;
                Color ray = (i & 1) != 0 ? Color.white : finaleRainbow[i % 7]; ray.a = fade * .16f;
                s.GlowLine(new Vector2(270f, 330f), new Vector2(Mathf.Cos(angle) * 320f + 270f, Mathf.Sin(angle) * 320f + 330f), 46f, ray);
            }
            s.Glow(270f, 330f, scale * 420f, scale * 220f, new Color(1f, .9f, .45f, fade * .35f), 0f);
            float size = 46f * scale;
            TextOutline(s, "全部打光！", 270f, 330f - size * .55f, size, A(Gold, fade), A(Outline, fade), 3.2f * scale);
            if (endAge < .12f) s.Glow(270f, 330f, 700f, 700f, new Color(1f, 1f, 1f, (1f - endAge / .12f) * .7f), 0f);
        }

        // f54754: running count under the header while the fan sweeps.
        void DrawBubbleFinaleCount(PixelSurface s, Finale finale, float age)
        {
            if (age < 1.65f || age >= 3.71f) return;
            float alpha = age < 3.15f ? 1f : Clamp01(1f - (age - 3.15f) / .7f);
            float appear = OutBack(Clamp01((age - 1.65f) / .3f));
            float elapsed = ui - finaleMilestoneAt, pop = Mathf.Max(1f - elapsed * 4f, 0f) * .35f + 1f;
            float size = 24f * appear * pop;
            if (size < 2f) return;
            TextOutline(s, "打掉 ×" + finale.Kills, 270f, 146f - size * .5f, size, A(Cream, alpha), A(Outline, alpha), 2f);
        }
    }
}
#endif
