using System.Collections.Generic;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    // src: FlightTopic metadata, TypeDefIndex 4380.
    public enum FlightTopic
    {
        Launch = 0, FusionInvite = 1, Fusion = 2, Waiting = 3,
        Matching = 4, Ammunition = 5, Refill = 6, Finale = 7
    }

    public sealed class FlightNarration
    {
        public const float MinimumReadSeconds = 3.2f;
        private readonly Battle battle;
        private readonly FlightLesson lesson;
        private readonly List<FlightTopic> pending;
        private readonly HashSet<FlightTopic> observed;
        private readonly HashSet<FlightTopic> shown;
        private int wrongChoices;

        // src: FlightNarration.get_Topic f7113 / set_Topic f7114.
        public FlightTopic Topic { get; private set; }
        // src: FlightNarration.get_Age f5961 / set_Age f5966.
        public float Age { get; private set; }

        // src: FlightNarration.HasShown f54324.
        public bool HasShown(FlightTopic topic) => shown.Contains(topic);

        // src: FlightNarration..ctor f54325.
        public FlightNarration(Battle battle, FlightLesson lesson)
        {
            pending = new List<FlightTopic>();
            observed = new HashSet<FlightTopic>();
            shown = new HashSet<FlightTopic>();
            this.lesson = lesson;
            this.battle = battle;
            Show(lesson.Chapter == 1 ? FlightTopic.Launch : FlightTopic.Ammunition);
        }

        // src: FlightNarration.Show f54326.
        private void Show(FlightTopic topic)
        {
            Age = 0f;
            Topic = topic;
            observed.Add(topic);
            shown.Add(topic);
        }

        // src: FlightNarration.Priority f54327; memory.bin i32 table at 3647880.
        private static int Priority(FlightTopic topic)
        {
            if (topic == FlightTopic.Fusion || topic == FlightTopic.Refill) return 0;
            return topic == FlightTopic.Waiting ? 1 : 2;
        }

        // src: FlightNarration.Queue f54328. Equal priorities retain insertion order.
        private void Queue(FlightTopic topic)
        {
            if (!observed.Add(topic)) return;
            int index = 0;
            while (index < pending.Count && Priority(pending[index]) <= Priority(topic)) index++;
            pending.Insert(index, topic);
        }

        // src: FlightNarration.Tick f54329.
        public void Tick(float dt)
        {
            var state = battle.State;
            if (state == Phase.Intro || state == Phase.Won || state == Phase.Lost) return;
            Age += dt;
            if (lesson.WrongChoices > wrongChoices)
            {
                wrongChoices = lesson.WrongChoices;
                Queue(FlightTopic.Waiting);
            }
            if (lesson.Chapter == 1)
            {
                if (lesson.Step == 3 && battle.Merges == 0) Queue(FlightTopic.FusionInvite);
                if (battle.Merges > 0)
                {
                    pending.Remove(FlightTopic.FusionInvite);
                    Queue(FlightTopic.Fusion);
                }
                if (battle.Exposed(1) || battle.Exposed(2)) Queue(FlightTopic.Matching);
            }
            else if (battle.EmptyPushes > 0) Queue(FlightTopic.Refill);
            if (battle.FinaleReady || battle.FinaleUsed) Queue(FlightTopic.Finale);
            if (Age < MinimumReadSeconds || pending.Count == 0) return;
            var topic = pending[0];
            pending.RemoveAt(0);
            Show(topic);
        }

        // src: FlightNarration.get_Line f54330; memory.bin table at 3647900.
        public string Line
        {
            get
            {
                switch (Topic)
                {
                    case FlightTopic.Launch: return "点击蓝色战机出击。\n它会自动攻击前排的同色敌人。"; // 5864020
                    case FlightTopic.FusionInvite: return "同色三架到位后会自动合成。\n再派蓝色战机，试试火力升级。"; // 5862996
                    case FlightTopic.Fusion: return "三架同色战机合成一架！\n火力增强，还空出两个机位。"; // 5862352
                    case FlightTopic.Waiting: return "同色目标被挡住，战机会等待。\n先清前排，它就能继续攻击。"; // 5863004
                    case FlightTopic.Matching: return "战机只打前排的同色敌人。\n前排换色，就换同色战机出击。"; // 5863360
                    case FlightTopic.Ammunition: return "战机下方的数字是剩余弹药。\n每次攻击都会消耗弹药。"; // 5863352
                    case FlightTopic.Refill: return "弹药用完，战机会冲锋击退。\n空出机位后，接上新战机。"; // 5863292
                    default: return "战机全部出击后，可点击决战。\n全机合体，用激光清空残敌。"; // 5863356
                }
            }
        }
    }
}
