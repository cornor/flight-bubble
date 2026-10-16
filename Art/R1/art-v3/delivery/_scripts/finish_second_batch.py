"""Finalize only generated batch-two art; record auditable technical checks."""
from pathlib import Path
import json,sys,hashlib
import numpy as np, cv2
from PIL import Image,ImageDraw
from process_second_batch import ROOT,MANIFEST,process,clean,resize_alpha,meta,contact

def smooth(x): x=np.clip(x,0,1);return x*x*(3-2*x)

def blue_grade(im):
    a=np.array(im);rgb=a[:,:,:3].astype('float32')/255
    hsv=cv2.cvtColor(rgb,cv2.COLOR_RGB2HSV);lab=cv2.cvtColor(rgb,cv2.COLOR_RGB2LAB)
    target=cv2.cvtColor(np.array([[[41,130,244]]],dtype='float32')/255,cv2.COLOR_RGB2LAB)[0,0]
    d=lab-target;dist=np.linalg.norm(d,axis=2)
    capped=target+d*np.minimum(1,7/np.maximum(dist,1e-6))[:,:,None]
    weight=smooth((lab[:,:,0]-(target[0]-10))/7)*smooth((hsv[:,:,1]-.30)/.15)
    weight*=((hsv[:,:,0]>195)&(hsv[:,:,0]<245)&(a[:,:,3]>0))
    graded=lab+(capped-lab)*weight[:,:,None]
    a[:,:,:3]=(cv2.cvtColor(graded,cv2.COLOR_LAB2RGB)*255).clip(0,255).round().astype('uint8')
    a[a[:,:,3]==0]=0
    return Image.fromarray(a)

def finish(a):
    process(a,a['selected_attempt'])
    p=ROOT/a['path'];im=Image.open(p);ops=a['postprocess'];ident=a['id']
    if 38<=ident<=41:
        src=next(x for x in a['attempts'] if x['attempt']==a['selected_attempt'])
        raw=clean(Image.open(src['archive_path']),True,True)
        # Same camera viewport and uniform scaling for all four source portraits.
        raw=resize_alpha(raw,(1254,1254));crop=raw.crop((60,145,1254,1179))
        small=resize_alpha(crop,(224,194));im=Image.new('RGBA',(240,240));im.alpha_composite(small,(8,23))
        im=blue_grade(im)
        ops.append({'method':'shared 1254px reference viewport, same scale/camera crop across all portraits; blue-body Lab grade; preserve white speculars and dark outlines','viewport':[60,145,1254,1179],'placement':[8,23,224,194],'target_body_hex':'#2982F4'})
    ar=np.array(im).astype(float);h,w=ar.shape[:2];yy,xx=np.indices((h,w))
    if ident in [47,48,54]:
        envelope={47:[1,1,1,.95,.75,.3],48:[1,1,1,.95,.7,.4],54:[1,1,1,1,.9,.7,.4,.15]}[ident]
        fw=w//a['frames']
        for i,v in enumerate(envelope): ar[:,i*fw:(i+1)*fw,3]*=v
        ops.append({'method':'animation-tail alpha fade','per_frame_alpha':envelope})
    if ident in [51,52]:
        ar[:,:,3]*=smooth(np.minimum(xx,w-1-xx)/24)*smooth(np.minimum(yy,h-1-yy)/5)
        if ident==52:
            ar[:,:,3]*=smooth((h-1-yy)/(h*.26))
        ops.append({'method':'soft edge fade; revive lower 26% fades into transparency'})
    if ident==53:
        # Preserve the generated iridescent rim, highlight and star, open the soap interior.
        r=np.sqrt(((xx-(w-1)/2)/(w*.438))**2+((yy-(h-1)/2)/(h*.438))**2)
        white=smooth((ar[:,:,:3].min(2)-192)/48)
        hollow=.23+.77*smooth((r-.64)/.33)
        ar[:,:,3]*=np.maximum(hollow,white)*smooth((1.045-r)/.045)
        ops.append({'method':'retain bubble rim and white highlight/star; lower interior alpha, remove exterior matte haze','interior_alpha_factor':.23})
    if ident==55:
        # Cyan glow is intentionally faint; white dashes and bubble highlights stay crisp.
        white=smooth((ar[:,:,:3].min(2)-158)/76)
        ar[:,:,3]*=.13+.87*white
        ops.append({'method':'suppress broad cyan bloom while retaining generated white dashes and bubble rims','minimum_glow_alpha_factor':.13})
    if ident>=46:
        old=ROOT.parents[1]/'art-v2'/'delivery'/a['path']
        old_mean=float(np.array(Image.open(old).convert('RGBA'))[:,:,3].mean())
        scale=min(1,old_mean/max(ar[:,:,3].mean(),1e-6));ar[:,:,3]*=scale
        ops.append({'method':'reference-v2 alpha density ceiling; preserve generated alpha detail','v2_mean_alpha':old_mean,'alpha_multiplier':scale})
    ar=ar.clip(0,255).round().astype('uint8');ar[:,:,3][ar[:,:,3]<2]=0;ar[ar[:,:,3]==0]=0
    Image.fromarray(ar).save(p);a['file_metadata']=meta(p);a['status']='finished_pending_visual_review'
    return a

