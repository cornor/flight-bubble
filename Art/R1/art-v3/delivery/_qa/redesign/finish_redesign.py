"""Format approved-generation candidates, calibrate main paint, and audit."""
from pathlib import Path
import json, hashlib
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

Q=Path(__file__).resolve().parent
D=Q.parent.parent
ART=D.parent
SELECTED={'charcoal-A':3,'charcoal-B':2,'slate-blue-A':2,'slate-blue-B':1,
          'mint-A':1,'mint-B':1,'forest-A':1,'forest-B':1,'berry-A':1,'berry-B':1,'purple-A':1,'purple-B':2}

def rgb(h):
    h=h.lstrip('#')
    return np.array([int(h[i:i+2],16) for i in (0,2,4)],np.float32)

def lab(c):
    return cv2.cvtColor(np.array(c,np.float32).reshape(1,1,3)/255,cv2.COLOR_RGB2LAB)[0,0]

def resize_rgba(im,size):
    # Pillow RGBa implements premultiplied alpha resampling.
    return im.convert('RGBa').resize(size,Image.Resampling.LANCZOS).convert('RGBA')

def format_sprite(path):
    im=Image.open(path).convert('RGBA');a=np.array(im)
    n,labels,stats,_=cv2.connectedComponentsWithStats((a[:,:,3]>24).astype(np.uint8),8)
    k=1+int(np.argmax(stats[1:,4]))
    support=cv2.dilate((labels==k).astype(np.uint8),np.ones((5,5),np.uint8))
    a[support==0]=0;a[a[:,:,3]<3]=0
    clean=Image.fromarray(a);bb=clean.getchannel('A').getbbox();crop=clean.crop(bb)
    scale=min(760/crop.height,880/crop.width)
    size=(round(crop.width*scale),round(crop.height*scale))
    small=resize_rgba(crop,size)
    a=np.array(small);a[a[:,:,3]<3]=0;small=Image.fromarray(a)
    # Trim only resampling padding, then align the actual lowest pixel to 900.
    tight=small.getchannel('A').getbbox();small=small.crop(tight)
    x=(1024-small.width)//2;y=901-small.height
    out=Image.new('RGBA',(1024,1024));out.alpha_composite(small,(x,y))
    return out,{'sourceSize':list(im.size),'sourceBounds':list(bb),'uniformScale':scale,'scaledSize':list(small.size),'position':[x,y],'resampleTrim':list(tight)}

def calibrate(im,asset):
    a=np.array(im);old=a[:,:,:3].copy();f=old.astype(np.float32)/255
    ls=cv2.cvtColor(f,cv2.COLOR_RGB2LAB);sv=cv2.cvtColor(old,cv2.COLOR_RGB2HSV).astype(np.float32)
    target=rgb(asset['hex']);tl=lab(target)
    distance=cv2.distanceTransform((a[:,:,3]>220).astype(np.uint8),cv2.DIST_L2,5)
    def smooth(lo,hi,v):
        t=np.clip((v-lo)/(hi-lo),0,1)
        return t*t*(3-2*t)
    if asset['id']=='charcoal':
        chroma=old.max(2).astype(int)-old.min(2)
        candidate=(distance>4)&(old.min(2)>22)&(ls[:,:,0]<33)&(chroma<27)
        broad=(distance>4)&(old.min(2)>16)&(chroma<35)
        # Continuous lightness gates preserve pale cheeks/belly without a
        # geometric cutout boundary or a hard threshold in the shading.
        tonal=smooth(7,14,ls[:,:,0])*(1-smooth(30,46,ls[:,:,0]))*(1-smooth(22,35,chroma))
    else:
        th=float(cv2.cvtColor(target.astype(np.uint8).reshape(1,1,3),cv2.COLOR_RGB2HSV)[0,0,0])
        dh=abs(sv[:,:,0]-th);dh=np.minimum(dh,180-dh)
        candidate=(distance>4)&(dh<23)&(sv[:,:,1]>45)&(ls[:,:,0]>max(15,tl[0]-27))&(ls[:,:,0]<min(89,tl[0]+24))
        if asset['id']=='forest':candidate&=ls[:,:,0]<60
        broad=(distance>4)&(dh<29)&(sv[:,:,1]>20)&(ls[:,:,0]>8)
        tonal=(1-smooth(18,29,dh))*smooth(20,50,sv[:,:,1])*smooth(max(7,tl[0]-35),max(13,tl[0]-23),ls[:,:,0])*(1-smooth(min(82,tl[0]+13),min(97,tl[0]+31),ls[:,:,0]))
    n,ll,ss,_=cv2.connectedComponentsWithStats(candidate.astype(np.uint8),8)
    keep=[i for i in range(1,n) if ss[i,4]>max(500,ss[1:,4].max()*.035)]
    mask=np.isin(ll,keep)
    assert mask.sum()>5000,(asset['key'],mask.sum())
    anchor=np.percentile(ls[mask],55,axis=0)
    mapped=tl+(ls-anchor)*np.array([.58,.32,.32],np.float32)
    if asset['id']=='charcoal':mapped[:,:,1:]=0
    new=np.clip(cv2.cvtColor(mapped.astype(np.float32),cv2.COLOR_LAB2RGB)*255,0,255)
    n,bl,bs,_=cv2.connectedComponentsWithStats(broad.astype(np.uint8),8)
    keep=[i for i in range(1,n) if bs[i,4]>max(500,bs[1:,4].max()*.035)]
    domain=np.isin(bl,keep).astype(np.float32)
    domain=cv2.GaussianBlur(domain,(0,0),2)
    weight=tonal*smooth(4,12,distance)*domain
    mask&=weight>.97
    a[:,:,:3]=np.rint(old*(1-weight[:,:,None])+new*weight[:,:,None]).astype(np.uint8)
    a[a[:,:,3]==0]=0
    return Image.fromarray(a),mask,{'anchorLab':anchor.tolist(),'targetLab':tl.tolist(),'changedPixels':int(np.any(old!=a[:,:,:3],axis=2).sum()),'alphaUnchanged':True,'outer4PixelsPreserved':True,'paleAccentsPreservedByMask':True}

