#!/usr/bin/env bash
# Run all 39 frozen cases using only this project's local copies.
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
run="${1:?Usage: bash scripts/r1/regress.sh UNIQUE-RUN-NAME [all|battle-ui]}"
[[ "$run" =~ ^[a-zA-Z0-9_-]+$ ]] || exit 2
scope="${2:-all}"
[[ "$scope" == all || "$scope" == battle-ui ]] || exit 2
input="tools/work/$run-input"
output="tools/work/$run-model"
python3 - "$input" "$scope" <<'PY'
import json, shutil, sys
from pathlib import Path
base = Path(sys.argv[1]); base.mkdir(); cases = []
ui_cases = {'training01', 'training02', 'sector01', 'sector02',
    'training01-items-rejected', 'training02-items-rejected', 'sector01-items-consumed',
    'sector01-accept-revive', 'sector01-decline-revive'}
for folder in ['model-campaign-input-v2', 'model-specials-input-v1', 'model-indexed-AW-input-v1']:
    source = Path('tools/work/stages') / folder
    for case in json.loads((source / 'campaign.json').read_text())['cases']:
        name = case['name']
        if folder == 'model-indexed-AW-input-v1' and name != 'sector02': continue
        if sys.argv[2] == 'battle-ui' and name not in ui_cases: continue
        target = base / name; target.mkdir()
        # Frozen files remain byte-identical. Only this run's steps path is relocated.
        data = json.loads((source / name / 'input.json').read_text())
        data['steps'] = str((source / name / Path(data['steps']).name).resolve())
        (target / 'input.json').write_text(json.dumps(data))
        shutil.copyfile(source / name / 'source.json', target / 'source.json')
        cases.append(dict(case, input=str((target / 'input.json').resolve())))
(base / 'campaign.json').write_text(json.dumps({'cases': cases}))
assert len(cases) == (39 if sys.argv[2] == 'all' else 9), len(cases)
PY
mkdir -p Builds/R1-evidence
zstd_bin="$(command -v zstd)"
python3 scripts/r1/check_source.py > "Builds/R1-evidence/$run-source.json"
'/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity' \
 -batchmode -nographics -quit -projectPath "$repo/UnityProject" -executeMethod Flightbeat.Editor.GardenRegression.Run \
 --flightbeat-legacy-input "$repo/tools/work/stages/numeric-BR-legacy-input-v2/input.json" \
 --flightbeat-legacy-report "$repo/tools/work/$run-legacy/report.json" \
 --flightbeat-campaign-input "$repo/$input/campaign.json" --flightbeat-campaign-output "$repo/$output" \
 --flightbeat-campaign-zstd "$zstd_bin" \
 -logFile "$repo/Builds/R1-evidence/$run.log"
node tools/precision/check_model_campaign.mjs "$input" "$output"
