#!/usr/bin/env python3
"""Pack the hint glass slices and a scoped rounded font; leave the original UI font unchanged."""
import hashlib
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art/R1/ui-bars-assets-20261020/glass-v2'
EXTRA_ART = ROOT / 'Art/R1/ui-bars-assets-20261020/remaining-v1'
OUT = ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'
FONT = ROOT / 'Art/fonts/ChillRoundF/ChillRoundFBold.ttf'
SIZE, FONT_SIZE, CELL = 2048, 48, 56
TITLE_SIZE, TITLE_CELL, TITLE_STROKE = 96, 108, 1
TITLES = ('设置', '暂停一下', '休息一下', '泡泡全打光啦！', '失败了', '泡泡碰到护栏啦', '连胜中断', '体力', '泡泡图鉴')


def build(out=OUT):
    out = Path(out)
    source = json.loads((out / 'atlas.json').read_text())
    chars = sorted(set(source['characters']) | set(chr(c) for c in range(32, 127)))
    inputs = [Path(__file__), out / 'atlas.json', FONT, *sorted(ART.glob('*-source.png')),
              *sorted(EXTRA_ART.glob('*-source.png'))]
    fingerprint = hashlib.sha256(b''.join(p.read_bytes() for p in inputs)).hexdigest()
    target = out / 'bars.json'
    if target.exists() and (out / 'bars.png').exists() and (out / 'titles.json').exists():
        if json.loads(target.read_text()).get('fingerprint') == fingerprint:
            return
    image = Image.new('RGBA', (SIZE, SIZE))
    font = ImageFont.truetype(str(FONT), FONT_SIZE)
    fallback = ImageFont.truetype(str(ROOT / 'Art/fonts/NotoSansSC.ttf'), FONT_SIZE)
    fallback.set_variation_by_axes([700])
    missing_mask = bytes(font.getmask('\uffff'))
    glyphs, fallback_chars = [], []
    cols = SIZE // CELL
    for i, c in enumerate(chars):
        chosen = font
        if bytes(font.getmask(c)) == missing_mask:
            chosen = fallback
            fallback_chars.append(c)
        # Bake a subtle rounded emboldening at 48px, not repeated offset draws at runtime.
        box = chosen.getbbox(c, anchor='ls', stroke_width=1)
        w, h = box[2] - box[0], box[3] - box[1]
        x, y = (i % cols) * CELL + 2, (i // cols) * CELL + 2
        assert w + 4 <= CELL and h + 4 <= CELL, (c, box)
        ImageDraw.Draw(image).text((x - box[0], y - box[1]), c, font=chosen,
                                  fill='white', anchor='ls', stroke_width=1, stroke_fill='white')
        glyphs.append(dict(code=ord(c), x=x, y=y, w=w, h=h,
                           advance=chosen.getlength(c) / FONT_SIZE,
                           bearingX=box[0] / FONT_SIZE, bearingY=box[1] / FONT_SIZE))
    art_top = math.ceil(len(chars) / cols) * CELL + 4
    y = art_top
    slots = []
    # Each row is immutable cropped art; preserve padding and cap highlights at runtime.
    for name, height in [('capsule', 144), ('danger', 96), ('ring', 128)]:
        path = ART / (name + '-source.png')
        if not path.exists():
            raise SystemExit(f'Missing approved art: {path}')
        src = Image.open(path).convert('RGBA')
        alpha = src.getchannel('A')
        # Tight crop around the continuous object, excluding isolated generation specks.
        rows = [j for j in range(src.height) if sum(v > 32 for v in alpha.crop((0, j, src.width, j + 1)).getdata()) > src.width * .03]
        top, bottom = max(0, rows[0] - 3), min(src.height, rows[-1] + 4)
        band = alpha.crop((0, top, src.width, bottom))
        cols_used = [j for j in range(src.width) if sum(v > 32 for v in band.crop((j, 0, j + 1, band.height)).getdata()) > band.height * .08]
        box = (max(0, cols_used[0] - 3), top, min(src.width, cols_used[-1] + 4), bottom)
        art = src.crop(box)
        art = art.convert('RGBa').resize((round(art.width * height / art.height), height), Image.Resampling.LANCZOS).convert('RGBA')
        assert y + height + 4 <= SIZE and art.width + 4 <= SIZE, (name, y, art.size)
        image.alpha_composite(art, (2, y))
        art.save(ART / (name + '.png'))
        slots.append(dict(id=name, atlas=2, x=2, y=y, w=art.width, h=height,
                          fw=art.width, fh=height, ox=0, oy=0))
        y += height + 4
    # Spare right-hand area beside the original glass rows; do not enlarge the texture.
    for name, height, dx, dy in [('pause', 128, 900, 0), ('speech', 128, 1040, 0),
                                 ('number-pill', 88, 900, 148)]:
        src = Image.open(EXTRA_ART / (name + '-source.png')).convert('RGBA')
        box = src.getchannel('A').point(lambda a: 255 if a > 32 else 0).getbbox()
        art = src.crop(box)
        art = art.convert('RGBa').resize((round(art.width * height / art.height), height),
                                       Image.Resampling.LANCZOS).convert('RGBA')
        dy += art_top
        assert dx + art.width + 4 <= SIZE and dy + height + 4 <= SIZE, (name, art.size)
        image.alpha_composite(art, (dx, dy))
        art.save(EXTRA_ART / (name + '.png'))
        slots.append(dict(id=name, atlas=2, x=dx, y=dy, w=art.width, h=height,
                          fw=art.width, fh=height, ox=0, oy=0))
    # Title-only glyphs: rounded bold at 96 px. A 1 px optical stroke here is only ~0.33 logical px,
    # half the previous atlas's expansion, so dense counters stay open while headings retain weight.
    # Use the empty area beside/below the ring, keeping all existing UVs and the WebGL sampler count unchanged.
    title_font = ImageFont.truetype(str(FONT), TITLE_SIZE)
    title_chars = sorted(set(''.join(TITLES)))
    title_glyphs = []
    title_x, title_y = 144, art_top + 248
    title_cols = (SIZE - title_x) // TITLE_CELL
    title_missing = bytes(title_font.getmask('\uffff'))
    for i, c in enumerate(title_chars):
        assert bytes(title_font.getmask(c)) != title_missing, f'Missing rounded title glyph: {c}'
        box = title_font.getbbox(c, anchor='ls', stroke_width=TITLE_STROKE)
        w, h = box[2] - box[0], box[3] - box[1]
        x, ty = title_x + (i % title_cols) * TITLE_CELL + 4, title_y + (i // title_cols) * TITLE_CELL + 4
        assert w + 8 <= TITLE_CELL and h + 8 <= TITLE_CELL and ty + h + 4 <= SIZE, (c, box, ty)
        for slot in slots:
            assert (x + w + 4 <= slot['x'] or x - 4 >= slot['x'] + slot['w'] or
                    ty + h + 4 <= slot['y'] or ty - 4 >= slot['y'] + slot['h']), (c, slot['id'])
        ImageDraw.Draw(image).text((x - box[0], ty - box[1]), c, font=title_font, fill='white', anchor='ls',
                                  stroke_width=TITLE_STROKE, stroke_fill='white')
        title_glyphs.append(dict(code=ord(c), x=x, y=ty, w=w, h=h,
                                 advance=title_font.getlength(c) / TITLE_SIZE,
                                 bearingX=box[0] / TITLE_SIZE, bearingY=box[1] / TITLE_SIZE))
    (out / 'titles.json').write_text(json.dumps(dict(size=SIZE, fontSize=TITLE_SIZE,
        characters=''.join(title_chars), glyphs=title_glyphs, slots=[], titles=TITLES), ensure_ascii=False))
    image.save(out / 'bars.png')
    target.write_text(json.dumps(dict(size=SIZE, fontSize=FONT_SIZE, cell=CELL,
        characters=''.join(chars), glyphs=glyphs, slots=slots, fallbackCharacters=''.join(fallback_chars),
        fingerprint=fingerprint), ensure_ascii=False))
    print(json.dumps(dict(barGlyphs=len(chars), fallback=''.join(fallback_chars), usedHeight=y), ensure_ascii=False))


if __name__ == '__main__':
    build()
