#!/bin/bash
# Runs each integrator twice in separate Unity processes, hashing Assets/ProjectSettings between runs.
P="G:/DungeonCrawler/My project"; O="$P/Reports/Validation/V02/20261118-2e78c61/idempotency"
U="C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe"
cd "$P"
snap() { find Assets ProjectSettings -type f ! -path '*/Validation/*' -print0 | sort -z | xargs -0 sha256sum > "$O/$1.sha256"; }
snap h0-clean
for m in ProductionVisualIntegrator.IntegrateGame TextScaleSceneBuilder.Integrate InventorySceneBuilder.Integrate; do
  for n in 1 2; do
    "$U" -batchmode -nographics -projectPath "$P" -executeMethod DungeonCrawler.Editor.$m -quit -logFile "$O/$m-run$n.log"
    echo "$m run$n exit=$?" | tee -a "$O/runs.txt"
    snap "$m-run$n"
  done
done