def frames_proof(m):
    rows=[a for a in m['items'] if a['frames']>1 and (ROOT/a['path']).exists()]
    cw=264;rh=306;out=Image.new('RGB',(cw*8,rh*len(rows)),'#202C3E');d=ImageDraw.Draw(out)
    for j,a in enumerate(rows):
        im=Image.open(ROOT/a['path']);fw=im.width//a['frames']
        for k in range(a['frames']):
            x=k*cw;y=j*rh;d.text((x+8,y+8),str(a['id'])+' '+Path(a['path']).stem+' / '+str(k+1),fill='white')
            tile=im.crop((k*fw,0,(k+1)*fw,im.height));tile=resize_alpha(tile,(256,256));out.paste(tile,(x+4,y+34),tile)
    out.save(ROOT/'_qa/second-batch-frames.png')

def stretch(im,size,border,vertical=True):
    w,h=im.size;tw,th=size;m=border
    xs=[0,m,w-m,w];xd=[0,m,tw-m,tw]
    ys=[0,m,h-m,h] if vertical else [0,h];yd=[0,m,th-m,th] if vertical else [0,th]
    out=Image.new('RGBA',size)
    for j in range(len(ys)-1):
        for i in range(3):
            c=resize_alpha(im.crop((xs[i],ys[j],xs[i+1],ys[j+1])),(xd[i+1]-xd[i],yd[j+1]-yd[j]));out.paste(c,(xd[i],yd[j]))
    return out

def stretch_proof(m):
    ids=[30,32,33,42,44,45];out=Image.new('RGB',(1420,1630),'#879BB4');d=ImageDraw.Draw(out)
    y=0
    for ident in ids:
        a=next(x for x in m['items'] if x['id']==ident);im=Image.open(ROOT/a['path'])
        vertical=ident in [30,42];target=(650,440 if ident==30 else 300 if ident==42 else im.height)
        st=stretch(im,target,a['slice_margin'],vertical)
        d.text((15,y+12),str(ident)+' '+a['path']+' / original and stretched',fill='white')
        im.thumbnail((650,target[1]));out.paste(im,(15,y+36),im);out.paste(st,(730,y+36),st);y+=target[1]+58
    out.crop((0,0,1420,y)).save(ROOT/'_qa/second-batch-nine-slice.png')

if __name__=='__main__':
    m=json.loads(MANIFEST.read_text());ids=set(map(int,sys.argv[1:]))
    for a in m['items']:
        if a.get('selected_attempt') and (not ids or a['id'] in ids):
            finish(a);print(a['id'],a['path'])
    MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    contact(m);frames_proof(m);stretch_proof(m)

