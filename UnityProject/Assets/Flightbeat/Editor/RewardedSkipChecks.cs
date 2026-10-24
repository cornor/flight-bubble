#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class RewardedSkipChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Set(object target, string property, object value) => target.GetType().GetProperty(property, Flags).SetValue(target, value);
        static Battle Fixture(Phase phase)
        {
            var text = Resources.Load<TextAsset>("Levels/sector01");
            Require(text != null, "Authored sector01 fixture missing");
            var battle = new Battle(JsonUtility.FromJson<LevelData>(text.text), null);
            Set(battle, "State", phase);
            return battle;
        }
        public static void Run()
        {
            // Explicit fixtures exercise terminal guards, not a substitute for pointer-driven gameplay tests.
            foreach (var phase in new[] { Phase.Intro, Phase.Ready, Phase.Playing })
            using (var battle = Fixture(phase))
            {
                int wins = 0, cancellations = 0, total = battle.Total, score = battle.Score;
                battle.Cue += cue => { if (cue == "win") wins++; };
                battle.ShotCancelled += shot => cancellations++;
                var shot = MemoryPool.Acquire<Shot>();
                shot.TargetIndex = Array.FindIndex(battle.Alive, alive => alive);
                battle.Shots.Add(shot);
                Set(battle, "PendingStrike", new SuperStrike());
                Set(battle, "ActiveFinale", new Finale());
                Require(battle.CompleteRewardedSkip(), "Eligible phase must win: " + phase);
                Require(battle.State == Phase.Won && battle.Remaining == 0 && battle.Progress == 1f, "Skip must produce a full win");
                Require(battle.Total == total && battle.Score == score, "Skipping must not invent combat score");
                Require(battle.Alive.All(alive => !alive) && battle.Shots.Count == 0 && cancellations == 1, "Clear board and cancel in-flight shots");
                Require(battle.PendingStrike == null && battle.ActiveFinale == null, "Cancel pending attack/finale");
                Require(!battle.CompleteRewardedSkip() && wins == 1, "Repeated callback must not win twice");
                battle.Tick(1f);
                Require(battle.Remaining == 0 && battle.Shots.Count == 0 && wins == 1, "Terminal ticks must stay settled");
            }
            foreach (var phase in new[] { Phase.ReviveOffer, Phase.Lost, Phase.Won })
            using (var battle = Fixture(phase))
            {
                int remaining = battle.Remaining, cues = 0;
                battle.Cue += cue => cues++;
                Require(!battle.CompleteRewardedSkip(), "Reject terminal/revive phase: " + phase);
                Require(battle.State == phase && battle.Remaining == remaining && cues == 0, "Rejected skip must leave state untouched");
            }
            Debug.Log("REWARDED_SKIP_CHECKS_PASSED: eligible phases, terminal guards, single win cue, shot/strike/finale cleanup, no fabricated score");
        }
    }
}
#endif
