"""Read-only asset checks and QA previews; final artwork is created with ImageGen."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = {
    'theme/pad.png': (160, 64),
    'lose/hero-sad.png': (640, 640),
    'loading/logo-stacked.png': (900, 700),
    'loading/background.png': (1080, 2340),
    'home/logo.png': (1000, 300),
    'home/bear-idle.png': (1080, 1080),
    'home/bear-blink.png': (1080, 1080),
    'home/frog.png': (1080, 1080),
    'home/chick.png': (1080, 1080),
}
report = {'files': []}
for name, size in EXPECTED.items():
    path = ROOT / 'delivery' / name
    im = Image.open(path).convert('RGBA')
    a = np.array(im)
    alpha = a[:, :, 3]
    edge = np.concatenate([alpha[0], alpha[-1], alpha[:, 0], alpha[:, -1]])
    opaque = name == 'loading/background.png'
    assert im.size == size, (name, im.size, size)
    assert (alpha == 255).all() if opaque else alpha.min() == 0
    if not opaque:
        assert edge.max() <= 8, (name, int(edge.max()))
    report['files'].append({
        'path': 'delivery/' + name, 'size': list(im.size),
        'alpha_min': int(alpha.min()), 'alpha_max': int(alpha.max()),
        'outer_edge_max_alpha': int(edge.max()),
        'opaque': opaque, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    })

idle = np.array(Image.open(ROOT / 'delivery/home/bear-idle.png').convert('RGBA'))
blink = np.array(Image.open(ROOT / 'delivery/home/bear-blink.png').convert('RGBA'))
packing = json.loads((ROOT / 'qa/blink-packing.json').read_text())
yy, xx = np.mgrid[:1080, :1080]
x, y = xx + .5, yy + .5
mask = np.zeros((1080, 1080), dtype=bool)
for points in packing['source_eye_polygons']:
    p = np.array(points) * 1080 / 1254
    inside = np.zeros_like(mask)
    for i in range(len(p)):
        ax, ay = p[i]
        bx, by = p[i - 1]
        if ay == by:
            continue
        inside ^= ((ay > y) != (by > y)) & (x < (bx-ax) * (y-ay) / (by-ay) + ax)
    mask |= inside
diff = np.any(idle != blink, axis=2)
assert not np.any(diff & ~mask)
assert np.array_equal(idle[:, :, 3], blink[:, :, 3])
assert diff.any()
dy, dx = np.where(diff)
report['blink'] = {
    'changed_pixels': int(diff.sum()),
    'outside_eye_regions_changed_pixels': int((diff & ~mask).sum()),
    'alpha_identical': True,
    'difference_bbox': [int(dx.min()), int(dy.min()), int(dx.max()+1), int(dy.max()+1)],
}
report['wordmarks'] = {'expected': '泡泡小队', 'manual_character_check': ['泡', '泡', '小', '队'], 'ocr': 'qa/wordmark-ocr.json'}
report['status'] = 'passed'
(ROOT / 'qa/verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')

transparent = [name for name in EXPECTED if name != 'loading/background.png']
for page in range(2):
    sheet = Image.new('RGB', (1200, 1360), '#eeeeee')
    draw = ImageDraw.Draw(sheet)
    for row, name in enumerate(transparent[page*4:(page+1)*4]):
        draw.text((15, row*340+8), name, fill='black')
        for column, color in enumerate(['#FFFFFF', '#E8F2FF']):
            tile = Image.new('RGBA', (585, 300), color)
            im = Image.open(ROOT / 'delivery' / name).convert('RGBA')
            im.thumbnail((560, 280), Image.Resampling.LANCZOS)
            tile.alpha_composite(im, ((585-im.width)//2, (300-im.height)//2))
            sheet.paste(tile.convert('RGB'), (column*600+5, row*340+30))
    sheet.save(ROOT / f'qa/all-assets-white-blue-{page+1}.png')

composites = []
for frame in ['bear-idle', 'bear-blink']:
    im = Image.new('RGBA', (1080, 1080))
    for layer in ['frog', 'chick', frame]:
        im.alpha_composite(Image.open(ROOT / f'delivery/home/{layer}.png').convert('RGBA'))
    im.save(ROOT / f'qa/home-composite-{frame}.png')
    composites.append(im)
sheet = Image.new('RGB', (1200, 1260), '#eeeeee')
draw = ImageDraw.Draw(sheet)
for row, color in enumerate(['#FFFFFF', '#E8F2FF']):
    for column, im in enumerate(composites):
        tile = Image.new('RGBA', (585, 585), color)
        sized = im.resize((575, 575), Image.Resampling.LANCZOS)
        tile.alpha_composite(sized, (5, 5))
        sheet.paste(tile.convert('RGB'), (column*600+5, row*630+35))
        draw.text((column*600+15, row*630+10), ['idle', 'blink'][column]+' / '+color, fill='black')
sheet.save(ROOT / 'qa/home-frames-white-blue.png')

overview = Image.new('RGB', (1400, 1320), '#E8F2FF')
draw = ImageDraw.Draw(overview)
draw.text((30, 20), 'UIRefresh-B-20261030 / 9 delivery PNGs', fill='#223355')
loading = Image.open(ROOT / 'delivery/loading/background.png').convert('RGBA')
logo = Image.open(ROOT / 'delivery/loading/logo-stacked.png').convert('RGBA')
loading.alpha_composite(logo, (90, 500))
loading.thumbnail((540, 1170))
overview.paste(loading.convert('RGB'), (25, 75))
for name, pos, limit in [
    ('home/logo.png', (600, 70), (750, 230)),
    ('lose/hero-sad.png', (645, 840), (370, 370)),
    ('theme/pad.png', (1090, 995), (160, 64)),
]:
    im = Image.open(ROOT / 'delivery' / name).convert('RGBA')
    im.thumbnail(limit)
    overview.paste(im, pos, im)
hero = composites[0].copy()
hero.thumbnail((700, 600))
overview.paste(hero, (675, 280), hero)
draw.text((600, 1275), 'Home layers: frog -> chick -> bear; swap bear frame to blink', fill='#223355')
overview.save(ROOT / 'qa/overview.png')
print(json.dumps({'status': report['status'], 'files': len(EXPECTED), 'blink': report['blink']}, ensure_ascii=False))
