from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
qa=Path(__file__).resolve().parent
delivery=qa.parent.parent
assets=[]
for p in sorted((delivery/'ui4').glob('*.png')):
 im=Image.open(p);a=np.array(im);expected=(360,360) if p.stem=='panel-card' else (128,128)
 assert im.size==expected,(p.name,im.size)
 assert im.mode=='RGBA',(p.name,im.mode)
 assert np.max(a[0,:,3])==0 and np.max(a[-1,:,3])==0 and np.max(a[:,0,3])==0 and np.max(a[:,-1,3])==0,p.name
 assert np.max(a[:,:,3])>=250
 yy,xx=np.where(a[:,:,3]>=8)
 item=dict(file=str(p.relative_to(delivery)),size=list(im.size),mode=im.mode,alpha_bbox_8=[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],transparent_edge=True,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
 if p.stem=='panel-card':
  item['face_samples_top_middle_bottom']=[a[y,180,:].tolist() for y in [50,180,310]]
  # Verify 48 px source cut lines cross the fully straight opaque side at all four corners.
  extents=[]
  for y in [48,64,180,295,311]:
   xs=np.where(a[y,:,3]>=240)[0];extents.append([y,int(xs.min()),int(xs.max())])
  item['opaque_horizontal_extents']=extents
  assert max(e[1] for e in extents)-min(e[1] for e in extents)<=1
  assert max(e[2] for e in extents)-min(e[2] for e in extents)<=1
  item['nine_slice_source_cut_px']=48
 assets.append(item)
assert len(assets)==5
result=dict(status='passed',assets=assets,visual_review=['Subjects match the brief: music note, speaker plus two waves, four-point sparkle, frame containing five colored bubbles.','No baked text on any final asset.','Shared white top-left highlights and darker same-hue outlines.','128 px and 32 px icons reviewed on light and dark backgrounds.','Card face reviewed as creamy white to very pale blue, thin cyan rim, faint inner white highlight, soft contained shadow.','Nine-slice proofs generated using the existing 0.75 atlas scale, 36 px sprite border and 24 px displayed cap; 440x402 and 440x370 reviewed.'],limitations=['Art export and offline preview only; no Unity build or live-game screenshot produced.'])
(qa/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(result,ensure_ascii=False,indent=2))
