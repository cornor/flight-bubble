#!/usr/bin/env python3
"""Step 2 acceptance: actual converter output -> Unity JsonUtility -> Battle, with cross-language hashes.

Keeps inputs, previews, Unity log and report in a fresh Builds/R1-evidence directory.
Does not write the authoring manifest, Resources, PlayerPrefs or the WebGL output.
"""
import argparse
import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path

import levelsb_common as lc
import make_temp

UNITY = '/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity'
GAMES = (3, 7, 11, 40, 81)  # no stream; varied palettes; mystery members; nine board colours


def fixture(game, path, mode, name, expected_error=''):
    raw = path.read_bytes()
    level = json.loads(raw)
    totals = [0] * (len(level['palette']) // 3)
    for color in level['cells'] + (level.get('stream') or []):
        totals[color] += 1
    return dict(name=name, game=game, resource=lc.resource(game), mode=mode, path=str(path),
                fileSha256=lc.file_sha256(raw), cellsSha256=lc.cells_sha256(level['cells']),
                total=sum(totals), lanes=level['laneCount'], members=len(level['queue']),
                colorTotals=totals, expectedError=expected_error)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, help='新的证据目录；默认 Builds/R1-evidence/levels-b-construction-时间')
    parser.add_argument('--unity', default=UNITY, help='Unity 编辑器可执行文件')
    args = parser.parse_args()
    if not Path(args.unity).is_file():
        parser.error(f'Unity 不存在：{args.unity}')
    out = (args.out or lc.ROOT / 'Builds/R1-evidence' /
           ('levels-b-construction-' + datetime.now().strftime('%Y%m%d-%H%M%S-%f'))).resolve()
    out.mkdir(parents=True, exist_ok=False)
    cases = []
    for game in GAMES:
        for kind in ('flip', 'recolor', 'soft'):
            picture = make_temp.make(game, kind, out / 'images')
            folder = out / 'converted' / f'{game}-{kind}'
            cmd = [sys.executable, str(Path(__file__).with_name('convert.py')), '--level', str(game),
                   '--image', str(picture), '--out', str(folder), '--no-manifest']
            if kind == 'recolor':
                cmd.append('--auto-sigma')
            run = subprocess.run(cmd, capture_output=True, text=True)
            (out / f'{game}-{kind}.log').write_text(run.stdout + run.stderr, encoding='utf-8')
            if run.returncode != 0:
                raise RuntimeError(f'转换失败或超限：{game}-{kind}，见 {out}')
            report = json.loads((folder / f'{lc.file_tag(game)}.report.json').read_text())
            case = fixture(game, folder / f'{lc.resource(game)}.json', report['mode'], f'{game}-{kind}')
            if (case['fileSha256'], case['cellsSha256']) != (report['fileSha256'], report['cellsSha256']):
                raise RuntimeError(f'转换报告哈希不匹配：{case["name"]}')
            if kind == 'recolor' and report['mode'] != 'P':
                raise RuntimeError(f'置换用例没有覆盖模式 P：{case["name"]}')
            cases.append(case)
    # A malformed board must be rejected by the actual constructor, not just the Python validator.
    broken = json.loads(Path(cases[0]['path']).read_text())
    broken['cells'].pop()
    broken_path = out / 'invalid-board.json'
    broken_path.write_bytes(lc.dumps(broken, lc.load_a(3)[1]))
    cases.append(fixture(3, broken_path, 'invalid', 'invalid-board-length', 'Invalid authored level'))
    samples = [dict(name=name, cells=cells, cellsSha256=lc.cells_sha256(cells)) for name, cells in (
        ('documented', [3, 3, 10, 0, 17]), ('empty', []), ('signed', [-1, 0, 2147483647, -2147483648]))]
    input_path = out / 'input.json'
    input_path.write_text(json.dumps(dict(samples=samples, cases=cases), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    report_path = out / 'report.json'
    print(f'已生成 {len(cases) - 1} 个转换用例和 1 个非法棋盘；启动 Unity：{out}', flush=True)
    run = subprocess.run([args.unity, '-batchmode', '-nographics', '-quit', '-projectPath', str(lc.ROOT / 'UnityProject'),
                          '-executeMethod', 'Flightbeat.Editor.LevelsBConstructionChecks.Run',
                          '--levels-b-input', str(input_path), '--levels-b-output', str(report_path),
                          '-logFile', str(out / 'unity.log')], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    if run.returncode != 0 or not report_path.exists():
        raise RuntimeError(f'Unity 验收失败（退出码 {run.returncode}），见 {out / "unity.log"}')
    report = json.loads(report_path.read_text())
    if not report['passed'] or len(report['cases']) != len(cases) or len(report['hashes']) != len(samples) * 2:
        raise RuntimeError(f'验收报告不完整或未通过：{report_path}')
    print(f'通过：15 个 B 关构造、非法棋盘拒绝、逐文件/棋盘哈希及 6 次固定样例跨语言校验。\n报告：{report_path}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
