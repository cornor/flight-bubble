"""Postprocess generated R1 v3 round-nine art, with traceable bounds and color QA."""
from pathlib import Path
import sys,json,hashlib
import cv2,numpy as np
from PIL import Image,ImageDraw,ImageFont
from process_creatures import clean_cell,resize
B=Path(__file__).resolve().parents[1];Q=B/'_qa';P=B/'third-batch-round2-prompts.json'

def rgb(h):return np.array([int(h[i:i+2],16) for i in (0,2,4)],np.float32)
def lab(c):return cv2.cvtColor(np.array(c,dtype=np.float32).reshape(1,1,3)/255,cv2.COLOR_RGB2LAB)[0,0]
def meta(path):
 im=Image.open(path).convert('RGBA');a=np.array(im)[:,:,3];box=im.getbbox()
 return dict(path=str(path),size=list(im.size),mode='RGBA',bbox=list(box),baseline_y=box[3]-1,center_x=(box[0]+box[2])/2,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),transparent_pixels=int((a==0).sum()),partial_alpha_pixels=int(((a>0)&(a<255)).sum()))
def body_width(im,band=(.60,.87)):
 a=np.array(im)[:,:,3];b=im.getbbox();lo=round(b[1]+(b[3]-b[1])*band[0]);hi=round(b[1]+(b[3]-b[1])*band[1]);sp=[]
 for row in a[lo:hi]>16:
  x=np.where(row)[0];sp.append(int(x[-1]-x[0]+1) if len(x) else 0)
 return max(sp),[lo,hi]
def color(im,h,ident,state):
 a=np.array(im);old=a[:,:,:3].copy();sv=cv2.cvtColor(old,cv2.COLOR_RGB2HSV).astype(float);ls=cv2.cvtColor(old.astype(np.float32)/255,cv2.COLOR_RGB2LAB);target=rgb(h);tl=lab(target)
 th=float(cv2.cvtColor(target.astype(np.uint8).reshape(1,1,3),cv2.COLOR_RGB2HSV)[0,0,0]);dh=abs(sv[:,:,0]-th);dh=np.minimum(dh,180-dh)
 dist=cv2.distanceTransform((a[:,:,3]>200).astype(np.uint8),cv2.DIST_L2,5)
 candidate=(dist>2.5)&(dh<24)&(sv[:,:,1]>14)&(ls[:,:,0]>max(13,tl[0]-30))&(ls[:,:,0]<min(96,tl[0]+27))
 if ident=='forest':candidate&=ls[:,:,0]<58
 n,ll,ss,_=cv2.connectedComponentsWithStats(candidate.astype(np.uint8),8)
 keep=[j for j in range(1,n) if ss[j,4]>max(25,ss[1:,4].max()*.05)]
 mask=np.isin(ll,keep)
 if mask.sum()<80:raise ValueError(('body mask too small',ident,state,int(mask.sum())))
 anchor=np.percentile(ls[mask],55,axis=0)
 mapped=tl+(ls-anchor)*np.array([.58,.30,.30],np.float32)
 new=np.clip(cv2.cvtColor(mapped.astype(np.float32),cv2.COLOR_LAB2RGB)*255,0,255)
 # Feather only into body color; outer three pixels are never recolored.
 md=cv2.distanceTransform(mask.astype(np.uint8),cv2.DIST_L2,5);w=np.clip(md/1.8,0,1)
 a[:,:,:3]=np.rint(old*(1-w[:,:,None])+new*w[:,:,None]).astype(np.uint8);a[a[:,:,3]==0]=0
 return Image.fromarray(a),mask,dict(anchor_lab=anchor.tolist(),changed_pixels=int(np.any(old!=a[:,:,:3],2).sum()),outer_outline_preserved=True)
