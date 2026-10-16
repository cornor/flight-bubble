#!/usr/bin/env python3
"""Deterministic source-sheet slicing/layer separation for the requested R1 art pass.
Never writes Art/Concepts. Crop coordinates and digit-removal masks are reviewable.
Requires Pillow, NumPy and OpenCV. No generative repainting of the concept artwork.
"""
import hashlib, json, sys
from pathlib import Path
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT/'Art/Concepts'
OUT = ROOT/'Art/R1/slots'
REVIEW = ROOT/'Art/R1/art-v1'
cv2.setNumThreads(1)
# Source pixels, 2172 x 724. idle=待战, attack=出手, spent=打完.
PLANTS = {
 'morning-glory': ('morningglory','2982F4',[(22,198,307,586),(630,190,880,586),(1246,311,1522,586)],[(121,513,214,560),(728,513,821,560),(1298,513,1394,560)],(794,95,878,171)),
 'carrot': ('carrot','FF7E27',[(48,133,293,559),(684,148,977,559),(1344,217,1582,559)],[(127,479,223,536),(778,479,877,536),(1396,479,1496,536)],(947,94,1024,166)),
 'broccoli': ('broccoli','62E18E',[(23,210,296,584),(632,222,905,584),(1240,278,1520,584)],[(114,508,207,553),(718,508,814,553),(1309,508,1404,553)],(791,79,900,182)),
 'eggplant': ('eggplant','BD80FF',[(53,182,298,594),(650,207,866,594),(1250,260,1525,594)],[(108,516,203,566),(718,516,814,566),(1307,516,1404,566)],(801,141,851,188)),
 'sunflower': ('sunflower','FFDF48',[(25,163,331,587),(640,178,925,587),(1265,261,1541,587)],[(132,520,222,566),(752,520,844,566),(1337,520,1433,566)],(817,117,862,168)),
 'cabbage': ('cabbage','FFFFFF',[(22,154,288,584),(622,209,913,584),(1242,239,1528,584)],[(119,516,212,565),(710,512,798,564),(1320,516,1417,565)],(885,101,1003,195)),
 'strawberry': ('strawberry','FF525B',[(44,230,268,565),(594,253,828,565),(1246,290,1494,565)],[(109,487,205,537),(685,487,777,537),(1295,487,1392,537)],(799,117,852,174)),
 'truffle': ('truffle','2D2D2D',[(43,244,281,561),(651,236,865,561),(1248,307,1494,561)],[(109,479,208,536),(724,479,823,536),(1297,479,1398,536)],(799,78,865,153)),
}
records=[]

def save(im, ident):
 p=OUT/(ident+'.png');p.parent.mkdir(parents=True,exist_ok=True);im.save(p)

def rgba(a, alpha): return Image.fromarray(np.dstack([np.clip(a,0,255).astype('uint8'),alpha.astype('uint8')]),'RGBA')

