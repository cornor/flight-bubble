#!/usr/bin/env python3
"""Short picture brief for one B level (rules of 2026-10-27: difficulty by simulation + queue tuning, D10/D20 not
gating, recolouring allowed, stream balance allowed). Only what the picture really must respect:
  - at most as many colours as the A board uses;
  - with stream balance the areas are free, except that the n-th largest colour may not exceed the n-th largest
    colour total (board + stream) of the A level: small colours of A must stay small accents;
  - the colours may be any members that have debuted by this level (mode R); this level's new members must appear.

  python3 scripts/levels_b/brief.py 15 19 23 [--theme 15=推皮球小虎鲸] [--markdown]
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from levelsb_common import (ROOT, allowed_colors, counts, debut_colors, label, level_colors, load_a, load_manifest, name,
                            resource, rgb_int)

ANIMAL = None


def animal(level, c):
    global ANIMAL
    if ANIMAL is None:
        import re
        src = (ROOT / 'UnityProject/Assets/Flightbeat/Garden/GardenColorCatalog.Generated.cs').read_text(encoding='utf-8')
        body = src[src.index('public static string Animal(int rgb)'):]
        ANIMAL = {int(h, 16): n for h, n in re.findall(r'case 0x([0-9A-Fa-f]{6}): return "([^"]+)";', body[:body.index('default')])}
    return ANIMAL.get(rgb_int(level, c), '')


def theme_of(game):
    e = load_manifest()['levels'].get(resource(game))
    if e and e.get('title'):
        return e['title']
    names = sorted((ROOT / 'Art/levels-b/source').glob(f'第{game}关-*.png'))
    return names[-1].stem.split('-')[1] if names else '（待定题材）'


def brief(game, theme=None):
    a, _ = load_a(game)
    board = counts(a['cells'])
    drawn = sorted((c for c in level_colors(a) if board.get(c, 0)), key=lambda c: -board[c])
    shares = [board[c] / len(a['cells']) for c in drawn]
    must = sorted(c for c in debut_colors(game, a) if board.get(c, 0))
    allowed = sorted(allowed_colors(game, a), key=lambda c: name(a, c))
    col = lambda c: f'{name(a, c)}#{rgb_int(a, c):06X}'
    total = counts(list(a['cells']) + list(a.get('stream') or []))
    caps = sorted((total[c] for c in drawn), reverse=True)
    caps = [(i + 1, min(100, x * 100 / len(a['cells']))) for i, x in enumerate(caps)]
    return dict(game=game, label=label(game), theme=theme or theme_of(game), k=len(drawn),
                caps=[(i, round(x, 1)) for i, x in caps if x < 40],
                shares=[round(x * 100) for x in shares],
                original=[(col(c), round(board[c] / len(a['cells']) * 100, 1)) for c in drawn],
                must=[f'{col(c)}（{animal(a, c)}）' for c in must], allowed=[col(c) for c in allowed])


def cap_text(b):
    names = {1: '最多', 2: '第二多', 3: '第三多', 4: '第四多', 5: '第五多', 6: '第六多', 7: '第七多', 8: '第八多', 9: '第九多'}
    return '；'.join(f'{names[i]}的颜色不超过画面 {x:g}%' for i, x in b['caps'])


def prompt(b):
    must = f'其中必须包含 {"、".join(b["must"])}（本关新登场的角色色），' if b['must'] else ''
    caps = cap_text(b)
    small = (f'颜色面积可以自由安排，但小颜色必须很小：{caps}（这些只能是眼睛、嘴、小花、星星这类小点缀）。'
             if caps else '颜色面积可以自由安排。')
    return (f'可爱的像素画，题材：{b["theme"]}。正方形画面，平涂色块，无渐变、无描边阴影、无文字。'
            f'整幅画最多用 {b["k"]} 种颜色，从下面的颜色里挑：{"、".join(b["allowed"])}。{must}'
            f'{small}主体清楚、造型饱满好看，构图自由。')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('levels', type=int, nargs='+')
    ap.add_argument('--theme', action='append', default=[], help='关号=题材')
    ap.add_argument('--json', action='store_true')
    args = ap.parse_args()
    themes = {int(k): v for k, v in (t.split('=', 1) for t in args.theme)}
    out = [brief(g, themes.get(g)) for g in args.levels]
    if args.json:
        print(json.dumps(out, ensure_ascii=False, indent=1)); return 0
    for b in out:
        print(f'### 第{b["game"]}关 · {b["theme"]}\n')
        print(f'- 最多用 {b["k"]} 种颜色；面积自由（10-27 起沙流补偿）' + (f'，但{cap_text(b)}' if b['caps'] else ''))
        print(f'- 原关配色（参考，可换）：{"、".join(f"{c} {p}%" for c, p in b["original"])}')
        print(f'- 必须出现：{"、".join(b["must"]) or "无"}')
        print(f'- 可选颜色（{len(b["allowed"])} 种）：{"、".join(b["allowed"])}\n')
        print('```\n' + prompt(b) + '\n```\n')
    return 0


if __name__ == '__main__':
    sys.exit(main())
