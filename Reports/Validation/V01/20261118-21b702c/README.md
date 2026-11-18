# V01 — Inventário de entregas e escopo do candidato

- **Execução:** 18/11/2026, Claude (revisor), branch `claude/v01-delivery-inventory` criada de `main@21b702c` (= `origin/main`, árvore limpa).
- **Ambiente:** Windows 11 Pro, Git Bash, Python 3.11.9. Unity **6000.6.9f1** instalado (Hub), **Editor não aberto**: sem lock em `Temp/`.
- **Pacotes (manifest/lock conferidos):** URP 17.6.0, Input System 1.20.1, Cinemachine 6.6.0, AI Navigation 2.0.16, UGUI 2.6.0 e Test Framework 1.8.0 (builtin). Coincidem com ARCHITECTURE.
- **NÃO VALIDADO NO EDITOR.** Esta execução fez só análise estática de Git, código, metas e relatórios. Não executou EditMode, PlayMode, build nem revisão visual. Os resultados de teste citados abaixo são **históricos**, vêm dos relatórios de cada entrega e não são atribuídos a esta execução.

## Comandos executados e resultados

| Comando | Resultado | Artefato |
| --- | --- | --- |
| `git fetch --all --prune`; `git status`; `git branch -a`; `git branch [-r] --no-merged main` | main = origin/main; nenhuma branch local ou remota fora da main | — |
| `git worktree list` + `git -C <wt> status --porcelain` (6 worktrees) | todos limpos: 0 alterações | — |
| `curl https://api.github.com/repos/RodrigoAguiarS/Eldoria/pulls?state=all` (sem token; não há `gh` nesta máquina) | 32 PRs, **0 abertos, 32 mergeados** | [pr-snapshot.txt](pr-snapshot.txt) |
| `git log --merges --first-parent main` e commits diretos | PR → commits e commits sem PR | [main-merges.txt](main-merges.txt) |
| `git log --all --grep 'F06\|tutorial\|onboarding'`; `git grep -i -w tutorial\|onboarding\|F06` em todas as refs (Scripts/Tests/docs/Reports) | nada em código/testes; só menções em planos/catálogo/inventários | — |
| `python Tools/Audit/audit_project.py --output …/project-audit.json` | exit 0; 2856 arquivos; 0 cópias Tools/instaladas divergentes; 0 links Markdown quebrados; 86 grupos duplicados (histórico) | [project-audit.json](project-audit.json) |
| `python .claude/skills/unity-csharp-scripting/scripts/check_unity_project.py --json .` | exit 0; 1478 arquivos; **0 erros / 0 avisos** de meta/GUID/Missing Script (cenas texto) | [unity-static.json](unity-static.json) |
| Mesmo `audit_project.py`, rodado de novo depois das edições desta V01 (saída temporária fora do repositório) | 0 links Markdown quebrados, incluindo este README e as linhas novas de MVP e NEXT_STEPS | — |
| Varredura de GUIDs dos `.meta` de cada lote A em `.unity/.prefab/.asset/.mat/.controller/.anim` fora do próprio lote | **0 consumidores** em todos os lotes A01–A15 | (script inline; refazível) |
| Varredura de GUIDs de scripts F em cenas/prefabs texto | ver § Cenas; cenas dos andares são binárias | — |

## Matriz dos 30 IDs

Colunas separadas: **origem** (commit/PR/merge) e **estado** (nível e evidência). Níveis: conceito → produzido → importado → integrado → validado. Na coluna "Validado", "histórico" significa que um relatório anterior declara o teste, mas a V01 não reexecutou nada.

### Funcionalidades F01–F15

