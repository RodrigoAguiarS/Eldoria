# V10: modelo dos vasos destrutíveis (A12 / MODEL-DESTRUCTIBLE)

- **Base:** `main@ea56b20` ([base-commit.txt](base-commit.txt)), com a V09 já integrada (PR #45). **Branch:** `claude/v10-destructible-model`.
- **Commit testado:** `3718474`, a integração ([tested-commit.txt](tested-commit.txt)). O commit seguinte traz só estes documentos, o manifesto, o `NEXT_STEPS.md` e o `MVP.md`.
- **Unity:** 6000.6.9f1, batchmode com e sem `-nographics`. As capturas são renders fora da tela (RenderTexture) da câmera real do jogo e de uma câmera lateral, em PlayMode com GPU; não são o Game view do Editor gráfico.
- **Executor:** Claude, por pedido explícito do usuário, embora o catálogo atribua a V10 ao Codex. O escopo seguiu o catálogo: visuais Intact/Rubble, material/flash, integrador e testes. `DestructibleProp`, `DestructiblePropPresenter`, `DestructiblePropDefinition` (40 HP), colliders lógicos, máscaras e checkpoint não mudaram.

## Critérios de aceite

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Uma quebra e um dano por impacto básico/giro com múltiplos colliders | **Atendido.** Nos cinco vasos da cena de release, com o botão esquerdo e o E reais (InputTestFixture): o básico gera exatamente um `Damaged`, com HP = 40 − valor do evento e o vaso ainda inteiro, com os dois colliders (esfera do `Body` + cápsula do `Rim`) na área; o giro gera um `Damaged` e um único `Broken`. Depois, um básico e um giro reais sobre os destroços (o giro acontece: um `AttackImpacted`) não geram dano nem quebra. O flash do primeiro dano sai no renderer A12 intacto com a cor exata `Lerp(branco, (1; 0,65; 0,3), opção de flash)` e volta ao branco; as primitivas continuam invisíveis | `DestructiblePropModelArtSceneTests.FiveVases_…`; `DestructiblePropModelArtTests.ReleaseScene_…` |
| Toggle não cura; reinício cria run nova e apresentação acompanha estado | **Atendido.** Desligar/religar o vaso quebrado mantém HP 0, `Intact` desligado, `Rubble` (seis peças A12) ligado e colliders desligados; `TakeDamage` recusa. Religar `Intact` à força e reativar o presenter faz ele reconciliar de volta para quebrado. **R** real (com um vaso quebrado e outro com 30 HP) descarrega a cena e cria run nova: cinco vasos com 40 HP, A12 intacto, flash sem tint. Checkpoint: vaso quebrado, salvar, menu, **Continuar**: a sala concluída volta concluída e os cinco vasos voltam inteiros — é a política F11 existente ("vasos sem recompensa podem reaparecer inteiros", `docs/CHECKPOINTS-F11.md`); nenhuma persistência nova | `FiveVases_…`, `RestartKey_…`, `CheckpointResume_…` |
| Não interfere em EnemyTracker, portas/projéteis ou limpeza; referências e layout preservados | **Atendido.** Quebrar o vaso durante a onda não muda `Alive`/`Total` do `EnemyTracker`, não limpa a sala nem muda portas, e o vaso não está em `SpawnedEnemies`; matar os inimigos limpa normalmente. Nas salas limpas, a quebra não dispara `Cleared` nem muda `Total`. Em toda a cena de release e em todos os prefabs de `Assets/_Project`, a única máscara de gameplay que inclui a layer `Destructible` é `destructibleMask` de `PlayerCombat`/`PlayerSpecial` (projéteis, oclusão, gatilhos, drops e NavMesh não); a layer continua sem colisão física. Posição `(−5,5; 0; −2)`, definição, colliders e escala das primitivas inalterados; visuais dentro do footprint F09. Dump `main` × integrada: 1040 → 1085 objetos, 50 diferentes, todos sob os cinco `DestructibleVase_F09` ([scene-dump](scene-dump/scene-diff-objects.txt)) | `BreakDuringWave_…`, `FiveVases_…`, `OnlyBasicAndSpinMasks_…`, `Reapply_AndF09Builder_…` |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Editor/DestructiblePropModelArtIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/Environment/Apply Destructible Model Art (A12)` e `Validate …`. Lê `MODEL-DESTRUCTIBLE.source.json` (o mapa `variants` objeto a objeto) e `ModelDestructible.validation.json`; confere os quatro FBX importados: nós, triângulos 344/344/360/180 (≤ 600), uma submalha e o material `MODEL-DESTRUCTIBLE_Prop` por nó, pivôs e limites (com o espelho de X da importação), raiz sem rotação/escala, importação estática sem colisor/take, intacto dentro do footprint F09 e quebrado dentro do intacto. Cria `Mat_PropA12`, monta `Intact/Visual/Jar_Intact` e `Rubble/Visual/Jar_Broken_*` a partir das malhas e poses importadas (Y 180°), desliga só os `MeshRenderer` das primitivas e aponta o `DamageFlash` para `Jar_Intact`. Reutiliza `ProjectileArtIntegrator.EnsureCoreImport` para a paleta (já conforme; nada mudou). Idempotente; `Validate` recusa colisor/Rigidbody no visual, primitiva visível, flash errado, colliders lógicos alterados ou visual fora do footprint |
| `Assets/_Project/Materials/DestructibleArt/Mat_PropA12.mat` (novo) | URP/Lit opaco, `_BaseMap` = `MODEL-DESTRUCTIBLE_Palette.png`, branco, metallic 0, smoothness 0,15, sem emissão. Uma instância para intacto e destroços dos cinco vasos |
| `Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity` | Só os cinco `DestructibleVase_F09` (ver [scene-dump](scene-dump/scene-diff-objects.txt)) |
| `Assets/_Project/Scripts/Editor/DestructiblePropSceneBuilder.cs` | Duas linhas: com o A12 instalado, o builder F09 mantém o renderer A12 como alvo do flash, em vez de voltar ao `Body` (que ficou sem renderer ligado). Ver Divergência 1 |
| `Assets/_Project/Tests/EditMode/DestructiblePropModelArtTests.cs` (novo) | 6 testes: contrato × importação; material; cinco vasos com definição/HP/colliders/posição/flash; máscaras; reaplicação + builder F09 idempotentes com GUIDs preservados; instalação num vaso só de primitivas |
| `Assets/_Project/Tests/PlayMode/PlayerInput/DestructiblePropModelArtSceneTests.cs` (novo) | 4 testes na cena de release (cinco vasos com ataques reais, quebra durante a onda, R, checkpoint) e a captura `[Explicit]` com GPU |

## Decisões e divergências

1. **Builder F09 tocado.** `DestructiblePropSceneBuilder.ConfigureRelease` reatribuía `DamageFlash.targetRenderer` ao `Body` a cada execução, o que deixaria o flash num renderer desligado (sem feedback visível). O critério "referências preservadas" e a idempotência exigiam corrigir isso; a mudança é só a escolha do renderer. Sem o A12 instalado, o comportamento é o de antes.
2. **Só o vaso instalado; a caixa não.** O prompt manda aplicar primeiro o vaso ao alvo F09 e trata a caixa como variante artística, sem autorização de gameplay. A caixa é conferida pelo contrato (importação, orçamento, footprint) e fica pronta em staging; instalá-la exigiria decisão de design (qual sala, collider de caixa), fora da V10.
3. **Visual montado a partir das malhas, como na V09.** O integrador cria os nós com as malhas e poses lidas do FBX, em vez de instanciar o modelo; reimportar o FBX atualiza as malhas, e o `Validate` acusa pose divergente. O material embutido nos FBX não é remapeado no `.meta` (as importações ficam intactas): o integrador atribui `Mat_PropA12` aos renderers.
4. **Primitivas sem renderer, colliders mantidos.** Seguindo a proposta do A12, `Body` (esfera, com o `DamageFlash`) e `Rim` (cápsula) continuam como volumes de acerto, com o `MeshRenderer` desligado; os `Shard_*` (sem collider) também ficam só com o renderer desligado. O vaso A12 (0,78 × 0,68 m) cabe nesses volumes.
5. **Y 180°.** O modelo tem a frente (rachaduras) em +Z e a câmera do jogo olha de −Z (mesma razão do baú na V09). O footprint é simétrico, então nada muda em cobertura/navegação.
6. **Sem burst, física ou recompensa.** A troca continua estática; nenhum Rigidbody, partícula, XP ou loot.

## Comandos e resultados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. [idempotency/run.sh](idempotency/run.sh) roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([runs.txt](idempotency/runs.txt)). [runtests.sh](runtests.sh) roda `-runTests` com `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado ([test-runs.txt](test-runs.txt)). Os logs (`*.log`) e o `CombatProfiler.raw` não foram commitados.

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh DestructiblePropModelArtIntegrator.Apply run1` | exit 0: contrato × FBX ok na primeira execução; material criado e cena gravada |
| 2 | `Apply run2` | exit 0, "gravado": só `Mat_PropA12.mat` mudou (a URP acrescenta o subobjeto de versão ao material recém-criado); cena igual ao run1 |
| 3 | `Validate validate1`; `Apply run3` | `Validate ok`; run3 "sem mudanças", hashes iguais ao run2 |
| 4 | EditMode filtrado `DestructiblePropModelArtTests` | **6/6** ([targeted/attempt0](targeted/attempt0/)). O builder F09 do teste só regravou `Mat_Vase_F09.mat` e `Player.prefab` com fim de linha diferente (conteúdo igual); restaurados |
| 5 | PlayMode `DestructiblePropModelArtSceneTests`, sem GPU | attempt1: 2 falhas do teste. (a) No vaso 2 o giro não saiu: o E extra sobre os destroços do vaso 1 consumiu a recarga de 6 s; o teste passou a esperar o giro disponível e a provar que o giro extra acontece. (b) A checagem de cena nova comparava o `Scene` (handle); trocada pela destruição do vaso antigo. attempt2: R não reiniciava; o diagnóstico (attempt3 com erro de compilação do próprio diagnóstico, só log; attempt3b) mostrou `Não foi possível descartar o checkpoint anterior`: a pasta temporária de checkpoint do teste não existia. O `Setup` passou a criá-la. Nenhuma falha do produto |
| 6 | PlayMode `DestructiblePropModelArtSceneTests`, sem GPU | **4 passed, 0 failed, 1 skipped** (captura) ([targeted/attempt4](targeted/attempt4/)) |
| 7 | Captura com GPU, attempt0 | 1/1, mas a câmera lateral ficou atrás do Hero e o quadro do flash já tinha decaído (um quadro em batchmode passa dos 0,12 s do flash) ([graphical-attempt0](graphical-attempt0/perf/)). Câmera lateral movida para o lado oposto; o quadro do flash renderiza no mesmo quadro do dano |
| 8 | Captura com GPU, attempt1 | **1/1**: 25 PNG e [context.txt](graphical-attempt1/perf/context.txt) (flash com tint `(1; 0,65; 0,3)` no renderer A12) |
| 9 | Commit `3718474`; EditMode completa sem GPU | **374/374** ([editmode](editmode/EditMode.xml)). A suíte regrava prefabs, cenas e relatórios de Leadership pelos builders, como na V05–V09, incluindo a cena de release ([side-effects](editmode/side-effects-status.txt)). Antes de restaurar, `Validate` na cena regravada: **ok**, o A12 sobrevive aos builders ([runs.txt](idempotency/runs.txt)). Tudo restaurado com `git checkout` |
| 10 | PlayMode completa sem GPU | **255 passed, 0 failed, 28 skipped** (`[Explicit]` gráficos e testes só com GPU, incluindo a captura da V10) ([playmode](playmode/PlayMode.xml)); só `ProjectSettings` tocado ([side-effects](playmode/side-effects-status.txt)) |
| 11 | Dump da cena `main` × integrada (script temporário, removido; fonte em [scene-dump](scene-dump/V10SceneDump.cs.txt)) | Diferenças só nos cinco vasos. O dump (o mesmo da V09) percorre propriedades visíveis e não lista o `m_Enabled` dos componentes, então os renderers desligados de `Rim`/`Shard_*` não aparecem nele; isso é provado pelo `Validate` e pelos testes |

`ProjectSettings/ProjectSettings.asset` ganha `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor, e `EditorBuildSettings.asset` muda em fim de linha. É o efeito conhecido do pacote de inferência (`docs/BUILD-WINDOWS.md`), não desta tarefa; os dois foram restaurados antes de cada execução e de cada commit.

## Leitura visual (render real)

Cena de release, Hero do `Player.prefab` invulnerável a (1,4; 0; −1,1) do vaso, virado para ele; 1280×720 pela câmera do jogo e pela lateral nas cinco salas, e 1920×1080 na sala 1 ([captures](graphical-attempt1/perf/)):

- **Lateral intacto** (`1280-room-N-intact-side.png`): vaso de barro decagonal baixo, gargalo e boca escura, duas rachaduras finas viradas para a câmera, sombra no chão. Escala coerente com o Hero.
- **Lateral flash** (`1280-room-1-flash-side.png`): o barro fica mais alaranjado/saturado. Lê, mas é sutil, porque o tint laranja multiplica um barro já laranja.
- **Lateral quebrado** (`1280-room-N-broken-side.png`): casca partida com interior escuro, aro caído deitado sobre a base (lê como um disco escuro) e quatro cacos no chão, tudo dentro da área do vaso; sem física.
- **Câmera do jogo** (`*-game.png`): o vaso se destaca do piso azul pela cor quente, ao lado do Hero; quebrado, vira uma mancha baixa e escura com cacos. As cinco salas repetem a mesma leitura.

## Limitações e pendências

- **Aprovação artística humana pendente.** Flash laranja sutil sobre o barro (a cor do tint é do presenter F09, não foi mudada); aro caído do quebrado lê como um disco escuro. Os renders não são o Game view do Editor gráfico nem um monitor físico; HUD fora. Build Windows não executado.
- **Caixa não instalada** (Decisão 2).
- **Vasos fora do checkpoint**, como já decidido no F11: ao retomar, voltam inteiros.
- **Integrador depois de cenas novas.** Um vaso novo criado pelo builder F09 nasce de primitivas; rodar `Apply Destructible Model Art (A12)` em seguida. O `Validate` acusa.
- **Provas de footprint pelos limites.** Os FBX não são Read/Write (importação preservada); footprint e "quebrado dentro do intacto" usam os limites das malhas.