def foreground(rgb):
 """Remove connected grass, retain the largest outlined plant and enclosed leaves."""
 a=np.asarray(rgb).astype(float)/255
 mx=a.max(2);mn=a.min(2);sat=(mx-mn)/np.maximum(mx,1e-6)
 r,g,b=np.moveaxis(a,2,0)
 hue=np.degrees(np.arctan2(np.sqrt(3)*(g-b),2*r-g-b))%360
 grass=((hue>70)&(hue<122)&(sat>.45)&(mx>.55)).astype('uint8')
 n,labels=cv2.connectedComponents(grass,8)
 edge=np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]]));edge=edge[edge!=0]
 bg=np.isin(labels,edge).astype('uint8')
 bg=cv2.morphologyEx(bg,cv2.MORPH_OPEN,np.ones((3,3),'uint8'))
 fg=1-bg
 # Largest contour also closes enclosed eyes/leaf patches; disconnected motion marks drop out.
 contours,_=cv2.findContours(fg,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
 mask=np.zeros(fg.shape,'uint8')
 cv2.drawContours(mask,[max(contours,key=cv2.contourArea)],-1,255,-1)
 # Refine the contour against the actual grass colors. Fixed sage leaves have much
 # lower chroma than the background; their dark outline seeds remain foreground.
 labels=np.where(mask>0,cv2.GC_PR_FGD,cv2.GC_BGD).astype('uint8')
 inside=cv2.erode(mask,np.ones((5,5),'uint8'))>0
 labels[inside & (grass==0)]=cv2.GC_FGD
 if np.any(labels==cv2.GC_FGD) and np.any(labels==cv2.GC_BGD):
  cv2.setRNGSeed(130026)
  cv2.grabCut(np.asarray(rgb),labels,None,np.zeros((1,65)),np.zeros((1,65)),3,cv2.GC_INIT_WITH_MASK)
  mask=np.where((labels==cv2.GC_FGD)|(labels==cv2.GC_PR_FGD),255,0).astype('uint8')
  _,cc,stats,_=cv2.connectedComponentsWithStats(mask,8)
  mask=np.where(cc==1+np.argmax(stats[1:,4]),255,0).astype('uint8')
 return mask

def layers(image, alpha, ident, pot_top, seed_disk=None, headband=None):
 a=np.asarray(image).astype(float);r,g,b=np.moveaxis(a,2,0)
 hsv=cv2.cvtColor(a.astype('uint8'),cv2.COLOR_RGB2HSV);h=hsv[:,:,0];s=hsv[:,:,1]/255;v=hsv[:,:,2]/255
 yy,xx=np.indices(alpha.shape)
 pot=yy>=pot_top
 # Keep facial ink, whites and mouth colors in the fixed layer. Main glaze includes the entire base.
 if ident=='morning-glory': main=(b>r*1.13)&(b>g*.91)
 elif ident=='carrot': main=(r>g*1.16)&(g>b*1.4)&(h<32)
 elif ident=='broccoli': main=(g>r*1.06)&(g>b*1.04)&(s>.34)&(h>39)
 elif ident=='eggplant': main=(b>g*1.15)&(r>g*1.06)
 elif ident=='sunflower': main=(r>b*1.5)&(g>b*1.4)&(h<38)
 elif ident=='strawberry': main=(r>g*1.35)&(r>b*1.2)
 else: main=np.ones(alpha.shape,bool)
 if ident=='cabbage': main=~((g>r*1.02)&(g>b*1.08)&(s>.14))
 # Neutral/dark facial features are fixed, but dark truffle surface itself remains tintable.
 neutral=(s<.17)&(v>.78)
 dark=v<(.12 if ident=='truffle' else .18)
 mouth=(h<15)&(s>.4)&(v<.65)&~pot
 if ident=='truffle':
  # Preserve only eye whites and neutral black facial ink; the charcoal body and cracks tint together.
  main=~neutral & ~((v<.08)&~pot)
 elif ident!='cabbage': main &= ~neutral & ~dark & ~mouth
 else: main &= ~dark & ~mouth
 main |= pot & (v>.08)
 fixed=a.copy()
 # Normalize decorative leaves to the fixed sage palette, not a competing gameplay green.
 leaf=(g>r*1.015)&(g>b*1.07)&(s>.18)&~main&~pot
 if ident=='cabbage':
  t=np.clip((v-.4)/.55,0,1)[...,None]
  fixed[leaf]=(np.array([195,216,142])*(1-t)+np.array([228,237,184])*t)[leaf]
 else:
  shade=np.clip(v/.65,.45,1.1)[...,None]
  fixed[leaf]=(np.array([134,166,126])*shade)[leaf]
 if seed_disk is not None:
  cx,cy,rx,ry=seed_disk
  disk=((xx-cx)/rx)**2+((yy-cy)/ry)**2<1
  gold=disk&main
  # UI spec v1.1 / 创意方案 9.1: mustard gold #D2A63C; only a light shade so it never reads orange-brown.
  fixed[gold]=(np.array([210,166,60])*(.84+.16*np.clip(v/.82,0,1))[...,None])[gold]
  main[gold]=False
 if headband is not None:
  top,bottom=headband
  band=(yy>=top)&(yy<bottom)&main
  fixed[band]=(np.array([217,198,245])*np.clip(v/.88,.62,1)[...,None])[band]
  main[band]=False
 # Bright/middle regions equal the level color; only shadows multiply it darker.
 light=(r*.27+g*.60+b*.13)/255
 relevant=main&(alpha>0)
 denom=max(float(np.percentile(light[relevant],82)) if relevant.any() else .8,.05)
 gray=np.clip((light/denom)**1.15,.12,1)*255
 gray=np.repeat(gray[:,:,None],3,2)
 main_image=rgba(gray,alpha)
 fixed_image=rgba(fixed,alpha*(~main))
 return main_image,fixed_image

def small_size_pass(main, fixed, alpha, ring_px):
 """UI spec v1.1 section 6 (interim): thicker outer outline and a little more punch so
 the sprites survive being shown at 56-80 px. Outline lives in the fixed layer (never tinted)."""
 m=np.array(main);f=np.array(fixed);h,w=alpha.shape
 grow=lambda arr:np.pad(arr,((ring_px,ring_px),(ring_px,ring_px),(0,0)))
 m=grow(m);f=grow(f);a=np.pad(alpha,ring_px)
 kernel=cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(2*ring_px+1,2*ring_px+1))
 ring=(cv2.dilate(a,kernel)>0)&(a==0)
 f[ring]=(52,36,24,255)
 # Slightly richer fixed colors (faces, leaves, accessories); main stays a pure tint mask.
 body=f[:,:,3]>0;hsv=cv2.cvtColor(f[:,:,:3],cv2.COLOR_RGB2HSV).astype(float)
 hsv[:,:,1]=np.clip(hsv[:,:,1]*1.1,0,255)
 rgb=cv2.cvtColor(hsv.astype('uint8'),cv2.COLOR_HSV2RGB);f[:,:,:3]=np.where(body[...,None]&~ring[...,None],rgb,f[:,:,:3])
 return Image.fromarray(m,'RGBA'),Image.fromarray(f,'RGBA')

