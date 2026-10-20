#!/usr/bin/env python3
"""Plan step 4: difficulty comparison of B levels against their A levels with a bot (Unity batch mode).

  python3 scripts/levels_b/evaluate.py 3 7            drafts from the manifest vs A (updates the manifest)
  python3 scripts/levels_b/evaluate.py --temp 3 7      mirrored A pictures vs A (tool check, no manifest)
  python3 scripts/levels_b/evaluate.py --self 3 7      A vs A with the same seeds: must be identical (determinism)
  python3 scripts/levels_b/evaluate.py --noise 3 7     A vs A with other seeds: the bot's own spread (calibration)
  options: --runs 12  --strategies 0,1,2,3  --out NEW-EVIDENCE-DIR  --unity PATH
Close the Unity editor that has this project open first.

Unity (Editor/LevelsBDifficultyChecks.cs) plays every level x strategy x run to the end. Single deterministic
runs proved chaotic (16:20: a mirrored board with identical counts flipped won/lost), so each strategy runs
--runs times with a seeded "human" spread (decision timing, tie-breaks, hesitation); A and B use the same
seeds. 17:40 (user decision, plan A+D): the bot's own spread (--noise: unchanged levels with other seeds moved
win rates by up to 33 points and times by 11 %) is as large as most real differences, so the bot is only a
coarse screen for clear failures:
  win rate or revive rate differs by WIN_GAP (50 points) or more in any strategy  -> 超差
  any run missing, errored or timed out                                         -> 无法判断
  otherwise                                                                     -> 通过（粗筛）
A level becomes 'evaluated' only when this screen and structure.py both passed on the same file.
Medians of time / blocked time / closest approach and progress stay in the report for reading.
Fine-grained equality is judged by structure.py (check D) and the human playtest. Passing drafts become 'evaluated' in the manifest
(publish.py --approve needs that). Results, inputs and the Unity log stay in the evidence directory.
"""
import argparse
import json
import statistics as st
import subprocess
import sys
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import make_temp
from levelsb_common import LEVELS_A, ROOT, file_sha256, label, load_manifest, record_check, resource

UNITY = '/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity'
STRATEGIES = {0: '内置建议', 1: '优先攻击', 2: '优先合成', 3: '优先清位'}
# Initial tolerances; calibrate with --noise (the bot's own spread on unchanged levels) and the pilot.
WIN_GAP, TIME_REL, BLOCKED_REL, BLOCKED_ABS, DISTANCE_PX = .50, .15, .15, 3.0, 3.0


def summary(runs):
    won = [r for r in runs if r['outcome'] == 'won']
    med = lambda key, rs: st.median(r[key] for r in rs) if rs else None
    return dict(runs=len(runs), winRate=len(won) / len(runs), reviveRate=sum(r['revived'] for r in runs) / len(runs),
                timeouts=sum(r['outcome'] in ('timeout', 'error') for r in runs),
                time=med('seconds', won), blocked=med('blockedSeconds', won),
                distance=med('minDistance', [r for r in won if r['minDistance'] >= 0]),
                progress=st.mean(r['progress'] for r in runs))


def compare(a, b):
    if a['timeouts'] or b['timeouts'] or a['runs'] != b['runs']:
        return '无法判断', f'有超时或异常局（A {a["timeouts"]} / B {b["timeouts"]}）'
    if abs(a['winRate'] - b['winRate']) >= WIN_GAP:
        return '超差', f'胜率 {a["winRate"]:.0%}→{b["winRate"]:.0%}'
    if abs(a['reviveRate'] - b['reviveRate']) >= WIN_GAP:
        return '超差', f'复活率 {a["reviveRate"]:.0%}→{b["reviveRate"]:.0%}'
    return '通过', '粗筛'


