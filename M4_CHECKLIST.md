# M4 — Progressão e acabamento (parte A: progressão e loot)

Status: código escrito e **compilado fora do Unity** (Roslyn 6000.6.9f1: Runtime, Editor e EditMode, 0 erros).
**NÃO VALIDADO NO EDITOR** (nada abriu a cena nem rodou PlayMode/Test Runner).

## Escopo
- **Parte A (esta entrega):** XP, níveis, cura ao subir, +10% de dano, loot de ouro/poção/arma por ScriptableObject, HUD de progressão.
- **Parte B (pendente):** áudio, animações, VFX, acessibilidade, balanceamento final, pooling de efeitos repetidos, medição de desempenho em build Windows.
  O pooling de projéteis já existe desde o M2. Os pickups não usam pool (poucos por sala, expiram em 45 s).

## Regras implementadas
- XP para o nível N→N+1 = `30 × 1,35^(N-1)` (30, 41, 55, 74, ...), nível máximo 10. Valores serializados em `PlayerProgression`.
- Cada nível: **+10% de dano** aditivo (nível 3 = ×1,2) e cura de 40% da vida máxima (por nível ganho).
- Orbe de XP sempre cai: melee 10, ranged 15, elite 40, Boss 120 (campo `xp` do `EnemyReward` no prefab).
- Drops (`ScriptableObjects/Loot/Loot_*.asset`, cada entrada rola de forma independente):
  - Melee: ouro 50% (1–3), poção 10% (cura 15).
  - Ranged: ouro 60% (2–4), poção 15% (cura 20).
  - Elite: ouro 100% (5–10), poção 40% (cura 30), arma 25% (Espada de Ferro, +6 dano).
  - Boss: ouro 100% (25–40), poção 100% (cura 40), arma 100% (Lâmina do Guardião, +12 dano).
- Arma: bônus **plano** somado ao dano base antes do multiplicador; só substitui a atual se for melhor.
- Poção: cura imediata ao coletar (não há botão de usar).

## Arquivos novos
`Scripts/Systems`: `ExperienceModel`, `LootTable` (+`WeaponData`, `LootEntry`, `LootDrop`, `LootRoller`), `PlayerProgression`, `Pickup`.
`Scripts/Enemies/EnemyReward.cs`, `Scripts/UI/ProgressionPresenter.cs`, `Tests/EditMode/ProgressionTests.cs`.
Alterados: `PlayerCombat` (`WeaponDamageBonus`, `CurrentDamage`), `DungeonMvpSceneBuilder` (Player, `AddReward`, HUD), `DungeonFloorSceneBuilder` (assets de loot, `Pickup.prefab`, recompensas).
Referências a LootTable e ao prefab do Pickup ficam **dentro dos prefabs** (referências de cena a assets se perderam nas cenas binárias).

## Preparação
1. Console sem erros de compilação.
2. Pare o Play e rode `Tools/Dungeon Crawler/Build/Floor Scene (M2)`. Novos assets: `Prefabs/Items/Pickup.prefab`, `ScriptableObjects/Loot/*`, `ScriptableObjects/Items/*`.
3. Test Runner → EditMode → `ProgressionTests` (11 testes).

## Checklist manual
- [ ] HUD (canto inferior esquerdo) mostra `Nv 1  XP 0/30  Ouro 0  Arma: —` e uma barra de XP ciano.
- [ ] Ao matar um inimigo cai uma esfera ciano (XP) e, às vezes, amarela (ouro) / rosa (poção) / branca (arma).
- [ ] Encostar nos itens os coleta (raio ~1,2 m); a barra de XP e o texto atualizam.
- [ ] Ao subir de nível: vida sobe, "Nv" aumenta e o dano aumenta (conferir matando um melee: 40 HP → 2 golpes no nv 1, menos golpes com a arma).
- [ ] Poção cura (vida nunca passa do máximo); ouro soma.
- [ ] Arma de elite/Boss aparece no HUD e aumenta o dano; uma arma pior não substitui a melhor.
- [ ] Itens somem sozinhos após ~45 s; reiniciar (R) zera nível/ouro/arma.
- [ ] Sem erros no Console durante o andar inteiro.

## Pontos de atenção
- **Trigger com CharacterController:** o pickup tem Rigidbody cinemático para garantir `OnTriggerEnter`; se não coletar, é o primeiro suspeito.
- **Visual do pickup:** cor via `MaterialPropertyBlock` (`_BaseColor`); se aparecer tudo branco, o shader não lê essa propriedade.
- **Ordem de execução:** o jogador começa com `Awake` aplicando multiplicador 1; verificar que o dano no nível 1 é 20.
- **Balanceamento não testado:** XP total do andar sem o Boss é 165 (nível 4); ajuste `xpGrowth`/`xp` conforme playtest.
- **Sem persistência:** progressão vale só para a run atual (sem save).
- Builds já geram ruído de NRE (`Awake`/`OnEnable` durante `AddComponent`); ver `M3_CHECKLIST.md`.
