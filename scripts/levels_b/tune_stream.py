#!/usr/bin/env python3
"""Tune the sand stream (and the queue order) of a B draft so its difficulty matches the A level.

  python3 scripts/levels_b/tune_stream.py --level 15 [--budget 165]   resumable local search; call again to go on
  python3 scripts/levels_b/tune_stream.py --level 15 --validate       held-out seeds 201-264
  python3 scripts/levels_b/tune_stream.py --level 15 --apply          write stream + queue into the draft if it passes

Allowed by the user's decisions (2026-10-26 queue order, 2026-10-27 stream balance and "按建议" stream tuning):
  - the board (picture) never changes;
  - stream beads only move: a short segment moves earlier/later inside its column, or two equal segments swap between
    columns, so every colour's total and every column's length stay; the result is validated with stream_balance;
  - queue members only change order (levelsb_common.queue_reorder: slots stay, specials stay, <= 6 rows from A).
Moves favour shallow stream positions (they arrive earlier and matter more). Most random moves make things worse (the
landscape is rugged), so each round screens many moves (default 16) on a few seeds (default 4 per strategy), then
scores the best ones on the 32 tuning seeds; validation uses seeds 201-264.
State: tools/work/levels-b/stream-tune/<level>-<draft sha>.json (gitignored).
"""
import argparse
import hashlib
import json
import random
import sys
import tempfile
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import simlib
from levelsb_common import (QUEUE_MAX_ROW_SHIFT, QUEUE_SLOT_FIELDS, ROOT, apply_sigma, counts, dumps, file_sha256, full_map,
                            load_a, load_manifest, queue_reorder, resource, save_manifest, validate)

STATE_DIR = ROOT / 'tools/work/levels-b/stream-tune'
TUNE, SCREEN, VALID = list(range(1, 33)), list(range(1, 9)), list(range(201, 265))
ACCEPT = .15


def reordered(queue, perm):
    return [{k: (queue[i][k] if k in QUEUE_SLOT_FIELDS else queue[perm[i]][k]) for k in queue[i]} for i in range(len(queue))]


class Level:
    def __init__(self, game):
        self.game = game
        self.a, _ = load_a(game)
        self.entry = load_manifest()['levels'][resource(game)]
        self.path = ROOT / self.entry['draft']
        self.raw = self.path.read_bytes()
        self.b = json.loads(self.raw)
        sigma = {int(k): v for k, v in (self.entry.get('sigma') or {}).items()}
        self.full = full_map(self.a, sigma) if sigma else None
        self.a_in_b = apply_sigma(self.a, self.full) if sigma else self.a   # A's queue in B's colours
        self.offsets = self.b['streamOffsets']
        self.tmp = Path(tempfile.mkdtemp(prefix='stream-tune-'))

    def build(self, stream, perm):
        lv = json.loads(json.dumps(self.b))
        lv['stream'] = list(stream)
        lv['queue'] = reordered(self.b['queue'], perm)
        return lv

    def validate_problems(self, lv):
        return validate(self.a, lv, self.entry.get('mode', 'S'), self.full, self.game, stream_balance=True)

    def queue_ok(self, perm):
        problems, _ = queue_reorder(self.a_in_b['queue'], reordered(self.b['queue'], perm))
        return not problems

    def evaluate(self, stream, perm, seeds):
        lv = self.build(stream, perm)
        key = hashlib.sha256(json.dumps([stream, perm]).encode()).hexdigest()[:16]
        p = self.tmp / f'{key}.json'
        p.write_text(json.dumps(lv))
        return simlib.summarize(simlib.run(simlib.cases_for(p, self.game, seeds, 's')))


