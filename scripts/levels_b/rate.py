#!/usr/bin/env python3
"""Absolute difficulty rating (1-10) of game levels with the headless simulation (docs/r1/关卡难度评估教程.md).

  python3 scripts/levels_b/rate.py 1-50 [--seeds 32] [--budget 150]     runs/caches the simulation, prints the table
  python3 scripts/levels_b/rate.py 1-50 --report docs/r1/xxx.md          also writes the markdown table

Each level is played by the 4 bot strategies x N seeds (simlib, identical to the Unity bot). Per run, a
performance q: win without revive 1.0, win after the revive 0.85, loss 0.7 x clear progress (so a loss never
outranks a win). Two player models:
  skilled = the best strategy's mean q (a player who plays well for this level),
  casual  = the mean q over all four strategies (a player who sometimes plays naively).
difficulty D = 1 - (0.6 x skilled + 0.4 x casual), score = 1 + 9 x min(1, D / D_MAX), rounded to 0.5.
Raw data are cached per file hash in tools/work/levels-b/rating/, so the 180 s shell limit only pauses a run.
"""
import argparse
import hashlib
import json
import statistics as st
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import simlib
from levelsb_common import LEVELS_A, ROOT

CACHE = ROOT / 'tools/work/levels-b/rating'
D_MAX = .85          # D at or above this scores 10: every run lost at about 20 % progress or less (2026-10-27)
W_SKILLED = .6
WIN_REVIVE = .85
LOSS_SCALE = .7


def level_path(game, level_set='A'):
    """A = the original level; B = the current B draft from the manifest (None when there is none)."""
    if level_set == 'B':
        from levelsb_common import load_manifest, resource
        if game < 3:
            return None
        entry = load_manifest()['levels'].get(resource(game))
        return ROOT / entry['draft'] if entry else None
    return LEVELS_A / (f'training{game:02d}.json' if game <= 2 else f'sector{game - 2:02d}.json')


def perf(r):
    if r['outcome'] == 'won':
        return WIN_REVIVE if r['revived'] else 1.0
    return LOSS_SCALE * r['progress']


def measure(game, seeds, level_set='A'):
    path = level_path(game, level_set)
    sha = hashlib.sha256(path.read_bytes()).hexdigest()[:16]
    CACHE.mkdir(parents=True, exist_ok=True)
    cp = CACHE / f'{game}-{sha}-s{len(seeds)}.json'
    if cp.exists():
        return json.loads(cp.read_text())
    results = simlib.run(simlib.cases_for(path, game, seeds, f'r{game}'))
    by = {}
    for r in results:
        by.setdefault(r['strategy'], []).append(r)
    strat = {}
    for s, v in sorted(by.items()):
        won = [r for r in v if r['outcome'] == 'won']
        strat[s] = dict(n=len(v), win=st.mean(r['outcome'] == 'won' for r in v), revive=st.mean(bool(r['revived']) for r in v),
                        prog=st.mean(r['progress'] for r in v), q=st.mean(perf(r) for r in v),
                        blocked=st.mean(r['blockedSeconds'] for r in v), deploys=st.mean(r['deploys'] for r in v),
                        seconds=st.median(r['seconds'] for r in won) if won else None,
                        closest=st.median(r['minDistance'] for r in v if r['minDistance'] >= 0) if any(r['minDistance'] >= 0 for r in v) else None)
    level = json.loads(path.read_text())
    cells = [c for c in level['cells'] if c >= 0]
    stream = level.get('stream') or []
    data = dict(game=game, file=str(path.relative_to(ROOT)), sha=sha, seeds=[seeds[0], seeds[-1]], strategies=strat,
                static=dict(marker=level.get('difficulty'), colours=len(set(cells) | set(stream)), queue=len(level['queue']),
                            board=len(cells), stream=len(stream), lanes=level.get('laneCount'),
                            slots=level.get('initialSlots'), slotLimit=level.get('slotLimit'),
                            special=sum(1 for e in level['queue'] if e.get('kind', 0) != 0)))
    cp.write_text(json.dumps(data, ensure_ascii=False))
    return data


def rate(data):
    s = data['strategies']
    skilled = max(x['q'] for x in s.values())
    casual = st.mean(x['q'] for x in s.values())
    d = 1 - (W_SKILLED * skilled + (1 - W_SKILLED) * casual)
    score = 1 + 9 * min(1.0, max(0.0, d) / D_MAX)
    return dict(skilled=skilled, casual=casual, D=d, score=round(score * 2) / 2,
                best=max(s, key=lambda k: s[k]['q']))


MARKER = {0: '普通', 1: '困难', 2: '超难', None: '教学'}
SHORT = {0: '内置建议', 1: '优先攻击', 2: '优先合成', 3: '优先清位'}


