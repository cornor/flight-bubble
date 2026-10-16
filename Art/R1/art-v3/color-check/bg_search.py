"""在低饱和底色范围内搜索：使 22 个关卡色值在正常/绿弱/红弱下的最小 CIEDE2000 色差最大。
用法：python3 bg_search.py（仓库根目录）"""
import sys, colorsys
sys.path.insert(0, 'Art/Concepts/color-check-r1/scripts')
from color import hex2rgb, rgb2lab, de2000, simulate
import numpy as np
PAL = ['FFFFFF','30D183','46D9FF','2982F4','65392D','B672FD','FFCAA9','FFDF48','FF525B','92EF4D','FF7E27',
       '2D2D2D','0C7E1C','9C225E','696CAF','C8C5FF','FFC5E0','C5FFE4','42A9FF','62E18E','FF9D30','BD80FF']
KINDS = ('normal', 'deutan', 'protan')
pal_lab = {k: [rgb2lab(simulate(np.array(hex2rgb(c), float), k)) for c in PAL] for k in KINDS}
res = []
for h in range(180, 260, 10):          # 蓝绿到蓝紫（贴合云端题材）
    for s in (0.06, 0.10, 0.14, 0.18, 0.24):
        for v in np.arange(0.46, 0.86, 0.03):
            rgb = np.array(colorsys.hsv_to_rgb(h / 360, s, v)) * 255
            worst = []
            for k in KINDS:
                b = rgb2lab(simulate(rgb, k))
                worst.append(min(de2000(b, p) for p in pal_lab[k]))
            hexs = '%02X%02X%02X' % tuple(int(round(x)) for x in rgb)
            res.append((min(worst), worst, hexs, h, s, round(float(v), 2)))
res.sort(reverse=True)
for r in res[:12]:
    print(f"#{r[2]} h{r[3]} s{r[4]} v{r[5]}  三视觉最小 {r[0]:.1f}  正常 {r[1][0]:.1f} 绿弱 {r[1][1]:.1f} 红弱 {r[1][2]:.1f}")
