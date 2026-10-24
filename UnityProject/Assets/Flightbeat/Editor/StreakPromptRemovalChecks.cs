#if UNITY_EDITOR
using System;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Economy;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class StreakPromptRemovalChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            // Inactive fixture avoids Awake and never touches the player's persisted profile.
            var root = new GameObject("Streak action fixture"); root.SetActive(false);
            var game = root.AddComponent<PixelWingGame>();
            var type = typeof(PixelWingGame);
            var confirm = type.GetMethod("ConfirmStreakBreak", Flags);
            int checkedCases = 0;
            try
            {
                foreach (bool teaching in new[] { false, true })
                foreach (int streak in new[] { 0, 3 })
                foreach (Phase phase in new[] { Phase.Ready, Phase.Playing, Phase.ReviveOffer, Phase.Won, Phase.Lost })
                foreach (string actionName in new[] { "重新开始", "现在退出", "放弃复活" })
                {
                    var level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector01").text);
                    level.lesson = teaching ? 1 : 0;
                    using (var battle = new Battle(level, null))
                    {
                        typeof(Battle).GetProperty("State", Flags).SetValue(battle, phase);
                        var economy = new FleetEconomy(new FleetProfile { unlocked = 262, energy = 5, energyAt = 1800000000, streak = streak }, () => 1800000000L, _ => {}, _ => 0);
                        type.GetProperty("Battle", Flags).SetValue(game, battle);
                        type.GetProperty("Economy", Flags).SetValue(game, economy);
                        type.GetProperty("InHome", Flags).SetValue(game, false);
                        int calls = 0;
                        int expected = !teaching && phase != Phase.Won && phase != Phase.Lost ? 0 : streak;
                        Action action = () => { calls++; Require(economy.Streak == expected, "Streak must settle before requested action"); };
                        confirm.Invoke(game, new object[] { actionName, action, actionName == "放弃复活" });
                        Require(calls == 1 && !game.StreakPromptOpen, "Action must run once without a prompt");
                        Require(type.GetField("streakAction", Flags).GetValue(game) == null, "No pending confirmation callback");
                        checkedCases++;
                    }
                }
                Debug.Log("STREAK_PROMPT_REMOVAL_PASSED: " + checkedCases + " cases; exit/restart/decline actions, streak persistence, tutorial and terminal exclusions");
            }
            finally
            {
                type.GetProperty("Battle", Flags).SetValue(game, null);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
