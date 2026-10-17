"""Paste only generated eye edits; assert every outside RGBA pixel unchanged."""
from pathlib import Path
import json,cv2,numpy as np
from PIL import Image,ImageDraw
Q=Path(__file__).resolve().parent;D=Q.parent.parent
faces=json.loads((Q/'H8-face-crops.json').read_text())
sheet=Image.open(D/'_originals/H8-attempt-01.png').convert('RGB').resize((960,640),Image.Resampling.LANCZOS)
rois={
 'blue':[[453,947,548,1042],[565,932,651,1022]],
 'orange':[[328,651,400,716],[424,657,485,719]],
 'yellow':[[738,749,797,812]],
 'green':[[224,985,302,1060],[316,966,378,1032]],
 'purple':[[741,979,822,1045],[832,999,908,1082]],
}
qa=Image.new('RGB',(1500,680),'#e8eef7');draw=ImageDraw.Draw(qa);records={}
for i,(c,face) in enumerate(faces.items()):
 x0,y0,x1,y1=face;w,h=x1-x0,y1-y0
 base=np.array(Image.open(D/f'squad-{c}.png').convert('RGBA'));out=base.copy()
 tile=sheet.crop(((i%3)*320+10,(i//3)*320+10,(i%3)*320+310,(i//3)*320+310)).resize((w,h),Image.Resampling.LANCZOS)
 patch=np.zeros_like(base[:,:,:3]);patch[y0:y1,x0:x1]=np.array(tile)
 mask=np.zeros((1920,1080),np.uint8);rectangles=[]
 for rx0,ry0,rx1,ry1 in rois[c]:
  roi=base[ry0:ry1,rx0:rx1,:3];hsv=cv2.cvtColor(roi,cv2.COLOR_RGB2HSV)
  white=(((hsv[:,:,1]<70)&(hsv[:,:,2]>170))|(hsv[:,:,2]<185)).astype(np.uint8)*255
  contours,_=cv2.findContours(white,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
  contour=cv2.convexHull(max(contours,key=cv2.contourArea))
  # Fill the eye white contour (encloses its pupil), preserve external cheek.
  local=np.zeros_like(white);cv2.drawContours(local,[contour],-1,255,-1)
  bx,by,bw,bh=cv2.boundingRect(contour)
  eye=np.zeros_like(mask);eye[ry0:ry1,rx0:rx1]=local
  eye=cv2.dilate(eye,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(9,9)))
  eye=cv2.GaussianBlur(eye,(3,3),.5)
  # The detection includes white, pupil and outline; expand by exactly 4 px.
  rect=[rx0+bx-4,ry0+by-4,rx0+bx+bw+4,ry0+by+bh+4]
  allowed=np.zeros_like(mask);allowed[rect[1]:rect[3],rect[0]:rect[2]]=255
  eye=np.minimum(eye,allowed);mask=np.maximum(mask,eye);rectangles.append(rect)
 f=mask[:,:,None]/255.
 out[:,:,:3]=np.uint8(np.rint(base[:,:,:3]*(1-f)+patch*f))
 out[:,:,3]=base[:,:,3]
 diff=np.any(out!=base,axis=2);yy,xx=np.where(diff)
 assert not np.any(diff[mask==0]); assert np.array_equal(out[:,:,3],base[:,:,3])
 Image.fromarray(out).save(D/f'squad-{c}-blink.png')
 Image.fromarray(mask).save(Q/f'{c}-blink-edit-mask.png')
 for row,array in enumerate([base,out]):
  im=Image.fromarray(array).crop(face).resize((280,280),Image.Resampling.LANCZOS)
  qa.paste(im,(i*300+10,row*330+35),im)
 draw.text((i*300+12,12),c+' open / blink',fill='#23406b')
 records[c]={'eyeRectanglesInclusiveExclusive':rectangles,'differentPixelCount':int(diff.sum()),'differenceBounds':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],'outsideEyeRectangleDifferentPixels':0,'alphaIdentical':True}
qa.save(Q/'blink-face-check.png');(Q/'blink-differences.json').write_text(json.dumps(records,indent=2)+'\n')
print(json.dumps(records,indent=2))
