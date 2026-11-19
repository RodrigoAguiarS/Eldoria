#!/bin/bash
# V08: runs Unity tests in a fresh batchmode process.
# Usage: runtests.sh <EditMode|PlayMode> <filter|-> <outdir> [graphics]
P="G:/DungeonCrawler/My project"; U="C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe"
cd "$P"; mkdir -p "$3"
export DUNGEON_CRAWLER_TEST_REPORT_DIR="$P/$3/perf"
G="-nographics"; [ "$4" = "graphics" ] && G=""
F=""; [ "$2" != "-" ] && F="-testFilter $2"
"$U" -batchmode $G -projectPath "$P" -runTests -testPlatform "$1" $F -testResults "$P/$3/$1.xml" -logFile "$P/$3/$1.log"
code=$?
echo "$(date -Iseconds) $1 filter=$2 graphics=${4:-no} exit=$code" | tee -a "$P/Reports/Validation/V08/20261118-a30a30b/test-runs.txt"
