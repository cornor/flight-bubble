// Headless port of UnityProject/Assets/Flightbeat/Editor/LevelsBDifficultyChecks.cs (Play / Choose), minus the
// optional trace. Same input/output JSON as the Unity batch, so evaluate.py results can be compared case by case.
//   dotnet LevelsBSim.dll --input cases.json --output results.json [--threads N]
// Case extras (headless only): "queue" — an int array, a permutation of queue entry indices; entry k of the
// played queue takes the member of entry queue[k] while keeping slot k's lane/order/deployAt (see
// levelsb_common.queue_reorder). Lets a search try many orders without writing level files.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Flightbeat.Economy;
using Flightbeat.Model;

namespace Flightbeat.Headless
{
    public sealed class Case { public string name { get; set; } public string path { get; set; } public int game { get; set; } public int strategy { get; set; } public int seed { get; set; } public int[] queue { get; set; } }
    public sealed class Input { public Case[] cases { get; set; } }
    public sealed class Result
    {
        public string name { get; set; } public string path { get; set; } public string outcome { get; set; } public string error { get; set; } = "";
        public int game { get; set; } public int strategy { get; set; } public int seed { get; set; } public int deploys { get; set; } public int merges { get; set; }
        public int remaining { get; set; } public int total { get; set; } public int colorCount { get; set; }
        public bool revived { get; set; } public float reviveAt { get; set; } = -1f; public float progress { get; set; }
        public float seconds { get; set; } public float blockedSeconds { get; set; } public float minDistance { get; set; } public float longestBlocked { get; set; }
        public float[] firstExposed { get; set; }
    }
    public sealed class Report { public bool passed { get; set; } public string unityVersion { get; set; } = "headless-net8"; public string error { get; set; } = ""; public List<Result> results { get; set; } = new List<Result>(); }

    public static class Bot
    {
        public const float Step = .02f, DecisionEvery = .6f, MaxSeconds = 900f, IdleSeconds = 120f;
        public const int LastPadColumns = 3;

        static readonly JsonSerializerOptions Fields = new JsonSerializerOptions { IncludeFields = true };

        // JsonUtility semantics: serialized fields are never null (missing arrays/strings/objects become empty).
        static void Fill(object o)
        {
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(string) && f.GetValue(o) == null) f.SetValue(o, "");
                else if (f.FieldType.IsArray && f.GetValue(o) == null) f.SetValue(o, Array.CreateInstance(f.FieldType.GetElementType(), 0));
                if (f.FieldType == typeof(QueueEntry[])) foreach (var e in (QueueEntry[])f.GetValue(o)) Fill(e);
            }
        }

        public static LevelData Load(string json)
        {
            var level = JsonSerializer.Deserialize<LevelData>(json, Fields);
            Fill(level);
            return level;
        }

        public static void Reorder(LevelData level, int[] order)
        {
            if (order == null) return;
            var q = level.queue;
            if (order.Length != q.Length || order.OrderBy(x => x).Where((x, i) => x != i).Any()) throw new ArgumentException("queue must be a permutation");
            var played = new QueueEntry[q.Length];
            for (int k = 0; k < q.Length; k++)
            {
                var m = q[order[k]];
                played[k] = new QueueEntry
                {
                    lane = q[k].lane, order = q[k].order, deployAt = q[k].deployAt, sourceRow = q[k].sourceRow, sourceColumn = q[k].sourceColumn,
                    color = m.color, ammo = m.ammo, sourceAmmo = m.sourceAmmo, kind = m.kind, adjustment = m.adjustment, other = m.other, sourceToken = m.sourceToken
                };
            }
            level.queue = played;
        }

        public static Result Play(LevelData level, int strategy, int seed = 0)
        {
            var rng = seed > 0 ? new Random(seed) : null;
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
                    bool hesitated = rng != null && rng.NextDouble() < .15;
                    int lane = hesitated ? -1 : Choose(b, strategy, rng);
                    bool deployed = lane >= 0 && b.Deploy(lane);
                    if (deployed) { r.deploys++; lastProgress = clock; }
                }
                b.Tick(Step);
                clock += Step;
                if (b.Remaining != lastRemaining) { lastRemaining = b.Remaining; lastProgress = clock; }
                if (b.State == Phase.Playing && b.ActiveFinale == null)
                {
                    if (b.FrontY >= 0f) r.minDistance = Math.Min(r.minDistance, Battle.DefenseLine - b.FrontY);
                    if (b.BlockedTime > 0f) { r.blockedSeconds += Step; blockedRun += Step; r.longestBlocked = Math.Max(r.longestBlocked, blockedRun); }
                    else blockedRun = 0f;
                }
            }
            r.seconds = b.Time; r.merges = b.Merges; r.remaining = b.Remaining;
            r.progress = b.Total > 0 ? (b.Total - b.Remaining) / (float)b.Total : 0f;
            if (r.minDistance == float.MaxValue) r.minDistance = -1f;
            return r;
        }

        static int ExposedColumns(Battle b, int color)
        {
            int n = 0;
            for (int column = 0; column < b.Level.columns; column++) if (b.ExposedAt(column, color) >= 0) n++;
            return n;
        }

        static bool Better(int candidate, int current, bool fewest, Random rng)
        {
            if (candidate == current) return rng != null && rng.NextDouble() < .5;
            return fewest ? candidate < current : candidate > current;
        }

        static int Choose(Battle b, int strategy, Random rng)
        {
            if (b.Fleet.Count >= b.UnlockedSlots) return -1;
            if (strategy == 0) return b.SuggestedQueue();
            bool lastPad = b.Fleet.Count == b.UnlockedSlots - 1;
            int best = -1;
            if (strategy == 2)
                for (int lane = 0; lane < b.LaneCount; lane++)
                {
                    if (b.Reserve[lane].Count == 0) continue;
                    int color = b.Reserve[lane][0].Color, same = 0;
                    foreach (var ship in b.Fleet) if (ship.Color == color) same++;
                    if (same % 3 == 2 && (best < 0 || Better(b.Reserve[lane][0].Ammo, b.Reserve[best][0].Ammo, false, rng))) best = lane;
                }
            if (best >= 0) return best;
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

    public static class Program
    {
        static string Arg(string[] args, string key, string fallback = null)
        {
            int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback ?? throw new ArgumentException(key);
        }

        public static int Main(string[] args)
        {
            var input = JsonSerializer.Deserialize<Input>(File.ReadAllText(Arg(args, "--input")));
            int threads = int.Parse(Arg(args, "--threads", Environment.ProcessorCount.ToString()));
            var texts = new ConcurrentDictionary<string, string>();
            var results = new Result[input.cases.Length];
            Parallel.For(0, input.cases.Length, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                var c = input.cases[i];
                Result r;
                try
                {
                    var level = Bot.Load(texts.GetOrAdd(c.path, File.ReadAllText));
                    Bot.Reorder(level, c.queue);
                    r = Bot.Play(level, c.strategy, c.seed);
                }
                catch (Exception e) { r = new Result { outcome = "error", error = e.ToString() }; }
                r.name = c.name; r.path = c.path; r.game = c.game; r.strategy = c.strategy; r.seed = c.seed;
                results[i] = r;
            });
            var report = new Report { results = results.ToList() };
            report.passed = report.results.TrueForAll(x => x.outcome != "error");
            File.WriteAllText(Arg(args, "--output"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = false }));
            return report.passed ? 0 : 1;
        }
    }
}
