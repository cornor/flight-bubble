#!/usr/bin/env python3
"""Register generated inpainting, extract source pixels and verify 4-layer sprites.

Creative fur/soil content comes from image_gen masters. This script only performs
the requested source extraction, alpha masks, registration and compositing.
"""
from pathlib import Path
import json, hashlib, argparse
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFilter

ROOT=Path(__file__).resolve().parent
SIZE=(224,256)
ROSTER=json.loads((ROOT/'roster.json').read_text())
# leaf search top, exact lower-pot row, nominal front rim, body blend start
CFG={
 'koala':(121,154,148,119),'lion':(123,154,148,120),
 'green':(124,150,144,122),'purple':(124,154,148,122),
 'red':(128,158,152,126),'charcoal':(122,155,148,120),
 'slate-blue':(128,158,152,126),'cyan':(125,156,150,121),
 'lavender':(125,158,152,123),'berry':(130,157,151,127),
 'brown':(132,160,154,129),'violet':(125,156,150,123),
 'lime':(127,158,152,125),'mint':(113,144,138,111),
 'forest':(124,148,142,122),'pink':(133,160,154,131),
 'peach':(125,156,150,123),'pale-mint':(122,148,142,120),
}
# Tight per-leaf extraction regions where body/pot are also green.
REGIONS={
 'green': [[(51,128),(62,124),(83,125),(100,132),(112,146),(108,149),(76,146),(56,138)],[(114,146),(120,133),(143,124),(166,123),(177,127),(174,137),(155,145),(129,149)]],
 'lime': [[(48,131),(60,128),(79,129),(99,140),(109,154),(100,156),(76,151),(55,142)],[(109,154),(120,140),(145,132),(160,131),(173,135),(170,144),(149,152),(122,157)]],
 'mint': [[(53,121),(65,113),(80,114),(101,123),(119,140),(103,142),(74,138),(55,132)],[(120,139),(130,120),(154,114),(177,114),(191,122),(181,133),(159,139),(134,142)]],
 'forest': [[(57,129),(63,124),(82,125),(102,132),(117,145),(105,147),(79,142),(62,136)],[(118,145),(127,130),(150,124),(171,124),(184,130),(175,140),(150,145),(127,148)]],
 'pale-mint': [[(56,129),(68,122),(85,123),(104,131),(118,144),(105,147),(77,142),(60,137)],[(119,144),(129,130),(151,121),(171,121),(180,126),(173,138),(149,144),(130,148)]],
}

def read(p): return Image.open(p).convert('RGBA')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def box(a,t=32):
 y,x=np.where(a>t); return [int(x.min()),int(y.min()),int(x.max()+1),int(y.max()+1)]
def fit(im,target,source=None):
 source=source or box(np.array(im)[:,:,3])
 tile=im.crop(source).convert('RGBa').resize((target[2]-target[0],target[3]-target[1]),Image.Resampling.LANCZOS).convert('RGBA')
 out=Image.new('RGBA',SIZE);out.paste(tile,(target[0],target[1]));return np.array(out)
def hull(points):
 points=sorted(set(points))
 def cross(o,a,b):return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
 lo=[];hi=[]
 for p in points:
  while len(lo)>1 and cross(lo[-2],lo[-1],p)<=0:lo.pop()
  lo.append(p)
 for p in reversed(points):
  while len(hi)>1 and cross(hi[-2],hi[-1],p)<=0:hi.pop()
  hi.append(p)
 return lo[:-1]+hi[:-1]
def extract(ref,e):
 cid=e['creatureId'];start,lock,rim,blend=CFG[cid]
 yy,xx=np.indices(ref.shape[:2]);rgb=ref[:,:,:3].astype(int)
 excess=rgb[:,:,1]-np.maximum(rgb[:,:,0],rgb[:,:,2])
 hsv=cv2.cvtColor(ref[:,:,:3],cv2.COLOR_RGB2HSV)
 greenish=(hsv[:,:,0]>=25)&(hsv[:,:,0]<=64)&(hsv[:,:,1]>70)
 area=(yy>=start)&(yy<lock)&(ref[:,:,3]>0)
 shape=Image.new('L',SIZE);d=ImageDraw.Draw(shape)
 if cid in REGIONS:
  for poly in REGIONS[cid]:
   roi=Image.new('L',SIZE);ImageDraw.Draw(roi).polygon(poly,fill=255)
   roi=np.array(roi.filter(ImageFilter.MaxFilter(5)))>0
   seed=roi&area&greenish&(hsv[:,:,1]>155)
   if cid=='mint':seed &= hsv[:,:,0]<60
   ys,xs=np.where(seed);pts=[(int(x),int(y)) for x,y in zip(xs,ys)]
   if pts:d.polygon(hull(pts),fill=255)
  mask=(np.array(shape)>0)&area&greenish
  if cid=='lime':mask &= (xx>=52)&(xx<=171)&(yy>=130)
 else:
  seed=area&greenish&(ref[:,:,3]>8)
  for left,right in [(0,e['potCenterX']),(e['potCenterX'],224)]:
   ys,xs=np.where(seed[:,left:right]);pts=[(int(x+left),int(y)) for x,y in zip(xs,ys)]
   if pts:d.polygon(hull(pts),fill=255)
  mask=((np.array(shape)>0)|(area&greenish))&area&greenish
  if cid=='cyan': mask &= excess>8 # front flippers are body, never leaves
 out=np.zeros_like(ref);out[mask]=ref[mask]
 return out,mask
