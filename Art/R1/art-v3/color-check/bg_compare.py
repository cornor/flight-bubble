"""泡泡阵底色对照：真实关卡、真实间距（60 列，横 8.6、纵 6.55 逻辑像素），左示意图浅蓝底、右候选底色，各附绿弱模拟。
用法：python3 bg_compare.py <输出png> <候选色> （仓库根目录）"""
import sys, json
sys.path.insert(0, 'Art/Concepts/color-check-r1/scripts')
from color import simulate
import numpy as np
from PIL import Image, ImageDraw, ImageFont
LV = 'UnityProject/Assets/Flightbeat/Resources/levels/'
S = 2; D = 5.5  # 现有正式关圆珠显示直径
F = ImageFont.truetype('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc', 22)
def ex(x): return 270 + (x - 270) * 1.565
def ey(y): return 562 + (y - 562) * 1.31
def hexrgb(h): return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))
def field(name, bg):
    d = json.load(open(LV + name + '.json')); p = d['palette']; pal = [tuple(p[i:i+3]) for i in range(0, len(p), 3)]
    cols = d['columns']; teach = cols == 24; dia = 10.5 if teach else D
    img = Image.new('RGB', (540 * S, 470 * S), hexrgb(bg)); dr = ImageDraw.Draw(img); r = dia / 2
    for i, ci in enumerate(d['cells']):
        if ci is None or ci < 0: continue
        row, col = divmod(i, cols)
        x = ex(d['gridX'] + col * d['pitchX']); y = ey(d['gridY'] + row * d['pitchY']) - 92
        if not (0 <= y <= 466): continue
        c = pal[ci]; rim = tuple(int(v * .62) for v in c) if c != (255, 255, 255) else (140, 150, 162)
        if c == (45, 45, 45): rim = (150, 154, 160)
        dr.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c, outline=rim, width=1)
        dr.ellipse([(x - r * .55) * S, (y - r * .6) * S, (x - r * .05) * S, (y - r * .1) * S], fill=tuple(min(255, v + 80) for v in c))
    return img
def cvd(img, kind):
    a = np.asarray(img, float); return Image.fromarray(np.clip(simulate(a, kind), 0, 255).astype('uint8'))
out_path, cand = sys.argv[1], sys.argv[2]
levels = ['training02', 'sector02', 'sector07', 'sector08']
bgs = [('示意图浅蓝 #C8E3FE', 'C8E3FE'), (f'候选 #{cand}', cand)]
W, H = 540 * S, 470 * S; T = 40
sheet = Image.new('RGB', (4 * W + 50, len(levels) * (H + T) + 10), (250, 250, 250)); dr = ImageDraw.Draw(sheet)
for r, lv in enumerate(levels):
    for b, (lab, bg) in enumerate(bgs):
        im = field(lv, bg)
        for k, kind in enumerate(('normal', 'deutan')):
            x = (b * 2 + k) * (W + 10); y = r * (H + T)
            dr.text((x + 6, y + 6), f'{lv} · {lab} · {"正常" if kind == "normal" else "绿弱模拟"}', fill=(30, 30, 30), font=F)
            sheet.paste(im if kind == 'normal' else cvd(im, 'deutan'), (x, y + T))
sheet.save(out_path); print(sheet.size)
