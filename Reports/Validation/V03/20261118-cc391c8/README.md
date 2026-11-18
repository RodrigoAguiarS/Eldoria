# V03 — Integrar ícones de dash e giro (A01/A02)

- **Execução:** 18/11/2026, por Claude, com a tarefa V03 delegada pelo usuário (no catálogo, o executor previsto era o Codex). Branch `claude/v03-action-icons`, criada de `main@cc391c8` (= `origin/main`) com a árvore limpa.
- **Commit testado:** `c071c55` ([tested-commit.txt](tested-commit.txt)). Commits da branch: `a902e59` (integração), `dd837ac` (importação conforme a ficha e espera do dash nos testes) e `c071c55` (remove `.meta` do UI-SKIN que entraram por engano em `dd837ac`). Os documentos de estado vêm no commit seguinte, e esse commit não altera código.
- **Ambiente:** Windows 11 Pro e Unity **6000.6.9f1** (`C:/Program Files/Unity/Hub/Editor/6000.6.9f1`). Nenhuma outra instância do Editor estava aberta (não havia `Temp/`). Pacotes conforme manifest/lock, sem alteração.
- **Validado no Editor:** compilação, integrador por `-executeMethod`, EditMode e PlayMode completos em batchmode, e a captura gráfica no Editor com janela (sem `-batchmode`).
- **Não feito:** aprovação artística humana e observação humana em monitor físico. A matriz gráfica usa tamanhos emulados no Game view.

## Resultado por critério

| Critério | Resultado | Evidência |
| --- | --- | --- |
| Ícones legíveis a 32/48 px, distinguíveis em cinza e sem halo | **Atendido tecnicamente; falta aprovação humana.** Medido no Editor: 32×32 px em 720p e 48×48 px em 1080p (48 unidades lógicas, CanvasScaler 1920×1080). Os recortes reais têm contorno escuro sem franja clara. As silhuetas (cunha × círculo com diagonal) seguem distintas em cinza, e o estado de espera fica visivelmente mais escuro que o pronto | [graphical/icons_crops_real.png](graphical/icons_crops_real.png), [graphical/captures/measurements.txt](graphical/captures/measurements.txt), [legibility/](legibility/icon_sampling.json) |
| Rótulos e estados corretos; cliques de UI não geram ataques | **Atendido.** Pronto, recarga (Ns), sem estamina, pausa (congelado), morto (indisponível) e remapeamento (G/Y) foram verificados no andar de release com input real. O clique em **Continuar** na pausa gera 0 ataques, e os ícones não são alvo de raycast de UI | `ActionIconHudTests.States_FollowReadyCooldownNoStaminaPauseRemapAndDeath` em [playmode/PlayMode.xml](playmode/PlayMode.xml) |
| PlayMode observado em 720p/1080p × 100%/150% | **Atendido por captura automatizada no Editor gráfico** (1/1): 4 combinações no release e 720p/1080p a 150% em F14, com os estados pronto, recarga, sem estamina e pausa. O teste confere o tamanho físico, se o rótulo cabe e a ausência de sobreposição com outros blocos do HUD | [graphical/results.xml](graphical/results.xml), [graphical/captures/](graphical/captures/) |
| Integração idempotente | **Atendido.** A 2ª execução e a execução depois de reaplicar a F10 deram **0 arquivos diferentes** em `Assets/` e `ProjectSettings/`. O rebuild completo do HUD (`InventorySceneBuilder.ConfigureHud`) reaplica os ícones sem duplicar | [idempotency/runs.txt](idempotency/runs.txt), testes `Integration_…` e `FullHudRebuild_…` |

## O que foi feito

