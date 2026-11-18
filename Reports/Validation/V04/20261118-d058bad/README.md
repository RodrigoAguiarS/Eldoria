# V04 — Aplicar UI-SKIN (A03-I)

- **Execução:** 18/11/2026, por Claude, com a tarefa V04 delegada pelo usuário (no catálogo, o executor previsto era o Codex). Branch `claude/v04-ui-skin`, criada de `main@d058bad` (= `origin/main`) com a árvore limpa ([base-commit.txt](base-commit.txt)).
- **Commit testado:** `ea0ac94` ([tested-commit.txt](tested-commit.txt)), que traz código, testes, `UiSkin.asset`, as quatro cenas salvas pelo integrador e as `.meta` do UI-SKIN. A captura gráfica rodou com uma única mudança a mais, ainda não commitada na hora: o nome de diagnóstico do teste de captura ficou mais longo (80 → 400 `M`) para forçar rolagem a 100%. Essa mudança entra no commit seguinte junto com os documentos de estado, e não altera código de produção.
- **Ambiente:** Windows 11 Pro e Unity **6000.6.9f1** (`C:/Program Files/Unity/Hub/Editor/6000.6.9f1`). Nenhuma outra instância do Editor estava aberta (sem `Temp/`). Pacotes conforme manifest/lock, sem alteração.
- **Validado no Editor:** compilação, integrador e validador por `-executeMethod`, snapshot antes/depois, EditMode e PlayMode completos em batchmode, e a matriz de capturas no Editor com janela (sem `-batchmode`).
- **Não feito:** aprovação artística humana e observação humana em monitor físico. A matriz usa tamanhos emulados no Game view.

## Resultado por critério

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Snapshot comprova geometria e targets preservados | **Atendido.** O snapshot cobre todo RectTransform dos canvases raiz das 4 cenas: âncoras, pivô, posição, tamanho, rotação, escala, `activeSelf`, `raycastTarget` de cada Graphic, `interactable` e `Navigation` (modo e alvos explícitos) de cada Selectable, e os `onClick` persistentes. **Antes (cenas da `main`) = depois da aplicação**, 1170 linhas, diff vazio. Ficam de fora só os enfeites que o brief manda criar ou remodelar: `RarityBand`, `EquippedMarker`, `EmptyMarker` e `SelectionFrame` (com as arestas e o `Marker`). Os testes conferem que esses enfeites não têm Selectable nem raycast | [snapshot/before.txt](snapshot/before.txt), [snapshot/after-apply-only.txt](snapshot/after-apply-only.txt), [diff vazio](snapshot/before-vs-after-apply-only.diff); testes `SkinPass_OnUnskinnedUi_…` e `SavedScenes_…` |
| Estados de botão, vazio/seleção/equipado/raridade visíveis | **Atendido.** Em PlayMode, com teclado real, no andar de release: o botão em foco mostra `Button-Focus`, e as setas movem o foco e devolvem o anterior a `Button-Normal`. Com a bolsa vazia, Equipar/Desequipar/Usar/Largar mostram `Button-Disabled` com `Image.color` branca. Nos slots: selecionado → `Slot-Selected`, `SelectionFrame.activeSelf` e `Marker-Selected`; equipado → escudo; vazio → `Slot-Empty` e octógono; raridade → faixa com `ItemRarity.color` (Épico, Raro e Comum conferidos); a seleção segue as setas na grade | `UiSkinSceneTests.PauseButtons_…` e `UiSkinSceneTests.Slots_…` em [playmode/PlayMode.xml](playmode/PlayMode.xml); recortes reais em [graphical/states_crops_real.png](graphical/states_crops_real.png) |
| Title/bolsa rolada/opções/controles/HUD legíveis a 720p/1080p × 100%/150% | **Atendido tecnicamente; falta aprovação humana.** 28 capturas (title, options, controls, hud, pause, bag, bag-scrolled) nas 4 combinações. O teste confere que os textos cabem nos retângulos, que cada tela usa o sprite certo e que a coluna de leitura rola (conteúdo 870–1776 sobre 700). Bordas medidas: botão 8 px, janela 10,7 px e slot 5,3 px em 720p; 12/16/8 px em 1080p. Nas imagens, as bordas esticam sem deformar | [graphical/results.xml](graphical/results.xml), [graphical/captures/](graphical/captures/), [measurements.txt](graphical/captures/measurements.txt) |
| Reaplicação estável | **Atendido.** 2ª execução: "0 cenas salvas" e 0 arquivos diferentes em `Assets/` e `ProjectSettings/`. Depois de reaplicar a F10 (que passa pelo gancho da skin), o integrador também salva 0 cenas. O teste EditMode `TextScaleTests` (F10 integrada duas vezes, bytes iguais) continua passando com o gancho ativo | [idempotency/runs.txt](idempotency/runs.txt), `*.sha256` |

