from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import json, subprocess, sys, hashlib
import numpy as np

B=Path(__file__).resolve().parents[1]
roster=json.loads((B/'roster.json').read_text())
ids=sys.argv[1:]
records=[]
for a in roster:
 id=a['creatureId']
 if ids and id not in ids:continue
 masters=sorted((B/'masters').glob(f'{id}-{a["hex"]}-cheer-v*.png'))
 if not masters:continue
 master=masters[-1];small=B/'qa'/f'{id}-generated-224.png'
 subprocess.run(['sips','--resampleHeightWidth','256','224',str(master),'--out',str(small)],check=True,capture_output=True)
 orig=np.array(Image.open(B/'references'/a['reference']).convert('RGBA'));gen=np.array(Image.open(small).convert('RGBA'))
 def bounds(p):
  ys,xs=np.where(p[:,:,3]>=32);bottom=int(ys.max());sel=(p[:,:,3]>=128)&(np.arange(256)[:,None]>=bottom-45)
  yy,xx=np.where(sel);return (int(xx.min()),int(xx.max()),bottom,int(ys.min()))
 ob,gb=bounds(orig),bounds(gen)
 scale=(ob[1]-ob[0]+1)/(gb[1]-gb[0]+1);ox=(ob[0]+ob[1])/2;gx=(gb[0]+gb[1])/2
 # Keep generated ears/limbs inside the frame when registering the pot anchor.
 ys,xs=np.where(gen[:,:,3]>=16)
 scale=min(scale,(ob[2]-6)/max(1,gb[2]-ys.min()),(ox-6)/max(1,gx-xs.min()),(217-ox)/max(1,xs.max()-gx))
 dx=ox-gx*scale;dy=ob[2]-gb[2]*scale
 curve=json.loads((B/'qa/masks'/f'{id}-cutline.json').read_text())
 mask=np.arange(256)[:,None]>=np.array(curve)[None,:]
 # Old lowered wings/flippers are toy pixels, not part of the locked base.
 yy_grid,xx_grid=np.indices((256,224))
 mask[(xx_grid<3)|(xx_grid>220)|(yy_grid<3)|(yy_grid>252)]=False
 if id in ['charcoal','violet']:
  left=[58,54,48,45,44] if id=='charcoal' else [59,55,49,47,45]
  right=[172,175,180,183,184] if id=='charcoal' else [168,170,175,179,180]
  left=np.interp(yy_grid,[138,140,145,150,155],left);right=np.interp(yy_grid,[138,140,145,150,155],right)
  old_wing=(yy_grid>=138)&(yy_grid<=155)&((xx_grid<left)|(xx_grid>right))
  q=orig.astype(float)
  visible_leaf=(q[:,:,1]>q[:,:,0]*1.15)&(q[:,:,1]>q[:,:,2]*1.3)&(q[:,:,3]>0)
  mask[old_wing & ~visible_leaf]=False
 if id=='cyan':
  q=orig.astype(float)
  old_flipper=(yy_grid>=115)&(yy_grid<145)&(q[:,:,2]>q[:,:,0]*1.1)&(q[:,:,2]>q[:,:,1]*.65)&(q[:,:,3]>0)
  hand_bounds=(((xx_grid-70)/25)**2+((yy_grid-128)/17)**2<1)|(((xx_grid-155)/26)**2+((yy_grid-128)/17)**2<1)
  old_flipper &= hand_bounds
  # The former flippers' contact fringe/shadow belongs to the occlusion.
  # Repair it from ImageGen v02, retaining the unaffected visible leaf pixels.
  occlusion=np.array(Image.fromarray((old_flipper*255).astype('uint8')).filter(ImageFilter.MaxFilter(5)))>0
  mask[occlusion]=False
  Image.fromarray((occlusion*255).astype('uint8')).save(B/'qa/masks/cyan-occlusion-repair.png')
 Image.fromarray((mask*255).astype('uint8')).save(B/'qa/masks'/f'{id}-protected.png')
 shifts=np.zeros(224)
 if id not in ['green','lime','mint','forest','pale-mint','cyan']:
  q=gen.astype(float);gy,gx0=np.where((q[:,:,1]>q[:,:,0]*1.15)&(q[:,:,1]>q[:,:,2]*1.3)&(q[:,:,3]>128))
  for px,py in zip(gx0*scale+dx,gy*scale+dy):
   ix=int(round(px))
   if 0<=ix<224 and 100<py<curve[ix]:shifts[ix]=max(shifts[ix],curve[ix]-py+1)
  shifts=np.convolve(np.maximum.reduce([np.roll(shifts,-1),shifts,np.roll(shifts,1)]),np.ones(5)/5,mode='same')
 cfg={'id':id,'scale':scale,'dx':dx,'dy':dy,'cutline':curve,'leaf_alignment_offsets':shifts.tolist(),'protection_mask_path':str(B/'qa/masks'/f'{id}-protected.png'),'source':a['reference'],'master':str(master.relative_to(B))}
 cfgpath=B/'qa'/f'{id}-registration.json';cfgpath.write_text(json.dumps(cfg,indent=2))
 target=B/a['delivery'];target.parent.mkdir(parents=True,exist_ok=True)
 subprocess.run(['swift',str(B/'qa/pack_cheer.swift'),str(B/'references'/a['reference']),str(small),str(cfgpath),str(target)],check=True,capture_output=True)
 final=np.array(Image.open(target).convert('RGBA'))
 diff=np.any(final!=orig,axis=2);assert not np.any(diff&mask)
 edge=np.concatenate([final[0,:,3],final[-1,:,3],final[:,0,3],final[:,-1,3]])
 assert edge.max()==0,(id,int(edge.max()))
 yy,xx=np.where(final[:,:,3]>=16)
 assert yy.min()>=4 and xx.min()>=4 and xx.max()<=219,(id,xx.min(),yy.min(),xx.max())
 report={'id':id,'name':a['name'],'delivery':a['delivery'],'size':[224,256],'master':str(master.relative_to(B)),
 'protected_region_pixels':int(mask.sum()),'protected_region_changed_pixels':int((diff&mask).sum()),
 'visible_bbox_alpha16':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],'outer_edge_max_alpha':int(edge.max()),
 'registration':str(cfgpath.relative_to(B)),'protection_mask':f'qa/masks/{id}-protected.png',
 'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
 (B/'qa'/f'{id}-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');records.append(report)
 qa=Image.new('RGB',(936,580),'#eeeeee');draw=ImageDraw.Draw(qa)
 for row,bg in enumerate(['#FFFFFF','#E8F2FF']):
  for col,(label,p) in enumerate([('IDLE',orig),('CHEER',final)]):
   cell=Image.new('RGBA',(448,256),bg);im=Image.fromarray(p);cell.alpha_composite(im,(112,0));qa.paste(cell.convert('RGB'),(col*468+10,row*290+28));draw.text((col*468+20,row*290+8),id+' / '+label+' / '+bg,fill='black')
 qa.save(B/'qa'/f'{id}-idle-vs-cheer.png')
print(json.dumps([{'id':r['id'],'protected_diff':r['protected_region_changed_pixels'],'bbox':r['visible_bbox_alpha16']} for r in records],ensure_ascii=False))
