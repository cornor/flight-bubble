"""User-authorized finishing of imagegen outputs for R1 v3 batch two."""
from pathlib import Path
import json,sys,hashlib
import numpy as np, cv2
from PIL import Image,ImageDraw,ImageFont
from process_assets import resize_alpha,meta

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/'second-batch-prompts.json'

def clean(im,solid=False,detached=True):
    a=np.array(im.convert('RGBA'));alpha=a[:,:,3]
    if alpha.min()==255:
        raise ValueError('Opaque matte: inspect and remove specifically before continuing')
    if solid:
        n,l,stats,_=cv2.connectedComponentsWithStats((alpha>150).astype('uint8'),8)
        if n<2:raise ValueError('No solid component')
        biggest=stats[1:,4].max()
        ids=[i for i in range(1,n) if stats[i,4]>max(20,biggest*.0015)] if detached else [1+np.argmax(stats[1:,4])]
        mask=np.isin(l,ids).astype('uint8')*255
        contours,_=cv2.findContours(mask,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        cv2.drawContours(mask,contours,-1,255,cv2.FILLED)
        mask=cv2.GaussianBlur(mask,(3,3),.65)
        a[:,:,3]=np.minimum(alpha,mask)
    a[:,:,3][a[:,:,3]<4]=0;a[a[:,:,3]==0]=0
    return Image.fromarray(a)

def trim(im,threshold=5,pad=1):
    a=np.array(im)[:,:,3];ys,xs=np.where(a>threshold)
    if len(xs)==0:raise ValueError('Empty sprite')
    box=[max(0,int(xs.min())-pad),max(0,int(ys.min())-pad),min(im.width,int(xs.max())+pad+1),min(im.height,int(ys.max())+pad+1)]
    return im.crop(box),box

def fit(im,size,padding=2):
    im,box=trim(im);w,h=size;s=min((w-2*padding)/im.width,(h-2*padding)/im.height)
    sm=resize_alpha(im,(round(im.width*s),round(im.height*s)))
    out=Image.new('RGBA',size);out.alpha_composite(sm,((w-sm.width)//2,(h-sm.height)//2));return out,box

def capsule(im,size,margin=0):
    im=clean(im,True,False);im,box=trim(im);sw,sh=im.size;w,h=size
    source_cap=min(sh//2,sw//3);cap=min(margin or h//2,h//2)
    out=Image.new('RGBA',size)
    for sb,db in [((0,0,source_cap,sh),(0,0,cap,h)),((source_cap,0,sw-source_cap,sh),(cap,0,w-cap,h)),((sw-source_cap,0,sw,sh),(w-cap,0,w,h))]:
        c=resize_alpha(im.crop(sb),(db[2]-db[0],db[3]-db[1]));out.paste(c,db[:2])
    return out,box

def nine_panel(im,size,margin):
    im=clean(im,True,False);im,box=trim(im);sw,sh=im.size;w,h=size
    sx=max(1,round(sw*.12));sy=max(1,round(sh*.12))
    if size==(360,200):
        # Include the complete generated corner arc in the fixed 40 px border.
        sx=round(sw*.14);sy=round(sh*.25)
    xs=[0,sx,sw-sx,sw];ys=[0,sy,sh-sy,sh];dx=[0,margin,w-margin,w];dy=[0,margin,h-margin,h]
    out=Image.new('RGBA',size)
    for j in range(3):
        for i in range(3):
            c=resize_alpha(im.crop((xs[i],ys[j],xs[i+1],ys[j+1])),(dx[i+1]-dx[i],dy[j+1]-dy[j]));out.paste(c,(dx[i],dy[j]))
    return out,box

def seam_vertical(im,width=12):
    ar=np.array(im).astype(float);pre=ar.copy();pre[:,:,:3]*=pre[:,:,3:4]/255;src=pre.copy();h=im.height
    for k in range(width):
        t=.5*(1-k/width);pre[k]=src[k]*(1-t)+src[h-1-k]*t;pre[h-1-k]=src[h-1-k]*(1-t)+src[k]*t
    pre[:,:,:3]/=np.maximum(pre[:,:,3:4]/255,1/255)
    pre=pre.clip(0,255).round().astype('uint8');pre[pre[:,:,3]==0]=0
    return Image.fromarray(pre)

def gray_tintable(im):
    a=np.array(im);v=a[:,:,:3].mean(2).clip(185,255).round().astype('uint8');a[:,:,:3]=np.repeat(v[:,:,None],3,2);a[a[:,:,3]==0]=0
    return Image.fromarray(a)

def process(a,attempt):
    src=next(x for x in a['attempts'] if x['attempt']==attempt);im=Image.open(src['archive_path']).convert('RGBA')
    ident=a['id'];size=tuple(a['requested_size']);ops=[]
    if ident in [30,42]:
        out,box=nine_panel(im,size,a['slice_margin']);ops.append({'method':'nine-slice source remap','source_bbox':box,'border':a['slice_margin']})
    elif ident in [31,32,33,44,45]:
        out,box=capsule(im,size,a['slice_margin']);ops.append({'method':'preserve rounded end proportions, stretch only empty middle','source_bbox':box,'slice_margin':a['slice_margin']})
        if ident==45:
            ar=np.array(out);ar[:,:,3]=np.rint(ar[:,:,3]*.82).astype('uint8');out=Image.fromarray(ar);ops.append({'opacity_multiplier':.82})
    elif ident in [34,35,36,43]:
        out,box=fit(clean(im,True,ident==36),size,2);ops.append({'method':'solid silhouette alpha cleanup, proportional fit','source_bbox':box})
    elif ident==37:
        out,box=fit(clean(im),size,10);ar=np.array(out);yy,xx=np.indices((size[1],size[0]));r=np.sqrt((xx-(size[0]-1)/2)**2+(yy-(size[1]-1)/2)**2)/(min(size)/2)
        fade=np.clip((1-r)/.23,0,1);fade=fade*fade*(3-2*fade);ar[:,:,3]=np.rint(ar[:,:,3]*fade).astype('uint8');out=Image.fromarray(ar)
        ops.append({'method':'preserve generated rays/glow; circular alpha falloff and transparent margin','source_bbox':box})
    elif 38<=ident<=41:
        out,box=fit(clean(im,True,True),size,12);ops.append({'method':'portrait provisional fit; shared composition review follows','source_bbox':box})
    elif a['frames']>1:
        n=a['frames'];fw=size[0]//n;fh=size[1];cells=[];boxes=[]
        cuts=[round(k*im.width/n) for k in range(n+1)]
        if ident==48:
            # Source has six distinct effects but uneven generated slots. Locate
            # transparent valleys before packing exact 192 px output cells.
            profile=np.array(im)[:,:,3].astype(float).sum(0)
            profile=cv2.GaussianBlur(profile.reshape(1,-1),(21,1),4).ravel()
            for k in range(1,n):
                lo=round(im.width*(k-.5)/n);hi=round(im.width*(k+.5)/n)
                cuts[k]=lo+int(np.argmin(profile[lo:hi]))
        for k in range(n):
            c=im.crop((cuts[k],0,cuts[k+1],im.height));c=clean(c);c,b=trim(c,8);cells.append(c);boxes.append(b)
        scale=min((fw-10)/max(c.width for c in cells),(fh-10)/max(c.height for c in cells))
        out=Image.new('RGBA',size);bounds=[]
        for k,c in enumerate(cells):
            sm=resize_alpha(c,(round(c.width*scale),round(c.height*scale)));xy=(k*fw+(fw-sm.width)//2,(fh-sm.height)//2);out.alpha_composite(sm,xy);bounds.append([xy[0]-k*fw,xy[1],sm.width,sm.height])
        ops.append({'method':'split source cells (transparent valleys for merge), shared scale for all frames, center each frame with transparent gutters','source_column_cuts':cuts,'source_boxes':boxes,'shared_scale':scale,'frame_bounds':bounds})
        if ident==50:out=gray_tintable(out)
    else:
        im=clean(im);im,box=trim(im,5)
        if ident in [51,52]:
            out=resize_alpha(im,size)
        elif ident==55:
            out=Image.new('RGBA',size);out.alpha_composite(resize_alpha(im,(size[0]-6,size[1]-6)),(3,3))
        elif ident==49:
            out=resize_alpha(im,size);out=seam_vertical(gray_tintable(out))
        else:
            out,_=fit(im,size,3)
        if ident==46:out=gray_tintable(out)
        ops.append({'method':'source alpha cleanup and exact final dimensions; grayscale for tintable FX; seam blend for beam','source_bbox':box})
    ar=np.array(out);ar[:,:,3][ar[:,:,3]<2]=0;ar[ar[:,:,3]==0]=0;out=Image.fromarray(ar)
    dest=ROOT/a['path'];dest.parent.mkdir(exist_ok=True,parents=True);out.save(dest)
    a.update(selected_attempt=attempt,postprocess=ops,file_metadata=meta(dest),status='processed_pending_visual_review')
    return a

def contact(m):
    items=[a for a in m['items'] if (ROOT/a['path']).exists()];cols=4;cw=350;ch=260
    out=Image.new('RGB',(cols*cw,((len(items)+cols-1)//cols)*ch),(236,240,246));d=ImageDraw.Draw(out)
    for j,a in enumerate(items):
        x=j%cols*cw;y=j//cols*ch;d.text((x+10,y+8),f'{a["id"]}: {a["path"]}',fill=(28,42,62))
        for yy in range(y+28,y+244,12):
            for xx in range(x+10,x+340,12):
                d.rectangle((xx,yy,min(xx+11,x+339),min(yy+11,y+243)),fill=(160,170,185) if ((xx-x-10)//12+(yy-y-28)//12)%2 else (193,202,213))
        im=Image.open(ROOT/a['path']);im.thumbnail((314,200),Image.Resampling.LANCZOS)
        out.paste(im,(x+(cw-im.width)//2,y+28+(216-im.height)//2),im)
    path=ROOT/'_qa/second-batch-contact-sheet.png';out.save(path);return path

if __name__=='__main__':
    m=json.loads(MANIFEST.read_text())
    if len(sys.argv)>1:
        a=next(a for a in m['items'] if a['id']==int(sys.argv[1]));process(a,int(sys.argv[2]));MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'id':a['id'],'metadata':a['file_metadata']},ensure_ascii=False))
    print(contact(m))
