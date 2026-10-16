using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Model.OriginalMath;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Standalone source interfaces. Existing inlined page paths retain their
        // own source operand order; these helpers do not replace those paths.
        private static Vector2Int ZoneTile(int zone) // f54548, table 3648120
        {
            switch (zone) {
                case 1: return new Vector2Int(0, 1024);
                case 2: return new Vector2Int(0, 1504);
                case 3: return new Vector2Int(512, 0);
                default: return new Vector2Int(512, 480);
            }
        }

        private void Panel(PixelSurface s, float x, float y, float w, float h, Color edge) // f54721
            => Plate(s, x, y, w, h, uiPanelHi, uiPanel, edge, 6f, 1f);
        private void AdBadge(PixelSurface s, float x, float y) => AdIcon(s, x, y, 18f); // f54814

        private static void ModuleEmblem(PixelSurface s, int kind, float x, float y, float w, float h, Color tint) // f54676
        {
            int index = kind < 0 ? 0 : kind >= FleetCatalog.Modules.Length ? FleetCatalog.Modules.Length - 1 : kind;
            int icon = FleetCatalog.Modules[index].bankIcon;
            if (icon >= 0) s.Hull(icon, x, y, w, h, tint, 0f);
            else s.Sprite(unchecked(kind + 16), x, y, w, h, tint, 0f);
        }

        private void DrawCalendar(PixelSurface s, float cx, float cy) // f54636; string slot 5823116
        {
            s.Rect(Binary32(cx - 15f), Binary32(cy - 13f), 30f, 29f, SourceColor(4572548855856822007UL, 4575657222472527085UL));
            s.Rect(Binary32(cx - 15f), Binary32(cy - 13f), 30f, 9f, SourceColor(4512783441039782129UL, 4575657222457032833UL));
            Color ring = SourceColor(4521825962093229237UL, 4575657222463085789UL);
            s.Rect(Binary32(cx - 9f), Binary32(cy - 17f), 3f, 7f, ring);
            s.Rect(Binary32(cx + 6f), Binary32(cy - 17f), 3f, 7f, ring);
            s.Text("7", Binary32(cx - 4f), Binary32(cy - 1f), 1f, SourceColor(4503740919957221617UL, 4575657222459138209UL), false);
        }

        private void DrawClipboard(PixelSurface s, float cx, float cy) // f54635
        {
            s.Rect(Binary32(cx - 13f), Binary32(cy - 16f), 26f, 32f, SourceColor(4557289601555944147UL, 4575657222466868887UL));
            s.Rect(Binary32(cx - 11f), Binary32(cy - 12f), 22f, 26f, SourceColor(4572548855856822007UL, 4575657222472527085UL));
            Color ink = SourceColor(4540334874551185649UL, 4575657222466210957UL);
            s.Rect(Binary32(cx - 6f), Binary32(cy - 19f), 12f, 6f, ink);
            float lineX = Binary32(cx - 2f), bulletX = Binary32(cx - 8f), baseY = Binary32(cy - 6f);
            for (int i = 0; i < 3; i++) {
                float y = Binary32(baseY + (i == 0 ? 0f : i == 1 ? 7f : 14f));
                s.Rect(bulletX, y, 4f, 4f, uiGreen);
                s.Rect(lineX, Binary32(y + 1f), 11f, 2f, ink);
            }
        }

        private void DrawStreakBadge(PixelSurface s, float cx, float y, float scale) // f54581
        {
            int streak = Economy.Streak;
            if (streak <= 0) return;
            // Original string slots 5818832 / 5819336.
            string text = streak.ToString() + " 连胜 +" + FleetCatalog.StreakPercent(streak).ToString() + "%";
            float font = Binary32(scale * 1.1f), measured = PixelSurface.Measure(text, font);
            float pulse = Binary32(Binary32(Binary32(Binary32(Sin(Binary32(ui * 4f)) * .5f) + .5f) * .1f) + .18f);
            float h = Binary32(scale * 26f), width = Binary32(measured + 44f);
            s.Glow(cx, Binary32(Binary32(h * .5f) + y), Binary32(width + 30f), Binary32(h + 24f), SourceColor(4539628425454813184UL, 1041865114u, pulse), 0f);
            float left = Binary32(cx - Binary32(width * .5f));
            Plate(s, left, y, width, h, SourceColor(4477658893541336678UL, 4572774917126477578UL),
                SourceColor(4417130516429196493UL, 4572774917109700362UL), SourceColor(4554039943403405312UL, 4575657222458677658UL), 6f, 1f);
            DrawFlame(s, Binary32(left + 16f), Binary32(Binary32(h * .55f) + y), Binary32(scale * 11f), y);
            s.TextF(text, Binary32(cx + 10f), Binary32(Binary32(Binary32(h + Binary32(scale * -15.40000057220459f)) * .5f) + y), font, uiGoldHi, 0);
        }

        private void DrawSpeedGauge(PixelSurface s) // f54835
        {
            if (Battle.State != Phase.Playing || tutorialStep >= 0 || !Battle.Pressure.enabled || Battle.ActiveFinale != null) return;
            float factor = Battle.SpeedFactor, firepower = Battle.Firepower;
            Color color = factor >= Binary32(Battle.Pressure.idleFactor + -.01f) ? uiRed : factor > 1.05f ? uiGold : uiGreen;
            Plate(s, 16f, 88f, 206f, 24f, SourceColor(4453159313425678991UL, 4568451461328509338UL),
                SourceColor(4417130516392957706UL, 4568451461320791818UL), A(color, .7f), 5f, 1f);
            s.TextF("火力", 24f, 92f, 1f, uiDim, -1); // slot 5863972
            float strong = Battle.Pressure.strongFirepower;
            s.Rect(54f, 97f, 58f, 7f, SourceColor(4417130516401346314UL, 4575657222442571530UL));
            float v = Binary32(firepower / WasmMax(strong, 1f));
            v = v < 0f ? 0f : WasmMin(v, 1f);
            if (v > 0f) {
                float t = WasmMax(v, 0f);
                Color fill = new Color(Binary32(uiGold.r + Binary32(t * Binary32(uiGreen.r - uiGold.r))),
                    Binary32(uiGold.g + Binary32(t * Binary32(uiGreen.g - uiGold.g))),
                    Binary32(uiGold.b + Binary32(t * Binary32(uiGreen.b - uiGold.b))),
                    Binary32(uiGold.a + Binary32(t * Binary32(uiGreen.a - uiGold.a))));
                s.Rect(55f, 98f, Binary32(v * 56f), 5f, fill);
            }
            Color tick = SourceColor(0UL, 4539628424389459968UL);
            s.Rect(68.5f, 97f, 1f, 7f, tick); s.Rect(83f, 97f, 1f, 7f, tick); s.Rect(97.5f, 97f, 1f, 7f, tick);
            s.TextF("推进", 120f, 92f, 1f, uiDim, -1); // slot 5863464
            s.TextF("×" + factor.ToString("0.00"), 152f, 92f, 1f, color, -1); // slots 5861808 / 5821904
            if (Battle.PierceDepth <= 0) return;
            Plate(s, 230f, 88f, 92f, 24f, SourceColor(4477658893514493133UL, 4568451461331864781UL),
                SourceColor(4435865490164012810UL, 4568451461323476173UL), A(uiCyan, .8f), 5f, 1f);
            s.TextF("渗透 " + Battle.PierceDepth.ToString() + " 层", 276f, 92f, 1f, uiCyan, 0); // slots 5863936 / 5818744
        }
    }
}
