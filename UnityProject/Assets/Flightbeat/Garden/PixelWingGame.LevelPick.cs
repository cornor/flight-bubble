#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-10-19 B level set (plan step 3, "选关 / 重玩"): with shared progress a player far ahead could not
    // reach the levels that already have a new picture. This home page lists every unlocked level, 30 per page
    // (5 x 6), and starts any of them with the current level set. Settlement is the original one
    // (FleetEconomy.Reward: a replay is not a first clear, so cleared / unlocked do not change).
    // Cells: unlocked = blue bubble with its number; cleared = small gold star; the level 开始 opens = pulsing
    // glow; locked = dim bubble with a lock; when the new pictures are on, levels that have one wear a 「新」 tag.
    public partial class PixelWingGame
    {
        const int LevelPickOpenAction = 806, LevelPickCloseAction = 807, LevelPickPrevAction = 808, LevelPickNextAction = 809;
        const int LevelPickCellAction = 900;   // 900 + cell (0..29)
        const int LevelPickPerPage = 30, LevelPickColumns = 5;
        bool levelPickOpen;
        float levelPickOpenedAt;
        int levelPickPage;
        static int LevelPickPages => (Levels.Length + LevelPickPerPage - 1) / LevelPickPerPage;

        void OpenLevelPick()
        {
            levelPickOpen = true; levelPickOpenedAt = ui;
            levelPickPage = Mathf.Clamp(NextBubbleLevel() / LevelPickPerPage, 0, LevelPickPages - 1);
            audioChip.Play("page");
        }
        void CloseLevelPick() { levelPickOpen = false; audioChip.Play("ui"); }
        // Called from DeckTap while the page is open (modal).
        void LevelPickTap(int id)
        {
            if (id == LevelPickCloseAction) { CloseLevelPick(); return; }
            if (id == LevelPickPrevAction && levelPickPage > 0) { levelPickPage--; audioChip.Play("page"); return; }
            if (id == LevelPickNextAction && levelPickPage < LevelPickPages - 1) { levelPickPage++; audioChip.Play("page"); return; }
            if (id < LevelPickCellAction || id >= LevelPickCellAction + LevelPickPerPage) return;
            int index = levelPickPage * LevelPickPerPage + id - LevelPickCellAction;
            if (index >= Levels.Length || index >= Economy.Data.unlocked) { audioChip.Play("deny"); return; }
            levelPickOpen = false;
            if (!Economy.CanSortie) Economy.AddEnergy(1);
            StartSortie(index);
        }

        string levelPickHeader = "";
        int levelPickHeaderPage = -1;
        void DrawLevelPick(PixelSurface s)
        {
            if (!levelPickOpen) return;
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            GardenSkin.Clock = ui;
            float w = ScreenLayout.Width, h = ScreenLayout.Height, L = ScreenLayout.Left, oy = (h - 960f) * .5f;
            float t = Clamp01((ui - levelPickOpenedAt) / .3f), slide = (1f - OutBack(t)) * 30f;
            HitBlock(s);
            s.Rect(L, 0f, w, h, new Color(.05f, .12f, .28f, .95f * t));
            s.Glow(L + w * .5f, oy + 420f, 640f, 700f, new Color(.35f, .62f, 1f, .22f * t), 0f);
            GardenTitle(s, "选择关卡", 270f, oy + 24f - slide, 30f, true);
            HitBox(s, LevelPickCloseAction, 470f, oy + 14f, 60f, 60f);
            float cb = 1f - PressPulse(LevelPickCloseAction) * .1f;
            GardenSkin.Shot(s, Hex(0xFF8A7A), new Vector2(500f, oy + 44f), 42f * cb, t);
            TextOutline(s, "×", 500f, oy + 31f, 26f, Cream, Outline, 2f);

            int first = levelPickPage * LevelPickPerPage, last = Mathf.Min(Levels.Length, first + LevelPickPerPage);
            if (levelPickHeaderPage != levelPickPage)
            {
                levelPickHeaderPage = levelPickPage;
                // The original war-zone names do not fit the bubble world, so the page only shows its range.
                levelPickHeader = "第 " + (first + 1) + "–" + last + " 关";
            }
            TextOutline(s, levelPickHeader, 270f, oy + 84f, 16f, Hex(0xFFE58A), Outline, 1.5f);
            bool newSet = LevelSetCurrent == LevelSetB;
            if (LevelSetAvailableCount > 0)
                TextOutline(s, newSet ? "当前：新版关卡图案（可在设置里切换）" : "当前：经典关卡图案（可在设置里切换）", 270f, oy + 108f, 14f, Hex(0xBFD4F2), Outline, 1.2f);

            int next = NextBubbleLevel(), unlocked = Economy.Data.unlocked;
            const float cell = 92f, d = 70f, gridTop = 150f;
            float left = 270f - cell * LevelPickColumns * .5f;
            for (int i = first; i < last; i++)
            {
                int k = i - first;
                float cx = left + (k % LevelPickColumns + .5f) * cell, cy = oy + gridTop + (k / LevelPickColumns + .5f) * cell + slide;
                bool open = i < unlocked, cleared = Economy.Data.cleared.Contains(i + 1);
                float press = 1f - PressPulse(LevelPickCellAction + k) * .1f, size = d * press;
                HitBox(s, LevelPickCellAction + k, cx - cell * .5f, cy - cell * .5f, cell, cell);
                if (i == next && open)
                    s.Glow(cx, cy, d * 1.9f, d * 1.9f, new Color(1f, .84f, .35f, (.35f + .2f * Mathf.Sin(ui * 4f)) * t), 0f);
                Color tint = !open ? new Color(.36f, .44f, .6f, 1f) : cleared ? Hex(0x42A9FF) : Hex(0x7CC4F0);
                GardenSkin.Shot(s, tint, new Vector2(cx, cy), size, open ? t : .75f * t);
                if (open)
                    TextOutline(s, (i + 1).ToString(), cx, cy - 13f, 24f, Cream, Outline, 2f);
                else
                    Asset(s, "scene/lock-bubble", cx - 17f, cy - 17f, 34f, 34f, new Color(1f, 1f, 1f, .85f * t));
                if (cleared) LevelPickStar(s, cx + d * .3f, cy + d * .32f, 8f, t);
                if (newSet && open && LevelHasB(i))
                {
                    float bx = cx + d * .34f, by = cy - d * .36f;
                    GardenSkin.Shot(s, Hex(0xFF7A6B), new Vector2(bx, by), 24f, t);
                    TextOutline(s, "新", bx, by - 7f, 13f, Cream, Hex(0x8A1E1E), 1.2f);
                }
            }

            // Page buttons and page dots.
            float py = oy + gridTop + 6 * cell + 34f;
            if (levelPickPage > 0) GardenButton(s, LevelPickPrevAction, 40f, py, 150f, 54f, "上一页");
            if (levelPickPage < LevelPickPages - 1) GardenButton(s, LevelPickNextAction, 350f, py, 150f, 54f, "下一页");
            float dotsW = (LevelPickPages - 1) * 14f;
            for (int p = 0; p < LevelPickPages; p++)
                GardenSkin.Shot(s, p == levelPickPage ? Gold : new Color(.55f, .65f, .85f, 1f), new Vector2(270f - dotsW * .5f + p * 14f, py + 27f), p == levelPickPage ? 11f : 8f, t);
            s.MapLayout = mapped;
        }
        static void LevelPickStar(PixelSurface s, float x, float y, float r, float alpha)
        {
            Color rim = new Color(.9f, .55f, .1f, alpha), fill = new Color(1f, .9f, .4f, alpha);
            for (int pass = 0; pass < 2; pass++)
            {
                float rr = pass == 0 ? r + 1.5f : r, width = pass == 0 ? 4f : 2.6f;
                Color c = pass == 0 ? rim : fill;
                for (int a = 0; a < 5; a++)
                {
                    float ang = (a * 72f - 90f) * Mathf.Deg2Rad;
                    s.Segment(new Vector2(x, y), new Vector2(x + Mathf.Cos(ang) * rr, y + Mathf.Sin(ang) * rr), width, c);
                }
            }
        }
    }
}
#endif
