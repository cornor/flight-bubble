#!/usr/bin/env python3
"""Real Unity check: identical level/strategy/seed with diagnostics off vs on must have identical game results.
No manifest, Resources, source pictures or player saves are changed.
"""
import argparse
import json
import subprocess
from datetime import datetime
from pathlib import Path

from evaluate import STRATEGIES, UNITY
from levelsb_common import LEVELS_A, ROOT, file_sha256, load_manifest, resource


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('levels', type=int, nargs='+')
    ap.add_argument('--runs', type=int, default=2)
    ap.add_argument('--out', type=Path)
    ap.add_argument('--unity', default=UNITY)
    args = ap.parse_args()
    if args.runs < 1:
        ap.error('--runs 必须大于 0')
    out = (args.out or ROOT / 'Builds/R1-evidence' / f'levels-b-trace-check-{datetime.now():%Y%m%d-%H%M%S}').resolve()
    out.mkdir(parents=True, exist_ok=False)
    cases, pairs, sources = [], [], {}
    manifest = load_manifest()
    for game in args.levels:
        res = resource(game)
        entry = manifest['levels'][res]
        b = ROOT / entry['draft']
        if file_sha256(b.read_bytes()) != entry['fileSha256']:
            raise ValueError(f'第 {game} 关草稿哈希不一致')
        for side, path in [('A', LEVELS_A / f'{res}.json'), ('B', b)]:
            sources[str(path)] = file_sha256(path.read_bytes())
            for strategy in STRATEGIES:
                for seed in range(1, args.runs + 1):
                    prefix = f'{game}-{side}-{strategy}-{seed}'
                    pairs.append((prefix + '-off', prefix + '-on'))
                    for trace in (False, True):
                        cases.append(dict(name=prefix + ('-on' if trace else '-off'), path=str(path),
                                          game=game, strategy=strategy, seed=seed, trace=trace))
    inp, result = out / 'input.json', out / 'results.json'
    inp.write_text(json.dumps(dict(cases=cases), ensure_ascii=False, indent=1) + '\n')
    print(f'{len(cases)} 局；日志开关对照，证据：{out}', flush=True)
    started = datetime.now()
    proc = subprocess.run([args.unity, '-batchmode', '-nographics', '-quit', '-projectPath', str(ROOT / 'UnityProject'),
                           '-executeMethod', 'Flightbeat.Editor.LevelsBDifficultyChecks.Run',
                           '--levels-b-eval-input', str(inp), '--levels-b-eval-output', str(result),
                           '-logFile', str(out / 'unity.log')], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    report = json.loads(result.read_text()) if result.exists() else {}
    runs = report.get('results', [])
    got = {r['name']: r for r in runs}
    errors = []
    if proc.returncode or not report.get('passed') or report.get('error'):
        errors.append('Unity 未成功')
    if len(got) != len(cases) or len(runs) != len(cases) or set(got) != {c['name'] for c in cases}:
        errors.append('用例缺失、重复或多出')
    else:
        for case in cases:
            r = got[case['name']]
            if any(r[k] != case[k] for k in ('game', 'strategy', 'seed', 'path')):
                errors.append(f'{case["name"]} 身份字段不一致')
        for off, on in pairs:
            a, b = got[off], got[on]
            core = lambda r: {k: v for k, v in r.items() if k not in ('name', 'trace')}
            if core(a) != core(b):
                errors.append(f'{off} 对 {on} 结果改变')
            if a['outcome'] in ('error', 'timeout') or not (b.get('trace') or {}).get('events'):
                errors.append(f'{on} 未完成有效日志验证')
    if any(file_sha256(Path(p).read_bytes()) != sha for p, sha in sources.items()):
        errors.append('输入关卡在运行中变化')
    summary = dict(passed=not errors, pairs=len(pairs), games=len(cases), errors=errors, sources=sources,
                   seconds=(datetime.now() - started).total_seconds())
    (out / 'report.json').write_text(json.dumps(summary, ensure_ascii=False, indent=1) + '\n')
    print(json.dumps(summary, ensure_ascii=False, indent=1))
    return int(bool(errors))


if __name__ == '__main__':
    raise SystemExit(main())
