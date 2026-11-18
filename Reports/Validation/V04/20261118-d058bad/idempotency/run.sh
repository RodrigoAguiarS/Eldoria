#!/bin/bash
# V04: runs one Editor method in a fresh batchmode process and snapshots Assets/ProjectSettings hashes.
# Usage: run.sh <Class.Method> <label>   (env vars are passed through, e.g. DUNGEON_CRAWLER_V04_SNAPSHOT)
P="G:/DungeonCrawler/My project"; O="$P/Reports/Validation/V04/20261118-d058bad/idempotency"
U="C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe"
cd "$P"
snap() { find Assets ProjectSettings -type f ! -path '*/Validation/*' -print0 | sort -z | xargs -0 sha256sum > "$O/$1.sha256"; }
[ -f "$O/h0.sha256" ] || snap h0
"$U" -batchmode -nographics -projectPath "$P" -executeMethod "DungeonCrawler.Editor.$1" -quit -logFile "$O/$2.log"
code=$?
echo "$(date -Iseconds) $1 $2 exit=$code" | tee -a "$O/runs.txt"
snap "$2"