def sample_colors(im,mask,h):
 a=np.array(im);ls=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB);valid=cv2.erode(mask.astype(np.uint8),np.ones((5,5),np.uint8))>0
 yy,xx=np.indices(valid.shape);box=im.getbbox();valid&=(yy>=box[1]+(box[3]-box[1])*.45)&(yy<box[3]-7)
 coords=np.argwhere(valid);assert len(coords)>15
 coords=coords[np.argsort(ls[:,:,0][valid])];out=[]
 for quantile,label in [(.5,'身体中间调'),(.7,'身体较亮部')]:
  y,x=coords[round((len(coords)-1)*quantile)];c=np.rint(np.median(a[y-1:y+2,x-1:x+2,:3],axis=(0,1))).astype(np.uint8);de=float(np.linalg.norm(lab(c)-lab(rgb(h))))
  out.append(dict(label=label,xy=[int(x),int(y)],rgb=c.tolist(),hex='#'+''.join(f'{v:02X}' for v in c),delta_e76=round(de,3),passed=de<=8))
 return out
def place(im,mask=None):
 a=np.array(im);a[a[:,:,3]<3]=0;im=Image.fromarray(a);bb=im.getbbox();im=im.crop(bb)
 if mask is not None:mask=mask[bb[1]:bb[3],bb[0]:bb[2]]
 out=Image.new('RGBA',(224,256));x=(224-im.width)//2;y=213-im.height;out.alpha_composite(im,(x,y));full=np.zeros((256,224),bool)
 if mask is not None:full[y:y+im.height,x:x+im.width]=mask
 return out,full
def save_state(item,state,out,mask,detail):
 dest=Path(item['output_dir'])/f'{state}.png';dest.parent.mkdir(parents=True,exist_ok=True);out.save(dest);Image.fromarray(mask.astype(np.uint8)*255).save(Q/f"r2-mask-{item['character']}-{state}.png")
 info=meta(dest);info['samples']=sample_colors(out,mask,item['color']);info['postprocess']=detail
 b=info['bbox'];assert info['size']==[224,256] and b[3]==213 and not np.array(out)[213:,:,3].any()
 assert abs(info['center_x']-112)<=.5
 assert all(v['passed'] for v in info['samples']),info['samples']
 return info
def detect_eyes(im):
 a=np.array(im);r=a[:,:,:3];b=im.getbbox();yy,xx=np.indices(a.shape[:2]);m=(r.min(2)>220)&((r.max(2).astype(int)-r.min(2))<38)&(a[:,:,3]>220)
 m&=(yy>b[1]+(b[3]-b[1])*.08)&(yy<b[1]+(b[3]-b[1])*.67)&(xx>b[0]+(b[2]-b[0])*.15)&(xx<b[2]-(b[2]-b[0])*.15)
 _,ll,ss,_=cv2.connectedComponentsWithStats(m.astype(np.uint8),8)
 ids=[j for j in range(1,len(ss)) if ss[j,4]>=12 and ss[j,2]>=5 and ss[j,3]>=5]
 ids=sorted(ids,key=lambda j:ss[j,4],reverse=True)[:2];assert len(ids)==2,ss.tolist()
 boxes=[]
 for j in sorted(ids,key=lambda j:ss[j,0]):
  x,y,w,h,_=map(int,ss[j]);boxes.append([x-1,y-2,x+w+1,y+h+1])
 return boxes
