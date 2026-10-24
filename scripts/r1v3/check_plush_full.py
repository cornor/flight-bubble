#!/usr/bin/env python3
"""Audit shipped plush assets against all 22 actual palette identities."""
import hashlib,json,re
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
BATCH=ROOT/'Art/PlushToy/P2-P4'
roots=[BATCH/'delivery',ROOT/'Art/PlushToy/FirstFive/delivery',ROOT/'Art/PlushToy/P1/delivery']
res=ROOT/'UnityProject/Assets/Flightbeat/Resources/GardenR1'
atlas=json.loads((res/'atlas.json').read_text());slots={s['id']:s for s in atlas['slots']}
sources=json.loads((ROOT/'Art/R1/art-v3/generated/atlas-sources.json').read_text())
catalog=json.loads((ROOT/'Art/skin-colors.json').read_text())['colors'];rows=[]
code=(ROOT/'UnityProject/Assets/Flightbeat/Garden/GardenSkin.cs').read_text()
for c in catalog:
    hx=c['hex'].lstrip('#')
    if hx=='8C919C':continue
    match=re.search(r'case 0x'+hx+r': return new Vector2\(([\d.]+)f, ([\d.]+)f\)',code)
    assert match,('mouth',hx)
    bounds={}
    for state in ('idle','attack','spent','ready','blink'):
        rel=Path('creatures')/c['creatureId']/hx/(state+'.png')
        p=next((r/rel for r in roots if (r/rel).exists()),None);assert p,rel
        sid=f'plants/{c["slotId"]}/{hx}/{state}'
        sha=hashlib.sha256(p.read_bytes()).hexdigest();assert sources[sid]==sha,sid
        im=Image.open(p);assert im.mode=='RGBA' and im.size==(224,256)
        bb=im.getchannel('A').point(lambda v:255 if v>64 else 0).getbbox()
        assert bb and 0<bb[0]<bb[2]<224 and 0<bb[1]<bb[3]<256,(sid,bb)
        assert slots[sid]['fw']==224 and slots[sid]['fh']==256
        bounds[state]=bb;rows.append({'slot':sid,'source':str(p.relative_to(ROOT)),'sha256':sha})
    assert f'plants/{c["slotId"]}/{hx}/cheer' not in slots,'Legacy cheer must never leak'
    metadata={'canvas':[224,256],'feet':212,'mouthCanvas':list(map(float,match.groups())),
              'bounds':bounds,'reference':'../../../P0-20261023/battle-v2.png','version':'plush-p4'}
    dest=BATCH/'metadata'/f'{hx}.json';dest.parent.mkdir(exist_ok=True);dest.write_text(json.dumps(metadata,indent=2)+'\n')
assert len(rows)==110,len(rows)
for sid,rel in [('plants/mystery-bubble','creatures/mystery/bubble.png'),('plants/finale/king-ready','finale/king-ready.png'),('plants/finale/king-blow','finale/king-blow.png')]:
    assert sources[sid]==hashlib.sha256((roots[0]/rel).read_bytes()).hexdigest(),sid
for face in ('explain','happy','worried','proud'):
    sid='ui/guide-'+face
    assert sid in slots,sid
    assert any(hashlib.sha256(p.read_bytes()).hexdigest()==sources[sid] for root in roots for p in root.rglob('guide-'+face+'.png')),sid
assert (atlas['creatureWidth'],atlas['creatureHeight'])==(4096,3072)
assert 'nPOTScale: 0' in (res/'creatures.png.meta').read_text()
report={'passed':True,'colors':22,'states':110,'atlas':[4096,3072],'creatureScale':1.0,'rows':rows}
(BATCH/'asset-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print('PLUSH_FULL_ASSETS_PASSED: 22 colors, 110 states, four portraits, mystery, finale, no legacy cheer')
