# V08: impacto do Slam (A10 / FX-SLAM)

- **Base:** `main@a30a30b` ([base-commit.txt](base-commit.txt)), com o V07 já integrado (PR #43). **Branch:** `claude/v08-slam-impact`.
- **Commit testado:** `f555da9`, a integração ([tested-commit.txt](tested-commit.txt)). O commit seguinte traz só estes documentos, o manifesto, o `NEXT_STEPS.md` e o `MVP.md`.
- **Unity:** 6000.6.9f1. Batchmode com e sem `-nographics`, e Editor gráfico (sem `-batchmode`) para as capturas.
- **Executor:** Claude, por pedido explícito do usuário, embora o catálogo atribua a V08 ao Codex. O escopo seguiu o catálogo: evento de apresentação depois da resolução, presenter reutilizado, integrador e testes. BossData, fases, telegraph e dano não mudaram.

## Critérios de aceite

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Ataque cancelado antes da resolução não emite impacto tardio | **Atendido.** O evento só existe dentro de `ResolveAttack`, então não há nada agendado para cancelar. Quatro Wardens em windup ao mesmo tempo: transição a 50%, morte do Boss e morte do alvo cancelam o aviso; 1,5 s depois do fim original do windup os três têm 0 resoluções, 0 apresentações e 0 partículas. O quarto (golpe letal) resolve uma vez e o Boss volta ao Idle. Depois da transição, o próximo impacto vem de um aviso novo de 1,0 s que começou depois do cancelamento. Pausa durante o efeito congela e não gera outro | `SlamImpactPlayModeTests.CancelledWindups_…`, `FramesDurationOrientation_…` |
| Efeito não esconde aviso/player nem muda alcance ou número de danos | **Atendido.** Em todo quadro dos testes, aviso ativo e impacto visível nunca coexistem (0 quadros): o `telegraph.Hide()` acontece antes da resolução e o efeito (0,35 s) acaba antes do `recoverTime` (0,8 s). Com GPU, o efeito não altera nenhum dos 3.371 pixels do Hero e altera 16.973 pixels do piso (material sem escrita de profundidade, quad a 0,04 m). Dano e alcance foram comparados com um baseline sem presenter, Boss a Boss: dentro (2,0 m) 1/1 golpe, borda (3,45 m) 1/1, fora (3,6 m) 0/0, invulnerável 0/0, mesma vida e mesmo raio. Com o dash real, a resolução cai nos i-frames dentro do raio (2,55 m de 3,5 m): 0 dano e 1 impacto; parado, 1 dano e 1 impacto | `DamageAndRange_…` ([slam-damage-baseline.txt](targeted/final/nographics/perf/slam-damage-baseline.txt)), `Render_…` ([slam-impact-render.json](targeted/final/graphics/perf/slam-impact-render.json)), `SlamImpactSceneTests.RealDash_…` ([slam-dash-arena.txt](targeted/final/nographics/perf/slam-dash-arena.txt)) |
| Duração/frames/reset corretos e instância reutilizada | **Atendido.** Vida de 0,35 s; com passos de simulação exatos, o quad assado mostra o quadro k no meio de cada janela de 0,04375 s (0 → 7, linha a linha a partir do topo) e some em 0,36 s. O topo da célula aponta para +Z e a direita para +X. O lado assado é 7,7241 m na fase 1 e 10,0414 m na fase 2. Segunda ativação reinicia do quadro 0 no novo centro, sem segunda partícula. Pausa congela vida e quadro; desativação, morte do Boss e flash 0 apagam na hora. Um único `ParticleSystem` por Boss serve aos dois golpes das duas fases; 1.000 apresentações aquecidas: 0 alocações de GC e nenhum `ParticleSystem` criado | `FramesDurationOrientation_…` ([slam-frames.txt](targeted/final/nographics/perf/slam-frames.txt)), `Phases_…`, `WarmPresentations_…` ([slam-impact-cost.json](targeted/final/nographics/perf/slam-impact-cost.json)) |
| Transição única a 50%, 3 s invulnerável, aviso mínimo de 1 s | **Preservado.** Uma transição só, invulnerável por 3 s (dano recusado), nenhum golpe nela. Avisos usados: 1,4 s na fase 1 e 1,0 s na fase 2, ambos ≥ 1 s, medidos no evento | `Phases_…`, `CancelledWindups_…` |
| Visual observado no Editor | **Observado no Editor gráfico; aprovação artística pendente.** Veja [Leitura visual](#leitura-visual-captura-real). Build Windows não executado | [graphical/captures](graphical/captures/) |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Boss/BossBrain.cs` | Só o evento `SlamResolved(Vector3 centro, float raio)`. Em `ResolveAttack`, centro e raio são capturados antes de `ResolveSlam()` (os mesmos valores que ele usa) e o evento é disparado no fim, depois de cooldown, `recoverEndsAt` e estado, para a apresentação não conseguir adiar nem repetir a resolução. `ResolveSlam`, tempos, raios e o telegraph não mudaram |
| `Assets/_Project/Scripts/Boss/BossSlamImpactPresenter.cs` (novo) | Só apresentação. Escuta `SlamResolved`, `Health.Died` do Boss e `GameOptionsDefinition.Changed`. Por resolução: `Stop(StopEmittingAndClear)` + `Play` + `Emit(1)` no centro (y = 0,04 m), lado = raio × 2,206897, alpha = opção de flash. Apaga em morte, `OnDisable` e flash 0. Delegates criados uma vez em `Awake` |
| `Assets/_Project/Scripts/Editor/SlamImpactArtIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/Feedback/Apply Slam Impact Art (A10)` e `Validate …`. Lê `FX-SLAM.timing.json`, confere grade, quadros uniformes, vida = duração, partícula única, World/Local, Horizontal Billboard, pivô central e anel ≤ 1 R, e compara os raios 3,5/4,55 m e os lados 7,724/10,041 m com `Boss_Warden_Data.asset`. Também exige duração < `recoverTime`. Reutiliza `EnsureImport`/`EnsureMaterial` do V05. Idempotente |
| `Assets/_Project/Materials/SlamFx/Mat_FxSlam.mat` (novo) | URP/Particles/Unlit, transparente, Blend Alpha, branco, sem escrita de profundidade e sem emissão, compartilhado |
| `Assets/_Project/Prefabs/Boss/Boss_Warden.prefab` | Filho `SlamImpactFx` direto na raiz (fora do `Visual`, que pulsa na transição), na camada Enemy: `ParticleSystem` (1 partícula, sem loop, sem `playOnAwake`, emissão e shape desligados, semente fixa, World, escala Local, tempo escalado, vida 0,35 s, Texture Sheet 4×2 Whole Sheet linear 0 → 1 em 1 ciclo, Horizontal Billboard, sem sombras/probes, `maxParticleSize` 10) e o presenter. O `Boss_Warden_Generated.prefab` da release é variante e herda o filho sem mudar o arquivo. Nenhuma cena mudou: o Boss nasce dos encontros |
| `Assets/_Project/Tests/EditMode/SlamImpactArtTests.cs` (novo) | 6 testes: contrato × BossData, sheet real (nada além de 1 R, centro aberto, quadro 7 vazio), material, prefabs, reaplicação idempotente com `Validate`, instalação num Boss sem o filho |
| `Assets/_Project/Tests/PlayMode/SlamImpactPlayModeTests.cs` (novo) | 6 testes com os prefabs reais (release e base): fases, cancelamentos, baseline de dano/alcance, quadros/orientação/reset/flash, GC e reuso, render (só com GPU) |
| `Assets/_Project/Tests/PlayMode/PlayerInput/SlamImpactSceneTests.cs` (novo) | Dash real pelo teclado na MVP_Arena e a captura `[Explicit]` 720p/1080p |

## Decisões e divergências

1. **Horizontal Billboard usa o tamanho como diagonal (medido).** O primeiro passe do teste de quadros mediu o quad assado com 5,4618 m de lado para `startSize` 7,7241 (= 7,7241/√2), alinhado aos eixos ([targeted/attempt2](targeted/attempt2/)). No mesmo teste, o Billboard comum dá lado = `startSize` (10,9236 para 10,9236), então os efeitos do V05 não são afetados. O presenter emite `startSize` = lado × √2 (campo serializado `startSizePerSide`, escrito pelo integrador), e o teste confere o lado assado, não o `startSize`.
2. **Altura absoluta de 0,04 m.** O contrato compara a altura com as do telegraph (0,03/0,05), que são absolutas em `BossAttackTelegraph`. O efeito segue a mesma convenção: x/z do Boss, y = 0,04.
3. **Opção de flash: alpha × flash.** O impacto é rescaldo cosmético, como os efeitos do V05/V06, e não sinal de ameaça (diferente da cauda do projétil no V07). Com flash 0 nada aparece e o que estiver no chão é apagado; o evento continua sendo contado. O efeito não acrescenta flash nem shake e não toca `DamageFlash` nem `CinemachineImpulseSource`.
4. **Transição não apaga um impacto em curso.** O contrato manda apagar em morte, reset, desativação e retorno de pool. Uma transição que começa durante os 0,35 s do rescaldo de um golpe já resolvido não está na lista; o efeito termina sozinho.
5. **Prefab criado pelo integrador.** A ficha do manifesto sugeria `FX-SLAM.prefab` em staging; a integração pedida usa um filho no prefab do Boss, como o V07 no projétil, sem prefab novo.

## Comandos e resultados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. [idempotency/run.sh](idempotency/run.sh) roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([runs.txt](idempotency/runs.txt)). [runtests.sh](runtests.sh) roda `-runTests` com `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado ([test-runs.txt](test-runs.txt)). Os logs (`*.log`) não foram commitados; os XML, JSON e TXT foram.

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh SlamImpactArtIntegrator.Apply run1` | 1ª tentativa: erro de compilação no teste (`phase2SlamTelegraph` é privado; o teste passou a usar `Phase2SlamTelegraph`). Depois, exit 0: material e prefab gravados, importação inalterada (a `.meta` do A10 já seguia o template) |
| 2 | `Apply run2`, `Validate validate1` | run2 não grava nada (hashes iguais, só o arquivo de teste editado entre as execuções); `Validate` ok nos 2 prefabs |
| 3 | EditMode filtrado `SlamImpactArtTests` | 6/6 ([targeted/attempt0](targeted/attempt0/)) |
| 4 | PlayMode `SlamImpactPlayModeTests`, sem GPU | 4 passed, 1 failed, 1 skipped ([attempt1](targeted/attempt1/)): lado do quad 5,46 m. Diagnóstico com os vértices assados em [attempt2](targeted/attempt2/), que levou à Decisão 1 |
| 5 | Fator √2; `Apply run3`, `Apply run4`, `Validate validate2` | run3 grava o prefab (campo novo); run4 não grava nada e os hashes são iguais; `Validate` ok |
| 6 | PlayMode `SlamImpactPlayModeTests`, sem GPU | 5 passed, 0 failed, 1 skipped (render) ([attempt3](targeted/attempt3/)) |
| 7 | PlayMode `SlamImpactSceneTests`, sem GPU | 1 passed, 1 skipped (captura) ([attempt4](targeted/attempt4/)) |
| 8 | Rodada final: EditMode e as duas classes sem e com GPU | **EditMode 6/6**; **sem GPU 6 passed, 0 failed, 2 skipped** (render e captura); **com GPU 7 passed, 0 failed, 1 skipped** (captura) ([targeted/final](targeted/final/)) |
| 9 | Editor gráfico: `SlamImpactSceneTests.Capture_…` | **1/1**: 24 capturas e [diagnostics.txt](graphical/captures/diagnostics.txt) |
| 10 | Commit `f555da9`; suítes completas sem GPU | **EditMode 362/362** ([editmode](editmode/EditMode.xml)); **PlayMode 247 passed, 0 failed, 26 skipped** (`[Explicit]` gráficos e testes só com GPU, incluindo o render e a captura do V08) ([playmode](playmode/PlayMode.xml)). As suítes regravam prefabs, cenas e relatórios de Leadership pelos builders, como no V05–V07 ([side-effects](editmode/side-effects-status.txt)); o `Boss_Warden.prefab` **não** estava entre eles. Tudo foi restaurado com `git checkout` antes do PlayMode e do commit |

`ProjectSettings/ProjectSettings.asset` ganhou `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor, e `EditorBuildSettings.asset` mudou só em fim de linha. É o efeito conhecido do pacote de inferência (`docs/BUILD-WINDOWS.md`), não desta tarefa ([diff](idempotency/projectsettings-side-effect.diff)); os dois arquivos foram restaurados antes de cada commit.

## Custo (medido, sem promessa de FPS)

- 1 `ParticleSystem` e 1 partícula por Boss; nenhum objeto criado por golpe.
- GC: **0 alocações** (`GC.Alloc` do profiler) em 1.000 apresentações aquecidas; o controle `new byte[4096]` registrou 1 ([slam-impact-cost.json](targeted/final/nographics/perf/slam-impact-cost.json)).
- Uma draw call transparente enquanto o quad vive (0,35 s), a cada ≥ 2,4 s de luta (0,8 s de recuperação + 1,6 s de cooldown mínimo). Tempo de GPU e overdraw do quad de até 10 m não foram medidos.

## Leitura visual (captura real)

MVP_Arena (Hero do `Player.prefab`, a cápsula do MVP) com o `Boss_Warden_Generated` da release 2,2 m à frente, Hero invulnerável e parado, 1280×720 e 1920×1080, pausado no instante de cada captura ([captures](graphical/captures/)):

- **Aviso** (`*-phase1-warning.png`, progresso 0,8): o disco vermelho existente, sem nenhum impacto (`visible=False`).
- **Impacto, fase 1** (`*-phase1-impact-t05/t13/t22/t31.png`): anel fino de poeira cinza-clara com borda laranja apagada, do tamanho do aviso, e lascas de pedra entre o Warden e a borda. O centro sob o Warden fica aberto e o piso continua legível. O Hero e o Warden aparecem inteiros por cima do anel. Em t = 0,31 s (quadro 7, transparente pelo contrato) o chão já está vazio, embora a partícula ainda viva até 0,35 s.
- **Impacto, fase 2** (`*-phase2-impact-*.png`): o mesmo efeito 30% maior (raio 4,55 m), com o Warden na cor da fase 2.
- **Depois** (`*-after.png`): nada no chão (`visible=False`).

Leitura: o anel marca a borda real do golpe e se distingue do aviso (sem vermelho saturado). As lascas são pequenas na cápsula do MVP. Na fase 2 a borda laranja apagada lê pouco sobre a pedra clara. Isso fica para a revisão de arte.

## Limitações e pendências

- **Aprovação artística humana pendente.** As capturas usam tamanhos emulados do Game view, sem monitor físico, e a MVP_Arena, não a sala do Boss da release (a luta real é a mesma: o Boss nasce do mesmo prefab). Build Windows não executado nesta tarefa.
- **Builder recria o prefab.** `DungeonFloorSceneBuilder.BuildBossPrefab` salva um `Boss_Warden.prefab` novo do zero, o que apagaria o `SlamImpactFx`. Não foi alterado (fora do escopo). Quem rodar o builder precisa rodar de novo `Apply Slam Impact Art (A10)`; o `Validate` acusa a falta. Para reproduzir: `Tools/Dungeon Crawler/Build/Floor Scene (M2)` e depois `Validate Slam Impact Art (A10)`.
- **Hero real só no teste de dash.** As provas de fase, cancelamento e baseline usam um `Health` simples como alvo, com a navegação do Warden desligada; o dash real e a captura usam o Hero do `Player.prefab`.
- **Mipmaps e distância.** O anel fino pode perder detalhe em mips altos; não foi comparado em monitores reais.
