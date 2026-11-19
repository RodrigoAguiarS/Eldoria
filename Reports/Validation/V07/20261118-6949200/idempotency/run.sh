#!/bin/bash
# V07: runs one Editor method in a fresh batchmode process and snapshots Assets/ProjectSettings hashes.
# Usage: run.sh <Class.Method> <label>
P="G:/DungeonCrawler/My project"; O="$P/Reports/Validation/V07/20261118-6949200/idempotency"
U="C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe"
cd "$P"
snap() { find Assets ProjectSettings -type f ! -path '*/Validation/*' -print0 | sort -z | xargs -0 sha256sum > "$O/$1.sha256"; }
[ -f "$O/h0.sha256" ] || snap h0
"$U" -batchmode -nographics -projectPath "$P" -executeMethod "DungeonCrawler.Editor.$1" -quit -logFile "$O/$2.log"
code=$?
echo "$(date -Iseconds) $1 $2 exit=$code" | tee -a "$O/runs.txt"
snap "$2"
