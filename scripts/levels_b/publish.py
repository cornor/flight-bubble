#!/usr/bin/env python3
"""Plan step 3: release approved B levels into the game, and check what the game would ship.

  python3 scripts/levels_b/publish.py --approve 3 7 [--without-evaluation]   mark drafts approved
  python3 scripts/levels_b/publish.py                                         copy approved levels to Resources
  python3 scripts/levels_b/publish.py --check [--allow-test]                  pre-build check (build.sh runs it)

Release: every manifest entry with status 'approved' is re-validated against its A level, copied to
UnityProject/Assets/Flightbeat/Resources/levels/B/<sectorNN>.json, and listed in B/released.json
({"levels": [{game, resource, fileSha256, cellsSha256}]}); B files that are no longer approved are removed.
The game enables a B level only when it is in released.json and its file hash matches.

Check: B/ holds exactly the released files with matching hashes, each approved in the manifest and valid;
Resources/levels/B-test/ (test channel, stage_test.py) must not exist unless --allow-test.
"""
import argparse
import json
import shutil
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from levelsb_common import (FIRST, LAST, LEVELS_A, ROOT, cells_sha256, file_sha256, label, load_a, load_manifest,
                            resource, save_manifest, validate)

B_DIR = LEVELS_A / 'B'
TEST_DIR = LEVELS_A / 'B-test'
RELEASED = B_DIR / 'released.json'


def sigma_of(entry):
    return {int(k): v for k, v in (entry.get('sigma') or {}).items()} or None


def check_entry(entry, data):
    """Problems for one manifest entry against the given file bytes."""
    problems = []
    game = entry['game']
    if not FIRST <= game <= LAST or entry['resource'] != resource(game):
        return [f'关号与资源名不符：{game} {entry["resource"]}']
    if file_sha256(data) != entry.get('fileSha256'):
        problems.append('文件哈希与清单不一致')
    level = json.loads(data)
    if cells_sha256(level['cells']) != entry.get('cellsSha256'):
        problems.append('棋盘哈希与清单不一致')
    a, _ = load_a(game)
    sigma = sigma_of(entry)
    full = None
    if sigma:
        full = {c: c for c in set(a['cells']) | set(a.get('stream') or [])}
        full.update(sigma)
    problems += validate(a, level, entry.get('mode', 'S'), full)
    return problems


def approve(games, without_evaluation):
    manifest = load_manifest()
    bad = 0
    for game in games:
        res = resource(game)
        entry = manifest['levels'].get(res)
        if not entry:
            print(f'{label(game)}：清单里没有，先用 convert.py 生成草稿'); bad += 1; continue
        if not entry.get('withinLimits'):
            print(f'{label(game)}：补齐改动超限，不能验收'); bad += 1; continue
        if entry.get('status') != 'evaluated' and not without_evaluation:
            print(f'{label(game)}：还没有通过难度评估（状态 {entry.get("status")}）；确需先验收请加 --without-evaluation'); bad += 1; continue
        data = (ROOT / entry['draft']).read_bytes()
        problems = check_entry(entry, data)
        if problems:
            print(f'{label(game)}：', '；'.join(problems)); bad += 1; continue
        # Without a passing bot evaluation (none yet, 超差 or 无法判断) the approval rests on a human playtest.
        entry['approvedWithoutEvaluation'] = entry.get('status') != 'evaluated'
        entry['status'] = 'approved'
        entry['approved'] = datetime.now().isoformat(timespec='seconds')
        print(f'{label(game)}：已验收')
    save_manifest(manifest)
    return 1 if bad else 0


def release():
    manifest = load_manifest()
    approved = sorted((e for e in manifest['levels'].values() if e.get('status') == 'approved'), key=lambda e: e['game'])
    B_DIR.mkdir(parents=True, exist_ok=True)
    listed = []
    for entry in approved:
        data = (ROOT / entry['draft']).read_bytes()
        problems = check_entry(entry, data)
        if problems:
            print(f'{label(entry["game"])} 不发布：', '；'.join(problems))
            return 1
        (B_DIR / f'{entry["resource"]}.json').write_bytes(data)
        listed.append(dict(game=entry['game'], resource=entry['resource'], fileSha256=entry['fileSha256'],
                           cellsSha256=entry['cellsSha256']))
    keep = {f'{e["resource"]}.json' for e in listed} | {'released.json'}
    for f in B_DIR.iterdir():
        name = f.name[:-5] if f.name.endswith('.meta') else f.name
        if f.is_file() and name not in keep:
            f.unlink()
            print(f'移除未验收的文件：{f.relative_to(ROOT)}')
    RELEASED.write_text(json.dumps(dict(levels=listed), ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    print(f'已发布 {len(listed)} 关到 {B_DIR.relative_to(ROOT)}')
    return 0


def check(allow_test):
    problems = []
    if TEST_DIR.exists() and not allow_test:
        problems.append(f'{TEST_DIR.relative_to(ROOT)} 存在（测试通道）：正式构建前先运行 stage_test.py --clean，'
                        '或用 R1_LEVELS_TEST=1 做测试构建')
    if B_DIR.exists():
        listed = json.loads(RELEASED.read_text(encoding='utf-8'))['levels'] if RELEASED.exists() else []
        manifest = load_manifest()['levels']
        names = {f'{e["resource"]}.json' for e in listed} | {'released.json'}
        # Resources imports other extensions and subdirectories too. Only the listed files and
        # their own existing Unity .meta companions may ship; an orphan .meta is not an approval.
        for f in sorted(B_DIR.rglob('*')):
            if f.is_dir() and not f.is_symlink():
                continue
            relative = f.relative_to(B_DIR).as_posix()
            companion = relative[:-5] if relative.endswith('.meta') else None
            allowed = relative in names or (companion in names and (B_DIR / companion).is_file())
            if f.is_symlink() or not allowed:
                problems.append(f'{f.relative_to(ROOT)} 不在 released.json 里')
        for e in listed:
            f = B_DIR / f'{e["resource"]}.json'
            m = manifest.get(e['resource'], {})
            if not f.exists():
                problems.append(f'{label(e["game"])}：released.json 列出但文件不存在'); continue
            if m.get('status') != 'approved' or m.get('fileSha256') != e['fileSha256']:
                problems.append(f'{label(e["game"])}：清单里不是已验收状态，或哈希不一致')
            problems += [f'{label(e["game"])}：{p}' for p in check_entry(e | {'mode': m.get('mode', 'S'), 'sigma': m.get('sigma')}, f.read_bytes())]
    for p in problems:
        print('  ✗', p)
    count = len(json.loads(RELEASED.read_text())['levels']) if RELEASED.exists() else 0
    print(f'B 套发布检查：{"通过" if not problems else "失败"}（已发布 {count} 关'
          + ('，含测试通道' if TEST_DIR.exists() else '') + '）')
    return 1 if problems else 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--approve', type=int, nargs='+', metavar='关号')
    ap.add_argument('--without-evaluation', action='store_true')
    ap.add_argument('--check', action='store_true')
    ap.add_argument('--allow-test', action='store_true')
    args = ap.parse_args()
    if args.approve:
        return approve(args.approve, args.without_evaluation)
    if args.check:
        return check(args.allow_test)
    return release()


if __name__ == '__main__':
    sys.exit(main())
