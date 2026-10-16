"""Finish native-generated emotes, preserving all pre-existing character PNGs."""
from pathlib import Path
import json,sys,hashlib
import numpy as np,cv2
from PIL import Image,ImageDraw
from process_creatures import clean_cell,resize,color_finish,metadata

ROOT=Path(__file__).resolve().parents[1]
P=ROOT/'emotes-58-59-prompts.json'
# Eye outlines in the approved 224x256 idle sprites, before the required 4px margin.
EYES={
 'blue':[(80,107,119,143),(128,101,165,137)],
 'orange':[(80,105,111,135),(125,98,155,127)],
 'green':[(91,106,128,144),(139,101,171,136)],
 'purple':[(75,105,108,134),(127,103,155,132)],
 'yellow':[(126,110,156,140)],
 'white':[(74,101,113,137),(120,93,160,130)],
 'red':[(73,131,107,162),(126,126,158,159)],
 'charcoal':[(77,108,113,144),(122,107,157,143)],
}


def body_width(im,band=(.55,.85)):
    a=np.array(im.getchannel('A'));ys,xs=np.where(a>0);y0,y1=ys.min(),ys.max()+1
    lo,hi=round(y0+(y1-y0)*band[0]),round(y0+(y1-y0)*band[1])
    spans=[]
    for row in a[lo:hi]>10:
        r=np.where(row)[0];spans.append(int(r[-1]-r[0]+1)if len(r)else 0)
    return max(spans),[int(lo),int(hi)]


