#!/usr/bin/env bash
# R1 v3 (bubble squad): rebuild the atlas (deliveries + placeholders), regenerate the battle UI prefabs,
# audit logic/data, then build WebGL.
# usage: bash scripts/r1v3/build.sh [/absolute/new/output/dir]
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
if [ "$(git branch --show-current)" != "reskin/r1-v3-bubble" ]; then echo 'v3 构建要求 reskin/r1-v3-bubble 分支'; exit 1; fi
# Fixed output directory (user request 2026-10-15): every build replaces Builds/WebGL/GardenR1-bubble.
output="${1:-$repo/Builds/WebGL/GardenR1-bubble}"
case "$output" in "$repo"/Builds/WebGL/*) rm -rf "$output" "$output".*.log "$output".*.json ;; *) [ -e "$output" ] && { echo "输出目录已存在：$output"; exit 1; } ;; esac
mkdir -p "$(dirname "$output")"
unity='/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity'
python3 scripts/r1v3/build_atlas_v3.py | tee "$output.atlas.json"
"$unity" -batchmode -nographics -quit -projectPath "$repo/UnityProject" \
  -executeMethod Flightbeat.Editor.GardenBattleUiAssets.RecreateForV3 -logFile "$output.prefabs.log" || {
    echo "界面 Prefab 重建失败，日志：$output.prefabs.log"; grep -E "error CS|Error|Exception" "$output.prefabs.log" | head -40; exit 1; }
grep -q GARDEN_UI_PREFABS_CREATED "$output.prefabs.log" || { echo '界面 Prefab 未生成'; exit 1; }
python3 scripts/r1v3/build_atlas_v3.py > /dev/null      # prefab copy changed: make sure every glyph is in the atlas
python3 scripts/r1/check_source.py > "$output.source.json"
"$unity" -batchmode -nographics -quit -projectPath "$repo/UnityProject" -buildTarget WebGL \
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
    if p.exists(): p.write_text(p.read_text().replace('第一版美术', '泡泡版').replace('第二版美术', '泡泡版'))
# The fixed directory keeps the same Build/ file names: add a per-build query so browsers fetch the new build.
import time, re
p = out / 'index.html'
if p.exists():
    stamp = time.strftime('%Y%m%d%H%M%S')
    p.write_text(re.sub(r"(Build/[^'\"?]+\.(?:unityweb|js))(\?v=\d+)?", lambda m: m[1] + '?v=' + stamp, p.read_text()))
PY
echo "构建完成：$output"
echo "试玩：python3 -m http.server 8785 --bind 127.0.0.1 --directory \"$output\"  然后打开 http://127.0.0.1:8785/（服务可以一直开着，重新构建后刷新即可）"
