"""Authorized finishing of generated round-eight art. Never changes previous assets."""
from pathlib import Path
import json, hashlib
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

B = Path(__file__).resolve().parents[1]
QA = B / '_qa'
ORIG = B / '_originals'
RS = Image.Resampling.LANCZOS
BG = (238, 241, 245)
CAST = [
    ('berry','9C225E','小玫',[(145,260),(268,239)]),
    ('brown','65392D','小棕',[(505,248),(635,285)]),
    ('violet','B672FD','小晶',[(870,270),(975,276)]),
    ('peach','FFCAA9','小霞',[(1260,275),(1390,250)]),
    ('slate-blue','696CAF','小靛',[(140,550),(265,560)]),
    ('lavender','C8C5FF','小雾',[(480,555),(645,594)]),
    ('cyan','46D9FF','小冰',[(870,550),(1025,527)]),
    ('pink','FFC5E0','小粉',[(1265,580),(1415,564)]),
    ('lime','92EF4D','小荧',[(140,900),(240,890)]),
    ('mint','30D183','小碧',[(530,880),(655,891)]),
    ('forest','0C7E1C','小翠',[(915,890),(982,854)]),
    ('pale-mint','C5FFE4','小清',[(1265,845),(1395,838)]),
]

def rgb(h): return np.array([int(h[i:i+2],16) for i in (0,2,4)], dtype=np.float32)
def lab(a): return cv2.cvtColor(np.asarray(a,dtype=np.float32).reshape(-1,1,3)/255,cv2.COLOR_RGB2LAB).reshape(np.asarray(a).shape)
def hx(a): return '#' + ''.join(f'{int(v):02X}' for v in a)
def sample(a, p, r=3):
    x,y=p
    return np.rint(np.median(a[y-r:y+r+1,x-r:x+r+1,:3],axis=(0,1))).astype(np.uint8)
def resize(im, size): return im.convert('RGBa').resize(tuple(size),RS).convert('RGBA')
def save(im, path): path.parent.mkdir(parents=True,exist_ok=True); im.save(path)
def bbox(a, t=0):
    yy,xx=np.where(a>t)
    return [int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)] if len(xx) else None
def trim(im): return im.crop(tuple(bbox(np.array(im)[:,:,3])))
def metadata(path):
    im=Image.open(path).convert('RGBA'); a=np.array(im)[:,:,3]
    return dict(path=str(path),size=list(im.size),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),alpha_min=int(a.min()),alpha_max=int(a.max()),partial_alpha_pixels=int(((a>0)&(a<255)).sum()),bbox=bbox(a),bbox_alpha16=bbox(a,16))

