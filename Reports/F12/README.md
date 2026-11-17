# F12 — Talento por nível: evidências e limites

Base `151c675` (main), branch `claude/f12-level-talents`, Unity 6000.6.9f1 em
batchmode `-nographics`. Regras e valores: [TALENTS-F12](../../docs/TALENTS-F12.md).

## Resultados executados

| Arquivo | Escopo | Resultado |
| --- | --- | --- |
| [editmode-full-final.xml](editmode-full-final.xml) | EditMode completo (código final) | **227/228**; 1 falha pré-existente da F10 (abaixo) |
| [editmode-full-1.xml](editmode-full-1.xml) | EditMode completo, 1ª execução | 227/228, mesma falha |
| [playmode-full.xml](playmode-full.xml) | PlayMode completo, 1ª execução | 162 aprovados, 1 falha F12 (corrigida), 11 capturas explícitas ignoradas |
| [playmode-retest.xml](playmode-retest.xml) | F12 + checkpoint + menus/pausa/escala após correção | 23/23, 1 captura ignorada |
| [editmode-f12-final.xml](editmode-f12-final.xml) | Talentos, checkpoint, progressão, comparação F01, especial | 71/71 |
| [playmode-committed-state.xml](playmode-committed-state.xml) | F12, checkpoint, menus, pausa, escala, aceite M1, andar | **37/37**, 1 captura ignorada |

`committed-state.sha256` registra prefab, três cenas e catálogo de checkpoint
commitados; o PlayMode final rodou sobre esses bytes e não os alterou. Uma nova
execução do builder sobre esse estado não muda nenhum byte (ponto fixo).

A falha do 1º PlayMode completo vinha do próprio teste novo: `Restart()` recusou
porque o diretório isolado do checkpoint não existia e `File.Delete` lança
`DirectoryNotFoundException`. O teste agora cria o diretório. Em produção
`persistentDataPath` sempre existe; o comportamento do F11 não foi alterado.

## Cobertura do aceite

- **+10%/nível íntegro:** `DamageMultiplier` permanece 1,1/1,2/1,3; talento é
  `TalentDamageMultiplier` separado. Ferro +6 no nível 2 com Lâmina 1 = 26 × 1,1 × 1,05.
- **Cura de nível e armas:** dois níveis numa coleta curam 2 × 40% e emitem um
  `LeveledUp`; bônus da arma inalterado. Asserts confirmam que as definições de
  especial e armadura não são editadas pelos talentos.
- **Escolha única / repetida:** cada escolha aceita gasta uma; sem pendência é recusada;
  duplo clique dentro de 0,3 s não gasta a segunda escolha enfileirada.
- **Sem reentrância:** `TryChoose` chamado dentro de `Chosen` é recusado.
- **Reativação:** desabilitar/reabilitar talentos e inventário 5× mantém dano,
  defesa e cooldown idênticos (atribuição absoluta).
- **Cancelar UI:** Esc/Voltar retorna à Pausa sem alterar ranks ou pendências.
- **Morte/reinício:** morto não escolhe nem abre a tela; menus fecham. Reiniciar
  cria jogador sem talentos e multiplicadores 1.
- **Retorno ao estado base:** restauração vazia zera os três termos; desequipar
  armadura deixa só o termo do talento.
- **Checkpoint v2:** round-trip de ranks; v1 sem talentos válido e migrado;
  v1 com talentos, ID desconhecido/duplicado, rank 0 ou acima do máximo, soma acima
  de `nível − 1` e entrada nula rejeitados antes de mutar.

## Correção do HUD após playtest do usuário (16/11/2026)

Na build F12 o usuário observou a barra do Boss no meio da tela (y −230, 56 px,
herdada do F10 e reaplicada pelo builder) e o texto de checkpoint durante o combate.

- Barra do Boss de volta ao topo central: 640×44, y −112 (`TextScaleSceneBuilder`).
- HUD do checkpoint mostra **somente falhas** (`RunCheckpointController.StatusIsError`):
  gravação/descarte falhos e snapshot inválido. Status rotineiros ficam ocultos.