| ID | Origem: commits → PR / merge na main | Caminho principal | Nível | Evidência (histórica) | Estado V01 / lacuna |
| --- | --- | --- | --- | --- | --- |
| F01 | `8d8ddd2`, commit direto (sem PR, antes da regra D-003 `d0b97fc`) | `PlayerInventory.TryGetEquipmentComparison`, InventoryPresenter; `EditMode/EquipmentComparisonTests` | integrado (3 cenas de gameplay, release incluída) | [Reports/F01](../../../F01/README.md): 24 EditMode + 26 PlayMode e captura | Na main. Composição em cena binária: NÃO COMPROVADO estaticamente |
| F02 | `a86d2cd`, commit direto (sem PR) | `InventoryModel.TrySort`, botão Ordenar; `InventorySortTests` | integrado | [Reports/F02](../../../F02/README.md): 32 + 32 | Na main. Idem |
| F03 | `ac09475` → PR #1 / `3468060` | `InventoryModel.GetPotionIndex`, opção JSON v3; `QuickPotionIntegrationTests` | integrado | [Reports/F03](../../../F03/README.md) | Na main |
| F04 | `f289917`, `bbfe895` → PR #2 / `4a1cada` | `RuntimeInputBindings`, `InputBindingSet`; `InputBindingTests`, `ControlRebindingInputTests` | integrado (Player.prefab + MainMenu, confirmado por GUID) | [Reports/F04](../../../F04/README.md): 153 EditMode, 35 PlayMode, captura | Na main |
| F05 | `1dde5b4` → PR #3 / `850479b` | `PlayerRewardFeedback` (Player.prefab, confirmado por GUID) | integrado | [Reports/F05](../../../F05/README.md): 158/83, build sem erros | Na main. A04–A06 ainda não ligados |
| **F06** | **nenhuma origem**: sem commit, branch, PR, worktree, código, teste ou relatório em 40+ refs e 6 worktrees | — (só o brief em PROMPT_CATALOG) | **não iniciado** | — | **NÃO COMPROVADO / ausente.** O motivo: nenhuma referência fora de planos. `BindingHintPresenter` (dicas fixas, `25e105d`) não é o onboarding F06 |
| F07 | `8bc9d9e` → PR #4 / `2cb7610` | `EnemyCombat.HasLineOfSight`, `EnemyData.lineOfSightInterval`; `RangedLineOfSight*Tests` | integrado | [Reports/F07](../../../F07/README.md): 159/32, captura, build | Na main |
| F08 | `5432912` → PR #6 / `77e4b13`; `f300d82` → PR #7 / `6fdf184` | `RoomRewardChest`/`RoomRewardPresenter` nas 5 salas; `RoomReward*Tests` | integrado (cena release) | [Reports/F08-AllRooms](../../../F08-AllRooms/README.md): 161/132, captura, build | Na main. Baús ficam na cena binária, não nos prefabs `Room_*`: NÃO COMPROVADO estaticamente |
| F09 | `9747195` → PR #8 / `e0860e6` | `DestructibleProp`/Presenter, layer Destructible; `DestructibleProp*Tests` | integrado | [Reports/F09](../../../F09/README.md): 163/146 | Na main. Idem (cena binária) |
| F10 | `538f847` → PR #9 / `ac6ce43` | `UiTextScale` (MainMenu por GUID), `TextScaleSceneBuilder`; `TextScale*Tests` | integrado | [Reports/F10](../../../F10/README.md): 41 + 13 EditMode, 23 PlayMode, matriz gráfica | Na main. **Regressão aberta**: `ItemsCombatAssetTests.IntegrateTwice_…` (`Inventory slot missing: 0`), registrada desde a F10 até o último EditMode completo (F13: 244/245). Sem mudança nos arquivos envolvidos depois disso |
| F11 | `d3fe1f1`, `7883007` → PR #10 / `151c675` | `RunCheckpoint*` (schema hoje v3); `RunCheckpoint*Tests` | integrado | [Reports/F11](../../../F11/README.md): 205/206 (a falha é a da F10) | Na main |
| F12 | `db9d7a9`…`ca8edd8` → PR #11 / `35bd7be` | `PlayerTalents` (Player.prefab por GUID), `TalentChoicePresenter`; `Talent*Tests` | integrado; balanceamento sem playtest | [Reports/F12](../../../F12/README.md): 227/228 | Na main |
| F13 | `664e355`, `daa37d5`, `1ef1559` (branch `claude/f13-lancer`) → PR #13 / `1d91a36` (merge `fc07208`) + PR #14 / `39a1908` | `EnemyLungeAttack` (`Enemy_Lancer.prefab`), cena `F13_Lancer_Test.unity` | **protótipo isolado**, fora do build | [Reports/F13](../../../F13/README.md): 244/245, 14/14 PlayMode F13 | Na main. Playtest humano pendente |
| F14 | `e76164c`, `6c2d01b`, `893a885` → PR #15 / `c9f8016` | `FloorTransitionController`, `Floor_02_Fixed.unity` (no build), checkpoint v3 | integrado (1 sala) | [Reports/F14](../../../F14/README.md): 66 EditMode + 35 PlayMode pertinentes, percurso gráfico | Na main. Só subconjunto pertinente, não suíte completa |
| F15 | `e88b56b` → PR #16 / `9dd31ab` | `StoneSentinelBrain` (`Boss_StoneSentinel.prefab`), `F15_StoneSentinel_Test.unity` | **protótipo isolado**, fora do build | [Reports/F15](../../../F15/README.md): 33/33 EditMode (aceite), 20 PlayMode, 14 WindowsPlayer | Na main. Playtest humano pendente |
| (extra) Portas NavMesh | `49dc79a`, `4860073` → PR #12 / `b76758f` | `DoorNavMeshBlockerBuilder`, prefabs `Room_*` | integrado | [Reports/DoorNavMesh](../../../DoorNavMesh/README.md) | Na main; fora dos 30 IDs |

