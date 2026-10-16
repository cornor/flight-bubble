from pathlib import Path
import json,numpy as np,cv2
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];P=json.loads((R/'prompts.json').read_text())
F=Image.Resampling.LANCZOS
def sprite(i):return Image.open(P['assets'][i]['output_path']).convert('RGBA')
def nine(im,size,b=32):
 w,h=im.size;tw,th=size;out=Image.new('RGBA',size)
 xs=[0,b,w-b,w];ys=[0,b,h-b,h];dx=[0,b,tw-b,tw];dy=[0,b,th-b,th]
 for yy in range(3):
  for xx in range(3):
   crop=im.crop((xs[xx],ys[yy],xs[xx+1],ys[yy+1]))
   crop=crop.resize((dx[xx+1]-dx[xx],dy[yy+1]-dy[yy]),F)
   out.paste(crop,(dx[xx],dy[yy]))
 return out
# Character family readability at exactly 40px body height on light/dark-ish gray.
line=Image.open(P['assets'][0]['output_path']).convert('RGB');arr=np.array(line)
proof=Image.new('RGB',(800,190),'#EEF1F5');d=ImageDraw.Draw(proof)
d.rectangle((0,95,799,189),fill='#8C919C')
for i in range(8):
 x0=(i%4)*384;y0=(i//4)*512;cell=arr[y0:y0+512,x0:x0+384]
 mask=(np.linalg.norm(cell.astype(float)-np.array([238,241,245]),axis=2)>24).astype('uint8')
 n,l,s,_=cv2.connectedComponentsWithStats(mask,8);k=1+int(np.argmax(s[1:,cv2.CC_STAT_AREA]));x,y,w,h,_=s[k]
 crop=line.crop((x0+x-4,y0+y-4,x0+x+w+4,y0+y+h+4)).convert('RGBA');a=np.array(crop)
 delta=np.linalg.norm(a[:,:,:3].astype(float)-np.array([238,241,245]),axis=2)
 allowed=(delta<22).astype('uint8');_,labels,_,_=cv2.connectedComponentsWithStats(allowed,8)
 ids=np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]]));ids=ids[ids!=0]
 a[np.isin(labels,ids),3]=0;crop=Image.fromarray(a)
 crop=crop.resize((round(crop.width*40/h),round(crop.height*40/h)),F)
 for row in range(2):
  proof.paste(crop,(i*100+(100-crop.width)//2,row*95+30),crop)
 d.text((i*100+25,9),['blue','orange','green','purple','yellow','white','red','charcoal'][i],fill='#23406B')
proof.save(R/'_qa/lineup-40px.png')

proof=Image.new('RGBA',(960,700),'#8C919C');d=ImageDraw.Draw(proof)
d.text((20,14),'Queue: original 192x360 / 9-slice 288x540; border 32px',fill='white')
proof.alpha_composite(sprite(14),(20,50));proof.alpha_composite(nine(sprite(14),(288,540)),(242,50))
d.text((560,14),'Pad bases & overhead swirl flattened to 0.45',fill='white')
proof.alpha_composite(sprite(8),(580,170));proof.alpha_composite(sprite(9),(760,100))
sw=sprite(10).resize((112,50),F);proof.alpha_composite(sw,(594,165))
d.text((560,270),'Bottom buttons at actual 155x72 game size',fill='white')
for j,i in enumerate([16,17,18]): proof.alpha_composite(sprite(i).resize((155,72),F),(570,300+j*90))
d.text((20,630),'Empty frame with generated fill: 38% and 100%',fill='white')
frame=sprite(6).resize((276,28),F);fill=sprite(7).resize((264,18),F)
for j,pc in enumerate([.38,1]):
 x=20+j*310;proof.alpha_composite(frame,(x,656));proof.alpha_composite(fill.crop((0,0,round(264*pc),18)),(x+6,661))
proof.convert('RGB').save(R/'_qa/ui-proof.png')
print('Saved lineup-40px and ui-proof')

