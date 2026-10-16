#!/usr/bin/env python3
"""Verify every protected file against SHA-256 exported from d53a2cc5."""
import hashlib, json, re, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
changed = []
checked = 0
for line in (ROOT / 'scripts/protected-files.sha256').read_text().splitlines():
    if not line or line.startswith('#'): continue
    expected, relative = line.split('  ', 1)
    file = ROOT / relative
    actual = hashlib.sha256(file.read_bytes()).hexdigest() if file.is_file() else None
    checked += 1
    if actual != expected: changed.append(dict(path=relative, expected=expected, actual=actual))
atlas = json.loads((ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1/atlas.json').read_text())
missing_glyphs = []
for file in (ROOT / 'UnityProject/Assets/Flightbeat/Garden').rglob('*.cs'):
    for label in re.findall(r'"([^"\n]*)"', file.read_text()):
        for char in label:
            if ord(char) >= 128 and char not in atlas['characters']:
                missing_glyphs.append(dict(path=str(file.relative_to(ROOT)), character=char))
passed = checked > 0 and not changed and not missing_glyphs
print(json.dumps(dict(passed=passed, source='d53a2cc5c24de9851ed5ff1462c6c94e1e64b131',
    checked=checked, changed=changed, missingGlyphs=missing_glyphs), ensure_ascii=False, indent=2))
sys.exit(0 if passed else 1)
