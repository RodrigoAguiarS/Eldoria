# V06 — Rastro de dash (A07 / FX-DASH)

- **Base:** `main@4a9c93c` ([base-commit.txt](base-commit.txt)). **Branch:** `claude/v06-dash-trail`.
- **Commit testado:** `a141586`, a integração ([tested-commit.txt](tested-commit.txt)). O commit seguinte traz só estes documentos, o manifesto e o `NEXT_STEPS.md`.
- **Unity:** 6000.6.9f1, em batchmode (com e sem `-nographics`) e no Editor gráfico para a captura.
- **Executor:** Claude, por pedido explícito do usuário, embora o catálogo atribua a V06 ao Codex. O escopo seguiu o catálogo: presenter dedicado, gancho mínimo no `PlayerDash`, integrador e testes.

## Critérios de aceite

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Um efeito por dash aceito; tentativa em cooldown sem rastro | **Atendido.** Com o teclado real na `MVP_Arena`, um dash aceito dispara `DashStarted` uma vez, o presenter inicia um rastro (`TrailsStarted` = 1) e o renderer emite. A nova tentativa dentro dos 2 s de cooldown não dispara evento, não inicia rastro e o renderer continua desligado. Em 8 dashes seguidos (cooldown zerado só no teste) foram 8 rastros | `DashTrailSceneTests.AcceptedDash_…`, `WarmDashUse_…` em [targeted/final](targeted/final/) e na [suíte completa](playmode/PlayMode.xml) |
| Sem segmentos antigos | **Atendido.** No início do segundo dash a cauda anterior tem 0 pontos. Um teleporte de 12 m durante o dash limpa o rastro e nenhum ponto fica do outro lado. Restaurar checkpoint, desativar o `PlayerDash`, desativar/reativar o presenter e morrer deixam o renderer desligado com 0 pontos. Andar logo depois do dash não estende a cauda, que some em até `trail.time` (0,16 s) com o Hero parado | `SecondDashAndTeleport_…`, `CheckpointDisableReactivationAndDeath_…`, `AcceptedDash_…` |
| Piso e perigos legíveis | **Atendido na captura; aprovação artística pendente.** Veja [Leitura visual](#leitura-visual-captura-real). Pelo contrato, o alpha máximo é 163/255 e as bordas são transparentes (teste EditMode). Blend Alpha, não aditivo. O azul da capa fica fora do matiz de ameaça | `DashTrailArtTests.Contract_…`, [graphical/](graphical/) |
| Regressões de dash/invulnerabilidade preservadas | **Atendido.** O dash move mais de 1 m, recusa dano durante a invulnerabilidade e respeita o cooldown. Os valores serializados do `PlayerDash` no prefab são iguais aos `player_dash_defaults_read_only` do contrato (16 m/s, 0,18 s, 0,25 s, 2 s). O gancho só adiciona três eventos, depois do estado já aplicado. As suítes completas passam | `AcceptedDash_…`, `DashTrailArtTests.PlayerPrefab_…`, `MvpSceneAcceptanceTests.Arena_DashMoves…`, [editmode](editmode/EditMode.xml), [playmode](playmode/PlayMode.xml) |
| Sem criar efeito por uso aquecido | **Atendido.** Em 8 dashes reais, transforms e componentes do jogador não mudam (8 e 42) e o `TrailRenderer` é a mesma instância. Os callbacks `DashStarted`/`DashFinished` do presenter fizeram **0 alocações** em 1.000 ciclos depois de 64 de aquecimento. Controle positivo: `new byte[4096]` registrou 1 | [targeted/final/perf-graphics/dash-trail.json](targeted/final/perf-graphics/dash-trail.json) |
| Pausa, morte, desativação, opções visuais; VFX de combo/giro mantidos | **Atendido.** Com timeScale 0 no meio do dash, nenhum ponto novo é gravado e o Hero não se move; ao retomar, o dash termina normalmente. Flash 0 suprime o rastro sem mudar o dash; flash 0,5 dá alpha 0,5; trocar para 0 no meio do dash limpa. Na cena de release o rastro do dash é outro objeto e outro material que o `SlashTrail`, e o dash não liga o rastro do combo | `Pause_…`, `FlashZero_…`, `DashAndComboVfx_StayIndependent` |
| Direção e envelope do contrato | **Atendido.** Com GPU, a UV assada pelo próprio TrailRenderer tem u < 0,1 no vértice mais próximo do Hero e u > 0,9 no mais distante, como pede o contrato. Comprimento ≤ 2,56 m + 0,3, altura constante de 0,8 m | `TrailGeometry_HeadAtHeroWithinContractLength` (ignorado sem GPU, de propósito) |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Player/PlayerDash.cs` | Gancho mínimo: `DashStarted` (dash aceito, depois de velocidade e invulnerabilidade), `DashFinished` (fim normal) e `DashInterrupted` (desativado durante o dash ou `RestoreCheckpoint`). Nenhuma linha de movimento, custo, cooldown ou invulnerabilidade mudou |
| `Assets/_Project/Scripts/Player/DashTrailPresenter.cs` (novo) | Só apresentação. Observa os eventos do `PlayerDash` e o `Died`/`ResetPerformed` do `Health`, além das opções (flash). Controla um único TrailRenderer reutilizado |
| `Assets/_Project/Scripts/Editor/DashTrailArtIntegrator.cs` (novo) | Menu `Tools/Dungeon Crawler/Feedback/Apply Dash Trail Art (A07)` e `Validate …`. Lê `FX-DASH.trail.json` (tempo, largura, curva, altura, modo de textura, alinhamento, vértices, sentido de U) e recusa divergências. Reutiliza `EnsureImport`/`EnsureMaterial` do V05 |
| `Assets/_Project/Materials/DashFx/Mat_FxDash.mat` (novo) | URP/Particles/Unlit, transparente, Blend Alpha, branco, tiling (1, 1), porque U = 0 está na cabeça |
| `Assets/_Project/Prefabs/Player/Player.prefab` | Filho novo `DashTrail` em (0; 0,8; −0,15) com TrailRenderer (0,16 s, 0,6 m, curva 1 → 0,85 → 0,55, Stretch, View, 0 cap / 1 corner, sem sombras ou probes, **desligado por padrão**) e `DashTrailPresenter`. Diff só aditivo (179 linhas). Herdado por `MVP_Arena`, `Floor_01`, release e `Floor_02_Fixed` |
| `Assets/_Project/Tests/EditMode/DashTrailArtTests.cs` (novo) | Contrato × PNG real, prefab × contrato, valores do dash, reaplicação idempotente, curva e `Validate` |
| `Assets/_Project/Tests/PlayMode/PlayerInput/DashTrailSceneTests.cs` (novo) | 8 testes com o teclado real na cena salva (um deles na cena de release) |
| `Assets/_Project/Tests/PlayMode/PlayerInput/DashTrailVisualTests.cs` (novo) | Captura `[Explicit]` 720p/1080p na cena de release |

## Decisões e divergências

1. **`emitting = false` não basta na Unity 6000.6 (medido).** A ponte do contrato era "borda de IsDashing → `emitting = true/false`; a cauda some sozinha". A primeira rodada de testes falhou porque apareciam pontos com `emitting = false`. Três sondas isoladas ([trail-probe](trail-probe/), um TrailRenderer limpo e uma câmera) mostraram, em batchmode com GPU:
   - movendo o objeto com `emitting = false`, o renderer grava pontos **e** os renderiza: 11 pontos e 24 vértices assados (`probe2.txt` B);
   - com o objeto parado, os pontos não envelhecem: depois de 0,4 s ainda há 11 pontos e 24 vértices. Eles só expiram no próximo movimento (`probe3.txt` C/C2);
   - `minVertexDistance` = 10.000 impede pontos novos (`probe3.txt` B), e `enabled = false` com `Clear()` zera tudo (`probe2.txt` E).

   Por isso o presenter controla o ciclo inteiro:
   - **Início:** `Clear()`, renderer ligado e emitindo.
   - **Fim normal:** para de emitir e congela a cauda (`minVertexDistance` = 10.000). A cauda é apagada depois de `trail.time`.
   - **Interrupção, morte, reset, desativação, teleporte (> 6 m entre quadros) e flash 0:** apaga na hora.

   O efeito visual é o do contrato: rastro durante o dash e cauda de no máximo 0,16 s. A diferença é que a cauda não depende do Hero se mover para sumir. O renderer fica desligado no prefab.
2. **"Atrás do corpo"** está no contrato só como texto. O emissor fica 0,15 m atrás do pivô, na altura de 0,8 m do contrato (`BehindBodyMetres`).
3. **Teleporte:** o contrato pede para limpar; não há evento de teleporte. O presenter limpa quando o rastro salta mais de 6 m entre dois quadros. O dash percorre no máximo ~5,3 m num quadro de 0,33 s.
4. **Opções:** o rastro usa só a opção de flash, como o F05/V05: 0 desliga e valores intermediários multiplicam o alpha. Shake não se aplica.
5. **Animação:** a ponte de animação (`GeneratedPlayerAnimationBridge`) não foi tocada. Ela continua usando a borda de `IsDashing`, e o rastro não duplica personagem nem Animator.

## Comandos e resultados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. [idempotency/run.sh](idempotency/run.sh) roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([runs.txt](idempotency/runs.txt)).

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh DashTrailArtIntegrator.Apply run1` | 1ª: **exit 1**, `CS0619` (`GetInstanceID` obsoleto no teste novo), em [attempt0](idempotency/attempt0/). Depois da correção: exit 0, material criado, prefab salvo |
| 2 | `run.sh … Apply run2` | exit 0, nada gravado; hashes iguais ao run1 |
| 3 | `run.sh … Validate validate1` | ok no prefab e nas 4 cenas; nenhum arquivo mudou |
| 4 | EditMode filtrado (`DashTrailArtTests`, `RewardFxArtTests`) | 10/10 ([targeted/attempt0](targeted/attempt0/)) |
| 5 | PlayMode filtrado `DashTrailSceneTests`, sem e com GPU | **4/8 e 4/8**: pontos com `emitting = false` ([targeted/attempt0](targeted/attempt0/)). Levou às sondas e à Decisão 1 |
| 6 | Sondas `ZzTrailProbe` (temporárias, removidas antes do commit) | [trail-probe](trail-probe/) |
| 7 | Presenter refeito, `run.sh … Apply run3`, `run4`, `Validate validate2` | prefab salvo (renderer desligado por padrão); depois nada gravado e hashes iguais; Validate ok |
| 8 | PlayMode filtrado, com e sem GPU | 7/8 nos dois: tolerância de alpha do teste (o gradiente guarda 8 bits, 0,5 → 0,502) ([targeted/attempt1](targeted/attempt1/)) |
| 9 | Tolerância de 1/255; PlayMode filtrado com GPU; EditMode filtrado | **8/8** e **10/10** ([targeted/final](targeted/final/)) |
| 10 | Commit `a141586`; suítes completas com `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado | EditMode **349/350** ([editmode](editmode/EditMode.xml)); PlayMode **233 passed, 0 failed, 22 skipped** (`[Explicit]` gráficos e a geometria com GPU) ([playmode](playmode/PlayMode.xml)). A falha EditMode é `GearArtTests.ReapplyingBothItemIntegrators_KeepsGearAndCombatArtByteIdentical` e **é anterior ao V06**: reproduzida isolada no commit `a141586` e na base `main@4a9c93c`, 4/5 nos dois ([gear-baseline](gear-baseline/)). As suítes regravam prefabs, cenas e relatórios de Leadership pelos builders, como no V05 ([side-effects](editmode/side-effects-status.txt)); tudo foi restaurado com `git checkout` |
| 11 | Captura no Editor gráfico (sem `-batchmode`) | **1/1**: 12 capturas e `diagnostics.txt` ([graphical/captures](graphical/captures/)). A etapa "dash ao lado de inimigo" não rodou porque nenhum `EnemyBrain` existe ao carregar a sala inicial. Os esqueletos entram depois e aparecem nas capturas laterais |

`ProjectSettings/ProjectSettings.asset` ganhou `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor, e `EditorBuildSettings.asset` mudou só em fim de linha. É o efeito do pacote de inferência descrito em `docs/BUILD-WINDOWS.md`, não desta tarefa. O diff está em [targeted/projectsettings-side-effect.diff](targeted/projectsettings-side-effect.diff), e os dois arquivos foram restaurados com `git checkout` antes de cada commit.

## Leitura visual (captura real)

Cena de release (`Floor_01_GeneratedCharacters`), sala inicial, 1280×720 e 1920×1080 ([captures](graphical/captures/)):

- **t = 0,06 s** (`*-up-t006.png`): a faixa ainda está escondida atrás do corpo e da capa. A cabeça da textura entra do zero.
- **t = 0,12 s** (`*-up-t012.png`): faixa azul de capa atrás do Hero, com facetas e um fio ciano, legível a 720p e a 1080p. O piso e as juntas das lajes continuam visíveis através dela (Blend Alpha, pico 163/255).
- **Lateral, t = 0,10 s** (`*-right-t010.png`): faixa curta saindo da capa na direção oposta ao dash. Os esqueletos e os telegraphs vermelhos de mira estão no mesmo quadro, com cores bem separadas, mas sem sobreposição direta com a faixa.
- **Cauda** (`*-up-tail.png`, cerca de 0,04 s depois do fim): nada visível. A cauda some antes do limite de 0,16 s.
- **Limpo** (`*-up-clean.png`) e **flash 0** (`*-flash0-t010.png`): nenhum rastro; com flash 0, o dash acontece sem rastro.

Leitura: o rastro aparece de verdade só na segunda metade do dash e é curto (~1–1,5 m na tela, menos que os 2,56 m do contrato), porque a cabeça fica atrás do corpo e a cauda some cedo. Ele cumpre "restrito e legível", mas pode parecer tímido. Ajuste possível em revisão de arte: emissor mais para trás ou `ENV.peak` maior no A07. Sobreposição real com telegraph de perigo não foi capturada.

## Limitações e pendências

- **Aprovação artística humana pendente.** As capturas usam tamanhos emulados do Game view, sem monitor físico.
- **Falha preexistente fora do escopo:** `GearArtTests.ReapplyingBothItemIntegrators_KeepsGearAndCombatArtByteIdentical` falha na `main@4a9c93c`. Reaplicar o ITEMS-GEAR regrava `Ground_Item_CopperAmulet.prefab`, `Ground_Item_LeatherGloves.prefab` e `ItemVisualCatalog.asset`. Para reproduzir: `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter DungeonCrawler.Tests.EditMode.GearArtTests` na main.
- **Perigo sobreposto:** a captura não mostra o rastro passando sobre um telegraph. A legibilidade vem do contrato (alpha e matiz), da captura com inimigos e telegraphs no mesmo quadro e do teste EditMode.
- **Achado fora do escopo, a confirmar:** o `SlashTrail` do combo (`PlayerAttackVfxPresenter`) também para só com `emitting = false`. Pelas sondas, na 6000.6 ele pode continuar gravando pontos entre golpes, enquanto a espada se move na animação. Não foi alterado (fora do escopo). Para reproduzir: na cena de release, golpear uma vez, andar 1 s sem atacar e conferir `PlayerAttackVfxPresenter.SlashTrail.positionCount` ou a captura.
- As sondas e o batchmode usam o mesmo player loop do Editor. O comportamento num build Windows não foi medido aqui.
- A medição de GC é sintética no Editor (GC.Alloc Count, não bytes) e cobre os callbacks do presenter. O TrailRenderer é nativo.
- `DungeonMvpSceneBuilder` e os outros builders carregam e modificam o `Player.prefab`, então o filho `DashTrail` persiste. Se alguém recriar o prefab do zero, precisa rodar de novo `Apply Dash Trail Art (A07)`. Não houve gancho em outros builders, para não ampliar o escopo.
