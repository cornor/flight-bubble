#!/usr/bin/env python3
"""Plan step 2.1: colour table for every B-set level (game levels 3-262).

Writes Art/levels-b/palette-table.csv (opens in Excel/Numbers), palette-table.json and one
contact sheet per war zone (palette-sheet-zone<N>.png): the A board next to its colours, target
cell counts and shares, so subjects and prompts can be chosen per level.
"""
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from levelsb_common import (FIRST, LAST, ROOT, counts, label, level_colors, load_a, name, resource,
                            rgb)
from levelsb_render import board_image, font

ZONES = [30, 50, 60, 60, 60]          # Economy/WarZones.Catalog.cs Sizes
OUT = ROOT / 'Art/levels-b'


def zone_of(game_level):
    n = game_level - 2
    for z, size in enumerate(ZONES):
        if n <= size:
            return z + 1
        n -= size
    return len(ZONES)


def rows():
    for game in range(FIRST, LAST + 1):
        level, _ = load_a(game)
        board = counts(level['cells'])
        used = level_colors(level)
        colors = []
        for c in sorted(board, key=lambda c: (-board[c], c)):
            colors.append(dict(index=c, name=name(level, c), hex='#%02X%02X%02X' % rgb(level, c),
                               cells=board[c], share=round(board[c] / 3600, 4)))
        stream_only = [dict(index=c, name=name(level, c), hex='#%02X%02X%02X' % rgb(level, c))
                       for c in used if c not in board]
        yield dict(game=game, resource=resource(game), zone=zone_of(game), title=level.get('title', ''),
                   colorCount=len(board), background=colors[0], colors=colors, streamOnly=stream_only,
                   stream=len(level.get('stream') or []), members=len(level['queue']))


def sheet(zone, items):
    from PIL import Image, ImageDraw
    cols, cell_w, cell_h = 4, 470, 230
    rows_n = (len(items) + cols - 1) // cols
    img = Image.new('RGB', (cols * cell_w + 20, rows_n * cell_h + 60), (28, 32, 46))
    d = ImageDraw.Draw(img)
    d.text((14, 14), f'战区 {zone} 配色表（第 {items[0]["game"]}–{items[-1]["game"]} 关）', font=font(26), fill=(255, 229, 138))
    for k, it in enumerate(items):
        x, y = 10 + (k % cols) * cell_w, 56 + (k // cols) * cell_h
        level, _ = load_a(it['game'])
        img.paste(board_image(level['cells'], level, 3), (x, y + 26))
        d.text((x, y), f'第 {it["game"]} 关  {it["resource"]}  {it["title"]}', font=font(17), fill=(235, 238, 245))
        yy = y + 28
        for c in it['colors']:
            col = tuple(int(c['hex'][i:i + 2], 16) for i in (1, 3, 5))
            d.rectangle([x + 192, yy, x + 210, yy + 16], fill=col, outline=(90, 96, 120))
            d.text((x + 216, yy - 1), f'{c["name"]}', font=font(15), fill=(235, 238, 245))
            bar = int(150 * c['share'])
            d.rectangle([x + 262, yy + 3, x + 262 + max(bar, 1), yy + 13], fill=col)
            d.text((x + 418, yy - 1), f'{c["share"] * 100:4.1f}%', font=font(14), fill=(190, 198, 215))
            yy += 20
        if it['streamOnly']:
            d.text((x + 192, yy + 2), '仅沙流：' + '、'.join(s['name'] for s in it['streamOnly']), font=font(13), fill=(160, 168, 190))
    return img


def main():
    items = list(rows())
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'palette-table.json').write_text(json.dumps(items, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    with open(OUT / 'palette-table.csv', 'w', newline='', encoding='utf-8-sig') as f:
        w = csv.writer(f)
        w.writerow(['游戏关号', '资源名', '战区', '原关名', '棋盘颜色数', '背景色', '背景占比',
                    '颜色（名称 色值 格数 占比，按格数从多到少）', '仅在沙流的颜色', '沙流泡泡数', '队员数'])
        for it in items:
            w.writerow([it['game'], it['resource'], it['zone'], it['title'], it['colorCount'],
                        it['background']['name'], f'{it["background"]["share"] * 100:.1f}%',
                        '；'.join(f'{c["name"]} {c["hex"]} {c["cells"]} {c["share"] * 100:.1f}%' for c in it['colors']),
                        '、'.join(s['name'] for s in it['streamOnly']), it['stream'], it['members']])
    for zone in range(1, len(ZONES) + 1):
        part = [it for it in items if it['zone'] == zone]
        sheet(zone, part).save(OUT / f'palette-sheet-zone{zone}.png')
    print(f'配色表：{len(items)} 关（{label(items[0]["game"])} – {label(items[-1]["game"])}）→ {OUT.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
