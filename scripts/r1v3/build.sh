#!/usr/bin/env bash
# R1 v3 (bubble squad): rebuild the atlas (deliveries + placeholders), regenerate the battle UI prefabs,
# audit logic/data, then build WebGL.
# usage: bash scripts/r1v3/build.sh [/absolute/new/output/dir]
#   CLEAN=1 bash scripts/r1v3/build.sh   full rebuild (clears Unity's IL2CPP cache, rebuilds the atlas)
# Build speed (2026-10-19): the atlas is skipped when its inputs did not change (Builds/.atlas-cache.json), and the
# WebGL build is incremental unless CLEAN=1. Each step prints how long it took.
set -euo pipefail
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo"
# Fixed output directory (user request 2026-10-15): every build replaces Builds/WebGL/GardenR1-bubble.
output="$repo/Builds/WebGL/GardenR1-bubble"
if [ "$#" -gt 0 ] && [ "$1" != "$output" ]; then echo "输出固定为 $output"; exit 2; fi
case "$output" in "$repo"/Builds/WebGL/*) rm -rf "$output" "$output".*.log "$output".*.json ;; *) [ -e "$output" ] && { echo "输出目录已存在：$output"; exit 1; } ;; esac
mkdir -p "$(dirname "$output")"
unity='/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity'
clean_flag=()
if [ "${CLEAN:-0}" = 1 ]; then clean_flag=(--flightbeat-clean-build); export R1_ATLAS_FORCE=1; fi
step_start=$SECONDS; build_start=$SECONDS
step() { echo "[$(( SECONDS - step_start ))s] $1"; step_start=$SECONDS; }
python3 scripts/r1v3/build_atlas_v3.py | tee "$output.atlas.json"
step "图集"
"$unity" -batchmode -nographics -quit -projectPath "$repo/UnityProject" \
  -executeMethod Flightbeat.Editor.GardenBattleUiAssets.RecreateForV3 -logFile "$output.prefabs.log" || {
    echo "界面 Prefab 重建失败，日志：$output.prefabs.log"; grep -E "error CS|Error|Exception" "$output.prefabs.log" | head -40; exit 1; }
grep -q GARDEN_UI_PREFABS_CREATED "$output.prefabs.log" || { echo '界面 Prefab 未生成'; exit 1; }
step "界面 Prefab"
python3 scripts/r1v3/build_atlas_v3.py > /dev/null      # prefab copy changed: make sure every glyph is in the atlas
python3 scripts/r1v3/coverage.py > "$output.coverage.json"
python3 scripts/r1/check_source.py > "$output.source.json"
step "图集补字、覆盖与源码检查"
"$unity" -batchmode -nographics -quit -projectPath "$repo/UnityProject" -buildTarget WebGL \
  -executeMethod Flightbeat.Editor.WebGlCliBuild.Run --flightbeat-garden-r1 \
  --flightbeat-webgl-build "$output" ${clean_flag[@]+"${clean_flag[@]}"} -logFile "$output.build.log" || {
    echo "Unity 构建失败，日志：$output.build.log"; grep -E "error CS|Error|Exception" "$output.build.log" | head -40; exit 1; }
step "Unity WebGL 构建"
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
echo "构建完成：${output}（共 $(( SECONDS - build_start )) 秒）"
echo "试玩：python3 -m http.server 8792 --bind 127.0.0.1 --directory \"$output\"  然后打开 http://127.0.0.1:8792/（服务可以一直开着，重新构建后刷新即可）"
