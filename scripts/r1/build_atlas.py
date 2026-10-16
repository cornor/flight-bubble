#!/usr/bin/env python3
"""Build a deterministic single atlas from optional, individually replaceable R1 PNGs.
Usage: python3 scripts/r1/build_atlas.py /path/to/NotoSansSC.ttf
       python3 scripts/r1/build_atlas.py --reuse-glyphs   (keep the current glyph block; only when the character set is unchanged)
White RGB main masks receive the level palette at runtime; fixed layers keep RGB.
"""
import hashlib, json, re, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'
ART = ROOT / 'Art/R1/slots'
REUSE = sys.argv[1] == '--reuse-glyphs'
FONT = None if REUSE else Path(sys.argv[1])
size, cell = 2048, 40
text = ''.join(p.read_text() for p in (ROOT/'UnityProject/Assets/Flightbeat').rglob('*.cs'))
# Unity serializes Chinese prefab copy as escaped Unicode. Authored UI labels
# must participate in the glyph set just like source-code strings.
for p in (ROOT/'UnityProject/Assets/Flightbeat').rglob('*.prefab'):
    text += re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m[1], 16)), p.read_text())
chars = ''.join(chr(i) for i in range(32,127)) + ''.join(sorted(set(c for c in text if ord(c) >= 128)))
font_size=32
image = Image.new('RGBA', (size,size))
columns=size//cell
cursor_y = ((len(chars)+columns-1)//columns)*cell
if REUSE:
    old=json.loads((OUT/'atlas.json').read_text())
    if old['characters']!=chars or old['size']!=size or old['cell']!=cell:
        raise SystemExit('Character set changed: rebuild with the font file instead of --reuse-glyphs.')
    glyphs=old['glyphs']
    image.alpha_composite(Image.open(OUT/'atlas.png').convert('RGBA').crop((0,0,size,cursor_y)),(0,0))
else:
    font = ImageFont.truetype(str(FONT), font_size)
    font.set_variation_by_axes([600])
    draw = ImageDraw.Draw(image)
    glyphs=[]
    for i,c in enumerate(chars):
        x,y = (i%columns)*cell,(i//columns)*cell
        left,top,right,bottom=font.getbbox(c,anchor='ls')
        w,h=right-left,bottom-top
        draw.text((x+2-left,y+2-top), c, font=font, fill='white', anchor='ls')
        glyphs.append(dict(code=ord(c),x=x+2,y=y+2,w=w,h=h,advance=font.getlength(c)/font_size,bearingX=left/font_size,bearingY=top/font_size))
slots=[]; x=0; row_h=0
manifest = json.loads((ROOT/'scripts/r1/art-slots.json').read_text())
# Tall sprites first: keep shelves compact while retaining a single texture/material.
available=[ident for ident in manifest['slots'] if (ART/(ident+'.png')).exists()]
available.sort(key=lambda ident:(-Image.open(ART/(ident+'.png')).height,ident))
for ident in available:
    source=ART/(ident+'.png')
    if not source.exists(): continue
    tile=Image.open(source).convert('RGBA'); w,h=tile.size
    if w > size-4 or h > size-4: raise ValueError(f'{source}: exceeds atlas')
    if x+w+4>size: x=0;cursor_y+=row_h+4;row_h=0
    if cursor_y+h+4>size: raise ValueError('Atlas full. Increase atlas size and keep one shared texture.')
    image.alpha_composite(tile,(x+2,cursor_y+2))
    slots.append(dict(id=ident,x=x+2,y=cursor_y+2,w=w,h=h))
    x+=w+4;row_h=max(row_h,h)
OUT.mkdir(parents=True,exist_ok=True)
image.save(OUT/'atlas.png')
(OUT/'atlas.json').write_text(json.dumps(dict(size=size,cell=cell,fontSize=font_size,characters=chars,glyphs=glyphs,slots=slots),ensure_ascii=False))
if not REUSE: (OUT/'font-source.json').write_text(json.dumps(dict(name='Noto Sans SC',license='SIL Open Font License 1.1',url='https://github.com/google/fonts/tree/main/ofl/notosanssc',sha256=hashlib.sha256(FONT.read_bytes()).hexdigest(),glyphs=len(chars)),indent=2)+'\n')
print(json.dumps(dict(glyphs=len(chars),replacedSlots=len(slots),textureBytes=(OUT/'atlas.png').stat().st_size)))
