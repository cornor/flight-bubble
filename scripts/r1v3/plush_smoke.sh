#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
out="${1:?Fresh evidence output directory required}"
port="${R1_HTTP_PORT:-8804}"
export R1_CDP_PORT="${R1_CDP_PORT:-9344}"
if lsof -tiTCP:"$port" -sTCP:LISTEN >/dev/null || lsof -tiTCP:"$R1_CDP_PORT" -sTCP:LISTEN >/dev/null; then
  echo 'Acceptance ports are occupied'; exit 1
fi
mkdir -p /tmp/flight-bubble
session="$(mktemp -d /tmp/flight-bubble/plush-XXXXXX)"
python3 -m http.server "$port" --bind 127.0.0.1 --directory "$repo/Builds/WebGL/GardenR1-bubble" >"$session/http.log" 2>&1 & HP=$!
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --remote-debugging-port="$R1_CDP_PORT" \
 --user-data-dir="$session/chrome" --no-first-run --no-default-browser-check --use-angle=metal --window-size=540,960 \
 "http://127.0.0.1:$port/" >"$session/chrome.log" 2>&1 & CP=$!
trap 'kill "$CP" "$HP" 2>/dev/null || true' EXIT
sleep 5
R1_URL="http://127.0.0.1:$port/" node "${R1_SMOKE_SCRIPT:-scripts/r1v3/plush_smoke.mjs}" "$out"
