"""Size/crop generated originals using sips; preserve imagegen masters and first-batch files."""
from pathlib import Path
import json, subprocess
import numpy as np
from PIL import Image

BASE = Path(__file__).resolve().parent.parent
manifest = json.loads((BASE/'prompts.json').read_text())
record_path = BASE/'qa/batch2-exports.json'
records = json.loads(record_path.read_text()) if record_path.exists() else {}
for a in manifest['assets']:
    if a.get('batch') != 'second-20261029' or not a.get('selected_master'):
        continue
    master = BASE/a['selected_master']
    dest = BASE/'delivery'/a['file']
    if records.get(a['id'], {}).get('master') == a['selected_master'] and dest.exists():
        continue
    dest.parent.mkdir(parents=True, exist_ok=True)
    im = Image.open(master)
    w, h = im.size
    tw, th = a['size']
    crop = None
    source = master
    if a['id'] == 'T16':
        crop = [0, 100, 2042, 566]
    elif a['id'] == 'T11':
        crop = [0, 115, 2172, 448]
    elif tw/th > 3.01 and a['alpha']:
        arr = np.asarray(im.convert('RGBA'))[:, :, 3]
        ys, xs = np.where(arr > 8)
        pad = max(8, int(w*0.012))
        ch = max(int(round(w*th/tw)), int(ys.max()-ys.min()+1)+pad*2)
        ch = min(ch, h)
        cy = int(round((ys.min()+ys.max())/2-ch/2))
        cy = min(max(0, cy), h-ch)
        crop = [0, cy, w, ch]
    if crop:
        x, y, cw, ch = crop
        source = BASE/'qa'/f"{a['id']}-batch2-export-crop.png"
        subprocess.run(['sips','--cropToHeightWidth',str(ch),str(cw),'--cropOffset',str(y),str(x),str(master),'--out',str(source)],check=True,capture_output=True)
    subprocess.run(['sips','--resampleHeightWidth',str(th),str(tw),str(source),'--out',str(dest)],check=True,capture_output=True)
    records[a['id']] = {'master':a['selected_master'],'master_size':[w,h], 'crop_xywh':crop,'size':[tw,th],'method':'sips canvas crop if needed, then exact-size resample; original preserved'}
record_path.write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
print('Exported second-batch files:', len(records))
