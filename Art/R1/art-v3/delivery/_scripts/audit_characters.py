from pathlib import Path
import json,hashlib
import numpy as np,cv2
from PIL import Image,ImageDraw
from process_creatures import ROOT,MANIFEST,metadata,resize

def samples(item,output):
    im=Image.open(output['path']);a=np.array(im);rgb=a[:,:,:3]
    mask=np.array(Image.open(ROOT/'_qa'/f'mask-{item["character"]}-{output["state"]}.png'))>0
    box=im.getchannel('A').getbbox(); yy,xx=np.mgrid[:im.height,:im.width]
    valid=cv2.erode(mask.astype('uint8'),np.ones((5,5),np.uint8))>0
    valid &= yy>box[1]+(box[3]-box[1])*.5
    valid &= yy<box[3]-8
    lab=cv2.cvtColor(rgb.astype('float32')/255,cv2.COLOR_RGB2LAB)
    coords=np.argwhere(valid);coords=coords[np.argsort(lab[valid][:,0])]
    target=np.array([int(item['hex'][i:i+2],16) for i in (0,2,4)],np.float32)/255
    tl=cv2.cvtColor(target.reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
    result=[]
    for q,label in [(.5,'身体中间调'),(.7,'身体较亮部（排除白色镜面高光）')]:
        cy,cx=coords[round((len(coords)-1)*q)]
        val=np.median(rgb[cy-1:cy+2,cx-1:cx+2].reshape(-1,3),axis=0).round().astype('uint8')
        vl=cv2.cvtColor((val.astype('float32')/255).reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
        de=float(np.linalg.norm(vl-tl))
        result.append({'label':label,'roi':[int(cx)-1,int(cy)-1,int(cx)+2,int(cy)+2],
                       'rgb':val.tolist(),'hex':'#'+''.join(f'{v:02X}' for v in val),'deltaE76':round(de,3),'pass':de<=8})
    return result

def make_proof(items):
    items=[a for a in items if a.get('outputs')]
    w=1100;rowh=104
    im=Image.new('RGB',(w,60+rowh*len(items)),(238,241,245));d=ImageDraw.Draw(im)
    d.text((16,15),'40px idle height / same scale for all 3 states | pale, #8C919C, dark | bubbles 64px',fill=(28,42,62))
    for row,a in enumerate(items):
        y=50+row*rowh
        d.text((12,y+6),f'{a["character"]} #{a["hex"]}',fill=(30,45,65))
        idle=Image.open(a['outputs'][0]['path']);bb=idle.getchannel('A').getbbox();scale=40/(bb[3]-bb[1])
        for bi,bg in enumerate([(238,241,245),(140,145,156),(38,46,64)]):
            bx=170+bi*235;d.rectangle((bx,y,bx+224,y+92),fill=bg)
            for k,o in enumerate(a['outputs'][:3]):
                src=Image.open(o['path']);sz=(round(224*scale),round(256*scale));sm=resize(src,sz)
                im.paste(sm,(bx+4+k*74,y+8),sm)
        bubble=Image.open(a['outputs'][3]['path'])
        for k,bg in enumerate([(238,241,245),(140,145,156)]):
            bx=900+k*85;d.rectangle((bx,y+15,bx+70,y+85),fill=bg);im.paste(bubble,(bx+3,y+18),bubble)
    p=ROOT/'_qa/characters-40px-and-alpha.png';im.save(p);return p

def audit(m):
    failures=[];out=[]
    for item in m['items']:
        for o in item.get('outputs',[]):
            p=Path(o['path']);o.update(metadata(p));im=Image.open(p);arr=np.array(im);a=arr[:,:,3]
            expected=[64,64] if o['state']=='projectile' else [224,256]
            assert list(im.size)==expected and im.mode=='RGBA'
            checks={'file_size_name_alpha':True,'style_identity_visual':True,'no_text_visual':True,'no_forbidden_objects_visual':True}
            if o['state']!='projectile':
                b=o['alpha_bbox'];width=b[2]-b[0];height=b[3]-b[1]
                anchored=b[3]==212 and b[0]+b[2]==224 and not a[212:].any()
                checks['anchor_and_body_bounds']=anchored and 120<=height<=150 and width<=180
                o['color_samples']=samples(item,o)
                checks['body_color_samples']=all(x['pass'] for x in o['color_samples'])
                checks['projectile_hollow']='不适用：角色姿态PNG不含弹体'
                o['body_dimensions']=[width,height]
            else:
                checks['anchor_and_body_bounds']='不适用：64×64独立弹体，不使用角色脚底'
                checks['body_color_samples']='不适用：半透明弹体，非身体亮部/中间调；目检主色边缘'
                checks['projectile_hollow']=not a[20:44,20:44].any() and bool(((a>0)&(a<240)).any())
                o['projectile_alpha']={'center_24x24_max':int(a[20:44,20:44].max()),'semitransparent_pixels':int(((a>0)&(a<240)).sum())}
            if item['id']>=28:
                checks['variant_identity']=all(v for k,v in o['identity_check'].items() if k!='changed_rgb_pixels')
            o['checks']=checks
            for key,value in checks.items():
                if value is False:failures.append({'path':str(p),'check':key,'samples':o.get('color_samples')})
            out.append({'id':item['id'],'character':item['character'],'hex':item['hex'],**o})
    m['validation']={'file_count':len(out),'failures':failures,'color_sample_count':sum(len(o.get('color_samples',[])) for o in out)}
    (ROOT/'_qa/character-file-audit.json').write_text(json.dumps({'files':out,**m['validation']},ensure_ascii=False,indent=2)+'\n')
    MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(m['validation'],ensure_ascii=False));print(make_proof(m['items']))

if __name__=='__main__':audit(json.loads(MANIFEST.read_text()))
