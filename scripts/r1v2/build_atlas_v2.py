#!/usr/bin/env python3
"""R1 v2 atlas: art-AI deliveries + program-drawn bead bugs + the existing glyph block.

usage: python3 scripts/r1v2/build_atlas_v2.py [--delivery DIR] [--out DIR]
- Reads finished PNGs from Art/R1/art-v2/delivery (2x logical size) and stores them at
  1.5x logical (x0.75), the background at 1x (x0.5): one 2048x2048 texture, as in v1.
- Plants are full-color per level color: slot plants/<id>/<HEX>/<state>; no runtime tint.
- Bead bugs are drawn here (11 px field, 21 px teaching) as a tinted main layer plus a
  fixed layer; also written to Art/R1/art-v2/generated/bugs for review.
- Glyphs are reused from the current atlas; the build fails if the C# character set changed.
"""
import argparse, hashlib, json, math, re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
from build_fx_atlas import load_frames, write_index

ROOT = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--delivery', default=str(ROOT / 'Art/R1/art-v2/delivery'))
ap.add_argument('--out', default=str(ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'))
ap.add_argument('--generated', default=str(ROOT / 'Art/R1/art-v2/generated'))
ap.add_argument('--code', default=str(ROOT / 'UnityProject/Assets/Flightbeat'))
args = ap.parse_args()
DEL, OUT, GEN, CODE = map(Path, (args.delivery, args.out, args.generated, args.code))
SIZE, CELL = 2048, 40

# ---- slots: (slot id, delivery file, scale)
UI = 0.75
SLOTS = [('scene/background', 'scene/background.png', 0.5), ('scene/fence', 'scene/fence.png', UI)]
for n in ['pause', 'level', 'progress-frame', 'progress-fill', 'plot', 'plot-locked', 'plot-label', 'plot-sprout',
          'queue-panel', 'queue-corner', 'button-shuffle', 'button-designate', 'button-water', 'badge', 'crown']:
    SLOTS.append(('scene/' + n, f'ui/{n}.png', UI))
for n in ['popup-panel', 'popup-ribbon', 'button-primary', 'button-secondary', 'toggle-on', 'toggle-off', 'card-frame',
          'card-rays', 'hedgehog-explain', 'hedgehog-happy', 'hedgehog-worried', 'hedgehog-proud', 'speech-bubble',
          'paw-pointer', 'banner-warning', 'banner-hint']:
    SLOTS.append(('ui/' + n, f'ui2/{n}.png', 0.5 if n in ('card-rays', 'card-frame') else UI))
PLANTS = {'morning-glory': ['2982F4', '42A9FF'], 'carrot': ['FF7E27', 'FF9D30'], 'broccoli': ['62E18E'],
          'eggplant': ['BD80FF'], 'sunflower': ['FFDF48'], 'cabbage': ['FFFFFF'], 'strawberry': ['FF525B'], 'truffle': ['2D2D2D']}
for pid, hexes in PLANTS.items():
    for h in hexes:
        for st in ['idle', 'attack', 'spent', 'projectile']:
            SLOTS.append((f'plants/{pid}/{h}/{st}', f'plants/{pid}/{h}/{st}.png', UI))

# ---- bead bugs (program-drawn)
def bead(px, teaching_mark=None, rim=None):
    s = 16; n = px * s
    main = Image.new('RGBA', (n, n), (0, 0, 0, 0)); fixed = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    dm, df = ImageDraw.Draw(main), ImageDraw.Draw(fixed)
    r = n * .46; cx = cy = n / 2 - n * .02
    for i in range(int(r), 0, -1):          # radial shading: light top-left, darker bottom-right
        t = i / r
        g = int(255 * (0.70 + 0.30 * (1 - t) ** .6))
        ox, oy = -(1 - t) * r * .28, -(1 - t) * r * .30
        dm.ellipse([cx - i + ox, cy - i + oy, cx + i + ox, cy + i + oy], fill=(g, g, g, 255))
    ring = Image.new('L', (n, n), 0); dr = ImageDraw.Draw(ring)
    dr.ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
    inner = ring.filter(ImageFilter.MinFilter(int(s * 1.6) | 1))
    main.putalpha(ring)
    edge = Image.eval(Image.composite(Image.new('L', (n, n), 0), ring, inner), lambda v: v)
    mg = Image.new('RGBA', (n, n), (150, 150, 150, 255)); main.paste(mg, (0, 0), edge)   # same-hue darker rim (tinted)
    # feet (fixed, dark) and highlight (fixed, white)
    fy = cy + r * .86
    for fx in (cx - r * .38, cx + r * .38):
        df.ellipse([fx - r * .2, fy - r * .14, fx + r * .2, fy + r * .14], fill=(40, 30, 24, 230))
    df.ellipse([cx - r * .55, cy - r * .62, cx - r * .05, cy - r * .22], fill=(255, 255, 255, 215))
    if rim:
        rr = Image.new('RGBA', (n, n), rim); fixed.paste(rr, (0, 0), edge)
    if teaching_mark:
        mk = Image.new('RGBA', (n, n), (0, 0, 0, 0)); dk = ImageDraw.Draw(mk); w = r * .34
        mx, my = cx + r * .08, cy + r * .12
        if teaching_mark == 'ring': dk.ellipse([mx - w, my - w, mx + w, my + w], outline=(255, 255, 255, 255), width=int(r * .16))
        elif teaching_mark == 'tri': dk.polygon([(mx - w, my - w * .7), (mx + w, my - w * .7), (mx, my + w)], fill=(255, 255, 255, 255))
        elif teaching_mark == 'cloud':
            for dx, dy in ((-.55, .2), (.55, .2), (0, -.35)): dk.ellipse([mx + dx * w - w * .55, my + dy * w - w * .55, mx + dx * w + w * .55, my + dy * w + w * .55], fill=(255, 255, 255, 255))
        elif teaching_mark == 'drop': dk.polygon([(mx, my - w * 1.1), (mx + w * .8, my + w * .2), (mx, my + w), (mx - w * .8, my + w * .2)], fill=(255, 255, 255, 255))
        sh = mk.split()[3].filter(ImageFilter.MaxFilter(int(s * 1.4) | 1))
        dark = Image.new('RGBA', (n, n), (45, 35, 28, 255)); fixed.paste(dark, (0, 0), sh); fixed.alpha_composite(mk)
    return main.resize((px, px), Image.LANCZOS), fixed.resize((px, px), Image.LANCZOS)

BUGS = {}
m, f = bead(11); BUGS['bugs/bead-main'], BUGS['bugs/bead-fixed'] = m, f
BUGS['bugs/bead-white-fixed'] = bead(11, rim=(140, 150, 162, 255))[1]
BUGS['bugs/bead-dark-fixed'] = bead(11, rim=(150, 154, 160, 255))[1]
BUGS['bugs/bead-teaching-main'] = bead(21)[0]
for key, mark in {'morning-glory': 'ring', 'carrot': 'tri', 'broccoli': 'cloud', 'eggplant': 'drop'}.items():
    BUGS[f'bugs/bead-teaching-{key}-fixed'] = bead(21, teaching_mark=mark)[1]
BUGS['bugs/bead-teaching-fixed'] = bead(21)[1]
# ---- program-drawn effects: soft glow (tinted at runtime) and a bouncing pointer arrow
def glow(px):
    n = px * 4; im = Image.new('L', (n, n), 0); d = ImageDraw.Draw(im)
    for i in range(n // 2, 0, -1):
        t = i / (n / 2); d.ellipse([n / 2 - i, n / 2 - i, n / 2 + i, n / 2 + i], fill=int(255 * min(1, 1.25 * (1 - t) ** 0.9)))
    im = im.filter(ImageFilter.GaussianBlur(n / 40)).resize((px, px), Image.LANCZOS)
    out = Image.new('RGBA', (px, px), (255, 255, 255, 0)); out.putalpha(im); return out
def arrow(px):
    s = 8; n = px * s; im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    pts = [(.30, .06), (.70, .06), (.70, .46), (.90, .46), (.50, .94), (.10, .46), (.30, .46)]
    P = [(x * n, y * n) for x, y in pts]
    d.polygon(P, fill=(90, 58, 30, 255))                                   # outline (drawn larger below)
    out = im.filter(ImageFilter.MaxFilter(int(n * .09) | 1))
    d2 = ImageDraw.Draw(out); d2.polygon(P, fill=(255, 213, 74, 255))      # gold body
    d2.polygon([(x * n, y * n) for x, y in [(.50, .94), (.90, .46), (.70, .46), (.70, .30), (.56, .30), (.56, .62)]], fill=(232, 168, 40, 255))
    d2.polygon([(x * n, y * n) for x, y in [(.34, .10), (.46, .10), (.46, .42), (.34, .42)]], fill=(255, 246, 200, 255))
    return out.resize((px, px), Image.LANCZOS)
BUGS['fx/glow'] = glow(96)
BUGS['fx/arrow'] = arrow(48)
(GEN / 'bugs').mkdir(parents=True, exist_ok=True); (GEN / 'fx').mkdir(parents=True, exist_ok=True)
for k, im in BUGS.items(): im.save(GEN / ('bugs' if k.startswith('bugs/') else 'fx') / (k.split('/')[1] + '.png'))

# ---- glyph block reuse
old = json.loads((OUT / 'atlas.json').read_text())
text = ''.join(p.read_text(encoding='utf-8') for p in CODE.rglob('*.cs'))
for p in CODE.rglob('*.prefab'):
    text += re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m[1], 16)), p.read_text(encoding='utf-8'))