def clean_solid(im):
    """Keep generated antialiasing only adjacent to its largest solid silhouette."""
    a=np.array(im.convert('RGBA'))
    _,labels,stats,_=cv2.connectedComponentsWithStats((a[:,:,3]>128).astype(np.uint8),8)
    k=1+stats[1:,4].argmax()
    m=(labels==k).astype(np.uint8)
    contours,_=cv2.findContours(m,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    filled=np.zeros_like(m); cv2.drawContours(filled,contours,-1,1,cv2.FILLED)
    support=cv2.dilate(filled,np.ones((5,5),np.uint8))
    a[:,:,3]=np.where(support,a[:,:,3],0)
    a[a[:,:,3]<4]=0
    return Image.fromarray(a)

def anchored(im, size, height, baseline):
    im=trim(im); w=round(im.width*height/im.height)
    im=resize(im,(w,height)); a=np.array(im)
    a[a[:,:,3]<2]=0
    im=trim(Image.fromarray(a))
    out=Image.new('RGBA',size); xy=((size[0]-im.width)//2,baseline+1-im.height)
    out.alpha_composite(im,xy)
    return out,dict(size=list(size),placed_xy=list(xy),scaled_size=list(im.size),baseline_y=baseline)

def finish_lineup():
    im=Image.open(ORIG/'61-attempt-01.png').convert('RGB'); a=np.array(im)
    revision=np.array(Image.open(ORIG/'61-attempt-02.png').convert('RGB'))
    assert revision.shape==a.shape
    # AI-authored local brow revision; all other source pixels remain first-attempt pixels.
    yy,xx=np.indices((58,132)); patch_weight=np.minimum.reduce([xx/4,(131-xx)/4,yy/4,(57-yy)/4,np.ones_like(xx)]).clip(0,1)
    a[114:172,146:278]=np.rint(a[114:172,146:278]*(1-patch_weight[:,:,None])+revision[114:172,146:278]*patch_weight[:,:,None]).astype(np.uint8)
    delta=np.linalg.norm(a.astype(float)-BG,axis=2)
    _,labels,stats,_=cv2.connectedComponentsWithStats((delta>24).astype(np.uint8),8)
    candidates=[(i,s) for i,s in enumerate(stats) if i and s[4]>5000]
    candidates.sort(key=lambda item:(int(item[1][1])//340,int(item[1][0])))
    assert len(candidates)==12
    canvas=Image.new('RGB',im.size,BG); records=[]; sprites={}
    for idx,((ident,target,name,pts),(label,stat)) in enumerate(zip(CAST,candidates)):
        mask=(labels==label).astype(np.uint8)
        contours,_=cv2.findContours(mask,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        full=np.zeros_like(mask); cv2.drawContours(full,contours,-1,255,cv2.FILLED)
        # Recover a narrow soft boundary from the colored source edge, excluding gray matte.
        support=cv2.dilate(full,np.ones((3,3),np.uint8))>0
        alpha=np.where(full>0,255,np.where(support,np.clip(delta/24,0,1)*255,0)).astype(np.uint8)
        aa=np.dstack((a,alpha)); bb=bbox(alpha)
        x,y,x1,y1=bb; tile=aa[y:y1,x:x1].copy(); old=tile[:,:,:3].copy()
        hsv=cv2.cvtColor(old,cv2.COLOR_RGB2HSV).astype(float)
        tl=lab(rgb(target)); sl=cv2.cvtColor(old.astype(np.float32)/255,cv2.COLOR_RGB2LAB)
        localpts=[(px-x,py-y) for px,py in pts]
        anchors=np.array([lab(sample(old,p)) for p in localpts])
        anchor=anchors.mean(axis=0)
        source_hue=float(np.median([cv2.cvtColor(sample(old,p).reshape(1,1,3),cv2.COLOR_RGB2HSV)[0,0,0] for p in localpts]))
        dh=np.abs(hsv[:,:,0]-source_hue); dh=np.minimum(dh,180-dh)
        # Smooth hue/saturation/lightness gates preserve neutral eyes and white glints.
        weight=np.clip((23-dh)/7,0,1)*np.clip((hsv[:,:,1]-15)/28,0,1)*np.clip((sl[:,:,0]-18)/12,0,1)
        weight*=np.clip((sl[:,:,0]-(anchor[0]-30))/14,0,1)
        weight*=tile[:,:,3]/255
        mapped=tl+(sl-anchor)*np.array([0.65,0.46,0.46],np.float32)
        corrected=np.clip(cv2.cvtColor(mapped.astype(np.float32),cv2.COLOR_LAB2RGB)*255,0,255)
        tile[:,:,:3]=np.rint(old*(1-weight[:,:,None])+corrected*weight[:,:,None]).astype(np.uint8)
        if ident in ('forest','pale-mint'):
            # Recolor the existing perimeter, feathering inward at native resolution.
            tile[:,:,3]=cv2.GaussianBlur(tile[:,:,3],(3,3),0.55)
            inner=cv2.distanceTransform((tile[:,:,3]>128).astype(np.uint8),cv2.DIST_L2,5)
            w=np.clip((5.5-inner)/2.5,0,1)*(tile[:,:,3]>0)
            edgecolor='75DC89' if ident=='forest' else '39B59B'
            tile[:,:,:3]=np.rint(tile[:,:,:3]*(1-w[:,:,None])+rgb(edgecolor)*w[:,:,None]).astype(np.uint8)
        sprite=Image.fromarray(tile)
        sprites[ident]=sprite
        save(sprite,QA/f'61-{ident}-cutout.png')
        # Preserve scale across the source; translation only aligns row baselines.
        baseline=[348,663,978][idx//4]
        dx=idx%4*384+(384-sprite.width)//2; dy=baseline+1-sprite.height
        canvas.paste(sprite,(dx,dy),sprite)
        samples=[]
        for p in localpts:
            color=sample(tile,p); de=float(np.linalg.norm(lab(color)-tl))
            samples.append(dict(source_xy=[p[0]+x,p[1]+y],final_xy=[p[0]+dx,p[1]+dy],rgb=color.tolist(),hex=hx(color),delta_e76=round(de,3),method='7x7 median',pass_delta_e76=de<=8))
        records.append(dict(id=ident,name=name,target='#'+target,source_bbox=bb,placed_xy=[dx,dy],baseline_y=baseline,samples=samples,body_palette_anchor_lab=anchor.tolist(),source_hue=source_hue,outline_adjusted=ident in ('forest','pale-mint')))
    dest=B.parent/'lineup/02-lineup-new.png'; save(canvas,dest)
    return records,sprites,metadata(dest)

def finish_mystery():
    im=clean_solid(Image.open(ORIG/'76-attempt-01.png'))
    a=np.array(im); s=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
    pts=[(550,520),(850,650)]; anchors=np.array([lab(sample(a,p)) for p in pts]); anchor=anchors.mean(0); target=lab(rgb('8C919C'))
    # Gray glass palette mapping preserves high white glints and darker contour.
    w=np.clip((s[:,:,0]-25)/12,0,1)*np.clip((94-s[:,:,0])/12,0,1)*(a[:,:,3]/255)
    new=target+(s-anchor)*np.array([0.72,0.35,0.35],np.float32)
    nr=cv2.cvtColor(new.astype(np.float32),cv2.COLOR_LAB2RGB)*255
    a[:,:,:3]=np.clip(np.rint(a[:,:,:3]*(1-w[:,:,None])+nr*w[:,:,None]),0,255).astype(np.uint8)
    im,op=anchored(Image.fromarray(a),(224,256),144,212)
    dest=B/'creatures/mystery/8C919C/idle.png';save(im,dest)
    aa=np.array(im); ps=[(88,128),(130,151)]
    op['samples']=[dict(xy=list(p),hex=hx(sample(aa,p)),rgb=sample(aa,p).tolist(),delta_e76=round(float(np.linalg.norm(lab(sample(aa,p))-target)),3)) for p in ps]
    return metadata(dest),op

def finish_king():
    im=Image.open(ORIG/'77-attempt-02.png').convert('RGBA')
    left=im.crop((0,0,im.width//2,im.height))
    left=clean_solid(left)
    im,op=anchored(left,(512,512),386,470)
    dest=B/'finale/king-ready.png';save(im,dest)
    op['source_cell']=[0,0,887,887];op['blow_exported']=False
    return metadata(dest),op

def finish_button():
    im=trim(clean_solid(Image.open(ORIG/'78-attempt-01.png')))
    im=resize(im,(676,164)); out=Image.new('RGBA',(680,172));out.alpha_composite(im,(2,4))
    dest=B/'finale/button.png';save(out,dest)
    return metadata(dest),dict(cropped_source_size=list(im.size),placement=[2,4],center_text_blank=True)

def finish_rocket():
    src=Image.open(ORIG/'79-attempt-01.png').convert('RGBA'); out=Image.new('RGBA',(1152,192)); ops=[]
    # All frames share a source-y interval. Keep outward expansion through the fade.
    # Every effect is independently centered in its 192 square cell.
    for i in range(6):
        x0=round(src.width*i/6);x1=round(src.width*(i+1)/6)
        a=np.array(src.crop((x0,150,x1,610)))
        # Suppress isolated sub-opacity noise from background removal; retain intended glow.
        a[a[:,:,3]<=3]=0
        alpha=a[:,:,3].astype(float)
        opacity=[1,1,1,0.88,0.45,0.11][i]
        a[:,:,3]=np.rint(alpha*opacity).astype(np.uint8)
        im=Image.fromarray(a);bb=bbox(a[:,:,3]);im=im.crop(bb)
        scale=[0.48,0.48,0.48,0.50,0.56,0.70][i]
        im=resize(im,(round(im.width*scale),round(im.height*scale)))
        if max(im.size)>178: raise ValueError(('frame exceeds safe cell',i,im.size))
        xy=(i*192+(192-im.width)//2,(192-im.height)//2)
        out.alpha_composite(im,xy)
        cell=np.array(out.crop((i*192,0,(i+1)*192,192)))[:,:,3]
        ops.append(dict(frame=i+1,source_cell=[x0,150,x1,610],source_bbox=bb,scale=scale,opacity_multiplier=opacity,bbox=bbox(cell),alpha_max=int(cell.max()),alpha_mean=round(float(cell.mean()),3),alpha_mass=int(cell.sum())))
    dest=B/'fx/rocket-pop.png';save(out,dest)
    return metadata(dest),ops

def finish_rescue():
    src=Image.open(ORIG/'80-attempt-01.png').convert('RGBA'); a=np.array(src)
    # Lift sky-blue generation toward required pale cyan, preserving generated white glints.
    a[:,:,:3]=np.rint(a[:,:,:3]*0.55+255*0.45).astype(np.uint8)
    a[a[:,:,3]<=2]=0
    # Uniform scale: original bubble center approximately (511,395), final (64,56).
    im=resize(Image.fromarray(a),(123,184)); out=Image.new('RGBA',(128,192));out.alpha_composite(im,(3,9))
    a=np.array(out); yy,xx=np.indices(a.shape[:2]); fade=np.ones(a.shape[:2],float)
    fade*=np.clip((xx+1)/4,0,1)*np.clip((128-xx)/4,0,1)*np.clip((yy+1)/5,0,1)*np.clip((188-yy)/16,0,1)
    a[:,:,3]=np.rint(a[:,:,3]*fade).astype(np.uint8);a[a[:,:,3]<=1]=0
    out=Image.fromarray(a); dest=B/'fx/rescue-bubble.png';save(out,dest)
    return metadata(dest),dict(source_bubble_center=[511,395],scale=0.12,offset=[3,9],final_bubble_center=[64.32,56.4],palette='45% mix toward white, preserve alpha',bottom_fade_y=[172,188])

def font(size):
    return ImageFont.truetype('/System/Library/Fonts/STHeiti Light.ttc',size)
def thumb(im,h=40):
    im=trim(im);return resize(im,(round(im.width*h/im.height),h))

def qa_sheets(sprites):
    old=[('blue','2982F4','小蓝'),('orange','FF7E27','小橙'),('green','62E18E','小绿'),('purple','BD80FF','小紫'),('yellow','FFDF48','小黄'),('white','FFFFFF','小白'),('red','FF525B','小红'),('charcoal','2D2D2D','小黑')]
    names={v[0]:v[2] for v in CAST}; names.update({v[0]:v[2] for v in old})
    for ident,color,_ in old:sprites[ident]=Image.open(B/f'creatures/{ident}/{color}/idle.png').convert('RGBA')
    groups=[['cyan','blue'],['violet','lavender','slate-blue'],['lime','mint','forest','pale-mint'],['berry','red','pink'],['peach','orange'],['brown','charcoal'],['pale-mint','white'],['green','purple','yellow']]
    board=Image.new('RGB',(1160,920),'#F4F6F9');d=ImageDraw.Draw(board)
    d.text((22,12),'第三批 · 40 像素彩色 / 灰度对照',font=font(26),fill='#26364D')
    d.text((22,50),'新角色来自阵容图；旧角色来自已交付 idle。角色有效轮廓高 40 px，按 100% 查看。',font=font(15),fill='#59687D')
    for r,group in enumerate(groups):
        y=86+r*98;d.rounded_rectangle((16,y,1144,y+91),8,fill=('#E6EBF1' if r%2 else '#FFFFFF'))
        label=f'近色组 {r+1}' if r<7 else '其余老角色'
        d.text((28,y+8),label,font=font(15),fill='#33425A')
        d.text((153,y+5),'彩色',font=font(13),fill='#78859A');d.text((663,y+5),'灰度',font=font(13),fill='#78859A')
        for c,ident in enumerate(group):
            t=thumb(sprites[ident]);gray=t.convert('LA').convert('RGBA');gray.putalpha(t.getchannel('A'))
            for start,image in [(150,t),(660,gray)]:
                x=start+c*118+(80-image.width)//2;board.paste(image,(x,y+23),image)
                d.text((start+c*118,y+65),f'{names[ident]} {ident}',font=font(11),fill='#33425A')
    save(board,QA/'lineup-new-40px.png')
    # Large named sheet for user confirmation; the actual lineup remains text-free.
    lineup=Image.open(B.parent/'lineup/02-lineup-new.png').convert('RGB')
    named=Image.new('RGB',(1536,1204),BG);named.paste(lineup,(0,0)); nd=ImageDraw.Draw(named)
    for idx,(ident,color,name,_) in enumerate(CAST):
        nd.text((24+idx%4*384,1034+idx//4*53),f'{name} · {ident} · #{color}',font=font(21),fill='#2B3C55')
    save(named,QA/'lineup-new-names.png')
    # Transparent assets shown on both light and dark backgrounds.
    proof=Image.new('RGB',(1160,1030),'#EEF1F5');dr=ImageDraw.Draw(proof)
    dr.text((24,16),'第三批第一轮 · 成品透明边缘与布局预览',font=font(26),fill='#26364D')
    for col,bg in enumerate(['#E7EDF3','#25364B']):
        x=16+col*576;dr.rectangle((x,60,x+560,1014),fill=bg)
        king=Image.open(B/'finale/king-ready.png');proof.paste(king,(x+24,70),king)
        mystery=Image.open(B/'creatures/mystery/8C919C/idle.png');proof.paste(mystery,(x+173,530),mystery)
        button=resize(Image.open(B/'finale/button.png'),(510,129));proof.paste(button,(x+25,777),button)
        rescue=Image.open(B/'fx/rescue-bubble.png');proof.paste(rescue,(x+410,576),rescue)
        strip=resize(Image.open(B/'fx/rocket-pop.png'),(540,90));proof.paste(strip,(x+10,918),strip)
    save(proof,QA/'third-batch-round1-preview.png')
    # Animation proof retains exactly the delivered frame pixels before viewing scale.
    strip=Image.open(B/'fx/rocket-pop.png').convert('RGBA');frames=[]
    for i in range(6):
        fr=Image.new('RGB',(256,256),'#26354A');t=strip.crop((192*i,0,192*(i+1),192));fr.paste(t,(32,32),t);frames.append(fr)
    frames[0].save(QA/'rocket-pop-preview.gif',save_all=True,append_images=frames[1:],duration=[100,60,70,80,100,450],loop=0)
    # Include all twenty characters + two tutorial colors beside the faceless cocoon.
    mini=Image.new('RGB',(1160,162),'#E6EBF1');md=ImageDraw.Draw(mini);md.text((16,8),'神秘队员与 22 个彩色版本（有效轮廓高 40 px）',font=font(18),fill='#26364D')
    allsprites=list(sprites.items())
    allsprites += [('blue-tutorial',Image.open(B/'creatures/blue/42A9FF/idle.png').convert('RGBA')),('orange-tutorial',Image.open(B/'creatures/orange/FF9D30/idle.png').convert('RGBA')),('mystery',Image.open(B/'creatures/mystery/8C919C/idle.png').convert('RGBA'))]
    for i,(ident,im) in enumerate(allsprites):
        t=thumb(im);x=12+(i%12)*95;y=44+(i//12)*59;mini.paste(t,(x+(76-t.width)//2,y),t);md.text((x,y+40),ident,font=font(9),fill='#33425A')
    save(mini,QA/'mystery-40px-comparison.png')

def main():
    records,sprites,lineup=finish_lineup()
    mystery,mop=finish_mystery();king,kop=finish_king();button,bop=finish_button();rocket,rop=finish_rocket();rescue,reop=finish_rescue()
    qa_sheets(sprites)
    result=dict(lineup=lineup,lineup_colors=records,mystery=dict(file=mystery,postprocess=mop),king=dict(file=king,postprocess=kop),button=dict(file=button,postprocess=bop),rocket=dict(file=rocket,frames=rop),rescue=dict(file=rescue,postprocess=reop))
    (QA/'third-batch-round1-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(lineup_delta_e={r['id']:[v['delta_e76'] for v in r['samples']] for r in records},mystery=mop,king=king,rocket=rop,rescue=reop),ensure_ascii=False,indent=2))

if __name__=='__main__': main()
