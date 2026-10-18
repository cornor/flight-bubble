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
            // Headline (level line above it).
            bool last = levelIndex == Levels.Length - 1;
            string head = last ? "全部关卡通过！" : "泡泡全打光啦！";
            float hp = OutBack(Clamp01(t / .35f), 2.2f), hs = 44f * hp;
            if (hs > 1f)
            {
                s.Glow(ox + 270f, oy + 168f, 420f, 110f, new Color(.02f, .06f, .2f, .45f * fade), 0f);
                TextOutline(s, "第 " + (levelIndex + 1) + " 关", ox + 270f, oy + 112f, 18f, new Color(1f, .9f, .55f, fade), new Color(Outline.r, Outline.g, Outline.b, fade), 1.6f);
                TextOutline(s, head, ox + 270f, oy + 150f + (44f - hs) * .5f, hs, Color.white, Outline, 4f * hp);
            }
            // Hero: the first new member; otherwise (17:34, user) this level's MVP: the member whose color cleared the
            // most bubbles, with a crown and the count.
            int mvpCount = 0;
            bool mvp = newMembers.Count == 0 && MvpEntry(out mvpHero, out mvpCount);
            CodexEntry hero = newMembers.Count > 0 ? newMembers[0] : mvp ? mvpHero : WinHero();
            float cx = ox + 270f, feet = oy + 470f;
            DrawBubbleHalo(s, cx, oy + 372f, 380f, fade);
            DrawHaloBubbles(s, cx, oy + 372f, 380f, t - .1f, fade, true, false);
            s.Glow(cx, feet, 220f, 50f, new Color(hero.tint.r, hero.tint.g, hero.tint.b, .5f * fade), 0f);
            float hop = t - .1f;
            if (hop > 0f)
            {
                float scale = hop < .28f ? Mathf.Lerp(.3f, 1.1f, OutBack(hop / .28f, 1.7f)) : hop < .44f ? Mathf.Lerp(1.1f, 1f, (hop - .28f) / .16f) : 1f;
                bool before = GardenSkin.Celebrate;
                GardenSkin.Celebrate = true;
                Creature(s, hero.tint, cx, feet, 230f * scale, 0, "", 0f, 12f, false, false, .04f, 0f);
                GardenSkin.Celebrate = before;
            }
            DrawHaloBubbles(s, cx, oy + 372f, 380f, t - .1f, fade, true, true);
            if (mvp && hop > .44f)
            {
                float c = OutBack(Clamp01((hop - .44f) / .3f), 2.6f), bob = Mathf.Sin(ui * 3f) * 3f;
                float cy = feet - 230f * (212f - 62f) / 256f - 26f + bob;
                s.Glow(cx, cy, 120f * c, 70f * c, new Color(1f, .85f, .35f, .45f * fade), 0f);
                Asset(s, "scene/crown", cx - 30f * c, cy - 26f * c, 60f * c, 45f * c, new Color(1f, 1f, 1f, fade));
                if (c > .3f) TextOutline(s, "MVP", cx, cy + 14f * c, 20f * c, A(Gold, fade), A(Outline, fade), 2f * c);
            }
            // New members join the squad: a short caption and their avatars in bubbles (11:39: no names).
            float y = oy + 494f;
            if (newMembers.Count > 0 && t > .5f)
            {
                float a = Clamp01((t - .5f) / .25f);
                TextOutline(s, "新伙伴入队！", cx, y, 17f, new Color(1f, .9f, .5f, a), new Color(Outline.r, Outline.g, Outline.b, a), 1.6f);
                DrawMemberAvatars(s, cx, y + 58f, newMembers, t - .6f);
                y += 104f;
            }
            else if (mvp && t > .5f)
            {
                float a = Clamp01((t - .5f) / .25f);
                if (mvpLine.Length == 0 || mvpLineCount != mvpCount) { mvpLineCount = mvpCount; mvpLine = "打掉 " + mvpCount + " 颗泡泡"; }
                float w1 = TextWidth("本关 MVP", 20f), w2 = TextWidth(mvpLine, 18f), x0 = cx - (w1 + 12f + w2) * .5f;
                TextOutline(s, "本关 MVP", x0, y + 4f, 20f, A(Gold, a), A(Outline, a), 1.8f, false);
                TextOutline(s, mvpLine, x0 + w1 + 12f, y + 6f, 18f, A(Color.white, a), A(Outline, a), 1.6f, false);
                y += 44f;
            }
            else y += 16f;
            // One line of results.
            if (t > .65f)
            {
                float a = Clamp01((t - .65f) / .25f);
                string combo = "最高连击 " + Battle.BestCombo, time = "用时 " + ResultRound(Battle.Time) + " 秒";
                const float size = 17f;
                float w1 = TextWidth(combo, size) + 36f, w2 = TextWidth(time, size) + 36f, gap = 14f, x0 = cx - (w1 + w2 + gap) * .5f;
                Color pill = new Color(.06f, .14f, .32f, .75f * a);
                foreach (var (text, x, w) in new[] { (combo, x0, w1), (time, x0 + w1 + gap, w2) })
                {
                    if (!Asset9(s, "scene/badge-ammo", x, y, w, 34f, 14f, 0f, 17f, 0f, new Color(1f, 1f, 1f, .9f * a))) s.Rect(x, y, w, 34f, pill);
                    TextOutline(s, text, x + w * .5f, y + 8f, size, new Color(1f, 1f, 1f, a), new Color(Outline.r, Outline.g, Outline.b, a), 1.4f);
                }
            }
            s.MapLayout = mapped;
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
