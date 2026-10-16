using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54593. enter is unused by the original; sizes and positions follow
        // the WAT, including its minimum card height on short viewports.
        private void DrawShop(PixelSurface s, float slide, float enter)
        {
            float y = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Top + 2f) + 104f), x = OriginalMath.Binary32(slide + 24f);
            Title(s, "补给商店", OriginalMath.Binary32(slide + 270f), OriginalMath.Binary32(y - 4f), 2.1f, uiText, uiInk);
            y  =             OriginalMath.Binary32(y+(42f));
            DrawOffers(s, x, y);
            y  =             OriginalMath.Binary32(y+(184f));
            Plate(s, x, y, 492f, 100f, SourceColor(4471894287212169134UL, 4572054341873293066UL),
                SourceColor(4417130516419465708UL, 4572054341856515850UL), uiGold, 10f, 1f);
            Label(s, "每日福利", OriginalMath.Binary32(x + 16f), OriginalMath.Binary32(y + 8f), 1.2f, uiGoldHi, -1);
            Label(s, "看广告免费领取", OriginalMath.Binary32(x + 100f), OriginalMath.Binary32(y + 10f), 1f, uiDim, -1);
            int coinsLeft = unchecked(5 - Economy.Data.adCoins), fragmentsLeft = unchecked(3 - Economy.Data.adFragments);
            AdTile(s, 214, OriginalMath.Binary32(x + 12f), OriginalMath.Binary32(y + 32f), 228f, 1, "+200", "剩余 " + coinsLeft + "/5", coinsLeft > 0, uiGold);
            AdTile(s, 215, OriginalMath.Binary32(x + 252f), OriginalMath.Binary32(y + 32f), 228f, 2, "+5", "剩余 " + fragmentsLeft + "/3", fragmentsLeft > 0, uiCyan);
            string[] descriptions = { "打乱前三排候选", "任选前三排出击", "清敌击退并压制" };
            Color[] colors = { SourceColor(4566897280173272301UL, 4575657222468908470UL),
                SourceColor(4554746392497803987UL, 4575657222473777152UL), SourceColor(4544573556310147072UL, 4575657222460717241UL) };
            y  =             OriginalMath.Binary32(y+(112f));
            float height = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.SafeBottom - 96f) - y) - 212f);
            height = height < 170f ? 170f : OriginalMath.WasmMin(height, 262f);
            float icon = height < 220f ? 60f : 80f;
            for (int i = 0; i < 3; i++)
            {
                float px = OriginalMath.Binary32(x + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 168f)), flash = ShopBuyFlash(i);
                Color color = colors[i];
                Plate(s, px, y, 156f, height, Mix(new Color(.06f, .17f, .25f, .96f), color, OriginalMath.Binary32(flash * .4f)),
                    SourceColor(4435865490164012810UL, 4572774917138892718UL), A(color, .85f), 10f, 1f);
                float sweep = OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)i) * .3f) + OriginalMath.Binary32(ui * .35f)), 1.8f);
                if (sweep < 1f) s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(px + 8f) + OriginalMath.Binary32(sweep * 130f)), OriginalMath.Binary32(y + 4f), 5f, OriginalMath.Binary32(height - 8f),
                    SourceColor(4575657222473777152UL, 4435865490211995648UL));
                float bounce = OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui + ui) + OriginalMath.Binary32((float)i))) * 3f), cx = OriginalMath.Binary32(px + 78f);
                s.Glow(cx, OriginalMath.Binary32(y + 62f), 120f, 100f, A(color, .22f), 0f);
                if (i <= 1) s.DeckArt(i + 15, OriginalMath.Binary32(cx - OriginalMath.Binary32(icon * .5f)), OriginalMath.Binary32(OriginalMath.Binary32(y + 12f) + bounce), icon, icon, Color.white, true);
                else s.DeckArt(17, OriginalMath.Binary32(cx - OriginalMath.Binary32(icon * .55f)), OriginalMath.Binary32(OriginalMath.Binary32(y + 8f) + bounce), OriginalMath.Binary32(icon * 1.1f), OriginalMath.Binary32(icon * 1.1f), Color.white, true);
                Label(s, FleetCatalog.ItemNames[i], cx, OriginalMath.Binary32(OriginalMath.Binary32(y + icon) + 18f), 1.4f, Color.white);
                if (height >= 220f) s.TextF(descriptions[i], cx, OriginalMath.Binary32(OriginalMath.Binary32(y + icon) + 48f), .95f, uiDim, 0);
                Label(s, "库存 " + Economy.Data.items[i], cx, OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 76f), 1.15f, color);
                int price = FleetEconomy.ItemPrices[i]; bool available = Economy.Data.coins >= price;
                Button(s, 210 + i, OriginalMath.Binary32(px + 10f), OriginalMath.Binary32(OriginalMath.Binary32(y + height) - 50f), 136f, 40f, price.ToString(),
                    available ? Btn.Primary : Btn.Secondary, 2f, available, 1);
                if (flash > 0f) s.Ring(new Vector2(cx, OriginalMath.Binary32(y + 60f)), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - flash)) * 60f) + 30f), new Color(1f, 1f, 1f, flash), 3f, 28, 0f, 360f);
            }
            y  =             OriginalMath.Binary32(y+(OriginalMath.Binary32(height + 14f)));
            int bundlePrice = FleetEconomy.BundlePrice, coins = Economy.Data.coins;
            float t = OriginalMath.Binary32(ShopBuyFlash(3) * .3f); t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            Plate(s, x, y, 492f, 100f, new Color(OriginalMath.Binary32(OriginalMath.Binary32(t * .74f) + .26f), OriginalMath.Binary32(OriginalMath.Binary32(t * .9f) + .1f), OriginalMath.Binary32(OriginalMath.Binary32(t * .76f) + .24f), OriginalMath.Binary32(OriginalMath.Binary32(t * .04f) + .96f)),
                SourceColor(4392630932042730701UL, 4572774917137550541UL), SourceColor(4535389743705227264UL, 4575657222470816467UL), 10f, 1f);
            float left = OriginalMath.Binary32(x + 14f), row = OriginalMath.Binary32(y + 24f);
            float bounce0 = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.4f) + 0f));
            s.DeckArt(15, OriginalMath.Binary32(left + 0f), OriginalMath.Binary32(row + (OriginalMath.Binary32(bounce0 + bounce0))), 48f, 48f, Color.white, true);
            float bounce1 = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.4f) + 1f));
            s.DeckArt(16, OriginalMath.Binary32(left + 44f), OriginalMath.Binary32(row + (OriginalMath.Binary32(bounce1 + bounce1))), 48f, 48f, Color.white, true);
            float bounce2 = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(ui * 2.4f) + 2f));
            s.DeckArt(17, OriginalMath.Binary32(x + 100f), OriginalMath.Binary32(OriginalMath.Binary32(y + 20f) + (OriginalMath.Binary32(bounce2 + bounce2))), 54f, 54f, Color.white, true);
            Label(s, "战备礼包", OriginalMath.Binary32(x + 176f), OriginalMath.Binary32(y + 18f), 1.5f, Color.white, -1);
            Label(s, "三种道具各 ×2", OriginalMath.Binary32(x + 176f), OriginalMath.Binary32(y + 54f), 1.1f, uiDim, -1);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(x + 392f) + OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 5f)) * 3f) * .3f)), OriginalMath.Binary32(y - 12f), 72f, 28f,
                SourceColor(4518011146384441344UL, 4575657222458677658UL), SourceColor(4453159313464098816UL, 4575657222445255885UL), uiInk, 6f, 1f);
            Label(s, "7折", OriginalMath.Binary32(x + 428f), OriginalMath.Binary32(y - 7f), 1.2f, Color.white);
            Button(s, 213, OriginalMath.Binary32(x + 332f), OriginalMath.Binary32(y + 46f), 148f, 42f, bundlePrice.ToString(), coins >= bundlePrice ? Btn.Primary : Btn.Secondary, 2f, coins >= bundlePrice, 1);
            t = OriginalMath.Binary32(ShopBuyFlash(4) * .3f); t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
            y  =             OriginalMath.Binary32(y+(114f)); coins = Economy.Data.coins;
            Plate(s, x, y, 492f, 80f, new Color(OriginalMath.Binary32(OriginalMath.Binary32(t * .95f) + .05f), OriginalMath.Binary32(OriginalMath.Binary32(t * .82f) + .18f), OriginalMath.Binary32(OriginalMath.Binary32(t * .76f) + .24f), OriginalMath.Binary32(OriginalMath.Binary32(t * .04f) + .96f)),
                SourceColor(4435865490164012810UL, 4572774917138892718UL), uiCyan, 10f, 1f);
            s.Glow(OriginalMath.Binary32(x + 44f), OriginalMath.Binary32(y + 40f), 80f, 80f, A(uiCyan, .3f), 0f);
            s.DeckArt(2, left, OriginalMath.Binary32(y + 10f), 60f, 60f, Color.white, true);
            Label(s, "碎片补给 ×10", OriginalMath.Binary32(x + 88f), OriginalMath.Binary32(y + 14f), 1.4f, Color.white, -1);
            Label(s, "用于战机补给与装备补给", OriginalMath.Binary32(x + 88f), OriginalMath.Binary32(y + 46f), 1.05f, uiDim, -1);
            Button(s, 216, OriginalMath.Binary32(x + 332f), OriginalMath.Binary32(y + 19f), 148f, 42f, "500", coins > 499 ? Btn.Primary : Btn.Secondary, 2f, coins > 499, 1);
        }
        // f54593's inlined flash expression is 1-(age+age), not 1-age+age.
        private float ShopBuyFlash(int index)
        {
            if (buyFlash != index) return 0f;
            float age = Since(217); return Clamp01(OriginalMath.Binary32(1f - (OriginalMath.Binary32(age + age))));
        }

        // f54618. Disabled tiles remain hit-testable; the handler displays why.
        private void AdTile(PixelSurface s, int id, float x, float y, float width, int icon, string amount, string left, bool enabled, Color tint)
        {
            HitBox(s, id, x, y, width, 62f);
            float pulse = PressPulse(id);
            Plate(s, OriginalMath.Binary32(OriginalMath.Binary32(pulse * 3f) + x), OriginalMath.Binary32(OriginalMath.Binary32(pulse + pulse) + y), OriginalMath.Binary32(width + OriginalMath.Binary32(pulse * -6f)), OriginalMath.Binary32(OriginalMath.Binary32(pulse * -4f) + 62f),
                enabled ? new Color(.12f, .08f, .02f, 1f) : new Color(.06f, .06f, .06f, 1f),
                SourceColor(4369572502454916874UL, 4575657221408423936UL), enabled ? A(tint, .9f) : uiEdge, 8f, 1f);
            s.Glow(OriginalMath.Binary32(x + 34f), OriginalMath.Binary32(y + 31f), 70f, 60f, A(tint, enabled ? .3f : .05f), 0f);
            float intensity = enabled ? 1f : .5f;
            s.DeckArt(icon, OriginalMath.Binary32(x + 8f), OriginalMath.Binary32(y + 7f), 48f, 48f, new Color(intensity, intensity, intensity, 1f), true);
            s.Text(amount, OriginalMath.Binary32(x + 64f), OriginalMath.Binary32(y + 12f), 2f, enabled ? tint : uiDim, false);
            s.TextF(left, OriginalMath.Binary32(x + 64f), OriginalMath.Binary32(y + 38f), .95f, uiDim, -1);
            if (enabled)
            {
                float right = OriginalMath.Binary32(x + width);
                Plate(s, OriginalMath.Binary32(right - 52f), OriginalMath.Binary32(y + 14f), 42f, 34f, uiGoldHi, uiGoldLo, uiInk, 6f, 1f);
                AdIcon(s, OriginalMath.Binary32(right - 40f), OriginalMath.Binary32(y + 22f), 18f);
            }
            else Label(s, "明日", OriginalMath.Binary32(OriginalMath.Binary32(x + width) - 30f), OriginalMath.Binary32(y + 22f), 1f, uiDim);
        }
    }
}
