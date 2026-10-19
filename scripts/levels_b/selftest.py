#!/usr/bin/env python3
"""Self test for the B-set tools. Uses an isolated temporary directory (removed after).

  python3 scripts/levels_b/selftest.py
"""
import json
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import levelsb_common as lc
import make_temp

# Fixed sample shared with the C# runtime check (plan 3rd review P2-1).
CELLS_SAMPLE = [3, 3, 10, 0, 17]


def run(*args):
    p = subprocess.run([sys.executable, str(HERE / 'convert.py'), *args], capture_output=True, text=True)
    return p.returncode, p.stdout + p.stderr


def main():
    fails = []
    ok = lambda cond, msg: (print(('  ok  ' if cond else '  失败 ') + msg), None if cond else fails.append(msg))

    print('1. 原关卡文件可按原格式逐字节重写')
    games = range(lc.FIRST, lc.LAST + 1)
    exact = sum(lc.dumps(*lc.load_a(g)) == lc.load_a(g)[1] for g in games)
    ok(exact == len(games), f'{exact}/{len(games)} 关逐字节一致')

    print('2. 棋盘哈希口径')
    sha = lc.cells_sha256(CELLS_SAMPLE)
    print(f'      样例 {CELLS_SAMPLE} → {sha}')
    ok(sha == lc.file_sha256(b'3,3,10,0,17'), '等于 "3,3,10,0,17" 的 SHA-256')

    print('3. 校验能拦截篡改')
    a, _ = lc.load_a(5)
    b = json.loads(json.dumps(a))
    ok(lc.validate(a, b, 'S') == [], '原样复制通过')
    b['queue'][0]['ammo'] += 1
    ok(any('queue' in p for p in lc.validate(a, b, 'S')), '改一个队员子弹 → 报 queue 不一致')
    b = json.loads(json.dumps(a))
    other = next(i for i, color in enumerate(a['cells']) if color != a['cells'][0])
    b['cells'][0], b['cells'][other] = b['cells'][other], b['cells'][0]
    ok(b['cells'] != a['cells'] and lc.counts(b['cells']) == lc.counts(a['cells']),
       '确实交换了两种不同颜色，格子数保持不变')
    ok(lc.validate(a, b, 'S') == [], '只调换格子位置仍通过')
    b = json.loads(json.dumps(a)); b['cells'][0] = (b['cells'][0] + 1) % 18
    ok(any('格子数' in p for p in lc.validate(a, b, 'S')), '改一格颜色 → 报格子数不一致')
    used = lc.level_colors(a)
    sigma = dict(zip(used, used[1:] + used[:1]))
    p = lc.apply_sigma(a, sigma)
    ok(lc.validate(a, p, 'P', sigma) == [], '整关置换后（模式 P）通过')
    ok(lc.validate(a, p, 'S') != [], '同一关按模式 S 校验被拒')
    wrong = dict(zip(used, used[2:] + used[:2]))
    ok(lc.validate(a, p, 'P', wrong) != [], '用错的置换还原被拒')

    print('4. 临时图端到端')
    with tempfile.TemporaryDirectory(prefix='levels-b-selftest-') as temporary:
        out = Path(temporary)
        for game in (3, 7, 40):
            for kind, extra, expect_zero in (('flip', [], True), ('recolor', ['--auto-sigma'], None), ('soft', [], False)):
                img = make_temp.make(game, kind, out / 'images')
                code, text = run('--level', str(game), '--image', str(img), '--out', str(out / f'{game}-{kind}'),
                                 '--no-manifest', *extra)
                rep = json.loads((out / f'{game}-{kind}' / f'{lc.file_tag(game)}.report.json').read_text()) if code in (0, 2) else {}
                note = f'改动 {rep.get("changedCells")} 格，模式 {rep.get("mode")}' if rep else text.strip()
                ok(code in (0, 2), f'{lc.label(game)} {kind}：转换并通过校验（{note}）')
                if expect_zero:
                    ok(rep.get('changedCells') == 0, f'{lc.label(game)} {kind}：翻转图无需补齐')
    print('全部通过' if not fails else f'{len(fails)} 项失败')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
