#!/usr/bin/env python3
"""Plan step 3 test channel: put B levels that are NOT approved into the game for testing only.

  python3 scripts/levels_b/stage_test.py 3 7            stage the current drafts of game levels 3 and 7
  python3 scripts/levels_b/stage_test.py --temp 3 7 40  stage mirrored A pictures (no drafts needed)
  python3 scripts/levels_b/stage_test.py --clean        remove the test channel

Files go to UnityProject/Assets/Flightbeat/Resources/levels/B-test/ with B-test/test.json (same format as
released.json, each entry "status": "test"). The game uses them only while that list exists; reports show
the source as "B-test". The directory is git-ignored, a normal build refuses to run while it exists
(publish.py --check), and R1_LEVELS_TEST=1 builds are marked as test builds that must not be published.
Nothing here is marked approved.
"""
import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import make_temp
from levelsb_common import ROOT, cells_sha256, file_sha256, label, load_manifest, resource
from publish import TEST_DIR, check_entry


def clean():
    removed = TEST_DIR.exists()
    shutil.rmtree(TEST_DIR, ignore_errors=True)
    meta = TEST_DIR.with_name(TEST_DIR.name + '.meta')
    if meta.exists():
        meta.unlink()
    if TEST_DIR.exists() or meta.exists():
        print(f'清除失败：{TEST_DIR.relative_to(ROOT)} 仍在，请手动删除'); return 1
    print('已清除测试通道' if removed else '测试通道本来就是空的')
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('levels', type=int, nargs='*', metavar='关号')
    ap.add_argument('--temp', action='store_true', help='用 A 原图左右翻转生成测试关，不需要草稿')
    ap.add_argument('--clean', action='store_true')
    args = ap.parse_args()
    if args.clean:
        return clean()
    if not args.levels:
        ap.error('给出关号，或用 --clean')
    manifest = load_manifest()['levels']
    staged = []
    with tempfile.TemporaryDirectory(prefix='levels-b-stage-') as tmp:
        for game in args.levels:
            res = resource(game)
            if args.temp:
                picture = make_temp.make(game, 'flip', Path(tmp) / 'images')
                out = Path(tmp) / res
                run = subprocess.run([sys.executable, str(HERE / 'convert.py'), '--level', str(game), '--image', str(picture),
                                      '--out', str(out), '--no-manifest', '--title', '测试翻转'], capture_output=True, text=True)
                if run.returncode not in (0, 2):
                    print(run.stdout + run.stderr); return 1
                data = (out / f'{res}.json').read_bytes()
                entry = dict(game=game, resource=res, mode='S')
            else:
                entry = manifest.get(res)
                if not entry:
                    print(f'{label(game)}：清单里没有草稿，先用 convert.py 生成，或加 --temp'); return 1
                data = (ROOT / entry['draft']).read_bytes()
                problems = check_entry(entry, data)
                if problems:
                    print(f'{label(game)}：', '；'.join(problems)); return 1
            TEST_DIR.mkdir(parents=True, exist_ok=True)
            (TEST_DIR / f'{res}.json').write_bytes(data)
            staged.append(dict(game=game, resource=res, fileSha256=file_sha256(data),
                               cellsSha256=cells_sha256(json.loads(data)['cells']), status='test'))
            print(f'{label(game)} → B-test')
    old = json.loads((TEST_DIR / 'test.json').read_text())['levels'] if (TEST_DIR / 'test.json').exists() else []
    merged = {e['resource']: e for e in old}
    merged.update({e['resource']: e for e in staged})
    levels = sorted(merged.values(), key=lambda e: e['game'])
    (TEST_DIR / 'test.json').write_text(json.dumps(dict(levels=levels), ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    print(f'测试通道共 {len(levels)} 关。测试构建：R1_LEVELS_TEST=1 bash scripts/r1v3/build.sh；测完运行 stage_test.py --clean')
    return 0


if __name__ == '__main__':
    sys.exit(main())
