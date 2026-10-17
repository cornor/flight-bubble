#!/bin/bash
# Double-click in Finder: rebuilds the WebGL game and writes the log to Builds/claude-build.log.
cd "$(dirname "$0")/../.." || exit 1
LOG="Builds/claude-build.log"
echo "START $(date '+%F %T')" > "$LOG"
bash scripts/r1v3/build.sh >> "$LOG" 2>&1
echo "EXIT $? $(date '+%F %T')" >> "$LOG"
