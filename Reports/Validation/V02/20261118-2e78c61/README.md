# V02 — Regressão global e idempotência após F10

- **Execução:** 18/11/2026, Claude (com a tarefa V02 delegada pelo usuário). Branch `claude/v02-regression-idempotency`, criada de `main@2e78c61` (= `origin/main`, com árvore limpa).
- **Ambiente:** Windows 11 Pro e Unity **6000.6.9f1** em **batchmode** (`-nographics`), na instalação real (`C:/Program Files/Unity/Hub/Editor/6000.6.9f1`). Nenhuma outra instância do Editor estava aberta (sem `Temp/`). Pacotes conforme manifest/lock, sem alteração.
- **Validado no Editor em batchmode:** compilação, EditMode completo, PlayMode completo e integradores executados por `-executeMethod`.
- **Fora desta execução:** testes `[Explicit]` de captura, revisão visual e PlayMode gráfico. Não houve aceite humano.

## Resultado por critério

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Compilação e suítes atuais sem falhas, com Explicit/skip discriminados | **Atendido.** 0 `error CS` nos três logs. EditMode **269/269**. PlayMode **199 passed, 0 failed, 15 skipped**. Os 15 skips são `[Explicit]` de captura/percurso gráfico (lista abaixo) | [after/EditMode.xml](after/EditMode.xml), [after/PlayMode.xml](after/PlayMode.xml) |
| Falha histórica resolvida ou comprovadamente ausente, com causa e evidência | **Resolvida.** Reproduzida no `2e78c61` (268/269) e corrigida (269/269), sem mexer no teste ou no assert | [before/EditMode.xml](before/EditMode.xml), § Causa |
| Segunda integração sem perda de dados, duplicação ou alterações fora do escopo | **Atendido.** Nos três integradores, a 2ª execução produz **0 arquivos diferentes** em `Assets/` + `ProjectSettings/`. GUIDs e `Pickup.prefab` ficam intactos | [idempotency/](idempotency/diffs.txt) |

## Causa da falha `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`

- **Antes (`2e78c61`):** `System.InvalidOperationException : Inventory slot missing: 0`. O caminho da pilha foi `IntegrateGame` → `IntegrateReleaseFloorScene` → `ProductionVisualIntegrator.ConfigureSceneInventoryVisuals` → `InventorySceneBuilder.ConfigureItemVisuals`, que procura `Window/Slot_00`.
- **Diagnóstico: defeito de composição, não fixture desatualizada.** A F10 (`TextScaleSceneBuilder.Inventory`) move os 16 slots para `Window/Slots`, um `GridLayoutGroup`, e move `Equipment`/`Details` para `ItemReading/Viewport/Content`. Isso já está salvo nas cenas.
  - O fluxo canônico `InventorySceneBuilder.ConfigureHud` funciona porque segue uma ordem própria. Primeiro chama `TextScaleSceneBuilder.PrepareInventoryRebuild`, que devolve os slots a `Window`. Depois configura os ícones. Por último chama `TextScaleSceneBuilder.ConfigureScene`, que reaplica o layout da F10.
  - O caminho isolado do integrador de itens chamava `ConfigureItemVisuals` sobre a cena já reorganizada e não seguia essa ordem.
  - O mesmo caminho é usado por `DungeonFloorSceneBuilder` (linha 189) ao regenerar o andar. A falha não era só do teste.
- **Reparo mínimo** em `Assets/_Project/Scripts/Editor/ProductionVisualIntegrator.cs` (`ConfigureSceneInventoryVisuals`, +3 linhas): segue a mesma ordem do fluxo canônico, chamando `PrepareInventoryRebuild`, depois `ConfigureItemVisuals` e por fim `ConfigureScene`. O teste, os asserts, o layout, as regras e a arte não mudaram.
- **O layout da F10 foi preservado.** A cena de release que sai da suíte com o reparo é **idêntica** (mesmo hunk binário e blob `22c287b`) à cena que sai da suíte sem o reparo. Na execução sem reparo, os demais builders salvam a cena e o integrador de itens aborta antes de salvar. Então o reparo não acrescenta nenhuma alteração além do que os builders já produzem.

## Comandos executados

