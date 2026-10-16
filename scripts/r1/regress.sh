#!/usr/bin/env bash
# Re-run frozen rules and all existing representative/special Battle replays.
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
run="${1:?Usage: bash scripts/r1/regress.sh UNIQUE-RUN-NAME}"
[[ "$run" =~ ^[a-zA-Z0-9_-]+$ ]] || exit 2
scope="${2:-all}"
[[ "$scope" == all || "$scope" == battle-ui ]] || { echo 'Scope must be all or battle-ui'; exit 2; }
bash scripts/replication.sh link
trap 'bash scripts/replication.sh unlink' EXIT
input="Tools/work/$run-input"
output="Tools/work/$run-model"
python3 - "$input" "$scope" <<'PY'
import json,sys
from pathlib import Path
base=Path(sys.argv[1]);base.mkdir();cases=[]
ui_cases={'training01','training02','sector01','sector02',
 'training01-items-rejected','training02-items-rejected','sector01-items-consumed',
 'sector01-accept-revive','sector01-decline-revive'}
for folder in ['model-campaign-input-v2','model-specials-input-v1','model-indexed-AW-input-v1']:
 p=Path('Tools/work/stages')/folder
 for c in json.loads((p/'campaign.json').read_text())['cases']:
  if folder=='model-indexed-AW-input-v1' and c['name']!='sector02':continue
  if sys.argv[2]=='battle-ui' and c['name'] not in ui_cases:continue
  cases.append(c);(base/c['name']).symlink_to((p/c['name']).absolute())
(base/'campaign.json').write_text(json.dumps({'cases':cases}))
PY
mkdir -p Builds/R1-evidence
'/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity' \
 -batchmode -nographics -quit -projectPath "$repo/UnityProject" -executeMethod Flightbeat.Editor.GardenRegression.Run \
 --flightbeat-legacy-input "$repo/Tools/work/stages/numeric-BR-legacy-input-v2/input.json" \
 --flightbeat-legacy-report "$repo/Tools/work/$run-legacy/report.json" \
 --flightbeat-campaign-input "$repo/$input/campaign.json" --flightbeat-campaign-output "$repo/$output" \
 -logFile "$repo/Builds/R1-evidence/$run.log"
node Tools/precision/check_model_campaign.mjs "$input" "$output"
python3 scripts/r1/check_source.py > "Builds/R1-evidence/$run-source.json"
