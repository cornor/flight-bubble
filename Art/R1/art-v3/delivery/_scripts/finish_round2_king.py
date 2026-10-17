from pathlib import Path
import json,sys,hashlib
import cv2,numpy as np
from PIL import Image,ImageDraw
from process_creatures import clean_cell,resize
from finish_round2 import meta
B=Path(__file__).resolve().parents[1];P=B/'third-batch-round2-prompts.json';d=json.loads(P.read_text());i=next(i for i in d['items'] if i['key']=='77-king-blow');at=i['attempts'][-1]
ready=Image.open(B/'finale/king-ready.png').convert('RGBA');rb=ready.getbbox();src,bb=clean_cell(Image.open(at['archive_path']),False)
scale=(rb[2]-rb[0])/src.width;im=resize(src,(rb[2]-rb[0],round(src.height*scale)));a=np.array(im);a[a[:,:,3]<3]=0;im=Image.fromarray(a)
out=Image.new('RGBA',(512,512));pos=[(512-im.width)//2,471-im.height];out.alpha_composite(im,pos)
def mouth(im):
 a=np.array(im);r=a[:,:,:3].astype(float);yy,xx=np.indices(a.shape[:2]);hsv=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2HSV)
 m=(r[:,:,0]>r[:,:,1]*1.7)&(r[:,:,0]>r[:,:,2]*1.3)&(hsv[:,:,1]>100)&(a[:,:,3]>220)&(xx>215)&(xx<295)&(yy>90)&(yy<195)
 n,l,s,c=cv2.connectedComponentsWithStats(m.astype(np.uint8),8);idx=1+np.argmax(s[1:,4]);x,y,w,h,area=map(int,s[idx]);assert area>100
 return dict(cavity_bbox=[x,y,x+w,y+h],center=[x+(w-1)/2,y+(h-1)/2],colored_pixels=area,definition='红色口腔主体连通区域外接框像素中心，排除唇边与皇冠宝石')
before=mouth(out);target=256;shift=target-before['center'][0]
if abs(shift)>0:
 # Tiny localized horizontal registration of the AI-rendered lips; no new drawing.
 a=np.array(out);yy,xx=np.indices((512,512),dtype=np.float32);wx=np.clip((46-abs(xx-before['center'][0]))/10,0,1);wy=np.clip((48-abs(yy-before['center'][1]))/12,0,1);w=wx*wy;mpx=xx-shift*w
 a=cv2.remap(a,mpx,yy,cv2.INTER_LANCZOS4,borderMode=cv2.BORDER_CONSTANT);a[a[:,:,3]<3]=0;out=Image.fromarray(a)
after=mouth(out);dest=B/'_qa'/f'r2-king-blow-attempt-{at["attempt"]:02}-candidate.png';out.save(dest);v=meta(dest);v['mouth']=after;v['postprocess']=dict(reference_ready_bbox=list(rb),ready_sha256=hashlib.sha256((B/'finale/king-ready.png').read_bytes()).hexdigest(),source_bbox=bb,uniform_scale=scale,position=pos,matched_width=rb[2]-rb[0],mouth_before_registration=before,mouth_horizontal_registration_px=shift)
print(json.dumps(v,ensure_ascii=False,indent=2));(B/'_qa/r2-king-candidate.json').write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
proof=Image.new('RGB',(1024,550),'#DCE5EF');dr=ImageDraw.Draw(proof)
for x,im,label in [(0,ready,'READY (approved)'),(512,out,'BLOW')]:
 proof.paste(im,(x,20),im);dr.line((x,491,x+511,491),fill='#76869A');dr.text((x+200,520),label,fill='#24344A')
mx,my=after['center'];dr.ellipse((512+mx-3,20+my-3,512+mx+3,20+my+3),outline='#FF3059');dr.text((512+mx+8,20+my),str(after['center']),fill='#8C1740');proof.save(B/'_qa/r2-king-ready-blow-comparison.png')
