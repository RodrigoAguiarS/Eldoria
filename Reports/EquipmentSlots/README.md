# D-006 — Seis slots de equipamento

Base `main` @ `14ee565` (= `origin/main`, árvore limpa), branch `claude/equipment-six-slots`.
Unity 6000.6.9f1, URP 17.6.0, Input System 1.20.1, UGUI 2.6.0; `Packages/` e
`ProjectSettings/` inalterados. Commit testado: `b08837b` ([tested-commit.txt](tested-commit.txt)).
Regras, valores e checkpoint em [EQUIPMENT-SLOTS](../../docs/EQUIPMENT-SLOTS.md) e
[CHECKPOINTS-F11 § D-006](../../docs/CHECKPOINTS-F11.md).

## Commits

| Commit | Conteúdo |
| --- | --- |
| `deaaefe` | Domínio (6 vínculos, `GearData`, totais, comparação), checkpoint v4, UI em três áreas, builder idempotente, 4 SOs de playtest, cenas, testes e docs |
| `94304c6` | Testes F01/showcase alinhados ao novo cabeçalho de detalhes e às peças do cenário diagnóstico; ordem do catálogo F11 preservada |
| `a6ad622` | `TearDown` dos testes de cena limpa só as cenas do fixture (ver Achados) |
| `b08837b` | Cabeçalho EQUIPAMENTOS com 38 px para caber a 150% (achado da captura gráfica); cenas reintegradas pelo menu |

## Critérios de aceite

| Critério | Estado | Evidência |
| --- | --- | --- |
| Seis categorias, cada uma com slot próprio, todas ativas ao mesmo tempo | OK | `EquipmentSlotsModelTests`, `EquipmentSlotsPlayModeTests.SixSlots_*` |
| Grade 3×2 Capacete \| Arma \| Acessório / Armadura \| Luvas \| Botas; bolsa à esquerda | OK | Cenas; [capturas](graphical/) |
| ATRIBUTOS do domínio (plano, nível, talentos, combo/giro), sem fórmula na UI | OK | Presenter lê `PlayerCombat`/`PlayerInventory`; capturas |
| DETALHES com comparação só no mesmo slot (atual → após, diferença), sem efeitos colaterais | OK | `Comparison_StaysInSlotAndHasNoSideEffects`, `EquipmentComparisonTests` |
| Clicar slot ocupado seleciona o item e nunca desequipa; Desequipar explícito | OK | `ReleaseFloor_RealClicksEquipSix…` (cliques reais) |
| Arma básica quando não há arma | OK | Slot “Espada básica”; [1920x1080-100-empty](graphical/1920x1080-100-empty.png) |
| Equipar/trocar/desequipar sem espaço extra, com bolsa cheia; item trocado fica no lugar | OK | `FullBag_*`, `TextScale150_*` |
| Ordenar remapeia os seis vínculos e mantém seleção; largar remove vínculo e bônus | OK | `TrySort` com duplicatas, `DroppingEquippedGear_*`, `SavedFloor_SortButton…` |
| Bônus determinísticos, aplicados uma vez; equipar não cura | OK | 25 trocas/refresh sem acúmulo; fuzz de 1000 comandos |
| Checkpoint v4; v1/v2/v3 legíveis; campo ausente nunca vira índice 0; validação antes de mutar | OK | `EquipmentCheckpointTests` (8 cenários inválidos), `RunCheckpointTests` |
| Transferência de andar e continuar preservam os seis vínculos | OK | `SixEquipped_TransferToSecondFloorAndContinue…`, `ArrivalFromLegacySource_*` |
| Builder idempotente (`Tools/Dungeon Crawler/Inventory/Integrate Equipment Slots`) | OK | `EquipmentSlotsBuilderTests` (bytes e GUIDs); [hashes-run1](hashes-run1.txt) = [hashes-run2](hashes-run2.txt) em `deaaefe` |
| 1280×720 e 1920×1080 com texto 100/125/150%, nomes longos, bolsa cheia, vazios, foco | OK (emulado) | [PlayMode-graphical.xml](PlayMode-graphical.xml) 1/1, [measurements.txt](graphical/measurements.txt) |
| Tabelas de loot da release inalteradas; peças no cenário diagnóstico | OK | `ItemsCombatSceneTests.ReleaseFloor_ItemShowcase…` |

