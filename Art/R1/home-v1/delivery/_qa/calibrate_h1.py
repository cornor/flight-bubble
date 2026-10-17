#!/usr/bin/env python3
"""Authorized H1 body-color grading; no character movement or layer extraction."""
from PIL import Image
from pathlib import Path
import colorsys,json,math
from check_h1_colors import PATCHES,lab,measure
Q=Path(__file__).resolve().parent
src=Q/'H1-attempt-04-1080x1920.png';out=Q.parent.parent/'mock/01-home-mock.png'
def rgb_from_lab(vals):
 l,a,b=vals;fy=(l+16)/116;fx=fy+a/500;fz=fy-b/200
 def f(v):return v**3 if v>6/29 else 3*(6/29)**2*(v-4/29)
 x,y,z=.95047*f(fx),f(fy),1.08883*f(fz)
 rgb=(3.2404542*x-1.5371385*y-.4985314*z,-.969266*x+1.8760108*y+.041556*z,.0556434*x-.2040259*y+1.0572252*z)
 def gamma(v):return 12.92*v if v<=.0031308 else 1.055*max(0,v)**(1/2.4)-.055
 return tuple(round(max(0,min(1,gamma(v)))*255) for v in rgb)
def smooth(v,lo,hi):
 t=max(0,min(1,(v-lo)/(hi-lo)));return t*t*(3-2*t)
areas={
 'blue':((345,840,713,1185),(.52,.69),.50),
 'orange':((200,895,460,1166),(.045,.12),.42),
 'yellow':((635,870,875,1180),(.105,.19),.30),
 'green':((10,945,265,1186),(.28,.47),.35),
}
prior=json.loads((Q/'H1-before-color-colors.json').read_text());samples={(r['character'],r['tone']):r for r in prior['samples']}
im=Image.open(src).convert('RGB');dst=im.copy();pixels=im.load();dest=dst.load();changes={};settings={}
for name,(box,hues,smin) in areas.items():
 target=tuple(int(PATCHES[name]['target'][i:i+2],16) for i in [0,2,4]);tl,ta,tb=lab(target)
 ml,ma,mb=lab(samples[name,'mid']['rgb']);ll,la,lb=lab(samples[name,'lit']['rgb']);count=0
 settings[name]={'rectangle':box,'hueRange':hues,'minimumSaturation':smin,'sourceMidLab':(ml,ma,mb),'sourceLitLab':(ll,la,lb),'targetLab':(tl,ta,tb)}
 for y in range(box[1],box[3]):
  for x in range(box[0],box[2]):
   old=pixels[x,y];h,s,v=colorsys.rgb_to_hsv(*(k/255 for k in old))
   if not hues[0]<=h<=hues[1] or s<smin or v<.30:continue
   if name=='yellow' and 785<=x<945 and 780<=y<925:continue # Preserve the hollow soap bubble.
   L,A,B=lab(old)
   # Compress only broad lit/mid body variation to the requested main color;
   # retain the darker shadow lightness and preserve low-chroma white reflections.
   if L<ml:new_l=tl-2+(L-ml)
   elif L<=ll:new_l=tl-2+(L-ml)/max(1,ll-ml)*6.5
   else:new_l=tl+4.5+(L-ll)*.25
   new=(new_l,ta+.15*(A-ma),tb+.15*(B-mb))
   graded=rgb_from_lab(new)
   weight=smooth(s,smin,min(.70,smin+.15))*smooth(v,.30,.65)
   value=tuple(round(o+(g-o)*weight) for o,g in zip(old,graded))
   if value!=old:dest[x,y]=value;count+=1
 changes[name]=count
out.parent.mkdir(exist_ok=True);assert not out.exists(),out
dst.save(out)
(Q/'H1-color-grading.json').write_text(json.dumps({'source':str(src),'final':str(out),'method':'Hue-limited rectangular body masks; CIELAB lightness/chroma remap; pale specular highlights and dark ink retained. Purple unchanged. Soap bubble explicitly excluded. No geometry edits.','changedPixels':changes,'settings':settings},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'changedPixels':changes,'final':str(out)}))
measure(out,'H1-final')
