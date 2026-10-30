"""Read-only second-batch measurements and dual-background contact sheets."""
from pathlib import Path
import json, math
from PIL import Image, ImageDraw
import numpy as np

BASE = Path(__file__).resolve().parent.parent
manifest=json.loads((BASE/'prompts.json').read_text())
assets=[a for a in manifest['assets'] if a.get('batch')=='second-20261029' and (BASE/'delivery'/a['file']).exists()]
measurements=[]
for a in assets:
    im=Image.open(BASE/'delivery'/a['file']);assert list(im.size)==a['size'],a['id']
    rgba=im.convert('RGBA');al=np.asarray(rgba)[:,:,3]
    corners=[int(al[y,x]) for y,x in [(0,0),(0,-1),(-1,0),(-1,-1)]]
    edges=np.concatenate([al[0],al[-1],al[:,0],al[:,-1]])
    if a['alpha']:
        assert im.mode=='RGBA',a['id']
        assert min(corners)==0,(a['id'],corners)
    else:assert int(al.min())==255,a['id']
    measurements.append({'id':a['id'],'path':'delivery/'+a['file'],'size':list(im.size),'mode':im.mode,'alpha_range':[int(al.min()),int(al.max())],'corner_alpha':corners,'outer_edge_alpha_max':int(edges.max())})
(BASE/'qa/batch2-measurements.json').write_text(json.dumps(measurements,indent=2)+'\n')
groups=[('ui', [a for a in assets if a['id'].startswith('T') and a['id'] not in ['T2','T3']]),('backgrounds',[a for a in assets if a['id'] in ['T2','T3','N2']]),('new',[a for a in assets if a['id'].startswith('N') and a['id']!='N2'])]
for label,group in groups:
    if not group:continue
    cw,ch=(420,320) if label=='ui' else ((320,660) if label=='backgrounds' else (520,380))
    for name,color in [('white','#FFFFFF'),('light-blue','#E8F2FF')]:
        sheet=Image.new('RGB',(cw*3,ch*math.ceil(len(group)/3)+40),color);d=ImageDraw.Draw(sheet)
        d.text((16,12),f'SECOND BATCH / {label} / {name}',fill='#1D3A7A')
        for i,a in enumerate(group):
            im=Image.open(BASE/'delivery'/a['file']).convert('RGBA');im.thumbnail((cw-32,ch-64),Image.Resampling.LANCZOS)
            x=(i%3)*cw;y=(i//3)*ch+40
            d.text((x+16,y+10),f"{a['id']}  {a['size'][0]} x {a['size'][1]}",fill='#1D3A7A')
            px=x+(cw-im.width)//2;py=y+40
            sheet.paste(im,(px,py),im)
            if a['id'].startswith('T17-'):
                d.rounded_rectangle((x+60,y+160,x+cw-60,y+260),radius=40,fill='#2F7BFF')
                sheet.paste(im,(px,y+178),im)
                d.text((x+16,y+280),'White glyph also checked on blue button',fill='#1D3A7A')
        sheet.save(BASE/'qa'/f'batch2-{label}-{name}.png')
print(json.dumps(measurements,indent=2))

def sliced(im, size, border):
    l,t,r,b=border;sw,sh=im.size;dw,dh=size
    sx=[0,l,sw-r,sw];sy=[0,t,sh-b,sh]
    dx=[0,l,dw-r,dw];dy=[0,t,dh-b,dh]
    out=Image.new('RGBA',size)
    for yi in range(3):
        for xi in range(3):
            p=im.crop((sx[xi],sy[yi],sx[xi+1],sy[yi+1]))
            p=p.resize((dx[xi+1]-dx[xi],dy[yi+1]-dy[yi]),Image.Resampling.LANCZOS)
            out.paste(p,(dx[xi],dy[yi]))
    return out

slice_specs={'T10':([48,48,48,48],[500,350],[24,60]),'T11':([18,18,18,18],[720,72],[24,460]),'T15':([50,30,50,30],[500,88],[24,580]),'T16':([60,36,60,36],[720,108],[24,720])}
for name,color in [('white','#FFFFFF'),('light-blue','#E8F2FF')]:
    sheet=Image.new('RGB',(800,860),color);d=ImageDraw.Draw(sheet)
    for a in assets:
        if a['id'] not in slice_specs:continue
        border,size,pos=slice_specs[a['id']]
        im=Image.open(BASE/'delivery'/a['file']).convert('RGBA')
        expanded=sliced(im,size,border)
        d.text((pos[0],pos[1]-22),f"{a['id']}: {size}, border L/T/R/B {border}",fill='#1D3A7A')
        sheet.paste(expanded,pos,expanded)
    sheet.save(BASE/'qa'/f'batch2-nine-slice-{name}.png')
(BASE/'qa/batch2-nine-slice.json').write_text(json.dumps(slice_specs,indent=2)+'\n')
