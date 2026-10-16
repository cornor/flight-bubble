"""去掉灰底后"糊"的对照：泡泡大小（6.4 现在 / 7.6 略叠压）× 泡泡阵背后（实际背景图 / 纯色天空）。
真实关卡、真实间距，按 2 倍输出。用法：python3 bead_options.py <输出png>（仓库根目录）"""
import sys, json
from PIL import Image, ImageDraw, ImageFont
LV = 'UnityProject/Assets/Flightbeat/Resources/levels/'
S = 2
F = ImageFont.truetype('Builds/fonts/NotoSansSC.ttf', 24)
bg = Image.open('Art/R1/art-v3/delivery/scene/background.png').convert('RGB').resize((540 * S, 960 * S), Image.LANCZOS)
FLAT = (0x7E, 0xC4, 0xFC)   # the sky's own mid tone
def ex(x): return 270 + (x - 270) * 1.565
def ey(y): return 562 + (y - 562) * 1.31
def base(flat):
    return Image.new('RGBA', (520 * S, 464 * S), FLAT + (255,)) if flat else bg.crop((10 * S, 92 * S, 530 * S, 556 * S)).convert('RGBA')
def beads(im, name, dia):
    d = json.load(open(LV + name + '.json')); p = d['palette']; pal = [tuple(p[i:i + 3]) for i in range(0, len(p), 3)]
    cols = d['columns']; dia = dia * 13 / 6.4 if cols == 24 else dia; r = dia / 2; dr = ImageDraw.Draw(im)
    for i, ci in enumerate(d['cells']):     # row order: lower rows drawn later, so overlap reads as stacked bubbles
        if ci is None or ci < 0: continue
        row, col = divmod(i, cols)
        x = ex(d['gridX'] + col * d['pitchX']) - 10; y = ey(d['gridY'] + row * d['pitchY']) - 92
        if not (0 <= y <= 464): continue
        c = pal[ci]; rim = (140, 150, 162) if c == (255, 255, 255) else ((150, 154, 160) if c == (45, 45, 45) else tuple(int(v * .62) for v in c))
        dr.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c + (255,), outline=rim + (255,), width=max(1, int(S * .8)))
        dr.ellipse([(x - r * .55) * S, (y - r * .6) * S, (x - r * .1) * S, (y - r * .15) * S], fill=tuple(min(255, v + 80) for v in c) + (255,))
    return im
opts = [(6.4, False, 'A 现在：6.4 + 背景图'), (7.6, False, 'B 泡泡 7.6 略叠压 + 背景图'), (6.4, True, 'C 6.4 + 纯色天空'), (7.6, True, 'D 7.6 略叠压 + 纯色天空')]
levels = [('sector02', '第 2 关'), ('training01', '教学 1'), ('sector07', '第 7 关')]
W, H, T = 520 * S, 464 * S, 44
sheet = Image.new('RGB', (len(opts) * (W + 12), len(levels) * (H + T) + 8), (250, 250, 250)); dr = ImageDraw.Draw(sheet)
for r, (lv, lname) in enumerate(levels):
    for k, (dia, flat, label) in enumerate(opts):
        x, y = k * (W + 12), r * (H + T)
        dr.text((x + 8, y + 8), f'{lname} · {label}', fill=(30, 30, 30), font=F)
        sheet.paste(beads(base(flat), lv, dia).convert('RGB'), (x, y + T))
sheet.save(sys.argv[1]); print(sheet.size)
