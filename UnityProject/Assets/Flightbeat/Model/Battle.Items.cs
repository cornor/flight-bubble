using System;
using System.Collections.Generic;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55037.
        public void Message(string text) { NoticeTime = 2.4f; Notice = text; }

        // f55044: row is an unsigned range check, followed by the real deployment.
        public bool Designate(int column, int row) => unchecked((uint)row) <= 2u && DeployAt(column, row);

        // f55048. Fisher-Yates over only the first three rows of each queue;
        // rotate once when the shuffled object references would be unchanged.
        public bool Shuffle()
        {
            if (unchecked((uint)((int)State - 1)) > 1u) return false;
            var ships = new List<Fighter>();
            var columns = new List<int>();
            var rows = new List<int>();
            for (int column = 0; column < Reserve.Length; column++)
                for (int row = 0; row < Math.Min(3, Reserve[column].Count); row++)
                { ships.Add(Reserve[column][row]); columns.Add(column); rows.Add(row); }
            if (ships.Count < 2) return false;
            int seed = Level.seed;
            shuffleSerial = unchecked(shuffleSerial + 1);
            var random = new Random(unchecked(seed + shuffleSerial * 101));
            for (int i = ships.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Fighter previous = ships[i]; ships[i] = ships[j]; ships[j] = previous;
            }
            bool changed = false;
            for (int i = 0; i < ships.Count; i++) changed = Reserve[columns[i]][rows[i]] != ships[i] | changed;
            if (!changed) { Fighter first = ships[0]; ships.RemoveAt(0); ships.Add(first); }
            for (int i = 0; i < ships.Count; i++)
            { ships[i].Queue = columns[i]; Reserve[columns[i]][rows[i]] = ships[i]; }
            Cue?.Invoke("shuffle");
            NoticeTime = 2.4f;
            Notice = "后备战机已重排";
            return true;
        }

        // f55036. The bound is BaseSlots + extraBay.Length, not MaximumSlots.
        public bool UnlockBay(int index)
        {
            if (unchecked((uint)((int)State - 1)) > 1u || BaseSlots > index || unchecked(extraBay.Length + BaseSlots) <= index) return false;
            int extra = index - BaseSlots;
            if (extraBay[extra]) return false;
            extraBay[extra] = true;
            Cue?.Invoke("reward");
            NoticeTime = 2.4f;
            Notice = "已解锁战机位";
            return true;
        }
    }
}
