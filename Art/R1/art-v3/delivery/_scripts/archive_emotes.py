from pathlib import Path
import sys,json,shutil,hashlib
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
p=ROOT/'emotes-58-59-prompts.json';m=json.loads(p.read_text())
for r in json.loads(sys.argv[1]):
    item=next(x for x in m['items']if x['key']==r['key']);a=next(x for x in item['attempts']if x['attempt']==r['attempt'])
    if r.get('error'):
        a.update(status='error_no_image',error=r['error']);continue
    src=Path(r['source']);dst=ROOT/'_originals'/f"{r['key']}-{item['pose']}-attempt-{r['attempt']:02d}.png"
    shutil.copyfile(src,dst);im=Image.open(dst)
    a.update(status='generated_pending_review',native_path=str(src),archive_path=str(dst),size=list(im.size),mode=im.mode,
             sha256=hashlib.sha256(dst.read_bytes()).hexdigest())
    print(json.dumps({'key':r['key'],'attempt':r['attempt'],'size':list(im.size),'mode':im.mode,'archive_path':str(dst)}))
p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
