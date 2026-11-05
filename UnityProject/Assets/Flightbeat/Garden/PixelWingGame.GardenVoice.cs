#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    // 2026-11-04 11:42 (user: put these voice lines in the game): the captain's first ten lines (Art/PlushToy/Voice-20261103,
    // clips in Resources/GardenR1/Voice, played by ChipAudio.GardenSpeak):
    //   home-1 / -2 / -3       12:25 (user: the greeting is for entering the game, not for coming back from a level): one
    //                          line per launch, on the home screen the game opens on - home-1 on the day's first launch,
    //                          home-2 / home-3 in turn on later ones. Coming back from a level never greets; a launch
    //                          that opens straight into a level (a new player's level 1) does not greet either.
    //                          It starts .8 s after home shows. 17:05 (user: no greeting when opening the link): it used
    //                          to wait for the first tap, and when that tap was the start button the level took over first,
    //                          so it never played. Now it does not wait: before the first tap the browser holds the audio
    //                          (as it does the music) and the line comes out with that tap, also over the dash into a level.
    //   win-first / win-2, -4  the win page of level 1 / of any other level (the two alternate)
    //   fail-near / -retry     the lose page: progress at 70 % or more / below
    //   revive-1 / -2          each time the revive page opens (the two alternate)
    //   revive-ok              right after reviving (RescueGarden)
    //   danger-1               the first time in a level the bubbles come inside the rail's warning band (16 px)
    //   (combo lines, 11-04 17:32 / 17:43, were tried and removed at 19:57 at the user's request, clips deleted too)
    public partial class PixelWingGame
    {
        const string GardenVoiceHomeKey = "pw.voice-home-day", GardenVoiceHomeTurnKey = "pw.voice-home-turn";
        float gardenVoiceHomeSince = -1f;
        bool gardenVoiceGreetDone;   // this launch has greeted, or has already been in a level
        bool gardenVoiceOffer, gardenVoiceWinPage, gardenVoiceLosePage;
        int gardenVoiceWinTurn, gardenVoiceReviveTurn;
        Flightbeat.Model.Battle gardenVoiceDangerBattle;
        bool GardenSay(string id) => audioChip != null && audioChip.GardenSpeak(id);

        // Called while the home screen is drawn (DrawWaveHome).
        void GardenVoiceHome()
        {
            if (gardenVoiceGreetDone) return;
            if (gardenVoiceHomeSince < 0f) gardenVoiceHomeSince = ui;
            if (ui - gardenVoiceHomeSince < .8f || audioChip == null) return;
            string today = System.DateTime.Now.ToString("yyyyMMdd");
            bool first = PlayerPrefs.GetString(GardenVoiceHomeKey, "") != today;
            int turn = PlayerPrefs.GetInt(GardenVoiceHomeTurnKey, 0);
            gardenVoiceGreetDone = true;   // with the sound effects off this launch simply does not greet
            if (!GardenSay(first ? "home-1" : turn % 2 == 0 ? "home-2" : "home-3")) return;
            PlayerPrefs.SetString(GardenVoiceHomeKey, today);
            if (!first) PlayerPrefs.SetInt(GardenVoiceHomeTurnKey, turn + 1);
            PlayerPrefs.Save();
        }

        // Called every frame of a level (DrawGardenOverlay), before the result and revive pages draw.
        void GardenVoiceTick()
        {
            // Any level screen ends the launch greeting's chance: coming back home from a level never greets.
            gardenVoiceGreetDone = true;
            if (Battle == null) return;
            bool offer = Battle.State == Phase.ReviveOffer;
            if (offer && !gardenVoiceOffer) GardenSay(gardenVoiceReviveTurn++ % 2 == 0 ? "revive-1" : "revive-2");
            gardenVoiceOffer = offer;
            // The result pages appear .48 s after the level ends (DrawWinScreen / DrawLoseScreen).
            bool winPage = Battle.State == Phase.Won && feedback.EndAge > .48f;
            if (winPage && !gardenVoiceWinPage)
                GardenSay(levelIndex == 0 ? "win-first" : gardenVoiceWinTurn++ % 2 == 0 ? "win-2" : "win-4");
            gardenVoiceWinPage = winPage;
            bool losePage = Battle.State == Phase.Lost && feedback.EndAge > .48f;
            if (losePage && !gardenVoiceLosePage) GardenSay(Battle.Progress >= .7f ? "fail-near" : "fail-retry");
            gardenVoiceLosePage = losePage;
            if (gardenVoiceDangerBattle != Battle && Battle.State == Phase.Playing && Battle.ActiveFinale == null && Battle.Level.lesson <= 0)
            {
                float front = Battle.FrontY;
                if (front >= 0f && 562f - front < RailWarning && GardenSay("danger-1")) gardenVoiceDangerBattle = Battle;
            }
        }
    }
}
#endif
