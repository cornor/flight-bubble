#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-17 victory screen, after the lightup reference ("有氛围感"): no panel. Over the dimmed battle a big
    // outlined headline, a squad member hopping in to cheer inside slowly turning rays and a warm glow, paper
    // confetti in the squad colors falling and turning, a few soap bubbles drifting up, new members joining, and
    // one line of results (best combo, time). The prefab's 下 一 关 / 返回主页 buttons stay below (y 650 / 740).
    // No coins or fragments for now.
    public partial class PixelWingGame
    {
        float winShownAt = -1f;
        Flightbeat.Model.Battle winBattle;
        static readonly int[] ConfettiTints = { 0x2982F4, 0xFF7E27, 0xFFDF48, 0x62E18E, 0xBD80FF, 0xFFC5E0, 0x46D9FF, 0xFFFFFF };

        void DrawWinScreen(PixelSurface s)
        {
            if (Battle == null || Battle.State != Phase.Won || !(feedback.EndAge > .48f)) { winShownAt = -1f; return; }
            if (winBattle != Battle || winShownAt < 0f) { winBattle = Battle; winShownAt = ui; }
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            GardenSkin.Clock = ui;
            float L = ScreenLayout.Left, W = ScreenLayout.Width, H = ScreenLayout.Height, ox = L, oy = (H - 960f) * .5f;
            float t = ui - winShownAt, fade = Clamp01(t / .3f);
            // Confetti behind everything else: two waves, pieces flutter (width follows the turn).
            for (int wave = 0; wave < 2; wave++)
                for (int k = 0; k < 34; k++)
                {
                    float start = wave * .9f + (k * 37 % 19) * .02f, life = 3.2f + (k * 13 % 10) * .2f, u = (t - start) / life;
                    if (u < 0f || u >= 1f) continue;
                    float x = L + (k * 71 % 100) / 100f * W + Mathf.Sin(u * 6f + k) * 26f + ((k * 29 % 11) - 5) * 12f * u;
                    float py = -30f + u * (H + 80f) - (k * 53 % 9) * 20f;
                    float turn = Mathf.Cos(ui * (3f + k % 4) + k), w = (k % 2 == 0 ? 14f : 8f) * Mathf.Abs(turn) + 1.5f, h = k % 2 == 0 ? 8f : 14f;
                    Color c = Hex(ConfettiTints[(k + wave * 3) % ConfettiTints.Length]);
                    c.a = Clamp01((1f - u) / .2f) * fade;
                    if (turn < 0f) { c.r *= .82f; c.g *= .82f; c.b *= .82f; }   // the back of the paper is a shade darker
                    s.Rect(x - w * .5f, py - h * .5f, w, h, c);
                }
            // ---- 2026-10-21 victory page (user, after a reference; mock-up approved): a ribbon with 挑战成功, the level's
            // own picture rebuilt bead by bead in a white frame inside turning rays, the MVP / new member cheering on the
            // frame's lower right corner, a purple glass line about the next new member, two small stat pills; the
            // prefab's 主页 · 下一关 · 重玩 row sits below (y 772). Painted ribbon when delivered (win/ribbon.png).
            bool last = levelIndex == Levels.Length - 1;
            float cx = ox + 270f;
            const float S = 310f, F = 12f;   // 16:32: 330 -> 310, room for the members' row
            float cardTop = oy + 200f, cardCY = cardTop + F + S * .5f, cardBottom = cardTop + S + F * 2f;
            // 15:55: a soft blue-white spotlight behind the card over the deep indigo shade (GardenBattlePage.WinShade).
            s.Glow(cx, cardCY, 720f, 780f, new Color(.36f, .48f, .9f, .22f * fade), 0f);
            float cardK = OutBack(Clamp01((t - .12f) / .35f), 1.6f);
            if (cardK > .01f) WinRays(s, cx, cardCY, t, fade * Clamp01(cardK));
            // 16:20 (user): no 「第 N 关」 line above the ribbon.
            if (cardK > .01f) WinPicture(s, cx, cardCY, S * cardK, F * cardK, Clamp01((t - .4f) / .6f), fade);
            // Twinkles around the frame.
            for (int i = 0; i < 4 && cardK > .9f; i++)
            {
                float ph = (ui * .9f + i * .37f) % 1f, a = Mathf.Sin(ph * Mathf.PI) * fade, r = (i % 2 == 0 ? 12f : 7f) * (.6f + .4f * a);
                Vector2 p = new Vector2(cx + (i < 2 ? -1f : 1f) * (S * .5f + 26f - (i % 2) * 14f), cardTop + 60f + i * 83f % (S - 40f));
                // 16:20: a soft four-point glint (two tapered beams and a glow), not a thin 「+」.
                s.Glow(p.x, p.y, r * 2.6f, r * 2.6f, new Color(1f, .95f, .7f, .45f * a), 0f);
                s.Glow(p.x, p.y, r * 4.2f, r * .55f, new Color(1f, 1f, 1f, a), 0f);
                s.Glow(p.x, p.y, r * .55f, r * 4.2f, new Color(1f, 1f, 1f, a), 0f);
                s.Glow(p.x, p.y, r * .9f, r * .9f, new Color(1f, 1f, 1f, a), 0f);
            }
            // 16:32 (user): the row under the card just shows the members themselves, no capsules: each stands on a soft
            // floor light with its tag above the head: the MVP with a crown and 「MVP」, a new member with a 「新成员」
            // chip, and the next newcomer as the rainbow bubble with 「N 关后加入」 (N <= 5). One to three, centred.
            int mvpCount = 0;
            bool mvp = MvpEntry(out mvpHero, out mvpCount);
            if (WinRibbon(s, cx, oy + 150f, OutBack(Clamp01(t / .35f), 2.2f), last ? "全部通关！" : "挑战成功", fade)) { }
            int nextIn = 0;
            foreach (var e in Codex)
                if (e.level > levelIndex + 1) { int d = e.level - (levelIndex + 1); if (nextIn == 0 || d < nextIn) nextIn = d; }
            winRow.Clear();
            if (mvp)
            {
                bool alsoNew = false;
                foreach (var m in newMembers) if (m.tint == mvpHero.tint) alsoNew = true;
                winRow.Add((mvpHero.tint, 1 | (alsoNew ? 2 : 0), 0));
            }
            // 16:38 (user: this level's newcomer was missing): newMembers is filled only on a level's first clear, so a
            // replay showed nobody. The members debuting on this level now always stand in the row: 「新成员」 on the first
            // clear, 「本关登场」 (tag 8) on a replay.
            bool firstClear = newMembers.Count > 0;
            winDebut.Clear();
            foreach (var e in Codex) if (e.level == levelIndex + 1) winDebut.Add(e);
            foreach (var m in winDebut)
                if (winRow.Count < 3 && !(mvp && m.tint == mvpHero.tint)) winRow.Add((m.tint, firstClear ? 2 : 8, 0));
            if (winRow.Count > 0 && mvp) foreach (var m in winDebut) if (m.tint == mvpHero.tint) winRow[0] = (winRow[0].tint, 1 | (firstClear ? 2 : 0), 0);
            if (winDebut.Count == 0 && nextIn > 0 && nextIn <= 5 && winRow.Count < 3) winRow.Add((Color.clear, 4, nextIn));
            float feet = cardBottom + 112f, step = 124f;   // tags clear the frame; 16:38 stats at feet + 30 (room under the feet)
            float groupMin = float.MaxValue, groupMax = float.MinValue, groupTop = float.MaxValue; int groupLast = -1; bool groupNew = false;
            for (int i = 0; i < winRow.Count; i++)
            {
                float mx = cx + (i - (winRow.Count - 1) * .5f) * step;
                WinMember(s, mx, feet, t - (.9f + i * .12f), fade, winRow[i].tint, winRow[i].tags, winRow[i].n);
                if ((winRow[i].tags & (2 | 8)) != 0)
                {
                    groupMin = Mathf.Min(groupMin, mx); groupMax = Mathf.Max(groupMax, mx); groupLast = i; groupNew |= (winRow[i].tags & 2) != 0;
                    // 16:52 (user: the chip covered the crown): the chip goes above the highest head / crown of the group.
                    float head = feet - 96f * ArtScale(winRow[i].tint) * (FeetLine - BodyTop) - ((winRow[i].tags & 1) != 0 ? 17f : 0f);
                    groupTop = Mathf.Min(groupTop, head);
                }
            }
            if (groupLast >= 0)   // one chip over the newcomers: 新伙伴加入！ (first clear) or 本关登场 (replay)
            {
                float age = t - (.9f + groupLast * .12f) - .25f, tagA = Clamp01(age / .15f) * fade, tp = OutBack(Clamp01(age / .25f), 2.4f);
                if (tagA > 0f)
                {
                    string w = groupNew ? "新伙伴加入!" : "本关登场";   // narrow "!" keeps the chip text centred
                    float gy = Mathf.Max(groupTop - 22f, cardBottom + 17f);   // 16:53 (user): still, no bobbing
                    GardenBars.Bar(s, (groupMin + groupMax) * .5f, gy, (GardenBarArt.TextWidth(w, 16f) + 34f) * tp, 28f * tp,
                        GardenBars.Sky, null, w, 16f * tp, GardenBars.Navy, tagA);   // 16:47 (user): the quiet sky glass, not pink
                }
            }
            float y = winRow.Count > 0 ? feet + 28f : cardBottom + 60f;
            if (t > 1.1f)
            {
                float a = Clamp01((t - 1.1f) / .2f) * fade;
                WinStat(s, cx - 8f - 78f, y, a, true, "最高连击", Battle.BestCombo.ToString());
                WinStat(s, cx + 8f + 78f, y, a, false, "用时", ResultRound(Battle.Time) + " 秒");
            }
            s.MapLayout = mapped;
        }
        static readonly Color winInk = Hex(0x1D2F63), winRibbonInk = Hex(0x8E2218);
        // Warm rays turning slowly behind the card, with a glow.
        void WinRays(PixelSurface s, float cx, float cy, float t, float a)
        {
            s.Glow(cx, cy, 560f, 560f, new Color(1f, .88f, .5f, .4f * a), 0f);
            for (int i = 0; i < 18; i++)
            {
                float ang = i / 18f * Mathf.PI * 2f + ui * .12f, r = (i % 2 == 0 ? 340f : 270f);
                s.GlowLine(new Vector2(cx, cy), new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r), i % 2 == 0 ? 46f : 30f,
                    new Color(1f, .86f, .45f, (i % 2 == 0 ? .26f : .18f) * a));
            }
        }
        // White rounded frame with a soft shadow, and the level's picture rebuilt from its beads (the battle's own bead
        // art), rows lighting up from the bottom (`reveal` 0..1), each new row popping in.
        void WinPicture(PixelSurface s, float cx, float cy, float size, float frame, float reveal, float a)
        {
            float w = size + frame * 2f, x0 = cx - w * .5f, y0 = cy - w * .5f, r = 16f * w / (330f + 24f);
            s.Glow(cx, cy + 10f, w * 1.15f, w * 1.15f, new Color(0f, 0f, 0f, .35f * a), 0f);
            WinRoundRect(s, x0, y0, w, w, r, new Color(1f, 1f, 1f, a));
            WinRoundRect(s, x0 + 4f, y0 + 4f, w - 8f, w - 8f, r - 3f, new Color(.81f, .89f, .96f, a));
            WinRoundRect(s, x0 + 6f, y0 + 6f, w - 12f, w - 12f, r - 4f, new Color(1f, 1f, 1f, a));
            var level = Battle.Level;
            int cols = level.columns > 0 ? level.columns : 60, rows = level.rows > 0 ? level.rows : 60;
            float left = cx - size * .5f, top = cy - size * .5f, cell = size / cols;
            s.Rect(left, top, size, size, new Color(.17f, .37f, .66f, a));
            int[] cells = level.cells, pal = level.palette;
            if (cells == null || pal == null) return;
            // 16:20 (user): the picture's background colour (the commonest colour on its outer ring) is softened towards
            // white so the subject stands out.
            int bg = WinBackgroundColour(cells, cols, rows);
            float shown = reveal * (rows + 6f);
            for (int i = 0; i < cells.Length && i < cols * rows; i++)
            {
                int v = cells[i];
                if (v < 0 || v * 3 + 2 >= pal.Length) continue;
                // 15:51 (user: the picture was upside down): cells run from the top row down.
                int col = i % cols, rowFromBottom = rows - 1 - i / cols;
                float grow = rowFromBottom < shown - 6f ? 1f : Clamp01((shown - rowFromBottom) / 6f);
                if (grow <= 0f) continue;
                Color tint = new Color32((byte)pal[v * 3], (byte)pal[v * 3 + 1], (byte)pal[v * 3 + 2], 255);
                if (v == bg) tint = Color.Lerp(tint, Color.white, .5f);
                tint.a = a;
                float d = cell * 1.16f * (.5f + .5f * OutBack(grow, 2f));
                GardenSkin.Bug(s, tint, new Vector2(left + (col + .5f) * cell, top + (rows - rowFromBottom - .5f) * cell), d, false, d);
            }
        }
        int[] winBgCells; int winBg = -1;
        int WinBackgroundColour(int[] cells, int cols, int rows)
        {
            if (ReferenceEquals(cells, winBgCells)) return winBg;
            winBgCells = cells; winBg = -1;
            var count = new System.Collections.Generic.Dictionary<int, int>();
            for (int i = 0; i < cells.Length && i < cols * rows; i++)
            {
                int c = i % cols, r = i / cols;
                if (c != 0 && c != cols - 1 && r != 0 && r != rows - 1) continue;
                int v = cells[i]; if (v < 0) continue;
                count.TryGetValue(v, out int k); count[v] = k + 1;
            }
            int best = 0;
            foreach (var kv in count) if (kv.Value > best) { best = kv.Value; winBg = kv.Key; }
            if (best < (cols + rows)) winBg = -1;   // only when it really frames the picture (over a quarter of the ring)
            return winBg;
        }
        static void WinRoundRect(PixelSurface s, float x, float y, float w, float h, float r, Color c)
        {
            // 16:20 (user: spiky corners): the rounded ends are exact quarter circles built from 1 px rows (one rect
            // per row, no overlaps), so even see-through fills stay clean.
            r = Mathf.Max(1f, Mathf.Min(r, Mathf.Min(w, h) * .5f));
            s.Rect(x, y + r, w, h - r * 2f, c);
            int n = Mathf.CeilToInt(r);
            for (int i = 0; i < n; i++)
            {
                float hRow = Mathf.Min(1f, r - i), dy = r - (i + hRow * .5f), inset = r - Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));
                s.Rect(x + inset, y + i, w - inset * 2f, hRow, c);
                s.Rect(x + inset, y + h - i - hRow, w - inset * 2f, hRow, c);
            }
        }
        // The ribbon: painted art when delivered (win/ribbon.png, words drawn here), else a coral glass bar.
        bool WinRibbon(PixelSurface s, float cx, float cy, float p, string title, float a)
        {
            if (p <= .01f) return false;
            bool art = Has("plants/win/ribbon");
            if (art) Asset(s, "plants/win/ribbon", cx - 200f * p, cy - 56f * p, 400f * p, 112f * p, new Color(1f, 1f, 1f, a));
            else GardenBars.Capsule(s, cx, cy, 360f * p, 64f * p, Hex(0xF0644C), a);
            // The delivered ribbon's band sits in the upper third of its canvas (rows 10-139 of 224 at the middle), its
            // tails below: the title is centred on the band, 19 px above the slot centre.
            float size = 42f * p;
            GardenBarArt.TextOutline(s, title, cx, cy - (art ? 19f : 4f) * p - size * .55f, size, new Color(1f, 1f, 1f, a), A(winRibbonInk, a), 3.6f * p);
            return true;
        }
        readonly System.Collections.Generic.List<CodexEntry> winDebut = new System.Collections.Generic.List<CodexEntry>();
        readonly System.Collections.Generic.List<(Color tint, int tags, int n)> winRow = new System.Collections.Generic.List<(Color tint, int tags, int n)>();
        static readonly Color winNewEdge = Hex(0xFF6FAE), winNewInk = Hex(0xB0329B);
        // One member of the row: `tags` 1 = MVP (crown + 「MVP」), 2 = new member (「新成员」 chip; a small NEW star when it
        // is also the MVP), 4 = the next newcomer (the rainbow bubble, 「n 关后加入」). It hops in at `age` 0, lands
        // with a few glints and keeps cheering.
        void WinMember(PixelSurface s, float x, float feet, float age, float fade, Color tint, int tags, int n)
        {
            if (age <= 0f) return;
            float a = Clamp01(age / .12f) * fade, p = OutBack(Clamp01(age / .32f), 1.8f), drop = age < .2f ? (1f - age / .2f) * 26f : 0f;
            const float canvas = 96f;
            // 16:38: a clearer floor light (was hard to see on the dark shade).
            s.Glow(x, feet + 2f, 110f * p, 22f * p, new Color(1f, .95f, .8f, .55f * a), 0f);
            s.Glow(x, feet + 1f, 70f * p, 12f * p, new Color(1f, 1f, 1f, .45f * a), 0f);
            float headTop;
            if ((tags & 4) != 0)
            {
                float ah = canvas * p, aw = ah * ArtAspect, bob = Mathf.Sin(ui * 2.4f) * 2f;
                Asset(s, "plants/mystery-bubble", x - aw * .5f, feet - drop + bob - ah * FeetLine, aw, ah, new Color(1f, 1f, 1f, a));
                headTop = feet - drop + bob - ah * (FeetLine - 71f / 256f);
            }
            else
            {
                bool before = GardenSkin.Celebrate; GardenSkin.Celebrate = true;
                Creature(s, tint, x, feet - drop, canvas * p, 0, "", 0f, 12f, false, false, .04f, x * .013f);
                GardenSkin.Celebrate = before;
                headTop = feet - drop - canvas * p * ArtScale(tint) * (FeetLine - BodyTop);
            }
            if (age > .15f && age < .7f)   // glints as it lands
                for (int k = 0; k < 4; k++)
                {
                    float u = Clamp01((age - .15f) / .55f), ang = k * 1.57f + .6f, d = 28f + 26f * u, r = 5f * (1f - u);
                    Vector2 sp = new Vector2(x + Mathf.Cos(ang) * d, feet - 30f + Mathf.Sin(ang) * d * .7f);
                    s.Glow(sp.x, sp.y, r * 4f, r * .6f, new Color(1f, 1f, 1f, a), 0f);
                    s.Glow(sp.x, sp.y, r * .6f, r * 4f, new Color(1f, 1f, 1f, a), 0f);
                }
            float tagA = Clamp01((age - .25f) / .15f) * fade, tp = OutBack(Clamp01((age - .25f) / .25f), 2.4f);
            if (tagA <= 0f) return;
            // 16:42 (user review): every tag sits on one shared line above the row (heads differ in height, so tags
            // following each head looked ragged); only the crown stays on the MVP's head. An MVP that is also new gets a
            // small pink 「新」 bubble after 「MVP」 instead of a NEW chip over its face.
            float tagY = feet - canvas * (FeetLine - BodyTop) - 34f, bob2 = 0f;   // 16:53: tags stay still
            // 16:45 (user): the MVP keeps only its crown (no 「MVP」 word); new / debuting members get no tag of their own:
            // one 「新伙伴加入！」 / 「本关登场」 chip is drawn over the whole group (DrawWinScreen).
            if ((tags & 1) != 0)   // 16:52: the crown sits down on the head (it rose into the group chip)
                Asset(s, "scene/crown", x - 19f * tp, headTop - 17f * tp, 38f * tp, 28f * tp, new Color(1f, 1f, 1f, tagA));
            if ((tags & 4) != 0)
            {
                string w = n + " 关后加入";
                GardenBars.Bar(s, x, tagY + bob2, (GardenBarArt.TextWidth(w, 15f) + 26f) * tp, 26f * tp, GardenBars.Purple, null, w, 15f * tp, GardenBars.DeepPurple, tagA);
            }
        }
        // Purple glass line: [rainbow bubble] 再过 [2] 关，新伙伴加入！ (or a star and 新伙伴加入小队！ on the level it joins).
        void WinPartnerLine(PixelSurface s, float cx, float cy, float p, float a, string pre, string num, string post, bool joined)
        {
            float h = 50f * p, w = 350f * p;
            GardenBars.Capsule(s, cx, cy, w, h, GardenBars.Purple, a);
            float sPre = 18f * p, sNum = 30f * p, sPost = 20f * p, icon = 42f * p, gap = 6f * p;
            float wPre = pre.Length > 0 ? GardenBarArt.TextWidth(pre, sPre) + gap : 0f, wNum = num.Length > 0 ? GardenBarArt.TextWidth(num, sNum) + gap : 0f;
            float wPost = GardenBarArt.TextWidth(post, sPost), total = icon + gap + wPre + wNum + wPost, x = cx - total * .5f;
            if (!joined && Has("plants/mystery-bubble"))
            {
                float q = icon / 141f;
                Asset(s, "plants/mystery-bubble", x + icon * .5f - 112f * q, cy - 141.5f * q - 2f, 224f * q, 256f * q, new Color(1f, 1f, 1f, a));
            }
            else GardenBars.Icon(s, "star", x + icon * .5f, cy, icon * .9f, a);
            x += icon + gap;
            Color ink = A(GardenBars.DeepPurple, a);
            if (pre.Length > 0) { GardenBarArt.Text(s, pre, x, cy - sPre * .55f, sPre, false, ink); x += wPre; }
            if (num.Length > 0) { GardenBarArt.TextOutline(s, num, x, cy - 2f - sNum * .55f, sNum, A(Gold, a), ink, 2.4f * p, false); x += wNum; }
            GardenBarArt.Text(s, post, x, cy - sPost * .55f, sPost, false, A(Hex(0xB0329B), a));
        }
        // Dark see-through pill: [flame / clock] label  NUMBER.
        void WinStat(PixelSurface s, float cx, float cy, float a, bool combo, string label, string value)
        {
            const float w = 156f, h = 30f;   // 16:38: a little smaller and lighter, quieter than the members
            // 15:51: the navy number-pill art (the round ends drawn from segments showed spiky overlaps when see-through).
            if (!GardenBarArt.Plate(s, "number-pill", cx, cy, w, h, Color.white, .72f * a))
                s.Rect(cx - w * .5f, cy - h * .5f, w, h, new Color(.04f, .1f, .24f, .62f * a));
            float ix = cx - w * .5f + 25f;   // 16:20: icon inside the round end, not on it
            if (combo) GardenBars.Icon(s, "flame", ix, cy, 25f, a);
            else if (Has("plants/win/icon-clock")) Asset(s, "plants/win/icon-clock", ix - 12.5f, cy - 12.5f, 25f, 25f, new Color(1f, 1f, 1f, a));
            else
            {
                s.Ring(new Vector2(ix, cy), 5.5f, new Color(1f, 1f, 1f, a), 11f, 16, 0f, 360f);
                s.Ring(new Vector2(ix, cy), 11f, new Color(.17f, .48f, .9f, a), 2.5f, 20, 0f, 360f);
                s.Segment(new Vector2(ix, cy), new Vector2(ix, cy - 6.5f), 2.2f, A(winInk, a));
                s.Segment(new Vector2(ix, cy), new Vector2(ix + 5f, cy + 2f), 2.2f, A(winInk, a));
            }
            float sl = 13f, sv = 17f, wl = GardenBarArt.TextWidth(label, sl), wv = GardenBarArt.TextWidth(value, sv), x = cx + 16f - (wl + 6f + wv) * .5f;
            GardenBarArt.Text(s, label, x, cy - sl * .55f, sl, false, new Color(.66f, .83f, 1f, a));
            GardenBarArt.TextOutline(s, value, x + wl + 6f, cy - sv * .55f, sv, new Color(1f, 1f, 1f, a), A(winInk, a), 1.6f, false);
        }
        // Light behind a showcased member (victory screen, codex): the H14 light with its big bubbles removed at pack
        // time (home/halo), slowly turning and breathing; a soft cyan-white glow until it is packed.
        void DrawBubbleHalo(PixelSurface s, float cx, float cy, float size, float alpha)
        {
            if (Has("home/halo"))
            {
                float breathe = 1f + Mathf.Sin(ui * 1.6f) * .025f;
                GardenSkin.Rotated(s, "home/halo", cx, cy, size * 1.15f * breathe, ui * .12f, new Color(1f, 1f, 1f, alpha));
                return;
            }
            float pulse = Mathf.Sin(ui * 2f) * .5f + .5f;
            s.Glow(cx, cy, size * .95f, size * .85f, new Color(.72f, .9f, 1f, (.45f + .1f * pulse) * alpha), 0f);
            s.Glow(cx, cy, size * .55f, size * .5f, new Color(1f, 1f, 1f, (.35f + .1f * pulse) * alpha), 0f);
        }

        // 2026-10-17 12:19: bubbles that live in the light instead of a fixed ring. An opening burst from behind the
        // member (victory only), then bubbles keep rising out of the light at the member's feet, swaying, in three
        // sizes (small = far: paler and slower), each faintly tinted; some fade out up high, others pop with the H13
        // frames; now and then one glints. `front` draws only the big near ones, after the member.
        static readonly Color[] BubbleTints = { new Color(1f, .93f, .97f), new Color(.9f, 1f, 1f), new Color(1f, .97f, .86f), Color.white, new Color(.95f, .92f, 1f) };
        void DrawHaloBubbles(PixelSurface s, float cx, float cy, float size, float shown, float alpha, bool lively, bool front)
        {
            float k = size / 380f;
            if (lively && !front)
                for (int i = 0; i < 14; i++)
                {
                    float life = 1f + (i * 7 % 5) * .1f, u = shown / life;
                    if (u < 0f || u >= 1f) continue;
                    float ang = i / 14f * Mathf.PI * 2f + (i * 13 % 7) * .09f, v = (240f + (i * 37 % 5) * 30f) * k;
                    float dist = v * (1f - Mathf.Exp(-3f * shown)) / 3f;
                    Vector2 c = new Vector2(cx + Mathf.Cos(ang) * dist, cy + Mathf.Sin(ang) * dist * .85f - 20f * k);
                    float d = (i % 3 == 0 ? 46f : i % 3 == 1 ? 32f : 22f) * k;
                    if (i % 3 == 0 && u > .82f) { SoapPop(s, c, d * .5f, (u - .82f) / .18f, alpha); continue; }
                    SoapBubble(s, c, d, BubbleTints[i % BubbleTints.Length], alpha * Mathf.Clamp01((1f - u) / .25f), i);
                }
            int streams = lively ? 9 : 6;
            for (int j = 0; j < streams; j++)
            {
                int tier = j % 3;                                   // 0 big (near), 1 mid, 2 small (far)
                if (front != (tier == 0)) continue;
                float period = (lively ? 3.2f : 4.4f) + (j * 7 % 5) * .45f, u = ((ui + j * 1.37f) % period) / period;
                float side = (j * 5 % 7) / 6f - .5f;                // -0.5 .. 0.5 across the light
                float x = cx + side * size * .7f + Mathf.Sin(u * 7f + j) * 12f * k + side * u * 60f * k;
                float y = cy + size * .26f - u * size * (tier == 2 ? .7f : .85f);
                if (tier == 0 && Mathf.Abs(x - cx) < size * .2f) x += Mathf.Sign(side == 0f ? 1f : side) * size * .2f;   // keep big ones off the face
                float d = (tier == 0 ? 40f : tier == 1 ? 28f : 17f) * k * (1f + (j * 3 % 4) * .08f);
                float a = alpha * (tier == 2 ? .6f : 1f) * Mathf.Clamp01(u / .12f);
                bool pops = j % 2 == 0;
                if (pops && u > .86f) { SoapPop(s, new Vector2(x, y), d * .5f, (u - .86f) / .14f, alpha); continue; }
                if (!pops) a *= Mathf.Clamp01((1f - u) / .22f);
                SoapBubble(s, new Vector2(x, y), d, BubbleTints[j % BubbleTints.Length], a, j);
                if (((int)(ui * 1.3f) + j) % 6 == 0 && tier != 2)   // an occasional glint on the highlight
                {
                    float g = Mathf.Sin((ui * 1.3f % 1f) * Mathf.PI);
                    s.Glow(x - d * .22f, y - d * .22f, d * .5f * g, d * .5f * g, new Color(1f, 1f, 1f, .9f * a * g), 0f);
                }
            }
        }
        void SoapBubble(PixelSurface s, Vector2 c, float d, Color tint, float alpha, int seed)
        {
            if (alpha <= .01f) return;
            float wob = Mathf.Sin(ui * 5f + seed * 1.9f) * .04f;
            tint.a = alpha;
            if (!Asset(s, "plants/home/soap-bubble", c.x - d * (1f + wob) * .5f, c.y - d * (1f - wob) * .5f, d * (1f + wob), d * (1f - wob), tint))
                GardenSkin.Shot(s, Hex(0xCFEAFF), c, d * .5f, .6f * alpha);
        }
        void SoapPop(PixelSurface s, Vector2 c, float r, float p, float alpha)
        {
            if (Has("plants/home/soap-pop/0"))
            {
                int last = 0;
                while (Has("plants/home/soap-pop/" + (last + 1))) last++;
                int f = Mathf.Min(last, (int)(Mathf.Clamp01(p) * (last + 1)));
                Asset(s, "plants/home/soap-pop/" + f, c.x - r * 1.3f, c.y - r * 1.3f, r * 2.6f, r * 2.6f, new Color(1f, 1f, 1f, alpha));
            }
            else GardenFx.Burst(s, "rocket-pop", c, r * 2.6f, p * .4f, .4f);
        }
        // New members (12:16: not inside bubbles, they were hard to see): each pops up on a small glow in its color,
        // overshoots, settles, and a few sparkles flash at its feet.
        void DrawMemberAvatars(PixelSurface s, float cx, float y, System.Collections.Generic.List<CodexEntry> members, float t)
        {
            int n = members.Count;
            const float step = 78f, h = 64f;
            for (int i = 0; i < n; i++)
            {
                float a = t - i * .15f;
                if (a < 0f) continue;
                float k = OutBack(Clamp01(a / .35f), 2.2f), x = cx + (i - (n - 1) * .5f) * step, feet = y + h * .42f;
                if (k < .02f) continue;
                Color tint = members[i].tint;
                s.Glow(x, feet - h * .3f, h * 1.3f, h * 1.1f, new Color(tint.r, tint.g, tint.b, .45f * Clamp01(a / .3f)), 0f);
                s.Glow(x, feet, h * 1.1f, h * .3f, new Color(1f, .95f, .75f, .6f * Clamp01(a / .3f)), 0f);
                Creature(s, tint, x, feet, h * 1.15f * k, 0, "", 0f, 12f, false, false, .04f, i * 1.7f);
                float sp = (a - .3f) / .4f;
                if (sp > 0f && sp < 1f)
                    for (int j = 0; j < 4; j++)
                    {
                        float ang = (200f + j * 45f) * Mathf.Deg2Rad, r = 10f + 26f * sp;
                        float gs = 9f * (1f - sp);
                        s.Glow(x + Mathf.Cos(ang) * r * 1.4f, feet - 4f + Mathf.Sin(ang) * r * .5f, gs, gs, new Color(1f, .95f, .7f, 1f - sp), 0f);
                    }
            }
        }
        // ---- MVP (17:34): bubbles cleared per color during the battle. Every member fires at its own color, so the
        // color that lost the most bubbles is the member that did the most work (bomb and finale hits included).
        Flightbeat.Model.Battle mvpBattle;
        bool[] mvpAlive;
        int[] mvpKills;
        int mvpRemaining = -1, mvpLineCount = -1;
        string mvpLine = "";
        CodexEntry mvpHero;
        void TrackMvpKills()
        {
            if (Battle == null) return;
            if (mvpBattle != Battle)
            {
                mvpBattle = Battle; mvpRemaining = -1;
                mvpAlive = (bool[])Battle.Alive.Clone();
                mvpKills = new int[Mathf.Max(1, Battle.Level.palette.Length / 3)];
            }
            if (Battle.Remaining == mvpRemaining || mvpAlive.Length != Battle.Alive.Length) return;
            mvpRemaining = Battle.Remaining;
            for (int i = 0; i < mvpAlive.Length; i++)
                if (mvpAlive[i] && !Battle.Alive[i])
                {
                    mvpAlive[i] = false;
                    int c = Battle.ColorAt(i);
                    if (c >= 0 && c < mvpKills.Length) mvpKills[c]++;
                }
        }
        bool MvpEntry(out CodexEntry entry, out int count)
        {
            entry = default; count = 0;
            if (mvpKills == null || mvpBattle != Battle) return false;
            int best = -1;
            for (int c = 0; c < mvpKills.Length; c++) if (mvpKills[c] > count) { count = mvpKills[c]; best = c; }
            if (best < 0) return false;
            var p = Battle.Level.palette;
            string name = GardenColorCatalog.Name((p[best * 3] << 16) | (p[best * 3 + 1] << 8) | p[best * 3 + 2]);
            foreach (var e in Codex) if (e.name == name) { entry = e; return true; }
            return false;
        }
        CodexEntry WinHero()
        {
            var list = Codex;
            int got = 0;
            foreach (var e in list) if (CodexObtained(e)) got++;
            if (got == 0) return list[0];
            int pick = levelIndex % got;
            foreach (var e in list)
                if (CodexObtained(e) && pick-- == 0) return e;
            return list[0];
        }
    }
}
#endif