def color_samples(im,mask,hx):
    a=np.array(im);rgb=a[:,:,:3];b=im.getchannel('A').getbbox();yy,xx=np.mgrid[:im.height,:im.width]
    valid=cv2.erode(mask.astype('uint8'),np.ones((5,5),np.uint8))>0
    valid&=(yy>b[1]+(b[3]-b[1])*.5)&(yy<b[3]-8)
    lab=cv2.cvtColor(rgb.astype('float32')/255,cv2.COLOR_RGB2LAB)
    coords=np.argwhere(valid);assert len(coords)>10
    coords=coords[np.argsort(lab[valid][:,0])]
    target=np.array([int(hx[i:i+2],16)for i in (0,2,4)],np.float32)/255
    target=cv2.cvtColor(target.reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
    out=[]
    for q,label in [(.5,'身体中间调'),(.7,'身体较亮部（不含镜面高光）')]:
        cy,cx=coords[round((len(coords)-1)*q)];v=np.median(rgb[cy-1:cy+2,cx-1:cx+2].reshape(-1,3),axis=0).round().astype('uint8')
        l=cv2.cvtColor((v.astype('float32')/255).reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0];delta=float(np.linalg.norm(l-target))
        out.append({'label':label,'roi_xyxy_exclusive':[int(cx)-1,int(cy)-1,int(cx)+2,int(cy)+2],
                    'rgb':v.tolist(),'hex':'#'+''.join(f'{x:02X}'for x in v),'deltaE76':round(delta,3),'pass':delta<=8})
    return out


def charcoal_lids(im):
    """Use the approved light-gray outline color for visible closed eyelids."""
    a=np.array(im);rgb=a[:,:,:3];yy,xx=np.mgrid[:256,:224]
    region=(yy>=106)&(yy<140)&(xx>=65)&(xx<164)
    dark=(rgb.max(2)<24)&(a[:,:,3]>230)&region
    n,labels,stats,_=cv2.connectedComponentsWithStats(dark.astype('uint8'),8)
    ids=[j for j in range(1,n)if stats[j,4]>=60 and stats[j,2]>=18]
    assert len(ids)==2,stats.tolist()
    core=np.isin(labels,ids).astype('uint8')
    edge=cv2.GaussianBlur(core.astype('float32'),(3,3),.55)
    weight=np.maximum(core,edge*.7)
    target=np.array([136,138,153],float)
    a[:,:,:3]=np.rint(rgb*(1-weight[:,:,None])+target*weight[:,:,None]).astype('uint8')
    return Image.fromarray(a)


def blink(item,a):
    idle=Image.open(item['idle_path']).convert('RGBA');orig=np.array(idle);box=idle.getchannel('A').getbbox()
    gen,genbox=clean_cell(Image.open(a['archive_path']),False)
    aligned=Image.new('RGBA',idle.size);aligned.alpha_composite(resize(gen,(box[2]-box[0],box[3]-box[1])),box[:2])
    aligned.save(ROOT/'_qa'/f"{item['key']}-aligned.png")
    src=np.array(aligned);out=orig.copy();allowed=np.zeros(orig.shape[:2],bool);rects=[]
    for eye in EYES[item['character']]:
        x0,y0,x1,y1=rect=[eye[0]-4,eye[1]-4,eye[2]+4,eye[3]+4];rects.append(rect);allowed[y0:y1,x0:x1]=True
        patch=src[y0:y1,x0:x1,:3].astype(float);base=orig[y0:y1,x0:x1,:3].astype(float);h,w=patch.shape[:2]
        yy,xx=np.mgrid[:h,:w];dist=np.minimum.reduce([xx,yy,w-1-xx,h-1-yy]).astype(float)
        ring=dist<=1
        # Match surrounding body color, then feather within the 4px margin only.
        delta=np.median((base-patch)[ring],axis=0);patch=np.clip(patch+delta,0,255)
        blend=np.clip(dist/3,0,1);blend=blend*blend*(3-2*blend)
        out[y0:y1,x0:x1,:3]=np.rint(base*(1-blend[:,:,None])+patch*blend[:,:,None]).astype('uint8')
    # Original alpha is kept everywhere, even within the eye rectangles.
    out[:,:,3]=orig[:,:,3]
    if item['character']=='charcoal':out=np.array(charcoal_lids(Image.fromarray(out)))
    diff=np.any(out!=orig,axis=2)
    assert not np.any(diff&~allowed)
    ys,xs=np.where(diff);result=Image.fromarray(out)
    detail={'eye_outline_boxes_xyxy_exclusive':[list(x)for x in EYES[item['character']]],'eye_patch_boxes_xyxy_exclusive':rects,
        'margin_px':4,'changed_pixels':int(diff.sum()),'difference_bbox_xywh':[int(xs.min()),int(ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)],
        'outside_eye_rectangles_changed_pixels':int((diff&~allowed).sum()),'alpha_pixel_identical':bool(np.array_equal(orig[:,:,3],out[:,:,3])),
        'generated_alignment_bbox':genbox,'method':'align native generated sprite; copy only eye rectangles expanded 4px, local color matching and 3px inward feather; preserve original alpha and every pixel outside rectangles'}
    if item['character']=='charcoal':detail['eyelid_outline_color']='#888A99; sampled approved idle outer outline; closed-eye component only'
    mask=np.array(Image.open(ROOT/'_qa'/f"mask-{item['character']}-idle.png"))>0
    return result,mask,detail


def cheer(item,a):
    idle=Image.open(item['idle_path']).convert('RGBA');wanted,band=body_width(idle)
    gen,box=clean_cell(Image.open(a['archive_path']),False)
    # Raised side hands/paws cross the usual torso band; measure below them.
    cheer_band={'yellow':(.66,.90),'red':(.64,.90),'white':(.70,.90)}.get(item['character'],(.55,.85))
    source_width,source_band=body_width(gen,cheer_band)
    scale=min(wanted/source_width,182/gen.height,210/gen.width);w=2*round(gen.width*scale/2);h=round(gen.height*scale)
    assert source_width*scale/wanted>=.95,{'body_width_ratio_if_fit':source_width*scale/wanted,'needs_regeneration':True}
    assert w<=210 and h<=182,{'generated_size':gen.size,'target_size':[w,h],'body_width':wanted,'needs_regeneration':True}
    c=resize(gen,(w,h));c,mask=color_finish(c,item['color'][1:],item['character'],'cheer')
    b=c.getchannel('A').getbbox();c=c.crop(b);mask=mask[b[1]:b[3],b[0]:b[2]]
    if c.width%2:
        c=resize(c,(c.width+1,c.height));mask=np.array(Image.fromarray(mask.astype('uint8')*255).resize(c.size,Image.Resampling.NEAREST))>0
    out=Image.new('RGBA',(224,256));x=112-c.width//2;y=212-c.height;out.alpha_composite(c,(x,y))
    fullmask=np.zeros((256,224),bool);fullmask[y:y+c.height,x:x+c.width]=mask
    if item['character']=='charcoal':out=charcoal_lids(out)
    actual,actualband=body_width(out,cheer_band)
    detail={'source_bbox':box,'source_body_width':source_width,'uniform_scale':scale,'idle_body_width':wanted,
            'cheer_body_width':actual,'body_width_ratio':actual/wanted,'idle_measurement_y_band':band,
            'cheer_measurement_y_band':actualband,'cheer_measurement_fraction':list(cheer_band),
            'body_width_measurement':'maximum occupied row span (alpha >10) in the recorded visible torso band immediately below raised hands; yellow 66%–90%, red 64%–90%, white 70%–90%, others 55%–85%; visually verified',
            'method':'native generated artwork, alpha cleanup, uniform scale from body width, color calibration, x=112 center and y=212 foot boundary'}
    if item['character']=='charcoal':detail['eyelid_outline_color']='#888A99; sampled approved idle outer outline; closed-eye component only'
    return out,fullmask,detail


def cheer_variant(item,a):
    name=item['character'];basehex='2982F4'if name=='blue'else'FF7E27'
    source=ROOT/f'creatures/{name}/{basehex}/cheer.png';base=Image.open(source).convert('RGBA');orig=np.array(base);out=orig.copy()
    body=np.array(Image.open(ROOT/'_qa'/f'59-{name}-{basehex}-body-mask.png'))>0
    mask=cv2.erode(body.astype('uint8'),np.ones((3,3),np.uint8))>0
    # Orange mouth/brows and blue eyebrows share the body's hue. Protect the
    # actual painted face explicitly; hue alone cannot distinguish these parts.
    face=np.zeros((256,224),np.uint8);yy,xx=np.mgrid[:256,:224]
    if name=='blue':
        dark=(orig[:,:,:3].max(2)<155)&(xx>=73)&(xx<=161)&(yy>=100)&(yy<=125)
        polygons=[[(88,98),(89,94),(95,91),(101,91),(103,94),(102,97),(98,96),(92,99)],
                  [(126,90),(128,87),(135,87),(140,89),(141,93),(135,92),(129,93)],
                  [(116,120),(119,118),(123,119),(125,122),(122,123),(116,123)]]
    else:
        dark=(orig[:,:,:3].max(2)<190)&(xx>=70)&(xx<=151)&(yy>=65)&(yy<=104)
        polygons=[[(95,108),(102,109),(113,110),(122,108),(130,103),(134,103),
                   (133,113),(131,126),(128,136),(123,142),(116,145),(109,143),
                   (103,138),(99,132),(97,123)],
                  [(112,99),(116,97),(120,99),(121,102),(117,104),(112,103)]]
    contours,_=cv2.findContours(dark.astype('uint8'),cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(face,contours,-1,1,cv2.FILLED)
    face=cv2.dilate(face,np.ones((3,3),np.uint8))
    for polygon in polygons:cv2.fillPoly(face,[np.array(polygon,np.int32)],1)
    mask&=~face.astype(bool)
    def target_lab(hx):
        rgb=np.array([int(hx[i:i+2],16)for i in(0,2,4)],np.float32)/255
        return cv2.cvtColor(rgb.reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
    delta=target_lab(item['color'][1:])-target_lab(basehex)
    lab=cv2.cvtColor(orig[:,:,:3].astype('float32')/255,cv2.COLOR_RGB2LAB)
    rgb=np.clip(cv2.cvtColor((lab+delta).astype('float32'),cv2.COLOR_LAB2RGB)*255,0,255).round().astype('uint8')
    out[:,:,:3][mask]=rgb[mask]
    mask_path=ROOT/'_qa'/f"{item['key']}-recolor-mask.png";Image.fromarray(mask.astype('uint8')*255).save(mask_path)
    face_path=ROOT/'_qa'/f"{item['key']}-face-preserve-mask.png";Image.fromarray(face*255).save(face_path)
    idle=Image.open(item['idle_path']).convert('RGBA');iw,ib=body_width(idle);cw,cb=body_width(base)
    detail={'color_variant_source':str(source),'color_variant_source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
            'color_variant_mask':str(mask_path),'color_edit_prompt_number':28 if name=='blue'else 29,
            'face_preserve_mask':str(face_path),'face_preserved_pixels':int(face.sum()),
            'face_pixel_identical':bool(np.array_equal(out[face>0],orig[face>0])),
            'delta_lab':delta.tolist(),'idle_body_width':iw,'cheer_body_width':cw,'body_width_ratio':cw/iw,
            'idle_measurement_y_band':ib,'cheer_measurement_y_band':cb,
            'alpha_pixel_identical_to_source':True,'outside_body_mask_pixel_identical':True,
            'method':'native image_gen recolor study archived; preserve source cheer as exact silhouette/alpha/face/outline/highlight master, apply prescribed Lab palette offset only inside eroded body-color mask'}
    assert np.array_equal(out[:,:,3],orig[:,:,3])and not np.any(np.any(out!=orig,2)&~mask)
    return Image.fromarray(out),body,detail


def preview(m):
    names=[]
    for item in m['items']:
        if item.get('output')and (item['character'],item['color'])not in names:names.append((item['character'],item['color']))
    canvas=Image.new('RGB',(760,290*len(names)),(232,237,244));draw=ImageDraw.Draw(canvas)
    for row,(name,hx)in enumerate(names):
        y=row*290;draw.text((8,y+8),name+' '+hx,fill=(25,40,65))
        folder=ROOT/f'creatures/{name}/{hx[1:]}'
        for k,pose in enumerate(['idle','blink','cheer']):
            path=folder/f'{pose}.png'
            if not path.exists():continue
            im=Image.open(path);x=10+k*250;canvas.paste(im,(x,y+20),im);draw.line((x,y+232,x+224,y+232),fill=(140,155,175));draw.text((x+80,y+272),pose,fill=(30,45,70))
    canvas.save(ROOT/'_qa/emotes-contact-sheet.png')


if __name__=='__main__':
    m=json.loads(P.read_text())
    for key in sys.argv[1:]:
        item=next(x for x in m['items']if x['key']==key);a=item['attempts'][-1]
        out,mask,detail=blink(item,a)if item['pose']=='blink'else cheer_variant(item,a)if item['color']in['#42A9FF','#FF9D30']else cheer(item,a)
        path=ROOT/item['path'];out.save(path)
        Image.fromarray(mask.astype('uint8')*255).save(ROOT/'_qa'/f"{key}-body-mask.png")
        b=out.getchannel('A').getbbox();aa=np.array(out)[:,:,3]
        result={'metadata':metadata(path),'postprocessing':detail,'color_samples':color_samples(out,mask,item['color'][1:]),
                'checks':{'size':out.size==(224,256),'rgba':out.mode=='RGBA','foot_boundary_y212':b[3]==212 and not aa[212:].any(),
                          'center_x112':b[0]+b[2]==224,'top_at_least_30':b[1]>=30,'silhouette_width_at_most_210':b[2]-b[0]<=210}}
        result['checks']['color_samples']=all(x['pass']for x in result['color_samples'])
        if item['pose']=='cheer':result['checks']['body_width_within_5_percent']=abs(detail['body_width_ratio']-1)<=.05
        else:result['checks']['only_eye_rectangles_changed']=detail['outside_eye_rectangles_changed_pixels']==0 and detail['alpha_pixel_identical']
        item.update(output=result,selected_attempt=a['attempt'],status='finished_pending_visual_review')
        print(json.dumps({'key':key,**result},ensure_ascii=False))
    P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n');preview(m)