def sheet(item,at):
 src=Image.open(at['archive_path']).convert('RGBA');a=np.array(src)[:,:,3]
 _,ll,ss,_=cv2.connectedComponentsWithStats((a>100).astype(np.uint8),8);ids=sorted(range(1,len(ss)),key=lambda j:ss[j,4],reverse=True)[:3];subjects=sorted([ss[j] for j in ids],key=lambda s:s[0]);cuts=[0]+[round((subjects[k][0]+subjects[k][2]+subjects[k+1][0])/2) for k in range(2)]+[src.width]
 cells=[];bounds=[]
 for i in range(3):
  cell,bb=clean_cell(src.crop((cuts[i],0,cuts[i+1],src.height)),True);cells.append(cell);bounds.append([cuts[i],0,cuts[i+1],src.height,bb])
 scale=min(148/max(c.height for c in cells),178/max(c.width for c in cells));sizes=[(round(c.width*scale),round(c.height*scale)) for c in cells]
 if min(s[1] for s in sizes)<120:raise ValueError(('common-scale dimensions cannot fit',item['character'],sizes))
 outputs={}
 for i,state in enumerate(['idle','attack','spent']):
  im=resize(cells[i],sizes[i]);im,mask,cc=color(im,item['color'],item['character'],state);out,mask=place(im,mask)
  outputs[state]=save_state(item,state,out,mask,dict(shared_scale=scale,source_cell=bounds[i],scaled_size=list(sizes[i]),color=cc))
 idle=Image.open(Path(item['output_dir'])/'idle.png').convert('RGBA');item['eye_boxes']=detect_eyes(idle);item['outputs']=outputs;item['selected_attempt']=at['attempt'];item['status']='three_states_ready_for_emotes'
 return outputs
def blink(item,at,data):
 sh=next(i for i in data['items'] if i['character']==item['character'] and i['pose']=='sheet');rects=sh['eye_boxes'];idle=Image.open(Path(item['output_dir'])/'idle.png').convert('RGBA');old=np.array(idle);b=idle.getbbox()
 gen,bb=clean_cell(Image.open(at['archive_path']),False);aligned=Image.new('RGBA',idle.size);aligned.alpha_composite(resize(gen,(b[2]-b[0],b[3]-b[1])),b[:2]);src=np.array(aligned);out=old.copy();allowed=np.zeros((256,224),bool);patches=[]
 if item.get('clean_generated_brow_region'):
  x0,y0,x1,y1=item['clean_generated_brow_region'];m=np.zeros((256,224),np.uint8);r=src[y0:y1,x0:x1,:3];m[y0:y1,x0:x1]=((r[:,:,0]<135)&(r[:,:,1]<105)&(r[:,:,2]<220)).astype(np.uint8)*255
  m=cv2.dilate(m,np.ones((3,3),np.uint8));src[:,:,:3]=cv2.inpaint(src[:,:,:3],m,3,cv2.INPAINT_TELEA)
 if item.get('inpaint_generated_rectangles'):
  m=np.zeros((256,224),np.uint8)
  for x0,y0,x1,y1 in item['inpaint_generated_rectangles']:m[y0:y1,x0:x1]=255
  src[:,:,:3]=cv2.inpaint(src[:,:,:3],m,3,cv2.INPAINT_TELEA)
 for x0,y0,x1,y1 in rects:
  x0-=4;y0-=4;x1+=4;y1+=4;patches.append([x0,y0,x1,y1]);allowed[y0:y1,x0:x1]=True
  base=old[y0:y1,x0:x1,:3].astype(float);patch=src[y0:y1,x0:x1,:3].astype(float);h,w=patch.shape[:2];yy,xx=np.indices((h,w));dist=np.minimum.reduce([yy,xx,h-1-yy,w-1-xx]).astype(float)
  ring=dist<=1;delta=np.median((base-patch)[ring],axis=0);patch=np.clip(patch+delta,0,255);weight=np.clip(dist/3,0,1);weight=weight**2*(3-2*weight)
  out[y0:y1,x0:x1,:3]=np.rint(base*(1-weight[:,:,None])+patch*weight[:,:,None]).astype(np.uint8)
 for x0,y0,x1,y1 in item.get('preserve_rectangles',[]):out[y0:y1,x0:x1]=old[y0:y1,x0:x1]
 out[:,:,3]=old[:,:,3];diff=np.any(out!=old,axis=2);assert not (diff&~allowed).any();ys,xs=np.where(diff);assert len(xs)>20
 mask=np.array(Image.open(Q/f"r2-mask-{item['character']}-idle.png"))>0
 detail=dict(eye_boxes=rects,patch_rectangles=patches,margin_px=4,changed_pixels=int(diff.sum()),changed_bbox_xywh=[int(xs.min()),int(ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)],outside_eye_rectangles_changed=0,alpha_identical=bool(np.array_equal(out[:,:,3],old[:,:,3])),source_alignment_bbox=bb,preserved_brow_rectangles=item.get('preserve_rectangles',[]),generated_brow_cleanup=item.get('clean_generated_brow_region'),generated_rectangles_inpainted=item.get('inpaint_generated_rectangles',[]))
 im=Image.fromarray(out);result=save_state(item,'blink',im,mask,detail)
 aligned.save(Q/f"r2-{item['character']}-blink-aligned.png");Image.fromarray((diff.astype(np.uint8)*255)).save(Q/f"r2-{item['character']}-blink-diff.png")
 item['outputs']={'blink':result};item['selected_attempt']=at['attempt'];item['status']='finished_pending_visual_review';return result
