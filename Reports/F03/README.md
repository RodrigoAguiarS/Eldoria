# F03 — Política selecionável de poção rápida

Data: 15/11/2026. Base `d0b97fc1d1ed376d4d41ac737986c859fb25118b`, branch `codex/f03-quick-potion`, criada da main atualizada. Unity 6000.6.9f1; versões de pacotes preservadas.

## Comportamento e migração

Em **Opções → Poção rápida (Q)**: **Primeira** é o padrão; **Menor potência** e **Maior potência** comparam a potência armazenada, sem considerar HP faltante, raridade ou nome. Empates usam o menor índice atual. Seleção é uma consulta sem mutação em `InventoryModel.GetPotionIndex`; input e HUD consultam `PlayerInventory.QuickPotionIndex`. Ao esvaziar uma pilha, a próxima consulta escolhe novamente. O total do HUD continua somando todas as poções.

HP cheio, morte e componente desativado recusam consumo; Q funciona com a bolsa pausada. O botão **Usar** continua comandando o índice selecionado. Não foram criados recursos, cooldown, animação de beber ou alterações de progressão/loot.

JSON de opções passa a v3 na mesma chave `DungeonCrawler.Options.v1`. v1/v2 migram para Primeira, preservando os demais ajustes (v1 também conserva a migração anterior de qualidade para Padrão). Em v3, política ausente ou enum desconhecido volta para Primeira. JSON inválido, versão ausente/zero ou futura é rejeitado e usa os defaults; recarregar não sobrescreve o documento rejeitado. Aplicar valida, copia e persiste v3. A preferência é independente da bolsa de uma run nova; não há saves de progressão envolvidos.

## Arquivos e composição

- Domínio/opções: `QuickPotionPolicy`, `InventoryModel`, `PlayerInventory`, `GameOptionsData`, `GameOptionsDefinition`.
- Input/apresentação: `PlayerInventoryInput`, `GameOptionsPresenter`, `PotionHudPresenter`.
- Builders: integração específica `GameShellSceneBuilder.IntegrateQuickPotionPolicy` (menu **Tools / Dungeon Crawler / Inventory / Integrate Quick Potion Policy (F03)**); integração geral de opções/inventário também inclui as novas referências e o seletor.
- Unity salva MainMenu, MVP_Arena, Floor_01, Floor_01_GeneratedCharacters e Player.prefab pelas APIs. Não há reconstrução de salas/arte. O seletor recebe espaço próprio; janela de opções 1040×920 na referência 1920×1080.
- Testes: InventoryModelTests, GameOptionsTests, QuickPotionIntegrationTests, PlayerInventoryPlayModeTests e InventorySceneUiTests. Estes últimos preservam/restauram PlayerPrefs para isolamento.
- Documentos essenciais: GAME_DESIGN, ARCHITECTURE, NEXT_STEPS e este relatório.

## Evidências executadas

| Evidência | Resultado / alcance |
| --- | --- |
| [EditMode.xml](EditMode.xml) | 52 aprovados, zero falhas. Políticas com 15/20/30/40, empate por índice (definições distintas), ausência de poções, pilhas 5+1 e troca após esvaziar, consulta sem eventos; regressões de inventário/ordenação/comparação/progressão. Opções v1/v2/v3, valores ausentes/inválidos/futuros, validação e persistência em nova instância de opções. Builder duas vezes: bytes idênticos nas quatro cenas/prefab/dados, GUIDs preservados, referências explícitas e um seletor por painel. SOs de opções/inventário inalterados |
| [PlayMode-focused.xml](PlayMode-focused.xml) | 16 aprovados, zero falhas. Domínio no jogador, HP cheio/morto em todas as políticas, assinatura única após reativação e remoção ao desativar. Cena real de release: cliques no dropdown/Aplicar/Usar; descartar rascunho; Q na bolsa pausada; HUD antes/depois de consumir; reinício com bolsa vazia e preferência preservada; Q cheio/morto sem consumir |
| [PlayMode-regression.xml](PlayMode-regression.xml) | 25 aprovados, zero falhas, três capturas Explicit ignoradas em batchmode. Inventário/input/menus/pausa/combo e teste anterior de HUD de itens. Junto da execução focada: **39 testes PlayMode distintos aprovados** (dois novos casos aparecem nas duas execuções) |
| [Visual.xml](Visual.xml) | 1 aprovado em Editor com renderização, fora de batchmode. Abriu a cena real de release em PlayMode; cliques reais nas três escolhas/Aplicar; 14 capturas em 1280×720 e 1920×1080, incluindo dropdown expandido e HUD para cada política. Imagens inspecionadas: textos legíveis, seletor sem corte, botões acessíveis e potência +30/+15/+40 correspondente à escolha |
| [unity-check.json](unity-check.json) | 1138 arquivos inspecionados; zero erros/avisos de metas, GUIDs e referências textuais |
| [final-audit.json](final-audit.json) / [asset-delta.json](asset-delta.json) | Zero cópias instaladas divergentes/ausentes e zero links locais ausentes. Hashes antes/depois: somente as quatro cenas e o prefab mudaram entre os assets preexistentes; SOs, metas/GUIDs e ProjectSettings preservados |

