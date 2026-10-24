#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-16 22:35: home settings/codex buttons, the bubble codex and new members on the result page.
    // A member is obtained once the level where it first appears (the debut table) has been cleared.
    public partial class PixelWingGame
    {
        struct CodexEntry { public string name; public Color tint; public int level; }   // level: 1-based
        static List<CodexEntry> codex;
        static List<CodexEntry> Codex
        {
            get
            {
                if (codex != null) return codex;
                codex = new List<CodexEntry>();
                var seen = new HashSet<string>();
                for (int level = 0; level < Levels.Length; level++)
                    foreach (int rgb in GardenColorCatalog.Debut(level))
                    {
                        string name = GardenColorCatalog.Name(rgb);
                        if (!seen.Add(name)) continue;
                        // The opening levels use the lighter blue/orange variants; the codex shows the main colors.
                        int art = rgb == 0x42A9FF ? 0x2982F4 : rgb == 0xFF9D30 ? 0xFF7E27 : rgb;
                        codex.Add(new CodexEntry { name = name, tint = Hex(art), level = level + 1 });
                    }
                return codex;
            }
        }
        bool CodexObtained(CodexEntry e) => Economy.Data.cleared.Contains(e.level);

        // ---- home: round icon buttons in the top corners (art: Art/R1/home-v1 H11/H12; program bubble until then)
        const int CodexOpenAction = 803, CodexCloseAction = 804;
        bool codexOpen;
        public bool GardenCodexOpen => codexOpen;
        public int GardenCodexSelection => codexSelected;
        float codexOpenedAt;
        void DrawHomeCornerButtons(PixelSurface s)
        {
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float top = ScreenLayout.Top + 8f;
            HomeIconButton(s, 801, ScreenLayout.Left + 12f, top, "plants/home/btn-settings", "设置", Hex(0x5BB8FF), 45f);   // 80 % (10-17)
            // 2026-10-17: feature entries live in the right-hand column at mid height (as in most casual games),
            // beside the squad; settings stays in the top-left corner.
            HomeArtView(out float k, out _, out float oy);
            HomeIconButton(s, CodexOpenAction, ScreenLayout.Right - 70f, oy + 410f * k, "plants/home/btn-codex", "图鉴", Hex(0xFFB347));   // squad mid height (10-17)
            // 2026-10-22: hide the level picker entry for now, including its hit region.
            s.MapLayout = mapped;
        }
        // 2026-10-17 (user: a reason to come back). Under the start button: the next member still to join, its name,
        // how many more clears it takes and pips from the last member to this one. Tapping it opens the codex.
        int homeNextLevel = -1;
        string homeNextName = "", homeNextLine = "";
        void DrawHomeNextMember(PixelSurface s, float y)
        {
            int current = NextBubbleLevel() + 1;   // the 1-based level the start button opens
            CodexEntry next = default;
            bool found = false;
            foreach (var e in Codex)
            {
                if (CodexObtained(e)) continue;
                next = e; found = true; break;
            }
            if (!found) return;
            int left = Mathf.Max(1, next.level - current + 1);
            if (current != homeNextLevel)
            {
                homeNextLevel = current; homeNextName = next.name;
                homeNextLine = left == 1 ? "通关这一关就来啦！" : "再通关 " + left + " 关就来啦！";
            }
            // 16:18 (user picked version A of four mock-ups): no box. The member floats inside a big soap bubble
            // (bubble behind, member, bubble film in front), two small bubbles drift beside it, and the words sit to
            // the right: 新伙伴 + gold name, then 再通关 N 关就来啦！
            float bob = Mathf.Sin(ui * 1.8f) * 4f, gx = 196f, gy = y + 46f + bob, d = 110f;
            HitBox(s, CodexOpenAction, gx - d * .5f, y, 300f, d);
            s.Glow(gx, gy, 150f, 130f, A(next.tint, .32f + .08f * Mathf.Sin(ui * 2.4f)), 0f);
            SoapBubble(s, new Vector2(gx, gy), d, Color.white, .55f, 7);
            bool before = GardenSkin.Celebrate;
            GardenSkin.Celebrate = true;
            Creature(s, next.tint, gx, gy + 34f, 84f, 0, "", 0f, 12f, false, false, .03f, 1.3f);
            GardenSkin.Celebrate = before;
            SoapBubble(s, new Vector2(gx, gy), d, Color.white, .45f, 7);
            SoapBubble(s, new Vector2(gx - 58f, gy - 34f + Mathf.Sin(ui * 2.3f) * 3f), 14f, Color.white, .9f, 2);
            SoapBubble(s, new Vector2(gx + 54f, gy - 48f + Mathf.Sin(ui * 2.7f + 1f) * 3f), 10f, Color.white, .9f, 4);
            float tx = 252f, tagW = TextWidth("新伙伴", 17f);
            TextOutline(s, "新伙伴", tx, y + 18f, 17f, Hex(0xFFF0C8), Outline, 1.6f, false);
            TextOutline(s, homeNextName, tx + tagW + 8f, y + 12f, 24f, Gold, Outline, 2f, false);
            TextOutline(s, homeNextLine, tx, y + 48f, 19f, Cream, Outline, 1.7f, false);
        }
        void HomeIconButton(PixelSurface s, int id, float x, float y, string art, string label, Color fallback, float size = 56f)
        {
            HitBox(s, id, x - 4f, y - 4f, size + 8f, size + 24f);
            float cx = x + size * .5f, cy = y + size * .5f, d = size * (1f - PressPulse(id) * .1f);
            if (!Asset(s, art, cx - d * .5f, cy - d * .5f, d, d, Color.white))
            {
                GardenSkin.Shot(s, fallback, new Vector2(cx, cy), d, 1f);
                TextOutline(s, label.Substring(0, 1), cx, cy - size * .21f, size * .39f, Cream, Outline, 1.8f);
            }
            // 2026-10-22 11:29 (user: the home buttons with the others): the words were 10-13 px in the old thin font;
            // now the rounded type of the other buttons, 16 px, white with a navy edge.
            GardenBarArt.TextOutline(s, label, cx, y + size + 1f, 16f, Color.white, Hex(0x54318C), 2.2f);
        }

        // ---- codex page: cloud album; original 20-member order, unlock rules and card interactions.
        const int CodexCardAction = 810;   // 810 + index
        int codexSelected = -1;
        float codexPickedAt = -9f;
        static readonly System.Collections.Generic.Dictionary<string, string[]> CodexBlurb = new System.Collections.Generic.Dictionary<string, string[]> {
            { "小蓝", new[] { "队长", "泡泡小队的队长，挺胸一喊，大家就出发！" } },
            { "小橙", new[] { "调皮", "最爱蹦高高，总想第一个冲出去。" } },
            { "小绿", new[] { "憨厚", "软软的青蛙脸，鼓起腮帮就想吹泡泡。" } },
            { "小紫", new[] { "爱笑", "大耳朵轻轻扇，小鼻子吹出彩虹泡泡。" } },
            { "小黄", new[] { "机灵", "眨眨眼，就能吹出一串彩虹泡泡。" } },
            { "小白", new[] { "认真", "绒绒的猫耳朵，做事总是一丝不苟。" } },
            { "小红", new[] { "热情", "狐狸耳朵竖起来，见到伙伴就开心。" } },
            { "小黑", new[] { "酷", "一身炭黑小绒毛，其实心里最暖。" } },
            { "小靛", new[] { "迷糊", "小熊耳朵圆圆的，总是半睁着眼。" } },
            { "小冰", new[] { "好奇", "冰蓝色的小绒熊，什么都想看看。" } },
            { "小雾", new[] { "爱做梦", "雾紫色的绒耳朵，常在梦里吹泡泡。" } },
            { "小玫", new[] { "傲娇", "莓红色的兔耳朵，一挑眉就不服气。" } },
            { "小棕", new[] { "稳重", "两只小圆耳朵，笑起来憨憨的。" } },
            { "小晶", new[] { "优雅", "紫罗兰色的小象，慢悠悠地卷起鼻子。" } },
            { "小荧", new[] { "好动", "黄绿色的小青蛙，一刻也停不下来。" } },
            { "小碧", new[] { "随和", "薄荷绿的毛绒脸，抿嘴一笑最治愈。" } },
            { "小翠", new[] { "勇敢", "深绿绒毛暖乎乎，越危险越向前。" } },
            { "小粉", new[] { "甜甜", "两只软软的兔耳朵，最爱黏着大家。" } },
            { "小霞", new[] { "温和", "蜜桃色的软兔子，笑起来像棉花糖。" } },
            { "小清", new[] { "害羞", "浅薄荷色的小青蛙，红着脸躲在后面。" } },
        };
        // Cloud album, 2026-10-22: the approved page concept with live collection data and original character art.
        static readonly Color CodexInk = Hex(0x54318C), CodexMuted = Hex(0x88739A), CodexSilhouette = Hex(0xB7A2CD);
        static void CodexRound(PixelSurface s, float x, float y, float w, float h, float radius, Color color)
            => s.GardenRoundedRect(x, y, w, h, radius, color, color);
        static void CodexLock(PixelSurface s, float cx, float cy, float size)
        {
            float k = size / 20f;
            CodexRound(s, cx - 6f * k, cy - 10f * k, 12f * k, 14f * k, 6f * k, Color.white);
            CodexRound(s, cx - 3.5f * k, cy - 7.5f * k, 7f * k, 11f * k, 3.5f * k, CodexSilhouette);
            CodexRound(s, cx - 8f * k, cy - 1f * k, 16f * k, 12f * k, 3f * k, A(CodexInk, .14f));
            CodexRound(s, cx - 8f * k, cy - 2f * k, 16f * k, 12f * k, 3f * k, Hex(0xF9FCFF));
            CodexRound(s, cx - 1.8f * k, cy + 1f * k, 3.6f * k, 5.5f * k, 1.8f * k, CodexMuted);
        }
        static void CodexSilhouetteArt(PixelSurface s, Color tint, float cx, float feet, float height)
        {
            float ah = height * ArtScale(tint), aw = ah * ArtAspect;
            CreatureSilhouette(s, PlantSlot(tint, "idle"), cx - aw * .5f, feet - ah * FeetLine, aw, ah, CodexSilhouette);
        }
        void DrawBubbleCodex(PixelSurface s)
        {
            if (!codexOpen) return;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            GardenSkin.Clock = ui;
            var list = Codex;
            int n = list.Count, got = 0, latest = 0;
            for (int i = 0; i < n; i++) if (CodexObtained(list[i])) { got++; latest = i; }
            if (codexSelected < 0 || codexSelected >= n) codexSelected = latest;
            float oy = (ScreenLayout.Height - 960f) * .5f;
            float enter = Clamp01((ui - codexOpenedAt) / .3f), slide = (1f - OutCubic(enter)) * 10f;
            HitBlock(s);
            // The opaque cloud plate is rendered by GardenBattlePage; BubbleHome skips its home foreground.
            GardenTitleArt.Draw(s, "泡泡图鉴", 270f, oy + 43f - slide, 42f);
            SoapBubble(s, new Vector2(150f, oy + 50f), 26f, Color.white, .7f, 2);
            SoapBubble(s, new Vector2(389f, oy + 40f), 17f, Color.white, .65f, 4);
            HitBox(s, CodexCloseAction, 470f, oy + 14f, 60f, 60f);
            float close = 42f * (1f - PressPulse(CodexCloseAction) * .1f), cy = oy + 44f;
            CodexRound(s, 500f - close * .5f, cy - close * .5f + 2f, close, close, close * .5f, A(Hex(0xD6544C), .2f));
            CodexRound(s, 500f - close * .5f, cy - close * .5f, close, close, close * .5f, Hex(0xF06B61));
            s.GardenRoundedRect(501.2f - close * .5f, cy - close * .5f + 1.2f, close - 2.4f, close - 2.4f, close * .5f,
                Hex(0xFFA08F), Hex(0xFF6C62));
            float cross = close * .18f;
            s.Segment(new Vector2(500f - cross, cy - cross), new Vector2(500f + cross, cy + cross), 4f, Color.white);
            s.Segment(new Vector2(500f + cross, cy - cross), new Vector2(500f - cross, cy + cross), 4f, Color.white);
            // Keep the small collection label single-pass so dense strokes stay distinct.
            GardenSkin.Text(s, "已收集 " + got + " / " + n, 270f, oy + 86f, 20f, true, CodexInk);
            CodexRound(s, 163f, oy + 110f, 214f, 19f, 9.5f, Hex(0xF5FCFF));
            s.GardenRoundedRect(165f, oy + 112f, 210f, 15f, 7.5f, Hex(0xD5C1E9), Hex(0xE7D9F2));
            float fill = 210f * got / Mathf.Max(1, n);
            if (fill > 0f) s.GardenRoundedRect(165f, oy + 112f, fill, 15f, 7.5f, Hex(0xBDEB9B), Hex(0x6BC75F));

            var entry = list[codexSelected];
            bool have = CodexObtained(entry);
            float pick = Mathf.Max(0f, ui - codexPickedAt);
            float pop = pick < .45f ? 1f + .1f * Mathf.Sin(pick / .45f * Mathf.PI) * (1f - pick / .45f) : 1f;
            const float cx = 270f;
            float feet = oy + 331f;
            s.Glow(cx, oy + 253f, 330f, 320f, new Color(.85f, .99f, 1f, .85f), 0f);
            s.Glow(cx, feet + 7f, 242f, 45f, new Color(.33f, .65f, .88f, .3f), 0f);
            Asset(s, "scene/plot", cx - 113f, feet - 19f, 226f, 43f, Color.white);
            for (int i = 0; i < 3; i++)
            {
                float bx = i == 1 ? 397f : i == 0 ? 150f : 137f;
                float by = oy + (i == 0 ? 181f : i == 1 ? 226f : 291f) + Mathf.Sin(ui * .8f + i * 2f) * 3f;
                SoapBubble(s, new Vector2(bx, by), i == 2 ? 25f : 48f, Color.white, .64f, i + 1);
            }
            if (have)
            {
                bool before = GardenSkin.Celebrate;
                GardenSkin.Celebrate = pick < .9f;
                Creature(s, entry.tint, cx, feet, 282f * pop, 0, "", 0f, 12f, false, false, .018f, codexSelected * 1.3f);
                GardenSkin.Celebrate = before;
            }
            else
            {
                CodexSilhouetteArt(s, entry.tint, cx, feet, 282f * pop);
                CodexLock(s, cx, oy + 265f, 34f);
            }
            string[] blurb = CodexBlurb.TryGetValue(entry.name, out var b) ? b : new[] { "", "" };
            string title = have ? entry.name : "？？？";
            float nameWidth = GardenBarArt.TextWidth(title, 36f), tagWidth = have ? GardenBarArt.TextWidth(blurb[0], 15f) + 18f : 0f;
            float groupWidth = nameWidth + (have ? 12f + tagWidth : 0f), nameLeft = cx - groupWidth * .5f;
            GardenBarArt.TextOutline(s, title, nameLeft + nameWidth * .5f, oy + 362f, 36f,
                have ? Hex(0xFFD56C) : CodexInk, have ? Hex(0x985C27) : Color.white, have ? 1.3f : 2f);
            if (have)
            {
                float tagX = nameLeft + nameWidth + 12f;
                s.GardenRoundedRect(tagX, oy + 372f, tagWidth, 24f, 12f, Hex(0xB08ADE), Hex(0x8F65C2));
                GardenBarArt.TextOutline(s, blurb[0], tagX + tagWidth * .5f, oy + 375f, 15f, Color.white, Hex(0x684097), .7f);
                // At most two lines; wrap after a clause so the title and unlock note keep stable positions.
                string story = blurb[1];
                int split = story.LastIndexOf('，');
                if (split >= 9 && split < story.Length - 1)
                {
                    // Body copy uses the existing plain text atlas: no rounded-bold stroke or offset overdraw.
                    GardenSkin.Text(s, story.Substring(0, split + 1), cx, oy + 402f, 17f, true, CodexInk);
                    GardenSkin.Text(s, story.Substring(split + 1), cx, oy + 426f, 17f, true, CodexInk);
                }
                else GardenSkin.Text(s, story, cx, oy + 414f, 17f, true, CodexInk);
                DialogText(s, "第 " + entry.level + " 关加入队伍", cx, oy + 450f, 14f, CodexMuted);
            }
            else
            {
                GardenSkin.Text(s, "还没遇见它", cx, oy + 410f, 18f, true, CodexInk);
                GardenSkin.Text(s, "通过第 " + entry.level + " 关就能获得", cx, oy + 445f, 16f, true, CodexMuted);
            }
            GardenBarArt.Text(s, "伙伴一览", 24f, oy + 472f, 23f, false, CodexInk);
            GardenBarArt.Text(s, "点击卡片查看", 516f - GardenBarArt.TextWidth("点击卡片查看", 13f), oy + 481f, 13f, false, CodexMuted);
            const int cols = 5;
            const float cw = 92f, ch = 94f, gap = 8f;
            float gy = oy + 507f + slide;
            for (int i = 0; i < n; i++)
            {
                var c = list[i];
                bool owned = CodexObtained(c), selected = i == codexSelected;
                float x = 24f + i % cols * (cw + gap), y = gy + i / cols * (ch + gap), mid = x + cw * .5f;
                HitBox(s, CodexCardAction + i, x, y, cw, ch);
                CodexRound(s, x, y + 2f, cw, ch, 11f, new Color(.27f, .55f, .76f, .13f));
                CodexRound(s, x, y, cw, ch, 11f, selected ? Hex(0xFFBD34) : Hex(0xCFB5EF));
                s.GardenRoundedRect(x + 1.1f, y + 1.1f, cw - 2.2f, ch - 2.2f, 10f,
                    selected ? Hex(0xFFFEF5) : Hex(0xFFFFFF), selected ? Hex(0xFFF5DA) : Hex(0xF3EAFB));
                if (owned) Creature(s, c.tint, mid, y + 69f, 96f, 0, "", 0f, 12f, false, false, selected ? .018f : 0f, i * 1.3f);
                else
                {
                    CodexSilhouetteArt(s, c.tint, mid, y + 69f, 96f);
                    CodexLock(s, mid, y + 42f, 17f);
                }
                DialogText(s, owned ? c.name : "第" + c.level + "关", mid, y + 75f, 14f, owned ? CodexInk : Hex(0x8C739E));
                if (selected)
                {
                    float checkX = x + cw - 13f, checkY = y + 12f;
                    CodexRound(s, checkX - 8.5f, checkY - 8.5f, 17f, 17f, 8.5f, Hex(0xFFBD34));
                    s.Segment(new Vector2(checkX - 4f, checkY), new Vector2(checkX - 1f, checkY + 3f), 2.4f, Color.white);
                    s.Segment(new Vector2(checkX - 1f, checkY + 3f), new Vector2(checkX + 5f, checkY - 3f), 2.4f, Color.white);
                }
            }
            DialogText(s, got == n ? "伙伴全部到齐啦！" : "通关新关卡，认识更多伙伴", cx, oy + 929f, 14f, CodexMuted);
            s.MapLayout = mapped;
        }
        void SelectCodex(int index)
        {
            if (index == codexSelected && ui - codexPickedAt < .5f) return;
            codexSelected = index; codexPickedAt = ui;
            audioChip.Play("ui");   // 22:21 (user): no bell-like chime (was "merge" for collected members)
        }
        void OpenCodex() { codexOpen = true; codexOpenedAt = ui; codexSelected = -1; codexPickedAt = ui - .1f; audioChip.Play("page"); }
        void CloseCodex() { codexOpen = false; audioChip.Play("ui"); }

        // ---- result page: members that joined on this level (first clear only)
        Flightbeat.Model.Battle membersBattle;
        readonly List<CodexEntry> newMembers = new List<CodexEntry>();
        float membersShownAt = -1f;
        // Called every battle frame; decides at the start of a battle, before the win records the clear.
        void TrackNewMembers()
        {
            if (membersBattle == Battle) return;
            membersBattle = Battle; newMembers.Clear(); membersShownAt = -1f;
            if (Economy.Data.cleared.Contains(levelIndex + 1)) return;
            foreach (var e in Codex) if (e.level == levelIndex + 1) newMembers.Add(e);
        }
        void DrawResultNewMembers(PixelSurface s)
        {
            if (newMembers.Count == 0 || Battle == null || Battle.State != Phase.Won) return;
            bool teaching = Battle.Level.lesson > 0;
            if (!teaching) return;   // 2026-10-17: mission wins show new members on the victory screen (BubbleWin)
            if (membersShownAt < 0f) membersShownAt = ui;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            GardenSkin.Clock = ui;
            // The dialog content is a 540x960 block centred on the screen.
            float ox = ScreenLayout.Left, oy = (ScreenLayout.Height - 960f) * .5f;
            float t = ui - membersShownAt, y0 = oy + (teaching ? 492f : 536f);
            float head = OutBack(Clamp01(t / .3f));
            if (head > .02f)
                TextOutline(s, "新伙伴入队！", ox + 270f, y0, 18f * head, Gold, Outline, 2f);
            int n = newMembers.Count;
            DrawMemberAvatars(s, ox + 270f, y0 + 56f, newMembers, t - .25f);   // 11:39: avatars, no names
            float note = Clamp01((t - .6f - n * .15f) / .3f);
            if (note > 0f)
                TextOutline(s, "已收入泡泡图鉴", ox + 270f, y0 + 95f, 14f, new Color(.86f, .92f, 1f, note), new Color(Outline.r, Outline.g, Outline.b, note), 1.2f);
            s.MapLayout = mapped;
        }
    }
}
#endif
