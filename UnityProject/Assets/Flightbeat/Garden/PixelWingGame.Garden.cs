#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        struct GardenBeadDraw { public int index; public Vector2 at; public float squash; }
        readonly System.Collections.Generic.List<GardenBeadDraw> gardenBeadDraws = new System.Collections.Generic.List<GardenBeadDraw>(4096);
        static readonly System.Comparison<GardenBeadDraw> GardenBeadOrder = (a, b) => {
            int order = a.at.y.CompareTo(b.at.y);
            if (order == 0) order = a.at.x.CompareTo(b.at.x);
            return order != 0 ? order : a.index.CompareTo(b.index);
        };
        Color GardenColor(int index) {
            var p = Battle.Level.palette; int i = index * 3;
            return new Color32((byte)p[i], (byte)p[i + 1], (byte)p[i + 2], 255);
        }
        // v2 (UI spec v1.1 section 5, art batch 2): wooden frame + paper; falls back to the graybox panel.
        void GardenPanel(PixelSurface s, float x, float y, float w, float h) {
            if (Asset9(s, "ui/popup-panel", x, y, w, h, 30f, 30f, Mathf.Min(18f, h * .3f), Mathf.Min(18f, h * .3f), Color.white)) return;
            s.Rect(x, y, w, h, Paper); s.Border(x, y, w, h, Wood, 2);
        }
        void GardenBanner(PixelSurface s, string id, float x, float y, float w, float h) {
            if (!Asset9(s, id, x, y, w, h, 45f, 0f, h * .58f, 0f, Color.white)) GardenPanel(s, x, y, w, h);
        }
        // The selected light glass hint: one rigid capsule, so its rounded ends and text keep
        // their proportions on tall screens. This is a prompt above the rail, with no hit box.
        void DrawGardenReadyHint(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            // 2026-10-19 11:01: a gentle 2.5 px float so the waiting prompt draws the eye.
            // 11:13 (user: it hid part of the picture): moved below the rail, into the empty pad row (nobody is on the
            // pads before the first deployment), right above the queue it points to; narrower so it clears the locked
            // pads' ad badges.
            // 2026-10-21 (quality pass): 8 px higher (600 -> 592) so it keeps a gap above the pad row.
            float cy = (mapped ? PlushLayout.Y(592f) : 592f) + Mathf.Sin(ui * 2.2f) * 2.5f;
            s.MapLayout = false;
            try
            {
                // 2026-10-20 (UI redesign 01): tap icon, sky edge, one decoration bubble on the rim.
                const string ready = "点下方队员，开始出战";
                GardenBars.Bar(s, 270f, cy, Mathf.Max(318f, GardenBars.Width(ready, 22f, 50f, "tap")), 50f, GardenBars.Sky, "tap", ready, 22f,
                    GardenBars.Navy, 1f, 1f, GardenBars.Deco.One, BarAge(0));
            }
            finally { s.MapLayout = mapped; }
        }
        // Seconds since a persistent bar (by key) started showing: a bar not drawn for 0.1 s counts as new next time.
        readonly float[] barShownAt = new float[16], barLastDrawn = { -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f,
            -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f };   // 8..15: the 合 badge per lane
        float BarAge(int key)
        {
            if (ui - barLastDrawn[key] > .1f) barShownAt[key] = ui;
            barLastDrawn[key] = ui;
            return ui - barShownAt[key];
        }

        // 2026-10-19 11:07 (user picked C): the stalled-fleet hint uses the same glass capsule, with a coral edge, a warning
        // sign and the problem line in red; the second line (what to do) stays navy. Not red all over: red is the rail's
        // danger colour, and being stuck is not yet danger.
        void DrawGardenStallHint(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            float cy = mapped ? PlushLayout.Y(312f) : 312f;
            s.MapLayout = false;
            try
            {
                const float cx = 270f, w = 420f, h = 72f;
                GardenBars.Capsule(s, cx, cy, w, h, GardenBars.Coral);
                if (!GardenBars.Icon(s, "warn", cx - w * .5f + 45f, cy, h * .82f)) WarnSign(s, cx - w * .5f + 34f, cy, 1f);
                const float textCenter = cx + 32f;
                GardenBarArt.Text(s, "出战位满了，同色泡泡被挡住啦", textCenter, cy - 23f, 19f, true, GardenBars.DeepCoral);
                const string second = "试试泡泡炸弹，清开最下排";
                float lineW = GardenBarArt.TextWidth(second, 17f), bomb = 27f;
                bool hasBomb = GardenBars.HasIcon("bomb");
                float start = textCenter - (lineW + (hasBomb ? bomb + 5f : 0f)) * .5f;
                if (hasBomb) GardenBars.Icon(s, "bomb", start + bomb * .5f, cy + 13f, bomb);
                GardenBarArt.Text(s, second, start + (hasBomb ? bomb + 5f : 0f), cy + 6f, 17f, false, GardenBars.Navy);
            }
            finally { s.MapLayout = mapped; }
        }

        // 2026-10-19 11:12: one-line hints in the glass capsule (logical y; rigid on tall screens like the ready hint).
        // 2026-10-20 (UI redesign): optional icon and decoration bubbles; the bar widens to fit its icon and text.
        void HintCapsule(PixelSurface s, string text, float logicalCy, float w, float h, float size, Color edge, Color ink, float alpha = 1f, float fill = 1f,
            string icon = null, GardenBars.Deco deco = GardenBars.Deco.None, float age = 1f)
        {
            bool mapped = s.MapLayout;
            float cy = mapped ? PlushLayout.Y(logicalCy) : logicalCy;
            s.MapLayout = false;
            try
            {
                w = Mathf.Min(520f, Mathf.Max(w, GardenBars.Width(text, size, h, icon)));
                // See-through glass: a thin white outline keeps the text readable over the beads.
                GardenBars.Bar(s, 270f, cy, w, h, edge, icon, text, size, ink, alpha, fill, deco, age, fill < .9f);
            }
            finally { s.MapLayout = mapped; }
        }
        // Kind of a short message (toasts and the model's notices): edge colour, ink and icon (UI redesign 13-20, 26).
        static void MessageStyle(string text, bool refusal, out Color edge, out Color ink, out string icon)
        {
            ink = GardenBars.Navy; icon = null;
            if (text.Contains("重排")) { edge = GardenBars.Mint; icon = "arrows"; }
            else if (text.Contains("解锁") || text.Contains("出战位 +")) { edge = GardenBars.Mint; icon = "unlock"; }
            else if (text.Contains("获得")) { edge = GardenBars.Mint; icon = text.Contains("炸弹") ? "bomb" : text.Contains("优先") ? "pick" : "star"; }
            else if (text.Contains("已满")) { edge = GardenBars.Coral; icon = "warn"; }
            else if (text.Contains("准星")) { edge = GardenBars.Amber; icon = "crosshair"; }
            else if (text.Contains("广告")) { edge = GardenBars.Amber; icon = "ad"; }
            else if (text.Contains("金币")) { edge = GardenBars.Amber; icon = "coin"; }
            else if (text.Contains("连胜") && text.Contains("中断")) { edge = GardenBars.Amber; icon = "broken-link"; }
            else if (text.Contains("先点击")) { edge = GardenBars.Sky; icon = "tap"; }
            else if (refusal) { edge = GardenBars.Coral; ink = GardenBars.DeepCoral; icon = "warn"; }
            else edge = GardenBars.Sky;
        }
        // Top toast (2.2 s): a capsule sized to its text, popping in and fading out; coral edge for the red (refusal) toasts.
        void DrawGardenToast(PixelSurface s)
        {
            float age = ui - toastAt;
            if (age > 2.2f || toast.Length == 0) return;
            string text = GardenWords(toast).Replace("获得 ", "获得");   // 2026-10-20: 「获得泡泡炸弹 ×1」, no space
            float size = Mathf.Clamp(440f / Mathf.Max(1, text.Length), 14f, 17f), w = Mathf.Clamp(TextWidth(text, size) + 70f, 160f, 510f);
            float alpha = Clamp01(age / .15f) * Clamp01((2.2f - age) / .3f);
            bool refusal = toastTint.r > toastTint.g + .3f;
            MessageStyle(text, refusal, out Color edge, out Color ink, out string icon);
            // Avoid occupied hint rows in screen units, keeping the 38px toast rigid on tall screens.
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 108f);
            void Avoid(float top, float bottom)
            {
                if (cy + 19f > top - 6f && cy - 19f < bottom + 6f) cy = bottom + 25f;
            }
            if (feedback != null && feedback.GardenComboVisible)
                Avoid(GardenBars.ScreenY(s, 145f) - 25f, GardenBars.ScreenY(s, 145f) + 25f);
            if (selection == 2)
                Avoid(GardenBars.ScreenY(s, 140f) - 22f, GardenBars.ScreenY(s, 140f) + 22f);
            if (lesson != null && Battle.State != Phase.Intro && Battle.State != Phase.Won && Battle.State != Phase.Lost
                && !settings && selection == 0 && !finaleGuide)
                Avoid(GardenBars.ScreenY(s, 104f) - 20f,
                    GardenBars.ScreenY(s, 180f) + 52f);
            if (Battle != null && Battle.AdvanceHeld > 0f && selection == 0)
                Avoid(GardenBars.ScreenY(s, 240f) - 34f, GardenBars.ScreenY(s, 240f) + 34f);
            if (Battle != null && Battle.Stalled && selection == 0 && !settings && Battle.State == Phase.Playing)
                Avoid(GardenBars.ScreenY(s, 312f) - 36f, GardenBars.ScreenY(s, 312f) + 36f);
            s.MapLayout = false;
            try { HintCapsule(s, text, cy, w, 38f, size, edge, ink, alpha, 1f, icon, GardenBars.Deco.None, age); }
            finally { s.MapLayout = mapped; }
        }
        // Shared painted glass, fixed end reflections and stretchable centre; see-through fill keeps the rim.
        void GlassCapsule(PixelSurface s, float cx, float cy, float w, float h, Color edgeColor, float alpha = 1f, float fill = 1f)
            => GardenBars.Capsule(s, cx, cy, w, h, edgeColor, alpha, fill);
        void GardenButton(PixelSurface s, int id, float x, float y, float w, float h, string text) {
            HitBox(s, id, x, y, w, h);
            if (id >= 10 && id <= 12) {
                string slot = id == 10 ? "scene/button-shuffle" : id == 11 ? "scene/button-designate" : "scene/button-water";
                bool pressed = (id == 11 && selection == 1) || (id == 12 && selection == 2);
                if (Asset(s, slot, x, y + (pressed ? 2f : 0f), w, h, pressed ? new Color(.78f, .74f, .7f, 1f) : Color.white)) {
                    if (id == 12) GardenBars.Icon(s, "bomb", x + w * .18f, y + h * .48f + (pressed ? 2f : 0f), h * .65f);
                    return;
                }
            }
            // Primary (wide) and secondary wooden plank buttons; toggles show their switch.
            bool primary = w >= 300f || id == 1 || id == 9;
            string art = primary ? "ui/button-primary" : "ui/button-secondary";
            float edgeW = h * (primary ? 45f : 30f) / (primary ? 78f : 72f);
            if (!Asset9(s, art, x, y, w, h, primary ? 45f : 30f, 0f, edgeW, 0f, Color.white)) {
                GardenPanel(s, x, y, w, h);
                Text(s, text, x + w / 2, y + (h - 18) / 2, 18);
                return;
            }
            float size = Mathf.Min(20f, h * .42f), cx = x + w / 2;
            if (id >= 20 && id <= 22) {
                bool on = text.EndsWith("开");
                Asset(s, on ? "ui/toggle-on" : "ui/toggle-off", x + w - 78, y + (h - 32) / 2, 60, 32, Color.white);
                cx -= 30;
            }
            TextOutline(s, text, cx, y + (h - size) / 2 - 1, size, Cream, Outline, 1.8f);
        }
        // 2026-10-16 22:47: a main-action button with a big spaced title and a small line under it (e.g. 开 始 /
        // 第 12 关); the pair is centred on the button face.
        void GardenButtonTwoLine(PixelSurface s, int id, float x, float y, float w, float h, string title, string line) {
            GardenButton(s, id, x, y, w, h, "");
            float big = Mathf.Min(30f, h * .4f), small = Mathf.Min(14f, h * .19f), gap = h * .03f;
            float top = y + (h - (big + gap + small)) * .5f - 2f;
            TextOutline(s, title, x + w * .5f, top, big, Cream, Outline, 2.2f);
            TextOutline(s, line, x + w * .5f, top + big + gap, small, Hex(0xFFF3B0), Outline, 1.4f);
        }
        // 2026-10-17 (user, readability): a main button with one big title and no small line under it. Ad buttons put
        // a gold ▶ in front of the title instead of spelling out 「看广告」 in 10-14 px type.
        void GardenTitleButton(PixelSurface s, int id, float x, float y, float w, float h, string title, bool ad) {
            GardenButton(s, id, x, y, w, h, "");
            float size = Mathf.Min(30f, h * .44f), mark = ad ? size * .78f : 0f, gap = ad ? size * .32f : 0f;
            float width = TextWidth(title, size), left = x + (w - (mark + gap + width)) * .5f, top = y + (h - size) * .5f - 2f;
            if (ad && !GardenBars.Icon(s, "ad", left + mark * .5f, y + h * .5f - 1f, mark))
                TextOutline(s, "▶", left + mark * .5f, y + (h - mark) * .5f - 1f, mark, Hex(0xFFE27A), Outline, 2f);
            TextOutline(s, title, left + mark + gap + width * .5f, top, size, Cream, Outline, 2.2f);
        }
        // 2026-10-22: one centred text-only confirm; cancel lives on the bomb prop and its × badge.
        // Keep the original action id and bottom-band layout map, clear of the aimable board above the rail.
        void BombConfirmButton(PixelSurface s)
        {
            const float x = 158f, y = 742f, w = 224f, h = 64f;
            const int id = 9;
            HitBox(s, id, x, y, w, h);
            float k = 1f - PressPulse(id) * .05f, bw = w * k, bh = h * k;
            float bx = x + (w - bw) * .5f, by = GardenBars.ScreenY(s, y + (h - bh) * .5f);
            bool mapped = s.MapLayout; s.MapLayout = false;
            try
            {
                if (!Asset9(s, "plants/win/button-next", bx, by, bw, bh, 64f, 0f, bh * 64f / 126f, 0f, Color.white))
                    GardenPanel(s, bx, by, bw, bh);
                float size = 26f * k, cy = by + bh * .46f;
                GardenBarArt.TextOutline(s, "确认使用", x + w * .5f, cy - size * .55f, size,
                    Color.white, Hex(0xA55410), 1.8f * k);
            }
            finally { s.MapLayout = mapped; }
        }
        // 16:09 (user: 「泡泡停住 x 秒」 read like debug text). After a bubble bomb the wall stops pressing down for
        // 3 s (Battle.AdvanceHeld). A glossy blue capsule pops in over the top of the field: an icy countdown ring with
        // the seconds left, and 「泡泡被控住！」; it pops out when the hold ends.
        float heldStartAt = -1f, heldTotal = 1f;
        int heldShownSeconds = -1;
        string heldSecondsText = "";
        void DrawHeldBadge(PixelSurface s) {
            float left = Battle.AdvanceHeld;
            if (left <= 0f || selection != 0) { heldStartAt = -1f; return; }
            if (heldStartAt < 0f || left > heldTotal + .05f) { heldStartAt = ui; heldTotal = left; }
            int secs = Mathf.CeilToInt(left);
            if (secs != heldShownSeconds) { heldShownSeconds = secs; heldSecondsText = secs.ToString(); }
            float age = ui - heldStartAt, k = OutBack(Clamp01(age / .3f), 2.2f) * (left < .2f ? left / .2f : 1f);
            if (k < .05f) return;
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 240f);
            float w = 236f * k, h = 54f * k, x = 270f - w * .5f;
            s.MapLayout = false;
            try
            {
                s.Glow(270f, cy, w + 70f, h + 50f, new Color(.55f, .9f, 1f, .35f * k), 0f);
                GardenBars.Capsule(s, 270f, cy, w, h, GardenBars.Ice);   // 2026-10-20 (UI redesign 05): ice-edged glass
                // Countdown ring and text share the capsule's fixed size on tall screens.
                Vector2 ring = new Vector2(x + 34f * k, cy);
                float r = 18f * k, frac = Clamp01(left / Mathf.Max(heldTotal, .01f));
                GardenBarArt.Ring(s, ring.x, ring.y, 47f * k, 1f);
                s.Ring(ring, r, Hex(0x48EAFF), 4.2f * k, Mathf.Max(2, Mathf.CeilToInt(48 * frac)), -90f, 360f * frac);
                GardenBarArt.Text(s, heldSecondsText, ring.x, cy - 12f * k, 24f * k, true, GardenBars.Navy);
                float pulse = 1f + .05f * Mathf.Sin(ui * 8f);
                GardenBarArt.Text(s, "泡泡被控住！", x + 62f * k + 74f * k, cy - 11f * k * pulse, 22f * k * pulse, true, GardenBars.Navy);
            }
            finally { s.MapLayout = mapped; }
        }
        static readonly Color GardenGrass = Hex(0x7FA85A), GardenFloor = Hex(0xC9C29A);
        void DrawGarden(PixelSurface s) {
            s.MapLayout = true;
            // Keep the molded rim outside the actual bead area. Map the field first:
            // the HUD/field breakpoint otherwise compresses the top rim to < 1 px.
            s.MapLayout = false;
            float boardTop = PlushLayout.Y(114f) - 10f, boardBottom = PlushLayout.Y(562f) + 10f;
            Asset9(s, "scene/plush-board", 2f, boardTop, 536f, boardBottom - boardTop, 27f, 27f, 18f, 18f, Color.white);
            s.MapLayout = true;
            // The same original EnemyPoint and shownY interpolation keep sand/target alignment.
            int visibleBugs = 0;
            float offset = GardenFormationOffset();
            feedback.GardenOffsetDelta = offset - Battle.Offset;   // shots aim at where the beads are drawn
            // 2026-10-16: bead size follows the level's own column/row pitch so neighbouring beads touch and the
            // level picture reads as one image (the fixed 7.6 px left ~2.5 px gaps between columns). The bead art
            // fills 94% of its quad; columns overlap slightly, rows a little more (each row covers the one above).
            float pitchX = (Battle.Level.pitchX > 0f ? Battle.Level.pitchX : 10.5f) * ScreenLayout.EnemyScaleX;
            float pitchY = Battle.PitchY * ScreenLayout.EnemyScaleY;
            float beadW = pitchX * 1.05f / .94f, beadH = Mathf.Min(beadW, pitchY * 1.3f / .94f);
            feedback.GardenBeadSize = beadH;   // 2026-10-21: the hit pops start from the bead's own size
            float strikeShake = feedback.GardenStrikeShake;
            UpdateGardenSand(offset);   // 2026-10-21: beads fall and roll into place (PixelWingGame.GardenSand.cs)
            gardenBeadDraws.Clear();
            for (int i = 0; i < Battle.Alive.Length; i++) {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                GardenSandAt(i, out float gx, out float gy);
                Vector2 target = ScreenLayout.EnemyPoint(gx, gy + offset), at = target;
                if (Battle.State == Phase.Intro) {
                    if (FleetOpening.Progress(i, Battle.IntroTime, target) <= 0) continue;
                    at = FleetOpening.EnemyPosition(i, Battle.IntroTime, target);
                }
                if (at.y < 114 || at.y > 572 || at.x < 12 || at.x > 528) continue;
                at.y += strikeShake;   // 2026-10-21: the board jolts when a bubble bomb goes off
                visibleBugs++;
                float squash = GardenSandSquash(i);
                if (squash > 0f) at.y += beadH * squash * .5f;
                gardenBeadDraws.Add(new GardenBeadDraw { index = i, at = at, squash = squash });
            }
            // Flow and streamed replacements reorder the board independently of cell IDs.
            // Paint each body/highlight pair top-to-bottom so overlap never reverses at a stream boundary.
            gardenBeadDraws.Sort(GardenBeadOrder);
            for (int i = 0; i < gardenBeadDraws.Count; i++) {
                GardenBeadDraw bead = gardenBeadDraws[i];
                Vector2 at = bead.at;
                Color tint = GardenColor(Battle.ColorAt(bead.index));
                // 2026-10-16: beads inside the bomb circle light up (the original whitened them too, f54801).
                if (selection == 2 && InBombCircle(at)) tint = Color.Lerp(tint, Color.white, .3f + .2f * Mathf.Sin(ui * 7f));
                // 2026-10-21 20:45 (user): the teaching levels drew the original's shape-marked beads (dot / triangle /
                // square / cross per colour, 21 px, point-filtered), unlike every later level. Same bead everywhere now.
                Bug(s, tint, at, beadH * (1f - bead.squash), false, beadW * (1f + bead.squash));
            }
            GardenWebMetrics.Visible(visibleBugs);
            // The tray provides the quiet lower edge; danger still marks the defense line.
            DrawBubbleRail(s);
            if (feedback.Hurt > 0) s.Border(10, 92, 520, 466, A(Hex(0xE25555), feedback.Hurt), 3);
            float baySpacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            // UI spec v3 3 (2026-10-15): muted blue-gray tray behind pads and queues, as in the mockup's lower half.
            // A painted stage (art-v3 scene/stage.png) wins; otherwise the program-drawn nine-sliced tray.
            // Continuous lavender studio background underneath the fabric toys.
            // UI spec v3 3: flat bubble-ring pads centered on y=647 (no pad numbers); locked pads carry a padlock bubble.
            float plotW = Mathf.Min(70f, baySpacing - 5f), plotH = plotW * .5f;
            // 2026-10-21 17:24 (改动清单 3.2, user): every open pad looks the same, empty or not; no golden swirl.
            for (int bay = 0; bay < Battle.MaximumSlots; bay++) {
                float cx = Battle.FleetPositionX(bay);
                if (Battle.BayOpen(bay)) {
                    bool mapped = s.MapLayout; s.MapLayout = false;
                    float padY = PlushLayout.Y(647f) - GardenPadLift(), padH = plotW * .4f;
                    if (!Asset(s, "scene/plot", cx - plotW / 2, padY - padH / 2, plotW, padH, Color.white))
                        s.Rect(cx - plotW / 2, padY - padH / 2, plotW, padH, Paper);
                    s.MapLayout = mapped;
                } else {
                    // Keep the current purple base; only the transparent lock comes from the Git artwork.
                    bool mapped = s.MapLayout; s.MapLayout = false;
                    float padY = PlushLayout.Y(647f) - GardenPadLift(), padH = plotW * .4f;
                    Asset(s, "scene/plot", cx - plotW / 2, padY - padH / 2, plotW, padH, Color.white);
                    float lockSize = plotW * .6f;
                    Asset(s, "scene/plot-lock-bubble", cx - lockSize / 2, padY - plotW * .8f, lockSize, lockSize, Color.white);
                    // A small translucent corner badge sits with the lock's soft glass treatment.
                    float adX = cx + 13f, adY = padY - plotW * .76f + 2f;
                    if (!PauseVideoBadge(s, adX, adY, 18f, .78f))
                        TextOutline(s, "▶", adX, adY - 7f, 14f, A(Color.white, .78f), A(Hex(0x826CB2), .78f), 1f);
                    s.MapLayout = mapped;
                }
            }
            PadUnlockFx(s, plotW);   // 2026-10-21 14:18: the unlocked pad celebrates (replaces the 「已解锁出战位」 bar)
            // UI spec v3 4.3 (2026-10-15) gave empty open pads a turning golden swirl; removed 2026-10-21 17:24 (user:
            // all pads alike, nothing on the stage competing with the board).
            // Queue (UI spec v3 3): one glass tube per lane. 2026-10-21 (改动清单 3.3): briefly a drawn groove, then a
            // painted one (ui/queue-groove.png, kept but unused); 18:16 (user): the groove lost the dreamy glass feel, so
            // the glass tube is back at 55 % opacity: same shape and soft blue rim, far less glare next to the board.
            // No pad under the front member (17:24).
            for (int lane = 0; lane < Battle.LaneCount; lane++) {
                float w = Mathf.Min(108f, 500f / Battle.LaneCount - 8f), qx = Battle.QueuePositionX(lane);
                var reserve = Battle.Reserve[lane];
                // Three individual cards, with empty reserve cells kept visually quiet.
                bool mapped = s.MapLayout; s.MapLayout = false;
                float top = PlushLayout.Y(688f), bottom = PlushLayout.Y(868f), rowH = (bottom - top) / 3f;
                for (int row = 0; row < 3; row++) {
                    float cardY = top + row * rowH;
                    bool selectable = row < reserve.Count && (row == 0 || selection == 1);
                    if (selectable) {
                        s.GardenRoundedRect(qx - w / 2 - 1f, cardY - 1f, w + 2f, rowH - 4f, 12f, Hex(0xF4D58A), Hex(0xD8B660));
                        s.GardenRoundedRect(qx - w / 2 + 1f, cardY + 1f, w - 2f, rowH - 8f, 10f, Hex(0xC5A6E4), Hex(0xD8C1F0));
                    } else Asset9(s, "scene/queue-panel", qx - w / 2, cardY, w, rowH - 6f, 18f, 18f, 10f, 10f,
                        new Color(1f, 1f, 1f, row < reserve.Count ? .65f : .28f));
                }
                s.MapLayout = mapped;
                if (GardenHintLane(out int hint) && hint == lane && reserve.Count > 0) {
                    // Tutorial hint (option A): warm pulsing glow behind the suggested front creature.
                    float pulse = .7f + .3f * Mathf.Sin(ui * 4f);
                    Asset(s, "fx/glow", Battle.QueuePositionX(lane) - 54, QueueDisplayY(725f) - 58, 108, 108, new Color(1f, .82f, .32f, pulse));
                }
                // 2026-10-21 14:32 (user): the coloured corner marks at the top of each queue are gone (the front member
                // now carries a white outline when it can be tapped).
            }
        }
        // 2026-10-21 (改动清单 3.3): one queue lane as a shallow groove in the stage: a faint navy wash with round
        // ends, a soft shadow under its top edge and a thin light lip along the lower curve. Drawn in 1 px screen rows
        // (MapLayout off, ends mapped like Asset9) so the round ends keep their shape on tall screens.
        static void QueueGroove(PixelSurface s, float cx, float top, float bottom, float w)
        {
            bool mapped = s.MapLayout;
            if (mapped) { top = PlushLayout.Y(top); bottom = PlushLayout.Y(bottom); }
            s.MapLayout = false;
            try
            {
                float half = w * .5f, r = Mathf.Min(half * .9f, (bottom - top) * .5f);
                int cap = Mathf.CeilToInt(r);
                float RowHalf(int row) {
                    if (row >= cap) return half;
                    float dy = r - row - .5f;
                    return half - r + Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));
                }
                Color wash = new Color(.09f, .2f, .38f, .06f);
                for (int row = 0; row < cap; row++) {
                    float h = RowHalf(row);
                    s.Rect(cx - h, top + row, h * 2f, 1f, wash);
                    s.Rect(cx - h, bottom - row - 1f, h * 2f, 1f, wash);
                }
                s.Rect(cx - half, top + cap, w, Mathf.Max(0f, bottom - top - cap * 2f), wash);
                for (int row = 0; row < 14; row++) {
                    float h = RowHalf(row);
                    s.Rect(cx - h, top + row, h * 2f, 1f, new Color(.09f, .2f, .38f, .1f * (1f - row / 14f)));
                }
                Color lip = new Color(1f, 1f, 1f, .45f), side = new Color(1f, 1f, 1f, .22f);
                s.Ring(new Vector2(cx - half + r, bottom - r), r, lip, 1.2f, 14, 90f, 90f);
                s.Ring(new Vector2(cx + half - r, bottom - r), r, lip, 1.2f, 14, 0f, 90f);
                if (half - r > .5f) s.Segment(new Vector2(cx - half + r, bottom), new Vector2(cx + half - r, bottom), 1.2f, lip);
                s.Segment(new Vector2(cx - half, top + r), new Vector2(cx - half, bottom - r), 1f, side);
                s.Segment(new Vector2(cx + half, top + r), new Vector2(cx + half, bottom - r), 1f, side);
            }
            finally { s.MapLayout = mapped; }
        }
        void DrawGardenHUD(PixelSurface s) {
            BeginHitsCore(); s.MapLayout = true;
            GardenSkin.Clock = ui;   // idle motion uses UI time only
            GardenSkin.Celebrate = Battle.State == Phase.Won;
            feedback.GardenMouth = GardenMouth; feedback.GardenEnemyAt = GardenEnemyAt;
            TrackNewMembers();
            TrackMvpKills();   // 17:34: per-color clears for the victory screen MVP (BubbleWin)
            // 2026-10-16: the original combo counter (f53835) in bubble style, same visibility rule as the source HUD.
            // 2026-10-21 16:20: the victory page hides the combo counter and the 已出战 tags behind it.
            bool winShown = Battle.State == Phase.Won && feedback.EndAge > .48f;
            feedback.HideCombo = selection == 2 || settings || lesson != null || winShown;
            feedback.DrawBubbleCombo(s);
            if (Battle.Level.lesson <= 0 && !settings && selection == 0) {
                DrawRailWarning(s);   // 2026-10-18 22:21: warning tape instead of the button-like red capsule (BubbleRail)
            }
            // UI spec v3 3/4.2: creatures stand on the pads (feet y=640 when settled), number badge under the feet.
            float baySpacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            float bayCanvas = GardenBayCanvas(), bayNumber = baySpacing >= 74f ? 15f : 14f;
            float maxShakeAge = feedback.GardenMaxFusionAge;
            float padShake = maxShakeAge < .3f ? Mathf.Sin(ui * 70f) * 2.5f * (1f - maxShakeAge / .3f) : 0f;
            for (int i = 0; i < Battle.Fleet.Count; i++) {
                Fighter ship = Battle.Fleet[i];
                Vector2 at = feedback.Position(ship.Id, i);
                at.x += padShake; at.y += feedback.GardenStrikeShake * .5f;
                float squash = PadLanding(s, ship, i, ref at, bayCanvas);
                float since = Battle.Time - ship.LastFired;
                float fire = ship.Ammo > 0 ? (since < .1f ? 1f : Mathf.Clamp01(1f - (since - .1f) / .2f)) : 0f;
                float pulse = since >= 0f && since < .06f ? 1f - since / .06f : 0f;
                Creature(s, GardenColor(ship.Color), at.x, GardenPadFeet(ship.Id, at.y + 12f), bayCanvas, ship.Tier, ship.Ammo.ToString(),
                    0f, bayNumber, true, true, .035f, ship.Id * 1.7f, PadScale(ship), squash, 0f,
                    fire, pulse, ship.Ammo > 0 ? 1f - fire : 0f, false, 0f, PadInhale(ship, ship.Ammo > 0 && fire < .5f));
                padMouth[ship.Id] = LastMouth;
                float wait = PadWaitAlpha(ship);
                // 2026-10-21 (user): no ball icon; 10:21 a quiet regular-weight note instead of bold (it only informs).
                if (wait > .01f) GardenBars.QuietChip(s, "等待", at.x, GardenRaisedPadY(674f), 58f, 25f, 16f, wait);
            }
            float queueScale = Mathf.Min(108f, 500f / Battle.LaneCount - 8f) / 108f;
            // 2026-10-22 10:00 (user: queue members look small on phones): the queue members are drawn as big as a
            // tier-0 member on a pad (they were ~0.9 of it), so a member keeps its size when it is sent out; never wider
            // than the glass tube allows.
            // 10:30: at most 82 (the rows are 58 apart: a bigger member's number pill would cover the head below).
            float rowHeight = (PlushLayout.Y(868f) - PlushLayout.Y(688f)) / 3f;
            float queueCanvas = Mathf.Min(94f, Mathf.Min(rowHeight * 1.06f, (500f / Battle.LaneCount - 12f) / ArtAspect));
            GardenHintLane(out int hintLane);
            for (int lane = 0; lane < Battle.LaneCount; lane++) {
                var reserve = Battle.Reserve[lane];
                // Back rows first so the front plant's pot and number stay on top.
                for (int row = Mathf.Min(3, reserve.Count) - 1; row >= 0; row--) {
                    Fighter ship = reserve[row];
                    Vector2 at = feedback.ReservePosition(ship, lane, row);
                    if (Battle.State == Phase.Intro) {
                        float offset = FleetOpening.BayOffset(Battle.IntroTime, lane, row);
                        if (offset >= 159) continue;
                        at.y += offset * .35f;
                    }
                    if (row == 0 && lane == hintLane) at.y -= 3f * Mathf.Abs(Mathf.Sin(ui * 4f));
                    // 2026-10-21 23:33 (user): no sleeping back rows and no white outline any more; the members you can tap
                    // (the front row, or the first three rows while the 优先出战 prop is picking) stand in a soft gold light
                    // under their feet. The back rows idle normally. A member reaching the front still gives a small hop.
                    bool back = row > 0;
                    float wake = QueueWake(ship.Id, back), hop = wake < .3f ? Mathf.Sin(wake / .3f * Mathf.PI) : 0f;
                    float feetY = QueueDisplayY(at.y - 6f * hop) + 12;
                    Color queueTint = row != 0 && ship.Kind == 2 ? Hex(0x8C919C) : GardenColor(ship.Color);
                    // Queue portraits share a 100% canvas scale; cancel the old per-character size compensation.
                    Creature(s, queueTint, at.x, feetY, queueCanvas, ship.Tier,
                        row != 0 && ship.Kind == 2 ? "" : ship.Ammo.ToString(), 0f, 14f * queueScale, false, true, row == 0 ? .03f : .02f, lane * 2.3f + row * 1.1f,
                        1f / ArtScale(queueTint), -.05f * hop, 14f * queueScale, -1f, 0f, 0f, false, 0f);   // 2026-10-16 22:30: number pill tucked against the feet
                    // 2026-10-21 12:26 (user): hidden members show no number pill (was a "?" pill); the bubble art carries the "?".
                    // 2026-10-21: a hidden member reaching the front bursts out of its bubble.
                    float reveal = QueueReveal(ship, row);
                    if (reveal < .5f) {
                        float qh = queueCanvas, bodyY = QueueDisplayY(at.y) + 12f - qh * .35f;
                        GardenFx.Burst(s, "rocket-pop", new Vector2(at.x, bodyY), qh * 1.35f, reveal, .4f);
                        GardenFx.Burst(s, "impact-dust", new Vector2(at.x, bodyY + qh * .1f), qh * 1.1f, reveal - .05f, .4f);
                        for (int k = 0; k < 6; k++) {
                            float ang = k / 6f * Mathf.PI * 2f + .3f, d = qh * (.25f + .55f * reveal / .5f), a = 1f - reveal / .5f;
                            Color c = Color.HSVToRGB(k / 6f, .55f, 1f); c.a = a;
                            s.Glow(at.x + Mathf.Cos(ang) * d, bodyY + Mathf.Sin(ang) * d * .8f, 10f * queueScale, 10f * queueScale, c, 0f);
                        }
                    }
                    // 2026-10-15 20:52: queue numbers sit on the same navy pill as the pads (mockup); no tier coin in the queue.
                }
                // 23:33: the 优先出战 gold corner marks are replaced by the gold foot light on every selectable member.
                if (GardenMergeHintVisible(Battle, lane)) {
                    Vector2 at = feedback.ReservePosition(reserve[0], lane, 0);
                    MergeBadge(s, at.x + 27f * queueScale, QueueDisplayY(at.y) - 30f * queueScale, queueScale, lane);
                }
                // 11:16 (user: too small on phones): bigger. 14:39 (user: now too eye-catching): the quiet note of 等待
                // instead (soft translucent capsule, regular-weight blue-grey words), still a readable 17 (x lane scale).
                if (reserve.Count == 0 && !FinaleButtonVisible && !winShown) GardenBars.QuietChip(s, "已出战", Battle.QueuePositionX(lane), 776f,
                    Mathf.Min(86f * queueScale, Mathf.Min(108f, 500f / Battle.LaneCount - 8f) - 10f), 28f * queueScale, 17f * queueScale, .85f);
            }
            if (Battle.State != Phase.ReviveOffer) feedback.DrawGardenFeedback(s, GardenColor);
            // Third batch: bubble finale; the original routines still run muted for their cues, hit regions and clocks.
            DrawBubbleFinaleButton(s); DrawBubbleFinale(s);
            if (selection == 1) HintCapsule(s, "选择前三排任意队员", 512f, 330f, 48f, 21f, GardenBars.Sky, GardenBars.Navy, 1f, 1f, "pick", GardenBars.Deco.One, BarAge(1));   // 11:12: glass capsule
            if (selection == 2) {
                DrawBombAim(s);
                // 11:16 (user): see-through glass (fill 45 %) so the picture under it stays visible while aiming.
                HintCapsule(s, "拖动选择泡泡，双击确认", 140f, 340f, 44f, 19f, GardenBars.Sky, GardenBars.Navy, 1f, .45f, "crosshair", GardenBars.Deco.None, BarAge(2));
                // 2026-10-18 22:21 (user): the confirm button sat right over the rail, i.e. on the bottom rows players
                // bomb most, hiding the reticle and catching drags. While aiming, the queue cannot be used anyway, so
                // the queue area is dimmed and holds 确认使用 (id 9); the bomb prop (id 12) handles cancellation.
                // Soft top/bottom fades keep the inactive queue subdued without a hard rectangular panel.
                for (int row = 0; row < 224; row += 2)
                {
                    float fade = Mathf.SmoothStep(0f, 1f, Clamp01((row + 1f) / 40f))
                        * Mathf.SmoothStep(0f, 1f, Clamp01((223f - row) / 40f));
                    s.Rect(0f, 652f + row, 540f, 2f, new Color(.05f, .12f, .28f, .38f * fade));
                }
                BombConfirmButton(s);
            }
            if (Battle.State == Phase.Ready && tutorialStep < 0) {
                DrawGardenReadyHint(s);
            }
            if (Battle.Stalled && selection == 0 && !settings && Battle.State == Phase.Playing) {
                DrawGardenStallHint(s);
            }
            if (Battle.NoticeTime > 0 && selection == 0 && !settings && Battle.Notice != "已解锁战机位") {
                // 2026-10-20 (UI redesign 13/14/17): the model's notices (重排, 解锁, 出战位已满) as short bars.
                string notice = GardenWords(Battle.Notice);
                MessageStyle(notice, false, out Color nEdge, out Color nInk, out string nIcon);
                // 2026-10-21 (user): at y 872 the bar sat under the prop buttons; now just above the pad members' heads
                // (board bottom), clear of the ready hint at 592 and of the buttons.
                HintCapsule(s, notice, 536f, 160f, 34f, 16f, nEdge, nInk, 1f, 1f, nIcon, GardenBars.Deco.None, BarAge(4));
            }
            DrawHeldBadge(s);
            if (lesson != null && Battle.State != Phase.Intro && Battle.State != Phase.Won && Battle.State != Phase.Lost && !settings && selection == 0 && !finaleGuide) DrawGardenTutorial(s);
            if (Battle.State == Phase.Intro || Battle.State == Phase.Ready || Battle.State == Phase.Playing) DrawGardenOpening(s);
            EndHitsCore();
        }
        // Higher layer than prefab dialogs: revive/confirmation/transition retain
        // their existing paint and input priority. Fixed UI is in nested prefabs.
        void DrawGardenOverlay(PixelSurface s) {
            s.MapLayout = true;
            DrawWinScreen(s);   // all levels, teaching ones included (the small lesson card is no longer used)
            DrawLoseScreen(s);  // Failure ribbon and plush illustration above the prefab shade/buttons.
            if (Battle.State == Phase.ReviveOffer) DrawGardenRevive(s);
            else if (!settings && finaleGuide) DrawBubbleFinaleGuide(s);
            if (streakPrompt) DrawGardenStreakPrompt(s);   // 2026-10-21 20:51: card dialog (PixelWingGame.GardenDialogs.cs)
            if (energyOpen) DrawGardenEnergy(s);
            DrawGardenToast(s);   // 2026-10-19 11:12: glass capsule (was the wide jelly panel over the combo counter)
            DrawGardenTransition(s);
        }
        void DrawGardenTransition(PixelSurface s) {
            if (!transitionActive) return;
            HitBlock(s);
            bool mapped = s.MapLayout; s.MapLayout = false;
            float hold = transitionKind == TransitionKind.Home ? 0 : 1.05f;
            float alpha = transitionAge < .42f ? Clamp01(transitionAge / .42f)
                : transitionAge < hold + .42f ? 1 : 1 - Clamp01((transitionAge - hold - .42f) / .5f);
            s.Rect(ScreenLayout.Left, 0, ScreenLayout.Width, ScreenLayout.Height, A(Soil, alpha));
            Text(s, transitionKind == TransitionKind.Home ? "返回主页" : "前往第 " + (transitionLevel + 1) + " 关", 270, ScreenLayout.Height / 2 - 12, 25, true, A(Ink, alpha));
            s.MapLayout = mapped;
        }
        // 2026-10-20 (UI review board 14): "战机位" first, or "已解锁战机位" became "已解锁队员位".
        static string GardenWords(string text) => text.Replace("战机位", "出战位").Replace("战机", "队员").Replace("机位", "出战位").Replace("敌阵", "泡泡阵").Replace("敌人", "泡泡").Replace("出击", "出战").Replace("火力", "攻击").Replace("超级炸弹", "泡泡炸弹");
        // UI spec v3 3: queue rows display at 721 / 779 / 837 instead of the logical 725 / 775 / 825.
        // Linear in y, so arrival and shift animations keep their proportions; hit areas stay on the logical rows.
        bool GardenQueueCell(Vector2 p, out int lane, out int row)
        {
            lane = LaneAt(p.x); row = -1;
            float top = PlushLayout.Y(688f), bottom = PlushLayout.Y(868f), y = PlushLayout.Y(p.y);
            if (p.x < 20f || p.x >= 520f || y < top || y >= bottom) return false;
            row = Mathf.Clamp(Mathf.FloorToInt((y - top) * 3f / (bottom - top)), 0, 2);
            return true;
        }
        static float QueueDisplayY(float y)
        {
            float top = PlushLayout.Y(688f), rowHeight = (PlushLayout.Y(868f) - top) / 3f;
            return PlushLayout.InverseY(top + ((y - 725f) / 50f + 1f) * rowHeight - 12f) - 12f;
        }
        // 2026-10-17: light titles: large white lettering with a navy outline, a small bubble pair either side and a
        // short fading gold underline; `halo` adds a soft dark glow for titles over busy art.
        void GardenTitle(PixelSurface s, string title, float cx, float y, float size, bool halo) {
            float w = TextWidth(title, size);
            if (halo) s.Glow(cx, y + size * .55f, w + 120f, size + 30f, new Color(.02f, .06f, .16f, .45f), 0f);
            TextOutline(s, title, cx, y, size, Cream, Outline, size * .09f);
            for (int side = -1; side <= 1; side += 2) {
                float bx = cx + side * (w * .5f + 18f), by = y + size * .55f;
                GardenSkin.Shot(s, Hex(0x8FD8FF), new Vector2(bx, by), size * .38f, 1f);
                GardenSkin.Shot(s, Hex(0xFFD6EC), new Vector2(bx + side * size * .45f, by - size * .3f), size * .2f, .9f);
                for (int seg = 0; seg < 6; seg++) {
                    float x0 = cx + side * seg * 15f;
                    s.Rect(side > 0 ? x0 : x0 - 15f, y + size + 5f, 15f, 2f, new Color(1f, .84f, .35f, (1f - seg / 6f) * .85f));
                }
            }
        }
        void DrawGardenTutorial(PixelSurface s) {
            int topic = narration == null ? 0 : (int)narration.Topic;
            string face = lesson.Complete || topic == 2 ? "ui/guide-happy" : topic == 3 || topic == 6 ? "ui/guide-worried" : "ui/guide-explain";
            // Keep both teaching rows below the header and above the small training formation.
            DrawGardenDialogue(s, face, Narration(topic), 194f, compact: true);
            if (!lesson.Complete) {
                // 2026-10-20 (UI redesign 27); 14:24 (user): no target icon on every step's goal, just the words.
                HintCapsule(s, Goal(lesson.Step), 140f, 320f, 26f, 16f, GardenBars.Sky, GardenBars.Navy, 1f, 1f, null, GardenBars.Deco.One, BarAge(3));
                // 2026-10-21 (user): the practice-step 「提示」 button is gone. It only skipped the wait: the same glow and
                // arrow on the suggested member still come by themselves (FlightLesson.HintLevel, from 6 s in practice).
                if (lesson.SuggestedLane >= 0 && lesson.HintLevel > 0) {
                    float x = Battle.QueuePositionX(lesson.SuggestedLane);
                    if (lesson.HintLevel >= 3) {
                        // Bouncing gold arrow above the creature (the glow is drawn in DrawGarden).
                        float bounce = Mathf.Abs(Mathf.Sin(ui * 5f)) * 5f;
                        Asset(s, "fx/arrow", x - 14, 662 - bounce, 28, 28, Color.white);
                    }
                }
            }
            // 2026-10-20 (UI redesign 29): a small gold tag with the shield.
            float tagW = GardenBars.Width("课堂保护中", 14f, 28f, "shield");
            bool tagMapped = s.MapLayout;
            float tagY = tagMapped ? PlushLayout.Y(538f) : 538f;
            s.MapLayout = false;
            GardenBars.Bar(s, Mathf.Min(466f, 528f - tagW * .5f), tagY, tagW, 28f, GardenBars.Gold, "shield", "课堂保护中", 14f, GardenBars.Brown);
            s.MapLayout = tagMapped;
        }
        // Same gating as DrawGardenTutorial: the hint only shows while the tutorial overlay does.
        bool GardenHintLane(out int lane) {
            lane = -1;
            if (lesson == null || lesson.Complete || lesson.SuggestedLane < 0 || lesson.HintLevel <= 0) return false;
            if (Battle.State == Phase.Intro || Battle.State == Phase.Won || Battle.State == Phase.Lost || settings || selection != 0 || finaleGuide) return false;
            lane = lesson.SuggestedLane;
            return true;
        }
        void GardenCorners(PixelSurface s, float x, float y, float w, float h, Color c) {
            const float k = 15f;
            if (!Asset(s, "scene/queue-corner", x, y, k, k, c)) { s.Corners(x, y, w, h, c, 9, 3); return; }
            Asset(s, "scene/queue-corner", x + w - k, y, k, k, c, false, true, true);
            Asset(s, "scene/queue-corner", x, y + h - k, k, k, c, false, true, false, true);
            Asset(s, "scene/queue-corner", x + w - k, y + h - k, k, k, c, false, true, true, true);
        }
    }
}
#endif