Outros pontos do brief:

- **Compila sem erros novos; cenas sem Missing Script.** `Validate UI Skin (A03)` passou nas 4 cenas ([validate1.log](idempotency/validate1.log)) e também confere a importação contra `UI-SKIN.states.json`: Sprite Single, Full Rect, PPU 100, mipmaps off e bordas 16/12/8/0.
- **Regressão:** EditMode **286/286**, PlayMode **203 passed, 0 failed, 17 skipped**. Os skips são os 16 `[Explicit]` já listados na V03 mais a captura nova, executada à parte. `InventorySceneUiTests`, `ItemsCombatSceneTests`, `TextScaleTests`, `TextScaleInputTests`, `InputBindingTests` e `EquipmentComparisonTests` passaram.
- **Teclado:** o foco inicial da pausa (Continuar), da bolsa (Slot_00) e a navegação por setas foram conferidos em PlayMode. `Navigation` de todos os Selectables está no snapshot. Gamepad completo não foi adicionado (fora do escopo); a navegação é a mesma, porque nada nela mudou.
- **Prefabs:** nenhum alvo está em prefab ou instância de prefab. O integrador confere isso antes de mexer em qualquer coisa e para com erro se encontrar um.
- **Código que pinta botões em runtime:** só `InventorySlotView.Render` escrevia cor (agora troca sprite quando há skin). `TalentChoicePresenter` só muda `interactable`. `TalentSceneBuilder` define `colors.disabledColor`, que não tem efeito com SpriteSwap.

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/UI/UiSkin.cs` (novo) | ScriptableObject só com referências de sprite: painel janela/inset, botão normal/hover/foco/pressionado/desabilitado, slot normal/hover/selecionado/vazio/faixa e marcadores selecionado/equipado/vazio |
| `Assets/_Project/ScriptableObjects/UI/UiSkin.asset` (novo) | Criado e preenchido pelo integrador, de forma idempotente |
| `Assets/_Project/Scripts/Editor/UiSkinIntegrator.cs` (novo) | Menus `Tools/Dungeon Crawler/UI/Apply UI Skin (A03)` e `Validate UI Skin (A03)`. `ConfigureScene(Scene)` para o gancho. Sprites por `AssetDatabase.LoadAssetAtPath` com caminho constante. Só salva a cena quando há mudança. Também expõe o snapshot de alvos (`WriteTargetSnapshot`) usado nesta validação |
| `Assets/_Project/Scripts/UI/InventorySlotView.cs` | Campos opcionais `skin`, `rarityBand`, `equippedMarker`, `emptyMarker`. Com skin, `Render` troca a base (selecionado > preenchido/vazio) e mostra os overlays por estado. **Sem skin, o tingimento antigo continua igual**, e os overlays ficam ocultos |
| `Assets/_Project/Scripts/Editor/TextScaleSceneBuilder.cs` | Uma linha no fim de `ConfigureScene`, protegida por "skin asset existe" |
| `MainMenu.unity`, `MVP_Arena.unity`, `Floor_01.unity`, `Floor_01_GeneratedCharacters.unity` (release) | Salvas só pelo integrador. `MainMenu` é texto; as outras três são binárias |
| `UI-SKIN-*.png.meta` (16) | Reserializadas pela importação da Unity: `spriteSheet`/`internalIDToNameTable`, mesmo GUID, mesmas configurações. Ver Decisões |
| `Tests/EditMode/UiSkinTests.cs`, `Tests/PlayMode/PlayerInput/UiSkinSceneTests.cs` (novos) | 5 casos EditMode, 2 PlayMode funcionais e 1 captura `[Explicit]` |
| `ASSET_MANIFEST.json` (UI-SKIN e lote `V04-UI-SKIN-INTEGRATION`), `NEXT_STEPS.md` | Estado V04 |

**Regras aplicadas.** Todo Image alvo recebe `Sliced`, `fillCenter`, multiplicador 1 e `color` branca. `raycastTarget`, RectTransform, `Navigation` e `onClick` dos alvos não são tocados. O rótulo dos botões continua `#0A0F14`. Os slots continuam com `Transition.None`. O overlay escuro de tela cheia (`InventoryPanel`, `PausePanel`, `OptionsPanel`, `ControlsPanel`, `TalentPanel`) continua com a cor atual; só a `Window` dentro dele recebe `Panel-Window`.

