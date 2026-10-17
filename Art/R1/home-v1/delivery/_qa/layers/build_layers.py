"""Deterministic registration, matte cleanup, reconstruction and composition.

Visible interiors come from H1 unchanged. Generated art supplies only covered
parts, the approved green correction, and newly exposed background.
"""
from pathlib import Path
import cv2,json,numpy as np
from PIL import Image,ImageDraw,ImageFilter
Q=Path(__file__).resolve().parent;D=Q.parent.parent;H=D.parent
src=np.array(Image.open(H/'mock/01-home-mock.png').convert('RGB'))
names=['blue','orange','yellow','green','purple']
mask={c:np.array(Image.open(Q/f'{c}-visible-mask.png')) for c in names}
# Sky visible in the gap between blue's feet is not part of the hero.
hsv=cv2.cvtColor(src,cv2.COLOR_RGB2HSV)
m=mask['blue'];gap=m[1240:1276,450:620].copy();gap[hsv[1240:1276,450:620,1]<160]=0
gap=cv2.GaussianBlur(gap,(3,3),.42);m[1240:1276,450:620]=gap
mask['blue']=m
for c in names: Image.fromarray(mask[c]).save(Q/f'{c}-clean-visible-mask.png')

# Replace the characters, soap bubble and old cast shadows, preserving the rest.
repair=np.array(Image.open(Q/'all-visible-support.png'))
shadow=Image.fromarray(repair);dd=ImageDraw.Draw(shadow)
for box in [(390,1240,704,1301),(171,1196,408,1242),(691,1203,930,1263)]:dd.ellipse(box,fill=255)
repair=cv2.dilate(np.array(shadow),cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(81,81)))
repair[repair>0]=255
repair[540:760,:180]=0  # Protect the approved distant left island.
generated=np.array(Image.open(Q/'H2-generated-core.png').convert('RGB'))
x,y,w,h=cv2.boundingRect(repair)
core=cv2.seamlessClone(generated.copy(),src.copy(),repair.copy(),(x+w//2,y+h//2),cv2.NORMAL_CLONE)
feather=np.minimum(cv2.distanceTransform(repair,cv2.DIST_L2,5)/12,1)[:,:,None]
core=np.uint8(np.rint(src*(1-feather)+core*feather))
Image.fromarray(repair).save(Q/'background-repair-mask.png')
Image.fromarray(core).save(Q/'background-core.png')
extended=np.array(Image.open(Q/'H2-extended-generated.png').convert('RGB'))
# Match the generated extension through a narrow soft-sky/cloud seam; this
# changes only the first/last 80 rows of the core, without moving its scene.
for y in range(80):
 t=(y/79)**2*(3-2*y/79)
 core[y]=np.uint8(np.rint(core[y]*t+extended[y+210]*(1-t)))
 core[1919-y]=np.uint8(np.rint(core[1919-y]*t+extended[2129-y]*(1-t)))
Image.fromarray(core).save(Q/'background-core.png')
extended[210:2130]=core;Image.fromarray(extended).save(D/'background.png')

occluders={'blue':[],'orange':['blue'],'yellow':['blue','purple'],'green':['blue'],'purple':['blue','yellow']}
records={}; outputs={}
for c in names:
 original=np.dstack([src,mask[c]])
 gen=np.array(Image.open(Q/f'{c}-completion-registered.png').convert('RGBA'))
 # Green lower body in the generated crop was shorter; align the unseen portion
 # to the original feet while keeping the source face entirely unchanged.
 if c=='green':
  yy,xx=np.indices((1920,1080),dtype=np.float32)
  offset=np.clip((yy-1080)/125,0,1)*20
  gen=cv2.remap(gen,xx,yy-offset,cv2.INTER_CUBIC,borderMode=cv2.BORDER_CONSTANT)
 cover=np.zeros((1920,1080),np.uint8)
 for front in occluders[c]:cover=np.maximum(cover,mask[front])
 cover=cv2.dilate(cover,np.ones((7,7),np.uint8))
 # Restrict generated completion to truly occluded regions. Visible original
 # pixels are laid over it again at unmodified integer x/y coordinates.
 # Blend across the occlusion seam only; the rest of the visible body stays
 # untouched. A 9 px transition removes source antialiasing from the occluder.
 blend=np.maximum(cv2.GaussianBlur(cv2.dilate(cover,np.ones((5,5),np.uint8)),(19,19),4)/255.,cover/255.)
 gen[:,:,3]=gen[:,:,3]*(blend>0).astype(np.uint8)
 original[:,:,3]=np.uint8(np.rint(original[:,:,3]*(1-blend)))
 base=Image.fromarray(gen)
 base.alpha_composite(Image.fromarray(original))
 out=np.array(base)
 if c=='green':
  complete=np.array(Image.open(Q/'green-completion-registered.png').convert('RGBA'))
  yy,xx=np.indices((1920,1080),dtype=np.float32)
  complete=cv2.remap(complete,xx,yy-np.clip((yy-1080)/125,0,1)*20,cv2.INTER_CUBIC,borderMode=cv2.BORDER_CONSTANT)
  # Replace only the lower-back edge, erasing the approved extra fourth bump.
  correction=np.zeros((1920,1080),np.float32)
  correction[1100:1171,110:177]=1
  correction=cv2.GaussianBlur(correction,(13,13),2)
  aa=out[:,:,3:4]/255.;bb=complete[:,:,3:4]/255.;t=correction[:,:,None]
  oa=aa*(1-t)+bb*t
  rgb=(out[:,:,:3]*aa*(1-t)+complete[:,:,:3]*bb*t)/np.maximum(oa,.0001)
  out=np.dstack([np.uint8(np.rint(rgb)),np.uint8(np.rint(oa[:,:,0]*255))])
  Image.fromarray(np.uint8(correction*255)).save(Q/'green-correction-mask.png')
 if c=='yellow':
  support=np.array(Image.open(Q/'yellow-bubble-support.png'))/255.
  support*=1-mask[c]/255.
  B=core.astype(float); I=src.astype(float)
  needed=np.maximum(np.maximum((B-I)/np.maximum(B,1),(I-B)/np.maximum(255-B,1)).max(axis=2),0)
  alpha=np.clip(np.maximum(cv2.GaussianBlur(needed,(19,19),3)+.15,needed+.015),.62,1)*support
  C=np.clip((I-(1-alpha[:,:,None])*B)/np.maximum(alpha[:,:,None],.001),0,255)
  bubble=np.dstack([np.rint(C).astype(np.uint8),np.rint(alpha*255).astype(np.uint8)])
  # Preserve the bubble's observed RGB contribution while removing the sky.
  b=Image.fromarray(bubble);b.alpha_composite(Image.fromarray(out));out=np.array(b)
 # Remove alpha noise and normalize invisible RGB for exact blink comparisons.
 out[out[:,:,3]<3]=0
 out[out[:,:,3]==0,:3]=0
 Image.fromarray(out).save(D/f'squad-{c}.png');outputs[c]=Image.fromarray(out)
 yy,xx=np.where(out[:,:,3]>0)
 kept=(mask[c]==255)&(out[:,:,3]==255)&np.all(out[:,:,:3]==src,axis=2)
 records[c]={'alphaBounds':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],'unchangedOpaqueSourcePixels':int(kept.sum()),'opaqueSourcePixels':int((mask[c]==255).sum())}

compose=Image.fromarray(core).convert('RGBA')
for c in ['orange','yellow','green','purple','blue']:compose.alpha_composite(outputs[c])
compose.convert('RGB').save(D/'_qa/compose.png')
sheet=Image.new('RGB',(1800,500),'#263C5D');dd=ImageDraw.Draw(sheet)
boxes=json.loads((Q/'crop-boxes.json').read_text())
for i,c in enumerate(names):
 im=outputs[c].crop(boxes[c]);im.thumbnail((345,440),Image.Resampling.LANCZOS);sheet.paste(im,(i*360+5,40),im);dd.text((i*360+10,12),c,fill='white')
sheet.save(Q/'character-layers-check.png')
(Q/'layer-metrics.json').write_text(json.dumps(records,indent=2)+'\n')
print(json.dumps(records,indent=2))
