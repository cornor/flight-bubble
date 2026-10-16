"""Deterministic, authorized finishing of selected generated originals."""
from pathlib import Path
import json
import numpy as np,cv2
from PIL import Image
from process_assets import meta,resize_alpha,contacts
R=Path(__file__).resolve().parents[1]
p=json.loads((R/'prompts.json').read_text())
a=p['assets'][0];src=Path(a['attempts'][1]['archive_path'])
rgb=np.array(Image.open(src).convert('RGB'));out=rgb.copy();h,w=rgb.shape[:2]
# Boundary-connected flat gray is the presentation background, not white character fill.
delta=np.linalg.norm(rgb.astype(float)-np.array([238,241,245]),axis=2)
allowed=(delta<23).astype('uint8')
_,lab,_,_=cv2.connectedComponentsWithStats(allowed,8)
ids=np.unique(np.concatenate([lab[0],lab[-1],lab[:,0],lab[:,-1]]));ids=ids[ids!=0]
bg=np.isin(lab,ids);out[bg]=[238,241,245]
colors=[('blue','#2982F4'),('orange','#FF7E27'),('green','#62E18E'),('purple','#BD80FF'),('yellow','#FFDF48'),('white','#FFFFFF'),('red','#FF525B'),('charcoal','#2D2D2D')]
sample_points=[[(220,400),(130,350)],[(570,400),(500,350)],[(1000,365),(1020,250)],[(1320,390),(1210,320)],[(220,820),(110,750)],[(590,830),(520,760)],[(960,800),(1040,750)],[(1335,830),(1240,795)]]
ops=[]
for i,(name,hx) in enumerate(colors):
 x0=(i%4)*384;x1=x0+384;y0=(i//4)*512;y1=y0+512
 src=rgb[y0:y1,x0:x1];dst=out[y0:y1,x0:x1]
 hsv=cv2.cvtColor(src,cv2.COLOR_RGB2HSV).astype(float)
 target=np.array([int(hx[k:k+2],16) for k in [1,3,5]],float)
 th=cv2.cvtColor(target.astype('uint8').reshape(1,1,3),cv2.COLOR_RGB2HSV)[0,0].astype(float)
 nonbg=~bg[y0:y1,x0:x1]
 if name not in ['white','charcoal']:
  dh=np.abs(hsv[:,:,0]-th[0]);dh=np.minimum(dh,180-dh)
  # Main body hue range, retaining small white speculars, eyes, mouths and cold outlines.
  mask=(dh<20)&(hsv[:,:,1]>75)&(hsv[:,:,2]>100)&nonbg
  src_lab=cv2.cvtColor(src.astype('float32')/255,cv2.COLOR_RGB2LAB)
  target_lab=cv2.cvtColor((target.astype('float32')/255).reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
  anchors=[]
  for gx,gy in sample_points[i]:
   patch=src_lab[gy-y0-4:gy-y0+5,gx-x0-4:gx-x0+5]
   anchors.append(np.median(patch.reshape(-1,3),axis=0))
  anchor=np.mean(anchors,axis=0)
  new_lab=src_lab.copy()
  # Translate the palette while retaining most of the generated shading contrast.
  # Luminance, rather than HSV value, preserves volume on saturated blue/orange bodies.
  new_lab[:]=target_lab+(src_lab-anchor)*np.array([0.78,0.40,0.40],dtype='float32')
  new=np.clip(cv2.cvtColor(new_lab.astype('float32'),cv2.COLOR_LAB2RGB)*255,0,255)
  weight=np.minimum(1,(hsv[:,:,1]-75)/35)*np.minimum(1,(20-dh)/4)
  weight=np.clip(weight,0,1)*mask
  dst[:]=np.rint(dst*(1-weight[:,:,None])+new*weight[:,:,None]).astype('uint8')
 elif name=='white':
  mi=src.min(2);ma=src.max(2)
  mask=nonbg&(mi>208)&((ma-mi)<35)
  # Neutral cool white broad body midtones; darker outlines and shaded contour remain intact.
  vals=cv2.cvtColor(src.astype('float32')/255,cv2.COLOR_RGB2LAB)
  vals[:,:,0]=np.minimum(100,vals[:,:,0]+2.2); vals[:,:,1:]*=0.5
  new=np.clip(cv2.cvtColor(vals,cv2.COLOR_LAB2RGB)*255,0,255)
  dst[mask]=new[mask].round().astype('uint8')
 else:
  ma=src.max(2);mi=src.min(2)
  mask=nonbg&(ma>=25)&(ma<100)&((ma-mi)<30)
  new=src.mean(2)*0.82
  dst[mask]=np.repeat(new[:,:,None],3,2)[mask].round().astype('uint8')
 ops.append({'character':name,'target_hex':hx,'changed_pixels':int(np.any(dst!=src,2).sum())})
bounds=[]
for i in range(8):
 x0=(i%4)*384;y0=(i//4)*512;cell=out[y0:y0+512,x0:x0+384]
 mask=(np.linalg.norm(cell.astype(float)-np.array([238,241,245]),axis=2)>24).astype('uint8')
 _,labels,stats,_=cv2.connectedComponentsWithStats(mask,8)
 ix=1+int(np.argmax(stats[1:,cv2.CC_STAT_AREA]));bx,by,bw,bh,_=stats[ix]
 bounds.append((int(bx),int(by),int(bw),int(bh)))
canvas=Image.new('RGB',(1536,1024),(238,241,245));offsets=[]
for i,(bx,by,bw,bh) in enumerate(bounds):
 x0=(i%4)*384;y0=(i//4)*512
 baseline=max(b[1]+b[3]-1 for b in bounds[(i//4)*4:(i//4)*4+4])
 dx=int(round(192-(bx+bw/2)));dy=baseline-(by+bh-1)
 left=max(0,x0+bx-6);top=max(0,y0+by-6);right=min(1536,x0+bx+bw+6);bottom=min(1024,y0+by+bh+6)
 cell=Image.fromarray(out[top:bottom,left:right]);canvas.paste(cell,(left+dx,top+dy))
 offsets.append({'character':colors[i][0],'dx':dx,'dy':dy,'baseline_y':y0+baseline})
canvas.save(a['output_path'])
a['postprocess']=[x for x in a['postprocess'] if 'color calibration' not in x['method']]
a['postprocess'].append({'method':'main-body color calibration in CIELAB, preserving 78% luminance and 40% chroma variation within body-hue masks; neutral cool white and charcoal correction; small specular highlights and face details retained','background':'#EEF1F5','characters':ops,'anchor_points_native':sample_points})
a['lineup_alignment']={'method':'translate complete character cells on flat gray, center each column and align feet within each row','offsets':offsets}
a['file_metadata']=meta(Path(a['output_path']))
# Move the generated far-edge islands into the required 58–72% vertical band.
a=p['assets'][1];src=np.array(Image.open(a['attempts'][1]['archive_path']).convert('RGB'))
sh,sw=src.shape[:2]
source_y=np.array([0,230,835,990,sh-1],float)
dest_y=np.array([0,230,0.58*(sh-1),0.715*(sh-1),sh-1],float)
yy=np.interp(np.arange(sh),dest_y,source_y).astype('float32')
mx=np.broadcast_to(np.arange(sw,dtype='float32')[None,:],(sh,sw));my=np.broadcast_to(yy[:,None],(sh,sw))
mapped=cv2.remap(src,mx,my,cv2.INTER_CUBIC,borderMode=cv2.BORDER_REPLICATE)
im=Image.fromarray(mapped).resize((1080,1920),Image.Resampling.LANCZOS);im.save(a['output_path'])
a['postprocess']=[x for x in a['postprocess'] if 'vertical remap' not in x['method']]
a['postprocess'].append({'method':'continuous vertical remap of generated background to place edge islands at 58–71.5%, then exact resize; no scene objects drawn','source_y':source_y.tolist(),'destination_y':dest_y.tolist()})
a['file_metadata']=meta(Path(a['output_path']))
(R/'prompts.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
print(contacts(p['assets']))

