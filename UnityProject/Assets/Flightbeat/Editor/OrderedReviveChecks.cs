#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class OrderedReviveChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void PhaseOf(Battle battle, Phase phase) => typeof(Battle).GetProperty("State", Flags).SetValue(battle, phase);
        static Battle Fixture(int extras = 2)
        {
            var level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector01").text);
            level.initialSlots = 5; level.slotLimit = 5 + extras;
            var battle = new Battle(level, null);
            PhaseOf(battle, Phase.Ready);
            return battle;
        }
        static int[] OpenSlots(Battle battle) => Enumerable.Range(0, battle.MaximumSlots).Where(battle.BayOpen).ToArray();

        public static void Run()
        {
            // All combinations include the case where the right-hand ad bay was
            // opened first. Fixtures exercise rules without changing player saves.
            for (int mask = 0; mask < 4; mask++)
            using (var battle = Fixture())
            {
                for (int i = 0; i < 2; i++) if ((mask & (1 << i)) != 0) Require(battle.UnlockBay(5 + i), "Open fixture bay");
                int[] before = OpenSlots(battle);
                int target = Enumerable.Range(0, battle.MaximumSlots).Where(i => !battle.BayOpen(i)).DefaultIfEmpty(battle.MaximumSlots).First();
                int maximum = battle.MaximumSlots, bombs = battle.RescueMissiles, remaining = battle.Remaining;
                PhaseOf(battle, Phase.ReviveOffer);
                Require(battle.ReviveInSlotOrder(), "Revival accepted");
                int[] expected = before.Append(target).OrderBy(i => i).ToArray();
                Require(OpenSlots(battle).SequenceEqual(expected), "Unlock first closed bay, mask=" + mask);
                Require(battle.UnlockedSlots == before.Length + 1, "Exactly one extra deployable slot");
                Require(battle.MaximumSlots == maximum + (mask == 3 ? 1 : 0), "Expand only when all existing bays are open");
                for (int i = 0; i < expected.Length; i++) Require(battle.VisualSlot(i) == expected[i], "Fleet uses the same physical slot order");
                Require(!battle.UnlockBay(target), "Reward bay cannot grant a duplicate ad unlock");
                Require(battle.Revived && battle.State != Phase.ReviveOffer, "Revival resumes or completes battle");
                Require(battle.RescueMissiles == bombs + 1 && battle.Remaining < remaining && battle.Repelling && battle.Invulnerable > 0,
                    "Bomb, purge, pushback and invulnerability preserved");
                Require(!battle.ReviveInSlotOrder() && OpenSlots(battle).SequenceEqual(expected) && battle.RescueMissiles == bombs + 1,
                    "Duplicate reward does not unlock another slot or grant another bomb");
                // Remaining ad bays still work after the revival reward is used.
                for (int i = 5; i < 7; i++) if (!battle.BayOpen(i)) Require(battle.UnlockBay(i), "Remaining ad bay remains unlockable");
                Require(battle.UnlockedSlots == battle.MaximumSlots, "All unlocked slots counted once");
            }
            using (var battle = Fixture(0))
            {
                PhaseOf(battle, Phase.ReviveOffer);
                Require(battle.ReviveInSlotOrder() && battle.MaximumSlots == 6 && battle.UnlockedSlots == 6 && battle.BayOpen(5),
                    "Levels without locked bays still receive one new slot");
            }
            foreach (Phase phase in new[] { Phase.Intro, Phase.Ready, Phase.Playing, Phase.Won, Phase.Lost })
            using (var battle = Fixture())
            {
                PhaseOf(battle, phase);
                Require(!battle.ReviveInSlotOrder() && battle.MaximumSlots == 7 && battle.UnlockedSlots == 5 && !battle.Revived,
                    "Ineligible revival leaves slots unchanged: " + phase);
            }
            using (var battle = Fixture())
            {
                PhaseOf(battle, Phase.ReviveOffer);
                Require(battle.Revive() && battle.MaximumSlots == 8 && !battle.BayOpen(5) && !battle.BayOpen(6) && battle.BayOpen(7),
                    "Original model entry point retains its legacy behavior");
            }
            Debug.Log("ORDERED_REVIVE_CHECKS_PASSED: bay ordering, partial/all unlocks, no extra bays, fleet positions, ad unlocks, duplicate/phase guards and existing rescue effects");
        }
    }
}
#endif