### Arte A01–A15

Todos os 15 lotes estão na main. Nenhum GUID de lote é referenciado fora da própria pasta, e as `.meta` dos lotes foram escritas à mão ou derivadas de modelo (manifesto: "NÃO VALIDADO NO EDITOR"). Por isso nenhum lote passa de **produzido** (A01–A12) ou **conceito** (A13–A15): importado, integrado e validado ficam NÃO COMPROVADOS.

| ID | Recurso | Origem: commit → PR / merge | Caminho | Nível | Destino previsto → tarefa |
| --- | --- | --- | --- | --- | --- |
| A01 | ICON-ACTION-DASH | `f95bb5e` → #17 / `f568ada` | `Art/ProductionStaging/ICON-ACTION-DASH` | produzido | HUD do dash (`DashCooldownPresenter`/`PlayerResourcePresenter`) → V03 |
| A02 | ICON-ACTION-SPECIAL | `c9d857d` → #18 / `5c564b1` | `…/ICON-ACTION-SPECIAL` | produzido | HUD do giro → V03 |
| A03 | UI-SKIN | `d60991d`, `c009693` → #19 / `65a9eaa` | `…/UI-SKIN` | produzido (16 sprites) | Telas via builder, [A03-UI-SKIN-INTEGRATION](../../../../docs/A03-UI-SKIN-INTEGRATION.md) → V04 |
| A04 | FX-HEAL | `f1b8ba4` → #20 / `3b87326` | `…/FX-HEAL` | produzido | `PlayerRewardFeedback` (cura) → V05 |
| A05 | FX-LEVEL | `8b1a3b6` → #21 / `0b79390` | `…/FX-LEVEL` | produzido | `LeveledUp` → V05 |
| A06 | FX-LOOT | `5ffbe69` → #22 / `d6a321b` | `…/FX-LOOT` | produzido | `CollectionAccepted` → V05 |
| A07 | FX-DASH | `e4bbbf7` → #23 / `8d0d58a` | `…/FX-DASH` | produzido | Nova ponte início/fim do dash → V06 |
| A08 | FX-PROJECTILE | `ee3afa8` → #24 / `ea0be31` | `…/FX-PROJECTILE` | produzido (FBX) | Filho visual de `Projectile.prefab` → V07 |
| A09 | FX-PROJECTILE-TRAIL | `eeeff13` → #25 / `ba2715e` | `…/FX-PROJECTILE-TRAIL` | produzido | Trail + `Clear()` no pool → V07 |
| A10 | FX-SLAM | `518dda1` → #26 / `d269801` | `…/FX-SLAM` | produzido | Evento após `ResolveSlam` → V08 |
| A11 | MODEL-CHEST | `52183f8` → #27 / `99fcc7b` | `…/MODEL-CHEST` | produzido (FBX + take Open) | `RoomRewardPresenter.lid` nas 5 salas → V09 (atenção ao achado do giro da tampa) |
| A12 | MODEL-DESTRUCTIBLE | `0da498a` → #28 / `4d2c7a5` | `…/MODEL-DESTRUCTIBLE` | produzido (4 FBX) | `DestructiblePropPresenter` Intact/Rubble → V10 |
| A13 | REF-CONCEPT-10 lanceiro | `8cae9db` → #29 / `d431f61` | `Art/Concepts/SkeletonLancer_Concept_01.png` | conceito | Decisão V11 → V12 condicional |
| A14 | REF-CONCEPT-11 Sentinela | `dade9e2` → #30 / `79f0968` | `Art/Concepts/StoneSentinel_Concept_01.png` | conceito | Decisão V11 → V13 condicional |
| A15 | REF-CONCEPT-12 cripta | `a62a5f7` → #31 / `25cd438` | `Art/Concepts/FloodedCrypt_Concept_01.png` | conceito | Decisão V11 → V14 condicional |

