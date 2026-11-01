#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-31 16:52 (user: 转场做成沙画那种"一个小水桶人冲"的感觉; going home only the sweep). Drawn over everything
    // on the overlay. A diagonal sweep lays a blue radial plate with a drifting pattern of faint bubbles and stars over
    // the screen; going into a level (开始游戏 / 重新开始 / 下一关) 啵啵 dashes in from the lower left, hops on the spot in its pot
    // (17:19: no legs - bounce, squash and stretch, arms flung up, speed lines, landing puffs, popping stars) above 「第 N 关」, then leaps out to the upper
    // right as the sweep uncovers the level. 2026-11-01: the in-game 啵啵 swapping its idle / cheer poses (the rigged parts
    // in Art/PlushToy/DashTransition-20261031 are no longer used). Timing: covered by .42 s (where the swap happens, DeckFlow),
    // the level transition ends at 1.97 s, going home at .72 s.
    public partial class PixelWingGame
    {
        internal const float HomeTransitionEnd = .72f, LevelTransitionEnd = 1.97f;
        static readonly Color DashBase = Hex(0x2F86FF), DashGlow = Hex(0x5BE3FF);
        // 2026-11-01 08:55 (user picked E: the rigged arms never looked attached, A-D all wrong): no parts any more - the
        // in-game 啵啵 (2982F4) switches whole poses: its idle frame on the ground, its cheer frame (arms up) in the air
        // and on the leap out. The hop, squash, lean, speed lines and stars stay procedural.
        static readonly Color DashBear = Hex(0x2982F4);

        void DrawGardenTransition(PixelSurface s)
        {
            if (!transitionActive) return;
            HitBlock(s);
            bool mapped = s.MapLayout; s.MapLayout = false;
            try
            {
                bool home = transitionKind == TransitionKind.Home;
                float end = home ? HomeTransitionEnd : LevelTransitionEnd, outLen = home ? .3f : .37f, outStart = end - outLen;
                float age = transitionAge, W = ScreenLayout.Width, H = ScreenLayout.Height, L0 = ScreenLayout.Left;
                float inP = OutCubic(Clamp01(age / (home ? .26f : .34f))), outP = Clamp01((age - outStart) / outLen);
                outP = outP * outP;
                // ---- the sweep: a huge band along the bottom-left -> top-right diagonal
                Vector2 n = new Vector2(.7071f, -.7071f), perp = new Vector2(.7071f, .7071f), o = new Vector2(L0, H);
                float diag = (W + H) * .7071f * 1.06f;
                float lead = age < outStart ? inP * diag : diag, tail = outP * diag;
                if (lead - tail > 1f)
                {
                    s.Segment(o + n * (tail - 1f), o + n * (lead + 1f), 3200f, DashBase);
                    if (lead < diag - 1f) s.GlowLine(o + n * lead - perp * 1600f, o + n * lead + perp * 1600f, 70f, new Color(.75f, .95f, 1f, .45f));
                    if (tail > 1f) s.GlowLine(o + n * tail - perp * 1600f, o + n * tail + perp * 1600f, 70f, new Color(.75f, .95f, 1f, .45f));
                }
                float k = age < outStart ? Clamp01((inP - .55f) / .45f) : 1f - Clamp01(outP / .4f);
                float cx = L0 + W * .5f, cy = H * .46f;
                if (k > .01f)
                {
                    s.Glow(cx, cy, W * 1.7f, H * 1.15f, A(DashGlow, .62f * k), 0f);
                    s.Glow(cx, cy, W * .95f, H * .6f, new Color(.72f, 1f, 1f, .32f * k), 0f);
                    DashPattern(s, age, k, L0, W, H);
                }
                if (home)
                {
                    if (k > .01f) GardenBarArt.TextOutline(s, "返回主页", cx, cy - 18f, 32f, A(Color.white, k), A(Hex(0x1D3A7A), k), 2.6f);
                    return;
                }
                // ---- 啵啵: dash in, run on the spot, leap out
                float x = cx, y = cy, scale = 1f, rot = 0f;
                if (age < .37f)
                {
                    float u = age / .37f, e = OutCubic(u);
                    x = Mathf.Lerp(cx - 440f, cx, e); y = Mathf.Lerp(cy + 340f, cy, e) - Mathf.Sin(u * Mathf.PI) * 90f;
                    scale = u < .63f ? Mathf.Lerp(.45f, 1.07f, u / .63f) : Mathf.Lerp(1.07f, 1f, (u - .63f) / .37f);
                    rot = Mathf.Lerp(22f, 0f, e);
                }
                else if (age > outStart)
                {
                    float u = Clamp01((age - outStart) / outLen), e = u * u;
                    x = Mathf.Lerp(cx, cx + 440f, e); y = Mathf.Lerp(cy, cy - 380f, e) - Mathf.Sin(u * Mathf.PI) * 60f;
                    scale = Mathf.Lerp(1f, .45f, e); rot = Mathf.Lerp(0f, -14f, e);
                }
                // Hopping: a .42 s bounce - up 36 px, stretched a little in the air, squashed wide on landing, tipping the
                // other way each hop, arms flung up in the air, mouth open at the top, dust puffs at each landing.
                const float hopT = .42f;
                bool running = age >= .3f && age <= outStart + .05f;
                float ph = (age % hopT) / hopT, air = running ? 4f * ph * (1f - ph) : 0f;
                float land = running ? Clamp01(1f - Mathf.Min(ph, 1f - ph) / .12f) : 0f;
                float bob = -air * 36f, squash = land * .1f - air * .05f;
                float lean = running ? Mathf.Sin(age / hopT * Mathf.PI) * 5f - 4f : 0f;
                float fade = Mathf.Clamp01((age - .3f) / .15f) * (1f - outP);
                if (running) DashTrail(s, x, y, age, fade, ph);
                DashRunner(s, x, y + bob, 310f * scale, rot + lean, squash, air > .3f || age > outStart, air);   // 2026-11-01 09:32: 15% bigger
                if (running) DashStars(s, x, y, age, 1f - outP);
                float ta = Clamp01((age - .3f) / .2f) * (1f - Clamp01((age - outStart) / .2f));
                if (ta > .01f && transitionLevel >= 0)
                {
                    if (transitionLevel + 1 != dashLabelLevel) { dashLabelLevel = transitionLevel + 1; dashLabel = "第 " + dashLabelLevel + " 关"; }
                    GardenBarArt.TextOutline(s, dashLabel, cx, cy + 168f, 38f, A(Color.white, ta), A(Hex(0x1D3A7A), ta), 3f);
                }
            }
            finally { s.MapLayout = mapped; }
        }
        int dashLabelLevel = -1; string dashLabel = "";

        // Faint bubbles, stars and dots on a staggered 96 px grid, drifting down-left.
        void DashPattern(PixelSurface s, float age, float k, float left, float w, float h)
        {
            const float step = 96f;
            float ox = ((-age * 22f) % step + step) % step, oy = ((age * 60f) % step + step) % step;
            int cols = Mathf.CeilToInt(w / step) + 2, rows = Mathf.CeilToInt(h / step) + 2;
            for (int j = -1; j < rows; j++)
                for (int i = -1; i < cols; i++)
                {
                    float px = left + i * step + (j & 1) * step * .5f + ox, py = j * step + oy;
                    int kind = ((i * 7 + j * 3) % 3 + 3) % 3;
                    if (kind == 0)
                    {
                        s.GardenRoundedRect(px - 15f, py - 15f, 30f, 30f, 15f, new Color(1f, 1f, 1f, .13f * k), new Color(1f, 1f, 1f, .07f * k));
                        s.GardenRoundedRect(px - 9f, py - 10f, 7f, 7f, 3.5f, new Color(1f, 1f, 1f, .3f * k), new Color(1f, 1f, 1f, .3f * k));
                    }
                    else if (kind == 1)
                    {
                        s.Glow(px, py, 30f, 5f, new Color(1f, 1f, 1f, .32f * k), 0f);
                        s.Glow(px, py, 5f, 30f, new Color(1f, 1f, 1f, .32f * k), 0f);
                    }
                    else s.GardenRoundedRect(px - 4f, py - 4f, 8f, 8f, 4f, new Color(1f, 1f, 1f, .16f * k), new Color(1f, 1f, 1f, .16f * k));
                }
        }

        // The runner: the in-game 啵啵, idle on the ground and cheering (arms up) in the air. `size` is the old 512 parts
        // canvas size (the bear stood ~.73 of it); the 224x256 frame is drawn .87 of it tall, so it keeps that height.
        // squash widens and flattens it from the pot's foot.
        void DashRunner(PixelSurface s, float x, float y, float size, float rot, float squash, bool cheer, float air)
        {
            // the ground shadow stays put and shrinks while the pot is up in the air
            float h = size * .87f, gy = y + air * 36f + h * .42f, gk = 1f - air * .35f;
            s.Glow(x, gy, size * .62f * gk, size * .1f * gk, new Color(0f, .1f, .35f, .28f * gk), 0f);
            float fh = h * (1f - squash), fw = h * 224f / 256f * (1f + squash);
            string id = PlantSlot(DashBear, cheer && Has(PlantSlot(DashBear, "cheer")) ? "cheer" : "idle");
            if (!Has(id))
            {
                Creature(s, DashBear, x, y + h * .42f, h, 0, "", 0f, 12f, false, false, 0f, 1.3f);
                return;
            }
            // keep the pot's foot (.92 of the frame) on the same line while squashed
            float drop = h * .42f - fh * .42f, r = rot * Mathf.Deg2Rad;
            AssetRotated(s, id, new Vector2(x - drop * Mathf.Sin(r), y + drop * Mathf.Cos(r)), fw, fh, rot, Color.white);
        }

        // Speed lines streaming off to the left behind the runner, and little dust puffs kicked up at its feet.
        void DashTrail(PixelSurface s, float x, float y, float age, float a, float ph)
        {
            if (a <= .01f) return;
            if (ph < .2f)   // a little puff either side of the pot as it lands
                for (int side = -1; side <= 1; side += 2)
                    for (int j = 0; j < 2; j++)
                    {
                        float u = ph / .2f, d = (16f + j * 8f) * (.6f + .6f * u);
                        float px = x + side * (70f + u * 40f + j * 18f), py = y + 96f - u * 10f - j * 4f;
                        s.GardenRoundedRect(px - d * .5f, py - d * .5f, d, d, d * .5f, new Color(1f, 1f, 1f, .5f * a * (1f - u)), new Color(.85f, .95f, 1f, .35f * a * (1f - u)));
                    }
            for (int i = 0; i < 7; i++)
            {
                float u = (age * 2.4f + i * .37f) % 1f, len = 60f + (i % 3) * 30f, yy = y - 80f + i * 26f;
                float x0 = x - 110f - u * 220f;
                s.GlowLine(new Vector2(x0, yy), new Vector2(x0 - len, yy + 6f), 5f, new Color(1f, 1f, 1f, .55f * a * (1f - u)));
            }
        }

        // Gold stars popping round the runner in turn (scale 0 -> 1 -> 0), like the reference's twelve.
        void DashStars(PixelSurface s, float x, float y, float age, float a)
        {
            if (!Has("scene/revive-sparkle") || a <= .01f) return;
            for (int i = 0; i < 9; i++)
            {
                float u = (age * 1.6f + i * .41f) % 1f;
                if (u > .35f) continue;
                float k = Mathf.Sin(u / .35f * Mathf.PI), d = (i % 3 == 0 ? 34f : 22f) * k;
                float ang = i * 2.39f, rx = 150f + (i % 2) * 30f, ry = 130f + (i % 3) * 18f;
                float px = x + Mathf.Cos(ang) * rx, py = y - 10f + Mathf.Sin(ang) * ry;
                Asset(s, "scene/revive-sparkle", px - d * .5f, py - d * .5f, d, d, new Color(1f, 1f, 1f, a * k));
            }
        }
    }
}
#endif
