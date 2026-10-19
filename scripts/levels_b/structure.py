#!/usr/bin/env python3
"""Plan step 4, check D: structural comparison of a B level against its A level, without the game or a bot.

  python3 scripts/levels_b/structure.py 3 7          drafts from the manifest vs A
  python3 scripts/levels_b/structure.py --temp 3 7    mirrored A pictures vs A
  python3 scripts/levels_b/structure.py --temp --all  every level, mirrored (calibration; a few seconds per level)

The only thing a B level changes is where each colour sits on the 60 x 60 board, i.e. in which order colours
reach the front of their columns. Two measures, both deterministic:

1. Front layers: the colour shares of the first 3 / 10 / 20 rows from the defence line (row 59 is the front)
   and the half-distance between A and B (0 = same mix, 1 = nothing in common). Reported, not judged.
2. Ideal player: a simplified, timeless replay. Columns are stacks (board bottom row first, then the column's
   sand stream); a member on a pad removes front beads of its colour (tier rate 1 / 3.4 / 4.6 / 6.3 per step,
   three of a kind merge into one, tier + 1); one member is deployed per step: one that completes a merge,
   else the head whose colour is exposed in the most columns, else (pads empty) the head with most ammo
   ("blind deploy"). A step where nothing can fire or deploy usefully is "stuck": with a free pad the head with
   most ammo goes out anyway (also a blind deploy); with full pads the front bead of every column is removed
   ("rescue", like a revive purge). Measured: steps to clear, stuck steps,
   blind deploys, rescues. Sideways sand flow and the advance timer are not modelled.
   18:45: this replay is chaotic: swapping 20 adjacent cells (invisible) moved its steps by up to 35 % and flipped
   the old verdict on 7 of 10 sample levels, so it is reported only.

Judgement (18:50, provisional until the pilot playtests calibrate it): the mean front-mix difference of rows 10 and
20 must be at most MIX_LIMIT; otherwise 超差. The first generated pictures scored 0.02-0.05 (fish), 0.08-0.15 (fox)
and 0.11-0.21 (turtle).
"""
import argparse
import json
import subprocess
import sys
import tempfile
from collections import Counter, deque
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import make_temp
from levelsb_common import FIRST, LAST, LEVELS_A, ROOT, SIDE, file_sha256, label, load_a, load_manifest, record_check, resource

RATE = (1.0, 3.4, 4.6, 6.3)
LAYERS = (3, 10, 20)


def columns_of(level):
    cells, stream, offsets = level['cells'], level.get('stream') or [], level.get('streamOffsets') or []
    cols = []
    for c in range(SIDE):
        col = deque(cells[r * SIDE + c] for r in range(SIDE - 1, -1, -1))
        if stream:
            col.extend(stream[offsets[c]:offsets[c + 1]])
        cols.append(col)
    return cols


def layers(level):
    out = {}
    for depth in LAYERS:
        cells = [level['cells'][r * SIDE + c] for r in range(SIDE - depth, SIDE) for c in range(SIDE)]
        n = len(cells)
        out[depth] = {k: v / n for k, v in Counter(cells).items()}
    return out


def layer_distance(a, b):
    keys = set(a) | set(b)
    return sum(abs(a.get(k, 0) - b.get(k, 0)) for k in keys) / 2


