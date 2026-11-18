# V05 — Integrar arte de cura, nível e coleta (A04/A05/A06)

- **Execução:** 18/11/2026, por Claude, com a tarefa V05 delegada pelo usuário (no catálogo, o executor previsto era o Codex). Branch `claude/v05-reward-fx`, criada de `main@2f9e9d6` (= `origin/main`) com a árvore limpa ([base-commit.txt](base-commit.txt)).
- **Commits:** `471bdca` (integração) e `a954300` (correção de profundidade dos glints e do anel). As suítes finais, a captura final e o Validate final rodaram sobre `a954300` ([tested-commit.txt](tested-commit.txt)). O commit seguinte só traz estes documentos, o manifesto e as evidências.
- **Ambiente:** Windows 11 Pro, Unity **6000.6.9f1**, sem outra instância do Editor aberta. Pacotes conforme manifest/lock, sem alteração.
- **Validado no Editor:** compilação, integrador e validador por `-executeMethod`, EditMode e PlayMode completos em batchmode, e a captura no Editor com janela (sem `-batchmode`).
- **Não feito:** aprovação artística humana, observação em monitor físico e build Windows nova.

## Resultado por critério

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Rejeições por HP cheio/morto/bolsa cheia não geram falso sucesso | **Atendido.** Com os emissores reais do `Player.prefab` e o perfil real: poção com HP cheio e com o herói morto → `false`, nada consumido, nenhuma sheet de cura, nenhum aviso; pickup com bolsa cheia fica no chão, sem glint e sem célula nova (`LastLootFrame` = −1); coleta com o herói morto → `false`, sem efeito. Os testes F05 de rejeição continuam passando | `RewardFxArtPlayModeTests.Rejections_FullHpDeadAndFullBag_EmitNoArtAndNoSuccess`, `RewardFeedbackPlayModeTests.*` em [playmode/PlayMode.xml](playmode/PlayMode.xml) |
| Frames/variantes corretos, sem grade vazando ou estado antigo; flash zero mantém mensagens | **Atendido.** A UV assada pelo próprio renderer (`BakeMesh`) confere a célula de **cada** quadro, no centro da janela do contrato: FX-HEAL 8/8 e FX-LEVEL 12/12 na ordem linha a linha a partir do topo-esquerdo. FX-LOOT usa a célula 0/1/2 para Xp/Gold/(Potion, Weapon, Armor). Nos PNG reais, toda célula visível tem ≥ 8 px de margem (sem vazamento no bilinear até o mip 3), o último quadro é transparente e a largura cabe no raio de gameplay. Retrigger e reativação voltam ao quadro 0, à célula neutra, à semente fixa e ao alpha atual da opção (nada do efeito anterior). Flash 0: nenhuma partícula, avisos e contadores mantidos, e o texto aparece | `RewardFxArtTests.SheetContract_…` (2 casos), `LootAtlas_…`; `RewardFxArtPlayModeTests.HealAndLevelSheets_…`, `LootAtlas_…`, `CloseEvents_…`, `ResetDeathAndReactivation_…`, `FlashZero_…`; [captura](graphical/states_crops_real.png) |
| Eventos próximos/múltiplos níveis/pausa/reset cobertos; GC das partículas separado de strings | **Atendido.** Curas e coletas seguidas reiniciam o mesmo emissor (1 quad de cura, 5 glints), sem objeto novo. Vários níveis num ganho: uma sheet de nível e nenhuma de cura. Q aceito na bolsa pausada: a sheet fica parada no quadro 0 e o aviso na fila até retomar. Reset, morte e desativação limpam. GC: 64 aquecimentos e 1.000 trios (cura, coleta alternando ouro/XP, nível) com **0 alocações**; controle positivo `new byte[4096]` = 1; texto medido à parte (6.999 alocações em 1.000 avisos); máximo de 7 partículas vivas | `MultipleLevelsInOneGain_…`, `PauseWithPotionAcceptedInBag_…`, `WarmArtCallbacks_…`; [playmode/perf/reward-fx-art.json](playmode/perf/reward-fx-art.json) |

Outros pontos do brief:

