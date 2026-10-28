"""Headless battle simulation (sim/, the game's own model + the Unity bot) for B level difficulty checks.

Results match the Unity batch (Editor/LevelsBDifficultyChecks.cs) case by case (2026-10-26: 400 of 400 games
identical) at about 26x the speed. Build once: dotnet build scripts/levels_b/sim -c Release -o ~/simbin
"""
import json
import os
import statistics as st
import subprocess
import tempfile
from pathlib import Path

from levelsb_common import LEVELS_A, ROOT, resource

DOTNET = os.environ.get('DOTNET', str(Path.home() / '.dotnet/dotnet'))
SIM_DLL = os.environ.get('LEVELSB_SIM', str(Path.home() / 'simbin/LevelsBSim.dll'))
STRATEGIES = {0: '内置建议', 1: '优先攻击', 2: '优先合成', 3: '优先清位'}


def run(cases, threads=None):
    """cases: dicts with name, path, game, strategy, seed and optional queue (permutation of entry indices)."""
    with tempfile.TemporaryDirectory() as tmp:
        inp, out = Path(tmp) / 'in.json', Path(tmp) / 'out.json'
        inp.write_text(json.dumps({'cases': cases}), encoding='utf-8')
        cmd = [DOTNET, SIM_DLL, '--input', str(inp), '--output', str(out)]
        if threads:
            cmd += ['--threads', str(threads)]
        subprocess.run(cmd, check=False, capture_output=True)
        report = json.loads(out.read_text(encoding='utf-8'))
    bad = [r for r in report['results'] if r['outcome'] in ('error', 'timeout')]
    if bad:
        raise RuntimeError(f'模拟异常 {len(bad)} 局：{bad[0]["name"]} {bad[0]["outcome"]} {bad[0]["error"][:300]}')
    return report['results']


def cases_for(path, game, seeds, tag, strategies=STRATEGIES, queue=None):
    out = []
    for s in strategies:
        for seed in seeds:
            c = dict(name=f'{tag}-{s}-{seed}', path=str(path), game=game, strategy=s, seed=seed)
            if queue is not None:
                c['queue'] = list(queue)
            out.append(c)
    return out


def summarize(results):
    """Per strategy: win rate, mean progress, revive rate, mean blocked seconds, mean deploys."""
    by = {}
    for r in results:
        by.setdefault(r['strategy'], []).append(r)
    return {s: dict(n=len(v), win=st.mean(r['outcome'] == 'won' for r in v), prog=st.mean(r['progress'] for r in v),
                    revive=st.mean(bool(r['revived']) for r in v), blocked=st.mean(r['blockedSeconds'] for r in v),
                    deploys=st.mean(r['deploys'] for r in v)) for s, v in sorted(by.items())}


def gap(sa, sb):
    """Difficulty gap B vs A over strategies (0 = same).
    score = mean over strategies of |Δwin| + |Δprogress| + 0.5 |Δrevive|;
    ease = mean signed (Δwin + Δprogress) / 2, positive means B is easier; worst = largest single |Δ|."""
    keys = [s for s in sa if s in sb]
    d = {s: {k: sb[s][k] - sa[s][k] for k in ('win', 'prog', 'revive')} for s in keys}
    score = st.mean(abs(d[s]['win']) + abs(d[s]['prog']) + .5 * abs(d[s]['revive']) for s in keys)
    ease = st.mean((d[s]['win'] + d[s]['prog']) / 2 for s in keys)
    worst = max(max(abs(d[s]['win']), abs(d[s]['prog']), abs(d[s]['revive'])) for s in keys)
    return dict(score=score, ease=ease, worst=worst)


def a_path(game):
    return LEVELS_A / f'{resource(game)}.json'
