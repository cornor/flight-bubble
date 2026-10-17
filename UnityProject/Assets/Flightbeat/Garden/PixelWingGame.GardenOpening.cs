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
    //   0.40 D - 0.66 D  upper field, no panel: two translucent hazard bands, a big pulsing "WARNING" and one line
    //                    "泡泡来袭！" (困难关卡 / 超难关卡); slides in from the right and shakes.
    //   0.66 D - D       "准备出战".
    // New members (first level of each character, GardenColorCatalog.Debut): a row of name chips appears with
    // "准备出战", stays for ChipHold seconds, then each chip shrinks and flies to the mouth of the glass tube of the
    // lane holding that member (front-most occurrence, hidden or not), bursts into a small sparkle and is gone.
    // This runs on UI time, so it finishes whether or not the player has tapped to start.
    public partial class PixelWingGame
    {
        const float ChipHold = 1.5f, ChipFly = .5f, ChipStagger = .12f, ChipBurst = .45f;
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
                float e = time - readyAt, alpha = Clamp01(e * 5f), size = 30f + (1f - Clamp01(e * 4f)) * 14f;
                s.Glow(270f, 412f, 420f, 120f, new Color(1f, .9f, .45f, alpha * .35f), 0f);
                TextOutline(s, "准备出战", 270f, 412f - size * .55f, size, A(Gold, alpha), A(Outline, alpha), 2.6f);
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
            for (int side = 0; side < 2; side++)
            {
                float y = side == 0 ? top : top + 104f, travel = (side == 0 ? ui : -ui) * 120f % 22f;
                for (int n = -1; n < 26; n++)
                {
                    float x = travel + n * 22f;
                    if (x < 12f || x > 520f) continue;
                    s.Segment(new Vector2(x, y + 10f), new Vector2(x + 8f, y), 4f, new Color(1f, .4f, .4f, alpha * .45f));
                }
            }
            float pulse = Mathf.Sin(ui * 20f) * .5f + .5f, x0 = 270f + slide + shake;
            Color red = Color.Lerp(new Color(1f, .3f, .3f), Color.white, pulse * .35f);
            TextOutline(s, "WARNING", x0, top + 18f, 44f, A(red, alpha), A(Hex(0x5A1010), alpha * .9f), 2.6f);
            int difficulty = Battle.Level.difficulty;
            string text = difficulty == 2 ? "超难关卡" : difficulty == 1 ? "困难关卡" : "泡泡来袭！";
            TextOutline(s, text, x0, top + 70f, 22f, A(Cream, alpha), A(Hex(0x8A1E1E), alpha), 2f);
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

        void DrawDebutChips(PixelSurface s, float age)
        {
            int[] debut = GardenColorCatalog.Debut(levelIndex);
            int n = debut.Length;
            if (n == 0 || age > ChipHold + ChipFly + ChipStagger * n + ChipBurst) return;
            if (debutLanes.Length != n) debutLanes = new int[n];
            if (age < ChipHold) for (int k = 0; k < n; k++) debutLanes[k] = DebutLane(debut[k]);   // fixed once flying
            const float tagW = 64f, chipW = 96f, h = 36f, gap = 8f, y = 446f;
            float rowX = 270f - (tagW + n * (chipW + gap)) * .5f;
            float tagAlpha = Clamp01(age * 5f) * (1f - Clamp01((age - ChipHold) / .3f));
            if (tagAlpha > 0f)
            {
                if (!Asset9(s, "ui/banner-warning", rowX, y + 3f, tagW, h - 6f, 45f, 0f, (h - 6f) * .58f, 0f, new Color(1f, .85f, .4f, tagAlpha)))
                    s.Rect(rowX, y + 3f, tagW, h - 6f, A(Gold, tagAlpha));
                TextOutline(s, "新队员", rowX + tagW * .5f, y + 10f, 14f, A(Cream, tagAlpha), A(Hex(0x7A3A00), tagAlpha), 1.4f);
            }
            for (int k = 0; k < n; k++)
            {
                Color tint = new Color32((byte)(debut[k] >> 16), (byte)(debut[k] >> 8), (byte)debut[k], 255);
                Vector2 home = new Vector2(rowX + tagW + gap + k * (chipW + gap) + chipW * .5f, y + h * .5f);
                Vector2 mouth = new Vector2(Battle.QueuePositionX(debutLanes[k]), 694f);
                float fly = (age - ChipHold - k * ChipStagger) / ChipFly;
                if (fly >= 1f) { GardenFx.Burst(s, "merge-burst", mouth, 64f, (fly - 1f) * ChipFly, ChipBurst); continue; }
                float pop = OutBack(Clamp01((age - k * .12f) / .25f));
                if (pop <= 0f) continue;
                float f = fly <= 0f ? 0f : InOut(fly), scale = pop * Mathf.Lerp(1f, .35f, f);
                Vector2 c = Vector2.Lerp(home, mouth, f) + new Vector2(0f, -Mathf.Sin(f * Mathf.PI) * 60f);   // gentle arc
                float w = chipW * scale, ch = h * scale;
                if (f < .5f)
                {
                    float panelAlpha = 1f - f * 2f;
                    if (!Asset9(s, "ui/popup-panel", c.x - w * .5f, c.y - ch * .5f, w, ch, 30f, 30f, 12f * scale, 12f * scale, new Color(1f, 1f, 1f, panelAlpha)))
                        s.Rect(c.x - w * .5f, c.y - ch * .5f, w, ch, A(Paper, panelAlpha));
                    if (pop >= .9f && f <= 0f) Text(s, Name(tint), c.x + 16f, c.y - 9f, 16f, true, Ink);
                }
                if (pop < .9f) continue;
                // The avatar leads the flight (it is what lands in the tube); it starts at the chip's left end.
                float av = 38f * Mathf.Lerp(1f, .8f, f);
                Vector2 a = Vector2.Lerp(new Vector2(home.x - chipW * .5f + 21f, home.y), mouth, f) + new Vector2(0f, -Mathf.Sin(f * Mathf.PI) * 60f);
                Asset(s, PlantSlot(tint, "idle"), a.x - av * .5f, a.y - av * .75f, av, av * 256f / 224f, Color.white);
            }
        }
    }
}
#endif
