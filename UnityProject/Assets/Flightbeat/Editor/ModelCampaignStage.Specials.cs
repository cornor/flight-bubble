using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;

namespace Flightbeat.Editor
{
    public static partial class ModelCampaignStage
    {
        [Serializable] private sealed class Checkpoint
        { public string kind; public int frame, phase, timeBits, remaining; public Controller controller; }

        // Exact declared nonwinning input order in the frozen special_driver.js
        // and mechanism_driver.js. Expected states/actions do not drive this script.
        private sealed class SpecialScript
        {
            private readonly Input input;
            private readonly PixelWingGame game;
            private readonly Battle battle;
            private readonly CombatFeedback feedback;
            private readonly FleetEconomy economy;
            private readonly List<FleetProfile> saves;
            private string stage = "ready";
            private int laneCursor, itemCursor, checkpoint;
            private bool done;
            private List<ActionInput> actions;
            public SpecialScript(Input input, PixelWingGame game, Battle battle, CombatFeedback feedback, FleetEconomy economy, List<FleetProfile> saves)
            { this.input = input; this.game = game; this.battle = battle; this.feedback = feedback; this.economy = economy; this.saves = saves; }
            private void Record(string action, int[] args, object value)
            {
                actions.Add(new ActionInput { action = action, args = args, aimBits = Array.Empty<int>(),
                    resultKind = value == null ? "null" : "bool", result = value is bool b && b });
            }
            private void Point(string kind)
            {
                if (checkpoint >= input.checkpoints.Length) throw new Exception("Unexpected source-script checkpoint " + kind);
                var c = input.checkpoints[checkpoint++]; result.checkpoints++;
                Equal("checkpoint/kind", c.kind, kind); Equal("checkpoint/frame", c.frame, result.frames);
                Equal("checkpoint/phase", c.phase, (int)battle.State); Equal("checkpoint/time", c.timeBits, Bits(battle.Time));
                Equal("checkpoint/remaining", c.remaining, battle.Remaining);
                CompareController(game, feedback, economy, saves.Count, c.controller);
            }
            private void Deploy(int lane)
            { bool accepted = battle.Deploy(lane); Record("deploy", new[] { lane }, accepted); if (!accepted) throw new Exception("Declared source deployment rejected"); }
            public void BeforeTick(List<ActionInput> output)
            {
                if (done) throw new Exception("Frames after declared script completion");
                actions = output;
                if (input.mode == "mechanism") { Mechanism(); return; }
                var phase = battle.State; // Original captures this before input changes.
                if (phase == Phase.Won) throw new Exception("Nonwinning input unexpectedly won");
                if (stage == "ready" && phase == Phase.Ready) { Point("ready"); Deploy(0); Point("deployed"); stage = "breach"; }
                if (stage == "breach" && phase == Phase.ReviveOffer) { Point("revive-offer"); stage = "offer"; }
                if (stage == "offer" && feedback.RescueAge >= BitConverter.Int32BitsToSingle(input.rescueAgeMinimumBits))
                {
                    Point("rescue-input");
                    if (input.mechanism == "decline-revive")
                    {
                        bool accepted = battle.DeclineRevive(); Record("declineRevive", Array.Empty<int>(), accepted);
                        if (!accepted) throw new Exception("Declared decline rejected");
                    }
                    else if (input.mechanism == "accept-revive")
                    { economy.CountAd(); Record("revive", Array.Empty<int>(), Call(game, "Rescue")); }
                    else throw new Exception("Unknown source revive script");
                    Point("rescue-result"); stage = "final-tick";
                }
                if (phase == Phase.Lost && stage != "final-tick") throw new Exception("Lost before expected revive input");
            }
            private void Mechanism()
            {
                var phase = battle.State;
                if ((int)phase >= 3) throw new Exception("Unexpected terminal/offer in nonwinning mechanism input");
                if (stage == "ready")
                {
                    if (phase != Phase.Ready) return;
                    Point("ready");
                    foreach (int bay in input.unlockBays)
                    {
                        economy.CountAd(); bool accepted = battle.UnlockBay(bay); Record("unlockBay", new[] { bay }, accepted);
                        if (!accepted) throw new Exception("Declared bay callback rejected");
                    }
                    stage = "deploy";
                }
                if (stage == "deploy")
                {
                    foreach (var ship in battle.Fleet) if (ship.Arrival > 0f) return;
                    if (laneCursor < input.deployLanes.Length)
                    { Deploy(input.deployLanes[laneCursor++]); Point("deployed-" + laneCursor); return; }
                    if (input.mechanism == "MAX")
                    {
                        if (battle.MaxFusions < 1) throw new Exception("Fixed source queue input did not reach MAX");
                        Point("MAX"); stage = "final-tick"; return;
                    }
                    stage = "items";
                }
                if (stage == "items")
                {
                    if (itemCursor < input.itemKinds.Length)
                    {
                        int kind = input.itemKinds[itemCursor++];
                        if (economy.Data.items[kind] <= 0) throw new Exception("Undeclared item replenishment");
                        Record("useProp", new[] { kind }, Call(game, "UseProp", kind)); Point("item-" + itemCursor); return;
                    }
                    Point("items-complete"); stage = "final-tick";
                }
            }
            public void AfterTick()
            {
                if (battle.State == Phase.Won) throw new Exception("Unexpected Won after nonwinning tick");
                if (stage != "final-tick") return;
                if ((int)battle.State != input.expectedPhase) throw new Exception("Source script result phase differs");
                Point("complete"); done = true;
            }
            public void Finish()
            {
                Equal("script/completed", true, done); Equal("script/checkpoints", input.checkpoints.Length, checkpoint);
                Equal("script/PilotTicks", 0, result.pilotTicks);
            }
        }
    }
}
