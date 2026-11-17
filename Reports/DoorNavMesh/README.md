# Portas fechadas bloqueiam o NavMesh — evidências

Branch `claude/door-navmesh-block` a partir de `main` @ `35bd7be`. Unity 6000.6.9f1 (Windows 11), batch `-nographics`.
Corrige o "Achado 1" registrado na F13 (`Reports/F13/README.md` na branch `claude/f13-lancer`).

## Problema

A layer Door fica fora do bake (`DungeonMvpSceneBuilder.BuildNavMesh` usa só Ground|Obstacle) e as portas
não tinham `NavMeshObstacle`. O vão de 4 m de uma porta **fechada** continuava no NavMesh, então qualquer
`NavMeshAgent` (melee/ranged/elite/Boss) podia atravessá-la. O collider do bloqueio só barra o jogador
(CharacterController) e projéteis/visão.

## Correção

- `DoorController` ganha a referência serializada `navMeshBlocker` (`NavMeshObstacle`) e a liga/desliga junto com
  o collider em `Open`, `Close` e `SetOpenInstant` (checkpoint). Porta aberta = obstáculo desligado = sem carving.
- O obstáculo fica na **raiz estática** da porta, não no `Blocker` (diferente da sala F13). O `Blocker` afunda/sobe
  animado; com `carveOnlyStationary` no objeto que se move, o recorte só voltaria ~0,5 s depois de a porta
  parar de subir. Na raiz, o recorte acompanha o estado no frame seguinte (teste 2) e não depende da
  profundidade de 2,7 m. Box, centro/tamanho iguais ao collider do bloqueio fechado (4 × 1,4 × 1 m), `carving` e
  `carveOnlyStationary` ligados (sem efeito: a raiz não se move).
- `DoorNavMeshBlockerBuilder.Integrate` (menu `Tools/Dungeon Crawler/Dungeon/Integrate Door NavMesh Blockers`,
  batch `-executeMethod DungeonCrawler.Editor.DoorNavMeshBlockerBuilder.IntegrateBatch`): aplica aos 7 prefabs de
  `Prefabs/Rooms` pelas APIs do Editor e verifica as cenas de release (só grava o que mudou). As salas das cenas
  são instâncias desses prefabs e herdam o componente: **nenhuma cena foi regravada**. NavMesh, arte e salas
  não foram reconstruídos.
- `DungeonFloorSceneBuilder.CreateDoor` chama o mesmo `Configure`, então um rebuild completo mantém o bloqueio.

## Resultado

| Artefato | O quê | Resultado |
| --- | --- | --- |
| [PlayMode-before.xml](PlayMode-before.xml) | Teste novo **antes** de aplicar o builder (só código) | **0/2: reproduz o achado** |
| [hashes-integrate-1/2](hashes-integrate-1.txt), `integrate-*.log` | Builder executado 2× | 1ª: 7 prefabs alterados, 0 cenas; 2ª: `14 portas, 0 prefabs e 0 cenas alterados`, hashes idênticos |
| [PlayMode-after-logassert.xml](PlayMode-after-logassert.xml) | Teste novo após o builder | comportamento aprovado; 1 falha do **próprio teste** (log de diagnóstico sem `LogAssert.Expect`), corrigida |
| [PlayMode-door-final.xml](PlayMode-door-final.xml) | Teste novo, estado final | **2/2** |
| [PlayMode.xml](PlayMode.xml) | PlayMode completo, `DUNGEON_CRAWLER_TEST_REPORT_DIR` = [PlayModeRun](PlayModeRun/) | **165/176, 0 falhas**, 11 capturas explícitas ignoradas em batch |
| [EditMode.xml](EditMode.xml) | EditMode completo | **229/230**; 1 falha pré-existente (abaixo); 2/2 novos |
| [first-run/](first-run/) | 1ª execução completa | PlayMode 164/176 + 1 falha de ambiente (benchmark recusou sobrescrever medições sem `DUNGEON_CRAWLER_TEST_REPORT_DIR`); EditMode 228/230 com 1 falha do teste novo (`Collider.bounds` é zero em prefab não instanciado; corrigido para usar transforms) |

### Antes × depois (sala 2 do `Floor_01_GeneratedCharacters` salvo, face interna da entrada em z = 10,0)

| Medida | Antes | Depois |
| --- | --- | --- |
| Agente melee dirigido à sala 1 com a entrada fechada: menor z | **4,95** (entrou na sala 1), `PathComplete` | **11,363**, `PathPartial` |
| Melee perseguindo o jogador invulnerável encostado na porta (z = 11): menor z | 11,922 | 12,015 |
| `CalculatePath` sala 1 → sala 2 com a saída da sala 1 fechada (estado inicial) | `PathComplete` (falha) | não completo |
| Após `SetOpenInstant(true)` / `(false)` (caminho do checkpoint), 1 frame | — | completo / não completo |