def band(score):
    return ('基本都能不复活通关' if score <= 1.5 else '大多能通关，常要复活或个别打法会输' if score <= 3.5 else
            '只有对路的打法较稳' if score <= 5.5 else '最好的打法也赢不稳，输时能清掉一半以上' if score <= 7.5 else
            '基本赢不了，输时清掉不到一半' if score <= 8.5 else '都赢不了，很早卡死')


def basis(r):
    """One sentence, generated from the data, on why the level got its score."""
    s = {int(k): v for k, v in r['strategies'].items()}
    pct = lambda x: f'{x * 100:.0f}%'
    win = {k: v['win'] for k, v in s.items()}
    parts = []
    if min(win.values()) >= .9:
        heavy = [SHORT[k] for k, v in s.items() if v['revive'] >= .5]
        parts.append('4 种打法都能通关' + (f'，但{"、".join(heavy)}常要用复活' if heavy else '，基本不用复活'))
    else:
        good = [k for k in s if win[k] >= .5]
        bad = [k for k in s if win[k] < .5]
        lost_prog = st.mean(s[k]['prog'] for k in bad) if bad else None
        if good and not bad:
            parts.append(f'4 种打法多数能赢（胜率 {pct(min(win.values()))}–{pct(max(win.values()))}），但不稳')
        elif good:
            parts.append(f'{"、".join(f"{SHORT[k]}胜{pct(win[k])}" for k in good)}；'
                         f'{"、".join(SHORT[k] for k in bad)}多数输（平均清掉{pct(lost_prog)}）')
        elif max(win.values()) > 0:
            k = max(s, key=lambda k: win[k])
            parts.append(f'最好的{SHORT[k]}也只赢{pct(win[k])}，各打法平均清掉{pct(st.mean(v["prog"] for v in s.values()))}')
        else:
            progs = [v['prog'] for v in s.values()]
            parts.append(f'4 种打法都赢不了，平均清掉{pct(min(progs))}–{pct(max(progs))}')
        if s[0]['win'] < .3 and max(win.values()) >= .6:
            parts.append('跟游戏提示（内置建议）打反而会输')
        if min(win.values()) < .9 and all(v['revive'] >= .9 for v in s.values()):
            parts.append('每局都要用复活')
    blk = max(v['blocked'] for v in s.values())
    if blk >= 40:
        parts.append(f'卡死严重（每局最多卡住约 {blk:.0f} 秒）')
    return '；'.join(parts) + '。'


def report(rows, path):
    lines = ['| 关 | 分 | 档位 | 依据 | 胜率 内置/攻击/合成/清位 | 复活率 | 清除进度 | 游戏标记 |', '|---|---|---|---|---|---|---|---|']
    for r in rows:
        s = {int(k): v for k, v in r['strategies'].items()}
        f = lambda key: '/'.join(f'{s[k][key] * 100:.0f}' for k in sorted(s))
        k = r['rating']
        lines.append(f"| {r['game']} | **{k['score']:g}** | {band(k['score'])} | {basis(r)} | {f('win')} | {f('revive')} | {f('prog')} | {MARKER.get(r['static']['marker'], r['static']['marker'])} |")
    Path(path).write_text('\n'.join(lines) + '\n', encoding='utf-8')


def parse_levels(text):
    out = []
    for part in text.split(','):
        a, _, b = part.partition('-')
        out += list(range(int(a), int(b or a) + 1))
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('levels')
    ap.add_argument('--seeds', type=int, default=32)
    ap.add_argument('--set', choices=['A', 'B'], default='A', help='A 原版；B 清单里的当前改版草稿（没有草稿的关跳过）')
    ap.add_argument('--budget', type=float, default=1e9, help='stop starting new levels after this many seconds')
    ap.add_argument('--json', action='store_true')
    ap.add_argument('--report', type=Path, help='写 markdown 打分表（逐关依据由数据生成）')
    args = ap.parse_args()
    t0 = time.time()
    seeds = list(range(1, args.seeds + 1))
    rows, missing = [], []
    for g in parse_levels(args.levels):
        if time.time() - t0 > args.budget:
            missing.append(g); continue
        if level_path(g, args.set) is None:
            continue
        data = measure(g, seeds, args.set)
        rows.append(dict(data, set=args.set, rating=rate(data)))
    if args.report:
        report(rows, args.report)
    if args.json:
        print(json.dumps(rows, ensure_ascii=False)); return 0
    names = simlib.STRATEGIES
    for r in rows:
        s, k = r['strategies'], r['rating']
        print(f"{r['game']:>3} 分 {k['score']:>4} D={k['D']:.3f} 标记{r['static']['marker']} | " +
              ' '.join(f"{names[int(i)][:2]} 胜{x['win']:.0%} 复{x['revive']:.0%} 进{x['prog']:.0%}" for i, x in s.items()))
    if missing:
        print('未完成：', missing)
    return 0


if __name__ == '__main__':
    sys.exit(main())
