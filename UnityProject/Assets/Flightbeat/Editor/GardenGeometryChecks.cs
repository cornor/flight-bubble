using System;
using System.Reflection;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Require bit-identical vertex data to the generic f54395 path, including UV,
    // Color32, normals/tangents and both viewport layouts. No tolerance or fitted constants.
    public static class GardenGeometryChecks
    {
        public static int Run(PixelSurface surface)
        {
            var meshField=typeof(PixelSurface).GetField("mesh",BindingFlags.Instance|BindingFlags.NonPublic);
            var random=new System.Random(54395); int count=0;
            float savedHeight=ScreenLayout.Height,savedWidth=ScreenLayout.Width,savedTop=ScreenLayout.Top,savedBottom=ScreenLayout.Bottom,savedStretch=ScreenLayout.Stretch;
            bool savedMap=surface.MapLayout;
            try {
                foreach(int height in new[]{960,1170}) {
                    ScreenLayout.Configure(540,height,new Rect(0,0,1,1));
                    for(int i=0;i<2500;i++) {
                        float x=(float)(random.NextDouble()*800-100),y=(float)(random.NextDouble()*1600-100);
                        float w=(float)(random.NextDouble()*600+.1),h=(float)(random.NextDouble()*500+.1);
                        if(i%10==0){x=0;y=0;w=5.5f;h=5.5f;}
                        var uv=new Rect((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble()*(i%2==0?-1:1),(float)random.NextDouble());
                        var color=new Color((float)(random.NextDouble()*1.2-.1),(float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble());
                        if(i%4==0)color=Color.white;
                        bool mapped=i%3!=0,rigid=i%2==0,pixel=i%4==0;
                        using(var reference=new VertexHelper())using(var candidate=new VertexHelper()) {
                            surface.MapLayout=mapped;meshField.SetValue(surface,reference);
                            surface.Quad(x,y,w,h,uv,color,0,pixel?21:20,rigid,0);
                            meshField.SetValue(surface,candidate);surface.GardenQuad(x,y,w,h,uv,color,pixel,rigid);
                            if(reference.currentVertCount!=candidate.currentVertCount)throw new Exception("Quad vertex count mismatch");
                            var a=new UIVertex();var b=new UIVertex();
                            for(int v=0;v<reference.currentVertCount;v++) {
                                reference.PopulateUIVertex(ref a,v);candidate.PopulateUIVertex(ref b,v);
                                int[] av=DrawObservation.Pack(a),bv=DrawObservation.Pack(b);
                                for(int k=0;k<av.Length;k++)if(av[k]!=bv[k])throw new Exception($"Garden quad mismatch: viewport={height}, case={i}, vertex={v}, field={k}, expected={av[k]:x8}, actual={bv[k]:x8}");
                            }
                            count++;
                        }
                    }
                }
            } finally {
                ScreenLayout.Height=savedHeight;ScreenLayout.Width=savedWidth;ScreenLayout.Top=savedTop;ScreenLayout.Bottom=savedBottom;ScreenLayout.Stretch=savedStretch;surface.MapLayout=savedMap;
            }
            return count;
        }
    }
}
