#!/bin/bash
# Publish a WebGL build to minigame.cornor.cn (nginx static root /www/wwwroot/minigame).
# usage: bash scripts/r1v2/deploy.sh <build dir> [remote folder name, default garden-r1-v2]
# Uses the Mac's existing SSH key for root@118.178.99.37 (BatchMode: never prompts for a password).
# Replaces the remote folder's content; the previous content is moved to <name>.prev on the server.
set -euo pipefail
src="${1:?build dir}"; name="${2:-garden-r1-v2}"
dst=/www/wwwroot/minigame/$name; H=root@118.178.99.37
O=(-o BatchMode=yes -o ConnectTimeout=15 -o LogLevel=ERROR)
ssh "${O[@]}" $H "rm -rf $dst.prev; if [ -e $dst ]; then mv $dst $dst.prev; fi; mkdir -p $dst"
scp "${O[@]}" -r "$src/Build" "$src/index.html" "$src/r1-perf.js" "$src/LICENSE-NotoSansSC.txt" "$H:$dst/"
ssh "${O[@]}" $H "chmod -R a+rX $dst && cd $dst && find . -type f -printf '%s %p\n' | sort -k2"
for p in "" $(cd "$src" && ls Build | sed 's#^#Build/#'); do
  curl -s -o /dev/null -w "%{http_code} %{size_download} /$p\n" "http://minigame.cornor.cn/$name/$p"
done
echo "已发布：http://minigame.cornor.cn/$name/"