- **Leitura dos contratos.** `RewardFxArtIntegrator` lê `FX-HEAL.timing.json`, `FX-LEVEL.timing.json` e `FX-LOOT.atlas.json` e recusa (exceção) qualquer divergência: tamanho da sheet ≠ grade × célula, ordem dos quadros fora da grade, durações não uniformes, vida ≠ duração, tamanho ≠ célula em metros, pivô do renderer ≠ pivô dos pés, `loot_kinds` ≠ `loot_kind_to_index`, ou textura importada com outro tamanho. Os únicos números escritos à mão são as posições dos glints, que no contrato estão em texto ("5 posições fixas num círculo de 0.2 m a 0.15 m do piso, subindo 0.4 m/s").
- **Sem regra duplicada.** Health, PlayerInventory e PlayerProgression não mudaram. O presenter só emite depois de `Healed` (origem ≠ LevelUp), `CollectionAccepted` e `LeveledUp`, como na F05. Nenhum uso de `UnityEngine.Random` (o teste confere o estado do RNG). O áudio continua o da F05, sem som novo.
- **Builder idempotente.** Apply 2× e 3× sem nenhuma mudança (0 importações, 0 assets, 0 prefab/cena salvos), inclusive depois de reaplicar a F05: o gancho em `RewardFeedbackSceneBuilder.ConfigurePlayer` devolve a arte, e o Apply seguinte não muda nenhum byte (teste `Apply_IsIdempotentAndF05ReintegrationKeepsArt`).
- **Cenas.** Os emissores vêm do `Player.prefab`; as quatro cenas herdam a configuração e não precisaram ser regravadas. O Validate e um teste EditMode abrem cada cena e conferem perfil, material, grade, espaço de simulação, vida, tamanho e pivô, além de zero Missing Script.
- **Regressão:** EditMode **291/291**; PlayMode **212 passed, 0 failed, 18 skipped** (os 17 `[Explicit]` da V04 mais a captura nova, rodada à parte).

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/UI/RewardFxProfile.cs` (novo) | ScriptableObject só de apresentação: emissão de cura/coleta/nível (partículas, raio, altura, subida, vida, tamanho), célula do atlas por `LootKind` e semente fixa |
| `Assets/_Project/Scripts/UI/PlayerRewardFeedback.cs` | Campo opcional `art`. Com perfil: `EmitArt` reinicia o mesmo emissor (Stop+Clear, Play, Emit) com branco × alpha do flash, posições e semente fixas; a coleta troca só a célula (`frameOverTime` constante). Reset limpa a célula. **Sem perfil, os motes da F05 continuam iguais** |
| `Assets/_Project/Scripts/Editor/RewardFxArtIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/Feedback/Apply Reward FX Art (A04-A06)` e `Validate Reward FX Art (A04-A06)`. Lê e valida os contratos, confere a importação, cria materiais e perfil, configura os emissores. Só grava o que mudou |
| `Assets/_Project/Scripts/Editor/RewardFeedbackSceneBuilder.cs` | Uma linha no fim de `ConfigurePlayer`: reaplica a arte quando já integrada |
| `Assets/_Project/Materials/RewardFx/Mat_FxHeal.mat`, `Mat_FxLevel.mat`, `Mat_FxLoot.mat` (novos) | URP/Particles/Unlit, transparente, Blend Alpha, branco. O FX_Spark é aditivo e clarearia/escureceria o rubi e o marfim |
| `Assets/_Project/ScriptableObjects/UI/RewardFxProfile.asset` (novo) | Preenchido pelo integrador |
| `Assets/_Project/Prefabs/Player/Player.prefab` | `art` ligado; `RewardHealing` (4×2, 0,4 s, 1,6 m, World), `RewardLevel` (4×3, 0,7 s, 2 m, Local) e `RewardCollection` (3×1, 0,28 s, 0,2 m, World, fade 0→1→0) com os novos materiais e pivôs |
| `FX-LEVEL-Sheet.png.meta` | `nPOTScale` ToNearest → None (mesmo GUID). Ver Decisão 3 |
| Testes novos | `Tests/EditMode/RewardFxArtTests.cs` (5 casos), `Tests/PlayMode/RewardFxArtPlayModeTests.cs` (9), `Tests/PlayMode/PlayerInput/RewardFxArtVisualTests.cs` (captura `[Explicit]`) |
| `ASSET_MANIFEST.json`, `NEXT_STEPS.md`, `MVP.md` | Estado V05 (FX-HEAL/FX-LEVEL/FX-LOOT e lote `V05-REWARD-FX-INTEGRATION`) |

## Divergências e decisões

1. **Glints de coleta ficavam invisíveis (divergência do contrato FX-LOOT).** A primeira captura não mostrava glint nenhum ([attempt1](graphical/attempt1-glints-not-visible/)). O diagnóstico ([attempt2](graphical/attempt2-diagnostics/captures/diagnostics.txt)) mostrou 5 partículas vivas, na célula certa e com alpha alto, mas a ~19,6 m da câmera o círculo de 0,2 m do contrato fica todo atrás da capa: com o herói oculto, os 5 glints aparecem no mesmo ponto. Correção: o pivô do renderer aproxima os quads 0,6 m da câmera no espaço de visão (z = 3 × 0,2 m). A posição na tela quase não muda, e o teste confere a profundidade e o ponto na tela.
2. **Sinal de z do pivô (FX-LEVEL).** O contrato FX-LEVEL propõe z = −0,2 para "aproximar o quad da câmera" e pede para conferir o sinal no Editor. Medido por `BakeMesh`: na Unity, z negativo **afasta** o quad (+0,6 m no `forward` com z = −3, [attempt2 targeted](targeted/attempt2-depth-sign-away/)). O integrador inverte o sinal ao ler o contrato, para cumprir a intenção declarada (arco da frente diante das pernas). O FX-HEAL tem z = 0 e não muda. O teste agora confere o sentido nos dois efeitos.
3. **FX-LEVEL é NPOT.** 1024×768 não é potência de 2. Com `nPOTScale` ToNearest, a Unity reamostraria a sheet, e as células de 256 px deixariam de cair em pixels inteiros. Por isso o integrador põe `None` em texturas NPOT, como o contrato do FX-LOOT já pedia. FX-HEAL (1024×512) continua com a `.meta` como foi entregue.
4. **Floor_02_Fixed (F14) incluído.** `RewardFeedbackSceneBuilder.Scenes` lista três cenas, mas a F14 é uma cópia do shell de release com o mesmo presenter. Como os emissores vêm do prefab, a F14 recebe a arte sem ser regravada. O Validate e os testes também a conferem, para o segundo andar não voltar aos motes antigos (o mesmo problema que a V04 registrou para a skin).
5. **Perfil separado do presenter.** O caminho primitivo da F05 continua disponível e testado (`RewardFeedbackPlayModeTests` usa emissores sem sheet e sem perfil). A arte é ligada só pelo campo `art`.
6. **Célula do glint por `frameOverTime`.** A coleta troca o `frameOverTime` constante para o centro da célula (`(i + 0,5) / 3`) antes do Emit. Assim não depende da unidade de `startFrame`, que o contrato pedia para conferir. Sem alocação (medido).

## Comandos executados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez. `run.sh <Classe.Método> <rótulo>` roda `Unity.exe -batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([idempotency/run.sh](idempotency/run.sh), [runs.txt](idempotency/runs.txt)).

