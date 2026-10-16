"""v02 对照图：每行 [v02 概念图 540x960] [真实比例参考] [植物版同关截图（有则放）]"""
import sys
from PIL import Image, ImageDraw, ImageFont
root = sys.argv[1]
F = ImageFont.truetype("/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc", 22)
W, H, G, T = 540, 960, 16, 40
rows = [
  ("01-smiley-intro.png", "reference/real-scale-sector02.png", "../../../Builds/FX-batch3-evidence/pressure-final/battle-4-colors2-fleet2.png", "第 2 关 元气笑脸"),
  ("02-cake-cascade.png", "reference/real-scale-sector07.png", None, "第 7 关 奶油蛋糕"),
  ("03-ghost-merge.png", "reference/real-scale-sector08.png", None, "第 8 关 委屈幽灵"),
]
def fit(p):
    im = Image.open(p).convert("RGB")
    if abs(im.width / im.height - W / H) > 0.02:  # 截图比例不同：按宽缩放后居中裁成 540x960
        s = W / im.width; im = im.resize((W, round(im.height * s)), Image.LANCZOS)
        top = max(0, (im.height - H) // 2 - 60); return im.crop((0, top, W, top + H))
    return im.resize((W, H), Image.LANCZOS)
out = Image.new("RGB", (3 * W + 4 * G, len(rows) * (H + T + G) + G), (245, 245, 245))
d = ImageDraw.Draw(out)
for r, (a, b, c, name) in enumerate(rows):
    y = G + r * (H + T + G)
    for k, (p, lab) in enumerate([(a, "v02 概念图（缩到 540×960）"), (b, "真实比例参考"), (c, "植物版实机截图")]):
        x = G + k * (W + G)
        d.text((x, y + 4), f"{name} · {lab}" if p else "", fill=(40, 40, 40), font=F)
        if p:
            out.paste(fit(f"{root}/{p}"), (x, y + T))
out.save(f"{root}/qa/对照-v02-真实比例-植物版.png")
print(out.size)
