using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class SettingsBroadcastStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked, preferencesRestored;
            public string unityVersion, error;
            public int assertions, commits, meshes, vertices, pilotDecisions;
        }
        private static Report report;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-settings-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI report path required");
            string output = Path.GetFullPath(args[flag + 1]); if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Pilot(); TuningAndMeshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh(Func<int,int> rng = null, Action<FleetProfile> save = null) =>
            new FleetEconomy(new FleetProfile { unlocked = 3, offerSeed = 1 }, () => 1791604800,
                d => { report.commits++; save?.Invoke(d); }, rng ?? (n => 0));
        private static void Transactions()
        {
            var e = Fresh(); var d = e.Data;
            d.coins = 99999998; d.fragments = 9999998; d.energy = 998; d.items = new[] { 98, 0, -9 };
            int revision = d.revision; e.DevGrant(20, 20, 20, 20);
            Check(e.Data.coins == 99999999 && e.Data.fragments == 9999999 && e.Data.energy == 999 && e.Data.items[0] == 99 && e.Data.items[2] == 11, "Independent grant caps");
            Check(e.Data.revision == revision + 1 && !ReferenceEquals(d, e.Data) && d.energy == 998, "Single clone commit");
            e.Data.coins = Int32.MaxValue; e.Data.fragments = Int32.MaxValue; e.Data.energy = Int32.MaxValue; e.Data.items[0] = Int32.MaxValue;
            e.DevGrant(Int32.MaxValue, Int32.MaxValue, 1, 1);
            Check(e.Data.coins == 99999999 && e.Data.fragments == 9999999 && e.Data.energy == Int32.MinValue && e.Data.items[0] == Int32.MinValue, "Signed wide currency versus wrapping narrow sums");
            e.DevGrant(-200000000, -20000000, 0, 0); Check(e.Data.coins < 0 && e.Data.fragments < 0, "No invented lower clamp");
            e.Data.unlimitedUntil = 1791604800 + 600; e.GrantUnlimited(30);
            Check(e.Data.unlimitedUntil == 1791604800 + 2400, "Unlimited extends future deadline");
            e.GrantUnlimited(-50); Check(e.Data.unlimitedUntil == 1791604200, "Negative minutes preserved");
            e.GrantUnlimited(Int32.MaxValue); Check(e.Data.unlimitedUntil == 1791604800L + (long)Int32.MaxValue*60L, "Minutes multiply in signed i64");
            int calls = 0; e = Fresh(n => { calls++; return 0; }); calls = 0;
            Array.Clear(e.Data.parts, 0, 25); Array.Clear(e.Data.spares, 0, 25); e.DevParts(2);
            Check(calls == 10 && e.Data.partDraws == 0, "Five ordered batches without draw stats");
            for (int f = 0; f < 5; f++) Check(e.Data.parts[f * 5] == 1 && e.Data.spares[f * 5] == 1, "New copy then spare per fighter");
            var bounds = new List<int>(); e = Fresh(n => { bounds.Add(n); return 0; }); bounds.Clear();
            int oldId = e.Data.nextCore, oldCount = e.Data.cores.Count; Check(e.DevModules(2) == 2, "Two dev modules");
            Check(bounds.Count >= 4 && bounds[0] == 5 && e.Data.cores[oldCount].id == oldId && e.Data.cores[oldCount+1].id == oldId+1
                && e.Data.cores[oldCount].durability == FleetCatalog.DurabilityMax[0], "Quality before module selection, sequential IDs and durability");
            while (e.Data.cores.Count < 120) e.Data.cores.Add(new FleetCore { id = e.Data.nextCore++ });
            revision = e.Data.revision; bounds.Clear(); Check(e.DevModules(20) == 0 && e.Data.revision == revision+1 && bounds.Count == 0, "Full inventory commits without RNG");
            Array.Clear(e.Data.presets, 0, e.Data.presets.Length); revision = e.Data.revision;
            Check(e.DevWearAll() == 0 && e.Data.revision == revision, "Empty wear guard");
            int id = e.Data.cores[0].id; e.Data.presets[0] = id; e.Data.presets[1] = id; e.Data.presets[2] = 999999;
            Check(e.DevWearAll() == 2 && e.Data.cores[0].durability == 1, "Distinct preset IDs include stale IDs");
            e.Data.streak = 10; e.DevStreak(1); Check(e.Data.streak == 10, "Streak upper clamp");
            e.DevStreak(Int32.MaxValue); Check(e.Data.streak == 0, "Streak wraps before clamp");
            e.Data.cleared.Clear(); e.DevUnlockAll(); Check(e.Data.unlocked == 262 && e.Data.cleared.Count == 0, "Unlock is not clear");
            for (int zone = 0; zone < 5; zone++)
            {
                e.Data.unlocked = 3; e.Data.cleared.Clear(); e.Data.zoneClaimed = 0; e.Data.dayWins = 7;
                e.DevClearZone(zone); int count = e.Data.cleared.Count; e.DevClearZone(zone);
                Check(count == WarZones.LevelsIn(zone) && e.Data.cleared.Count == count && e.ZoneReady(zone) && e.Data.zoneClaimed == 0 && e.Data.dayWins == 7, "Clear idempotency and unclaimed reward");
                Check(e.Data.unlocked == Math.Min(262, WarZones.LastLevel(zone)+2), "Next level unlocked");
            }
            bool fail = false; e = Fresh(save: p => { if (fail) throw new IOException("expected save rejection"); });
            string prior = JsonUtility.ToJson(e.Data); d = e.Data; fail = true;
            try { e.DevGrant(100000, 5000, 20, 10); throw new Exception("Save error swallowed"); }
            catch (IOException) { Check(ReferenceEquals(d, e.Data) && JsonUtility.ToJson(d) == prior, "Save failure publishes nothing"); }
        }
        private static Battle Tiny(int slots = 1, int limit = 2)
        {
            return new Battle(new LevelData { columns = 2, rows = 1, cells = new[] { 0, 1 }, palette = new[] { 255, 0, 0, 0, 255, 0 },
                laneCount = 2, initialSlots = slots, slotLimit = limit, lesson = 1, stream = new int[0],
                queue = new[] { new QueueEntry { lane = 0, color = 0, ammo = 10 }, new QueueEntry { lane = 1, color = 1, ammo = 10 } } }, PressureSettings.Legacy);
        }
        private static void State(Battle b, Phase value) => typeof(Battle).GetProperty("State").SetValue(b, value);
        private static BroadcastDecision Decide(Battle b, float dt = 9f)
        { report.pilotDecisions++; return new BroadcastPilot(b.ColorCount, 137).Tick(b, dt); }
        private static void Pilot()
        {
            using (var b = Tiny())
            {
                var pilot = new BroadcastPilot(2, 137);
                Check(pilot.Tick(b, .69f).action == BroadcastAction.Wait, "Initial decision delay");
                Check(pilot.Tick(b, .02f).action == BroadcastAction.Deploy, "Ready decision at initial threshold");
                Check(pilot.Tick(b, 0f).action == BroadcastAction.Wait, "Same time cannot decide again");
                foreach (Phase s in new[] { Phase.Intro, Phase.Won, Phase.Lost }) { State(b, s); Check(Decide(b).action == BroadcastAction.Wait, "Inactive guard"); }
                State(b, Phase.ReviveOffer); Check(Decide(b).action == BroadcastAction.Rescue, "Rescue decision");
                State(b, Phase.Playing); b.Fleet.Add(new Fighter(0, 10, 77));
                typeof(Battle).GetProperty("BlockedTime").SetValue(b, 9f);
                // Unchanged duration resets on first observation; a second delayed tick crosses its guard.
                pilot = new BroadcastPilot(2, 137); pilot.Tick(b, 9f);
                Check(pilot.Tick(b, 2f).action == BroadcastAction.Bay, "Blocked full fleet opens a bay");
                b.UnlockBay(1); b.Fleet.Add(new Fighter(1, 10, 78)); pilot = new BroadcastPilot(2, 137); pilot.Tick(b, 9f);
                Check(pilot.Tick(b, 2f).action == BroadcastAction.Bomb, "Full opened bays need bomb");
                b.Reserve[0].Clear(); b.Reserve[1].Clear(); b.Level.lesson = 0;
                Check(Decide(b).action == BroadcastAction.Finale, "Empty reserve and remaining enemies trigger finale");
                b.TrainingHold = true; Check(Decide(b).action != BroadcastAction.Finale, "Training hold suppresses finale");
            }
            using (var b = Tiny(3, 3))
            {
                b.Fleet.Add(new Fighter(0, 10, 50)); b.Fleet.Add(new Fighter(0, 10, 51));
                Check(Decide(b).index == 0, "Merge opportunity beats random visible-color alternative");
            }
        }
        private static object Call(PixelWingGame game, string name, params object[] args) => typeof(PixelWingGame).GetMethod(name, Flags).Invoke(game, args);
        private static void Field(PixelWingGame game, string name, object value) => typeof(PixelWingGame).GetField(name, Flags).SetValue(game, value);
        private static void TuningAndMeshes()
        {
            const string key = PressureSettings.TuningKey; bool existed = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key);
            FieldInfo liveField = typeof(PressureSettings).GetField("live", BindingFlags.NonPublic | BindingFlags.Static); object priorLive = liveField.GetValue(null);
            var owner = new GameObject("Settings stage fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                liveField.SetValue(null, null); PlayerPrefs.DeleteKey(key); PressureSettings.SaveTuning(); Check(!PressureSettings.Tuned, "Null live does not save");
                PressureSettings a = PressureSettings.Load(), b = PressureSettings.Load(); a.fusionPush = 999f;
                Check(!ReferenceEquals(a,b) && b.fusionPush != 999f && liveField.GetValue(null) == null, "Test load is fresh authored data, not live tuning");
                PressureSettings.ResetTuning(); object live = liveField.GetValue(null); PressureSettings.SaveTuning(); Check(PressureSettings.Tuned, "Reset creates live, save writes key");
                ((PressureSettings)live).fusionPush = 999f; PressureSettings.ResetTuning();
                Check(ReferenceEquals(live, liveField.GetValue(null)) && !PressureSettings.Tuned && ((PressureSettings)live).fusionPush == b.fusionPush, "Reset overwrites same live instance and deletes key");
                Check(TuningKnob.All.Length == 10, "Ten original knobs");
                for (int i = 0; i < 9; i++) { TuningKnob.All[i].set(a, (float)i + .25f); Check(TuningKnob.All[i].get(a) == (float)i + .25f, "Knob setter target"); }
                foreach (float v in new[] { 2.5f, 3.5f, -2.5f, float.NaN, float.PositiveInfinity, 2147483648f })
                { TuningKnob.All[9].set(a,v); int expected = float.IsNaN(v) || float.IsInfinity(v) || v >= 2147483648f ? Int32.MinValue : (int)Math.Round(v); Check(a.revivePurgeRows == expected, "Integer knob ties and conversion sentinel"); }
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(s,null);
                typeof(PixelWingGame).GetProperty("Economy").SetValue(game, Fresh()); Field(game,"ui",30f); Field(game,"hud",s);
                Field(game,"tuning",true); Call(game,"TuningTap",39); Check(!(bool)typeof(PixelWingGame).GetField("tuning",Flags).GetValue(game), "Done closes tuning");
                for(int k=0;k<20;k++) Call(game,"TuningTap",k);
                Check(PressureSettings.Load().fusionPush == b.fusionPush, "Knob taps preserve authored runtime load");
                Call(game,"TuningTap",38); Check(!PressureSettings.Tuned, "Reset button removes preference");
                Field(game,"devPanel",true); string method = "DrawDevPanel"; s.Paint = x => Call(game,method,x);
                var marks = (Dictionary<int,float>)typeof(PixelWingGame).GetField("uiMarks",Flags).GetValue(game);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh",Flags,null,new[]{typeof(VertexHelper)},null);
                using(var vertices = new VertexHelper()) foreach(string draw in new[]{"DrawDevPanel","DrawTuning"})
                    foreach(float h in new[]{960f,1280f}) foreach(float age in new[]{.05f,.15f,.3f,1f})
                    {
                        method=draw; ScreenLayout.Width=540f;ScreenLayout.Height=h;marks[740]=30f-age;marks[60]=30f-age;s.MapLayout=true;
                        Call(game,"BeginHits");populate.Invoke(s,new object[]{vertices});Check(vertices.currentVertCount>0 && s.MapLayout,"Mesh and mapping restoration");
                        UIVertex v=default;for(int i=0;i<vertices.currentVertCount;i++){vertices.PopulateUIVertex(ref v,i);if(!float.IsFinite(v.position.x)||!float.IsFinite(v.position.y))throw new Exception("Non-finite mesh");}
                        report.meshes++; report.vertices+=vertices.currentVertCount;
                    }
            }
            finally
            {
                ScreenLayout.Width=width;ScreenLayout.Height=height;liveField.SetValue(null,priorLive);
                if(existed)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();report.preferencesRestored=true;
                var s=owner.GetComponent<PixelSurface>();if(s!=null){FieldInfo f=typeof(PixelSurface).GetField("ownedMaterial",Flags);var m=(Material)f.GetValue(s);f.SetValue(s,null);if(m!=null)UnityEngine.Object.DestroyImmediate(m);}
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
