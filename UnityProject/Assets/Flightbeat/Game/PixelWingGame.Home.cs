using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54860: paired store at +144 is previousPage=1, launchAt=-1.
        private DeckPage previousPage = DeckPage.Bridge;
        private float launchAt = -1f;
        private int homeSwapDir;
        // f54591 InitializeArray field 5816792, metadata offset 0x370050.
        private static readonly float[] HomeBurners = { .255f, .5f, .745f };

        // src: DrawHome f54591. The original enter parameter is unused.
        private void DrawHome(PixelSurface s, float slide, float enter)
        {
            float top = OriginalMath.Binary32(PageTop - 4f), cx = OriginalMath.Binary32(slide + 270f), bottom = OriginalMath.Binary32(NavTop - 8f);
            int unlocked = Economy.Data.unlocked;
            unlocked = unlocked <= 0 ? 1 : unlocked > Levels.Length ? Levels.Length : unlocked;
            DrawZoneCard(s, cx, top, unlocked - 1);
            float height = OriginalMath.Binary32(OriginalMath.Binary32(bottom - top) + -300f);
            height = height < 380f ? height : 380f;
            float half = OriginalMath.Binary32(height * .5f), cy = OriginalMath.Binary32(OriginalMath.Binary32(top + 74f) + half);
            s.Glow(cx, cy, OriginalMath.Binary32(height * 1.5f), OriginalMath.Binary32(height * 1.3f),
                SourceColor(4333543704410193920UL, 4557642823921681039UL), 0f);
            s.Glow(cx, cy, OriginalMath.Binary32(height * 1.1f), OriginalMath.Binary32(height * .95f),
                SourceColor(4539628425431325082UL, 4477658893548047565UL), 0f);
            float ground = OriginalMath.Binary32(OriginalMath.Binary32(height * .47f) + cy);
            float bob = OriginalMath.Sin(OriginalMath.Binary32(ui * 1.6f));
            // f54591 third radius operand is 0x3e6147ad, not decimal .22f.
            float[] radii = { OriginalMath.Binary32(height * .42f), OriginalMath.Binary32(height * .32f), OriginalMath.Binary32(height * 0.2199999839067459f) };
            ulong[] ringBA = { 4518011146384441344UL, 4506481929448587264UL, 4486305805179551744UL };
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = radii[ring];
                for (int i = 0; i < 40; i++)
                {
                    float angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) / 40f) * 3.1415927410125732f); angle  = OriginalMath.Binary32(angle+(angle));
                    Vector2 start = new Vector2(OriginalMath.Binary32(cx + OriginalMath.Binary32(radius * OriginalMath.Cos(angle))), OriginalMath.Binary32(ground + OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Sin(angle)) * .22f)));
                    angle = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)(i + 1)) / 40f) * 3.1415927410125732f); angle  = OriginalMath.Binary32(angle+(angle));
                    Vector2 end = new Vector2(OriginalMath.Binary32(cx + OriginalMath.Binary32(radius * OriginalMath.Cos(angle))), OriginalMath.Binary32(ground + OriginalMath.Binary32(OriginalMath.Binary32(radius * OriginalMath.Sin(angle)) * .22f)));
                    s.Segment(start, end, 1.5f, SourceColor(4568451461336897946UL, ringBA[ring]));
                }
            }
            float shipTop = OriginalMath.Binary32(cy - half);
            s.GlowLine(new Vector2(cx, ground), new Vector2(cx, shipTop), half,
                SourceColor(4568451461336897946UL, 1065353216u, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui + ui)) * .04f) + .05f)));
            float scan = OriginalMath.Remainder(OriginalMath.Binary32(ui * .5f), 1f), lowerOffset = OriginalMath.Binary32(height * .45f);
            s.Rect(OriginalMath.Binary32(cx - lowerOffset), OriginalMath.Binary32(shipTop + OriginalMath.Binary32(height * scan)), OriginalMath.Binary32(height * .9f), 1f,
                SourceColor(4575657222465388544UL, 1065353216u, OriginalMath.Binary32((OriginalMath.Binary32(1f - scan)) * .2f)));
            bob  =             OriginalMath.Binary32(bob*(6f));
            float rise = 0f, shake = 0f, launchAge = -1f, launching = 0f;
            int fighter;
            if (launchAt >= 0f && (launchAge = OriginalMath.Binary32(ui - launchAt)) >= 0f)
            {
                float flame = Clamp01(OriginalMath.Binary32(launchAge / .2f));
                if (launchAge < .3f) shake = OriginalMath.Binary32(OriginalMath.Binary32(flame * OriginalMath.Sin(OriginalMath.Binary32(ui * 95f))) * 2.4f);
                if (launchAge > .3f)
                {
                    rise = Clamp01(OriginalMath.Binary32((OriginalMath.Binary32(launchAge + -.3f)) / .55f));
                    rise = OriginalMath.Binary32(OriginalMath.Binary32(rise * (OriginalMath.Binary32(rise * rise))) * 1400f);
                }
                fighter = Economy.ActiveFighter;
                DrawAfterburner(s, OriginalMath.Binary32(cx + shake), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(lowerOffset + cy) + bob) - rise), height, flame, rise, launchAge);
                launching = 1f;
            }
            else fighter = Economy.ActiveFighter;
            float swap = Clamp01(OriginalMath.Binary32(Since(482) / .35f)), remaining = OriginalMath.Binary32(1f - Clamp01(swap));
            DrawFighterArt(s, fighter, OriginalMath.Binary32(OriginalMath.Binary32(cx + shake) + OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(remaining * (OriginalMath.Binary32(remaining * remaining))) + -1f) + 1f)) * OriginalMath.Binary32((float)homeSwapDir)) * 80f)),
                OriginalMath.Binary32(OriginalMath.Binary32(cy + bob) - rise), OriginalMath.Binary32(height * .96f), 0f, -1, swap, false, true);
            if (launchAge < 0f) DrawHomeFighterPicker(s, cx, cy, height);
            if (launchAge > .3f) DrawSpeedLines(s, OriginalMath.Binary32(launchAge + -.3f));

            float statsY = OriginalMath.Binary32(OriginalMath.Binary32(half + cy) + 12f), statsX = OriginalMath.Binary32(cx + -150f);
            Plate(s, statsX, statsY, 300f, 42f,
                SourceColor(4471894287214182400UL, 4568451461309383311UL),
                SourceColor(4417130516420807885UL, 4568451461295625994UL), A(uiGold, .8f), 8f);
            string name = FleetCatalog.FighterNames[fighter] + " LV" + Economy.FighterLevel(fighter).ToString();
            float textY = OriginalMath.Binary32(statsY + 11f), textX = OriginalMath.Binary32(cx + -72f);
            s.TextF(name, OriginalMath.Binary32(textX + 1.125f), OriginalMath.Binary32(textY + 1.125f), 1.25f, new Color(0f, 0f, 0f, .7f), 0);
            s.TextF(name, textX, textY, 1.25f, Color.white, 0);
            s.Rect(OriginalMath.Binary32(cx + -6f), OriginalMath.Binary32(statsY + 9f), 2f, 24f, A(uiGold, .5f));
            s.TextF("战力", OriginalMath.Binary32(OriginalMath.Binary32(cx + 30f) + 1.08f), OriginalMath.Binary32(textY + 1.08f), 1.2f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiGold.a * .7f)), 0);
            s.TextF("战力", OriginalMath.Binary32(cx + 30f), textY, 1.2f, uiGold, 0);
            s.Text(Economy.Power.ToString(), OriginalMath.Binary32(cx + 58f), OriginalMath.Binary32(statsY + 12f), 2f, uiGoldHi, false);
            float launchTop = OriginalMath.Binary32(bottom + -128f), talkBottom = OriginalMath.Binary32(launchTop + -18f), talkTop = OriginalMath.Binary32(statsY + 54f);
            if (OriginalMath.Binary32(talkBottom - talkTop) >= 80f && launchAge < 0f)
                DrawLiaison(s, OriginalMath.Binary32(cx + -230f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(talkBottom + talkTop)) * .5f) + -38f), 460f);

            float buttonY = OriginalMath.Binary32(OriginalMath.Binary32(bottom + -124f) + -4f);
            HitBox(s, 110, statsX, buttonY, 300f, 124f);
            float press = OriginalMath.Binary32(PressPulse(110) * .05f), pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .5f) + .5f);
            s.Glow(cx, OriginalMath.Binary32(buttonY + 62f), 400f, 180f,
                SourceColor(4543231306015703040UL, 1041865114u, OriginalMath.Binary32(OriginalMath.Binary32(launching * .25f) + (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .07f) + .1f)))), 0f);
            s.Glow(cx, OriginalMath.Binary32(buttonY + 62f), 330f, 130f,
                SourceColor(4575657222453644493UL, 1063675494u, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .06f) + .08f)), 0f);
            float buttonW = OriginalMath.Binary32((OriginalMath.Binary32(1f - press)) * 300f), buttonH = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(press * -1.2f) + 1f)) * 124f);
            float buttonX = OriginalMath.Binary32(cx - OriginalMath.Binary32(buttonW * .5f)), artY = OriginalMath.Binary32(buttonY + OriginalMath.Binary32((OriginalMath.Binary32(124f - buttonH)) * .5f));
            s.DeckArt(18, buttonX, artY, buttonW, buttonH, Color.white, false);
            foreach (float position in HomeBurners)
            {
                float intensity = OriginalMath.Binary32(OriginalMath.Binary32(launching * .6f) + (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(position * 9f) + OriginalMath.Binary32(ui * 6f))) * .25f) + .35f)));
                float bx = OriginalMath.Binary32(buttonX + OriginalMath.Binary32(buttonW * position)), by = OriginalMath.Binary32(OriginalMath.Binary32(buttonH * .8f) + artY);
                s.Glow(bx, by, OriginalMath.Binary32(buttonW * .16f), OriginalMath.Binary32(buttonH * .3f),
                    SourceColor(4543231306015703040UL, 1039516303u, OriginalMath.Binary32(intensity * .45f)), 0f);
                s.Glow(bx, by, OriginalMath.Binary32(buttonW * .08f), OriginalMath.Binary32(buttonH * .14f),
                    SourceColor(4568451461351997440UL, 1056964608u, OriginalMath.Binary32(intensity * .5f)), 0f);
            }
            float innerTop = OriginalMath.Binary32(OriginalMath.Binary32(buttonH * .32f) + artY), innerBottom = OriginalMath.Binary32(OriginalMath.Binary32(buttonH * .62f) + artY);
            float middle = OriginalMath.Binary32((OriginalMath.Binary32(innerTop + innerBottom)) * .5f), sweep = OriginalMath.Remainder(OriginalMath.Binary32(ui * .4f), 1.6f);
            if (sweep < 1f)
            {
                float distance = Math.Abs(OriginalMath.Binary32(sweep + -.5f)), from = OriginalMath.Binary32(OriginalMath.Binary32(buttonW * .13f) + buttonX);
                s.Glow(OriginalMath.Binary32(from + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(buttonW * .868f) + buttonX) - from)) * (sweep < 0f ? 0f : OriginalMath.WasmMin(sweep, 1f)))),
                    middle, 46f, OriginalMath.Binary32(innerBottom - innerTop),
                    SourceColor(4575657222467066266UL, 1065353216u, OriginalMath.Binary32((OriginalMath.Binary32(1f - (OriginalMath.Binary32(distance + distance)))) * .2f)), 0f);
            }
            string digits = WideDigits(unlocked.ToString("D2"));
            float digitsW = PixelSurface.Measure(digits, 2.55f), wordW = PixelSurface.Measure("第", 1.55f);
            s.Glow(cx, middle, OriginalMath.Binary32(digitsW + 80f), OriginalMath.Binary32(OriginalMath.Binary32(innerBottom - innerTop) + 26f),
                SourceColor(4557642823964295168UL, 1048576000u, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .1f) + .22f)), 0f);
            // f54591 WAT: digitsW + ((wordW + wordW) + 20), preserving each f32.add.
            float allW = OriginalMath.Binary32(digitsW + OriginalMath.Binary32(OriginalMath.Binary32(wordW + wordW) + 20f)), left = OriginalMath.Binary32(cx - OriginalMath.Binary32(allW * .5f));
            float wordY = OriginalMath.Binary32(OriginalMath.Binary32(middle + -10.84999942779541f) + 1f);
            Color wordFace = SourceColor(4573495496711707034UL, 4575657222473777152UL);
            Color wordEdge = SourceColor(4453159312402939904UL, 4575657222450960138UL);
            Title(s, "第", OriginalMath.Binary32(OriginalMath.Binary32(wordW * .5f) + left), wordY, 1.55f, wordFace, wordEdge);
            float digitsY = OriginalMath.Binary32(middle + -17.85f);
            Title(s, digits, cx, digitsY, 2.55f, uiGoldHi, SourceColor(4464688526094080410UL, 4575657221408423936UL));
            s.TextF(digits, cx, OriginalMath.Binary32(digitsY + -1f), 2.55f, SourceColor(4573495496714223616UL, 4543231306012683141UL), 0);
            Title(s, "关", OriginalMath.Binary32(OriginalMath.Binary32(allW + left) - OriginalMath.Binary32(wordW * .5f)), wordY, 1.55f, wordFace, wordEdge);
            string sector = SectorTitle(unlocked);
            if (sector.Length > 0)
            {
                float width = OriginalMath.Binary32(PixelSurface.Measure(sector, 1.05f) + 28f), y = OriginalMath.Binary32(buttonY + -12f);
                Plate(s, OriginalMath.Binary32(cx + OriginalMath.Binary32(width * -.5f)), y, width, 22f,
                    SourceColor(4489188110453031567UL, 4572054341897116713UL),
                    SourceColor(4441630096484456202UL, 4572054341887050383UL), A(uiCyan, .85f), 5f);
                s.TextF(sector, cx, OriginalMath.Binary32(y + 3f), 1.05f, uiCyan, 0);
            }
            if (Economy.Data.unlocked >= 2)
            {
                int daily = Economy.DailyReady;
                float offset = OriginalMath.Sin(OriginalMath.Binary32(ui * 2.2f)), y = OriginalMath.Binary32(launchTop + 50f);
                RoundButton(s, 111, OriginalMath.Binary32(cx + -196f), y, 36f, 0, "每日任务", uiGreen, daily, OriginalMath.Binary32(offset + offset));
                offset = OriginalMath.Sin(OriginalMath.Binary32(ui * 2.2f));
                s.Sprite(13, OriginalMath.Binary32(OriginalMath.Binary32(cx + -196f) + -24f), OriginalMath.Binary32(OriginalMath.Binary32(launchTop + 26f) + OriginalMath.Binary32(offset + offset)), 48f, 48f, Color.white, 0f);
                int sign = Economy.CanSign ? 1 : 0;
                offset = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.2f) + 1f));
                RoundButton(s, 112, OriginalMath.Binary32(cx + 196f), y, 36f, 0, "签到", uiGold, sign, OriginalMath.Binary32(offset + offset));
                offset = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.2f) + 1f));
                s.Sprite(14, OriginalMath.Binary32(OriginalMath.Binary32(cx + 196f) + -24f), OriginalMath.Binary32(OriginalMath.Binary32(launchTop + 26f) + OriginalMath.Binary32(offset + offset)), 48f, 48f, Color.white, 0f);
            }
        }
    }
}