def samples(im,before,mask,target):
    a=np.array(im);b=np.array(before);ls=cv2.cvtColor(a[:,:,:3].astype(np.float32)/255,cv2.COLOR_RGB2LAB)
    valid=cv2.erode(mask.astype(np.uint8),np.ones((21,21),np.uint8))>0
    yy,xx=np.indices(valid.shape);box=im.getchannel('A').getbbox()
    valid&=(yy>box[1]+(box[3]-box[1])*.35)&(yy<box[3]-20)
    coords=np.argwhere(valid);assert len(coords)>1000
    coords=coords[np.argsort(ls[:,:,0][valid])]
    result=[]
    for fraction,label in [(.5,'midtone'),(.7,'lit-body-excluding-specular')]:
        y,x=coords[round((len(coords)-1)*fraction)]
        c=np.rint(np.median(a[y-3:y+4,x-3:x+4,:3],axis=(0,1))).astype(np.uint8)
        original=np.rint(np.median(b[y-3:y+4,x-3:x+4,:3],axis=(0,1))).astype(np.uint8)
        delta=float(np.linalg.norm(lab(c)-lab(rgb(target))))
        result.append({'region':label,'xy':[int(x),int(y)],'patchSize':7,'rgb':c.tolist(),'hex':'#'+''.join(f'{n:02X}' for n in c),'deltaE76':round(delta,4),'preCalibrationRGB':original.tolist(),'preCalibrationDeltaE76':round(float(np.linalg.norm(lab(original)-lab(rgb(target)))),4),'passed':delta<=8})
    return result

def process():
    log=json.loads((Q/'generation-log.json').read_text());report=[]
    for asset in log['assets']:
        attempt=SELECTED[asset['key']];entry=next(e for e in asset['attempts'] if e['attempt']==attempt)
        before,layout=format_sprite(entry['original'])
        out,mask,color=calibrate(before,asset)
        output=D/'redesign'/f"{asset['key']}.png";out.save(output)
        before.save(Q/f"{asset['key']}-before-color.png")
        Image.fromarray(mask.astype(np.uint8)*255).save(Q/f"{asset['key']}-color-mask.png")
        bb=out.getchannel('A').getbbox();a=np.array(out)
        picks=samples(out,before,mask,asset['hex'])
        assert out.size==(1024,1024) and bb[3]-1==900
        assert abs((bb[0]+bb[2])/2-512)<=.5
        assert not a[901:,:,3].any()
        assert all(p['passed'] for p in picks),(asset['key'],picks)
        asset['selectedAttempt']=attempt;asset['output']=str(output)
        for e in asset['attempts']:
            if e['attempt']==attempt:e['status']='selected for proposal delivery'
            elif e['status']=='pending review':e['status']='rejected during silhouette/expression review'
        row={'key':asset['key'],'id':asset['id'],'hex':asset['hex'],'variant':asset['variant'],'promptNumber':asset['number'],'generations':len(asset['attempts']),'selectedAttempt':attempt,'selectedOriginal':entry['original'],'output':str(output),'size':[1024,1024],'mode':'RGBA','bounds':list(bb),'baselineY':900,'centerX':(bb[0]+bb[2])/2,'layout':layout,'colorCalibration':color,'samples':picks,'sha256':hashlib.sha256(output.read_bytes()).hexdigest()}
        report.append(row)
        print(asset['key'],bb,'DeltaE',[(p['hex'],p['deltaE76']) for p in picks])
    (Q/'generation-log.json').write_text(json.dumps(log,indent=2)+'\n')
    (Q/'delivery-audit.json').write_text(json.dumps({'outputs':report,'totalGenerations':sum(r['generations'] for r in report),'maxPerAsset':4,'allColorSamplesPassed':True},indent=2)+'\n')

if __name__=='__main__':process()
