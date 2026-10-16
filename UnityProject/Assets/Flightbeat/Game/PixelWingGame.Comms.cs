using System;
using Flightbeat.Audio;
using Flightbeat.Economy;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private string outcomeLine = ""; // original +44; f54860
        private Phase outcomeFor; // +48
        internal ChipAudio audioChip; // +452
        internal int levelIndex; // +544
        private float clock; // +548

        // src: PixelWingGame.get_Economy f54836 / set_Economy f54837.
        public FleetEconomy Economy { get; internal set; }
        // src: PixelWingGame.get_LastReward f54842 / set_LastReward f54843.
        public FleetReward LastReward { get; private set; }

        // src: IL2CPP float-to-i32 guards in f54496/f54504.
        private static int CommsInt(float value) => Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;

        // src: PixelWingGame.get_AssistantFrame f54496, f10845 exact fmodf.
        public int AssistantFrame
        {
            get
            {
                int blink = OriginalMath.Remainder(clock, 3.7f) < 0.14f ? 2 : 0;
                if (spokenCharacters >= dialogue.Length) return blink;
                return (blink | (CommsInt(OriginalMath.Binary32(dialogueAge * 11f)) & 1)) ^ 1;
            }
        }

        // src: PixelWingGame.UpdateComms f54503. At most one UTF-16 code unit
        // per call, including long frames; no catch-up loop or grapheme merging.
        private void UpdateComms(float dt)
        {
            if (lesson != null)
            {
                lesson.Tick(dt);
                lesson.NoteHint();
                tutorialStep = lesson.Step;
                narration.Tick(dt);
                if (lesson.IndependentChoice) Economy.CompleteIndependentTutorial();
            }
            string line = OutcomeLine();
            if (line.Length == 0 && Battle.State != Phase.Intro && narration != null) line = narration.Line;
            int spoken = 0;
            float age;
            if (line == dialogue)
            {
                line = dialogue;
                spoken = spokenCharacters;
                age = dialogueAge;
            }
            else
            {
                glyphAt = 0.05f; // f54503 i32 1028443341
                spokenCharacters = 0;
                dialogue = line;
                age = 0f;
            }
            dialogueAge = OriginalMath.Binary32(age + dt);
            if (spoken >= line.Length || !(dialogueAge >= glyphAt)) return;
            spokenCharacters = unchecked(spoken + 1);
            char glyph = line[spoken];
            float gap = 0.12f;
            if (!OriginalCharacters.IsPunctuation(glyph) && !OriginalCharacters.IsWhiteSpace(glyph))
            {
                audioChip.SpeakGlyph(glyph, spokenCharacters);
                gap = 0.03f;
            }
            glyphAt = OriginalMath.Binary32(dialogueAge + gap);
        }

        // src: PixelWingGame.OutcomeLine f54504. Text slots are from original
        // memory-symbols.json; the phase cache intentionally survives ResetComms.
        private string OutcomeLine()
        {
            Phase phase = Battle.State;
            if (unchecked((uint)((int)phase - 3)) >= 3u)
            {
                outcomeFor = Phase.Intro;
                return ""; // 5817548
            }
            if (outcomeFor == phase) return outcomeLine;
            outcomeFor = phase;
            int pick = unchecked(CommsInt(OriginalMath.Binary32(Battle.Time * 7f)) + levelIndex + 1);
            int sign = pick >> 31;
            uint variant = unchecked((uint)((pick + sign) ^ sign)) % 3u;
            if (phase == Phase.ReviveOffer)
            {
                outcomeLine = variant == 0 ? "警报！敌阵突破了防线！\n救援舰队已就位，请下令！" // 5864480
                    : variant == 1 ? "防线告急，指挥官！\n呼叫救援，把它们推回去！" // 5864836
                    : "敌群压上来了！\n只要一声令下，援军马上到。"; // 5863520
            }
            else if (phase == Phase.Won)
            {
                if (lesson != null && lesson.Chapter == 1) outcomeLine = "看前排颜色，派同色战机。\n三架同色合成，变强又腾位。"; // 5864132
                else if (lesson != null && lesson.Chapter == 2) outcomeLine = "数字是弹药，弹尽冲锋腾位。\n接上新战机，保持火力。"; // 5863564
                else if (Battle.FinaleUsed) outcomeLine = "决战命中，敌群全灭！\n这一击会写进舰队战史。"; // 5862688
                else if (Battle.Revived) outcomeLine = "险胜！援军没有白来，\n舰队全员平安返航。"; // 5864852
                else if (LastReward != null && LastReward.zoneComplete >= 0) outcomeLine = WarZones.Names[LastReward.zoneComplete] + "全线收复！\n战区奖励已送达指挥舱。"; // 5862596
                else if (LastReward != null && LastReward.streak >= 3) outcomeLine = LastReward.streak.ToString() + " 连胜！保持节奏，\n下一关奖励加成更高。"; // 5818844
                else if (Battle.Time <= 180f) outcomeLine = "漂亮的闪电战！\n司令部都在为你鼓掌。"; // 5863968
                else outcomeLine = variant != 0 ? "干得漂亮！星海防线\n又安全了一段时间。" // 5863244
                    : "敌阵已清空，指挥官。\n舰队状态良好，随时再战。"; // 5863540
            }
            else
            {
                int cleared = CommsInt(OriginalMath.Binary32((float)Math.Floor(OriginalMath.Binary32(Battle.Progress * 100f))));
                if (cleared >= 80) outcomeLine = "只差一点！已清除" + cleared.ToString() + "%，\n再来一次一定能拿下。"; // 5862904/5819436
                else if (Battle.Revived) outcomeLine = "援军也没能守住……\n先回港整备，我等你的命令。"; // 5863480
                else outcomeLine = "防线失守，舰队正在撤回。\n升级战机后，我们再打回来！"; // 5864844
            }
            return outcomeLine;
        }
    }
}