Todos foram executados a partir de `G:/DungeonCrawler/My project`, um de cada vez. O `-quit` foi omitido nas execuções com `-runTests`, como pede o `docs/BUILD-WINDOWS.md`.

| # | Comando (Unity.exe `-batchmode -nographics -projectPath …`) | Estado do código | Resultado |
| --- | --- | --- | --- |
| 1 | `-runTests -testPlatform EditMode`, `DUNGEON_CRAWLER_TEST_REPORT_DIR=…/before/perf` | `2e78c61` sem alteração | 269 casos, **268 passed, 1 failed** (a falha histórica). Exit 2 |
| 2 | Idem, em `after/` | com o reparo | **269 passed**, 0 failed, 0 skipped. Exit 0 |
| 3 | `-executeMethod DungeonCrawler.Editor.<integrador> -quit`, 2× cada, hash de `Assets/` e `ProjectSettings/` depois de cada processo ([idem.sh](idempotency/idem.sh)) | com o reparo, árvore restaurada | 6 execuções com exit 0 (ver [runs.txt](idempotency/runs.txt)) |
| 4 | `-runTests -testPlatform PlayMode`, em `after/` | com o reparo, árvore restaurada | 214 casos: **199 passed, 0 failed, 15 skipped**. Exit 0 |

O PlayMode não foi executado antes do reparo. O reparo altera só código de Editor, que o PlayMode não carrega.

### Skips do PlayMode (todos `[Explicit]`, não representam aceite visual)

`ControlRebindingInputTests.Capture_Controls720pAnd1080p`, `InventorySceneUiTests.Capture_EquipmentComparison720pAnd1080p`, `…Capture_InventorySort720pAnd1080p`, `…Capture_QuickPotionOptionsAndHud720pAnd1080p`, `ItemsCombatSceneTests.Capture_ReleaseFloorEvidence`, `RangedLineOfSightReleaseTests.Capture_ReleaseRangedBehindDoorAndFiring`, `RewardFeedbackVisualTests.Capture_ReleaseFeedback720pAnd1080p`, `RoomRewardChestTests.Capture_AllRoomsAndTorchPositions`, `…Capture_ReleaseChestLockedReadyAndOpened`, `SecondFloorPlayModeTests.Traverse_BothFloors_WithActualCombatAndMovement`, `StoneSentinelArenaTests.CaptureRealWarningRecoveryVictoryAndRestart`, `…PlaytestActualMovementCombatAndSpatialDodge`, `TextScaleInputTests.Capture_LayoutsAtAllScalesAndResolutions`, `DestructiblePropTests.Capture_ReleaseVaseBeforeAndAfterActualAttacks`, `LancerTestRoomTests.Capture_TelegraphDashAndClearedRoom`. Eles exigem um Editor com janela e ficam para V16.

## Idempotência dos integradores (comando 3)

| Integrador | 1ª execução, em relação ao estado anterior | 2ª execução, em relação à 1ª |
| --- | --- | --- |
| `ProductionVisualIntegrator.IntegrateGame` (sobre a árvore limpa) | cena de release, 18 `.meta` A01/A03 (reimportação), `ProjectSettings.asset` (define), `EditorBuildSettings` (EOL) | **0 arquivos** (digest `d3f3982…` igual) |
| `TextScaleSceneBuilder.Integrate` | `Floor_01`, `MVP_Arena`, `MainMenu` (este só em EOL) | **0 arquivos** (`3c6ae6c…`) |
| `InventorySceneBuilder.Integrate` | cena de release, `Floor_01`, `MVP_Arena`, `Player.prefab` (só EOL) | **0 arquivos** (`0b3d2b1…`) |

- **GUIDs:** 0 linhas `guid:` alteradas em todo o diff. `Pickup.prefab` manteve o blob `887bffa` (igual ao da HEAD), e o próprio teste confere o GUID dele.
- **Convergência:** depois dos três integradores, as cenas de release, `Floor_01` e `MVP_Arena` têm exatamente os blobs (`22c287b`, `f6b1bf2`, `e8b09c0`) produzidos pela suíte EditMode. Os builders chegam a um único ponto fixo, sem duplicar objetos a cada passagem.
- **Cenas não relacionadas:** `Floor_02_Fixed`, `F13_Lancer_Test`, `F15_StoneSentinel_Test` e as demais não mudaram em nenhuma etapa.

