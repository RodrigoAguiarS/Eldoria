# Lemo3DGenerator — personagens 3D low-poly para o Dungeon Crawler

Gera **modelos 3D reais** (malha skinned, materiais, ossos, equipamentos e animações) e exporta **FBX** (Unity)
e **GLB**, usando o **three.js r170 instalado no Lemo-Opuscar** (`G:\lemo-opuscar\lemo-opuscar\node_modules\three`),
carregado em modo somente leitura. O vocabulário de modelagem segue os estilos 3D do Lemo (`lowpoly-island`: primitivas
facetadas, `flatShading`, jitter leve; `brick-toy`: hierarquia de articulações + poses como dados).

## Requisitos
- Node 20+ (testado com 24.18). Sem dependências npm (o three vem do Lemo; `LEMO_ROOT` muda o caminho).
- `check-cs`: Unity 6000.6.9f1 instalado (usa o Roslyn do Editor e as referências do `DungeonCrawler.Editor.csproj`).
- Blender **não** é necessário (não está instalado nesta máquina).

## Comandos (dentro de `Tools/Lemo3DGenerator`)
| Comando | Faz |
|---|---|
| `npm run generate` | todos os personagens do catálogo → `Assets/_Project/Art/Models/Generated/<Nome>/` |
| `node src/generate.mjs --character hero --out <pasta>` | um personagem, em outra pasta (iteração sem disparar import no Unity) |
| `npm run validate` | relê GLB/FBX do disco e valida (ver REPORT.md §3) → `reports/validation.json` |
| `npm run preview` | servidor em http://localhost:5178 : visualizador 360°, troca de clip, câmera isométrica, ossos |
| `…/?sheet=turn&clip=Idle` | prancha com 8 ângulos por personagem → `preview/captures/` |
| `…/?sheet=clips&models=Hero&angle=60` | prancha com 6 instantes de cada clip |
| `npm run check-cs` / `npm run install-unity` | compila os scripts Unity fora do Editor / copia para `Generated/_Scripts` |

## Saídas por personagem
`<Nome>.fbx` (FBX 7.4 ASCII: malha skinned + equipamentos + ossos + bind pose + um take por animação) ·
`<Nome>.glb` (glTF 2.0 binário: mesmo conteúdo; a Unity só o lê com o pacote glTFast, que **não** foi instalado) ·
`<Nome>_manifest.json` (ossos, soquetes, equipamentos, materiais oficiais, clips, loops, velocidades, suposições).
Fonte editável = a definição em `src/characters/<id>.mjs` (não há `.blend`, ver REPORT.md).

## Na Unity (menu **Tools › Dungeon Crawler › Generated Characters**)
1. A importação é automática (`GeneratedModelPostprocessor`, só nesta pasta): escala 1 (metros), rig **Generic**,
   avatar próprio, normais do arquivo, loops conforme o manifesto. Ajustes manuais posteriores são preservados.
2. **Build Test Assets (all)**: materiais **URP Lit** com os valores do documento, remapeados no FBX;
   `<Nome>.controller` (blend tree `Locomotion` por `Speed` em m/s, gatilho por ação com o nome do clip, `Dead` → Death);
   `<Nome>_Test.prefab` (modelo + Animator + `GeneratedCharacterAnimator` + `GeneratedCharacterRig` com soquetes e armas);
   `_Test/GeneratedCharacters_Test.unity` (todos os personagens percorrendo seus clips). Nenhum prefab/cena oficial é tocado.
3. **Validate In Editor**: reproduz cada clip com `SampleAnimation` + `BakeMesh` e grava `reports/unity_validation.json`.

## Testar dentro do jogo (sem alterar nada oficial)

O menu principal **Tools > Dungeon Crawler > Build > Floor Scene (M2)** agora executa o fluxo
completo: reconstrói a base `Floor_01`, gera a cópia `Floor_01_GeneratedCharacters`, abre essa
cena 3D e integra o shell da versão 1.0. O build final usa `MainMenu` primeiro e
`Floor_01_GeneratedCharacters` como destino de Jogar, preservando as cenas de teste no Editor.
Os passos abaixo continuam disponíveis para reconstruir somente a cópia.
Os GUIDs da cópia e dos encounters são preservados nas reconstruções.

