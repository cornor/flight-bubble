"""泡泡阵底色方案对照（2026-10-15 用户要求去掉灰底后）：真实关卡、真实间距、v3 泡泡尺寸（6.4 / 13），
底下用实际交付的背景图。方案：天空直接做底 / 天空加半透明白雾 / 浅灰紫 / 原灰底。
用法：python3 bg_options.py <输出png>（仓库根目录）"""
import sys, json
from PIL import Image, ImageDraw, ImageFont
LV = 'UnityProject/Assets/Flightbeat/Resources/levels/'
S = 2
F = ImageFont.truetype('Builds/fonts/NotoSansSC.ttf', 24)
bg = Image.open('Art/R1/art-v3/delivery/scene/background.png').convert('RGB').resize((540 * S, 960 * S), Image.LANCZOS)
def ex(x): return 270 + (x - 270) * 1.565
def ey(y): return 562 + (y - 562) * 1.31
def base(kind):
    im = bg.crop((10 * S, 92 * S, 530 * S, 556 * S)).convert('RGBA')
    if kind == 'mist':
        im.alpha_composite(Image.new('RGBA', im.size, (255, 255, 255, 140)))
    elif kind.startswith('#'):
        im = Image.new('RGBA', im.size, tuple(int(kind[i:i + 2], 16) for i in (1, 3, 5)) + (255,))
    return im
def beads(im, name):
    d = json.load(open(LV + name + '.json')); p = d['palette']; pal = [tuple(p[i:i + 3]) for i in range(0, len(p), 3)]
    cols = d['columns']; dia = 13 if cols == 24 else 6.4; r = dia / 2; dr = ImageDraw.Draw(im)
    for i, ci in enumerate(d['cells']):
        if ci is None or ci < 0: continue
        row, col = divmod(i, cols)
        x = ex(d['gridX'] + col * d['pitchX']) - 10; y = ey(d['gridY'] + row * d['pitchY']) - 92
        if not (0 <= y <= 464): continue
        c = pal[ci]; rim = (140, 150, 162) if c == (255, 255, 255) else ((150, 154, 160) if c == (45, 45, 45) else tuple(int(v * .62) for v in c))
        dr.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c + (255,), outline=rim + (255,), width=max(1, int(S * .8)))
        dr.ellipse([(x - r * .55) * S, (y - r * .6) * S, (x - r * .1) * S, (y - r * .15) * S], fill=tuple(min(255, v + 80) for v in c) + (255,))
    return im
opts = [('sky', '① 天空直接做底'), ('mist', '② 天空 + 半透明白雾'), ('#BAB7C7', '③ 浅灰紫 #BAB7C7'), ('#8C919C', '④ 原灰底 #8C919C')]
levels = [('training01', '教学 1'), ('sector02', '第 2 关'), ('sector08', '第 8 关')]
W, H, T = 520 * S, 464 * S, 44
sheet = Image.new('RGB', (len(opts) * (W + 12), len(levels) * (H + T) + 8), (250, 250, 250)); dr = ImageDraw.Draw(sheet)
for r, (lv, lname) in enumerate(levels):
    for k, (kind, label) in enumerate(opts):
        x, y = k * (W + 12), r * (H + T)
        dr.text((x + 8, y + 8), f'{lname} · {label}', fill=(30, 30, 30), font=F)
        sheet.paste(beads(base(kind), lv).convert('RGB'), (x, y + T))
sheet.save(sys.argv[1]); print(sheet.size)
