#!/usr/bin/env python3
"""Compare retained sprite/glyph pixels with the original imported atlas, independent of packing."""
import io, json, subprocess
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
PREFIX='UnityProject/Assets/Flightbeat/Resources/GardenR1/'
def original(name):return subprocess.check_output(['git','show','b470a8e:'+PREFIX+name],cwd=ROOT)
a=json.loads(original('atlas.json'));b=json.loads((ROOT/PREFIX/'atlas.json').read_text())
old=Image.open(io.BytesIO(original('atlas.png'))).convert('RGBA')
new=[Image.open(ROOT/PREFIX/n).convert('RGBA') for n in ['atlas.png','creatures.png']]
def crop(im,s):return im.crop(tuple(int(v) for v in (s['x'],s['y'],s['x']+s['w'],s['y']+s['h']))).tobytes()
slots={s['id']:s for s in b['slots']};errors=[];checked=0
for s in a['slots']:
 if s['id'].endswith('/projectile'):continue
 t=slots[s['id']];checked+=1
 if any(s.get(k,0)!=t.get(k,0) for k in ['w','h','ox','oy','fw','fh']) or crop(old,s)!=crop(new[t.get('atlas',0)],t):errors.append(s['id'])
glyphs={g['code']:g for g in b['glyphs']};glyph_count=0
for s in a['glyphs']:
 if s['code'] not in glyphs:continue
 t=glyphs[s['code']];glyph_count+=1
 if any(s[k]!=t[k] for k in ['w','h','advance','bearingX','bearingY']) or crop(old,s)!=crop(new[0],t):errors.append('glyph/'+str(s['code']))
r=dict(passed=not errors,sprites=checked,sharedGlyphs=glyph_count,errors=errors,baseline='b470a8e (unmodified source import)')
(ROOT/'Builds/R1-evidence').mkdir(parents=True,exist_ok=True)
(ROOT/'Builds/R1-evidence/imported-art-comparison.json').write_text(json.dumps(r,indent=2)+'\n')
print(json.dumps(r));raise SystemExit(bool(errors))