| # | Comando | Resultado |
| --- | --- | --- |
| 1 | `run.sh RewardFxArtIntegrator.Apply run1` | exit 0: 3 materiais, perfil e `Player.prefab`. Também mudou `nPOTScale` nas duas sheets; a mudança no FX-HEAL (POT) foi revertida e a regra passou a valer só para NPOT |
| 2 | `run.sh … Apply run2` e `Validate validate1` | 1ª tentativa: **exit 1**, `CS1503` no teste novo (construtor de `LootDrop` para armadura). Logs em `attempt0-*.log`. Depois da correção: exit 0, FX-LEVEL NPOT → None; Validate ok |
| 3 | `run.sh … Apply run3` | exit 0, **0/0/0**: nada a gravar |
| 4 | EditMode filtrado (`RewardFxArtTests`, `RewardFeedbackCompositionTests`) | 6/6 |
| 5 | PlayMode filtrado (`RewardFxArtPlayModeTests`, `RewardFeedbackPlayModeTests`) | 1ª: **18/19**, falha de fronteira no próprio teste (amostra em 0,30 s, exatamente entre os quadros 5 e 6) — [attempt0](targeted/attempt0/). Com a amostra no centro do quadro: 19/19 |
| 6 | Commit `471bdca`; suítes completas | EditMode 291/291, PlayMode 212/0/18 ([attempt1-suites-471bdca](attempt1-suites-471bdca/)) |
| 7 | Captura sem `-batchmode` | 1ª: **falhou**, porque o roteiro chamava `SetInvulnerableFor` antes do dano; com HP cheio, a poção foi corretamente recusada ([attempt0](graphical/attempt0-invulnerable-first/)). 2ª: 1/1, mas sem glints visíveis (Decisão 1). 3ª: com diagnóstico |
| 8 | `run.sh … Apply run4-loot-depth`, `run5`, `Validate validate2` | prefab salvo (pivô do glint), depois 0/0/0; Validate ok. PlayMode filtrado: **18/19**, o teste novo de profundidade mostrou o sinal invertido (Decisão 2); captura com glints ainda ocultos ([attempt3](graphical/attempt3-depth-sign-away/)) |
| 9 | `run.sh … Apply run6-depth-sign`, `run7`, `Validate validate3` | prefab salvo (z do glint +3, do nível +0,2), depois 0/0/0; Validate ok nas 4 cenas e no prefab |
| 10 | PlayMode filtrado | **19/19** ([targeted/final](targeted/final/)) |
| 11 | Captura sem `-batchmode` | **1/1**: 14 capturas e `diagnostics.txt` ([graphical/captures](graphical/captures/)) |
| 12 | Commit `a954300`; suítes completas, `DUNGEON_CRAWLER_TEST_REPORT_DIR` isolado | EditMode **291/291** (exit 0); PlayMode **212 passed, 0 failed, 18 skipped** (exit 0) |
| 13 | `python -I graphical/crop_sheet.py graphical/captures graphical/states_crops_real.png` | Prancha de recortes reais (feita dos PNG originais, antes da conversão para JPEG) |