Com melee, a perseguição sozinha não reproduziu a entrada no vão: eles param no alcance de ataque. A F13 viu
isso com o lanceiro (z = −8,07). Por isso o teste dirige um agente diretamente para trás da porta, o que
reproduz o problema de forma determinística.

### Cobertura do aceite

| Critério | Evidência |
| --- | --- |
| Reproduz antes e passa depois | `DoorNavMeshBlockingTests.SavedFloor_ClosedDoorStopsAgentsAtItsFace_OpenDoorsKeepRoomsConnected` e `..._InstantOpenAndCloseTogglesCarvingImmediately`: 0/2 antes, 2/2 depois |
| Agente não cruza a face da porta fechada | mesmo teste: agente dirigido e perseguidores com z > 10,0 por 6 s; caminho de volta à sala 1 não completo |
| Porta aberta não carva nem bloqueia o caminho entre salas | mesmo teste: após limpar a sala 1, um `NavMeshAgent` real vai da sala 1 à sala 2 e `CalculatePath` sala 1→2 e 2→3 dá `PathComplete`; `carving enabled == !IsOpen` em todas as 10 portas, em todas as fases |
| Jogador passa por portas abertas | `FloorIntegrationTests` (5/5), `MvpSceneAcceptanceTests` (9/9), `RunCheckpointPlayModeTests` (7/7): o jogador usa CharacterController, e o collider da porta segue o mesmo estado de antes |
| Spawns e sala limpa sem softlock | `FloorIntegrationTests.SavedFloor_AllWaves...` (5 salas, vitória uma vez), `RoomSystemsPlayModeTests` 3/3, `BossPlayModeTests` 1/1, `RoomRewardChestTests` 8/8 (+2 explícitos) |
| Assets e idempotência | `DoorNavMeshBlockerAssetTests`: cada porta dos prefabs e das duas cenas de andar tem um obstáculo Box com carving na raiz, com o volume do bloqueio, herdado do prefab (sem override de cena); `NavMeshSurface` segue Ground\|Obstacle; 2ª execução não altera bytes/GUIDs de prefabs, cenas e `NavMeshData` |

## Falhas e limites

- **Pré-existente (não desta tarefa):** `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`,
  `Inventory slot missing: 0`, a mesma falha registrada desde a F10.
- Os testes de idempotência do EditMode regravaram cenas, prefabs, materiais, ProjectSettings e relatórios
  históricos de release. Esses efeitos foram descartados com `git checkout` antes do commit (prefabs `Room_*`
  restaurados ao estado commitado da correção). Ao abrir o projeto, o pacote Sentis acrescentou um define em
  `ProjectSettings.asset`; também descartado.
- Os prefabs `Room_*` também passaram a serializar dois campos runtime que já existiam sem estar gravados
  (`EnemySpawner.audioPool`, `RoomController.persistentId`), ambos com o valor padrão, o mesmo usado em runtime.
- `MVP_Arena` não tem portas. `Room_Normal`/`Room_Boss` (legados, fora das cenas de release) receberam o mesmo
  bloqueio por estarem na pasta de salas.
- Uma porta que fecha com um agente dentro do vão o desloca para fora do recorte. Isso não ocorre no fluxo atual
  (as portas fecham quando o jogador entra, e os inimigos nascem depois, do lado da saída).
- Não executados: Editor gráfico/captura, build Windows, playtest humano. A sala F13
  (`LancerTestSceneBuilder.Door`, ainda fora da `main`) continua com o carving no `Blocker`. Ao integrar as duas
  branches, convém trocar por `DoorNavMeshBlockerBuilder.Configure`.

## Reproduzir

```powershell
$p = '<worktree ou projeto>'; $u = 'C:/Program Files/Unity/Hub/Editor/6000.6.9f1/Editor/Unity.exe'
& $u -batchmode -nographics -projectPath $p -executeMethod DungeonCrawler.Editor.DoorNavMeshBlockerBuilder.IntegrateBatch -quit -logFile <novo.log>
& $u -batchmode -nographics -projectPath $p -runTests -testPlatform PlayMode -testFilter DungeonCrawler.Tests.PlayerInput.DoorNavMeshBlockingTests -testResults <novo.xml> -logFile <novo.log>
& $u -batchmode -nographics -projectPath $p -runTests -testPlatform EditMode -testFilter DungeonCrawler.Tests.EditMode.DoorNavMeshBlockerAssetTests -testResults <novo.xml> -logFile <novo.log>
```

Para o "antes": em `main` @ `35bd7be`, aplicar apenas `DoorController.cs` e o teste PlayMode, sem rodar o builder.