## Divergências e decisões

1. **F14 precisa de tarefa complementar.** O brief cobre as 4 cenas de `ControlBindingsSceneBuilder.Scenes`. `Floor_02_Fixed` (F14) é uma cópia do shell do release, está no percurso do build e ficou sem skin: o validador relata **65 pendências** ali, sem falhar ([validate1.log](idempotency/validate1.log)). Ao descer para o segundo andar, botões e janelas voltam ao visual antigo. Nenhum builder atual passa a F10 nessa cena, então o gancho não a alcança sozinho. A correção é pequena (incluir o caminho no `Apply`/`Validate`, rodar e validar), mas exige mexer numa cena fora da lista do brief. Por isso não fiz.
2. **Botões incluídos além do `Button()`.** `RestartButton` (derrota) e `PlayAgainButton` (vitória) vêm de `DungeonMvpSceneBuilder`/`DungeonFloorSceneBuilder`, mas têm a mesma forma (Image alvo e Text `Label`) e ficam no mesmo painel que o `MainMenu` criado por `Button()`. O integrador reconhece a forma, não quem criou o botão. Sem isso, o mesmo painel teria um botão com skin e outro cinza-claro.
3. **Janelas de Controles e Talentos.** O brief lista as janelas de inventário, pausa e opções. A regra 4 ("Window recebe Panel-Window") é genérica, então `ControlsPanel/Window` e `TalentPanel/Window` também recebem o painel. Os botões dessas telas já estavam cobertos pela passagem de botões.
4. **`.meta` do UI-SKIN versionadas.** Toda execução da Unity regrava as 16 `.meta` (a V03 já registrava isso e deixou para a V04). O GUID e as configurações não mudam. As referências da cena e do `UiSkin.asset` usam `fileID 21300000`, então não dependem do `internalID` novo. Versioná-las só evita que a mudança volte em toda execução. Se preferir manter as `.meta` originais do A03, dá para tirar do PR sem afetar o resultado.
5. **Posição do octógono de vazio.** A ficha cita a área do Icon como (6,−20) 64×64, mas a F10 move o Icon para (6,−24) 60×60. O integrador lê o Icon atual e centraliza o marcador nele, em vez de usar o número antigo.
6. **Deriva da F10 não commitada.** Reaplicar a F10 estica o `Label` do botão `NextFloorF14` (`VictoryPanel`, release), que a F14 criou depois da última passada da F10. A mudança não vem da skin: o snapshot "antes" só difere do "depois da F10" nessa linha ([before-vs-after.diff](snapshot/before-vs-after.diff)). As cenas foram restauradas para a `main` e a skin foi aplicada sem a F10 (`run4-clean`), para o diff do PR ter só o efeito do integrador.
7. **Cenas binárias não são byte a byte determinísticas na primeira aplicação.** `run1` e `run4-clean`, partindo da mesma `main`, dão bytes diferentes nas três cenas com slots, porque a Unity sorteia fileIDs locais para objetos novos (`RarityBand` e os marcadores). `MainMenu`, que não tem slots, dá bytes iguais. Reaplicar sobre uma cena que já tem skin dá bytes idênticos (run2, run3).

## Comandos executados

Todos a partir de `G:/DungeonCrawler/My project`, um de cada vez, com `Unity.exe -projectPath …`. `run.sh <Classe.Método> <rótulo>` roda `-batchmode -nographics -executeMethod DungeonCrawler.Editor.<…> -quit` e grava os hashes de `Assets/` e `ProjectSettings/` ([idempotency/run.sh](idempotency/run.sh)).

