# V11: playtest de protótipos e aprovação do conteúdo

- **Base:** `main@dd9034b` ([base-commit.txt](base-commit.txt)), com V01–V10 integradas. **Branch:** `claude/v11-prototype-playtest`.
- **Executor:** Claude, por pedido explícito do usuário (o catálogo atribui a preparação ao Codex). Nenhum modelo, script de jogo, cena, prefab, SO ou valor de balanço foi alterado. Os únicos arquivos novos são esta pasta e o registro de estado em `NEXT_STEPS.md`/`MVP.md`.
- **Unity:** 6000.6.9f1. Testes funcionais em `-batchmode -nographics`; as quatro capturas `[Explicit]` no **Editor gráfico** (sem `-batchmode`), Game view 2560×1440, NVIDIA GeForce RTX 3060. Não houve build Windows nesta execução.
- **Código de teste:** só testes que já existiam na `main` (F13, F14, F15). Nenhum teste novo foi escrito.

## Quem fez o quê (separação exigida)

| | Participante | Versão / condições | O que conta |
| --- | --- | --- | --- |
| **Técnico (automação)** | Claude, pelo Unity Test Framework | `dd9034b`, cenas `F13_Lancer_Test`, `F15_StoneSentinel_Test`, `Floor_01_GeneratedCharacters` → `Floor_02_Fixed`; teclado/mouse simulados ou direção por `PlayerMovement.ExternalVelocity`; nas capturas F13 e na F15 "captura" o herói está invulnerável | Comportamento, contagens, quadros reais. **Não é avaliação humana**: um bot que desvia com conhecimento interno do setor não diz nada sobre legibilidade, justiça ou diversão |
| **Humano** | Usuário (dono do produto) | Revisou o material desta pasta (pranchas e conceitos) no app, em 19/11/2026, **sem jogar** os protótipos nesta execução | Aprovação artística e de escopo ([Decisões](#decisões-do-usuário)). Sem jogada humana, os valores de F13/F15 continuam **propostas**, não balanceamento aprovado |

## Resultados técnicos

### Testes executados (todos em processo novo, um de cada vez)

| # | Comando ([runtests.sh](runtests.sh), [test-runs.txt](test-runs.txt)) | Resultado |
| --- | --- | --- |
| 1 | EditMode `LancerAssetTests;StoneSentinelTests;SecondFloorTests`, sem GPU | **28/28** ([xml](editmode-targeted/EditMode.xml)) |
| 2 | PlayMode `LancerLungeTests;LancerTestRoomTests;StoneSentinelCombatTests;StoneSentinelArenaTests;SecondFloorPlayModeTests`, sem GPU | **34 passed, 0 failed, 4 skipped** (as 4 capturas `[Explicit]`) ([xml](playmode-targeted/PlayMode.xml), [lista por caso](playmode-targeted/cases.txt)) |
| 3 | Editor gráfico `LancerTestRoomTests.Capture_TelegraphDashAndClearedRoom` | **1/1**, 3 PNG ([editor-f13](editor-f13/perf/)) |
| 4 | Editor gráfico `StoneSentinelArenaTests.CaptureRealWarningRecoveryVictoryAndRestart` | **1/1**, 5 PNG ([editor-f15-capture](editor-f15-capture/perf/)) |
| 5 | Editor gráfico `StoneSentinelArenaTests.PlaytestActualMovementCombatAndSpatialDodge` | **1/1**, 4 PNG + [playtest.txt](editor-f15-playtest/perf/playtest.txt) |
| 6 | Editor gráfico `SecondFloorPlayModeTests.Traverse_BothFloors_WithActualCombatAndMovement` | **1/1** em 64 s, 4 PNG + [traversal.txt](editor-f14-traversal/perf/traversal.txt) |

Efeitos colaterais: os builders chamados pelos testes regravaram `Floor_01_GeneratedCharacters.unity` (mesmo tamanho), `MainMenu.unity` e `EditorBuildSettings.asset` (fim de linha), e o Editor acrescentou `SENTIS_ANALYTICS_ENABLED` ao `ProjectSettings.asset` (efeito conhecido, `docs/BUILD-WINDOWS.md`). Registrados em [editmode-targeted](editmode-targeted/side-effects-status.txt) e [playmode-targeted](playmode-targeted/side-effects-status.txt) e restaurados com `git checkout` depois de cada execução. Os `*.log` ficam fora do commit (`.gitignore`).

### Matriz pedida: aviso, compromisso, esquiva, acerto, morte, vitória, reinício

| Momento | Lanceiro (F13) | Sentinela (F15) |
| --- | --- | --- |
| **Aviso** | `Telegraph_LastsAtLeastOneSecond_WithoutMovementOrDamage` ✔. Quadro: [lancer-telegraph](editor-f13/perf/lancer-telegraph.png): faixa escura inteira + preenchimento laranja, corpo tingido de laranja | `InsideHitsOnceAfterOneSecond_ThenRecovers` ✔. Quadro: [02-warning](editor-f15-capture/perf/02-warning.png): setor escuro de 100°/6 m + laranja crescendo |
| **Compromisso** (direção travada) | `Direction_IsLockedAtCommit_TargetLeavingLaneTakesNoDamage` ✔, `Brain_ChasesCommitsAndKeepsFourExternalStates` ✔ | `LeavingSideOrRangeEscapes_AndDirectionDoesNotTrack` ✔, `DataChangesDuringWarningDoNotEnlargeAnnouncedDamage` ✔ |
| **Esquiva** | Dash: `DashInvulnerability_ConsumesContact_LancerPassesThroughWithoutLaterHit` ✔; sair da faixa: teste do compromisso ✔. **Sem quadro gráfico de esquiva** | Dash real: `RealDashAvoidsImpactInsideFan_ThenNoDelayedHit` ✔. Esquiva lateral pelo `CharacterController`: [playtest-dodge](editor-f15-playtest/perf/playtest-dodge.png) |
| **Acerto** | `TargetInsideLaneForWholeDash_IsDamagedExactlyOnce` ✔. Quadro [lancer-dash](editor-f13/perf/lancer-dash.png) mostra o meio da investida com o herói invulnerável, não um quadro de dano | `InsideHitsOnceAfterOneSecond_ThenRecovers` ✔, `DamageCallbackReentryCannotResolveTwice` ✔. Recuperação: [03-recovery](editor-f15-capture/perf/03-recovery.png) |
| **Morte** | Do lanceiro: `Brain_DeathMidDash_StopsTravelAndHidesWarning` ✔. Do alvo: `DeadOrDestroyedTarget_CancelsDuringTelegraph` ✔. **Morte do jogador pelo HUD não coberta** na sala F13 | Do Boss no aviso: `BossDeathDuringWindupCancelsAndToggleDoesNotRevive` ✔. Do jogador: `PauseMenuCancelsWindup_PlayerDeathHidesHud_AndRestartWorks` ✔ (Game Over e reinício) |
| **Vitória** | `SavedRoom_…_AndRoomClearsWithoutSoftlock` ✔, `Room_KillingLancersDuringTelegraph_ClearsRoomAndOpensExit` ✔. Quadro [room-cleared](editor-f13/perf/room-cleared.png) (+15 XP) | `ArenaHasValidNavMesh_HudOnlyDuringFight_VictoryOnce_AndLocalRestart` ✔. Quadros [04-victory](editor-f15-capture/perf/04-victory.png), [playtest-victory](editor-f15-playtest/perf/playtest-victory.png) |
| **Reinício** | **Não funciona** na sala F13 (limite conhecido de `docs/LANCER-F13.md`: a cena está fora das Build Settings); é preciso sair do PlayMode | Local, sem tocar a run salva: [05-restart](editor-f15-capture/perf/05-restart.png), [playtest-restart](editor-f15-playtest/perf/playtest-restart.png) (HP cheio, sala de novo em espera) |

Luta automatizada da Sentinela (sem teleporte, cheat ou dano sintético): 3 compromissos, 3 resoluções, Boss morto, herói com 100/100 HP. O bot sabe a origem e a direção travadas do setor e anda tangente a 5 m/s; por isso o HP final **não** mede dificuldade.

### Transição F14

`ExactStateTransfersOnce_AndMenuContinuesAtSafeEntry`, `TransitionRejectsIncompletePauseDeathAndMissingSequence`, `MissingSceneAndIncompatibleCatalogKeepCurrentRunAndCheckpoint`, `SecondRoomPausesSpawning_LastDeathCompletesOnce_…`, `CombatMenuResumesEntry…_DeathDeletesAndRestartStartsFloorOne`, `WriteFailureKeepsPlayerAndCurrentFloorUsable` e `NewRunFromSecondFloorStartsFirstAndDiscardsState`: **7/7**. Percurso gráfico com combate real nos dois andares ([traversal.txt](editor-f14-traversal/perf/traversal.txt)): andar 1, salas 1–5 limpas (HP 90 → 100, nível 1 → 5, ouro 21 → 135); descida voluntária; andar 2, sala 1 limpa (nível 6, ouro 160). Quadros: [andar 1 concluído](editor-f14-traversal/perf/floor-1-completed.png) com "Descer ao segundo andar", [chegada](editor-f14-traversal/perf/floor-2-arrival.png), [combate](editor-f14-traversal/perf/floor-2-combat.png), [andar 2 concluído](editor-f14-traversal/perf/floor-2-completed.png).

## Material de revisão

- Pranchas: [F13/F15](review/sheet-f13-f15-captures.png) (aviso, investida, sala limpa, aviso do leque, recuperação, esquiva, vitória, reinício) e [F14](review/sheet-f14-transition.png). Montadas por [contact_sheet.py](review/contact_sheet.py) a partir dos PNG originais desta execução.
- Conceitos (imagens 2D, **não** 3D pronto; sem malha, rig, take, kit, collider ou NavMesh):
  - **A13 Lanceiro corrompido**: [`SkeletonLancer_Concept_01.png`](../../../../Assets/_Project/Art/Concepts/SkeletonLancer_Concept_01.png), [README](../../../../Assets/_Project/Art/Concepts/Source~/SkeletonLancer/README.md).
  - **A14 Sentinela de Pedra**: [`StoneSentinel_Concept_01.png`](../../../../Assets/_Project/Art/Concepts/StoneSentinel_Concept_01.png), [README](../../../../Assets/_Project/Art/Concepts/Source~/StoneSentinel/README.md).
  - **A15 Cripta inundada** (entregue, PR #31): [`FloodedCrypt_Concept_01.png`](../../../../Assets/_Project/Art/Concepts/FloodedCrypt_Concept_01.png), [README](../../../../Assets/_Project/Art/Concepts/Source~/FloodedCrypt/README.md).

### Leitura visual (análise do revisor, não aceite humano)

**Protótipos**
- **Aviso do lanceiro** lê bem: faixa vermelho-escura de comprimento real com preenchimento laranja, e o corpo fica laranja. **Durante a investida a faixa some** e não há rastro: o deslocamento de ~0,44 s só se lê pelo movimento da cápsula. Pergunta de playtest: isso basta para entender de onde veio o golpe?
- A sala F13 usa o **jogador-cápsula do MVP**, não o Hero (`LancerTestSceneBuilder` usa `GetOrCreateMvpPlayerPrefab`); a F15 usa o Hero. A sensação de esquiva com o lanceiro não é a da release.
- **Leque da Sentinela** é a leitura mais forte do lote: setor inteiro visível desde o início, laranja chegando à borda, recuperação parada com a barra do Boss.
- **F14**: transição clara ("Colete as recompensas antes de descer. Não há retorno."). O andar 2 se chama **"Galeria das sentinelas"** no HUD (`SecondFloorSceneBuilder`, linha 74), mas não tem Sentinela; o nome antecipa um conteúdo que ainda não foi escolhido.

**Conceitos**
- **A13**: família coerente com os esqueletos (crânio grande, costelas, saiote) e silhueta distinta a 128 px (IoU máx. 0,54, medido no README). Nas vistas de **frente e costas a lança praticamente some** (escorço/oclusão); a identificação como lanceiro depende da vista lateral e da 45°. As placas aparecem azul-marinho, mais saturadas que o ferro #3E4152 da ficha.
- **A14**: silhueta claramente diferente do Warden (IoU 0,37), núcleo ciano contido, escala com o Hero e painel 45° com o setor real da F15 (100°/6 m). Pose de windup simétrica coerente com o leque. Largura 3,30 m contra colisor de 2,5 m.
- **A15**: rotas e centro 10×10 livres, água só nas bordas. Desenhado para **sala de 16 m**; a sala F14 tem **20 m** com coberturas em x ±5. Usar A15 exige decidir entre manter 20 m (adaptar o conceito) e mudar a métrica da sala.

### Achados técnicos (com reprodução)

1. **Texto de coleta sobre o painel de vitória.** O aviso flutuante "Arma coletada" fica desenhado em cima do botão "Coletar recompensas" no painel "Andar concluído" ([floor-1-completed](editor-f14-traversal/perf/floor-1-completed.png)); no andar 2 aparece o mesmo tipo de sobreposição ([floor-2-completed](editor-f14-traversal/perf/floor-2-completed.png)). Reproduzir: passo 6 acima, ou coletar um item e matar o último inimigo antes do aviso sumir. Correção fora do escopo da V11 (UI).
2. **Reinício na sala F13** não funciona pelo HUD (limite já documentado). Um playtest humano do lanceiro exige sair e entrar no PlayMode a cada tentativa.
3. **Baú do andar 2** continua de primitivas (pendência da V09), visível em [floor-2-combat](editor-f14-traversal/perf/floor-2-combat.png).

## Alternativas de escopo apresentadas

| Opção | O que significa | Efeito na fila |
| --- | --- | --- |
| **A. Manter protótipos isolados** | F13 e F15 continuam em cenas de teste fora do build; andar 2 com uma sala; conceitos ficam como referência | V12–V15 **não aplicáveis ao candidato**; segue V16 com a base atual |
| **B. Incluir atores escolhidos** | Ex.: lanceiro no encontro da sala do andar 2, e/ou Sentinela como Boss do andar 2 (exigiria uma arena no andar 2) | Libera V12 e/ou V13 (3D) e V15 (integração) só para o que for escolhido |
| **C. Ampliar salas** | Mais salas no andar 2 (quantidade explícita) e/ou kit A15 | Libera V14/V15 para a quantidade escolhida; exige decidir a métrica 16 × 20 m |

Nenhuma das opções cria padrão, fase ou bioma obrigatório por esta tarefa.

## Decisões do usuário

Perguntadas em 19/11/2026, **depois** de o material acima estar pronto e mostrado (pranchas F13/F15 e F14, mais os três conceitos). Respostas explícitas do usuário no chat; nenhuma foi inferida de tempo decorrido nem da automação.

| Item | Decisão | Consequência registrada |
| --- | --- | --- |
| **A13** Lanceiro corrompido | **Aprovado para V12** | V12 desbloqueada (malha, rig Generic, takes em `Art/ProductionStaging/CHAR-LANCER/`). A observação da lança pouco visível na frente/costas fica como nota de revisão para a modelagem, não como condição |
| **A14** Sentinela de Pedra | **Aprovado para V13** | V13 desbloqueada para a **fase única atual** (sem segunda fase nem padrão novo) |
| **A15** Cripta inundada | **Aprovado, mantendo a sala F14 em 20 m** | V14 desbloqueada para vestir a sala F14 existente na métrica atual (20 m, coberturas em x ±5); o conceito de 16 m se adapta, a sala não muda |
| **Escopo** | **Lanceiro + Sentinela no andar 2** | Andar 2 = a sala normal F14 atual com o lanceiro no encontro + uma arena nova com a Sentinela como Boss do andar. Nenhuma outra sala. Libera V15 para exatamente isso |

Fora da decisão (continua não autorizado): mais salas normais no andar 2, bioma/geração procedural, segunda fase da Sentinela, mudança no Guardião ou no loot global, lanceiro nos encontros do andar 1. Se a arena nova da Sentinela usa o kit A15 não foi decidido; a V14 cobre só a sala F14 até nova decisão.

Esta escolha veio do usuário, não deste plano. Os números de F13 (45 HP, aviso 1,1 s, 18 de dano…) e F15 (240 HP, leque 100°/6 m, aviso 1,4 s, 24 de dano…) **continuam propostas**: nenhum playtest humano foi jogado. A V15 deve tratá-los como valores iniciais e a avaliação humana fica para a V18 (ou um playtest antes, se o usuário pedir).

## Limitações

- Nenhum playtest humano jogado nesta execução. Valores de F13/F15 continuam propostas.
- Capturas são do Editor gráfico (Game view 2560×1440), não de build Windows nem monitor físico.
- Os percursos automatizados usam direção por velocidade externa e mira guiada; não exercitam teclado/mouse humanos.