def cheer(item,at):
 idle=Image.open(Path(item['output_dir'])/'idle.png').convert('RGBA');gen,bb=clean_cell(Image.open(at['archive_path']),False)
 band=tuple(item.get('body_band',[.60,.87]));wanted,ib=body_width(idle,band);sw,sb=body_width(gen,band)
 scale=wanted/sw
 if round(gen.height*scale)>183 or round(gen.width*scale)>210:raise ValueError(('cheer proportion cannot fit without changing body scale',item['character'],gen.size,scale))
 im=resize(gen,(round(gen.width*scale),round(gen.height*scale)));im,mask,cc=color(im,item['color'],item['character'],'cheer');out,mask=place(im,mask);actual,cb=body_width(out,band);assert abs(actual/wanted-1)<=.05
 result=save_state(item,'cheer',out,mask,dict(source_bbox=bb,uniform_scale=scale,idle_body_width=wanted,cheer_body_width=actual,body_width_ratio=actual/wanted,idle_y_band=ib,cheer_y_band=cb,fraction_band=list(band),color=cc));assert result['bbox'][1]>=30 and result['bbox'][2]-result['bbox'][0]<=210
 item['outputs']={'cheer':result};item['selected_attempt']=at['attempt'];item['status']='finished_pending_visual_review';return result
def proof(data,characters):
 canvas=Image.new('RGB',(1180,300*len(characters)),'#E8EDF3');dr=ImageDraw.Draw(canvas);ft=ImageFont.truetype('/System/Library/Fonts/STHeiti Light.ttc',16)
 for row,name in enumerate(characters):
  sh=next(i for i in data['items'] if i['character']==name and i['pose']=='sheet');dr.text((12,row*300+8),f'{name} #{sh["color"]}',font=ft,fill='#27364C')
  for col,state in enumerate(['idle','attack','spent','blink','cheer']):
   f=Path(sh['output_dir'])/f'{state}.png'
   if not f.exists():continue
   im=Image.open(f);x=8+col*234;y=row*300+26;canvas.paste(im,(x,y),im);dr.line((x,y+213,x+224,y+213),fill='#AFBDCC');dr.text((x+70,y+255),state,font=ft,fill='#27364C')
 dest=Q/('r2-'+('-'.join(characters))+'-states.png');canvas.save(dest);return str(dest)
if __name__=='__main__':
 data=json.loads(P.read_text());keys=sys.argv[1:];names=[]
 for key in keys:
  item=next(i for i in data['items'] if i['key']==key);at=item['attempts'][-1]
  res=sheet(item,at) if item['pose']=='sheet' else blink(item,at,data) if item['pose']=='blink' else cheer(item,at)
  if item['character'] not in names:names.append(item['character'])
  P.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(key=key,result=res),ensure_ascii=False))
 print(proof(data,names))
