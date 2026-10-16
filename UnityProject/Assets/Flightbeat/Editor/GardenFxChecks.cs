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
    public static class GardenFxChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Set(object o, string key, object value) => (o.GetType().GetField(key, Flags) ?? o.GetType().GetField("<" + key + ">k__BackingField", Flags)).SetValue(o, value);
        static object Get(object o, string key) => o.GetType().GetField(key, Flags).GetValue(o);
        static void Record(CombatFeedback f, string list, string type, params object[] fields) {
            var obj = Activator.CreateInstance(typeof(CombatFeedback).GetNestedType(type, BindingFlags.NonPublic));
            for (int i = 0; i < fields.Length; i += 2) Set(obj, (string)fields[i], fields[i + 1]);
            ((IList)Get(f, list)).Add(obj);
        }
        public static void Run(PixelWingGame game, Action<bool,string> check, Action<string,int> capture)
        {
            check(game.StartSortie(3), "FX fixtures initialize a separate formal battle");
            Set(game.Battle, "State", Phase.Playing); Set(game, "applicationSuspended", true);
            var feedback = game.feedback; var hud = (PixelSurface)Get(game, "hud");
            var oldPaint = hud.Paint; bool oldMap = hud.MapLayout;
            Func<int,Color> color = i => i == 0 ? GardenSkin.Hex(0x2982F4) : GardenSkin.Hex(0xFF7E27);
            void Clear() {
                foreach (var list in new[] { "marks", "fusions", "waves", "kamikazes", "flights", "transitions" }) ((IList)Get(feedback,list)).Clear();
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
                check(GardenGeometryChecks.Run(hud)==5000,"5000 axis-aligned quads match original f54395 in every vertex bit at both viewports");
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
                foreach (int height in new[] { 960, 1170 }) {
                    Set(game,"evidenceViewport",new Vector2Int(540,height));
                    game.GetType().GetMethod("ApplyDisplay",Flags).Invoke(game,new object[]{540,height});
                    game.gardenPage.ApplyLayout(); game.RefreshGardenUi(game.gardenPage);
                    foreach (string kind in new[] { "merge", "charge", "impact", "repel", "revive", "beam", "water-can", "water-splash" }) {
                        Clear();
                        switch(kind) {
                            case "merge": Record(feedback,"fusions","Fusion","at",new Vector2(270,620),"age",.42f,"tier",2,"color",0); break;
                            case "charge": case "impact": Record(feedback,"kamikazes","Kamikaze","from",new Vector2(270,628),"x",270f,"delay",.4f,"age",kind=="charge"?.32f:.6f,"color",0,"tier",1,"exploded",kind=="impact"); break;
                            case "repel": case "revive": Record(feedback,"waves","Wave","age",.18f,"duration",kind=="repel"?.7f:1.35f,"kind",kind=="repel"?RepelKind.Empty:RepelKind.Revive); break;
                            case "beam": game.Battle.Fleet.Add(new Fighter(0,72,9001) { Tier=2, BeamLife=1, BeamX=270, BeamY=400 }); break;
                            case "water-can": Set(game.Battle,"PendingStrike",new SuperStrike {X=270,Y=400,RadiusX=120,RadiusY=120,Age=.7f}); break;
                            case "water-splash": Set(feedback,"lastStrike",new SuperStrike {X=270,Y=400,RadiusX=120,RadiusY=120}); Set(feedback,"superAge",.3f); break;
                        }
                        int remaining=game.Battle.Remaining,score=game.Battle.Score;float time=game.Battle.Time;
                        check(Draw(s=>feedback.DrawGardenFeedback(s,color))>0,"Feedback route emits art: "+kind+" at "+height);
                        check(game.Battle.Remaining==remaining&&game.Battle.Score==score&&game.Battle.Time==time,"Drawing preserves model state: "+kind);
                        // Keep the original HUD/plants, then render the same seeded records through the real camera.
                        hud.Paint=oldPaint; hud.Redraw(); capture("fx-"+kind+"-"+height,height);
                        check(!UnityEditor.ShaderUtil.ShaderHasError(Resources.Load<Shader>("GardenR1/GardenSurface")), "Garden shader compiled without errors");
                    }
                }
            } finally { hud.Paint=oldPaint; hud.MapLayout=oldMap; Clear(); hud.Redraw(); }
        }
    }
}