1. Rode **Build Test Assets (all)** (uma vez, ou após regenerar).
2. Rode **Build In-Game Test (Floor_01 copy)**. Ele cria em `Generated/_GameTest`:
   - `Enemy_Melee/Elite/Ranged_Generated.prefab` e `Boss_Warden_Generated.prefab`: **variantes** dos prefabs oficiais com o
     modelo gerado; a cápsula fica invisível (colisor, NavMeshAgent, IA, combate e barra de vida continuam os mesmos);
   - `Encounters/*_Generated.asset`: cópias dos encounters apontando para as variantes;
   - `Floor_01_GeneratedCharacters.unity`: cópia de `Floor_01` religada às cópias, com o herói gerado no Player.
3. Abra `Floor_01_GeneratedCharacters.unity` e dê **Play**. Controles normais do jogo (WASD, mouse, clique, espaço, R).
4. Reexecute o passo 2 sempre que `Floor_01` mudar (a cópia é refeita do zero).

As pontes (`unity/GameBridge`) só **leem** o jogo: `EnemyCombat.IsBusy` → Attack, `PlayerDash.IsDashing` → Dash,
eventos C# de `PlayerCombat`/`PlayerSpecial` → Attack_01/02/03/Special (nenhum AnimationEvent aplica dano), `BossBrain.State` (+ tamanho do telegraph) → SlamWindup/Slam/Volley/Roar,
`Health.Damaged/Died` → Hit + flash/Death, `FloorController.Completed` → Victory.

## Arte do cenário no jogo (salas)
Menu **Tools › Dungeon Crawler › Generated Characters › Apply Generated Art To Game** (`GeneratedArtApplier`):
- troca **só malha e material** dos cubos de piso (`Ground`), paredes (`Obstacle`) e portões (`Door`) dos prefabs
  `Prefabs/Rooms/*` e da geometria solta de `Floor_01_GeneratedCharacters` (cena inicial do build), `Floor_01` e
  `MVP_Arena` por pedra low-poly (#55596B / #3E4152, grade de ferro com ferrugem). BoxCollider, layer, escala e renderer
  continuam os mesmos (colisão, NavMesh e `CameraOcclusionHider` intactos);
- adiciona por sala 6 tochas (2 com luz pontual, sem sombra), 4 runas #4FD8FF nos cantos e musgo #4C5A44 — sem colisor e
  com `NavMeshModifier.ignoreFromBuild`;
- assets em `Art/Models/Generated/_GameArt/{Materials,Meshes}`. Idempotente.

**Integrado aos builders (13/11/2026, 19:02, autorizado pelo usuário):** `DungeonFloorSceneBuilder.Build()` chama
`GeneratedArtApplier.ApplyEnvironment()` logo após `GeneratedGameTestBuilder.Build()`, e `DungeonMvpSceneBuilder.Build()`
chama no final — sempre depois do bake do NavMesh (o NavMesh continua vindo dos cubos; só malha/material mudam).
Verificado rodando `Build/Floor Scene (M2)`: o cenário é reaplicado sozinho e o Play abre sem erros. O menu manual continua
disponível.
Os personagens já entram no jogo pelas variantes do `GeneratedGameTestBuilder`, que o builder do Codex usa para a cena inicial.

## Criar ou alterar um personagem
1. Crie `src/characters/<id>.mjs` exportando `P` (proporções em metros), `definition` (nome, materiais com hex/metallic/
   smoothness, `scale`, `basePose`, `locomotion`, `sockets`, `extraBones`), `body(M)` (lista de peças), `equipment(M)` e `clips()`.
2. Peças: use `src/parts.mjs` (braço, perna, bota, punho, ombreira, olhos), `src/characters/_skeleton_kit.mjs` (crânio,
   costelas, ossos, ombreiras espinhosas, espada serrilhada) e as primitivas de `src/geo.mjs`
   (`bevelBox`, `lathe`, `convex`, `extrude`, `strut`, `sheet`, `jitter`). Cada peça é `{ geo, mat, bone }` (rígida) ou
   `{ geo, mat, weights }` (pesos mistos, p.ex. tronco e capa).
3. Animações: `src/anim.mjs` (`idle`, `locomotion`, `slash`, `dash`, `hit`, `death`, `victory`) ou chaves próprias
   `{ name, duration, loop, keys: [[t, pose], …] }`. O bake faz 30 fps, inclui todos os ossos e aplica o "ground clamp".
4. Registre em `src/catalog.mjs`, rode `generate` + `validate`, revise no `preview`, depois **Build Test Assets** no Unity.

Convenções: personagem olha para +Z, esquerda = +X, pivô nos pés, 1 unidade = 1 m; braço/perna para frente = X negativo.
