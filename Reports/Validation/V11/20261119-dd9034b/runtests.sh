#!/bin/bash
# V11: runs existing Unity tests in a fresh process (no new test code).
# Usage: runtests.sh <EditMode|PlayMode> <filter|-> <outdir> [graphics|editor]
#   (none)   = -batchmode -nographics
#   graphics = -batchmode with GPU
#   editor   = graphical Editor (no -batchmode), required by the [Explicit] captures
P="G:/DungeonCrawler/My project"; U="C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe"
R="Reports/Validation/V11/20261119-dd9034b"
cd "$P"; mkdir -p "$3"
export DUNGEON_CRAWLER_TEST_REPORT_DIR="$P/$3/perf"
M="-batchmode -nographics"; [ "$4" = "graphics" ] && M="-batchmode"; [ "$4" = "editor" ] && M=""
F=""; [ "$2" != "-" ] && F="-testFilter $2"
"$U" $M -projectPath "$P" -runTests -testPlatform "$1" $F -testResults "$P/$3/$1.xml" -logFile "$P/$3/$1.log"
code=$?
echo "$(date -Iseconds) $1 filter=$2 mode=${4:-nographics} exit=$code" | tee -a "$P/$R/test-runs.txt"
