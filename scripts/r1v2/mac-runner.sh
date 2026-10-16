#!/usr/bin/env bash
# Runs job files dropped into Builds/mac-jobs/inbox on the Mac (Unity build / regression),
# so the linked session can start them without typing into Terminal.
# Each job: inbox/<name>.job (bash script) -> log <name>.log, exit code <name>.exit.
# Stops when Builds/mac-jobs/stop exists, or after 8 hours.
repo="$(cd "$(dirname "$0")/../.." && pwd)"
jobs="$repo/Builds/mac-jobs"; mkdir -p "$jobs/inbox" "$jobs/done"
rm -f "$jobs/stop"; echo $$ > "$jobs/runner.pid"; date > "$jobs/runner.started"
end=$(( $(date +%s) + 8*3600 ))
while [ ! -e "$jobs/stop" ] && [ "$(date +%s)" -lt "$end" ]; do
  for f in "$jobs"/inbox/*.job; do
    [ -e "$f" ] || continue
    name="$(basename "$f" .job)"; mv "$f" "$jobs/done/$name.job"
    echo "running" > "$jobs/$name.status"
    ( cd "$repo" && bash "$jobs/done/$name.job" ) > "$jobs/$name.log" 2>&1
    echo $? > "$jobs/$name.exit"; echo "finished" > "$jobs/$name.status"
  done
  date +%s > "$jobs/runner.heartbeat"; sleep 3
done
rm -f "$jobs/runner.pid"
