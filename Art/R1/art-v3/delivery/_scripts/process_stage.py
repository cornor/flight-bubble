"""Finish only the generated stage underlay; keep source and QA provenance."""
from pathlib import Path
import json,sys
import numpy as np,cv2
from PIL import Image,ImageDraw
from process_assets import resize_alpha,meta

ROOT=Path(__file__).resolve().parents[1]
P=ROOT/'stage-prompts.json'

def finish(attempt):
    m=json.loads(P.read_text());a=next(x for x in m['attempts']if x['attempt']==attempt)
    im=Image.open(a['archive_path']).convert('RGBA');ar=np.array(im)
    if ar[:,:,3].min()==255:raise ValueError('Native alpha missing')
    # Remove only detached matte noise, retain cloudy silhouettes and soft AA.
    alpha=ar[:,:,3];n,labels,stats,_=cv2.connectedComponentsWithStats((alpha>24).astype('uint8'),8)
    biggest=1+int(np.argmax(stats[1:,4]));keep=(labels==biggest).astype('uint8')*255
    contours,_=cv2.findContours(keep,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    mask=np.zeros_like(alpha);cv2.drawContours(mask,contours,-1,255,cv2.FILLED);mask=cv2.GaussianBlur(mask,(5,5),.8)
    ar[:,:,3]=np.minimum(alpha,mask);ar[:,:,3][ar[:,:,3]<3]=0;ar[ar[:,:,3]==0]=0
    ys,xs=np.where(ar[:,:,3]>4);box=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]
    crop=Image.fromarray(ar).crop(box)
    # The specified three-percent rail band is a real 24-pixel transparent band.
    out=Image.new('RGBA',(1080,772));out.alpha_composite(resize_alpha(crop,(1080,748)),(0,24))
    # Remap the generated border strips so corner ornament stays outside the
    # protected middle 80%; the generated panel and cloud artwork are retained.
    layout=Image.new('RGBA',out.size)
    sx=[0,140,940,1080];dx=[0,104,976,1080]
    for i in range(3):
        part=resize_alpha(out.crop((sx[i],0,sx[i+1],772)),(dx[i+1]-dx[i],772));layout.paste(part,(dx[i],0))
    raw=np.array(layout).astype(float);yy=np.arange(772)
    baseline=np.median(raw[39:680,150:930,:3],axis=1)
    baseline=np.vstack([np.repeat(baseline[:1],39,axis=0),baseline,np.repeat(baseline[-1:],92,axis=0)])
    t=np.clip((yy-39)/(679-39),0,1)[:,None]
    desired=np.array([180,200,224])*(1-t)+np.array([157,180,210])*t
    residual=raw[:,:,:3]-baseline[:,None,:]
    dist=np.linalg.norm(residual,axis=2)
    smooth=lambda z:np.clip(z,0,1)**2*(3-2*np.clip(z,0,1))
    weight=smooth((36-dist)/16)*smooth((raw[:,:,3]-150)/50)
    # Color grade and denoise the plain frosted surface, preserving the generated
    # bright rim, cloud shading and colored ornaments outside its color range.
    target=desired[:,None,:]+residual*.04
    raw[:,:,:3]+=weight[:,:,None]*(target-raw[:,:,:3])
    raw[:,:,3]+=weight*(230-raw[:,:,3])
    final=raw.clip(0,255).round().astype('uint8');final[:24]=0;final[:,:,3][final[:,:,3]<2]=0;final[final[:,:,3]==0]=0
    out=Image.fromarray(final);dest=ROOT/'scene/stage.png';out.save(dest)
    m.update(selected_attempt=attempt,file_metadata=meta(dest),postprocess=[{'method':'native-alpha silhouette cleanup and empty-margin trim','bbox':box},{'method':'premultiplied-alpha resize to1080x748, place at y24 on1080x772 transparent canvas','top_transparent_rows':24},{'method':'three-column border remap; retain generated border artwork','source_columns':sx,'destination_columns':dx},{'method':'frosted-panel color calibration and texture/alpha denoise; preserve rim, clouds and colored bubbles','top_color':'#B4C8E0','bottom_color':'#9DB4D2','panel_alpha':230,'source_residual_weight':.04}],status='processed_pending_visual_review')
    P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    previews()
    print(json.dumps(m['file_metadata'],ensure_ascii=False))

def previews():
    im=Image.open(ROOT/'scene/stage.png').convert('RGBA');w,h=im.size;yy,xx=np.indices((h,w))
    v=np.where((xx//16+yy//16)%2,180,211).astype('uint8');checker=Image.fromarray(np.stack([v,v,v,np.full_like(v,255)],2))
    checker.alpha_composite(im);checker.convert('RGB').save(ROOT/'_qa/stage-checker.png')
    marked=checker.copy();d=ImageDraw.Draw(marked);d.rectangle((108,39,971,679),outline='#FF315F',width=2);d.line((0,24,1079,24),fill='#00CC66',width=2);marked.convert('RGB').save(ROOT/'_qa/stage-protected-area.png')
    bg=Image.open(ROOT/'scene/background.png').convert('RGBA');bg.alpha_composite(im,(0,1148));bg.convert('RGB').save(ROOT/'_qa/stage-on-background.png')

if __name__=='__main__':finish(int(sys.argv[1]))

