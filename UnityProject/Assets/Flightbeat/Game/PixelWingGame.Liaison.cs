using Flightbeat.Economy;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private float liaisonGlyphAt, liaisonCheck = 1f, homeEnteredAt = -10f; // f54860 +184/+188/+192
        private int liaisonBriefing, lastOutcome; // +200/+204
        // src: f54861, static +8 string literals in authored order.
        private static readonly string[] Briefings =
        {
            "星区雷达一切正常。\n舰队随时可以出击，指挥官。",
            "敌群越靠近防线越快，\n记得先合成、再压制。",
            "同色三架会自动合成，\nMAX 战机还会发射导弹。",
            "打到最后敌人不足一千只时，\n别忘了还有「决战」。"
        };

        // src: BetterModuleWaiting f54662. Compare first rarity-sorted candidate.
        private bool BetterModuleWaiting()
        {
            for (int slot = 0; slot < 5; slot++)
            {
                var available = Economy.ForSlot(slot);
                if (available.Count == 0) continue;
                FleetCore equipped = Economy.InSlot(slot);
                if (equipped == null || available[0].quality > equipped.quality) return true;
            }
            return false;
        }

        // src: PickLiaison f54663. Earlier topics take priority over all later ones.
        private int PickLiaison()
        {
            FleetProfile data = Economy.Data;
            if (data.unlocked == 1) return 0;
            if (data.unlocked == 2) return 1;
            if (!data.growthTutorial) return 2;
            if (OriginalMath.Binary32(ui - homeEnteredAt) < 9f)
            {
                if (lastOutcome > 0) return 4;
                if (lastOutcome < 0) return Economy.PartsReady > 0 ? 10 : 5;
            }
            if (Economy.CanSign) return 6;
            if (Economy.DailyReady > 0) return 7;
            if (Economy.CanAssemble(1) || Economy.CanAssemble(2) || Economy.CanAssemble(3) || Economy.CanAssemble(4)) return 12;
            if (BetterModuleWaiting()) return 8;
            if (Economy.PartsReady > 0) return 10;
            if (Economy.Data.fragments > 9) return 9;
            for (int item = 0; item < 3; item++)
                if (Economy.Data.items[item] == 0 && Economy.Data.coins >= FleetEconomy.ItemPrices[item]) return 11;
            return 3;
        }

        // src: LiaisonLine f54664. Original literals, including line breaks.
        private string LiaisonLine(int topic)
        {
            int level = Economy.Data.unlocked;
            level = level <= 0 ? 1 : level > Levels.Length ? Levels.Length : level;
            switch (topic)
            {
                case 0: return "指挥官，先来一次试飞。\n你派战机，它们负责瞄准攻击。";
                case 1: return "颜色和合成都认识了。\n下一关练习弹药与接力出击。";
                case 2: return "两次试飞完成！机库到了新部件，\n升级机首就能解锁「渗透」。";
                case 4: return "凯旋归来，指挥官！\n下一片星域在第" + level.ToString() + "关等我们。";
                case 5: return "别灰心，指挥官。\n整备好舰队，再去夺回第" + level.ToString() + "关。";
                case 6: return "今天的签到补给已经到港，\n记得去签收。";
                case 7: return "今日作战任务有奖励待领，\n去任务面板签收吧。";
                case 8: return "机库里有更强的模块还没装上，\n装备后再出击更稳妥。";
                case 9: return "碎片够抽一次补给了，\n新战机的部件就在补给舱里。";
                case 10: return lastOutcome < 0 && OriginalMath.Binary32(ui - homeEnteredAt) < 9f
                        ? "上一战吃了点亏……\n先去机库升级部件，我们再上！" : "重复部件够升级了，\n火力和外观都会进化。";
                case 11: return "道具库存见底了，\n补给商店可以补货，指挥官。";
                case 12: return "新战机的五个部件已经集齐，\n快去机库组装！";
                default: return Briefings[liaisonBriefing % Briefings.Length];
            }
        }

        // src: UpdateLiaison f54665. At most one UTF-16 code unit per update.
        private void UpdateLiaison(float dt)
        {
            liaisonAge  =             OriginalMath.Binary32(liaisonAge+(dt));
            liaisonCheck  =             OriginalMath.Binary32(liaisonCheck+(dt));
            if (liaisonCheck >= .5f)
            {
                liaisonCheck = 0f;
                int topic = PickLiaison();
                string line = LiaisonLine(topic);
                if (liaisonTopic != topic || line != liaisonText)
                {
                    liaisonChars = 0;
                    liaisonText = line;
                    liaisonTopic = topic;
                    liaisonAge = 0f;
                    liaisonGlyphAt = .25f; // f54665 paired i64 store 4503599627370496000.
                }
            }
            int spoken = liaisonChars;
            string text = liaisonText;
            if (spoken >= text.Length || !(liaisonAge >= liaisonGlyphAt)) return;
            liaisonChars = unchecked(spoken + 1);
            char glyph = text[spoken];
            float gap = .12f;
            if (!OriginalCharacters.IsPunctuation(glyph) && !OriginalCharacters.IsWhiteSpace(glyph))
            {
                gap = .045f;
                if (Page == DeckPage.Bridge && deckOverlay == DeckOverlay.None) audioChip.SpeakGlyph(glyph, liaisonChars);
            }
            liaisonGlyphAt = OriginalMath.Binary32(liaisonAge + gap);
        }
    }
}
