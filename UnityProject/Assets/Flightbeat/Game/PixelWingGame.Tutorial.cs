using System;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int tutorialStep = -1; // original +16; f54860
        private int spokenCharacters; // +20
        private string dialogue = ""; // +24; f54860
        private float dialogueAge; // +28
        private float glyphAt; // +32
        private FlightLesson lesson; // +36
        private FlightNarration narration; // +40
        internal bool evidenceControlled; // +573
        internal int selection; // +600

        // src: PixelWingGame.get_Battle f54762 / set_Battle f54763.
        public Battle Battle { get; internal set; }
        // src: PixelWingGame.get_TutorialStep f4596.
        public int TutorialStep => tutorialStep;
        // src: PixelWingGame.get_Lesson f7115.
        public FlightLesson Lesson => lesson;
        // src: PixelWingGame.get_Narration f6063.
        public FlightNarration Narration => narration;
        // src: PixelWingGame.get_Dialogue f6993.
        public string Dialogue => dialogue;
        // src: PixelWingGame.get_DialogueAge f5960.
        public float DialogueAge => dialogueAge;
        // src: PixelWingGame.get_SpokenCharacters f3235.
        public int SpokenCharacters => spokenCharacters;
        // src: PixelWingGame.get_TrainingDemoAge f23927.
        public float TrainingDemoAge => -1f;

        // src: PixelWingGame.ResetComms f54497. Old subscriptions are retained
        // by the source; neither reset nor skip removes their Battle handlers.
        private void ResetComms()
        {
            lesson = Battle.Level.lesson > 0 && !evidenceControlled ? new FlightLesson(Battle) : null;
            narration = lesson == null ? null : new FlightNarration(Battle, lesson);
            tutorialStep = lesson == null ? -1 : lesson.Step;
            Battle.DeferTrainingRecoil = false;
            Battle.TrainingHold = false;
            Battle.TrainingSafe = lesson != null;
            dialogue = "";
            dialogueAge = 0f;
            glyphAt = 0f;
            spokenCharacters = 0;
        }

        // src: PixelWingGame.SkipTutorial f54498. It intentionally preserves
        // dialogueAge, glyphAt and spokenCharacters until their next source update.
        public void SkipTutorial()
        {
            lesson = null;
            narration = null;
            tutorialStep = -1;
            Battle.DeferTrainingRecoil = false;
            Battle.TrainingHold = false;
            Battle.TrainingSafe = false;
            dialogue = "";
        }

        // src: PixelWingGame.TutorialTap f54499. A tap in this rectangle is
        // consumed even when its lane is empty or Battle refuses deployment.
        private bool TutorialTap(Vector2 p)
        {
            if (lesson == null || lesson.Complete || Battle.State == Phase.Intro || selection > 0
                || !(p.y >= 700f) || !(p.y < 754f) || !(p.x >= 20f) || !(p.x < 520f)) return false;
            int lane = LaneAt(p.x);
            if (lesson.Allows(lane)) Battle.Deploy(lane);
            return true;
        }

        // src: PixelWingGame.LaneAt f54500; strict comparison keeps earlier ties.
        private int LaneAt(float x)
        {
            float nearest = float.MaxValue;
            int result = 0;
            for (int lane = 0; lane < Battle.LaneCount; lane++)
            {
                float distance = Math.Abs(x - Battle.QueuePositionX(lane));
                if (nearest > distance)
                {
                    nearest = distance;
                    result = lane;
                }
            }
            return result;
        }
    }
}
