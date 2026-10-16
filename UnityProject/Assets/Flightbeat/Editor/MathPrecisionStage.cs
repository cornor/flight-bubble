using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Editor
{
    public static class MathPrecisionStage
    {
        [Serializable] private sealed class Group { public string name,path,outputPath,sha256,outputSha256; public int arity,inputWidth,outputWidth,count; }
        [Serializable] private sealed class Input { public Group[] files; }
        [Serializable] private sealed class Result { public string name; public int cases,failures,nanInputFailures; }
        [Serializable] private sealed class Failure { public string name,actual,expected; public int index; public bool nanInput; public string[] arguments; }
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string unityVersion,error;
            public long cases,failures;
            public double seconds;
            public List<Result> groups=new List<Result>();
            public List<Failure> examples=new List<Failure>();
        }
        private delegate int Reduction(float value,out double remainder);
        private static T Bind<T>(string name) where T:Delegate => (T)typeof(Battle).Assembly.GetType("Flightbeat.Model.OriginalMath").GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).CreateDelegate(typeof(T));
        private static ulong Bits(float v)=>unchecked((uint)BitConverter.SingleToInt32Bits(v));
        private static ulong Bits(double v)=>unchecked((ulong)BitConverter.DoubleToInt64Bits(v));
        private static string Sha(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        [Serializable] private sealed class NanRow { public string input,rawArrayCopy,intBitsLoad,byteLoad,arrayLoad,identity; }
        [Serializable] private sealed class NanReport { public string unityVersion; public List<NanRow> rows=new List<NanRow>(); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static float Identity(float value)=>value;
        public static void RunNaNProbe()
        {
            string[] args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--flightbeat-math-report");
            if(!Application.isBatchMode||flag<0||flag+1>=args.Length)throw new ArgumentException("Batch output required");
            string output=Path.GetFullPath(args[flag+1]);if(File.Exists(output))throw new IOException("Fresh report required");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=new NanReport{unityVersion=Application.unityVersion};
            foreach(uint bits in new[]{0u,0x80000000u,1u,0x3f800000u,0x7f800000u,0x7fc00000u,0x7fc00001u,0x7f800001u,0xff800001u})
            {
                byte[] bytes=BitConverter.GetBytes(bits),copied=new byte[4];float[] storage=new float[1];
                Buffer.BlockCopy(bytes,0,storage,0,4);Buffer.BlockCopy(storage,0,copied,0,4);
                report.rows.Add(new NanRow{input=bits.ToString("x8"),rawArrayCopy=BitConverter.ToUInt32(copied,0).ToString("x8"),
                    intBitsLoad=Bits(BitConverter.Int32BitsToSingle(unchecked((int)bits))).ToString("x8"),byteLoad=Bits(BitConverter.ToSingle(bytes,0)).ToString("x8"),
                    arrayLoad=Bits(storage[0]).ToString("x8"),identity=Bits(Identity(storage[0])).ToString("x8")});
            }
            File.WriteAllText(output,JsonUtility.ToJson(report,true));
        }
        public static void Run()
        {
            string[] args=Environment.GetCommandLineArgs();
            string Arg(string name){int i=Array.IndexOf(args,name);if(i<0||i+1>=args.Length)throw new ArgumentException(name);return Path.GetFullPath(args[i+1]);}
            if(!Application.isBatchMode)throw new InvalidOperationException("Batch mode required");
            string output=Arg("--flightbeat-math-report");if(File.Exists(output))throw new IOException("Fresh report required");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=new Report{unityVersion=Application.unityVersion};var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var input=JsonUtility.FromJson<Input>(File.ReadAllText(Arg("--flightbeat-math-input")));
                var single=new Dictionary<string,Func<float,float>>{{"exp",Bind<Func<float,float>>("Exp")},{"sin",Bind<Func<float,float>>("Sin")},{"cos",Bind<Func<float,float>>("Cos")},{"atanSingle",Bind<Func<float,float>>("AtanSingle")}};
                var pair=new Dictionary<string,Func<float,float,float>>{{"systemMin",Bind<Func<float,float,float>>("Min")},{"systemMax",Bind<Func<float,float,float>>("Max")},{"wasmMin",Bind<Func<float,float,float>>("WasmMin")},{"wasmMax",Bind<Func<float,float,float>>("WasmMax")},{"atan2Single",Bind<Func<float,float,float>>("Atan2Single")}};
                var atan=Bind<Func<double,double>>("Atan");var atan2=Bind<Func<double,double,double>>("Atan2");var reduce=Bind<Reduction>("ReduceSingle");var color=Bind<Func<float,float,float,float,uint>>("Color32Bits");
                foreach(Group group in input.files)
                {
                    byte[] data=File.ReadAllBytes(group.path),expected=File.ReadAllBytes(group.outputPath);
                    if(Sha(data)!=group.sha256||Sha(expected)!=group.outputSha256||data.Length!=group.count*group.arity*group.inputWidth||expected.Length!=group.count*group.outputWidth)throw new Exception("Invalid source evidence: "+group.name);
                    var result=new Result{name=group.name,cases=group.count};
                    for(int i=0;i<group.count;i++)
                    {
                        int at=i*group.arity*group.inputWidth;ulong actual;
                        if(group.inputWidth==8)actual=group.name=="atan"?Bits(atan(BitConverter.ToDouble(data,at))):Bits(atan2(BitConverter.ToDouble(data,at),BitConverter.ToDouble(data,at+8)));
                        else
                        {
                            float a=BitConverter.ToSingle(data,at),b=group.arity>=2?BitConverter.ToSingle(data,at+4):0f;
                            if(single.TryGetValue(group.name,out var one))actual=Bits(one(a));
                            else if(pair.TryGetValue(group.name,out var two))actual=Bits(two(a,b));
                            else if(group.name=="reduceN")actual=unchecked((uint)reduce(a,out _));
                            else if(group.name=="reduceY"){reduce(a,out double y);actual=Bits(y);}
                            else if(group.name=="color")actual=color(a,b,BitConverter.ToSingle(data,at+8),BitConverter.ToSingle(data,at+12));
                            else throw new Exception("Unknown source group "+group.name);
                        }
                        ulong wanted=group.outputWidth==4?BitConverter.ToUInt32(expected,i*4):BitConverter.ToUInt64(expected,i*8);
                        report.cases++;
                        if(actual!=wanted)
                        {
                            report.failures++;result.failures++;bool nan=false;var arguments=new string[group.arity];
                            for(int j=0;j<group.arity;j++)
                            {
                                int address=at+j*group.inputWidth;
                                if(group.inputWidth==4){nan|=float.IsNaN(BitConverter.ToSingle(data,address));arguments[j]=BitConverter.ToUInt32(data,address).ToString("x8");}
                                else{nan|=double.IsNaN(BitConverter.ToDouble(data,address));arguments[j]=BitConverter.ToUInt64(data,address).ToString("x16");}
                            }
                            if(nan)result.nanInputFailures++;
                            if(report.examples.Count<2000)report.examples.Add(new Failure{name=group.name,index=i,actual=actual.ToString("x16"),expected=wanted.ToString("x16"),nanInput=nan,arguments=arguments});
                        }
                    }
                    report.groups.Add(result);
                }
                report.passed=report.failures==0;if(!report.passed)throw new Exception("Mathematics bit mismatch; see report");
            }
            catch(Exception e){report.error=e.ToString();throw;}
            finally{report.seconds=watch.Elapsed.TotalSeconds;File.WriteAllText(output,JsonUtility.ToJson(report,true));}
        }
    }
}