- Aviso de talentos desenhado abaixo das telas de Pausa/Talentos; a reordenação
  alternava a cada execução do builder e foi tornada idempotente (agora testada).
  Aviso 960×60 para caber em uma linha a 150%; cartões de talento 472×320.
- Captura gráfica explícita `TextScaleInputTests.Capture_LayoutsAtAllScalesAndResolutions`
  estendida com Pausa e Talentos: **aprovada** em 1280×720, 1920×1080 e 2560×1080
  emulado × 100/125/150% ([visual-capture.xml](visual-capture.xml), 85 imagens em
  [Visual](Visual)). Inspecionadas: HUD 1080p/720p, Pausa 2560×1080 150%, Talentos
  720p/1080p 150%. As duas primeiras execuções falharam por texto do aviso/dica
  quebrando linha e pela ordem de desenho; ambas corrigidas antes da aprovação.
- Regressão: EditMode 227/228 (mesma falha F10) [editmode-hudfix.xml](editmode-hudfix.xml);
  PlayMode 162/174 com 11 capturas ignoradas e 1 benchmark que recusou sobrescrever a
  medição da execução anterior no mesmo diretório; reexecutado em diretório novo, 1/1
  ([playmode-hudfix.xml](playmode-hudfix.xml), [benchmark](playmode-hudfix-benchmark.xml)).
- Build Windows regenerada: `Succeeded`, 0 erros, 496 avisos; smoke sem gráficos
  20 s sem exceções. Hashes em `windows-build-files.sha256`.

## Limites e pendências

- Validação visual por captura diagnóstica em tamanhos emulados (seção acima);
  não substitui uma partida normal completa nem monitor ultrawide físico. A barra do
  Boss foi vista ativada artificialmente, não durante a luta real.
- **Sem playtest humano nem medição de balanceamento.** Os valores são a proposta
  inicial; não se afirma melhoria de variação ou dificuldade.
- Build Windows gerada depois (commit `7ae0ac8`): `Builds/WindowsMVP/Eldoria.exe`,
  `Succeeded`, 0 erros, 496 avisos de shader ([windows-build.json](windows-build.json),
  hashes em `windows-build-files.sha256`). Smoke sem gráficos com `-dcc-start-floor`:
  menu e andar carregados, 20 s sem exceções. Sem janela/gráficos: não é revisão visual
  nem partida completa. Builds antigas removidas a pedido do usuário; mantido só o ZIP 1.0.
- **Falha pré-existente (já na main, registrada no F11):**
  `ItemsCombatAssetTests.IntegrateTwice_IsIdempotentAndPreservesPickupIdentity`
  com `Inventory slot missing: 0` (`TextScaleSceneBuilder` move os slots).
- Testes de builder antigos (F09/F11/arte) reescrevem cenas, `MainMenu`,
  `Mat_Vase_F09.mat` e `Reports/Leadership/ProductionVisual/validation.json`
  durante o EditMode; esses efeitos foram descartados e o builder F12 foi reaplicado.
  Abrir o Editor acrescenta `SENTIS_ANALYTICS_ENABLED` em `ProjectSettings`;
  revertido, não faz parte desta entrega.
- Componente recém-adicionado recebe `fileID` novo quando o builder parte de uma
  base sem F12; sobre estado já integrado o resultado é idêntico.

## Reprodução

```
Unity.exe -batchmode -nographics -projectPath <projeto> -executeMethod DungeonCrawler.Editor.TalentSceneBuilder.Integrate -quit -logFile <log>
Unity.exe -batchmode -nographics -projectPath <projeto> -runTests -testPlatform PlayMode -testFilter "TalentSceneTests|PlayerTalentsPlayModeTests" -testResults <xml> -logFile <log>
```

Menu equivalente: `Tools/Dungeon Crawler/Progression/Integrate Level Talents (F12)`.
