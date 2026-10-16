using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    // Plan T1.5/T2.4/T2.5 named legacy sets, against the actual game classes.
    // Fixtures follow Reverse/probe scripts, never the old RecoveredRules class.
    public static class LegacyModelStage
    {
        [Serializable] private sealed class MathCase
        {
            public string name;
            public int tier, missile, combo, streak;
            public float interval, power, weak, strong, low, high, cx, cy, rx, ry, x, y;
            public double expected; public string expectedDoubleBits;
        }
        [Serializable] private sealed class Quality { public int epicPity, legendPity; public int[] outcomes; }
        [Serializable] private sealed class Module { public int id, quality; public float scale; public string[] names; public int[] expected; }
        [Serializable] private sealed class Ship { public int color,ammo,initialAmmo,tier,id; public float cooldown,lastFired; }
        [Serializable] private sealed class Merge { public Ship[] before; public Ship after; }
        [Serializable] private sealed class StateValues { public int remaining,score,combo,accepted,state,revived,slots,missiles; }
        [Serializable] private sealed class State { public string name; public StateValues actual; }
        [Serializable] private sealed class Input { public MathCase[] math; public Quality[] quality; public Module[] modules; public Merge[] merges; public State[] states; }
        [Serializable] private sealed class Report
        {
            public bool passed; public string error,unityVersion;
            public int arithmeticCases,qualityCases,moduleCases,mergeCases,stateCases,checks,decimalParserDifferences;
            public List<string> failures=new List<string>();
        }
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static Report report;
        private static Battle New(string level)=>new Battle(JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/"+level).text),null,true);
        private static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
        private static void Set(Battle b,string name,object value)
        {
            PropertyInfo p=typeof(Battle).GetProperty(name,Flags);
            if(p!=null)p.SetValue(b,value);else typeof(Battle).GetField(name,Flags).SetValue(b,value);
        }
        private static int Bits(float v)=>BitConverter.SingleToInt32Bits(v);
        private static float Single(float v)=>BitConverter.Int32BitsToSingle(Bits(v));
        private static void Equal(string name,object actual,object expected)
        {
            report.checks++;if(!Equals(actual,expected))report.failures.Add(name+": "+actual+" != "+expected);
        }
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("CLI only");
            string[] args=Environment.GetCommandLineArgs();
            string Arg(string name){int i=Array.IndexOf(args,name);if(i<0||i+1==args.Length)throw new ArgumentException(name);return Path.GetFullPath(args[i+1]);}
            string output=Arg("--flightbeat-legacy-report");if(File.Exists(output))throw new IOException("Fresh output required");Directory.CreateDirectory(Path.GetDirectoryName(output));
            report=new Report{unityVersion=Application.unityVersion};
            try
            {
                Input input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-legacy-input")));
                using(var b=New("training01"))
                    foreach(var c in input.math)
                    {
                        var fighter=new Fighter(0,0,0){Tier=c.tier};double actual;
                        switch(c.name)
                        {
                            case "Fighter.Volley":actual=fighter.Volley;break;
                            case "Fighter.Interval":actual=fighter.Interval(c.interval);break;
                            case "Fighter.DPS":actual=fighter.DPS(c.interval);break;
                            case "Shot.Duration":actual=new Shot{Tier=c.tier,Rocket=c.missile!=0}.Duration;break;
                            case "Pressure.FirepowerFactor":actual=new PressureSettings{weakFactor=c.weak,strongFactor=c.strong,weakFirepower=c.low,strongFirepower=c.high}.FirepowerFactor(c.power);break;
                            case "Battle.ChainStacks":Set(b,"Combo",c.combo);actual=b.ChainStacks;break;
                            case "SuperStrike.Contains":actual=new SuperStrike{X=c.cx,Y=c.cy,RadiusX=c.rx,RadiusY=c.ry}.Contains(c.x,c.y)?1:0;break;
                            case "FleetCatalog.StreakPercent":actual=FleetCatalog.StreakPercent(c.streak);break;
                            default:throw new InvalidDataException(c.name);
                        }
                        // Preserve original binary64 oracle bits across JsonUtility decimal parsing.
                        long expectedBits=unchecked((long)Convert.ToUInt64(c.expectedDoubleBits,16));
                        if(BitConverter.DoubleToInt64Bits(c.expected)!=expectedBits)report.decimalParserDifferences++;
                        Equal(c.name+" binary64",BitConverter.DoubleToInt64Bits(actual),expectedBits);report.arithmeticCases++;
                    }
                int roll=0,calls=0;
                var economy=new FleetEconomy(new FleetProfile(),()=>0L,_=>{},max=>{Equal("quality RNG bound",max,100);calls++;return roll;});
                foreach(var c in input.quality)
                    for(roll=0;roll<100;roll++)
                    {
                        int before=calls;
                        int actual=(int)Call(economy,"RollQuality",new FleetProfile{epicPity=c.epicPity,legendPity=c.legendPity});
                        Equal("quality",actual,c.outcomes[roll]);Equal("quality consumes RNG even at pity",calls,before+1);report.qualityCases++;
                    }
                foreach(var c in input.modules)
                {
                    ModuleSpec spec=FleetCatalog.Modules[c.id];var loadout=new FleetLoadout();
                    spec.apply(loadout,Single(spec.values[c.quality]*c.scale),Single((spec.second==null?0f:spec.second[c.quality])*c.scale));
                    for(int i=0;i<c.names.Length;i++)
                    {
                        object value=typeof(FleetLoadout).GetField(c.names[i]).GetValue(loadout);
                        Equal("module "+c.id+" "+c.names[i],value is float f?Bits(f):Convert.ToInt32(value),c.expected[i]);
                    }
                    report.moduleCases++;
                }
                foreach(var c in input.merges)
                    using(var b=New("training01"))
                    {
                        foreach(var s in c.before)b.Fleet.Add(new Fighter(s.color,s.ammo,s.id){InitialAmmo=s.initialAmmo,Tier=s.tier,Cooldown=s.cooldown,LastFired=s.lastFired});
                        Call(b,"Merge",0);Equal("merge fleet count",b.Fleet.Count,1);
                        Fighter a=b.Fleet[0];Ship e=c.after;
                        Equal("merge color",a.Color,e.color);Equal("merge ammo",a.Ammo,e.ammo);Equal("merge initial ammo",a.InitialAmmo,e.initialAmmo);
                        Equal("merge tier",a.Tier,e.tier);Equal("merge dominant id",a.Id,e.id);
                        Equal("merge cooldown bits",Bits(a.Cooldown),Bits(e.cooldown));Equal("merge last-fired bits",Bits(a.LastFired),Bits(e.lastFired));report.mergeCases++;
                    }
                var expected=new Dictionary<string,StateValues>();foreach(var x in input.states)expected.Add(x.name,x.actual);
                StateValues Record(string name){report.stateCases++;return expected[name];}
                var shot=new Shot{PierceA=0,PierceB=0,PierceC=0}; // original malloc+zero, not ctor defaults.
                for(int tier=0;tier<4;tier++)foreach(float time in new[]{0f,1.39f,1.4f,1.41f})
                    using(var b=New("sector01"))
                    {
                        Set(b,"Combo",17);Set(b,"lastHit",0f);Set(b,"Time",time);shot.Tier=tier;
                        Call(b,"ResolveTarget",shot,Target(b));
                        string label="hit "+tier+" @ "+time.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
                        var e=Record(label);Equal(label+" remaining",b.Remaining,e.remaining);Equal(label+" score",b.Score,e.score);Equal(label+" combo",b.Combo,e.combo);
                    }
                foreach(string guard in new[]{"ready","trainingHold","trainingSafe","invulnerable","push","normal"})
                    using(var b=New("sector01"))
                    {
                        Set(b,"State",guard=="ready"?Phase.Ready:Phase.Playing);Set(b,"Time",10f);
                        if(guard=="trainingHold")b.TrainingHold=true;if(guard=="trainingSafe")b.TrainingSafe=true;
                        if(guard=="invulnerable")Set(b,"hurtUntil",11f);
                        if(guard=="push"){Set(b,"pushAge",0f);Set(b,"pushDuration",1f);}
                        var e=Record("breach guard "+guard);Equal("breach accepted",b.Breach()?1:0,e.accepted);Equal("breach phase",(int)b.State,e.state);
                        if(guard!="normal")continue;
                        e=Record("revive");Equal("revive accepted",b.Revive()?1:0,e.accepted);Equal("revive phase",(int)b.State,e.state);Equal("revived",b.Revived?1:0,e.revived);
                        Equal("revive slots",b.MaximumSlots,e.slots);Equal("revive missiles",b.RescueMissiles,e.missiles);
                        Set(b,"Time",100f);Set(b,"pushAge",2f);Set(b,"pushDuration",0f);
                        e=Record("second breach");Equal("second breach accepted",b.Breach()?1:0,e.accepted);Equal("second breach phase",(int)b.State,e.state);
                    }
                using(var b=New("sector01")){Set(b,"State",Phase.ReviveOffer);var e=Record("decline revive");Equal("decline",b.DeclineRevive()?1:0,e.accepted);Equal("decline phase",(int)b.State,e.state);}
                using(var b=New("sector01")){Set(b,"State",Phase.Playing);Set(b,"Remaining",1);shot.TargetIndex=Target(b);Call(b,"Resolve",shot);var e=Record("last hit wins");Equal("last hit remaining",b.Remaining,e.remaining);Equal("last hit phase",(int)b.State,e.state);}
                Equal("legacy total",report.arithmeticCases+report.qualityCases,20554);Equal("modules",report.moduleCases,170);Equal("merges",report.mergeCases,64);Equal("states",report.stateCases,26);
                report.passed=report.failures.Count==0;if(!report.passed)throw new Exception("Legacy source mismatch: "+report.failures[0]);
            }
            catch(Exception e){report.error=e.ToString();throw;}
            finally{File.WriteAllText(output,JsonUtility.ToJson(report,true));}
        }
        private static int Target(Battle b)
        {
            int[] positions=(int[])typeof(Battle).GetField("position",Flags).GetValue(b);
            for(int i=0;i<b.Alive.Length;i++)if(b.Alive[i]&&positions[i]>=0)return i;
            throw new InvalidOperationException("No on-board target");
        }
    }
}
