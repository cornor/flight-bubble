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
        void GardenButton(PixelSurface s, int id, float x, float y, float w, float h, string text) {
            HitBox(s, id, x, y, w, h);
            if (id >= 10 && id <= 12) {
                string slot = id == 10 ? "scene/button-shuffle" : id == 11 ? "scene/button-designate" : "scene/button-water";
                bool pressed = (id == 11 && selection == 1) || (id == 12 && selection == 2);
                if (Asset(s, slot, x, y + (pressed ? 2f : 0f), w, h, pressed ? new Color(.78f, .74f, .7f, 1f) : Color.white)) return;
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
            if (ad) TextOutline(s, "▶", left + mark * .5f, y + (h - mark) * .5f - 1f, mark, Hex(0xFFE27A), Outline, 2f);
            TextOutline(s, title, left + mark + gap + width * .5f, top, size, Cream, Outline, 2.2f);
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
            const float cy = 240f;
            float w = 236f * k, h = 54f * k, x = 270f - w * .5f;
            s.Glow(270f, cy, w + 70f, h + 50f, new Color(.55f, .9f, 1f, .35f * k), 0f);
            GardenBanner(s, "ui/banner-hint", x, cy - h * .5f, w, h);
            // Countdown ring: a faint full circle and a bright arc that empties clockwise.
            Vector2 ring = new Vector2(x + 34f * k, cy);
            float r = 16f * k, frac = Clamp01(left / Mathf.Max(heldTotal, .01f));
            s.Ring(ring, r, new Color(1f, 1f, 1f, .25f), 4f * k, 24, -90f, 360f);
            s.Ring(ring, r, Hex(0x9FF0FF), 4.2f * k, Mathf.Max(2, Mathf.CeilToInt(24 * frac)), -90f, 360f * frac);
            TextOutline(s, heldSecondsText, ring.x, cy - 10f * k, 20f * k, Gold, Outline, 1.8f);
            float pulse = 1f + .05f * Mathf.Sin(ui * 8f);
            TextOutline(s, "泡泡被控住！", x + 62f * k + 74f * k, cy - 11f * k * pulse, 22f * k * pulse, Cream, Hex(0x1C4C8C), 2.2f);
        }
        static readonly Color GardenGrass = Hex(0x7FA85A), GardenFloor = Hex(0xC9C29A);
        void DrawGarden(PixelSurface s) {
            s.MapLayout = true;
            // UI spec v3 2 (user decision 2026-10-15): no mosaic panel; beads sit on the background sky as in the mockup.
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
            for (int i = 0; i < Battle.Alive.Length; i++) {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                float gy = shownY != null && i < shownY.Length ? shownY[i] : Battle.GridY(i);
                Vector2 target = ScreenLayout.EnemyPoint(Battle.EnemyX(i), gy + offset), at = target;
                if (Battle.State == Phase.Intro) {
                    if (FleetOpening.Progress(i, Battle.IntroTime, target) <= 0) continue;
                    at = FleetOpening.EnemyPosition(i, Battle.IntroTime, target);
                }
                if (at.y < 114 || at.y > 572 || at.x < 12 || at.x > 528) continue;
                visibleBugs++;
                Color tint = GardenColor(Battle.ColorAt(i));
                // 2026-10-16: beads inside the bomb circle light up (the original whitened them too, f54801).
                if (selection == 2 && InBombCircle(at)) tint = Color.Lerp(tint, Color.white, .3f + .2f * Mathf.Sin(ui * 7f));
                Bug(s, tint, at, beadH, Battle.Level.lesson > 0, beadW);
            }
            GardenWebMetrics.Visible(visibleBugs);
            // UI spec v3 3: foam-cloud ceiling over the mosaic's top edge; new rows appear from under it.
            Asset(s, "scene/ceiling", 0, 76, 540, 40, Color.white);
            // Foam rail centered on the logical defense line y=562; it flushes red when hit.
            DrawBubbleRail(s);   // 2026-10-18: the rail's bubble beads flow; red, faster and pulsing while the beads close in
            if (feedback.Hurt > 0) s.Border(10, 92, 520, 466, A(Hex(0xE25555), feedback.Hurt), 3);
            float baySpacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            // UI spec v3 3 (2026-10-15): muted blue-gray tray behind pads and queues, as in the mockup's lower half.
            // A painted stage (art-v3 scene/stage.png) wins; otherwise the program-drawn nine-sliced tray.
            if (!AssetBandsFrom(s, "scene/stage", 574f)) Asset9(s, "scene/tray", 6, 576, 528, 384, 28f, 20f, Color.white);
            // UI spec v3 3: flat bubble-ring pads centered on y=647 (no pad numbers); locked pads carry a padlock bubble.
            float plotW = Mathf.Min(70f, baySpacing - 5f), plotH = plotW * .5f;
            for (int bay = 0; bay < Battle.MaximumSlots; bay++) {
                float cx = Battle.FleetPositionX(bay);
                if (Battle.BayOpen(bay)) {
                    if (!Asset(s, "scene/plot", cx - plotW / 2, 647 - plotH / 2, plotW, plotH, Color.white))
                        s.Rect(cx - plotW / 2, 647 - plotH / 2, plotW, plotH, Paper);
                } else if (!Asset(s, "scene/plot-locked", cx - plotW / 2, 647 + plotH / 2 - plotW, plotW, plotW, Color.white))
                    s.Rect(cx - plotW / 2, 647 - plotH / 2, plotW, plotH, Wood);
            }
            // UI spec v3 4.3 (user decision 2026-10-15): empty open pads show a slowly turning golden swirl.
            bool[] used = new bool[Battle.MaximumSlots];
            for (int i = 0; i < Battle.Fleet.Count; i++) { int v = Battle.VisualSlot(i); if (v >= 0 && v < used.Length) used[v] = true; }
            float swirlAngle = -(ui % 4f) / 4f * 2f * Mathf.PI, swirlAlpha = .85f + .15f * Mathf.Sin(ui * Mathf.PI);
            for (int bay = 0; bay < Battle.MaximumSlots; bay++)
                if (Battle.BayOpen(bay) && !used[bay])
                {
                    float bx = Battle.FleetPositionX(bay);
                    // 16:26 (user, with a reference crop): not a rising light; the turning gold swirl itself should
                    // stand fuller (it was squashed to 45 % height, a thin line) and sit a little above the pad,
                    // with a warm glow behind it.
                    float breathe = .85f + .15f * Mathf.Sin(ui * 2.4f + bay);
                    // 16:47 (user: looked crooked and too high): flattened to the pad's own perspective (pad height =
                    // half its width) and sitting on the pad surface; the art is re-centred in the atlas so it no longer
                    // wobbles while turning.
                    s.Glow(bx, 642f, plotW * 1.1f, plotW * .62f, new Color(1f, .84f, .38f, .38f * breathe), 0f);
                    for (int pass = 0; pass < 2; pass++) Swirl(s, bx, 645, plotW * .95f, swirlAngle, swirlAlpha, .5f);
                }
            // Queue: one glass tube per lane (UI spec v3 3); front-row corner marks take the front creature's color.
            for (int lane = 0; lane < Battle.LaneCount; lane++) {
                float w = Mathf.Min(108f, 500f / Battle.LaneCount - 8f), x = Battle.QueuePositionX(lane) - w / 2;
                var reserve = Battle.Reserve[lane];
                if (!Asset9(s, "scene/queue-panel", x, 688, w, 180, 24f, 16f, Color.white)) {
                    s.Rect(x, 688, w, 180, new Color(.85f, .93f, 1f, .35f));
                    s.Border(x, 688, w, 180, Wood, 1);
                }
                if (GardenHintLane(out int hint) && hint == lane && reserve.Count > 0) {
                    // Tutorial hint (option A): warm pulsing glow behind the suggested front creature.
                    float pulse = .7f + .3f * Mathf.Sin(ui * 4f);
                    Asset(s, "fx/glow", Battle.QueuePositionX(lane) - 54, QueueDisplayY(725f) - 58, 108, 108, new Color(1f, .82f, .32f, pulse));
                }
                if (reserve.Count > 0) {
                    Color mark = GardenColor(reserve[0].Color);
                    if (!Asset(s, "scene/queue-corner", x + 4, 692, 14, 14, mark)) s.Corners(x + 3, 691, w - 6, 56, mark, 9, 3);
                    else Asset(s, "scene/queue-corner", x + w - 18, 692, 14, 14, mark, false, true, true);
                }
            }
        }
        void DrawGardenHUD(PixelSurface s) {
            BeginHitsCore(); s.MapLayout = true;
            GardenSkin.Clock = ui;   // idle motion uses UI time only
            GardenSkin.Celebrate = Battle.State == Phase.Won;
            feedback.GardenMouth = GardenMouth; feedback.GardenEnemyAt = GardenEnemyAt;
            TrackNewMembers();
            TrackMvpKills();   // 17:34: per-color clears for the victory screen MVP (BubbleWin)
            // 2026-10-16: the original combo counter (f53835) in bubble style, same visibility rule as the source HUD.
            feedback.HideCombo = selection == 2 || settings || lesson != null;
            feedback.DrawBubbleCombo(s);
            if (Battle.Level.lesson <= 0 && !settings && selection == 0) {
                DrawRailWarning(s);   // 2026-10-18 22:21: warning tape instead of the button-like red capsule (BubbleRail)
            }
            for (int bay = 0; bay < Battle.MaximumSlots; bay++) {
                if (Battle.BayOpen(bay)) continue;
                // Small "watch an ad" badge instead of the four-character label.
                float x = Battle.FleetPositionX(bay) + 21, y = 603;
                s.Rect(x - 10, y - 6, 20, 13, Hex(0x5A3A1E)); s.Rect(x - 9, y - 5, 18, 11, Hex(0xF6ECD2));
                s.Segment(new Vector2(x - 3, y - 3), new Vector2(x - 3, y + 4), 2f, Hex(0x5A3A1E));
                s.Segment(new Vector2(x - 3, y - 3), new Vector2(x + 4, y + .5f), 2f, Hex(0x5A3A1E));
                s.Segment(new Vector2(x - 3, y + 4), new Vector2(x + 4, y + .5f), 2f, Hex(0x5A3A1E));
            }
            // UI spec v3 3/4.2: creatures stand on the pads (feet y=640 when settled), number badge under the feet.
            float baySpacing = Battle.MaximumSlots >= 2 ? 456f / (Battle.MaximumSlots - 1) : 120f;
            float bayCanvas = GardenBayCanvas(), bayNumber = baySpacing >= 74f ? 15f : 14f;
            float maxShakeAge = feedback.GardenMaxFusionAge;
            float padShake = maxShakeAge < .3f ? Mathf.Sin(ui * 70f) * 2.5f * (1f - maxShakeAge / .3f) : 0f;
            for (int i = 0; i < Battle.Fleet.Count; i++) {
                Fighter ship = Battle.Fleet[i];
                Vector2 at = feedback.Position(ship.Id, i);
                at.x += padShake;
                float squash = PadLanding(s, ship, i, ref at, bayCanvas);
                Creature(s, GardenColor(ship.Color), at.x, at.y + 12, bayCanvas, ship.Tier, ship.Ammo.ToString(),
                    Mathf.Clamp01(1 - (Battle.Time - ship.LastFired) / .18f), bayNumber, true, true, .035f, ship.Id * 1.7f, PadScale(ship), squash);
                if (ship.Arrival <= 0 && !Battle.Exposed(ship.Color)) TextOutline(s, "等待", at.x, 664, 15f, Cream, Outline, 1.5f);
            }
            float queueScale = Mathf.Min(108f, 500f / Battle.LaneCount - 8f) / 108f;
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
                    Creature(s, row != 0 && ship.Kind == 2 ? Hex(0x8C919C) : GardenColor(ship.Color), at.x, QueueDisplayY(at.y) + 12, (row == 0 ? 82f : 76f) * queueScale, ship.Tier,
                        row != 0 && ship.Kind == 2 ? "?" : ship.Ammo.ToString(), 0f, 14f * queueScale, false, true, row == 0 ? .03f : .02f, lane * 2.3f + row * 1.1f,
                        0f, 0f, 9f * queueScale);   // 2026-10-16 22:30: number pill tucked against the feet (was 2 px below them)
                    // 2026-10-15 20:52: queue numbers sit on the same navy pill as the pads (mockup); no tier coin in the queue.
                }
                // Priority planting (option B): pulsing gold corner marks on every selectable plant, drawn after the plants.
                if (selection == 1)
                    for (int row = 0; row < Mathf.Min(3, reserve.Count); row++) {
                        Vector2 at = feedback.ReservePosition(reserve[row], lane, row);
                        GardenCorners(s, at.x - 32 * queueScale, QueueDisplayY(at.y) - 28 * queueScale, 64 * queueScale, 56 * queueScale, new Color(1f, .86f, .3f, .75f + .25f * Mathf.Sin(ui * 5f)));
                    }
                if (GardenMergeHintVisible(Battle, lane)) {
                    Vector2 at = feedback.ReservePosition(reserve[0], lane, 0);
                    MergeBadge(s, at.x + 27f * queueScale, QueueDisplayY(at.y) - 30f * queueScale, queueScale, lane);
                }
                if (reserve.Count == 0 && !FinaleButtonVisible) TextOutline(s, "已出战", Battle.QueuePositionX(lane), 766, 19f, Cream, Outline, 1.7f);
            }
            if (Battle.State != Phase.ReviveOffer) feedback.DrawGardenFeedback(s, GardenColor);
            // Third batch: bubble finale; the original routines still run muted for their cues, hit regions and clocks.
            DrawBubbleFinaleButton(s); DrawBubbleFinale(s);
            if (selection == 1) TextOutline(s, "选择前三排任意队员", 270, 527, 19f, Cream, Outline, 1.8f);
            if (selection == 2) {
                DrawBombAim(s);
                TextOutline(s, "拖动选择泡泡，双击确认", 270, 128, 18f, Cream, Outline, 1.8f);
                // 2026-10-18 22:21 (user): the confirm button sat right over the rail, i.e. on the bottom rows players
                // bomb most, hiding the reticle and catching drags. While aiming, the queue cannot be used anyway, so
                // the queue area is dimmed and holds 确认使用 (id 9) and 取消 (id 12, the bomb prop's own cancel).
                // 22:40 (user): ears of the front row poked out above y 690; the dim now starts at 668 with a 12 px fade.
                for (int k = 0; k < 4; k++) s.Rect(6, 656 + k * 3, 528, 3, new Color(.05f, .12f, .28f, .62f * (k + 1) / 5f));
                s.Rect(6, 668, 528, 206, new Color(.05f, .12f, .28f, .62f));
                GardenTitleButton(s, 9, 120, 712, 300, 64, "确 认 使 用", false);
                HitBox(s, 12, 180, 792, 180, 48);
                if (!Asset9(s, "ui/button-secondary", 180, 792, 180, 48, 30f, 0f, 48f * 30f / 72f, 0f, Color.white)) GardenPanel(s, 180, 792, 180, 48);
                TextOutline(s, "取  消", 270, 792 + (48 - 20) / 2 - 1, 20f, Cream, Outline, 1.8f);
            }
            if (Battle.State == Phase.Ready && tutorialStep < 0) {
                GardenPanel(s, 105, 500, 330, 44); Text(s, "点下方队员，开始出战", 270, 513, 18);
            }
            if (Battle.Stalled && selection == 0 && !settings && Battle.State == Phase.Playing) {
                GardenPanel(s, 40, 280, 460, 64); Text(s, "出战位满了，同色泡泡被挡住啦", 270, 290, 16);
                Text(s, "试试泡泡炸弹，清开最下排", 270, 317, 16);
            }
            if (Battle.NoticeTime > 0 && selection == 0 && !settings) TextOutline(s, GardenWords(Battle.Notice), 270, 862, 16f, Cream, Outline, 1.6f);
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
            DrawLoseScreen(s);  // 2026-10-17: fail page content over the prefab panel (BubbleLose)
            if (Battle.State == Phase.ReviveOffer) DrawGardenRevive(s);
            else if (!settings && finaleGuide) DrawBubbleFinaleGuide(s);
            if (streakPrompt) {
                GardenModal(s, "连胜中断", 280, 400);
                Text(s, streakWhat + "将中断连胜", 270, 420, 18);
                GardenTitleButton(s, 511, 80, 532, 380, 64, streakRevive ? "复 活 继 续" : "继 续 挑 战", streakRevive);
                GardenButton(s, 510, 150, 608, 240, 44, "确认中断连胜");
            }
            if (energyOpen) {
                GardenModal(s, "体力", 280, 380); Text(s, Economy.Energy + " / 5", 270, 370, 25);
                GardenButton(s, 503, 420, 292, 48, 40, "X");
                GardenTitleButton(s, 501, 84, 506, 372, 62, "补充体力 +5", true);
                GardenButton(s, 502, 84, 576, 372, 50, "400 金币补充体力 +5");
            }
            if (ui - toastAt <= 2.2f && toast.Length > 0) { GardenPanel(s, 40, 90, 460, 36); Text(s, GardenWords(toast), 270, 101, 15); }
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
        static string GardenWords(string text) => text.Replace("战机", "队员").Replace("机位", "出战位").Replace("敌阵", "泡泡阵").Replace("敌人", "泡泡").Replace("出击", "出战").Replace("火力", "攻击").Replace("超级炸弹", "泡泡炸弹");
        // UI spec v3 3: queue rows display at 721 / 779 / 837 instead of the logical 725 / 775 / 825.
        // Linear in y, so arrival and shift animations keep their proportions; hit areas stay on the logical rows.
        static float QueueDisplayY(float y) => 721f + (y - 725f) * 1.16f;
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
        void GardenModal(PixelSurface s, string title, float y, float height) {
            HitBlock(s); s.Rect(0, 0, 540, 960, new Color(0, 0, 0, .65f));
            GardenPanel(s, 50, y, 440, height);
            GardenTitle(s, title, 270, y + 16, 28f, false);   // 2026-10-17: no ribbon plate (it read as a heavy button)
        }
        void DrawGardenTutorial(PixelSurface s) {
            // UI spec v3 4.6: guide "Xiao Lan" (the blue squad member) beside a speech bubble; content and conditions unchanged.
            bool bubble = Asset9(s, "ui/speech-bubble", 96, 134, 414, 92, 30f, 30f, 18f, 18f, Color.white);
            if (!bubble) GardenPanel(s, 32, 136, 476, 88);
            int topic = narration == null ? 0 : (int)narration.Topic;
            string face = lesson.Complete || topic == 2 ? "ui/guide-happy" : topic == 3 || topic == 6 ? "ui/guide-worried" : "ui/guide-explain";
            if (!Asset(s, face, 14, 130, 96, 96, Color.white)) {
                for (int i = 0; i < 5; i++) s.Segment(new Vector2(68 + i * 10, 178), new Vector2(63 + i * 10, 166), 5, Wood);
                s.Rect(64, 179, 52, 30, Wood); s.Rect(108, 185, 15, 18, Paper); s.Rect(114, 188, 3, 3, Ink);
            }
            TextOutline(s, "小蓝", 150, 126, 15f, Cream, Outline, 1.5f);
            var lines = Narration(topic);
            Text(s, lines[0], 124, 160, 16, false); Text(s, lines[1], 124, 189, 16, false);
            if (!lesson.Complete) {
                TextOutline(s, Goal(lesson.Step), 270, 98, 18f, Cream, Outline, 1.8f);
                if (lesson.Practice) GardenButton(s, 50, 410, 225, 98, 28, "提示");
                if (lesson.SuggestedLane >= 0 && lesson.HintLevel > 0) {
                    float x = Battle.QueuePositionX(lesson.SuggestedLane);
                    if (lesson.HintLevel >= 3) {
                        // Bouncing gold arrow above the creature (the glow is drawn in DrawGarden).
                        float bounce = Mathf.Abs(Mathf.Sin(ui * 5f)) * 5f;
                        Asset(s, "fx/arrow", x - 14, 662 - bounce, 28, 28, Color.white);
                    }
                }
            }
            TextOutline(s, "课堂保护中", 466, 530, 15f, Cream, Outline, 1.5f);
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