chars = ''.join(chr(i) for i in range(32, 127)) + ''.join(sorted(set(c for c in text if ord(c) >= 128)))
missing_chars = sorted(set(chars) - set(old['characters']))
if missing_chars or old['size'] != SIZE or old['cell'] != CELL:
    raise SystemExit(f'New characters {"".join(missing_chars)}: rebuild glyphs with scripts/r1/build_atlas.py <font> first.')
chars = old['characters']   # the glyph block may keep characters no longer used; that is harmless
cols = SIZE // CELL
cursor_y = ((len(chars) + cols - 1) // cols) * CELL
image = Image.new('RGBA', (SIZE, SIZE))
image.alpha_composite(Image.open(OUT / 'atlas.png').convert('RGBA').crop((0, 0, SIZE, cursor_y)), (0, 0))

tiles, missing = [], []
for sid, rel, sc in SLOTS:
    p = DEL / rel
    if not p.exists(): missing.append(rel); continue
    im = Image.open(p).convert('RGBA')
    im = im.resize((max(1, round(im.width * sc)), max(1, round(im.height * sc))), Image.LANCZOS)
    tiles.append((sid, im, hashlib.sha256(p.read_bytes()).hexdigest()))
for sid, im in BUGS.items(): tiles.append((sid, im, 'generated'))
tiles.extend(load_frames(DEL))
# Trim transparent margins; the slot keeps its full frame (fw, fh) and the trimmed offset (ox, oy)
# so the game still positions it by the full canvas (pot baseline etc.).
trimmed = []
for sid, im, src in tiles:
    fw, fh = im.size
    bb = im.getchannel('A').point(lambda v: 255 if v > 0 else 0).getbbox() or (0, 0, 1, 1)
    if sid.startswith('bugs/') or sid.startswith('fx/') or sid in ('scene/background',): bb = (0, 0, fw, fh)
    trimmed.append((sid, im.crop(bb), src, bb[0], bb[1], fw, fh))
tiles = trimmed
tiles.sort(key=lambda t: (-max(t[1].size), -t[1].width * t[1].height, t[0]))
# MaxRects packing below the unchanged glyph block. Reuse cavities between tall
# scene/UI assets so the FX frames fit the existing 2048px atlas/sampler budget.
free = [(0, cursor_y, SIZE, SIZE - cursor_y)]
def place(w, h):
    choices = [(min(rw-w,rh-h), max(rw-w,rh-h), y, x) for x,y,rw,rh in free if w<=rw and h<=rh]
    if not choices: return None
    _, _, y, x = min(choices); right, bottom = x+w, y+h
    remaining = []
    for fx,fy,fw,fh in free:
        fr, fb = fx+fw, fy+fh
        if right<=fx or x>=fr or bottom<=fy or y>=fb:
            remaining.append((fx,fy,fw,fh)); continue
        if x>fx: remaining.append((fx,fy,x-fx,fh))
        if right<fr: remaining.append((right,fy,fr-right,fh))
        if y>fy: remaining.append((fx,fy,fw,y-fy))
        if bottom<fb: remaining.append((fx,bottom,fw,fb-bottom))
    remaining = list(dict.fromkeys(remaining))
    free[:] = [r for i,r in enumerate(remaining) if not any(i!=j and q[0]<=r[0] and q[1]<=r[1] and q[0]+q[2]>=r[0]+r[2] and q[1]+q[3]>=r[1]+r[3] for j,q in enumerate(remaining))]
    return x,y
slots, top = [], cursor_y
for sid, im, src, ox, oy, fw, fh in tiles:
    w, h = im.size
    at = place(w + 4, h + 4)
    if at is None: raise SystemExit(f'Atlas full at {sid}')
    image.alpha_composite(im, (at[0] + 2, at[1] + 2)); top = max(top, at[1] + h + 4)
    if sid.startswith('fx/'):
        # FX cells must retain equal frames. Extrude edges into the 2px guard for bilinear sampling.
        px, py = at[0] + 2, at[1] + 2
        image.paste(im.crop((0,0,w,1)).resize((w,2)),(px,py-2))
        image.paste(im.crop((0,h-1,w,h)).resize((w,2)),(px,py+h))
        image.paste(im.crop((0,0,1,h)).resize((2,h)),(px-2,py))
        image.paste(im.crop((w-1,0,w,h)).resize((2,h)),(px+w,py))
    slots.append(dict(id=sid, x=at[0] + 2, y=at[1] + 2, w=w, h=h, ox=ox, oy=oy, fw=fw, fh=fh))
row_h = 0; cursor_y = top
image.save(OUT / 'atlas.png')
write_index(slots, SIZE, OUT, GEN)
(OUT / 'atlas.json').write_text(json.dumps(dict(size=SIZE, cell=CELL, fontSize=old['fontSize'], characters=chars,
                                                 glyphs=old['glyphs'], slots=slots), ensure_ascii=False))
(GEN / 'atlas-sources.json').write_text(json.dumps({t[0]: t[2] for t in tiles}, indent=1, ensure_ascii=False))
print(json.dumps(dict(slots=len(slots), missing=missing, usedHeight=cursor_y + row_h, size=SIZE)))
