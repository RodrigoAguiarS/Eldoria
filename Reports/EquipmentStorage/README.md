# D-007 — Equipamento separado da bolsa e arma inicial

Branch `claude/equipment-storage`, **empilhada** sobre `claude/equipment-six-slots@bb8c704`
(D-006 ainda não mesclada na `main@14ee565`; empilhamento escolhido pelo usuário).
Unity 6000.6.9f1, URP 17.6.0, Input System 1.20.1, UGUI 2.6.0; `Packages/` e `ProjectSettings/`
inalterados. Commit testado: `2eaaeb5` ([tested-commit.txt](tested-commit.txt)).
Regras em [EQUIPMENT-SLOTS](../../docs/EQUIPMENT-SLOTS.md) e
[CHECKPOINTS-F11 § D-007](../../docs/CHECKPOINTS-F11.md). Os relatórios históricos de D-006
([EquipmentSlots](../EquipmentSlots/README.md)) não foram alterados.

## Commits

| Commit | Conteúdo |
| --- | --- |
| `84e8af4` | Domínio (bolsa + seis slots próprios), comandos, `startingWeapon`, checkpoint v5 com migração, presenter/UI, builder e testes |
| `893f3c6` | Integrador aplicado pelo menu (duas vezes, bytes idênticos): arma inicial no SO, nota da bolsa nas quatro cenas; documentos |
| `aeb7256` | Correções de fixtures de teste (achados da primeira execução) |
| `2eaaeb5` | Mantém o foco inicial F10 ao reabrir a bolsa (contrato existente do `MenuScreenStack`) |

## Critérios de aceite

| Critério | Estado | Evidência |
| --- | --- | --- |
| Cada unidade em um só lugar (bolsa, slot ou chão); equipado não ocupa a bolsa | OK | `EquipmentSlotsModelTests` (conservação em 1000 comandos), `UnequippedGear_DropsFromTheBag…` |
| Equipar move da bolsa para o slot e libera o espaço | OK | `EquipEachCategory_MovesTheUnitAndFreesItsBagSlot`, cliques reais em `ReleaseFloor_RealClicks…` |
| Troca atômica: peça anterior no espaço liberado, com bolsa cheia | OK | `Swap_PutsThePreviousItem…`, `FullBag_SwapsWithoutSpace…`, `TextScale150_…` |
| Desequipar vai para espaço livre; bolsa cheia recusa com mensagem e sem alteração | OK | `UnequipWithFullBag_IsRefusedWithoutAnyChange`, `FullBag_…ClearReasonAndNoChange`, aviso "Bolsa cheia" na UI |
| Duas unidades da mesma definição distintas | OK | `TwoUnitsOfTheSameDefinition_StayDistinct`, migração com dois elmos |
| Ordenar só a bolsa | OK | `Sort_ReordersTheBagOnlyAndKeepsEquipment`, `SavedFloor_SortButton…` |
| Seleção distingue bolsa × slot; botões e detalhes após transferências | OK | `ReleaseFloor_RealClicks…`, `SavedFloor_EmptyAndFullBag…` |
| Comparação no mesmo slot, sem efeitos | OK | `Comparison_StaysInSlotAndHasNoSideEffects`, `InventorySceneUiTests` |
| Bônus uma vez, sem acúmulo nem cura; recusas sem eventos | OK | `SixSlots_ComposeDamageAndDefenseOnce…`, `RepeatedSwapsAndUnequips…` |
| Nova run com uma Espada de Ferro Comum equipada, sem cópia na bolsa/chão | OK | `NewRun_StartsWithOneCommonIronSwordEquippedAndNothingElse`, `ShippedInventory_StartsWithTheCommonIronSword` |
| Abrir/fechar, reativar, trocar de andar ou continuar não concedem arma | OK | `NewRun_…`, `StartingWeapon_IsEquippedOnce…`, `StartingWeapon_IsNotGrantedWhenAContinueIsPending` |
| Continuar/andar com arma vazia sem receber outra | OK | `EmptyWeaponSlot_TransferAndContinue_NeverGrantTheStartingWeapon`, `V5_EmptyWeaponSlotStaysEmpty` |
| Checkpoint v5 salva bolsa e equipamento separados | OK | `V5_RoundTrip…`, `SixEquipped_TransferToSecondFloorAndContinue…`, `RunCheckpointPlayModeTests` |
| v1–v4 migram sem duplicar e sem conceder arma; inválidos recusados inteiros | OK | `V4_Migrates…`, `LegacyVersions_…`, `LegacyWithoutBoundWeapon…`, 15 cenários inválidos, `MigrateBindings_*` |
| Coleta nunca equipa (autoequipamento revisto) | OK | `GearCollection_NeverEquips…`, `ItemsCombatSceneTests`, `MvpSceneAcceptanceTests` |
| Integradores reaplicados não desfazem a correção | OK | [hashes-run1](hashes-run1.txt) = [hashes-run2](hashes-run2.txt); `EquipmentSlotsBuilderTests` |
| 720p/1080p × texto 100/125/150% | OK (emulado) | [PlayMode-graphical.xml](PlayMode-graphical.xml) 1/1, [capturas](graphical/) |

