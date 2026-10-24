#!/usr/bin/env python3
"""Audit actual front-five palette coverage, all state sources and sprite registration."""
import hashlib,json
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
BATCH=ROOT/'Art/PlushToy/FirstFive'
DELIVERIES=[BATCH/'delivery',ROOT/'Art/PlushToy/P1/delivery']
RESOURCES=ROOT/'UnityProject/Assets/Flightbeat/Resources'
STATES=('idle','attack','spent','ready','blink')
catalog={c['hex'].lstrip('#'):c for c in json.loads((ROOT/'Art/skin-colors.json').read_text())['colors']}
sources=json.loads((ROOT/'Art/R1/art-v3/generated/atlas-sources.json').read_text())
atlas=json.loads((RESOURCES/'GardenR1/atlas.json').read_text())
slots={s['id']:s for s in atlas['slots']}
levels=[];required=set();rows=[]
for i,name in enumerate(('training01','training02','sector01','sector02','sector03'),1):
    path=RESOURCES/'levels'/(name+'.json');level=json.loads(path.read_text())
    used={v for v in level['cells']+level['stream'] if v>=0}
    for q in level['queue']:
        used.add(q['color']);used.update(q.get('other',[]))
    colors=sorted({''.join(f'{v:02X}' for v in level['palette'][c*3:c*3+3]) for c in used})
    required.update(colors)
    levels.append({'displayLevel':i,'resource':name,'title':level['title'],'colors':colors,
                   'fileSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'lanes':level['laneCount'],
                   'initialSlots':level['initialSlots'],'maximumSlots':level['slotLimit']})
for hx in sorted(required):
    c=catalog[hx]
    for state in STATES:
        rel=Path('creatures')/c['creatureId']/hx/(state+'.png')
        path=next((r/rel for r in DELIVERIES if (r/rel).exists()),None)
        assert path, f'Missing plush state {rel}'
        sid=f'plants/{c["slotId"]}/{hx}/{state}'
        sha=hashlib.sha256(path.read_bytes()).hexdigest()
        assert sources.get(sid)==sha,f'Old or incorrect atlas source: {sid}'
        im=Image.open(path);assert im.mode=='RGBA' and im.size==(224,256),rel
        b=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
        assert b and 0<b[0]<b[2]<224 and 0<b[1]<b[3]<256,(rel,b)
        slot=slots[sid];assert slot['fw']==224 and slot['fh']==256,(sid,slot)
        rows.append({'slot':sid,'path':str(path.relative_to(ROOT)),'sha256':sha,'opaqueBounds':b})
    assert f'plants/{c["slotId"]}/{hx}/cheer' not in slots, f'Old cheer can leak into plush {hx}'
for portrait in json.loads((BATCH/'production.json').read_text()).get('portraits',[]):
    path=BATCH/'delivery'/portrait['destination'];sid='ui/'+path.stem
    assert sources.get(sid)==hashlib.sha256(path.read_bytes()).hexdigest(),sid
report={'passed':True,'levels':levels,'colors':len(required),'characterStates':len(rows),
        'atlasSize':[atlas['creatureWidth'],atlas['creatureHeight']],'rows':rows}
(BATCH/'asset-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k not in ('rows',)},ensure_ascii=False,indent=2))