def sprout(e,rim):
 im=read(ROOT/'example/bear-2982F4-sprout.png');a=np.array(im);b=box(a[:,:,3]);foot=b[3]-1
 x=int(round(np.median(np.where(a[foot-2:foot+1,:,3]>32)[1])))
 out=Image.new('RGBA',SIZE);out.paste(im,(e['potCenterX']-x,rim-3-foot))
 return np.array(out)
def blend_rgba(a,b,w):
 wa=w[:,:,None];aa=a[:,:,3:4]/255.;ba=b[:,:,3:4]/255.
 alpha=aa*(1-wa)+ba*wa; rgb=a[:,:,:3]*aa*(1-wa)+b[:,:,:3]*ba*wa
 rgb=np.divide(rgb,alpha,out=np.zeros_like(rgb),where=alpha>0)
 out=np.clip(np.rint(np.concatenate([rgb,alpha*255],2)),0,255).astype('uint8');out[w==0]=a[w==0];out[w==1]=b[w==1];return out
def make(e,deliver):
 cid=e['creatureId'];start,lock,rim,by=CFG[cid]
 ref=np.array(read(ROOT/'references'/e['reference']));yy,xx=np.indices(ref.shape[:2])
 leaves,mask=extract(ref,e)
 # Copy foreground leaves literally. Completed concealed bases use the supplied
 # image_gen leaf master, sampled only outside the original visible mask.
 lm=ROOT/'masters/shared-leaf-completion.png'
 lb=box(leaves[:,:,3]);lg=fit(read(lm),[lb[0],lb[1],lb[2],lb[3]+2])
 near=np.array(Image.fromarray(mask.astype('uint8')*255).filter(ImageFilter.MaxFilter(5)))>0
 add=(~mask)&near&(yy>=lock-2)&(yy<lock+2)&(lg[:,:,3]>0)
 leaves[add]=lg[add]
 # Source-locked lower pot. A single generated oval avoids leaf-shaped notches.
 alpha=ref[:,:,3].copy();alpha[:lock]=0;pb=box(alpha)
 if cid=='charcoal': pb[0],pb[2]=46,185
 pot=fit(read(ROOT/'masters'/f'{cid}-pot-01.png'),[pb[0],rim-13,pb[2],pb[3]])
 # Fit the newly exposed oval into the shallow space behind the original front
 # rim. A tall rear wall would cover the lower chest when pot is above body.
 upper=Image.fromarray(pot).crop((0,rim-13,224,lock)).convert('RGBa').resize((224,12),Image.Resampling.LANCZOS).convert('RGBA')
 pot[:lock]=0;pot[lock-12:lock]=np.array(upper)
 rim=lock-3
 pot[lock:]=ref[lock:]
 # Only animal arms can extend outside the pot; route those source pixels back
 # into body rather than accidentally leaving flippers on the empty pot.
 outside=np.zeros(mask.shape,bool)
 if cid=='charcoal': outside=(yy>=122)&(yy<158)&((xx<46)|(xx>=185));pot[outside]=0
 # Use generated torso, then restore the original face/ears exactly.
 ha=ref[:,:,3].copy();ha[start:]=0;hb=box(ha)
 bm=ROOT/'masters'/f'{cid}-body-02.png'
 if not bm.exists():bm=ROOT/'masters'/f'{cid}-body-01.png'
 body=fit(read(bm),[hb[0],hb[1],hb[2],201])
 original=ref.copy();original[mask]=0;original[lock:]=0
 w=np.clip((yy-by)/8.,0,1);w=w*w*(3-2*w)
 w[mask]=1
 # Keep the original rounded lower muzzle. A horizontal blend through a cream
 # muzzle would shave it flat even when the generated torso is continuous.
 rgb=ref[:,:,:3].astype(float)
 if cid in ['koala','lion','red','slate-blue','berry','brown','violet','pale-mint']:
  cream=(rgb[:,:,0]>rgb[:,:,2]*1.08)&(rgb[:,:,1]>110)&(rgb[:,:,2]>85)&(yy<start+7)&~mask
  w[cream]=0
 body=blend_rgba(original,body,w)
 # Body is contained behind the original pot below the lock row.
 body[lock:,:,3]=np.where(ref[lock:,:,3]>=240,body[lock:,:,3],0)
 body[201:]=0;body[:6]=0
 if cid=='charcoal':body[outside]=ref[outside]
 if cid=='cyan':
  flippers=(((xx-68)/23.)**2+((yy-128)/14.)**2<1)|(((xx-154)/23.)**2+((yy-126)/15.)**2<1)
  flippers &= (ref[:,:,1]>ref[:,:,0]*1.2)&(ref[:,:,2]>ref[:,:,1]*.88)&(ref[:,:,3]>8)
  body[flippers]=ref[flippers]
 neck=ROOT/'masters'/f'{cid}-neck-repair-01.png'
 if neck.exists():
  corrected=fit(read(neck),box(body[:,:,3]))
  nw=np.minimum(np.clip((yy-by)/5.,0,1),np.clip((164-yy)/6.,0,1))
  nw=nw*nw*(3-2*nw);body=blend_rgba(body,corrected,nw)
 sd=sprout(e,rim)
 layers={'pot':pot,'sprout':sd,'leaves':leaves,'body':body}
 folder=ROOT/('delivery/creatures' if deliver else 'qa/candidates')/cid/e['hex'];folder.mkdir(parents=True,exist_ok=True)
 for kind,a in layers.items():Image.fromarray(a).save(folder/f'{kind}.png')
 composite=Image.alpha_composite(Image.alpha_composite(Image.fromarray(body),Image.fromarray(pot)),Image.fromarray(leaves))
 planted=Image.alpha_composite(Image.fromarray(pot),Image.fromarray(sd))
 composite.save(ROOT/'qa'/f'{cid}-composite.png');planted.save(ROOT/'qa'/f'{cid}-sprout.png')
 Image.fromarray(mask.astype('uint8')*255).save(ROOT/'qa'/f'{cid}-leaf-mask.png')
 locked=(yy>=lock)&~outside
 assert np.array_equal(pot[locked],ref[locked])
 assert np.array_equal(leaves[mask],ref[mask])
 assert np.array_equal(body[6:by],ref[6:by])
 assert not sd[rim:,:,3].any()
 sb=box(sd[:,:,3]);assert 30<=sb[3]-sb[1]<=40
 points={'rimY':rim,'stemX':e['potCenterX'],'stemY':rim-3}
 assert pot[points['stemY'],points['stemX'],3]>240
 assert body[by:lock+6,e['potCenterX']-8:e['potCenterX']+8,3].min()>240
 report={'creatureId':cid,'hex':e['hex'],'size':[224,256],'mode':'RGBA','points':points,'potExactFromY':lock,'potLockedPixels':int(locked.sum()),'potDifferentPixels':int(np.any(pot[locked]!=ref[locked],axis=1).sum()),'originalLeavesDifferentPixels':int(np.any(leaves[mask]!=ref[mask],axis=1).sum()),'faceExactBeforeY':by,'sproutBounds':sb,'stemSoilRGBA':pot[points['stemY'],points['stemX']].tolist(),'centralNeckAlphaMin':int(body[by:lock+6,e['potCenterX']-8:e['potCenterX']+8,3].min()),'sourceSha256':sha(ROOT/'references'/e['reference']),'outputs':{k:sha(folder/f'{k}.png') for k in layers}}
 return ref,layers,composite,planted,report

