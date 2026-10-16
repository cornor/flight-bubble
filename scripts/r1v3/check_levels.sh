#!/usr/bin/env bash
# Actual scene, all levels, zero-energy entry routes, and isolated effect captures.
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
out="${1:?Usage: bash scripts/r1v3/check_levels.sh NEW-EVIDENCE-DIRECTORY}"
[[ "$out" = /* ]] || out="$repo/$out"
[ ! -e "$out" ] || { echo "证据目录已存在：$out"; exit 1; }
mkdir -p "$(dirname "$out")"
'/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity' \
 -batchmode -projectPath "$repo/UnityProject" \
 -executeMethod Flightbeat.Editor.BubbleMigrationChecks.Run \
 --idle-test --bubble-checks "$out" -logFile "$out.log"
python3 - "$out/report.json" <<'PY'
import json, sys
r=json.load(open(sys.argv[1]));assert r['passed'],r['error'];assert len(r['levels'])==262
print('262/262 实际进关出战通过；D6 与效果检查通过')
PY