| Arquivo | Mudança |
| --- | --- |
| `Assets/_Project/Scripts/Editor/ActionIconSceneBuilder.cs` (novo) | Integrador idempotente, no menu `Tools/Dungeon Crawler/UI/Integrate Action Icons (A01-A02)`. Confere a importação e corrige só o que diverge da ficha. Cria `PlayerResource/Icon` (A02) e `DashBar/DashAction/{Icon,Label}` (A01), liga as referências explícitas e registra o rótulo novo no `UiTextScale` (base de 20 px). Só salva a cena quando há mudança |
| `Assets/_Project/Scripts/UI/ActionIconStyle.cs` (novo) | Estado (`Ready/Cooldown/NoStamina/Unavailable`) e tons compartilhados. Só apresentação |
| `Assets/_Project/Scripts/UI/DashCooldownPresenter.cs` | Campos opcionais `icon`, `label`, `bindings`, `health`. A barra continua igual. Rótulo: `DASH [binding] pronto / Ns / indisponível` |
| `Assets/_Project/Scripts/UI/PlayerResourcePresenter.cs` | Campos opcionais `icon`, `health`. O texto anterior foi mantido, e só o estado morto passou a mostrar `indisponível`. O cache inclui o estado |
| `Assets/_Project/Scripts/Editor/InventorySceneBuilder.cs` | **+2 linhas (divergência registrada abaixo):** chama `ActionIconSceneBuilder.ConfigureHud` antes da F10 |
| `ICON-ACTION-DASH.png.meta`, `ICON-ACTION-SPECIAL.png.meta` | Reserializados pela importação da Unity (`spriteSheet`/`internalIDToNameTable`, mesmo GUID). As configurações continuam as da ficha |
| `MVP_Arena.unity`, `Floor_01.unity`, `Floor_01_GeneratedCharacters.unity` (release), `Floor_02_Fixed.unity` (F14) | Salvas pelo integrador via API do Editor. As cenas são binárias, ver os limites |
| `Tests/EditMode/ActionIconTests.cs`, `Tests/PlayMode/PlayerInput/ActionIconHudTests.cs` (novos) | 12 casos EditMode, 2 PlayMode funcionais e 1 captura `[Explicit]` |
| `ASSET_MANIFEST.json` (A01/A02), `NEXT_STEPS.md`, `MVP.md` | Estado V03 |

**Layout.** Em 1080p, o ícone do giro ocupa (6, −6) dentro de `PlayerResource` (660×100). O rótulo nativo ganhou recuo esquerdo de 62 unidades, e o contêiner não foi movido nem redimensionado. O dash é filho da `DashBar`: fica à direita dela, centralizado na barra, e acompanha qualquer reposicionamento da F10. Os tons usam branco no estado pronto, cinza 0,42 com α 0,72 em recarga e sem estamina, e cinza 0,28 com α 0,45 quando morto. O estado também aparece em texto, então não depende só de cor. Ícones e rótulos têm `raycastTarget = false`, a mesma regra do `PotionHud`.

**Regras inalteradas.** Cooldown do dash (2 s), custo do giro (30), cooldown (6 s), alcance e movimento não foram tocados. Os testes conferem custo e cooldown em PlayMode.

## Divergências e decisões

1. **Gancho em `InventorySceneBuilder.ConfigureHud`, fora da lista literal de arquivos (builders de HUD eram "fontes").** Sem o gancho, `PlayerSpecialSceneBuilder.ConfigureHud` volta o rótulo do giro para `offsetMin (0, 9)` a cada rebuild do HUD, e o texto passa a ficar sobre o ícone. `DungeonMvpSceneBuilder` e `DungeonFloorSceneBuilder` destroem o HUD e perdem os ícones. Esse é o mesmo tipo de defeito de composição que a V02 encontrou. A mudança tem duas linhas e segue o padrão dos outros integradores, como `ConfigureItemVisuals`, `RewardFeedbackSceneBuilder` e `TalentSceneBuilder`.
2. **Mipmaps: segui a ficha (off) e não alterei o requisito.** A primeira tentativa (`a902e59`) ligou os mipmaps. O `ASSET_MANIFEST` → `technical_requirements` de A01/A02 pede "mipmaps off", então revertemos em `dd837ac`. A análise está em [legibility/](legibility/), e é uma recomendação para você decidir:
   - Sem mip, o 512→48 bilinear fica 20–30% mais longe da redução ideal por área do que com mip (`icon_sampling.json`, erro médio 2,8–11,7/255 contra 2,3–5,6 a 48 px). A 32 px, a cadeia de mips coincide com a redução por área (512/32 = 2⁴).
   - Visualmente, sem mip o contorno fica mais nítido, mas com degraus e trechos interrompidos (cauda do arco a 32 px). Com mip fica mais suave e menos contrastado ([sampling_sheet.png](legibility/sampling_sheet.png)).
   - Como os ícones do HUD são estáticos, não há cintilação. Os recortes reais da Unity sem mip foram considerados legíveis. Mudar exige atualizar a ficha.
3. **Cenas de protótipo F13/F15 (`F13_Lancer_Test`, `F15_StoneSentinel_Test`) não foram integradas.** Elas estão fora do percurso e o brief pede só a F14. O `LancerTestSceneBuilder` usa `InventorySceneBuilder.ConfigureHud`, então ganha os ícones se for reexecutado.

## Comandos executados

Todos foram executados a partir de `G:/DungeonCrawler/My project`, um de cada vez, com `Unity.exe -projectPath …`.