**A15:** a fotografia do plano foi tirada em `79f0968`, que é o merge do #30, ou seja, antes do #31. O trabalho então não versionado agora está versionado pelo PR #31. Hoje não há arquivo de A15 não versionado nem ignorado no checkout ou nos 6 worktrees. Não é possível comprovar se aqueles arquivos locais são idênticos aos versionados, porque não existe cópia anterior: fica NÃO COMPROVADO. Nada foi incluído nem descartado.

## Cenas e build

| Cena | Papel | Formato | Build de release (`MvpReleaseBuild`) |
| --- | --- | --- | --- |
| `Scenes/MainMenu.unity` | Menu, Continuar run, opções | texto | sim (1ª) |
| `Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity` | **1º andar de release** (5 salas + Guardião) | **binário** | sim |
| `Scenes/Floor_02_Fixed.unity` | 2º andar F14 (1 sala) | **binário** | sim |
| `Scenes/Floor_01.unity`, `Scenes/MVP_Arena.unity` | gameplay de teste e arena M1 | **binário** | não |
| `Scenes/F13_Lancer_Test.unity`, `Scenes/F15_StoneSentinel_Test.unity` | protótipos isolados | **binário** | não |
| `Scenes/Bootstrap.unity`, `Scenes/Art_Test_Hero.unity`, `Scenes/SampleScene.unity`, cenas SpriteLab/`_Test` | fundação, testes e experimentos | misto | não |

Achados de cena, nenhum corrigido aqui:

1. **8 cenas binárias com Force Text ativo** (`EditorSettings.m_SerializationMode: 2`), entre elas as duas cenas de andar do build. Estão assim desde `cf3720d` e não aparecem documentadas. Consequência: a composição F01–F03, F08–F11 e F14 dentro das cenas não pode ser inspecionada nem revisada por diff. A verificação depende do Editor (V02/V16). Uma possível reserialização mexe em cenas e exige tarefa coordenada.
2. `EditorBuildSettings` habilita 6 cenas: MainMenu, Floor_01_GeneratedCharacters, SampleScene, Bootstrap, MVP_Arena e Floor_02_Fixed. A build de release passa lista própria, com MainMenu e os dois andares. Uma build feita pelo menu padrão da Unity incluiria cenas de teste. Conferir em V20.
3. `MvpReleaseBuild.ReleaseVersion` e `PlayerSettings.bundleVersion` continuam `1.0.0` (já apontado no plano para V20).

## Schemas persistentes (estado do código em `21b702c`)

| Dado | Local / chave | Versão atual | Compatibilidade |
| --- | --- | --- | --- |
| Checkpoint da run | `persistentDataPath/run-checkpoint.json` + `.bak`/`.tmp`; isolamento `DUNGEON_CRAWLER_CHECKPOINT_DIR` | **v3** (`RunCheckpointData.CurrentVersion`) | Lê v1/v2. `floorId` `eldoria.floor01.v1` (entrada sem checkpoint) e `eldoria.floor02.v1` (`allowEntryCheckpoint: 1`). Talentos desde v2 |
| Opções gerais | PlayerPrefs `DungeonCrawler.Options.v1` | JSON **v4** (`GameOptionsData.CurrentVersion`) | Migra v1–v3. `textScalePercent` 100/125/150 e `quickPotionPolicy` |
| Bindings | PlayerPrefs `DungeonCrawler.Bindings.v1` | documento **v1** (`version != 1` rejeita) | 14 slots, `Interact/0` por último (F08) |
| Identidade | PlayerPrefs ficam sob o produto `Eldoria` (D-003) | — | Opções da build pré-Eldoria voltam ao padrão uma vez |

