"""Register generated completion crops to immutable approved source pixels."""
from pathlib import Path
import cv2, json, numpy as np
from PIL import Image, ImageDraw
Q=Path(__file__).resolve().parent; D=Q.parent.parent
boxes=json.loads((Q/'crop-boxes.json').read_text())
records={}; sheet=Image.new('RGB',(1800,900),'#283E5B'); draw=ImageDraw.Draw(sheet)
for i,c in enumerate(['blue','orange','yellow','green','purple']):
 box=boxes[c]; x0,y0,x1,y1=box; w,h=x1-x0,y1-y0
 src=np.array(Image.open(Q/f'{c}-source-crop.png').convert('RGB'))
 vis=np.array(Image.open(Q/f'{c}-visible-mask.png'))[y0:y1,x0:x1]
 gen=np.array(Image.open(D/f'_originals/H{i+3}-attempt-01.png').convert('RGBA').resize((w,h),Image.Resampling.LANCZOS))
 # Remove tiny isolated alpha noise without changing the principal cutout.
 n,lab,stats,_=cv2.connectedComponentsWithStats((gen[:,:,3]>20).astype(np.uint8),8)
 keep=np.isin(lab,[j for j in range(1,n) if stats[j,cv2.CC_STAT_AREA]>50])
 gen[:,:,3][~cv2.dilate(keep.astype(np.uint8),np.ones((3,3),np.uint8)).astype(bool)]=0
 sift=cv2.SIFT_create(nfeatures=3000,contrastThreshold=.015)
 ka,da=sift.detectAndCompute(cv2.cvtColor(gen[:,:,:3],cv2.COLOR_RGB2GRAY),(gen[:,:,3]>220).astype(np.uint8)*255)
 kb,db=sift.detectAndCompute(cv2.cvtColor(src,cv2.COLOR_RGB2GRAY),(vis>245).astype(np.uint8)*255)
 pairs=cv2.BFMatcher().knnMatch(da,db,k=2)
 good=[a for a,b in pairs if a.distance<.76*b.distance]
 p=np.float32([ka[m.queryIdx].pt for m in good]);t=np.float32([kb[m.trainIdx].pt for m in good])
 mat,inliers=cv2.estimateAffinePartial2D(p,t,method=cv2.RANSAC,ransacReprojThreshold=3,maxIters=5000)
 if mat is None: raise RuntimeError(c+' registration failed')
 # Preserve source pixel coordinates; only generated hidden material is warped.
 fullmat=mat.copy();fullmat[:,2]+=[x0,y0]
 warped=cv2.warpAffine(gen,fullmat,(1080,1920),flags=cv2.INTER_LANCZOS4,borderValue=(0,0,0,0))
 warped[warped[:,:,3]==0,:3]=0
 Image.fromarray(warped).save(Q/f'{c}-completion-registered.png')
 local=Image.fromarray(warped).crop(box)
 tile=Image.new('RGBA',(w,h),'#283E5B');tile.alpha_composite(local)
 original=Image.open(Q/f'{c}-visible-source.png').crop(box)
 blend=Image.blend(tile,Image.alpha_composite(Image.new('RGBA',(w,h),'#283E5B'),original),.5)
 for row,im in enumerate([tile,blend]):
  im.thumbnail((340,390),Image.Resampling.LANCZOS);sheet.paste(im,(i*360+10,row*450+40))
 draw.text((i*360+10,15),c+' registered / 50% overlay',fill='white')
 err=np.linalg.norm((p@mat[:,:2].T+mat[:,2])-t,axis=1)[inliers.ravel().astype(bool)]
 records[c]={'crop':box,'matrixToFullFrame':fullmat.tolist(),'matches':len(good),'inliers':int(inliers.sum()),'medianFeatureErrorPx':float(np.median(err))}
(Q/'registration.json').write_text(json.dumps(records,indent=2)+'\n')
sheet.save(Q/'registration-check.png')
print(json.dumps(records,indent=2))
