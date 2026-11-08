#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Approved waving-bear composition, authored at 540 × 1026. Fit the complete
        // group and both action rows into the safe area without stretching the toys.
        internal static void HomeWaveView(out float k, out float x, out float y)
        {
            float height = ScreenLayout.SafeBottom - ScreenLayout.Top;
            k = Mathf.Min(1f, ScreenLayout.Width / 540f, height / 1026f);
            x = 270f - 270f * k;
            y = ScreenLayout.Top + (height - 1026f * k) * .5f;
        }
        internal static Rect HomeWaveRect(float x, float y, float w, float h)
        {
            HomeWaveView(out float k, out float ox, out float oy);
            return new Rect(ox + x * k, oy + y * k, w * k, h * k);
        }
        static readonly Rect[] HomeWaveFrames = {
            new Rect(-10f, 335f, 178f, 204f), new Rect(375f, 394f, 180f, 205f),
            new Rect(-23f, 475f, 206f, 235f), new Rect(357f, 513f, 196f, 224f),
            new Rect(110f, 282f, 335f, 402f)
        };
        // 2026-10-30 16:10 (调整 4, 方案B): the redrawn group (art batch 3, H2) - bear in the paw-print pot, the frog and
        // the chick peeking from behind - painted on one 1080 square canvas; when it is present the four side partners
        // are hidden and only the bear (the whole group) can be poked. Authored over the reference at 540 × 1026.
        static int homeGroupArt = -1;
        internal static bool HomeGroupArt
        {
            get { if (homeGroupArt < 0) homeGroupArt = Resources.Load<Texture2D>("GardenR1/home-wave-frog") ? 1 : 0; return homeGroupArt == 1; }
        }
        static readonly Rect HomeGroupFrame = new Rect(20f, 236f, 500f, 500f);   // the art fills its canvas edge to edge
        internal float HomeWaveAge(int member)
        {
            float age = ui - homePokeAt[member];
            int turn = Mathf.FloorToInt(ui / HomeHopEvery);
            if (!(age >= 0f && age < 2.5f) && HomeHopOrder[((turn % 5) + 5) % 5] == member)
                age = ui - turn * HomeHopEvery;
            return age;
        }
        internal int HomeWavePose(int member)
        {
            if ((ui + HomePhase[member]) % 4.1f < .13f) return 2;
            float age = HomeWaveAge(member);
            return member != 4 && age >= 0f && age < .6f ? 1 : 0;
        }
        internal Rect HomeWaveMemberRect(int member)
        {
            Rect frame = member == 4 && HomeGroupArt ? HomeGroupFrame : HomeWaveFrames[member];
            float breathe = 1f + .008f * Mathf.Sin(ui * 2.2f + HomePhase[member]);
            float h = frame.height * breathe, w = frame.width / breathe;
            return HomeWaveRect(frame.center.x - w * .5f, frame.yMax - h, w, h);
        }
        void DrawWaveHome(PixelSurface s)
        {
            s.MapLayout = false;
            GardenVoiceHome();   // 2026-11-04: the launch greeting (PixelWingGame.GardenVoice)
            HomeWaveView(out float k, out float ox, out float oy);
            // Quiet decoration: the formerly oversized upper-right bubble is now
            // 42 logical px (about one third of the reference) at 32% opacity.
            // 2026-10-30 (调整 4): the reference's big rainbow bubble beside the bear's raised paw.
            if (HomeGroupArt) WaveBubble(s, 418f, 270f, 104f, .9f, 0);
            else WaveBubble(s, 435f, 290f, 42f, .32f, 0);
            WaveBubble(s, 270f, 96f, 26f, .35f, 1);
            WaveBubble(s, 159f, 301f, 24f, .28f, 2);
            WaveBubble(s, 505f, 380f, 23f, .25f, 3);
            WaveBubble(s, 61f, 723f, 24f, .28f, 4);
            // Match the painting order in GardenHomeArtwork, including hit precedence.
            foreach (int i in Garden.UI.GardenHomeArtwork.PaintOrder)
            {
                if (i != 4 && HomeGroupArt) continue;
                Rect r = HomeWaveMemberRect(i);
                HitBox(s, HomeMemberAction + i, r.x + r.width * .08f, r.y + r.height * .08f, r.width * .84f, r.height * .86f);
                float age = HomeWaveAge(i);
                if (age < 0f || age >= 2.5f) continue;
                // Double the restored blown-bubble diameter, retaining its opacity and timing.
                float drift = Mathf.Max(0f, age - .6f), size = (24f + i * 2f) * 2f * k * Mathf.Clamp01(age / .6f);
                Vector2 at;
                if (i == 4 && HomeGroupArt) at = new Vector2(r.x + r.width * .7f, r.y + r.height * .2f);   // over the raised paw
                else if (i == 4) at = new Vector2(r.xMax - 13f * k, r.y + 67f * k);
                else {
                    Vector2 m = MouthCanvas(Hex(HomeSquadColor[i]));
                    at = new Vector2(r.x + r.width * m.x / 224f, r.y + r.height * m.y / 256f);
                }
                at += new Vector2(Mathf.Sin(drift * 2f + i) * drift * 9f * k, -size * .55f - drift * 65f * k);
                SoapBubble(s, at, size, Color.white, Mathf.Clamp01((2.5f - age) / .3f) * .8f, i);
            }
            DrawWaveStart(s);
            // 2026-11-03 10:19 (user: no word under the settings button).
            // 2026-11-06 (user: plan A): with a capsule the button sits level with it, at the left of the same row.
            HomeIconButton(s, 801, ScreenLayout.Left + 24f, HomeCornerY(48f, ScreenLayout.Top + 18f), "plants/home/btn-settings", "设置", Hex(0x5BB8FF), 48f, -1f);
            // 2026-10-30 (界面优化 4, after the reference): 伙伴图鉴 in the top-right corner, opposite 设置.
            // 2026-10-30 16:10 (调整 4): exactly where the reference has it - at the right edge, level with the bubble.
            // 2026-11-03 10:19 (user: the album button's icon and word a little bigger): icon 48 -> 60 px (right edge kept),
            // word 14 -> 18 px.
            float codexY = Mathf.Max(ScreenLayout.Top + 96f, HomeWaveRect(0f, 300f, 0f, 0f).y);
            HomeIconButton(s, CodexOpenAction, ScreenLayout.Right - 82f, codexY, "plants/home/btn-codex", "伙伴图鉴", Hex(0xFFB347), 60f, 18f);
            // 2026-11-08 (user): Douyin's 侧边栏 reward button under it, same size (PixelWingGame.Sidebar).
            DrawSidebarEntry(s, ScreenLayout.Right - 82f, codexY + 104f);
            DrawWavePartner(s, k, ox, oy);
            s.MapLayout = true;
            DrawLevelPick(s);
            DrawSidebarDialog(s);
        }
        void WaveBubble(PixelSurface s, float x, float y, float size, float opacity, int seed)
        {
            Rect r = HomeWaveRect(x, y + Mathf.Sin(ui * .55f + seed) * 2f, size, size);
            SoapBubble(s, new Vector2(r.x, r.y), r.width, Color.white, opacity, seed);
        }
        // 2026-10-30 (界面优化 4, after the reference): under the squad a white 「下一位伙伴」 card (opens the album), then the
        // level line 「第 N 关」 and the big blue 开始游戏 capsule at the bottom.
        void DrawWaveStart(PixelSurface s)
        {
            int level = NextBubbleLevel() + 1;
            if (level != homeLabelLevel) { homeLabelLevel = level; homeLabel = "第 " + level + " 关"; }
            Rect label = HomeWaveRect(170f, 862f, 200f, 30f);
            float lk = label.height / 30f;
            // 2026-11-03 10:19 (user: the level line a little bigger): 22 -> 28 px, nudged up 3 px to keep its gap to the button.
            GardenBarArt.Text(s, homeLabel, label.center.x, label.y - 3f * lk, 28f * lk, true, Hex(0x1D3A7A));
            // 2026-10-31 16:26 (user: 开始游戏和设置里的按钮不像一种样式): the same capsule, words and proportions as the
            // dialog buttons (300x80, 32 px words, the face at .47) - it was a chunkier 340x96 with 36 px words.
            Rect r = HomeWaveRect(110f, 904f, 320f, 80f);
            HitBox(s, 800, r.x, r.y, r.width, r.height);
            float k = r.width / 320f * (1f - PressPulse(800) * .04f);
            float w = 320f * k, h = 80f * k, x = r.center.x - w * .5f, y = r.center.y - h * .5f;
            s.Glow(r.center.x, y + h * .9f, w * 1.04f, h * .78f, new Color(.08f, .28f, .7f, .26f), 0f);   // 2026-10-31: the dialog buttons' soft shadow, in blue on the light page
            if (!Asset9(s, "plants/uiref/button-blue", x, y, w, h, 52f, 0f, h * .5f, 0f, Color.white))
                Asset9(s, "plants/win/button-next", x, y, w, h, 64f, 0f, h * 64f / 126f, 0f, Color.white);
            float ts = 32f * k;   // 2026-10-30: clean white words like the reference
            ButtonWord(s, "开始游戏", r.center.x - GardenBarArt.TextWidth("开始游戏", ts) * .5f, y + h * .47f, ts, Color.white, false);
        }
        void DrawWavePartner(PixelSurface s, float k, float ox, float oy)
        {
            int current = NextBubbleLevel() + 1;
            CodexEntry next = default; bool found = false;
            foreach (var e in Codex) { if (CodexObtained(e)) continue; next = e; found = true; break; }
            if (!found) next = new CodexEntry { name = "", tint = Hex(0x2982F4), level = current };
            if (homeNextLevel != current || homeNextName != next.name)
            {
                homeNextLevel = current; homeNextName = next.name;
                homeNextLine = !found ? "小队伙伴已集齐" : "通关第 " + next.level + " 关解锁";
                homeNextMid = "第 " + next.level + " 关";
            }
            Rect card = HomeWaveRect(66f, 762f, 408f, 84f);
            HitBox(s, CodexOpenAction, card.x, card.y, card.width, card.height);
            float press = 1f - PressPulse(CodexOpenAction) * .03f, cw = card.width * press, ch = card.height * press;
            float cx0 = card.center.x - cw * .5f, cy0 = card.center.y - ch * .5f, rr = 20f * k;
            // 2026-10-31 11:49 (user: 字的颜色和描述、边框没有依照示意图), measured off the reference: a 2 px pale-blue rim
            // (#C7DEFA) with only a whisper of shadow, the partner in a round pale-cyan badge, the heading deep navy
            // (#16285A), the line under it grey (#6B7589) with 「第 N 关」 in bright blue (#1A6DF8, bolder), and a
            // thicker blue chevron.
            WinRoundRect(s, cx0, cy0 + 3f * k, cw, ch, rr, new Color(.12f, .25f, .55f, .06f));
            WinRoundRect(s, cx0 - 2f * k, cy0 - 2f * k, cw + 4f * k, ch + 4f * k, rr + 2f * k, Hex(0xC7DEFA));
            WinRoundRect(s, cx0, cy0, cw, ch, rr, Color.white);
            float gx = cx0 + 52f * k, gy = card.center.y, br = 31f * k;
            s.GardenRoundedRect(gx - br, gy - br, br * 2f, br * 2f, br, Hex(0xE8F7FF), Hex(0xCBEBFB));
            bool before = GardenSkin.Celebrate; GardenSkin.Celebrate = true;
            Creature(s, next.tint, gx, gy + 27f * k + Mathf.Sin(ui * 1.8f) * 1.2f * k, 62f * k, 0, "", 0f, 12f, false, false, .03f, 1.3f);
            GardenSkin.Celebrate = before;
            float tx = cx0 + 104f * k;
            string head = found ? "下一位伙伴" : "全员集结";
            Color navy = Hex(0x16285A), grey = Hex(0x6B7589), blue = Hex(0x1A6DF8);
            GardenBarArt.Text(s, head, tx, card.center.y - 30f * k, 22f * k, false, navy);
            // 2026-11-03 19:50 (UI check): one ink pass - the rounded atlas is already baked bold; the offset second pass thickened the strokes.
            float ly = card.center.y + 3f * k, ls = 17f * k;
            if (!found) GardenBarArt.Text(s, homeNextLine, tx, ly, ls, false, grey);
            else
            {
                string mid = homeNextMid;
                float x = tx;
                GardenBarArt.Text(s, "通关", x, ly, ls, false, grey); x += GardenBarArt.TextWidth("通关", ls) + 3f * k;
                GardenBarArt.Text(s, mid, x, ly, ls, false, blue);
                x += GardenBarArt.TextWidth(mid, ls) + 3f * k;
                GardenBarArt.Text(s, "解锁", x, ly, ls, false, grey);
            }
            float vx = cx0 + cw - 30f * k, vy = card.center.y;
            s.Segment(new Vector2(vx - 5f * k, vy - 8f * k), new Vector2(vx + 3f * k, vy), 4f * k, blue);
            s.Segment(new Vector2(vx + 3f * k, vy), new Vector2(vx - 5f * k, vy + 8f * k), 4f * k, blue);
        }
    }
}
#endif