def simulate(level, max_steps=200000):
    cols = columns_of(level)
    lanes = {}
    for entry in level['queue']:
        lanes.setdefault(entry['lane'], deque()).append([entry['color'], entry['ammo']])
    lanes = [lanes[k] for k in sorted(lanes)]
    slots = level.get('initialSlots') or 5
    slots = min(slots, 8)
    beads = sum(len(c) for c in cols)
    front = {}   # colour -> set of columns whose front bead is that colour
    for i, col in enumerate(cols):
        if col:
            front.setdefault(col[0], set()).add(i)
    fleet = []   # [colour, ammo, tier, credit]
    steps = stuck = blind = rescues = merges = 0
    half_at = None

    def pop(i):
        nonlocal beads
        col = cols[i]
        front[col[0]].discard(i)
        col.popleft()
        beads -= 1
        if col:
            front.setdefault(col[0], set()).add(i)

    while beads > 0 and steps < max_steps:
        steps += 1
        acted = False
        if len(fleet) < slots and any(lanes):
            pick, kind = None, None
            for i, lane in enumerate(lanes):
                if not lane:
                    continue
                color, ammo = lane[0]
                if sum(1 for m in fleet if m[0] == color) % 3 == 2:
                    if pick is None or kind != 'merge' or ammo > lanes[pick][0][1]:
                        pick, kind = i, 'merge'
            if pick is None:
                best = 0
                for i, lane in enumerate(lanes):
                    if lane:
                        n = len(front.get(lane[0][0], ()))
                        if n > best or (n == best and n > 0 and lane[0][1] > lanes[pick][0][1]):
                            pick, best = i, n
            if pick is None and not fleet:
                pick = max((i for i, l in enumerate(lanes) if l), key=lambda i: lanes[i][0][1])
                blind += 1
            if pick is not None:
                color, ammo = lanes[pick].popleft()
                fleet.append([color, ammo, 0, 0.0])
                acted = True
                same = [m for m in fleet if m[0] == color]
                if len(same) >= 3:
                    group = same[:3]
                    for m in group:
                        fleet.remove(m)
                    fleet.append([color, sum(m[1] for m in group), min(3, max(m[2] for m in group) + 1), 0.0])
                    merges += 1
        fired = False
        for m in list(fleet):
            targets = front.get(m[0])
            if not targets:
                continue
            m[3] += RATE[m[2]]
            while m[3] >= 1 and m[1] > 0 and front.get(m[0]):
                m[3] -= 1
                i = min(front[m[0]], key=lambda i: (-len(cols[i]), i))   # deepest column first
                pop(i)
                m[1] -= 1
                fired = True
            if m[1] <= 0:
                fleet.remove(m)
        if fired or acted:
            if half_at is None and beads <= level_total(level) / 2:
                half_at = steps
            continue
        stuck += 1
        if len(fleet) < slots and any(lanes):
            # Free pad but no useful head: deploy blind (the head with most ammo) rather than wait.
            pick = max((i for i, l in enumerate(lanes) if l), key=lambda i: lanes[i][0][1])
            color, ammo = lanes[pick].popleft()
            fleet.append([color, ammo, 0, 0.0])
            blind += 1
            same = [m for m in fleet if m[0] == color]
            if len(same) >= 3:
                group = same[:3]
                for m in group:
                    fleet.remove(m)
                fleet.append([color, sum(m[1] for m in group), min(3, max(m[2] for m in group) + 1), 0.0])
                merges += 1
        else:
            rescues += 1
            for i in range(SIDE):
                if cols[i]:
                    pop(i)
    return dict(steps=steps, stuck=stuck, blind=blind, rescues=rescues, merges=merges, half=half_at or steps)


def level_total(level):
    return len(level['cells']) + len(level.get('stream') or [])


MIX_LIMIT = .20


def judge_mix(mix):
    return ('通过', '') if mix <= MIX_LIMIT else ('超差', f'前排颜色构成差异 {mix:.2f} > {MIX_LIMIT}')


