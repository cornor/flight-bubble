#!/usr/bin/env bash
# R1 v3: real-mouse browser run of the fixed build (Builds/WebGL/GardenR1-bubble) through the first four levels,
# screenshots into the fixed Builds/R1-evidence/play-v3 and a side-by-side sheet with the mockup.
# Uses its own port (8799) and an isolated headless Chrome, so a play server on 8785 can stay open.
set -uo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"; cd "$repo"
# usage: bash scripts/r1v3/play.sh [output dir]   (R1_HEIGHT / R1_DPR / R1_CPU_RATE / R1_TOUCH pass through, e.g. the long-screen check)
build="$repo/Builds/WebGL/GardenR1-bubble"; out="${1:-Builds/R1-evidence/play-v3}"
[ -e "$build/index.html" ] || { echo "没有构建：$build"; exit 1; }
rm -rf "$out" "$out-对照.png" /tmp/v3chrome-play
bash scripts/replication.sh link >/dev/null
python3 -m http.server 8799 --bind 127.0.0.1 --directory "$build" >/tmp/v3http-play.log 2>&1 & HP=$!
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --remote-debugging-port=9336 \
  --user-data-dir=/tmp/v3chrome-play --no-first-run --no-default-browser-check --use-angle=metal --window-size=540,960 \
  http://127.0.0.1:8799/ >/tmp/v3chrome-play.log 2>&1 & CP=$!
sleep 6
R1_URL=http://127.0.0.1:8799/ R1_PREFAB_UI=1 node scripts/r1/play_webgl.mjs "$out"; rc=$?
kill $CP $HP 2>/dev/null
bash scripts/replication.sh unlink >/dev/null
python3 scripts/r1v3/compare_shots.py "$out" "$out-对照.png" || true
echo PLAY_EXIT=$rc; exit $rc
