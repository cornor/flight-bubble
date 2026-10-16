"""Pack batch-3 effects into the existing garden atlas (no extra GPU sampler).
Imported by build_atlas_v2.py. Delivered 2x art is sampled at 1x logical size.
"""
import hashlib, json
from PIL import Image
SPECS={'repel-gust':1,'dash-trail':1,'impact-dust':6,'merge-burst':6,'beam':1,'beam-tip':4,'water-can':1,'water-splash':8,'water-aim':1,'revive-wave':1}
def load_frames(delivery):
    tiles=[]
    for name,count in SPECS.items():
        path=delivery/'fx'/(name+'.png'); im=Image.open(path).convert('RGBA'); assert im.width%count==0
        cw=im.width//count; sha=hashlib.sha256(path.read_bytes()).hexdigest()
        for i in range(count):
            cell=im.crop((i*cw,0,(i+1)*cw,im.height)).convert('RGBa').resize((cw//2,im.height//2),Image.Resampling.LANCZOS).convert('RGBA')
            tiles.append((f'fx/{name}/{i}',cell,sha))
    return tiles

def write_index(slots,size,out,gen):
    lookup={s['id']:s for s in slots};clips=[]
    for name,count in SPECS.items():
        clips.append(dict(id=name,frames=[{k:lookup[f'fx/{name}/{i}'][k] for k in ('x','y','w','h')} for i in range(count)]))
    (out/'fx-atlas.json').write_text(json.dumps(dict(size=size,clips=clips)))
