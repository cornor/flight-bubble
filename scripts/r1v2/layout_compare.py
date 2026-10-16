"""R1 v2: mockup vs v1 vs v2 layout comparison (540x960 logical, drawn at 2x).
usage: python3 layout_compare.py MOCKUP V1_SCREENSHOT FONT OUT"""
import sys
from PIL import Image, ImageDraw, ImageFont
mock, v1, fontp, out = sys.argv[1:5]
K = 2
F = lambda s: ImageFont.truetype(fontp, s * K, index=2)  # SC
def panel(img):
    return img.convert('RGB').resize((540 * K, 960 * K), Image.LANCZOS)
A = panel(Image.open(mock)); B = panel(Image.open(v1))
# v2 wireframe (logical coordinates)
C = Image.new('RGB', (540 * K, 960 * K), (236, 233, 226)); d = ImageDraw.Draw(C)
def box(x0, y0, x1, y1, fill, text='', tc=(40, 30, 20), size=11, outline=(90, 70, 50)):
    d.rectangle([x0 * K, y0 * K, x1 * K, y1 * K], fill=fill, outline=outline, width=K)
    if text:
        d.multiline_text(((x0 + x1) / 2 * K, (y0 + y1) / 2 * K), text, font=F(size), fill=tc, anchor='mm', align='center', spacing=2)
box(0, 0, 540, 84, (122, 90, 58), '顶栏 0–84', (255, 246, 224))
box(12, 6, 64, 54, (168, 120, 74), '暂停', (255, 246, 224), 10)
box(72, 8, 196, 50, (168, 120, 74), '关卡牌 第N关', (255, 246, 224), 10)
box(22, 56, 518, 79, (70, 150, 60), '进度条（百分比在条内右端）', (255, 255, 255), 10)
box(0, 84, 540, 960, (120, 170, 90), outline=(120, 170, 90))
box(10, 92, 530, 556, (176, 186, 150), '虫区 10–530 × 92–556\n外围草地，虫群底下低饱和地面\n虫子按原逻辑投影绘制，可见范围 y 114–572', size=12)
d.line([0, 562 * K, 540 * K, 562 * K], fill=(200, 40, 40), width=K)
d.text((6 * K, 563 * K), '防线 y=562（逻辑，不能动）', font=F(10), fill=(200, 40, 40))
box(0, 548, 540, 586, (240, 236, 222), '白色尖头篱笆 548–586', size=10)
for i in range(7):
    cx = 42 + i * 456 / 6
    box(cx - 35, 592, cx + 35, 668, (118, 84, 56), '', outline=(214, 186, 140))
    box(cx - 24, 600, cx + 24, 660, (200, 150, 60), '植物≤100高\n盆底 660', (255, 255, 255), 8)
    box(cx - 22, 664, cx + 22, 682, (226, 200, 160), str(i + 1), (70, 50, 30), 10)
d.text((6 * K, 684 * K), '种植格 x=42+76i（逻辑），格 592–668，号牌 664–682', font=F(10), fill=(30, 50, 20))
lw = 516 / 4
for lane in range(4):
    x0 = 12 + lw * lane + 5; x1 = x0 + lw - 10
    box(x0, 696, x1, 862, (62, 104, 58), '', outline=(48, 84, 44))
    for r, y in enumerate((725, 775, 825)):
        box((x0 + x1) / 2 - 30, y - 28, (x0 + x1) / 2 + 30, y + 28, (90, 140, 210) if r == 0 else (110, 150, 200), '第%d排\ny=%d' % (r + 1, y), (255, 255, 255), 8)
d.text((6 * K, 864 * K), '队列面板 696–862，行心 725/775/825（原 QueueY），前排角标用植物主色', font=F(10), fill=(30, 50, 20))
for (x0, x1, t) in ((27, 182, '翻土'), (190, 350, '优先栽种'), (356, 512, '喷壶')):
    box(x0, 880, x1, 952, (176, 120, 66), t + '\n数量角标在右上', (255, 246, 224), 10)
W = 540 * K; G = 24 * K; H = 960 * K; T = 40 * K
canvas = Image.new('RGB', (W * 3 + G * 4, H + T + G), (250, 248, 244)); dc = ImageDraw.Draw(canvas)
for i, (img, t) in enumerate(((A, '示意图（目标稿，按 540×960 缩放）'), (B, 'v1 验收包 第 4 关'), (C, 'v2 布局（逻辑坐标）'))):
    x = G + i * (W + G); canvas.paste(img, (x, T)); dc.text((x, 8 * K), t, font=F(16), fill=(40, 30, 20))
    for y in (84, 562, 592, 696, 880):
        dc.line([x - 6 * K, T + y * K, x, T + y * K], fill=(200, 40, 40), width=K)
canvas.save(out, optimize=True)
print(canvas.size)
