#!/usr/bin/env python3
"""Check distinct color identities, registered art, atlas sources and unchanged level data."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / 'Art/PlushToy/UniqueAnimals-20261026'
RES = ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'
ROOTS = [BATCH / 'delivery'] + [ROOT / 'Art/PlushToy' / name / 'delivery'
                              for name in ('P2-P4', 'FirstFive', 'P1')]
STATES = ('idle', 'attack', 'spent', 'ready', 'blink')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    catalog = json.loads((ROOT / 'Art/skin-colors.json').read_text())['colors']
    production = json.loads((BATCH / 'production.json').read_text())
    baseline = json.loads((BATCH / 'preserved-baseline.json').read_text())
    sources = json.loads((ROOT / 'Art/R1/art-v3/generated/atlas-sources.json').read_text())
    atlas = json.loads((RES / 'atlas.json').read_text())
    slots = {slot['id']: slot for slot in atlas['slots']}
    assert len(catalog) == 22
    for key in ('hex', 'creatureId', 'displayName', 'animalId', 'animalName'):
        assert len({row[key] for row in catalog}) == 22, ('duplicate identity', key)
    assert {row['hex'] for row in catalog} == {row['hex'] for row in baseline['originalPalette']}
    for row in baseline['retainedStates']:
        assert sha(ROOT / row['path']) == row['sha256'], ('retained art changed', row['path'])
    for path, digest in baseline['levels'].items():
        assert sha(ROOT / path) == digest, ('gameplay level changed', path)
    code = (ROOT / 'UnityProject/Assets/Flightbeat/Garden/GardenSkin.cs').read_text()
    lookups = (ROOT / 'UnityProject/Assets/Flightbeat/Garden/GardenColorCatalog.Generated.cs').read_text()
    all_debuts = re.findall(r'0x([0-9A-F]{6})', lookups.split('static readonly int[][] debut =')[1])
    assert len(all_debuts) == 22 and len(set(all_debuts)) == 22, 'Each animal must debut exactly once'
    changed = {job['hex']: job for job in production['jobs']}
    rows = []
    for row in catalog:
        hx = row['hex'][1:]
        pose_hashes = []
        for state in STATES:
            rel = Path('creatures') / row['creatureId'] / hx / (state + '.png')
            path = next((root / rel for root in ROOTS if (root / rel).is_file()), None)
            assert path, rel
            sprite = Image.open(path)
            assert sprite.mode == 'RGBA' and sprite.size == (224, 256), path
            bounds = sprite.getchannel('A').point(lambda a: 255 if a > 64 else 0).getbbox()
            assert bounds and 0 < bounds[0] < bounds[2] < 224 and 0 < bounds[1] < bounds[3] < 256, (path, bounds)
            if hx in changed:
                assert path.is_relative_to(BATCH), ('legacy state leaked', path)
                assert abs(bounds[3] - 212) <= 2, ('baseline drift', path, bounds)
            slot_id = f'plants/{row["slotId"]}/{hx}/{state}'
            digest = sha(path)
            assert sources[slot_id] == digest, ('stale atlas', slot_id)
            assert slots[slot_id]['fw'] == 224 and slots[slot_id]['fh'] == 256
            pose_hashes.append(digest)
            rows.append(dict(hex=row['hex'], animal=row['animalName'], state=state,
                             source=str(path.relative_to(ROOT)), sha256=digest))
        assert len(set(pose_hashes)) == 5, ('copied animation states', hx)
        assert f'plants/{row["slotId"]}/{hx}/cheer' not in slots, ('legacy cheer', hx)
        if hx in changed:
            mouth = changed[hx]['mouthCanvas']
            match = re.search(r'case 0x' + hx + r': return new Vector2\(([\d.]+)f, ([\d.]+)f\)', code)
            assert match and list(map(float, match.groups())) == mouth, ('mouth lookup', hx)
            sprite = Image.open(BATCH / 'delivery/creatures' / row['creatureId'] / hx / 'attack.png')
            assert sprite.getpixel(tuple(map(round, mouth)))[3] > 128, ('mouth outside sprite', hx)
    assert (atlas['creatureWidth'], atlas['creatureHeight']) == (4096, 3072)
    report = dict(passed=True, colors=22, animals=22, changedAnimals=14, retainedAnimals=8,
                  states=len(rows), unchangedLevels=len(baseline['levels']), rows=rows)
    (BATCH / 'asset-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('UNIQUE_ANIMALS_PASSED: 22 colors, 22 animals, 110 states; 8 retained animals and level data unchanged')


if __name__ == '__main__':
    main()
