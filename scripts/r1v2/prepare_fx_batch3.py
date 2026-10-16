#!/usr/bin/env python3
"""Reproducible production sizing/strip packing of the batch-3 imagegen originals.
No procedural replacement art. Source alpha is retained; tint sheets are neutralized.
"""
from pathlib import Path
import hashlib, json
from PIL import Image, ImageOps, ImageDraw
import numpy as np
ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT/'Art/R1/art-v2/delivery'
RAW, OUT, QA = BASE/'_originals/fx-batch3', BASE/'fx', BASE/'_qa/fx-batch3'
OUT.mkdir(exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
SPECS = {
 'repel-gust': (1080,120,1,False), 'dash-trail': (96,256,1,True),
 'impact-dust': (960,160,6,False), 'merge-burst': (1152,192,6,False),
 'beam': (48,256,1,True), 'beam-tip': (256,64,4,True),
 'water-can': (256,256,1,False), 'water-splash': (2048,256,8,False),
 'water-aim': (256,160,1,False), 'revive-wave': (1080,360,1,False),
}
def bounds(im): return im.getchannel('A').point(lambda x:255 if x>2 else 0).getbbox()
def fit(im,size):
    # Resample premultiplied alpha to avoid colored fringes, retaining native transparency.
    return ImageOps.contain(im.convert('RGBa'),size,Image.Resampling.LANCZOS).convert('RGBA')
def periodic(im,axis,pad):
    # Texture export seam bleed: blend only the guard band to one common edge.
    a=np.array(im,dtype=np.float32)
    if axis==1:a=a.transpose(1,0,2)
    alpha=a[:,:,3:]/255; a[:,:,:3]*=alpha
    edge=(a[0]+a[-1])*.5
    for i in range(pad):
        t=(i/(pad-1))**.65
        a[i]=edge*(1-t)+a[i]*t; a[-1-i]=edge*(1-t)+a[-1-i]*t
    np.divide(a[:,:,:3],a[:,:,3:]/255,out=a[:,:,:3],where=a[:,:,3:]>0)
    if axis==1:a=a.transpose(1,0,2)
    return Image.fromarray(np.clip(a,0,255).astype('uint8'))
report=[]
for name,(w,h,n,gray) in SPECS.items():
    src=Image.open(RAW/(name+'.png')).convert('RGBA'); notes=[]
    if n>1:
        # The water source is 4x2. All others are one row with extra transparent vertical padding.
        cols,rows=(4,2) if name=='water-splash' else (n,1)
        frames=[]
        for i in range(n):
            x,y=i%cols,i//cols
            if name=='water-splash':
                # Generated row 1 overhangs its nominal half-height. The clean gap is y=440..480.
                # Align the water landing plane across both rows, excluding the previous row.
                cut=src.crop((round(x*src.width/cols),0 if y==0 else 480,round((x+1)*src.width/cols),440 if y==0 else src.height))
                frame=Image.new('RGBA',(round(src.width/cols),496)); frame.paste(cut,(0,0 if y==0 else 140)); frames.append(frame)
            else:
                frames.append(src.crop((round(x*src.width/cols),round(y*src.height/rows),round((x+1)*src.width/cols),round((y+1)*src.height/rows))))
        # Same vertical crop and scale for ALL frames, so sizes and animation origins do not jump.
        bb=[bounds(f) for f in frames]; top=min(b[1] for b in bb); bottom=max(b[3] for b in bb)
        side=max(max(f.width for f in frames),bottom-top)
        final=Image.new('RGBA',(w,h)); cell=w//n
        for i,f in enumerate(frames):
            canvas=Image.new('RGBA',(side+20,side+20)); cut=f.crop((0,top,f.width,bottom))
            canvas.paste(cut,((canvas.width-cut.width)//2,(canvas.height-cut.height)//2))
            final.paste(fit(canvas,(cell,h)),(i*cell,0))
        notes.append(f'{cols}x{rows} source packed to {n}x1; common frame origin/scale; transparent safety margin')
    else:
        if name=='repel-gust':
            b=bounds(src); src=src.crop((0,b[1]-8,src.width,b[3]+8))
        elif name=='beam':
            b=bounds(src); src=src.crop((b[0]-8,0,b[2]+8,src.height))
        final=src.convert('RGBa').resize((w,h),Image.Resampling.LANCZOS).convert('RGBA')
    if gray:
        a=final.getchannel('A'); g=ImageOps.grayscale(final).point(lambda v:max(192,v))
        final=Image.merge('RGBA',(g,g,g,a));notes.append('Neutral white/light-gray export; R=G=B >=192 wherever alpha>0')
    if name=='repel-gust':final=periodic(final,1,16);notes.append('16px horizontal seam guard export')
    if name=='beam':final=periodic(final,0,12);notes.append('12px vertical seam guard export')
    # Last frames keep the generated silhouettes and colors but use a production alpha envelope.
    if n in (6,8):
        a=np.array(final); cell=w//n
        for frame,factor in [(n-2,.55),(n-1,.18)]: a[:,frame*cell:(frame+1)*cell,3]=(a[:,frame*cell:(frame+1)*cell,3]*factor).astype('uint8')
        final=Image.fromarray(a);notes.append('Last two frames alpha fades: 55%, 18%')
    if name=='revive-wave':
        a=np.array(final); fade=np.clip((h-1-np.arange(h))/(h*.38),0,1)
        a[:,:,3]=(a[:,:,3]*fade[:,None]).astype('uint8');final=Image.fromarray(a);notes.append('Bottom 38% alpha fade to transparent')
    final.save(OUT/(name+'.png'))
    a=np.array(final); visible=a[:,:,3]>0
    assert final.size==(w,h) and a[:,:,3].min()==0 and a[:,:,3].max()>0
    if gray:assert np.all(a[:,:,:3][visible].max(1)==a[:,:,:3][visible].min(1))
    if name=='repel-gust':assert np.array_equal(a[:,0],a[:,-1])
    if name=='beam':assert np.array_equal(a[0],a[-1])
    report.append(dict(file='fx/'+name+'.png',size=[w,h],frames=n,tinted=gray,sourceSize=list(Image.open(RAW/(name+'.png')).size),sha256=hashlib.sha256((OUT/(name+'.png')).read_bytes()).hexdigest(),alphaRange=[int(a[:,:,3].min()),int(a[:,:,3].max())],notes=notes))
(QA/'assets.json').write_text(json.dumps(dict(passed=True,excluded='hit-pop: cancelled by user; retained in originals only',assets=report),ensure_ascii=False,indent=2))
# Review board, scaled previews on a neutral checker; not a deliverable texture.
board=Image.new('RGB',(1200,1000),(225,224,212)); d=ImageDraw.Draw(board)
for i,r in enumerate(report):
    x=(i%2)*600;y=(i//2)*200;d.text((x+10,y+8),r['file']+' '+str(r['size']),fill=(40,40,40))
    im=Image.open(BASE/r['file']);im=fit(im,(570,160));board.paste(im,(x+15,y+32),im)
board.save(QA/'contact-sheet.jpg')
print(json.dumps(dict(passed=True,assets=len(report))))