Capturas: [opções 720p](Visual/Captures/1280-options-2.png), [dropdown 720p](Visual/Captures/1280-dropdown.png), [HUD Primeira](Visual/Captures/1280-hud-0.png), [HUD Menor](Visual/Captures/1280-hud-1.png), [HUD Maior](Visual/Captures/1280-hud-2.png); [opções 1080p](Visual/Captures/1920-options-2.png), [dropdown 1080p](Visual/Captures/1920-dropdown.png) e [HUD 1080p](Visual/Captures/1920-hud-2.png). HP foi reduzido para tornar a potência visível; pausa mantém o flash do último dano nas capturas. Não representam aceite artístico nem run normal.

Ao salvar o prefab, o Unity também explicitou os campos já existentes de PlayerAim `movement: null` e `faceMouse: false`, iguais aos defaults anteriores; não há alteração de comportamento desses campos. A alteração automática de `SENTIS_ANALYTICS_ENABLED` feita pelo pacote foi revertida para o baseline após fechar o Editor. Espaços finais em `m_Name:` das cenas são saída do serializador Unity; o diff de C#/Markdown não tem erros de whitespace.

Compilação e builder concluídos pelo Editor. A execução ampla inicial de PlayMode (`PlayMode.log`, ignorado pelo Git) deixou de progredir ao incluir a suíte antiga inteira de ItemsCombatSceneTests e foi interrompida; não gerou XML e **não conta como aprovada**. As execuções focada e de regressão acima terminaram normalmente. A causa do bloqueio da suíte ampla não foi determinada; os testes antigos de VFX fora da seleção acima não estão certificados por esta entrega.

## Reprodução

Usar destinos novos e fechar outras instâncias do Editor. Definir `DUNGEON_CRAWLER_TEST_REPORT_DIR` para uma pasta exclusiva.

```text
Builder: -batchmode -nographics -executeMethod DungeonCrawler.Editor.GameShellSceneBuilder.IntegrateQuickPotionPolicy -quit
EditMode: QuickPotionIntegrationTests|InventoryModelTests|InventorySortTests|GameOptionsTests|EquipmentComparisonTests|ProgressionTests
PlayMode focado: PlayerInventoryPlayModeTests|InventorySceneUiTests.QuickPotion_
PlayMode regressão: InventorySceneUiTests|InventoryPauseInputTests|GameMenusInputTests|PlayerComboCombatTests|ItemsCombatSceneTests.ReleaseFloor_InventoryShowsIconsQuantityAndSelectionShape_PotionHudCounts
Visual (sem -batchmode/-nographics): DungeonCrawler.Tests.PlayerInput.InventorySceneUiTests.Capture_QuickPotionOptionsAndHud720pAnd1080p
```

Comandos completos de execução em [BUILD-WINDOWS](../../docs/BUILD-WINDOWS.md). Não usar `-quit` com `-runTests`.

## Limites e próximo passo

Não houve build Windows, teste em executável ou run completa até vitória/derrota. Persistência foi comprovada por PlayerPrefs, Reload e nova instância de opções; não por relançamento de uma build Windows. Ultrawide, outras escalas de texto e hardware adicional não foram avaliados.

Próximo passo: revisar o merge request; merge somente após aprovação do usuário. Para distribuir, reconstruir e testar o executável conforme o roteiro de build.
