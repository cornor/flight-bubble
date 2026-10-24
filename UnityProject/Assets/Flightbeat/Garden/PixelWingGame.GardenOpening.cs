#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-16 (user decisions): level opening in bubble dress, replacing the heavy "新队员加入" card.
    // Warning, on the original FleetOpening timeline (protected, not drawn by the bubble HUD), light like the original:
    //   0.40 D - 0.66 D  colour-coded glass with an icon and "泡泡来袭！" / difficulty; slides in and shakes.
    //   0.66 D - D       "准备出战".
    // New members (first level of each character, GardenColorCatalog.Debut): a row of name chips appears with
    // "准备出战", stays for ChipHold seconds, then each chip shrinks and flies to the mouth of the glass tube of the
    // lane holding that member (front-most occurrence, hidden or not), bursts into a small sparkle and is gone.
    // This runs on UI time, so it finishes whether or not the player has tapped to start.
    public partial class PixelWingGame
    {
        const float ChipHold = 1.5f, ChipFly = .5f, ChipStagger = .12f, ChipBurst = .45f;
        const float OpeningReadyY = 412f, DebutRowOffset = 80f;
        Flightbeat.Model.Battle debutBattle;
        float debutStart = -1f;
        int[] debutLanes = new int[0];

        static int Rgb(Color c) { Color32 k = c; return (k.r << 16) | (k.g << 8) | k.b; }

        void DrawGardenOpening(PixelSurface s)
        {
            bool intro = Battle.State == Phase.Intro;
            float duration = Battle.Level.introDuration > 0f ? Battle.Level.introDuration : 4.6f;
            float time = intro ? Battle.IntroTime : duration;
            float warningAt = duration * .4f, readyAt = duration * .66f;
            if (intro && time >= warningAt && time < readyAt) DrawOpeningWarning(s, (time - warningAt) / (readyAt - warningAt));
            if (intro && time >= readyAt)
            {
                float e = time - readyAt, alpha = Clamp01(e * 5f), size = 26f + (1f - Clamp01(e * 4f)) * 12f;
                // 2026-10-20 (UI redesign 10, review: sky, not gold, so it is not mistaken for 困难关卡): a sky-edged
                // glass bar with a gold star that settles from a little larger.
                float h = size * 2f;
                bool mapped = s.MapLayout;
                float cy = GardenBars.ScreenY(s, OpeningReadyY);
                s.MapLayout = false;
                GardenBars.Bar(s, 270f, cy, GardenBars.Width("准备出战", size, h, "star"), h, GardenBars.Sky, "star", "准备出战", size,
                    GardenBars.Navy, alpha, 1f, GardenBars.Deco.One, e);
                s.MapLayout = mapped;
            }
            if (debutBattle != Battle) { debutBattle = Battle; debutStart = -1f; }
            if (debutStart < 0f && (!intro || time >= readyAt)) debutStart = ui;
            if (debutStart >= 0f) DrawDebutChips(s, ui - debutStart);
        }

        void DrawOpeningWarning(PixelSurface s, float t)
        {
            float alpha = Clamp01(t * 8f) * Clamp01((1f - t) * 8f);
            float slide = (1f - Clamp01(t * 4f)) * 600f, shake = t > .25f ? Mathf.Sin(ui * 60f) * 1.5f : 0f;
            const float top = 124f;
            float x0 = 270f + slide + shake;
            int difficulty = Battle.Level.difficulty;
            string text = difficulty == 2 ? "超难关卡" : difficulty == 1 ? "困难关卡" : "泡泡来袭！";
            // 2026-10-20 (UI redesign 07-09): a glass bar replaces the generic WARNING and hazard stripes: sky with a bubble cluster
            // (泡泡来袭！), gold with two stars and brown ink (困难关卡), purple with three stars (超难关卡).
            Color edge = difficulty == 2 ? GardenBars.Purple : difficulty == 1 ? GardenBars.Gold : GardenBars.Sky;
            Color ink = difficulty == 2 ? GardenBars.DeepPurple : difficulty == 1 ? GardenBars.Brown : GardenBars.Navy;
            string icon = difficulty == 2 ? "stars3" : difficulty == 1 ? "stars2" : "bubbles";
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, top + 60f);
            s.MapLayout = false;
            GardenBars.Bar(s, x0, cy, GardenBars.Width(text, 24f, 50f, icon), 50f, edge, icon, text, 24f, ink, alpha, 1f, GardenBars.Deco.One, t * 2f);
            s.MapLayout = mapped;
        }

        // Lane of the front-most queued member with this color (any row, hidden mystery rows included).
        int DebutLane(int rgb)
        {
            int best = -1, bestRow = int.MaxValue;
            for (int l = 0; l < Battle.LaneCount; l++)
            {
                var reserve = Battle.Reserve[l];
                for (int r = 0; r < reserve.Count && r < bestRow; r++)
                    if (Rgb(GardenColor(reserve[r].Color)) == rgb) { best = l; bestRow = r; break; }
            }
            return best >= 0 ? best : Battle.LaneCount / 2;
        }

        // 2026-10-19 11:40 (user picked C): the debut chip is the hint glass capsule with a gold edge, a soft gold halo
        // and a twinkling four-point star on its top-right shoulder (replaces the old popup-panel jelly frame).
        void DebutCapsule(PixelSurface s, float cx, float logicalCy, float w, float h, float alpha, int k)
        {
            bool mapped = s.MapLayout;
            float cy = mapped ? PlushLayout.Y(logicalCy) : logicalCy;
            s.MapLayout = false;
            try
            {
                s.Glow(cx, cy, w + 18f, h + 20f, A(Hex(0xFFD45A), .3f * alpha), 0f);
                GlassCapsule(s, cx, cy, w, h, Hex(0xF5B83A), alpha);
                float r = Mathf.Min(7f, h * .2f), sx = cx + w * .5f - h * .22f, sy = cy - h * .5f + 1f;
                float tw = .75f + .25f * Mathf.Sin(ui * 5f + k * 1.7f);
                Color star = A(Hex(0xFFF3B0), alpha), rim = A(Hex(0xE8A21C), alpha * .9f);
                for (int pass = 0; pass < 2; pass++)
                {
                    float rr = r * tw + (pass == 0 ? 1.2f : 0f), width = pass == 0 ? 3.2f : 2f;
                    Color col = pass == 0 ? rim : star;
                    s.Segment(new Vector2(sx, sy - rr), new Vector2(sx, sy + rr), width, col);
                    s.Segment(new Vector2(sx - rr, sy), new Vector2(sx + rr, sy), width, col);
                }
                s.Rect(sx - 1.5f, sy - 1.5f, 3f, 3f, A(Color.white, alpha));
            }
            finally { s.MapLayout = mapped; }
        }

        void DrawDebutChips(PixelSurface s, float age)
        {
            int[] debut = GardenColorCatalog.Debut(levelIndex);
            int n = debut.Length;
            if (n == 0 || age > ChipHold + ChipFly + ChipStagger * n + ChipBurst) return;
            if (debutLanes.Length != n) debutLanes = new int[n];
            if (age < ChipHold) for (int k = 0; k < n; k++) debutLanes[k] = DebutLane(debut[k]);   // fixed once flying
            const float tagW = 80f, h = 36f;
            float chipW = n >= 4 ? 92f : 96f, gap = n >= 4 ? 12f : 16f;
            // Capsules and portraits keep their pixel size when the board is compressed.
            // Reserve the title's entrance overshoot plus avatar/glint clearance in that same space.
            float rowY = s.MapLayout
                ? PlushLayout.InverseY(PlushLayout.Y(OpeningReadyY) + DebutRowOffset)
                : OpeningReadyY + DebutRowOffset;
            float rowX = 270f - (tagW + n * (chipW + gap)) * .5f;
            float tagAlpha = Clamp01(age * 5f) * (1f - Clamp01((age - ChipHold) / .3f));
            if (tagAlpha > 0f)
            {
                // 2026-10-20 (UI redesign 11): the tag is a gold-edged glass capsule with brown ink, like the chips.
                bool tagMapped = s.MapLayout;
                float tagCy = GardenBars.ScreenY(s, rowY);
                s.MapLayout = false;
                GardenBars.Capsule(s, rowX + tagW * .5f, tagCy, tagW, h - 6f, GardenBars.Gold, tagAlpha);
                GardenBarArt.Text(s, "初次登场", rowX + tagW * .5f, tagCy - 8.5f, 15f, true, A(GardenBars.Brown, tagAlpha));
                s.MapLayout = tagMapped;
            }
            for (int k = 0; k < n; k++)
            {
                Color tint = new Color32((byte)(debut[k] >> 16), (byte)(debut[k] >> 8), (byte)debut[k], 255);
                Vector2 home = new Vector2(rowX + tagW + gap + k * (chipW + gap) + chipW * .5f, rowY);
                // 2026-10-17: the member dives to the BOTTOM of its glass tube and vanishes with a whoosh (stretched,
                // shrinking, with a short streak); only a faint glow in its color is left, no burst.
                Vector2 mouth = new Vector2(Battle.QueuePositionX(debutLanes[k]), 852f);
                float fly = (age - ChipHold - k * ChipStagger) / ChipFly;
                if (fly >= 1f)
                {
                    float g = (fly - 1f) * ChipFly / .25f;
                    if (g < 1f) s.Glow(mouth.x, mouth.y - 12f, 64f, 44f, A(tint, (1f - g) * .45f), 0f);
                    continue;
                }
                float pop = OutBack(Clamp01((age - k * .12f) / .25f));
                if (pop <= 0f) continue;
                float f = fly <= 0f ? 0f : fly * fly, scale = pop * Mathf.Lerp(1f, .35f, f);   // speeds up: whoosh
                Vector2 c = Vector2.Lerp(home, mouth, f) + new Vector2(0f, -Mathf.Sin(f * Mathf.PI) * 60f);   // gentle arc
                float w = chipW * scale, ch = h * scale;
                if (f < .5f)
                {
                    float panelAlpha = 1f - f * 2f;
                    DebutCapsule(s, c.x, c.y, w, ch, panelAlpha, k);
                    if (pop >= .9f && f <= 0f)
                    {
                        bool mapped = s.MapLayout;
                        float textCy = GardenBars.ScreenY(s, c.y);
                        s.MapLayout = false;
                        GardenBarArt.Text(s, Name(tint), c.x + 18f, textCy - 9f, 16f, true, Hex(0x163E6D));
                        s.MapLayout = mapped;
                    }   // 22:40: centred right of the avatar
                }
                if (pop < .9f) continue;
                // The avatar leads the flight (it is what lands in the tube); it starts at the chip's left end.
                Vector2 from = new Vector2(home.x - chipW * .5f + 24f, home.y);
                Vector2 At(float q) => Vector2.Lerp(from, mouth, q) + new Vector2(0f, -Mathf.Sin(q * Mathf.PI) * 60f);
                Vector2 a = At(f);
                float vanish = fly > .6f ? Clamp01((fly - .6f) / .4f) : 0f, stretch = Clamp01((fly - .35f) / .65f);
                float av = 44f * Mathf.Lerp(1f, .8f, f) * (1f - vanish * vanish);   // 22:40: 38 -> 44
                if (fly > .25f)
                    for (int j = 1; j <= 3; j++)
                    {
                        Vector2 b = At(Mathf.Max(0f, (fly - j * .06f) * (fly - j * .06f)));
                        s.GlowLine(b, a, Mathf.Max(6f, av * .55f), A(tint, .32f * (1f - j * .25f) * (1f - vanish * .5f)));
                    }
                if (av > .5f)
                {
                    float art = GardenSkin.ArtScale(tint);   // 2026-10-20: thin members drawn larger (GardenSkin.ArtScale)
                    float sw = av * art * (1f - .35f * stretch), sh = av * art * 256f / 224f * (1f + .7f * stretch);
                    // 2026-10-18 22:40 (user): the avatar sat high in its chip (anchored at 3/4 of the canvas); the body centre
                    // (137/256 of the 224x256 canvas, between BodyTop and FeetLine) now sits on the chip centre.
                    Asset(s, PlantSlot(tint, "idle"), a.x - sw * .5f, a.y - sh * (137f / 256f), sw, sh, Color.white);
                }
            }
        }
    }
}
#endif