| # | Comando | Código | Resultado |
| --- | --- | --- | --- |
| 0 | `-batchmode -nographics -executeMethod DungeonCrawler.Editor.ActionIconSceneBuilder.Integrate -quit` | rascunho | exit 1, `CS0103 MipmapFilter`, corrigido ([attempt1/…/attempt0-compile-error.log](attempt1-a902e59/idempotency/attempt0-compile-error.log)) |
| 1 | Tentativa 1 completa: integrar 2×, F10 → integrar, EditMode, PlayMode | `a902e59` (mipmaps on) | EditMode 281/281. PlayMode 199/2 failed/16. As 2 falhas eram dos testes novos: E pressionado durante o dash, que `PlayerSpecial.cs:76` recusa por regra. Guardado em [attempt1-a902e59/](attempt1-a902e59/) e **não atribuído ao resultado final** |
| 2 | `idem.sh ActionIconSceneBuilder.Integrate run1` | `dd837ac` | exit 0. "2 importações ajustadas, 0 cenas salvas" (só `enableMipMap` 1→0) |
| 3 | `idem.sh … run2` | `dd837ac` | exit 0, "0 / 0". **run2 = run1** |
| 4 | `idem.sh TextScaleSceneBuilder.Integrate f10-reapply` e depois `idem.sh ActionIconSceneBuilder.Integrate run3-after-f10` | `dd837ac` | exit 0 e 0. **run3 = após F10**, "0 importações, 0 cenas salvas" |
| 5 | `-batchmode -nographics -runTests -testPlatform EditMode`, `DUNGEON_CRAWLER_TEST_REPORT_DIR=editmode/perf` | `c071c55` | **281 passed**, 0 failed, 0 skipped. Exit 0 |
| 6 | `-batchmode -nographics -runTests -testPlatform PlayMode`, `DUNGEON_CRAWLER_TEST_REPORT_DIR=playmode/perf` | `c071c55` | 217 casos: **201 passed, 0 failed, 16 skipped**. Exit 0 |
| 7 | Sem `-batchmode`: `-runTests -testPlatform PlayMode -testFilter DungeonCrawler.Tests.PlayerInput.ActionIconHudTests.Capture_ActionIcons720pAnd1080pAt100And150` | `c071c55` | **1/1 passed**, 24 capturas e `measurements.txt` |
| 8 | `python -I legibility/icon_sampling.py <A01.png> <A02.png> legibility` e `python -I graphical/crop_sheet.py graphical/captures graphical/icons_crops_real.png` | — | Análise de amostragem e prancha dos recortes reais (feita a partir dos PNG originais, antes da conversão) |

Os 16 skips do PlayMode são os 15 `[Explicit]` listados na V02 mais `ActionIconHudTests.Capture_…`, que foi executado à parte no passo 7. Os 12 casos EditMode e os 2 PlayMode novos aparecem como `Passed` nos XMLs.

## Efeitos colaterais preservados e não commitados

Os mesmos que a V02 registrou. Cada processo da Unity regrava:

- 16 `.meta` do **UI-SKIN** (A03, escopo da V04);
- `ProjectSettings.asset` (`SENTIS_ANALYTICS_ENABLED`);
- `EditorBuildSettings.asset` (EOL).

As suítes também regravam cenas binárias, prefabs de inimigo (`lunge`), `Player.prefab`, `MainMenu.unity` e `Reports/Leadership/*`. A F10 reaplicada regrava release, `Floor_01`, `MVP_Arena` e `MainMenu`, com a deriva já conhecida. Tudo foi registrado em `*/side-effects-status.txt` e [integrate-side-effects.patch](attempt1-a902e59/idempotency/integrate-side-effects.patch), e restaurado para a HEAD depois de cada etapa. O commit `dd837ac` levou os 16 `.meta` do UI-SKIN por engano, e `c071c55` devolveu esses arquivos à versão da `main`. O diff final da branch contra a `main` não contém nenhum desses arquivos.

Artefatos removidos ou convertidos: `playmode/perf/CombatProfiler.raw` (2,4 MB) foi removido, e os JSONs de desempenho foram mantidos. As 24 capturas de tela inteira foram convertidas de PNG para JPEG q88 (de 14 MB para 3 MB). A fidelidade de pixel dos ícones está nos recortes PNG, extraídos dos originais.

## Limites

- **Sem aprovação artística humana** e sem observação humana em monitor físico. A matriz usa tamanhos emulados no Game view.
- **Cenas binárias:** não dá para ler o diff. A presença e as referências dos ícones são verificadas por `SavedScene_WiresBothIconsByExplicitReference` nas quatro cenas. Não está comprovado que a regravação não leva junto a deriva antiga apontada pela V02 (`lunge`). O aumento por cena é de cerca de 3,3 KB, e nenhum dado é removido.
- **Ultrawide e 125%** não entraram na matriz. A F10 cobre esses casos para o restante do HUD. O rótulo do dash tem 504 unidades de largura e cabe a 150% em uma linha.
- O clique sobre a área de um ícone conta como clique de gameplay (ataque), porque o ícone é decorativo e não captura raycast. É o mesmo comportamento do resto do HUD.