## Efeitos colaterais das suítes e dos integradores (preservados, não commitados)

Cada etapa regravou assets versionados. Esses arquivos foram identificados como saída **desta execução**, não como mudança de terceiros. Salvei o diff de cada etapa e só depois restaurei os arquivos ao estado da HEAD, para que o PR fique dentro do escopo: [before/side-effects.patch](before/side-effects.patch), [after/side-effects.patch](after/side-effects.patch), [idempotency/side-effects.patch](idempotency/side-effects.patch) e [after/playmode-side-effects.patch](after/playmode-side-effects.patch), com os `*-status.txt` correspondentes.

1. **Cenas binárias versionadas estão defasadas em relação aos builders atuais.** Aparecem regravadas em todas as execuções: release (+768 B), `Floor_01` (+944 B) e `MVP_Arena` (+944 B). Os prefabs `Enemy_Melee/Ranged/Elite` ganham `lunge: {fileID: 0}`, campo serializado de `EnemyBrain` criado na F13 (`664e355`) sem regravar os assets. A causa provável nas cenas é a mesma, mas **não está comprovada**: as cenas são binárias e não dá para ler o diff. Nenhum dado é removido; o campo novo fica nulo, que é o comportamento atual. Regravar essas cenas e prefabs exige coordenação (AGENTS/CLAUDE), por isso não entrou aqui.
2. **18 `.meta` de A01/A03** (`ICON-ACTION-DASH`, `ICON-ACTION-SPECIAL`, `UI-SKIN-*`): a reimportação preenche `spriteSheet.sprites`/`internalIDToNameTable` com o mesmo GUID. Isso confirma o achado da V01 de que as `.meta` foram escritas à mão. O destino correto é V03/V04.
3. **`ProjectSettings.asset`:** o Editor acrescenta `SENTIS_ANALYTICS_ENABLED` ao define Standalone quando abre o projeto (visto com `-executeMethod -quit` e no PlayMode). Não vem dos builders do projeto. Fica fora do escopo.
4. **Arquivos alterados só em EOL** (LF↔CRLF): `Player.prefab`, `Boss_Warden.prefab`, `MainMenu.unity`, `RunCheckpointCatalog.asset`, `EditorBuildSettings.asset`, `Mat_Vase_F09.mat` e relatórios de `Reports/Leadership`. `validation.json` muda só a data.
5. **Artefatos de desempenho:** os testes escreveram em `after/perf` e `before/perf`, porque `DUNGEON_CRAWLER_TEST_REPORT_DIR` estava isolado. `CombatProfiler.raw` (2,4 MB) foi removido do relatório. Os JSONs foram mantidos.

Snapshots de hash: [h0-clean.sha256](idempotency/h0-clean.sha256) (árvore limpa) e [InventorySceneBuilder.Integrate-run2.sha256](idempotency/InventorySceneBuilder.Integrate-run2.sha256) (estado final). As snapshots intermediárias foram substituídas por [snapshot-digests.txt](idempotency/snapshot-digests.txt), em que run1 = run2 nos três integradores, e por [diffs.txt](idempotency/diffs.txt).

## Limitações

- **NÃO VALIDADO NO EDITOR GRÁFICO.** Não houve PlayMode com janela, capturas nem inspeção visual do inventário depois do reparo. A preservação do layout foi comprovada só pela igualdade de bytes da cena (ver § Causa), não por imagem.
- Resultados históricos (F10–F13: 244/245 e semelhantes) não pertencem a esta execução. Os números acima vêm dos XMLs desta pasta.
- A regravação das cenas e prefabs defasados (efeito 1) e a reimportação das `.meta` (efeito 2) ficaram pendentes, porque exigem tarefa coordenada.

## Próximo passo

Depois do merge: V03 em diante, conforme `NEXT_STEPS.md`. Recomendação: decidir se a regravação das cenas binárias e dos prefabs defasados (efeito 1) entra em V16 ou numa tarefa própria. Até lá, toda suíte EditMode vai deixar esses arquivos modificados no checkout.
