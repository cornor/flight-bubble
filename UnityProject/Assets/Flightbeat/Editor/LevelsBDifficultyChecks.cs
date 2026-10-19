using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // B level set, plan step 4 (docs/r1/关卡第二套-实施计划.md): play a level to the end with a fixed bot and
    // report what the comparison needs. Driven by scripts/levels_b/evaluate.py; reads level files directly
    // (no Levels mutation, no scene, no PlayerPrefs). Same setup as evidence runs: empty loadout, default
    // weapon, no items, finale launched as soon as it is ready. 16:20 (first runs: the bot lost even the 5th
    // formal level, so most pairs were "both lost"): the one revive every player gets is taken (and reported),
    // and strategies 1-3 keep the last free pad for a merge or a widely exposed colour instead of clogging it.
    //   Step 0.02 s (Game/PixelWingGame.Frame.cs); a decision every 0.6 s deploys at most one member.
    //   End: Won / Lost, 900 s of model time, or 120 s with no bead removed and no deployment ("timeout").
    // Strategies: 0 game suggestion (SuggestedQueue); 1 attack (exposed head, most ammo); 2 merge (head whose
    // colour completes a three-of-a-kind on the pads, mystery heads by their real colour; else 1); 3 clear
    // (exposed head with least ammo; else 1). With nothing exposed and empty pads, 1-3 deploy the head with the
    // most ammo so the bot never waits forever. Ties go to the lowest lane.
    public static class LevelsBDifficultyChecks
    {
        public const float Step = .02f, DecisionEvery = .6f, MaxSeconds = 900f, IdleSeconds = 120f;
        [Serializable] sealed class Case { public string name, path; public int game, strategy, seed; }
        [Serializable] sealed class Input { public Case[] cases; }
        [Serializable] public sealed class Result
        {
            public string name, path, outcome, error;   // outcome: won / lost / timeout / error
            public int game, strategy, seed, deploys, merges, remaining, total, colorCount;
            public bool revived; public float reviveAt = -1f, progress;
            public float seconds, blockedSeconds, minDistance, longestBlocked;
            public float[] firstExposed;   // per colour index, -1 = never
        }
        [Serializable] sealed class Report { public bool passed; public string unityVersion, error; public List<Result> results = new List<Result>(); }

        static string Arg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            if (at < 0 || at + 1 == args.Length) throw new ArgumentException(key);
            return Path.GetFullPath(args[at + 1]);
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            string output = Arg("--levels-b-eval-output");
            if (File.Exists(output)) throw new IOException("Fresh report required: " + output);
            var report = new Report { unityVersion = Application.unityVersion };
            try
            {
                var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--levels-b-eval-input")));
                if (input.cases == null || input.cases.Length == 0) throw new InvalidDataException("Empty input");
                foreach (var c in input.cases)
                {
                    Result r;
                    try { r = Play(JsonUtility.FromJson<LevelData>(File.ReadAllText(c.path)), c.strategy, c.seed); }
                    catch (Exception e) { r = new Result { outcome = "error", error = e.ToString() }; }
                    r.name = c.name; r.path = c.path; r.game = c.game; r.strategy = c.strategy; r.seed = c.seed;
                    report.results.Add(r);
                    if (c.seed <= 1) Debug.Log($"LEVELS_B_EVAL {c.name}: {r.outcome} {r.seconds:0.0}s");
                }
                report.passed = report.results.TrueForAll(x => x.outcome != "error");
            }
            catch (Exception e) { report.error = e.ToString(); }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (!report.passed) throw new Exception("B level evaluation failed: " + output);
            Debug.Log("LEVELS_B_EVAL_DONE " + output);
        }

        // 16:30: single deterministic runs turned out chaotic (a mirrored board with identical counts flipped
        // won/lost), so the comparison uses several runs per strategy. seed 0 = the fixed bot; seed > 0 adds a
        // reproducible "human" spread: each decision comes 0.4-0.8 s after the previous one, ties between
        // equally good lanes are broken at random and 15 % of decisions are skipped (hesitation).
        public static Result Play(LevelData level, int strategy, int seed = 0)
        {
            var rng = seed > 0 ? new System.Random(seed) : null;
            using var b = new Battle(level, null, true);
            b.ApplyLoadout(FleetLoadout.Empty);
            b.ApplyWeapon(new WeaponProfile());
            var r = new Result { total = b.Total, colorCount = b.ColorCount, minDistance = float.MaxValue, firstExposed = new float[b.ColorCount] };
            for (int i = 0; i < r.firstExposed.Length; i++) r.firstExposed[i] = -1f;
            float clock = 0f, nextDecision = 0f, lastProgress = 0f, blockedRun = 0f;
            int lastRemaining = b.Remaining;
            while (true)
            {
                if (b.State == Phase.ReviveOffer)
                {
                    if (!b.Revived && b.Revive()) { r.revived = true; r.reviveAt = b.Time; lastProgress = clock; }
                    else b.DeclineRevive();
                }
                if (b.State == Phase.Won || b.State == Phase.Lost) { r.outcome = b.State == Phase.Won ? "won" : "lost"; break; }
                if (b.Time >= MaxSeconds || (b.State == Phase.Playing && clock - lastProgress >= IdleSeconds)) { r.outcome = "timeout"; break; }
                if (b.State == Phase.Playing && b.FinaleReady) b.LaunchFinale(0);
                bool ready = b.State == Phase.Ready || b.State == Phase.Playing;
                if (ready && b.ActiveFinale == null && clock >= nextDecision)
                {
                    nextDecision = clock + (rng == null ? DecisionEvery : .4f + .4f * (float)rng.NextDouble());
                    for (int color = 0; color < r.firstExposed.Length; color++)
                        if (r.firstExposed[color] < 0f && b.Exposed(color)) r.firstExposed[color] = b.Time;
                    int lane = rng != null && rng.NextDouble() < .15 ? -1 : Choose(b, strategy, rng);
                    if (lane >= 0 && b.Deploy(lane)) { r.deploys++; lastProgress = clock; }
                }
                b.Tick(Step);
                clock += Step;
                if (b.Remaining != lastRemaining) { lastRemaining = b.Remaining; lastProgress = clock; }
                if (b.State == Phase.Playing && b.ActiveFinale == null)
                {
                    if (b.FrontY >= 0f) r.minDistance = Mathf.Min(r.minDistance, Battle.DefenseLine - b.FrontY);
                    if (b.BlockedTime > 0f) { r.blockedSeconds += Step; blockedRun += Step; r.longestBlocked = Mathf.Max(r.longestBlocked, blockedRun); }
                    else blockedRun = 0f;
                }
            }
            r.seconds = b.Time; r.merges = b.Merges; r.remaining = b.Remaining;
            r.progress = b.Total > 0 ? (b.Total - b.Remaining) / (float)b.Total : 0f;
            if (r.minDistance == float.MaxValue) r.minDistance = -1f;
            return r;
        }

        public const int LastPadColumns = 3;
        static int ExposedColumns(Battle b, int color)
        {
            int n = 0;
            for (int column = 0; column < b.Level.columns; column++) if (b.ExposedAt(column, color) >= 0) n++;
            return n;
        }

        // Equal ammo is a tie; with rng the later tied lane wins half the time.
        static bool Better(int candidate, int current, bool fewest, System.Random rng)
        {
            if (candidate == current) return rng != null && rng.NextDouble() < .5;
            return fewest ? candidate < current : candidate > current;
        }

        static int Choose(Battle b, int strategy, System.Random rng)
        {
            if (b.Fleet.Count >= b.UnlockedSlots) return -1;
            if (strategy == 0) return b.SuggestedQueue();
            bool lastPad = b.Fleet.Count == b.UnlockedSlots - 1;
            int best = -1;
            if (strategy == 2)
                for (int lane = 0; lane < b.LaneCount; lane++)
                {
                    if (b.Reserve[lane].Count == 0) continue;
                    int color = b.Reserve[lane][0].Color, same = 0;   // mystery heads count by their real colour
                    foreach (var ship in b.Fleet) if (ship.Color == color) same++;
                    if (same % 3 == 2 && (best < 0 || Better(b.Reserve[lane][0].Ammo, b.Reserve[best][0].Ammo, false, rng))) best = lane;
                }
            if (best >= 0) return best;
            // The last free pad only takes a member that completes a merge or whose colour is exposed widely.
            if (lastPad)
            {
                for (int lane = 0; lane < b.LaneCount; lane++)
                {
                    if (b.Reserve[lane].Count == 0) continue;
                    var head = b.Reserve[lane][0];
                    int same = 0;
                    foreach (var ship in b.Fleet) if (ship.Color == head.Color) same++;
                    if (same % 3 == 2 || ExposedColumns(b, head.Color) >= LastPadColumns)
                        if (best < 0 || Better(head.Ammo, b.Reserve[best][0].Ammo, false, rng)) best = lane;
                }
                return best;
            }
            bool fewest = strategy == 3;
            for (int lane = 0; lane < b.LaneCount; lane++)
            {
                if (b.Reserve[lane].Count == 0) continue;
                var head = b.Reserve[lane][0];
                if (!b.Exposed(head.Color)) continue;
                if (best < 0 || Better(head.Ammo, b.Reserve[best][0].Ammo, fewest, rng)) best = lane;
            }
            if (best >= 0 || b.Fleet.Count > 0) return best;
            for (int lane = 0; lane < b.LaneCount; lane++)
                if (b.Reserve[lane].Count > 0 && (best < 0 || Better(b.Reserve[lane][0].Ammo, b.Reserve[best][0].Ammo, false, rng))) best = lane;
            return best;
        }
    }
}
