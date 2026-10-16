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
        static readonly Color GardenGrass = Hex(0x7FA85A), GardenFloor = Hex(0xC9C29A);
        void DrawGarden(PixelSurface s) {
            s.MapLayout = true;
            // UI spec v3 2 (user decision 2026-10-15): no mosaic panel; beads sit on the background sky as in the mockup.
            // The same original EnemyPoint and shownY interpolation keep sand/target alignment.
            int visibleBugs = 0;
            for (int i = 0; i < Battle.Alive.Length; i++) {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                float gy = shownY != null && i < shownY.Length ? shownY[i] : Battle.GridY(i);
                Vector2 target = ScreenLayout.EnemyPoint(Battle.EnemyX(i), gy + Battle.Offset), at = target;
                if (Battle.State == Phase.Intro) {
                    if (FleetOpening.Progress(i, Battle.IntroTime, target) <= 0) continue;
                    at = FleetOpening.EnemyPosition(i, Battle.IntroTime, target);
                }
                if (at.y < 114 || at.y > 572 || at.x < 12 || at.x > 528) continue;
                visibleBugs++;
                // UI spec v3 4.1 (2026-10-15): beads slightly larger than the 6.55 row pitch, so each row overlaps the one
                // above (drawn in row order) and the picture reads solid like the mockup; display only.
                float size = Battle.Level.lesson > 0 ? 15.4f : 7.6f;
                Color tint = GardenColor(Battle.ColorAt(i));
                Bug(s, tint, at, size, Battle.Level.lesson > 0);
            }
            GardenWebMetrics.Visible(visibleBugs);
            // UI spec v3 3: foam-cloud ceiling over the mosaic's top edge; new rows appear from under it.
            Asset(s, "scene/ceiling", 0, 76, 540, 40, Color.white);
            // Foam rail centered on the logical defense line y=562; it flushes red when hit.
            Color fenceTint = Color.Lerp(Color.white, new Color(1f, .45f, .45f, 1f), Clamp01(feedback.Hurt));
            if (!Asset(s, "scene/fence", 0, 551, 540, 22, fenceTint)) s.Rect(10, 560, 520, 4, Wood);
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
                    // The delivered swirl is faint at pad size (mean alpha ~7%): draw it twice to read like the mockup.
                    for (int pass = 0; pass < 2; pass++) Swirl(s, Battle.FleetPositionX(bay), 645, plotW * .95f, swirlAngle, swirlAlpha);
            // Queue: one glass tube per lane (UI spec v3 3); front-row corner marks take the front creature's color.
            for (int lane = 0; lane < Battle.LaneCount; lane++) {
                float w = 108f, x = Battle.QueuePositionX(lane) - w / 2;
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
            if (Battle.Level.lesson <= 0 && !settings && selection == 0) {
                if (Battle.Combo >= 10) TextOutline(s, "连击 " + Battle.Combo, 64, 96, 14f, Cream, Outline, 1.4f);
                if (Battle.Danger > .3f && Battle.State == Phase.Playing) {
                    GardenBanner(s, "ui/banner-warning", 125, 506, 290, 44);
                    TextOutline(s, "泡泡快压到护栏了！", 270, 517, 19f, Cream, Hex(0x8A1E1E), 1.8f);
                }
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
            // 2026-10-15: larger than the first build (mockup scale): body about 55 px with 8 pads.
            float bayCanvas = Mathf.Min(118f, baySpacing * 1.45f), bayNumber = baySpacing >= 74f ? 14f : 13f;
            for (int i = 0; i < Battle.Fleet.Count; i++) {
                Fighter ship = Battle.Fleet[i];
                Vector2 at = feedback.Position(ship.Id, i);
                Creature(s, GardenColor(ship.Color), at.x, at.y + 12, bayCanvas, ship.Tier, ship.Ammo.ToString(),
                    Mathf.Clamp01(1 - (Battle.Time - ship.LastFired) / .18f), bayNumber, true, true, .035f, ship.Id * 1.7f);
                if (ship.Arrival <= 0 && !Battle.Exposed(ship.Color)) TextOutline(s, "等待", at.x, 666, 11f, Cream, Outline, 1.3f);
            }
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
                    Creature(s, GardenColor(ship.Color), at.x, QueueDisplayY(at.y) + 12, row == 0 ? 82f : 76f, ship.Tier,
                        row != 0 && ship.Kind == 2 ? "?" : ship.Ammo.ToString(), 0f, 12f, false, true, row == 0 ? .03f : .02f, lane * 2.3f + row * 1.1f);
                    // 2026-10-15 20:52: queue numbers sit on the same navy pill as the pads (mockup); no tier coin in the queue.
                }
                // Priority planting (option B): pulsing gold corner marks on every selectable plant, drawn after the plants.
                if (selection == 1)
                    for (int row = 0; row < Mathf.Min(3, reserve.Count); row++) {
                        Vector2 at = feedback.ReservePosition(reserve[row], lane, row);
                        GardenCorners(s, at.x - 32, QueueDisplayY(at.y) - 28, 64, 56, new Color(1f, .86f, .3f, .75f + .25f * Mathf.Sin(ui * 5f)));
                    }
                if (reserve.Count > 0 && Battle.ActiveFinale == null && Battle.WouldMerge(lane)) {
                    Vector2 at = feedback.ReservePosition(reserve[0], lane, 0);
                    TextOutline(s, "可合成", at.x + 30, QueueDisplayY(at.y) - 30, 12f, Gold, Outline, 1.3f);
                }
                if (reserve.Count == 0 && !FinaleButtonVisible) TextOutline(s, "已出战", Battle.QueuePositionX(lane), 768, 16f, Cream, Outline, 1.5f);
            }
            if (Battle.State != Phase.ReviveOffer) feedback.DrawGardenFeedback(s, GardenColor);
            // Keep all original finale drawing/cues/hit regions as the agreed exception.
            DrawFinaleButton(s); DrawFinale(s);
            if (selection == 1) TextOutline(s, "选择前三排任意队员", 270, 527, 19f, Cream, Outline, 1.8f);
            if (selection == 2) {
                bool map = s.MapLayout; s.MapLayout = false;
                GardenFx.Aim(s, ScreenLayout.Point(aim), (aimMissile ? 150 : 120) * (Battle.Loadout.bombRadius + 1));
                s.MapLayout = map;
                TextOutline(s, "拖动选择泡泡，双击确认", 270, 128, 18f, Cream, Outline, 1.8f);
                GardenButton(s, 9, 120, 512, 300, 54, "确认使用泡泡炸弹");
            }
            if (Battle.State == Phase.Ready && tutorialStep < 0) {
                GardenPanel(s, 105, 500, 330, 44); Text(s, "点下方队员，开始出战", 270, 513, 18);
            }
            if (Battle.Stalled && selection == 0 && !settings && Battle.State == Phase.Playing) {
                GardenPanel(s, 40, 280, 460, 64); Text(s, "出战位满了，同色泡泡被挡住啦", 270, 290, 16);
                Text(s, "试试泡泡炸弹，清开最下排", 270, 317, 16);
            }
            if (Battle.NoticeTime > 0 && selection == 0 && !settings) TextOutline(s, GardenWords(Battle.Notice), 270, 864, 13f, Cream, Outline, 1.4f);
            if (Battle.AdvanceHeld > 0 && selection == 0) TextOutline(s, "泡泡停住 " + Battle.AdvanceHeld.ToString("0.0") + " 秒", 270, 238, 16f, Cream, Outline, 1.6f);
            if (lesson != null && Battle.State != Phase.Intro && Battle.State != Phase.Won && Battle.State != Phase.Lost && !settings && selection == 0 && !finaleGuide) DrawGardenTutorial(s);
            if (Battle.State == Phase.Intro) DrawGardenNewFriends(s);
            EndHitsCore();
        }
        // Higher layer than prefab dialogs: revive/confirmation/transition retain
        // their existing paint and input priority. Fixed UI is in nested prefabs.
        void DrawGardenOverlay(PixelSurface s) {
            s.MapLayout = true;
            if (Battle.State == Phase.ReviveOffer) DrawGardenRevive(s);
            else if (!settings && finaleGuide) DrawFinaleGuide(s);
            if (streakPrompt) {
                GardenModal(s, "连胜中断", 280, 400);
                Text(s, streakWhat + "将中断连胜", 270, 420, 18);
                GardenButton(s, 511, 80, 536, 380, 58, streakRevive ? "复活继续" : "继续挑战");
                GardenButton(s, 510, 150, 608, 240, 44, "确认中断连胜");
            }
            if (energyOpen) {
                GardenModal(s, "体力", 280, 380); Text(s, Economy.Energy + " / 5", 270, 370, 25);
                GardenButton(s, 503, 420, 292, 48, 40, "X");
                GardenButton(s, 501, 84, 512, 372, 54, "广告补充体力 +5");
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
        void GardenModal(PixelSurface s, string title, float y, float height) {
            HitBlock(s); s.Rect(0, 0, 540, 960, new Color(0, 0, 0, .65f));
            GardenPanel(s, 50, y, 440, height);
            if (Asset(s, "ui/popup-ribbon", 80, y - 26, 380, 60, Color.white)) TextOutline(s, title, 270, y - 13, 24f, Cream, Outline, 2f);
            else Text(s, title, 270, y + 24, 26);
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
            TextOutline(s, "小蓝", 150, 128, 13f, Cream, Outline, 1.4f);
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
            TextOutline(s, "课堂保护中", 470, 532, 12f, Cream, Outline, 1.3f);
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
        // New squad member card (UI spec v1.1 5.1, v3 copy): ready art, name, and a same-color bead; drawn during the level intro.
        static readonly string[][] NewFriends = {
            new[] { "morning-glory", "carrot", "broccoli" }, new[] { "eggplant" }, new[] { "sunflower", "cabbage" }, new[] { "strawberry", "truffle" } };
        void DrawGardenNewFriends(PixelSurface s) {
            if (levelIndex < 0 || levelIndex >= NewFriends.Length) return;
            string[] keys = NewFriends[levelIndex];
            var pal = Battle.Level.palette;
            Asset(s, "ui/card-rays", 70, 160, 400, 400, new Color(1f, 1f, 1f, .85f));
            int n = keys.Length;
            float cw = Mathf.Min(190f, 480f / n - 10f), ch = cw * 720f / 520f, total = n * cw + (n - 1) * 10f, x0 = 270 - total / 2, y0 = 240;
            for (int k = 0; k < n; k++) {
                Color tint = Color.white; bool found = false;
                for (int i = 0; i + 2 < pal.Length && !found; i += 3) {
                    Color c = new Color32((byte)pal[i], (byte)pal[i + 1], (byte)pal[i + 2], 255);
                    if (Key(c) == keys[k] && Has(PlantSlot(c, "idle"))) { tint = c; found = true; }
                }
                float x = x0 + k * (cw + 10f);
                if (!Asset(s, "ui/card-frame", x, y0, cw, ch, Color.white)) GardenPanel(s, x, y0, cw, ch);
                if (found) Creature(s, tint, x + cw / 2, y0 + ch * .64f, cw * .9f, 0, "", 0f, 14f, false, false);
                TextOutline(s, found ? Name(tint) : "", x + cw / 2, y0 + ch * .80f, Mathf.Min(20f, cw * .13f), Cream, Outline, 1.8f);
                if (found) Bug(s, tint, new Vector2(x + cw / 2, y0 + ch + 16), 10.5f, true);
            }
            if (Asset(s, "ui/popup-ribbon", 80, y0 - 64, 380, 60, Color.white)) TextOutline(s, "新队员加入", 270, y0 - 51, 24f, Cream, Outline, 2f);
            else Text(s, "新队员加入", 270, y0 - 50, 22);
            TextOutline(s, "专打同色泡泡", 270, y0 + ch + 30, 15f, Cream, Outline, 1.5f);
        }
        void DrawGardenRevive(PixelSurface s) {
            GardenModal(s, "泡泡压到护栏啦", 210, 560);
            Text(s, "复活：清最下排、推回泡泡", 270, 330, 19);
            Text(s, "出战位 +1，泡泡炸弹 +1", 270, 370, 19);
            GardenButton(s, 30, 80, 610, 380, 68, "广告复活，继续战斗");
            GardenButton(s, 31, 190, 692, 160, 40, "放弃");
        }
    }
}
#endif
