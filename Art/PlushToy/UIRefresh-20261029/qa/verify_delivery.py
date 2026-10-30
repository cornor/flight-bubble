"""Read-only asset QA; generates review sheets and measurements, never changes delivery PNGs."""
from pathlib import Path
import json
from PIL import Image, ImageDraw
import numpy as np

BASE = Path(__file__).resolve().parent.parent
manifest = json.loads((BASE / 'prompts.json').read_text())
manifest['assets'] = [a for a in manifest['assets'] if a.get('batch') != 'second-20261029']
positions = {
    'T1': (24, 72), 'T4': (338, 72), 'T5': (602, 72),
    'T6': (826, 72), 'T9': (1106, 72),
    'T7': (338, 354), 'T8': (802, 354),
    'T13': (338, 510), 'T14': (802, 510), 'N9': (338, 698),
}
report = []
for bg_name, bg_color in [('white', '#FFFFFF'), ('light-blue', '#E8F2FF')]:
    sheet = Image.new('RGB', (1240, 910), bg_color)
    draw = ImageDraw.Draw(sheet)
    draw.text((24, 22), f'PRIORITY BATCH / {bg_name} / assets shown at native size except T1', fill='#1D3A7A')
    for asset in manifest['assets']:
        path = BASE / 'delivery' / asset['file']
        if not path.exists():
            continue
        im = Image.open(path)
        assert list(im.size) == asset['size'], (asset['id'], im.size, asset['size'])
        rgba = im.convert('RGBA')
        alpha = np.asarray(rgba)[:, :, 3]
        if bg_name == 'white':
            edges = np.concatenate([alpha[0], alpha[-1], alpha[:, 0], alpha[:, -1]])
            corners = [int(alpha[y, x]) for y, x in [(0, 0), (0, -1), (-1, 0), (-1, -1)]]
            ys, xs = np.where(alpha > 8)
            report.append({
                'id': asset['id'], 'path': str(path.relative_to(BASE)),
                'size': list(im.size), 'mode': im.mode,
                'alpha_range': [int(alpha.min()), int(alpha.max())],
                'corner_alpha': corners, 'outer_edge_alpha_max': int(edges.max()),
                'visible_bbox_alpha_gt_8': [int(xs.min()), int(ys.min()), int(xs.max())+1, int(ys.max())+1],
                'center_rgba': list(rgba.getpixel((im.width//2, im.height//2))),
            })
            if asset['alpha']:
                assert im.mode == 'RGBA', asset['id']
                assert max(corners) <= 2, (asset['id'], corners)
            else:
                assert int(alpha.min()) == 255, asset['id']
        if asset['id'] == 'T1':
            rgba = rgba.resize((270, 480), Image.Resampling.LANCZOS)
        x, y = positions[asset['id']]
        draw.text((x, y-20), f"{asset['id']}  {im.width} x {im.height}", fill='#1D3A7A')
        sheet.paste(rgba, (x, y), rgba)
    sheet.save(BASE / 'qa' / f'priority-{bg_name}.png')
(BASE / 'qa' / 'measurements.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))

# Render nine-slice samples without modifying source assets.
def nine_slice(im, size, border):
    left, top, right, bottom = border
    sw, sh = im.size
    dw, dh = size
    sx = [0, left, sw-right, sw]
    sy = [0, top, sh-bottom, sh]
    dx = [0, left, dw-right, dw]
    dy = [0, top, dh-bottom, dh]
    out = Image.new('RGBA', size)
    for yi in range(3):
        for xi in range(3):
            piece = im.crop((sx[xi], sy[yi], sx[xi+1], sy[yi+1]))
            piece = piece.resize((dx[xi+1]-dx[xi], dy[yi+1]-dy[yi]), Image.Resampling.LANCZOS)
            out.paste(piece, (dx[xi], dy[yi]))
    return out

for bg_name, bg_color in [('white', '#FFFFFF'), ('light-blue', '#E8F2FF')]:
    sheet = Image.new('RGB', (1120, 800), bg_color)
    draw = ImageDraw.Draw(sheet)
    samples = [
        ('board-frame.png', (576, 432), (36, 36, 36, 36), (24, 50)),
        ('button-blue.png', (720, 108), (60, 36, 60, 36), (24, 540)),
        ('button-yellow.png', (720, 108), (60, 36, 60, 36), (24, 680)),
    ]
    for name, size, border, pos in samples:
        im = Image.open(BASE / 'delivery/theme' / name).convert('RGBA')
        expanded = nine_slice(im, size, border)
        draw.text((pos[0], pos[1]-20), f'{name}: {size}, border L/T/R/B {border}', fill='#1D3A7A')
        sheet.paste(expanded, pos, expanded)
    sheet.save(BASE / 'qa' / f'nine-slice-{bg_name}.png')
