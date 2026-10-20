#!/usr/bin/env python3
"""Original-level data for writing B-level subjects, one zone at a time (the 「难度依据（原关数据）」 blocks of
docs/r1/关卡第二套-题材.md, zone 1 was written from the same numbers).

  python3 scripts/levels_b/zone_data.py --zone 2                 per-level blocks (markdown) to stdout
  python3 scripts/levels_b/zone_data.py --zone 2 --summary       zone summary and suggested pilot levels
  python3 scripts/levels_b/zone_data.py --levels 33 40 --out x.md

Rows count from the defence line: row 1 is the bottom row (shot first), row 60 the top. "背景为主" rows are the
bottom rows where the background colour is at least 90 %.
"""
import argparse
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import structure as st
from levelsb_common import SIDE, load_a, name, rgb_int

ZONES = {1: (3, 32, '泡泡岛'), 2: (33, 82, '冰雪乐园'), 3: (83, 142, '火山热带岛'), 4: (143, 202, '绿野森林'), 5: (203, 262, '星空游乐场')}


def short(a, c):
    return name(a, c).replace('小', '')


def data(game):
    a, _ = load_a(game)
    c = a['cells']; tot = Counter(c); b20 = Counter(c[(SIDE - 20) * SIDE:])
    bg = tot.most_common(1)[0][0]
    clear = 0
    for r in range(SIDE - 1, -1, -1):
        row = c[r * SIDE:(r + 1) * SIDE]
        if row.count(bg) >= .9 * SIDE: clear += 1
        else: break
    lay = st.layers(a)
    low = st.lowest_rows(a)
    stream = Counter(a['stream'])
    only = [short(a, k) for k in stream if k not in tot]
    return dict(game=game, a=a, title=a['title'], bg=short(a, bg), bgShare=tot[bg] / len(c), clear=clear, ncolors=len(tot),
                colors=[(short(a, k), f'#{rgb_int(a, k):06X}', n, b20[k] / n, low[k]) for k, n in tot.most_common()],
                f10=' '.join(f'{short(a, k)}{round(100 * float(v))}' for k, v in sorted(lay[10].items(), key=lambda kv: -kv[1]) if v >= .005),
                f20=' '.join(f'{short(a, k)}{round(100 * float(v))}' for k, v in sorted(lay[20].items(), key=lambda kv: -kv[1]) if v >= .005),
                members=len(a['queue']), lanes=a['laneCount'], stream=len(a['stream']), streamOnly=only,
                streamTop='、'.join(f'{short(a, k)}{round(100 * n / len(a["stream"]))}%' for k, n in stream.most_common(3)) if a['stream'] else '',
                speed=a['advanceSpeed'])


def block(d):
    lines = [f"#### 第 {d['game']} 关（原关「{d['title']}」）", '',
             f"- 配色：{d['ncolors']} 色，" + ' '.join(f"{n}{'<1' if k < 36 else round(100 * k / 3600)}" for n, _, k, _, _ in d['colors'])
             + (f"；仅补给里有：{'、'.join(d['streamOnly'])}" if d['streamOnly'] else ''),
             f"- 背景 {d['bg']}（{round(100 * d['bgShare'])}%）；最下面 {d['clear']} 排背景为主（≥90%）",
             f"- 最下面 10 排：{d['f10']}；最下面 20 排：{d['f20']}",
             '- 各色（格数，下 20 排占比，最低第几排）：' + '；'.join(f"{n} {h} {k}（{round(100 * p)}%，第 {lo} 排）" for n, h, k, p, lo in d['colors']),
             f"- 不变：{d['members']} 名队员、{d['lanes']} 列，补给 {d['stream']} 颗" + (f"（{d['streamTop']}）" if d['stream'] else '') + f"，推进速度 {d['speed']}", '']
    return '\n'.join(lines)


def summary(zone):
    lo, hi, dest = ZONES[zone]
    ds = [data(g) for g in range(lo, hi + 1)]
    nc = Counter(d['ncolors'] for d in ds)
    out = [f"战区 {zone}（第 {lo}–{hi} 关，{dest}）：{len(ds)} 关",
           '颜色数：' + '、'.join(f'{k} 色 {v} 关' for k, v in sorted(nc.items())),
           f"背景占比：{round(100 * min(d['bgShare'] for d in ds))}%–{round(100 * max(d['bgShare'] for d in ds))}%",
           f"底部贴底（背景为主 0 排）：{sum(d['clear'] == 0 for d in ds)} 关；有仅补给颜色：{sum(bool(d['streamOnly']) for d in ds)} 关",
           f"队员 {min(d['members'] for d in ds)}–{max(d['members'] for d in ds)}"]
    # pilot: most colours, fewest colours, lowest background, touches the bottom, two near-equal colours (mode P), first level
    pick = []
    def add(d, why):
        if d['game'] not in [p[0] for p in pick]: pick.append((d['game'], why))
    add(max(ds, key=lambda d: (d['ncolors'], -d['bgShare'])), '颜色最多')
    add(min(ds, key=lambda d: (d['ncolors'], d['game'])), '颜色最少')
    add(min(ds, key=lambda d: d['bgShare']), '背景占比最低')
    add(next((d for d in ds if d['clear'] == 0), ds[0]), '主体贴底')
    add(next((d for d in ds if d['streamOnly']), ds[1]), '有仅补给颜色')
    add(max(ds, key=lambda d: d['members']), '队员最多')
    common = Counter(d['ncolors'] for d in ds).most_common(1)[0][0]
    add(next(d for d in ds[len(ds) // 2:] if d['ncolors'] == common and d['clear'] > 0), f'{common} 色的常见关')
    add(ds[0], '本战区第一关')
    out.append('建议试点：' + '、'.join(f'第 {g} 关（{w}）' for g, w in pick))
    return '\n'.join(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--zone', type=int, choices=ZONES)
    ap.add_argument('--levels', type=int, nargs='+')
    ap.add_argument('--summary', action='store_true')
    ap.add_argument('--out', type=Path)
    args = ap.parse_args()
    if args.summary:
        text = summary(args.zone)
    else:
        games = args.levels or range(ZONES[args.zone][0], ZONES[args.zone][1] + 1)
        text = '\n'.join(block(data(g)) for g in games)
    if args.out: args.out.write_text(text + '\n', encoding='utf-8')
    else: print(text)


if __name__ == '__main__':
    main()
