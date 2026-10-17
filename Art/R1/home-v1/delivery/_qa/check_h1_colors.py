#!/usr/bin/env python3
"""H1 main-body patch measurements: sRGB -> CIELAB D65, Delta E 1976."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,math,statistics,sys
ROOT=Path(__file__).resolve().parent
PATCHES={
 'blue':{'target':'2982F4','lit':[490,1030],'mid':[486,1080]},
 'orange':{'target':'FF7E27','lit':[297,1048],'mid':[306,1090]},
 'yellow':{'target':'FFDF48','lit':[750,1040],'mid':[750,1090]},
 'green':{'target':'62E18E','lit':[140,1070],'mid':[160,1120]},
 'purple':{'target':'BD80FF','lit':[900,1100],'mid':[910,1135]},
}
def lab(rgb):
 c=[v/255 for v in rgb];c=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c];r,g,b=c
 x=(.4124564*r+.3575761*g+.1804375*b)/.95047;y=.2126729*r+.7151522*g+.0721750*b;z=(.0193339*r+.1191920*g+.9503041*b)/1.08883
 f=lambda v:v**(1/3) if v>(6/29)**3 else v/(3*(6/29)**2)+4/29
 x,y,z=map(f,(x,y,z));return (116*y-16,500*(x-y),200*(y-z))
def measure(path,prefix):
 im=Image.open(path).convert('RGB');overlay=im.copy();d=ImageDraw.Draw(overlay);rows=[]
 for name,conf in PATCHES.items():
  target=tuple(int(conf['target'][i:i+2],16) for i in [0,2,4]);tlab=lab(target)
  for tone in ['lit','mid']:
   x,y=conf[tone];box=[x-7,y-7,x+8,y+8];pix=list(im.crop(box).getdata());rgb=tuple(round(statistics.median([p[c] for p in pix])) for c in range(3));value=lab(rgb);de=math.sqrt(sum((a-b)**2 for a,b in zip(value,tlab)))
   rows.append({'character':name,'tone':tone,'target':'#'+conf['target'],'box':box,'rgb':list(rgb),'hex':'#'+''.join(f'{v:02X}' for v in rgb),'deltaE76':round(de,4),'pass':de<=8})
   d.rectangle(box,outline='#FFFFFF',width=1);d.text((x+10,y-7),name[0]+'-'+tone,fill='#222222')
 result={'image':str(path),'method':'15x15 unfiltered median RGB body patches, sRGB CIELAB D65, Euclidean DeltaE76 against specified main hex; excludes deliberately placed specular whites, eyes and outlines','passed':all(r['pass'] for r in rows),'samples':rows}
 (ROOT/(prefix+'-colors.json')).write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');overlay.crop((0,800,1080,1230)).save(ROOT/(prefix+'-sample-patches.png'))
 print(json.dumps(result,ensure_ascii=False));return result
if __name__=='__main__':measure(Path(sys.argv[1]),sys.argv[2])