## Testes executados

| Artefato | Resultado |
| --- | --- |
| [EditMode-final.xml](EditMode-final.xml) | **317/317** em `b08837b` |
| [PlayMode-final.xml](PlayMode-final.xml) | 240: **220 aprovados**, 19 `[Explicit]` ignorados no batch, 1 falha ambiental (abaixo) |
| [PlayMode-final-rewardfeedback.xml](PlayMode-final-rewardfeedback.xml) | 1/1: o teste da falha acima, com `DUNGEON_CRAWLER_TEST_REPORT_DIR` limpo; 0 alocações nos callbacks |
| [PlayMode-graphical.xml](PlayMode-graphical.xml) | 1/1 no Editor com janela: 18 capturas da release + 1 do `Floor_02_Fixed` |
| [PlayMode-retest-a/c/d.xml](PlayMode-retest-d.xml) | Retestes isolados: 2/2, 5/5 e 4/4 (+1 captura ignorada) |
| [EditMode-2/3.xml](EditMode-3.xml), [PlayMode-1.xml](PlayMode-1.xml) | Execuções intermediárias em `deaaefe` (as duas falhas do PlayMode-1 foram corrigidas em `94304c6`) |

A falha ambiental: `RewardFeedbackPlayModeTests.WarmCallbacksAndEffects_*` se recusa a
sobrescrever `Reports/F05/Benchmarks/reward-feedback.json` (versionado) quando
`DUNGEON_CRAWLER_TEST_REPORT_DIR` não está definida. Não é regressão desta tarefa.
[artifacts/](artifacts/) guarda as medições redirecionadas na primeira execução.

Efeito colateral conhecido (V02): as suítes reescrevem cenas, prefabs, `ProjectSettings` e
relatórios de outras fases; após cada execução eles foram restaurados com `git checkout`.

## Validação visual

Editor com janela, Game view em tamanho fixo (emulado, não monitor físico). Conferidas:
[720p 150% bolsa cheia](graphical/1280x720-150-full-focus-gloves.png),
[720p 150% comparação](graphical/1280x720-150-full-details-scrolled.png),
[1080p vazia](graphical/1920x1080-100-empty.png) e
[andar 2](graphical/1920x1080-100-floor02-full.png). Nome longo truncado no slot e completo
nos detalhes; foco nas Luvas visível; comparação “COMPARAÇÃO · Luvas”; nenhum alvo
sobreposto (asserção no teste).

## Achados

1. **Corrigido — cabeçalho a 150%:** em 1280×720 o título EQUIPAMENTOS precisava de 37,5 px
   num retângulo de 34 px. Agora 38 px (`b08837b`).
2. **Corrigido — execução filtrada travava:** filtrar `EquipmentSlotsSceneTests` incluía a
   captura `[Explicit]`, ignorada no batch antes de carregar cena; o `TearDown` destruía as
   raízes da cena ativa (a do runner) e o PlayMode nunca terminava. Reprodução no estado
   antigo: `-runTests -testPlatform PlayMode -testFilter DungeonCrawler.Tests.PlayerInput.EquipmentSlotsSceneTests`.
3. **Fora do escopo — mesmo padrão em 17 classes antigas** (por exemplo `InventorySceneUiTests`,
   `TextScaleInputTests`, `UiSkinSceneTests`, `SecondFloorPlayModeTests`, `ItemsCombatSceneTests`):
   filtrar a classe inteira em batch trava pelo mesmo motivo. A suíte completa e o filtro
   por nome de teste funcionam.

## Pendências

- Ícones `ICON-ITEM-*`, modelos de chão e peças vestíveis das quatro peças novas (fallback
  tingido em `LootKind.Gear`). Ver [EQUIPMENT-SLOTS § Arte pendente](../../docs/EQUIPMENT-SLOTS.md).
- Inclusão das peças em tabelas de loot da release: tarefa própria, com chances justificadas.
- `Floor_02_Fixed` sem UI-SKIN (pendência V04 já registrada).
- Valores das peças são proposta de playtest.

**NÃO VALIDADO NO EDITOR:** playtest humano dos valores; monitores físicos 1280×720/1920×1080
(só Game view emulada); gamepad; build Windows deste incremento.