def compare_fine(a, b):   # former fine comparison, kept for reference reports
    if abs(a['winRate'] - b['winRate']) > .25:
        return '超差', f'胜率 {a["winRate"]:.0%}→{b["winRate"]:.0%}'
    if a['winRate'] < .5 or b['winRate'] < .5:
        return '无法判断', f'胜率都低（{a["winRate"]:.0%} / {b["winRate"]:.0%}），清除 {a["progress"]:.0%} / {b["progress"]:.0%}'
    notes = []
    if abs(b['time'] - a['time']) > TIME_REL * a['time']:
        notes.append(f'用时 {a["time"]:.0f}→{b["time"]:.0f}s')
    if abs(b['blocked'] - a['blocked']) > max(BLOCKED_REL * a['blocked'], BLOCKED_ABS):
        notes.append(f'卡住 {a["blocked"]:.1f}→{b["blocked"]:.1f}s')
    if a['distance'] is not None and b['distance'] is not None and abs(b['distance'] - a['distance']) > DISTANCE_PX:
        notes.append(f'最近距离 {a["distance"]:.0f}→{b["distance"]:.0f}px')
    return ('超差', '；'.join(notes)) if notes else ('通过', '')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('levels', type=int, nargs='+', metavar='关号')
    mode = ap.add_mutually_exclusive_group()
    for m in ('temp', 'self', 'noise'):
        mode.add_argument('--' + m, action='store_true')
    ap.add_argument('--runs', type=int, default=12)
    ap.add_argument('--strategies', default='0,1,2,3')
    ap.add_argument('--out', type=Path)
    ap.add_argument('--unity', default=UNITY)
    ap.add_argument('--trace', action='store_true', help='记录开局、首次卡住前决策和复活/终局快照，不改变打法')
    ap.add_argument('--no-manifest', action='store_true', help='隔离评估，只写证据，不更新制作清单')
    ap.add_argument('--candidate-dir', type=Path, help='从独立目录读取 sectorNN.json，必须配合 --no-manifest')
    args = ap.parse_args()
    strategies = [int(x) for x in args.strategies.split(',')]
    kind = next((m for m in ('temp', 'self', 'noise') if getattr(args, m)), 'draft')
    if args.candidate_dir and (kind != 'draft' or not args.no_manifest):
        ap.error('--candidate-dir 只用于独立候选对比，必须配合 --no-manifest，不能与 self/noise/temp 混用')
    out = (args.out or ROOT / 'Builds/R1-evidence' / f'levels-b-eval-{kind}-{datetime.now():%Y%m%d-%H%M%S}').resolve()
    out.mkdir(parents=True, exist_ok=False)
    manifest = load_manifest()
    cases, sources = [], {}
    for game in args.levels:
        res = resource(game)
        a_path = LEVELS_A / f'{res}.json'
        if args.self or args.noise:
            b_path = a_path
        elif args.temp:
            picture = make_temp.make(game, 'flip', out / 'images')
            folder = out / 'converted' / res
            run = subprocess.run([sys.executable, str(HERE / 'convert.py'), '--level', str(game), '--image', str(picture),
                                  '--out', str(folder), '--no-manifest'], capture_output=True, text=True)
            if run.returncode not in (0, 2):
                print(run.stdout + run.stderr); return 1
            b_path = folder / f'{res}.json'
        elif args.candidate_dir:
            b_path = (args.candidate_dir / f'{res}.json').resolve()
        else:
            entry = manifest['levels'].get(res)
            if not entry:
                print(f'{label(game)}：清单里没有草稿'); return 1
            b_path = ROOT / entry['draft']
            if file_sha256(b_path.read_bytes()) != entry['fileSha256']:
                print(f'{label(game)}：草稿文件与清单哈希不一致，先重新 convert'); return 1
        sources[game] = dict(a=str(a_path), aSha=file_sha256(a_path.read_bytes()),
                             b=str(b_path), bSha=file_sha256(b_path.read_bytes()))
        for s in strategies:
            for k in range(args.runs):
                seed = 1 + k
                cases.append(dict(name=f'{game}-A-{s}-{k}', path=str(a_path), game=game, strategy=s, seed=seed, trace=args.trace))
                cases.append(dict(name=f'{game}-B-{s}-{k}', path=str(b_path), game=game, strategy=s,
                                  seed=seed + 1000 if args.noise else seed, trace=args.trace))
    input_path, result_path = out / 'input.json', out / 'results.json'
    input_path.write_text(json.dumps(dict(cases=cases), ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    print(f'{len(args.levels)} 关 × {len(strategies)} 种打法 × {args.runs} 次 × A/B = {len(cases)} 局，启动 Unity…（证据：{out}）', flush=True)
    started = datetime.now()
    run = subprocess.run([args.unity, '-batchmode', '-nographics', '-quit', '-projectPath', str(ROOT / 'UnityProject'),
                          '-executeMethod', 'Flightbeat.Editor.LevelsBDifficultyChecks.Run',
                          '--levels-b-eval-input', str(input_path), '--levels-b-eval-output', str(result_path),
                          '-logFile', str(out / 'unity.log')], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    if not result_path.exists():
        print(f'Unity 没有生成结果（退出码 {run.returncode}），见 {out / "unity.log"}'); return 1
    print(f'Unity 用时 {(datetime.now() - started).total_seconds():.0f} 秒')
    report_unity = json.loads(result_path.read_text())
    if run.returncode != 0 or not report_unity.get('passed') or report_unity.get('error'):
        print(f'Unity 运行未成功（退出码 {run.returncode}），不记录评估状态；见 {out / "unity.log"}')
        return 1
    results = report_unity['results']
    # Every expected run must be there exactly once.
    expected = {c['name'] for c in cases}
    got = [r['name'] for r in results]
    if set(got) != expected or len(got) != len(expected):
        missing = sorted(expected - set(got))[:5]
        print(f'Unity 结果不完整：期望 {len(expected)} 局，得到 {len(got)} 局；缺少例如 {missing}'); return 1
    by = {}
    for r in results:
        side = r['name'].split('-')[1]
        by.setdefault((r['game'], r['strategy'], side), []).append(r)

    rows, level_verdicts = [], {}
    for game in args.levels:
        verdicts = []
        for s in strategies:
            ra, rb = by[(game, s, 'A')], by[(game, s, 'B')]
            a, b = summary(ra), summary(rb)
            if args.self:
                key = lambda r: tuple(json.dumps(r[k], sort_keys=True) for k in sorted(r) if k not in ('name', 'path'))
                same = [key(x) for x in sorted(ra, key=lambda r: r['seed'])] == [key(x) for x in sorted(rb, key=lambda r: r['seed'])]
                verdict, note = ('通过', '同种子结果完全相同') if same else ('超差', '同种子结果不同（不确定）')
            else:
                verdict, note = compare(a, b)
            verdicts.append(verdict)
            rows.append(dict(game=game, strategy=s, verdict=verdict, note=note, a=a, b=b))
        level_verdicts[game] = '超差' if '超差' in verdicts else '无法判断' if '无法判断' in verdicts else '通过'

    def fmt(x):
        t = f'{x["time"]:.0f}s' if x['time'] is not None else '-'
        bl = f'{x["blocked"]:.1f}s' if x['blocked'] is not None else '-'
        d = f'{x["distance"]:.0f}px' if x['distance'] is not None else '-'
        return f'胜 {x["winRate"]:.0%} 复活 {x["reviveRate"]:.0%} 用时 {t} 卡住 {bl} 距离 {d} 清除 {x["progress"]:.0%}'
    lines = [f'# B 套难度评估（{kind}，{datetime.now():%Y-%m-%d %H:%M}，每种打法 {args.runs} 次）', '',
             '| 关 | 打法 | 判定 | A | B | 说明 |', '|---|---|---|---|---|---|']
    for row in rows:
        lines.append(f'| {row["game"]} | {STRATEGIES[row["strategy"]]} | {row["verdict"]} | {fmt(row["a"])} | {fmt(row["b"])} | {row["note"]} |')
    lines += ['', '| 关 | 结论 |', '|---|---|'] + [f'| {label(g)} | {v} |' for g, v in level_verdicts.items()]
    (out / 'report.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    (out / 'report.json').write_text(json.dumps(dict(kind=kind, isolated=args.no_manifest, trace=args.trace,
                                                     candidateDirectory=str(args.candidate_dir) if args.candidate_dir else None,
                                                     runs=args.runs, levels=level_verdicts, rows=rows, sources=sources,
                                                     tolerance=dict(winGap=WIN_GAP, timeRel=TIME_REL, blockedRel=BLOCKED_REL,
                                                                    blockedAbs=BLOCKED_ABS, distancePx=DISTANCE_PX)),
                                                ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    for g, v in level_verdicts.items():
        print(f'{label(g)}：{v}')
    if args.trace:
        from diagnose import write_report
        write_report(results, out)
        print(f'诊断：{out / "diagnostics.md"}')
    if kind == 'draft' and not args.no_manifest:
        evidence = str(out.relative_to(ROOT)) if out.is_relative_to(ROOT) else str(out)
        for g, v in level_verdicts.items():
            record_check(g, 'bot', v, evidence, sources[g]['bSha'], dict(runs=args.runs))
    print(f'报告：{out / "report.md"}')
    return 0 if all(v == '通过' for v in level_verdicts.values()) else 2


if __name__ == '__main__':
    sys.exit(main())
