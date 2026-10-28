#!/usr/bin/env python3
"""Search a queue order for a B board so that its difficulty matches the A level (headless sim, simlib.py).

  python3 scripts/levels_b/tune_queue.py --level 24 [--board B.json] [--budget 150]   resumable; call again to go on
  python3 scripts/levels_b/tune_queue.py --level 24 --validate                        held-out seeds for the best order

Rules (levelsb_common.queue_reorder, user decision 2026-10-26 18:08): slots stay, members only move; special
members (kind != 0) stay; a member moves at most QUEUE_MAX_ROW_SHIFT rows from its A row.
Search: local search over member swaps. Each round screens a few random legal swaps on 8 seeds per strategy,
then scores the best two on the 32 tuning seeds (common random numbers with A) and keeps an improvement.
The gap is simlib.gap; tuning seeds 1-32, validation seeds 201-264 (never used for the search).
State: tools/work/levels-b/queue-tune/<level>.json (gitignored), so the 180 s shell limit only pauses it.
"""
import argparse
import hashlib
import json
import random
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import simlib
from levelsb_common import (QUEUE_MAX_ROW_SHIFT, QUEUE_SLOT_FIELDS, ROOT, _queue_member, _queue_rows, cells_sha256, dumps,
                            file_sha256, load_a, load_manifest, queue_reorder, resource, save_manifest, validate)

STATE_DIR = ROOT / 'tools/work/levels-b/queue-tune'
TUNE = list(range(1, 33))
SCREEN = list(range(1, 9))
VALID = list(range(201, 265))
ACCEPT = .15   # provisional: held-out gap score; A vs A noise on 32 seeds was 0.04 mean, 0.18 max


def legal_rows(a_queue):
    rows = _queue_rows(a_queue)
    special = [a_queue[i].get('kind', 0) != 0 for i in range(len(a_queue))]
    return rows, special


def reordered(queue, perm):
    """Python twin of sim/Program.cs Reorder: slot k keeps its slot fields and takes the member of entry perm[k]."""
    return [{key: (queue[k][key] if key in QUEUE_SLOT_FIELDS else queue[perm[k]][key]) for key in queue[k]}
            for k in range(len(queue))]


def apply(game, st, accept):
    v = st.get('validation')
    if not v or st.get('validationParts', {}).get('perm') != json.dumps(st['best']['perm']):
        print('先运行 --validate'); return 1
    if v['tuned']['score'] > accept or v['tuned']['score'] >= v['original']['score']:
        print(f'验证差距 {v["original"]["score"]:.3f} → {v["tuned"]["score"]:.3f}，未达到上限 {accept} 或没有改善，不写入'); return 2
    manifest = load_manifest()
    entry = manifest['levels'][resource(game)]
    path = ROOT / entry['draft']
    raw = path.read_bytes()
    if file_sha256(raw) != st['boardSha256']:
        print('草稿已变化，与本次调整所用的棋盘不一致'); return 1
    a, a_raw = load_a(game)
    level = json.loads(raw)
    level['queue'] = reordered(level['queue'], st['best']['perm'])
    sigma = {int(k): x for k, x in (entry.get('sigma') or {}).items()}
    full = None
    if sigma:
        full = {c: c for c in set(a['cells']) | set(a.get('stream') or [])}
        full.update(sigma)
    problems = validate(a, level, entry.get('mode', 'S'), full, game, stream_balance=bool(entry.get('streamBalance')))
    if problems:
        print('校验失败：', '；'.join(problems)); return 1
    data = dumps(level, raw)
    path.write_bytes(data)
    restored_q = level['queue'] if not sigma else [dict(e, color={b: a_ for a_, b in full.items()}.get(e['color'], e['color'])) for e in level['queue']]
    _, stats = queue_reorder(a['queue'], restored_q)
    entry['fileSha256'] = file_sha256(data)
    entry['status'] = 'draft'
    entry['queueTune'] = dict(moved=stats['moved'], maxRowShift=stats['maxRowShift'], boardSha256=st['boardSha256'],
                              tuningGap=st['best']['gap'], validation=dict(original=v['original'], tuned=v['tuned']),
                              seeds=dict(tuning=len(TUNE), validation=[VALID[0], VALID[-1]]), at=time.strftime('%Y-%m-%dT%H:%M:%S'))
    save_manifest(manifest)
    print(json.dumps(dict(level=game, written=str(path.relative_to(ROOT)), moved=stats['moved'], maxRowShift=stats['maxRowShift'],
                          validation=round(v['tuned']['score'], 3)), ensure_ascii=False))
    return 0


