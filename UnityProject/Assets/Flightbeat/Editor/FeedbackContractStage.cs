using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    public static class FeedbackContractStage
    {
        [Serializable] private sealed class Command { public string name; public int[] bits, color; }
        [Serializable] private sealed class Case
        {
            public int id; public string member, expectedText;
            public int[] ints, bits, expectedBits, expectedInts, palette;
            public bool noLook; public Command[] commands;
        }
        [Serializable] private sealed class Input { public int[] accentBits, display; public Case[] cases; }
        [Serializable] private sealed class Failure { public int id; public string member, error; }
        [Serializable] private sealed class Report
        {
            public bool passed; public string unityVersion, error;
            public int cases, exactCases, geometryCases, comparedVertices;
            public List<Failure> failures = new List<Failure>();
        }
        private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type FeedbackType = typeof(CombatFeedback);
        private static readonly MethodInfo Populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", All, null, new[] { typeof(VertexHelper) }, null);
        private static object Invoke(CombatFeedback feedback, string name, params object[] args) => FeedbackType.GetMethod(name, All).Invoke(feedback, args);
        private static void Field(CombatFeedback feedback, string name, object value) => FeedbackType.GetField(name, All).SetValue(feedback, value);
        private static T Field<T>(CombatFeedback feedback, string name) => (T)FeedbackType.GetField(name, All).GetValue(feedback);
        private static T Property<T>(CombatFeedback feedback, string name) => (T)FeedbackType.GetProperty(name, All).GetValue(feedback);
        private static float Float(int bits) => BitConverter.Int32BitsToSingle(bits);
        private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);
        private static int[] Bits(Color c) => new[] { Bits(c.r), Bits(c.g), Bits(c.b), Bits(c.a) };
        private static int[] Bits(Vector2 v) => new[] { Bits(v.x), Bits(v.y) };
        private static Color ColorAt(int[] bits, int offset = 0) => new Color(Float(bits[offset]), Float(bits[offset + 1]), Float(bits[offset + 2]), Float(bits[offset + 3]));
        private static void Exact(int[] expected, int[] actual)
        {
            if (!expected.SequenceEqual(actual)) throw new Exception("bits/int expected [" + String.Join(",", expected) + "] actual [" + String.Join(",", actual) + "]");
        }
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string key) { int i = Array.IndexOf(args, key); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(key); return Path.GetFullPath(args[i + 1]); }
            if (!Application.isBatchMode) throw new Exception("CLI only");
            string output = Arg("--flightbeat-feedback-contract-report");
            if (File.Exists(output)) throw new IOException("Fresh report required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = new Report { unityVersion = Application.unityVersion };
            Color[] savedAccents = (Color[])FleetCatalog.FighterAccent.Clone();
            float savedWidth = ScreenLayout.Width, savedHeight = ScreenLayout.Height;
            GameObject owner = null;
            try
            {
                Input input = JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-feedback-contract-input")));
                for (int n = 0; n < 5; n++) FleetCatalog.FighterAccent[n] = ColorAt(input.accentBits, n * 4);
                var originalDisplay = (Color32[])typeof(CombatPalette).GetField("Display", All).GetValue(null);
                Exact(input.display, originalDisplay.Select(c => unchecked((int)((uint)c.r | (uint)c.g << 8 | (uint)c.b << 16 | (uint)c.a << 24))).ToArray());
                owner = new GameObject("Feedback helper contracts", typeof(RectTransform));
                PixelSurface surface = owner.AddComponent<PixelSurface>();
                ScreenLayout.Width = 540; ScreenLayout.Height = 1280;
                var battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/training01").text), null, true);
                var feedback = new CombatFeedback(battle, null, CombatPalette.ForLevel(battle.Level));
                foreach (Case c in input.cases)
                {
                    report.cases++;
                    try
                    {
                        Check(c, battle, feedback, surface, report); report.exactCases++;
                    }
                    catch (Exception e) { report.failures.Add(new Failure { id = c.id, member = c.member, error = e.ToString() }); }
                }
                report.passed = report.failures.Count == 0;
                if (!report.passed) throw new Exception(report.failures.Count + " feedback contract mismatches");
            }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally
            {
                Array.Copy(savedAccents, FleetCatalog.FighterAccent, 5);
                ScreenLayout.Width = savedWidth; ScreenLayout.Height = savedHeight;
                if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
        }
        private static void Check(Case c, Battle battle, CombatFeedback feedback, PixelSurface surface, Report report)
        {
            float F(int n) => Float(c.bits[n]);
            switch (c.member)
            {
                case "ComboColor": Exact(c.expectedBits, Bits((Color)Invoke(null, c.member, c.ints[0]))); break;
                case "TickCombo":
                    typeof(Battle).GetProperty("Combo").GetSetMethod(true).Invoke(battle, new object[] { c.ints[0] });
                    Field(feedback, "comboBand", c.ints[1]); Field(feedback, "lastCombo", c.ints[2]); Field(feedback, "milestoneValue", c.ints[3]);
                    Field(feedback, "comboPunch", F(1)); Field(feedback, "comboMilestoneAge", F(2)); Field(feedback, "comboFade", F(3));
                    Invoke(feedback, c.member, F(0));
                    Exact(c.expectedInts, new[] { Field<int>(feedback, "comboBand"), Field<int>(feedback, "lastCombo"), Field<int>(feedback, "milestoneValue") });
                    Exact(c.expectedBits, new[] { Bits(Field<float>(feedback, "comboPunch")), Bits(Field<float>(feedback, "comboMilestoneAge")), Bits(Field<float>(feedback, "comboFade")) });
                    break;
                case "Look": case "PartLv":
                    feedback.Look = c.noLook ? null : new FleetLook { model = c.ints[0], parts = new[] { -1, 0, 2, 5, 6 } };
                    if (c.member == "PartLv") Exact(c.expectedInts, new[] { (int)Invoke(feedback, c.member, c.ints[1]) });
                    else
                    {
                        Exact(c.expectedInts, new[] { feedback.Model });
                        Exact(c.expectedBits, new[] { Bits(Property<float>(feedback, "BeamWidth")) }.Concat(Bits(Property<Color>(feedback, "Accent"))).ToArray());
                    }
                    break;
                case "QueueBadge": case "QueueParameter":
                    var ship = new Fighter(0, 0, 0) { Kind = c.ints[0], Other = c.ints[1] == 0 ? null : c.ints[1] == 1 ? new int[0] : new[] { -7, 0 } };
                    string text = (string)Invoke(null, c.member, ship);
                    if (text != c.expectedText) throw new Exception("text expected " + c.expectedText + " actual " + text);
                    break;
                case "SplitCurve": Exact(c.expectedBits, Bits((Vector2)Invoke(null, c.member, new Vector2(F(0), F(1)), new Vector2(F(2), F(3)), new Vector2(F(4), F(5)), F(6)))); break;
                case "Perp": Exact(c.expectedBits, Bits((Vector2)Invoke(null, c.member, new Vector2(F(0), F(1))))); break;
                case "A": Exact(c.expectedBits, Bits((Color)Invoke(null, c.member, ColorAt(c.bits), F(4)))); break;
                case "PaletteAt": Exact(c.expectedBits, Bits(CombatPalette.At(c.ints[0]))); break;
                case "ForLevel":
                    Exact(c.expectedBits, CombatPalette.ForLevel(new LevelData { lesson = c.ints[0], palette = c.ints[1] == 0 ? null : c.palette }).SelectMany(Bits).ToArray());
                    break;
                case "DrawTimes": case "Mark":
                    Geometry(c, surface, report); break;
                default: throw new Exception("Unhandled source fixture " + c.member);
            }
        }
        private static List<UIVertex> Mesh(PixelSurface surface, Action<PixelSurface> paint, bool mapped)
        {
            surface.MapLayout = mapped; surface.Paint = paint;
            using (var mesh = new VertexHelper())
            {
                Populate.Invoke(surface, new object[] { mesh });
                var vertices = new List<UIVertex>(); mesh.GetUIVertexStream(vertices); return vertices;
            }
        }
        private static void Geometry(Case c, PixelSurface surface, Report report)
        {
            // Compare helper composition to captured original primitive calls.
            // Both paths share PixelSurface, so this does not validate the primitive implementation or GPU output.
            bool mapped = c.ints[0] != 0;
            float F(int n) => Float(c.bits[n]); Color color = ColorAt(c.bits, 3);
            var actual = Mesh(surface, s =>
            {
                if (c.member == "DrawTimes") Invoke(null, "DrawTimes", s, F(0), F(1), F(2), color);
                else CombatPalette.Mark(s, c.ints[1], new Vector2(F(0), F(1)), F(2), color);
            }, mapped);
            if (surface.MapLayout != mapped) throw new Exception("Mapping flag not restored");
            var expected = Mesh(surface, s =>
            {
                foreach (Command command in c.commands)
                {
                    float V(int n) => Float(command.bits[n]); Color tint = ColorAt(command.color);
                    if (command.name == "Rect") s.Rect(V(0), V(1), V(2), V(3), tint);
                    else if (command.name == "Border") s.Border(V(0), V(1), V(2), V(3), tint, V(4));
                    else s.Segment(new Vector2(V(0), V(1)), new Vector2(V(2), V(3)), V(4), tint);
                }
            }, mapped);
            if (actual.Count != expected.Count) throw new Exception("Primitive vertex count differs");
            int[] Channels(UIVertex v) => new[] { Bits(v.position.x), Bits(v.position.y), Bits(v.position.z), Bits(v.uv0.x), Bits(v.uv0.y), Bits(v.uv1.x), Bits(v.uv1.y), (int)v.color.r, v.color.g, v.color.b, v.color.a };
            for (int n = 0; n < actual.Count; n++) Exact(Channels(expected[n]), Channels(actual[n]));
            report.geometryCases++; report.comparedVertices += actual.Count;
        }
    }
}
