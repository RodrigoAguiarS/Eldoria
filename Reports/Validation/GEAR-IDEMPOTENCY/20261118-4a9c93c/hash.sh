#!/usr/bin/env bash
# Hashes every asset the ITEMS-GEAR / ITEMS-COMBAT integrators may write (prefabs, catalog, materials, icons, models + metas).
cd "$(git rev-parse --show-toplevel)"
{ ls Assets/_Project/Art/Items/Prefabs/*.prefab Assets/_Project/Art/Items/Materials/*.mat Assets/_Project/Art/Items/Icons/*.png \
     Assets/_Project/Art/Items/Models/*.fbx Assets/_Project/ScriptableObjects/Items/ItemVisualCatalog.asset 2>/dev/null; } |
  while read -r f; do echo "$f"; [ -f "$f.meta" ] && echo "$f.meta"; done | sort | xargs sha256sum