def evaluate(path, game, seeds, perm):
    return simlib.summarize(simlib.run(simlib.cases_for(path, game, seeds, 'q', queue=perm)))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--level', type=int, required=True)
    ap.add_argument('--board', type=Path, help='B level file (default: the manifest draft)')
    ap.add_argument('--budget', type=float, default=150, help='seconds to spend in this call')
    ap.add_argument('--proposals', type=int, default=10)
    ap.add_argument('--finalists', type=int, default=3)
    ap.add_argument('--patience', type=int, default=5, help='stop after this many rounds without improvement')
    ap.add_argument('--target', type=float, default=.06, help='stop once the tuning gap is this small')
    ap.add_argument('--max-shift', type=int, default=QUEUE_MAX_ROW_SHIFT, help='试验用：放宽队员前后移动上限（写入仍按 levelsb_common 的上限校验）')
    ap.add_argument('--seeds', type=int, default=32, help='tuning seeds per strategy (1..N); a new N starts a new state')
    ap.add_argument('--validate', action='store_true')
    ap.add_argument('--reset', action='store_true')
    ap.add_argument('--apply', action='store_true', help='把验证通过的排队写入草稿并更新清单（之后需重跑结构检查和机器人粗筛）')
    ap.add_argument('--accept', type=float, default=ACCEPT, help='验证差距上限')
    args = ap.parse_args()
    t0 = time.time()
    global TUNE
    TUNE = list(range(1, args.seeds + 1))
    game = args.level
    a, _ = load_a(game)
    board = args.board or ROOT / load_manifest()['levels'][resource(game)]['draft']
    b_sha = hashlib.sha256(Path(board).read_bytes()).hexdigest()
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    stem = f'{game}-{b_sha[:12]}' + ('' if args.seeds == 32 else f'-s{args.seeds}') + \
        ('' if args.max_shift == QUEUE_MAX_ROW_SHIFT else f'-m{args.max_shift}')
    sp = STATE_DIR / f'{stem}.json'
    st = json.loads(sp.read_text()) if sp.exists() and not args.reset else None
    n = len(a['queue'])
    rows, special = legal_rows(a['queue'])
    if st is None:
        sa = evaluate(simlib.a_path(game), game, TUNE, None)
        ident = list(range(n))
        base = STATE_DIR / f'{game}-{b_sha[:12]}.json'
        if (args.seeds != 32 or args.max_shift != QUEUE_MAX_ROW_SHIFT) and base.exists():
            ident = json.loads(base.read_text())['best']['perm']   # continue from the 32-seed result
        sb = evaluate(board, game, TUNE, ident)
        st = dict(game=game, board=str(Path(board).resolve().relative_to(ROOT)), boardSha256=b_sha, a=sa, rounds=0,
                  best=dict(perm=ident, summary=sb, gap=simlib.gap(sa, sb)), start=simlib.gap(sa, sb), history=[], tried=[])
        sp.write_text(json.dumps(st))
    sa = {int(k): v for k, v in st['a'].items()}
    if args.apply:
        return apply(game, st, args.accept)
    if args.validate:
        # Three summaries on held-out seeds, cached one by one so a slow level can finish over several calls.
        v = st.setdefault('validationParts', {})
        perm_key = json.dumps(st['best']['perm'])
        if v.get('perm') != perm_key:
            v.clear(); v['perm'] = perm_key
        jobs = [('a', simlib.a_path(game), None), ('b0', board, list(range(n))), ('b1', board, st['best']['perm'])]
        for key, path, perm in jobs:
            if key in v:
                continue
            if time.time() - t0 > args.budget * .4 and len(v) > 1:
                sp.write_text(json.dumps(st)); print(json.dumps(dict(level=game, validation='partial', have=sorted(v)))); return 0
            v[key] = v['b0'] if key == 'b1' and perm == list(range(n)) and 'b0' in v else evaluate(path, game, VALID, perm)
            sp.write_text(json.dumps(st))
        va, v0, v1 = ({int(k): x for k, x in v[key].items()} for key in ('a', 'b0', 'b1'))
        st['validation'] = dict(original=simlib.gap(va, v0), tuned=simlib.gap(va, v1), a=va, b0=v0, b1=v1)
        sp.write_text(json.dumps(st))
        print(json.dumps(dict(level=game, original=st['validation']['original'], tuned=st['validation']['tuned']), ensure_ascii=False))
        return 0
    rng = random.Random(st['rounds'] * 7919 + game)
    tried = set(map(tuple, st['tried']))
    members = [_queue_member(e) for e in a['queue']]
    last = st.get('roundSeconds', 45)
    while time.time() - t0 + last * 1.15 < args.budget:
        r0 = time.time()
        perm = st['best']['perm']
        # perm[k] = A entry index whose member stands in slot k; that member's A row is rows[perm[k]].
        props = []
        for _ in range(200):
            i, j = rng.sample(range(n), 2)
            if special[i] or special[j] or members[perm[i]] == members[perm[j]]:
                continue
            if abs(rows[perm[j]] - rows[i]) > args.max_shift or abs(rows[perm[i]] - rows[j]) > args.max_shift:
                continue
            q = list(perm); q[i], q[j] = q[j], q[i]
            if tuple(q) in tried:
                continue
            props.append(q)
            if len(props) >= args.proposals:
                break
        if not props or st.get('stall', 0) >= args.patience or st['best']['gap']['score'] <= args.target:
            st['done'] = True
            break
        if 'aScreen' not in st:
            st['aScreen'] = evaluate(simlib.a_path(game), game, SCREEN, None)
        a_screen = {int(k): v for k, v in st['aScreen'].items()}
        screened = [(simlib.gap(a_screen, evaluate(board, game, SCREEN, q))['score'], q) for q in props]
        screened.sort(key=lambda x: x[0])
        improved = False
        for _, q in screened[:args.finalists]:
            tried.add(tuple(q))
            sq = evaluate(board, game, TUNE, q)
            g = simlib.gap(sa, sq)
            if g['score'] < st['best']['gap']['score']:
                st['best'] = dict(perm=q, summary=sq, gap=g)
                improved = True
        st['stall'] = 0 if improved else st.get('stall', 0) + 1
        st['rounds'] += 1
        last = st['roundSeconds'] = time.time() - r0
        st['history'].append(round(st['best']['gap']['score'], 4))
        st['tried'] = [list(t) for t in tried]
        sp.write_text(json.dumps(st))
    moved = sum(1 for k, p in enumerate(st['best']['perm']) if members[p] != members[k])
    print(json.dumps(dict(level=game, rounds=st['rounds'], start=round(st['start']['score'], 3),
                          best=round(st['best']['gap']['score'], 3), ease=round(st['best']['gap']['ease'], 3),
                          moved=moved, done=st.get('done', False), history=st['history'][-6:]), ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
