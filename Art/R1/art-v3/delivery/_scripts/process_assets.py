"""Authorized sprite postprocessing: alpha, crop, size, visual-QA sheets."""
from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import cv2
ROOT=Path(__file__).resolve().parents[1]
RESAMPLE=Image.Resampling.LANCZOS

def meta(path):
    im=Image.open(path); a=np.asarray(im.convert('RGBA'))[:,:,3]
    return {'width':im.width,'height':im.height,'mode':im.mode,
            'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
            'transparent_pixels':int((a==0).sum()),'partial_alpha_pixels':int(((a>0)&(a<255)).sum()),
            'alpha_min':int(a.min()),'alpha_max':int(a.max())}

def trim_alpha(im,threshold=3):
    ar=np.asarray(im.convert('RGBA')); mask=ar[:,:,3]>threshold
    ys,xs=np.where(mask)
    if not len(xs): raise ValueError('Empty alpha')
    box=(max(0,int(xs.min())-1),max(0,int(ys.min())-1),min(im.width,int(xs.max())+2),min(im.height,int(ys.max())+2))
    return im.crop(box),box

def remove_flat_background(im):
    '''Flood fill only boundary-connected uniform matte; not an object detector.'''
    ar=np.array(im.convert('RGBA')); rgb=ar[:,:,:3].astype(float)
    corners=np.concatenate([rgb[:8,:8].reshape(-1,3),rgb[:8,-8:].reshape(-1,3),
                            rgb[-8:,:8].reshape(-1,3),rgb[-8:,-8:].reshape(-1,3)])
    bg=np.median(corners,axis=0)
    if np.max(np.std(corners,axis=0))>6:
        raise ValueError('Opaque non-uniform matte: requires specific alpha extraction, not automatic flat removal')
    d=np.linalg.norm(rgb-bg,axis=2)
    allowed=(d<34).astype('uint8')
    count,labels,stats,centroids=cv2.connectedComponentsWithStats(allowed,8)
    boundary=np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]]))
    mask=np.isin(labels,boundary[boundary!=0])
    ar[mask,3]=0
    return Image.fromarray(ar),{'method':'boundary-connected flat-matte removal','matte_rgb':bg.tolist()}

def resize_alpha(im,size):
    # Pillow RGBa avoids white/dark fringes caused by unassociated alpha interpolation.
    return im.convert('RGBa').resize(tuple(size),RESAMPLE).convert('RGBA')

def clean_sprite_alpha(im,ident):
    ar=np.array(im.convert('RGBA')); alpha=ar[:,:,3]
    if ident in [2,4,6,7,8,11,12,13,15,16,17,18,19]:
        # Remove detached generated matte specks and outside glow, preserving the source silhouette.
        solid=(alpha>200).astype('uint8')
        n,labels,stats,_=cv2.connectedComponentsWithStats(solid,8)
        if n<2: raise ValueError('No reliable solid sprite silhouette')
        largest=1+int(np.argmax(stats[1:,cv2.CC_STAT_AREA]))
        core=(labels==largest).astype('uint8')*255
        contours,_=cv2.findContours(core,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        mask=np.zeros_like(alpha); cv2.drawContours(mask,contours,-1,255,cv2.FILLED)
        mask=cv2.GaussianBlur(mask,(3,3),0.6)
        ar[:,:,3]=np.minimum(alpha,mask)
        return Image.fromarray(ar),{'method':'source-alpha silhouette cleanup; remove detached matte specks and unintended outside glow','source_alpha_threshold':200}
    return im,{'method':'retain intended translucent surface / effect glow'}

def process(asset,attempt=1):
    at=next(x for x in asset['attempts'] if x['attempt']==attempt)
    source=Path(at['archive_path']); im=Image.open(source).convert('RGBA')
    operations=[]
    if asset['id']==0:
        # Gray presentation sheet is the explicit exception to the transparency rule.
        final=im.convert('RGB'); operations.append({'method':'preserve original landscape lineup pending color/shape review'})
    elif asset['id']==1:
        final=im.convert('RGB').resize(tuple(asset['requested_size']),RESAMPLE)
        operations.append({'method':'opaque landscape-background resize','target':asset['requested_size']})
    else:
        alpha=np.asarray(im)[:,:,3]
        if alpha.min()==255:
            im,op=remove_flat_background(im); operations.append(op)
        else: operations.append({'method':'preserve generated RGBA alpha'})
        im,op=clean_sprite_alpha(im,asset['id']); operations.append(op)
        im,box=trim_alpha(im)
        operations.append({'method':'alpha bounding-box crop','box':box})
        w,h=asset['requested_size']
        final=resize_alpha(im,(w,h))
        operations.append({'method':'premultiplied-alpha Lanczos resize','target':[w,h]})
        if asset['id']==15:
            ar=np.array(final); ar[:,:,:3]=255; final=Image.fromarray(ar)
            operations.append({'method':'set bracket RGB to exact #FFFFFF, retain alpha'})
    dest=Path(asset['output_path']); dest.parent.mkdir(parents=True,exist_ok=True); final.save(dest)
    asset['selected_attempt']=attempt
    asset['postprocess']=operations
    asset['file_metadata']=meta(dest)
    asset['status']='processed_pending_visual_review'
    return asset

def checker(size):
    w,h=size; yy,xx=np.indices((h,w)); c=np.where(((xx//12+yy//12)%2)==0,175,205).astype('uint8')
    return Image.fromarray(np.stack([c,c,c,np.full_like(c,255)],axis=2),'RGBA')

def contacts(assets,filename='contact-sheet.png'):
    chosen=[a for a in assets if Path(a['output_path']).exists()]
    cw,ch=380,300; cols=4; rows=(len(chosen)+cols-1)//cols
    sheet=Image.new('RGB',(cw*cols,ch*rows),'#EEEEF2'); draw=ImageDraw.Draw(sheet)
    for j,a in enumerate(chosen):
        x=(j%cols)*cw; y=(j//cols)*ch
        draw.text((x+10,y+8),f"{a['id']:02d} {a['relative_path']}",fill='black')
        canvas=checker((cw-20,ch-48))
        im=Image.open(a['output_path']).convert('RGBA')
        im.thumbnail((cw-36,ch-64),RESAMPLE)
        canvas.alpha_composite(im,((canvas.width-im.width)//2,(canvas.height-im.height)//2))
        sheet.paste(canvas.convert('RGB'),(x+10,y+30))
    out=ROOT/'_qa'/filename; sheet.save(out); return out

if __name__=='__main__':
    import sys
    manifest=ROOT/'prompts.json'; data=json.loads(manifest.read_text())
    ids=[int(x) for x in sys.argv[1:]]
    for ident in ids:
        a=data['assets'][ident]
        try: process(a,a.get('selected_attempt',1)); print(ident,a['file_metadata'])
        except Exception as exc:
            a['status']='postprocess_needs_attention';a['postprocess_error']=str(exc); print(ident,str(exc))
    manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
    print(contacts(data['assets']))

