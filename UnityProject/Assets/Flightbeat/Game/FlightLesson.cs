using Flightbeat.Model;

namespace Flightbeat.Game
{
    public sealed class FlightLesson
    {
        private readonly Battle battle;
        public readonly int Chapter;

        // src: FlightLesson.get_Step f4596 / set_Step f4682.
        public int Step { get; private set; }
        // src: FlightLesson.get_Age f3363 / set_Age f23956.
        public float Age { get; private set; }
        // src: FlightLesson.get_IndependentChoice f8292 / set_IndependentChoice f8293.
        public bool IndependentChoice { get; private set; }
        // src: FlightLesson.get_HelpUsed f13255 / set_HelpUsed f18283.
        public bool HelpUsed { get; private set; }
        // src: FlightLesson.get_WrongChoices f7354 / set_WrongChoices f7372.
        public int WrongChoices { get; private set; }

        // src: FlightLesson.get_Complete f54312 (i32.shr_u 31).
        public bool Complete => Step < 0;
        // src: FlightLesson.get_Practice f54313.
        public bool Practice => Step == (Chapter == 1 ? 5 : 12);

        // src: FlightLesson..ctor f54314; Battle.add_Launched f54992.
        public FlightLesson(Battle b)
        {
            battle = b;
            Chapter = b.Level.lesson;
            Step = Chapter == 1 ? 0 : 10;
            b.Launched += Launched;
        }

        // src: FlightLesson.Go f54315.
        private void Go(int step)
        {
            Age = 0f;
            Step = step;
        }

        // src: FlightLesson.Launched f54316.
        private void Launched(Fighter ship, int slot)
        {
            bool exposed = battle.Exposed(ship.Color);
            if (!exposed) WrongChoices = unchecked(WrongChoices + 1);
            if (Chapter == 1 && Step == 5 && exposed && ship.Color != 0)
            {
                Step = -1;
                IndependentChoice = !HelpUsed;
                Age = 0f;
            }
            else if (Chapter == 1)
            {
                if (Step == 0 && ship.Color == 0) Go(3);
            }
            else if (Step == 10) Go(12);
        }

        // src: FlightLesson.Tick f54317.
        public void Tick(float dt)
        {
            int step = Step;
            if (step < 0) return;
            var phase = battle.State;
            if ((int)phase == 0) return;
            Age += dt;
            if ((int)phase == 4)
            {
                Go(-1);
                return;
            }
            if (Chapter == 1)
            {
                switch (step)
                {
                    case 3:
                        if (battle.Merges <= 0) break;
                        Go(4);
                        goto case 4;
                    case 4:
                        if (battle.Exposed(1) || battle.Exposed(2)) Go(5);
                        break;
                }
            }
            else if (step == 12 && battle.EmptyPushes > 0 && Age > 3f) Go(-1);

            int count = 0;
            for (int i = 0; i < battle.Reserve.Length; i++)
                count = unchecked(count + battle.Reserve[i].Count);
            if (count == 0) Go(-1);
        }

        // src: FlightLesson.Allows f54318.
        public bool Allows(int lane) => lane >= 0 && lane < battle.LaneCount && battle.Reserve[lane].Count != 0;

        // src: FlightLesson.get_SuggestedLane f54319.
        public int SuggestedLane
        {
            get
            {
                if (Chapter == 1 && (Step == 0 || Step == 3))
                    for (int lane = 0; lane < battle.LaneCount; lane++)
                        if (Allows(lane) && battle.Reserve[lane][0].Color == 0) return lane;
                for (int lane = 0; lane < battle.LaneCount; lane++)
                    if (Allows(lane) && battle.Exposed(battle.Reserve[lane][0].Color)) return lane;
                return -1;
            }
        }

        // src: FlightLesson.get_HintLevel f54320.
        public int HintLevel
        {
            get
            {
                if (Step < 0 || Step == 4) return 0;
                float threshold = Practice ? 6f : Step != 0 ? 1.4f : 0.8f;
                if (Age < threshold) return 0;
                if (Age < threshold + 2f) return 1;
                return Age < threshold + 4f ? 2 : 3;
            }
        }

        // src: FlightLesson.ShowHelp f54321. Preserve the select's NaN behavior.
        public void ShowHelp()
        {
            HelpUsed = true;
            float threshold = Practice ? 20f : 14f;
            Age = Age > threshold ? Age : threshold;
        }

        // src: FlightLesson.NoteHint f54322.
        public void NoteHint()
        {
            if (Step != (Chapter == 1 ? 5 : 12) || Step < 0 || Step == 4 || Age < 6f) return;
            HelpUsed = true;
        }

        // src: FlightLesson.get_Goal f54323; memory.bin i32 table at 3647828.
        // The unsigned bound in WASM sends negative steps to the default string.
        public string Goal
        {
            get
            {
                switch (Step)
                {
                    case 0: return "击碎像素入侵者"; // string slot 5862736
                    case 3: return "再点两架蓝机，合成！"; // 5862656
                    case 4: return "合成成功！双翼火力"; // 5862984
                    case 5: return "换颜色，继续打！"; // 5863448
                    case 10: return "打穿星际母舰"; // 5863404
                    case 12: return "连续出击，保持火力"; // 5864628
                    default: return "清空敌阵，守住防线"; // 5863924
                }
            }
        }
    }
}