def propose(L, stream, perm, rng, queue_only=False):
    """One random legal move: ('seg', new stream) inside a column, ('swap', new stream) across columns, or a queue swap."""
    off = L.offsets
    cols = [c for c in range(len(off) - 1) if off[c + 1] - off[c] >= 8]
    kind = 0.0 if queue_only else rng.random()
    s = list(stream)
    if kind < .10:
        n = len(perm)
        for _ in range(50):
            i, j = rng.sample(range(n), 2)
            q = list(perm); q[i], q[j] = q[j], q[i]
            if L.b['queue'][q[i]] != L.b['queue'][q[j]] and L.queue_ok(q):
                return ('queue', s, q)
        return None
    if kind < .55 and cols:
        c = rng.choice(cols); lo, hi = off[c], off[c + 1]; n = hi - lo
        length = rng.randint(3, min(16, n // 2))
        i = lo + int((n - length) * rng.random() ** 2)
        j = lo + int((n - length) * rng.random() ** 2)
        if i == j:
            return None
        seg = s[i:i + length]
        rest = s[lo:i] + s[i + length:hi]
        k = j - lo if j < i else j - lo - 0
        k = max(0, min(len(rest), k))
        col = rest[:k] + seg + rest[k:]
        if col == s[lo:hi]:
            return None
        s[lo:hi] = col
        return ('seg', s, list(perm))
    if len(cols) >= 2:
        x, y = rng.sample(cols, 2)
        nx, ny = off[x + 1] - off[x], off[y + 1] - off[y]
        length = rng.randint(3, min(16, nx // 2, ny // 2))
        i = off[x] + int((nx - length) * rng.random() ** 2)
        j = off[y] + int((ny - length) * rng.random() ** 2)
        if s[i:i + length] == s[j:j + length]:
            return None
        s[i:i + length], s[j:j + length] = s[j:j + length], s[i:i + length]
        return ('swap', s, list(perm))
    return None


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--level', type=int, required=True)
    ap.add_argument('--budget', type=float, default=150)
    ap.add_argument('--proposals', type=int, default=12)
    ap.add_argument('--screen-seeds', type=int, default=4, help='每个候选先用几组局面快速筛')
    ap.add_argument('--finalists', type=int, default=3)
    ap.add_argument('--round-seeds', type=int, default=16, help='每轮重新抽取的比较局面数')
    ap.add_argument('--margin', type=float, default=.02, help='新改动至少要好这么多才换')
    ap.add_argument('--patience', type=int, default=8)
    ap.add_argument('--target', type=float, default=.06)
    ap.add_argument('--queue-only', action='store_true', help='只调排队顺序（同样每轮重新抽局面比较），状态文件加 -q')
    ap.add_argument('--init-perm', type=Path, help='从 tune_queue 状态文件的最佳排队起步（同一张图）')
    ap.add_argument('--validate', action='store_true')
    ap.add_argument('--apply', action='store_true')
    ap.add_argument('--accept', type=float, default=ACCEPT)
    args = ap.parse_args()
    t0 = time.time()
    L = Level(args.level)
    sha = file_sha256(L.raw)
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    sp = STATE_DIR / f'{args.level}-{sha[:12]}{"-q" if args.queue_only else ""}.json'
    st = json.loads(sp.read_text()) if sp.exists() else None
    n = len(L.b['queue'])
    if st is None:
        sa = simlib.summarize(simlib.run(simlib.cases_for(simlib.a_path(args.level), args.level, TUNE, 'a')))
        ident = list(range(n))
        if args.init_perm:
            ident = json.loads(args.init_perm.read_text())['best']['perm']
            if len(ident) != n or not L.queue_ok(ident):
                raise SystemExit('--init-perm 的排队与草稿不符')
        sb = L.evaluate(L.b['stream'], ident, TUNE)
        st = dict(game=args.level, draftSha256=sha, a=sa, rounds=0, start=simlib.gap(sa, sb),
                  best=dict(stream=L.b['stream'], perm=ident, summary=sb, gap=simlib.gap(sa, sb)), history=[], moves={})
        sp.write_text(json.dumps(st))
    sa = {int(k): v for k, v in st['a'].items()}

    if args.validate:
        v = st.setdefault('validationParts', {})
        key = hashlib.sha256(json.dumps([st['best']['stream'], st['best']['perm']]).encode()).hexdigest()
        if v.get('key') != key:
            v.clear(); v['key'] = key
        jobs = [('a', None), ('b0', (L.b['stream'], list(range(n)))), ('b1', (st['best']['stream'], st['best']['perm']))]
        for k, x in jobs:
            if k in v:
                continue
            if time.time() - t0 > args.budget * .4 and len(v) > 1:
                sp.write_text(json.dumps(st)); print(json.dumps(dict(level=args.level, validation='partial', have=sorted(v)))); return 0
            v[k] = (simlib.summarize(simlib.run(simlib.cases_for(simlib.a_path(args.level), args.level, VALID, 'a'))) if x is None
                    else L.evaluate(x[0], x[1], VALID))
            sp.write_text(json.dumps(st))
        va, v0, v1 = ({int(i): y for i, y in v[k].items()} for k in ('a', 'b0', 'b1'))
        st['validation'] = dict(original=simlib.gap(va, v0), tuned=simlib.gap(va, v1))
        sp.write_text(json.dumps(st))
        print(json.dumps(dict(level=args.level, **st['validation']), ensure_ascii=False))
        return 0

    if args.apply:
        val = st.get('validation')
        key = hashlib.sha256(json.dumps([st['best']['stream'], st['best']['perm']]).encode()).hexdigest()
        if not val or st.get('validationParts', {}).get('key') != key:
            print('先运行 --validate'); return 1
        if val['tuned']['score'] > args.accept or val['tuned']['score'] >= val['original']['score']:
            print(f"验证差距 {val['original']['score']:.3f} → {val['tuned']['score']:.3f}，未达到上限 {args.accept} 或没有改善，不写入"); return 2
        lv = L.build(st['best']['stream'], st['best']['perm'])
        problems = L.validate_problems(lv)
        if problems:
            print('校验失败：', '；'.join(problems)); return 1
        data = dumps(lv, L.raw)
        L.path.write_bytes(data)
        manifest = load_manifest()
        e = manifest['levels'][resource(args.level)]
        a_stream = (L.a_in_b.get('stream') or [])
        sb = e.setdefault('streamBalance', {})
        sb['changed'] = sum(x != y for x, y in zip(lv['stream'], a_stream))
        sb['length'] = len(lv['stream'])
        e['streamTune'] = dict(draftSha256=st['draftSha256'], moves=st['moves'], rounds=st['rounds'], tuningGap=st['best']['gap'],
                               validation=val, at=time.strftime('%Y-%m-%dT%H:%M:%S'))
        _, qs = queue_reorder(L.a_in_b['queue'], lv['queue'])
        e.setdefault('queueTune', {}).update(moved=qs['moved'], maxRowShift=qs['maxRowShift'])
        e['fileSha256'] = file_sha256(data)
        e['status'] = 'draft'
        save_manifest(manifest)
        print(json.dumps(dict(level=args.level, written=str(L.path.relative_to(ROOT)), streamChanged=sb['changed'],
                              queueMoved=qs['moved'], validation=round(val['tuned']['score'], 3)), ensure_ascii=False))
        return 0

    rng = random.Random(st['rounds'] * 104729 + args.level)
    screen = list(range(1, args.screen_seeds + 1))
    if st.get('aScreenSeeds') != len(screen):
        st['aScreen'] = simlib.summarize(simlib.run(simlib.cases_for(simlib.a_path(args.level), args.level, screen, 'a')))
        st['aScreenSeeds'] = len(screen)
    a_screen = {int(k): v for k, v in st['aScreen'].items()}
    last = st.get('roundSeconds', 60)
    while time.time() - t0 + last * 1.15 < args.budget:
        if st.get('stall', 0) >= args.patience:
            st['done'] = True
            break
        r0 = time.time()
        props = []
        for _ in range(200):
            p = propose(L, st['best']['stream'], st['best']['perm'], rng, args.queue_only)
            if p:
                props.append(p)
            if len(props) >= args.proposals:
                break
        screened = sorted(((simlib.gap(a_screen, L.evaluate(s, q, screen))['score'], kind, s, q) for kind, s, q in props),
                          key=lambda x: x[0])
        # Fresh seeds every round (from 1-200, never the validation seeds): the incumbent and the finalists are compared
        # on the same new sample, so the search cannot keep improving on a fixed lucky set.
        fresh = sorted(rng.sample(range(1, 201), args.round_seeds))
        if 'aRuns' not in st:   # A never changes: play seeds 1-200 once and reuse them
            st['aRuns'] = [{k: r[k] for k in ('strategy', 'seed', 'outcome', 'revived', 'progress', 'blockedSeconds', 'deploys')}
                           for r in simlib.run(simlib.cases_for(simlib.a_path(args.level), args.level, range(1, 201), 'a'))]
            sp.write_text(json.dumps(st))
        a_fresh = simlib.summarize([r for r in st['aRuns'] if r['seed'] in set(fresh)])
        inc = simlib.gap(a_fresh, L.evaluate(st['best']['stream'], st['best']['perm'], fresh))['score']
        st.setdefault('incumbentTrace', []).append(round(inc, 4))
        improved = False
        best_cand = None
        for _, kind, s, q in screened[:args.finalists]:
            g = simlib.gap(a_fresh, L.evaluate(s, q, fresh))
            if g['score'] < inc - args.margin and (best_cand is None or g['score'] < best_cand[0]):
                best_cand = (g['score'], kind, s, q)
        if best_cand:
            score, kind, s, q = best_cand
            st['best'] = dict(stream=s, perm=q, summary=None, gap=dict(score=score, ease=0.0, worst=0.0, seeds='fresh'))
            st['moves'][kind] = st['moves'].get(kind, 0) + 1
            improved = True
        st['stall'] = 0 if improved else st.get('stall', 0) + 1
        st['rounds'] += 1
        st['history'].append(round(best_cand[0] if best_cand else inc, 4))
        last = st['roundSeconds'] = time.time() - r0
        sp.write_text(json.dumps(st))
    changed = sum(x != y for x, y in zip(st['best']['stream'], L.b['stream']))
    print(json.dumps(dict(level=args.level, rounds=st['rounds'], start=round(st['start']['score'], 3),
                          best=round(st['best']['gap']['score'], 3), ease=round(st['best']['gap']['ease'], 3),
                          streamChanged=changed, moves=st['moves'], done=st.get('done', False), history=st['history'][-6:]),
                     ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
