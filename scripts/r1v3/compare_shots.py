#!/usr/bin/env python3
"""Side-by-side review sheet: bubble mockup | v3 build | plant v2 build, plus all four v3 battle shots.
usage: python3 scripts/r1v3/compare_shots.py <v3 play dir> <output png> [plant v2 play dir]"""
import sys, glob, os
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
play, out = sys.argv[1], sys.argv[2]
plant = sys.argv[3] if len(sys.argv) > 3 else os.path.join(ROOT, 'Builds/FX-batch3-evidence/pressure-final')
font = next((p for p in ['/System/Library/Fonts/PingFang.ttc', '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',
                         os.path.join(ROOT, 'Builds/fonts/NotoSansSC.ttf')] if os.path.exists(p)), None)
F = ImageFont.truetype(font, 22) if font else ImageFont.load_default()
W, H, G, T = 540, 960, 14, 36
def fit(p):
    im = Image.open(p).convert('RGB')
    s = W / im.width; im = im.resize((W, round(im.height * s)), Image.LANCZOS)
    return im.crop((0, 0, W, H)) if im.height >= H else im
def last(pattern):
    hits = sorted(glob.glob(pattern)); return hits[-1] if hits else None
row1 = [(os.path.join(ROOT, 'Art/Concepts/bubble-squad-v02/01-smiley-intro.png'), '示意图（第 2 关）'),
        (last(os.path.join(play, '3-2.png')), 'v3 实机 第 2 关'),
        (last(os.path.join(plant, 'battle-4-*.png')), '植物 v2 实机 第 2 关')]
row2 = [(last(os.path.join(play, f'{k}-2.png')), n) for k, n in ((0, 'v3 实机 泡泡课堂 1/2'), (1, 'v3 实机 泡泡课堂 2/2'), (2, 'v3 实机 第 1 关'))]
rows = [row1, row2]
sheet = Image.new('RGB', (3 * W + 4 * G, len(rows) * (H + T + G) + G), (245, 246, 248)); d = ImageDraw.Draw(sheet)
for r, row in enumerate(rows):
    for k, (p, label) in enumerate(row):
        x, y = G + k * (W + G), G + r * (H + T + G)
        d.text((x, y + 4), label + ('' if p else '（缺）'), fill=(30, 30, 30), font=F)
        if p: sheet.paste(fit(p), (x, y + T))
sheet.save(out); print(out, sheet.size)
