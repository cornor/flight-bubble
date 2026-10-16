using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Frozen original commands are INPUTS only in replay mode. In game mode they
    // are never read; declared controller inputs come from a separate source snapshot.
    public static partial class FixedDrawStage
    {
        [Serializable] private sealed class Input { public CaseInput[] cases; }
        [Serializable] private sealed class CaseInput
        {
            public string name, level, profileJson;
            public bool inHome;
            public float gameClock, uiClock, homeEnteredAt;
            public QuadInput[] commands;
            public bool hasController;
            public ControllerInput controller;
            public int frame;
            public BattleStep[] battleSteps;
            public UiField[] uiFields;
            public SupplyInput transaction;
        }
        [Serializable] private sealed class UiField { public string name, kind; public int bits; }
        [Serializable] private sealed class BattleAction { public int queue; public bool result; }
        [Serializable] private sealed class BattleStep { public int frame, dtBits; public BattleAction[] actions; }
        [Serializable] private sealed class ControllerInput
        {
            public int tutorialStep, spokenCharacters, narrationTopic;
            public string dialogue;
            public float dialogueAge, glyphAt, narrationAge;
            public bool lessonBattleTrainingSafe;
            public LessonInput lesson;
        }
        [Serializable] private sealed class LessonInput
        {
            public int Chapter, Step, WrongChoices;
            public float Age;
            public bool IndependentChoice, HelpUsed;
        }
        [Serializable] private sealed class QuadInput
        {
            public string surface;
            public float[] values;
            public bool rigid, mapLayout, lighting;
        }
        [Serializable] private sealed class QuadOutput
        {
            public string surface;
            public int sequence, order;
            public int[] callPath;
            public int[] inputBits, vertexBits;
            public bool rigid, mapLayout, lighting;
        }

        [Serializable] private sealed class HitOutput { public int id; public int[] rectBits; }
        [Serializable] private sealed class CaseOutput
        {
            public string name, error;
            public List<SupplyState> transactionStates;
            public GpuOutput gpu;
            public int frame, battleState, battleTimeBits, remaining, fleetCount;
            public List<QuadOutput> commands = new List<QuadOutput>();
            public List<DrawObservation.MeshData> meshes = new List<DrawObservation.MeshData>();
            public DrawObservation.Data observed;
            public List<HitOutput> hits = new List<HitOutput>();
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion, mode;
            public bool fullOriginalEquivalenceChecked;
            public List<CaseOutput> cases = new List<CaseOutput>();
        }
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void Set(object o, string name, object v)
        {
            var t = o.GetType(); var p = t.GetProperty(name, Instance);
            if (p != null) p.SetValue(o, v); else (t.GetField(name, Instance) ?? throw new MissingFieldException(name)).SetValue(o, v);
        }
        private static object Call(object o, string method, params object[] args) =>
            o.GetType().GetMethod(method, Instance, null, Array.ConvertAll(args, a => a.GetType()), null).Invoke(o, args);
        private static int Bits(float f) => BitConverter.SingleToInt32Bits(f);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static float InputSingle(float f) => BitConverter.Int32BitsToSingle(Bits(f));
        private static int[] Pack(UIVertex v) => DrawObservation.Pack(v);
        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 == args.Length) throw new ArgumentException(name);
            return args[i + 1];
        }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("CLI only");
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-fixed-input")));
            string output = Path.GetFullPath(Arg("--flightbeat-fixed-report")), mode = Arg("--flightbeat-fixed-mode");
            if (mode != "replay" && mode != "game") throw new ArgumentException("replay or game mode required");
            if (File.Exists(output)) throw new IOException("Fresh output required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = new Report { unityVersion = Application.unityVersion, mode = mode };
            float? savedDelta = PixelWingGame.PaintDeltaOverride;
            // All mutable layout statics are restored even if painting fails.
            var layout = typeof(ScreenLayout).GetFields(BindingFlags.Public | BindingFlags.Static);
            var saved = new Dictionary<FieldInfo, object>();
            foreach (var field in layout) if (!field.IsLiteral && !field.IsInitOnly) saved.Add(field, field.GetValue(null));
            try
            {
                PixelWingGame.PaintDeltaOverride = 0f;
                foreach (CaseInput c in input.cases)
                {
                    var result = new CaseOutput { name = c.name, frame = c.frame }; report.cases.Add(result);
                    try { Capture(c, result, mode == "replay"); }
                    catch (Exception e) { result.error = e.ToString(); }
                }
            }
            finally
            {
                PixelWingGame.PaintDeltaOverride = savedDelta;
                foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
                File.WriteAllText(output, JsonUtility.ToJson(report));
            }
            foreach (var c in report.cases) if (!string.IsNullOrEmpty(c.error)) throw new Exception(c.name + ": " + c.error);
        }
        private static void Capture(CaseInput c, CaseOutput output, bool replay)
        {
            var owner = new GameObject("Fixed draw CLI fixture"); owner.SetActive(false);
            GameObject effectOwner = null;
            Battle battle = null, lessonBattle = null;
            var gpuMeshes = new Dictionary<PixelSurface, Mesh>();
            string[] args = Environment.GetCommandLineArgs(); int gpuFlag = Array.IndexOf(args, "--flightbeat-gpu-directory");
            string gpuDirectory = gpuFlag >= 0 ? Path.GetFullPath(Arg("--flightbeat-gpu-directory")) : null;
            try
            {
                ScreenLayout.Configure(540, 960, new Rect(0f, 0f, 1f, 1f));
                PixelWingGame game = null;
                if (!replay)
                {
                    game = owner.AddComponent<PixelWingGame>();
                    // Inactive component: constructor defaults, no Awake/PlayerPrefs.
                    Set(game, "audioChip", owner.AddComponent<ChipAudio>());
                    var level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/" + c.level).text);
                    battle = new Battle(level, null, true);
                    // JsonUtility materializes absent nested classes; the actions array distinguishes an optional transaction.
                    bool hasTransaction = c.transaction?.actions != null;
                    if (hasTransaction && c.transaction.randomTape == null) throw new Exception("Missing supply RNG input");
                    int randomCursor = 0; var saves = new List<string>();
                    var economy = new FleetEconomy(JsonUtility.FromJson<FleetProfile>(c.profileJson), () => 1800000000L,
                        p => { if (hasTransaction) saves.Add(FleetSaveStore.Encode(p)); }, n =>
                        {
                            if (!hasTransaction || randomCursor >= c.transaction.randomTape.Length) throw new Exception("Unexpected random boundary");
                            var input = c.transaction.randomTape[randomCursor++];
                            if (input.bound != n || input.value < 0 || input.value >= n) throw new Exception("Invalid declared economy RNG input");
                            return input.value;
                        });
                    saves.Clear();
                    var colors = (Color[])typeof(PixelWingGame).GetMethod("BuildPalette", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { level });
                    PixelEffectPool effects;
                    if (c.battleSteps != null && c.battleSteps.Length != 0)
                    {
                        // Real authored particles may be requested by visual callbacks.
                        // Particle transforms/GPU output are outside this mesh observation.
                        effectOwner = new GameObject("Battle drawing particle owner", typeof(RectTransform));
                        effects = new PixelEffectPool(effectOwner.transform, (RectTransform)effectOwner.transform);
                    }
                    else effects = new PixelEffectPool(null, null);
                    var feedback = new CombatFeedback(battle, effects, colors);
                    Set(game, "Battle", battle); Set(game, "Economy", economy); Set(game, "feedback", feedback);
                    Set(game, "colors", colors); Set(game, "effects", effects);
                    Set(game, "clock", c.gameClock); Set(game, "ui", c.uiClock); Set(game, "homeEnteredAt", c.homeEnteredAt);
                    Set(game, "shownCoins", (float)economy.Data.coins); Set(game, "shownFragments", (float)economy.Data.fragments);
                    Set(game, "InHome", c.inHome); Set(game, "Page", DeckPage.Bridge);
                    Call(game, "SetupSortie");
                    if (c.hasController)
                    {
                        // Source graph has two distinct Ready battles: the lesson retains
                        // the startup battle, while Game/feedback use the model-driver battle.
                        ControllerInput state = c.controller;
                        lessonBattle = new Battle(level, null, true);
                        lessonBattle.TrainingSafe = state.lessonBattleTrainingSafe;
                        var lesson = new FlightLesson(lessonBattle);
                        if (lesson.Chapter != state.lesson.Chapter) throw new Exception("Lesson chapter input mismatch");
                        Set(lesson, "Step", state.lesson.Step); Set(lesson, "Age", state.lesson.Age);
                        Set(lesson, "IndependentChoice", state.lesson.IndependentChoice);
                        Set(lesson, "HelpUsed", state.lesson.HelpUsed); Set(lesson, "WrongChoices", state.lesson.WrongChoices);
                        var narration = new FlightNarration(lessonBattle, lesson);
                        Set(narration, "Topic", (FlightTopic)state.narrationTopic); Set(narration, "Age", state.narrationAge);
                        Set(game, "lesson", lesson); Set(game, "narration", narration);
                        Set(game, "tutorialStep", state.tutorialStep); Set(game, "spokenCharacters", state.spokenCharacters);
                        Set(game, "dialogue", state.dialogue); Set(game, "dialogueAge", state.dialogueAge); Set(game, "glyphAt", state.glyphAt);
                    }
                    if (c.battleSteps != null && c.battleSteps.Length != 0)
                    {
                        // f54695 initial displayed heights. Inputs are the old deployment
                        // tape; no expected model/drawing state is assigned here.
                        var shown = new float[battle.Alive.Length];
                        for (int i = 0; i < shown.Length; i++) shown[i] = battle.GridY(i);
                        Set(game, "shownY", shown);
                        float gameClock = c.gameClock, uiClock = c.uiClock;
                        int frame = 0;
                        foreach (var step in c.battleSteps)
                        {
                            if (step.frame != frame++) throw new Exception("Nonconsecutive battle draw tape");
                            float dt = BitConverter.Int32BitsToSingle(step.dtBits);
                            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) throw new Exception("Invalid battle draw delta");
                            foreach (var action in step.actions)
                                if (battle.Deploy(action.queue) != action.result) throw new Exception("Frozen deployment result changed");
                            uiClock = InputSingle(uiClock + dt); gameClock = InputSingle(gameClock + dt);
                            Set(game, "ui", uiClock); Set(game, "clock", gameClock);
                            // Declared isolated visual fixture, not Game.Update lifecycle.
                            feedback.Tick(dt); battle.Tick(dt); Call(game, "UpdateShownSand", dt);
                        }
                        if (frame - 1 != c.frame) throw new Exception("Battle draw frame mismatch");
                    }
                    output.battleState = (int)battle.State; output.battleTimeBits = Bits(battle.Time);
                    output.remaining = battle.Remaining; output.fleetCount = battle.Fleet.Count;
                    if (c.uiFields != null)
                        foreach (var field in c.uiFields)
                        {
                            var property = typeof(PixelWingGame).GetProperty(field.name, Instance);
                            var member = typeof(PixelWingGame).GetField(field.name, Instance);
                            Type type = property?.PropertyType ?? member?.FieldType ?? throw new MissingFieldException(field.name);
                            object value;
                            if (field.kind == "float" && type == typeof(float)) value = BitConverter.Int32BitsToSingle(field.bits);
                            else if (field.kind == "bool" && type == typeof(bool) && (field.bits == 0 || field.bits == 1)) value = field.bits != 0;
                            else if (field.kind == "int" && type == typeof(int)) value = field.bits;
                            else if (field.kind == "enum" && type.IsEnum && Enum.IsDefined(type, field.bits)) value = Enum.ToObject(type, field.bits);
                            else throw new Exception("Unsupported declared UI input " + field.name + ": " + field.kind);
                            Set(game, field.name, value);
                        }
                    if (hasTransaction) output.transactionStates = RunSupply(game, c.transaction, saves, () => randomCursor);
                }
                // Initialize both surfaces before opening the original-equivalent drawing gate.
                var surfaces = new Dictionary<PixelSurface, string>();
                foreach (string name in new[] { "world", "hud" })
                {
                    var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(owner.transform, false);
                    var surface = child.AddComponent<PixelSurface>();
                    if (!replay) Call(surface, "Awake");
                    surfaces.Add(surface, name);
                    if (game != null) Set(game, name == "world" ? "surface" : "hud", surface);
                }
                using (var observation = new DrawObservation(surfaces))
                {
                    output.observed = observation.Result;
                    foreach (var pair in surfaces)
                    {
                        var surface = pair.Key; string name = pair.Value;
                        surface.QuadObserver = q => {
                            var vertices = new List<int>(); foreach (UIVertex v in q.vertices) vertices.AddRange(Pack(v));
                            output.commands.Add(new QuadOutput { surface = name, sequence = output.commands.Count,
                                order = q.order, callPath = q.callPath, inputBits = Array.ConvertAll(q.input, Bits),
                                vertexBits = vertices.ToArray(), rigid = q.rigid, mapLayout = q.mapLayout, lighting = q.lighting });
                        };
                        if (replay) surface.Paint = s => {
                            foreach (QuadInput q in c.commands) if (q.surface == name)
                            {
                                float[] v = q.values; s.MapLayout = q.mapLayout; s.Lighting = q.lighting;
                                s.Quad(v[0], v[1], v[2], v[3], new Rect(v[4], v[5], v[6], v[7]),
                                    new Color(v[8], v[9], v[10], v[11]), v[12], v[13], q.rigid, v[14]);
                            }
                        };
                        else
                        {
                            surface.Paint = (Action<PixelSurface>)Delegate.CreateDelegate(typeof(Action<PixelSurface>), game,
                                typeof(PixelWingGame).GetMethod(name == "world" ? "Draw" : "DrawHUD", Instance));
                        }
                        using (var helper = new VertexHelper())
                        {
                            Call(surface, "OnPopulateMesh", helper);
                            output.meshes.Add(DrawObservation.ReadMesh(name, helper));
                            if (gpuDirectory != null) { var nativeMesh = new Mesh(); helper.FillMesh(nativeMesh); gpuMeshes.Add(surface, nativeMesh); }
                        }
                    }
                    if (game != null)
                        foreach (object hit in (IEnumerable)typeof(PixelWingGame).GetField("uiHits", Instance).GetValue(game))
                        {
                            Type t = hit.GetType(); var r = new int[4]; int i = 0;
                            foreach (string field in new[] { "x", "y", "w", "h" }) r[i++] = Bits((float)t.GetField(field, Instance).GetValue(hit));
                            output.hits.Add(new HitOutput { id = (int)t.GetField("id", Instance).GetValue(hit), rectBits = r });
                        }
                }
                if (gpuDirectory != null) output.gpu = RenderGpu(gpuDirectory, c.name, surfaces, gpuMeshes);
            }
            finally
            {
                foreach (var m in gpuMeshes.Values) UnityEngine.Object.DestroyImmediate(m);
                battle?.Dispose();
                lessonBattle?.Dispose();
                if (effectOwner != null) UnityEngine.Object.DestroyImmediate(effectOwner);
                foreach (PixelSurface s in owner.GetComponentsInChildren<PixelSurface>(true))
                {
                    s.QuadObserver = null;
                    var material = (Material)typeof(PixelSurface).GetField("ownedMaterial", Instance).GetValue(s);
                    Set(s, "ownedMaterial", null);
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
