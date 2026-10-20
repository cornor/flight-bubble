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
            // 2026-10-19: level select / replay (PixelWingGame.LevelPick.cs) under the codex entry.
            HomeIconButton(s, LevelPickOpenAction, ScreenLayout.Right - 70f, oy + 410f * k + 92f, "plants/home/btn-levels", "选关", Hex(0x7CC4F0));
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
            TextOutline(s, label, cx, y + size + 1f, 13f * size / 56f, Cream, Outline, 1.5f);
        }

        // ---- codex page (2026-10-17 redesign: a full-screen showcase instead of a small list panel)
        // Top: the selected member on a glowing pedestal with spinning rays, name ribbon, personality and a line
        // about it; tapping a card below selects it and the member cheers. Locked members stay dark with a lock.
        const int CodexCardAction = 810;   // 810 + index
        int codexSelected = -1;
        float codexPickedAt = -9f;
        static readonly System.Collections.Generic.Dictionary<string, string[]> CodexBlurb = new System.Collections.Generic.Dictionary<string, string[]> {
            { "小蓝", new[] { "队长", "泡泡小队的队长，挺胸一喊，大家就出发！" } },
            { "小橙", new[] { "调皮", "最爱蹦高高，总想第一个冲出去。" } },
            { "小绿", new[] { "憨厚", "背上三个小鼓包，跑起来一颠一颠。" } },
            { "小紫", new[] { "爱笑", "长耳朵一甩，笑声能传好远。" } },
            { "小黄", new[] { "机灵", "眨眨眼，就能吹出一串彩虹泡泡。" } },
            { "小白", new[] { "认真", "圆滚滚的胶囊身材，做事一丝不苟。" } },
            { "小红", new[] { "热情", "倒心形的小个子，刘海一翘就开心。" } },
            { "小黑", new[] { "酷", "一身软软的小刺，其实心里最暖。" } },
            { "小靛", new[] { "迷糊", "额头顶着软软的长角，总是半睁着眼。" } },
            { "小冰", new[] { "好奇", "像水母一样飘来飘去，什么都想看看。" } },
            { "小雾", new[] { "爱做梦", "云朵做的身体，走到哪儿都轻飘飘。" } },
            { "小玫", new[] { "傲娇", "头顶一颗泡泡发髻，一挑眉就不服气。" } },
            { "小棕", new[] { "稳重", "两只小圆耳朵，笑起来憨憨的。" } },
            { "小晶", new[] { "优雅", "像宝石一样闪闪发亮，酷酷的不爱说话。" } },
            { "小荧", new[] { "好动", "五角星的身体，一刻也停不下来。" } },
            { "小碧", new[] { "随和", "又宽又扁的软团子，抿嘴一笑最治愈。" } },
            { "小翠", new[] { "勇敢", "两颗泡泡叠在一起，越危险越向前。" } },
            { "小粉", new[] { "甜甜", "两颗泡泡小辫子，最爱黏着大家。" } },
            { "小霞", new[] { "温和", "背上一对小翅膀，像软糖一样甜。" } },
            { "小清", new[] { "害羞", "小小一团气，卷着尾巴躲在后面。" } },
        };
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
            float w = ScreenLayout.Width, h = ScreenLayout.Height, L = ScreenLayout.Left, oy = (h - 960f) * .5f;
            float t = Clamp01((ui - codexOpenedAt) / .3f), slide = (1f - OutBack(t)) * 30f;
            HitBlock(s);
            // Backdrop: deep night blue with a soft light behind the showcase.
            s.Rect(L, 0f, w, h, new Color(.05f, .12f, .28f, .95f * t));
            s.Glow(L + w * .5f, oy + 290f, 640f, 560f, new Color(.35f, .62f, 1f, .35f * t), 0f);
            // Title ribbon, collected count and progress bar, close button.
            GardenTitle(s, "泡泡图鉴", 270f, oy + 24f - slide, 30f, true);
            HitBox(s, CodexCloseAction, 470f, oy + 14f, 60f, 60f);
            float cb = 1f - PressPulse(CodexCloseAction) * .1f;
            GardenSkin.Shot(s, Hex(0xFF8A7A), new Vector2(500f, oy + 44f), 42f * cb, t);
            TextOutline(s, "×", 500f, oy + 31f, 26f, Cream, Outline, 2f);
            TextOutline(s, "已收集 " + got + " / " + n, 270f, oy + 84f, 15f, Hex(0xFFE58A), Outline, 1.5f);
            s.Rect(170f, oy + 106f, 200f, 8f, new Color(.02f, .06f, .16f, .8f * t));
            s.Rect(170f, oy + 106f, 200f * got / Mathf.Max(1, n), 8f, new Color(1f, .82f, .3f, t));
            // Showcase.
            var e = list[codexSelected];
            bool have = CodexObtained(e);
            float pick = ui - codexPickedAt, pop = pick < .45f ? 1f + .18f * Mathf.Sin(pick / .45f * Mathf.PI) * (1f - pick / .45f) : 1f;
            const float cx = 270f;
            float feet = oy + 372f;
            Color glow = have ? e.tint : new Color(.4f, .5f, .7f, 1f);
            DrawBubbleHalo(s, cx, oy + 288f, 330f, (have ? 1f : .45f) * t);   // clears the count line above
            if (have) DrawHaloBubbles(s, cx, oy + 288f, 330f, 99f, t, false, false);   // calm: no burst, fewer, slower
            s.Glow(cx, oy + 272f, 220f, 220f, new Color(glow.r, glow.g, glow.b, .22f * t), 0f);
            s.Glow(cx, feet, 260f, 64f, new Color(glow.r, glow.g, glow.b, .55f * t), 0f);
            Asset(s, "scene/plot", cx - 100f, feet - 26f, 200f, 50f, new Color(1f, 1f, 1f, t));
            if (have)
            {
                bool cheer = pick < .9f, before = GardenSkin.Celebrate;
                GardenSkin.Celebrate = cheer;
                Creature(s, e.tint, cx, feet, 210f * pop, 0, "", 0f, 12f, false, false, .03f, codexSelected * 1.3f);
                DrawHaloBubbles(s, cx, oy + 288f, 330f, 99f, t, false, true);
                GardenSkin.Celebrate = before;
            }
            else
            {
                float ah = 210f * pop * GardenSkin.ArtScale(e.tint), aw = ah * ArtAspect;
                Asset(s, PlantSlot(e.tint, "idle"), cx - aw * .5f, feet - ah * FeetLine, aw, ah, new Color(.1f, .16f, .3f, .95f * t));
                Asset(s, "scene/lock-bubble", cx - 23f, feet - 118f, 46f, 46f, new Color(1f, 1f, 1f, t));   // 12:30: lock only, no pad plate
            }
            // Name ribbon, personality and a line about the member (locked: where it joins).
            // 2026-10-17: the name has no button-like plate: a soft dark halo for contrast, big gold title, a short
            // fading underline and a small bubble on each side in the member's color.
            string title = have ? e.name : "？？？";
            float nameSize = 30f, nameW = TextWidth(title, nameSize), ny = feet + 14f;
            s.Glow(cx, ny + nameSize * .55f, nameW + 120f, 56f, new Color(.02f, .06f, .16f, .55f * t), 0f);
            TextOutline(s, title, cx, ny, nameSize, have ? Gold : Cream, Outline, 2.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                float bx = cx + side * (nameW * .5f + 20f), by = ny + nameSize * .55f + Mathf.Sin(ui * 2f + side) * 2f;
                GardenSkin.Shot(s, have ? e.tint : new Color(.5f, .58f, .75f, 1f), new Vector2(bx, by), 13f, t);
                GardenSkin.Shot(s, have ? e.tint : new Color(.5f, .58f, .75f, 1f), new Vector2(bx + side * 14f, by - 9f), 7f, .8f * t);
                for (int seg = 0; seg < 6; seg++)
                {
                    float x0 = cx + side * seg * 15f;
                    s.Rect(side > 0 ? x0 : x0 - 15f, ny + nameSize + 6f, 15f, 2f, new Color(1f, .84f, .35f, (1f - seg / 6f) * .8f * t));
                }
            }
            string[] blurb = CodexBlurb.TryGetValue(e.name, out var b) ? b : new[] { "", "" };
            if (have)
            {
                TextOutline(s, "性格 · " + blurb[0], cx, feet + 64f, 15f, Hex(0xFFE58A), Outline, 1.5f);
                TextOutline(s, blurb[1], cx, feet + 86f, 15f, Cream, Outline, 1.5f);
                TextOutline(s, "第 " + e.level + " 关加入队伍", cx, feet + 106f, 15f, Hex(0xBFD4F2), Outline, 1.2f);
            }
            else
            {
                TextOutline(s, "还没遇见它", cx, feet + 70f, 16f, Cream, Outline, 1.5f);
                TextOutline(s, "通过第 " + e.level + " 关就能获得", cx, feet + 94f, 15f, Hex(0xFFE58A), Outline, 1.5f);
            }
            // Card grid: 5 x 4, tinted by each member's color; the selected card is lifted, with its own rim turned gold (2026-10-18).
            const int cols = 5;
            const float cw = 88f, ch = 90f, gap = 8f;
            float gx = 270f - (cols * cw + (cols - 1) * gap) * .5f, gy = oy + 500f + slide;
            // 2026-10-17 10:57: no yellow rim. The selected card is a bit bigger, lifted and gently bobbing over a glow
            // in the member's own color; the others are slightly dimmed so the pick stands out.
            for (int i = 0; i < n; i++)
            {
                var c = list[i];
                bool owned = CodexObtained(c), sel = i == codexSelected;
                float bx = gx + (i % cols) * (cw + gap), by = gy + (i / cols) * (ch + gap);
                HitBox(s, CodexCardAction + i, bx, by, cw, ch);
                float grow = sel ? 1.08f + .1f * Mathf.Max(0f, 1f - pick / .25f) : 1f;
                float w2 = cw * grow, h2 = ch * grow, x = bx + (cw - w2) * .5f, y = by + (ch - h2) * .5f - (sel ? 6f + 2f * Mathf.Sin(ui * 3f) : 0f), mid = x + w2 * .5f;
                if (sel)
                {
                    // 2026-10-18 00:06 (user picked A): the blue glow vanished against the blue card rim and the navy
                    // page, so the pick now has a gold rim (the card art recoloured) over a soft gold
                    // halo; the lift, enlarge and bob stay.
                    float lit = .85f + .15f * Mathf.Sin(ui * 4f);
                    s.Glow(mid, y + h2 * .5f, w2 + 52f, h2 + 52f, new Color(1f, .78f, .24f, .62f * lit * t), 0f);
                }
                Color card = new Color(1f, 1f, 1f, t);   // 11:10: every card is white
                // 00:14 (user): no frame around the card; the selected card's own rim is gold (ui/popup-panel-gold).
                if (!(sel && Asset9(s, "ui/popup-panel-gold", x, y, w2, h2, 30f, 30f, 14f * grow, 14f * grow, card))
                    && !Asset9(s, "ui/popup-panel", x, y, w2, h2, 30f, 30f, 14f * grow, 14f * grow, card)) s.Rect(x, y, w2, h2, card);
                float f = y + 64f * grow, ah = 70f * grow;
                if (owned) Creature(s, c.tint, mid, f, ah, 0, "", 0f, 12f, false, false, sel ? .04f : 0f, i * 1.3f);
                else
                {
                    float lah = ah * GardenSkin.ArtScale(c.tint), aw = lah * ArtAspect;
                    Asset(s, PlantSlot(c.tint, "idle"), mid - aw * .5f, f - lah * FeetLine, aw, lah, new Color(.12f, .18f, .32f, .9f * t));
                    Asset(s, "scene/lock-bubble", mid - 14f * grow, y + 21f * grow, 28f * grow, 28f * grow, new Color(1f, 1f, 1f, t));   // 15:51: centred on the silhouette
                }
                TextOutline(s, owned ? c.name : "第" + c.level + "关", mid, y + h2 - 24f * grow, (owned ? 15f : 14f) * grow, owned ? Cream : Hex(0xDCE6F5), Outline, 1.5f);   // 2026-10-17: bigger (readability)
            }
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
