#!/usr/bin/env python3
"""Audit all playable colors, states and lane geometry; write art coverage outputs."""
from pathlib import Path
import json
ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'UnityProject/Assets/Flightbeat/Resources'
entries = json.loads((ROOT / 'Art/skin-colors.json').read_text())['colors']
colors = {e['hex'].upper(): e for e in entries}
assert len(colors) == len(entries)
atlas = json.loads((DATA / 'GardenR1/atlas.json').read_text())
slots = {s['id']: s for s in atlas['slots']}
levels = ['training01', 'training02'] + [f'sector{i:02}' for i in range(1,261)]
rows = []
for index, name in enumerate(levels, 1):
    d = json.loads((DATA / 'levels' / (name + '.json')).read_text())
    hexes = sorted({'#' + ''.join(f'{c:02X}' for c in d['palette'][q['color']*3:q['color']*3+3]) for q in d['queue']})
    for h in hexes:
        e = colors[h]
        for state in ['idle','attack','spent']:
            slot = slots[f'plants/{e["slotId"]}/{h[1:]}/{state}']
            assert slot['atlas'] == 1, (name,h,state)
    lane_count = d.get('laneCount', 4)
    width = min(108, 500 / lane_count - 8)
    assert width <= 500 / lane_count - 8
    kinds = {}
    for q in d['queue']:
        if q.get('kind',0): kinds[str(q['kind'])] = kinds.get(str(q['kind']),0) + 1
    rows.append(dict(number=index, id=name, colors=hexes, placeholders=[h for h in hexes if colors[h]['status']=='placeholder'],
                     kinds=kinds, lanes=lane_count, queueWidth=width, maximumSlots=max(min(d.get('initialSlots',0),8) if d.get('initialSlots',0)>0 else 5, min(d['slotLimit'],8)) if d.get('slotLimit',0)>0 else 7))
used = set().union(*(set(r['colors']) for r in rows))
assert used == set(colors), (used-set(colors),set(colors)-used)
for state in ['idle','attack','spent']:
    assert f'plants/color-8c919c/8C919C/{state}' in slots
report = dict(levelCount=len(rows), colorCount=len(colors), placeholders=[e for e in entries if e['status']=='placeholder'],
              atlases=['atlas.png','creatures.png'], levels=rows)
(ROOT/'Builds').mkdir(exist_ok=True)
(ROOT/'Builds/包装覆盖清单.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
lines=['# 包装覆盖清单','',f'由 `python3 scripts/r1v3/coverage.py` 生成。共 {len(rows)} 关、{len(colors)} 种颜色；角色均由第二张图集绘制。',
       '','已有交付角色保留原显示名，以保持前四关一致；缺图颜色暂用颜色名加“队员”。补齐 idle / attack / spent 后，重新构建自动切换为正式素材。',
       '','| 颜色 | 角色 ID | 槽位 ID | 显示名 | 状态 |','|---|---|---|---|---|']
for e in entries:lines.append('| {hex} | {creatureId} | {slotId} | {displayName} | {status} |'.format(**e))
lines += ['','神秘队员后两排使用独立灰色占位素材 `plants/color-8c919c/8C919C/idle`，不读取真实颜色贴图。',
          '','| 总关号 | 数据 ID | 跑道 | 颜色 | 占位颜色 | 特殊 kind（数量） |','|---|---|---|---|---|---|']
for r in rows:lines.append(f"| {r['number']} | {r['id']} | {r['lanes']} | {', '.join(r['colors'])} | {', '.join(r['placeholders']) or '无'} | {', '.join(k+'×'+str(v) for k,v in r['kinds'].items()) or '无'} |")
(ROOT/'docs/包装覆盖清单.md').write_text('\n'.join(lines)+'\n')
print(json.dumps(dict(levels=len(rows), colors=len(colors), placeholderColors=len(report['placeholders']),fiveLanes=sum(r['lanes']==5 for r in rows))))
