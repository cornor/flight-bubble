#!/usr/bin/env bash
# 复刻阶段资料的接回／撤除工具。
# 资料实际存放在仓库旁的 ../Flightbeat-archive/replication/（与原路径一一对应）。
#
#   scripts/replication.sh status   查看各项当前状态
#   scripts/replication.sh link     以链接方式把资料临时接回原路径（跑回归检查、复刻工具前用）
#   scripts/replication.sh unlink   撤除这些链接（只删链接，不动归档里的文件）
#   scripts/replication.sh freeze   把归档里的文件设为只读（Tools/work 除外），在本机终端运行
#
# 归档放在别处时，设置 FLIGHTBEAT_REPLICATION_ROOT 指向 replication 目录。
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
ROOT="${FLIGHTBEAT_REPLICATION_ROOT:-}"
ITEMS=(
  Evidence
  Reverse
  Analysis
  Build
  index.html
  download-manifest.json
  下载说明.txt
  Tools
  Builds/Native
  Builds/WebGL/Probe-CF-v1
  Builds/WebGL/Probe-CF-v1.summary.json
  Builds/WebGL/Probe-CF-v2
  Builds/WebGL/Probe-CF-v2.summary.json
  ui.md
  当前进度一览.md
  教学试玩说明.md
  README-repo.md
)

target_for() {  # 链接目标：默认用相对路径，换电脑时只要两个目录仍然相邻就有效
  local item="$1"
  if [ -n "$ROOT" ]; then
    printf '%s/%s' "$ROOT" "$item"
  else
    local slashes="${item//[^\/]/}" up="../" i
    for ((i = 0; i < ${#slashes}; i++)); do up="../$up"; done
    printf '%sFlightbeat-archive/replication/%s' "$up" "$item"
  fi
}

archive_path() {
  if [ -n "$ROOT" ]; then printf '%s/%s' "$ROOT" "$1"
  else printf '%s/../Flightbeat-archive/replication/%s' "$REPO" "$1"; fi
}

cmd="${1:-status}"
cd "$REPO"
case "$cmd" in
  status)
    for item in "${ITEMS[@]}"; do
      if [ -L "$item" ]; then s="已接回（链接）"
      elif [ -e "$item" ]; then s="项目内有实体文件（未迁出）"
      else s="不在项目内"; fi
      if [ -e "$(archive_path "$item")" ]; then a="归档存在"; else a="归档缺失"; fi
      printf '%-40s %s / %s\n' "$item" "$s" "$a"
    done ;;
  link)
    for item in "${ITEMS[@]}"; do
      if [ -L "$item" ]; then continue; fi
      if [ -e "$item" ]; then echo "跳过 ${item}：项目内已有同名实体文件"; continue; fi
      if [ ! -e "$(archive_path "$item")" ]; then echo "缺失 ${item}：归档里没有，请检查归档位置"; continue; fi
      mkdir -p "$(dirname "$item")"
      ln -s "$(target_for "$item")" "$item"
      echo "已接回 $item"
    done ;;
  unlink)
    for item in "${ITEMS[@]}"; do
      if [ -L "$item" ]; then rm "$item"; echo "已撤除 $item"; fi
    done ;;
  freeze)
    base="$(archive_path "")"
    find "$base" -type f ! -path "*/Tools/work/*" -perm -u+w -exec chmod a-w {} +
    echo "已把 $base 下的文件设为只读（Tools/work 除外）" ;;
  *)
    echo "用法：$0 {status|link|unlink|freeze}"; exit 2 ;;
esac
