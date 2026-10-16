#!/usr/bin/env bash
# R1 v2: rebuild the atlas from the art-AI delivery, audit logic/data, then build WebGL.
# usage: bash scripts/r1v2/build.sh [/absolute/new/output/dir]
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
if [ "$(git branch --show-current)" != "reskin/r1-v2" ]; then echo 'v2 构建要求 reskin/r1-v2 分支'; exit 1; fi
output="${1:-$repo/Builds/WebGL/GardenR1-art-v2-$(date +%Y%m%d-%H%M%S)}"
if [ -e "$output" ]; then echo "输出目录已存在：$output"; exit 1; fi
mkdir -p "$(dirname "$output")"
python3 scripts/r1v2/build_atlas_v2.py
python3 scripts/r1/check_source.py > "$output.source.json"
'/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity' \
  -batchmode -nographics -quit -projectPath "$repo/UnityProject" -buildTarget WebGL \
  -executeMethod Flightbeat.Editor.WebGlCliBuild.Run --flightbeat-garden-r1 \
  --flightbeat-webgl-build "$output" -logFile "$output.build.log" || {
    echo "Unity 构建失败，日志：$output.build.log"; grep -E "error CS|Error|Exception" "$output.build.log" | head -40; exit 1; }
python3 scripts/r1/install_web_extras.py "$output"
python3 - "$output" <<'PY'
import sys
from pathlib import Path
out = Path(sys.argv[1])
for name in ('index.html', '打开说明.txt'):
    p = out / name
    if p.exists(): p.write_text(p.read_text().replace('第一版美术', '第二版美术'))
PY
echo "构建完成：$output"
echo "试玩：python3 -m http.server 8784 --bind 0.0.0.0 --directory \"$output\"  然后打开 http://127.0.0.1:8784/"
