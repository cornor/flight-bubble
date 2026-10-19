#!/usr/bin/env bash
# B level set acceptance in the built WebGL page (plan step 3). Needs a build that contains the levels:
#   released levels: python3 scripts/levels_b/publish.py && bash scripts/r1v3/build.sh
#   test channel:    python3 scripts/levels_b/stage_test.py --temp 3 7 && R1_LEVELS_TEST=1 bash scripts/r1v3/build.sh
# usage: bash scripts/levels_b/check_web.sh [NEW-EVIDENCE-DIR]
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
build="$repo/Builds/WebGL/GardenR1-bubble"
out="${1:-Builds/R1-evidence/levels-b-web-$(date +%Y%m%d-%H%M%S)}"
[ -e "$build/index.html" ] || { echo "没有构建：$build"; exit 1; }
[ ! -e "$out" ] || { echo "证据目录已存在：$out"; exit 1; }
mkdir -p /tmp/flight-bubble
session="$(mktemp -d /tmp/flight-bubble/levels-b-XXXXXX)"
port="${R1_HTTP_PORT:-8802}"
export R1_CDP_PORT="${R1_CDP_PORT:-9342}"
if lsof -tiTCP:"$port" -sTCP:LISTEN >/dev/null || lsof -tiTCP:"$R1_CDP_PORT" -sTCP:LISTEN >/dev/null; then
  echo "端口已占用：$port / $R1_CDP_PORT"; exit 1
fi
python3 -m http.server "$port" --bind 127.0.0.1 --directory "$build" >"$session/http.log" 2>&1 & HP=$!
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --remote-debugging-port="$R1_CDP_PORT" \
  --user-data-dir="$session/chrome" --no-first-run --no-default-browser-check --use-angle=metal --window-size=540,960 \
  "http://127.0.0.1:$port/" >"$session/chrome.log" 2>&1 & CP=$!
trap 'kill "$CP" "$HP" 2>/dev/null || true' EXIT
sleep 6
R1_URL="http://127.0.0.1:$port/" node scripts/levels_b/check_web.mjs "$out"
