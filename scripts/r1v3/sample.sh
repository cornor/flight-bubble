#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
build="${R1_BUILD:-$repo/Builds/WebGL/GardenR1-bubble}"
out="${1:?Fresh output directory required}"
[ ! -e "$out" ] || { echo "证据目录已存在：$out"; exit 1; }
mkdir -p /tmp/flight-bubble
session="$(mktemp -d /tmp/flight-bubble/sample-XXXXXX)"
port="${R1_HTTP_PORT:-8802}"
export R1_CDP_PORT="${R1_CDP_PORT:-9342}"
if lsof -tiTCP:"$port" -sTCP:LISTEN >/dev/null || lsof -tiTCP:"$R1_CDP_PORT" -sTCP:LISTEN >/dev/null; then
 echo '试玩端口已占用';exit 1
fi
python3 -m http.server "$port" --bind 127.0.0.1 --directory "$build" >"$session/http.log" 2>&1 & HP=$!
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --remote-debugging-port="$R1_CDP_PORT" --user-data-dir="$session/chrome" --no-first-run --no-default-browser-check --use-angle=metal --window-size=540,960 "http://127.0.0.1:$port/" >"$session/chrome.log" 2>&1 & CP=$!
trap 'kill "$CP" "$HP" 2>/dev/null || true' EXIT
sleep 6
R1_URL="http://127.0.0.1:$port/" node scripts/r1/play_levels.mjs "$out"