def judge(a, b):
    notes = []
    if abs(b['steps'] - a['steps']) > .10 * a['steps']:
        notes.append(f'清完步数 {a["steps"]}→{b["steps"]}')
    if abs(b['stuck'] - a['stuck']) > max(.10 * a['stuck'], 5):
        notes.append(f'卡住步数 {a["stuck"]}→{b["stuck"]}')
    if abs(b['blind'] - a['blind']) > max(.20 * a['blind'], 2):
        notes.append(f'盲出 {a["blind"]}→{b["blind"]}')
    if abs(b['rescues'] - a['rescues']) > max(.20 * a['rescues'], 1):
        notes.append(f'救场 {a["rescues"]}→{b["rescues"]}')
    return ('超差', '；'.join(notes)) if notes else ('通过', '')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('levels', type=int, nargs='*', metavar='关号')
    ap.add_argument('--temp', action='store_true')
    ap.add_argument('--all', action='store_true')
    ap.add_argument('--out', type=Path)
    args = ap.parse_args()
    games = list(range(FIRST, LAST + 1)) if args.all else args.levels
    if not games:
        ap.error('给出关号或 --all')
    kind = 'temp' if args.temp else 'draft'
    out = (args.out or ROOT / 'Builds/R1-evidence' / f'levels-b-structure-{kind}-{datetime.now():%Y%m%d-%H%M%S}').resolve()
    out.mkdir(parents=True, exist_ok=False)
    manifest = load_manifest()
    rows = []
    with tempfile.TemporaryDirectory(prefix='levels-b-structure-') as tmp:
        for game in games:
            a, _ = load_a(game)
            if args.temp:
                picture = make_temp.make(game, 'flip', Path(tmp) / 'images')
                folder = Path(tmp) / str(game)
                run = subprocess.run([sys.executable, str(HERE / 'convert.py'), '--level', str(game), '--image', str(picture),
                                      '--out', str(folder), '--no-manifest'], capture_output=True, text=True)
                if run.returncode not in (0, 2):
                    print(run.stdout + run.stderr); return 1
                b = json.loads((folder / f'{resource(game)}.json').read_text())
            else:
                entry = manifest['levels'].get(resource(game))
                if not entry:
                    print(f'{label(game)}：清单里没有草稿'); return 1
                data = (ROOT / entry['draft']).read_bytes()
                if file_sha256(data) != entry['fileSha256']:
                    print(f'{label(game)}：草稿文件与清单哈希不一致，先重新 convert'); return 1
                b = json.loads(data)
            sa, sb = simulate(a), simulate(b)
            la, lb = layers(a), layers(b)
            mix = round(sum(layer_distance(la[d], lb[d]) for d in (10, 20)) / 2, 3)
            verdict, note = judge_mix(mix)
            replay, replay_note = judge(sa, sb)
            rows.append(dict(game=game, verdict=verdict, note=note, mix=mix, replay=replay, replayNote=replay_note, a=sa, b=sb,
                             layers={d: round(layer_distance(la[d], lb[d]), 3) for d in LAYERS}))
            print(f'{label(game)}：{verdict} {note}', flush=True)
            if not args.temp:
                record_check(game, 'structure', verdict, str(out.relative_to(ROOT)) if out.is_relative_to(ROOT) else str(out),
                             entry['fileSha256'])
    fmt = lambda s: f'步 {s["steps"]} 卡 {s["stuck"]} 盲 {s["blind"]} 救 {s["rescues"]} 合 {s["merges"]}'
    lines = [f'# B 套结构检查（{kind}，{datetime.now():%Y-%m-%d %H:%M}）', '',
             '| 关 | 判定 | 前 3/10/20 排差异 | 说明 | A 理想打法（仅参考） | B 理想打法（仅参考） |', '|---|---|---|---|---|---|']
    for r in rows:
        lines.append(f'| {r["game"]} | {r["verdict"]} | ' + ' / '.join(f'{r["layers"][d]:.2f}' for d in LAYERS)
                     + f' | {r["note"]} | {fmt(r["a"])} | {fmt(r["b"])}（{r["replay"]}） |')
    passed = sum(r['verdict'] == '通过' for r in rows)
    lines += ['', f'通过 {passed} / {len(rows)}']
    (out / 'report.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    (out / 'report.json').write_text(json.dumps(dict(kind=kind, rows=rows), ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    print(f'通过 {passed} / {len(rows)}；报告：{out / "report.md"}')
    return 0 if passed == len(rows) else 2


if __name__ == '__main__':
    sys.exit(main())
