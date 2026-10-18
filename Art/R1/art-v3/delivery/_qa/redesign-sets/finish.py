from pathlib import Path
import sys,json,hashlib,shutil,importlib.util
import cv2,numpy as np
from PIL import Image,ImageDraw,ImageFont
Q=Path(__file__).resolve().parent;D=Q.parent.parent
sys.path.insert(0,str(D/'_scripts'))
from process_creatures import clean_cell,resize
from finish_round2 import detect_eyes
spec=importlib.util.spec_from_file_location('proposal_finish',Q.parent/'redesign/finish_redesign.py');pr=importlib.util.module_from_spec(spec);spec.loader.exec_module(pr)
STATES=['idle','attack','spent','blink','cheer']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read():return json.loads((Q/'config.json').read_text())
def save(c):(Q/'config.json').write_text(json.dumps(c,indent=2)+'\n')
def place(im):
 a=np.array(im);a[a[:,:,3]<3]=0;im=Image.fromarray(a);b=im.getchannel('A').getbbox();im=im.crop(b)
 out=Image.new('RGBA',(224,256));x=(224-im.width)//2;y=213-im.height
 assert x>=0 and y>=30,(im.size,x,y)
 out.alpha_composite(im,(x,y));return out,(x,y,b)
def sample(im,mask,hx):
 a=np.array(im);ls=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
 valid=cv2.erode(mask.astype(np.uint8),np.ones((3,3),np.uint8))>0
 yy,xx=np.indices(valid.shape);b=im.getchannel('A').getbbox();valid&=(yy>b[1]+(b[3]-b[1])*.28)&(yy<b[3]-5)
 coords=np.argwhere(valid);assert len(coords)>15,len(coords)
 coords=coords[np.argsort(ls[:,:,0][valid])];out=[]
 for q,label in [(.5,'midtone'),(.7,'lit-body-excluding-specular')]:
  y,x=coords[round((len(coords)-1)*q)];v=np.rint(np.median(a[y-1:y+2,x-1:x+2,:3],axis=(0,1))).astype(np.uint8)
  de=float(np.linalg.norm(pr.lab(v)-pr.lab(pr.rgb(hx))))
  out.append(dict(region=label,xy=[int(x),int(y)],rgb=v.tolist(),hex='#'+''.join(f'{n:02X}' for n in v),deltaE76=round(de,4),passed=de<=8))
 return out
def meta(path):
 im=Image.open(path);a=np.array(im);b=im.getchannel('A').getbbox()
 return dict(path=str(path),sha256=sha(path),size=list(im.size),mode=im.mode,bounds=list(b),width=b[2]-b[0],height=b[3]-b[1],centerX=(b[0]+b[2])/2,lowestPixelY=b[3]-1,below212Empty=not bool(a[213:,:,3].any()))
def process(ident,state):
 c=read();it=c['characters'][ident];attempt=it['attempts'][state][-1];source=Path(attempt['original'])
 crop,bb=clean_cell(Image.open(source).convert('RGBA'),state=='spent')
 # All colour adjustment is to generated paint; outer outline and pale accents preserved.
 calibrated,mask,color=pr.calibrate(crop,dict(id=ident,key=ident+'-'+state,hex=it['hex']))
 desired={'idle':144,'attack':139,'spent':134,'cheer':150}[state]
 if state=='cheer':
  b=Image.open(Q/'staging'/ident/'idle.png').getchannel('A').getbbox();scale=(b[2]-b[0])/crop.width*it.get('cheerScaleAdjustment',1)
 else:scale=min(desired/crop.height,178/crop.width)
 size=(round(crop.width*scale),round(crop.height*scale));small=resize(calibrated,size)
 sm=np.array(Image.fromarray(mask.astype(np.uint8)*255).resize(size,Image.Resampling.NEAREST))>0
 out,(x,y,trim)=place(small);sm=sm[trim[1]:trim[3],trim[0]:trim[2]];fm=np.zeros((256,224),bool);fm[y:y+sm.shape[0],x:x+sm.shape[1]]=sm
 dest=Q/'staging'/ident/f'{state}.png';dest.parent.mkdir(parents=True,exist_ok=True);out.save(dest)
 calibrated.save(Q/f'{ident}-{state}-calibrated-source.png');Image.fromarray(fm.astype(np.uint8)*255).save(Q/f'{ident}-{state}-mask.png')
 row=meta(dest);row.update(source=str(source),attempt=attempt['attempt'],sourceBounds=bb,uniformScale=scale,colorCalibration=color,samples=sample(out,fm,it['hex']))
 assert all(p['passed'] for p in row['samples']),(ident,state,row['samples'])
 assert row['lowestPixelY']==212 and row['below212Empty'] and abs(row['centerX']-112)<=.5
 if state in ['idle','attack','spent']:assert 120<=row['height']<=150 and row['width']<=180,row
 if state=='cheer':
  idle=it['outputs']['idle'];row['widthDifferencePercent']=round(abs(row['width']/idle['width']-1)*100,4);assert row['widthDifferencePercent']<=5
 it.setdefault('outputs',{})[state]=row
 if state=='idle':it['eyeBoxes']=detect_eyes(out)
 save(c);return row
