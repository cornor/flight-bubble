"""Extract visible source pixels and alpha guides without moving artwork."""
from pathlib import Path
import json, cv2, numpy as np
from PIL import Image, ImageDraw

Q=Path(__file__).resolve().parent
H=Q.parent.parent.parent
source=np.array(Image.open(H/'mock/01-home-mock.png').convert('RGB'))
specs={
 'blue':([345,845,738,1280],[94,125],95),
 'orange':([200,570,565,920],[0,25],95),
 'yellow':([553,666,858,975],[19,42],85),
 'green':([105,910,420,1230],[48,94],80),
 'purple':([675,845,975,1240],[123,168],70),
}
sheet=Image.new('RGB',(1500,460),'#263C5D');d=ImageDraw.Draw(sheet);records={}
union=np.zeros(source.shape[:2],np.uint8)
for i,(name,(box,hs,smin)) in enumerate(specs.items()):
 x0,y0,x1,y1=box;roi=source[y0:y1,x0:x1].copy();hsv=cv2.cvtColor(roi,cv2.COLOR_RGB2HSV)
 seed=((hsv[:,:,0]>=hs[0])&(hsv[:,:,0]<=hs[1])&(hsv[:,:,1]>=smin)&(hsv[:,:,2]>38)).astype(np.uint8)*255
 seed=cv2.morphologyEx(seed,cv2.MORPH_CLOSE,np.ones((3,3),np.uint8))
 contours,_=cv2.findContours(seed,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
 fill=np.zeros_like(seed)
 c=max(contours,key=cv2.contourArea);cv2.drawContours(fill,[c],-1,255,-1)
 if name=='yellow':
  cv2.ellipse(fill,(767-x0,777-y0),(25,28),0,0,360,255,-1)
 trimap=np.zeros_like(seed);dilate=cv2.dilate(fill,np.ones((7,7),np.uint8));erode=cv2.erode(fill,np.ones((5,5),np.uint8))
 trimap[dilate>0]=cv2.GC_PR_BGD;trimap[fill>0]=cv2.GC_PR_FGD;trimap[erode>0]=cv2.GC_FGD
 cv2.grabCut(roi,trimap,None,np.zeros((1,65),np.float64),np.zeros((1,65),np.float64),4,cv2.GC_INIT_WITH_MASK)
 binary=np.where((trimap==cv2.GC_FGD)|(trimap==cv2.GC_PR_FGD),255,0).astype(np.uint8)
 # Keep only body-sized components; shadows/other color species are outside seeds.
 n,labels,stats,_=cv2.connectedComponentsWithStats(binary,8)
 binary=np.where(np.isin(labels,[j for j in range(1,n) if stats[j,cv2.CC_STAT_AREA]>300]),255,0).astype(np.uint8)
 alpha=cv2.GaussianBlur(binary,(3,3),.42);alpha[alpha<3]=0;alpha[alpha>252]=255
 full=np.zeros(source.shape[:2],np.uint8);full[y0:y1,x0:x1]=alpha
 Image.fromarray(full).save(Q/f'{name}-visible-mask.png')
 union=np.maximum(union,full)
 rgba=np.dstack([source,full]);rgba[full==0,:3]=0
 out=Image.fromarray(rgba,'RGBA');out.save(Q/f'{name}-visible-source.png')
 thumb=out.crop(box);thumb.thumbnail((280,400),Image.Resampling.LANCZOS);sheet.paste(thumb,(i*300+10,40),thumb);d.text((i*300+12,12),name+' visible source',fill='white')
 yy,xx=np.where(full>0);records[name]={'roi':box,'visibleAlphaBox':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],'sourcePixelCount':int(np.count_nonzero(full==255))}

# Yellow's semitransparent soap bubble and its neck are handled separately.
bub=Image.new('L',(1080,1920));d=ImageDraw.Draw(bub)
d.ellipse((803,640,949,783),fill=255)
d.polygon([(771,826),(788,795),(840,748),(856,758),(812,811),(788,832)],fill=255)
bubble=np.array(bub);bubble=cv2.GaussianBlur(bubble,(3,3),.5)
Image.fromarray(bubble).save(Q/'yellow-bubble-support.png')
union=np.maximum(union,bubble);Image.fromarray(union).save(Q/'all-visible-support.png')
sheet.save(Q/'visible-extraction-check.png')
(Q/'visible-extraction.json').write_text(json.dumps(records,indent=2)+'\n')
print(json.dumps(records,indent=2))
