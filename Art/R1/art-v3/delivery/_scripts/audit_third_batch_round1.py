"""Meaningful acceptance checks for the six requested round-eight deliverables."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image

B=Path(__file__).resolve().parents[1]
ROOT=B.parents[3]
Q=B/'_qa'
d=json.loads((Q/'third-batch-round1-checks.json').read_text())
checks=[]
for key,size in [('mystery',[224,256]),('king',[512,512]),('button',[680,172]),('rocket',[1152,192]),('rescue',[128,192])]:
    info=d[key]['file']; assert info['size']==size
    im=np.array(Image.open(info['path']));assert im.shape[2]==4 and im[:,:,3].min()==0
    if key in ('mystery','king'):
        baseline=212 if key=='mystery' else 470
        assert info['bbox'][3]-1==baseline and not im[baseline+1:,:,3].any()
    if key=='mystery':
        x0,y0,x1,y1=info['bbox'];assert x1-x0<=180 and 120<=y1-y0<=150
    if key=='king':
        x0,y0,x1,y1=info['bbox'];assert 360<=y1-y0<=400 and x1-x0<=440 and y0>=16
        assert abs((x0+x1)/2-256)<=0.5
checks+=['five runtime PNGs: exact sizes, RGBA with actual transparency','mystery baseline y=212; king baseline y=470; dimensions and centers within limits']
for r in d['lineup_colors']: assert all(v['delta_e76']<=8 for v in r['samples'])
assert all(v['delta_e76']<=8 for v in d['mystery']['postprocess']['samples'])
checks+=['24 new-lineup body samples and 2 mystery body samples: delta E76 <= 8']
a=np.array(Image.open(B/'fx/rocket-pop.png'))[:,:,3]
for i in range(6):
    f=a[:,192*i:192*(i+1)]
    assert not f[:5].any() and not f[-5:].any() and not f[:,:5].any() and not f[:,-5:].any()
assert a[:,960:].max()<=10 and a[:,960:].sum()/a[:,:960].sum()<0.01
checks+=['six 192-square cells, >=5 px transparent margins; frame 6 max alpha <=10 and alpha mass <1% of preceding frames']
assert not (B/'finale/king-blow.png').exists()
for r in d['lineup_colors']: assert not (B/'creatures'/r['id']).exists()
checks+=['no new-character runtime states or king-blow exported']
before=json.loads((Q/'before-third-batch-round1-sha256.json').read_text());changed=[];existing_art_changed=[]
for path,sha in before.items():
    p=ROOT/path; current=hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
    if current!=sha:
        changed.append(dict(path=path,before_sha256=sha,after_sha256=current))
        if path.endswith('.png'):existing_art_changed.append(path)
assert not existing_art_changed,existing_art_changed
checks+=['all pre-existing PNGs byte-for-byte unchanged']
report=dict(passed=True,checks=checks,baseline_files=len(before),existing_pngs_changed=existing_art_changed,other_baseline_changes=changed,fx_last_frame_mass_ratio=round(float(a[:,960:].sum()/a[:,:960].sum()),6),manual_reviews=['seven near-color groups distinguishable at 40px in grayscale','12 silhouettes and expression family reviewed; berry brow corrected using generated patch','light-green forest outline and medium mint-teal wisp outline visible','mystery faceless; no question mark; distinct from 22 color variants','king READY only, closed mouth, up-looking eyes, crown matches design','button center blank; no text in any runtime PNG','rescue bubble centered near y=56 and trail points downward','light/dark background inspection found no retained matte or unintended aura'])
(Q/'third-batch-round1-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
