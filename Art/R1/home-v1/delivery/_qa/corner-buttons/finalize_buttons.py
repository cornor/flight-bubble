"""Size/alpha normalization and requested preview compositing; no repainting."""
from pathlib import Path
import json,hashlib,cv2,numpy as np
from PIL import Image,ImageDraw
Q=Path(__file__).resolve().parent;D=Q.parent.parent;H=D.parent
records={};buttons={}
items={'settings':'H11-attempt-02.png','codex':'H12-attempt-01.png'}
for name,original in items.items():
 im=Image.open(D/'_originals'/original).convert('RGBA');a=np.array(im)
 n,labels,stats,_=cv2.connectedComponentsWithStats((a[:,:,3]>16).astype(np.uint8),8)
 k=1+int(np.argmax(stats[1:,cv2.CC_STAT_AREA]));support=cv2.dilate((labels==k).astype(np.uint8),np.ones((3,3),np.uint8))
 discarded=int(np.count_nonzero((support==0)&(a[:,:,3]>0)))
 a[support==0]=0;a[a[:,:,3]<3]=0
 # Generated solid button interiors carry a near-opaque 252-254 alpha.
 # Restore solid UI opacity while preserving the antialiased perimeter.
 interior=cv2.distanceTransform((a[:,:,3]>16).astype(np.uint8),cv2.DIST_L2,5)>3
 a[:,:,3][interior&(a[:,:,3]>=250)]=255
 im=Image.fromarray(a);box=im.getchannel('A').getbbox();cropped=im.crop(box)
 # Premultiplied-alpha resampling avoids adding black/white edge fringes.
 fitted=cropped.convert('RGBa').resize((240,240),Image.Resampling.LANCZOS).convert('RGBA')
 out=Image.new('RGBA',(256,256));out.alpha_composite(fitted,(8,8));a=np.array(out);a[a[:,:,3]<2]=0;a[a[:,:,3]==0,:3]=0
 out=Image.fromarray(a);out.save(D/f'btn-{name}.png');buttons[name]=out
 yy,xx=np.where(a[:,:,3]>0);bbox=[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)]
 records[name]={'source':original,'sourceSize':im.size,'sourceCrop':box,'outputSize':[256,256],'alphaBounds':bbox,'transparentMargins':[bbox[0],bbox[1],256-bbox[2],256-bbox[3]],'discardedOutsideAlphaPixels':discarded,'transparentPixels':int((a[:,:,3]==0).sum()),'partialAlphaPixels':int(((a[:,:,3]>0)&(a[:,:,3]<255)).sum()),'alphaRange':[int(a[:,:,3].min()),int(a[:,:,3].max())]}

home=Image.open(H/'mock/01-home-mock.png').convert('RGBA')
positions={'settings':(24,28),'codex':(944,28)}
for name,pos in positions.items():home.alpha_composite(buttons[name].resize((112,112),Image.Resampling.LANCZOS),pos)
home.convert('RGB').save(D/'_qa/corner-buttons-on-home.png')
small=Image.new('RGBA',(160,80),'#E7F5FF')
for name,pos in [('settings',(16,12)),('codex',(88,12))]:small.alpha_composite(buttons[name].resize((56,56),Image.Resampling.LANCZOS),pos)
small.convert('RGB').save(D/'_qa/corner-buttons-56.png')

edge=Image.new('RGBA',(576,576),'#E7F5FF')
for row,bg in enumerate(['#FFFFFF','#233B60']):
 for col,name in enumerate(['settings','codex']):
  tile=Image.new('RGBA',(272,272),bg);tile.alpha_composite(buttons[name],(8,8));edge.alpha_composite(tile,(8+col*288,8+row*288))
edge.convert('RGB').save(Q/'alpha-on-light-dark.png')
records['preview']={'base':'mock/01-home-mock.png','size':[1080,1920],'buttonImageSize':[112,112],'positions':positions,'logicalSizeAt2x':[56,56],'logoAdded':False,'labelsAdded':False}
refs=json.loads((Q/'reference-hashes.json').read_text())
for r in refs:r['unchanged']=hashlib.sha256(Path(r['path']).read_bytes()).hexdigest()==r['sha256']
assert all(r['unchanged'] for r in refs)
records['references']=refs
for name in buttons:
 assert records[name]['alphaBounds']==[8,8,248,248]
 assert records[name]['alphaRange']==[0,255]
(Q/'delivery-audit.json').write_text(json.dumps(records,indent=2)+'\n')
print(json.dumps(records,indent=2))
