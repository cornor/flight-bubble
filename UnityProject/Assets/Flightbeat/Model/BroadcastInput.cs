namespace Flightbeat.Model
{
    // Input contract only. The original decision algorithm is excluded from
    // ordinary players with the rest of Flightbeat.DevTools (plan T9.3).
    public enum BroadcastAction { Wait, Deploy, Bay, Bomb, Finale, Rescue }
    public struct BroadcastDecision
    {
        public BroadcastAction action;
        public int index;
        // Compiler-shared f3463.
        public BroadcastDecision(BroadcastAction action, int index = -1) { this.index = index; this.action = action; }
    }
    public interface IBroadcastPilot
    {
        BroadcastDecision Tick(Battle battle, float dt);
    }
}
