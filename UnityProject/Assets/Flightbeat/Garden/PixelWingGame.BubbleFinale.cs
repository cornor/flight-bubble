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
        // Registered plush king-blow mouth at (225,227) on the 512-square master.
        const float KingMouthX = 225f / 512f - .5f, KingMouthY = 227f / 512f - .5f;
        static readonly Color FinaleInk = Hex(0x7A3A00);
        bool BubbleFinaleArt => GardenSkin.Has(KingReady) && GardenSkin.Has(FinaleButtonArt);
        // 2026-10-20 (UI redesign 31/33/34): blank plates from the art AI; the words are written here. Each is optional.
        const string ButtonPlate = "finale/button-plate", TitlePlate = "finale/title-plate", ClearPlate = "finale/clear-plate";
        // Keep the illustrated finale controls and their motion; use the plush UI's
        // purple ink, warm gold lettering and orange success accent around them.
        static readonly Color FinalePurple = Hex(0x553094), FinaleYellow = Hex(0xFFF794), FinaleOrange = Hex(0xE78B24);
        string ButtonArt => GardenSkin.Has(ButtonPlate) ? ButtonPlate : FinaleButtonArt;

        // ---- button (same 340x86 box at (270, 759); hit regions come from the muted original)
        // 2026-10-17 17:03 (user: it appeared like an ordinary button, not a super-move switch). Entrance (~1 s): the
        // screen dims a little and every member on the pads shoots a beam of its color into the button spot; the button
        // slams down from 2.2x with a screen kick, a rainbow shockwave and a ring of bubbles, and the title flashes
        // white. While waiting: a rainbow comet runs round the rim, a heartbeat every 1.2 s with a rainbow echo of the
        // button, and small bubbles and stars orbit it.
        const float SlamAt = .45f, SlamTime = .15f;
        float finaleSlamKicked = -1f;
        void DrawBubbleFinaleButton(PixelSurface s)
        {
            if (!BubbleFinaleArt) { DrawFinaleButton(s); return; }
            s.BeginMute();
            try { DrawFinaleButton(s); } finally { s.EndMute(); }
            if (!FinaleButtonVisible || finaleReadyAt < 0f) return;
            float age = ui - finaleReadyAt;
            const float cx = 270f, cy = 759f;
            // 1) dim + beams from the squad into the button spot
            if (age < 1.1f)
            {
                float dim = Clamp01(age / .15f) * Clamp01((1.1f - age) / .4f);
                bool mapped = s.MapLayout;
                s.MapLayout = false;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.03f, .08f, .2f, .3f * dim));
                s.MapLayout = mapped;
                for (int i = 0; i < Battle.Fleet.Count; i++)
                {
                    var ship = Battle.Fleet[i];
                    Vector2 from = feedback.Position(ship.Id, i) + new Vector2(0f, -22f), to = new Vector2(cx, cy);
                    float head = Clamp01((age - i * .03f) / SlamAt), tail = Clamp01((age - SlamAt - .05f) / .3f);
                    if (head <= tail) continue;
                    Color c = Color.Lerp(GardenColor(ship.Color), Color.white, .25f);
                    Vector2 a = Vector2.Lerp(from, to, tail * tail), b = Vector2.Lerp(from, to, head * head);
                    s.GlowLine(a, b, 22f, A(c, .55f));
                    s.GlowLine(a, b, 7f, A(Color.white, .7f));
                    s.Glow(b.x, b.y, 34f, 34f, A(c, .9f), 0f);
                }
            }
            if (age < SlamAt) return;
            // 2) slam
            float t = age - SlamAt, alpha = Clamp01(t / .06f), scale;
            if (t < SlamTime) { float u = t / SlamTime; scale = 2.2f - 1.2f * u * u; }
            else { float r = t - SlamTime; scale = 1f - .08f * Mathf.Exp(-r * 8f) * Mathf.Cos(r * 30f); }
            if (t >= SlamTime && finaleSlamKicked != finaleReadyAt) { finaleSlamKicked = finaleReadyAt; feedback.Kick(7f); }
            float hit = t - SlamTime;
            if (hit >= 0f && hit < .6f)
            {
                float u = hit / .6f;
                for (int k = 0; k < 3; k++)
                {
                    float uk = Clamp01(u * 1.15f - k * .08f);
                    s.Ring(new Vector2(cx, cy), 60f + 230f * uk, A(finaleRainbow[k * 2 % 7], (1f - uk) * .8f), 6f * (1f - uk) + 1.5f, 40, 0f, 360f);
                }
                for (int k = 0; k < 12; k++)
                {
                    float ang = k * 30f * Mathf.Deg2Rad + .3f, dist = 40f + 170f * (1f - (1f - u) * (1f - u));
                    SoapBubble(s, new Vector2(cx + Mathf.Cos(ang) * dist * 1.3f, cy + Mathf.Sin(ang) * dist * .6f), 18f + (k % 3) * 6f,
                        Color.Lerp(Color.white, finaleRainbow[k % 7], .4f), (1f - u), k);
                }
                if (hit < .2f) s.Glow(cx, cy, 520f, 260f, new Color(1f, 1f, 1f, (1f - hit / .2f) * .55f), 0f);
            }
            // 3) waiting: heartbeat, echo, comet, orbiters
            float beat = 0f;
            if (t > .8f)
            {
                float b = ((t - .8f) % 1.2f) / 1.2f;
                beat = b < .15f ? Mathf.Sin(b / .15f * Mathf.PI) : 0f;
                float e = Clamp01(b / .7f);
                if (b < .7f)
                {
                    float k = 1f + e * .32f, ew = 340f * k, eh = 86f * k + e * 34f;
                    Asset(s, ButtonArt, cx - ew * .5f, cy - eh * .5f, ew, eh, A(Color.Lerp(Color.white, RainbowAt(ui * .2f), .55f), (1f - e) * (1f - e) * .5f));
                }
            }
            scale *= (1f + .06f * beat) * (1f - PressPulse(8) * .05f);
            float pulse = Mathf.Sin(ui * 5f) * .5f + .5f, w = 340f * scale, h = 86f * scale;
            s.Glow(cx, cy, w + 110f, h + 90f, new Color(1f, .82f, .3f, alpha * (.3f + .15f * pulse + .25f * beat)), 0f);
            for (int k = 0; k < 6; k++)   // orbiting bubbles and stars
            {
                float ang = ui * .9f + k * Mathf.PI / 3f;
                Vector2 p = new Vector2(cx + Mathf.Cos(ang) * (w * .5f + 26f), cy + Mathf.Sin(ang) * (h * .5f + 20f));
                if (k % 2 == 0) SoapBubble(s, p, 14f, Color.white, alpha * .9f, k);
                else { s.Glow(p.x, p.y, 18f, 18f, new Color(1f, .95f, .6f, alpha * .9f), 0f); s.Glow(p.x, p.y, 6f, 6f, A(Color.white, alpha), 0f); }
            }
            DrawFinaleButtonFace(s, scale, alpha);
            // rainbow comet running round the pill rim
            if (t > SlamTime)
                for (int k = 0; k < 7; k++)
                {
                    Vector2 p = PillPoint(cx, cy, w * .5f - 6f * scale, h * .5f - 6f * scale, ui * .45f - k * .012f);
                    s.Glow(p.x, p.y, 22f - k * 2f, 22f - k * 2f, A(finaleRainbow[k], alpha * (1f - k / 8f)), 0f);
                }
            if (t > SlamTime && t < SlamTime + .25f)   // title flash
            {
                float f = 1f - (t - SlamTime) / .25f;
                FaceTitle(scale, out float tx, out float ty, out float title);
                bool mapped = s.MapLayout;
                float dy = GardenBars.ScreenY(s, 759f) - 759f;
                s.MapLayout = false;
                GardenBarArt.TextOutline(s, "决 战", tx, ty + dy, title, A(Color.white, f), A(Color.white, f * .6f), 2.6f * scale);
                s.MapLayout = mapped;
            }
        }
        // A point on a pill outline (straight top/bottom, round ends), u = fraction of the way round.
        static Vector2 PillPoint(float cx, float cy, float hw, float hh, float u)
        {
            float r = hh, straight = Mathf.Max(0f, hw - r) * 2f, arc = Mathf.PI * r, total = straight * 2f + arc * 2f;
            float d = ((u % 1f) + 1f) % 1f * total;
            if (d < straight) return new Vector2(cx - straight * .5f + d, cy - r);
            d -= straight;
            if (d < arc) { float a = -Mathf.PI * .5f + d / r; return new Vector2(cx + straight * .5f + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r); }
            d -= arc;
            if (d < straight) return new Vector2(cx + straight * .5f - d, cy + r);
            d -= straight;
            float b = Mathf.PI * .5f + d / r;
            return new Vector2(cx - straight * .5f + Mathf.Cos(b) * r, cy + Mathf.Sin(b) * r);
        }
        // 2026-10-17 (user, readability): the 10 px second line ("全员合体 · 彩虹泡泡扫射") is gone; the guide above the
        // button already says it. The title alone, a little bigger, centred on the face.
        // 2026-10-20 (user, UI review): with the new plate the second line comes back, bigger: 「点击合体」 in the plate's
        // purple pill (centre at 82 % of its height); 「决 战」 sits above it, a little right of centre (crown on the left).
        void FaceTitle(float scale, out float x, out float top, out float size)
        {
            bool plate = GardenSkin.Has(ButtonPlate);
            size = (plate ? 40f : 38f) * scale;   // 2026-10-21 (user): 34 -> 40 on the plate, fills the button
            x = 270f;   // 2026-10-20 21:24 (user): centred on the same axis as 「点击合体」 (was 17 px right of it)
            float centre = plate ? 759f - 43f * scale + 86f * scale * .38f : 759f - 3f * scale;
            top = centre - size * (plate ? .55f : .5f);
        }
        void DrawFinaleButtonFace(PixelSurface s, float scale, float alpha)
        {
            float w = 340f * scale, h = 86f * scale;
            bool plate = GardenSkin.Has(ButtonPlate);
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 759f);
            s.MapLayout = false;
            try
            {
                Asset(s, ButtonArt, 270f - w * .5f, cy - h * .5f, w, h, new Color(1f, 1f, 1f, alpha));
                FaceTitle(scale, out float x, out float top, out float title);
                top += cy - 759f;
                if (!plate) { GardenBarArt.TextOutline(s, "决 战", x, top, title, A(Cream, alpha), A(FinaleInk, alpha), 2.6f * scale); return; }
                GardenBarArt.TextOutline(s, "决 战", x, top, title, A(FinaleYellow, alpha), A(FinalePurple, alpha), 2.8f * scale);
                float sub = 15f * scale, pill = cy - h * .5f + h * .82f;   // 2026-10-21: 13 -> 15
                GardenBarArt.TextOutline(s, "点击合体", 270f, pill - sub * .58f, sub, A(Cream, alpha), A(FinalePurple, alpha), 1.3f * scale);
                // 2026-10-21 (user, the right side looked empty next to the crown): a few see-through rainbow bubbles
                // rise off the ribbon on the right, swell, drift a little and fade, then come round again.
                for (int k = 0; k < 3; k++)
                {
                    float u = ui / 2.2f + k / 3f, t = u - Mathf.Floor(u), life = Mathf.Sin(t * Mathf.PI);
                    float bx = 270f + w * (.22f + .07f * k) + Mathf.Sin(t * 6.3f + k) * 3f * scale;
                    float by = cy + h * .2f - t * h * .55f, d = (k % 2 == 0 ? 10f : 7.5f) * scale * (.6f + .4f * life);
                    Color tint = Color.Lerp(Color.white, finaleRainbow[(k * 2 + (int)Mathf.Floor(u)) % 7], .45f);
                    SoapBubble(s, new Vector2(bx, by), d, tint, alpha * life * .9f, k);
                }
            }
            finally { s.MapLayout = mapped; }
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
            // 2026-10-17 (user: the lighter box with four gold corners read like a debug selection). One even veil over
            // everything; the button is drawn again on top of it, breathing, with rainbow echoes swelling out of it.
            Color mask = new Color(.12f, .09f, .22f, alpha * .7f);
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, mask);
            s.MapLayout = mapped;
            for (int i = 0; i < 3; i++)
            {
                float t = FinaleRepeat(ui * .8f + i / 3f, 1f), k = 1f + t * .3f;
                float ew = 340f * k, eh = 86f * k + t * 36f;
                Color echo = Color.Lerp(Color.white, finaleRainbow[i * 2 % 7], .55f);
                Asset(s, ButtonArt, 270f - ew * .5f, 759f - eh * .5f, ew, eh, A(echo, alpha * (1f - t) * (1f - t) * .5f));
            }
            float breathe = 1f + .045f * pulse;
            s.Glow(270f, 759f, 460f * breathe, 176f * breathe, new Color(1f, .85f, .35f, alpha * (.25f + .2f * pulse)), 0f);
            DrawFinaleButtonFace(s, breathe * (1f - PressPulse(8) * .05f), alpha);
            HintCapsule(s, "决战时刻", 496f, 172f, 40f, 20f, GardenBars.Gold, GardenBars.Brown,
                alpha, 1f, "crown", GardenBars.Deco.Many, finaleGuideAge);
            GardenBarArt.TextOutline(s, "全员已出战 · 泡泡不到 " + Flightbeat.Model.Battle.FinaleThreshold + " 个", 270f, 528f, 16f, A(Cream, alpha), A(Outline, alpha), 1.6f);
            // Speech bubble with Xiao Lan; reveal at the original typing speed (spokenCharacters of the source line).
            int shown = dialogue.Length == 0 ? FinaleGuideLine.Length
                : Mathf.Clamp(Mathf.CeilToInt(FinaleGuideLine.Length * (float)spokenCharacters / dialogue.Length), 0, FinaleGuideLine.Length);
            string[] lines = FinaleGuideLine.Substring(0, shown).Split('\n');
            DrawGardenDialogue(s, "ui/guide-happy", lines, 632f, alpha);
            float bounce = Mathf.Abs(Mathf.Sin(ui * 5f)) * 7f;
            Asset(s, "fx/arrow", 256f, 692f - bounce, 28f, 28f, new Color(1f, 1f, 1f, alpha));
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
            s.GardenRoundedRect(12f, 574f, 516f, 300f, 24f,
                new Color(.44f,.28f,.65f,dim*.12f),new Color(.44f,.28f,.65f,dim*.2f));
            BubbleFinaleCues(age);
            DrawBubbleFinaleHold(s, age, after);
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
            DrawBubbleFinaleBursts(s, age, after);
            DrawBubbleFinaleRelease(s, age, after, mouth);
            DrawBubbleFinaleEnd(s, finale, age, after, mouth);
            DrawBubbleFinaleCount(s, finale, age);
        }

        void DrawBubbleFinaleTitle(PixelSurface s, float age)
        {
            if (age > 1.5f) return;
            float appear = OutBack(age / .3f, 2.4f), alpha = Clamp01((1.5f - age) / .35f);
            if (appear < .05f) return;
            if (GardenSkin.Has(TitlePlate))
            {
                // 2026-10-20 (UI redesign 33): rainbow plate 520x100 centred at y 305; 「决 战」 at 45 %, the subtitle in
                // the cream pill at 82 % of its height.
                // Map the entire banner once: separately mapped glyphs drift out of the plate on tall screens.
                bool mapped = s.MapLayout;
                float pw = 520f * appear, ph = 100f * appear, pcy = GardenBars.ScreenY(s, 305f);
                s.MapLayout = false;
                try
                {
                    s.Glow(270f, pcy, pw + 80f, ph + 60f, new Color(1f, .9f, .5f, alpha * .35f), 0f);
                    Asset(s, TitlePlate, 270f - pw * .5f, pcy - ph * .5f, pw, ph, new Color(1f, 1f, 1f, alpha));
                    float big = 46f * appear;
                    GardenBarArt.TextOutline(s, "决 战", 270f, pcy - ph * .05f - big * .55f, big, A(FinaleYellow, alpha), A(FinalePurple, alpha), 3f * appear);
                    if (age > .35f)
                    {
                        float a = alpha * Clamp01((age - .35f) / .2f), sub = 16f * appear;
                        GardenBarArt.Text(s, "泡泡大王 · 彩虹扫射！", 270f, pcy + ph * .32f - sub * .58f, sub, true, A(FinalePurple, a));
                    }
                }
                finally { s.MapLayout = mapped; }
                return;
            }
            s.Glow(270f, 300f, appear * 420f, appear * 150f, new Color(1f, .9f, .5f, alpha * .35f), 0f);
            // 17:10 (user: the subtitle sat in the wrong place): the rainbow bars ran through 泡泡大王 · 彩虹扫射！
            // (bars 316-344, subtitle 336). Bars now sit right under 决 战 (296-324) and the subtitle below them.
            for (int i = 0; i < 7; i++)
                s.Rect(270f - appear * 150f, appear * 30f + 266f + i * 4f, appear * 300f, 3f, A(finaleRainbow[i], alpha * .75f));
            GardenBarArt.TextOutline(s, "决 战", 270f, 262f - 26f * appear, 52f * appear, A(Gold, alpha), A(Outline, alpha), 3f * appear);
            if (age > .35f)
            {
                float a = alpha * Clamp01((age - .35f) / .2f);
                s.Glow(270f, 349f, 300f, 46f, new Color(.03f, .08f, .2f, .45f * a), 0f);
                GardenBarArt.TextOutline(s, "泡泡大王 · 彩虹扫射！", 270f, 338f, 21f, A(Cream, a), A(Outline, a), 1.8f);
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
                float hold = Clamp01((age - 1.45f) / .2f);   // 23:09: the last breath puffs the cheeks out further
                wide = 1f + .07f * c + .08f * hold; tremble = 1f + 4f * c + 2f * hold;
                return .7f + .45f * c * c;
            }
            if (age < 1.95f)
            {
                float r = (age - 1.65f) / .3f, w = Mathf.Exp(-r * 6f);
                wide = 1f - .14f * w;
                return 1f + .24f * w * Mathf.Cos(r * Mathf.PI * 3f);   // 23:09: recoil .15 -> .24
            }
            return 1f - .15f * Clamp01((age - 1.95f) / 1.35f);
        }

        // The king stands where the giant plane assembled: centre (270, 694 - lift), shaking while sweeping.
        Vector2 KingMouthAt(Finale finale, float age, float after)
        {
            float scale = KingScale(age, out float wide, out float tremble);
            float lift = FinaleLift(age, after), shake = finale.Sweeping ? Mathf.Sin(clock * 70f) * 1.6f : Mathf.Sin(clock * 83f) * tremble;
            bool blowing = age >= 1.65f && age < 3.3f;
            float squash = wide * (blowing ? 1f + Mathf.Sin(clock * 30f) * .015f : 1f);
            return new Vector2(270f + shake + KingMouthX * KingSize * scale * squash,
                PlushLayout.InverseY(PlushLayout.Y(694f - lift) + KingMouthY * KingSize * scale / squash));
        }
        void DrawBubbleKing(PixelSurface s, Finale finale, float age, float after, Vector2 mouth)
        {
            float assemble = Clamp01((age - .5f) / .65f);
            if (assemble <= 0f) return;
            float alpha = (1f - Clamp01(after / 1.1f)) * Clamp01(assemble / .35f);
            float scale = KingScale(age, out float wide, out float tremble);
            float shake = finale.Sweeping ? Mathf.Sin(clock * 70f) * 1.6f : Mathf.Sin(clock * 83f) * tremble;
            float x = 270f + shake, y = 694f - FinaleLift(age, after);
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
            bool mapped = s.MapLayout;
            float screenY = mapped ? PlushLayout.Y(y) : y;
            s.MapLayout = false;
            Asset(s, art, x - size * .5f * squash, screenY - size * .5f / squash, size * squash, size / squash, new Color(1f, 1f, 1f, alpha));
            s.MapLayout = mapped;
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
            float punch = Clamp01(1f - (age - 1.65f) / .25f);   // 23:09: the first quarter second comes out thick and bright
            for (float angle = -spread; angle <= spread + .01f; angle += step)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
                Color tint = RainbowAt((angle / 180f + .5f) + clock * .15f);
                s.GlowLine(mouth, mouth + dir * 1100f, 34f * (1f + 1.2f * punch), A(Color.Lerp(tint, Color.white, .35f + .3f * punch), alpha * (.2f + .3f * punch)));
                int n = 0;
                for (float d = 18f + flow; d < 1100f; d += gap, n++)
                {
                    Vector2 p = mouth + dir * d;
                    if (p.y < -30f || p.x < -30f || p.x > 570f) break;
                    // 2026-10-20 14:22 (user): the rainbow bubbles 30 % bigger (15/11/13 -> x1.3).
                    float size = (n % 3 == 0 ? 15f : n % 3 == 1 ? 11f : 13f) * 1.3f * Mathf.Clamp01((d - 18f) / 30f) * (1f + .4f * punch);
                    // Only every third (largest) bubble gets the rim layer; the others are a single quad.
                    if (n % 3 == 0) GardenSkin.Shot(s, tint, p, size, alpha);
                    else Asset(s, "bugs/bead-main", p.x - size * .5f, p.y - size * .5f, size, size, A(tint, alpha));
                }
            }
            if (age < 2.1f) GardenFx.Burst(s, "rocket-pop", mouth, 220f, age - 1.65f, .45f);
        }

        // 2026-10-19 23:09 (user: after the deep breath the release had no weight, the move did not feel strong).
        // Two beats now. Hold (1.45-1.65 s): the screen darkens further, a rainbow ring closes in on the mouth, the
        // cheeks puff out and the music drops away. Release (1.65 s): a harder screen
        // kick, a bright flash with rainbow speed lines, three shockwave rings out to the screen edge and a ring of
        // bubbles flung from the mouth; the fan's first quarter second is thick and bright and the king recoils harder.
        // The original FinaleEvents (protected) still plays its cues, flash, kick and hit-stop at 1.65 s.
        float bubbleFinaleLastAge = -1f;
        void BubbleFinaleCues(float age)
        {
            float last = bubbleFinaleLastAge;
            bubbleFinaleLastAge = age;
            if (last > age) last = -1f;   // a new finale
            if (last < 1.45f && age >= 1.45f) audioChip?.GardenDuckMusic(.95f, .22f, .9f);
            // 2026-10-20 09:38 (user picked option 1, "光炮"): the release is now maxpower.ogg + finale-beam.ogg themselves
            // (scripts/r1v3/bubble_audio_finale2.py), so the borrowed bubble-bomb impact and shockwave are gone.
            if (last < 1.65f && age >= 1.65f) feedback.Kick(14f);
        }

        void DrawBubbleFinaleHold(PixelSurface s, float age, float after)
        {
            if (age < 1.45f || age >= 1.65f || after > 0f) return;
            float h = (age - 1.45f) / .2f;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.02f, .05f, .14f, .28f * h));
            s.MapLayout = mapped;
        }

        void DrawBubbleFinaleRelease(PixelSurface s, float age, float after, Vector2 mouth)
        {
            float fade = 1f - Clamp01(after / .6f);
            if (fade <= 0f) return;
            if (age >= 1.45f && age < 1.65f)
            {
                // The breath: a rainbow ring shrinking into the mouth, twice.
                for (int k = 0; k < 2; k++)
                {
                    float t = Clamp01((age - 1.45f - k * .06f) / .14f);
                    if (t <= 0f || t >= 1f) continue;
                    s.Ring(mouth, Mathf.Lerp(170f, 24f, InOut(t)), A(finaleRainbow[(k * 3) % 7], .75f * t * fade), 3f + 7f * t, 40, 0f, 360f);
                }
                return;
            }
            float r = age - 1.65f;
            if (r < 0f || r > .7f) return;
            if (r < .3f)
            {
                // Flash: bright white, a rainbow tinge as it fades.
                bool mapped = s.MapLayout;
                s.MapLayout = false;
                float f = 1f - r / .3f;
                s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, A(Color.Lerp(RainbowAt(clock * .5f), Color.white, f), .75f * f * f * fade));
                s.MapLayout = mapped;
            }
            s.Glow(mouth.x, mouth.y, 520f * (1f - r), 520f * (1f - r), new Color(1f, .97f, .85f, .7f * (1f - r / .7f) * fade), 0f);
            // Speed lines racing out from the mouth.
            if (r < .4f)
                for (int i = 0; i < 24; i++)
                {
                    float a = (i * 15f + (i & 1) * 7f) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    float d0 = 60f + r * 1400f + (i % 3) * 30f;
                    s.GlowLine(mouth + dir * d0, mouth + dir * (d0 + 160f), 10f, A(Color.Lerp(finaleRainbow[i % 7], Color.white, .5f), .7f * (1f - r / .4f) * fade));
                }
            // Three shockwave rings out to the screen edge.
            for (int k = 0; k < 3; k++)
            {
                float t = (r - k * .07f) / .5f;
                if (t <= 0f || t >= 1f) continue;
                float e = OutCubic(t);
                s.Ring(mouth, 30f + 700f * e, A(Color.Lerp(finaleRainbow[k * 2 % 7], Color.white, .3f), .85f * (1f - t) * fade), 4f + 22f * (1f - t), 64, 0f, 360f);
            }
            // A ring of bubbles flung from the mouth.
            for (int i = 0; i < 28; i++)
            {
                float t = (r - (i % 4) * .02f) / .6f;
                if (t <= 0f || t >= 1f) continue;
                float a = (i * 12.86f + (i % 3) * 5f) * Mathf.Deg2Rad;
                float reach = 150f + (i * 37 % 5) * 45f;
                Vector2 at = mouth + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (OutCubic(t) * reach + 20f);
                GardenSkin.Shot(s, finaleRainbow[i % 7], at, (16f - 8f * t) * (i % 2 == 0 ? 1f : .7f), (1f - t * t) * fade);
            }
        }

        // 2026-10-17 (user: the original sparks/explosions in the finale -> bubble style). The original particle bursts
        // (FinaleEvents f54734: squad merge, power-up ring at 1.15 s, victory volley at 3.15 s) are hidden now
        // (BattleDraw), and soap bubbles take their place at the same moments and spots.
        void DrawBubbleFinaleBursts(PixelSurface s, float age, float after)
        {
            float fade = 1f - Clamp01(after / .6f);
            if (fade <= 0f) return;
            // The squad has merged into the king (~.88 s): one soft pop at the king.
            float m = (age - .88f) / .45f;
            if (m >= 0f && m < 1f) SoapPop(s, new Vector2(270f, 694f), 46f, m, fade);
            // Power-up (1.15 s): seven rainbow bubbles swell on the ring around the king, rise a little and pop in turn.
            for (int i = 0; i < 7; i++)
            {
                float t = age - 1.15f - i * .035f;
                if (t < 0f || t > .9f) continue;
                Vector2 at = finaleTierUpAt[i];
                if (t < .45f) SoapBubble(s, at + new Vector2(0f, -t * 30f), 54f * OutBack(Clamp01(t / .25f), 2f), Color.Lerp(Color.white, finaleRainbow[i], .35f), fade, i);
                else SoapPop(s, at + new Vector2(0f, -13.5f), 30f, (t - .45f) / .45f, fade);
            }
            // Victory volley (3.15 s): a dozen big bubbles burst across the board, flinging droplet bubbles.
            for (int i = 0; i < 12; i++)
            {
                float t = age - 3.15f - (i % 6) * .05f;
                if (t < 0f || t > .7f) continue;
                Vector2 at = i < 7 ? new Vector2(40f + i * 76f, (i & 1) == 0 ? 200f : 320f)
                    : new Vector2(70f + (i - 7) * 100f, ((i - 7) & 1) == 0 ? 260f : 370f);
                float r = i < 7 ? 40f : 56f;
                SoapPop(s, at, r, t / .7f, fade);
                if (t < .5f)
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 1.047f + i;
                        GardenSkin.Shot(s, finaleRainbow[(i + k) % 7], at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * (.6f + 1.6f * t),
                            7f * (1f - t * 1.6f) + 2f, fade * (1f - t * 2f));
                    }
            }
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
            if (GardenSkin.Has(ClearPlate))
            {
                // 2026-10-20 (UI redesign 34): gold plate 520x110 (title at 45 %, count pill at 84 %), red title with a
                // white outline and the final count in the pill.
                bool mapped = s.MapLayout;
                float pw = 520f * scale, ph = 110f * scale, pcy = GardenBars.ScreenY(s, 335f);
                s.MapLayout = false;
                try
                {
                    Asset(s, ClearPlate, 270f - pw * .5f, pcy - ph * .5f, pw, ph, new Color(1f, 1f, 1f, fade));
                    float big = 40f * scale;
                    GardenBarArt.TextOutline(s, "全部打光！", 270f, pcy - ph * .05f - big * .55f, big, A(FinaleOrange, fade), A(Color.white, fade), 3.2f * scale);
                    float sub = 16f * scale;
                    GardenBarArt.Text(s, "打掉 ×" + finale.Kills, 270f, pcy + ph * .34f - sub * .58f, sub, true, A(FinalePurple, fade));
                }
                finally { s.MapLayout = mapped; }
            }
            else
            {
                float size = 46f * scale;
                GardenBarArt.TextOutline(s, "全部打光！", 270f, 330f - size * .55f, size, A(Gold, fade), A(Outline, fade), 3.2f * scale);
            }
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
            GardenBarArt.TextOutline(s, "打掉 ×" + finale.Kills, 270f, 146f - size * .5f, size, A(Cream, alpha), A(Outline, alpha), 2f);
        }
    }
}
#endif
