import json, numpy as np
from PIL import Image, ImageDraw, ImageFont
from color import *
S=2  # render scale: 540x960 logical -> 1080x1920
LV='/mnt/user-data/uploads/Flightbeat/UnityProject/Assets/Flightbeat/Resources/levels/'
FONT='/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc'
def font(px): return ImageFont.truetype(FONT,int(px*S))
PLANT={'#2982F4':'morningglory','#42A9FF':'morningglory','#FF7E27':'carrot','#FF9D30':'carrot','#30D183':'broccoli','#62E18E':'broccoli',
       '#B672FD':'eggplant','#BD80FF':'eggplant','#FFDF48':'sunflower','#FFFFFF':'cabbage','#FF525B':'strawberry','#2D2D2D':'truffle'}
TH={}
def thumb(pid,h):
    key=(pid,h)
    if key not in TH:
        im=Image.open(f'plant_{pid}.png'); w=int(im.width*h/im.height); TH[key]=im.resize((w,h),Image.LANCZOS)
    return TH[key]
def darker(rgb,f=0.62): return tuple(int(c*f) for c in rgb)
def load(n):
    d=json.load(open(LV+n+'.json')); p=d['palette']
    d['hex']=['#%02X%02X%02X'%tuple(p[i*3:i*3+3]) for i in range(len(p)//3)]; return d
def draw_bugs(dr,d,ox=0,oy=0):
    px,py=d['pitchX'],d['pitchY']; cols=d['columns']
    big=px>8
    for idx,c in enumerate(d['cells']):
        if c<0: continue
        r,k=divmod(idx,cols); x=(d['gridX']+k*px+ox)*S; y=(d['gridY']+r*py+oy)*S
        rgb=tuple(hex2rgb(d['hex'][c]))
        if big:
            m=0.9*S; w=px*S-2*m; h=py*S-2*m
            dr.rounded_rectangle([x+m,y+m,x+m+w,y+m+h],radius=w*0.42,fill=rgb,outline=darker(rgb),width=max(1,int(0.6*S)))
            cx=x+px*S/2; dr.line([cx,y+m+1,cx,y+m+h*0.55],fill=darker(rgb),width=1)
            e=0.9*S; dr.ellipse([cx-2.6*S-e/2,y+m+h*0.62,cx-2.6*S+e/2,y+m+h*0.62+e],fill=(30,30,30)); dr.ellipse([cx+2.6*S-e/2,y+m+h*0.62,cx+2.6*S+e/2,y+m+h*0.62+e],fill=(30,30,30))
        else:
            m=0.45*S; w=px*S-2*m; h=py*S-2*m
            dr.rounded_rectangle([x+m,y+m,x+m+w,y+m+h],radius=w*0.4,fill=rgb,outline=darker(rgb),width=1)
def screen(n,bug_bg,yard_bg='#93A97E',title=None):
    d=load(n); im=Image.new('RGB',(540*S,960*S),bug_bg); dr=ImageDraw.Draw(im)
    dr.rectangle([0,565*S,540*S,960*S],fill=yard_bg)
    # HUD
    dr.rectangle([0,0,540*S,82*S],fill='#6B5440'); dr.text((20*S,22*S),title or d.get('title',n),font=font(26),fill='white')
    # fence
    for x in range(0,540,18): dr.rounded_rectangle([x*S+2,560*S,(x+12)*S,592*S],radius=4,fill='#E8DCC0',outline='#8A6E4E',width=2)
    dr.rectangle([0,571*S,540*S,577*S],fill='#C9B48E')
    draw_bugs(dr,d)
    # bays
    q=d['queue']; lanes={}
    for e in q: lanes.setdefault(e['lane'],[]).append(e)
    bw=540/7
    first=[lanes[l][0] for l in sorted(lanes)][:4]
    for i in range(7):
        x0=(i*bw+5)*S; dr.rounded_rectangle([x0,600*S,x0+(bw-10)*S,668*S],radius=8*S,fill='#7A6450',outline='#5A4632',width=3)
    for i,e in enumerate(first):
        t=thumb(PLANT[d['hex'][e['color']]],int(64*S)); x=int((i*bw+bw/2)*S-t.width/2); im.paste(t,(x,int(603*S)),t)
    # queue: 4 lanes x 3 rows (skip the first entry of each lane, already deployed)
    lw=540/4
    for li,l in enumerate(sorted(lanes)):
        dr.rounded_rectangle([(li*lw+8)*S,684*S,((li+1)*lw-8)*S,872*S],radius=10*S,fill='#5F7552')
        for j,e in enumerate(lanes[l][1:4]):
            t=thumb(PLANT[d['hex'][e['color']]],int(56*S)); x=int((li*lw+lw/2)*S-t.width/2); im.paste(t,(x,int((690+j*60)*S)),t)
    for i,lbl in enumerate(['翻土','优先栽种','喷壶']):
        x0=(10+i*177)*S; dr.rounded_rectangle([x0,885*S,x0+167*S,952*S],radius=14*S,fill='#A6773F',outline='#6B4A22',width=4)
        tw=dr.textlength(lbl,font=font(24)); dr.text((x0+83*S-tw/2,900*S),lbl,font=font(24),fill='white')
    return im
def sim_image(im,kind):
    a=np.asarray(im).astype(float)
    if kind=='gray': b=gray(a)
    else: b=simulate(a,kind)
    return Image.fromarray(np.clip(b,0,255).astype(np.uint8))
