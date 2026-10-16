using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class RandomInput { public int bound, value; }
        [Serializable] private sealed class SupplyAction { public string kind; public int[] ints; public int dtBits; }
        [Serializable] private sealed class PartState { public int index; public bool first; }
        [Serializable] private sealed class SupplyInput { public RandomInput[] randomTape; public SupplyAction[] actions; }
        [Serializable] private sealed class SupplyState
        {
            public int actionIndex, randomCursor, gachaAgeBits, gachaBest, lastDrawCount, supplyPool, supplyTarget, deckOverlay;
            public bool partResultsPresent, coreResultsPresent, gachaNew, gachaRevealed;
            public string profileEncoded;
            public string[] saves;
            public PartState[] partResults;
            public FleetCore[] coreResults;
        }
        private static T Field<T>(PixelWingGame game, string name) => (T)typeof(PixelWingGame).GetField(name, Instance).GetValue(game);
        private static List<SupplyState> RunSupply(PixelWingGame game, SupplyInput input, List<string> saves, Func<int> cursor)
        {
            var states = new List<SupplyState>();
            for (int i = 0; i < input.actions.Length; i++)
            {
                var action = input.actions[i];
                switch (action.kind)
                {
                    case "openSupply": Call(game, "OpenSupply", action.ints[0], action.ints[1]); break;
                    case "supplyUi": if (!(bool)Call(game, "OnSupplyUi", action.ints[0])) throw new Exception("Unhandled supply id"); break;
                    case "gachaTap": Call(game, "GachaTapAnywhere"); break;
                    case "tickGacha":
                        float dt = BitConverter.Int32BitsToSingle(action.dtBits);
                        if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0) throw new Exception("Invalid declared dt");
                        Set(game, "clock", InputSingle(Field<float>(game, "clock") + dt));
                        Set(game, "ui", InputSingle(Field<float>(game, "ui") + dt));
                        Call(game, "TickGacha", dt); break;
                    default: throw new Exception("Unknown supply action " + action.kind);
                }
                // Snapshot by value: later transactions must not rewrite old observations.
                var state = new SupplyState
                {
                    actionIndex = i, randomCursor = cursor(), saves = saves.ToArray(),
                    profileEncoded = FleetSaveStore.Encode(game.Economy.Data),
                    partResultsPresent = game.PartResults != null, coreResultsPresent = game.GachaResults != null,
                    partResults = Array.ConvertAll(game.PartResults ?? Array.Empty<PartPull>(), p => new PartState { index = p.index, first = p.first }), coreResults = game.GachaResults ?? Array.Empty<FleetCore>(),
                    gachaAgeBits = Bits(Field<float>(game, "gachaAge")), gachaBest = Field<int>(game, "gachaBest"),
                    gachaNew = Field<bool>(game, "gachaNew"), gachaRevealed = Field<bool>(game, "gachaRevealed"),
                    lastDrawCount = Field<int>(game, "lastDrawCount"), supplyPool = game.SupplyPool,
                    supplyTarget = Field<int>(game, "supplyTarget"), deckOverlay = (int)Field<DeckOverlay>(game, "deckOverlay")
                };
                states.Add(JsonUtility.FromJson<SupplyState>(JsonUtility.ToJson(state)));
            }
            if (cursor() != input.randomTape.Length) throw new Exception("Unconsumed supply RNG input");
            return states;
        }
    }
}
