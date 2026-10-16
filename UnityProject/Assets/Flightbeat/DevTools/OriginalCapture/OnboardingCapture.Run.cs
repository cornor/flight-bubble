using System;
using System.Collections;
using System.IO;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public sealed partial class OnboardingCapture
    {
        // f54342/f54359. Each yield corresponds to source states 1..47;
        // screenshot sizes, checks, input order and timing come from the WAT.
        private IEnumerator Run()
        {
            game.PrepareOnboardingEvidence();
            yield return null; // 1
            Check(!game.InHome && game.Battle.State == Phase.Ready && !game.Paused,
                "fresh boot opens the playable invader without a home or opening gate");
            Check(game.Battle.Total == 302, "first pattern uses 302 authored enemies");
            yield return Shot("01-invader"); // 2
            Check(game.Dialogue.Contains("自动攻击前排的同色敌人"), "opening explains the action, automatic fire and matching colour");
            float began = Time.realtimeSinceStartup;
            Lane(0);
            yield return Until(() => game.Battle.Remaining < game.Battle.Total, "first real hit", 2f); // 3; f54344
            float firstHit = Time.realtimeSinceStartup - began;
            Check(firstHit < 1.5f && game.Battle.Merges == 0, "first click hits immediately without a fusion");
            Check(game.Lesson.Allows(0) && game.Lesson.Allows(1) && game.Lesson.Allows(2) && game.Lesson.Allows(3),
                "firing never locks other reserve choices");
            Lane(1);
            yield return new WaitForSecondsRealtime(.12f); // 4
            Lane(2);
            yield return Until(() => game.Battle.Merges > 0, "first fusion", 4f); // 5; f54345
            float firstFusion = Time.realtimeSinceStartup - began;
            Check(firstFusion < 4f, "continuous taps produce the first fusion within four seconds");
            Check(game.Narration.Topic == FlightTopic.Launch, "rapid deployment preserves the opening explanation");
            yield return Until(() => game.Narration.Topic == FlightTopic.Fusion, "readable fusion explanation", 5f); // 6; f54346
            yield return Shot("02-first-fusion"); // 7
            yield return Until(() => game.TutorialStep == 5, "next colour exposed", 15f); // 8; f54347
            int independentLane = game.Lesson.SuggestedLane;
            Check(independentLane >= 0, "independent colour is attackable");
            Lane(independentLane);
            yield return null; // 9
            Check(game.Lesson.IndependentChoice && game.Economy.Data.independentTutorial, "first unprompted colour choice is recorded");
            Check(game.Dialogue.Length > 0, "completing the guided actions does not remove the explanation");
            yield return Shot("03-independent"); // 10
            yield return FinishBattle(); // 11
            float firstVictory = Time.realtimeSinceStartup - began;
            Check(firstVictory < 35f && game.Economy.Data.unlocked == 2, "short first sortie wins and unlocks the second");
            yield return Shot("04-first-victory"); // 12
            game.TapUi(40);
            yield return Until(() => game.LevelIndex == 1 && game.LessonTestReady, "second pattern"); // 13; f54348
            Check(!game.InHome && game.Battle.Total == 258, "second pattern starts directly with 258 enemies");
            yield return Shot("05-mothership"); // 14
            float secondBegan = Time.realtimeSinceStartup;
            Lane(0);
            yield return null; // 15
            Check(game.Lesson.Allows(1) && game.Lesson.Allows(3), "ammunition tip allows continuous deployment");
            yield return Until(() => game.Battle.EmptyPushes > 0, "short opening magazine", 3f); // 16; f54349
            Check(!game.Paused && game.Battle.Fleet.Count == 0, "empty magazine gives a real charge and free bay without a modal");
            yield return Until(() => game.Narration.Topic == FlightTopic.Refill, "readable ammunition explanation", 5f); // 17; f54350
            yield return Shot("06-ammo-relay"); // 18
            yield return FinishBattle(); // 19
            float secondVictory = Time.realtimeSinceStartup - secondBegan;
            Check(secondVictory < 35f && game.Economy.Data.unlocked == 3, "second sortie clears without a forced observation sequence");
            yield return Shot("07-second-victory"); // 20
            game.TapUi(40);
            yield return Until(() => game.LevelIndex == 2 && game.LessonTestReady, "continue to original stage"); // 21; f54351
            Check(!game.InHome && !game.Economy.Data.growthTutorial && game.Battle.Total == 3600 && !game.Battle.TrainingSafe,
                "next level keeps momentum without forcing a trip to upgrade");
            game.ReturnToDeck();
            yield return Until(() => game.InHome && game.LessonTestReady, "optional upgrade visit"); // 22; f54352
            yield return new WaitForSecondsRealtime(2f); // 23
            game.TapUi(116);
            yield return new WaitForSecondsRealtime(.5f); // 24
            if (game.Page != DeckPage.Upgrade)
            {
                game.TapUi(116);
                yield return new WaitForSecondsRealtime(.5f); // 25
            }
            yield return Shot("08-optional-upgrade"); // 26
            int coins = game.Economy.Data.coins;
            Check(game.Economy.Spare(0, 0) >= 2 && game.SelectedPart == 0, "the growth visit gifts two 巡航者 noses and selects the nose");
            Check(game.TapUi(180), "optional upgrade control exists");
            yield return null; // 27
            Check(game.Economy.PartLevel(0, 0) == 2 && game.Economy.Snapshot().pierce == 1
                && game.Economy.Data.coins == coins && game.Economy.Data.growthTutorial,
                "optional growth upgrades the nose with part copies (no coins) and unlocks 渗透");
            game.ReturnToDeck();
            yield return Until(() => game.InHome && game.LessonTestReady, "deck ready"); // 28; f54352
            game.Sortie(2);
            yield return Until(() => !game.InHome && game.LevelIndex == 2 && game.LessonTestReady, "original space sortie"); // 29; f54353
            Lane(0);
            yield return new WaitForSecondsRealtime(4f); // 30
            game.TapUi(12);
            yield return null; // 31
            yield return Shot("09-support-aim"); // 32
            game.PointerDown(game.AimPosition);
            game.PointerMove(game.AimPosition + new Vector2(12f, 8f));
            game.PointerUp(game.AimPosition);
            Check(game.TapUi(9), "bomb confirmation control exists");
            yield return Until(() => game.Battle.SuperStrikeCount > 0, "bomb impact", 8f); // 33; f54354
            Check(game.Economy.Data.bombTutorial && game.Battle.SuperStrikeKills > 0, "support still uses real aiming, inventory and impact");
            game.ReturnToDeck();
            yield return Until(() => game.InHome && game.LessonTestReady, "replay entry"); // 34; f54352
            yield return new WaitForEndOfFrame(); // 35
            int replayCoins = game.Economy.Data.coins;
            Check(game.TapUi(100), "settings gear is available after returning home");
            yield return new WaitForSecondsRealtime(.5f); // 36
            Check(game.TapUi(299), "settings exposes the training replay");
            yield return Until(() => !game.InHome && game.LevelIndex == 0 && game.LessonTestReady, "training replay"); // 37; f54355
            Check(game.Economy.Data.unlocked == 3 && game.Economy.PartLevel(0, 0) == 2 && game.Economy.Data.coins == replayCoins,
                "replay preserves progress, part upgrades and coins");
            yield return new WaitForSecondsRealtime(5f); // 38
            Check(game.Lesson.HintLevel >= 3, "idle help escalates without stopping the game");
            yield return Shot("10-idle-help", 390, 844); // 39; packed original dimensions
            game.TapUi(1);
            yield return new WaitForSecondsRealtime(.4f); // 40
            float pausedAt = game.Battle.Time;
            Lane(0);
            yield return new WaitForSecondsRealtime(.3f); // 41
            Check(game.Paused && game.Battle.Fleet.Count == 0 && game.Battle.Time == pausedAt, "pause blocks click-through and freezes combat");
            game.TapUi(24);
            yield return null; // 42
            Lane(3);
            yield return new WaitForSecondsRealtime(.4f); // 43
            Check(game.Narration.Topic == FlightTopic.Waiting && game.Dialogue.Contains("先清前排"), "wrong colour explains both waiting and how to resume fire");
            Check(game.Lesson.WrongChoices == 1 && !game.Paused, "wrong-colour choice remains available and recoverable");
            yield return Shot("11-waiting-aircraft"); // 44
            Lane(0);
            yield return new WaitForSecondsRealtime(.2f); // 45
            game.TapUi(1);
            yield return new WaitForSecondsRealtime(.4f); // 46
            game.TapUi(23);
            yield return Until(() => game.LessonTestReady && game.TutorialStep == 0, "restart"); // 47; f54356
            Check(game.Battle.Remaining == game.Battle.Total && game.Battle.Fleet.Count == 0, "restart resets the short sortie and ammunition");
            File.WriteAllText(Path.Combine(output, "pace.json"), JsonUtility.ToJson(new Pace
            {
                firstHit = firstHit, firstFusion = firstFusion, firstVictory = firstVictory, secondVictory = secondVictory
            }, true));
            Debug.Log(String.Format("ONBOARDING_NATIVE_PACE hit={0:F2} fusion={1:F2} first={2:F2} second={3:F2}",
                firstHit, firstFusion, firstVictory, secondVictory));
        }
    }
}