def blink(ident):
 c=read();it=c['characters'][ident];at=it['attempts']['blink'][-1]
 idle=Image.open(Q/'staging'/ident/'idle.png').convert('RGBA');a=np.array(idle);bb=idle.getchannel('A').getbbox()
 crop,srcbb=clean_cell(Image.open(at['original']),False)
 aligned=Image.new('RGBA',idle.size);aligned.alpha_composite(resize(crop,(bb[2]-bb[0],bb[3]-bb[1])),bb[:2]);s=np.array(aligned)
 out=a.copy();allowed=np.zeros((256,224),bool);rects=[]
 for eye_index,bx in enumerate(it['eyeBoxes']):
  x0,y0,x1,y1=bx;x0-=4;y0-=4;x1+=4;y1+=4;rects.append([x0,y0,x1,y1]);allowed[y0:y1,x0:x1]=True
  base=a[y0:y1,x0:x1,:3].astype(float)
  if 'blinkSourceRectangles' in it:
   sr=it['blinkSourceRectangles'][eye_index];patch=np.array(aligned.crop(tuple(sr)).resize((x1-x0,y1-y0),Image.Resampling.LANCZOS))[:,:,:3].astype(float)
  else:patch=s[y0:y1,x0:x1,:3].astype(float)
  h,w=patch.shape[:2];yy,xx=np.indices((h,w));dist=np.minimum.reduce([xx,yy,w-1-xx,h-1-yy]).astype(float)
  ring=dist<=1;delta=np.median((base-patch)[ring],axis=0);patch=np.clip(patch+delta,0,255);weight=np.clip(dist/2,0,1);weight=weight*weight*(3-2*weight)
  out[y0:y1,x0:x1,:3]=np.rint(base*(1-weight[:,:,None])+patch*weight[:,:,None]).astype(np.uint8)
 for x0,y0,x1,y1 in it.get('blinkPreserveRectangles',[]):out[y0:y1,x0:x1]=a[y0:y1,x0:x1]
 out[:,:,3]=a[:,:,3];out[a[:,:,3]==0]=0;diff=np.any(out!=a,axis=2);assert not (diff&~allowed).any()
 dest=Q/'staging'/ident/'blink.png';im=Image.fromarray(out);im.save(dest);aligned.save(Q/f'{ident}-blink-aligned.png');Image.fromarray(diff.astype(np.uint8)*255).save(Q/f'{ident}-blink-diff.png')
 yy,xx=np.where(diff);row=meta(dest);row.update(source=at['original'],attempt=at['attempt'],eyeBoxes=it['eyeBoxes'],allowedRectangles=rects,changedPixels=int(diff.sum()),changedBounds=[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],outsideEyeRectanglesChanged=0,alphaIdentical=bool(np.array_equal(a[:,:,3],out[:,:,3])),samples=it['outputs']['idle']['samples'])
 row['sourceEyeRectangles']=it.get('blinkSourceRectangles',rects);row['preservedRectangles']=it.get('blinkPreserveRectangles',[])
 it['outputs']['blink']=row;save(c);return row
def proof(names,dest=None):
 can=Image.new('RGB',(1180,320*len(names)),'#E8EDF3');dr=ImageDraw.Draw(can);font=ImageFont.truetype('/System/Library/Fonts/STHeiti Light.ttc',16)
 for row,ident in enumerate(names):
  y=row*320;dr.text((10,y+5),ident,font=font,fill='#27364C')
  for j,state in enumerate(STATES):
   p=Q/'staging'/ident/f'{state}.png'
   if not p.exists():continue
   im=Image.open(p);x=8+j*234
   dr.rectangle((x,y+30,x+223,y+285),fill='#17213A')
   can.paste(im,(x,y+30),im);dr.line((x,y+243,x+223,y+243),fill='#64738A');dr.text((x+70,y+292),state,font=font,fill='#27364C')
 dest=Path(dest) if dest else Q/('-'.join(names)+'-states.png');can.save(dest);return str(dest)
def accept(ident):
 c=read();it=c['characters'][ident];assert set(it['outputs'])==set(STATES)
 archive=D/'_originals'/f'old-{ident}';archive.mkdir(exist_ok=True)
 baseline=json.loads((Q/'protected-baseline.json').read_text());records=[]
 for state in STATES:
  dst=Path(it['outputDir'])/f'{state}.png';old=archive/f'{state}.png';src=Q/'staging'/ident/f'{state}.png'
  assert dst.exists() and sha(dst)==baseline[str(dst)] and not old.exists(),(dst,old)
  shutil.move(str(dst),old);assert sha(old)==baseline[str(dst)];shutil.copy2(src,dst)
  records.append(dict(original=str(dst),archive=str(old),oldSha256=sha(old),newSha256=sha(dst)))
  it['outputs'][state]['deliveryPath']=str(dst)
 it['archive']=records;it['status']='passed and delivered';save(c);return records
if __name__=='__main__':
 op=sys.argv[1]
 if op=='process':r=process(sys.argv[2],sys.argv[3])
 elif op=='blink':r=blink(sys.argv[2])
 elif op=='proof':r=proof(sys.argv[2:])
 elif op=='accept':r=accept(sys.argv[2])
 print(json.dumps(r,indent=2))
