#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
output="${1:-$repo/Builds/WebGL/GardenR1-graybox}"
if [ "$(git branch --show-current)" != "reskin/r1" ]; then echo 'R1 构建要求 reskin/r1 分支'; exit 1; fi
mkdir -p "$(dirname "$output")"
'/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity' \
  -batchmode -nographics -quit -projectPath "$repo/UnityProject" -buildTarget WebGL \
  -executeMethod Flightbeat.Editor.WebGlCliBuild.Run --flightbeat-garden-r1 \
  --flightbeat-webgl-build "$output" -logFile "$output.build.log"
python3 scripts/r1/install_web_extras.py "$output"