def main():
 p=argparse.ArgumentParser();p.add_argument('--deliver',action='store_true');args=p.parse_args()
 (ROOT/'qa').mkdir(exist_ok=True)
 done=[]
 for e in ROSTER:
  if all((ROOT/'masters'/f"{e['creatureId']}-{k}-01.png").exists() for k in ['body','pot']):done.append((e,make(e,args.deliver)))
 points={e['hex']:item[-1]['points'] for e,item in done}
 dest=ROOT/('delivery' if args.deliver else 'qa');dest.mkdir(exist_ok=True)
 (dest/'sprout-points.json').write_text(json.dumps(points,ensure_ascii=False,indent=2)+'\n')
 (ROOT/'qa/validation.json').write_text(json.dumps({'characters':len(done),'images':len(done)*4,'assets':[item[-1] for e,item in done]},ensure_ascii=False,indent=2)+'\n')
 for n in range(0,len(done),6):
  batch=done[n:n+6];review=Image.new('RGB',(1120,280*len(batch)),(222,217,234));d=ImageDraw.Draw(review)
  for i,(e,item) in enumerate(batch):
   ref,layers,comp,planted,report=item
   for j,(label,img) in enumerate([('ORIGINAL',Image.fromarray(ref)),('BODY > POT > LEAVES',comp),('POT + SPROUT',planted),('BODY',Image.fromarray(layers['body'])),('LEAVES',Image.fromarray(layers['leaves']))]):
    x=j*224;y=i*280;d.text((x+5,y+4),e['creatureId']+' / '+label,fill=(45,35,65));review.paste(img,(x,y+24),img)
  review.save(ROOT/'qa'/f'batch-{n//6+1}-comparison.png')
 print('Completed',len(done),'characters;',len(done)*4,'PNG files')

if __name__=='__main__':main()