def sharpen(im):
 from PIL import ImageFilter
 rgb=im.convert('RGB').filter(ImageFilter.UnsharpMask(radius=1.1,percent=70,threshold=2))
 out=Image.merge('RGBA',(*rgb.split(),im.getchannel('A')));return out

def projectile_mask(image, ident):
 """Isolate the projectile itself: the sheet's attached motion trails are not sprites."""
 ellipses={'morning-glory':(8,0,83,75),'carrot':(8,2,75,69),'eggplant':(3,3,46,45)}
 if ident in ellipses:
  mask=Image.new('L',image.size);ImageDraw.Draw(mask).ellipse(ellipses[ident],fill=255)
  return np.array(mask)
 a=np.array(image);hsv=cv2.cvtColor(a,cv2.COLOR_RGB2HSV)
 r,g,b=np.moveaxis(a.astype(float),2,0)
 if ident=='broccoli': select=(hsv[:,:,0]>47)&(hsv[:,:,0]<90)&(hsv[:,:,1]>80)
 elif ident=='strawberry': select=(r>g*1.5)&(r>b*1.2)
 elif ident=='cabbage': select=(np.abs(r-g)<22)&(np.abs(g-b)<22)
 elif ident=='sunflower': select=a.max(2)<175
 else: return foreground(image)
 mask=select.astype('uint8')*255
 mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,np.ones((3,3),'uint8'))
 contours,_=cv2.findContours(mask,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
 contour=max(contours,key=cv2.contourArea)
 if ident=='sunflower':contour=cv2.convexHull(contour)
 mask[:]=0;cv2.drawContours(mask,[contour],-1,255,-1)
 return mask

def plant_assets():
 preview=Image.new('RGB',(8*180,3*220),(163,149,129));pd=ImageDraw.Draw(preview)
 for col,(ident,(src,color,boxes,digits,bullet)) in enumerate(PLANTS.items()):
  image=Image.open(SOURCE/f'plant-characters-v05/action-{src}.png').convert('RGB')
  # 1.5x canvases (168x192, same 7:8 aspect) so 76-80 px bay plants stay sharp on DPR 2 screens.
  scale=min(165/max(b[3]-b[1] for b in boxes),156/max(b[2]-b[0] for b in boxes))
  ring_px=max(5,round(.018*max(b[3]-b[1] for b in boxes)))
  for row,(state,box,digit) in enumerate(zip(['idle','attack','spent'],boxes,digits)):
   crop=image.crop(box);alpha=foreground(crop)
   yy,xx=np.indices(alpha.shape);xx+=box[0];yy+=box[1]
   # Remove attached motion streaks and grass flecks; projectile art is a separate slot.
   if state=='attack':
    if ident=='carrot': alpha[(xx>827)&(xx<890)&(yy<229)]=0
    if ident=='morning-glory': alpha[yy<213]=0
    if ident=='broccoli': alpha[yy<236]=0
    if ident=='eggplant': alpha[(xx>765)&(xx<815)&(yy<257)]=0
    if ident=='sunflower': alpha[yy<209]=0
    if ident=='strawberry': alpha[(xx>796)&(yy<323)]=0
    if ident=='cabbage': alpha[(xx>826)&(yy>440)&(yy<500)]=0
   # Foreground extraction occasionally joins an isolated grass blade to the outer contour.
   raw=np.array(crop).astype(float);hsv=cv2.cvtColor(raw.astype('uint8'),cv2.COLOR_RGB2HSV)
   outer=cv2.erode(alpha,np.ones((5,5),'uint8'))==0
   grass_edge=(hsv[:,:,0]>38)&(hsv[:,:,0]<57)&(hsv[:,:,1]>95)&(hsv[:,:,2]>175)&outer
   alpha[grass_edge]=0
   if ident=='morning-glory':
    # Blue petals and glaze cannot contain source grass; keep the actual leaves
    # between these regions. This also removes the isolated corner at the flower.
    green=(hsv[:,:,0]>30)&(hsv[:,:,0]<70)&(hsv[:,:,1]>50)
    alpha[green&((yy<410)|(yy>570))]=0
   if ident=='carrot' and state=='attack':
    alpha[(xx>820)&(xx<880)&(yy<247)&(hsv[:,:,0]>38)&(hsv[:,:,0]<57)&(hsv[:,:,1]>110)]=0
    alpha[(xx>925)&(yy>414)&(yy<475)]=0
   if ident!='broccoli':
    alpha[(yy>box[3]-25)&(hsv[:,:,0]>30)&(hsv[:,:,0]<70)&(hsv[:,:,1]>50)]=0
   # Cutting attached motion trails can leave detached sparkles: retain only the
   # character component after cleanup, including the full outlined leaf tips.
   _,cc,stats,_=cv2.connectedComponentsWithStats(alpha,8)
   alpha=np.where(cc==1+np.argmax(stats[1:,4]),255,0).astype('uint8')
   a=np.array(crop);mask=np.zeros(alpha.shape,'uint8')
   x0,y0,x1,y1=digit;x0-=box[0];x1-=box[0];y0-=box[1];y1-=box[1]
   mask[max(0,y0-6):y1+6,max(0,x0-12):x1+12]=255
   a=cv2.inpaint(a,mask,7,cv2.INPAINT_TELEA)
   pot_top=(470 if ident in ['morning-glory','eggplant','cabbage','sunflower'] else 451)-box[1]
   seed=None
   if ident=='sunflower':
    cx,cy,rx,ry=[(181,323,72,72),(794,315,72,68),(1404,393,67,57)][row]
    seed=(cx-box[0],cy-box[1],rx,ry)
   band=None
   if ident=='eggplant':
    t,b=[(264,312),(277,318),(338,367)][row];band=(t-box[1],b-box[1])
   main,fixed=layers(Image.fromarray(a),alpha,ident,pot_top,seed,band)
   main,fixed=small_size_pass(main,fixed,alpha,ring_px)
   # Same per-plant scale and pot baseline across all poses; no squeezing the spent pose tall.
   pad=ring_px;size=(round((crop.width+2*pad)*scale),round((crop.height+2*pad)*scale));at=((168-size[0])//2,189-size[1])
   cut=rgba(a,alpha);cutpath=REVIEW/'cutouts'/f'{ident}-{state}.png';cutpath.parent.mkdir(parents=True,exist_ok=True);cut.save(cutpath)
   result=[]
   for suffix,part in [('main',main),('fixed',fixed)]:
    canvas=Image.new('RGBA',(168,192));canvas.alpha_composite(sharpen(part.resize(size,Image.Resampling.LANCZOS)),at)
    save(canvas,f'plants/{ident}/{state}-{suffix}');result.append(canvas)
   tinted=np.array(result[0]);tinted[:,:,:3]=(tinted[:,:,:3].astype(float)*np.array(tuple(bytes.fromhex(color)))/255).astype('uint8')
   combo=Image.fromarray(tinted);combo.alpha_composite(result[1]);preview.paste(combo.resize((168,192)),(col*180+6,row*220),combo.resize((168,192)))
   pd.text((col*180+8,row*220+198),f'{ident} {state}',fill=(40,35,30))
   records.append({'id':ident+'/'+state,'source':str(image.filename) if hasattr(image,'filename') else f'action-{src}.png','crop':box,'digitsRemoved':digit,'scale':scale,'canvas':[168,192],'potBaseline':189,'outerOutlineSourcePx':ring_px})
  shot=image.crop(bullet);alpha=projectile_mask(shot,ident)
  main,fixed=layers(shot,alpha,ident,10000)
  for suffix,part in [('main',main),('fixed',fixed)]:
   part.thumbnail((28,28),Image.Resampling.LANCZOS);canvas=Image.new('RGBA',(32,32));canvas.alpha_composite(part,((32-part.width)//2,(32-part.height)//2));save(canvas,f'plants/{ident}/projectile-{suffix}')
  records.append({'id':ident+'/projectile','source':f'Art/Concepts/plant-characters-v05/action-{src}.png','crop':bullet,'mask':'projectile_mask: outlined object only, excluding attached motion trails','size':[32,32]})
 preview.save(REVIEW/'previews/plants-three-states.png')

def bug_assets():
 base=SOURCE/'bugs-r1/calibration/sprites'
 specs=[('regular','2982f4-11px.png','2982f4'),('teaching','2982f4-21px.png','2982f4')]
 specs += [('teaching-'+name,f'training-{color}-21px.png',color) for name,color in [('morning-glory','42a9ff'),('carrot','ff9d30'),('broccoli','62e18e'),('eggplant','bd80ff')]]
 specs=[x for x in specs if x[0]!='regular']
 regular_bug()
 for ident,filename,color in specs:
  im=Image.open(base/filename).convert('RGBA');a=np.array(im);rgb=np.array(tuple(bytes.fromhex(color)));edge=np.round(rgb*.65).astype('uint8')
  primary=np.all(a[:,:,:3]==rgb,2);outline=np.all(a[:,:,:3]==edge,2)
  gray=np.full(a.shape[:2],255,'uint8');gray[outline]=166
  save(rgba(np.repeat(gray[:,:,None],3,2),a[:,:,3]),f'bugs/{ident}-main')
  save(rgba(a[:,:,:3],a[:,:,3]*(~primary&~outline)),f'bugs/{ident}-fixed')
  records.append({'id':'bugs/'+ident,'source':str((base/filename).relative_to(ROOT)),'size':list(im.size),'primaryPixels':int(primary.sum()),'outlinePixels':int(outline.sum())})

def regular_bug():
 """5.5 px field bugs: shell + one highlight + same-hue outline only (no eyes, antennae or legs).
 Drawn at 4x then reduced to 24x22 so the shell slightly overlaps its 5.5x5 cell and hides the soil."""
 W,H,S=24,22,4
 yy,xx=np.indices((H*S,W*S)).astype(float)
 cx,cy=W*S/2-.5,H*S/2+S*.6
 # Rounded dome: superellipse, flatter at the bottom.
 nx=(xx-cx)/(W*S/2-S*.4);ny=(yy-cy)/(H*S/2-S*.2)
 shell=(np.abs(nx)**2.6+np.abs(ny)**(2.6 if True else 2))<=1
 edge=shell&~(cv2.erode(shell.astype('uint8'),np.ones((2*S+1,2*S+1),'uint8'))>0)
 seam=shell&~edge&(np.abs(xx-cx)<S*.55)&(yy<cy+H*S*.18)
 gray=np.where(edge,150,np.where(seam,205,255)).astype('uint8')
 main=Image.fromarray(np.dstack([gray,gray,gray,shell*255]).astype('uint8'),'RGBA').resize((W,H),Image.Resampling.LANCZOS)
 hl=((xx-(cx-W*S*.2))/(S*2.6))**2+((yy-(cy-H*S*.24))/(S*1.7))**2<=1
 fixed=Image.fromarray(np.dstack([np.full(hl.shape,255),np.full(hl.shape,255),np.full(hl.shape,255),hl*235]).astype('uint8'),'RGBA').resize((W,H),Image.Resampling.LANCZOS)
 save(main,'bugs/regular-main');save(fixed,'bugs/regular-fixed')
 # White bugs: cool gray rim on top of the (white) tinted shell, highlight unchanged.
 rim=edge*255
 white=Image.fromarray(np.dstack([np.full(hl.shape,158),np.full(hl.shape,164),np.full(hl.shape,172),rim]).astype('uint8'),'RGBA').resize((W,H),Image.Resampling.LANCZOS)
 save(white,'bugs/regular-white-fixed')
 records.append({'id':'bugs/regular','source':'procedural shell (UI spec v1.1 carryover): shell, seam, highlight, same-hue rim','size':[W,H]})

def remove_border(im):
 """Key the neutral sheet surround by connected color distance, retaining leaf decorations."""
 a=np.array(im.convert('RGB'));lab=cv2.cvtColor(a,cv2.COLOR_RGB2LAB).astype(float)
 corners=np.array([lab[0,0],lab[0,-1],lab[-1,0],lab[-1,-1]])
 distance=np.min(np.linalg.norm(lab[:,:,None,:]-corners[None,None,:,:],axis=3),axis=2)
 near=(distance<24).astype('uint8');_,labels=cv2.connectedComponents(near,8)
 ids=np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]]));ids=ids[ids!=0]
 alpha=np.where(np.isin(labels,ids),0,255).astype('uint8')
 return rgba(a,alpha)

