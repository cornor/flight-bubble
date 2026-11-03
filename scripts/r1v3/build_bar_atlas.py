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
# 2026-11-03 10:10 (user: use this font in the game): the rounded hint / dialog-title font is Arita Heiti Medium too
# (same 48 px body with the 1 px emboldening, 90 px titles); characters it lacks still fall back to Noto Sans SC.
if (ROOT / 'Art/fonts/AritaHeiti/AritaHeiti-Medium.ttf').is_file(): FONT = ROOT / 'Art/fonts/AritaHeiti/AritaHeiti-Medium.ttf'
# 2026-11-03 18:17 (user: switch to this font): now OPPOSans-S Medium (the user's new ARITA-HEITI.TTF), which keeps
# the hooks on slanted strokes.
if (ROOT / 'Art/fonts/OPPOSansS/OPPOSans-S-Medium.ttf').is_file(): FONT = ROOT / 'Art/fonts/OPPOSansS/OPPOSans-S-Medium.ttf'
SIZE, FONT_SIZE, CELL = 2048, 48, 56
# Result headings no longer magnify the 48 px body font. Keep the title font's light optical stroke.
# Tight packing keeps the larger title glyphs in the existing texture without moving the glass or body glyphs.
TITLE_SIZE, TITLE_STROKE = 90, 1
# 2026-11-03 10:56 (user: titles bold): with Arita Heiti the titles are baked bold - a 3 px stroke - at 84 px (no room
# left for bold 90 px glyphs; titles are drawn at 72 px at most, so nothing is lost).
if 'AritaHeiti' in FONT.name: TITLE_SIZE, TITLE_STROKE = 84, 3
if 'OPPOSans' in FONT.name: TITLE_SIZE, TITLE_STROKE = 82, 3   # its glyph body is larger than Arita's: 84 px bold no longer fits
# 2026-11-03 19:50 (UI check on a phone: 设置 / 暂停一下 / 泡泡图鉴 clogged - 置, 停, 图, 鉴 lost their inner gaps): the 3 px
# stroke is too heavy once the 82 px glyphs are shown at 32-46 px. 1 px still reads bold and keeps the counters open.
if 'OPPOSans' in FONT.name: TITLE_STROKE = 1
TITLES = ('设置', '暂停一下', '休息一下', '泡泡全打光啦！', '失败了', '泡泡碰到护栏啦', '复活', '连胜中断', '体力', '泡泡图鉴',
          '挑战失败', '太棒了，挑战成功！', '全部通关，太棒了！', '差一点就成功了！', '复活成功！')


