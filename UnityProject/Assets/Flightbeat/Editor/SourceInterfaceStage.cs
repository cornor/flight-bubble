#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class SourceInterfaceStage
    {
        [Serializable] private sealed class Change { public string path,kind,text; public int bits; public int[] values; }
        [Serializable] private sealed class Check { public string name,kind,text; public int index,arg,bits; public bool method; public int[] values; }
        [Serializable] private sealed class Case { public string name; public bool hasBattle; public int lesson,bombRadiusBits; public int[] items; public Change[] changes; public Check[] expected; }
        [Serializable] private sealed class ConfigField { public string name,text; }
        [Serializable] private sealed class Input { public string profileEncoded,levelJson,pressureJson; public int screenWidth,screenHeight; public Case[] cases; public ConfigField[] config; }
        [Serializable] private sealed class Failure { public string path,expected,actual; }
        [Serializable] private sealed class Report { public bool passed; public string error; public int cases,checks,values,configFields; public List<Failure> failures=new List<Failure>(); }
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private static FieldInfo Field(Type t,string n)=>t.GetField(n,Flags)??t.GetField("<"+n+">k__BackingField",Flags)??throw new MissingFieldException(t.FullName,n);
        private static void Set(object t,string n,object value)=>Field(t.GetType(),n).SetValue(t,value);
        private static int Bits(object x)=>x is float f?BitConverter.SingleToInt32Bits(f):x is bool b?(b?1:0):Convert.ToInt32(x);
        private static string Arg(string n){var a=Environment.GetCommandLineArgs();int at=Array.IndexOf(a,n);if(at<0||at+1==a.Length)throw new ArgumentException(n);return Path.GetFullPath(a[at+1]);}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("CLI only");
            string output=Arg("--flightbeat-interfaces-report");if(File.Exists(output))throw new IOException("Fresh report required");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var data=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-interfaces-input")));
            var report=new Report();var owner=new GameObject("Original standalone interface fixture");owner.SetActive(false);
            Battle battle=null;PixelWingGame game=null;
            try
            {
                game=owner.AddComponent<PixelWingGame>(); // initializer only; no Awake/PlayerLoop
                var level=JsonUtility.FromJson<LevelData>(data.levelJson);
                battle=new Battle(level,JsonUtility.FromJson<PressureSettings>(data.pressureJson),true);
                int saves=0;
                var economy=new FleetEconomy(FleetSaveStore.Decode(data.profileEncoded),()=>1800000000L,_=>saves++,_=>throw new Exception("Undeclared random input"));
                Set(game,"Economy",economy);
                PixelWingGame.FrameInputOverride=(id,arg)=>id==50264?data.screenWidth:id==50265?data.screenHeight:throw new Exception("Undeclared engine input "+id);
                int initialSaves=saves;
                var finale=new Finale();
                foreach(var c in data.cases)
                {
                    foreach(var x in c.changes)
                    {
                        string[] parts=x.path.Split('/');object target=parts[0]=="game"?(object)game:economy.Data;
                        var field=Field(target.GetType(),parts[1]);object value;
                        if(x.kind=="string")value=x.text;
                        else if(x.kind=="long")value=Int64.Parse(x.text);
                        else if(x.kind=="float")value=BitConverter.Int32BitsToSingle(x.bits);
                        else if(x.kind=="bool")value=x.bits!=0;
                        else if(x.kind=="Vector2Int")value=new Vector2Int(x.values[0],x.values[1]);
                        else if(x.kind=="Vector2")value=new Vector2(BitConverter.Int32BitsToSingle(x.values[0]),BitConverter.Int32BitsToSingle(x.values[1]));
                        else if(x.kind=="Finale")value=x.bits!=0?finale:null;
                        else value=field.FieldType.IsEnum?Enum.ToObject(field.FieldType,x.bits):(object)x.bits;
                        field.SetValue(target,value);
                    }
                    level.lesson=c.lesson;battle.Loadout.bombRadius=BitConverter.Int32BitsToSingle(c.bombRadiusBits);
                    economy.Data.items=(int[])c.items.Clone();Set(game,"Battle",c.hasBattle?battle:null);
                    foreach(var check in c.expected)
                    {
                        report.checks++;
                        object actual=check.method?typeof(PixelWingGame).GetMethod(check.name,Flags).Invoke(game,check.name=="AimWorld"?null:new object[]{check.arg}):typeof(PixelWingGame).GetProperty(check.name,Flags).GetValue(game);
                        string label=c.name+"/f"+check.index+"/"+check.name;
                        if(check.kind=="Vector2")
                        {
                            var v=(Vector2)actual;Compare(report,label+"/x",check.values[0],Bits(v.x));Compare(report,label+"/y",check.values[1],Bits(v.y));
                        }
                        else if(check.kind=="string")Compare(report,label,check.text,actual as string);
                        else if(check.kind=="identity")Compare(report,label,check.bits,actual==null?0:ReferenceEquals(actual,finale)?1:-1);
                        else Compare(report,label,check.bits,Bits(actual));
                    }
                    if(saves!=initialSaves)throw new Exception("Getter unexpectedly saved profile");
                    report.cases++;
                }
                var config=ToutiaoReleaseConfig.Load();
                foreach(var c in data.config){Compare(report,"f54904/"+c.name,c.text,Field(typeof(ToutiaoReleaseConfig),c.name).GetValue(config) as string);report.configFields++;}
                report.passed=report.failures.Count==0;
            }
            catch(Exception e){report.error=e.ToString();report.passed=false;}
            finally
            {
                PixelWingGame.FrameInputOverride=null;if(game!=null)Set(game,"Battle",null);battle?.Dispose();UnityEngine.Object.DestroyImmediate(owner);
                File.WriteAllText(output,JsonUtility.ToJson(report,true));
            }
            if(!report.passed)throw new Exception("Original interface comparison failed: "+output);
        }
        private static void Compare(Report r,string path,object expected,object actual)
        {r.values++;if(!Equals(expected,actual))r.failures.Add(new Failure{path=path,expected=expected?.ToString()??"null",actual=actual?.ToString()??"null"});}
    }
}
#endif