V17 deve validar a retomada entre processos com esses três documentos e fazer backup e restauração de preferências reais.

## PRs e textos pendentes

- **PRs pendentes: nenhum.** São 0 abertos e 32 mergeados (#1–#32). Também não há branch local ou remota fora da main.
- Textos antigos que ainda dizem "merge depende da revisão" ou "integração aguarda": em MVP, F08, F09, F10, F12, Portas e F13; em NEXT_STEPS, F05, F07, F08, F10 (linha 9), F11, F12, F14 e F15. Todos já foram integrados (tabela acima). Por limite de escopo, esta V01 acrescentou **uma linha de estado** em MVP e NEXT_STEPS apontando para esta matriz, sem reescrever o histórico.
- Worktrees antigos limpos (`a01-dash-icon`, `a02-spin-icon`, `f05-feedback`, `f13-door-compat`, `validation-task-prompts` e `.claude/worktrees/silly-driscoll-88ece9`) podem ser removidos pelo usuário. A V01 não os removeu.

## Baseline reproduzível

- **Commit:** `21b702c` (main, PR #32). Unity 6000.6.9f1 e os pacotes acima.
- **Estático (esta execução):** auditoria e checker sem erros. Os comandos estão na tabela inicial e podem ser repetidos com `--output` novo.
- **Último resultado completo conhecido (histórico, não desta execução):** EditMode 244/245 na F13 (`Reports/F13/EditMode.xml`, 17/11 02:13). A falha única é `IntegrateTwice_…`. F14 e F15 rodaram só subconjuntos, então **não existe suíte completa depois de F14/F15**. Esse é o primeiro gate da V02: compilação, EditMode e PlayMode completos sobre `21b702c` (ou sobre a main que receber este PR, que só altera documentação, relatórios e uma linha do manifesto).

## Candidato base e plano

**Proposta de candidato base (decisão final do usuário em V11):** a main atual inclui a release 1.0 com pós-release, F01–F05, F07–F12, F14 (uma sala) e a correção de portas. F13 e F15 continuam em cenas de teste fora do build. A13–A15 ficam só como conceito e não entram no candidato. **F06 não existe** e não entra no candidato base. Implementá-lo seria funcionalidade nova, então depende de decisão explícita.

**Correções do que já existe (sem expandir escopo):**

1. V02: reproduzir e corrigir `ItemsCombatAssetTests.IntegrateTwice_…` (F10). Depois, suíte completa e idempotência dos integradores.
2. V02/V16: decidir com o usuário se as 8 cenas binárias serão reserializadas em texto, em tarefa coordenada porque altera cenas. Enquanto isso, toda verificação de composição exige o Editor.
3. V03–V10, em sequência: importar e reserializar as `.meta` dos lotes A01–A12 no Editor e integrar sem refazer os assets. Em V09, tratar o achado do giro da tampa do baú (A11).
4. V20: lista de cenas de `EditorBuildSettings`, versão `1.0.0` do empacotador e XMLs Leadership.

**Expansão condicional (não autorizada por esta V01):** F06, que precisa de tarefa nova aprovada; V11 (playtest F13/F15 e aprovação de A13–A15); V12–V15 (3D do lanceiro e da Sentinela, kit da cripta, salas e andares extras). Se o usuário não escolher esse conteúdo, ele fica "Não aplicável ao candidato" com motivo.

## Limitações

- Sem Editor: nenhum teste, captura ou build. A composição em cenas binárias e o estado importado dos lotes não foram observados.
- A lista de PRs veio da API pública sem autenticação no momento indicado em `pr-snapshot.txt`. Comentários e revisões dos PRs não foram lidos.
- A ausência de F06 está comprovada nas refs locais e remotas buscadas e nos worktrees deste computador. Trabalho fora do Git ou em outra máquina não é observável daqui.
- Contagens históricas foram copiadas dos READMEs e XMLs de cada entrega, sem reexecução.