| # | Comando | Resultado |
| --- | --- | --- |
| 0 | `run.sh UiSkinIntegrator.WriteTargetSnapshot snapshot-before` (2 rascunhos) | exit 1: `CS0117` (helpers `internal` do builder usados no teste) e `CS7036`; corrigidos. Logs em [attempt0-compile-error.log](idempotency/attempt0-compile-error.log) e [attempt0b-compile-error.log](idempotency/attempt0b-compile-error.log) |
| 1 | `DUNGEON_CRAWLER_V04_SNAPSHOT=…/before.txt run.sh UiSkinIntegrator.WriteTargetSnapshot snapshot-before` | exit 0, 1170 linhas das cenas da `main` |
| 2 | `run.sh UiSkinIntegrator.Apply run1` | exit 0, "4 cenas salvas" |
| 3 | `run.sh UiSkinIntegrator.Apply run2` | exit 0, "0 cenas salvas"; **run2 = run1** |
| 4 | `run.sh UiSkinIntegrator.Validate validate1` | exit 0; 4 cenas válidas; F14: 65 pendências (informativo) |
| 5 | `run.sh TextScaleSceneBuilder.Integrate f10-reapply`, depois `run.sh UiSkinIntegrator.Apply run3-after-f10` | exit 0 e 0; F10 regrava 3 cenas; **run3 = após F10**, "0 cenas salvas" |
| 6 | `…WriteTargetSnapshot snapshot-after` | diff de 1 linha (`NextFloorF14/Label`, da F10; Decisão 6) |
| 7 | `git checkout HEAD -- <4 cenas>`, `run.sh UiSkinIntegrator.Apply run4-clean`, `…WriteTargetSnapshot snapshot-after-apply-only` | exit 0; "4 cenas salvas"; **diff vazio** contra o snapshot "antes"; o snapshot não grava nada (hashes iguais) |
| 8 | Commit `ea0ac94` | — |
| 9 | `-batchmode -nographics -runTests -testPlatform EditMode`, `DUNGEON_CRAWLER_TEST_REPORT_DIR=editmode/perf` | **286 passed**, 0 failed, 0 skipped. Exit 0 |
| 10 | `-batchmode -nographics -runTests -testPlatform PlayMode`, `DUNGEON_CRAWLER_TEST_REPORT_DIR=playmode/perf` | 220 casos: **203 passed, 0 failed, 17 skipped**. Exit 0 |
| 11 | Sem `-batchmode`: `-runTests -testPlatform PlayMode -testFilter …UiSkinSceneTests.Capture_SkinnedScreens720pAnd1080pAt100And150` | 1ª tentativa: **falhou** a 1280×720/100% (conteúdo 534 < 700, o nome longo não forçava rolagem), guardada em [graphical/attempt0-short-name/](graphical/attempt0-short-name/). Com o nome mais longo: **1/1 passed**, 28 capturas e `measurements.txt` |
| 12 | `python -I graphical/crop_sheet.py graphical/captures graphical/states_crops_real.png` | Prancha de recortes reais (feita a partir dos PNG originais, antes da conversão) |

## Efeitos colaterais preservados e não commitados

São os mesmos que a V02/V03 registraram. Cada processo da Unity regrava `ProjectSettings.asset` (`SENTIS_ANALYTICS_ENABLED`) e `EditorBuildSettings.asset` (EOL). As suítes também regravam cenas binárias (incluindo `Floor_02_Fixed`), prefabs de inimigo, `Player.prefab`, `Mat_Vase_F09.mat`, `RunCheckpointCatalog.asset` e `Reports/Leadership/*`. Tudo foi listado em `*/side-effects-status.txt` e restaurado para a HEAD depois de cada etapa. O diff final da branch contra a `main` não contém nenhum desses arquivos.

Artefatos removidos ou convertidos: `playmode/perf/CombatProfiler.raw` foi removido, e os JSONs de desempenho foram mantidos. As capturas foram convertidas de PNG para JPEG q88; os recortes PNG vêm dos originais.

## Limites e próximos passos

- **Sem aprovação artística humana** e sem observação em monitor físico. A matriz usa tamanhos emulados no Game view. Ultrawide, 125% e a tela de Talentos não entraram nas capturas (Talentos está coberta pelos testes de regra e pelo validador).
- **Foco de botão sutil.** `Button-Focus` difere do normal só por capas claras nos cantos. A 1080p dá para ver; a 720p fica discreto ([states_crops_real.png](graphical/states_crops_real.png)). A ficha recomenda somar `FocusCorner` ×4, que o brief deixou fora. Sugestão de **A03-I2**: `FocusCorner` nos botões, hover de slot (`Slot-Hover`) e foco de slot. No slot, foco é igual a seleção, porque `OnSelect` já seleciona o item.
- **Foco inicial em Opções é um Slider** (Música). Slider, dropdown, toggle e o trilho/alça da rolagem não têm skin (fora do escopo), então o foco ali continua com o visual antigo.
- **Cenas binárias:** não dá para ler o diff. As referências da skin nas 4 cenas são verificadas por `SavedScenes_ReapplyWithoutChangesAndPreserveTargets` e pelo validador.
- **F14:** tarefa complementar descrita na Decisão 1.
