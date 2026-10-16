#!/usr/bin/env python3
"""R1 invariant audit: unchanged rules/data and exact palette/name/glyph coverage."""
import hashlib,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
paths=['UnityProject/Assets/Flightbeat/Model','UnityProject/Assets/Flightbeat/Economy','UnityProject/Assets/Flightbeat/Resources/levels','UnityProject/Assets/Flightbeat/Game/FlightLesson.cs','UnityProject/Assets/Flightbeat/Game/FlightNarration.cs']
changed=subprocess.check_output(['git','diff','162436e9','--name-only','--',*paths],cwd=ROOT,text=True).splitlines()
assert not changed,changed
levels=[]
known={0x42A9FF:'小蓝',0x2982F4:'小蓝',0xFF9D30:'小橙',0xFF7E27:'小橙',0x62E18E:'小绿',0xBD80FF:'小紫',0xFFDF48:'小黄',0xFFFFFF:'小白',0xFF525B:'小红',0x2D2D2D:'小黑'}  # v3 creature names (UI spec v3 5)
for name in ['training01','training02','sector01','sector02']:
 p=ROOT/f'UnityProject/Assets/Flightbeat/Resources/levels/{name}.json';d=json.loads(p.read_text());used=sorted({c for c in d['cells'] if c>=0}|{q['color'] for q in d['queue']})
 colors=[]
 for i in used:
  r,g,b=d['palette'][i*3:i*3+3];rgb=(r<<16)|(g<<8)|b
  assert rgb in known,(name,i,rgb)
  colors.append(dict(index=i,hex=f'#{rgb:06X}',plant=known[rgb]))
 levels.append(dict(id=name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),colors=colors))
a=json.loads((ROOT/'UnityProject/Assets/Flightbeat/Resources/GardenR1/atlas.json').read_text())
for p in (ROOT/'UnityProject/Assets/Flightbeat/Garden').glob('*.cs'):
 for text in re.findall(r'"([^"\n]*)"',p.read_text()):
  for c in text:
   if ord(c)>=128:assert c in a['characters'],(p.name,c)
print(json.dumps(dict(passed=True,baseline='162436e9',logicAndDataChanged=changed,levels=levels),ensure_ascii=False,indent=2))
