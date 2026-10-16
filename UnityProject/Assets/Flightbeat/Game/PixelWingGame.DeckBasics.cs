using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private float shownCoins, shownFragments; // f54860 defaults, +136 / +140
        private string sectorTitle = ""; // f54860 +160
        private int sectorTitleFor = -1; // f54860 +164
        private float DeckTop => OriginalMath.Binary32(ScreenLayout.Top + 2f); // f54550
        private float PageTop => OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Top + 2f) + 104f); // f54587
        private float NavTop => OriginalMath.Binary32(ScreenLayout.SafeBottom + -96f); // f54588

        // f54589. Original bridge background and animated grid for other deck pages.
        private void DrawDeckBackground(PixelSurface s)
        {
            s.MapLayout = false;
            s.Environment(0, ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height);
            float alpha = Page == DeckPage.Bridge ? .18f : .9f;
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, ScreenLayout.Height, new Color(.01f, .03f, .06f, alpha));
            if (Page != DeckPage.Bridge)
            {
                for (float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(clock * 14f)) % 40f) + -40f); y < ScreenLayout.Height; y  = OriginalMath.Binary32(y+(40f)))
                    s.Rect(ScreenLayout.Left, y, ScreenLayout.Width, 1f, new Color(.2f, .6f, .8f, .06f));
                for (int x = 0; ; x = unchecked(x + 40))
                {
                    s.Rect(OriginalMath.Binary32((float)x), 0f, 1f, ScreenLayout.Height, new Color(.2f, .6f, .8f, .05f));
                    if (x > 499) break;
                }
            }
            s.Glow(270f, OriginalMath.Binary32(ScreenLayout.Height * .5f), OriginalMath.Binary32(ScreenLayout.Width * 1.6f),
                OriginalMath.Binary32(ScreenLayout.Height * 1.2f), new Color(0f, 0f, 0f, 0f), 0f);
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, OriginalMath.Binary32(ScreenLayout.Top + 60f), new Color(.01f, .02f, .05f, .55f));
        }
        // f54594.
        private void DrawTopBar(PixelSurface s)
        {
            float y = DeckTop;
            HitBox(s, 100, 10f, y, 54f, 62f);
            float pulse = PressPulse(100), offset = OriginalMath.Binary32(pulse + pulse), size = OriginalMath.Binary32(OriginalMath.Binary32(pulse * -4f) + 44f);
            Plate(s, OriginalMath.Binary32(offset + 14f), OriginalMath.Binary32(OriginalMath.Binary32(y + 2f) + offset), size, size, uiPanelHi, uiPanel, uiEdge, 7f, 1f);
            s.DeckArt(3, 18f, OriginalMath.Binary32(y + 6f), 36f, 36f, Color.white, true);
            Label(s, "设置", 36f, OriginalMath.Binary32(y + 48f), 1f, uiDim);
            float energyY = OriginalMath.Binary32(y + 44f);
            float width = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.RowRight(y, energyY) + -68f) + -8f)) * .5f);
            width = width < 104f ? 104f : OriginalMath.WasmMin(width, 150f);
            ResourcePill(s, 101, 68f, OriginalMath.Binary32(y + 3f), width, 1, Economy.Data.coins, ref shownCoins, uiGold, true);
            ResourcePill(s, 102, OriginalMath.Binary32(OriginalMath.Binary32(width + 68f) + 8f), OriginalMath.Binary32(y + 3f), width, 2, Economy.Data.fragments, ref shownFragments, uiCyan, true);
            DrawEnergyPill(s, 68f, energyY, width);
        }
        // f54576.
        private static string Clock(int seconds)
        {
            seconds = seconds > 0 ? seconds : 0;
            if (seconds <= 3599) return (seconds / 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
            return (seconds / 3600).ToString() + ":" + ((seconds / 60) % 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
        }
        // f54577. Bolt geometry and line order are original, over icon 13's authored bounds.
        private void EnergyIcon(PixelSurface s, float cx, float cy, float size, bool unlimited = false)
        {
            Color tint = unlimited ? uiGold : uiGreen;
            s.Glow(cx, cy, OriginalMath.Binary32(size * 1.6f), OriginalMath.Binary32(size * 1.6f), A(tint, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .1f) + .3f)), 0f);
            DeckIcon(s, 13, cx, cy, size, Color.white);
            Color bolt = unlimited ? uiGoldHi : new Color(.666666687f, 1f, .470588237f, 1f);
            float unit = OriginalMath.Binary32(size / 34f), y = OriginalMath.Binary32(unit + cy), bottom = OriginalMath.Binary32(OriginalMath.Binary32(unit * 10f) + cy), left = OriginalMath.Binary32(cx - OriginalMath.Binary32(unit * 3f));
            float right = OriginalMath.Binary32(OriginalMath.Binary32(unit + unit) + cx), middle = OriginalMath.Binary32(cx + OriginalMath.Binary32(unit * -4f)), top = OriginalMath.Binary32(cy + OriginalMath.Binary32(unit * -9f)), upper = OriginalMath.Binary32(OriginalMath.Binary32(unit * 3f) + cx);
            var p0 = new Vector2(upper, top);
            var p1 = new Vector2(middle, y);
            var p2 = new Vector2(right, y);
            var p3 = new Vector2(left, bottom);
            s.Segment(p0, p1, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p1, p2, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p2, p3, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p0, p1, OriginalMath.Binary32(unit * 1.8f), bolt);
            s.Segment(p1, p2, OriginalMath.Binary32(unit * 1.8f), bolt);
            s.Segment(p2, p3, OriginalMath.Binary32(unit * 1.8f), bolt);
        }
        // f54578. Keep the two independent UnlimitedSeconds reads in the unlimited branch.
        private void DrawEnergyPill(PixelSurface s, float x, float y, float w)
        {
            HitBox(s, 500, x, y, w, 34f);
            float pulse = PressPulse(500);
            bool unlimited = Economy.UnlimitedEnergy;
            int energy = Economy.Energy;
            Color tint = unlimited ? uiGold : energy <= 0 ? uiRed : uiGreen;
            float twice = OriginalMath.Binary32(pulse + pulse);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 12f) + twice), OriginalMath.Binary32(OriginalMath.Binary32(y + 3f) + pulse), OriginalMath.Binary32(OriginalMath.Binary32(w + -12f) + OriginalMath.Binary32(pulse * -4f)), OriginalMath.Binary32(28f - twice),
                new Color(.02f, .07f, .12f, .92f), new Color(.01f, .04f, .08f, .92f), A(tint, .75f), 5f, 1f);
            EnergyIcon(s, OriginalMath.Binary32(x + 16f), OriginalMath.Binary32(y + 17f), 30f, unlimited);
            float tx = OriginalMath.Binary32(x + 34f);
            if (unlimited)
            {
                float lx = OriginalMath.Binary32(tx + 2f), ly = OriginalMath.Binary32(y + 9f);
                s.TextF("无限", OriginalMath.Binary32(lx + 1.035f), OriginalMath.Binary32(ly + 1.035f), 1.15f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiGoldHi.a * .7f)), -1);
                s.TextF("无限", lx, ly, 1.15f, uiGoldHi, -1);
                string clockText = Clock(Economy.UnlimitedSeconds);
                float textWidth = PixelSurface.Measure(Clock(Economy.UnlimitedSeconds), 1f);
                s.Text(clockText, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(x + w) + -8f) - textWidth), OriginalMath.Binary32(y + 13f), 1f, uiGold, false);
                return;
            }
            s.Text(energy.ToString() + "/" + 5.ToString(), OriginalMath.Binary32(tx + 2f), OriginalMath.Binary32(y + 10f), 2f,
                new Color(1f, energy > 0 ? 1f : .55f, energy > 0 ? 1f : .5f, 1f), false);
            int seconds = Economy.EnergyNextSeconds;
            if (seconds > 0)
            {
                string clockText = Clock(seconds);
                float width = PixelSurface.Measure(clockText, 1f);
                s.Text(clockText, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(x + w) + -26f) - width), OriginalMath.Binary32(y + 13f), 1f, uiDim, false);
            }
            float px = OriginalMath.Binary32(OriginalMath.Binary32(x + w) + -22f), py = OriginalMath.Binary32(y + 8f);
            s.Rect(px, py, 18f, 18f, A(tint, .9f));
            s.Rect(OriginalMath.Binary32(px + 2f), OriginalMath.Binary32(py + 2f), 14f, 14f, new Color(.03f, .1f, .14f, 1f));
            s.Rect(OriginalMath.Binary32(px + 4f), OriginalMath.Binary32(py + 8f), 10f, 2f, tint);
            s.Rect(OriginalMath.Binary32(px + 8f), OriginalMath.Binary32(py + 4f), 2f, 10f, tint);
        }
        // f54631.
        private static string WideDigits(string ascii)
        {
            char[] chars = ascii.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] >= '0' && chars[i] <= '9') chars[i] = "０１２３４５６７８９"[chars[i] - '0'];
            return new string(chars);
        }
        // f54608. Preserve cache-before-load and original asset unload behavior.
        private string SectorTitle(int sector)
        {
            if (sector == sectorTitleFor) return sectorTitle;
            sectorTitleFor = sector;
            sectorTitle = "";
            int index = unchecked(sector - 1);
            index = index < 0 ? 0 : index > Levels.Length - 1 ? Levels.Length - 1 : index;
            TextAsset asset = Resources.Load<TextAsset>("Levels/" + Levels[index]);
            if (asset)
            {
                LevelData level = JsonUtility.FromJson<LevelData>(asset.text);
                if (level != null && !String.IsNullOrEmpty(level.title)) sectorTitle = level.title;
                Resources.UnloadAsset(asset);
            }
            return sectorTitle;
        }
    }
}