def subtract_rect(free, used):
    """Split free rectangles around an occupied rectangle, discarding contained duplicates."""
    x, y, w, h = used
    remaining = []
    for fx, fy, fw, fh in free:
        if x >= fx + fw or x + w <= fx or y >= fy + fh or y + h <= fy:
            remaining.append((fx, fy, fw, fh))
            continue
        if x > fx: remaining.append((fx, fy, x - fx, fh))
        if x + w < fx + fw: remaining.append((x + w, fy, fx + fw - x - w, fh))
        if y > fy: remaining.append((fx, fy, fw, y - fy))
        if y + h < fy + fh: remaining.append((fx, y + h, fw, fy + fh - y - h))
    return [a for i, a in enumerate(remaining) if not any(
        i != j and a[0] >= b[0] and a[1] >= b[1] and a[0] + a[2] <= b[0] + b[2]
        and a[1] + a[3] <= b[1] + b[3] and (a != b or j < i)
        for j, b in enumerate(remaining))]


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
        if c.isspace():
            # No ink for blanks (OPPOSans-S has no space glyph; a stroked fallback space left a 13x2 px sliver).
            glyphs.append(dict(code=ord(c), x=0, y=0, w=0, h=0, advance=chosen.getlength(c) / FONT_SIZE, bearingX=0, bearingY=0))
            continue
        # Bake a subtle rounded emboldening at 48px, not repeated offset draws at runtime.
        box = chosen.getbbox(c, anchor='ls', stroke_width=1)
        draw_font, size = chosen, FONT_SIZE
        # OPPOSans' '$' and 'j' run a few px taller than a cell: bake just those a size or two smaller (same advance).
        while max(box[2] - box[0], box[3] - box[1]) + 4 > CELL and size > FONT_SIZE - 6:
            size -= 1; draw_font = ImageFont.truetype(chosen.path, size)
            if chosen is fallback: draw_font.set_variation_by_axes([700])
            box = draw_font.getbbox(c, anchor='ls', stroke_width=1)
        w, h = box[2] - box[0], box[3] - box[1]
        x, y = (i % cols) * CELL + 2, (i // cols) * CELL + 2
        assert w + 4 <= CELL and h + 4 <= CELL, (c, box)
        ImageDraw.Draw(image).text((x - box[0], y - box[1]), c, font=draw_font,
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
    # Pack title ink bounds (plus 4 px sampling gutters) beside/below the glass assets.
    title_chars = sorted(set(''.join(TITLES)))
    free0 = [(0, art_top, SIZE, SIZE - art_top)]
    for slot in slots:
        free0 = subtract_rect(free0, (slot['x'] - 4, slot['y'] - 4, slot['w'] + 8, slot['h'] + 8))
    # 2026-11-03 18:42 (Mac build: a glyph overflowed): FreeType on the Mac draws a pixel or two larger than elsewhere,
    # so the bold titles step down from TITLE_SIZE (to 72 px at most, the largest size they are shown at) until they pack.
    for title_size in range(TITLE_SIZE, 71, -1):
        title_font = ImageFont.truetype(str(FONT), title_size)
        title_missing = bytes(title_font.getmask('\uffff'))
        free, placed = list(free0), []
        boxes = [(c, title_font.getbbox(c, anchor='ls', stroke_width=TITLE_STROKE)) for c in title_chars]
        boxes.sort(key=lambda item: -(item[1][3] - item[1][1]))
        for c, box in boxes:
            assert bytes(title_font.getmask(c)) != title_missing, f'Missing rounded title glyph: {c}'
            w, h = box[2] - box[0], box[3] - box[1]
            candidates = [r for r in free if r[2] >= w + 8 and r[3] >= h + 8]
            if not candidates: break
            cell = min(candidates, key=lambda r: (min(r[2] - w - 8, r[3] - h - 8), r[1], r[0]))
            free = subtract_rect(free, (cell[0], cell[1], w + 8, h + 8)); placed.append((c, box, cell))
        if len(placed) == len(boxes): break
    assert len(placed) == len(boxes), ('title glyphs do not fit', TITLE_SIZE, art_top)
    title_glyphs = []
    for c, box, cell in placed:
        w, h = box[2] - box[0], box[3] - box[1]
        x, ty = cell[0] + 4, cell[1] + 4
        ImageDraw.Draw(image).text((x - box[0], ty - box[1]), c, font=title_font, fill='white', anchor='ls',
                                  stroke_width=TITLE_STROKE, stroke_fill='white')
        title_glyphs.append(dict(code=ord(c), x=x, y=ty, w=w, h=h,
                                 advance=title_font.getlength(c) / title_size,
                                 bearingX=box[0] / title_size, bearingY=box[1] / title_size))
    title_glyphs.sort(key=lambda g: g['code'])
    (out / 'titles.json').write_text(json.dumps(dict(size=SIZE, fontSize=title_size,
        characters=''.join(title_chars), glyphs=title_glyphs, slots=[], titles=TITLES), ensure_ascii=False))
    image.save(out / 'bars.png')
    target.write_text(json.dumps(dict(size=SIZE, fontSize=FONT_SIZE, cell=CELL,
        characters=''.join(chars), glyphs=glyphs, slots=slots, fallbackCharacters=''.join(fallback_chars),
        fingerprint=fingerprint), ensure_ascii=False))
    print(json.dumps(dict(barGlyphs=len(chars), fallback=''.join(fallback_chars), usedHeight=y), ensure_ascii=False))


if __name__ == '__main__':
    build()
