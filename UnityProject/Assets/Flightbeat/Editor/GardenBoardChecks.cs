#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Flightbeat.Feedback;
using Flightbeat.Game;
using Flightbeat.Garden;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Exercise invalidation and compare every retained vertex/triangle with the
    // former immediate board renderer. No profile writes or gameplay changes.
    public static class GardenBoardChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
        static void Property(object target, string name, object value) => target.GetType().GetProperty(name, Flags).SetValue(target, value);
        static void Require(bool yes, string message) { if (!yes) throw new Exception(message); }
        struct Bead { public int index; public Vector2 at; public float squash; }

        static void Reference(PixelWingGame game, PixelSurface surface, Battle battle)
        {
            surface.MapLayout = false;
            float top = PlushLayout.Y(114f) - 10f, bottom = PlushLayout.Y(562f) + 10f;
            GardenSkin.Asset9(surface, "scene/plush-board", 2f, top, 536f, bottom - top, 27f, 27f, 18f, 18f, Color.white);
            surface.MapLayout = true;
            float offset = (float)Field(game, "advanceShown"), ui = (float)Field(game, "ui");
            var feedback = (CombatFeedback)Field(game, "feedback");
            float pitchX = (battle.Level.pitchX > 0 ? battle.Level.pitchX : 10.5f) * ScreenLayout.EnemyScaleX;
            float w = pitchX * 1.05f / .94f, h = Mathf.Min(w, battle.PitchY * ScreenLayout.EnemyScaleY * 1.3f / .94f);
            var x = (float[])Field(game, "sandX"); var y = (float[])Field(game, "sandY");
            var land = (float[])Field(game, "sandLandAt"); var k = (float[])Field(game, "sandLandK");
            var beads = new List<Bead>();
            for (int i = 0; i < battle.Alive.Length; i++) {
                if (!battle.Alive[i] || !battle.OnBoard(i)) continue;
                Vector2 target = ScreenLayout.EnemyPoint(x[i], y[i] + offset), at = target;
                if (battle.State == Phase.Intro) {
                    if (FleetOpening.Progress(i, battle.IntroTime, target) <= 0) continue;
                    at = FleetOpening.EnemyPosition(i, battle.IntroTime, target);
                }
                if (at.y < 114 || at.y > 572 || at.x < 12 || at.x > 528) continue;
                at.y += feedback.GardenStrikeShake;
                float age = ui - land[i], squash = age < 0 || age > .16f ? 0 : Mathf.Sin(age / .16f * Mathf.PI) * .14f * k[i];
                if (squash > 0) at.y += h * squash * .5f;
                beads.Add(new Bead { index = i, at = at, squash = squash });
            }
            beads.Sort((a, b) => { int n = a.at.y.CompareTo(b.at.y); if (n == 0) n = a.at.x.CompareTo(b.at.x); return n == 0 ? a.index.CompareTo(b.index) : n; });
            foreach (var bead in beads) {
                int j = battle.ColorAt(bead.index) * 3; var p = battle.Level.palette;
                Color tint = new Color32((byte)p[j], (byte)p[j + 1], (byte)p[j + 2], 255);
                if ((int)Field(game, "selection") == 2 && (bool)Call(game, "InBombCircle", bead.at))
                    tint = Color.Lerp(tint, Color.white, .3f + .2f * Mathf.Sin(ui * 7f));
                GardenSkin.Bug(surface, tint, bead.at, h * (1f - bead.squash), false, w * (1f + bead.squash));
            }
        }

        public static void Run()
        {
            var root = new GameObject("Retained board checks"); root.SetActive(false);
            bool plush = PlushLayout.Enabled;
            var battles = new List<Battle>();
            int checks = 0;
            try {
                var canvas = new GameObject("Canvas", typeof(Canvas)); canvas.transform.SetParent(root.transform);
                var go = new GameObject("Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(PixelSurface));
                go.transform.SetParent(canvas.transform);
                var surface = go.GetComponent<PixelSurface>(); Call(surface, "Awake");
                var rect = surface.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(540, 960); rect.anchoredPosition = new Vector2(-270, -480);
                var game = root.AddComponent<PixelWingGame>();
                Set(game, "surface", surface); Set(game, "canvasRect", rect);
                var pool = new PixelEffectPool(root.transform, rect) { Enabled = false };
                Battle battle = null;
                void Load(string name) {
                    battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/" + name).text), null);
                    battles.Add(battle); Property(battle, "State", Phase.Ready); Property(game, "Battle", battle);
                    Set(game, "feedback", new CombatFeedback(battle, pool, CombatPalette.ForLevel(battle.Level)));
                    Set(game, "selection", 0);
                }
                Load("sector01"); PlushLayout.Enabled = true;
                ScreenLayout.Configure(390, 844, new Rect(0, 0, 1, 1));
                Call(game, "InitializeGardenBoard");
                var board = (PixelSurface)Field(game, "gardenBoard");
                if (Field(board, "ownedMaterial") == null) Call(board, "Awake");
                Require(board.transform.GetSiblingIndex() < surface.transform.GetSiblingIndex(), "Board must precede rail and queue");
                Require(board.GetComponent<Canvas>() && !board.GetComponent<Canvas>().overrideSorting, "Board needs its own inherited canvas");
                using var expected = new VertexHelper(); using var actual = new VertexHelper();
                var em = new Mesh(); var am = new Mesh();
                try {
                    void Compare(string label) {
                        expected.Clear(); actual.Clear(); Set(surface, "mesh", expected); Reference(game, surface, battle);
                        Set(board, "mesh", actual); Call(game, "DrawGardenBoard", board);
                        Require(expected.currentVertCount == actual.currentVertCount, label + " vertex count");
                        UIVertex a = default, b = default;
                        for (int i = 0; i < expected.currentVertCount; i++) {
                            expected.PopulateUIVertex(ref a, i); actual.PopulateUIVertex(ref b, i);
                            Require(a.Equals(b), label + " vertex " + i);
                        }
                        expected.FillMesh(em); actual.FillMesh(am);
                        int[] ei = em.triangles, ai = am.triangles;
                        Require(ei.Length == ai.Length, label + " triangle count");
                        for (int i = 0; i < ei.Length; i++) Require(ei[i] == ai[i], label + " triangle " + i);
                        checks++;
                    }
                    bool Prepare() => (bool)Call(game, "PrepareGardenBoard", false);
                    float time = 1f;
                    void Step(float dt = 1f / 60f) { time += dt; Set(game, "ui", time); }
                    Require(Prepare(), "Initial board must build"); Compare("ready");
                    int count = actual.currentVertCount;
                    for (int i = 0; i < 180; i++) { Step(); Require(!Prepare(), "Static board was invalidated at frame " + i); }
                    Require(count == 28836, "Full board must keep all 3600 two-layer bubbles and rim");
                    foreach (var size in new[] { new Vector2Int(540, 960), new Vector2Int(768, 1024), new Vector2Int(390, 844) }) {
                        ScreenLayout.Configure(size.x, size.y, new Rect(0, 0, 1, 1));
                        Require(Prepare(), "Viewport must invalidate"); Compare("viewport " + size);
                    }
                    ScreenLayout.Configure(390, 844, new Rect(0, .02f, 1, .94f));
                    Require(Prepare(), "Safe area must invalidate"); Compare("safe area");
                    Set(game, "selection", 2); Set(game, "aim", new Vector2(270, 300));
                    Require(Prepare(), "Aim must invalidate"); Compare("aim enter");
                    Step(); Set(game, "aim", new Vector2(320, 450)); Require(Prepare(), "Aim move/pulse must invalidate"); Compare("aim move");
                    Set(game, "selection", 0); Require(Prepare(), "Aim cancel must restore original colors"); Compare("aim cancel");
                    Require(!Prepare(), "Cancelled aim must settle");
                    battle.Alive[120] = false; Require(Prepare(), "Removal must invalidate"); Compare("remove");
                    battle.Alive[120] = true; Step(); Require(Prepare(), "Reappearance must invalidate"); Compare("reappear");
                    // Move one grain diagonally, then let the falling and squash frames settle.
                    var positions = (int[])Field(battle, "position"); var ys = (float[])Field(battle, "y");
                    positions[300] += 61; ys[300] += battle.PitchY;
                    for (int i = 0; i < 70; i++) { Step(); bool dirty = Prepare(); if (dirty) Compare("fall " + i); }
                    Require(!Prepare(), "Landing squash must finish and settle");
                    Property(battle, "Offset", battle.Offset + 1f);
                    Step(); Require(Prepare(), "Formation advance must invalidate"); Compare("advance");
                    Property(battle, "State", Phase.Intro);
                    foreach (float age in new[] { .1f, .7f, 1.4f, 2.5f }) {
                        Property(battle, "IntroTime", age); Step(); Require(Prepare(), "Intro must animate"); Compare("intro " + age);
                    }
                    Load("sector01"); Step(); Require(Prepare(), "Same-size new battle must invalidate"); Compare("same size restart");
                    Load("training02"); Step(); Require(Prepare(), "Smaller level must invalidate"); Compare("teaching level");
                    Load("sector02"); Step(); Require(Prepare(), "Streamed level must invalidate"); Compare("stream level");
                    positions = (int[])Field(battle, "position"); ys = (float[])Field(battle, "y");
                    int streamed = battle.Level.cells.Length;
                    positions[streamed] = positions[0]; positions[0] = -1;
                    ys[streamed] = ys[0]; battle.Alive[streamed] = true; battle.Alive[0] = false;
                    Step(); Require(Prepare(), "Stream replacement must invalidate"); Compare("stream replacement");
                    for (int i = 0; i < 100; i++) { Step(); if (Prepare()) Compare("stream fall " + i); }
                    Require(!Prepare(), "Stream replacement must settle");
                    Property(game, "InHome", true); Call(game, "RefreshGardenBoard");
                    Require(!board.gameObject.activeSelf, "Board must hide on home/codex");
                    Property(game, "InHome", false); Call(game, "RefreshGardenBoard");
                    Require(board.gameObject.activeSelf, "Board must reappear on battle");
                    Debug.Log($"GARDEN_BOARD_CHECKS_PASSED: {checks} exact mesh comparisons; 180 static frames without invalidation; full resolution, two layers, viewport/safe area, aim, removal, falling/squash, advance, intro, restart, stream, home lifecycle");
                } finally { UnityEngine.Object.DestroyImmediate(em); UnityEngine.Object.DestroyImmediate(am); }
            } finally {
                foreach (var b in battles) b.Dispose();
                PlushLayout.Enabled = plush; UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
