# F01 — Comparação de equipamentos

Data: 15/11/2026 (America/Fortaleza). Base: `702a6ed`, branch `codex/f01-equipment-comparison`. Esse commit preservou o estado local anterior, conforme autorização do usuário. Unity 6000.6.9f1; pacotes existentes preservados.

## Entrega

A bolsa mostra atual → selecionado e diferença. Armas separam bônus plano e dano base no nível atual, antes dos multiplicadores do combo/especial. Exemplo observado: +6 → +12 (+6), nível 3: 31,2 → 38,4 (+7,2). Armadura compara defesa total, incluindo o baseline configurado. Sem arma/armadura usa o baseline; poção e slot vazio limpam/ocultam a comparação.

`PlayerInventory.TryGetEquipmentComparison` retorna um snapshot sem trocar equipamento, alterar slots ou publicar eventos. `PlayerProgression.GetBaseDamage` reutiliza o cálculo do `PlayerCombat`; a composição de defesa é compartilhada entre consulta e equipamento. UI apenas formata valores. Raridades continuam sem multiplicador oculto.

Builder específico: **Tools / Dungeon Crawler / Inventory / Integrate Equipment Comparison (F01)**. Configura a referência serializada e reposiciona a instrução existente, pelas APIs do Editor. Foi executado duas vezes; bytes das três cenas e GUIDs permaneceram iguais entre as execuções. A integração geral do inventário também chama essa configuração. Os 16 slots, seus índices, itens, ícones e comandos foram preservados.

## Arquivos afetados

- Domínio: `EquipmentComparison.cs` (novo), `PlayerInventory.cs`, `PlayerProgression.cs`, `PlayerCombat.cs`.
- UI e composição: `InventoryPresenter.cs`, `InventorySceneBuilder.cs`; cenas `MVP_Arena`, `Floor_01` e `Floor_01_GeneratedCharacters` salvas pelo builder.
- Testes: `EquipmentComparisonTests.cs` (novo), `PlayerInventoryPlayModeTests.cs`, `InventorySceneUiTests.cs`; metas novas geradas pela Unity.
- Documentação: contrato em `ARCHITECTURE.md`, estado em `NEXT_STEPS.md` e evidências nesta pasta.

## Validação executada

| Evidência | Resultado e alcance |
| --- | --- |
| [EditMode.xml](EditMode.xml) | 24/24: comparação com base de combate customizada, builder idempotente/referências/GUIDs/16 slots, InventoryModel e Progression |
| [PlayMode-retest.xml](PlayMode-retest.xml) | 26 aprovados, zero falhas, 1 captura ignorada em batchmode. Comparação, seleção com clique real, equipar/desequipar, nível, bônus +6/+12, defesa com baseline 7, valores negativos/zero, vazio/cheio, morte, poção, descarte, Q, pausa e combo |
| [Visual-retest.xml](Visual-retest.xml) | 1/1 em Editor com renderização: cena real de release em PlayMode, cliques reais e capturas 1280×720 / 1920×1080; dimensões e ausência de quebra/corte da comparação verificadas também por asserções |
| Checker da skill Unity | 0 erros / 0 avisos após importação das metas; compilação das assemblies passou nas execuções do Editor |
| [editor-assets.json](editor-assets.json) | 207 assets carregados/inspecionados pelas APIs do Editor; zero referências/tipos não resolvidos; FoundationValidator aprovado |
| [preservation.json](preservation.json) e [final-inventory.json](final-inventory.json) | Hashes antes/depois, cópias instaladas e caminhos de links conferidos; dados, prefabs e GUIDs existentes preservados |

O teste de UI confirma zero `AttackStarted` durante seleção, equipamento, consumo e descarte. Consulta após morte permanece disponível, e comandos continuam recusados. Reabrir a bolsa mantém o equipamento e segue o foco inicial já definido pelo menu; não foi introduzida persistência nova de seleção.

O pacote de inferência alterou transitoriamente `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor; o arquivo `ProjectSettings.asset` terminou idêntico ao baseline. Não houve mudança final de configuração/pacotes. A inspeção de assets não salvou cenas ou prefabs.

Capturas conferidas visualmente, produzidas pela cena aberta em PlayMode:

- 720p: [arma](VisualRetest/Captures/1280x720-weapon.png), [armadura](VisualRetest/Captures/1280x720-armor.png), [poção](VisualRetest/Captures/1280x720-potion.png).
- 1080p: [arma](VisualRetest/Captures/1920x1080-weapon.png), [armadura](VisualRetest/Captures/1920x1080-armor.png), [poção](VisualRetest/Captures/1920x1080-potion.png).

As tentativas anteriores foram preservadas: `PlayMode.xml` registra captura executada sem dispositivo gráfico e uma expectativa incorreta de foco na fixture; `Visual.xml` registra erro na remoção da resolução temporária de teste após as três capturas de 720p. A fixture foi corrigida para exigir Editor com renderização, seguir o foco existente e remover somente suas entradas temporárias com o índice absoluto exigido pela API. Retestes acima aprovados.

## Reprodução

Fechar outras instâncias do Editor. Usar a versão fixada, `-projectPath`, diretório de relatórios novo e `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado. Filtros executados:

```text
EditMode: EquipmentComparisonTests|InventoryModelTests|ProgressionTests
PlayMode: PlayerInventoryPlayModeTests|InventorySceneUiTests|InventoryPauseInputTests|PlayerComboCombatTests|PlayerProgression
Visual (sem -batchmode/-nographics): DungeonCrawler.Tests.PlayerInput.InventorySceneUiTests.Capture_EquipmentComparison720pAnd1080p
```

O teste visual restaura a resolução selecionada e remove suas entradas temporárias. XML/logs usam nomes novos; capturas recusam sobrescrita. Logs locais `.log` seguem o ignore existente.

## Limites e próximo passo

Validação desta entrega ocorreu no Editor. Não foi gerada build Windows nem repetida uma run completa até vitória/derrota; QA artística pós-release e os demais itens da fila continuam com seu próprio aceite. Texto foi observado nas duas resoluções solicitadas; ultrawide e outras escalas não foram avaliados. Próximo passo: usar o atalho de teste de itens para experimentar trocas na bolsa; qualquer distribuição exige reconstruir e testar a build conforme `docs/BUILD-WINDOWS.md`.
