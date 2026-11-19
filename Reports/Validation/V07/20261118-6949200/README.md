# V07: núcleo e cauda do projétil (A08 / FX-PROJECTILE e A09 / FX-PROJECTILE-TRAIL)

- **Base:** `main@6949200` ([base-commit.txt](base-commit.txt)), com o V06 já integrado (PR #42). **Branch:** `claude/v07-projectile`.
- **Commit testado:** `e6ad537`, a integração ([tested-commit.txt](tested-commit.txt)). O commit seguinte traz só estes documentos, o manifesto, o `NEXT_STEPS.md` e o `MVP.md`.
- **Unity:** 6000.6.9f1. Batchmode com e sem `-nographics`, e Editor gráfico (sem `-batchmode`) para as capturas.
- **Executor:** Claude, por pedido explícito do usuário, embora o catálogo atribua a V07 ao Codex. O escopo seguiu o catálogo: filho e material visual, presenter, integrador, callback mínimo de reset e testes.
- **Pré-requisito:** quando a tarefa começou, o V06 ainda não estava na `main`. A execução esperou o merge do PR #42 e a branch foi recriada a partir de `main@6949200`.

## Critérios de aceite

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Rajada de 16 usa instâncias aquecidas distintas | **Atendido.** O pool real aquece 16 instâncias do `Projectile.prefab`. Uma rajada de 16 usa as 16, sem nenhuma criada. A segunda rajada, 40 m adiante e no sentido oposto, reutiliza as mesmas 16 | `ProjectileArtPlayModeTests.Burst16_…` em [targeted/final-rerun](targeted/final-rerun/) |
| Nenhum rastro de tiro anterior | **Atendido.** Na segunda rajada, todos os pontos de todas as caudas estão a z > 35 m, ou seja, nenhum ponto da primeira rajada (z ≤ 3 m). Retorno por parede, porta, timeout e desativação do pool deixa a cauda desligada com 0 pontos. Depois de reativar o pool, o primeiro tiro não traz resto. No Ranged real, a cauda nasce a menos de 1,5 m da origem F07 | `Burst16_…`, `ReturnByWallDoorTimeoutAndPoolDisable_…`, `F07_…`, `ProjectileArtSceneTests.Release_GeneratedRanged…` |
| Dano, colisão e lifetime iguais ao baseline | **Atendido.** O baseline é a esfera do `DungeonFloorSceneBuilder` (escala 0,4, máscara Player/Obstacle/Door, raio 0,25, 4 s). Com passo fixo de 1/60 s, nas pistas jogador, parede, porta e Enemy-antes-da-parede, os dois passes param no mesmo z, voltam no mesmo quadro e causam o mesmo dano (7, uma vez). O Enemy, que está fora da máscara, é atravessado pelos dois. O lifetime termina no mesmo quadro (4,000 s). O prefab mantém hitMask 12800, raio 0,25 e lifetime 4, sem collider nem Rigidbody | [perf-graphics/projectile-art-baseline.txt](targeted/final-rerun/perf-graphics/projectile-art-baseline.txt), `DamageCollisionAndLifetime_…`, `ProjectileArtTests.ProjectilePrefab_…` |
| Pausa e reinício limpam ativos | **Atendido, com interpretação.** A pausa (timeScale 0) congela o orbe e a cauda: mesma posição e mesmos pontos depois de 0,3 s reais, e ao despausar não há salto. A pausa **não** descarta ameaças, porque isso mudaria o jogo; congelar é a regra do F07. Reiniciar a partida pelo `GameRestarter` da cena de release destrói o pool anterior e deixa o novo com 0 ativos e as 16 caudas desligadas | `Pause_…`, `Release_PauseFreezesActives_RestartLeavesNoActiveProjectileOrTrail` |
| Visão F07, parede, porta e jogador | **Atendido.** Com o prefab real: tiro do Ranged com visão livre atinge o alvo (dano 5). Cobertura Obstacle ou Door recusa o windup sem projétil. Uma porta fechando durante o voo absorve o tiro, sem dano, e a cauda some junto. Na cena de release, o `Enemy_Ranged_Generated` atira pelo pool real, acerta o Hero e não cria instância | `F07_…`, `Release_GeneratedRanged…`; diagnóstico gráfico F07 em [graphical/f07](graphical/f07/) |
| Rajada acima da capacidade medida, não mascarada | **Atendido.** 20 tiros simultâneos criam exatamente 4 instâncias além das 16 (o `ObjectPool` cresce até `maxSize` 64), em 0,25 ms. Numa segunda rajada de 20, nenhuma instância nova | [projectile-art-cost.json](targeted/final-rerun/perf-graphics/projectile-art-cost.json) |
| Visual observado no Editor/Windows | **Observado no Editor gráfico; aprovação artística pendente.** Veja [Leitura visual](#leitura-visual-captura-real). Build Windows não executado | [graphical/captures](graphical/captures/) |
| Custo registrado sem promessa de FPS | **Registrado.** Veja [Custo](#custo-medido-sem-promessa-de-fps) | [projectile-art-render.json](targeted/final-rerun/perf-graphics/projectile-art-render.json), [projectile-art-cost.json](targeted/final-rerun/perf-graphics/projectile-art-cost.json) |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Systems/Projectile.cs` | Callback mínimo: o evento `Launched`, disparado no fim de `Launch`, depois de posição e rotação. Nenhuma linha de voo, colisão, dano ou lifetime mudou |
| `Assets/_Project/Scripts/Systems/ProjectileVisualPresenter.cs` (novo) | Só apresentação. Liga a cauda limpa em `Launched` (`enabled`, `Clear()`, `emitting`) e a apaga em `OnEnable`/`OnDisable`, que cobrem pool.Get, retorno por colisão/timeout/ForceRelease, desativação do pool e troca de cena. Inscrito uma vez em `Awake`; não aloca por tiro e ignora a opção de flash (a cauda é sinal de ameaça) |
| `Assets/_Project/Scripts/Editor/ProjectileArtIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/Feedback/Apply Projectile Art (A08-A09)` e `Validate …`. Lê os dois contratos `Source~` e o relatório do gerador, confere a malha real do FBX (156 triângulos, 1 submalha, tamanho e pivô em metros, raio ≤ 0,2 m) e recusa divergências. Também confere os limites da cauda (≤ 0,15 s, ≤ 0,6 m a 12 m/s, largura ≤ 75% do orbe). Reutiliza `EnsureImport`/`EnsureMaterial` do V05 e `ReadWidthCurve` do V06 |
| `Assets/_Project/Materials/ProjectileFx/Mat_FxProjectileCore.mat` (novo) | URP/Lit opaco, Base/Emission 16×16, emissão (1, 1, 1), metallic 0,2, smoothness 0,35, compartilhado |
| `Assets/_Project/Materials/ProjectileFx/Mat_FxProjectileTrail.mat` (novo) | URP/Particles/Unlit, transparente, Blend Alpha, branco, tiling (1, 1), porque U = 0 está na cabeça |
| `Assets/_Project/Prefabs/Projectiles/Projectile.prefab` | Filho `Visual` (escala 2,5 sob a raiz 0,4, com sombra e probes como a esfera) e filho `Trail` (z −0,35 local = 0,14 m atrás do centro; 0,05 s × 0,26 m, curva 1 → 0,8, 0,05 m, Stretch, View, 0/0 vértices, sem sombras/probes, **desligado por padrão**). Presenter na raiz. O MeshRenderer da esfera foi desligado, não removido. A raiz continua em 0,4 e o `ProjectilePool.prefab` não mudou. As 5 cenas com pool herdam a mudança |
| `Assets/_Project/Tests/EditMode/ProjectileArtTests.cs` (novo) | 6 testes: malha × envelope/pivô, PNG × contrato, prefab × regras preservadas, materiais, reaplicação idempotente com `Validate`, instalação sobre a esfera de baseline |
| `Assets/_Project/Tests/PlayMode/ProjectileArtPlayModeTests.cs` (novo) | 7 testes com o prefab real: rajada, retornos, baseline, pausa, F07, crescimento e GC, contadores de render (só com GPU) |
| `Assets/_Project/Tests/PlayMode/PlayerInput/ProjectileArtSceneTests.cs` (novo) | 2 testes na cena de release (Ranged gerado; pausa e reinício) e a captura `[Explicit]` 720p/1080p |

## Decisões e divergências

1. **Escala e pivô lidos do contrato.** A raiz fica em 0,4, como no baseline e no builder, e o `Visual` usa 1/0,4 = 2,5. Assim o orbe mede 0,386 × 0,374 × 0,375 m no mundo, dentro do envelope de 0,4 m, conferido no EditMode. A cauda fica a 0,14 m (contrato) ÷ 0,4 = 0,35 local. A importação do FBX espelha X (FBX destro para Unity); o integrador compara tamanho e |centro|, que não mudam com isso.
2. **GI de emissão: o contrato pede None, o material usa RealtimeEmissive (medido).** Com flags None, o `_EMISSION` desaparecia a cada reimportação do `.mat` e o arquivo mudava a cada execução: `Validate` falhava com "emissão desligada" ([idempotency/attempt0](idempotency/attempt0/)). Com `RealtimeEmissive`, como os materiais emissivos do ITEMS-COMBAT, a keyword persiste e o Apply fica idempotente. Nada é assado em lightmap.
3. **Um único evento como callback de reset.** O retorno ao pool sempre desativa o objeto (`actionOnRelease`), então `OnDisable` do presenter cobre colisão, timeout, ForceRelease, desativação do pool e destruição. Por isso não houve gancho em `OnDespawned`. O `Clear()` acontece depois do teleporte, como pede o contrato. O renderer fica desligado fora do voo por causa do achado do V06: na 6000.6, `emitting = false` ainda grava pontos.
4. **Pausa não descarta projéteis.** "Pausa/reinício limpam ativos" foi lido como: a pausa congela (o orbe não anda e a cauda nem cresce nem encolhe) e o reinício limpa. Descartar ameaças na pausa mudaria a regra do F07 ("pausa congela os tempos").
5. **Opções visuais.** A cauda não usa a opção de flash: o contrato diz que é "sinal estável de ameaça, não flash". Shake não se aplica.

## Comandos e resultados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. [idempotency/run.sh](idempotency/run.sh) roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([runs.txt](idempotency/runs.txt)). Os logs (`*.log`, 7,7 MB) não foram commitados; os XML, JSON e TXT foram.

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh ProjectileArtIntegrator.Apply run1` | exit 0: 2 materiais criados, prefab salvo, 0 importações ajustadas |
| 2 | `run.sh … Apply run2`, `Validate validate1` | 1º: material regravado e `Validate` **exit 1** ("emissão desligada"), em [attempt0](idempotency/attempt0/). Levou à Decisão 2 |
| 3 | GI para RealtimeEmissive; `Apply run2`, `Apply run3`, `Validate validate1` | run2 grava o material com `_EMISSION`; run3 não grava nada e os hashes são iguais; `Validate` ok no prefab, no `ProjectilePool.prefab` e em 5 pools de 6 cenas (a `MVP_Arena` não tem pool) |
| 4 | EditMode filtrado `ProjectileArtTests` | 6/6 ([targeted/attempt0](targeted/attempt0/)) |
| 5 | PlayMode filtrado, duas classes, sem GPU | **Travou** (encerrado após 20 min) ([attempt0](targeted/attempt0/)). Causa: a captura `[Explicit]` roda quando o filtro nomeia a classe. Em batchmode ela é ignorada antes de carregar cena, e o `TearDown` destruía as raízes da InitTestScene, inclusive o executor dos testes |
| 6 | Classe do pool sozinha, sem GPU | 6/6 + 1 ignorado (GPU) ([attempt1](targeted/attempt1/)) |
| 7 | `TearDown` restrito à cena de gameplay; classe de cena | 1/2: o reinício não recarregava ([attempt2](targeted/attempt2/)). Causa do ambiente de teste: a pasta isolada de checkpoint não existia, `File.Delete` falhava e `BeginNew` abortava o `GameRestarter`. A pasta passou a ser criada no `Setup` |
| 8 | Classe de cena | 2/2 + captura ignorada ([attempt3](targeted/attempt3/)) |
| 9 | As duas classes, sem e com GPU | sem GPU 8/8 + 2 ignorados; com GPU, travou no teste de render ([attempt4](targeted/attempt4/)): em batchmode, `WaitForEndOfFrame` não dispara. O teste passou a usar `camera.Render()` explícito |
| 10 | Teste de render sozinho, com GPU | 1/1 ([attempt5](targeted/attempt5/)) |
| 11 | Rodada final: as duas classes sem e com GPU, e EditMode | **sem GPU 8 passed, 0 failed, 2 skipped** (render sem GPU e captura); **com GPU 9 passed, 0 failed, 1 skipped** (captura); **EditMode 6/6** ([targeted/final-rerun](targeted/final-rerun/)) |
| 12 | Commit `e6ad537`; suítes completas sem GPU, com `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado | **EditMode 356/356** ([editmode](editmode/EditMode.xml)); **PlayMode 241 passed, 0 failed, 24 skipped** (`[Explicit]` gráficos e testes só com GPU) ([playmode](playmode/PlayMode.xml)). As suítes regravam prefabs, cenas e relatórios de Leadership pelos builders, como no V05/V06 ([side-effects](editmode/side-effects-status.txt)). O `Projectile.prefab` **não** estava entre eles. Tudo foi restaurado com `git checkout` |
| 13 | Editor gráfico: `ProjectileArtSceneTests.Capture_…` | **1/1**: 8 capturas e [diagnostics.txt](graphical/captures/diagnostics.txt) |
| 14 | Editor gráfico: `RangedLineOfSightReleaseTests.Capture_ReleaseRangedBehindDoorAndFiring` (F07 existente) | **1/1**: porta bloqueia, windup e tiro na release com o prefab novo ([graphical/f07](graphical/f07/)). No quadro `door-open-projectile.png` o projétil já não aparece; a prova funcional são as asserções, não a imagem |

`ProjectSettings/ProjectSettings.asset` ganhou `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor, e `EditorBuildSettings.asset` mudou só em fim de linha. É o efeito conhecido do pacote de inferência (`docs/BUILD-WINDOWS.md`), não desta tarefa. Os diffs estão em [idempotency](idempotency/projectsettings-side-effect.diff), [playmode](playmode/projectsettings-side-effect.diff) e [graphical](graphical/projectsettings-side-effect.diff), e os dois arquivos foram restaurados antes de cada commit. Também foram removidas três `Assets/InitTestScene*.unity` deixadas pelos runs encerrados à força.

## Custo (medido, sem promessa de FPS)

- **Render, D3D12, 16 orbes parados em grade, câmera 1280×720, média de 30 quadros** ([render.json](targeted/final-rerun/perf-graphics/projectile-art-render.json)):

  | Cenário | Draw calls | SetPass | Triângulos | Vértices |
  | --- | --- | --- | --- | --- |
  | Câmera vazia | 7 | 7 | 1.686 | 5.058 |
  | Esfera de baseline | 39 | 9 | 26.262 | 21.538 |
  | Arte nova | 54,5 | 10 | 6.709 | 17.408 |

  O núcleo tem 156 triângulos contra 768 da esfera (2 passes com sombra). As caudas acrescentam cerca de uma draw call por TrailRenderer ativo (+15,5) e 1 SetPass. Não houve medição de tempo de GPU nem conclusão de FPS.
- **CPU e memória** ([cost.json](targeted/final-rerun/perf-graphics/projectile-art-cost.json)):
  - Aquecimento do pool: 0,69 ms (arte), contra 0,45 ms (esfera).
  - Primeira rajada de 16: 0,10 ms, contra 0,08 ms.
  - Rajada de 20 acima da capacidade: 0,25 ms.
  - Prefab: 3 transforms e 10 componentes, contra 1 e 4.
  - GC: **0 alocações** (`GC.Alloc` do profiler) em 2.000 tiros aquecidos `Fire`/`Launch`/`ForceRelease`, com presenter e cauda; o controle positivo `new byte[4096]` registrou 1.
  - Os bytes de GC gerenciados do aquecimento e do excesso deram 0, mas o `Instantiate` aloca memória nativa, que esse contador não vê.

## Leitura visual (captura real)

Cena de release (`Floor_01_GeneratedCharacters`), sala inicial, Hero invulnerável e parado, 1280×720 e 1920×1080 ([captures](graphical/captures/)):

- **Voo, t = 0,12 s** (`*-flight-t012.png`): quatro projéteis convergindo no Hero (cima, direita, esquerda e diagonal), a 11 m/s. Cada orbe aparece como ferro escuro facetado com as fendas "spine" e "brow" em laranja emissivo. A cauda é uma gota laranja curta e sempre aponta para trás do movimento nos quatro rumos. A sombra no piso marca a posição no chão, como na esfera.
- **Pausado** (`*-paused.png`): idêntico ao quadro de voo. O diagnóstico confirma os mesmos pontos e o mesmo comprimento.
- **Depois do impacto** (`*-after-impact.png`): nenhum orbe nem cauda (`visible=False`, 0 pontos).
- **Lateral, t = 0,30 s** (`*-side-t030.png`): orbe com cauda à esquerda, junto ao vão da parede inferior. É a mesma instância reutilizada (`launches=2`), sem faixa até o tiro anterior.

Leitura: a ameaça se lê pelas fendas e pela cauda laranja (#FF9A2E), distintas do azul do rastro de dash e do branco do combo. O corpo de ferro (#3E4152) tem pouco contraste com o piso, de propósito no A08, e a esfera laranja anterior era mais visível de longe. Isso é para a revisão de arte.

## Limitações e pendências

- **Aprovação artística humana pendente.** As capturas usam tamanhos emulados do Game view, sem monitor físico. Build Windows não executado nesta tarefa.
- **Comprimento da cauda depende do FPS.** O contrato prevê 0,6 m (0,05 s × 12 m/s). Nas capturas do Editor gráfico, o diagnóstico mediu 0,64–1,24 m, porque o TrailRenderer sempre liga o último ponto à posição atual, e com quadros longos esse segmento cresce. O FPS do Editor não foi medido. Se a revisão achar longa, há duas saídas no A09: reduzir `time_s` ou usar `minVertexDistance` menor.
- **Builder recria o prefab.** `DungeonFloorSceneBuilder.BuildProjectilePrefab` salva um `Projectile.prefab` novo do zero, o que apagaria `Visual`, `Trail` e o presenter. Não foi alterado (fora do escopo) e nenhum teste o executa (o prefab não mudou nas suítes). Quem rodar o builder precisa rodar de novo `Apply Projectile Art (A08-A09)`; o `Validate` acusa a falta. Para reproduzir: `Tools/Dungeon Crawler/Build/Floor Scene (M2)` e depois `Validate Projectile Art (A08-A09)`.
- **Pausa congela, não remove.** Veja a Decisão 4. Se o produto quiser limpar projéteis na pausa, isso muda a regra do F07 e precisa de decisão própria.
- **Telegraph do Warden.** A legibilidade sobre o telegraph do Boss não foi capturada (o contrato sugere). Ficou coberta só pelo contrato de cor.
- A medição de GC é sintética no Editor (contagem de `GC.Alloc`, não bytes). O TrailRenderer é nativo.
