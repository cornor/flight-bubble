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
            Rect frame = HomeWaveFrames[member];
            float breathe = 1f + .008f * Mathf.Sin(ui * 2.2f + HomePhase[member]);
            float h = frame.height * breathe, w = frame.width / breathe;
            return HomeWaveRect(frame.center.x - w * .5f, frame.yMax - h, w, h);
        }
        void DrawWaveHome(PixelSurface s)
        {
            s.MapLayout = false;
            HomeWaveView(out float k, out float ox, out float oy);
            // Quiet decoration: the formerly oversized upper-right bubble is now
            // 42 logical px (about one third of the reference) at 32% opacity.
            WaveBubble(s, 435f, 290f, 42f, .32f, 0);
            WaveBubble(s, 270f, 96f, 26f, .35f, 1);
            WaveBubble(s, 159f, 301f, 24f, .28f, 2);
            WaveBubble(s, 505f, 380f, 23f, .25f, 3);
            WaveBubble(s, 61f, 723f, 24f, .28f, 4);
            // Match the painting order in GardenHomeArtwork, including hit precedence.
            foreach (int i in Garden.UI.GardenHomeArtwork.PaintOrder)
            {
                Rect r = HomeWaveMemberRect(i);
                HitBox(s, HomeMemberAction + i, r.x + r.width * .08f, r.y + r.height * .08f, r.width * .84f, r.height * .86f);
                float age = HomeWaveAge(i);
                if (age < 0f || age >= 2.5f) continue;
                // Double the restored blown-bubble diameter, retaining its opacity and timing.
                float drift = Mathf.Max(0f, age - .6f), size = (24f + i * 2f) * 2f * k * Mathf.Clamp01(age / .6f);
                Vector2 at;
                if (i == 4) at = new Vector2(r.xMax - 13f * k, r.y + 67f * k);
                else {
                    Vector2 m = MouthCanvas(Hex(HomeSquadColor[i]));
                    at = new Vector2(r.x + r.width * m.x / 224f, r.y + r.height * m.y / 256f);
                }
                at += new Vector2(Mathf.Sin(drift * 2f + i) * drift * 9f * k, -size * .55f - drift * 65f * k);
                SoapBubble(s, at, size, Color.white, Mathf.Clamp01((2.5f - age) / .3f) * .8f, i);
            }
            DrawWaveStart(s);
            HomeIconButton(s, 801, ScreenLayout.Left + 20f, ScreenLayout.Top + 16f, "plants/home/btn-settings", "设置", Hex(0x5BB8FF), 60f);
            Rect album = HomeWaveRect(448f, 745f, 70f, 70f);
            HomeIconButton(s, CodexOpenAction, album.x, album.y, "plants/home/btn-codex", "图鉴", Hex(0xFFB347), album.width);
            DrawWavePartner(s, k, ox, oy);
            s.MapLayout = true;
            DrawLevelPick(s);
        }
        void WaveBubble(PixelSurface s, float x, float y, float size, float opacity, int seed)
        {
            Rect r = HomeWaveRect(x, y + Mathf.Sin(ui * .55f + seed) * 2f, size, size);
            SoapBubble(s, new Vector2(r.x, r.y), r.width, Color.white, opacity, seed);
        }
        void DrawWaveStart(PixelSurface s)
        {
            int level = NextBubbleLevel() + 1;
            if (level != homeLabelLevel) { homeLabelLevel = level; homeLabel = "第 " + level + " 关"; }
            Rect r = HomeWaveRect(105f, 736f, 330f, 110f);
            HitBox(s, 800, r.x, r.y, r.width, r.height);
            float k = r.width / 330f * (1f - PressPulse(800) * .04f);
            float w = 330f * k, h = 110f * k, x = r.center.x - w * .5f, y = r.center.y - h * .5f;
            Asset9(s, "plants/win/button-next", x, y, w, h, 64f, 0f, h * 64f / 126f, 0f, Color.white);
            Asset(s, "plants/home/level-plate", r.center.x - 70f * k, y + 67f * k, 140f * k, 30f * k, Color.white);
            GardenBarArt.TextOutline(s, "开始游戏", r.center.x, y + 13f * k, 42f * k, Color.white, Hex(0xA55410), 2.5f * k);
            GardenBarArt.TextOutline(s, homeLabel, r.center.x, y + 68f * k, 24f * k, Color.white, Hex(0xA55410), 1.5f * k);
        }
        void DrawWavePartner(PixelSurface s, float k, float ox, float oy)
        {
            int current = NextBubbleLevel() + 1;
            CodexEntry next = default; bool found = false;
            foreach (var e in Codex) { if (CodexObtained(e)) continue; next = e; found = true; break; }
            if (!found) next = new CodexEntry { name = "", tint = Hex(0x2982F4), level = current };
            int left = Mathf.Max(1, next.level - current + 1);
            if (homeNextLevel != current || homeNextName != next.name)
            {
                homeNextLevel = current; homeNextName = next.name;
                homeNextLine = !found ? "小队伙伴已集齐" : left == 1 ? "通关这一关就来啦！" : "再通关 " + left + " 关就来啦！";
            }
            Rect row = HomeWaveRect(86f, 865f, 427f, 140f);
            HitBox(s, CodexOpenAction, row.x, row.y, row.width, row.height);
            float gx = ox + 168f * k, gy = oy + (935f + Mathf.Sin(ui * 1.8f) * 2f) * k;
            float d = 142f * k;
            SoapBubble(s, new Vector2(gx, gy), d, Color.white, .28f, 7);
            bool before = GardenSkin.Celebrate; GardenSkin.Celebrate = true;
            Creature(s, next.tint, gx, gy + 53f * k, 135f * k, 0, "", 0f, 12f, false, false, .03f, 1.3f);
            GardenSkin.Celebrate = before;
            SoapBubble(s, new Vector2(gx, gy), d, Color.white, .12f, 7);
            float tx = ox + 255f * k;
            GardenBarArt.TextOutline(s, found ? "新伙伴" : "全员集结", tx, oy + 902f * k, 22f * k, Color.white, Outline, 1.8f * k, false);
            if (found) GardenBarArt.TextOutline(s, homeNextName, tx + 73f * k, oy + 895f * k, 29f * k, Gold, Outline, 2f * k, false);
            GardenBarArt.TextOutline(s, homeNextLine, tx, oy + 945f * k, 21f * k, Color.white, Outline, 1.6f * k, false);
        }
    }
}
#endif