## Leitura visual (captura real, release, 720p/1080p)

[Prancha](graphical/states_crops_real.png) e [zoom das variantes em 1080p](graphical/loot_variants_1080p_zoom4x.png):

- **Cura (quadro 3):** motes rubi→marfim e anel baixo nos pés, sem poça e sem grade visível. O texto `+25 HP` aparece.
- **Nível (quadro 5):** anel ciano com o arco da frente diante das pernas e motes de latão dos lados; centro livre.
- **Coleta:** estrelas de 4 pontas sobre a barra da capa; o ciano (XP) é claro. **Ouro e marfim ficam parecidos** (lilás) sobre o azul saturado da capa, e 2 dos 5 glints continuam atrás do corpo. As células estão certas (UV); a fraqueza é de leitura. O aviso de texto continua dizendo o tipo.
- **Flash 0:** nenhuma partícula, o aviso aparece.

## Efeitos colaterais preservados e não commitados

São os mesmos da V02–V04: cada processo da Unity regrava `ProjectSettings.asset` e `EditorBuildSettings.asset`; as suítes regravam cenas binárias (incluindo `Floor_02_Fixed`), prefabs de inimigo/boss, `Mat_Vase_F09.mat`, `RunCheckpointCatalog.asset` e `Reports/Leadership/*`. Tudo foi listado em `*/side-effects-status.txt` e restaurado para a HEAD depois de cada etapa. O diff da branch contra a `main` não contém nenhum desses arquivos. `CombatProfiler.raw` foi removido; as capturas foram convertidas de PNG para JPEG q88.

## Limites e próximos passos

- **Sem aprovação artística humana** nem monitor físico; a captura usa tamanhos emulados do Game view. Cura/XP são comandos diagnósticos, não uma run normal.
- **Leitura dos glints (sugestão para revisão de arte):** subir um pouco o latão do ouro (mais saturado ou mais claro) ou abrir o raio para 0,3 m, dentro da meta de 0,35 m, melhoraria ouro × marfim sobre a capa. Isso muda a arte ou o contrato, então não foi feito aqui.
- **GC:** medição sintética no Editor (GC.Alloc Count, não bytes); não representa o frame time da build.
- **Build Windows** não foi refeita nesta tarefa.
