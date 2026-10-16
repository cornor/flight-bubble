"""泡泡阵底色挑选：候选底色与 22 个关卡色值（正式 18 + 教学 4）的最小 CIEDE2000 色差，正常视觉和两种色弱模拟。
用法：python3 bg_pick.py  （在仓库根目录运行）"""
import sys, json
sys.path.insert(0, 'Art/Concepts/color-check-r1/scripts')
from color import hex2rgb, rgb2lab, de2000, simulate
import numpy as np
PAL = ['FFFFFF','30D183','46D9FF','2982F4','65392D','B672FD','FFCAA9','FFDF48','FF525B','92EF4D','FF7E27',
       '2D2D2D','0C7E1C','9C225E','696CAF','C8C5FF','FFC5E0','C5FFE4','42A9FF','62E18E','FF9D30','BD80FF']
R1 = ['42A9FF','FF9D30','62E18E','BD80FF','FFFFFF','2982F4','FFDF48','FF7E27','FF525B','2D2D2D']
CANDS = {'示意图原色': 'C8E3FE', '浅蓝灰': 'BCCFE0', '中浅蓝灰': 'A9BDD2', '中蓝灰': '97AEC6', '灰蓝偏暖': 'A8B4C4',
         '中灰蓝(低饱和)': 'A3B1C2', '石板蓝灰': '8FA3BA'}
def lab(h, kind='normal'): return rgb2lab(simulate(np.array(hex2rgb(h), float), kind))
out = {}
for name, bg in CANDS.items():
    row = {}
    for kind in ('normal', 'deutan', 'protan'):
        ds = sorted((de2000(lab(bg, kind), lab(c, kind)), c) for c in PAL)
        r1 = sorted((de2000(lab(bg, kind), lab(c, kind)), c) for c in R1)
        row[kind] = {'min_all': [round(ds[0][0], 1), ds[0][1]], 'min_r1': [round(r1[0][0], 1), r1[0][1]],
                     'white': round(de2000(lab(bg, kind), lab('FFFFFF', kind)), 1)}
    out[f'{name} #{bg}'] = row
    n = row['normal']; d = row['deutan']; p = row['protan']
    print(f"{name:12s} #{bg}  正常 最小{n['min_all']} R1最小{n['min_r1']} 白{n['white']} | 绿弱 最小{d['min_all']} | 红弱 最小{p['min_all']}")
json.dump(out, open('Art/R1/art-v3/color-check/bg_pick.json', 'w'), ensure_ascii=False, indent=1)
