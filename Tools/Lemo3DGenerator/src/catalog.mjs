// Catálogo de personagens efetivamente previstos no projeto (documentos + dados/prefabs existentes).
// "documented" = descrição visual no ART_DIRECTION_AND_PROMPTS.md; os demais existem no jogo, mas o visual
// foi derivado da família documentada e está marcado como suposição pendente de aprovação.
export const CATALOG = [
  { module: 'hero', name: 'Hero', gameplay: 'Player.prefab · PlayerMovement/Combat/Dash · HP 100', art: 'Asset 01 (documentado)', documented: true },
  { module: 'skeleton_melee', name: 'SkeletonMelee', gameplay: 'Enemy_Melee.prefab · EnemyData Enemy_Melee (windup 0,4 s, alcance 1,6)', art: 'Asset 02 (documentado)', documented: true },
  { module: 'skeleton_elite', name: 'SkeletonElite', gameplay: 'Enemy_Elite.prefab · Body 1,4× · windup 0,5 s, alcance 2,2', art: 'Asset 02, variante elite: 1,4×, ombreiras extras, olhos laranja (documentado)', documented: true },
  { module: 'skeleton_ranged', name: 'SkeletonRanged', gameplay: 'Enemy_Ranged.prefab · projétil esférico, windup 0,5 s, mantém 5 m', art: 'NÃO descrito no documento — derivado da família esqueleto (suposição)', documented: false },
  { module: 'warden', name: 'Warden', gameplay: 'Boss_Warden.prefab (cápsula 2,2× ≈ 4,4 m, Horn_L/Horn_R) · Slam + Volley · 2 fases', art: 'NÃO descrito no documento — chifres do placeholder + família esqueleto (suposição)', documented: false },
];
