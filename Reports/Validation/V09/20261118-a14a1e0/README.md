# V09: modelo e tampa do baú (A11 / MODEL-CHEST)

- **Base:** `main@a14a1e0` ([base-commit.txt](base-commit.txt)), com o V08 já integrado (PR #44). **Branch:** `claude/v09-chest-model`.
- **Commit testado:** `6ee0c87`, a integração ([tested-commit.txt](tested-commit.txt)). O commit seguinte traz só estes documentos, o manifesto, o `NEXT_STEPS.md` e o `MVP.md`.
- **Unity:** 6000.6.9f1, batchmode com e sem `-nographics`. As capturas são renders fora da tela (RenderTexture) da câmera real do jogo e de uma câmera lateral, em PlayMode com GPU; não são o Game view do Editor gráfico.
- **Executor:** Claude, por pedido explícito do usuário, embora o catálogo atribua a V09 ao Codex. O escopo seguiu o catálogo: filho visual, referência da tampa, integrador dedicado e testes. `RoomRewardChest`, `RoomRewardPresenter`, recompensas, interação e tabelas não mudaram.

## Critérios de aceite

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Tampa abre corretamente sem atravessar base ou duplicar animação | **Atendido.** Dono único: o `RoomRewardPresenter` existente, que já aplica `Euler(−105, 0, 0)` quando `IsOpened`. É exatamente a pose final do take `Open` importado (medido no clip: < 0,5°), então o presenter não mudou. O take não é tocado: não há `Animator`/`Animation` em nenhum baú (Validate e testes). Com o `Visual` a Y 180°, −105° no eixo X do modelo ergue a frente e a tampa repousa atrás da dobradiça (+Z do baú, longe da câmera e do `dropOrigin`). Nenhum vértice da tampa fica abaixo do topo das paredes e à frente da traseira da base nas duas poses (fora da zona coaxial de 3,5 cm da dobradiça); a frente sobe > 0,6 m acima do eixo. Em PlayMode, a pose fica constante por 10 quadros depois da abertura e a tampa aberta não toca parede/porta nem os pickups | `ChestModelArtTests.Contract_…`, `ClosedAndOpenPoses_…`; `ChestModelArtSceneTests.FiveRooms_…`; capturas `*-side.png` |
| Uma recompensa independente por sala/run; toggle/retomada não renovam loot | **Atendido.** Nas cinco salas, F antes de limpar não abre; depois, cada F abre só o próprio baú com 2 pickups (ouro + espada) criados no `Opened`; os baús das salas seguintes continuam fechados. Desligar/religar o baú mantém aberto, `TryOpen` falso e nenhum pickup novo. Checkpoint: baú 0 aberto com os 2 drops no chão, salvar, voltar ao menu e **Continuar**: mesmos IDs no registro de loot, mesmo estado do RNG, baú 0 aberto e os outros 4 fechados | `FiveRooms_…`, `Checkpoint_…` |
| Visual reflete IsOpened inclusive em checkpoint; cinco salas observadas e builder idempotente | **Atendido.** No primeiro quadro da retomada, ainda pausado, a tampa do baú 0 já está aberta e as outras fechadas. Interação remapeada para G: F não abre e a tampa fica fechada; G abre. Última morte: F não abre, tampa fechada. Bolsa cheia: abre, a espada fica no chão e a tampa continua aberta. A recompensa do Boss abre depois de **Coletar recompensas**. Cinco salas renderizadas fechadas e abertas. `Apply` reexecutado não grava nada (hashes iguais); `RoomRewardSceneBuilder.Integrate` sobre a cena instalada deixa os arquivos idênticos e o A11 válido; `Apply` depois dele também não grava | `Checkpoint_…`, `RemappedInteract_…`, `LastDeath_…`, `Reapply_AndF08Builder_…`, [idempotency](idempotency/runs.txt), [captures](graphical-attempt1/perf/) |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Editor/ChestModelArtIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/Rewards/Apply Chest Model Art (A11)` e `Validate …`. Lê `MODEL-CHEST.source.json` e `ModelChest.validation.json`; confere o FBX importado: 624/204 triângulos, duas submalhas por nó, limites locais (com o espelho de X da importação), `Base` na origem, `Lid` no eixo da dobradiça (0; 0,54; −0,342) com rotação de repouso 0, escala 1, e o take `Open` (0,6 s, sem loop) terminando a < 0,5° de `Euler(−105, 0, 0)`. Cria dois materiais, monta `Visual/Base` e `Visual/Lid` a partir das malhas e da pose importadas, aponta `RoomRewardPresenter.lid` para `Visual/Lid` e desliga (não remove) `Body`/`LidPivot`/`Latch`. Reutiliza `ProjectileArtIntegrator.EnsureCoreImport` para a paleta (já conforme; nada mudou). Idempotente; `Validate` recusa colisor, `Animator`, primitiva ligada ou `lid` errado |
| `Assets/_Project/Materials/ChestArt/Mat_ChestA11_Stone.mat`, `Mat_ChestA11_Brass.mat` (novos) | URP/Lit opaco, `_BaseMap` = `MODEL-CHEST_Palette.png`, branco, metallic/smoothness 0/0,12 e 0,45/0,3 do contrato, sem emissão. Uma instância de cada para os cinco baús |
| `Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity` | Só os cinco `RoomRewardChest_F08*`: filho `Visual` (Y 180°) com `Base`/`Lid` (MeshFilter + MeshRenderer, sombras ligadas, camada do baú), `lid` do presenter → `Visual/Lid`, primitivas desligadas. Dump comparativo da cena da `main` contra a integrada: 945 → 960 objetos, 35 diferentes, todos sob os cinco baús; na raiz de cada baú só mudou `lid` ([scene-dump](scene-dump/scene-diff-objects.txt)) |
| `Assets/_Project/Scripts/Editor/RoomRewardSceneBuilder.cs` | Duas linhas: com o A11 instalado, o builder F08 continua ligando `Visual/Lid` como tampa apresentada, em vez de voltar ao `LidPivot` desligado. Ver Divergência 1 |
| `Assets/_Project/Tests/EditMode/ChestModelArtTests.cs` (novo) | 6 testes: contrato × importação e fim do take; poses vértice a vértice; materiais; cinco baús, referências de recompensa, `dropOrigin`, sem colisor/NavMeshObstacle e footprint dentro do `Body` F08; reaplicação + builder F08 idempotentes com GUIDs preservados; instalação num baú só de primitivas |
| `Assets/_Project/Tests/PlayMode/PlayerInput/ChestModelArtSceneTests.cs` (novo) | 4 testes na cena de release (cinco salas, checkpoint, remapeamento, última morte + bolsa cheia) e a captura `[Explicit]` com GPU |

## Decisões e divergências

1. **Builder F08 tocado (fora da lista de arquivos).** `RoomRewardSceneBuilder.ConfigureRoom` reatribuía `lid` ao `LidPivot` a cada execução, o que desfaria o A11 em silêncio (a tampa visível nunca abriria). O critério "builder idempotente" exigia corrigir isso; a mudança é só a escolha do Transform da tampa. Sem o A11 instalado, o comportamento é o de antes.
2. **Dono da animação: presenter, pose binária.** O take `Open` de 0,6 s fica importado e não é usado. Usá-lo exigiria Animator/controlador e mudar o presenter (disparo no `Opened`, pose final na retomada) — fora do escopo pedido ("presenter apenas se necessário para pivô"). A abertura é instantânea, como no F08. Se a revisão quiser a abertura animada, é uma tarefa nova que troca o dono, sem manter os dois.
3. **Visual montado a partir das malhas, não instância do FBX.** Uma instância do modelo Generic traria um `Animator` (segundo dono potencial). O integrador cria `Base`/`Lid` com as malhas e a pose lidas do FBX; reimportar o FBX atualiza as malhas, e o `Validate` acusa pose divergente.
4. **Sem colisor.** O F08 é não bloqueante (o builder remove os colisores das primitivas) e o FBX não traz colisor; nada foi acrescentado. O modelo fechado cabe no `Body` antigo (1,2 × 0,73 m contra 1,4 × 0,85 m), então cobertura e NavMesh não mudam.
5. **Só os cinco baús do andar 1.** `Floor_02_Fixed.unity` (F14) tem um sexto baú, copiado do andar 1 na criação, que continua de primitivas, com a tampa girando para baixo/trás (achado do A11). Não foi tocado, por estar fora do escopo "cinco baús". Ver pendências.
6. **Achado do F08 resolvido no andar 1.** Com o `Visual` a 180°, os −105° do presenter abrem a tampa para cima. O presenter não mudou, e o `LidPivot` de primitivas está desligado; as duas correções não coexistem.

## Comandos e resultados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. [idempotency/run.sh](idempotency/run.sh) roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([runs.txt](idempotency/runs.txt)). [runtests.sh](runtests.sh) roda `-runTests` com `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado ([test-runs.txt](test-runs.txt)). Os logs (`*.log`) não foram commitados.

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh ChestModelArtIntegrator.Apply run1` | exit 1: `MissingComponentException` (`GetComponent<MeshFilter>() ?? AddComponent` não funciona com o null falso da Unity). Materiais criados; cena não gravada |
| 2 | Correção; `Apply run2` | exit 0: cena gravada (`[V09] … gravado`) |
| 3 | Outra checagem `?.` corrigida; `Apply run3`, `Validate validate1` | run3 `sem mudanças`: hashes iguais a run2, exceto o próprio script editado; `Validate ok` |
| 4 | EditMode filtrado `ChestModelArtTests` | 6/6 ([targeted/attempt0](targeted/attempt0/)) |
| 5 | PlayMode `ChestModelArtSceneTests`, sem GPU | attempt1: 0/4 — a malha do FBX não é Read/Write em runtime; a checagem por vértice passou a usar os 8 cantos dos limites (a prova por vértice continua no EditMode). attempt2–4: falhas do teste, não do produto: o trigger largo do pickup alcança o AABB da base (como já alcançava o `Body` maior), e o jogador perto coleta os drops nos quadros de espera, o que esvaziava o snapshot do checkpoint e a contagem do toggle. Ajustes: tampa × pickups e posição à frente da base; drops registrados no `Opened`; drops afastados antes de qualquer quadro |
| 6 | PlayMode `ChestModelArtSceneTests`, sem GPU | **4 passed, 0 failed, 1 skipped** (captura) ([targeted/attempt5](targeted/attempt5/)) |
| 7 | Captura com GPU (`graphics`) | attempt0 travou: `WaitForEndOfFrame` não dispara em batchmode; processo encerrado após 10 min, nenhum PNG gravado (a pasta `graphical/` só tem o log, que não é commitado). Troca por `yield return null` (o render é explícito) |
| 8 | Captura com GPU, attempt1 | **1/1**: 30 PNG e [context.txt](graphical-attempt1/perf/context.txt) |
| 9 | Dump da cena `main` × integrada (script temporário, não commitado; fonte em [scene-dump](scene-dump/V09SceneDump.cs.txt)) | Diferenças só nos cinco baús |
| 10 | Commit `6ee0c87`; suítes completas sem GPU | **EditMode 368/368** ([editmode](editmode/EditMode.xml)); **PlayMode 251 passed, 0 failed, 27 skipped** (`[Explicit]` gráficos e testes só com GPU, incluindo a captura do V09) ([playmode](playmode/PlayMode.xml)). A EditMode regrava prefabs, cenas e relatórios de Leadership pelos builders, como no V05–V08, incluindo a cena de release ([side-effects](editmode/side-effects-status.txt)). Antes de restaurar, `Validate` na cena regravada: **ok**, o A11 sobrevive aos builders da suíte ([validate-after-editmode-suite](idempotency/runs.txt)). Tudo restaurado com `git checkout` antes do PlayMode e do commit; a PlayMode só tocou `ProjectSettings` ([side-effects](playmode/side-effects-status.txt)) |

`ProjectSettings/ProjectSettings.asset` ganha `SENTIS_ANALYTICS_ENABLED` ao abrir o Editor, e `EditorBuildSettings.asset` muda em fim de linha. É o efeito conhecido do pacote de inferência (`docs/BUILD-WINDOWS.md`), não desta tarefa; os dois foram restaurados com `git checkout` antes de cada execução e de cada commit.

## Leitura visual (render real)

Cena de release, Hero do `Player.prefab` invulnerável a 1,8 m à frente do baú, 1280×720 e 1920×1080 pela câmera do jogo e 1280×720 por uma câmera lateral a ~3 m ([captures](graphical-attempt1/perf/)):

- **Lateral fechado** (`1280-room-N-closed-side.png`): caixa baixa de pedra em blocos, plinto escuro, cintas, aro e pega de latão, fechadura na frente (lado da câmera do jogo). Tampa assentada, sem fresta.
- **Lateral aberto** (`1280-room-N-open-side.png`): a tampa gira na dobradiça traseira, fica pouco além do vertical, mostra a face de baixo escura e o interior oco; os nós da dobradiça continuam alinhados. Nada atravessa a base. É o oposto do achado do staging (tampa de primitivas indo para baixo/trás).
- **Câmera do jogo** (`*-game.png`): aberto, a tampa lê como um painel escuro com aro de latão atrás do Hero. Fechado, o baú fica quase todo atrás do Hero nessa posição, e a pedra `#55596B` se confunde com o piso, como o A11 previa. As cinco salas e a do Boss repetem a mesma leitura.
- Diagnóstico por sala em [context.txt](graphical-attempt1/perf/context.txt): fechado `lidEuler (0, 0, 0)`; aberto `(285, 180, 180)` = −105° em X; o centro da tampa aberta fica 0,55 m atrás do centro do baú.

## Limitações e pendências

- **Aprovação artística humana pendente.** Leitura do baú fechado fraca na câmera do jogo (pedra com a cor do piso e o Hero na frente). O A11 sugere trocar a célula da paleta (paredes em `#3E4152`) sem refazer a malha. Os renders não são o Game view do Editor gráfico nem um monitor físico; HUD fora. Build Windows não executado.
- **Abertura instantânea.** O take `Open` não é usado (Decisão 2).
- **Sexto baú no andar 2 (F14).** `Floor_02_Fixed.unity` continua com o baú de primitivas e a tampa girando para baixo/trás. Para reproduzir: descer ao segundo andar, limpar a sala, F perto do baú e olhar de lado. Resolver com o mesmo integrador estendido à cena, numa tarefa própria.
- **Checagem em runtime pelos limites.** O FBX não é Read/Write (importação preservada); em PlayMode a tampa é conferida pelos 8 cantos dos limites. A prova por vértice das duas poses roda no EditMode com a mesma malha e rotação.
- **Integrador depois de cenas novas.** Um baú novo criado pelo builder F08 nasce de primitivas; rodar `Apply Chest Model Art (A11)` em seguida. O `Validate` acusa.
