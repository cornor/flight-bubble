using System;
using System.Collections;
using System.IO;
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
    // Called by the CLI Play-mode UI suite with --garden-fx-checks. Explicit visual
    // fixtures, not gameplay evidence. The frozen model replays are run separately.
    public static class BubbleFxChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Set(object o, string key, object value) => (o.GetType().GetField(key, Flags) ?? o.GetType().GetField("<" + key + ">k__BackingField", Flags)).SetValue(o, value);
        static object Get(object o, string key) => o.GetType().GetField(key, Flags).GetValue(o);
        static void Record(CombatFeedback f, string list, string type, params object[] fields) {
            var obj = Activator.CreateInstance(typeof(CombatFeedback).GetNestedType(type, BindingFlags.NonPublic));
            for (int i = 0; i < fields.Length; i += 2) Set(obj, (string)fields[i], fields[i + 1]);
            ((IList)Get(f, list)).Add(obj);
        }
        // Focused edit-mode check: real model impacts, pooled shots and actual UI mesh dimensions.
        // Does not build a player or read/write the player's saved profile.
        public static void RunAttackPops()
        {
            var root = new GameObject("Attack pop fixture", typeof(RectTransform));
            root.SetActive(false);
            var surface = root.AddComponent<PixelSurface>();
            bool oldPlush = PlushLayout.Enabled;
            void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
            try
            {
                typeof(PixelSurface).GetMethod("Awake", Flags).Invoke(surface, null);
                PlushLayout.Enabled = true;
                foreach (var viewport in new[] { new Vector2Int(540, 960), new Vector2Int(390, 844) })
                foreach (int tier in new[] { 0, 1, 2, 3 })
                using (var battle = new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/sector01").text), null))
                using (var mesh = new VertexHelper())
                {
                    ScreenLayout.Configure(viewport.x, viewport.y, new Rect(0, 0, 1, 1));
                    var pool = new PixelEffectPool(root.transform, (RectTransform)root.transform) { Enabled = false };
                    var feedback = new CombatFeedback(battle, pool, CombatPalette.ForLevel(battle.Level));
                    var impactAt = new Vector2(123f, 333f);
                    feedback.GardenEnemyAt = _ => impactAt;
                    Set(surface, "mesh", mesh); surface.MapLayout = true;
                    // Repeated renders must install only one impact subscription.
                    feedback.DrawGardenFeedback(surface, _ => Color.white);
                    feedback.DrawGardenFeedback(surface, _ => Color.white);
                    var pops = (IList)Get(feedback, "gardenAttackPops");
                    var blasts = (IList)Get(feedback, "blasts");
                    var resolve = typeof(Battle).GetMethod("Resolve", Flags);
                    void Impact(bool rocket = false, int tierOverride = -1)
                    {
                        int target = Array.FindIndex(battle.Alive, value => value);
                        Require(target >= 0, "Fixture needs a live target");
                        var shot = new Shot { Tier = tierOverride < 0 ? tier : tierOverride, Color = battle.ColorAt(target), TargetIndex = target,
                            TargetX = battle.EnemyX(target), TargetY = battle.EnemyY(target), Rocket = rocket,
                            SplashTargets = rocket ? new[] { target } : null };
                        if (rocket)
                        {
                            shot.TargetX = 270f + (impactAt.x - 270f) / ScreenLayout.EnemyScaleX;
                            shot.TargetY = 562f + (impactAt.y - 562f) / ScreenLayout.EnemyScaleY;
                        }
                        resolve.Invoke(battle, new object[] { shot });
                        shot.Clear(); // Same pooling boundary as gameplay; effects must retain copied values.
                    }
                    int Draw(string method)
                    {
                        mesh.Clear();
                        typeof(CombatFeedback).GetMethod(method, Flags).Invoke(feedback, new object[] { surface });
                        Require(surface.MapLayout, "Impact drawing must preserve layout mapping");
                        return mesh.currentVertCount;
                    }
                    Rect Bounds()
                    {
                        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                        var vertex = new UIVertex();
                        for (int i = 0; i < mesh.currentVertCount; i++)
                        {
                            mesh.PopulateUIVertex(ref vertex, i);
                            var point = new Vector2(vertex.position.x, ScreenLayout.Height - vertex.position.y);
                            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                        }
                        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    }
                    Impact();
                    if (tier == 3)
                    {
                        Require(pops.Count == 0 && Draw("DrawGardenAttackPops") == 0, "MAX main shot must not add a large burst");
                        Impact(true);
                        Require(blasts.Count == 1, "MAX retains its original impact feedback record");
                    }
                    Require(pops.Count == 1, "An isolated eligible impact must add one pop, tier " + tier);
                    int remaining = battle.Remaining, score = battle.Score;
                    float modelTime = battle.Time;
                    Require(Draw("DrawGardenAttackPops") == 4, "Impact emits one authored sprite");
                    var bounds = Bounds();
                    float expected = new[] { 25f, 40f, 60f, 120f }[tier];
                    Require(Mathf.Abs(bounds.width - expected) < .002f && Mathf.Abs(bounds.height - expected) < .002f,
                        "Wrong rendered impact size at " + viewport + ", tier " + tier);
                    Require((bounds.center - new Vector2(123f, PlushLayout.Y(333f))).sqrMagnitude < .001f,
                        "Impact must align with the displayed target");
                    Require(Draw("DrawGardenAttackPops") == 4 && Bounds() == bounds, "Paused redraw must keep the same effect");
                    Require(battle.Remaining == remaining && battle.Score == score && battle.Time == modelTime,
                        "Effect rendering must not change gameplay");
                    Set(feedback, "time", .06f);
                    impactAt.x += 12f; Impact(tier == 3);
                    Require(pops.Count == 1 && Draw("DrawGardenAttackPops") == 4,
                        "Adjacent impacts within 0.12 seconds must share one clip");
                    Require((float)Get(pops[0], "born") == 0f && (Bounds().center - bounds.center).sqrMagnitude < .001f,
                        "Merged hits must not restart or move the clip");
                    impactAt.x = 350f; Impact(tier == 3);
                    Require(pops.Count == 2 && Draw("DrawGardenAttackPops") == 8,
                        "Distant simultaneous impacts must retain their own feedback");
                    impactAt.x = 123f; Set(feedback, "time", .121f); Impact(tier == 3);
                    Require(pops.Count == 3, "The original location must admit another burst after 0.12 seconds");
                    Set(feedback, "time", .4f);
                    Require(Draw("DrawGardenAttackPops") == 8 && pops.Count == 2,
                        "The first clip must expire on its original schedule despite merged hits");
                    Set(feedback, "time", 1f);
                    Require(Draw("DrawGardenAttackPops") == 0, "All clips must expire");
                    remaining = battle.Remaining; score = battle.Score;
                    for (int i = 0; i < 40; i++) Impact(tier == 3);
                    Require(pops.Count == 1 && Draw("DrawGardenAttackPops") == 4, "Forty clustered hits must share one burst");
                    Require(battle.Remaining == remaining - 40 && battle.Score == score + 40 * (tier * 10 + 10),
                        "Visual merging must still resolve all forty gameplay hits");
                    Set(feedback, "time", 1.6f);
                    for (int i = 0; i < 40; i++)
                    {
                        impactAt = new Vector2(40f + i % 8 * 64f, 180f + i / 8 * 100f);
                        Impact(tier == 3);
                    }
                    int limit = tier == 3 ? 12 : 32;
                    Require(pops.Count == limit && Draw("DrawGardenAttackPops") == limit * 4,
                        "Dispersed impacts must retain the visual budget, tier " + tier);
                    Set(feedback, "time", 2.2f); Draw("DrawGardenAttackPops");
                    impactAt = new Vector2(123f, 333f); Impact(false, 0);
                    Set(feedback, "time", 2.26f); Impact(true, 3); Impact(false, 1);
                    Require(pops.Count == 1 && Draw("DrawGardenAttackPops") == 4 && Mathf.Abs(Bounds().width - 120f) < .002f,
                        "Mixed tiers must retain the largest clip when a MAX splash joins a small burst");
                    Require((float)Get(pops[0], "born") == 2.2f, "Promoting a burst must preserve its original age");
                    impactAt.x = 450f; Impact(false, 3);
                    Require(pops.Count == 1, "Even an isolated MAX main shot must not add a duplicate large burst");
                    Set(feedback, "time", 3f);
                    Require(Draw("DrawGardenAttackPops") == 0 && pops.Count == 0, "Promoted effects must expire");
                    pool.Dispose();
                }
                Debug.Log("GARDEN_ATTACK_POP_CHECKS_PASSED: four sizes, two viewports, MAX splash only, spatial merging, 0.12s admission, mixed-tier priority, pooled shots, pause/expiry, visual budgets, unchanged gameplay");
            }
            finally
            {
                PlushLayout.Enabled = oldPlush;
                var material = (Material)Get(surface, "ownedMaterial");
                Set(surface, "ownedMaterial", null);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        public static void Run(PixelWingGame game, Action<bool,string> check, Action<string,int> capture)
        {
            check(game.StartSortie(3), "FX fixtures initialize a separate formal battle");
            Set(game.Battle, "State", Phase.Playing); Set(game, "applicationSuspended", true);
            var feedback = game.feedback; var hud = (PixelSurface)Get(game, "hud");
            var oldPaint = hud.Paint; bool oldMap = hud.MapLayout;
            Func<int,Color> color = i => { var p=game.Battle.Level.palette; return new Color32((byte)p[i*3],(byte)p[i*3+1],(byte)p[i*3+2],255); };
            void Clear() {
                foreach (var list in new[] { "marks", "fusions", "waves", "kamikazes", "flights", "transitions", "blasts", "purges", "sparks", "weaponFx", "beams", "gardenPops", "gardenAttackPops" }) ((IList)Get(feedback,list)).Clear();
                game.Battle.Fleet.Clear(); Set(game.Battle, "PendingStrike", null); Set(feedback, "lastStrike", null);
            }
            int Draw(Action<PixelSurface> draw) {
                using (var mesh = new VertexHelper()) {
                    Set(hud, "mesh", mesh); hud.MapLayout = true; draw(hud);
                    if (!hud.MapLayout) throw new Exception("FX failed to restore caller layout flag");
                    var v = new UIVertex();
                    for (int i = 0; i < mesh.currentVertCount; i++) {
                        mesh.PopulateUIVertex(ref v, i);
                        if (float.IsNaN(v.position.x) || float.IsNaN(v.position.y) || v.uv0.x < 0 || v.uv0.x > 1 || v.uv0.y < 0 || v.uv0.y > 1)
                            throw new Exception("Invalid FX vertex/atlas UV");
                    }
                    return mesh.currentVertCount;
                }
            }
            try {
                
                Clear();
                Record(feedback,"marks","Mark","at",new Vector2(270,300),"age",.1f,"color",0,"fusion",false);
                check(Draw(s=>feedback.DrawGardenFeedback(s,color))==0,"Ordinary hit record draws zero vertices (no ring and no hit-pop)");
                Clear();
                foreach (string id in new[] { "repel-gust", "dash-trail", "impact-dust", "merge-burst", "beam", "beam-tip", "water-can", "water-splash", "water-aim", "revive-wave" }) {
                    var clip = GardenFx.Get(id);
                    for (int frame = 0; frame < clip.frames.Length; frame++) {
                        int f = frame;
                        check(Draw(s=>GardenFx.Draw(s,id,new Vector2(270,300),32,32,Color.white,f))==4,"Atlas frame available: "+id+"/"+frame);
                    }
                }
                check(Draw(s=>GardenFx.Burst(s,"merge-burst",new Vector2(270,300),96,-.001f,.5f))==0,"Burst waits for its existing event age");
                check(Draw(s=>GardenFx.Burst(s,"impact-dust",new Vector2(270,300),80,.5f,.5f))==0,"Burst emits nothing at expiry");
                int objects = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                int materials = Resources.FindObjectsOfTypeAll<Material>().Length;
                for (int i=0;i<100;i++) Draw(s=>GardenFx.Beam(s,new Vector2(270,628),new Vector2(150,180),16,color(0),i/60f));
                check(objects==UnityEngine.Object.FindObjectsOfType<GameObject>().Length && materials==Resources.FindObjectsOfTypeAll<Material>().Length,
                    "Repeated effects allocate no GameObjects or materials");
                foreach (int height in new[] { 1170 }) {
                    Set(game,"evidenceViewport",new Vector2Int(540,height));
                    game.GetType().GetMethod("ApplyDisplay",Flags).Invoke(game,new object[]{540,height});
                    game.gardenPage.ApplyLayout(); game.RefreshGardenUi(game.gardenPage);
                    foreach (string kind in new[] { "merge", "charge", "impact", "repel", "revive", "beam", "water-can", "water-splash", "rocket", "purge" }) {
                        Clear();
                        switch(kind) {
                            case "merge": Record(feedback,"fusions","Fusion","at",new Vector2(270,620),"age",.42f,"tier",2,"color",0); break;
                            case "charge": case "impact": Record(feedback,"kamikazes","Kamikaze","from",new Vector2(270,628),"x",270f,"delay",.4f,"age",kind=="charge"?.32f:.6f,"color",0,"tier",1,"exploded",kind=="impact"); break;
                            case "repel": case "revive": Record(feedback,"waves","Wave","age",.18f,"duration",kind=="repel"?.7f:1.35f,"kind",kind=="repel"?RepelKind.Empty:RepelKind.Revive); break;
                            case "beam": game.Battle.Fleet.Add(new Fighter(0,72,9001) { Tier=2, BeamLife=1, BeamX=270, BeamY=400 }); break;
                            case "rocket":
                                Record(feedback,"blasts","Blast","at",new Vector2(270,300),"age",.15f);
                                Record(feedback,"gardenAttackPops","GardenAttackPop","at",new Vector2(270,300),"born",feedback.Time-.15f,"size",120f);
                                break;
                            case "purge": Record(feedback,"purges","PurgeFx","front",350f,"age",.4f,"count",35); break;
                            case "water-can": Set(game.Battle,"PendingStrike",new SuperStrike {X=270,Y=400,RadiusX=120,RadiusY=120,Age=.7f}); break;
                            case "water-splash": Set(feedback,"lastStrike",new SuperStrike {X=270,Y=400,RadiusX=120,RadiusY=120}); Set(feedback,"superAge",.3f); break;
                        }
                        if (kind == "beam") typeof(CombatFeedback).GetMethod("Launch",Flags).Invoke(feedback,new object[]{game.Battle.Fleet[0],0});
                        int remaining=game.Battle.Remaining,score=game.Battle.Score;float time=game.Battle.Time;
                        int vertices = Draw(s=>feedback.DrawGardenFeedback(s,color));
                        check(vertices>0,"Feedback route emits art: "+kind+" at "+height);
                        if (kind == "rocket") check(vertices == 4, "MAX's legacy blast and Garden impact draw only one clip");
                        check(game.Battle.Remaining==remaining&&game.Battle.Score==score&&game.Battle.Time==time,"Drawing preserves model state: "+kind);
                        // Isolate each seeded effect through the real camera, against the same field background.
                        hud.Paint=s=>{s.MapLayout=true;feedback.DrawGardenFeedback(s,color);}; hud.Redraw(); capture("fx-"+kind+"-bubble-"+height,height);
                        hud.Paint=s=>{s.MapLayout=true;feedback.DrawTransitions(s);feedback.DrawTargetsAndShots(s);feedback.DrawAccents(s);};
                        hud.Redraw(); capture("fx-"+kind+"-source-"+height,height);
                        hud.Paint=oldPaint;
                        check(!UnityEditor.ShaderUtil.ShaderHasError(Resources.Load<Shader>("GardenR1/GardenSurface")), "Garden shader compiled without errors");
                    }
                }
            } finally { hud.Paint=oldPaint; hud.MapLayout=oldMap; Clear(); hud.Redraw(); }
        }
    }
}
