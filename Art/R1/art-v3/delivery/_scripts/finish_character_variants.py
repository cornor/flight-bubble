"""Enforce pixel identity after the requested individual imagegen color edits."""
from pathlib import Path
import json
import numpy as np,cv2
from PIL import Image
from process_creatures import ROOT,MANIFEST,metadata,preview

def lab_color(hx):
    v=np.array([int(hx[i:i+2],16) for i in (0,2,4)],np.float32)/255
    return cv2.cvtColor(v.reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]

def finish(m,item,source_id):
    base=next(x for x in m['items'] if x['id']==source_id)
    folder=ROOT/'creatures'/item['character']/item['hex'];folder.mkdir(parents=True,exist_ok=True)
    delta=lab_color(item['hex'])-lab_color(base['hex']);outputs=[]
    for i,src in enumerate(base['outputs']):
        state=src['state'];im=Image.open(src['path']);a=np.array(im);b=a.copy()
        if state=='projectile':
            hsv=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2HSV)
            mask=(a[:,:,3]>0)&(hsv[:,:,1]>35)
        else:
            mask=np.array(Image.open(ROOT/'_qa'/f'mask-{item["character"]}-{state}.png'))>0
            # Shrink color-region boundaries to preserve antialiased facial and outline pixels.
            mask=cv2.erode(mask.astype('uint8'),np.ones((3,3),np.uint8))>0
        lab=cv2.cvtColor(a[:,:,:3].astype('float32')/255,cv2.COLOR_RGB2LAB)
        rgb=np.clip(cv2.cvtColor((lab+delta).astype('float32'),cv2.COLOR_LAB2RGB)*255,0,255).round().astype('uint8')
        b[:,:,:3][mask]=rgb[mask]
        path=folder/(state+'.png');Image.fromarray(b).save(path)
        changed=np.any(a!=b,axis=2)
        assert np.array_equal(a[:,:,3],b[:,:,3])
        assert not np.any(changed & ~mask)
        outputs.append(dict(state=state,source_file=src['path'],source_sha256=src['sha256'],
                            source_edit_attempt=i+1,**metadata(path),
                            identity_check={'alpha_pixel_identical':True,'outside_body_color_mask_pixel_identical':True,
                                            'changed_rgb_pixels':int(changed.sum()),'pose_position_size_unchanged':True}))
    item['outputs']=outputs
    item['postprocess']={'method':'imagegen individual recolor studies retained as originals; final export uses the source sprite as geometry/alpha/face master and applies the exact prescribed CIELAB palette offset only inside its body-color mask. This enforces pixel-identical pose, facial detail, highlights, outline and transparency rather than trusting generative redraws.',
                         'source_item':source_id,'delta_Lab':delta.tolist()}
    return outputs

if __name__=='__main__':
    m=json.loads(MANIFEST.read_text())
    for id,source_id in [(28,20),(29,21)]:
        item=next(x for x in m['items'] if x['id']==id)
        if len(item['attempts'])==4:finish(m,item,source_id)
    MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n');print(preview(m['items']))
