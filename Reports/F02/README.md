# F02 — Ordenar inventário com identidade preservada

Data: 15/11/2026. Base: `8d8ddd2` (F01), branch `claude/f02-inventory-sort`. Unity 6000.6.9f1; pacotes e ProjectSettings preservados. Implementado por Claude a pedido do usuário, com o prompt F02 do catálogo como escopo.

## Entrega

Botão **Ordenar** no cabeçalho da bolsa, sem arrastar. A ordem é: tipo (arma → armadura → poção), raridade da mais rara para a mais comum (sem raridade fica abaixo de Comum), nome sem diferenciar maiúsculas. Itens empatados mantêm a ordem relativa anterior e os slots vazios ficam no fim.

- Domínio: `InventoryModel.TrySort(out int[] newIndexOf)` move pilhas inteiras e remapeia `EquippedWeaponIndex`/`EquippedArmorIndex` pela instância do slot. Duas espadas iguais continuam distintas: a equipada permanece equipada. Não funde nem divide pilhas e não altera potência, capacidade ou auto-equip. Publica **um** `Changed`; numa bolsa já ordenada retorna `false` e não publica nada.
- `PlayerInventory.TrySort` valida o comando como os demais (recusa com jogador morto). Dano, defesa e loot não mudam: o equipamento aplicado continua o mesmo.
- `InventoryPresenter.SortSelected` só pede o comando e usa `newIndexOf` para manter a seleção no mesmo item (um slot vazio selecionado segue para sua nova posição vazia). Com o foco do teclado/gamepad num slot, o foco acompanha o item; com o foco no botão, ele fica no botão.
- Builder: **Tools / Dungeon Crawler / Inventory / Integrate Inventory Sort (F02)** (`ConfigureInventorySort`, também chamado por `ConfigureHud`). Encurta o retângulo do título para 580 px (o texto ocupa cerca de 350 px) e cria `Sort` em (632, −26), 188×46, alinhado à 4ª coluna de slots. Comandos, slots e referências existentes são preservados.

## Arquivos afetados

- Domínio: `InventoryModel.cs`, `PlayerInventory.cs`.
- UI e composição: `InventoryPresenter.cs`, `InventorySceneBuilder.cs` (o laço de cenas do F01 foi extraído para `ConfigureSavedHuds`, sem mudança de comportamento). Cenas `MVP_Arena`, `Floor_01` e `Floor_01_GeneratedCharacters` salvas pelo builder.
- Testes: `InventorySortTests.cs` (novo), `InventoryAssetIntegrationTests.cs`, `InventorySceneUiTests.cs`.
- Documentação: contrato em `ARCHITECTURE.md`, estado em `NEXT_STEPS.md`, evidências nesta pasta.

## Validação executada

| Evidência | Resultado e alcance |
| --- | --- |
| [EditMode.xml](EditMode.xml) | 32/32. Novos: ordem tipo/raridade/nome com vazios intercalados; mesmo multiconjunto (definição, quantidade, potência); um evento por ordenação, zero quando já ordenada; duplicatas equipadas (arma e armadura); pilhas 4+3 não fundidas; capacidade e auto-equip intactos; nome sem diferenciar maiúsculas e raridade nula; 500 comandos aleatórios com semente fixa (adicionar, retirar, equipar, ordenar), verificando conservação, permutação e mapa de equipamento a cada ordenação. Builder F02 executado duas vezes: bytes iguais, um único `Sort` ligado a `SortSelected`, 16 slots. Regressões: InventoryModel, EquipmentComparison, Progression e integração geral |
| [PlayMode.xml](PlayMode.xml) | 32 aprovados, 0 falhas, 2 capturas Explicit ignoradas em batchmode. Cena de release: clique real em Ordenar com a partida pausada (`timeScale` 0); ordem esperada; a segunda espada idêntica, que estava equipada, continua equipada no novo índice; dano e defesa inalterados; seleção acompanha a Lâmina; um `Changed`, e o segundo clique não gera evento. Depois de fechar a bolsa: zero `AttackStarted`. Retângulos do botão sem sobreposição com o título, a dica, as instruções, a 4ª coluna e o painel de equipamento. Ordenar com jogador morto é recusado. Suítes de inventário, pausa, menus, combo e progressão sem regressão |
| [Visual.xml](Visual.xml) | 1/1 em Editor com janela e renderização: cena de release em PlayMode, cliques reais, capturas antes/depois em 1280×720 e 1920×1080 |
| `hashes-*.txt` | Antes/depois. Builder idempotente: a 2ª execução sobre cena já integrada gerou bytes idênticos (run1 = run2). Partindo do HEAD, o `Sort` recém-criado recebe um fileID local novo, por isso a execução 3 tem outro hash, com o mesmo conteúdo. As cenas finais não foram alteradas pelo PlayMode |

Capturas conferidas visualmente: [720p antes](Visual/Captures/1280x720-sort-before.png), [720p depois](Visual/Captures/1280x720-sort-after.png), [1080p antes](Visual/Captures/1920x1080-sort-before.png), [1080p depois](Visual/Captures/1920x1080-sort-after.png).

Efeitos colaterais conhecidos, revertidos e fora do commit: o teste existente `IntegrateTwice_…` executa a integração geral e regravou `Player.prefab` com dois campos serializados de `PlayerAim` (`movement`, `faceMouse`), deriva anterior ao F02, vinda de `b8fc9b8`. Abrir o Editor removeu `SENTIS_ANALYTICS_ENABLED` dos defines (mesmo efeito do F01). Os dois arquivos foram restaurados byte a byte (hashes em `hashes-before.txt`). As cenas que esse teste regravou foram restauradas para o HEAD, e o builder F02 foi reaplicado isoladamente.

## Reprodução

Fechar outras instâncias do Editor; usar `DUNGEON_CRAWLER_TEST_REPORT_DIR` e destinos novos.

```text
Builder:  -batchmode -nographics -executeMethod DungeonCrawler.Editor.InventorySceneBuilder.IntegrateInventorySort -quit
EditMode: InventorySortTests|InventoryModelTests|EquipmentComparisonTests|InventoryAssetIntegrationTests|ProgressionTests
PlayMode: PlayerInventoryPlayModeTests|InventorySceneUiTests|InventoryPauseInputTests|GameMenusInputTests|PlayerComboCombatTests|PlayerProgression
Visual (sem -batchmode/-nographics): DungeonCrawler.Tests.PlayerInput.InventorySceneUiTests.Capture_InventorySort720pAnd1080p
```

Depois de rodar `InventoryAssetIntegrationTests`, conferir `git status`: o teste de integração geral regrava o prefab, as cenas e os defines (ver acima).

## Limites e próximo passo

- Validação no Editor; não houve build Windows nem run completa até vitória/derrota.
- A ordem decrescente de raridade (Épico primeiro) e a posição do botão são decisões de UX desta entrega, revisáveis.
- Q bebe a primeira poção na ordem atual: depois de ordenar, entre poções iguais vale a ordem anterior, e não necessariamente a de menor cura.
- Ultrawide e outras escalas de texto não foram avaliados.
- A navegação automática do Unity UI inclui o novo botão; não foram definidos vizinhos explícitos para gamepad.

Próximo passo: revisão e integração da branch. Para distribuir, reconstruir e testar a build conforme `docs/BUILD-WINDOWS.md`.