## Testes executados

| Artefato | Commit | Resultado |
| --- | --- | --- |
| [EditMode-1.xml](EditMode-1.xml) | `893f3c6` | 340: 335 aprovados, 5 falhas de fixture/expectativa (corrigidas em `aeb7256`) |
| [EditMode-2.xml](EditMode-2.xml) | `aeb7256` | **340/340** (o commit seguinte só altera o presenter e um teste PlayMode) |
| [PlayMode-1.xml](PlayMode-1.xml), [PlayMode-2.xml](PlayMode-2.xml) | `893f3c6`, `aeb7256` | 224 aprovados, 1 falha (foco ao reabrir; ver Achados), 19 `[Explicit]` ignorados |
| [PlayMode-3.xml](PlayMode-3.xml) | `2eaaeb5` | **244: 225 aprovados, 0 falhas**, 19 `[Explicit]` ignorados no batch |
| [PlayMode-graphical.xml](PlayMode-graphical.xml) | `aeb7256` | 1/1 no Editor com janela: 18 capturas da release + andar 2 ([measurements](graphical/measurements.txt)) |

As medições de desempenho geradas pela suíte foram redirecionadas para [artifacts/](artifacts/)
(`DUNGEON_CRAWLER_TEST_REPORT_DIR`) para não sobrescrever relatórios versionados. Efeitos
colaterais conhecidos da suíte (cenas/prefabs/ProjectSettings/relatórios de outras fases) foram
restaurados com `git checkout` após cada execução. Compilação também verificada fora do Editor
com o Roslyn do próprio Unity (todos os assemblies de runtime, Editor e testes, 0 erros).

## Validação visual

Editor com janela, Game view em tamanho fixo (emulado). Conferidas:
[1080p início da run](graphical/1920x1080-100-empty.png) (0/16, Espada de Ferro no slot, dano 26),
[720p 150% bolsa cheia + seis peças](graphical/1280x720-150-full-focus-gloves.png) (16/16 só da
bolsa, nota nova em três linhas sem sobreposição) e
[720p 150% detalhes](graphical/1280x720-150-full-details-scrolled.png) ("Fora da bolsa…").

## Achados

1. **Corrigido (UX/contrato):** ao reabrir a bolsa, o `MenuScreenStack` aplica o foco inicial F10
   (primeiro espaço) e a seleção segue o foco. Mantido o contrato existente; o teste agora o
   verifica e seleciona o slot explicitamente.
2. **Decisão:** largar direto de um slot não é permitido; é preciso desequipar antes (botão
   Largar desativado com slot selecionado). Evita remoção parcial quando não há posição livre.
3. **Efeito de jogo:** começar com a Espada de Ferro eleva o dano inicial de 20 para 26; antes a
   primeira arma coletada produzia esse valor. Sem playtest humano do ritmo da primeira sala.

## Pendências

**NÃO VALIDADO NO EDITOR:** playtest humano; monitores físicos (só Game view emulada); gamepad;
build Windows deste incremento. Arte das quatro peças e skin no `Floor_02_Fixed` seguem as
pendências de D-006/V04.
