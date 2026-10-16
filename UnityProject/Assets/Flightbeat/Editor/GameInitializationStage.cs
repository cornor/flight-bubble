#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Flightbeat.Game;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class GameInitializationStage
    {
        [Serializable] private sealed class Input { public Check[] checks; }
        [Serializable] private sealed class Check { public string path,kind,text; public int bits; public int[] values; }
        [Serializable] private sealed class Failure { public string path, expected, actual; }
        [Serializable] private sealed class Report { public bool passed; public int checks, values; public string error; public List<Failure> failures=new List<Failure>(); }
        private static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        private static string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);if(i<0||i+1==a.Length)throw new ArgumentException(name);return Path.GetFullPath(a[i+1]);}
        private static object Get(object game,string path)
        {
            object value=null;string[] names=path.Split('/');
            for(int i=0;i<names.Length;i++)
            {
                string n=names[i];if(i==0){value=n=="game"?game:(object)typeof(PixelWingGame);continue;}
                if(value==null)return null;
                if(Int32.TryParse(n,out int at)){value=((IList)value)[at];continue;}
                Type t=value as Type??value.GetType();FieldInfo f=t.GetField(n,Flags)??t.GetField("<"+n+">k__BackingField",Flags);
                if(f==null && t==typeof(Vector2Int) && (n=="x"||n=="y")){value=t.GetProperty(n).GetValue(value);continue;}
                if(f==null)throw new MissingFieldException(t.FullName,n);value=f.GetValue(value is Type?null:value);
            }
            return value;
        }
        private static int Bits(object value)=>value is float f?BitConverter.SingleToInt32Bits(f):value is bool b?(b?1:0):value is uint u?unchecked((int)u):Convert.ToInt32(value);
        public static void Run()
        {
            if(!Application.isBatchMode)throw new Exception("CLI only");
            string output=Arg("--flightbeat-initialization-report");if(File.Exists(output))throw new IOException("Fresh report required");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-initialization-input")));var report=new Report();
            var owner=new GameObject("Original constructor comparison");owner.SetActive(false);
            try
            {
                var game=owner.AddComponent<PixelWingGame>(); // real C# initializer, no Awake
                foreach(var c in input.checks)
                {
                    report.checks++;report.values++;
                    try
                    {
                        object actual=Get(game,c.path);bool pass;
                        if(c.kind=="null")pass=actual==null;
                        else if(c.kind=="object")pass=actual!=null;
                        else if(c.kind=="string"||c.kind=="long"||c.kind=="ulong")pass=actual?.ToString()==c.text;
                        else if(c.kind=="count")pass=actual!=null&&(actual is ICollection list?list.Count:(int)actual.GetType().GetProperty("Count").GetValue(actual))==c.bits;
                        else if(c.kind.EndsWith("[]",StringComparison.Ordinal))
                        {
                            var values=new List<int>();if(actual is IEnumerable array)foreach(var item in array)values.Add(Bits(item));
                            pass=actual!=null&&values.Count==c.values.Length;
                            for(int i=0;i<Math.Min(values.Count,c.values.Length);i++){report.values++;if(values[i]!=c.values[i]){pass=false;report.failures.Add(new Failure{path=c.path+"/"+i,expected=c.values[i].ToString(),actual=values[i].ToString()});}}
                        }
                        else pass=actual!=null&&Bits(actual)==c.bits;
                        if(!pass)report.failures.Add(new Failure{path=c.path,expected=c.kind+":"+(c.text??c.bits.ToString()),actual=actual?.ToString()??"null"});
                    }
                    catch(Exception e){report.failures.Add(new Failure{path=c.path,expected=c.kind,actual=e.ToString()});}
                }
                report.passed=report.failures.Count==0;
            }
            catch(Exception e){report.error=e.ToString();}
            finally{UnityEngine.Object.DestroyImmediate(owner);File.WriteAllText(output,JsonUtility.ToJson(report,true));}
            if(!report.passed)throw new Exception("Game initialization differs: "+output);
        }
    }
}
#endif
