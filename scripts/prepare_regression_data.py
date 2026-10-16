#!/usr/bin/env python3
"""Copy frozen regression inputs/evidence locally without changing their bytes."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('archive', type=Path)
args = parser.parse_args()
archive = args.archive.resolve()
paths = [
    'Tools/work/stages/model-campaign-input-v2',
    'Tools/work/stages/model-specials-input-v1',
    'Tools/work/stages/model-indexed-AW-input-v1/sector02',
    'Tools/work/stages/model-indexed-AW-input-v1/campaign.json',
    'Tools/work/stages/numeric-BR-legacy-input-v2',
]
manifest = []
evidence = set()

def digest(file):
    h = hashlib.sha256()
    with file.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()

def copy(file, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    sha = digest(file)
    if target.exists():
        if target.is_symlink():
            raise ValueError('Refusing to write through a symlink: ' + str(target))
        if digest(target) != sha:
            target.chmod(target.stat().st_mode | 0o200)
            shutil.copy2(file, target)
    else:
        shutil.copy2(file, target)
    assert digest(target) == sha, target
    manifest.append(dict(source=file.relative_to(archive).as_posix(),
                         target=target.relative_to(ROOT).as_posix(),
                         bytes=file.stat().st_size, sha256=sha))

for name in paths:
    path = archive / name
    files = [path] if path.is_file() else sorted(f for f in path.rglob('*') if f.is_file())
    if not files:
        raise FileNotFoundError(path)
    for file in files:
        relative = file.relative_to(archive).as_posix()
        copy(file, ROOT / ('tools/' + relative[len('Tools/'):]))
        if file.name == 'source.json':
            for entry in json.loads(file.read_text()).get('files', []):
                evidence.add(entry['path'])
for name in sorted(evidence):
    copy(archive / name, ROOT / 'tools' / name)
(ROOT / 'tools/data-manifest.json').write_text(json.dumps(
    dict(archive_root=str(archive), files=manifest), ensure_ascii=False, indent=2) + '\n')
print(json.dumps(dict(copied_files=len(manifest), bytes=sum(f['bytes'] for f in manifest))))
