namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f54979: unsigned subtraction admits only Ready=1 and Playing=2.
        public bool Active => unchecked((uint)((int)State - 1)) < 2u;
        // f54980: revive slots and every true extra bay count, with i32 wrapping.
        public int UnlockedSlots
        {
            get
            {
                int count = unchecked(reviveSlots + BaseSlots);
                foreach (bool open in extraBay) count = unchecked(count + (open ? 1 : 0));
                return count;
            }
        }
        // f55034: indices beyond extraBay but below MaximumSlots are open.
        public bool BayOpen(int index)
        {
            if (index < 0 || MaximumSlots <= index) return false;
            if (index < BaseSlots || unchecked(extraBay.Length + BaseSlots) <= index) return true;
            return extraBay[unchecked(index - BaseSlots)];
        }
        // f55035: no matching fleet index returns slot zero, including negatives.
        public int VisualSlot(int fleetIndex)
        {
            int count = 0;
            for (int slot = 0; slot < MaximumSlots; slot++)
            {
                if (slot >= BaseSlots && unchecked(extraBay.Length + BaseSlots) > slot
                    && !extraBay[unchecked(slot - BaseSlots)]) continue;
                if (fleetIndex == count) return slot;
                count = unchecked(count + 1);
            }
            return 0;
        }
        // f55043: only the reserve head's Kind==2 is excluded. Existing fighters
        // count by color, regardless of tier/arrival/kind, and a free bay is needed.
        public bool WouldMerge(int column)
        {
            if (column < 0 || !Active || Reserve.Length <= column || Reserve[column].Count == 0) return false;
            if (UnlockedSlots <= Fleet.Count) return false;
            Fighter incomingShip = Reserve[column][0];
            if (incomingShip.Kind == 2) return false;
            int count = 0;
            foreach (Fighter ship in Fleet)
                count = unchecked(count + (ship.Color == incomingShip.Color ? 1 : 0));
            return count % 3 == 2;
        }
        // f55082: reserve heads compete by source integer score, then Order.
        // Availability probes exclude reserved targets and mutate pierce fields.
        public int SuggestedQueue()
        {
            int bestScore = int.MinValue, bestOrder = int.MaxValue, bestQueue = -1;
            for (int queue = 0; queue < Reserve.Length; queue++)
            {
                if (Reserve[queue].Count == 0) continue;
                Fighter candidate = Reserve[queue][0];
                if (Time < candidate.DeployAt) continue;
                int matching = 0;
                foreach (Fighter ship in Fleet)
                    matching = unchecked(matching + (ship.Color == candidate.Color ? 1 : 0));
                if (UnlockedSlots <= Fleet.Count) continue;
                int targets = 0, depth = Loadout.pierce < 3 ? Loadout.pierce : 3;
                depth = depth > 0 ? depth : 0;
                for (int column = 0; column < Level.columns; column++)
                    if (ColumnTarget(column, candidate.Color, depth) >= 0) targets = unchecked(targets + 1);
                int score = unchecked((matching * 10) + (matching > 1 ? 100000 : 0) + (targets * 100));
                if (score > bestScore || (score == bestScore && candidate.Order < bestOrder))
                {
                    bestScore = score;
                    bestOrder = candidate.Order;
                    bestQueue = queue;
                }
            }
            return bestQueue;
        }
    }
}