def scene_assets():
 ui=Image.open(SOURCE/'battle-scene-r1/ui-elements-sheet.png')
 battle=Image.open(SOURCE/'battle-scene-r1/battle-empty.png')
 # All silhouettes and wood/ceramic textures below are cuts from the authored source sheets.
 specs={
  'plot':((191,52,432,280),(134,136)), 'plot-locked':((487,52,730,280),(134,136)),
  'plot-ready':((783,52,1038,280),(134,136)),
  'fence':((172,343,725,442),(540,44)), 'fence-hurt':((813,316,1359,442),(540,58)),
  'button-shuffle':((106,492,505,637),(312,114)), 'button-designate':((562,492,974,637),(312,114)),
  'button-water':((1030,492,1429,637),(312,114)),
  'button-shuffle-pressed':((106,679,505,823),(312,114)), 'button-designate-pressed':((562,679,974,823),(312,114)),
  'button-water-pressed':((1030,679,1429,823),(312,114)),
  'pause':((46,854,175,982),(96,96)), 'level':((203,871,512,976),(252,76)),
  'progress':((548,869,1120,915),(320,30)), 'prop-slot':((1157,863,1317,979),(96,72)),
 }
 for name,(box,size) in specs.items():
  piece=ui.crop(box)
  if name=='level':
   # Retain frame, replace baked "第4关" using clean inner wood sampled from the same plaque.
   a=np.array(piece);mask=np.zeros(a.shape[:2],'uint8');mask[20:83,74:268]=255
   a=cv2.inpaint(a,mask,9,cv2.INPAINT_TELEA);piece=Image.fromarray(a)
  if name.startswith('button'):
   # Keep baked artwork/text together; runtime only adds counts and cancellation overlay.
   pass
  tile=remove_border(piece).resize(size,Image.Resampling.LANCZOS);save(tile,'scene/'+name)
  records.append({'id':'scene/'+name,'source':'Art/Concepts/battle-scene-r1/ui-elements-sheet.png','crop':box,'size':size})
 # The full battle sheet contains the authored dense run of pickets, rather than
 # stretching the seven-picket inventory segment across the whole battlefield.
 rail=np.array(battle.crop((0,1120,1080,1184)))
 distance=np.minimum(np.linalg.norm(rail.astype(float)-[163,149,129],axis=2),np.linalg.norm(rail.astype(float)-[147,169,126],axis=2))
 save(rgba(rail,np.clip((distance-4)/8,0,1)*255).resize((540,32),Image.Resampling.LANCZOS),'scene/fence')
 records.append({'id':'scene/fence','source':'Art/Concepts/battle-scene-r1/battle-empty.png','crop':[0,1120,1080,1184],'size':[540,32]})
 # Top board has no baked text after the source's center label is covered by clean wood.
 board=battle.crop((0,0,1080,170)).resize((540,85),Image.Resampling.LANCZOS)
 a=np.array(board);clean=np.array(board.crop((220,65,419,77)).resize((540,85),Image.Resampling.LANCZOS));
 # Use the source wood as a base; individual original parts are drawn separately over it.
 save(Image.fromarray(clean),'scene/topbar')
 # Low-contrast soil uses deterministic broad grain plus sparse pores; all channels shift equally.
 # Half-resolution (270x240, stretched at runtime) so the 1.5x plant canvases fit the single 2048 atlas.
 rng=np.random.default_rng(130026);noise=rng.normal(0,1,(240,270)).astype('float32')
 grain=cv2.GaussianBlur(noise,(0,0),1.5);grain/=max(float(grain.std()),1e-6)
 pores=cv2.GaussianBlur((rng.random((240,270))>.992).astype('float32'),(0,0),.5)*-10
 delta=np.clip(np.rint(grain*.65+pores),-2,2).astype('int16')
 soil=np.clip(np.array([163,149,129])[None,None,:]+delta[:,:,None],0,255).astype('uint8')
 save(Image.fromarray(soil).convert('RGBA'),'scene/background')
 sys.path.insert(0,str(SOURCE/'color-check-r1/scripts'));from color import rgb2lab,de2000
 colors=np.unique(soil.reshape(-1,3),axis=0);diffs=[de2000(rgb2lab([163,149,129]),rgb2lab(c)) for c in colors]
 assert max(diffs)<3
 records.append({'id':'scene/background','source':'battle-empty.png soil #A39581; deterministic microtexture','uniqueColors':colors.tolist(),'maxDeltaE2000':max(diffs),'maxRgbChannelDeviation':2})

def main():
 (REVIEW/'previews').mkdir(parents=True,exist_ok=True)
 plant_assets();bug_assets();scene_assets()
 slots=sorted(str(p.relative_to(OUT).with_suffix('')) for p in OUT.rglob('*.png'))
 (ROOT/'scripts/r1/art-slots.json').write_text(json.dumps({'slots':slots},indent=2)+'\n')
 (REVIEW/'extraction.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
 hashes={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob('*.png')}
 (REVIEW/'asset-hashes.json').write_text(json.dumps(hashes,indent=2)+'\n')
 print(json.dumps({'slots':len(slots),'recordedCrops':len(records)}))
if __name__=='__main__':main()
